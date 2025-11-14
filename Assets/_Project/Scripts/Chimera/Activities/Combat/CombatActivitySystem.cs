using Unity.Entities;
using UnityEngine;

namespace Laboratory.Chimera.Activities.Combat
{
    /// <summary>
    /// ECS system that registers and manages combat activity
    /// Integrates with the core ActivitySystem
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ActivitySystem))]
    public partial class CombatActivitySystem : SystemBase
    {
        private CombatActivity _combatActivity;
        private CombatConfig _combatConfig;
        private bool _isInitialized;

        protected override void OnCreate()
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
        /// </summary>
        public Entity CreateCombatRequest(Entity monsterEntity, ActivityDifficulty difficulty)
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

        /// <summary>
        /// Creates a tournament request (multi-round combat)
        /// </summary>
        public Entity CreateTournamentRequest(Entity monsterEntity, ActivityDifficulty difficulty)
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
