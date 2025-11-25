using UnityEngine;
using UnityEngine.Rendering;

namespace Laboratory.Subsystems.Settings
{
    /// <summary>
    /// Comprehensive graphics settings management
    /// Handles quality, resolution, effects, and performance
    /// </summary>
    public class GraphicsSettings : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SettingsConfig config;

        // Current settings
        private int _currentQualityLevel;
        private int _currentResolutionWidth;
        private int _currentResolutionHeight;
        private bool _isFullscreen;
        private int _targetFrameRate;
        private bool _vSyncEnabled;

        // Graphics features
        private bool _antiAliasingEnabled = true;
        private bool _shadowsEnabled = true;
        private bool _postProcessingEnabled = true;
        private bool _particlesEnabled = true;
        private float _renderScale = 1f;

        // Performance monitoring
        private float _averageFPS = 60f;
        private bool _autoQualityAdjustment = false;

        // Events
        public event System.Action OnGraphicsSettingsChanged;

        // PlayerPrefs keys
        private const string PREF_QUALITY = "Graphics_Quality";
        private const string PREF_RES_WIDTH = "Graphics_ResWidth";
        private const string PREF_RES_HEIGHT = "Graphics_ResHeight";
        private const string PREF_FULLSCREEN = "Graphics_Fullscreen";
        private const string PREF_VSYNC = "Graphics_VSync";
        private const string PREF_FRAMERATE = "Graphics_TargetFPS";

        private void Start()
        {
            LoadSettings();
            ApplySettings();
        }

        /// <summary>
        /// Load settings from PlayerPrefs or use defaults
        /// </summary>
        public void LoadSettings()
        {
            if (config == null)
            {
                Debug.LogWarning("[GraphicsSettings] No configuration assigned!");
                return;
            }

            _currentQualityLevel = PlayerPrefs.GetInt(PREF_QUALITY, config.DefaultQualityLevel);
            _currentResolutionWidth = PlayerPrefs.GetInt(PREF_RES_WIDTH, config.DefaultResolutionWidth);
            _currentResolutionHeight = PlayerPrefs.GetInt(PREF_RES_HEIGHT, config.DefaultResolutionHeight);
            _isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, config.DefaultFullscreen ? 1 : 0) == 1;
            _vSyncEnabled = PlayerPrefs.GetInt(PREF_VSYNC, config.DefaultVSync ? 1 : 0) == 1;
            _targetFrameRate = PlayerPrefs.GetInt(PREF_FRAMERATE, config.DefaultTargetFrameRate);

            Debug.Log($"[GraphicsSettings] Loaded settings: Quality={_currentQualityLevel}, Resolution={_currentResolutionWidth}x{_currentResolutionHeight}");
        }

        /// <summary>
        /// Save settings to PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetInt(PREF_QUALITY, _currentQualityLevel);
            PlayerPrefs.SetInt(PREF_RES_WIDTH, _currentResolutionWidth);
            PlayerPrefs.SetInt(PREF_RES_HEIGHT, _currentResolutionHeight);
            PlayerPrefs.SetInt(PREF_FULLSCREEN, _isFullscreen ? 1 : 0);
            PlayerPrefs.SetInt(PREF_VSYNC, _vSyncEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PREF_FRAMERATE, _targetFrameRate);
            PlayerPrefs.Save();

            Debug.Log("[GraphicsSettings] Settings saved");
        }

        /// <summary>
        /// Apply all graphics settings
        /// </summary>
        public void ApplySettings()
        {
            // Quality level
            QualitySettings.SetQualityLevel(_currentQualityLevel);

            // Resolution and fullscreen
            Screen.SetResolution(_currentResolutionWidth, _currentResolutionHeight, _isFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

            // VSync
            QualitySettings.vSyncCount = _vSyncEnabled ? 1 : 0;

            // Target frame rate
            Application.targetFrameRate = _targetFrameRate;

            // Render pipeline quality
            ApplyRenderPipelineSettings();

            OnGraphicsSettingsChanged?.Invoke();

            Debug.Log($"[GraphicsSettings] Applied settings: Quality={_currentQualityLevel}, FPS target={_targetFrameRate}");
        }

        /// <summary>
        /// Apply render pipeline specific settings
        /// </summary>
        private void ApplyRenderPipelineSettings()
        {
            // This would be customized based on your render pipeline (URP, HDRP, Built-in)
            // Example for URP:
            var renderPipelineAsset = QualitySettings.renderPipeline;
            if (renderPipelineAsset != null)
            {
                // Configure render pipeline asset settings here
                Debug.Log($"[GraphicsSettings] Render pipeline: {renderPipelineAsset.name}");
            }
        }

        /// <summary>
        /// Set quality preset (Low, Medium, High, Ultra)
        /// </summary>
        public void SetQuality(int qualityLevel)
        {
            _currentQualityLevel = Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(_currentQualityLevel);

            SaveSettings();
            OnGraphicsSettingsChanged?.Invoke();

            Debug.Log($"[GraphicsSettings] Quality set to: {QualitySettings.names[_currentQualityLevel]}");
        }

        /// <summary>
        /// Set resolution
        /// </summary>
        public void SetResolution(int width, int height, bool fullscreen)
        {
            _currentResolutionWidth = width;
            _currentResolutionHeight = height;
            _isFullscreen = fullscreen;

            Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

            SaveSettings();
            OnGraphicsSettingsChanged?.Invoke();

            Debug.Log($"[GraphicsSettings] Resolution set to: {width}x{height} (Fullscreen: {fullscreen})");
        }

        /// <summary>
        /// Toggle fullscreen
        /// </summary>
        public void SetFullscreen(bool fullscreen)
        {
            _isFullscreen = fullscreen;
            Screen.fullScreenMode = fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

            SaveSettings();
            OnGraphicsSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set target frame rate
        /// </summary>
        public void SetTargetFrameRate(int fps)
        {
            _targetFrameRate = fps;
            Application.targetFrameRate = fps;

            SaveSettings();
            OnGraphicsSettingsChanged?.Invoke();

            Debug.Log($"[GraphicsSettings] Target FPS set to: {fps}");
        }

        /// <summary>
        /// Toggle VSync
        /// </summary>
        public void SetVSync(bool enabled)
        {
            _vSyncEnabled = enabled;
            QualitySettings.vSyncCount = enabled ? 1 : 0;

            SaveSettings();
            OnGraphicsSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Enable/disable anti-aliasing
        /// </summary>
        public void SetAntiAliasing(bool enabled)
        {
            _antiAliasingEnabled = enabled;
            // Apply to render pipeline
            OnGraphicsSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Enable/disable shadows
        /// </summary>
        public void SetShadows(bool enabled)
        {
            _shadowsEnabled = enabled;
            QualitySettings.shadows = enabled ? ShadowQuality.All : ShadowQuality.Disable;
            OnGraphicsSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Enable/disable post-processing
        /// </summary>
        public void SetPostProcessing(bool enabled)
        {
            _postProcessingEnabled = enabled;
            // Apply to render pipeline
            OnGraphicsSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set render scale for performance
        /// </summary>
        public void SetRenderScale(float scale)
        {
            _renderScale = Mathf.Clamp(scale, 0.5f, 2f);
            // Apply to render pipeline
            OnGraphicsSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            if (config == null) return;

            _currentQualityLevel = config.DefaultQualityLevel;
            _currentResolutionWidth = config.DefaultResolutionWidth;
            _currentResolutionHeight = config.DefaultResolutionHeight;
            _isFullscreen = config.DefaultFullscreen;
            _vSyncEnabled = config.DefaultVSync;
            _targetFrameRate = config.DefaultTargetFrameRate;

            ApplySettings();
            SaveSettings();

            Debug.Log("[GraphicsSettings] Reset to defaults");
        }

        // Getters
        public int GetQualityLevel() => _currentQualityLevel;
        public int GetResolutionWidth() => _currentResolutionWidth;
        public int GetResolutionHeight() => _currentResolutionHeight;
        public bool IsFullscreen() => _isFullscreen;
        public bool IsVSyncEnabled() => _vSyncEnabled;
        public int GetTargetFrameRate() => _targetFrameRate;
        public float GetRenderScale() => _renderScale;
    }
}
