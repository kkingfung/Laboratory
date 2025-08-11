using Unity.Entities;
using Unity.Mathematics;

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CombatSystem : SystemBase
    {
        // Simple fixed damage per attack
        private const int AttackDamage = 10;

        protected override void OnUpdate()
        {
            Entities
                .WithAll<PlayerInputComponent, PlayerStateComponent>()
                .ForEach((ref PlayerStateComponent state, in PlayerInputComponent input) =>
                {
                    if (!state.IsAlive) return;

                    if (input.AttackPressed)
                    {
                        // For demo: damage self (replace with target logic)
                        state.CurrentHP -= AttackDamage;

                        if (state.CurrentHP <= 0)
                        {
                            state.IsAlive = false;
                            state.CurrentHP = 0;
                            // Optionally raise death event
                        }
                    }
                }).ScheduleParallel();
        }
    }
}
