using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Laboratory.Networking
{
    /// <summary>
    /// Manages player health over the network with synchronized state.
    /// </summary>
    public class NetworkHealth : NetworkBehaviour
    {
        #region Fields

        [Header("Health Configuration")]
        [SerializeField] private int maxHealth = 100;

        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        #endregion

        #region Properties

        /// <summary>Maximum health value.</summary>
        public int MaxHealth => maxHealth;

        /// <summary>Whether this player is currently alive.</summary>
        public bool IsAlive => CurrentHealth.Value > 0;

        /// <summary>Health as a normalized percentage (0.0 to 1.0).</summary>
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth.Value / MaxHealth : 0f;

        #endregion

        #region Unity Override Methods

        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentHealth.Value = maxHealth;
            }
            CurrentHealth.OnValueChanged += OnHealthChanged;
        }

        public override void OnNetworkDespawn()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies damage to this health component. Server only.
        /// </summary>
        /// <param name="amount">Amount of damage to apply.</param>
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
        /// Heals this health component. Server only.
        /// </summary>
        /// <param name="amount">Amount of health to restore.</param>
        public void Heal(int amount)
        {
            if (!IsServer || amount <= 0) return;

            CurrentHealth.Value = math.clamp(CurrentHealth.Value + amount, 0, maxHealth);
        }

        /// <summary>
        /// Resets health to maximum. Server only.
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
        /// </summary>
        /// <param name="oldValue">Previous health value.</param>
        /// <param name="newValue">New health value.</param>
        private void OnHealthChanged(int oldValue, int newValue)
        {
            Debug.Log($"[{gameObject.name}] Health changed from {oldValue} to {newValue}");
            
            // Broadcast health change event for UI updates
            // MessageBus could be used here for decoupled communication
        }

        /// <summary>
        /// Handles player death logic.
        /// </summary>
        private void OnPlayerDeath()
        {
            Debug.Log($"[{gameObject.name}] Player has died");
            // Additional death logic can be implemented here
        }

        #endregion

        #region Inner Classes, Enums

        // No inner classes or enums currently.

        #endregion
    }
}
