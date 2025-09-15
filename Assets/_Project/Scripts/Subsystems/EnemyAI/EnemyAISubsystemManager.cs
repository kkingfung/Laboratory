using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Health.Components;
using Laboratory.Core.NPC;
using Laboratory.Subsystems.EnemyAI.NPC;

namespace Laboratory.Subsystems.EnemyAI
{
    /// <summary>
    /// Unified Enemy/AI subsystem manager that coordinates all AI-related functionality.
    /// Consolidates NPC behavior, pathfinding, combat AI, and interaction systems.
    /// Provides intelligent enemy management with state-based behavior and dynamic targeting.
    /// 
    /// Version 3.0 - Enhanced architecture with comprehensive AI management
    /// </summary>
    public class EnemyAISubsystemManager : MonoBehaviour, IDisposable
    {
        #region Serialized Fields

        [Header("AI Configuration")]
        [SerializeField] private EnemyAISubsystemConfig _config;
        
        [Header("Core Components")]
        [SerializeField] private NPCBehavior _npcBehavior;
        [SerializeField] private NPCMovement _npcMovement;
        [SerializeField] private HealthComponentBase _healthComponent;
        [SerializeField] private NavMeshAgent _navMeshAgent;
        
        [Header("AI Settings")]
        [SerializeField] private AIPersonality _aiPersonality = AIPersonality.Balanced;
        [SerializeField] private float _detectionRange = 15f;
        [SerializeField] private float _attackRange = 3f;
        [SerializeField] private LayerMask _targetLayers = -1;
        
        [Header("Patrol Settings")]
        [SerializeField] private Transform[] _patrolPoints;
        [SerializeField] private float _patrolWaitTime = 2f;
        [SerializeField] private bool _randomPatrolOrder = false;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _showAIGizmos = false;

        #endregion

        #region Private Fields

        private IEventBus _eventBus;
        private EnemyAIState _currentState = EnemyAIState.Idle;
        private bool _isInitialized = false;
        
        // AI targets and tracking
        private GameObject _currentTarget;
        private GameObject _lastKnownTargetPosition;
        private float _lastTargetSeenTime = -1f;
        private readonly List<GameObject> _detectedTargets = new();
        
        // Patrol system
        private int _currentPatrolIndex = 0;
        private float _patrolStartTime = -1f;
        private bool _isWaitingAtPatrol = false;
        
        // Combat AI
        private float _lastAttackTime = -1f;
        private float _lastDamageTime = -1f;
        private Vector3 _fleeDirection = Vector3.zero;
        
        // Performance optimization
        private float _nextUpdateTime = 0f;
        private readonly float _updateInterval = 0.1f; // 10 FPS AI updates
        
        // Statistics
        private EnemyAIStatistics _statistics = new();

        #endregion

        #region Properties

        /// <summary>Current state of the AI subsystem</summary>
        public EnemyAIState CurrentState => _currentState;
        
        /// <summary>Whether the AI subsystem is fully initialized</summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>Current AI target</summary>
        public GameObject CurrentTarget => _currentTarget;
        
        /// <summary>Whether AI is currently alive</summary>
        public bool IsAlive => _healthComponent?.IsAlive ?? false;
        
        /// <summary>Current health percentage (0-1)</summary>
        public float HealthPercentage => _healthComponent?.HealthPercentage ?? 0f;
        
        /// <summary>Whether AI can see its current target</summary>
        public bool CanSeeTarget => _currentTarget != null && CanSeePosition(_currentTarget.transform.position);
        
        /// <summary>Distance to current target</summary>
        public float DistanceToTarget => _currentTarget != null ? 
            Vector3.Distance(transform.position, _currentTarget.transform.position) : float.MaxValue;
        
        /// <summary>AI configuration settings</summary>
        public EnemyAISubsystemConfig Configuration => _config;

        #endregion

        #region Events

        /// <summary>Fired when AI state changes</summary>
        public event Action<AIStateChangedEventArgs> OnStateChanged;
        
        /// <summary>Fired when AI detects a new target</summary>
        public event Action<AITargetDetectedEventArgs> OnTargetDetected;
        
        /// <summary>Fired when AI loses sight of target</summary>
        public event Action<AITargetLostEventArgs> OnTargetLost;
        
        /// <summary>Fired when AI starts attacking</summary>
        public event Action<AIAttackStartedEventArgs> OnAttackStarted;
        
        /// <summary>Fired when AI takes damage</summary>
        public event Action<AIDamagedEventArgs> OnDamaged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            InitializeAsync();
        }

        private void Update()
        {
            if (_isInitialized && Time.time >= _nextUpdateTime)
            {
                UpdateAISubsystem();
                _nextUpdateTime = Time.time + _updateInterval;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Initialization

        private void InitializeAsync()
        {
            try
            {
                LogDebug("Initializing Enemy AI Subsystem...");
                
                // Inject dependencies
                InjectDependencies();
                
                // Initialize core systems
                InitializeHealthSystem();
                InitializeNavigationSystem();
                InitializePatrolSystem();
                
                // Set initial state
                ChangeState(EnemyAIState.Patrolling);
                
                _isInitialized = true;
                LogDebug("Enemy AI Subsystem initialized successfully");
                
                // Notify other systems
                _eventBus?.Publish(new EnemyAISubsystemInitializedEvent(gameObject));
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize Enemy AI Subsystem: {ex.Message}");
            }
        }

        private void InjectDependencies()
        {
            // Get services from DI container
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance?.TryResolve<IEventBus>(out _eventBus);
            }

            LogDebug($"Dependencies injected - EventBus: {_eventBus != null}");
        }

        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (_npcBehavior == null)
                _npcBehavior = GetComponent<NPCBehavior>();
            
            if (_npcMovement == null)
                _npcMovement = GetComponent<NPCMovement>();
                
            if (_healthComponent == null)
                _healthComponent = GetComponent<HealthComponentBase>();
            
            if (_navMeshAgent == null)
                _navMeshAgent = GetComponent<NavMeshAgent>();

            // Initialize statistics
            _statistics = new EnemyAIStatistics
            {
                InitializationTime = Time.time,
                TotalTargetsDetected = 0,
                TotalAttacks = 0,
                TotalDamageTaken = 0,
                StateChangeCount = 0
            };
        }

        private void InitializeHealthSystem()
        {
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged += OnHealthChanged;
                _healthComponent.OnDeath += OnAIDeath;
                LogDebug("Health system initialized");
            }
        }

        private void InitializeNavigationSystem()
        {
            if (_navMeshAgent != null)
            {
                var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
                _navMeshAgent.speed = config.MovementSpeed;
                _navMeshAgent.stoppingDistance = _attackRange * 0.8f; // Stop slightly before attack range
                LogDebug("Navigation system initialized");
            }
        }

        private void InitializePatrolSystem()
        {
            if (_patrolPoints != null && _patrolPoints.Length > 0)
            {
                // Start at the first patrol point
                _currentPatrolIndex = 0;
                if (_randomPatrolOrder)
                {
                    _currentPatrolIndex = UnityEngine.Random.Range(0, _patrolPoints.Length);
                }
                LogDebug($"Patrol system initialized with {_patrolPoints.Length} points");
            }
        }

        #endregion

        #region AI Update System

        private void UpdateAISubsystem()
        {
            if (!IsAlive) return;

            UpdateTargetDetection();
            UpdateStateMachine();
            UpdateMovement();
            UpdateStatistics();
        }

        private void UpdateTargetDetection()
        {
            // Find targets in detection range
            var colliders = Physics.OverlapSphere(transform.position, _detectionRange, _targetLayers);
            _detectedTargets.Clear();

            foreach (var collider in colliders)
            {
                if (IsValidTarget(collider.gameObject))
                {
                    _detectedTargets.Add(collider.gameObject);
                }
            }

            // Update current target
            GameObject bestTarget = FindBestTarget(_detectedTargets);
            
            if (bestTarget != _currentTarget)
            {
                if (_currentTarget != null)
                {
                    OnTargetLost?.Invoke(new AITargetLostEventArgs(_currentTarget, gameObject));
                    _eventBus?.Publish(new AITargetLostEvent(_currentTarget, gameObject));
                }

                _currentTarget = bestTarget;
                
                if (_currentTarget != null)
                {
                    _lastTargetSeenTime = Time.time;
                    _statistics.TotalTargetsDetected++;
                    
                    OnTargetDetected?.Invoke(new AITargetDetectedEventArgs(_currentTarget, gameObject));
                    _eventBus?.Publish(new AITargetDetectedEvent(_currentTarget, gameObject));
                    LogDebug($"New target detected: {_currentTarget.name}");
                }
            }

            // Update last seen time if we can still see the current target
            if (_currentTarget != null && CanSeeTarget)
            {
                _lastTargetSeenTime = Time.time;
                _lastKnownTargetPosition = _currentTarget;
            }
        }

        private void UpdateStateMachine()
        {
            var newState = DetermineNextState();
            if (newState != _currentState)
            {
                ChangeState(newState);
            }
        }

        private EnemyAIState DetermineNextState()
        {
            if (!IsAlive)
                return EnemyAIState.Dead;

            // Check for flee conditions
            if (ShouldFlee())
                return EnemyAIState.Fleeing;

            // Check for combat conditions
            if (_currentTarget != null)
            {
                float distanceToTarget = DistanceToTarget;
                
                if (distanceToTarget <= _attackRange && CanSeeTarget)
                {
                    return EnemyAIState.Attacking;
                }
                else if (CanSeeTarget || (Time.time - _lastTargetSeenTime < GetConfig().SearchDuration))
                {
                    return EnemyAIState.Chasing;
                }
                else
                {
                    return EnemyAIState.Searching;
                }
            }

            // Default to patrolling if no targets
            return EnemyAIState.Patrolling;
        }

        private void UpdateMovement()
        {
            if (_navMeshAgent == null) return;

            switch (_currentState)
            {
                case EnemyAIState.Patrolling:
                    UpdatePatrolMovement();
                    break;
                
                case EnemyAIState.Chasing:
                    UpdateChaseMovement();
                    break;
                
                case EnemyAIState.Searching:
                    UpdateSearchMovement();
                    break;
                
                case EnemyAIState.Fleeing:
                    UpdateFleeMovement();
                    break;
                
                case EnemyAIState.Attacking:
                    UpdateAttackMovement();
                    break;
            }
        }

        private void UpdateStatistics()
        {
            _statistics.TotalTimeAlive = Time.time - _statistics.InitializationTime;
        }

        #endregion

        #region State Management

        private void ChangeState(EnemyAIState newState)
        {
            var oldState = _currentState;
            _currentState = newState;
            
            // Handle state transitions
            OnStateExit(oldState);
            OnStateEnter(newState);
            
            // Update statistics
            _statistics.StateChangeCount++;
            
            // Fire events
            var eventArgs = new AIStateChangedEventArgs(oldState, newState, gameObject);
            OnStateChanged?.Invoke(eventArgs);
            
            _eventBus?.Publish(new AIStateChangedEvent(oldState, newState, gameObject));
            
            LogDebug($"AI state changed: {oldState} → {newState}");
        }

        private void OnStateExit(EnemyAIState state)
        {
            switch (state)
            {
                case EnemyAIState.Attacking:
                    // Reset attack timing
                    break;
                
                case EnemyAIState.Patrolling:
                    _isWaitingAtPatrol = false;
                    break;
            }
        }

        private void OnStateEnter(EnemyAIState state)
        {
            switch (state)
            {
                case EnemyAIState.Patrolling:
                    StartPatrolling();
                    break;
                
                case EnemyAIState.Chasing:
                    StartChasing();
                    break;
                
                case EnemyAIState.Searching:
                    StartSearching();
                    break;
                
                case EnemyAIState.Attacking:
                    StartAttacking();
                    break;
                
                case EnemyAIState.Fleeing:
                    StartFleeing();
                    break;
                
                case EnemyAIState.Dead:
                    OnAIStateDeath();
                    break;
            }
        }

        #endregion

        #region State Implementations

        private void StartPatrolling()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;
            
            var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
            _navMeshAgent.speed = config.MovementSpeed;
            MoveToNextPatrolPoint();
        }

        private void UpdatePatrolMovement()
        {
            if (_patrolPoints == null || _patrolPoints.Length == 0) return;

            if (!_isWaitingAtPatrol && _navMeshAgent.remainingDistance < 0.5f)
            {
                _isWaitingAtPatrol = true;
                _patrolStartTime = Time.time;
            }

            if (_isWaitingAtPatrol && Time.time - _patrolStartTime >= _patrolWaitTime)
            {
                MoveToNextPatrolPoint();
                _isWaitingAtPatrol = false;
            }
        }

        private void MoveToNextPatrolPoint()
        {
            if (_randomPatrolOrder)
            {
                _currentPatrolIndex = UnityEngine.Random.Range(0, _patrolPoints.Length);
            }
            else
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _patrolPoints.Length;
            }

            if (_patrolPoints[_currentPatrolIndex] != null)
            {
                _navMeshAgent.SetDestination(_patrolPoints[_currentPatrolIndex].position);
            }
        }

        private void StartChasing()
        {
            var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
            _navMeshAgent.speed = config.ChaseSpeed;
        }

        private void UpdateChaseMovement()
        {
            if (_currentTarget != null)
            {
                _navMeshAgent.SetDestination(_currentTarget.transform.position);
            }
        }

        private void StartSearching()
        {
            var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
            _navMeshAgent.speed = config.MovementSpeed;
            
            if (_lastKnownTargetPosition != null)
            {
                _navMeshAgent.SetDestination(_lastKnownTargetPosition.transform.position);
            }
        }

        private void UpdateSearchMovement()
        {
            // Search around last known position
            if (_navMeshAgent.remainingDistance < 1f)
            {
                Vector3 searchPoint = GetRandomSearchPoint();
                _navMeshAgent.SetDestination(searchPoint);
            }
        }

        private void StartAttacking()
        {
            _navMeshAgent.speed = 0f; // Stop moving when attacking
            
            if (CanAttack())
            {
                PerformAttack();
            }
        }

        private void UpdateAttackMovement()
        {
            if (_currentTarget == null) return;

            // Face the target
            Vector3 directionToTarget = (_currentTarget.transform.position - transform.position).normalized;
            directionToTarget.y = 0; // Keep rotation on Y axis only
            
            if (directionToTarget != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, 
                    Quaternion.LookRotation(directionToTarget), Time.deltaTime * 5f);
            }

            // Try to attack if cooldown is ready
            if (CanAttack())
            {
                PerformAttack();
            }
        }

        private void StartFleeing()
        {
            var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
            _navMeshAgent.speed = config.FleeSpeed;
            
            CalculateFleeDirection();
        }

        private void UpdateFleeMovement()
        {
            Vector3 fleePoint = transform.position + _fleeDirection * 10f;
            _navMeshAgent.SetDestination(fleePoint);
        }

        #endregion

        #region Combat System

        private bool CanAttack()
        {
            var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
            return Time.time - _lastAttackTime >= config.AttackCooldown;
        }

        private void PerformAttack()
        {
            if (_currentTarget == null) return;

            _lastAttackTime = Time.time;
            _statistics.TotalAttacks++;

            // Get target health component
            var targetHealth = _currentTarget.GetComponent<HealthComponentBase>();
            if (targetHealth != null)
            {
                var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
                var damageRequest = new Laboratory.Core.Health.DamageRequest
                {
                    Amount = config.AttackDamage,
                    Source = gameObject,
                    Type = Laboratory.Core.Health.DamageType.Normal,
                    Direction = (_currentTarget.transform.position - transform.position).normalized
                };

                targetHealth.TakeDamage(damageRequest);
            }

            // Fire attack events
            OnAttackStarted?.Invoke(new AIAttackStartedEventArgs(_currentTarget, gameObject));
            _eventBus?.Publish(new AIAttackStartedEvent(_currentTarget, gameObject));

            LogDebug($"AI attacked {_currentTarget.name}");
        }

        private bool ShouldFlee()
        {
            var config = _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
            
            // Check health threshold
            if (HealthPercentage <= config.FleeHealthThreshold)
                return true;

            // Check if recently damaged and personality supports fleeing
            if (_aiPersonality == AIPersonality.Coward && Time.time - _lastDamageTime < 3f)
                return true;

            return false;
        }

        private void CalculateFleeDirection()
        {
            if (_currentTarget != null)
            {
                _fleeDirection = (transform.position - _currentTarget.transform.position).normalized;
            }
            else
            {
                _fleeDirection = -transform.forward;
            }
        }

        #endregion

        #region Target Management

        private bool IsValidTarget(GameObject target)
        {
            if (target == gameObject) return false;
            if (!target.activeInHierarchy) return false;

            // Check if target has health component and is alive
            var healthComponent = target.GetComponent<HealthComponentBase>();
            if (healthComponent == null || !healthComponent.IsAlive) return false;

            // Additional validation based on AI configuration
            return true;
        }

        private GameObject FindBestTarget(List<GameObject> targets)
        {
            if (targets.Count == 0) return null;
            if (targets.Count == 1) return targets[0];

            GameObject bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var target in targets)
            {
                float score = CalculateTargetScore(target);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        private float CalculateTargetScore(GameObject target)
        {
            float score = 0f;

            // Distance factor (closer is better)
            float distance = Vector3.Distance(transform.position, target.transform.position);
            score += 100f / (1f + distance);

            // Line of sight bonus
            if (CanSeePosition(target.transform.position))
            {
                score += 50f;
            }

            // Health factor (lower health targets are easier)
            var targetHealth = target.GetComponent<HealthComponentBase>();
            if (targetHealth != null)
            {
                score += (1f - targetHealth.HealthPercentage) * 25f;
            }

            return score;
        }

        #endregion

        #region Utility Methods

        private bool CanSeePosition(Vector3 position)
        {
            Vector3 directionToTarget = (position - transform.position).normalized;
            
            if (Physics.Raycast(transform.position, directionToTarget, out RaycastHit hit, _detectionRange))
            {
                return hit.collider.transform.position == position;
            }

            return false;
        }

        private Vector3 GetRandomSearchPoint()
        {
            Vector3 basePosition = _lastKnownTargetPosition?.transform.position ?? transform.position;
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * 5f;
            randomDirection.y = 0;
            
            return basePosition + randomDirection;
        }

        private EnemyAISubsystemConfig GetConfig()
        {
            return _config ?? ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets comprehensive AI subsystem status.
        /// </summary>
        public EnemyAISubsystemStatus GetStatus()
        {
            return new EnemyAISubsystemStatus
            {
                IsInitialized = _isInitialized,
                CurrentState = _currentState,
                IsAlive = IsAlive,
                HealthPercentage = HealthPercentage,
                CurrentTarget = _currentTarget,
                CanSeeTarget = CanSeeTarget,
                DistanceToTarget = DistanceToTarget,
                DetectedTargetsCount = _detectedTargets.Count,
                Statistics = _statistics
            };
        }

        /// <summary>
        /// Forces AI to target a specific GameObject.
        /// </summary>
        public void ForceTarget(GameObject target)
        {
            _currentTarget = target;
            if (target != null)
            {
                _lastTargetSeenTime = Time.time;
                ChangeState(EnemyAIState.Chasing);
            }
        }

        /// <summary>
        /// Clears the current target and returns to patrolling.
        /// </summary>
        public void ClearTarget()
        {
            _currentTarget = null;
            ChangeState(EnemyAIState.Patrolling);
        }

        /// <summary>
        /// Sets new patrol points for the AI.
        /// </summary>
        public void SetPatrolPoints(Transform[] newPatrolPoints)
        {
            _patrolPoints = newPatrolPoints;
            _currentPatrolIndex = 0;
            
            if (_currentState == EnemyAIState.Patrolling)
            {
                StartPatrolling();
            }
        }

        #endregion

        #region Event Handlers

        private void OnHealthChanged(Laboratory.Core.Health.HealthChangedEventArgs args)
        {
            if (args.NewHealth < args.OldHealth)
            {
                _lastDamageTime = Time.time;
                _statistics.TotalDamageTaken += (args.OldHealth - args.NewHealth);
                
                var eventArgs = new AIDamagedEventArgs(args.OldHealth - args.NewHealth, args.Source as GameObject, gameObject);
                OnDamaged?.Invoke(eventArgs);
            }
        }

        private void OnAIDeath(Laboratory.Core.Health.DeathEventArgs args)
        {
            LogDebug("AI died");
            ChangeState(EnemyAIState.Dead);
        }

        private void OnAIStateDeath()
        {
            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = false;
            }
            
            _detectedTargets.Clear();
            _currentTarget = null;
        }

        #endregion

        #region Helper Methods

        private void ValidateConfiguration()
        {
            if (_config == null)
            {
                LogWarning("No configuration assigned, using default values");
                _config = ScriptableObject.CreateInstance<EnemyAISubsystemConfig>();
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[EnemyAI] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[EnemyAI] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[EnemyAI] {message}");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!_showAIGizmos) return;

            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);
            
            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = CanSeeTarget ? Color.green : Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
            }
            
            // Draw patrol points
            if (_patrolPoints != null)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < _patrolPoints.Length; i++)
                {
                    if (_patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(_patrolPoints[i].position, 0.5f);
                        
                        // Draw patrol route
                        int nextIndex = (i + 1) % _patrolPoints.Length;
                        if (_patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
                        }
                    }
                }
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // Unsubscribe from health events
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged -= OnHealthChanged;
                _healthComponent.OnDeath -= OnAIDeath;
            }
            
            // Clear tracked targets
            _detectedTargets.Clear();
            _currentTarget = null;
            
            // Clear event handlers
            OnStateChanged = null;
            OnTargetDetected = null;
            OnTargetLost = null;
            OnAttackStarted = null;
            OnDamaged = null;
            
            _isInitialized = false;
            LogDebug("Enemy AI Subsystem disposed");
        }

        #endregion
    }

    #region Data Structures and Enums

    /// <summary>Enemy AI states</summary>
    public enum EnemyAIState
    {
        Idle,
        Patrolling,
        Chasing,
        Attacking,
        Searching,
        Fleeing,
        Dead
    }

    /// <summary>AI personality types</summary>
    public enum AIPersonality
    {
        Aggressive,  // Always attacks, never retreats
        Balanced,    // Standard behavior
        Defensive,   // Prefers to stay back and defend
        Coward,      // Retreats quickly when damaged
        Berserker    // Attacks wildly, ignores self-preservation
    }

    /// <summary>AI statistics tracking</summary>
    [System.Serializable]
    public struct EnemyAIStatistics
    {
        public float InitializationTime;
        public float TotalTimeAlive;
        public int TotalTargetsDetected;
        public int TotalAttacks;
        public int TotalDamageTaken;
        public int StateChangeCount;
    }

    /// <summary>AI subsystem status</summary>
    [System.Serializable]
    public struct EnemyAISubsystemStatus
    {
        public bool IsInitialized;
        public EnemyAIState CurrentState;
        public bool IsAlive;
        public float HealthPercentage;
        public GameObject CurrentTarget;
        public bool CanSeeTarget;
        public float DistanceToTarget;
        public int DetectedTargetsCount;
        public EnemyAIStatistics Statistics;
    }

    #endregion

    #region Event Classes

    /// <summary>AI state changed event arguments</summary>
    public class AIStateChangedEventArgs : EventArgs
    {
        public EnemyAIState OldState { get; }
        public EnemyAIState NewState { get; }
        public GameObject AI { get; }

        public AIStateChangedEventArgs(EnemyAIState oldState, EnemyAIState newState, GameObject ai)
        {
            OldState = oldState;
            NewState = newState;
            AI = ai;
        }
    }

    /// <summary>AI target detected event arguments</summary>
    public class AITargetDetectedEventArgs : EventArgs
    {
        public GameObject Target { get; }
        public GameObject AI { get; }

        public AITargetDetectedEventArgs(GameObject target, GameObject ai)
        {
            Target = target;
            AI = ai;
        }
    }

    /// <summary>AI target lost event arguments</summary>
    public class AITargetLostEventArgs : EventArgs
    {
        public GameObject Target { get; }
        public GameObject AI { get; }

        public AITargetLostEventArgs(GameObject target, GameObject ai)
        {
            Target = target;
            AI = ai;
        }
    }

    /// <summary>AI attack started event arguments</summary>
    public class AIAttackStartedEventArgs : EventArgs
    {
        public GameObject Target { get; }
        public GameObject AI { get; }

        public AIAttackStartedEventArgs(GameObject target, GameObject ai)
        {
            Target = target;
            AI = ai;
        }
    }

    /// <summary>AI damaged event arguments</summary>
    public class AIDamagedEventArgs : EventArgs
    {
        public float Damage { get; }
        public GameObject Source { get; }
        public GameObject AI { get; }

        public AIDamagedEventArgs(float damage, GameObject source, GameObject ai)
        {
            Damage = damage;
            Source = source;
            AI = ai;
        }
    }

    #region Global Events

    /// <summary>Global event: Enemy AI subsystem initialized</summary>
    public class EnemyAISubsystemInitializedEvent
    {
        public GameObject AI { get; }

        public EnemyAISubsystemInitializedEvent(GameObject ai)
        {
            AI = ai;
        }
    }

    /// <summary>Global event: AI state changed</summary>
    public class AIStateChangedEvent
    {
        public EnemyAIState OldState { get; }
        public EnemyAIState NewState { get; }
        public GameObject AI { get; }

        public AIStateChangedEvent(EnemyAIState oldState, EnemyAIState newState, GameObject ai)
        {
            OldState = oldState;
            NewState = newState;
            AI = ai;
        }
    }

    /// <summary>Global event: AI detected target</summary>
    public class AITargetDetectedEvent
    {
        public GameObject Target { get; }
        public GameObject AI { get; }

        public AITargetDetectedEvent(GameObject target, GameObject ai)
        {
            Target = target;
            AI = ai;
        }
    }

    /// <summary>Global event: AI lost target</summary>
    public class AITargetLostEvent
    {
        public GameObject Target { get; }
        public GameObject AI { get; }

        public AITargetLostEvent(GameObject target, GameObject ai)
        {
            Target = target;
            AI = ai;
        }
    }

    /// <summary>Global event: AI started attack</summary>
    public class AIAttackStartedEvent
    {
        public GameObject Target { get; }
        public GameObject AI { get; }

        public AIAttackStartedEvent(GameObject target, GameObject ai)
        {
            Target = target;
            AI = ai;
        }
    }

    #endregion

    #endregion
}