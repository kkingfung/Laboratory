using UnityEngine;

namespace Laboratory.Subsystems.Settings
{
    /// <summary>
    /// ScriptableObject configuration for settings subsystem
    /// Designer-friendly presets and default values
    /// </summary>
    [CreateAssetMenu(fileName = "SettingsConfig", menuName = "Chimera/Settings/Settings Config")]
    public class SettingsConfig : ScriptableObject
    {
        [Header("Graphics Defaults")]
        [SerializeField] private int defaultQualityLevel = 2; // Medium
        [SerializeField] private int defaultResolutionWidth = 1920;
        [SerializeField] private int defaultResolutionHeight = 1080;
        [SerializeField] private bool defaultFullscreen = true;
        [SerializeField] private int defaultTargetFrameRate = 60;
        [SerializeField] private bool defaultVSync = true;

        [Header("Audio Defaults")]
        [SerializeField, Range(0f, 1f)] private float defaultMasterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultMusicVolume = 0.7f;
        [SerializeField, Range(0f, 1f)] private float defaultSFXVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float defaultVoiceVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float defaultUIVolume = 0.6f;

        [Header("Input Defaults")]
        [SerializeField, Range(0.1f, 5f)] private float defaultMouseSensitivity = 1f;
        [SerializeField] private bool defaultInvertYAxis = false;
        [SerializeField, Range(0f, 1f)] private float defaultDeadzone = 0.2f;

        [Header("Accessibility")]
        [SerializeField] private bool enableColorblindMode = false;
        [SerializeField] private bool enableSubtitles = true;
        [SerializeField] private float defaultSubtitleSize = 1f;
        [SerializeField] private bool enableScreenShake = true;
        [SerializeField] private bool enableMotionBlur = true;

        [Header("Performance")]
        [SerializeField] private bool enablePerformanceMode = false;
        [SerializeField] private bool enableAutoQuality = false;
        [SerializeField] private float minAcceptableFPS = 30f;

        // Properties
        public int DefaultQualityLevel => defaultQualityLevel;
        public int DefaultResolutionWidth => defaultResolutionWidth;
        public int DefaultResolutionHeight => defaultResolutionHeight;
        public bool DefaultFullscreen => defaultFullscreen;
        public int DefaultTargetFrameRate => defaultTargetFrameRate;
        public bool DefaultVSync => defaultVSync;

        public float DefaultMasterVolume => defaultMasterVolume;
        public float DefaultMusicVolume => defaultMusicVolume;
        public float DefaultSFXVolume => defaultSFXVolume;
        public float DefaultVoiceVolume => defaultVoiceVolume;
        public float DefaultUIVolume => defaultUIVolume;

        public float DefaultMouseSensitivity => defaultMouseSensitivity;
        public bool DefaultInvertYAxis => defaultInvertYAxis;
        public float DefaultDeadzone => defaultDeadzone;

        public bool EnableColorblindMode => enableColorblindMode;
        public bool EnableSubtitles => enableSubtitles;
        public float DefaultSubtitleSize => defaultSubtitleSize;
        public bool EnableScreenShake => enableScreenShake;
        public bool EnableMotionBlur => enableMotionBlur;

        public bool EnablePerformanceMode => enablePerformanceMode;
        public bool EnableAutoQuality => enableAutoQuality;
        public float MinAcceptableFPS => minAcceptableFPS;
    }
}
