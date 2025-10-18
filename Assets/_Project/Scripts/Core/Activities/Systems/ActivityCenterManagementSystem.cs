using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Activities.Components;

namespace Laboratory.Core.Activities.Systems
{
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
}