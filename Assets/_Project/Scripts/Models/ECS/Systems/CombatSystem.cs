using Unity.Entities;
using Unity.Mathematics;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Handles combat interactions between entities.
    /// Processes attack inputs and applies damage to targets.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CombatSystem : SystemBase
    {
        #region Constants

        /// <summary>
        /// Fixed damage amount applied per successful attack
        /// </summary>
        private const int AttackDamage = 10;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Processes combat actions for all entities with player input and state components.
        /// </summary>
        protected override void OnUpdate()
        {
            Entities
                .WithAll<PlayerInputComponent, PlayerStateComponent>()
                .ForEach((ref PlayerStateComponent state, in PlayerInputComponent input) =>
                {
                    ProcessCombatInput(ref state, in input);
                }).ScheduleParallel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes combat-related input for a single entity.
        /// </summary>
        /// <param name="state">The player's current state</param>
        /// <param name="input">The player's input this frame</param>
        private void ProcessCombatInput(ref PlayerStateComponent state, in PlayerInputComponent input)
        {
            if (!state.IsAlive) 
                return;

            if (input.AttackPressed)
            {
                ApplyDamageToSelf(ref state);
                CheckForDeath(ref state);
            }
        }

        /// <summary>
        /// Applies damage to the entity (temporary demo implementation).
        /// TODO: Replace with proper target selection and damage system.
        /// </summary>
        /// <param name="state">The state to apply damage to</param>
        private void ApplyDamageToSelf(ref PlayerStateComponent state)
        {
            state.CurrentHP -= AttackDamage;
        }

        /// <summary>
        /// Checks if the entity should die based on current health.
        /// </summary>
        /// <param name="state">The state to check</param>
        private void CheckForDeath(ref PlayerStateComponent state)
        {
            if (state.CurrentHP <= 0)
            {
                state.IsAlive = false;
                state.CurrentHP = 0;
                // TODO: Optionally raise death event
            }
        }

        #endregion
    }
}
