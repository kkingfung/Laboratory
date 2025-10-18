using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Laboratory.Core.TownBuilding.Components;
using Laboratory.Core.TownBuilding.Jobs;

namespace Laboratory.Core.TownBuilding.Systems
{
    /// <summary>
    /// Resource production and management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TownBuildingSystem))]
    public partial class ResourceProductionSystem : SystemBase
    {
        private EntityQuery productionBuildingQuery;

        protected override void OnCreate()
        {
            productionBuildingQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<BuildingComponent>(),
                ComponentType.ReadWrite<BuildingProductionComponent>(),
                ComponentType.ReadWrite<ResourceStorageComponent>()
            });
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var productionJob = new ResourceProductionJob
            {
                DeltaTime = deltaTime
            };

            Dependency = productionJob.ScheduleParallel(productionBuildingQuery, Dependency);
        }
    }
}