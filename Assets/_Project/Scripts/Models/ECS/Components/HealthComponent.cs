using UnityEngine;
using Unity.Netcode;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Base health component for entities in the ECS system.
    /// Provides a common interface for health management across different systems.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class HealthComponent : NetworkBehaviour
    {
        #region Fields

        [Header("Health Configuration")]
        [SerializeField] private int _maxHealth = 100;

        /// <summary>Current health value synchronized across all clients.</summary>
        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        #endregion

        #region Properties

        /// <summary>Maximum health value for this entity.</summary>
        public int MaxHealth => _maxHealth;

        /// <summary>Whether this entity is currently alive (health > 0).</summary>
        public bool IsAlive => CurrentHealth.Value > 0;

        /// <summary>Health as a normalized percentage (0.0 to 1.0).</summary>
        public float HealthPercentage => _maxHealth > 0 ? (float)CurrentHealth.Value / _maxHealth : 0f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize health when the component awakens.
        /// </summary>
        private void Awake()
        {
            CurrentHealth.Value = _maxHealth;
        }

        /// <summary>
        /// Initialize network health when spawned on network.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                CurrentHealth.Value = _maxHealth;
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
        public virtual void ApplyDamage(float amount)
        {
            if (!IsServer || amount <= 0) return;

            int damageAmount = Mathf.RoundToInt(amount);
            CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - damageAmount);

            if (CurrentHealth.Value == 0)
            {
                OnEntityDeath();
            }
        }

        /// <summary>
        /// Applies damage to this health component. Server authority only.
        /// </summary>
        /// <param name="amount">Amount of damage to apply (must be positive).</param>
        public virtual void ApplyDamage(int amount)
        {
            if (!IsServer || amount <= 0) return;

            CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - amount);

            if (CurrentHealth.Value == 0)
            {
                OnEntityDeath();
            }
        }

        /// <summary>
        /// Heals this health component. Server authority only.
        /// </summary>
        /// <param name="amount">Amount of health to restore (must be positive).</param>
        public virtual void Heal(int amount)
        {
            if (!IsServer || amount <= 0) return;

            CurrentHealth.Value = Mathf.Min(_maxHealth, CurrentHealth.Value + amount);
        }

        /// <summary>
        /// Resets health to maximum value. Server authority only.
        /// </summary>
        public virtual void ResetToMaxHealth()
        {
            if (!IsServer) return;
            CurrentHealth.Value = _maxHealth;
        }

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// Called when health value changes on any client.
        /// Override this in derived classes for custom health change behavior.
        /// </summary>
        /// <param name="oldValue">Previous health value.</param>
        /// <param name="newValue">New health value.</param>
        protected virtual void OnHealthChanged(int oldValue, int newValue)
        {
            Debug.Log($"[{gameObject.name}] Health changed from {oldValue} to {newValue}");
            // Can be overridden by derived classes for custom behavior
        }

        /// <summary>
        /// Called when entity health reaches zero.
        /// Override this in derived classes for custom death behavior.
        /// </summary>
        protected virtual void OnEntityDeath()
        {
            Debug.Log($"[{gameObject.name}] Entity has died");
            // Can be overridden by derived classes for custom death logic
        }

        #endregion
    }
}
