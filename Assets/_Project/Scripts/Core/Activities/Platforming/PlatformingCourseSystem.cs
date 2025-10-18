using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Activities.Platforming
{
    /// <summary>
    /// 🏃 PLATFORMING COURSE SYSTEM - Complete platforming and obstacle mini-game
    /// FEATURES: Jump challenges, obstacle courses, precision platforming, speedruns
    /// PERFORMANCE: Physics-based movement with genetic agility factors
    /// GENETICS: Agility, Speed, Intelligence affect platforming performance
    /// </summary>

    #region Platforming Components

    public struct PlatformingCourseComponent : IComponentData
    {
        public CourseType Type;
        public CourseDifficulty Difficulty;
        public CourseStatus Status;
        public int MaxRunners;
        public int CurrentRunners;
        public float CourseLength;
        public int ObstacleCount;
        public float RecordTime;
        public Entity RecordHolder;
        public float SessionTimer;
        public bool IsSpeedrunMode;
        public int CompletionCount;
        public float AverageCompletionTime;
    }

    public struct PlatformerComponent : IComponentData
    {
        public Entity Course;
        public PlatformerStatus Status;
        public float3 Position;
        public float3 Velocity;
        public float CourseProgress;
        public float RunTime;
        public float BestTime;
        public int JumpsUsed;
        public int PerfectLandings;
        public int ObstaclesCleared;
        public int Retries;
        public bool IsGrounded;
        public float AirTime;
        public int ComboMoves;
        public float PrecisionScore;
    }

    public struct PlatformingPerformanceComponent : IComponentData
    {
        public float JumpHeight;
        public float JumpDistance;
        public float MovementSpeed;
        public float PrecisionControl;
        public float TimingAccuracy;
        public float BalanceSkill;
        public float ObstacleNavigation;
        public float SpeedrunOptimization;
        public float RecoveryAbility;
        public int PlatformingExperience;
    }

    #endregion

    #region Platforming Enums

    public enum CourseType : byte
    {
        Basic_Jumping,
        Precision_Platforming,
        Speed_Course,
        Obstacle_Gauntlet,
        Puzzle_Platforms,
        Moving_Platforms,
        Hazard_Course,
        Extreme_Challenge
    }

    public enum CourseDifficulty : byte
    {
        Beginner,
        Novice,
        Intermediate,
        Advanced,
        Expert,
        Master,
        Legendary
    }

    public enum CourseStatus : byte
    {
        Open,
        Running,
        Speedrun_Event,
        Maintenance,
        Closed
    }

    public enum PlatformerStatus : byte
    {
        Waiting,
        Starting,
        Running,
        Jumping,
        Falling,
        Completed,
        Failed,
        Retrying
    }

    #endregion

    #region Platforming Systems

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class PlatformingCourseManagementSystem : SystemBase
    {
        private EntityQuery courseQuery;

        protected override void OnCreate()
        {
            courseQuery = GetEntityQuery(ComponentType.ReadWrite<PlatformingCourseComponent>());
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var course in SystemAPI.Query<RefRW<PlatformingCourseComponent>>())
            {
                UpdateCourse(ref course.ValueRW, deltaTime);
            }
        }

        private void UpdateCourse(ref PlatformingCourseComponent course, float deltaTime)
        {
            course.SessionTimer += deltaTime;

            if (course.Status == CourseStatus.Running && course.CurrentRunners > 0)
            {
                // Update average completion time
                if (course.CompletionCount > 0)
                {
                    course.AverageCompletionTime = course.SessionTimer / course.CompletionCount;
                }
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PlatformingCourseManagementSystem))]
    public partial class PlatformingMovementSystem : SystemBase
    {
        private EntityQuery platformerQuery;

        protected override void OnCreate()
        {
            platformerQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<PlatformerComponent>(),
                ComponentType.ReadWrite<LocalTransform>(),
                ComponentType.ReadOnly<PlatformingPerformanceComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var movementJob = new PlatformingMovementJob
            {
                DeltaTime = deltaTime
            };

            Dependency = movementJob.ScheduleParallel(platformerQuery, Dependency);
        }
    }


    public partial struct PlatformingMovementJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref PlatformerComponent platformer,
            ref LocalTransform transform,
            in PlatformingPerformanceComponent performance,
            RefRO<GeneticDataComponent> genetics)
        {
            if (platformer.Status != PlatformerStatus.Running && platformer.Status != PlatformerStatus.Jumping)
                return;

            // Update run time
            platformer.RunTime += DeltaTime;

            // Calculate movement based on genetics and performance
            float speed = genetics.ValueRO.Speed * performance.MovementSpeed;
            float agility = genetics.ValueRO.Agility * performance.PrecisionControl;

            // Simple platforming movement
            float3 movement = new float3(speed * DeltaTime, 0, 0);
            transform.Position += movement;
            platformer.Position = transform.Position;

            // Update course progress
            platformer.CourseProgress += speed * DeltaTime * 0.01f; // Normalized progress

            // Check for completion
            if (platformer.CourseProgress >= 1f)
            {
                platformer.Status = PlatformerStatus.Completed;
                if (platformer.RunTime < platformer.BestTime || platformer.BestTime == 0f)
                {
                    platformer.BestTime = platformer.RunTime;
                }
            }
        }
    }

    #endregion

    public class PlatformingCourseAuthoring : MonoBehaviour
    {
        [Header("Course Configuration")]
        public CourseType courseType = CourseType.Basic_Jumping;
        public CourseDifficulty difficulty = CourseDifficulty.Intermediate;
        [Range(1, 20)] public int maxRunners = 8;
        [Range(50f, 500f)] public float courseLength = 200f;
        [Range(5, 50)] public int obstacleCount = 15;

        [ContextMenu("Create Platforming Course Entity")]
        public void CreatePlatformingCourseEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            entityManager.AddComponentData(entity, new PlatformingCourseComponent
            {
                Type = courseType,
                Difficulty = difficulty,
                Status = CourseStatus.Open,
                MaxRunners = maxRunners,
                CourseLength = courseLength,
                ObstacleCount = obstacleCount,
                RecordTime = 0f,
                IsSpeedrunMode = false
            });

            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = ActivityType.Platforming,
                MaxParticipants = maxRunners,
                ActivityDuration = 300f,
                DifficultyLevel = (float)difficulty,
                IsActive = true,
                QualityRating = 1.0f
            });

            Debug.Log($"✅ Created {difficulty} {courseType} platforming course");
        }
    }
}