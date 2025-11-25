using UnityEngine;

namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// ScriptableObject configuration for camera system
    /// Designer-friendly settings for all camera modes
    /// </summary>
    [CreateAssetMenu(fileName = "CameraConfig", menuName = "Chimera/Camera/Camera Config")]
    public class CameraConfig : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField] private CameraMode defaultMode = CameraMode.ThirdPerson;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Third Person Settings")]
        [SerializeField] private float thirdPersonDistance = 5f;
        [SerializeField] private float thirdPersonHeight = 2f;
        [SerializeField] private float thirdPersonFollowSpeed = 10f;
        [SerializeField] private float thirdPersonRotationSpeed = 5f;
        [SerializeField] private Vector2 thirdPersonPitchLimits = new Vector2(-30f, 60f);

        [Header("First Person Settings")]
        [SerializeField] private float firstPersonHeight = 1.7f;
        [SerializeField] private float firstPersonMouseSensitivity = 2f;
        [SerializeField] private Vector2 firstPersonPitchLimits = new Vector2(-85f, 85f);
        [SerializeField] private float firstPersonFOV = 75f;

        [Header("Top Down Settings")]
        [SerializeField] private float topDownDistance = 15f;
        [SerializeField] private float topDownAngle = 45f;
        [SerializeField] private float topDownPanSpeed = 10f;
        [SerializeField] private Vector2 topDownZoomLimits = new Vector2(5f, 30f);

        [Header("Racing Settings")]
        [SerializeField] private float racingDistance = 6f;
        [SerializeField] private float racingHeight = 2.5f;
        [SerializeField] private float racingFollowSpeed = 15f;
        [SerializeField] private float racingLookAheadDistance = 5f;

        [Header("Strategy Settings")]
        [SerializeField] private float strategyPanSpeed = 20f;
        [SerializeField] private float strategyRotationSpeed = 100f;
        [SerializeField] private Vector2 strategyZoomLimits = new Vector2(10f, 50f);
        [SerializeField] private float strategyZoomSpeed = 5f;

        [Header("Effects Settings")]
        [SerializeField] private float shakeIntensityMultiplier = 1f;
        [SerializeField] private float shakeDurationMultiplier = 1f;
        [SerializeField] private float zoomSpeed = 5f;
        [SerializeField] private bool enableCameraEffects = true;

        [Header("Performance Settings")]
        [SerializeField] private float cullingDistance = 100f;
        [SerializeField] private bool enableOcclusion = true;
        [SerializeField] private LayerMask occlusionLayers = -1;

        // Properties
        public CameraMode DefaultMode => defaultMode;
        public float TransitionDuration => transitionDuration;
        public AnimationCurve TransitionCurve => transitionCurve;

        public float ThirdPersonDistance => thirdPersonDistance;
        public float ThirdPersonHeight => thirdPersonHeight;
        public float ThirdPersonFollowSpeed => thirdPersonFollowSpeed;
        public float ThirdPersonRotationSpeed => thirdPersonRotationSpeed;
        public Vector2 ThirdPersonPitchLimits => thirdPersonPitchLimits;

        public float FirstPersonHeight => firstPersonHeight;
        public float FirstPersonMouseSensitivity => firstPersonMouseSensitivity;
        public Vector2 FirstPersonPitchLimits => firstPersonPitchLimits;
        public float FirstPersonFOV => firstPersonFOV;

        public float TopDownDistance => topDownDistance;
        public float TopDownAngle => topDownAngle;
        public float TopDownPanSpeed => topDownPanSpeed;
        public Vector2 TopDownZoomLimits => topDownZoomLimits;

        public float RacingDistance => racingDistance;
        public float RacingHeight => racingHeight;
        public float RacingFollowSpeed => racingFollowSpeed;
        public float RacingLookAheadDistance => racingLookAheadDistance;

        public float StrategyPanSpeed => strategyPanSpeed;
        public float StrategyRotationSpeed => strategyRotationSpeed;
        public Vector2 StrategyZoomLimits => strategyZoomLimits;
        public float StrategyZoomSpeed => strategyZoomSpeed;

        public float ShakeIntensityMultiplier => shakeIntensityMultiplier;
        public float ShakeDurationMultiplier => shakeDurationMultiplier;
        public float ZoomSpeed => zoomSpeed;
        public bool EnableCameraEffects => enableCameraEffects;

        public float CullingDistance => cullingDistance;
        public bool EnableOcclusion => enableOcclusion;
        public LayerMask OcclusionLayers => occlusionLayers;
    }
}
