using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// System that manages ability cooldowns and activation for entities.
    /// </summary>
    public partial class AbilityManagerSystem : SystemBase
    {
        #region Fields

        // Add any system-wide fields here if needed.

        #endregion

        #region Override Unity Methods

        protected override void OnUpdate()
        {
            float deltaTime = Time.DeltaTime;

            Entities.ForEach((ref AbilityComponent ability) =>
            {
                // Update cooldown timers
                if (ability.CooldownRemaining > 0f)
                {
                    ability.CooldownRemaining -= deltaTime;
                    if (ability.CooldownRemaining < 0f)
                        ability.CooldownRemaining = 0f;
                }

                // Handle activation logic (example)
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

        // Add private helper methods here if needed.

        #endregion

        #region Inner Classes, Enums

        // Example ability component for demonstration.
        public struct AbilityComponent : IComponentData
        {
            public bool IsActive;
            public bool RequestedActivation;
            public float CooldownDuration;
            public float CooldownRemaining;
        }

        #endregion
    }
}
