using UnityEngine;

namespace Laboratory.Core.Configuration
{
    /// <summary>
    /// Centralized access point for all Laboratory configuration settings.
    /// Provides singleton access to configuration data and handles loading/caching.
    /// </summary>
    public class ConfigurationManager : MonoBehaviour
    {
        [Header("Configuration Assets")]
        [SerializeField] private PerformanceConfiguration performanceConfig;

        private static ConfigurationManager _instance;
        private static PerformanceConfiguration _defaultPerformanceConfig;

        /// <summary>
        /// Singleton instance of the configuration manager
        /// </summary>
        public static ConfigurationManager Instance
        {
            get
            {
                // Thread-safe lazy initialization
                if (_instance == null)
                {
                    lock (typeof(ConfigurationManager))
                    {
                        if (_instance == null)
                        {
                            _instance = FindFirstObjectByType<ConfigurationManager>();
                            if (_instance == null)
                            {
                                var go = new GameObject("ConfigurationManager");
                                _instance = go.AddComponent<ConfigurationManager>();
                                DontDestroyOnLoad(go);
                                _instance.LoadDefaultConfigurations();
                            }
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Access to performance configuration settings
        /// </summary>
        public static PerformanceConfiguration Performance
        {
            get
            {
                if (Instance.performanceConfig != null)
                    return Instance.performanceConfig;

                if (_defaultPerformanceConfig == null)
                    _defaultPerformanceConfig = LoadDefaultPerformanceConfig();

                return _defaultPerformanceConfig;
            }
        }

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                LoadDefaultConfigurations();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        #endregion

        #region Configuration Loading

        private void LoadDefaultConfigurations()
        {
            // Try to load performance configuration from Resources
            if (performanceConfig == null)
            {
                performanceConfig = LoadDefaultPerformanceConfig();
            }
        }

        private static PerformanceConfiguration LoadDefaultPerformanceConfig()
        {
            // Try to load from Resources folder
            var config = Resources.Load<PerformanceConfiguration>("DefaultPerformanceConfiguration");

            if (config == null)
            {
                // Create a runtime instance with default values
                config = CreateDefaultPerformanceConfig();
                Debug.LogWarning("[ConfigurationManager] No PerformanceConfiguration found in Resources. Using runtime defaults.");
            }

            return config;
        }

        private static PerformanceConfiguration CreateDefaultPerformanceConfig()
        {
            var config = ScriptableObject.CreateInstance<PerformanceConfiguration>();

            // Set reasonable defaults (these will match the ScriptableObject defaults)
            // Values are already set in the ScriptableObject definition

            return config;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Reload configuration from disk (useful for runtime configuration changes)
        /// </summary>
        [ContextMenu("Reload Configuration")]
        public void ReloadConfiguration()
        {
            LoadDefaultConfigurations();
            Debug.Log("[ConfigurationManager] Configuration reloaded");
        }

        /// <summary>
        /// Override performance configuration at runtime
        /// </summary>
        public void SetPerformanceConfiguration(PerformanceConfiguration config)
        {
            performanceConfig = config;
            Debug.Log("[ConfigurationManager] Performance configuration updated");
        }

        /// <summary>
        /// Get a configuration value with fallback
        /// </summary>
        public static T GetConfigValue<T>(System.Func<PerformanceConfiguration, T> selector, T fallback = default(T))
        {
            try
            {
                return selector(Performance);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ConfigurationManager] Failed to get config value: {e.Message}. Using fallback: {fallback}");
                return fallback;
            }
        }

        #endregion

        #region Debug/Editor

#if UNITY_EDITOR
        /// <summary>
        /// Create default configuration asset in the project
        /// </summary>
        [UnityEditor.MenuItem("🧪 Laboratory/Configuration/Create Default Performance Config")]
        public static void CreateDefaultConfigurationAsset()
        {
            var config = ScriptableObject.CreateInstance<PerformanceConfiguration>();

            // Ensure Resources folder exists
            var resourcesPath = "Assets/Resources";
            if (!UnityEditor.AssetDatabase.IsValidFolder(resourcesPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
            }

            var assetPath = $"{resourcesPath}/DefaultPerformanceConfiguration.asset";
            UnityEditor.AssetDatabase.CreateAsset(config, assetPath);
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            Debug.Log($"[ConfigurationManager] Created default performance configuration at {assetPath}");
            UnityEditor.Selection.activeObject = config;
        }

        /// <summary>
        /// Validate all configuration references in the scene
        /// </summary>
        [UnityEditor.MenuItem("🧪 Laboratory/Configuration/Validate Configuration Setup")]
        public static void ValidateConfigurationSetup()
        {
            var manager = FindFirstObjectByType<ConfigurationManager>();
            if (manager == null)
            {
                Debug.LogWarning("[ConfigurationManager] No ConfigurationManager found in scene");
                return;
            }

            if (manager.performanceConfig == null)
            {
                Debug.LogWarning("[ConfigurationManager] No PerformanceConfiguration assigned");
            }
            else
            {
                Debug.Log("[ConfigurationManager] Configuration setup is valid");
            }

            // Check for configuration asset in Resources
            var resourceConfig = Resources.Load<PerformanceConfiguration>("DefaultPerformanceConfiguration");
            if (resourceConfig == null)
            {
                Debug.LogWarning("[ConfigurationManager] No DefaultPerformanceConfiguration found in Resources folder");
            }
            else
            {
                Debug.Log("[ConfigurationManager] Default configuration found in Resources");
            }
        }
#endif

        #endregion
    }

    /// <summary>
    /// Static utility class for easy access to configuration values
    /// </summary>
    public static class Config
    {
        /// <summary>
        /// Performance configuration shortcut
        /// </summary>
        public static PerformanceConfiguration Performance => ConfigurationManager.Performance;

        /// <summary>
        /// Get update interval for frequency with configuration fallback
        /// </summary>
        public static float UpdateInterval(UpdateFrequency frequency)
        {
            return ConfigurationManager.GetConfigValue(c => c.GetUpdateInterval(frequency), 1f/15f);
        }

        /// <summary>
        /// Get network sync interval for priority with configuration fallback
        /// </summary>
        public static float NetworkInterval(NetworkPriority priority)
        {
            return ConfigurationManager.GetConfigValue(c => c.GetNetworkSyncInterval(priority), 0.1f);
        }
    }
}