using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Activities.Systems
{
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
}