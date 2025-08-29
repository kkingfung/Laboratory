using System;
using Laboratory.Core.Health;

namespace Laboratory.Core.Systems
{
    /// <summary>
    /// Interface for health system management across the game.
    /// Provides centralized health operations, coordination, and monitoring.
    /// Integrates with the unified service architecture.
    /// </summary>
    public interface IHealthSystem
    {
        #region Component Management
        
        /// <summary>
        /// Register a health component with the system.
        /// </summary>
        void RegisterHealthComponent(IHealthComponent healthComponent);

        /// <summary>
        /// Unregister a health component from the system.
        /// </summary>
        void UnregisterHealthComponent(IHealthComponent healthComponent);

        /// <summary>
        /// Get all registered health components.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<IHealthComponent> GetAllHealthComponents();
        
        #endregion
        
        #region Health Operations

        /// <summary>
        /// Apply damage to a specific health component.
        /// </summary>
        bool ApplyDamage(IHealthComponent target, DamageRequest damageRequest);

        /// <summary>
        /// Apply healing to a specific health component.
        /// </summary>
        bool ApplyHealing(IHealthComponent target, int amount, object source = null);
        
        #endregion
        
        #region Events

        /// <summary>
        /// Event fired when any health component takes damage.
        /// </summary>
        event Action<IHealthComponent, DamageRequest> OnDamageApplied;

        /// <summary>
        /// Event fired when any health component is healed.
        /// </summary>
        event Action<IHealthComponent, int> OnHealingApplied;

        /// <summary>
        /// Event fired when any health component dies.
        /// </summary>
        event Action<IHealthComponent> OnComponentDeath;
        
        #endregion
    }
}
