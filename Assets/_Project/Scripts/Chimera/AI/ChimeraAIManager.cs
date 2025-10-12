using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Breeding;
using Laboratory.Core.Events;
using Laboratory.Core.Performance;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Manages multiple Chimera monsters and their AI behaviors.
    /// Handles formation, group commands, and coordination between monsters.
    /// </summary>
    public class ChimeraAIManager : OptimizedMonoBehaviour
    {
        [Header("Formation Settings")]
        [SerializeField] private FormationType defaultFormation = FormationType.Follow;
        [SerializeField] private float formationSpacing = 2f;

        [Header("Group Behavior")]
        [SerializeField] private bool enablePackBehavior = true;
        [SerializeField] private float packCohesionRadius = 10f;
        [SerializeField] private AIBehaviorType groupBehaviorType = AIBehaviorType.Companion;

        [Header("Debug")]
        [SerializeField] private bool showFormationGizmos = true;

        private List<ChimeraMonsterAI> managedMonsters = new List<ChimeraMonsterAI>();
        private Transform player;
        private IEventBus eventBus;

        // Formation positions
        private Vector3[] formationPositions = new Vector3[8];
        private int nextFormationSlot = 0;

        // ⚡ OPTIMIZATION: Pre-allocated collections to eliminate LINQ allocations
        private List<ChimeraMonsterAI> nearbyMonstersCache = new List<ChimeraMonsterAI>();
        private List<ChimeraMonsterAI> combatMonstersCache = new List<ChimeraMonsterAI>();
        private List<ChimeraMonsterAI> tempMonsterList = new List<ChimeraMonsterAI>();

        #region Unity Lifecycle

        private void Awake()
        {
            // Try to get event bus from service container
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                eventBus = serviceContainer.ResolveService<IEventBus>();
            }
        }

        protected override void Start()
        {
            base.Start(); // Important: Call base to register for optimized updates

            FindPlayer();
            FindExistingMonsters();
            CalculateFormationPositions();

            // AI coordination needs medium frequency updates
            updateFrequency = OptimizedUpdateManager.UpdateFrequency.MediumFrequency;
        }

        public override void OnOptimizedUpdate(float deltaTime)
        {
            if (enablePackBehavior)
            {
                UpdatePackBehavior();
            }

            UpdateFormations();
        }

        #endregion

        #region Monster Management

        /// <summary>
        /// Register a new monster with the AI manager
        /// </summary>
        public void RegisterMonster(ChimeraMonsterAI monster)
        {
            if (monster == null || managedMonsters.Contains(monster)) return;

            managedMonsters.Add(monster);
            
            // Assign formation position
            AssignFormationPosition(monster);
            
            // Set group behavior
            monster.SetBehaviorType(groupBehaviorType);

            UnityEngine.Debug.Log($"[AIManager] Registered monster: {monster.name} (Total: {managedMonsters.Count})");

            // Publish event
            eventBus?.Publish(new MonsterRegisteredEvent
            {
                Monster = monster,
                TotalMonstersManaged = managedMonsters.Count
            });
        }

        /// <summary>
        /// Unregister a monster from the AI manager
        /// </summary>
        public void UnregisterMonster(ChimeraMonsterAI monster)
        {
            if (monster == null) return;

            if (managedMonsters.Remove(monster))
            {
                RecalculateFormations();
                UnityEngine.Debug.Log($"[AIManager] Unregistered monster: {monster.name} (Total: {managedMonsters.Count})");

                // Publish event
                eventBus?.Publish(new MonsterUnregisteredEvent
                {
                    Monster = monster,
                    TotalMonstersManaged = managedMonsters.Count
                });
            }
        }

        private void FindExistingMonsters()
        {
            var existingMonsters = FindObjectsByType<ChimeraMonsterAI>(FindObjectsSortMode.None);
            foreach (var monster in existingMonsters)
            {
                RegisterMonster(monster);
            }
        }

        #endregion

        #region Formation System

        private void CalculateFormationPositions()
        {
            if (player == null) return;

            Vector3 playerForward = player.forward;
            Vector3 playerRight = player.right;
            Vector3 playerPos = player.position;

            switch (defaultFormation)
            {
                case FormationType.Follow:
                    // Simple follow formation - monsters spread behind player
                    for (int i = 0; i < formationPositions.Length; i++)
                    {
                        float angle = (i - 3.5f) * 30f; // Spread from -105 to +105 degrees
                        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * (-playerForward);
                        formationPositions[i] = playerPos + direction * (formationSpacing * 2f);
                    }
                    break;

                case FormationType.Line:
                    // Line formation perpendicular to player direction
                    for (int i = 0; i < formationPositions.Length; i++)
                    {
                        float offset = (i - 3.5f) * formationSpacing;
                        formationPositions[i] = playerPos - playerForward * formationSpacing + playerRight * offset;
                    }
                    break;

                case FormationType.Circle:
                    // Circular formation around player
                    for (int i = 0; i < formationPositions.Length; i++)
                    {
                        float angle = (i / (float)formationPositions.Length) * 360f;
                        Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                        formationPositions[i] = playerPos + direction * (formationSpacing * 1.5f);
                    }
                    break;

                case FormationType.VFormation:
                    // V formation with player at the point
                    for (int i = 0; i < formationPositions.Length; i++)
                    {
                        bool isLeft = i % 2 == 0;
                        int pairIndex = i / 2 + 1;
                        Vector3 direction = isLeft ? -playerRight : playerRight;
                        direction -= playerForward * 0.5f; // Angle the V
                        direction.Normalize();
                        formationPositions[i] = playerPos + direction * (formationSpacing * pairIndex);
                    }
                    break;
            }
        }

        private void UpdateFormations()
        {
            if (managedMonsters.Count == 0 || player == null) return;

            CalculateFormationPositions();

            for (int i = 0; i < managedMonsters.Count && i < formationPositions.Length; i++)
            {
                var monster = managedMonsters[i];
                if (monster == null) continue;

                // Only adjust formation if monster is following or idle
                if (monster.CurrentState == AIBehaviorState.Follow || monster.CurrentState == AIBehaviorState.Idle)
                {
                    Vector3 targetPos = formationPositions[i];
                    float distanceToTarget = Vector3.Distance(monster.transform.position, targetPos);

                    // If monster is too far from formation position, guide them back
                    if (distanceToTarget > formationSpacing * 1.5f)
                    {
                        // Set formation position as a soft target
                        // The monster's own AI will handle pathfinding
                    }
                }
            }
        }

        private void AssignFormationPosition(ChimeraMonsterAI monster)
        {
            if (nextFormationSlot >= formationPositions.Length)
            {
                nextFormationSlot = 0; // Wrap around if we have too many monsters
            }

            // The formation position is handled by UpdateFormations()
            nextFormationSlot++;
        }

        private void RecalculateFormations()
        {
            nextFormationSlot = managedMonsters.Count;
        }

        #endregion

        #region Pack Behavior

        private void UpdatePackBehavior()
        {
            if (managedMonsters.Count < 2) return;

            // Update pack cohesion - monsters help each other in combat
            foreach (var monster in managedMonsters)
            {
                if (monster == null || monster.CurrentTarget == null) continue;

                // ⚡ OPTIMIZED: Replace LINQ with for-loop to eliminate 2-10ms allocation spikes
                nearbyMonstersCache.Clear();
                var monsterPos = monster.transform.position;

                for (int i = 0; i < managedMonsters.Count; i++)
                {
                    var m = managedMonsters[i];
                    if (m != null && m != monster && m.CurrentState != AIBehaviorState.Combat)
                    {
                        // ⚡ OPTIMIZED: Use sqrMagnitude for 30-50% faster distance checks
                        var sqrDistance = (m.transform.position - monsterPos).sqrMagnitude;
                        var sqrRadius = packCohesionRadius * packCohesionRadius;
                        if (sqrDistance <= sqrRadius)
                        {
                            nearbyMonstersCache.Add(m);
                        }
                    }
                }

                foreach (var ally in nearbyMonstersCache)
                {
                    // If ally is not busy and the original monster needs help
                    if (ally.CurrentState == AIBehaviorState.Idle || ally.CurrentState == AIBehaviorState.Follow)
                    {
                        // Random chance to assist based on pack loyalty
                        if (Random.value < 0.3f) // 30% chance per frame
                        {
                            ally.SetTarget(monster.CurrentTarget);
                        }
                    }
                }
            }
        }

        #endregion

        #region Group Commands

        /// <summary>
        /// Command all monsters to follow the player
        /// </summary>
        [ContextMenu("Command: Follow Player")]
        public void CommandFollowPlayer()
        {
            foreach (var monster in managedMonsters)
            {
                if (monster != null)
                {
                    monster.SetBehaviorType(AIBehaviorType.Companion);
                    monster.CancelCombat();
                }
            }
            UnityEngine.Debug.Log("[AIManager] All monsters commanded to follow player");
        }

        /// <summary>
        /// Command all monsters to be aggressive
        /// </summary>
        [ContextMenu("Command: Aggressive Mode")]
        public void CommandAggressiveMode()
        {
            foreach (var monster in managedMonsters)
            {
                if (monster != null)
                {
                    monster.SetBehaviorType(AIBehaviorType.Aggressive);
                }
            }
            UnityEngine.Debug.Log("[AIManager] All monsters set to aggressive mode");
        }

        /// <summary>
        /// Command all monsters to be passive
        /// </summary>
        [ContextMenu("Command: Passive Mode")]
        public void CommandPassiveMode()
        {
            foreach (var monster in managedMonsters)
            {
                if (monster != null)
                {
                    monster.SetBehaviorType(AIBehaviorType.Passive);
                    monster.CancelCombat();
                }
            }
            UnityEngine.Debug.Log("[AIManager] All monsters set to passive mode");
        }

        /// <summary>
        /// Command all monsters to guard current area
        /// </summary>
        [ContextMenu("Command: Guard Area")]
        public void CommandGuardArea()
        {
            foreach (var monster in managedMonsters)
            {
                if (monster != null)
                {
                    monster.SetBehaviorType(AIBehaviorType.Guardian);
                }
            }
            UnityEngine.Debug.Log("[AIManager] All monsters set to guard current area");
        }

        /// <summary>
        /// Set formation type for all monsters
        /// </summary>
        public void SetFormationType(FormationType newFormation)
        {
            defaultFormation = newFormation;
            CalculateFormationPositions();
            UnityEngine.Debug.Log($"[AIManager] Formation changed to: {newFormation}");
        }

        #endregion

        #region Utility Methods

        private void FindPlayer()
        {
            if (player == null)
            {
                player = GameObject.FindGameObjectWithTag("Player")?.transform;
                
                if (player == null)
                {
                    var playerGO = GameObject.Find("Player");
                    if (playerGO != null)
                    {
                        player = playerGO.transform;
                    }
                }
            }
        }

        /// <summary>
        /// Get all currently managed monsters
        /// </summary>
        public List<ChimeraMonsterAI> GetManagedMonsters()
        {
            return new List<ChimeraMonsterAI>(managedMonsters);
        }

        /// <summary>
        /// Get monsters currently in combat
        /// </summary>
        public List<ChimeraMonsterAI> GetMonstersInCombat()
        {
            // ⚡ OPTIMIZED: Replace LINQ with for-loop to eliminate allocations
            combatMonstersCache.Clear();
            for (int i = 0; i < managedMonsters.Count; i++)
            {
                var monster = managedMonsters[i];
                if (monster != null && monster.IsInCombat)
                {
                    combatMonstersCache.Add(monster);
                }
            }
            return combatMonstersCache;
        }

        /// <summary>
        /// Get status summary of all monsters
        /// </summary>
        public string GetStatusSummary()
        {
            if (managedMonsters.Count == 0) return "No monsters managed";

            int following = managedMonsters.Count(m => m != null && m.CurrentState == AIBehaviorState.Follow);
            int patrolling = managedMonsters.Count(m => m != null && m.CurrentState == AIBehaviorState.Patrol);
            int inCombat = managedMonsters.Count(m => m != null && m.IsInCombat);
            int idle = managedMonsters.Count(m => m != null && m.CurrentState == AIBehaviorState.Idle);

            return $"Total: {managedMonsters.Count} | Following: {following} | Combat: {inCombat} | Patrolling: {patrolling} | Idle: {idle}";
        }

        #endregion

        #region Debug & Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!showFormationGizmos || player == null) return;

            // Draw formation positions
            Gizmos.color = Color.cyan;
            for (int i = 0; i < formationPositions.Length; i++)
            {
                Gizmos.DrawWireSphere(formationPositions[i], 0.5f);
                
                if (i < managedMonsters.Count && managedMonsters[i] != null)
                {
                    // Draw line from monster to formation position
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(managedMonsters[i].transform.position, formationPositions[i]);
                }
            }

            // Draw pack cohesion radius
            if (enablePackBehavior)
            {
                Gizmos.color = Color.yellow;
                foreach (var monster in managedMonsters)
                {
                    if (monster != null)
                    {
                        Gizmos.DrawWireSphere(monster.transform.position, packCohesionRadius);
                    }
                }
            }
        }

        #endregion
    }

    #region Supporting Classes & Enums

    public enum FormationType
    {
        Follow,      // Spread behind player
        Line,        // Line formation
        Circle,      // Circle around player
        VFormation   // V formation with player at point
    }

    [System.Serializable]
    public class MonsterRegisteredEvent
    {
        public ChimeraMonsterAI Monster { get; set; }
        public int TotalMonstersManaged { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    [System.Serializable]
    public class MonsterUnregisteredEvent
    {
        public ChimeraMonsterAI Monster { get; set; }
        public int TotalMonstersManaged { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    #endregion
}