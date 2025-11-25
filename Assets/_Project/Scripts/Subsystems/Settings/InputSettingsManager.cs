using UnityEngine;

namespace Laboratory.Subsystems.Settings
{
    /// <summary>
    /// Input settings management
    /// Handles sensitivity, dead zones, and input preferences
    /// </summary>
    public class InputSettingsManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SettingsConfig config;

        // Current settings
        private float _mouseSensitivity;
        private bool _invertYAxis;
        private float _deadzone;

        // Events
        public event System.Action OnInputSettingsChanged;

        // PlayerPrefs keys
        private const string PREF_MOUSE_SENS = "Input_MouseSensitivity";
        private const string PREF_INVERT_Y = "Input_InvertY";
        private const string PREF_DEADZONE = "Input_Deadzone";

        private void Start()
        {
            LoadSettings();
        }

        /// <summary>
        /// Load settings from PlayerPrefs or use defaults
        /// </summary>
        public void LoadSettings()
        {
            if (config == null)
            {
                Debug.LogWarning("[InputSettings] No configuration assigned!");
                return;
            }

            _mouseSensitivity = PlayerPrefs.GetFloat(PREF_MOUSE_SENS, config.DefaultMouseSensitivity);
            _invertYAxis = PlayerPrefs.GetInt(PREF_INVERT_Y, config.DefaultInvertYAxis ? 1 : 0) == 1;
            _deadzone = PlayerPrefs.GetFloat(PREF_DEADZONE, config.DefaultDeadzone);

            Debug.Log($"[InputSettings] Loaded settings: Sensitivity={_mouseSensitivity:F2}, InvertY={_invertYAxis}");
        }

        /// <summary>
        /// Save settings to PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_MOUSE_SENS, _mouseSensitivity);
            PlayerPrefs.SetInt(PREF_INVERT_Y, _invertYAxis ? 1 : 0);
            PlayerPrefs.SetFloat(PREF_DEADZONE, _deadzone);
            PlayerPrefs.Save();

            Debug.Log("[InputSettings] Settings saved");
        }

        /// <summary>
        /// Set mouse sensitivity
        /// </summary>
        public void SetMouseSensitivity(float sensitivity)
        {
            _mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 5f);
            SaveSettings();
            OnInputSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Toggle Y-axis inversion
        /// </summary>
        public void SetInvertYAxis(bool invert)
        {
            _invertYAxis = invert;
            SaveSettings();
            OnInputSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set controller deadzone
        /// </summary>
        public void SetDeadzone(float deadzone)
        {
            _deadzone = Mathf.Clamp01(deadzone);
            SaveSettings();
            OnInputSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            if (config == null) return;

            _mouseSensitivity = config.DefaultMouseSensitivity;
            _invertYAxis = config.DefaultInvertYAxis;
            _deadzone = config.DefaultDeadzone;

            SaveSettings();
            OnInputSettingsChanged?.Invoke();

            Debug.Log("[InputSettings] Reset to defaults");
        }

        // Getters
        public float GetMouseSensitivity() => _mouseSensitivity;
        public bool IsYAxisInverted() => _invertYAxis;
        public float GetDeadzone() => _deadzone;
    }
}
