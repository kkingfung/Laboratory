using UnityEngine;

namespace Laboratory.Core.Abilities.Configuration
{
    /// <summary>
    /// Configuration settings for the Ability System.
    /// Centralized configuration for performance, UI, and debugging options.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilitySystemSettings", menuName = "Laboratory/Settings/Ability System")]
    public class AbilitySystemSettings : ScriptableObject
    {
        [Header("Performance")]
        [Tooltip("Maximum number of abilities per manager")]
        public int maxAbilitiesPerManager = 10;
        
        [Tooltip("How often to update ability systems (in seconds)")]
        public float updateFrequency = 0.02f; // 50 FPS
        
        [Tooltip("Enable object pooling for events")]
        public bool enableEventPooling = true;

        [Header("UI Settings")]
        [Tooltip("Enable visual cooldown indicators")]
        public bool enableCooldownVisuals = true;
        
        [Tooltip("Curve for cooldown animation")]
        public AnimationCurve cooldownCurve = AnimationCurve.Linear(0, 1, 1, 0);
        
        [Tooltip("Show ability tooltips on hover")]
        public bool enableTooltips = true;

        [Header("Audio")]
        [Tooltip("Enable ability sound effects")]
        public bool enableAbilitySFX = true;
        
        [Tooltip("Master volume for ability sounds")]
        [Range(0f, 1f)]
        public float abilitySFXVolume = 1f;

        [Header("Debugging")]
        [Tooltip("Enable debug logging for ability system")]
        public bool enableDebugLogs = false;
        
        [Tooltip("Show ability gizmos in scene view")]
        public bool enableGizmos = false;
        
        [Tooltip("Show performance metrics in console")]
        public bool showPerformanceMetrics = false;

        [Header("Validation")]
        [Tooltip("Validate ability data on startup")]
        public bool validateOnStartup = true;
        
        [Tooltip("Strict mode - fail on validation errors")]
        public bool strictValidation = false;

        #region Validation

        private void OnValidate()
        {
            // Ensure values are within reasonable bounds
            maxAbilitiesPerManager = Mathf.Clamp(maxAbilitiesPerManager, 1, 50);
            updateFrequency = Mathf.Clamp(updateFrequency, 0.01f, 1f);
            abilitySFXVolume = Mathf.Clamp01(abilitySFXVolume);
        }

        #endregion

        #region Static Access

        private static AbilitySystemSettings _instance;
        
        /// <summary>
        /// Gets the current ability system settings instance.
        /// </summary>
        public static AbilitySystemSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<AbilitySystemSettings>("AbilitySystemSettings");
                    if (_instance == null)
                    {
                        Debug.LogWarning("[AbilitySystemSettings] No settings found in Resources. Using default settings.");
                        _instance = CreateInstance<AbilitySystemSettings>();
                    }
                }
                return _instance;
            }
        }

        #endregion
    }
}
