using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Laboratory.Core.TownBuilding.Components;
using Laboratory.Core.TownBuilding.Jobs;

namespace Laboratory.Core.TownBuilding.Systems
{
    /// <summary>
    /// Core building management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TownBuildingSystem : SystemBase
    {
        private EntityQuery buildingQuery;
        private EntityQuery constructionQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            buildingQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<BuildingComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            });

            constructionQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<BuildingComponent>(),
                ComponentType.ReadOnly<BuildingProductionComponent>()
            });

            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update building construction and operation
            var buildingUpdateJob = new BuildingUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };

            Dependency = buildingUpdateJob.ScheduleParallel(buildingQuery, Dependency);
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}