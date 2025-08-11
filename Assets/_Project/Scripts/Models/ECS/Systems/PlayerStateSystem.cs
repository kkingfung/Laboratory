using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerStateSystem : SystemBase
    {
        // Stamina regen rate per second
        private const float StaminaRegenRate = 5f;

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            Entities.ForEach((ref PlayerStateComponent state) =>
            {
                // Skip dead players
                if (!state.IsAlive) return;

                // Regenerate stamina up to max
                state.Stamina += StaminaRegenRate * deltaTime;
                if (state.Stamina > state.MaxStamina)
                    state.Stamina = state.MaxStamina;

                // Check death
                if (state.CurrentHP <= 0)
                {
                    state.IsAlive = false;
                    state.CurrentHP = 0;
                    // Could add death event messaging here
                }

                // Additional status effect handling can be added here

            }).ScheduleParallel();
        }
    }
}
