using UnityEngine;

namespace Laboratory.Subsystems.Settings
{
    /// <summary>
    /// Subsystem manager for Settings
    /// Coordinates graphics, audio, and input settings
    /// Follows Project Chimera architecture pattern
    /// </summary>
    public class SettingsSubsystemManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SettingsConfig config;

        [Header("Components")]
        [SerializeField] private GraphicsSettings graphicsSettings;
        [SerializeField] private AudioSettings audioSettings;
        [SerializeField] private InputSettingsManager inputSettings;

        // Singleton
        private static SettingsSubsystemManager _instance;
        public static SettingsSubsystemManager Instance => _instance;

        // State
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
        }

        private void Start()
        {
            InitializeSubsystem();
        }

        /// <summary>
        /// Initialize required components
        /// </summary>
        private void InitializeComponents()
        {
            // Find or create graphics settings
            if (graphicsSettings == null)
            {
                graphicsSettings = GetComponentInChildren<GraphicsSettings>();
                if (graphicsSettings == null)
                {
                    GameObject graphicsObj = new GameObject("GraphicsSettings");
                    graphicsObj.transform.SetParent(transform);
                    graphicsSettings = graphicsObj.AddComponent<GraphicsSettings>();
                }
            }

            // Find or create audio settings
            if (audioSettings == null)
            {
                audioSettings = GetComponentInChildren<AudioSettings>();
                if (audioSettings == null)
                {
                    GameObject audioObj = new GameObject("AudioSettings");
                    audioObj.transform.SetParent(transform);
                    audioSettings = audioObj.AddComponent<AudioSettings>();
                }
            }

            // Find or create input settings
            if (inputSettings == null)
            {
                inputSettings = GetComponentInChildren<InputSettingsManager>();
                if (inputSettings == null)
                {
                    GameObject inputObj = new GameObject("InputSettings");
                    inputObj.transform.SetParent(transform);
                    inputSettings = inputObj.AddComponent<InputSettingsManager>();
                }
            }
        }

        /// <summary>
        /// Initialize settings subsystem
        /// </summary>
        private void InitializeSubsystem()
        {
            if (_isInitialized) return;

            if (config == null)
            {
                Debug.LogWarning("[SettingsSubsystem] No configuration assigned!");
            }

            // Load all settings
            LoadAllSettings();

            _isInitialized = true;
            Debug.Log("[SettingsSubsystem] Initialized");
        }

        /// <summary>
        /// Load all settings from PlayerPrefs
        /// </summary>
        public void LoadAllSettings()
        {
            if (graphicsSettings != null)
            {
                graphicsSettings.LoadSettings();
            }

            if (audioSettings != null)
            {
                audioSettings.LoadSettings();
            }

            if (inputSettings != null)
            {
                inputSettings.LoadSettings();
            }

            Debug.Log("[SettingsSubsystem] All settings loaded");
        }

        /// <summary>
        /// Save all settings to PlayerPrefs
        /// </summary>
        public void SaveAllSettings()
        {
            if (graphicsSettings != null)
            {
                graphicsSettings.SaveSettings();
            }

            if (audioSettings != null)
            {
                audioSettings.SaveSettings();
            }

            if (inputSettings != null)
            {
                inputSettings.SaveSettings();
            }

            Debug.Log("[SettingsSubsystem] All settings saved");
        }

        /// <summary>
        /// Apply all settings
        /// </summary>
        public void ApplyAllSettings()
        {
            if (graphicsSettings != null)
            {
                graphicsSettings.ApplySettings();
            }

            if (audioSettings != null)
            {
                audioSettings.ApplySettings();
            }

            Debug.Log("[SettingsSubsystem] All settings applied");
        }

        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        public void ResetAllToDefaults()
        {
            if (graphicsSettings != null)
            {
                graphicsSettings.ResetToDefaults();
            }

            if (audioSettings != null)
            {
                audioSettings.ResetToDefaults();
            }

            if (inputSettings != null)
            {
                inputSettings.ResetToDefaults();
            }

            Debug.Log("[SettingsSubsystem] All settings reset to defaults");
        }

        // Component accessors
        public GraphicsSettings GetGraphicsSettings() => graphicsSettings;
        public AudioSettings GetAudioSettings() => audioSettings;
        public InputSettingsManager GetInputSettings() => inputSettings;
        public SettingsConfig GetConfig() => config;

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
