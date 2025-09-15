using UnityEngine;

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// Local (single-player) health component implementation.
    /// Provides health management without network synchronization for single-player
    /// scenarios or local entities that don't need network replication.
    /// </summary>
    public class LocalHealthComponent : HealthComponentBase
    {
        #region Unity Inspector Configuration

        [Header("Local Health Settings")]
        [SerializeField] private bool _enableDebugLogging = false;

        #endregion

        #region Health Component Overrides

        public override bool TakeDamage(DamageRequest damageRequest)
        {
            bool damageApplied = base.TakeDamage(damageRequest);

            if (damageApplied && _enableDebugLogging)
            {
                Debug.Log($"[LocalHealthComponent] {gameObject.name} took {damageRequest.Amount} damage. " +
                         $"Health: {CurrentHealth}/{MaxHealth}");
            }

            return damageApplied;
        }

        public override bool Heal(int amount, object source = null)
        {
            bool healApplied = base.Heal(amount, source);

            if (healApplied && _enableDebugLogging)
            {
                Debug.Log($"[LocalHealthComponent] {gameObject.name} healed {amount}. " +
                         $"Health: {CurrentHealth}/{MaxHealth}");
            }

            return healApplied;
        }

        #endregion

        #region Protected Overrides

        protected override void OnEntityDied()
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[LocalHealthComponent] {gameObject.name} has died");
            }

            // Local-specific death behavior can be added here
            // For example: trigger death animation, drop items, etc.
        }

        #endregion
    }
}
