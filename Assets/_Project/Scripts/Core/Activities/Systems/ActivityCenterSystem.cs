using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Activities.Systems
{
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
}