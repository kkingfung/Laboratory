using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.Configuration;

namespace Laboratory.Core.Activities
{
    /// <summary>
    /// Core Activity Center System - Where Monsters Participate in Genre Activities
    /// FEATURES: Racing, Combat, Puzzles, Strategy, Music, Adventure, Platforming, Crafting
    /// PERFORMANCE: Batch processing for 100+ concurrent activities
    /// INTEGRATION: Works with genetics to determine monster performance
    /// </summary>



    #region Core Activity System

    /// <summary>
    /// Main activity center management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ActivityCenterSystem : SystemBase
    {
        private EntityQuery participantQuery;
        private EntityQuery centerQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            participantQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<ActivityParticipantComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>(),
                ComponentType.ReadOnly<CreatureIdentityComponent>(),
                ComponentType.ReadOnly<ActivityPerformanceComponent>()
            });

            centerQuery = GetEntityQuery(ComponentType.ReadWrite<ActivityCenterComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            RequireForUpdate(participantQuery);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update activity participation
            var participationJob = new ActivityParticipationJob
            {
                DeltaTime = deltaTime,
                CurrentTime = currentTime,
                CommandBuffer = ecb
            };

            Dependency = participationJob.ScheduleParallel(participantQuery, Dependency);
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }

    // Note: This is a partial declaration of ActivityParticipationJob
    // The main implementation with full logic is in ActivityParticipationJob.cs
    [BurstCompile]
    public partial struct ActivityParticipationJob : IJobEntity
    {
        public float DeltaTime;
        public float CurrentTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
            ref ActivityParticipantComponent participant,
            RefRO<GeneticDataComponent> genetics,
            RefRO<CreatureIdentityComponent> identity,
            RefRO<ActivityPerformanceComponent> performance)
        {
            if (participant.Status == ActivityStatus.NotParticipating)
                return;

            // Update activity progress
            participant.TimeInActivity += DeltaTime;

            switch (participant.Status)
            {
                case ActivityStatus.Queued:
                    // Auto-start if ready
                    if (participant.TimeInActivity > 2f) // 2 second queue time
                    {
                        participant.Status = ActivityStatus.Warming_Up;
                        participant.TimeInActivity = 0f;
                    }
                    break;

                case ActivityStatus.Warming_Up:
                    // Prepare for activity
                    if (participant.TimeInActivity > 3f) // 3 second warm-up
                    {
                        participant.Status = ActivityStatus.Active;
                        participant.TimeInActivity = 0f;
                        participant.ActivityProgress = 0f;
                    }
                    break;

                case ActivityStatus.Active:
                    // Calculate performance based on genetics and activity type
                    float performanceMultiplier = CalculatePerformanceMultiplier(participant.CurrentActivity, genetics.ValueRO);
                    float progressRate = performanceMultiplier * DeltaTime;

                    participant.ActivityProgress += progressRate;
                    participant.PerformanceScore = performanceMultiplier;

                    // Check for completion
                    if (participant.ActivityProgress >= 1f)
                    {
                        participant.Status = ActivityStatus.Completed;
                        participant.ExperienceGained = CalculateExperienceGained(performanceMultiplier, participant.CurrentActivity);
                        participant.HasRewards = true;
                    }
                    break;

                case ActivityStatus.Completed:
                    if (!participant.HasRewards)
                    {
                        participant.Status = ActivityStatus.Rewarded;
                        // Award experience and rewards would happen here
                    }
                    break;
            }
        }


        private float CalculatePerformanceMultiplier(ActivityType activity, GeneticDataComponent genetics)
        {
            return activity switch
            {
                ActivityType.Racing => (genetics.Speed * 0.4f + genetics.Stamina * 0.3f + genetics.Agility * 0.3f),
                ActivityType.Combat => (genetics.Aggression * 0.5f + genetics.Size * 0.3f + genetics.Dominance * 0.2f),
                ActivityType.Puzzle => (genetics.Intelligence * 0.7f + genetics.Curiosity * 0.3f),
                ActivityType.Strategy => (genetics.Intelligence * 0.6f + genetics.Caution * 0.4f),
                ActivityType.Music => (genetics.Intelligence * 0.4f + genetics.Sociability * 0.6f),
                ActivityType.Adventure => (genetics.Curiosity * 0.4f + genetics.Adaptability * 0.3f + genetics.Stamina * 0.3f),
                ActivityType.Platforming => (genetics.Agility * 0.6f + genetics.Intelligence * 0.4f),
                ActivityType.Crafting => (genetics.Intelligence * 0.5f + genetics.Adaptability * 0.5f),
                _ => 0.5f
            };
        }


        private int CalculateExperienceGained(float performance, ActivityType activity)
        {
            float baseExp = activity switch
            {
                ActivityType.Racing => 10f,
                ActivityType.Combat => 15f,
                ActivityType.Puzzle => 12f,
                ActivityType.Strategy => 18f,
                ActivityType.Music => 8f,
                ActivityType.Adventure => 20f,
                ActivityType.Platforming => 10f,
                ActivityType.Crafting => 14f,
                _ => 10f
            };

            return (int)(baseExp * performance * UnityEngine.Random.Range(0.8f, 1.2f));
        }
    }

    #endregion

    #region Specialized Activity Systems

    /// <summary>
    /// Racing Circuit System - High-speed competitive racing
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class RacingActivitySystem : SystemBase
    {
        private EntityQuery racingQuery;

        protected override void OnCreate()
        {
            racingQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<ActivityParticipantComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>(),
                ComponentType.ReadOnly<CreatureMovementComponent>()
            });
        }

        protected override void OnUpdate()
        {
            foreach (var (participant, genetics, movement) in
                SystemAPI.Query<RefRW<ActivityParticipantComponent>, RefRO<GeneticDataComponent>, RefRO<CreatureMovementComponent>>())
            {
                if (participant.ValueRO.CurrentActivity != ActivityType.Racing ||
                    participant.ValueRO.Status != ActivityStatus.Active)
                    continue;

                // Racing-specific performance calculation
                float speedFactor = genetics.ValueRO.Speed;
                float agilityFactor = genetics.ValueRO.Agility;
                float enduranceFactor = genetics.ValueRO.Stamina;

                // Track type affects performance (could be expanded)
                float trackDifficulty = 1f; // Base difficulty
                float performanceScore = (speedFactor * 0.5f + agilityFactor * 0.3f + enduranceFactor * 0.2f) / trackDifficulty;

                participant.ValueRW.PerformanceScore = math.clamp(performanceScore, 0.1f, 2.0f);
            }
        }
    }

    /// <summary>
    /// Combat Arena System - Tactical combat encounters
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class CombatActivitySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (participant, genetics) in
                SystemAPI.Query<RefRW<ActivityParticipantComponent>, RefRO<GeneticDataComponent>>())
            {
                if (participant.ValueRO.CurrentActivity != ActivityType.Combat ||
                    participant.ValueRO.Status != ActivityStatus.Active)
                    continue;

                // Combat performance calculation
                float combatPower = genetics.ValueRO.Aggression * genetics.ValueRO.Size;
                float combatStrategy = genetics.ValueRO.Intelligence;
                float combatEndurance = genetics.ValueRO.Stamina;

                float combatScore = (combatPower * 0.4f + combatStrategy * 0.3f + combatEndurance * 0.3f);
                participant.ValueRW.PerformanceScore = math.clamp(combatScore, 0.1f, 2.0f);
            }
        }
    }

    /// <summary>
    /// Puzzle Academy System - Intelligence-based problem solving
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class PuzzleActivitySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (participant, genetics) in
                SystemAPI.Query<RefRW<ActivityParticipantComponent>, RefRO<GeneticDataComponent>>())
            {
                if (participant.ValueRO.CurrentActivity != ActivityType.Puzzle ||
                    participant.ValueRO.Status != ActivityStatus.Active)
                    continue;

                // Puzzle solving is primarily intelligence-based
                float intelligenceFactor = genetics.ValueRO.Intelligence;
                float curiosityBonus = genetics.ValueRO.Curiosity * 0.5f;
                float learningSpeed = intelligenceFactor + curiosityBonus;

                // Puzzles get easier over time as the creature learns
                float learningProgress = participant.ValueRO.TimeInActivity / 60f; // 1 minute to learn
                float difficultyReduction = math.saturate(learningProgress) * 0.3f;

                float puzzleScore = learningSpeed * (1f + difficultyReduction);
                participant.ValueRW.PerformanceScore = math.clamp(puzzleScore, 0.1f, 2.0f);
            }
        }
    }

    #endregion

    #region Activity Center Management

    /// <summary>
    /// Manages activity center operations, queues, and resource allocation
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ActivityCenterManagementSystem : SystemBase
    {
        private EntityQuery centerQuery;

        protected override void OnCreate()
        {
            centerQuery = GetEntityQuery(ComponentType.ReadWrite<ActivityCenterComponent>());
        }

        protected override void OnUpdate()
        {
            foreach (var center in SystemAPI.Query<RefRW<ActivityCenterComponent>>())
            {
                // Update center status and capacity
                var centerData = center.ValueRO;

                // Calculate current participants (would query for participants at this center)
                // For now, simplified logic
                centerData.IsActive = centerData.CurrentParticipants > 0;

                // Quality affects performance bonuses
                centerData.QualityRating = math.clamp(centerData.QualityRating, 0.5f, 2.0f);

                center.ValueRW = centerData;
            }
        }
    }

    #endregion

    #region Authoring Components

    /// <summary>
    /// MonoBehaviour authoring component for Activity Centers
    /// Drop this on GameObjects to create activity centers in scenes
    /// </summary>
    public class ActivityCenterAuthoring : MonoBehaviour
    {
        [Header("Activity Configuration")]
        public ActivityType activityType = ActivityType.Racing;
        [Range(1, 50)] public int maxParticipants = 10;
        [Range(30f, 600f)] public float activityDuration = 120f;
        [Range(0.1f, 3.0f)] public float difficultyLevel = 1.0f;
        [Range(0.5f, 2.0f)] public float qualityRating = 1.0f;

        [Header("Center Properties")]
        public bool startActive = true;
        public Transform[] participantSpawnPoints;

        [ContextMenu("Create Activity Center Entity")]
        public void CreateActivityCenterEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add activity center component
            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = activityType,
                MaxParticipants = maxParticipants,
                CurrentParticipants = 0,
                ActivityDuration = activityDuration,
                DifficultyLevel = difficultyLevel,
                IsActive = startActive,
                QualityRating = qualityRating,
                OwnerCreature = Entity.Null
            });

            // Link to GameObject
            entityManager.AddComponentData(entity, new GameObjectLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy
            });

            Debug.Log($"✅ Created {activityType} Activity Center with {maxParticipants} max participants");
        }

        private void OnDrawGizmos()
        {
            // Draw activity center visualization
            var color = activityType switch
            {
                ActivityType.Racing => Color.yellow,
                ActivityType.Combat => Color.red,
                ActivityType.Puzzle => Color.blue,
                ActivityType.Strategy => Color.magenta,
                ActivityType.Music => Color.cyan,
                ActivityType.Adventure => Color.green,
                ActivityType.Platforming => Color.orange,
                ActivityType.Crafting => Color.white,
                _ => Color.gray
            };

            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 5f);

            // Draw participant spawn points
            if (participantSpawnPoints != null)
            {
                Gizmos.color = Color.green;
                foreach (var point in participantSpawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 0.5f);
                    }
                }
            }
        }
    }

    #endregion
}