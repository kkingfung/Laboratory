using System;
using UnityEngine;
using Laboratory.Core.Health;

#nullable enable

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// Local health component implementation for single-player or non-networked entities.
    /// Provides complete IHealthComponent implementation with events and validation.
    /// </summary>
    public class LocalHealthComponent : HealthComponentBase
    {
        // Note: Events are inherited from base class - no need to override
        
        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }
        
        private void Start()
        {
            // Initialize with max health if not already set
            if (currentHealth <= 0)
            {
                currentHealth = maxHealth;
            }
            
            lastDamageTime = -regenerationDelay;
        }
        
        protected override void Update()
        {
            base.Update();
            // Additional local-specific update logic can go here
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the health component with specified max health.
        /// </summary>
        public virtual void Initialize(int maxHealthValue = -1)
        {
            if (maxHealthValue > 0)
            {
                maxHealth = maxHealthValue;
            }
            
            currentHealth = maxHealth;
            isAlive = true;
            lastDamageTime = -regenerationDelay;
            
            Debug.Log($"[LocalHealthComponent] Initialized with {maxHealth} max health");
        }
        
        /// <summary>
        /// Gets whether this component is properly initialized.
        /// </summary>
        public bool IsInitialized => maxHealth > 0;
        
        #endregion
        
        #region Protected Methods
        
        protected override void OnEntityDied()
        {
            Debug.Log($"[LocalHealthComponent] Entity {gameObject.name} has died");
            
            // Disable colliders to prevent further interactions
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                collider.enabled = false;
            }
            
            // Optionally trigger death animation or effects
            TriggerDeathEffects();
        }
        
        /// <summary>
        /// Handles health regeneration over time.
        /// </summary>
        protected virtual void HandleRegeneration()
        {
            if (Time.time - lastDamageTime < regenerationDelay)
                return;
            
            if (currentHealth >= maxHealth)
                return;
            
            // Regenerate health over time
            float regenAmount = regenerationRate * Time.deltaTime;
            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + Mathf.RoundToInt(regenAmount));
            
            if (currentHealth != oldHealth)
            {
                var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, this);
                TriggerOnHealthChanged(eventArgs);
            }
        }
        
        /// <summary>
        /// Triggers visual and audio effects when the entity dies.
        /// </summary>
        protected virtual void TriggerDeathEffects()
        {
            // Override in derived classes to implement specific death effects
            // This could include:
            // - Particle effects
            // - Sound effects
            // - Screen shake
            // - UI notifications
        }
        
        /// <summary>
        /// Validates a damage request before applying it.
        /// </summary>
        protected virtual bool ValidateDamageRequest(DamageRequest damageRequest)
        {
            if (damageRequest == null)
                return false;
            
            if (damageRequest.Amount <= 0)
                return false;
            
            if (!isAlive)
                return false;
            
            // Add custom validation logic here
            // For example: invulnerability frames, damage immunity, etc.
            
            return true;
        }
        
        // Base class handles all event triggering - no override needed
        
        #endregion
        
        #region IHealthComponent Implementation
        
        public override bool TakeDamage(DamageRequest damageRequest)
        {
            if (!ValidateDamageRequest(damageRequest))
                return false;
            
            int oldHealth = currentHealth;
            int damageAmount = Mathf.RoundToInt(damageRequest.Amount);
            
            // Apply damage
            currentHealth = Mathf.Max(0, currentHealth - damageAmount);
            lastDamageTime = Time.time;
            
            // Trigger events
            var healthChangedArgs = new HealthChangedEventArgs(oldHealth, currentHealth, damageRequest.Source);
            TriggerOnHealthChanged(healthChangedArgs);
            TriggerOnDamageTaken(damageRequest);
            
            // Check for death
            if (currentHealth <= 0 && isAlive)
            {
                Die(damageRequest.Source, damageRequest);
            }
            
            Debug.Log($"[LocalHealthComponent] {gameObject.name} took {damageAmount} damage. Health: {currentHealth}/{maxHealth}");
            return true;
        }
        
        public override bool Heal(int amount, object? source = null)
        {
            if (amount <= 0 || !isAlive || currentHealth >= maxHealth)
                return false;
            
            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            
            var eventArgs = new HealthChangedEventArgs(oldHealth, currentHealth, source);
            TriggerOnHealthChanged(eventArgs);
            
            Debug.Log($"[LocalHealthComponent] {gameObject.name} healed for {amount}. Health: {currentHealth}/{maxHealth}");
            return true;
        }
        
        #endregion
        
        #region Inspector Debug Methods
        
        [ContextMenu("Take 25 Damage")]
        private void DebugTakeDamage()
        {
            var debugDamage = new DamageRequest(25f, gameObject, DamageType.Physical);
            TakeDamage(debugDamage);
        }
        
        [ContextMenu("Heal 25 Health")]
        private void DebugHeal()
        {
            Heal(25);
        }
        
        [ContextMenu("Reset to Max Health")]
        private void DebugResetHealth()
        {
            ResetToMaxHealth();
        }
        
        [ContextMenu("Kill Entity")]
        private void DebugKill()
        {
            Kill(null, "Debug Kill");
        }
        
        #endregion
    }
}
