using UnityEngine;

namespace Laboratory.Subsystems.Player
{
    /// <summary>
    /// Configuration settings for the Player Subsystem.
    /// ScriptableObject that can be customized per game mode or character type.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerSubsystemConfig", menuName = "Laboratory/Player/Subsystem Config")]
    public class PlayerSubsystemConfig : ScriptableObject
    {
        #region Movement Settings
        
        [Header("Movement Configuration")]
        [Tooltip("Minimum distance moved to register as movement")]
        [SerializeField, Range(0.001f, 0.1f)] 
        private float _movementThreshold = 0.01f;
        
        [Tooltip("Movement smoothing factor")]
        [SerializeField, Range(0.1f, 10f)] 
        private float _movementSmoothing = 2.0f;
        
        [Tooltip("Maximum movement speed")]
        [SerializeField, Range(1f, 20f)] 
        private float _maxMovementSpeed = 8.0f;
        
        #endregion
        
        #region Camera Settings
        
        [Header("Camera Configuration")]
        [Tooltip("Camera follow smoothing")]
        [SerializeField, Range(0.1f, 5f)] 
        private float _cameraFollowSmoothing = 1.5f;
        
        [Tooltip("Camera distance from player")]
        [SerializeField, Range(2f, 15f)] 
        private float _cameraDistance = 5.0f;
        
        [Tooltip("Camera height offset")]
        [SerializeField, Range(0f, 5f)] 
        private float _cameraHeightOffset = 1.5f;
        
        #endregion
        
        #region Health Settings
        
        [Header("Health Configuration")]
        [Tooltip("Player maximum health")]
        [SerializeField, Range(50, 1000)] 
        private int _maxHealth = 100;
        
        [Tooltip("Health regeneration rate per second")]
        [SerializeField, Range(0f, 50f)] 
        private float _healthRegenRate = 2.0f;
        
        [Tooltip("Delay before health regeneration starts")]
        [SerializeField, Range(0f, 10f)] 
        private float _healthRegenDelay = 3.0f;
        
        #endregion
        
        #region Combat Settings
        
        [Header("Combat Configuration")]
        [Tooltip("Invulnerability duration after taking damage")]
        [SerializeField, Range(0f, 5f)] 
        private float _invulnerabilityDuration = 0.5f;
        
        [Tooltip("Damage reduction percentage (0-1)")]
        [SerializeField, Range(0f, 0.9f)] 
        private float _damageReduction = 0.0f;
        
        #endregion
        
        #region Input Settings
        
        [Header("Input Configuration")]
        [Tooltip("Input response sensitivity")]
        [SerializeField, Range(0.1f, 3f)] 
        private float _inputSensitivity = 1.0f;
        
        [Tooltip("Input deadzone threshold")]
        [SerializeField, Range(0.05f, 0.5f)] 
        private float _inputDeadzone = 0.1f;
        
        #endregion
        
        #region Look System Settings
        
        [Header("Look System Configuration")]
        [Tooltip("Maximum angle the head can turn")]
        [SerializeField, Range(30f, 120f)] 
        private float _maxHeadTurnAngle = 80f;
        
        [Tooltip("Head rotation speed")]
        [SerializeField, Range(1f, 10f)] 
        private float _headRotationSpeed = 5f;
        
        [Tooltip("Use proximity-based targeting")]
        [SerializeField] 
        private bool _useProximityTargeting = false;
        
        [Tooltip("Proximity radius for target detection")]
        [SerializeField, Range(1f, 10f)] 
        private float _proximityRadius = 3f;
        
        #endregion
        
        #region Climbing Settings
        
        [Header("Climbing Configuration")]
        [Tooltip("Enable climbing system")]
        [SerializeField] 
        private bool _enableClimbing = true;
        
        [Tooltip("Climbing speed multiplier")]
        [SerializeField, Range(0.1f, 2f)] 
        private float _climbingSpeed = 0.5f;
        
        [Tooltip("Maximum climbing height")]
        [SerializeField, Range(5f, 50f)] 
        private float _maxClimbingHeight = 20f;
        
        #endregion
        
        #region Debug Settings
        
        [Header("Debug Configuration")]
        [Tooltip("Enable debug visualizations")]
        [SerializeField] 
        private bool _enableDebugVisuals = false;
        
        [Tooltip("Enable detailed logging")]
        [SerializeField] 
        private bool _enableDetailedLogging = false;
        
        [Tooltip("Show performance metrics")]
        [SerializeField] 
        private bool _showPerformanceMetrics = false;
        
        #endregion
        
        #region Properties
        
        // Movement Properties
        public float MovementThreshold => _movementThreshold;
        public float MovementSmoothing => _movementSmoothing;
        public float MaxMovementSpeed => _maxMovementSpeed;
        
        // Camera Properties
        public float CameraFollowSmoothing => _cameraFollowSmoothing;
        public float CameraDistance => _cameraDistance;
        public float CameraHeightOffset => _cameraHeightOffset;
        
        // Health Properties
        public int MaxHealth => _maxHealth;
        public float HealthRegenRate => _healthRegenRate;
        public float HealthRegenDelay => _healthRegenDelay;
        
        // Combat Properties
        public float InvulnerabilityDuration => _invulnerabilityDuration;
        public float DamageReduction => _damageReduction;
        
        // Input Properties
        public float InputSensitivity => _inputSensitivity;
        public float InputDeadzone => _inputDeadzone;
        
        // Look System Properties
        public float MaxHeadTurnAngle => _maxHeadTurnAngle;
        public float HeadRotationSpeed => _headRotationSpeed;
        public bool UseProximityTargeting => _useProximityTargeting;
        public float ProximityRadius => _proximityRadius;
        
        // Climbing Properties
        public bool EnableClimbing => _enableClimbing;
        public float ClimbingSpeed => _climbingSpeed;
        public float MaxClimbingHeight => _maxClimbingHeight;
        
        // Debug Properties
        public bool EnableDebugVisuals => _enableDebugVisuals;
        public bool EnableDetailedLogging => _enableDetailedLogging;
        public bool ShowPerformanceMetrics => _showPerformanceMetrics;
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            // Ensure sane values
            _movementThreshold = Mathf.Max(0.001f, _movementThreshold);
            _movementSmoothing = Mathf.Max(0.1f, _movementSmoothing);
            _maxMovementSpeed = Mathf.Max(1f, _maxMovementSpeed);
            
            _cameraFollowSmoothing = Mathf.Max(0.1f, _cameraFollowSmoothing);
            _cameraDistance = Mathf.Max(2f, _cameraDistance);
            _cameraHeightOffset = Mathf.Max(0f, _cameraHeightOffset);
            
            _maxHealth = Mathf.Max(1, _maxHealth);
            _healthRegenRate = Mathf.Max(0f, _healthRegenRate);
            _healthRegenDelay = Mathf.Max(0f, _healthRegenDelay);
            
            _invulnerabilityDuration = Mathf.Max(0f, _invulnerabilityDuration);
            _damageReduction = Mathf.Clamp01(_damageReduction);
            
            _inputSensitivity = Mathf.Max(0.1f, _inputSensitivity);
            _inputDeadzone = Mathf.Clamp(_inputDeadzone, 0.05f, 0.5f);
            
            _maxHeadTurnAngle = Mathf.Clamp(_maxHeadTurnAngle, 30f, 120f);
            _headRotationSpeed = Mathf.Max(1f, _headRotationSpeed);
            _proximityRadius = Mathf.Max(1f, _proximityRadius);
            
            _climbingSpeed = Mathf.Max(0.1f, _climbingSpeed);
            _maxClimbingHeight = Mathf.Max(5f, _maxClimbingHeight);
        }
        
        #endregion
        
        #region Presets
        
        [ContextMenu("Reset to Default")]
        public void ResetToDefault()
        {
            _movementThreshold = 0.01f;
            _movementSmoothing = 2.0f;
            _maxMovementSpeed = 8.0f;
            
            _cameraFollowSmoothing = 1.5f;
            _cameraDistance = 5.0f;
            _cameraHeightOffset = 1.5f;
            
            _maxHealth = 100;
            _healthRegenRate = 2.0f;
            _healthRegenDelay = 3.0f;
            
            _invulnerabilityDuration = 0.5f;
            _damageReduction = 0.0f;
            
            _inputSensitivity = 1.0f;
            _inputDeadzone = 0.1f;
            
            _maxHeadTurnAngle = 80f;
            _headRotationSpeed = 5f;
            _useProximityTargeting = false;
            _proximityRadius = 3f;
            
            _enableClimbing = true;
            _climbingSpeed = 0.5f;
            _maxClimbingHeight = 20f;
            
            _enableDebugVisuals = false;
            _enableDetailedLogging = false;
            _showPerformanceMetrics = false;
        }
        
        [ContextMenu("Set Combat Focused")]
        public void SetCombatFocused()
        {
            ResetToDefault();
            
            _maxHealth = 150;
            _healthRegenRate = 3.0f;
            _healthRegenDelay = 2.0f;
            _invulnerabilityDuration = 0.3f;
            _damageReduction = 0.1f;
            _inputSensitivity = 1.5f;
            _maxHeadTurnAngle = 90f;
            _headRotationSpeed = 7f;
            _useProximityTargeting = true;
        }
        
        [ContextMenu("Set Exploration Focused")]
        public void SetExplorationFocused()
        {
            ResetToDefault();
            
            _maxMovementSpeed = 12.0f;
            _movementSmoothing = 3.0f;
            _cameraDistance = 7.0f;
            _enableClimbing = true;
            _climbingSpeed = 0.8f;
            _maxClimbingHeight = 30f;
            _proximityRadius = 5f;
        }
        
        [ContextMenu("Set Performance Optimized")]
        public void SetPerformanceOptimized()
        {
            ResetToDefault();
            
            _enableDebugVisuals = false;
            _enableDetailedLogging = false;
            _showPerformanceMetrics = false;
            _movementSmoothing = 1.5f;
            _cameraFollowSmoothing = 1.2f;
            _healthRegenRate = 1.5f;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Creates a runtime copy of this configuration for modification.
        /// </summary>
        public PlayerSubsystemConfig CreateRuntimeCopy()
        {
            var copy = CreateInstance<PlayerSubsystemConfig>();
            copy.CopyFrom(this);
            return copy;
        }
        
        /// <summary>
        /// Copies all values from another configuration.
        /// </summary>
        public void CopyFrom(PlayerSubsystemConfig other)
        {
            _movementThreshold = other._movementThreshold;
            _movementSmoothing = other._movementSmoothing;
            _maxMovementSpeed = other._maxMovementSpeed;
            
            _cameraFollowSmoothing = other._cameraFollowSmoothing;
            _cameraDistance = other._cameraDistance;
            _cameraHeightOffset = other._cameraHeightOffset;
            
            _maxHealth = other._maxHealth;
            _healthRegenRate = other._healthRegenRate;
            _healthRegenDelay = other._healthRegenDelay;
            
            _invulnerabilityDuration = other._invulnerabilityDuration;
            _damageReduction = other._damageReduction;
            
            _inputSensitivity = other._inputSensitivity;
            _inputDeadzone = other._inputDeadzone;
            
            _maxHeadTurnAngle = other._maxHeadTurnAngle;
            _headRotationSpeed = other._headRotationSpeed;
            _useProximityTargeting = other._useProximityTargeting;
            _proximityRadius = other._proximityRadius;
            
            _enableClimbing = other._enableClimbing;
            _climbingSpeed = other._climbingSpeed;
            _maxClimbingHeight = other._maxClimbingHeight;
            
            _enableDebugVisuals = other._enableDebugVisuals;
            _enableDetailedLogging = other._enableDetailedLogging;
            _showPerformanceMetrics = other._showPerformanceMetrics;
        }
        
        /// <summary>
        /// Validates the configuration and returns any issues found.
        /// </summary>
        public string[] ValidateConfiguration()
        {
            var issues = new System.Collections.Generic.List<string>();
            
            if (_maxHealth <= 0)
                issues.Add("Max health must be greater than 0");
            
            if (_movementThreshold <= 0)
                issues.Add("Movement threshold must be greater than 0");
            
            if (_cameraDistance < 1f)
                issues.Add("Camera distance should be at least 1 unit");
            
            if (_inputDeadzone >= 0.5f)
                issues.Add("Input deadzone is too large (should be < 0.5)");
            
            return issues.ToArray();
        }
        
        #endregion
    }
}