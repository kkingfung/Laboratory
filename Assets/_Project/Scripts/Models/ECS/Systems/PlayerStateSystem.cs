using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Manages player state updates including stamina regeneration, death detection, and status effects.
    /// Runs during simulation group and processes all living players for state changes.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerStateSystem : SystemBase
    {
        #region Fields

        /// <summary>
        /// Rate at which stamina regenerates per second when not being consumed.
        /// </summary>
        private const float StaminaRegenRate = 5f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Updates player states each frame, handling stamina regeneration and death detection.
        /// </summary>
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Process all entities with PlayerStateComponent
            Entities.ForEach((ref PlayerStateComponent state) =>
            {
                ProcessPlayerState(ref state, deltaTime);
            }).ScheduleParallel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes individual player state updates including stamina regeneration and death detection.
        /// </summary>
        /// <param name="state">Reference to the player state component to update</param>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        private static void ProcessPlayerState(ref PlayerStateComponent state, float deltaTime)
        {
            // Skip processing for dead players
            if (!state.IsAlive)
                return;

            // Regenerate stamina up to maximum capacity
            RegenerateStamina(ref state, deltaTime);

            // Check for death condition and handle state transition
            CheckAndHandleDeath(ref state);

            // Additional status effect processing can be added here
            // ProcessStatusEffects(ref state, deltaTime);
        }

        /// <summary>
        /// Regenerates player stamina at the defined rate, capped at maximum stamina.
        /// </summary>
        /// <param name="state">Reference to the player state component</param>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        private static void RegenerateStamina(ref PlayerStateComponent state, float deltaTime)
        {
            state.Stamina += StaminaRegenRate * deltaTime;
            
            if (state.Stamina > state.MaxStamina)
            {
                state.Stamina = state.MaxStamina;
            }
        }

        /// <summary>
        /// Checks if player health has dropped to zero or below and handles death state transition.
        /// </summary>
        /// <param name="state">Reference to the player state component</param>
        private static void CheckAndHandleDeath(ref PlayerStateComponent state)
        {
            if (state.CurrentHP <= 0)
            {
                state.IsAlive = false;
                state.CurrentHP = 0;
                
                // TODO: Consider adding death event messaging system here
                // This could trigger death animations, respawn timers, or UI updates
            }
        }

        #endregion
    }
}
