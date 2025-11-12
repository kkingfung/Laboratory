using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Backend
{
    /// <summary>
    /// Feature flags system for remote feature toggling.
    /// Enables/disables features without rebuilding the app.
    /// Supports gradual rollouts, A/B testing, and kill switches.
    /// </summary>
    public class FeatureFlagsSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string flagsEndpoint = "/features/flags";
        [SerializeField] private string overrideEndpoint = "/features/override";

        [Header("Fetch Settings")]
        [SerializeField] private bool fetchOnStart = true;
        [SerializeField] private bool autoRefresh = false;
        [SerializeField] private float refreshInterval = 600f; // 10 minutes
        [SerializeField] private int requestTimeout = 10;

        [Header("Defaults")]
        [SerializeField] private bool defaultFlagValue = false;
        [SerializeField] private bool allowLocalOverrides = true;

        [Header("Cache Settings")]
        [SerializeField] private bool cacheFlags = true;
        [SerializeField] private float cacheExpiration = 3600f; // 1 hour

        #endregion

        #region Private Fields

        private static FeatureFlagsSystem _instance;

        // Feature flags
        private readonly Dictionary<string, FeatureFlag> _flags = new Dictionary<string, FeatureFlag>();
        private readonly Dictionary<string, bool> _localOverrides = new Dictionary<string, bool>();

        // State
        private bool _isFetching = false;
        private bool _hasFetchedOnce = false;
        private float _lastFetchTime = 0f;
        private DateTime _cacheTimestamp;

        // Statistics
        private int _totalFetches = 0;
        private int _failedFetches = 0;
        private int _flagChecks = 0;

        // Events
        public event Action<Dictionary<string, FeatureFlag>> OnFlagsFetched;
        public event Action<string> OnFetchFailed;
        public event Action<string, bool> OnFlagChanged;

        #endregion

        #region Properties

        public static FeatureFlagsSystem Instance => _instance;
        public bool HasFlags => _flags.Count > 0;
        public bool IsFetching => _isFetching;
        public int FlagCount => _flags.Count;

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
                FetchFlags();
            }
        }

        private void Update()
        {
            if (!autoRefresh || _isFetching) return;

            // Auto-refresh flags
            if (Time.time - _lastFetchTime >= refreshInterval)
            {
                FetchFlags();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[FeatureFlagsSystem] Initializing...");

            // Load cached flags
            if (cacheFlags)
            {
                LoadCachedFlags();
            }

            // Load local overrides
            if (allowLocalOverrides)
            {
                LoadLocalOverrides();
            }

            Debug.Log($"[FeatureFlagsSystem] Initialized with {_flags.Count} flags");
        }

        #endregion

        #region Fetching

        /// <summary>
        /// Fetch feature flags from backend.
        /// </summary>
        public void FetchFlags(Action onSuccess = null, Action<string> onError = null)
        {
            if (_isFetching)
            {
                Debug.LogWarning("[FeatureFlagsSystem] Fetch already in progress");
                return;
            }

            StartCoroutine(FetchFlagsCoroutine(onSuccess, onError));
        }

        private IEnumerator FetchFlagsCoroutine(Action onSuccess, Action<string> onError)
        {
            _isFetching = true;
            _totalFetches++;

            string url = backendUrl + flagsEndpoint;

            // Add user ID if authenticated
            if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                url += $"?userId={UserAuthenticationSystem.Instance.UserId}";
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
                        var response = JsonUtility.FromJson<FlagsResponse>(request.downloadHandler.text);

                        UpdateFlags(response.flags);

                        _hasFetchedOnce = true;
                        _lastFetchTime = Time.time;
                        _cacheTimestamp = DateTime.UtcNow;

                        // Cache flags
                        if (cacheFlags)
                        {
                            CacheFlags();
                        }

                        OnFlagsFetched?.Invoke(_flags);
                        onSuccess?.Invoke();

                        Debug.Log($"[FeatureFlagsSystem] Fetched {_flags.Count} feature flags");
                    }
                    catch (Exception ex)
                    {
                        _failedFetches++;
                        string error = $"Failed to parse flags response: {ex.Message}";
                        OnFetchFailed?.Invoke(error);
                        onError?.Invoke(error);
                        Debug.LogError($"[FeatureFlagsSystem] {error}");
                    }
                }
                else
                {
                    _failedFetches++;
                    string error = $"Flags fetch failed: {request.error}";
                    OnFetchFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[FeatureFlagsSystem] {error}");
                }
            }
        }

        private void UpdateFlags(FeatureFlag[] flags)
        {
            foreach (var flag in flags)
            {
                bool oldValue = _flags.ContainsKey(flag.key) ? _flags[flag.key].enabled : defaultFlagValue;

                _flags[flag.key] = flag;

                // Fire change event if value changed
                if (_flags[flag.key].enabled != oldValue)
                {
                    OnFlagChanged?.Invoke(flag.key, flag.enabled);
                }
            }
        }

        #endregion

        #region Flag Checking

        /// <summary>
        /// Check if a feature is enabled.
        /// </summary>
        public bool IsFeatureEnabled(string featureKey)
        {
            _flagChecks++;

            // Check local override first
            if (allowLocalOverrides && _localOverrides.TryGetValue(featureKey, out bool overrideValue))
            {
                return overrideValue;
            }

            // Check fetched flags
            if (_flags.TryGetValue(featureKey, out var flag))
            {
                // Check user targeting
                if (flag.targetedUserIds != null && flag.targetedUserIds.Length > 0)
                {
                    if (UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated)
                    {
                        string userId = UserAuthenticationSystem.Instance.UserId;
                        bool isTargeted = Array.Exists(flag.targetedUserIds, id => id == userId);

                        if (!isTargeted)
                        {
                            return false;
                        }
                    }
                }

                // Check rollout percentage
                if (flag.rolloutPercentage < 100)
                {
                    if (!IsUserInRollout(featureKey, flag.rolloutPercentage))
                    {
                        return false;
                    }
                }

                return flag.enabled;
            }

            // Default value if flag not found
            return defaultFlagValue;
        }

        /// <summary>
        /// Get feature flag value (alias for IsFeatureEnabled).
        /// </summary>
        public bool GetFlag(string featureKey)
        {
            return IsFeatureEnabled(featureKey);
        }

        /// <summary>
        /// Get feature flag variant (for A/B testing).
        /// </summary>
        public string GetVariant(string featureKey, string defaultVariant = "control")
        {
            if (_flags.TryGetValue(featureKey, out var flag))
            {
                if (!string.IsNullOrEmpty(flag.variant))
                {
                    return flag.variant;
                }
            }

            return defaultVariant;
        }

        /// <summary>
        /// Get feature flag metadata.
        /// </summary>
        public Dictionary<string, string> GetMetadata(string featureKey)
        {
            if (_flags.TryGetValue(featureKey, out var flag))
            {
                return flag.metadata ?? new Dictionary<string, string>();
            }

            return new Dictionary<string, string>();
        }

        private bool IsUserInRollout(string featureKey, int percentage)
        {
            // Consistent hash-based rollout
            string userId = UserAuthenticationSystem.Instance?.UserId ?? SystemInfo.deviceUniqueIdentifier;
            string hashInput = $"{featureKey}:{userId}";
            int hash = hashInput.GetHashCode();
            int bucket = Math.Abs(hash % 100);

            return bucket < percentage;
        }

        #endregion

        #region Local Overrides

        /// <summary>
        /// Set local override for a feature flag (testing/debugging).
        /// </summary>
        public void SetLocalOverride(string featureKey, bool enabled)
        {
            if (!allowLocalOverrides)
            {
                Debug.LogWarning("[FeatureFlagsSystem] Local overrides disabled");
                return;
            }

            _localOverrides[featureKey] = enabled;
            SaveLocalOverrides();

            OnFlagChanged?.Invoke(featureKey, enabled);

            Debug.Log($"[FeatureFlagsSystem] Local override set: {featureKey} = {enabled}");
        }

        /// <summary>
        /// Clear local override for a feature flag.
        /// </summary>
        public void ClearLocalOverride(string featureKey)
        {
            if (_localOverrides.Remove(featureKey))
            {
                SaveLocalOverrides();
                Debug.Log($"[FeatureFlagsSystem] Local override cleared: {featureKey}");
            }
        }

        /// <summary>
        /// Clear all local overrides.
        /// </summary>
        public void ClearAllLocalOverrides()
        {
            _localOverrides.Clear();
            SaveLocalOverrides();
            Debug.Log("[FeatureFlagsSystem] All local overrides cleared");
        }

        private void SaveLocalOverrides()
        {
            try
            {
                var data = new LocalOverridesData
                {
                    overrides = _localOverrides
                };

                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString("FeatureFlags_LocalOverrides", json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FeatureFlagsSystem] Failed to save local overrides: {ex.Message}");
            }
        }

        private void LoadLocalOverrides()
        {
            try
            {
                if (PlayerPrefs.HasKey("FeatureFlags_LocalOverrides"))
                {
                    string json = PlayerPrefs.GetString("FeatureFlags_LocalOverrides");
                    var data = JsonUtility.FromJson<LocalOverridesData>(json);

                    _localOverrides.Clear();

                    foreach (var kvp in data.overrides)
                    {
                        _localOverrides[kvp.Key] = kvp.Value;
                    }

                    Debug.Log($"[FeatureFlagsSystem] Loaded {_localOverrides.Count} local overrides");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FeatureFlagsSystem] Failed to load local overrides: {ex.Message}");
            }
        }

        #endregion

        #region Caching

        private void CacheFlags()
        {
            try
            {
                var cacheData = new FlagsCacheData
                {
                    timestamp = _cacheTimestamp,
                    flags = new FeatureFlag[_flags.Count]
                };

                int i = 0;
                foreach (var flag in _flags.Values)
                {
                    cacheData.flags[i++] = flag;
                }

                string json = JsonUtility.ToJson(cacheData);
                PlayerPrefs.SetString("FeatureFlags_Cache", json);
                PlayerPrefs.Save();

                Debug.Log($"[FeatureFlagsSystem] Cached {_flags.Count} flags");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FeatureFlagsSystem] Failed to cache flags: {ex.Message}");
            }
        }

        private void LoadCachedFlags()
        {
            try
            {
                if (PlayerPrefs.HasKey("FeatureFlags_Cache"))
                {
                    string json = PlayerPrefs.GetString("FeatureFlags_Cache");
                    var cacheData = JsonUtility.FromJson<FlagsCacheData>(json);

                    // Check if cache expired
                    if ((DateTime.UtcNow - cacheData.timestamp).TotalSeconds < cacheExpiration)
                    {
                        foreach (var flag in cacheData.flags)
                        {
                            _flags[flag.key] = flag;
                        }

                        Debug.Log($"[FeatureFlagsSystem] Loaded {_flags.Count} cached flags");
                    }
                    else
                    {
                        Debug.Log("[FeatureFlagsSystem] Cached flags expired");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FeatureFlagsSystem] Failed to load cached flags: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear cached flags.
        /// </summary>
        public void ClearCache()
        {
            PlayerPrefs.DeleteKey("FeatureFlags_Cache");
            PlayerPrefs.Save();

            Debug.Log("[FeatureFlagsSystem] Cache cleared");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get feature flags statistics.
        /// </summary>
        public FeatureFlagsStats GetStats()
        {
            return new FeatureFlagsStats
            {
                flagCount = _flags.Count,
                totalFetches = _totalFetches,
                failedFetches = _failedFetches,
                flagChecks = _flagChecks,
                localOverrideCount = _localOverrides.Count,
                hasFetchedOnce = _hasFetchedOnce,
                secondsSinceLastFetch = _hasFetchedOnce ? Time.time - _lastFetchTime : -1f
            };
        }

        /// <summary>
        /// Force refresh flags from server.
        /// </summary>
        public void ForceRefresh()
        {
            if (_isFetching)
            {
                Debug.LogWarning("[FeatureFlagsSystem] Already fetching");
                return;
            }

            FetchFlags();
        }

        /// <summary>
        /// Get all flag keys.
        /// </summary>
        public string[] GetAllKeys()
        {
            var keys = new string[_flags.Count];
            _flags.Keys.CopyTo(keys, 0);
            return keys;
        }

        /// <summary>
        /// Get all flags.
        /// </summary>
        public FeatureFlag[] GetAllFlags()
        {
            var flags = new FeatureFlag[_flags.Count];
            _flags.Values.CopyTo(flags, 0);
            return flags;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Fetch Flags")]
        private void FetchFlagsMenu()
        {
            FetchFlags();
        }

        [ContextMenu("Clear Cache")]
        private void ClearCacheMenu()
        {
            ClearCache();
        }

        [ContextMenu("Clear Local Overrides")]
        private void ClearLocalOverridesMenu()
        {
            ClearAllLocalOverrides();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Feature Flags Statistics ===\n" +
                      $"Flag Count: {stats.flagCount}\n" +
                      $"Total Fetches: {stats.totalFetches}\n" +
                      $"Failed Fetches: {stats.failedFetches}\n" +
                      $"Flag Checks: {stats.flagChecks}\n" +
                      $"Local Overrides: {stats.localOverrideCount}\n" +
                      $"Has Fetched: {stats.hasFetchedOnce}\n" +
                      $"Seconds Since Last Fetch: {stats.secondsSinceLastFetch:F0}s");
        }

        [ContextMenu("Print All Flags")]
        private void PrintAllFlags()
        {
            if (_flags.Count == 0)
            {
                Debug.Log("[FeatureFlagsSystem] No flags loaded");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("=== Feature Flags ===");

            foreach (var kvp in _flags)
            {
                var flag = kvp.Value;
                sb.AppendLine($"  {flag.key}: {flag.enabled}");

                if (!string.IsNullOrEmpty(flag.variant))
                {
                    sb.AppendLine($"    Variant: {flag.variant}");
                }

                if (flag.rolloutPercentage < 100)
                {
                    sb.AppendLine($"    Rollout: {flag.rolloutPercentage}%");
                }

                if (_localOverrides.ContainsKey(flag.key))
                {
                    sb.AppendLine($"    LOCAL OVERRIDE: {_localOverrides[flag.key]}");
                }
            }

            Debug.Log(sb.ToString());
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Feature flag data.
    /// </summary>
    [Serializable]
    public class FeatureFlag
    {
        public string key;
        public bool enabled;
        public string variant;
        public int rolloutPercentage = 100;
        public string[] targetedUserIds;
        public Dictionary<string, string> metadata;
    }

    /// <summary>
    /// Flags cache data.
    /// </summary>
    [Serializable]
    public class FlagsCacheData
    {
        public DateTime timestamp;
        public FeatureFlag[] flags;
    }

    /// <summary>
    /// Local overrides data.
    /// </summary>
    [Serializable]
    public class LocalOverridesData
    {
        public Dictionary<string, bool> overrides;
    }

    /// <summary>
    /// Feature flags statistics.
    /// </summary>
    [Serializable]
    public struct FeatureFlagsStats
    {
        public int flagCount;
        public int totalFetches;
        public int failedFetches;
        public int flagChecks;
        public int localOverrideCount;
        public bool hasFetchedOnce;
        public float secondsSinceLastFetch;
    }

    // Request/Response structures
    [Serializable] class FlagsResponse { public FeatureFlag[] flags; }

    #endregion
}
