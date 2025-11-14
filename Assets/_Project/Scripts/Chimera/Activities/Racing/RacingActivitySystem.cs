using Unity.Entities;
using Unity.Profiling;
using UnityEngine;

namespace Laboratory.Chimera.Activities.Racing
{
    /// <summary>
    /// ECS system that registers and manages racing activity
    /// Integrates with the core ActivitySystem
    /// Performance: Lightweight registration system with minimal overhead
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(ActivitySystem))]
    public partial class RacingActivitySystem : SystemBase
    {
        private RacingActivity _racingActivity;
        private RacingConfig _racingConfig;
        private bool _isInitialized;

        private static readonly ProfilerMarker s_InitializationMarker =
            new ProfilerMarker("RacingActivity.Initialize");
        private static readonly ProfilerMarker s_CreateRequestMarker =
            new ProfilerMarker("RacingActivity.CreateRequest");

        protected override void OnCreate()
        {
            using (s_InitializationMarker.Auto())
            {
                // Load racing configuration
                _racingConfig = Resources.Load<RacingConfig>("Configs/Activities/RacingConfig");

                if (_racingConfig == null)
                {
                    Debug.LogWarning("RacingConfig not found at Resources/Configs/Activities/RacingConfig. " +
                                    "Racing activity will use default settings.");
                    return;
                }

                // Create racing activity implementation
                _racingActivity = new RacingActivity(_racingConfig);

                Debug.Log($"Racing Activity System initialized: {_racingConfig.activityName}");
                _isInitialized = true;
            }
        }

        protected override void OnStartRunning()
        {
            if (!_isInitialized || _racingActivity == null)
                return;

            // Register racing activity with the core activity system
            var activitySystem = World.GetExistingSystemManaged<ActivitySystem>();
            if (activitySystem != null)
            {
                activitySystem.RegisterActivity(ActivityType.Racing, _racingActivity);
                Debug.Log("Racing activity registered with ActivitySystem");
            }
            else
            {
                Debug.LogError("ActivitySystem not found. Racing activity cannot be registered.");
            }
        }

        protected override void OnUpdate()
        {
            // This system primarily handles initialization and registration
            // The core ActivitySystem handles the actual activity execution
            // Future: Could add racing-specific visualization or effects here
        }

        /// <summary>
        /// Gets the racing configuration (for external systems to query)
        /// </summary>
        public RacingConfig GetConfig()
        {
            return _racingConfig;
        }

        /// <summary>
        /// Creates a racing activity request entity
        /// Performance: O(1) entity creation with minimal allocations
        /// </summary>
        public Entity CreateRacingRequest(Entity monsterEntity, ActivityDifficulty difficulty)
        {
            using (s_CreateRequestMarker.Auto())
            {
                var requestEntity = EntityManager.CreateEntity();

                EntityManager.AddComponentData(requestEntity, new StartActivityRequest
                {
                    monsterEntity = monsterEntity,
                    activityType = ActivityType.Racing,
                    difficulty = difficulty,
                    requestTime = (float)SystemAPI.Time.ElapsedTime
                });

                Debug.Log($"Created racing request for monster {monsterEntity.Index} at difficulty {difficulty}");

                return requestEntity;
            }
        }
    }
}
