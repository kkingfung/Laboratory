using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Health;
using Laboratory.Subsystems.EnemyAI.NPC;
using Laboratory.EnemyAI;
using Laboratory.Subsystems.EnemyAI;

namespace Laboratory.Subsystems.EnemyAI
{
    #region Behavior Tree Action Nodes

    /// <summary>
    /// Behavior tree node for NPC attacks
    /// </summary>
    public class AttackActionNode : ActionNode
    {
        private UnifiedNPCBehavior npc;

        public AttackActionNode(UnifiedNPCBehavior owner)
        {
            npc = owner;
        }

        public override BehaviorTreeStatus Execute()
        {
            if (npc != null && npc.TryAttack())
            {
                return BehaviorTreeStatus.Success;
            }
            return BehaviorTreeStatus.Failure;
        }
    }

    /// <summary>
    /// Behavior tree node for NPC patrol behavior
    /// </summary>
    public class PatrolActionNode : ActionNode
    {
        private UnifiedNPCBehavior npc;
        private Vector3 currentTarget;
        private bool hasTarget;

        public PatrolActionNode(UnifiedNPCBehavior owner)
        {
            npc = owner;
        }

        public override BehaviorTreeStatus Execute()
        {
            if (npc == null) return BehaviorTreeStatus.Failure;

            if (!hasTarget)
            {
                currentTarget = npc.GetNextPatrolPoint();
                hasTarget = true;
            }

            // Move towards patrol point
            float distance = Vector3.Distance(npc.transform.position, currentTarget);
            if (distance <= 0.5f)
            {
                hasTarget = false;
                return BehaviorTreeStatus.Success;
            }

            // Move towards target
            Vector3 direction = (currentTarget - npc.transform.position).normalized;
            npc.transform.position += direction * npc.MovementSpeed * Time.deltaTime;
            npc.transform.rotation = Quaternion.LookRotation(direction);

            return BehaviorTreeStatus.Running;
        }
    }

    #endregion

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
                    var npcBehavior = GetOwnerComponent<UnifiedNPCBehavior>();
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
        private UnifiedNPCBehavior npcBehavior;

        public override float MinDuration => 1f;

        protected override void OnEnter()
        {
            npcBehavior = GetOwnerComponent<UnifiedNPCBehavior>();
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
        private UnifiedNPCBehavior npcBehavior;
        private float lastTargetSeen;
        private float maxChaseTime = 15f;

        protected override void OnEnter()
        {
            npcBehavior = GetOwnerComponent<UnifiedNPCBehavior>();
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
            if (distance <= npcBehavior.Stats.attackRange)
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
        private UnifiedNPCBehavior npcBehavior;
        private float attackStartTime;
        private bool hasAttacked;

        public override float MinDuration => 1f;

        protected override void OnEnter()
        {
            npcBehavior = GetOwnerComponent<UnifiedNPCBehavior>();
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
            if (distance > npcBehavior.Stats.attackRange * 1.2f)
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
                if (distance <= npcBehavior.Stats.attackRange)
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
            var npcBehavior = GetOwnerComponent<UnifiedNPCBehavior>();
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
                var npcBehavior = GetOwnerComponent<UnifiedNPCBehavior>();
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

    #endregion

    #region Support Classes

    /// <summary>
    /// NPC difficulty scaling data
    /// </summary>
    [System.Serializable]
    public class NPCDifficultyScaling
    {
        public float HealthMultiplier = 1.0f;
        public float DamageMultiplier = 1.0f;
        public float SpeedMultiplier = 1.0f;
        public float DetectionRangeMultiplier = 1.0f;
        public float AttackSpeedMultiplier = 1.0f;
        public float PerceptionSpeedMultiplier = 1.0f;
        public float AccuracyMultiplier = 1.0f;
    }

    #endregion
}
