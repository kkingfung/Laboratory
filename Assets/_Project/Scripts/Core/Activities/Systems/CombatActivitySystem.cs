using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.Activities.Systems
{
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
}