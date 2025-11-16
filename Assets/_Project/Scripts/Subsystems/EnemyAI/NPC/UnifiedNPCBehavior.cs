using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Events;
using Laboratory.Core.Health;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Character;
using DamageType = Laboratory.Core.Enums.DamageType;

namespace Laboratory.Subsystems.EnemyAI.NPC
{
    /// <summary>
    /// Unified NPC Behavior System combining the best features from NPCBehavior and EnhancedNPCBehavior.
    /// Provides a comprehensive AI system with state management, behavior trees, and difficulty scaling.
    /// </summary>
    public class UnifiedNPCBehavior : MonoBehaviour
    {
        #region Enums and Data Structures

        public enum NPCType
        {
            Civilian,   // Basic NPC with simple behaviors
            Guard,      // Patrols and attacks intruders  
            Hunter,     // Actively seeks and chases targets
            Defender,   // Protects a specific area
            Scout,      // Fast moving, alerts others
            Merchant,   // Trading and quest-focused
            Hostile     // Always aggressive
        }

        public enum NPCDifficulty
        {
            Easy,       // Slower, weaker, less perceptive
            Normal,     // Balanced stats
            Hard,       // Faster, stronger, more perceptive
            Expert,     // Highly optimized, advanced behaviors
            Nightmare   // Maximum challenge
        }

        public enum NPCState
        {
            Idle,
            Patrolling,
            Talking,
            Chasing,
            Attacking,
            Searching,
            Fleeing,
            Panicking,
            Alert,
            Dead,
            Thankful,
            Hostile
        }

        public enum NPCPersonality
        {
            Coward,    // Always runs away
            Fighter,   // Sometimes fights back
            Neutral,   // Mixed reactions
            Brave,     // Rarely runs, might help others
            Aggressive // Always hostile
        }

        [System.Serializable]
        public class NPCQuest
        {
            public string questName;
            public string description;
            public bool isCompleted;
            public string thankYouMessage = "Thank you for helping me!";
            public GameObject rewardItem;
            public int rewardAmount = 1;
        }

        [System.Serializable]
        public class NPCStats
        {
            [Header("Movement")]
            public float walkSpeed = 3f;
            public float runSpeed = 6f;
            public float fleeSpeed = 8f;
            
            [Header("Combat")]
            public float attackDamage = 25f;
            public float attackRange = 3f;
            public float attackCooldown = 2f;
            
            [Header("Detection")]
            public float detectionRange = 15f;
            public float hearingRange = 10f;
            public float fieldOfViewAngle = 90f;
            
            [Header("Behavior")]
            public float panicDuration = 3f;
            public float fleeDuration = 5f;
            public float alertDuration = 10f;
            public float searchDuration = 8f;
        }

        [System.Serializable]
        public class DifficultyScaling
        {
            public float healthMultiplier = 1f;
            public float damageMultiplier = 1f;
            public float speedMultiplier = 1f;
            public float detectionMultiplier = 1f;
            public float reactionTimeMultiplier = 1f;
        }



        #endregion

        #region Fields

        [Header("NPC Configuration")]
        [SerializeField] private string _npcName = "NPC";
        [SerializeField] private NPCType _npcType = NPCType.Civilian;
        [SerializeField] private NPCPersonality _personality = NPCPersonality.Neutral;
        [SerializeField] private NPCDifficulty _difficulty = NPCDifficulty.Normal;

        [Header("Stats")]
        [SerializeField] private NPCStats _baseStats = new NPCStats();

        [Header("Quest System")]
        [SerializeField] private NPCQuest _quest;
        [SerializeField] private int _playerReputation = 0;
        [SerializeField] private int _minReputationForQuest = -5;

        [Header("Patrol System")]
        [SerializeField] private List<Transform> _patrolPoints = new List<Transform>();
        [SerializeField] private float _waitTimeAtPatrolPoint = 3f;
        [SerializeField] private bool _randomPatrol = false;

        [Header("Audio")]
        [SerializeField] private AudioClip[] _screamSounds;
        [SerializeField] private AudioClip[] _thankfulSounds;
        [SerializeField] private AudioClip[] _greetingSounds;
        [SerializeField] private AudioClip[] _combatSounds;

        [Header("Visual Feedback")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _alertColor = Color.yellow;
        [SerializeField] private Color _hostileColor = Color.red;
        [SerializeField] private Color _thankfulColor = Color.green;
        [SerializeField] private Color _deadColor = Color.gray;

        [Header("References")]
        [SerializeField] private Transform _detectionCenter;
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private LayerMask _obstacleLayers = 1;

        // Components
        private HealthComponentBase _healthComponent;
        private Rigidbody2D _rigidbody2D;
        private Rigidbody _rigidbody;
        private SpriteRenderer _spriteRenderer;
        private Renderer _renderer;
        private AudioSource _audioSource;
        private Animator _animator;
        private UnifiedTargetSelector _targetSelector;

        // Services
        private ServiceContainer _services;
        private IEventBus _eventBus;

        // Runtime state
        private NPCState _currentState = NPCState.Idle;
        private NPCState _previousState;
        private Transform _currentTarget;
        private Vector3 _lastKnownTargetPosition;
        private Vector3 _originalPosition;
        private float _stateTimer;
        private float _lastAttackTime;
        private int _currentPatrolIndex;
        private bool _isAlert;
        private bool _hasBeenAttacked;
        private bool _hasBeenHelped;

        // Difficulty scaling
        private NPCStats _scaledStats;
        private Dictionary<NPCDifficulty, DifficultyScaling> _difficultyScaling;

        // Performance optimization
        private float _nextPerceptionUpdate;
        private float _perceptionUpdateInterval = 0.2f;
        private bool _isInitialized = false;

        // Event delegates for proper unsubscription
        private System.Action<Laboratory.Core.Health.DeathEventArgs> _onDeathAction;
        private System.Action<Laboratory.Core.Health.HealthChangedEventArgs> _onHealthChangedAction;

        #endregion

        #region Properties

        public string NPCName => _npcName;
        public NPCType Type => _npcType;
        public NPCPersonality Personality => _personality;
        public NPCDifficulty Difficulty => _difficulty;
        public NPCState CurrentState => _currentState;
        public Transform CurrentTarget => _currentTarget;
        public bool IsAlert => _isAlert;
        public bool HasBeenAttacked => _hasBeenAttacked;
        public bool HasBeenHelped => _hasBeenHelped;
        public int PlayerReputation => _playerReputation;
        public NPCQuest Quest => _quest;
        public NPCStats Stats => _scaledStats ?? _baseStats;
        
        // Additional properties for compatibility with NPCBehaviorStates
        public List<Transform> PatrolPoints => _patrolPoints;
        public float MovementSpeed => (_scaledStats ?? _baseStats).walkSpeed;
        public float WaitTimeAtPatrolPoint => _waitTimeAtPatrolPoint;
        public Vector3 LastKnownTargetPosition => _lastKnownTargetPosition;
        public Transform DetectionCenter => _detectionCenter;
        public float DetectionRange => (_scaledStats ?? _baseStats).detectionRange;
        public LayerMask TargetLayers => _targetLayers;
        public LayerMask ObstacleLayers => _obstacleLayers;
        
        // Patrol helper methods
        public Vector3 GetNextPatrolPoint()
        {
            if (_patrolPoints.Count == 0) return transform.position;
            
            if (_randomPatrol)
            {
                _currentPatrolIndex = UnityEngine.Random.Range(0, _patrolPoints.Count);
            }
            else
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
            }
            
            return _patrolPoints[_currentPatrolIndex].position;
        }

        #endregion

        #region Events

        public event Action<NPCState, NPCState> OnStateChanged;
        public event Action<Transform> OnTargetChanged;
        public event Action<NPCQuest> OnQuestCompleted;
        public event Action<int> OnReputationChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            InitializeDifficultyScaling();
            ApplyDifficultyScaling();
        }

        private void Start()
        {
            InitializeNPC();

            // Try to auto-resolve services if not manually initialized
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                Initialize(serviceContainer);
            }
        }

        private void Update()
        {
            if (!_isInitialized) return;

            UpdateStateTimer();
            UpdatePerception();
            UpdateStateMachine();
            UpdateAnimations();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the NPC with service container
        /// </summary>
        public void Initialize(ServiceContainer services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));

            _eventBus = _services.ResolveService<IEventBus>();
            if (_eventBus != null)
            {
                Debug.Log($"[{_npcName}] Event bus resolved successfully");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Called when the NPC is attacked
        /// </summary>
        public void OnAttacked(Transform attacker, float damage = 0f)
        {
            if (_hasBeenAttacked && _currentState == NPCState.Dead) return;

            _hasBeenAttacked = true;
            _currentTarget = attacker;
            _lastKnownTargetPosition = attacker.position;

            PlayRandomSound(_screamSounds);
            ReactToAttack(attacker, damage);
            DecreaseReputation(2);

            // Publish event
            _eventBus?.Publish(new NPCAttackedEvent(this, attacker, damage));
        }

        /// <summary>
        /// Called when the NPC is helped by someone
        /// </summary>
        public void OnHelped(Transform helper)
        {
            if (_hasBeenAttacked || _currentState == NPCState.Dead) return;

            _hasBeenHelped = true;
            ChangeState(NPCState.Thankful);
            PlayRandomSound(_thankfulSounds);
            IncreaseReputation(1);
            
            ShowMessage("Thank you for helping me!");

            // Publish event
            _eventBus?.Publish(new NPCHelpedEvent(this, helper));
        }

        /// <summary>
        /// Starts a conversation with the NPC
        /// </summary>
        public void StartConversation(Transform talker)
        {
            if (!CanInteract()) return;

            ChangeState(NPCState.Talking);
            _currentTarget = talker;
            PlayRandomSound(_greetingSounds);

            if (_quest != null && !_quest.isCompleted && CanOfferQuest())
            {
                ShowQuestDialog();
            }
            else if (_hasBeenHelped)
            {
                ShowMessage("Thanks again for your help!");
            }
            else if (_playerReputation < 0)
            {
                ShowMessage("I don't trust you...");
            }
            else
            {
                ShowMessage($"Hello! I'm {_npcName}.");
            }

            // Publish event
            _eventBus?.Publish(new NPCConversationStartedEvent(this, talker));
        }

        /// <summary>
        /// Completes the NPC's quest
        /// </summary>
        public void CompleteQuest()
        {
            if (_quest == null || _quest.isCompleted) return;

            _quest.isCompleted = true;
            ChangeState(NPCState.Thankful);
            PlayRandomSound(_thankfulSounds);
            IncreaseReputation(3);

            ShowMessage(_quest.thankYouMessage);

            // Give reward
            if (_quest.rewardItem != null)
            {
                for (int i = 0; i < _quest.rewardAmount; i++)
                {
                    Vector3 spawnPos = transform.position + Vector3.up + UnityEngine.Random.insideUnitSphere * 0.5f;
                    Instantiate(_quest.rewardItem, spawnPos, Quaternion.identity);
                }
            }

            OnQuestCompleted?.Invoke(_quest);
            // Note: NPCQuestCompletedEvent expects original NPCQuest type
            // For now, we'll skip the event or create a separate event type for UnifiedNPCBehavior
            // _eventBus?.Publish(new NPCQuestCompletedEvent(this, _quest));
        }

        /// <summary>
        /// Sets the NPC difficulty and applies scaling
        /// </summary>
        public void SetDifficulty(NPCDifficulty newDifficulty)
        {
            _difficulty = newDifficulty;
            ApplyDifficultyScaling();
        }

        /// <summary>
        /// Adds a patrol point
        /// </summary>
        public void AddPatrolPoint(Transform point)
        {
            if (point != null && !_patrolPoints.Contains(point))
            {
                _patrolPoints.Add(point);
            }
        }

        /// <summary>
        /// Sets the NPC's target
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (_currentTarget == target) return;

            _currentTarget = target;
            if (target != null)
            {
                _lastKnownTargetPosition = target.position;
                if (!_isAlert) TriggerAlert();
            }

            OnTargetChanged?.Invoke(_currentTarget);
        }

        /// <summary>
        /// Checks if the NPC can interact
        /// </summary>
        public bool CanInteract()
        {
            return _currentState != NPCState.Fleeing && 
                   _currentState != NPCState.Panicking && 
                   _currentState != NPCState.Attacking &&
                   _currentState != NPCState.Dead;
        }

        /// <summary>
        /// Checks if the NPC can offer a quest
        /// </summary>
        public bool CanOfferQuest()
        {
            return _quest != null && !_quest.isCompleted && _playerReputation >= _minReputationForQuest;
        }
        
        /// <summary>
        /// Attempts to perform an attack on the current target
        /// </summary>
        /// <returns>True if attack was successfully performed</returns>
        public bool TryAttack()
        {
            if (_currentTarget == null) return false;
            if (Time.time - _lastAttackTime < _scaledStats.attackCooldown) return false;
            
            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
            if (distanceToTarget > _scaledStats.attackRange) return false;
            
            PerformAttack();
            return true;
        }
        
        /// <summary>
        /// Clears the alert state for this NPC
        /// </summary>
        public void ClearAlert()
        {
            _isAlert = false;
            UpdateVisualState();
        }

        #endregion

        #region Private Methods

        private void InitializeComponents()
        {
            // Get components
            _healthComponent = GetComponent<HealthComponentBase>();
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _rigidbody = GetComponent<Rigidbody>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _renderer = GetComponent<Renderer>();
            _audioSource = GetComponent<AudioSource>();
            _animator = GetComponent<Animator>();
            _targetSelector = GetComponent<UnifiedTargetSelector>();

            // Set detection center
            if (_detectionCenter == null)
                _detectionCenter = transform;

            // Subscribe to health events
            if (_healthComponent != null)
            {
                _onDeathAction = OnNPCDeathWithArgs;
                _onHealthChangedAction = OnHealthChangedWithArgs;
                _healthComponent.OnDeath += _onDeathAction;
                _healthComponent.OnHealthChanged += _onHealthChangedAction;
            }

            // Create audio source if missing
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f; // 3D sound
            }
        }

        private void InitializeDifficultyScaling()
        {
            _difficultyScaling = new Dictionary<NPCDifficulty, DifficultyScaling>
            {
                [NPCDifficulty.Easy] = new DifficultyScaling
                {
                    healthMultiplier = 0.7f,
                    damageMultiplier = 0.6f,
                    speedMultiplier = 0.8f,
                    detectionMultiplier = 0.7f,
                    reactionTimeMultiplier = 1.5f
                },
                [NPCDifficulty.Normal] = new DifficultyScaling
                {
                    healthMultiplier = 1f,
                    damageMultiplier = 1f,
                    speedMultiplier = 1f,
                    detectionMultiplier = 1f,
                    reactionTimeMultiplier = 1f
                },
                [NPCDifficulty.Hard] = new DifficultyScaling
                {
                    healthMultiplier = 1.5f,
                    damageMultiplier = 1.3f,
                    speedMultiplier = 1.2f,
                    detectionMultiplier = 1.3f,
                    reactionTimeMultiplier = 0.8f
                },
                [NPCDifficulty.Expert] = new DifficultyScaling
                {
                    healthMultiplier = 2f,
                    damageMultiplier = 1.6f,
                    speedMultiplier = 1.4f,
                    detectionMultiplier = 1.6f,
                    reactionTimeMultiplier = 0.6f
                },
                [NPCDifficulty.Nightmare] = new DifficultyScaling
                {
                    healthMultiplier = 3f,
                    damageMultiplier = 2f,
                    speedMultiplier = 1.6f,
                    detectionMultiplier = 2f,
                    reactionTimeMultiplier = 0.4f
                }
            };
        }

        private void ApplyDifficultyScaling()
        {
            if (!_difficultyScaling.TryGetValue(_difficulty, out var scaling)) return;

            _scaledStats = new NPCStats
            {
                walkSpeed = _baseStats.walkSpeed * scaling.speedMultiplier,
                runSpeed = _baseStats.runSpeed * scaling.speedMultiplier,
                fleeSpeed = _baseStats.fleeSpeed * scaling.speedMultiplier,
                attackDamage = _baseStats.attackDamage * scaling.damageMultiplier,
                attackRange = _baseStats.attackRange,
                attackCooldown = _baseStats.attackCooldown / scaling.reactionTimeMultiplier,
                detectionRange = _baseStats.detectionRange * scaling.detectionMultiplier,
                hearingRange = _baseStats.hearingRange * scaling.detectionMultiplier,
                fieldOfViewAngle = _baseStats.fieldOfViewAngle,
                panicDuration = _baseStats.panicDuration,
                fleeDuration = _baseStats.fleeDuration,
                alertDuration = _baseStats.alertDuration,
                searchDuration = _baseStats.searchDuration
            };

            // Apply health scaling
            if (_healthComponent != null)
            {
                int newMaxHealth = Mathf.RoundToInt(_healthComponent.MaxHealth * scaling.healthMultiplier);
                // Note: This would require a SetMaxHealth method on the health component
            }

            // Update perception interval
            _perceptionUpdateInterval = 0.2f / scaling.reactionTimeMultiplier;
        }

        private void InitializeNPC()
        {
            _originalPosition = transform.position;
            ChangeState(NPCState.Idle);
            ChangeColor(_normalColor);

            // Note: UnifiedNPCBehavior is designed to replace NPCBehavior
            // If integration with NPCManager is needed, consider updating NPCManager to support UnifiedNPCBehavior
        }

        private void UpdateStateTimer()
        {
            if (_stateTimer > 0)
            {
                _stateTimer -= Time.deltaTime;
                if (_stateTimer <= 0)
                {
                    OnStateTimerExpired();
                }
            }
        }

        private void UpdatePerception()
        {
            if (Time.time < _nextPerceptionUpdate) return;
            _nextPerceptionUpdate = Time.time + _perceptionUpdateInterval;

            // Use target selector if available, otherwise do manual detection
            if (_targetSelector != null)
            {
                _targetSelector.ForceTargetUpdate();
                var detectedTarget = _targetSelector.CurrentTarget;
                
                if (detectedTarget != _currentTarget)
                {
                    SetTarget(detectedTarget);
                }
            }
            else
            {
                PerformManualTargetDetection();
            }
        }

        private void PerformManualTargetDetection()
        {
            // Simple sphere detection fallback
            Collider[] hits = Physics.OverlapSphere(_detectionCenter.position, _scaledStats.detectionRange, _targetLayers);
            
            Transform closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var hit in hits)
            {
                if (hit.transform == transform) continue;

                float distance = Vector3.Distance(transform.position, hit.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = hit.transform;
                }
            }

            SetTarget(closestTarget);
        }

        private void UpdateStateMachine()
        {
            switch (_currentState)
            {
                case NPCState.Idle:
                    HandleIdleState();
                    break;
                case NPCState.Patrolling:
                    HandlePatrollingState();
                    break;
                case NPCState.Chasing:
                    HandleChasingState();
                    break;
                case NPCState.Attacking:
                    HandleAttackingState();
                    break;
                case NPCState.Searching:
                    HandleSearchingState();
                    break;
                case NPCState.Fleeing:
                    HandleFleeingState();
                    break;
                case NPCState.Panicking:
                    HandlePanicState();
                    break;
                case NPCState.Alert:
                    HandleAlertState();
                    break;
                case NPCState.Talking:
                    HandleTalkingState();
                    break;
                case NPCState.Thankful:
                    HandleThankfulState();
                    break;
                case NPCState.Hostile:
                    HandleHostileState();
                    break;
                case NPCState.Dead:
                    // Do nothing, NPC is dead
                    break;
            }

            // State transitions
            CheckStateTransitions();
        }

        private void CheckStateTransitions()
        {
            // Dead state overrides everything
            if (_healthComponent != null && !_healthComponent.IsAlive && _currentState != NPCState.Dead)
            {
                ChangeState(NPCState.Dead);
                return;
            }

            // Handle target-based transitions
            if (_currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.position);
                
                // Attack if close enough and hostile
                if (ShouldAttack() && distanceToTarget <= _scaledStats.attackRange)
                {
                    ChangeState(NPCState.Attacking);
                }
                // Chase if target detected and should chase
                else if (ShouldChase() && distanceToTarget <= _scaledStats.detectionRange)
                {
                    ChangeState(NPCState.Chasing);
                }
            }
            else if (_currentState == NPCState.Chasing || _currentState == NPCState.Attacking)
            {
                // Lost target, start searching
                if (_lastKnownTargetPosition != Vector3.zero)
                {
                    ChangeState(NPCState.Searching);
                    _stateTimer = _scaledStats.searchDuration;
                }
                else
                {
                    ReturnToDefaultState();
                }
            }
        }

        private bool ShouldAttack()
        {
            if (_currentTarget == null) return false;
            if (Time.time - _lastAttackTime < _scaledStats.attackCooldown) return false;

            switch (_npcType)
            {
                case NPCType.Guard:
                case NPCType.Hunter:
                case NPCType.Defender:
                case NPCType.Hostile:
                    return true;
                case NPCType.Civilian:
                    return _personality == NPCPersonality.Fighter && _hasBeenAttacked;
                case NPCType.Scout:
                    return _difficulty >= NPCDifficulty.Hard;
                default:
                    return false;
            }
        }

        private bool ShouldChase()
        {
            if (_currentTarget == null) return false;

            switch (_npcType)
            {
                case NPCType.Guard:
                case NPCType.Hunter:
                case NPCType.Hostile:
                    return true;
                case NPCType.Defender:
                    // Only chase if close to original position
                    return Vector3.Distance(transform.position, _originalPosition) < _scaledStats.detectionRange;
                case NPCType.Scout:
                    return true; // Scouts always investigate
                case NPCType.Civilian:
                    return _personality == NPCPersonality.Brave && _hasBeenAttacked;
                default:
                    return false;
            }
        }

        private void HandleIdleState()
        {
            // Return to original position if too far away
            if (Vector3.Distance(transform.position, _originalPosition) > 1f)
            {
                MoveTowards(_originalPosition, _scaledStats.walkSpeed);
            }
            else
            {
                StopMovement();
                
                // Start patrolling if we have patrol points
                if (_patrolPoints.Count > 0 && _npcType != NPCType.Civilian)
                {
                    ChangeState(NPCState.Patrolling);
                }
            }
        }

        private void HandlePatrollingState()
        {
            if (_patrolPoints.Count == 0)
            {
                ChangeState(NPCState.Idle);
                return;
            }

            Vector3 targetPoint = _patrolPoints[_currentPatrolIndex].position;
            float distanceToPoint = Vector3.Distance(transform.position, targetPoint);

            if (distanceToPoint <= 0.5f)
            {
                // Reached patrol point
                if (_stateTimer <= 0)
                {
                    _stateTimer = _waitTimeAtPatrolPoint;
                    StopMovement();
                }
                else if (_stateTimer <= 0)
                {
                    // Move to next patrol point
                    if (_randomPatrol)
                    {
                        _currentPatrolIndex = UnityEngine.Random.Range(0, _patrolPoints.Count);
                    }
                    else
                    {
                        _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Count;
                    }
                }
            }
            else
            {
                MoveTowards(targetPoint, _scaledStats.walkSpeed);
            }
        }

        private void HandleChasingState()
        {
            if (_currentTarget == null)
            {
                ChangeState(NPCState.Searching);
                return;
            }

            MoveTowards(_currentTarget.position, _scaledStats.runSpeed);
            _lastKnownTargetPosition = _currentTarget.position;
        }

        private void HandleAttackingState()
        {
            StopMovement();
            
            if (_currentTarget != null)
            {
                FaceTarget(_currentTarget.position);
                
                if (Time.time - _lastAttackTime >= _scaledStats.attackCooldown)
                {
                    PerformAttack();
                    _lastAttackTime = Time.time;
                }
            }
        }

        private void HandleSearchingState()
        {
            if (_lastKnownTargetPosition != Vector3.zero)
            {
                MoveTowards(_lastKnownTargetPosition, _scaledStats.walkSpeed);
                
                if (Vector3.Distance(transform.position, _lastKnownTargetPosition) <= 2f)
                {
                    _lastKnownTargetPosition = Vector3.zero;
                }
            }
            else
            {
                // Random search movement
                if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
                {
                    Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
                    randomDirection.y = 0;
                    MoveInDirection(randomDirection.normalized, _scaledStats.walkSpeed);
                }
            }
        }

        private void HandleFleeingState()
        {
            if (_currentTarget != null)
            {
                Vector3 fleeDirection = (transform.position - _currentTarget.position).normalized;
                MoveInDirection(fleeDirection, _scaledStats.fleeSpeed);
            }
        }

        private void HandlePanicState()
        {
            // Random panic movement
            if (UnityEngine.Random.Range(0f, 1f) < 0.2f)
            {
                Vector3 randomDirection = UnityEngine.Random.insideUnitSphere;
                randomDirection.y = 0;
                MoveInDirection(randomDirection.normalized, _scaledStats.fleeSpeed * 0.7f);
            }
        }

        private void HandleAlertState()
        {
            StopMovement();
            
            // Look around for threats
            if (UnityEngine.Random.Range(0f, 1f) < 0.1f)
            {
                Vector3 lookDirection = UnityEngine.Random.insideUnitSphere;
                lookDirection.y = 0;
                FaceDirection(lookDirection.normalized);
            }
        }

        private void HandleTalkingState()
        {
            StopMovement();
            if (_currentTarget != null)
            {
                FaceTarget(_currentTarget.position);
            }
        }

        private void HandleThankfulState()
        {
            StopMovement();
            // Could add some happy animation or behavior here
        }

        private void HandleHostileState()
        {
            // Similar to chasing but more aggressive
            if (_currentTarget != null)
            {
                MoveTowards(_currentTarget.position, _scaledStats.runSpeed * 1.2f);
            }
            else
            {
                // Aggressively search for targets
                PerformManualTargetDetection();
            }
        }

        private void ReactToAttack(Transform attacker, float damage)
        {
            switch (_personality)
            {
                case NPCPersonality.Coward:
                    ChangeState(NPCState.Fleeing);
                    _stateTimer = _scaledStats.fleeDuration;
                    break;
                case NPCPersonality.Fighter:
                    if (UnityEngine.Random.Range(0f, 1f) < 0.6f)
                        ChangeState(NPCState.Hostile);
                    else
                        ChangeState(NPCState.Fleeing);
                    break;
                case NPCPersonality.Neutral:
                    if (UnityEngine.Random.Range(0f, 1f) < 0.7f)
                        ChangeState(NPCState.Fleeing);
                    else
                        ChangeState(NPCState.Panicking);
                    break;
                case NPCPersonality.Brave:
                    if (UnityEngine.Random.Range(0f, 1f) < 0.4f)
                        ChangeState(NPCState.Fleeing);
                    else
                        ChangeState(NPCState.Hostile);
                    break;
                case NPCPersonality.Aggressive:
                    ChangeState(NPCState.Hostile);
                    break;
            }

            // Update visual state
            UpdateVisualState();
        }

        private void PerformAttack()
        {
            if (_currentTarget == null) return;

            PlayRandomSound(_combatSounds);
            
            // Apply damage if target has health component
            var targetHealth = _currentTarget.GetComponent<HealthComponentBase>();
            if (targetHealth != null)
            {
                var damageRequest = new DamageRequest
                {
                    Amount = _scaledStats.attackDamage,
                    Source = gameObject,
                    Type = DamageType.Physical,
                    HitPoint = _currentTarget.position
                };
                
                targetHealth.TakeDamage(damageRequest);
            }

            // Trigger attack animation
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }
        }

        private void MoveTowards(Vector3 target, float speed)
        {
            Vector3 direction = (target - transform.position).normalized;
            MoveInDirection(direction, speed);
            FaceDirection(direction);
        }

        private void MoveInDirection(Vector3 direction, float speed)
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = new Vector3(direction.x * speed, _rigidbody.linearVelocity.y, direction.z * speed);
            }
            else if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = new Vector2(direction.x * speed, direction.z * speed);
            }
            else
            {
                transform.Translate(direction * speed * Time.deltaTime, Space.World);
            }
        }

        private void StopMovement()
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = new Vector3(0, _rigidbody.linearVelocity.y, 0);
            }
            else if (_rigidbody2D != null)
            {
                _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        private void FaceTarget(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            FaceDirection(direction);
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction == Vector3.zero) return;

            if (_rigidbody2D != null)
            {
                // 2D sprite flipping
                if (direction.x > 0)
                    transform.localScale = new Vector3(1, 1, 1);
                else if (direction.x < 0)
                    transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                // 3D rotation
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }

        private void ChangeState(NPCState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;
            _stateTimer = 0f;

            UpdateVisualState();
            OnStateChanged?.Invoke(_previousState, _currentState);

            // Publish state change event
            _eventBus?.Publish(new NPCStateChangedEvent(this, _previousState, _currentState));
        }

        private void UpdateVisualState()
        {
            Color targetColor = _currentState switch
            {
                NPCState.Alert => _alertColor,
                NPCState.Hostile => _hostileColor,
                NPCState.Attacking => _hostileColor,
                NPCState.Thankful => _thankfulColor,
                NPCState.Dead => _deadColor,
                _ => _normalColor
            };

            ChangeColor(targetColor);
        }

        private void ChangeColor(Color newColor)
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = newColor;
            else if (_renderer != null)
                _renderer.material.color = newColor;
        }

        private void UpdateAnimations()
        {
            if (_animator == null) return;

            // Update animation parameters
            bool isMoving = _rigidbody != null ? _rigidbody.linearVelocity.magnitude > 0.1f : 
                           _rigidbody2D != null ? _rigidbody2D.linearVelocity.magnitude > 0.1f : false;

            _animator.SetBool("IsMoving", isMoving);
            _animator.SetBool("IsAlert", _isAlert);
            _animator.SetBool("HasTarget", _currentTarget != null);
            _animator.SetInteger("State", (int)_currentState);

            float currentSpeed = isMoving ? (_rigidbody?.linearVelocity.magnitude ?? _rigidbody2D?.linearVelocity.magnitude ?? 0f) : 0f;
            _animator.SetFloat("MovementSpeed", currentSpeed);
        }

        private void TriggerAlert()
        {
            _isAlert = true;
            _stateTimer = _scaledStats.alertDuration;
        }

        private void ReturnToDefaultState()
        {
            if (_patrolPoints.Count > 0)
                ChangeState(NPCState.Patrolling);
            else
                ChangeState(NPCState.Idle);
        }

        private void OnStateTimerExpired()
        {
            switch (_currentState)
            {
                case NPCState.Fleeing:
                case NPCState.Panicking:
                case NPCState.Alert:
                case NPCState.Searching:
                    ReturnToDefaultState();
                    break;
                case NPCState.Talking:
                    ChangeState(NPCState.Idle);
                    break;
            }

            if (_isAlert && _stateTimer <= 0)
            {
                _isAlert = false;
                UpdateVisualState();
            }
        }

        private void IncreaseReputation(int amount = 1)
        {
            _playerReputation = Mathf.Clamp(_playerReputation + amount, -10, 10);
            OnReputationChanged?.Invoke(_playerReputation);
        }

        private void DecreaseReputation(int amount = 1)
        {
            _playerReputation = Mathf.Clamp(_playerReputation - amount, -10, 10);
            OnReputationChanged?.Invoke(_playerReputation);
        }

        private void ShowMessage(string message)
        {
            // Create a simple message above the NPC
            GameObject messageObj = new GameObject("NPCMessage");
            messageObj.transform.SetParent(transform);
            messageObj.transform.localPosition = Vector3.up * 2f;

            // Add text component
            var textMesh = messageObj.AddComponent<TextMesh>();
            textMesh.text = message;
            textMesh.fontSize = 20;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;

            // Auto-destroy after 3 seconds (handle edit mode properly)
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEngine.Object.DestroyImmediate(messageObj);
            }
            else
            {
                Destroy(messageObj, 3f);
            }
            #else
            Destroy(messageObj, 3f);
            #endif
        }

        private void ShowQuestDialog()
        {
            if (_quest == null) return;
            ShowMessage($"Quest: {_quest.questName}\n{_quest.description}");
        }

        private void OnNPCDeath()
        {
            ChangeState(NPCState.Dead);
            StopMovement();
            
            // Publish death event
            _eventBus?.Publish(new NPCDeathEvent(this));
        }

        private void OnHealthChanged(float oldHealth, float newHealth)
        {
            // React to health changes if needed
            if (newHealth < oldHealth && newHealth > 0)
            {
                // Just took damage but not dead
                TriggerAlert();
            }
        }

        private void Cleanup()
        {
            // Unsubscribe from health events
            if (_healthComponent != null && _onDeathAction != null && _onHealthChangedAction != null)
            {
                _healthComponent.OnDeath -= _onDeathAction;
                _healthComponent.OnHealthChanged -= _onHealthChangedAction;
            }
        }

        private void PlayRandomSound(AudioClip[] sounds)
        {
            if (_audioSource == null || sounds == null || sounds.Length == 0) return;

            AudioClip clip = sounds[UnityEngine.Random.Range(0, sounds.Length)];
            if (clip != null)
                _audioSource.PlayOneShot(clip);
        }

        private void OnNPCDeathWithArgs(Laboratory.Core.Health.DeathEventArgs args)
        {
            ChangeState(NPCState.Dead);
            enabled = false; // Disable update loop
        }

        private void OnHealthChangedWithArgs(Laboratory.Core.Health.HealthChangedEventArgs args)
        {
            // React to health changes
            if (args.NewHealth < args.OldHealth && !_isAlert)
            {
                TriggerAlert(); // Getting damaged makes NPC alert
            }
        }



        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Vector3 center = _detectionCenter ? _detectionCenter.position : transform.position;
            
            // Draw detection range
            Gizmos.color = _isAlert ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(center, _scaledStats?.detectionRange ?? _baseStats.detectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(center, _scaledStats?.attackRange ?? _baseStats.attackRange);

            // Draw patrol points and connections
            if (_patrolPoints.Count > 0)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < _patrolPoints.Count; i++)
                {
                    if (_patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(_patrolPoints[i].position, 0.5f);
                        
                        // Draw connections
                        int nextIndex = (i + 1) % _patrolPoints.Count;
                        if (_patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
                        }
                    }
                }

                // Highlight current patrol target
                if (_currentPatrolIndex < _patrolPoints.Count && _patrolPoints[_currentPatrolIndex] != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(_patrolPoints[_currentPatrolIndex].position, 0.7f);
                }
            }

            // Draw current target connection
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }

            // Draw last known position
            if (_lastKnownTargetPosition != Vector3.zero)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(_lastKnownTargetPosition, 0.3f);
            }

            // Draw original position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_originalPosition, Vector3.one * 0.5f);
        }

        #endregion
    }

    #region Event Classes

    public class NPCDeathEvent : EventArgs
    {
        public UnifiedNPCBehavior NPC { get; }

        public NPCDeathEvent(UnifiedNPCBehavior npc)
        {
            NPC = npc;
        }
    }

    public class NPCAttackedEvent : EventArgs
    {
        public UnifiedNPCBehavior NPC { get; }
        public Transform Attacker { get; }
        public float Damage { get; }

        public NPCAttackedEvent(UnifiedNPCBehavior npc, Transform attacker, float damage)
        {
            NPC = npc;
            Attacker = attacker;
            Damage = damage;
        }
    }

    public class NPCHelpedEvent : EventArgs
    {
        public UnifiedNPCBehavior NPC { get; }
        public Transform Helper { get; }

        public NPCHelpedEvent(UnifiedNPCBehavior npc, Transform helper)
        {
            NPC = npc;
            Helper = helper;
        }
    }

    public class NPCConversationStartedEvent : EventArgs
    {
        public UnifiedNPCBehavior NPC { get; }
        public Transform Talker { get; }

        public NPCConversationStartedEvent(UnifiedNPCBehavior npc, Transform talker)
        {
            NPC = npc;
            Talker = talker;
        }
    }

    public class NPCQuestCompletedEvent : EventArgs
    {
        public UnifiedNPCBehavior NPC { get; }
        public NPCQuest Quest { get; }

        public NPCQuestCompletedEvent(UnifiedNPCBehavior npc, NPCQuest quest)
        {
            NPC = npc;
            Quest = quest;
        }
    }

    public class NPCStateChangedEvent : EventArgs
    {
        public UnifiedNPCBehavior NPC { get; }
        public UnifiedNPCBehavior.NPCState PreviousState { get; }
        public UnifiedNPCBehavior.NPCState NewState { get; }

        public NPCStateChangedEvent(UnifiedNPCBehavior npc, UnifiedNPCBehavior.NPCState previousState, UnifiedNPCBehavior.NPCState newState)
        {
            NPC = npc;
            PreviousState = previousState;
            NewState = newState;
        }
    }

    #endregion
    
    #region Event Arguments Classes
    
    /// <summary>
    /// Event arguments for NPC death events
    /// </summary>
    public class DeathEventArgs : EventArgs
    {
        public float Damage { get; }
        public Transform Source { get; }
        public DateTime Timestamp { get; }
        
        public DeathEventArgs(float damage, Transform source)
        {
            Damage = damage;
            Source = source;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Event arguments for health changed events
    /// </summary>
    public class HealthChangedEventArgs : EventArgs
    {
        public float OldHealth { get; }
        public float NewHealth { get; }
        public float MaxHealth { get; }
        public DateTime Timestamp { get; }
        
        public HealthChangedEventArgs(float oldHealth, float newHealth, float maxHealth)
        {
            OldHealth = oldHealth;
            NewHealth = newHealth;
            MaxHealth = maxHealth;
            Timestamp = DateTime.Now;
        }
    }
    
    /// <summary>
    /// Difficulty scaling settings for NPCs
    /// </summary>
    [System.Serializable]
    public class NPCDifficultyScaling
    {
        public float HealthMultiplier { get; set; } = 1f;
        public float DamageMultiplier { get; set; } = 1f;
        public float SpeedMultiplier { get; set; } = 1f;
        public float DetectionRangeMultiplier { get; set; } = 1f;
        public float AttackSpeedMultiplier { get; set; } = 1f;
        public float PerceptionSpeedMultiplier { get; set; } = 1f;
        public float AccuracyMultiplier { get; set; } = 1f;
    }
    
    #endregion
}
