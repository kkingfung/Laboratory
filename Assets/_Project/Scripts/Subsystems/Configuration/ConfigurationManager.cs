using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using Laboratory.Core.Events;
using Laboratory.Core.DI;
using Laboratory.Subsystems.Analytics;

namespace Laboratory.Subsystems.Configuration
{
    /// <summary>
    /// Advanced configuration management system for live operations
    /// Supports remote config, A/B testing, feature flags, and real-time updates
    /// </summary>
    public class ConfigurationManager : MonoBehaviour
    {
        [Header("Configuration Sources")]
        [SerializeField] private bool enableRemoteConfig = true;
        [SerializeField] private bool enableLocalOverrides = true;
        [SerializeField] private string remoteConfigUrl = "https://your-config-server.com/config";
        
        [Header("Update Settings")]
        [SerializeField] private float configCheckInterval = 300f; // 5 minutes
        [SerializeField] private bool enableAutoUpdate = true;
        [SerializeField] private bool enableVersionControl = true;
        
        [Header("Feature Flags")]
        [SerializeField] private bool enableFeatureFlags = true;
        [SerializeField] private bool enableABTesting = true;
        
        // Configuration storage
        private Dictionary<string, ConfigValue> configurations = new Dictionary<string, ConfigValue>();
        private Dictionary<string, FeatureFlag> featureFlags = new Dictionary<string, FeatureFlag>();
        private Dictionary<string, ABTestConfig> abTests = new Dictionary<string, ABTestConfig>();
        
        // Version tracking
        private string currentConfigVersion = "1.0.0";
        private string remoteConfigVersion = "";
        
        // File paths
        private string localConfigPath;
        private string cacheConfigPath;
        
        // Event bus for publishing configuration events
        private IEventBus _eventBus;
        
        // Analytics integration
        private AnalyticsManager analyticsManager;
        
        // Events
        public System.Action<string, ConfigValue> OnConfigurationUpdated;
        public System.Action<string, bool> OnFeatureFlagChanged;
        public System.Action<string> OnConfigurationError;
        public System.Action OnRemoteConfigLoaded;

        private void Start()
        {
            // Initialize event bus
            if (GlobalServiceProvider.IsInitialized)
            {
                _eventBus = GlobalServiceProvider.Resolve<IEventBus>();
            }
            
            InitializeConfiguration();
            LoadLocalConfiguration();
            
            if (enableRemoteConfig)
            {
                StartCoroutine(ConfigurationUpdateCoroutine());
                LoadRemoteConfiguration();
            }
            
            // Get analytics manager for tracking
            analyticsManager = FindFirstObjectByType<AnalyticsManager>();
        }

        private void InitializeConfiguration()
        {
            localConfigPath = Path.Combine(Application.persistentDataPath, "game_config.json");
            cacheConfigPath = Path.Combine(Application.persistentDataPath, "config_cache.json");
            
            // Set default configurations
            SetDefaultConfigurations();
            SetDefaultFeatureFlags();
            
            Debug.Log("Configuration Manager initialized");
        }

        private void SetDefaultConfigurations()
        {
            // Game balance settings
            SetConfiguration("player_max_health", new ConfigValue { FloatValue = 100f, Type = ConfigType.Float });
            SetConfiguration("player_move_speed", new ConfigValue { FloatValue = 5f, Type = ConfigType.Float });
            SetConfiguration("attack_damage", new ConfigValue { FloatValue = 25f, Type = ConfigType.Float });
            
            // Performance settings
            SetConfiguration("max_fps", new ConfigValue { IntValue = 60, Type = ConfigType.Int });
            SetConfiguration("vsync_enabled", new ConfigValue { BoolValue = true, Type = ConfigType.Bool });
            SetConfiguration("quality_level", new ConfigValue { IntValue = 2, Type = ConfigType.Int });
            
            // Analytics settings
            SetConfiguration("analytics_enabled", new ConfigValue { BoolValue = true, Type = ConfigType.Bool });
            SetConfiguration("crash_reporting_enabled", new ConfigValue { BoolValue = true, Type = ConfigType.Bool });
            
            // UI/UX settings
            SetConfiguration("tutorial_enabled", new ConfigValue { BoolValue = true, Type = ConfigType.Bool });
            SetConfiguration("hints_enabled", new ConfigValue { BoolValue = true, Type = ConfigType.Bool });
            SetConfiguration("auto_save_interval", new ConfigValue { FloatValue = 30f, Type = ConfigType.Float });
            
            // Monetization settings
            SetConfiguration("ads_enabled", new ConfigValue { BoolValue = false, Type = ConfigType.Bool });
            SetConfiguration("iap_enabled", new ConfigValue { BoolValue = false, Type = ConfigType.Bool });
            
            // Social features
            SetConfiguration("leaderboards_enabled", new ConfigValue { BoolValue = true, Type = ConfigType.Bool });
            SetConfiguration("achievements_enabled", new ConfigValue { BoolValue = true, Type = ConfigType.Bool });
        }

        private void SetDefaultFeatureFlags()
        {
            // Core features
            SetFeatureFlag("multiplayer_enabled", new FeatureFlag { IsEnabled = true, RolloutPercentage = 100f });
            SetFeatureFlag("voice_chat_enabled", new FeatureFlag { IsEnabled = false, RolloutPercentage = 0f });
            SetFeatureFlag("spectator_mode", new FeatureFlag { IsEnabled = true, RolloutPercentage = 100f });
            
            // Experimental features
            SetFeatureFlag("new_ui_design", new FeatureFlag { IsEnabled = false, RolloutPercentage = 10f });
            SetFeatureFlag("advanced_graphics", new FeatureFlag { IsEnabled = false, RolloutPercentage = 25f });
            SetFeatureFlag("beta_gameplay_mode", new FeatureFlag { IsEnabled = false, RolloutPercentage = 5f });
            
            // Platform-specific features
            SetFeatureFlag("mobile_optimizations", new FeatureFlag { IsEnabled = Application.isMobilePlatform, RolloutPercentage = 100f });
            SetFeatureFlag("console_features", new FeatureFlag { IsEnabled = IsConsole(), RolloutPercentage = 100f });
        }

        #region Configuration Management

        /// <summary>
        /// Set a configuration value
        /// </summary>
        public void SetConfiguration(string key, ConfigValue value)
        {
            bool isNewConfig = !configurations.ContainsKey(key);
            ConfigValue oldValue = isNewConfig ? null : configurations[key];
            
            configurations[key] = value;
            
            // Fire events
            OnConfigurationUpdated?.Invoke(key, value);
            _eventBus?.Publish(new ConfigurationChangedEvent 
            { 
                Key = key, 
                NewValue = value, 
                OldValue = oldValue,
                IsNewConfiguration = isNewConfig
            });
            
            // Track configuration changes
            TrackConfigurationChange(key, value, oldValue, isNewConfig);
            
            Debug.Log($"Configuration updated: {key} = {value.GetDisplayValue()}");
        }

        /// <summary>
        /// Get a configuration value
        /// </summary>
        public ConfigValue GetConfiguration(string key)
        {
            return configurations.ContainsKey(key) ? configurations[key] : null;
        }

        /// <summary>
        /// Get configuration as specific type with fallback
        /// </summary>
        public T GetConfigValue<T>(string key, T defaultValue = default(T))
        {
            if (!configurations.ContainsKey(key))
            {
                return defaultValue;
            }
            
            var config = configurations[key];
            return config.GetValue<T>(defaultValue);
        }

        /// <summary>
        /// Check if configuration exists
        /// </summary>
        public bool HasConfiguration(string key)
        {
            return configurations.ContainsKey(key);
        }

        /// <summary>
        /// Remove a configuration
        /// </summary>
        public bool RemoveConfiguration(string key)
        {
            if (configurations.ContainsKey(key))
            {
                var removedValue = configurations[key];
                configurations.Remove(key);
                
                _eventBus?.Publish(new ConfigurationRemovedEvent { Key = key, RemovedValue = removedValue });
                return true;
            }
            return false;
        }

        #endregion

        #region Feature Flags

        /// <summary>
        /// Set a feature flag
        /// </summary>
        public void SetFeatureFlag(string key, FeatureFlag flag)
        {
            bool isNewFlag = !featureFlags.ContainsKey(key);
            bool oldEnabledState = isNewFlag ? false : featureFlags[key].IsEnabled;
            
            featureFlags[key] = flag;
            
            // Check if user should see this feature based on rollout percentage
            bool shouldEnable = UnityEngine.Random.Range(0f, 100f) <= flag.RolloutPercentage && flag.IsEnabled;
            
            // Fire events if state changed
            if (isNewFlag || oldEnabledState != shouldEnable)
            {
                OnFeatureFlagChanged?.Invoke(key, shouldEnable);
                _eventBus?.Publish(new FeatureFlagChangedEvent 
                { 
                    Key = key, 
                    IsEnabled = shouldEnable,
                    RolloutPercentage = flag.RolloutPercentage
                });
            }
            
            // Track feature flag changes
            TrackFeatureFlagChange(key, flag, oldEnabledState, shouldEnable);
            
            Debug.Log($"Feature flag updated: {key} = {shouldEnable} ({flag.RolloutPercentage}% rollout)");
        }

        /// <summary>
        /// Check if a feature is enabled
        /// </summary>
        public bool IsFeatureEnabled(string key)
        {
            if (!enableFeatureFlags || !featureFlags.ContainsKey(key))
            {
                return false;
            }
            
            var flag = featureFlags[key];
            if (!flag.IsEnabled) return false;
            
            // Check rollout percentage
            return UnityEngine.Random.Range(0f, 100f) <= flag.RolloutPercentage;
        }

        /// <summary>
        /// Get feature flag details
        /// </summary>
        public FeatureFlag GetFeatureFlag(string key)
        {
            return featureFlags.ContainsKey(key) ? featureFlags[key] : null;
        }

        #endregion

        #region A/B Testing

        /// <summary>
        /// Set up an A/B test
        /// </summary>
        public void SetupABTest(string testKey, ABTestConfig config)
        {
            if (!enableABTesting) return;
            
            abTests[testKey] = config;
            
            // Assign user to test group
            string userId = GetUserId();
            int userHash = userId.GetHashCode();
            float normalizedHash = Mathf.Abs((float)(userHash % 10000) / 10000f);
            
            string assignedGroup = normalizedHash < 0.5f ? "A" : "B";
            config.AssignedGroup = assignedGroup;
            
            // Apply test configuration
            ApplyABTestConfiguration(testKey, config);
            
            // Track A/B test assignment
            TrackABTestAssignment(testKey, assignedGroup);
            
            _eventBus?.Publish(new ABTestAssignedEvent 
            { 
                TestKey = testKey, 
                AssignedGroup = assignedGroup,
                Config = config
            });
            
            Debug.Log($"A/B Test assigned: {testKey} -> Group {assignedGroup}");
        }

        /// <summary>
        /// Get A/B test assignment
        /// </summary>
        public string GetABTestGroup(string testKey)
        {
            return abTests.ContainsKey(testKey) ? abTests[testKey].AssignedGroup : null;
        }

        /// <summary>
        /// Track A/B test conversion
        /// </summary>
        public void TrackABTestConversion(string testKey, string conversionEvent, Dictionary<string, object> parameters = null)
        {
            if (!abTests.ContainsKey(testKey)) return;
            
            var config = abTests[testKey];
            
            if (analyticsManager != null)
            {
                var eventParameters = parameters ?? new Dictionary<string, object>();
                eventParameters["ab_test_key"] = testKey;
                eventParameters["ab_test_group"] = config.AssignedGroup;
                eventParameters["conversion_event"] = conversionEvent;
                
                analyticsManager.TrackEvent("ab_test_conversion", eventParameters);
            }
            
            _eventBus?.Publish(new ABTestConversionEvent 
            { 
                TestKey = testKey, 
                Group = config.AssignedGroup,
                ConversionEvent = conversionEvent,
                Parameters = parameters
            });
        }

        private void ApplyABTestConfiguration(string testKey, ABTestConfig config)
        {
            // Apply different configurations based on test group
            var configsToApply = config.AssignedGroup == "A" ? config.ConfigurationsA : config.ConfigurationsB;
            
            foreach (var kvp in configsToApply)
            {
                SetConfiguration(kvp.Key, kvp.Value);
            }
        }

        #endregion

        #region Remote Configuration

        private IEnumerator ConfigurationUpdateCoroutine()
        {
            while (enableRemoteConfig)
            {
                yield return new WaitForSeconds(configCheckInterval);
                
                if (enableAutoUpdate)
                {
                    LoadRemoteConfiguration();
                }
            }
        }

        private void LoadRemoteConfiguration()
        {
            StartCoroutine(LoadRemoteConfigurationCoroutine());
        }

        private IEnumerator LoadRemoteConfigurationCoroutine()
        {
            using (var www = UnityEngine.Networking.UnityWebRequest.Get(remoteConfigUrl))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    try
                    {
                        ProcessRemoteConfiguration(www.downloadHandler.text);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Failed to process remote configuration: {ex.Message}");
                        OnConfigurationError?.Invoke($"Remote config processing failed: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Failed to load remote configuration: {www.error}");
                    OnConfigurationError?.Invoke($"Remote config load failed: {www.error}");
                    
                    // Fall back to cached configuration
                    LoadCachedConfiguration();
                }
            }
        }

        private void ProcessRemoteConfiguration(string jsonData)
        {
            var remoteConfig = JsonUtility.FromJson<RemoteConfigData>(jsonData);
            
            if (remoteConfig == null) return;
            
            // Check version
            if (enableVersionControl && remoteConfig.Version == currentConfigVersion)
            {
                Debug.Log("Remote configuration is up to date");
                return;
            }
            
            // Update configurations
            foreach (var config in remoteConfig.Configurations)
            {
                SetConfiguration(config.Key, config.Value);
            }
            
            // Update feature flags
            foreach (var flag in remoteConfig.FeatureFlags)
            {
                SetFeatureFlag(flag.Key, flag.Value);
            }
            
            // Set up A/B tests
            foreach (var test in remoteConfig.ABTests)
            {
                SetupABTest(test.Key, test.Value);
            }
            
            // Update version
            remoteConfigVersion = remoteConfig.Version;
            currentConfigVersion = remoteConfig.Version;
            
            // Save to cache
            SaveConfigurationToCache(jsonData);
            
            // Fire events
            OnRemoteConfigLoaded?.Invoke();
            _eventBus?.Publish(new RemoteConfigurationLoadedEvent 
            { 
                Version = remoteConfig.Version,
                ConfigurationCount = remoteConfig.Configurations.Count,
                FeatureFlagCount = remoteConfig.FeatureFlags.Count,
                ABTestCount = remoteConfig.ABTests.Count
            });
            
            Debug.Log($"Remote configuration loaded: Version {remoteConfig.Version}");
        }

        private void LoadCachedConfiguration()
        {
            if (File.Exists(cacheConfigPath))
            {
                try
                {
                    string cachedData = File.ReadAllText(cacheConfigPath);
                    ProcessRemoteConfiguration(cachedData);
                    Debug.Log("Loaded cached configuration");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load cached configuration: {ex.Message}");
                }
            }
        }

        private void SaveConfigurationToCache(string configData)
        {
            try
            {
                File.WriteAllText(cacheConfigPath, configData);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to cache configuration: {ex.Message}");
            }
        }

        #endregion

        #region Local Configuration

        private void LoadLocalConfiguration()
        {
            if (File.Exists(localConfigPath))
            {
                try
                {
                    string localData = File.ReadAllText(localConfigPath);
                    var localConfig = JsonUtility.FromJson<LocalConfigData>(localData);
                    
                    if (localConfig != null && enableLocalOverrides)
                    {
                        // Apply local overrides
                        foreach (var config in localConfig.Overrides)
                        {
                            SetConfiguration(config.Key, config.Value);
                        }
                        
                        Debug.Log($"Loaded {localConfig.Overrides.Count} local configuration overrides");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to load local configuration: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Save current configuration to local file
        /// </summary>
        public void SaveLocalConfiguration()
        {
            try
            {
                var localConfig = new LocalConfigData
                {
                    Version = currentConfigVersion,
                    Overrides = configurations
                };
                
                string jsonData = JsonUtility.ToJson(localConfig, true);
                File.WriteAllText(localConfigPath, jsonData);
                
                Debug.Log("Local configuration saved");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to save local configuration: {ex.Message}");
            }
        }

        #endregion

        #region Analytics Integration

        private void TrackConfigurationChange(string key, ConfigValue newValue, ConfigValue oldValue, bool isNew)
        {
            if (analyticsManager == null) return;
            
            var parameters = new Dictionary<string, object>
            {
                {"config_key", key},
                {"new_value", newValue.GetDisplayValue()},
                {"value_type", newValue.Type.ToString()},
                {"is_new_config", isNew}
            };
            
            if (oldValue != null)
            {
                parameters["old_value"] = oldValue.GetDisplayValue();
            }
            
            analyticsManager.TrackEvent("configuration_changed", parameters);
        }

        private void TrackFeatureFlagChange(string key, FeatureFlag newFlag, bool oldState, bool newState)
        {
            if (analyticsManager == null) return;
            
            analyticsManager.TrackEvent("feature_flag_changed", new Dictionary<string, object>
            {
                {"flag_key", key},
                {"old_enabled", oldState},
                {"new_enabled", newState},
                {"rollout_percentage", newFlag.RolloutPercentage}
            });
        }

        private void TrackABTestAssignment(string testKey, string group)
        {
            if (analyticsManager == null) return;
            
            analyticsManager.TrackEvent("ab_test_assigned", new Dictionary<string, object>
            {
                {"test_key", testKey},
                {"assigned_group", group},
                {"user_id", GetUserId()}
            });
        }

        #endregion

        #region Utility Methods

        private string GetUserId()
        {
            // Get or generate a consistent user ID for A/B testing
            string userId = PlayerPrefs.GetString("user_id", "");
            if (string.IsNullOrEmpty(userId))
            {
                userId = System.Guid.NewGuid().ToString();
                PlayerPrefs.SetString("user_id", userId);
            }
            return userId;
        }

        private bool IsConsole()
        {
            return Application.platform == RuntimePlatform.PS4 ||
                   Application.platform == RuntimePlatform.PS5 ||
                   Application.platform == RuntimePlatform.XboxOne ||
                   Application.platform == RuntimePlatform.GameCoreXboxOne ||
                   Application.platform == RuntimePlatform.GameCoreXboxSeries ||
                   Application.platform == RuntimePlatform.Switch;
        }

        /// <summary>
        /// Get all current configurations
        /// </summary>
        public Dictionary<string, ConfigValue> GetAllConfigurations()
        {
            return new Dictionary<string, ConfigValue>(configurations);
        }

        /// <summary>
        /// Get all feature flags
        /// </summary>
        public Dictionary<string, FeatureFlag> GetAllFeatureFlags()
        {
            return new Dictionary<string, FeatureFlag>(featureFlags);
        }

        /// <summary>
        /// Get all A/B tests
        /// </summary>
        public Dictionary<string, ABTestConfig> GetAllABTests()
        {
            return new Dictionary<string, ABTestConfig>(abTests);
        }

        /// <summary>
        /// Force refresh from remote configuration
        /// </summary>
        public void ForceRefreshConfiguration()
        {
            if (enableRemoteConfig)
            {
                LoadRemoteConfiguration();
            }
        }

        /// <summary>
        /// Reset all configurations to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            configurations.Clear();
            featureFlags.Clear();
            abTests.Clear();
            
            SetDefaultConfigurations();
            SetDefaultFeatureFlags();
            
            _eventBus?.Publish(new ConfigurationResetEvent());
            
            Debug.Log("Configuration reset to defaults");
        }

        #endregion

        private void OnDestroy()
        {
            // Save current configuration before destroying
            SaveLocalConfiguration();
        }
    }

    #region Data Structures

    public enum ConfigType
    {
        String,
        Int,
        Float,
        Bool,
        Vector3,
        Color
    }

    [System.Serializable]
    public class ConfigValue
    {
        public ConfigType Type;
        public string StringValue;
        public int IntValue;
        public float FloatValue;
        public bool BoolValue;
        public Vector3 Vector3Value;
        public Color ColorValue;
        
        public T GetValue<T>(T defaultValue = default(T))
        {
            try
            {
                switch (Type)
                {
                    case ConfigType.String:
                        return (T)(object)StringValue;
                    case ConfigType.Int:
                        return (T)(object)IntValue;
                    case ConfigType.Float:
                        return (T)(object)FloatValue;
                    case ConfigType.Bool:
                        return (T)(object)BoolValue;
                    case ConfigType.Vector3:
                        return (T)(object)Vector3Value;
                    case ConfigType.Color:
                        return (T)(object)ColorValue;
                    default:
                        return defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }
        
        public string GetDisplayValue()
        {
            switch (Type)
            {
                case ConfigType.String: return StringValue;
                case ConfigType.Int: return IntValue.ToString();
                case ConfigType.Float: return FloatValue.ToString("F2");
                case ConfigType.Bool: return BoolValue.ToString();
                case ConfigType.Vector3: return Vector3Value.ToString();
                case ConfigType.Color: return ColorValue.ToString();
                default: return "Unknown";
            }
        }
    }

    [System.Serializable]
    public class FeatureFlag
    {
        public bool IsEnabled;
        public float RolloutPercentage = 100f;
        public string Description;
        public System.DateTime ExpirationDate;
    }

    [System.Serializable]
    public class ABTestConfig
    {
        public string TestName;
        public string Description;
        public Dictionary<string, ConfigValue> ConfigurationsA = new Dictionary<string, ConfigValue>();
        public Dictionary<string, ConfigValue> ConfigurationsB = new Dictionary<string, ConfigValue>();
        public string AssignedGroup;
        public System.DateTime StartDate;
        public System.DateTime EndDate;
    }

    [System.Serializable]
    public class RemoteConfigData
    {
        public string Version;
        public List<ConfigEntry> Configurations = new List<ConfigEntry>();
        public List<FeatureFlagEntry> FeatureFlags = new List<FeatureFlagEntry>();
        public List<ABTestEntry> ABTests = new List<ABTestEntry>();
    }

    [System.Serializable]
    public class LocalConfigData
    {
        public string Version;
        public Dictionary<string, ConfigValue> Overrides = new Dictionary<string, ConfigValue>();
    }

    [System.Serializable]
    public class ConfigEntry
    {
        public string Key;
        public ConfigValue Value;
    }

    [System.Serializable]
    public class FeatureFlagEntry
    {
        public string Key;
        public FeatureFlag Value;
    }

    [System.Serializable]
    public class ABTestEntry
    {
        public string Key;
        public ABTestConfig Value;
    }

    #endregion

    #region Events

    public class ConfigurationChangedEvent : BaseEvent
    {
        public string Key { get; set; }
        public ConfigValue NewValue { get; set; }
        public ConfigValue OldValue { get; set; }
        public bool IsNewConfiguration { get; set; }
    }

    public class ConfigurationRemovedEvent : BaseEvent
    {
        public string Key { get; set; }
        public ConfigValue RemovedValue { get; set; }
    }

    public class FeatureFlagChangedEvent : BaseEvent
    {
        public string Key { get; set; }
        public bool IsEnabled { get; set; }
        public float RolloutPercentage { get; set; }
    }

    public class ABTestAssignedEvent : BaseEvent
    {
        public string TestKey { get; set; }
        public string AssignedGroup { get; set; }
        public ABTestConfig Config { get; set; }
    }

    public class ABTestConversionEvent : BaseEvent
    {
        public string TestKey { get; set; }
        public string Group { get; set; }
        public string ConversionEvent { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class RemoteConfigurationLoadedEvent : BaseEvent
    {
        public string Version { get; set; }
        public int ConfigurationCount { get; set; }
        public int FeatureFlagCount { get; set; }
        public int ABTestCount { get; set; }
    }

    public class ConfigurationResetEvent : BaseEvent
    {
        // Inherits Timestamp from BaseEvent
    }

    #endregion
}
