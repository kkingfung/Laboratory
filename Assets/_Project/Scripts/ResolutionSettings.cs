using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.Infrastructure.Settings
{
    /// <summary>
    /// Manages display resolution settings with a predefined set of supported resolutions.
    /// Handles fullscreen toggle and saves/loads settings from PlayerPrefs.
    /// </summary>
    public class ResolutionSettings : MonoBehaviour
    {
        #region Constants
        
        private const string PREF_RESOLUTION_INDEX = "resolution_index";
        private const string PREF_FULLSCREEN = "fullscreen";
        
        #endregion
        
        #region Serialized Fields
        
        [Header("UI Elements")]
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Predefined list of supported screen resolutions.
        /// </summary>
        private readonly List<Vector2Int> allowedResolutions = new List<Vector2Int>()
        {
            new Vector2Int(1280, 720),   // HD
            new Vector2Int(1600, 900),   // HD+
            new Vector2Int(1920, 1080),  // Full HD
            new Vector2Int(2560, 1440),  // 2K
            new Vector2Int(3840, 2160)   // 4K
        };
        
        private int currentResolutionIndex = 0;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes resolution settings by loading saved preferences and setting up UI.
        /// </summary>
        private void Start()
        {
            LoadSettings();
            SetupResolutionOptions();
            SetupEventListeners();
        }
        
        #endregion
        
        #region Public Methods - UI Event Handlers
        
        /// <summary>
        /// Handles resolution dropdown selection change.
        /// </summary>
        /// <param name="index">The selected resolution index</param>
        public void OnResolutionChange(int index)
        {
            if (index < 0 || index >= allowedResolutions.Count)
            {
                Debug.LogWarning($"Invalid resolution index: {index}. Clamping to valid range.", this);
                index = Mathf.Clamp(index, 0, allowedResolutions.Count - 1);
            }
            
            currentResolutionIndex = index;
            ApplySettings();
        }

        /// <summary>
        /// Handles fullscreen toggle state change.
        /// </summary>
        /// <param name="isFullscreen">Whether fullscreen mode should be enabled</param>
        public void OnFullscreenToggle(bool isFullscreen)
        {
            ApplySettings();
        }
        
        /// <summary>
        /// Resets resolution settings to default values.
        /// </summary>
        public void ResetToDefaults()
        {
            currentResolutionIndex = GetDefaultResolutionIndex();
            
            if (resolutionDropdown != null)
            {
                resolutionDropdown.value = currentResolutionIndex;
                resolutionDropdown.RefreshShownValue();
            }
            
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = true;
            }
            
            ApplySettings();
        }
        
        #endregion
        
        #region Private Methods - Initialization
        
        /// <summary>
        /// Sets up the resolution dropdown with available options.
        /// </summary>
        private void SetupResolutionOptions()
        {
            if (resolutionDropdown == null)
            {
                Debug.LogError("Resolution dropdown is not assigned!", this);
                return;
            }
            
            resolutionDropdown.ClearOptions();
            
            var options = new List<string>();
            foreach (var resolution in allowedResolutions)
            {
                string option = $"{resolution.x} x {resolution.y}";
                options.Add(option);
            }
            
            resolutionDropdown.AddOptions(options);
            
            // Validate and set current resolution index
            currentResolutionIndex = Mathf.Clamp(currentResolutionIndex, 0, allowedResolutions.Count - 1);
            resolutionDropdown.value = currentResolutionIndex;
            resolutionDropdown.RefreshShownValue();
            
            // Set fullscreen toggle state
            if (fullscreenToggle != null)
            {
                fullscreenToggle.isOn = Screen.fullScreen;
            }
        }
        
        /// <summary>
        /// Sets up event listeners for UI components.
        /// </summary>
        private void SetupEventListeners()
        {
            if (resolutionDropdown != null)
            {
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChange);
            }
            
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
            }
        }
        
        #endregion
        
        #region Private Methods - Settings Management
        
        /// <summary>
        /// Applies the currently selected resolution and fullscreen settings.
        /// </summary>
        private void ApplySettings()
        {
            if (currentResolutionIndex < 0 || currentResolutionIndex >= allowedResolutions.Count)
            {
                Debug.LogError($"Cannot apply settings - invalid resolution index: {currentResolutionIndex}", this);
                return;
            }
            
            var resolution = allowedResolutions[currentResolutionIndex];
            bool isFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
            
            try
            {
                Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
                SaveSettings();
                
                Debug.Log($"Resolution applied: {resolution.x}x{resolution.y}, Fullscreen: {isFullscreen}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to apply resolution settings: {e.Message}", this);
            }
        }

        /// <summary>
        /// Saves the current resolution settings to PlayerPrefs.
        /// </summary>
        private void SaveSettings()
        {
            PlayerPrefs.SetInt(PREF_RESOLUTION_INDEX, currentResolutionIndex);
            PlayerPrefs.SetInt(PREF_FULLSCREEN, fullscreenToggle != null && fullscreenToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads resolution settings from PlayerPrefs.
        /// </summary>
        private void LoadSettings()
        {
            currentResolutionIndex = PlayerPrefs.GetInt(PREF_RESOLUTION_INDEX, GetDefaultResolutionIndex());
            bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, 1) == 1;
            
            // Apply fullscreen setting immediately
            Screen.fullScreen = isFullscreen;
        }
        
        /// <summary>
        /// Gets the default resolution index (Full HD - 1920x1080).
        /// </summary>
        /// <returns>Index of the default resolution</returns>
        private int GetDefaultResolutionIndex()
        {
            // Default to Full HD (1920x1080)
            for (int i = 0; i < allowedResolutions.Count; i++)
            {
                if (allowedResolutions[i].x == 1920 && allowedResolutions[i].y == 1080)
                {
                    return i;
                }
            }
            
            // Fallback to first resolution if Full HD not found
            return 0;
        }
        
        #endregion
    }
}
