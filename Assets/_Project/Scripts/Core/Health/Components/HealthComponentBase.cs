using System;
using UnityEngine;
using Laboratory.Core.Health;
using Laboratory.Core.Enums;

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// Base health component for all entities in the game
    /// </summary>
    public abstract class HealthComponentBase : MonoBehaviour, IHealthComponent
    {
        #region Fields

        [Header("Health Settings")]
        [SerializeField] protected int maxHealth = 100;
        [SerializeField] protected int currentHealth;
        [SerializeField] protected bool canRegenerate = false;
        [SerializeField] protected float regenerationRate = 1f;
        [SerializeField] protected float regenerationDelay = 3f;

        protected float lastDamageTime;
        protected bool isAlive = true;

        #endregion

        #region Protected Event Triggers

        /// <summary>
        /// Triggers the OnHealthChanged event
        /// </summary>
        protected virtual void TriggerOnHealthChanged(HealthChangedEventArgs args)
        {
            OnHealthChanged?.Invoke(args);
        }

        /// <summary>
        /// Triggers the OnDeath event
        /// </summary>
        protected virtual void TriggerOnDeath(DeathEventArgs args)
        {
            OnDeath?.Invoke(args);
        }

        /// <summary>
        /// Triggers the OnDamageTaken event
        /// </summary>
        protected virtual void TriggerOnDamageTaken(DamageRequest damageRequest)
        {
            OnDamageTaken?.Invoke(damageRequest);
        }

        #endregion

        #region Properties

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float HealthPercentage => maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
        public bool IsAlive => isAlive && currentHealth > 0f;
        public bool IsDead => !IsAlive;
        public bool CanRegenerate => canRegenerate;

        #endregion

        #region Events

        public event Action<HealthChangedEventArgs> OnHealthChanged;
        public event Action<DeathEventArgs> OnDeath;
        public event Action<DamageRequest> OnDamageTaken;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            isAlive = true;
        }

        protected virtual void Update()
        {
            if (canRegenerate && isAlive && currentHealth < maxHealth)
            {
                if (Time.time - lastDamageTime >= regenerationDelay)
                {
                    int regenAmount = Mathf.RoundToInt(regenerationRate * Time.deltaTime);
                    if (regenAmount > 0)
                    {
                        Heal(regenAmount);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply damage to this health component
        /// </summary>
        public virtual bool TakeDamage(DamageRequest damageRequest)
        {
            if (!isAlive || damageRequest == null) return false;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - Mathf.RoundToInt(damageRequest.Amount));
            lastDamageTime = Time.time;

            // Trigger damage taken event
            TriggerOnDamageTaken(damageRequest);

            var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, damageRequest.Source);
            TriggerOnHealthChanged(eventArgs);

            if (currentHealth <= 0 && isAlive)
            {
                Die(damageRequest.Source, damageRequest);
            }

            return true;
        }

        /// <summary>
        /// Heal this health component
        /// </summary>
        public virtual bool Heal(int amount, object source = null)
        {
            if (!isAlive || amount <= 0) return false;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);

            lastDamageTime = Time.time;

            var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, source);
            TriggerOnHealthChanged(eventArgs);

            return true;
        }

        /// <summary>
        /// Set the maximum health
        /// </summary>
        public virtual void SetMaxHealth(int newMaxHealth)
        {
            if (newMaxHealth <= 0) return;

            int oldHealth = currentHealth;
            float healthPercentage = HealthPercentage;
            
            maxHealth = newMaxHealth;
            currentHealth = Mathf.RoundToInt(maxHealth * healthPercentage);

            var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, null);
            TriggerOnHealthChanged(eventArgs);
        }

        /// <summary>
        /// Restore to full health
        /// </summary>
        public virtual void ResetToMaxHealth()
        {
            int oldHealth = currentHealth;
            currentHealth = maxHealth;

            var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, null);
            TriggerOnHealthChanged(eventArgs);
        }

        /// <summary>
        /// Kill this entity instantly
        /// </summary>
        public virtual void Kill(GameObject killer = null, string cause = "Killed")
        {
            if (!isAlive) return;

            int oldHealth = currentHealth;
            currentHealth = 0;

            var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, null);
            TriggerOnHealthChanged(eventArgs);

            // Create a temporary damage request for the death
            var deathDamage = new DamageRequest
            {
                Amount = oldHealth,
                Source = killer,
                Type = DamageType.Physical
            };

            Die(killer, deathDamage);
        }

        /// <summary>
        /// Revive this entity
        /// </summary>
        public virtual void Revive(int healthAmount = -1)
        {
            if (isAlive) return;

            isAlive = true;
            int oldHealth = currentHealth;
            currentHealth = healthAmount < 0 ? maxHealth : Mathf.Min(maxHealth, healthAmount);

            var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, null);
            TriggerOnHealthChanged(eventArgs);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Handle death logic
        /// </summary>
        protected virtual void Die(object source = null, DamageRequest finalDamage = null)
        {
            if (!isAlive) return;

            isAlive = false;

            var deathArgs = new DeathEventArgs(source, finalDamage);

            TriggerOnDeath(deathArgs);
            OnEntityDied();
        }

        /// <summary>
        /// Override this to implement custom death behavior
        /// </summary>
        protected abstract void OnEntityDied();

        #endregion
    }
}
