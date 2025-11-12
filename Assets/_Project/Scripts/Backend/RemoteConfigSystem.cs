using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Backend
{
    /// <summary>
    /// Remote configuration system for runtime config updates.
    /// Fetches configuration from backend and caches locally.
    /// Allows changing game parameters without rebuilding the app.
    /// </summary>
    public class RemoteConfigSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string configEndpoint = "/config";
        [SerializeField] private string environmentEndpoint = "/config/environment";

        [Header("Fetch Settings")]
        [SerializeField] private bool fetchOnStart = true;
        [SerializeField] private bool autoRefresh = false;
        [SerializeField] private float refreshInterval = 300f; // 5 minutes
        [SerializeField] private int requestTimeout = 10;

        [Header("Cache Settings")]
        [SerializeField] private bool cacheConfig = true;
        [SerializeField] private bool useCacheWhenOffline = true;
        [SerializeField] private float cacheExpiration = 3600f; // 1 hour

        [Header("Environment")]
        [SerializeField] private ConfigEnvironment environment = ConfigEnvironment.Production;

        #endregion

        #region Private Fields

        private static RemoteConfigSystem _instance;

        // Configuration state
        private Dictionary<string, ConfigValue> _config = new Dictionary<string, ConfigValue>();
        private Dictionary<string, ConfigValue> _cachedConfig = new Dictionary<string, ConfigValue>();

        // Fetch state
        private bool _isFetching = false;
        private bool _hasFetchedOnce = false;
        private float _lastFetchTime = 0f;
        private DateTime _cacheTimestamp;

        // Statistics
        private int _totalFetches = 0;
        private int _failedFetches = 0;
        private int _cacheHits = 0;

        // Events
        public event Action<Dictionary<string, ConfigValue>> OnConfigFetched;
        public event Action<string> OnConfigFetchFailed;
        public event Action<string, ConfigValue> OnConfigValueChanged;

        #endregion

        #region Properties

        public static RemoteConfigSystem Instance => _instance;
        public bool HasConfig => _config.Count > 0;
        public bool IsFetching => _isFetching;
        public int ConfigCount => _config.Count;
        public ConfigEnvironment Environment => environment;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (fetchOnStart)
            {
                FetchConfig();
            }
        }

        private void Update()
        {
            if (!autoRefresh || _isFetching) return;

            // Auto-refresh config
            if (Time.time - _lastFetchTime >= refreshInterval)
            {
                FetchConfig();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log($"[RemoteConfigSystem] Initializing for {environment} environment...");

            // Load cached config
            if (cacheConfig)
            {
                LoadCachedConfig();
            }

            Debug.Log($"[RemoteConfigSystem] Initialized with {_config.Count} cached values");
        }

        #endregion

        #region Fetching

        /// <summary>
        /// Fetch configuration from backend.
        /// </summary>
        public void FetchConfig(Action onSuccess = null, Action<string> onError = null)
        {
            if (_isFetching)
            {
                Debug.LogWarning("[RemoteConfigSystem] Fetch already in progress");
                onError?.Invoke("Fetch already in progress");
                return;
            }

            StartCoroutine(FetchConfigCoroutine(onSuccess, onError));
        }

        private IEnumerator FetchConfigCoroutine(Action onSuccess, Action<string> onError)
        {
            _isFetching = true;
            _totalFetches++;

            string url = backendUrl + configEndpoint;

            // Add environment query parameter
            url += $"?environment={environment.ToString().ToLower()}";

            // Add user ID if authenticated
            if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                url += $"&userId={UserAuthenticationSystem.Instance.UserId}";
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                // Add auth header if available
                if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {UserAuthenticationSystem.Instance.AuthToken}");
                }

                request.timeout = requestTimeout;

                yield return request.SendWebRequest();

                _isFetching = false;

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<ConfigResponse>(request.downloadHandler.text);

                        // Update config
                        UpdateConfig(response.config);

                        _hasFetchedOnce = true;
                        _lastFetchTime = Time.time;
                        _cacheTimestamp = DateTime.UtcNow;

                        // Cache config
                        if (cacheConfig)
                        {
                            CacheConfig();
                        }

                        OnConfigFetched?.Invoke(_config);
                        onSuccess?.Invoke();

                        Debug.Log($"[RemoteConfigSystem] Config fetched: {_config.Count} values");
                    }
                    catch (Exception ex)
                    {
                        _failedFetches++;
                        string error = $"Failed to parse config response: {ex.Message}";
                        OnConfigFetchFailed?.Invoke(error);
                        onError?.Invoke(error);
                        Debug.LogError($"[RemoteConfigSystem] {error}");
                    }
                }
                else
                {
                    _failedFetches++;
                    string error = $"Config fetch failed: {request.error}";

                    // Fall back to cache if available
                    if (useCacheWhenOffline && _cachedConfig.Count > 0)
                    {
                        Debug.LogWarning($"[RemoteConfigSystem] Using cached config (offline)");
                        _config = new Dictionary<string, ConfigValue>(_cachedConfig);
                        onSuccess?.Invoke();
                    }
                    else
                    {
                        OnConfigFetchFailed?.Invoke(error);
                        onError?.Invoke(error);
                        Debug.LogError($"[RemoteConfigSystem] {error}");
                    }
                }
            }
        }

        /// <summary>
        /// Fetch config for specific environment.
        /// </summary>
        public void FetchEnvironmentConfig(ConfigEnvironment env, Action onSuccess = null, Action<string> onError = null)
        {
            environment = env;
            FetchConfig(onSuccess, onError);
        }

        private void UpdateConfig(ConfigEntry[] entries)
        {
            foreach (var entry in entries)
            {
                ConfigValue oldValue = null;
                _config.TryGetValue(entry.key, out oldValue);

                var newValue = new ConfigValue
                {
                    key = entry.key,
                    stringValue = entry.value,
                    type = entry.type
                };

                _config[entry.key] = newValue;

                // Fire change event if value changed
                if (oldValue == null || oldValue.stringValue != newValue.stringValue)
                {
                    OnConfigValueChanged?.Invoke(entry.key, newValue);
                }
            }
        }

        #endregion

        #region Getters

        /// <summary>
        /// Get string value from config.
        /// </summary>
        public string GetString(string key, string defaultValue = "")
        {
            if (_config.TryGetValue(key, out var value))
            {
                _cacheHits++;
                return value.stringValue;
            }

            Debug.LogWarning($"[RemoteConfigSystem] Key not found: {key}, using default: {defaultValue}");
            return defaultValue;
        }

        /// <summary>
        /// Get int value from config.
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            string strValue = GetString(key, defaultValue.ToString());

            if (int.TryParse(strValue, out int result))
            {
                return result;
            }

            Debug.LogWarning($"[RemoteConfigSystem] Failed to parse int for key: {key}, using default: {defaultValue}");
            return defaultValue;
        }

        /// <summary>
        /// Get float value from config.
        /// </summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            string strValue = GetString(key, defaultValue.ToString());

            if (float.TryParse(strValue, out float result))
            {
                return result;
            }

            Debug.LogWarning($"[RemoteConfigSystem] Failed to parse float for key: {key}, using default: {defaultValue}");
            return defaultValue;
        }

        /// <summary>
        /// Get bool value from config.
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            string strValue = GetString(key, defaultValue.ToString()).ToLower();

            if (strValue == "true" || strValue == "1")
                return true;
            if (strValue == "false" || strValue == "0")
                return false;

            Debug.LogWarning($"[RemoteConfigSystem] Failed to parse bool for key: {key}, using default: {defaultValue}");
            return defaultValue;
        }

        /// <summary>
        /// Get JSON value from config (deserialize to type T).
        /// </summary>
        public T GetJson<T>(string key, T defaultValue = default)
        {
            string json = GetString(key, null);

            if (string.IsNullOrEmpty(json))
            {
                return defaultValue;
            }

            try
            {
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigSystem] Failed to parse JSON for key {key}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if config has a key.
        /// </summary>
        public bool HasKey(string key)
        {
            return _config.ContainsKey(key);
        }

        /// <summary>
        /// Get all keys.
        /// </summary>
        public string[] GetAllKeys()
        {
            var keys = new string[_config.Count];
            _config.Keys.CopyTo(keys, 0);
            return keys;
        }

        #endregion

        #region Caching

        private void CacheConfig()
        {
            try
            {
                var cacheData = new ConfigCacheData
                {
                    timestamp = _cacheTimestamp,
                    environment = environment,
                    entries = new ConfigEntry[_config.Count]
                };

                int i = 0;
                foreach (var kvp in _config)
                {
                    cacheData.entries[i++] = new ConfigEntry
                    {
                        key = kvp.Key,
                        value = kvp.Value.stringValue,
                        type = kvp.Value.type
                    };
                }

                string json = JsonUtility.ToJson(cacheData);
                PlayerPrefs.SetString("RemoteConfig_Cache", json);
                PlayerPrefs.Save();

                _cachedConfig = new Dictionary<string, ConfigValue>(_config);

                Debug.Log($"[RemoteConfigSystem] Cached {_config.Count} config values");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigSystem] Failed to cache config: {ex.Message}");
            }
        }

        private void LoadCachedConfig()
        {
            try
            {
                if (PlayerPrefs.HasKey("RemoteConfig_Cache"))
                {
                    string json = PlayerPrefs.GetString("RemoteConfig_Cache");
                    var cacheData = JsonUtility.FromJson<ConfigCacheData>(json);

                    // Check if cache expired
                    if ((DateTime.UtcNow - cacheData.timestamp).TotalSeconds < cacheExpiration)
                    {
                        foreach (var entry in cacheData.entries)
                        {
                            var value = new ConfigValue
                            {
                                key = entry.key,
                                stringValue = entry.value,
                                type = entry.type
                            };

                            _config[entry.key] = value;
                            _cachedConfig[entry.key] = value;
                        }

                        Debug.Log($"[RemoteConfigSystem] Loaded {_config.Count} cached config values");
                    }
                    else
                    {
                        Debug.Log("[RemoteConfigSystem] Cached config expired");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigSystem] Failed to load cached config: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear cached config.
        /// </summary>
        public void ClearCache()
        {
            _cachedConfig.Clear();
            PlayerPrefs.DeleteKey("RemoteConfig_Cache");
            PlayerPrefs.Save();

            Debug.Log("[RemoteConfigSystem] Cache cleared");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get remote config statistics.
        /// </summary>
        public RemoteConfigStats GetStats()
        {
            return new RemoteConfigStats
            {
                configCount = _config.Count,
                totalFetches = _totalFetches,
                failedFetches = _failedFetches,
                cacheHits = _cacheHits,
                hasFetchedOnce = _hasFetchedOnce,
                secondsSinceLastFetch = _hasFetchedOnce ? Time.time - _lastFetchTime : -1f,
                environment = environment
            };
        }

        /// <summary>
        /// Force refresh config from server.
        /// </summary>
        public void ForceRefresh()
        {
            if (_isFetching)
            {
                Debug.LogWarning("[RemoteConfigSystem] Already fetching");
                return;
            }

            FetchConfig();
        }

        /// <summary>
        /// Reset to defaults.
        /// </summary>
        public void Reset()
        {
            _config.Clear();
            ClearCache();
            _hasFetchedOnce = false;
            _totalFetches = 0;
            _failedFetches = 0;
            _cacheHits = 0;

            Debug.Log("[RemoteConfigSystem] Reset complete");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Fetch Config")]
        private void FetchConfigMenu()
        {
            FetchConfig();
        }

        [ContextMenu("Clear Cache")]
        private void ClearCacheMenu()
        {
            ClearCache();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Remote Config Statistics ===\n" +
                      $"Environment: {stats.environment}\n" +
                      $"Config Count: {stats.configCount}\n" +
                      $"Total Fetches: {stats.totalFetches}\n" +
                      $"Failed Fetches: {stats.failedFetches}\n" +
                      $"Cache Hits: {stats.cacheHits}\n" +
                      $"Has Fetched: {stats.hasFetchedOnce}\n" +
                      $"Seconds Since Last Fetch: {stats.secondsSinceLastFetch:F0}s");
        }

        [ContextMenu("Print All Config")]
        private void PrintAllConfig()
        {
            if (_config.Count == 0)
            {
                Debug.Log("[RemoteConfigSystem] No config loaded");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== Remote Configuration ===");

            foreach (var kvp in _config)
            {
                sb.AppendLine($"  {kvp.Key} ({kvp.Value.type}): {kvp.Value.stringValue}");
            }

            Debug.Log(sb.ToString());
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Configuration value.
    /// </summary>
    [Serializable]
    public class ConfigValue
    {
        public string key;
        public string stringValue;
        public ConfigValueType type;
    }

    /// <summary>
    /// Configuration entry from backend.
    /// </summary>
    [Serializable]
    public class ConfigEntry
    {
        public string key;
        public string value;
        public ConfigValueType type;
    }

    /// <summary>
    /// Backend response structure.
    /// </summary>
    [Serializable]
    public class ConfigResponse
    {
        public ConfigEntry[] config;
        public string environment;
        public long timestamp;
    }

    /// <summary>
    /// Cache data structure.
    /// </summary>
    [Serializable]
    public class ConfigCacheData
    {
        public DateTime timestamp;
        public ConfigEnvironment environment;
        public ConfigEntry[] entries;
    }

    /// <summary>
    /// Remote config statistics.
    /// </summary>
    [Serializable]
    public struct RemoteConfigStats
    {
        public int configCount;
        public int totalFetches;
        public int failedFetches;
        public int cacheHits;
        public bool hasFetchedOnce;
        public float secondsSinceLastFetch;
        public ConfigEnvironment environment;
    }

    /// <summary>
    /// Configuration value types.
    /// </summary>
    public enum ConfigValueType
    {
        String,
        Int,
        Float,
        Bool,
        Json
    }

    /// <summary>
    /// Configuration environments.
    /// </summary>
    public enum ConfigEnvironment
    {
        Development,
        Staging,
        Production
    }

    #endregion
}
