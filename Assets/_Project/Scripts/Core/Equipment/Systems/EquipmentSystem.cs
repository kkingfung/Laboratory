using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Laboratory.Core.Equipment.Components;
using Laboratory.Core.Equipment.Jobs;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;

namespace Laboratory.Core.Equipment.Systems
{
    /// <summary>
    /// Core equipment management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EquipmentSystem : SystemBase
    {
        private EntityQuery equippedCreatureQuery;
        private EntityQuery equipmentQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            equippedCreatureQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<CreatureEquipmentComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>(),
                ComponentType.ReadOnly<ActivityParticipantComponent>()
            });

            equipmentQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<EquipmentComponent>(),
                ComponentType.ReadOnly<EquipmentStatsComponent>()
            });

            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Update equipment effects on creatures
            var equipmentEffectsJob = new EquipmentEffectsJob
            {
                DeltaTime = deltaTime
            };
            Dependency = equipmentEffectsJob.ScheduleParallel(equippedCreatureQuery, Dependency);

            // Update equipment durability
            var durabilityJob = new EquipmentDurabilityJob
            {
                DeltaTime = deltaTime
            };
            Dependency = durabilityJob.ScheduleParallel(equipmentQuery, Dependency);
        }
    }
}