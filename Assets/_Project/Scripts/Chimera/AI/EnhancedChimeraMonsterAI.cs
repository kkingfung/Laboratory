using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Laboratory.AI.Pathfinding;
using Laboratory.AI.Agents;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Enhanced AI controller for Chimera monsters with advanced pathfinding.
    /// </summary>
    [RequireComponent(typeof(EnhancedAIAgent))]
    [RequireComponent(typeof(Animator))]
    public class EnhancedChimeraMonsterAI : MonoBehaviour
    {
        [Header("AI Configuration")]
        [SerializeField] private Laboratory.Chimera.AI.AIBehaviorType behaviorType = Laboratory.Chimera.AI.AIBehaviorType.Companion;
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float followDistance = 3f;
        [SerializeField] private float maxFollowDistance = 20f;
        [SerializeField] private float patrolRadius = 10f;
        [SerializeField] private LayerMask enemyLayers = 1 << 8;

        [Header("Enhanced Pathfinding")]
        [SerializeField] private PathfindingMode preferredPathfindingMode = PathfindingMode.Hybrid;
        [SerializeField] private float pathUpdateFrequency = 0.5f;
        [SerializeField] private float pathRecalculationThreshold = 2f;
        [SerializeField] private bool useSmartPathing = true;
        [SerializeField] private bool enablePathPrediction = true;

        [Header("Combat Settings")]
        [SerializeField] private float attackCooldown = 2f;
        [SerializeField] private float combatTimeout = 10f;
        [SerializeField] private int maxAttackDamage = 25;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;
        [SerializeField] private bool enableDetailedLogging = false;

        // Components
        private EnhancedAIAgent aiAgent;
        private Animator animator;

        // AI State
        private AIState currentState = AIState.Idle;
        private Transform player;
        private Transform currentTarget;
        private Vector3 lastKnownPlayerPosition;
        private Vector3 patrolCenter;
        private Vector3 currentDestination;

        // Timers
        private float lastAttackTime;
        private float combatStartTime;
        private float stateChangeTime;
        private float lastPlayerSeenTime;

        // Detection
        private List<Transform> detectedEnemies = new List<Transform>();


        public enum AIState
        {
            Idle,
            Following,
            Patrolling,
            Pursuing,
            Combat,
            Returning
        }

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void Start()
        {
            SetupInitialState();
        }

        private void Update()
        {
            UpdateAI();
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
            aiAgent.SetAgentType(EnhancedAIAgent.AgentType.Medium);

            // Configure pathfinding settings
            if (useSmartPathing)
            {
                // Store pathfinding preferences for internal use
                Debug.Log($"[EnhancedChimeraMonsterAI] Using smart pathing with mode: {preferredPathfindingMode}");
            }
        }

        private void SetupInitialState()
        {
            patrolCenter = transform.position;
            currentState = AIState.Idle;
            FindPlayer();
            GenerateNewPatrolTarget();
        }

        #endregion

        #region AI State Machine

        private void UpdateAI()
        {
            DetectEnemies();
            DetermineState();
            ExecuteCurrentState();

            // Update pathfinding based on frequency
            if (useSmartPathing && Time.time - lastPathUpdate > pathUpdateFrequency)
            {
                UpdatePathfinding();
            }
        }

        private float lastPathUpdate = 0f;

        private void UpdatePathfinding()
        {
            lastPathUpdate = Time.time;

            // Check if we need to recalculate path based on distance from original destination
            if (aiAgent != null && aiAgent.HasValidPath)
            {
                float distanceFromDestination = Vector3.Distance(transform.position, aiAgent.Destination);
                if (distanceFromDestination > pathRecalculationThreshold)
                {
                    // Recalculate path if we've strayed too far
                    Vector3 currentDestination = aiAgent.Destination;
                    aiAgent.SetDestination(currentDestination);
                }
            }
        }

        private void DetectEnemies()
        {
            detectedEnemies.Clear();
            
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, detectionRange, enemyLayers);
            
            foreach (var collider in nearbyColliders)
            {
                if (collider.transform != transform && HasLineOfSight(collider.transform))
                {
                    detectedEnemies.Add(collider.transform);
                }
            }
        }

        private bool HasLineOfSight(Transform target)
        {
            Vector3 directionToTarget = target.position - transform.position;
            return !Physics.Raycast(transform.position + Vector3.up, directionToTarget.normalized, 
                directionToTarget.magnitude, ~enemyLayers);
        }

        private void DetermineState()
        {
            AIState newState = currentState;

            // Combat check
            if (detectedEnemies.Count > 0 && ShouldEngageInCombat())
            {
                currentTarget = GetBestTarget();
                if (currentTarget != null)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
                    
                    if (distanceToTarget <= attackRange)
                    {
                        newState = AIState.Combat;
                    }
                    else
                    {
                        newState = AIState.Pursuing;
                    }
                }
            }
            // Following check
            else if (player != null && behaviorType == Laboratory.Chimera.AI.AIBehaviorType.Companion)
            {
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                
                if (distanceToPlayer > followDistance)
                {
                    newState = AIState.Following;
                }
                else
                {
                    newState = AIState.Idle;
                }
            }
            // Patrol check
            else if (currentState == AIState.Idle && Time.time - stateChangeTime > 3f)
            {
                newState = AIState.Patrolling;
            }

            if (newState != currentState)
            {
                ChangeState(newState);
            }
        }

        private void ChangeState(AIState newState)
        {
            if (enableDetailedLogging)
            {
                Debug.Log($"{gameObject.name}: {currentState} -> {newState}");
            }

            currentState = newState;
            stateChangeTime = Time.time;

            switch (newState)
            {
                case AIState.Combat:
                    combatStartTime = Time.time;
                    break;
            }
        }

        #endregion

        #region State Execution

        private void ExecuteCurrentState()
        {
            switch (currentState)
            {
                case AIState.Idle:
                    HandleIdleState();
                    break;
                case AIState.Following:
                    HandleFollowingState();
                    break;
                case AIState.Patrolling:
                    HandlePatrollingState();
                    break;
                case AIState.Pursuing:
                    HandlePursuingState();
                    break;
                case AIState.Combat:
                    HandleCombatState();
                    break;
                case AIState.Returning:
                    HandleReturningState();
                    break;
            }
        }

        private void HandleIdleState()
        {
            // Stop movement
            aiAgent.Stop();
        }

        private void HandleFollowingState()
        {
            if (player == null) return;

            Vector3 targetPosition = enablePathPrediction ? PredictPlayerPosition() : player.position;
            aiAgent.SetDestination(targetPosition);
        }

        private void HandlePatrollingState()
        {
            if (aiAgent.HasReachedDestination)
            {
                GenerateNewPatrolTarget();
            }
        }

        private void HandlePursuingState()
        {
            if (currentTarget == null)
            {
                ChangeState(AIState.Returning);
                return;
            }

            Vector3 targetPosition = enablePathPrediction ? PredictTargetPosition() : currentTarget.position;
            aiAgent.SetDestination(targetPosition);

            // Check if we should give up pursuit
            if (Time.time - combatStartTime > combatTimeout)
            {
                ChangeState(AIState.Returning);
            }
        }

        private void HandleCombatState()
        {
            if (currentTarget == null)
            {
                ChangeState(AIState.Returning);
                return;
            }

            // Stop and face target
            aiAgent.Stop();
            LookAtTarget(currentTarget);

            // Attack if ready
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                PerformAttack();
                lastAttackTime = Time.time;
            }

            // Check if target moved away
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            if (distanceToTarget > attackRange * 1.5f)
            {
                ChangeState(AIState.Pursuing);
            }
        }

        private void HandleReturningState()
        {
            Vector3 returnPosition = player != null ? player.position : patrolCenter;
            aiAgent.SetDestination(returnPosition);

            if (Vector3.Distance(transform.position, returnPosition) < 2f)
            {
                ChangeState(AIState.Idle);
            }
        }

        #endregion

        #region Combat System

        private bool ShouldEngageInCombat()
        {
            switch (behaviorType)
            {
                case Laboratory.Chimera.AI.AIBehaviorType.Aggressive:
                    return true;
                case Laboratory.Chimera.AI.AIBehaviorType.Neutral:
                    return player != null && Vector3.Distance(player.position, transform.position) < 8f;
                case Laboratory.Chimera.AI.AIBehaviorType.Companion:
                    return true; // Protect player
                case Laboratory.Chimera.AI.AIBehaviorType.Passive:
                    return false;
                case Laboratory.Chimera.AI.AIBehaviorType.Guardian:
                    return Vector3.Distance(transform.position, patrolCenter) < patrolRadius;
                default:
                    return true;
            }
        }

        private Transform GetBestTarget()
        {
            if (detectedEnemies.Count == 0) return null;

            Transform bestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var enemy in detectedEnemies)
            {
                if (enemy == null) continue;

                float distance = Vector3.Distance(transform.position, enemy.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = enemy;
                }
            }

            return bestTarget;
        }

        private void PerformAttack()
        {
            if (currentTarget == null) return;

            // Trigger attack animation
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }

            // Deal damage (simplified)
            int damage = Random.Range(maxAttackDamage / 2, maxAttackDamage);
            Debug.Log($"{gameObject.name} attacks {currentTarget.name} for {damage} damage!");

            // Add your damage dealing logic here
            // Example: currentTarget.GetComponent<HealthComponent>()?.TakeDamage(damage);
        }

        #endregion

        #region Movement Helpers

        private void FindPlayer()
        {
            if (player == null)
            {
                GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
                if (playerGO != null)
                {
                    player = playerGO.transform;
                }
            }
        }

        private void GenerateNewPatrolTarget()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += patrolCenter;
            randomDirection.y = patrolCenter.y;

            // Make sure the point is on the NavMesh
            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, patrolRadius, NavMesh.AllAreas))
            {
                aiAgent.SetDestination(hit.position);
            }
            else
            {
                aiAgent.SetDestination(patrolCenter);
            }
        }

        private Vector3 PredictPlayerPosition()
        {
            if (player == null) return lastKnownPlayerPosition;

            // Simple prediction based on player velocity
            Rigidbody playerRb = player.GetComponent<Rigidbody>();
            if (playerRb != null)
            {
                float predictionTime = Vector3.Distance(transform.position, player.position) / aiAgent.CurrentSpeed;
                return player.position + playerRb.linearVelocity * predictionTime;
            }

            return player.position;
        }

        private Vector3 PredictTargetPosition()
        {
            if (currentTarget == null) return Vector3.zero;

            Rigidbody targetRb = currentTarget.GetComponent<Rigidbody>();
            if (targetRb != null)
            {
                float predictionTime = Vector3.Distance(transform.position, currentTarget.position) / aiAgent.CurrentSpeed;
                return currentTarget.position + targetRb.linearVelocity * predictionTime;
            }

            return currentTarget.position;
        }

        private void LookAtTarget(Transform target)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0; // Keep on horizontal plane
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }

        #endregion

        #region Animation

        private void UpdateAnimations()
        {
            if (animator == null) return;

            // Update movement animation
            float speed = aiAgent.CurrentSpeed;
            animator.SetFloat("Speed", speed);
            animator.SetBool("IsMoving", speed > 0.1f);

            // Update state animations
            animator.SetInteger("State", (int)currentState);
            animator.SetBool("InCombat", currentState == AIState.Combat || currentState == AIState.Pursuing);
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            // Detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // Follow distance
            if (player != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(player.position, followDistance);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(player.position, maxFollowDistance);
            }

            // Patrol area
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(patrolCenter, patrolRadius);

            // Current target
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }

            // Detected enemies
            Gizmos.color = Color.orange;
            foreach (var enemy in detectedEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(transform.position, enemy.position);
                }
            }
        }

        #endregion
    }
}
