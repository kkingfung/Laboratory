// File: Core/Health/IHealthComponent.cs
using System;
using UnityEngine;

namespace Laboratory.Core.Health
{
    /// <summary>
    /// Common interface for all health components in the game.
    /// Provides a unified API for health management across different systems.
    /// </summary>
    public interface IHealthComponent
    {
        /// <summary>Current health value.</summary>
        int CurrentHealth { get; }
        
        /// <summary>Maximum health value.</summary>
        int MaxHealth { get; }
        
        /// <summary>Whether this entity is currently alive (health > 0).</summary>
        bool IsAlive { get; }
        
        /// <summary>Whether this entity is dead (health <= 0).</summary>
        bool IsDead { get; }
        
        /// <summary>Health as a normalized percentage (0.0 to 1.0).</summary>
        float HealthPercentage { get; }
        
        /// <summary>Event fired when health changes.</summary>
        event Action<HealthChangedEventArgs> OnHealthChanged;
        
        /// <summary>Event fired when entity dies (health reaches 0).</summary>
        event Action<DeathEventArgs> OnDeath;
        
        /// <summary>Event fired when damage is taken.</summary>
        event Action<DamageRequest> OnDamageTaken;
        
        /// <summary>Applies damage to this health component.</summary>
        bool TakeDamage(DamageRequest damageRequest);
        
        /// <summary>Heals this health component.</summary>
        bool Heal(int amount, object source = null);
        
        /// <summary>Resets health to maximum value.</summary>
        void ResetToMaxHealth();
    }

    /// <summary>Event arguments for health change events.</summary>
    public class HealthChangedEventArgs : EventArgs
    {
        public int OldHealth { get; }
        public int NewHealth { get; }
        public int HealthDelta => NewHealth - OldHealth;
        public object Source { get; }
        
        public HealthChangedEventArgs(int oldHealth, int newHealth, object source = null)
        {
            OldHealth = oldHealth;
            NewHealth = newHealth;
            Source = source;
        }
    }

    /// <summary>Event arguments for death events.</summary>
    public class DeathEventArgs : EventArgs
    {
        public object Source { get; }
        public DamageRequest FinalDamage { get; }
        
        public DeathEventArgs(object source, DamageRequest finalDamage = null)
        {
            Source = source;
            FinalDamage = finalDamage;
        }
    }
}
