using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Events;
using Laboratory.Core.Health;
using Laboratory.Core.DI;

namespace Laboratory.EnemyAI
{
    /// <summary>
    /// Enhanced NPC Behavior System for Laboratory Unity Project
    /// Version 2.0 - Complete AI implementation with state machine and behavior tree integration
    /// </summary>
    public class EnhancedNPCBehavior : AIStateMachine
    {
        #region Fields

        [Header("NPC Configuration")]
        [SerializeField] private NPCType npcType = NPCType.Guard;
        [SerializeField] private NPCDifficulty difficulty = NPCDifficulty.Normal;
        [SerializeField] protected float detectionRange = 15f;
        [SerializeField] protected float attackRange = 3f;
        [SerializeField] protected float movementSpeed = 5f;
        [SerializeField] protected float patrolRadius = 10f;

        [Header("Combat Settings")]
        [SerializeField] private float attackDamage = 25f;
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] protected LayerMask targetLayers = -1;
        [SerializeField] protected LayerMask obstacleLayers = 1;

        [Header("Patrol Settings")]
        [SerializeField] protected List<Transform> patrolPoints = new();
        [SerializeField] protected float waitTimeAtPatrolPoint = 3f;
        [SerializeField] private bool randomPatrol = false;

        [Header("References")]
        [SerializeField] protected Transform detectionCenter;
        [SerializeField] private HealthComponentBase healthComponent;
        [SerializeField] private Animator animator;

        // Components and systems
        private BehaviorTree behaviorTree;
        private NPCPerceptionSystem perception;
        private NPCCombatSystem combatSystem;
        private NPCMovementSystem movementSystem;
        private NPCDifficultyScaler difficultyScaler;

        // State tracking
        private Transform currentTarget;
        protected Vector3 lastKnownTargetPosition;
        private float lastAttackTime;
        private int currentPatrolIndex;
        private bool isAlert;
        private float alertStartTime;

        // Performance optimization
        private float nextPerceptionUpdate;
        private float perceptionUpdateInterval = 0.2f;

        #endregion

        #region Properties

        public NPCType Type => npcType;
        public NPCDifficulty Difficulty => difficulty;
        public Transform CurrentTarget => currentTarget;
        public bool IsAlert => isAlert;
        public float AlertDuration => isAlert ? Time.time - alertStartTime : 0f;
        
        // Public accessors for state classes
        public List<Transform> PatrolPoints => patrolPoints;
        public float MovementSpeed => movementSpeed;
        public float AttackRange => attackRange;
        public float WaitTimeAtPatrolPoint => waitTimeAtPatrolPoint;
        public float DetectionRange => detectionRange;
        public Transform DetectionCenter => detectionCenter;
        public LayerMask TargetLayers => targetLayers;
        public LayerMask ObstacleLayers => obstacleLayers;
        public Vector3 LastKnownTargetPosition => lastKnownTargetPosition;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeNPCComponents();
        }

        protected override void Start()
        {
            base.Start();
            SetupBehaviorTree();
            ApplyDifficultyScaling();
        }

        protected override void Update()
        {
            base.Update();
            UpdatePerception();
            UpdateBehaviorTree();
            UpdateAnimations();
        }

        #endregion

        #region State Machine Implementation

        protected override void InitializeStates()
        {
            // Register all NPC states
            RegisterState(new IdleState());
            RegisterState(new PatrolState());
            RegisterState(new ChaseState());
            RegisterState(new AttackState());
            RegisterState(new SearchState());
            RegisterState(new AlertState());
            RegisterState(new FleeState());
            RegisterState(new DeadState());
        }

        protected override Type GetInitialStateType()
        {
            return patrolPoints.Count > 0 ? typeof(PatrolState) : typeof(IdleState);
        }

        protected override void SetupTransitions()
        {
            // Idle -> Patrol/Chase/Alert
            AddTransition<IdleState, PatrolState>(() => patrolPoints.Count > 0 && !isAlert);
            AddTransition<IdleState, ChaseState>(() => currentTarget != null && IsTargetInRange(detectionRange));
            AddTransition<IdleState, AlertState>(() => isAlert && currentTarget == null);

            // Patrol -> Chase/Alert/Search
            AddTransition<PatrolState, ChaseState>(() => currentTarget != null && IsTargetInRange(detectionRange));
            AddTransition<PatrolState, AlertState>(() => isAlert && currentTarget == null);
            AddTransition<PatrolState, SearchState>(() => lastKnownTargetPosition != Vector3.zero && currentTarget == null);

            // Chase -> Attack/Search/Patrol
            AddTransition<ChaseState, AttackState>(() => currentTarget != null && IsTargetInRange(attackRange));
            AddTransition<ChaseState, SearchState>(() => currentTarget == null && lastKnownTargetPosition != Vector3.zero);
            AddTransition<ChaseState, PatrolState>(() => currentTarget == null && !isAlert && patrolPoints.Count > 0);

            // Attack -> Chase/Search/Patrol
            AddTransition<AttackState, ChaseState>(() => currentTarget != null && !IsTargetInRange(attackRange));
            AddTransition<AttackState, SearchState>(() => currentTarget == null && lastKnownTargetPosition != Vector3.zero);
            AddTransition<AttackState, PatrolState>(() => currentTarget == null && !isAlert && patrolPoints.Count > 0);

            // Search -> Chase/Patrol/Idle
            AddTransition<SearchState, ChaseState>(() => currentTarget != null);
            AddTransition<SearchState, PatrolState>(() => patrolPoints.Count > 0);
            AddTransition<SearchState, IdleState>(() => patrolPoints.Count == 0);

            // Alert -> Chase/Search
            AddTransition<AlertState, ChaseState>(() => currentTarget != null);
            AddTransition<AlertState, SearchState>(() => lastKnownTargetPosition != Vector3.zero && !isAlert);

            // Any state -> Flee (if health is low and difficulty allows)
            AddFleeTransitions();

            // Any state -> Dead
            AddDeathTransitions();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets a new target for the NPC
        /// </summary>
        public void SetTarget(Transform target)
        {
            if (target != currentTarget)
            {
                currentTarget = target;
                if (target != null)
                {
                    lastKnownTargetPosition = target.position;
                    TriggerAlert();
                }
            }
        }

        /// <summary>
        /// Clears the current target
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
        }

        /// <summary>
        /// Triggers alert state
        /// </summary>
        public void TriggerAlert()
        {
            isAlert = true;
            alertStartTime = Time.time;
        }

        /// <summary>
        /// Clears alert state
        /// </summary>
        public void ClearAlert()
        {
            isAlert = false;
        }

        /// <summary>
        /// Sets the NPC difficulty and applies scaling
        /// </summary>
        public void SetDifficulty(NPCDifficulty newDifficulty)
        {
            difficulty = newDifficulty;
            ApplyDifficultyScaling();
        }

        /// <summary>
        /// Adds a patrol point
        /// </summary>
        public void AddPatrolPoint(Transform point)
        {
            if (point != null && !patrolPoints.Contains(point))
            {
                patrolPoints.Add(point);
            }
        }

        /// <summary>
        /// Gets the next patrol point
        /// </summary>
        public Vector3 GetNextPatrolPoint()
        {
            if (patrolPoints.Count == 0)
            {
                return transform.position;
            }

            if (randomPatrol)
            {
                currentPatrolIndex = UnityEngine.Random.Range(0, patrolPoints.Count);
            }
            else
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Count;
            }

            return patrolPoints[currentPatrolIndex].position;
        }

        /// <summary>
        /// Performs an attack on the current target
        /// </summary>
        public bool TryAttack()
        {
            if (currentTarget == null || Time.time - lastAttackTime < attackCooldown)
            {
                return false;
            }

            if (IsTargetInRange(attackRange))
            {
                PerformAttack();
                lastAttackTime = Time.time;
                return true;
            }

            return false;
        }

        #endregion

        #region Private Methods

        private void InitializeNPCComponents()
        {
            // Get or create components
            if (detectionCenter == null)
                detectionCenter = transform;

            if (healthComponent == null)
                healthComponent = GetComponent<HealthComponentBase>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            // Initialize subsystems
            perception = new NPCPerceptionSystem(this);
            combatSystem = new NPCCombatSystem(this);
            movementSystem = new NPCMovementSystem(this);
            difficultyScaler = new NPCDifficultyScaler();

            // Subscribe to health events
            if (healthComponent != null)
            {
                healthComponent.OnDeath += OnNPCDeath;
                healthComponent.OnHealthChanged += OnHealthChanged;
            }
        }

        private void SetupBehaviorTree()
        {
            behaviorTree = gameObject.AddComponent<BehaviorTree>();

            // Create behavior tree structure
            var rootSelector = new SelectorNode();

            // Combat behavior
            var combatSequence = new SequenceNode();
            combatSequence.AddChild(new BlackboardConditionNode("HasTarget", true));
            combatSequence.AddChild(new DistanceConditionNode("CurrentTarget", attackRange, true));
            combatSequence.AddChild(new AttackActionNode(this));

            // Chase behavior
            var chaseSequence = new SequenceNode();
            chaseSequence.AddChild(new BlackboardConditionNode("HasTarget", true));
            chaseSequence.AddChild(new MoveToTargetNode("CurrentTarget", movementSpeed));

            // Patrol behavior
            var patrolSequence = new SequenceNode();
            patrolSequence.AddChild(new BlackboardConditionNode("HasPatrolPoints", true));
            patrolSequence.AddChild(new PatrolActionNode(this));

            // Idle behavior (fallback)
            var idleAction = new WaitNode(1f);

            // Assemble tree
            rootSelector.AddChild(combatSequence);
            rootSelector.AddChild(chaseSequence);
            rootSelector.AddChild(patrolSequence);
            rootSelector.AddChild(idleAction);

            behaviorTree.SetRootNode(rootSelector);

            // Initialize blackboard
            UpdateBlackboard();
        }

        private void UpdatePerception()
        {
            if (Time.time < nextPerceptionUpdate) return;
            nextPerceptionUpdate = Time.time + perceptionUpdateInterval;

            perception.UpdatePerception();
        }

        private void UpdateBehaviorTree()
        {
            if (behaviorTree == null) return;

            UpdateBlackboard();
            behaviorTree.Execute();
        }

        private void UpdateBlackboard()
        {
            if (behaviorTree == null) return;

            behaviorTree.SetBlackboardValue("HasTarget", currentTarget != null);
            behaviorTree.SetBlackboardValue("CurrentTarget", currentTarget);
            behaviorTree.SetBlackboardValue("LastKnownPosition", lastKnownTargetPosition);
            behaviorTree.SetBlackboardValue("IsAlert", isAlert);
            behaviorTree.SetBlackboardValue("HasPatrolPoints", patrolPoints.Count > 0);
            behaviorTree.SetBlackboardValue("MovementSpeed", movementSpeed);
            behaviorTree.SetBlackboardValue("AttackRange", attackRange);
            behaviorTree.SetBlackboardValue("DetectionRange", detectionRange);
        }

        private void UpdateAnimations()
        {
            if (animator == null) return;

            // Update animation parameters based on current state
            animator.SetBool("IsMoving", movementSystem?.IsMoving ?? false);
            animator.SetBool("IsAlert", isAlert);
            animator.SetBool("HasTarget", currentTarget != null);
            animator.SetFloat("MovementSpeed", movementSystem?.CurrentSpeed ?? 0f);

            // Trigger attack animation
            if (combatSystem?.ShouldTriggerAttackAnimation() == true)
            {
                animator.SetTrigger("Attack");
            }
        }

        private void ApplyDifficultyScaling()
        {
            var scaling = difficultyScaler.GetScalingForDifficulty(difficulty);

            detectionRange *= scaling.DetectionRangeMultiplier;
            attackDamage *= scaling.DamageMultiplier;
            movementSpeed *= scaling.SpeedMultiplier;
            attackCooldown /= scaling.AttackSpeedMultiplier;
            perceptionUpdateInterval /= scaling.PerceptionSpeedMultiplier;

            if (healthComponent != null)
            {
                healthComponent.SetMaxHealth(Mathf.RoundToInt(healthComponent.MaxHealth * scaling.HealthMultiplier));
            }
        }

        private bool IsTargetInRange(float range)
        {
            return currentTarget != null && 
                   Vector3.Distance(detectionCenter.position, currentTarget.position) <= range;
        }

        private void PerformAttack()
        {
            combatSystem.PerformAttack(currentTarget, attackDamage);
        }

        private void AddFleeTransitions()
        {
            if (difficulty == NPCDifficulty.Easy || difficulty == NPCDifficulty.Normal)
            {
                // Add flee transitions for easier difficulties
                System.Func<bool> shouldFlee = () => 
                    healthComponent != null && 
                    healthComponent.HealthPercentage < 0.25f && 
                    currentTarget != null;

                AddTransition<ChaseState, FleeState>(shouldFlee);
                AddTransition<AttackState, FleeState>(shouldFlee);
            }
        }

        private void AddDeathTransitions()
        {
            System.Func<bool> isDead = () => healthComponent != null && !healthComponent.IsAlive;

            // Add death transitions from all states
            foreach (var stateType in GetAllStateTypes())
            {
                if (stateType != typeof(DeadState))
                {
                    var transitionMethod = typeof(EnhancedNPCBehavior)
                        .GetMethod(nameof(AddTransition))
                        ?.MakeGenericMethod(stateType, typeof(DeadState));
                    
                    transitionMethod?.Invoke(this, new object[] { isDead });
                }
            }
        }

        private void OnNPCDeath(DeathEventArgs args)
        {
            ForceChangeState<DeadState>();
            enabled = false; // Disable this component
        }

        private void OnHealthChanged(HealthChangedEventArgs args)
        {
            // React to health changes (could trigger alerts, flee behavior, etc.)
            if (args.NewHealth < args.OldHealth)
            {
                TriggerAlert(); // Getting damaged makes NPC alert
            }
        }

        #endregion

        #region Debug

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            if (!enableStateVisualization) return;

            // Draw detection range
            Gizmos.color = isAlert ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(detectionCenter ? detectionCenter.position : transform.position, detectionRange);

            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Draw patrol points and connections
            if (patrolPoints.Count > 0)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Count; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                        
                        // Draw connections
                        int nextIndex = (i + 1) % patrolPoints.Count;
                        if (patrolPoints[nextIndex] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[nextIndex].position);
                        }
                    }
                }
            }

            // Draw target connection
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }

            // Draw last known position
            if (lastKnownTargetPosition != Vector3.zero)
            {
                Gizmos.color = Color.orange;
                Gizmos.DrawWireSphere(lastKnownTargetPosition, 0.3f);
            }
        }

        #endregion
    }

    #region Enums and Data Structures

    /// <summary>
    /// Types of NPCs with different behaviors
    /// </summary>
    public enum NPCType
    {
        Guard,      // Patrols and attacks intruders
        Hunter,     // Actively seeks and chases targets
        Defender,   // Protects a specific area
        Scout,      // Fast moving, alerts others
        Berserker   // Aggressive, high damage
    }

    /// <summary>
    /// Difficulty levels affecting NPC capabilities
    /// </summary>
    public enum NPCDifficulty
    {
        Easy,       // Slower, weaker, less perceptive
        Normal,     // Balanced stats
        Hard,       // Faster, stronger, more perceptive
        Expert,     // Highly optimized, advanced behaviors
        Nightmare   // Maximum challenge
    }

    /// <summary>
    /// Scaling factors for different difficulties
    /// </summary>
    [System.Serializable]
    public struct NPCDifficultyScaling
    {
        public float HealthMultiplier;
        public float DamageMultiplier;
        public float SpeedMultiplier;
        public float DetectionRangeMultiplier;
        public float AttackSpeedMultiplier;
        public float PerceptionSpeedMultiplier;
        public float AccuracyMultiplier;
    }

    #endregion
}