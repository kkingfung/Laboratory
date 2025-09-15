using UnityEngine;

namespace Laboratory.Core.Character.Configuration
{
    /// <summary>
    /// Configuration settings for character aiming behavior.
    /// Create instances via Create Asset Menu for different character types.
    /// </summary>
    [CreateAssetMenu(fileName = "CharacterAimSettings", menuName = "Laboratory/Character/Aim Settings")]
    public class CharacterAimSettings : ScriptableObject
    {
        [Header("Targeting")]
        [SerializeField, Tooltip("Maximum distance for target acquisition")]
        [Range(1f, 50f)]
        public float maxAimDistance = 15f;

        [SerializeField, Tooltip("Layer mask for valid targets")]
        public LayerMask targetLayers = -1;

        [SerializeField, Tooltip("Speed of aiming transitions")]
        [Range(0.1f, 20f)]
        public float aimSpeed = 5f;

        [SerializeField, Tooltip("Use proximity-based targeting instead of raycast")]
        public bool useProximityTargeting = false;

        [SerializeField, Tooltip("Radius for proximity-based target detection")]
        [Range(0.5f, 10f)]
        public float proximityRadius = 3f;

        [Header("Constraint Weights")]
        [SerializeField, Tooltip("Weight for head aiming constraint")]
        [Range(0f, 1f)]
        public float headWeight = 0.8f;

        [SerializeField, Tooltip("Weight for chest aiming constraint")]
        [Range(0f, 1f)]
        public float chestWeight = 0.5f;

        [SerializeField, Tooltip("Weight for shoulder aiming constraint")]
        [Range(0f, 1f)]
        public float shoulderWeight = 0.3f;

        [SerializeField, Tooltip("Weight for spine aiming constraint")]
        [Range(0f, 1f)]
        public float spineWeight = 0.2f;

        [Header("Rotation Limits")]
        [SerializeField, Tooltip("Maximum head rotation angle")]
        [Range(30f, 180f)]
        public float maxHeadAngle = 80f;

        [SerializeField, Tooltip("Maximum neck rotation angle")]
        [Range(15f, 90f)]
        public float maxNeckAngle = 45f;

        [SerializeField, Tooltip("Enable rotation clamping")]
        public bool clampRotation = true;

        [Header("IK Fallback")]
        [SerializeField, Tooltip("Use Animator IK as fallback when rigging unavailable")]
        public bool useIKFallback = true;

        [SerializeField, Tooltip("IK look-at weight")]
        [Range(0f, 1f)]
        public float ikLookWeight = 0.8f;

        [SerializeField, Tooltip("IK body weight")]
        [Range(0f, 1f)]
        public float ikBodyWeight = 0.3f;

        [SerializeField, Tooltip("IK head weight")]
        [Range(0f, 1f)]
        public float ikHeadWeight = 0.8f;

        [SerializeField, Tooltip("IK eyes weight")]
        [Range(0f, 1f)]
        public float ikEyesWeight = 1f;

        [Header("Animation Blending")]
        [SerializeField, Tooltip("Speed of weight transitions")]
        [Range(0.1f, 20f)]
        public float blendSpeed = 8f;

        [SerializeField, Tooltip("Enable smooth blending")]
        public bool smoothBlending = true;

        [SerializeField, Tooltip("Weight curve for smooth transitions")]
        public AnimationCurve weightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Target Scoring")]
        [SerializeField, Tooltip("Weight factor for distance in target scoring")]
        [Range(0f, 2f)]
        public float distanceWeight = 1f;

        [SerializeField, Tooltip("Weight factor for angle in target scoring")]
        [Range(0f, 2f)]
        public float angleWeight = 0.5f;

        [SerializeField, Tooltip("Weight factor for visibility in target scoring")]
        [Range(0f, 2f)]
        public float visibilityWeight = 0.8f;

        [SerializeField, Tooltip("Prioritize closest target over scoring system")]
        public bool prioritizeClosest = true;

        /// <summary>
        /// Creates a default aim settings instance
        /// </summary>
        /// <returns>CharacterAimSettings with default values</returns>
        public static CharacterAimSettings CreateDefault()
        {
            var settings = CreateInstance<CharacterAimSettings>();
            settings.name = "Default Aim Settings";
            return settings;
        }

        /// <summary>
        /// Validates the settings and logs warnings for invalid values
        /// </summary>
        public void ValidateSettings()
        {
            if (maxAimDistance <= 0f)
            {
                Debug.LogWarning($"[{name}] MaxAimDistance should be greater than 0");
            }

            if (aimSpeed <= 0f)
            {
                Debug.LogWarning($"[{name}] AimSpeed should be greater than 0");
            }

            if (maxHeadAngle <= 0f || maxHeadAngle > 180f)
            {
                Debug.LogWarning($"[{name}] MaxHeadAngle should be between 0 and 180 degrees");
            }

            if (maxNeckAngle <= 0f || maxNeckAngle > 90f)
            {
                Debug.LogWarning($"[{name}] MaxNeckAngle should be between 0 and 90 degrees");
            }

            if (proximityRadius <= 0f)
            {
                Debug.LogWarning($"[{name}] ProximityRadius should be greater than 0");
            }
        }

        private void OnValidate()
        {
            ValidateSettings();
        }
    }
}
