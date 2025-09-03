using UnityEngine;
using System;
using Laboratory.Infrastructure.Networking;
using Laboratory.Core.Events;
using Laboratory.Core.DI;
// UI dependency removed to prevent circular dependency
// using Laboratory.UI.Helper;

namespace Laboratory.Models
{
    /// <summary>
    /// Listens to player damage events and publishes them to the unified event bus.
    /// UI components can subscribe to these events separately.
    /// </summary>
    public class PlayerDamageListener : MonoBehaviour
    {
        #region Fields

        [Header("Dependencies")]
        [Tooltip("NetworkHealth component representing the player's health.")]
        // [SerializeField] private NetworkHealth networkHealth = null!;
        // TODO: NetworkHealth component needs to be created or moved to proper assembly

        // Event bus for decoupled communication
        private IEventBus _eventBus;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            // TODO: Re-enable when NetworkHealth component is created
            // if (networkHealth == null)
            // {
            //     Debug.LogError($"NetworkHealth component not found on {gameObject.name}");
            // }
            
            // Get event bus from service provider
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance.TryResolve<IEventBus>(out _eventBus);
            }
        }

        private void OnEnable()
        {
            // TODO: Re-enable when NetworkHealth component is created
            // if (networkHealth != null)
            //     networkHealth.CurrentHealth.OnValueChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            // TODO: Re-enable when NetworkHealth component is created
            // if (networkHealth != null)
            //     networkHealth.CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles health value changes and publishes damage events.
        /// </summary>
        /// <param name="oldVal">Previous health value.</param>
        /// <param name="newVal">New health value.</param>
        private void OnHealthChanged(int oldVal, int newVal)
        {
            int damageTaken = oldVal - newVal;
            if (damageTaken > 0 && _eventBus != null)
            {
                // TODO: Re-enable when NetworkHealth component is created
                // Publish damage event for UI to handle
                var damageEvent = new Laboratory.Core.Events.Messages.DamageEvent(
                    target: gameObject,
                    source: null,
                    amount: damageTaken,
                    type: Laboratory.Core.Health.DamageType.Normal,
                    direction: Vector3.zero,
                    targetClientId: 0, // networkHealth.OwnerClientId,
                    attackerClientId: 0
                );
                
                _eventBus.Publish(damageEvent);
                Debug.Log($"PlayerDamageListener: Published damage event - Amount: {damageTaken}");
                // Debug.Log($"PlayerDamageListener: Published damage event - Target: {networkHealth.OwnerClientId}, Amount: {damageTaken}");
            }
        }

        #endregion
    }
}
