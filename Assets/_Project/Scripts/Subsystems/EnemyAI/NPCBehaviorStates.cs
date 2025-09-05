using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Health;

namespace Laboratory.EnemyAI
{
    #region AI States

    /// <summary>
    /// Idle state - NPC is inactive and waiting
    /// </summary>
    public class IdleState : AIState
    {
        private float idleStartTime;
        private float maxIdleTime = 5f;

        protected override void OnEnter()
        {
            idleStartTime = Time.time;
            LogState("Entering idle state");
        }

        protected override void OnUpdate()
        {
            // Randomly exit idle after some time
            if (Time.time - idleStartTime > maxIdleTime)
            {
                if (Random.value < 0.1f) // 10% chance per frame to exit idle
                {
                    var npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
                    if (npcBehavior?.PatrolPoints?.Count > 0)
                    {
                        RequestStateChange<PatrolState>();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Patrol state - NPC moves between patrol points
    /// </summary>
    public class PatrolState : AIState
    {
        private Vector3 currentPatrolTarget;
        private bool hasReachedTarget;
        private float waitStartTime;
        private bool isWaiting;
        private EnhancedNPCBehavior npcBehavior;

        public override float MinDuration => 1f;

        protected override void OnEnter()
        {
            npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
            currentPatrolTarget = npcBehavior.GetNextPatrolPoint();
            hasReachedTarget = false;
            isWaiting = false;
            
            LogState($"Patrolling to {currentPatrolTarget}");
        }

        protected override void OnUpdate()
        {
            if (npcBehavior == null) return;

            var transform = GetOwnerTransform();
            if (transform == null) return;

            if (!hasReachedTarget)
            {
                // Move towards patrol point
                float distance = Vector3.Distance(transform.position, currentPatrolTarget);
                if (distance <= 0.5f)
                {
                    hasReachedTarget = true;
                    isWaiting = true;
                    waitStartTime = Time.time;
                }
                else
                {
                    // Move towards target
                    Vector3 direction = (currentPatrolTarget - transform.position).normalized;
                    float moveSpeed = npcBehavior.MovementSpeed;
                    transform.position += direction * moveSpeed * Time.deltaTime;
                    
                    // Face movement direction
                    if (direction.magnitude > 0.01f)
                    {
                        transform.rotation = Quaternion.LookRotation(direction);
                    }
                }
            }
            else if (isWaiting)
            {
                // Wait at patrol point
                if (Time.time - waitStartTime >= npcBehavior.WaitTimeAtPatrolPoint)
                {
                    // Get next patrol point
                    currentPatrolTarget = npcBehavior.GetNextPatrolPoint();
                    hasReachedTarget = false;
                    isWaiting = false;
                }
            }
        }

        protected override void OnExit()
        {
            LogState("Exiting patrol state");
        }
    }

    /// <summary>
    /// Chase state - NPC pursues a target
    /// </summary>
    public class ChaseState : AIState
    {
        private EnhancedNPCBehavior npcBehavior;
        private float lastTargetSeen;
        private float maxChaseTime = 15f;

        protected override void OnEnter()
        {
            npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
            lastTargetSeen = Time.time;
            LogState("Starting chase");
        }

        protected override void OnUpdate()
        {
            if (npcBehavior?.CurrentTarget == null)
            {
                LogState("Lost target during chase");
                RequestStateChange<SearchState>();
                return;
            }

            var transform = GetOwnerTransform();
            if (transform == null) return;

            var target = npcBehavior.CurrentTarget;
            float distance = Vector3.Distance(transform.position, target.position);

            // Check if close enough to attack
            if (distance <= npcBehavior.AttackRange)
            {
                RequestStateChange<AttackState>();
                return;
            }

            // Check if we've been chasing too long
            if (Time.time - lastTargetSeen > maxChaseTime)
            {
                LogState("Chase timeout");
                RequestStateChange<SearchState>();
                return;
            }

            // Move towards target
            Vector3 direction = (target.position - transform.position).normalized;
            float moveSpeed = npcBehavior.MovementSpeed * 1.2f; // Move faster when chasing
            transform.position += direction * moveSpeed * Time.deltaTime;

            // Face target
            transform.rotation = Quaternion.LookRotation(direction);

            lastTargetSeen = Time.time;
        }

        protected override void OnExit()
        {
            LogState("Ending chase");
        }
    }

    /// <summary>
    /// Attack state - NPC attacks the target
    /// </summary>
    public class AttackState : AIState
    {
        private EnhancedNPCBehavior npcBehavior;
        private float attackStartTime;
        private bool hasAttacked;

        public override float MinDuration => 1f;

        protected override void OnEnter()
        {
            npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
            attackStartTime = Time.time;
            hasAttacked = false;
            LogState("Initiating attack");
        }

        protected override void OnUpdate()
        {
            if (npcBehavior?.CurrentTarget == null)
            {
                RequestStateChange<SearchState>();
                return;
            }

            var transform = GetOwnerTransform();
            if (transform == null) return;

            var target = npcBehavior.CurrentTarget;
            float distance = Vector3.Distance(transform.position, target.position);

            // Face the target
            Vector3 direction = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(direction);

            // Check if target moved out of range
            if (distance > npcBehavior.AttackRange * 1.2f)
            {
                RequestStateChange<ChaseState>();
                return;
            }

            // Perform attack after brief delay
            if (!hasAttacked && Time.time - attackStartTime >= 0.5f)
            {
                bool attackSuccess = npcBehavior.TryAttack();
                hasAttacked = true;
                
                if (attackSuccess)
                {
                    LogState("Attack executed");
                }
            }

            // Exit attack state after animation completes
            if (hasAttacked && Time.time - attackStartTime >= 1.5f)
            {
                if (distance <= npcBehavior.AttackRange)
                {
                    // Stay in attack state for continuous attacks
                    hasAttacked = false;
                    attackStartTime = Time.time;
                }
                else
                {
                    RequestStateChange<ChaseState>();
                }
            }
        }

        protected override void OnExit()
        {
            LogState("Attack completed");
        }
    }

    /// <summary>
    /// Search state - NPC looks for lost target
    /// </summary>
    public class SearchState : AIState
    {
        private Vector3 searchCenter;
        private Vector3 currentSearchPoint;
        private float searchRadius = 10f;
        private float searchStartTime;
        private float maxSearchTime = 10f;
        private int searchPointsChecked;
        private int maxSearchPoints = 5;
        private bool movingToSearchPoint;

        protected override void OnEnter()
        {
            var npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
            searchCenter = npcBehavior?.LastKnownTargetPosition ?? GetOwnerTransform().position;
            searchStartTime = Time.time;
            searchPointsChecked = 0;
            movingToSearchPoint = false;
            
            GenerateNextSearchPoint();
            LogState($"Searching around {searchCenter}");
        }

        protected override void OnUpdate()
        {
            var transform = GetOwnerTransform();
            if (transform == null) return;

            // Check if search time exceeded
            if (Time.time - searchStartTime > maxSearchTime || searchPointsChecked >= maxSearchPoints)
            {
                LogState("Search timeout or completed");
                RequestStateChange<PatrolState>();
                return;
            }

            if (!movingToSearchPoint)
            {
                GenerateNextSearchPoint();
                movingToSearchPoint = true;
            }

            // Move to current search point
            float distance = Vector3.Distance(transform.position, currentSearchPoint);
            if (distance <= 1f)
            {
                // Reached search point, look around briefly then generate new point
                searchPointsChecked++;
                movingToSearchPoint = false;
            }
            else
            {
                // Move towards search point
                Vector3 direction = (currentSearchPoint - transform.position).normalized;
                var npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
                float moveSpeed = npcBehavior?.MovementSpeed ?? 5f;
                transform.position += direction * moveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void GenerateNextSearchPoint()
        {
            // Generate a random point within search radius
            Vector2 randomCircle = Random.insideUnitCircle * searchRadius;
            currentSearchPoint = searchCenter + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        protected override void OnExit()
        {
            LogState("Ending search");
        }
    }

    /// <summary>
    /// Alert state - NPC is aware something is wrong but hasn't found target
    /// </summary>
    public class AlertState : AIState
    {
        private float alertStartTime;
        private float alertDuration = 5f;
        private Vector3 alertPosition;

        protected override void OnEnter()
        {
            alertStartTime = Time.time;
            alertPosition = GetOwnerTransform().position;
            LogState("Entering alert state");
        }

        protected override void OnUpdate()
        {
            // Look around while alert
            var transform = GetOwnerTransform();
            if (transform != null)
            {
                // Slowly rotate to scan area
                float rotationSpeed = 45f; // degrees per second
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
            }

            // Exit alert after duration
            if (Time.time - alertStartTime > alertDuration)
            {
                var npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
                npcBehavior?.ClearAlert();
                RequestStateChange<PatrolState>();
            }
        }

        protected override void OnExit()
        {
            LogState("Alert state ended");
        }
    }

    /// <summary>
    /// Flee state - NPC runs away from danger
    /// </summary>
    public class FleeState : AIState
    {
        private Vector3 fleeDirection;
        private float fleeStartTime;
        private float fleeDuration = 8f;

        protected override void OnEnter()
        {
            var npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
            var transform = GetOwnerTransform();
            
            if (npcBehavior?.CurrentTarget != null && transform != null)
            {
                // Flee in opposite direction from target
                fleeDirection = (transform.position - npcBehavior.CurrentTarget.position).normalized;
            }
            else
            {
                // Flee in random direction
                fleeDirection = Random.insideUnitSphere.normalized;
                fleeDirection.y = 0; // Keep on ground
            }

            fleeStartTime = Time.time;
            LogState("Fleeing from danger");
        }

        protected override void OnUpdate()
        {
            var transform = GetOwnerTransform();
            if (transform == null) return;

            // Move away from danger
            var npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
            float moveSpeed = (npcBehavior?.MovementSpeed ?? 5f) * 1.5f; // Move faster when fleeing
            transform.position += fleeDirection * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(fleeDirection);

            // Stop fleeing after duration or if health is better
            if (Time.time - fleeStartTime > fleeDuration)
            {
                var healthComponent = GetOwnerComponent<HealthComponentBase>();
                if (healthComponent?.HealthPercentage > 0.5f)
                {
                    RequestStateChange<PatrolState>();
                }
            }
        }

        protected override void OnExit()
        {
            LogState("Stopped fleeing");
        }
    }

    /// <summary>
    /// Dead state - NPC has died
    /// </summary>
    public class DeadState : AIState
    {
        public override bool CanBeInterrupted => false;

        protected override void OnEnter()
        {
            LogState("NPC has died");
            
            // Disable further AI processing
            var npcBehavior = GetOwnerComponent<EnhancedNPCBehavior>();
            if (npcBehavior != null)
            {
                npcBehavior.enabled = false;
            }

            // Play death animation if available
            var animator = GetOwnerComponent<Animator>();
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }

            // Disable colliders to prevent further interactions
            var colliders = GetOwnerGameObject().GetComponents<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
        }

        protected override void OnUpdate()
        {
            // Dead NPCs don't update
        }
    }

    #endregion

    #region Support Systems

    /// <summary>
    /// Handles NPC perception and target detection
    /// </summary>
    public class NPCPerceptionSystem
    {
        private EnhancedNPCBehavior npc;
        private float lastPerceptionUpdate;
        private float perceptionInterval = 0.2f;

        public NPCPerceptionSystem(EnhancedNPCBehavior owner)
        {
            npc = owner;
        }

        public void UpdatePerception()
        {
            if (Time.time - lastPerceptionUpdate < perceptionInterval) return;
            lastPerceptionUpdate = Time.time;

            DetectTargets();
        }

        private void DetectTargets()
        {
            var detectionCenter = npc.DetectionCenter ? npc.DetectionCenter : npc.transform;
            var colliders = Physics.OverlapSphere(detectionCenter.position, npc.DetectionRange, npc.TargetLayers);

            Transform bestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                if (collider.transform == npc.transform) continue;

                // Check line of sight
                Vector3 directionToTarget = collider.transform.position - detectionCenter.position;
                if (Physics.Raycast(detectionCenter.position, directionToTarget.normalized, 
                    directionToTarget.magnitude, npc.ObstacleLayers))
                {
                    continue; // Line of sight blocked
                }

                // Find closest target
                float distance = directionToTarget.magnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    bestTarget = collider.transform;
                }
            }

            if (bestTarget != npc.CurrentTarget)
            {
                npc.SetTarget(bestTarget);
            }
        }
    }

    /// <summary>
    /// Handles NPC combat mechanics
    /// </summary>
    public class NPCCombatSystem
    {
        private EnhancedNPCBehavior npc;
        private bool shouldTriggerAttackAnimation;

        public NPCCombatSystem(EnhancedNPCBehavior owner)
        {
            npc = owner;
        }

        public void PerformAttack(Transform target, float damage)
        {
            if (target == null) return;

            // Apply damage if target has health component
            var healthComponent = target.GetComponent<HealthComponentBase>();
            if (healthComponent != null)
            {
                var damageRequest = new DamageRequest(damage, npc.gameObject);
                healthComponent.TakeDamage(damageRequest);
            }

            shouldTriggerAttackAnimation = true;
        }

        public bool ShouldTriggerAttackAnimation()
        {
            if (shouldTriggerAttackAnimation)
            {
                shouldTriggerAttackAnimation = false;
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Handles NPC movement mechanics
    /// </summary>
    public class NPCMovementSystem
    {
        private EnhancedNPCBehavior npc;
        private Vector3 lastPosition;
        private float currentSpeed;

        public bool IsMoving { get; private set; }
        public float CurrentSpeed => currentSpeed;

        public NPCMovementSystem(EnhancedNPCBehavior owner)
        {
            npc = owner;
            lastPosition = npc.transform.position;
        }

        public void UpdateMovement()
        {
            Vector3 currentPosition = npc.transform.position;
            Vector3 movement = currentPosition - lastPosition;
            currentSpeed = movement.magnitude / Time.deltaTime;
            IsMoving = currentSpeed > 0.1f;
            lastPosition = currentPosition;
        }
    }

    /// <summary>
    /// Scales NPC attributes based on difficulty
    /// </summary>
    public class NPCDifficultyScaler
    {
        private readonly Dictionary<NPCDifficulty, NPCDifficultyScaling> difficultyScalings = new()
        {
            [NPCDifficulty.Easy] = new NPCDifficultyScaling
            {
                HealthMultiplier = 0.7f,
                DamageMultiplier = 0.7f,
                SpeedMultiplier = 0.8f,
                DetectionRangeMultiplier = 0.8f,
                AttackSpeedMultiplier = 0.8f,
                PerceptionSpeedMultiplier = 0.7f,
                AccuracyMultiplier = 0.7f
            },
            [NPCDifficulty.Normal] = new NPCDifficultyScaling
            {
                HealthMultiplier = 1.0f,
                DamageMultiplier = 1.0f,
                SpeedMultiplier = 1.0f,
                DetectionRangeMultiplier = 1.0f,
                AttackSpeedMultiplier = 1.0f,
                PerceptionSpeedMultiplier = 1.0f,
                AccuracyMultiplier = 1.0f
            },
            [NPCDifficulty.Hard] = new NPCDifficultyScaling
            {
                HealthMultiplier = 1.3f,
                DamageMultiplier = 1.2f,
                SpeedMultiplier = 1.1f,
                DetectionRangeMultiplier = 1.2f,
                AttackSpeedMultiplier = 1.2f,
                PerceptionSpeedMultiplier = 1.3f,
                AccuracyMultiplier = 1.2f
            },
            [NPCDifficulty.Expert] = new NPCDifficultyScaling
            {
                HealthMultiplier = 1.6f,
                DamageMultiplier = 1.4f,
                SpeedMultiplier = 1.2f,
                DetectionRangeMultiplier = 1.4f,
                AttackSpeedMultiplier = 1.4f,
                PerceptionSpeedMultiplier = 1.5f,
                AccuracyMultiplier = 1.4f
            },
            [NPCDifficulty.Nightmare] = new NPCDifficultyScaling
            {
                HealthMultiplier = 2.0f,
                DamageMultiplier = 1.7f,
                SpeedMultiplier = 1.4f,
                DetectionRangeMultiplier = 1.6f,
                AttackSpeedMultiplier = 1.6f,
                PerceptionSpeedMultiplier = 2.0f,
                AccuracyMultiplier = 1.6f
            }
        };

        public NPCDifficultyScaling GetScalingForDifficulty(NPCDifficulty difficulty)
        {
            return difficultyScalings.TryGetValue(difficulty, out var scaling) ? 
                scaling : difficultyScalings[NPCDifficulty.Normal];
        }
    }

    #endregion

    #region Behavior Tree Action Nodes

    /// <summary>
    /// Behavior tree node for NPC attacks
    /// </summary>
    public class AttackActionNode : ActionNode
    {
        private EnhancedNPCBehavior npc;

        public AttackActionNode(EnhancedNPCBehavior owner)
        {
            npc = owner;
        }

        protected override void OnActionStart()
        {
            // Prepare for attack
        }

        protected override BehaviorTreeStatus OnActionUpdate()
        {
            if (npc.TryAttack())
            {
                return BehaviorTreeStatus.Success;
            }
            return BehaviorTreeStatus.Failure;
        }
    }

    /// <summary>
    /// Behavior tree node for NPC patrol
    /// </summary>
    public class PatrolActionNode : ActionNode
    {
        private EnhancedNPCBehavior npc;
        private Vector3 targetPatrolPoint;
        private bool hasTarget;

        public PatrolActionNode(EnhancedNPCBehavior owner)
        {
            npc = owner;
        }

        protected override void OnActionStart()
        {
            targetPatrolPoint = npc.GetNextPatrolPoint();
            hasTarget = true;
        }

        protected override BehaviorTreeStatus OnActionUpdate()
        {
            if (!hasTarget) return BehaviorTreeStatus.Failure;

            float distance = Vector3.Distance(npc.transform.position, targetPatrolPoint);
            if (distance <= 0.5f)
            {
                return BehaviorTreeStatus.Success;
            }

            // Move towards patrol point
            Vector3 direction = (targetPatrolPoint - npc.transform.position).normalized;
            npc.transform.position += direction * npc.MovementSpeed * Time.deltaTime;

            return BehaviorTreeStatus.Running;
        }
    }

    #endregion
}