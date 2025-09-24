using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Laboratory.AI.Pathfinding;
using Laboratory.AI.Agents;

namespace Laboratory.Subsystems.EnemyAI
{
    /// <summary>
    /// Enhanced enemy controller with advanced pathfinding and performance optimizations.
    /// </summary>
    [RequireComponent(typeof(EnhancedAIAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnhancedEnemyController : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private float health = 100f;
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float chaseSpeed = 6f;

        [Header("Detection")]
        [SerializeField] private float detectionRadius = 10f;
        [SerializeField] private float loseTargetRadius = 15f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private LayerMask targetLayers = 1;
        [SerializeField] private float fieldOfViewAngle = 90f;

        [Header("Enhanced Pathfinding")]
        [SerializeField] private PathfindingMode pathfindingMode = PathfindingMode.Hybrid;
        [SerializeField] private float pathUpdateInterval = 0.3f;
        [SerializeField] private float pathRecalculationDistance = 3f;
        [SerializeField] private bool enablePathOptimization = true;
        [SerializeField] private bool enableGroupBehavior = true;

        [Header("Combat")]
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float attackAnimationDuration = 0.8f;
        [SerializeField] private bool canAttackWhileMoving = false;

        [Header("Patrol")]
        [SerializeField] private bool enablePatrol = true;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float patrolWaitTime = 2f;
        [SerializeField] private bool randomPatrolOrder = false;

        [Header("Advanced Behavior")]
        [SerializeField] private float alertDuration = 5f;
        [SerializeField] private float searchDuration = 8f;

        // Core Components
        private EnhancedAIAgent aiAgent;
        private Animator animator;

        // Target and Detection
        private Transform currentTarget;
        private Vector3 lastKnownTargetPosition;
        private float lastTargetSeenTime;

        // State Management
        private EnemyState currentState = EnemyState.Idle;
        private float stateStartTime;

        // Combat
        private float lastAttackTime = 0f;
        private bool isAttacking = false;

        // Patrol
        private int currentPatrolIndex = 0;
        private float patrolWaitTimer = 0f;
        private bool waitingAtPatrolPoint = false;

        // Performance Optimization
        private float nextDetectionCheck = 0f;
        private const float DETECTION_INTERVAL = 0.15f;

        // Pathfinding optimization
        private float lastPathUpdateTime = 0f;
        private Vector3 lastPathTarget;
        private float lastPathOptimizationCheck = 0f;

        public enum EnemyState
        {
            Idle,
            Patrolling,
            Chasing,
            Attacking,
            Searching,
            Alert,
            Dead
        }

        // Events
        public System.Action<EnhancedEnemyController> OnDeath;
        public System.Action<EnhancedEnemyController, Transform> OnTargetDetected;
        public System.Action<EnhancedEnemyController> OnTargetLost;

        #region Properties

        public bool IsDead => health <= 0f;
        public bool HasTarget => currentTarget != null;
        public EnemyState CurrentState => currentState;
        public float HealthPercentage => health / maxHealth;
        public Transform Target => currentTarget;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            InitializeComponents();
            SetupInitialState();
        }

        private void Update()
        {
            if (IsDead) return;

            HandleDetection();
            UpdateStateMachine();
            UpdateAnimations();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            aiAgent = GetComponent<EnhancedAIAgent>();
            animator = GetComponent<Animator>();

            if (aiAgent == null)
            {
                Debug.LogError("EnhancedAIAgent not found!", this);
                enabled = false;
                return;
            }

            // Configure AI agent
            aiAgent.SetSpeed(moveSpeed);
            aiAgent.SetAgentType(EnhancedAIAgent.AgentType.Medium);
        }

        private void SetupInitialState()
        {
            health = maxHealth;
            
            if (enablePatrol && patrolPoints != null && patrolPoints.Length > 0)
            {
                ChangeState(EnemyState.Patrolling);
            }
            else
            {
                ChangeState(EnemyState.Idle);
            }
        }

        #endregion

        #region Detection System

        private void HandleDetection()
        {
            if (Time.time < nextDetectionCheck) return;
            nextDetectionCheck = Time.time + DETECTION_INTERVAL;

            SearchForTargets();
            CheckTargetLoss();
        }

        private void SearchForTargets()
        {
            Collider[] potentialTargets = Physics.OverlapSphere(
                transform.position, detectionRadius, targetLayers);

            foreach (var collider in potentialTargets)
            {
                if (IsValidTarget(collider.transform))
                {
                    SetTarget(collider.transform);
                    return;
                }
            }
        }

        private bool IsValidTarget(Transform target)
        {
            if (target == null || target == transform) return false;

            Vector3 directionToTarget = (target.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);

            // Check if target is within field of view
            if (angleToTarget > fieldOfViewAngle * 0.5f) return false;

            // Check line of sight
            if (Physics.Raycast(transform.position + Vector3.up, directionToTarget, 
                out RaycastHit hit, detectionRadius))
            {
                return hit.transform == target;
            }

            return false;
        }

        private void CheckTargetLoss()
        {
            if (currentTarget == null) return;

            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            if (distanceToTarget > loseTargetRadius || !IsValidTarget(currentTarget))
            {
                LoseTarget();
            }
            else
            {
                lastKnownTargetPosition = currentTarget.position;
                lastTargetSeenTime = Time.time;
            }
        }

        private void SetTarget(Transform target)
        {
            if (currentTarget != target)
            {
                currentTarget = target;
                lastKnownTargetPosition = target.position;
                lastTargetSeenTime = Time.time;
                OnTargetDetected?.Invoke(this, target);

                if (currentState != EnemyState.Chasing && currentState != EnemyState.Attacking)
                {
                    ChangeState(EnemyState.Chasing);
                }
            }
        }

        private void LoseTarget()
        {
            if (currentTarget != null)
            {
                OnTargetLost?.Invoke(this);
                currentTarget = null;
                ChangeState(EnemyState.Searching);
            }
        }

        #endregion

        #region State Machine

        private void UpdateStateMachine()
        {
            EnemyState newState = DetermineNextState();
            if (newState != currentState)
            {
                ChangeState(newState);
            }

            ExecuteCurrentState();
        }

        private EnemyState DetermineNextState()
        {
            if (IsDead) return EnemyState.Dead;

            float distanceToTarget = currentTarget != null ? 
                Vector3.Distance(transform.position, currentTarget.position) : float.MaxValue;

            switch (currentState)
            {
                case EnemyState.Idle:
                    if (currentTarget != null) return EnemyState.Chasing;
                    if (enablePatrol && patrolPoints.Length > 0) return EnemyState.Patrolling;
                    break;

                case EnemyState.Patrolling:
                    if (currentTarget != null) return EnemyState.Chasing;
                    break;

                case EnemyState.Chasing:
                    if (currentTarget == null) return EnemyState.Searching;
                    if (distanceToTarget <= attackRange && !isAttacking) return EnemyState.Attacking;
                    break;

                case EnemyState.Attacking:
                    if (currentTarget == null) return EnemyState.Searching;
                    if (distanceToTarget > attackRange * 1.2f) return EnemyState.Chasing;
                    break;

                case EnemyState.Searching:
                    if (currentTarget != null) return EnemyState.Chasing;
                    if (Time.time - stateStartTime > searchDuration)
                    {
                        return enablePatrol && patrolPoints.Length > 0 ? 
                            EnemyState.Patrolling : EnemyState.Idle;
                    }
                    break;

                case EnemyState.Alert:
                    if (currentTarget != null) return EnemyState.Chasing;
                    if (Time.time - stateStartTime > alertDuration) return EnemyState.Idle;
                    break;
            }

            return currentState;
        }

        private void ChangeState(EnemyState newState)
        {
            ExitCurrentState();
            
            currentState = newState;
            stateStartTime = Time.time;
            
            EnterNewState();
        }

        private void ExitCurrentState()
        {
            switch (currentState)
            {
                case EnemyState.Attacking:
                    isAttacking = false;
                    break;
                case EnemyState.Patrolling:
                    waitingAtPatrolPoint = false;
                    break;
            }
        }

        private void EnterNewState()
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    aiAgent.Stop();
                    aiAgent.SetSpeed(moveSpeed);
                    break;

                case EnemyState.Patrolling:
                    aiAgent.SetSpeed(moveSpeed);
                    MoveToNextPatrolPoint();
                    break;

                case EnemyState.Chasing:
                    aiAgent.SetSpeed(chaseSpeed);
                    if (currentTarget != null)
                    {
                        aiAgent.SetDestination(currentTarget.position);
                    }
                    break;

                case EnemyState.Attacking:
                    aiAgent.SetSpeed(canAttackWhileMoving ? chaseSpeed * 0.5f : 0f);
                    break;

                case EnemyState.Searching:
                    aiAgent.SetSpeed(moveSpeed);
                    aiAgent.SetDestination(lastKnownTargetPosition);
                    break;

                case EnemyState.Dead:
                    aiAgent.Stop();
                    OnDeath?.Invoke(this);
                    break;
            }
        }

        private void ExecuteCurrentState()
        {
            switch (currentState)
            {
                case EnemyState.Patrolling:
                    ExecutePatrolling();
                    break;
                case EnemyState.Chasing:
                    ExecuteChasing();
                    break;
                case EnemyState.Attacking:
                    ExecuteAttacking();
                    break;
                case EnemyState.Searching:
                    ExecuteSearching();
                    break;
            }
        }

        #endregion

        #region State Behaviors

        private void ExecutePatrolling()
        {
            if (waitingAtPatrolPoint)
            {
                patrolWaitTimer -= Time.deltaTime;
                if (patrolWaitTimer <= 0f)
                {
                    waitingAtPatrolPoint = false;
                    MoveToNextPatrolPoint();
                }
            }
            else if (aiAgent.HasReachedDestination)
            {
                waitingAtPatrolPoint = true;
                patrolWaitTimer = patrolWaitTime;
            }
        }

        private void ExecuteChasing()
        {
            if (currentTarget != null)
            {
                // Use pathfinding optimization settings
                bool shouldUpdatePath = Time.time - lastPathUpdateTime >= pathUpdateInterval;
                bool targetMovedSignificantly = Vector3.Distance(currentTarget.position, lastPathTarget) > pathRecalculationDistance;

                if (shouldUpdatePath || targetMovedSignificantly)
                {
                    if (enablePathOptimization)
                    {
                        // Optimize path based on pathfinding mode
                        SetOptimizedDestination(currentTarget.position);
                    }
                    else
                    {
                        aiAgent.SetDestination(currentTarget.position);
                    }

                    lastPathUpdateTime = Time.time;
                    lastPathTarget = currentTarget.position;
                }

                // Apply group behavior if enabled
                if (enableGroupBehavior)
                {
                    ApplyGroupBehavior();
                }
            }
        }

        private void ExecuteAttacking()
        {
            if (Time.time - lastAttackTime >= attackCooldown && !isAttacking)
            {
                StartCoroutine(PerformAttack());
            }
        }

        private void ExecuteSearching()
        {
            if (aiAgent.HasReachedDestination)
            {
                // Search in a random direction around last known position
                Vector3 searchPosition = lastKnownTargetPosition + 
                    Random.insideUnitSphere * 5f;
                searchPosition.y = lastKnownTargetPosition.y;
                aiAgent.SetDestination(searchPosition);
            }
        }

        #endregion

        #region Patrol System

        private void MoveToNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;

            if (randomPatrolOrder)
            {
                currentPatrolIndex = Random.Range(0, patrolPoints.Length);
            }
            else
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }

            if (patrolPoints[currentPatrolIndex] != null)
            {
                aiAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }

        #endregion

        #region Combat System

        private IEnumerator PerformAttack()
        {
            isAttacking = true;
            lastAttackTime = Time.time;

            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Wait for attack animation
            yield return new WaitForSeconds(attackAnimationDuration * 0.6f);

            // Apply damage if target is still in range
            if (currentTarget != null && 
                Vector3.Distance(transform.position, currentTarget.position) <= attackRange * 1.2f)
            {
                ApplyDamageToTarget();
            }

            yield return new WaitForSeconds(attackAnimationDuration * 0.4f);
            isAttacking = false;
        }

        private void ApplyDamageToTarget()
        {
            // Add your damage application logic here
            Debug.Log($"{gameObject.name} attacks {currentTarget.name} for {damage} damage!");
        }

        public void TakeDamage(float damageAmount)
        {
            if (IsDead) return;

            health = Mathf.Max(0, health - damageAmount);

            if (health <= 0)
            {
                ChangeState(EnemyState.Dead);
            }
            else if (currentState == EnemyState.Idle || currentState == EnemyState.Patrolling)
            {
                ChangeState(EnemyState.Alert);
            }
        }

        #endregion

        #region Animation

        private void UpdateAnimations()
        {
            if (animator == null) return;

            float speed = aiAgent.CurrentSpeed;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsMoving", speed > 0.1f);
            animator.SetInteger("State", (int)currentState);
            animator.SetBool("HasTarget", currentTarget != null);
            animator.SetFloat("HealthPercentage", HealthPercentage);
        }

        /// <summary>
        /// Set optimized destination based on pathfinding mode and settings
        /// </summary>
        private void SetOptimizedDestination(Vector3 targetPosition)
        {
            Vector3 optimizedTarget = targetPosition;

            // Apply pathfinding mode optimizations
            switch (pathfindingMode)
            {
                case PathfindingMode.Auto:
                case PathfindingMode.NavMesh:
                    optimizedTarget = targetPosition;
                    break;
                case PathfindingMode.AStar:
                case PathfindingMode.FlowField:
                    optimizedTarget = OptimizePathTarget(targetPosition);
                    break;
                case PathfindingMode.Hybrid:
                    optimizedTarget = GetHybridPathTarget(targetPosition);
                    break;
                default:
                    optimizedTarget = targetPosition;
                    break;
            }

            aiAgent.SetDestination(optimizedTarget);
        }

        /// <summary>
        /// Optimize path target to avoid obstacles and improve performance
        /// </summary>
        private Vector3 OptimizePathTarget(Vector3 originalTarget)
        {
            // Simple path optimization - in a real implementation, this would use the PathfindingAlgorithms
            Vector3 directionToTarget = (originalTarget - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, originalTarget);

            // Check for obstacles and adjust target if needed
            if (Physics.Raycast(transform.position, directionToTarget, out RaycastHit hit, distanceToTarget))
            {
                // Adjust target to avoid immediate obstacles
                Vector3 avoidanceOffset = Vector3.Cross(directionToTarget, Vector3.up) * 2f;
                return originalTarget + avoidanceOffset;
            }

            return originalTarget;
        }

        /// <summary>
        /// Get hybrid path target combining direct and optimized approaches
        /// </summary>
        private Vector3 GetHybridPathTarget(Vector3 originalTarget)
        {
            float distanceToTarget = Vector3.Distance(transform.position, originalTarget);

            // Use direct pathfinding for close targets, optimized for distant ones
            if (distanceToTarget < 10f)
            {
                return originalTarget;
            }
            else
            {
                return OptimizePathTarget(originalTarget);
            }
        }

        /// <summary>
        /// Apply group behavior adjustments to movement
        /// </summary>
        private void ApplyGroupBehavior()
        {
            if (Time.time - lastPathOptimizationCheck < 0.5f) return;
            lastPathOptimizationCheck = Time.time;

            // Find other enemies nearby for group behavior
            Collider[] nearbyEnemies = Physics.OverlapSphere(transform.position, 5f);
            List<EnhancedEnemyController> groupMembers = new List<EnhancedEnemyController>();

            foreach (var collider in nearbyEnemies)
            {
                var enemy = collider.GetComponent<EnhancedEnemyController>();
                if (enemy != null && enemy != this && enemy.HasTarget)
                {
                    groupMembers.Add(enemy);
                }
            }

            // Apply separation to avoid clustering
            if (groupMembers.Count > 0)
            {
                Vector3 separationForce = Vector3.zero;
                foreach (var member in groupMembers)
                {
                    Vector3 direction = transform.position - member.transform.position;
                    if (direction.magnitude < 3f) // Too close
                    {
                        separationForce += direction.normalized / direction.magnitude;
                    }
                }

                if (separationForce.magnitude > 0.1f)
                {
                    Vector3 adjustedTarget = lastPathTarget + separationForce.normalized * 2f;
                    aiAgent.SetDestination(adjustedTarget);
                }
            }
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // Detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Lose target radius
            Gizmos.color = Color.orange;
            Gizmos.DrawWireSphere(transform.position, loseTargetRadius);
            
            // Field of view
            Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle * 0.5f, Vector3.up) * transform.forward * detectionRadius;
            Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle * 0.5f, Vector3.up) * transform.forward * detectionRadius;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, fovLine1);
            Gizmos.DrawRay(transform.position, fovLine2);
        }

        #endregion
    }
}
