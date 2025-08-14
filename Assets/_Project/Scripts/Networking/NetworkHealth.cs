using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Network-synchronized health system with server authority.
    /// Manages player health state, damage application, and healing across all clients.
    /// </summary>
    public class NetworkHealth : NetworkBehaviour
    {
        #region Fields

        [Header("Health Configuration")]
        [SerializeField] private int maxHealth = 100;

        /// <summary>Current health value synchronized across all clients.</summary>
        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
            default, 
            NetworkVariableReadPermission.Everyone, 
            NetworkVariableWritePermission.Server);

        #endregion

        #region Properties

        /// <summary>Maximum health value for this entity.</summary>
        public int MaxHealth => maxHealth;

        /// <summary>Whether this entity is currently alive (health > 0).</summary>
        public bool IsAlive => CurrentHealth.Value > 0;

        /// <summary>Health as a normalized percentage (0.0 to 1.0).</summary>
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth.Value / MaxHealth : 0f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize network health when spawned on network.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentHealth.Value = maxHealth;
            }
            CurrentHealth.OnValueChanged += OnHealthChanged;
        }

        /// <summary>
        /// Cleanup network health when despawned from network.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies damage to this health component. Server authority only.
        /// </summary>
        /// <param name="amount">Amount of damage to apply (must be positive).</param>
        public void ApplyDamage(int amount)
        {
            if (!IsServer || amount <= 0) return;

            CurrentHealth.Value = math.clamp(CurrentHealth.Value - amount, 0, maxHealth);

            if (CurrentHealth.Value == 0)
            {
                OnPlayerDeath();
            }
        }

        /// <summary>
        /// Heals this health component. Server authority only.
        /// </summary>
        /// <param name="amount">Amount of health to restore (must be positive).</param>
        public void Heal(int amount)
        {
            if (!IsServer || amount <= 0) return;

            CurrentHealth.Value = math.clamp(CurrentHealth.Value + amount, 0, maxHealth);
        }

        /// <summary>
        /// Resets health to maximum value. Server authority only.
        /// </summary>
        public void ResetToMaxHealth()
        {
            if (!IsServer) return;
            CurrentHealth.Value = maxHealth;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Called whenever health value changes on any client.
        /// Used for UI updates and local feedback.
        /// </summary>
        /// <param name="oldValue">Previous health value.</param>
        /// <param name="newValue">New health value.</param>
        private void OnHealthChanged(int oldValue, int newValue)
        {
            Debug.Log($"[{gameObject.name}] Health changed from {oldValue} to {newValue}");
            
            // TODO: Integrate with message bus for decoupled health change notifications
            // MessageBus.Publish(new HealthChangedEvent(OwnerClientId, oldValue, newValue));
        }

        /// <summary>
        /// Handles entity death logic when health reaches zero.
        /// </summary>
        private void OnPlayerDeath()
        {
            Debug.Log($"[{gameObject.name}] Entity has died");
            
            // TODO: Implement additional death logic
            // - Disable components
            // - Trigger death animations
            // - Notify game systems via message bus
        }

        #endregion
    }
}
