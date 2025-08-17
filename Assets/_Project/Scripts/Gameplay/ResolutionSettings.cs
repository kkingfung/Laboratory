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
        [SerializeField] private Dropdown _resolutionDropdown;
        [SerializeField] private Toggle _fullscreenToggle;
        
        #endregion
        
        #region Private Fields
        
        /// <summary>
        /// Predefined list of supported screen resolutions.
        /// </summary>
        private readonly List<Vector2Int> _allowedResolutions = new List<Vector2Int>()
        {
            new Vector2Int(1280, 720),   // HD
            new Vector2Int(1600, 900),   // HD+
            new Vector2Int(1920, 1080),  // Full HD
            new Vector2Int(2560, 1440),  // 2K
            new Vector2Int(3840, 2160)   // 4K
        };
        
        private int _currentResolutionIndex = 0;
        
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
            if (index < 0 || index >= _allowedResolutions.Count)
            {
                Debug.LogWarning($"Invalid resolution index: {index}. Clamping to valid range.", this);
                index = Mathf.Clamp(index, 0, _allowedResolutions.Count - 1);
            }
            
            _currentResolutionIndex = index;
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
            _currentResolutionIndex = GetDefaultResolutionIndex();
            
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.value = _currentResolutionIndex;
                _resolutionDropdown.RefreshShownValue();
            }
            
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = true;
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
            if (_resolutionDropdown == null)
            {
                Debug.LogError("Resolution dropdown is not assigned!", this);
                return;
            }
            
            _resolutionDropdown.ClearOptions();
            
            var options = new List<string>();
            foreach (var resolution in _allowedResolutions)
            {
                string option = $"{resolution.x} x {resolution.y}";
                options.Add(option);
            }
            
            _resolutionDropdown.AddOptions(options);
            
            // Validate and set current resolution index
            _currentResolutionIndex = Mathf.Clamp(_currentResolutionIndex, 0, _allowedResolutions.Count - 1);
            _resolutionDropdown.value = _currentResolutionIndex;
            _resolutionDropdown.RefreshShownValue();
            
            // Set fullscreen toggle state
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.isOn = Screen.fullScreen;
            }
        }
        
        /// <summary>
        /// Sets up event listeners for UI components.
        /// </summary>
        private void SetupEventListeners()
        {
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.onValueChanged.AddListener(OnResolutionChange);
            }
            
            if (_fullscreenToggle != null)
            {
                _fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
            }
        }
        
        #endregion
        
        #region Private Methods - Settings Management
        
        /// <summary>
        /// Applies the currently selected resolution and fullscreen settings.
        /// </summary>
        private void ApplySettings()
        {
            if (_currentResolutionIndex < 0 || _currentResolutionIndex >= _allowedResolutions.Count)
            {
                Debug.LogError($"Cannot apply settings - invalid resolution index: {_currentResolutionIndex}", this);
                return;
            }
            
            var resolution = _allowedResolutions[_currentResolutionIndex];
            bool isFullscreen = _fullscreenToggle != null ? _fullscreenToggle.isOn : Screen.fullScreen;
            
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
            PlayerPrefs.SetInt(PREF_RESOLUTION_INDEX, _currentResolutionIndex);
            PlayerPrefs.SetInt(PREF_FULLSCREEN, _fullscreenToggle != null && _fullscreenToggle.isOn ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Loads resolution settings from PlayerPrefs.
        /// </summary>
        private void LoadSettings()
        {
            _currentResolutionIndex = PlayerPrefs.GetInt(PREF_RESOLUTION_INDEX, GetDefaultResolutionIndex());
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
            for (int i = 0; i < _allowedResolutions.Count; i++)
            {
                if (_allowedResolutions[i].x == 1920 && _allowedResolutions[i].y == 1080)
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
