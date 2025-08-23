using System;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// Base implementation of IHealthComponent providing common functionality.
    /// All health components should inherit from this to ensure consistent behavior.
    /// </summary>
    [System.Serializable]
    public abstract class HealthComponentBase : MonoBehaviour, IHealthComponent
    {
        #region Fields

        [Header("Health Configuration")]
        [SerializeField] protected int _maxHealth = 100;
        [SerializeField] protected int _currentHealth;
        
        [Header("Damage Settings")]
        [SerializeField] protected bool _canTakeDamage = true;
        [SerializeField] protected float _invulnerabilityDuration = 0.5f;
        
        private float _lastDamageTime = -1f;
        private IEventBus _eventBus;

        #endregion

        #region Properties

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public float HealthPercentage => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 0f;
        protected bool IsInvulnerable => _invulnerabilityDuration > 0f && 
                                       Time.time - _lastDamageTime < _invulnerabilityDuration;

        #endregion

        #region Events

        public event Action<HealthChangedEventArgs> OnHealthChanged;
        public event Action<DeathEventArgs> OnDeath;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeHealth();
            InjectDependencies();
        }

        protected virtual void OnDestroy()
        {
            OnHealthChanged = null;
            OnDeath = null;
        }

        #endregion

        #region IHealthComponent Implementation

        public virtual bool TakeDamage(DamageRequest damageRequest)
        {
            if (!CanTakeDamage(damageRequest))
                return false;

            int oldHealth = _currentHealth;
            int damage = Mathf.RoundToInt(damageRequest.Amount);
            
            // Apply damage processing (can be overridden)
            damage = ProcessDamage(damage, damageRequest);
            
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            _lastDamageTime = Time.time;

            // Fire events
            var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, damageRequest.Source);
            OnHealthChanged?.Invoke(healthChangedArgs);
            
            // Publish to event bus for UI and other systems
            _eventBus?.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));

            // Handle death
            if (_currentHealth <= 0 && oldHealth > 0)
            {
                HandleDeath(damageRequest);
            }

            return true;
        }

        public virtual bool Heal(int amount, object source = null)
        {
            if (amount <= 0 || !IsAlive)
                return false;

            int oldHealth = _currentHealth;
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);

            if (oldHealth != _currentHealth)
            {
                var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, source);
                OnHealthChanged?.Invoke(healthChangedArgs);
                
                _eventBus?.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));
                return true;
            }

            return false;
        }

        public virtual void ResetToMaxHealth()
        {
            int oldHealth = _currentHealth;
            _currentHealth = _maxHealth;

            if (oldHealth != _currentHealth)
            {
                var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, this);
                OnHealthChanged?.Invoke(healthChangedArgs);
                
                _eventBus?.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));
            }
        }

        /// <summary>
        /// Sets the maximum health value. Current health will be adjusted if it exceeds the new max.
        /// </summary>
        public virtual void SetMaxHealth(int newMaxHealth)
        {
            if (newMaxHealth <= 0)
            {
                Debug.LogWarning($"SetMaxHealth: Invalid max health value {newMaxHealth}. Must be > 0.");
                return;
            }

            int oldHealth = _currentHealth;
            _maxHealth = newMaxHealth;
            
            // Ensure current health doesn't exceed new max
            if (_currentHealth > _maxHealth)
            {
                _currentHealth = _maxHealth;
            }

            // Fire event if current health changed
            if (oldHealth != _currentHealth)
            {
                var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, this);
                OnHealthChanged?.Invoke(healthChangedArgs);
                
                _eventBus?.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));
            }
        }

        #endregion

        #region Protected Virtual Methods

        /// <summary>
        /// Initialize health values. Override for custom initialization logic.
        /// </summary>
        protected virtual void InitializeHealth()
        {
            if (_currentHealth <= 0)
                _currentHealth = _maxHealth;
        }

        /// <summary>
        /// Determines if this component can take damage from the request.
        /// </summary>
        protected virtual bool CanTakeDamage(DamageRequest damageRequest)
        {
            return _canTakeDamage && 
                   IsAlive && 
                   damageRequest.Amount > 0 && 
                   (!damageRequest.TriggerInvulnerability || !IsInvulnerable);
        }

        /// <summary>
        /// Process and modify damage before application. Override for damage reduction/amplification.
        /// </summary>
        protected virtual int ProcessDamage(int damage, DamageRequest damageRequest)
        {
            // Base implementation applies damage directly
            // Override in derived classes for armor, resistances, etc.
            return damage;
        }

        /// <summary>
        /// Handle entity death. Override for custom death behavior.
        /// </summary>
        protected virtual void HandleDeath(DamageRequest finalDamage)
        {
            var deathArgs = new DeathEventArgs(finalDamage.Source, finalDamage);
            OnDeath?.Invoke(deathArgs);
            
            _eventBus?.Publish(new DeathEvent(gameObject, finalDamage.Source as GameObject));

            // Default death behavior
            OnDeathBehavior();
        }

        /// <summary>
        /// Override for specific death behavior (disable components, play animation, etc.)
        /// </summary>
        protected virtual void OnDeathBehavior()
        {
            // Default: disable this component
            enabled = false;
        }

        #endregion

        #region Private Methods

        private void InjectDependencies()
        {
            // Try to get event bus from global service provider
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Services?.TryResolve<IEventBus>(out _eventBus);
            }
        }

        #endregion
    }

    #region Event Data Classes

    /// <summary>
    /// Event published when health changes for UI and other systems.
    /// </summary>
    public class HealthChangedEvent
    {
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public GameObject Target { get; }
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;

        public HealthChangedEvent(int currentHealth, int maxHealth, GameObject target)
        {
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Target = target;
        }
    }

    /// <summary>
    /// Event published when an entity dies for game systems.
    /// </summary>
    public class DeathEvent
    {
        public GameObject Target { get; }
        public GameObject Source { get; }

        public DeathEvent(GameObject target, GameObject source)
        {
            Target = target;
            Source = source;
        }
    }

    #endregion
}
