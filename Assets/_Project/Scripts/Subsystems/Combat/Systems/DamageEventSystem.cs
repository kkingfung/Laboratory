using UnityEngine;
using Unity.Netcode;
using Laboratory.Models.ECS.Components;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Health;
using DamageRequest = Laboratory.Core.Health.DamageRequest;
using DamageType = Laboratory.Core.Health.DamageType;

// Resolve DamageEvent ambiguity - use the ECS version in this context
using DamageEvent = Laboratory.Models.ECS.Components.DamageEvent;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Handles damage event processing and publishes results to the message bus.
    /// Manages health updates and death events when entities take fatal damage.
    /// </summary>
    public class DamageEventSystem : MonoBehaviour
    {
        #region Public Methods

        /// <summary>
        /// Applies damage to a target entity and publishes associated events.
        /// </summary>
        /// <param name="damageEvent">The damage event containing target and damage information</param>
        public void ApplyDamage(Laboratory.Models.ECS.Components.DamageEvent damageEvent)
        {
            var targetHealth = GetTargetHealthComponent(damageEvent.TargetClientId);
            ProcessDamageApplication(targetHealth, damageEvent);
            PublishDamageEvent(damageEvent);
            CheckForDeath(targetHealth, damageEvent);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Retrieves the health component for the specified target.
        /// </summary>
        /// <param name="targetId">Network ID of the target entity</param>
        /// <returns>HealthComponentBase if found, null otherwise</returns>
        private HealthComponentBase GetTargetHealthComponent(ulong targetId)
        {
            return NetworkManager.Singleton.SpawnManager
                .GetPlayerNetworkObject(targetId)
                ?.GetComponent<HealthComponentBase>();
        }

        /// <summary>
        /// Applies the damage amount to the target's health component.
        /// </summary>
        /// <param name="targetHealth">The health component to modify</param>
        /// <param name="damageEvent">The damage event data</param>
        private void ProcessDamageApplication(HealthComponentBase targetHealth, Laboratory.Models.ECS.Components.DamageEvent damageEvent)
        {
            if (targetHealth != null)
            {
                // Create a DamageRequest from the DamageEvent
                var damageRequest = new DamageRequest
                {
                    Amount = damageEvent.DamageAmount,
                    Type = DamageType.Normal, // Default to Normal damage
                    Source = null, // Could be enhanced to include source GameObject
                    Direction = Vector3.zero,
                    CanBeBlocked = true,
                    TriggerInvulnerability = true
                };
                
                targetHealth.TakeDamage(damageRequest);
            }
        }

        /// <summary>
        /// Publishes the damage event to the message bus for other systems to process.
        /// </summary>
        /// <param name="damageEvent">The damage event to publish</param>
        private void PublishDamageEvent(Laboratory.Models.ECS.Components.DamageEvent damageEvent)
        {
            MessageBus.Publish(damageEvent);
        }

        /// <summary>
        /// Checks if the target died from the damage and publishes death events if necessary.
        /// </summary>
        /// <param name="targetHealth">The target's health component</param>
        /// <param name="damageEvent">The original damage event</param>
        private void CheckForDeath(HealthComponentBase targetHealth, Laboratory.Models.ECS.Components.DamageEvent damageEvent)
        {
            if (targetHealth != null && targetHealth.CurrentHealth <= 0)
            {
                PublishDeathEvent(damageEvent);
            }
        }

        /// <summary>
        /// Creates and publishes a death event when an entity dies.
        /// </summary>
        /// <param name="damageEvent">The damage event that caused the death</param>
        private void PublishDeathEvent(Laboratory.Models.ECS.Components.DamageEvent damageEvent)
        {
            var deathEvent = new Laboratory.Models.ECS.Components.DeathEvent
            (
                victimClientId : damageEvent.TargetClientId,
                killerClientId : damageEvent.AttackerClientId
            );
            MessageBus.Publish(deathEvent);
        }

        #endregion
    }
}
