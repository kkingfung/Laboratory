using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Equipment.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Equipment.Systems
{
    /// <summary>
    /// Equipment management for activity performance bonuses
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Activities.Systems.ActivityCenterSystem))]
    public partial class ActivityEquipmentBonusSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Apply equipment bonuses to activity performance
            foreach (var (participant, equipment, entity) in
                SystemAPI.Query<RefRW<ActivityParticipantComponent>, RefRO<CreatureEquipmentComponent>>().WithEntityAccess())
            {
                if (participant.ValueRO.Status != ActivityStatus.Active)
                    continue;

                // Calculate activity-specific equipment bonuses
                float equipmentBonus = CalculateActivityBonus(ConvertActivityType(participant.ValueRO.CurrentActivity), equipment.ValueRO);

                // Apply equipment bonus to performance
                float basePerformance = participant.ValueRO.PerformanceScore;
                float enhancedPerformance = basePerformance * (1f + equipmentBonus + equipment.ValueRO.TotalStatBonus);

                participant.ValueRW.PerformanceScore = math.clamp(enhancedPerformance, 0.1f, 3.0f);
            }
        }

        private ActivityType ConvertActivityType(ActivityType activityType)
        {
            return activityType;
        }

        private float CalculateActivityBonus(ActivityType activity, CreatureEquipmentComponent equipment)
        {
            // This would query the actual equipment entities and sum their bonuses
            // For now, simplified calculation based on equipped item count
            float baseBonus = equipment.EquippedItemCount * 0.05f; // 5% per equipped item
            float setBonus = equipment.HasCompleteSet ? 0.2f : 0f; // 20% set bonus

            return baseBonus + setBonus;
        }
    }
}