using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// Manages the game settings menu UI with audio, graphics, and control options.
    /// Handles saving and loading player preferences with persistent storage.
    /// </summary>
    public class SettingsMenuUI : MonoBehaviour
    {
        #region Fields

        [Header("Audio Settings")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown qualityDropdown;

        [Header("Controls Settings")]
        [SerializeField] private Slider sensitivitySlider;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;

        #endregion

        #region Constants

        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string MUSIC_VOLUME_KEY = "MusicVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";
        private const string GRAPHICS_QUALITY_KEY = "GraphicsQuality";
        private const string CONTROL_SENSITIVITY_KEY = "ControlSensitivity";

        private const float DEFAULT_MASTER_VOLUME = 1f;
        private const float DEFAULT_MUSIC_VOLUME = 0.7f;
        private const float DEFAULT_SFX_VOLUME = 0.7f;
        private const float DEFAULT_SENSITIVITY = 1f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize UI components and load saved settings
        /// </summary>
        private void Awake()
        {
            ValidateComponents();
            LoadSettings();
            SetupEventHandlers();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply all current settings and save to PlayerPrefs
        /// </summary>
        public void ApplySettings()
        {
            SaveAudioSettings();
            SaveGraphicsSettings();
            SaveControlSettings();
            
            PlayerPrefs.Save();
            
            // Notify other systems of settings changes
            NotifySettingsChanged();
        }

        /// <summary>
        /// Reset all settings to default values
        /// </summary>
        public void ResetSettings()
        {
            ResetToDefaults();
            ApplySettings();
        }

        #endregion

        #region Private Methods - Initialization

        /// <summary>
        /// Validate that all required components are assigned
        /// </summary>
        private void ValidateComponents()
        {
            if (masterVolumeSlider == null || musicVolumeSlider == null || sfxVolumeSlider == null)
                Debug.LogError($"[{nameof(SettingsMenuUI)}] Audio sliders not assigned!", this);
                
            if (qualityDropdown == null)
                Debug.LogError($"[{nameof(SettingsMenuUI)}] Quality dropdown not assigned!", this);
                
            if (sensitivitySlider == null)
                Debug.LogError($"[{nameof(SettingsMenuUI)}] Sensitivity slider not assigned!", this);
                
            if (applyButton == null || resetButton == null)
                Debug.LogError($"[{nameof(SettingsMenuUI)}] Buttons not assigned!", this);
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupEventHandlers()
        {
            applyButton.onClick.AddListener(ApplySettings);
            resetButton.onClick.AddListener(ResetSettings);
        }

        /// <summary>
        /// Load all settings from PlayerPrefs
        /// </summary>
        private void LoadSettings()
        {
            LoadAudioSettings();
            LoadGraphicsSettings();
            LoadControlSettings();
        }

        #endregion

        #region Private Methods - Audio Settings

        /// <summary>
        /// Load audio settings from PlayerPrefs
        /// </summary>
        private void LoadAudioSettings()
        {
            masterVolumeSlider.value = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, DEFAULT_MASTER_VOLUME);
            musicVolumeSlider.value = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, DEFAULT_MUSIC_VOLUME);
            sfxVolumeSlider.value = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, DEFAULT_SFX_VOLUME);
        }

        /// <summary>
        /// Save audio settings to PlayerPrefs
        /// </summary>
        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolumeSlider.value);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolumeSlider.value);
            PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolumeSlider.value);
        }

        #endregion

        #region Private Methods - Graphics Settings

        /// <summary>
        /// Load graphics settings from PlayerPrefs
        /// </summary>
        private void LoadGraphicsSettings()
        {
            int defaultQuality = QualitySettings.GetQualityLevel();
            qualityDropdown.value = PlayerPrefs.GetInt(GRAPHICS_QUALITY_KEY, defaultQuality);
        }

        /// <summary>
        /// Save graphics settings to PlayerPrefs and apply immediately
        /// </summary>
        private void SaveGraphicsSettings()
        {
            int qualityLevel = qualityDropdown.value;
            PlayerPrefs.SetInt(GRAPHICS_QUALITY_KEY, qualityLevel);
            QualitySettings.SetQualityLevel(qualityLevel);
        }

        #endregion

        #region Private Methods - Control Settings

        /// <summary>
        /// Load control settings from PlayerPrefs
        /// </summary>
        private void LoadControlSettings()
        {
            sensitivitySlider.value = PlayerPrefs.GetFloat(CONTROL_SENSITIVITY_KEY, DEFAULT_SENSITIVITY);
        }

        /// <summary>
        /// Save control settings to PlayerPrefs
        /// </summary>
        private void SaveControlSettings()
        {
            PlayerPrefs.SetFloat(CONTROL_SENSITIVITY_KEY, sensitivitySlider.value);
        }

        #endregion

        #region Private Methods - Utility

        /// <summary>
        /// Reset all UI elements to default values
        /// </summary>
        private void ResetToDefaults()
        {
            // Audio defaults
            masterVolumeSlider.value = DEFAULT_MASTER_VOLUME;
            musicVolumeSlider.value = DEFAULT_MUSIC_VOLUME;
            sfxVolumeSlider.value = DEFAULT_SFX_VOLUME;

            // Graphics defaults
            qualityDropdown.value = QualitySettings.GetQualityLevel();

            // Control defaults
            sensitivitySlider.value = DEFAULT_SENSITIVITY;
        }

        /// <summary>
        /// Notify other systems that settings have changed
        /// This method can be extended to use events or dependency injection
        /// </summary>
        private void NotifySettingsChanged()
        {
            // TODO: Implement notification system for audio manager, input manager, etc.
            // Example: AudioManager.Instance?.ApplySettings();
            // Example: InputManager.Instance?.ApplySettings();
        }

        #endregion
    }
}
