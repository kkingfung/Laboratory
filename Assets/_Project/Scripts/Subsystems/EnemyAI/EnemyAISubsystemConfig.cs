using UnityEngine;

namespace Laboratory.Subsystems.EnemyAI
{
    /// <summary>
    /// Configuration settings for the Enemy AI Subsystem.
    /// ScriptableObject that can be customized per AI type or game mode.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAISubsystemConfig", menuName = "Laboratory/EnemyAI/Subsystem Config")]
    public class EnemyAISubsystemConfig : ScriptableObject
    {
        #region Movement Settings
        
        [Header("Movement Configuration")]
        [Tooltip("Base movement speed for AI")]
        [SerializeField, Range(0.5f, 10f)] 
        private float _movementSpeed = 3.5f;
        
        [Tooltip("Movement speed during chase")]
        [SerializeField, Range(1f, 15f)] 
        private float _chaseSpeed = 6f;
        
        [Tooltip("Movement speed when fleeing")]
        [SerializeField, Range(1f, 20f)] 
        private float _fleeSpeed = 8f;
        
        [Tooltip("Rotation speed for turning")]
        [SerializeField, Range(1f, 10f)] 
        private float _rotationSpeed = 5f;
        
        #endregion
        
        #region Detection Settings
        
        [Header("Detection Configuration")]
        [Tooltip("Maximum detection range")]
        [SerializeField, Range(5f, 50f)] 
        private float _detectionRange = 15f;
        
        [Tooltip("Maximum attack range")]
        [SerializeField, Range(1f, 10f)] 
        private float _attackRange = 3f;
        
        [Tooltip("Field of view angle for detection")]
        [SerializeField, Range(30f, 360f)] 
        private float _fieldOfView = 120f;
        
        [Tooltip("Time to search for lost target")]
        [SerializeField, Range(2f, 30f)] 
        private float _searchDuration = 10f;
        
        #endregion
        
        #region Combat Settings
        
        [Header("Combat Configuration")]
        [Tooltip("Base attack damage")]
        [SerializeField, Range(5f, 100f)] 
        private float _attackDamage = 20f;
        
        [Tooltip("Time between attacks")]
        [SerializeField, Range(0.5f, 5f)] 
        private float _attackCooldown = 2f;
        
        [Tooltip("Health percentage that triggers fleeing")]
        [SerializeField, Range(0.1f, 0.8f)] 
        private float _fleeHealthThreshold = 0.25f;
        
        [Tooltip("Enable critical hit chances")]
        [SerializeField] 
        private bool _enableCriticalHits = false;
        
        [Tooltip("Critical hit chance (0-1)")]
        [SerializeField, Range(0f, 1f)] 
        private float _criticalHitChance = 0.1f;
        
        #endregion
        
        #region AI Behavior Settings
        
        [Header("AI Behavior Configuration")]
        [Tooltip("AI aggression level")]
        [SerializeField, Range(0f, 2f)] 
        private float _aggressionLevel = 1f;
        
        [Tooltip("AI reaction time delay")]
        [SerializeField, Range(0.1f, 2f)] 
        private float _reactionTime = 0.5f;
        
        [Tooltip("How often AI updates per second")]
        [SerializeField, Range(1f, 30f)] 
        private float _updateFrequency = 10f;
        
        [Tooltip("Enable group coordination")]
        [SerializeField] 
        private bool _enableGroupCoordination = true;
        
        [Tooltip("Maximum coordination distance")]
        [SerializeField, Range(5f, 30f)] 
        private float _coordinationRange = 15f;
        
        #endregion
        
        #region Patrol Settings
        
        [Header("Patrol Configuration")]
        [Tooltip("Default patrol wait time at each point")]
        [SerializeField, Range(0.5f, 10f)] 
        private float _patrolWaitTime = 2f;
        
        [Tooltip("Use random patrol order")]
        [SerializeField] 
        private bool _randomPatrolOrder = false;
        
        [Tooltip("Return to patrol after losing target")]
        [SerializeField] 
        private bool _returnToPatrolAfterCombat = true;
        
        [Tooltip("Time before returning to patrol")]
        [SerializeField, Range(5f, 60f)] 
        private float _returnToPatrolDelay = 15f;
        
        #endregion
        
        #region Audio Settings
        
        [Header("Audio Configuration")]
        [Tooltip("Enable AI audio clips")]
        [SerializeField] 
        private bool _enableAudio = true;
        
        [Tooltip("Audio volume")]
        [SerializeField, Range(0f, 1f)] 
        private float _audioVolume = 0.7f;
        
        [Tooltip("Detection sound clip")]
        [SerializeField] 
        private AudioClip _detectionSound;
        
        [Tooltip("Attack sound clip")]
        [SerializeField] 
        private AudioClip _attackSound;
        
        [Tooltip("Death sound clip")]
        [SerializeField] 
        private AudioClip _deathSound;
        
        #endregion
        
        #region Visual Settings
        
        [Header("Visual Configuration")]
        [Tooltip("Enable visual indicators")]
        [SerializeField] 
        private bool _enableVisualIndicators = true;
        
        [Tooltip("Detection indicator color")]
        [SerializeField] 
        private Color _detectionColor = Color.yellow;
        
        [Tooltip("Combat indicator color")]
        [SerializeField] 
        private Color _combatColor = Color.red;
        
        [Tooltip("Patrol indicator color")]
        [SerializeField] 
        private Color _patrolColor = Color.blue;
        
        #endregion
        
        #region Debug Settings
        
        [Header("Debug Configuration")]
        [Tooltip("Enable debug gizmos")]
        [SerializeField] 
        private bool _enableDebugGizmos = false;
        
        [Tooltip("Enable detailed logging")]
        [SerializeField] 
        private bool _enableDebugLogging = false;
        
        [Tooltip("Show AI state in inspector")]
        [SerializeField] 
        private bool _showStateInInspector = true;
        
        #endregion
        
        #region Properties
        
        // Movement Properties
        public float MovementSpeed => _movementSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float FleeSpeed => _fleeSpeed;
        public float RotationSpeed => _rotationSpeed;
        
        // Detection Properties
        public float DetectionRange => _detectionRange;
        public float AttackRange => _attackRange;
        public float FieldOfView => _fieldOfView;
        public float SearchDuration => _searchDuration;
        
        // Combat Properties
        public float AttackDamage => _attackDamage;
        public float AttackCooldown => _attackCooldown;
        public float FleeHealthThreshold => _fleeHealthThreshold;
        public bool EnableCriticalHits => _enableCriticalHits;
        public float CriticalHitChance => _criticalHitChance;
        
        // AI Behavior Properties
        public float AggressionLevel => _aggressionLevel;
        public float ReactionTime => _reactionTime;
        public float UpdateFrequency => _updateFrequency;
        public bool EnableGroupCoordination => _enableGroupCoordination;
        public float CoordinationRange => _coordinationRange;
        
        // Patrol Properties
        public float PatrolWaitTime => _patrolWaitTime;
        public bool RandomPatrolOrder => _randomPatrolOrder;
        public bool ReturnToPatrolAfterCombat => _returnToPatrolAfterCombat;
        public float ReturnToPatrolDelay => _returnToPatrolDelay;
        
        // Audio Properties
        public bool EnableAudio => _enableAudio;
        public float AudioVolume => _audioVolume;
        public AudioClip DetectionSound => _detectionSound;
        public AudioClip AttackSound => _attackSound;
        public AudioClip DeathSound => _deathSound;
        
        // Visual Properties
        public bool EnableVisualIndicators => _enableVisualIndicators;
        public Color DetectionColor => _detectionColor;
        public Color CombatColor => _combatColor;
        public Color PatrolColor => _patrolColor;
        
        // Debug Properties
        public bool EnableDebugGizmos => _enableDebugGizmos;
        public bool EnableDebugLogging => _enableDebugLogging;
        public bool ShowStateInInspector => _showStateInInspector;
        
        #endregion
        
        #region Validation
        
        private void OnValidate()
        {
            // Ensure movement speeds are logical
            _movementSpeed = Mathf.Max(0.5f, _movementSpeed);
            _chaseSpeed = Mathf.Max(_movementSpeed, _chaseSpeed);
            _fleeSpeed = Mathf.Max(_movementSpeed, _fleeSpeed);
            _rotationSpeed = Mathf.Max(1f, _rotationSpeed);
            
            // Ensure detection ranges are logical
            _detectionRange = Mathf.Max(5f, _detectionRange);
            _attackRange = Mathf.Min(_detectionRange, Mathf.Max(1f, _attackRange));
            _fieldOfView = Mathf.Clamp(_fieldOfView, 30f, 360f);
            _searchDuration = Mathf.Max(2f, _searchDuration);
            
            // Ensure combat values are reasonable
            _attackDamage = Mathf.Max(1f, _attackDamage);
            _attackCooldown = Mathf.Max(0.1f, _attackCooldown);
            _fleeHealthThreshold = Mathf.Clamp(_fleeHealthThreshold, 0.1f, 0.8f);
            _criticalHitChance = Mathf.Clamp01(_criticalHitChance);
            
            // Ensure behavior values are reasonable
            _aggressionLevel = Mathf.Max(0f, _aggressionLevel);
            _reactionTime = Mathf.Max(0.1f, _reactionTime);
            _updateFrequency = Mathf.Clamp(_updateFrequency, 1f, 30f);
            _coordinationRange = Mathf.Max(5f, _coordinationRange);
            
            // Ensure patrol values are reasonable
            _patrolWaitTime = Mathf.Max(0.1f, _patrolWaitTime);
            _returnToPatrolDelay = Mathf.Max(1f, _returnToPatrolDelay);
            
            // Ensure audio values are reasonable
            _audioVolume = Mathf.Clamp01(_audioVolume);
        }
        
        #endregion
        
        #region Presets
        
        [ContextMenu("Reset to Default")]
        public void ResetToDefault()
        {
            _movementSpeed = 3.5f;
            _chaseSpeed = 6f;
            _fleeSpeed = 8f;
            _rotationSpeed = 5f;
            
            _detectionRange = 15f;
            _attackRange = 3f;
            _fieldOfView = 120f;
            _searchDuration = 10f;
            
            _attackDamage = 20f;
            _attackCooldown = 2f;
            _fleeHealthThreshold = 0.25f;
            _enableCriticalHits = false;
            _criticalHitChance = 0.1f;
            
            _aggressionLevel = 1f;
            _reactionTime = 0.5f;
            _updateFrequency = 10f;
            _enableGroupCoordination = true;
            _coordinationRange = 15f;
            
            _patrolWaitTime = 2f;
            _randomPatrolOrder = false;
            _returnToPatrolAfterCombat = true;
            _returnToPatrolDelay = 15f;
            
            _enableAudio = true;
            _audioVolume = 0.7f;
            
            _enableVisualIndicators = true;
            _detectionColor = Color.yellow;
            _combatColor = Color.red;
            _patrolColor = Color.blue;
            
            _enableDebugGizmos = false;
            _enableDebugLogging = false;
            _showStateInInspector = true;
        }
        
        [ContextMenu("Set Aggressive AI")]
        public void SetAggressiveAI()
        {
            ResetToDefault();
            
            _chaseSpeed = 8f;
            _detectionRange = 20f;
            _attackRange = 4f;
            _attackDamage = 30f;
            _attackCooldown = 1.5f;
            _fleeHealthThreshold = 0.1f;
            _aggressionLevel = 2f;
            _reactionTime = 0.3f;
        }
        
        [ContextMenu("Set Defensive AI")]
        public void SetDefensiveAI()
        {
            ResetToDefault();
            
            _chaseSpeed = 4f;
            _detectionRange = 12f;
            _attackRange = 2.5f;
            _attackDamage = 15f;
            _attackCooldown = 2.5f;
            _fleeHealthThreshold = 0.4f;
            _aggressionLevel = 0.7f;
            _reactionTime = 0.7f;
        }
        
        [ContextMenu("Set Boss AI")]
        public void SetBossAI()
        {
            ResetToDefault();
            
            _movementSpeed = 2f;
            _chaseSpeed = 4f;
            _detectionRange = 25f;
            _attackRange = 5f;
            _attackDamage = 50f;
            _attackCooldown = 3f;
            _fleeHealthThreshold = 0.05f;
            _enableCriticalHits = true;
            _criticalHitChance = 0.15f;
            _aggressionLevel = 1.8f;
            _reactionTime = 0.4f;
            _enableGroupCoordination = false;
        }
        
        [ContextMenu("Set Coward AI")]
        public void SetCowardAI()
        {
            ResetToDefault();
            
            _fleeSpeed = 12f;
            _detectionRange = 18f;
            _attackRange = 2f;
            _attackDamage = 10f;
            _fleeHealthThreshold = 0.6f;
            _aggressionLevel = 0.3f;
            _reactionTime = 0.2f;
            _returnToPatrolAfterCombat = true;
            _returnToPatrolDelay = 5f;
        }
        
        #endregion
        
        #region Utility Methods
        
        /// <summary>
        /// Creates a runtime copy of this configuration for modification.
        /// </summary>
        public EnemyAISubsystemConfig CreateRuntimeCopy()
        {
            var copy = CreateInstance<EnemyAISubsystemConfig>();
            copy.CopyFrom(this);
            return copy;
        }
        
        /// <summary>
        /// Copies all values from another configuration.
        /// </summary>
        public void CopyFrom(EnemyAISubsystemConfig other)
        {
            _movementSpeed = other._movementSpeed;
            _chaseSpeed = other._chaseSpeed;
            _fleeSpeed = other._fleeSpeed;
            _rotationSpeed = other._rotationSpeed;
            
            _detectionRange = other._detectionRange;
            _attackRange = other._attackRange;
            _fieldOfView = other._fieldOfView;
            _searchDuration = other._searchDuration;
            
            _attackDamage = other._attackDamage;
            _attackCooldown = other._attackCooldown;
            _fleeHealthThreshold = other._fleeHealthThreshold;
            _enableCriticalHits = other._enableCriticalHits;
            _criticalHitChance = other._criticalHitChance;
            
            _aggressionLevel = other._aggressionLevel;
            _reactionTime = other._reactionTime;
            _updateFrequency = other._updateFrequency;
            _enableGroupCoordination = other._enableGroupCoordination;
            _coordinationRange = other._coordinationRange;
            
            _patrolWaitTime = other._patrolWaitTime;
            _randomPatrolOrder = other._randomPatrolOrder;
            _returnToPatrolAfterCombat = other._returnToPatrolAfterCombat;
            _returnToPatrolDelay = other._returnToPatrolDelay;
            
            _enableAudio = other._enableAudio;
            _audioVolume = other._audioVolume;
            _detectionSound = other._detectionSound;
            _attackSound = other._attackSound;
            _deathSound = other._deathSound;
            
            _enableVisualIndicators = other._enableVisualIndicators;
            _detectionColor = other._detectionColor;
            _combatColor = other._combatColor;
            _patrolColor = other._patrolColor;
            
            _enableDebugGizmos = other._enableDebugGizmos;
            _enableDebugLogging = other._enableDebugLogging;
            _showStateInInspector = other._showStateInInspector;
        }
        
        /// <summary>
        /// Calculates the update interval based on frequency.
        /// </summary>
        public float GetUpdateInterval()
        {
            return 1f / Mathf.Max(1f, _updateFrequency);
        }
        
        /// <summary>
        /// Determines if a critical hit should occur based on configured chance.
        /// </summary>
        public bool ShouldCriticalHit()
        {
            return _enableCriticalHits && UnityEngine.Random.value <= _criticalHitChance;
        }
        
        /// <summary>
        /// Gets the effective attack damage, including critical hits.
        /// </summary>
        public float GetEffectiveAttackDamage()
        {
            float damage = _attackDamage;
            
            if (ShouldCriticalHit())
            {
                damage *= 2f; // Critical hits do double damage
            }
            
            return damage;
        }
        
        #endregion
    }
}
