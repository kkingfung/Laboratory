using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Laboratory.Core.Progression.Components;
using Laboratory.Core.Progression.Jobs;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;

namespace Laboratory.Core.Progression.Systems
{
    /// <summary>
    /// Core experience and level progression system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Activities.Systems.ActivityCenterSystem))]
    public partial class ExperienceProgressionSystem : SystemBase
    {
        private EntityQuery progressionQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            progressionQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<CreatureProgressionComponent>(),
                ComponentType.ReadWrite<CreatureSkillsComponent>(),
                ComponentType.ReadOnly<ActivityParticipantComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });

            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            var progressionJob = new ExperienceGainJob
            {
                CommandBuffer = ecb,
                CurrentTime = (float)SystemAPI.Time.ElapsedTime
            };

            Dependency = progressionJob.ScheduleParallel(progressionQuery, Dependency);
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}