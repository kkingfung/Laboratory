using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// System that manages ability cooldowns and activation for entities.
    /// Handles ability state transitions, cooldown management, and activation requests.
    /// </summary>
    public partial class AbilityManagerSystem : SystemBase
    {
        #region Fields

        // Add any system-wide fields here if needed.

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Updates ability cooldowns and handles activation requests each frame.
        /// </summary>
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            Entities.ForEach((ref AbilityComponent ability) =>
            {
                // Update cooldown timer
                if (ability.CooldownRemaining > 0f)
                {
                    ability.CooldownRemaining -= deltaTime;
                    if (ability.CooldownRemaining < 0f)
                        ability.CooldownRemaining = 0f;
                }

                // Process activation request
                if (ability.RequestedActivation && ability.CooldownRemaining == 0f)
                {
                    ability.IsActive = true;
                    ability.CooldownRemaining = ability.CooldownDuration;
                    ability.RequestedActivation = false;
                }
            }).Schedule();
        }

        #endregion

        #region Private Methods

        // Private methods removed - logic inlined into Entities.ForEach for Burst compatibility

        #endregion

        #region Component Definitions

        /// <summary>
        /// Component data structure for ability management.
        /// Contains state information for cooldowns and activation.
        /// </summary>
        public struct AbilityComponent : IComponentData
        {
            /// <summary>
            /// Whether the ability is currently active
            /// </summary>
            public bool IsActive;

            /// <summary>
            /// Whether activation has been requested
            /// </summary>
            public bool RequestedActivation;

            /// <summary>
            /// Total duration of the cooldown period
            /// </summary>
            public float CooldownDuration;

            /// <summary>
            /// Time remaining until ability can be used again
            /// </summary>
            public float CooldownRemaining;
        }

        #endregion
    }
}
