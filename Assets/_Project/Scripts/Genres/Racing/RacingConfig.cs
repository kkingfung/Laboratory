using UnityEngine;

namespace Laboratory.Genres.Racing
{
    /// <summary>
    /// ScriptableObject configuration for Racing genre
    /// Designer-friendly settings for vehicle physics and race rules
    /// </summary>
    [CreateAssetMenu(fileName = "RacingConfig", menuName = "Chimera/Genres/Racing Config")]
    public class RacingConfig : ScriptableObject
    {
        [Header("Vehicle Physics")]
        [SerializeField] private float maxSpeed = 30f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float brakeForce = 15f;
        [SerializeField] private float turnSpeed = 100f;
        [SerializeField] private float driftFactor = 0.95f;

        [Header("Race Settings")]
        [SerializeField] private int totalLaps = 3;
        [SerializeField] private float countdownDuration = 3f;
        [SerializeField] private bool enableWrongWayDetection = true;
        [SerializeField] private float wrongWayWarningTime = 3f;

        [Header("Difficulty")]
        [SerializeField] private float aiSpeedMultiplier = 0.8f;
        [SerializeField] private int aiRacers = 5;
        [SerializeField] private bool enableRubberBanding = true;

        [Header("Boost System")]
        [SerializeField] private bool enableBoost = true;
        [SerializeField] private float boostSpeed = 50f;
        [SerializeField] private float boostDuration = 2f;
        [SerializeField] private float boostCooldown = 5f;

        [Header("Track Settings")]
        [SerializeField] private float checkpointRadius = 5f;
        [SerializeField] private bool respawnOnOutOfBounds = true;
        [SerializeField] private float respawnHeight = 2f;

        // Properties
        public float MaxSpeed => maxSpeed;
        public float Acceleration => acceleration;
        public float BrakeForce => brakeForce;
        public float TurnSpeed => turnSpeed;
        public float DriftFactor => driftFactor;

        public int TotalLaps => totalLaps;
        public float CountdownDuration => countdownDuration;
        public bool EnableWrongWayDetection => enableWrongWayDetection;
        public float WrongWayWarningTime => wrongWayWarningTime;

        public float AISpeedMultiplier => aiSpeedMultiplier;
        public int AIRacers => aiRacers;
        public bool EnableRubberBanding => enableRubberBanding;

        public bool EnableBoost => enableBoost;
        public float BoostSpeed => boostSpeed;
        public float BoostDuration => boostDuration;
        public float BoostCooldown => boostCooldown;

        public float CheckpointRadius => checkpointRadius;
        public bool RespawnOnOutOfBounds => respawnOnOutOfBounds;
        public float RespawnHeight => respawnHeight;
    }
}
