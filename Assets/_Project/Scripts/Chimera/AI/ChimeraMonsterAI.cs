using UnityEngine;
using UnityEngine.AI;
using Unity.Entities;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Breeding;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Advanced AI system for Chimera creatures with genetic behavior modification
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class ChimeraMonsterAI : MonoBehaviour
    {
        [Header("AI Configuration")]
        [SerializeField] private AIBehaviorState currentState = AIBehaviorState.Idle;
        [SerializeField] private float detectionRange = 10f;
        [SerializeField] private float attackRange = 2f;
        [SerializeField] private float followDistance = 5f;
        [SerializeField] private float patrolRadius = 8f;
        [SerializeField] private LayerMask targetLayers = 1;
        
        [Header("Behavior Configuration")]
        [SerializeField] private AIBehaviorType behaviorType = AIBehaviorType.Neutral;

        [Header("Genetic Modifiers")]
        [SerializeField] private float geneticAggressionModifier = 1f;
        [SerializeField] private float geneticDetectionRangeModifier = 1f;
        [SerializeField] private float geneticFollowDistance = 5f;
        [SerializeField] private float geneticPatrolRadius = 8f;
        
        [Header("State Management")]
        [SerializeField] private float stateChangeInterval = 1f;
        [SerializeField] private float lastStateChange = 0f;
        
        // Components
        private NavMeshAgent navAgent;
        private Animator animator;
        private CreatureDefinition creatureDefinition;
        private GeneticProfile genetics;
        
        // AI State
        private Transform currentTarget;
        private Vector3 lastKnownTargetPosition;
        private Vector3 patrolCenter;
        private Vector3 currentPatrolTarget;
        private float lastTargetSeen;
        private bool isInitialized = false;
        
        // Behavior tracking
        private readonly Dictionary<AIBehaviorState, float> stateTimers = new Dictionary<AIBehaviorState, float>();
        private readonly Queue<Vector3> patrolPoints = new Queue<Vector3>();
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeComponents();
            InitializeStateTimers();
        }
        
        private void Start()
        {
            SetupAI();
        }
        
        private void Update()
        {
            if (!isInitialized || !navAgent.enabled) return;
            
            UpdateStateTimer();
            UpdateAI();
            UpdateAnimations();
        }
        
        #endregion
        
        #region Combat
        
        private void PerformAttack()
        {
            // Basic attack logic - extend this for more complex combat
            if (currentTarget != null)
            {
                var healthComponent = currentTarget.GetComponent<Laboratory.Core.Health.IHealthComponent>();
                if (healthComponent != null)
                {
                    int damage = CalculateAttackDamage();
                    healthComponent.TakeDamage(new Laboratory.Core.Health.DamageRequest(damage, gameObject, Laboratory.Core.Health.DamageType.Physical));
                }
            }
        }
        
        private int CalculateAttackDamage()
        {
            int baseDamage = 10;
            if (creatureDefinition != null)
            {
                baseDamage = creatureDefinition.baseStats.attack;
            }
            
            // Apply genetic aggression modifier
            return Mathf.RoundToInt(baseDamage * geneticAggressionModifier);
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            navAgent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            
            // Try to get creature data from CreatureInstanceComponent
            var creatureInstance = GetComponent<Laboratory.Chimera.CreatureInstanceComponent>();
            if (creatureInstance != null)
            {
                creatureDefinition = creatureInstance.Instance.Definition;
                genetics = creatureInstance.Instance.GeneticProfile;
            }
        }
        
        private void InitializeStateTimers()
        {
            foreach (AIBehaviorState state in System.Enum.GetValues(typeof(AIBehaviorState)))
            {
                stateTimers[state] = 0f;
            }
        }
        
        private void SetupAI()
        {
            patrolCenter = transform.position;
            GeneratePatrolPoints();
            ApplyGeneticModifiers();
            
            isInitialized = true;
        }
        
        private void ApplyGeneticModifiers()
        {
            if (genetics == null) return;
            
            // Apply genetic modifiers that were set by CreatureInstanceComponent
            detectionRange *= geneticDetectionRangeModifier;
            followDistance = geneticFollowDistance;
            patrolRadius = geneticPatrolRadius;
            
            // Apply aggression effects
            if (geneticAggressionModifier > 1.2f)
            {
                // High aggression: increased detection range, shorter attack range
                detectionRange *= 1.3f;
                attackRange *= 0.8f;
            }
            else if (geneticAggressionModifier < 0.8f)
            {
                // Low aggression: decreased detection range, longer attack range
                detectionRange *= 0.7f;
                attackRange *= 1.2f;
            }
        }
        
        #endregion
        
        #region Public Genetic Methods (Called by CreatureInstanceComponent)
        
        public void SetGeneticAggressionModifier(float modifier)
        {
            geneticAggressionModifier = modifier;
        }
        
        public void SetGeneticDetectionRangeModifier(float modifier)
        {
            geneticDetectionRangeModifier = modifier;
        }
        
        public void SetGeneticFollowDistance(float distance)
        {
            geneticFollowDistance = distance;
        }
        
        public void SetGeneticPatrolRadius(float radius)
        {
            geneticPatrolRadius = radius;
        }
        
        #endregion
        
        #region AI State Machine
        
        private void UpdateAI()
        {
            if (Time.time - lastStateChange < stateChangeInterval) return;
            
            switch (currentState)
            {
                case AIBehaviorState.Idle:
                    HandleIdleState();
                    break;
                case AIBehaviorState.Patrol:
                    HandlePatrolState();
                    break;
                case AIBehaviorState.Chase:
                    HandleChaseState();
                    break;
                case AIBehaviorState.Attack:
                    HandleAttackState();
                    break;
                case AIBehaviorState.Search:
                    HandleSearchState();
                    break;
                case AIBehaviorState.Return:
                    HandleReturnState();
                    break;
                case AIBehaviorState.Follow:
                    HandleFollowState();
                    break;
            }
            
            lastStateChange = Time.time;
        }
        
        private void HandleIdleState()
        {
            var target = FindNearestTarget();
            if (target != null)
            {
                SetTarget(target);
                ChangeState(AIBehaviorState.Chase);
                return;
            }
            
            // Random chance to start patrolling
            if (stateTimers[AIBehaviorState.Idle] > Random.Range(3f, 8f))
            {
                ChangeState(AIBehaviorState.Patrol);
            }
        }
        
        private void HandlePatrolState()
        {
            var target = FindNearestTarget();
            if (target != null)
            {
                SetTarget(target);
                ChangeState(AIBehaviorState.Chase);
                return;
            }
            
            // Move to next patrol point
            if (!navAgent.hasPath || navAgent.remainingDistance < 1f)
            {
                MoveToNextPatrolPoint();
            }
            
            // Return to idle after patrolling for a while
            if (stateTimers[AIBehaviorState.Patrol] > Random.Range(15f, 30f))
            {
                ChangeState(AIBehaviorState.Idle);
            }
        }
        
        private void HandleChaseState()
        {
            if (currentTarget == null)
            {
                ChangeState(AIBehaviorState.Search);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            // Switch to attack if close enough
            if (distanceToTarget <= attackRange)
            {
                ChangeState(AIBehaviorState.Attack);
                return;
            }
            
            // Continue chasing
            navAgent.SetDestination(currentTarget.position);
            lastKnownTargetPosition = currentTarget.position;
            lastTargetSeen = Time.time;
            
            // Lose target if too far from patrol center
            if (Vector3.Distance(transform.position, patrolCenter) > patrolRadius * 2f)
            {
                currentTarget = null;
                ChangeState(AIBehaviorState.Return);
            }
        }
        
        private void HandleAttackState()
        {
            if (currentTarget == null)
            {
                ChangeState(AIBehaviorState.Search);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            // Move back to chase if target moved away
            if (distanceToTarget > attackRange * 1.5f)
            {
                ChangeState(AIBehaviorState.Chase);
                return;
            }
            
            // Face the target
            Vector3 lookDirection = (currentTarget.position - transform.position).normalized;
            lookDirection.y = 0f;
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }
            
            // Stop moving for attack
            navAgent.ResetPath();
            
            // Trigger attack animation/damage here
            PerformAttack();
        }
        
        private void HandleSearchState()
        {
            // Move to last known target position
            if (Vector3.Distance(transform.position, lastKnownTargetPosition) > 1f)
            {
                navAgent.SetDestination(lastKnownTargetPosition);
            }
            
            // Look for target again
            var target = FindNearestTarget();
            if (target != null)
            {
                SetTarget(target);
                ChangeState(AIBehaviorState.Chase);
                return;
            }
            
            // Give up searching after a while
            if (stateTimers[AIBehaviorState.Search] > 10f)
            {
                ChangeState(AIBehaviorState.Return);
            }
        }
        
        private void HandleReturnState()
        {
            // Return to patrol center
            navAgent.SetDestination(patrolCenter);
            
            if (Vector3.Distance(transform.position, patrolCenter) < 2f)
            {
                ChangeState(AIBehaviorState.Idle);
            }
        }
        
        private void HandleFollowState()
        {
            if (currentTarget == null)
            {
                ChangeState(AIBehaviorState.Idle);
                return;
            }
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
            
            // Maintain follow distance
            if (distanceToTarget > followDistance)
            {
                navAgent.SetDestination(currentTarget.position);
            }
            else if (distanceToTarget < followDistance * 0.5f)
            {
                navAgent.ResetPath();
            }
        }
        
        private void ChangeState(AIBehaviorState newState)
        {
            currentState = newState;
            stateTimers[newState] = 0f;
        }
        
        private void UpdateStateTimer()
        {
            stateTimers[currentState] += Time.deltaTime;
        }
        
        #endregion
        
        #region Target Detection & Management
        
        private Transform FindNearestTarget()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange, targetLayers);
            Transform nearestTarget = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in colliders)
            {
                if (collider.transform == transform) continue;
                
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = collider.transform;
                }
            }
            
            return nearestTarget;
        }
        
        public void SetTarget(Transform target)
        {
            currentTarget = target;
            lastKnownTargetPosition = target.position;
            lastTargetSeen = Time.time;
        }
        
        #endregion
        
        #region Patrol System
        
        private void GeneratePatrolPoints()
        {
            patrolPoints.Clear();
            
            for (int i = 0; i < 4; i++)
            {
                Vector2 randomPoint = Random.insideUnitCircle * patrolRadius;
                Vector3 patrolPoint = patrolCenter + new Vector3(randomPoint.x, 0f, randomPoint.y);
                
                // Try to find valid NavMesh position
                if (NavMesh.SamplePosition(patrolPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    patrolPoints.Enqueue(hit.position);
                }
            }
            
            // Fallback: add patrol center if no points found
            if (patrolPoints.Count == 0)
            {
                patrolPoints.Enqueue(patrolCenter);
            }
        }
        
        private void MoveToNextPatrolPoint()
        {
            if (patrolPoints.Count == 0)
            {
                GeneratePatrolPoints();
                return;
            }
            
            currentPatrolTarget = patrolPoints.Dequeue();
            patrolPoints.Enqueue(currentPatrolTarget); // Re-add to end of queue
            
            navAgent.SetDestination(currentPatrolTarget);
        }
        
        #endregion
        
        #region Animation
        
        private void UpdateAnimations()
        {
            if (animator == null) return;
            
            // Set animation parameters based on state and movement
            animator.SetFloat("Speed", navAgent.velocity.magnitude);
            animator.SetBool("IsAttacking", currentState == AIBehaviorState.Attack);
            animator.SetBool("IsChasing", currentState == AIBehaviorState.Chase);
            animator.SetInteger("AIState", (int)currentState);
        }
        
        #endregion
        
        #region Public API
        
        public void SetFollowTarget(Transform target)
        {
            SetTarget(target);
            ChangeState(AIBehaviorState.Follow);
        }
        
        public void StopFollowing()
        {
            currentTarget = null;
            ChangeState(AIBehaviorState.Idle);
        }
        
        public void SetPatrolCenter(Vector3 center)
        {
            patrolCenter = center;
            GeneratePatrolPoints();
        }

        public void SetBehaviorType(AIBehaviorType newBehaviorType)
        {
            behaviorType = newBehaviorType;

            // Adjust AI parameters based on behavior type
            switch (behaviorType)
            {
                case AIBehaviorType.Companion:
                    geneticAggressionModifier = 0.5f;
                    targetLayers = 1 << LayerMask.NameToLayer("Enemy"); // Only target enemies
                    break;
                case AIBehaviorType.Aggressive:
                    geneticAggressionModifier = 1.5f;
                    detectionRange *= 1.2f;
                    break;
                case AIBehaviorType.Passive:
                    geneticAggressionModifier = 0.2f;
                    detectionRange *= 0.8f;
                    break;
                case AIBehaviorType.Territorial:
                    geneticAggressionModifier = 1.2f;
                    patrolRadius *= 1.5f;
                    break;
                case AIBehaviorType.Predator:
                    geneticAggressionModifier = 1.8f;
                    detectionRange *= 1.3f;
                    break;
                case AIBehaviorType.Guardian:
                    geneticAggressionModifier = 1.0f;
                    detectionRange *= 1.1f;
                    break;
            }
        }
        
        public void SetAggressionLevel(float aggression)
        {
            geneticAggressionModifier = Mathf.Clamp(aggression, 0.1f, 3f);
        }

        public AIBehaviorState GetCurrentState() => currentState;
        public Transform GetCurrentTarget() => currentTarget;
        public float GetStateTime() => stateTimers[currentState];

        // Additional public methods for external AI management
        public void SetDestinationPublic(Vector3 destination)
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(destination);
            }
        }

        public float GetGeneticPatrolRadius()
        {
            return geneticPatrolRadius;
        }

        // Public properties for ChimeraAIManager access
        public AIBehaviorState CurrentState => currentState;
        public Transform CurrentTarget => currentTarget;
        public bool IsInCombat => currentState == AIBehaviorState.Combat || currentState == AIBehaviorState.Hunt;

        public void CancelCombat()
        {
            if (IsInCombat)
            {
                currentTarget = null;
                ChangeState(AIBehaviorState.Idle);
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            
            // Draw patrol radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(patrolCenter, patrolRadius);
            
            // Draw current target
            if (currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, currentTarget.position);
            }
            
            // Draw patrol points
            Gizmos.color = Color.green;
            foreach (var point in patrolPoints)
            {
                Gizmos.DrawWireSphere(point, 0.5f);
            }
        }
        
        #endregion
    }
    
}
