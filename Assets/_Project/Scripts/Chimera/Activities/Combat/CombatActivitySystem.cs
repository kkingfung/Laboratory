using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace Laboratory.Chimera.Activities.Combat
{
    /// <summary>
    /// ECS system that registers and manages combat activity
    /// Integrates with the core ActivitySystem
    /// Performance: Lightweight registration system with tournament support
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ActivitySystem))]
    public partial class CombatActivitySystem : SystemBase
    {
        private CombatActivity _combatActivity;
        private CombatConfig _combatConfig;
        private bool _isInitialized;

        private static readonly ProfilerMarker s_InitializationMarker =
            new ProfilerMarker("CombatActivity.Initialize");
        private static readonly ProfilerMarker s_CreateRequestMarker =
            new ProfilerMarker("CombatActivity.CreateRequest");
        private static readonly ProfilerMarker s_CreateTournamentMarker =
            new ProfilerMarker("CombatActivity.CreateTournament");

        protected override void OnCreate()
        {
            using (s_InitializationMarker.Auto())
            {
                // Load combat configuration
                _combatConfig = Resources.Load<CombatConfig>("Configs/Activities/CombatConfig");

                if (_combatConfig == null)
                {
                    Debug.LogWarning("CombatConfig not found at Resources/Configs/Activities/CombatConfig. " +
                                    "Combat activity will use default settings.");
                    return;
                }

                // Create combat activity implementation
                _combatActivity = new CombatActivity(_combatConfig);

                Debug.Log($"Combat Activity System initialized: {_combatConfig.activityName}");
                _isInitialized = true;
            }
        }

        protected override void OnStartRunning()
        {
            if (!_isInitialized || _combatActivity == null)
                return;

            // Register combat activity with the core activity system
            var activitySystem = World.GetExistingSystemManaged<ActivitySystem>();
            if (activitySystem != null)
            {
                activitySystem.RegisterActivity(ActivityType.Combat, _combatActivity);
                Debug.Log("Combat activity registered with ActivitySystem");
            }
            else
            {
                Debug.LogError("ActivitySystem not found. Combat activity cannot be registered.");
            }
        }

        protected override void OnUpdate()
        {
            // This system primarily handles initialization and registration
            // The core ActivitySystem handles the actual activity execution
            // Future: Could add combat-specific effects, animations, or tournaments here
        }

        /// <summary>
        /// Gets the combat configuration (for external systems to query)
        /// </summary>
        public CombatConfig GetConfig()
        {
            return _combatConfig;
        }

        /// <summary>
        /// Creates a combat activity request entity
        /// Performance: O(1) entity creation with minimal allocations
        /// </summary>
        public Entity CreateCombatRequest(Entity monsterEntity, ActivityDifficulty difficulty)
        {
            using (s_CreateRequestMarker.Auto())
            {
                var requestEntity = EntityManager.CreateEntity();

                EntityManager.AddComponentData(requestEntity, new StartActivityRequest
                {
                    monsterEntity = monsterEntity,
                    activityType = ActivityType.Combat,
                    difficulty = difficulty,
                    requestTime = (float)SystemAPI.Time.ElapsedTime
                });

                Debug.Log($"Created combat request for monster {monsterEntity.Index} at difficulty {difficulty}");

                return requestEntity;
            }
        }

        /// <summary>
        /// Creates a tournament request (multi-round combat)
        /// Performance: Delegates to CreateCombatRequest for efficient request creation
        /// </summary>
        public Entity CreateTournamentRequest(Entity monsterEntity, ActivityDifficulty difficulty)
        {
            using (s_CreateTournamentMarker.Auto())
            {
                if (!_combatConfig.enableTournaments)
                {
                    Debug.LogWarning("Tournaments are disabled in config");
                    return Entity.Null;
                }

                // Create standard combat request (tournament logic handled separately)
                return CreateCombatRequest(monsterEntity, difficulty);
            }
        }
    }
}
