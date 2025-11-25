using UnityEngine;
using UnityEngine.UI;
using Laboratory.Subsystems.Settings;

namespace Laboratory.Demo.UI
{
    /// <summary>
    /// Demo settings menu UI
    /// Provides interface for graphics, audio, and input settings
    /// </summary>
    public class DemoSettingsMenu : MonoBehaviour
    {
        [Header("Menu Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject graphicsPanel;
        [SerializeField] private GameObject audioPanel;
        [SerializeField] private GameObject inputPanel;

        [Header("Graphics Controls")]
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vSyncToggle;
        [SerializeField] private Slider fpsSlider;
        [SerializeField] private Text fpsText;

        [Header("Audio Controls")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text masterVolumeText;
        [SerializeField] private Text musicVolumeText;
        [SerializeField] private Text sfxVolumeText;

        [Header("Input Controls")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private Toggle invertYToggle;
        [SerializeField] private Slider deadzoneSlider;
        [SerializeField] private Text sensitivityText;
        [SerializeField] private Text deadzoneText;

        [Header("Buttons")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeButton;

        // References
        private SettingsSubsystemManager _settingsSubsystem;
        private bool _isInitialized = false;

        private void Start()
        {
            _settingsSubsystem = SettingsSubsystemManager.Instance;

            if (_settingsSubsystem == null)
            {
                Debug.LogWarning("[DemoSettings Menu] Settings subsystem not found!");
                return;
            }

            InitializeUI();
            LoadCurrentSettings();

            // Hide menu by default
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }

            _isInitialized = true;
        }

        private void Update()
        {
            // Toggle menu with Tab key
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleMenu();
            }
        }

        /// <summary>
        /// Initialize UI controls
        /// </summary>
        private void InitializeUI()
        {
            // Setup quality dropdown
            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            // Setup resolution dropdown
            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                var resolutions = new System.Collections.Generic.List<string>
                {
                    "1280x720",
                    "1600x900",
                    "1920x1080",
                    "2560x1440",
                    "3840x2160"
                };
                resolutionDropdown.AddOptions(resolutions);
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            }

            // Setup toggles
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (vSyncToggle != null)
                vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            if (invertYToggle != null)
                invertYToggle.onValueChanged.AddListener(OnInvertYChanged);

            // Setup sliders
            if (fpsSlider != null)
            {
                fpsSlider.minValue = 30;
                fpsSlider.maxValue = 144;
                fpsSlider.wholeNumbers = true;
                fpsSlider.onValueChanged.AddListener(OnFPSChanged);
            }

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

            if (sensitivitySlider != null)
            {
                sensitivitySlider.minValue = 0.1f;
                sensitivitySlider.maxValue = 5f;
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);
            }

            if (deadzoneSlider != null)
            {
                deadzoneSlider.minValue = 0f;
                deadzoneSlider.maxValue = 0.5f;
                deadzoneSlider.onValueChanged.AddListener(OnDeadzoneChanged);
            }

            // Setup buttons
            if (applyButton != null)
                applyButton.onClick.AddListener(OnApplyClicked);
            if (resetButton != null)
                resetButton.onClick.AddListener(OnResetClicked);
            if (closeButton != null)
                closeButton.onClick.AddListener(OnCloseClicked);
        }

        /// <summary>
        /// Load current settings into UI
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (_settingsSubsystem == null) return;

            // Graphics
            var graphics = _settingsSubsystem.GetGraphicsSettings();
            if (graphics != null)
            {
                if (qualityDropdown != null)
                    qualityDropdown.value = graphics.GetQualityLevel();
                if (fullscreenToggle != null)
                    fullscreenToggle.isOn = graphics.IsFullscreen();
                if (vSyncToggle != null)
                    vSyncToggle.isOn = graphics.IsVSyncEnabled();
                if (fpsSlider != null)
                {
                    fpsSlider.value = graphics.GetTargetFrameRate();
                    UpdateFPSText(graphics.GetTargetFrameRate());
                }
            }

            // Audio
            var audio = _settingsSubsystem.GetAudioSettings();
            if (audio != null)
            {
                if (masterVolumeSlider != null)
                {
                    masterVolumeSlider.value = audio.GetMasterVolume();
                    UpdateVolumeText(masterVolumeText, audio.GetMasterVolume());
                }
                if (musicVolumeSlider != null)
                {
                    musicVolumeSlider.value = audio.GetMusicVolume();
                    UpdateVolumeText(musicVolumeText, audio.GetMusicVolume());
                }
                if (sfxVolumeSlider != null)
                {
                    sfxVolumeSlider.value = audio.GetSFXVolume();
                    UpdateVolumeText(sfxVolumeText, audio.GetSFXVolume());
                }
            }

            // Input
            var input = _settingsSubsystem.GetInputSettings();
            if (input != null)
            {
                if (sensitivitySlider != null)
                {
                    sensitivitySlider.value = input.GetMouseSensitivity();
                    UpdateSensitivityText(input.GetMouseSensitivity());
                }
                if (invertYToggle != null)
                    invertYToggle.isOn = input.IsYAxisInverted();
                if (deadzoneSlider != null)
                {
                    deadzoneSlider.value = input.GetDeadzone();
                    UpdateDeadzoneText(input.GetDeadzone());
                }
            }
        }

        /// <summary>
        /// Toggle menu visibility
        /// </summary>
        public void ToggleMenu()
        {
            if (menuPanel != null)
            {
                bool isActive = menuPanel.activeSelf;
                menuPanel.SetActive(!isActive);

                // Show graphics panel by default
                if (!isActive)
                {
                    ShowGraphicsPanel();
                }

                // Toggle cursor
                Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !isActive;
            }
        }

        // Panel switching
        public void ShowGraphicsPanel()
        {
            if (graphicsPanel != null) graphicsPanel.SetActive(true);
            if (audioPanel != null) audioPanel.SetActive(false);
            if (inputPanel != null) inputPanel.SetActive(false);
        }

        public void ShowAudioPanel()
        {
            if (graphicsPanel != null) graphicsPanel.SetActive(false);
            if (audioPanel != null) audioPanel.SetActive(true);
            if (inputPanel != null) inputPanel.SetActive(false);
        }

        public void ShowInputPanel()
        {
            if (graphicsPanel != null) graphicsPanel.SetActive(false);
            if (audioPanel != null) audioPanel.SetActive(false);
            if (inputPanel != null) inputPanel.SetActive(true);
        }

        // Event handlers
        private void OnQualityChanged(int value)
        {
            _settingsSubsystem?.GetGraphicsSettings()?.SetQuality(value);
        }

        private void OnResolutionChanged(int value)
        {
            int[][] resolutions = new int[][]
            {
                new int[] { 1280, 720 },
                new int[] { 1600, 900 },
                new int[] { 1920, 1080 },
                new int[] { 2560, 1440 },
                new int[] { 3840, 2160 }
            };

            if (value >= 0 && value < resolutions.Length)
            {
                _settingsSubsystem?.GetGraphicsSettings()?.SetResolution(
                    resolutions[value][0],
                    resolutions[value][1],
                    fullscreenToggle != null && fullscreenToggle.isOn
                );
            }
        }

        private void OnFullscreenChanged(bool value)
        {
            _settingsSubsystem?.GetGraphicsSettings()?.SetFullscreen(value);
        }

        private void OnVSyncChanged(bool value)
        {
            _settingsSubsystem?.GetGraphicsSettings()?.SetVSync(value);
        }

        private void OnFPSChanged(float value)
        {
            _settingsSubsystem?.GetGraphicsSettings()?.SetTargetFrameRate((int)value);
            UpdateFPSText((int)value);
        }

        private void OnMasterVolumeChanged(float value)
        {
            _settingsSubsystem?.GetAudioSettings()?.SetMasterVolume(value);
            UpdateVolumeText(masterVolumeText, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            _settingsSubsystem?.GetAudioSettings()?.SetMusicVolume(value);
            UpdateVolumeText(musicVolumeText, value);
        }

        private void OnSFXVolumeChanged(float value)
        {
            _settingsSubsystem?.GetAudioSettings()?.SetSFXVolume(value);
            UpdateVolumeText(sfxVolumeText, value);
        }

        private void OnSensitivityChanged(float value)
        {
            _settingsSubsystem?.GetInputSettings()?.SetMouseSensitivity(value);
            UpdateSensitivityText(value);
        }

        private void OnInvertYChanged(bool value)
        {
            _settingsSubsystem?.GetInputSettings()?.SetInvertYAxis(value);
        }

        private void OnDeadzoneChanged(float value)
        {
            _settingsSubsystem?.GetInputSettings()?.SetDeadzone(value);
            UpdateDeadzoneText(value);
        }

        private void OnApplyClicked()
        {
            _settingsSubsystem?.ApplyAllSettings();
            _settingsSubsystem?.SaveAllSettings();
            Debug.Log("[DemoSettingsMenu] Settings applied and saved");
        }

        private void OnResetClicked()
        {
            _settingsSubsystem?.ResetAllToDefaults();
            LoadCurrentSettings();
            Debug.Log("[DemoSettingsMenu] Settings reset to defaults");
        }

        private void OnCloseClicked()
        {
            ToggleMenu();
        }

        // UI text updates
        private void UpdateFPSText(int fps)
        {
            if (fpsText != null)
                fpsText.text = $"{fps} FPS";
        }

        private void UpdateVolumeText(Text text, float volume)
        {
            if (text != null)
                text.text = $"{(volume * 100f):F0}%";
        }

        private void UpdateSensitivityText(float sensitivity)
        {
            if (sensitivityText != null)
                sensitivityText.text = $"{sensitivity:F2}";
        }

        private void UpdateDeadzoneText(float deadzone)
        {
            if (deadzoneText != null)
                deadzoneText.text = $"{deadzone:F2}";
        }
    }
}
