using System;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;
using Laboratory.Core.Systems;
using Laboratory.Core.Health;

// Use DamageType from the Core.Health namespace to avoid ambiguity
using DamageRequest = Laboratory.Core.Health.DamageRequest;
using DamageType = Laboratory.Core.Health.DamageType;

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// Enhanced base implementation of IHealthComponent providing comprehensive functionality.
    /// All health components should inherit from this to ensure consistent behavior.
    /// 
    /// Version 2.0 - Enhanced with better validation, events, and error handling
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
        [SerializeField] protected bool _autoRegisterWithSystem = true;
        
        [Header("Healing Settings")]
        [SerializeField] protected bool _canHeal = true;
        [SerializeField] protected bool _allowOverheal = false;
        [SerializeField] protected float _healingMultiplier = 1.0f;
        
        [Header("Status Effects")]
        [SerializeField] protected bool _immuneToInstantKill = false;
        [SerializeField] protected DamageType[] _immuneDamageTypes = new DamageType[0];
        
        [Header("Events")]
        [SerializeField] protected bool _publishGlobalEvents = true;

        private protected float _lastDamageTime = -1f;
        private protected float _lastHealTime = -1f;
        private IEventBus _eventBus;
        private IHealthSystem _healthSystem;

        // Stats tracking
        private int _totalDamageReceived = 0;
        private int _totalHealingReceived = 0;
        private int _timesDamaged = 0;
        private int _timesHealed = 0;

        #endregion

        #region Properties

        public int CurrentHealth => _currentHealth;
        public int MaxHealth => _maxHealth;
        public bool IsAlive => _currentHealth > 0;
        public float HealthPercentage => _maxHealth > 0 ? (float)_currentHealth / _maxHealth : 0f;
        
        protected bool IsInvulnerable => _invulnerabilityDuration > 0f && 
                                       Time.time - _lastDamageTime < _invulnerabilityDuration;

        /// <summary>Gets comprehensive health statistics</summary>
        public HealthStats GetHealthStats() => new HealthStats
        {
            CurrentHealth = _currentHealth,
            MaxHealth = _maxHealth,
            HealthPercentage = HealthPercentage,
            TotalDamageReceived = _totalDamageReceived,
            TotalHealingReceived = _totalHealingReceived,
            TimesDamaged = _timesDamaged,
            TimesHealed = _timesHealed,
            LastDamageTime = _lastDamageTime,
            LastHealTime = _lastHealTime,
            IsInvulnerable = IsInvulnerable
        };

        #endregion

        #region Events

        public event Action<HealthChangedEventArgs> OnHealthChanged;
        public event Action<DeathEventArgs> OnDeath;
        public event Action<DamagePreventedEventArgs> OnDamagePrevented;
        public event Action<HealingAppliedEventArgs> OnHealingApplied;
        public event Action<HealthStatsChangedEventArgs> OnStatsChanged;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            InitializeHealth();
            InjectDependencies();
            ValidateConfiguration();
        }
        
        protected virtual void Start()
        {
            // Auto-register with health system if enabled
            if (_autoRegisterWithSystem && _healthSystem != null)
            {
                _healthSystem.RegisterHealthComponent(this);
            }
        }

        protected virtual void OnDestroy()
        {
            // Auto-unregister from health system
            if (_healthSystem != null)
            {
                _healthSystem.UnregisterHealthComponent(this);
            }
            
            ClearEventSubscriptions();
        }

        #endregion

        #region IHealthComponent Implementation

        public virtual bool TakeDamage(DamageRequest damageRequest)
        {
            // Validate damage request
            if (!ValidateDamageRequest(damageRequest))
                return false;

            if (!CanTakeDamage(damageRequest))
            {
                PublishDamagePrevented(damageRequest, "Cannot take damage");
                return false;
            }

            int oldHealth = _currentHealth;
            int damage = Mathf.RoundToInt(damageRequest.Amount);
            
            // Apply damage processing (can be overridden)
            damage = ProcessDamage(damage, damageRequest);
            
            if (damage <= 0)
            {
                PublishDamagePrevented(damageRequest, "Damage reduced to 0 or below");
                return false;
            }

            // Apply damage
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            _lastDamageTime = Time.time;
            
            // Update statistics
            _totalDamageReceived += damage;
            _timesDamaged++;

            // Fire events
            var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, damageRequest.Source);
            OnHealthChanged?.Invoke(healthChangedArgs);
            
            // Publish to event bus for UI and other systems
            if (_publishGlobalEvents && _eventBus != null)
            {
                _eventBus.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));
            }

            // Update stats
            PublishStatsChanged();

            // Handle death
            if (_currentHealth <= 0 && oldHealth > 0)
            {
                HandleDeath(damageRequest);
            }

            OnDamageApplied(damageRequest, damage, oldHealth);
            return true;
        }
        
        /// <summary>
        /// Applies damage directly to the health component.
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply</param>
        /// <param name="source">Source of the damage</param>
        /// <returns>True if damage was applied successfully</returns>
        public virtual bool ApplyDamage(int damageAmount, object source = null)
        {
            var damageRequest = new DamageRequest
            {
                Amount = damageAmount,
                Source = source,
                Type = DamageType.Normal,
                Direction = Vector3.zero,
                TriggerInvulnerability = true
            };
            return TakeDamage(damageRequest);
        }

        public virtual bool Heal(int amount, object source = null)
        {
            if (!ValidateHealingRequest(amount, source))
                return false;

            if (!CanHeal(amount, source))
                return false;

            int oldHealth = _currentHealth;
            
            // Apply healing multiplier
            int actualHealAmount = Mathf.RoundToInt(amount * _healingMultiplier);
            
            // Process healing (can be overridden)
            actualHealAmount = ProcessHealing(actualHealAmount, source);
            
            if (actualHealAmount <= 0)
                return false;

            // Apply healing
            int targetHealth = _allowOverheal ? 
                oldHealth + actualHealAmount : 
                Mathf.Min(_maxHealth, oldHealth + actualHealAmount);
            
            _currentHealth = targetHealth;
            _lastHealTime = Time.time;
            
            // Update statistics
            _totalHealingReceived += actualHealAmount;
            _timesHealed++;

            // Fire events
            var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, source);
            OnHealthChanged?.Invoke(healthChangedArgs);
            
            var healingArgs = new HealingAppliedEventArgs(actualHealAmount, oldHealth, _currentHealth, source);
            OnHealingApplied?.Invoke(healingArgs);
            
            if (_publishGlobalEvents && _eventBus != null)
            {
                _eventBus.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));
            }

            PublishStatsChanged();
            OnHealingAppliedBehavior(healingArgs);
            return true;
        }

        public virtual void ResetToMaxHealth()
        {
            int oldHealth = _currentHealth;
            _currentHealth = _maxHealth;

            if (oldHealth != _currentHealth)
            {
                var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, this);
                OnHealthChanged?.Invoke(healthChangedArgs);
                
                if (_publishGlobalEvents && _eventBus != null)
                {
                    _eventBus.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));
                }
                
                PublishStatsChanged();
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
            int oldMaxHealth = _maxHealth;
            _maxHealth = newMaxHealth;
            
            // Ensure current health doesn't exceed new max (unless overheal is allowed)
            if (!_allowOverheal && _currentHealth > _maxHealth)
            {
                _currentHealth = _maxHealth;
            }

            // Fire event if current health changed
            if (oldHealth != _currentHealth)
            {
                var healthChangedArgs = new HealthChangedEventArgs(oldHealth, _currentHealth, this);
                OnHealthChanged?.Invoke(healthChangedArgs);
                
                if (_publishGlobalEvents && _eventBus != null)
                {
                    _eventBus.Publish(new HealthChangedEvent(_currentHealth, _maxHealth, gameObject));
                }
            }

            // Always fire stats changed when max health changes
            PublishStatsChanged();
            OnMaxHealthChanged(oldMaxHealth, newMaxHealth);
        }

        /// <summary>
        /// Instantly kills the entity, bypassing normal damage processing.
        /// </summary>
        public virtual bool InstantKill(object source = null)
        {
            if (_immuneToInstantKill)
            {
                Debug.LogWarning($"InstantKill blocked - {gameObject.name} is immune to instant kill");
                return false;
            }

            if (!IsAlive)
                return false;

            int oldHealth = _currentHealth;
            _currentHealth = 0;
            
            var killRequest = new DamageRequest
            {
                Amount = oldHealth,
                Source = source,
                Type = DamageType.InstantKill,
                Direction = Vector3.zero
            };

            HandleDeath(killRequest);
            
            // Fire health changed event
            var healthChangedArgs = new HealthChangedEventArgs(oldHealth, 0, source);
            OnHealthChanged?.Invoke(healthChangedArgs);
            
            if (_publishGlobalEvents && _eventBus != null)
            {
                _eventBus.Publish(new HealthChangedEvent(0, _maxHealth, gameObject));
            }
            
            PublishStatsChanged();
            return true;
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
                
            // Ensure max health is positive
            if (_maxHealth <= 0)
            {
                Debug.LogWarning($"Invalid max health on {gameObject.name}. Setting to 100.");
                _maxHealth = 100;
                _currentHealth = 100;
            }
        }

        /// <summary>
        /// Determines if this component can take damage from the request.
        /// </summary>
        protected virtual bool CanTakeDamage(DamageRequest damageRequest)
        {
            if (!_canTakeDamage)
                return false;
                
            if (!IsAlive)
                return false;
                
            if (damageRequest.Amount <= 0)
                return false;
                
            if (damageRequest.TriggerInvulnerability && IsInvulnerable)
                return false;
                
            // Check damage type immunity
            if (_immuneDamageTypes.Length > 0)
            {
                foreach (var immuneType in _immuneDamageTypes)
                {
                    if (damageRequest.Type == immuneType)
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if this component can heal from the request.
        /// </summary>
        protected virtual bool CanHeal(int amount, object source)
        {
            if (!_canHeal)
                return false;
                
            if (amount <= 0)
                return false;
                
            if (!IsAlive && !_allowOverheal)
                return false;
                
            if (!_allowOverheal && _currentHealth >= _maxHealth)
                return false;

            return true;
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
        /// Process and modify healing before application. Override for healing bonuses/penalties.
        /// </summary>
        protected virtual int ProcessHealing(int healing, object source)
        {
            // Base implementation applies healing directly
            // Override in derived classes for healing bonuses, etc.
            return healing;
        }

        /// <summary>
        /// Handle entity death. Override for custom death behavior.
        /// </summary>
        protected virtual void HandleDeath(DamageRequest finalDamage)
        {
            var deathArgs = new DeathEventArgs(finalDamage.Source, finalDamage);
            OnDeath?.Invoke(deathArgs);
            
            if (_publishGlobalEvents && _eventBus != null)
            {
                _eventBus.Publish(new DeathEvent(gameObject, finalDamage.Source as GameObject));
            }

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

        /// <summary>
        /// Called when damage is successfully applied. Override for custom reactions.
        /// </summary>
        protected virtual void OnDamageApplied(DamageRequest request, int actualDamage, int oldHealth)
        {
            // Override in derived classes for specific damage reactions
        }

        /// <summary>
        /// Called when healing is successfully applied. Override for custom reactions.
        /// </summary>
        protected virtual void OnHealingAppliedBehavior(HealingAppliedEventArgs args)
        {
            // Override in derived classes for specific healing reactions
        }

        /// <summary>
        /// Called when max health changes. Override for custom logic.
        /// </summary>
        protected virtual void OnMaxHealthChanged(int oldMaxHealth, int newMaxHealth)
        {
            // Override in derived classes for max health change reactions
        }

        #endregion

        #region Protected Event Triggers
        
        /// <summary>
        /// Triggers the OnHealthChanged event from derived classes.
        /// </summary>
        protected void TriggerHealthChangedEvent(HealthChangedEventArgs args)
        {
            OnHealthChanged?.Invoke(args);
        }
        
        /// <summary>
        /// Triggers the OnDeath event from derived classes.
        /// </summary>
        protected void TriggerDeathEvent(DeathEventArgs args)
        {
            OnDeath?.Invoke(args);
        }
        
        #endregion

        #region Private Methods

        private void InjectDependencies()
        {
            // Try to get services from global service provider
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance?.TryResolve<IEventBus>(out _eventBus);
                GlobalServiceProvider.Instance?.TryResolve<IHealthSystem>(out _healthSystem);
            }
        }

        private void ValidateConfiguration()
        {
            if (_maxHealth <= 0)
            {
                Debug.LogError($"Invalid max health configuration on {gameObject.name}: {_maxHealth}");
                _maxHealth = 100;
            }

            if (_invulnerabilityDuration < 0)
            {
                Debug.LogWarning($"Negative invulnerability duration on {gameObject.name}: {_invulnerabilityDuration}");
                _invulnerabilityDuration = 0;
            }

            if (_healingMultiplier < 0)
            {
                Debug.LogWarning($"Negative healing multiplier on {gameObject.name}: {_healingMultiplier}");
                _healingMultiplier = 1.0f;
            }
        }

        private bool ValidateDamageRequest(DamageRequest request)
        {
            if (request == null)
            {
                Debug.LogError("Null damage request");
                return false;
            }

            if (request.Amount < 0)
            {
                Debug.LogWarning($"Negative damage amount: {request.Amount}");
                return false;
            }

            return true;
        }

        private bool ValidateHealingRequest(int amount, object source)
        {
            if (amount < 0)
            {
                Debug.LogWarning($"Negative healing amount: {amount}");
                return false;
            }

            return true;
        }

        private void PublishDamagePrevented(DamageRequest request, string reason)
        {
            var args = new DamagePreventedEventArgs(request, reason, gameObject);
            OnDamagePrevented?.Invoke(args);
        }

        private void PublishStatsChanged()
        {
            var args = new HealthStatsChangedEventArgs(GetHealthStats(), gameObject);
            OnStatsChanged?.Invoke(args);
        }

        private void ClearEventSubscriptions()
        {
            OnHealthChanged = null;
            OnDeath = null;
            OnDamagePrevented = null;
            OnHealingApplied = null;
            OnStatsChanged = null;
        }

        #endregion
    }

    #region Event Data Classes and Enums

    /// <summary>
    /// Comprehensive health statistics.
    /// </summary>
    [System.Serializable]
    public struct HealthStats
    {
        public int CurrentHealth;
        public int MaxHealth;
        public float HealthPercentage;
        public int TotalDamageReceived;
        public int TotalHealingReceived;
        public int TimesDamaged;
        public int TimesHealed;
        public float LastDamageTime;
        public float LastHealTime;
        public bool IsInvulnerable;
    }

    /// <summary>
    /// Event arguments for damage prevention.
    /// </summary>
    public class DamagePreventedEventArgs : EventArgs
    {
        public DamageRequest OriginalRequest { get; }
        public string Reason { get; }
        public GameObject Target { get; }

        public DamagePreventedEventArgs(DamageRequest request, string reason, GameObject target)
        {
            OriginalRequest = request;
            Reason = reason;
            Target = target;
        }
    }

    /// <summary>
    /// Event arguments for healing application.
    /// </summary>
    public class HealingAppliedEventArgs : EventArgs
    {
        public int Amount { get; }
        public int OldHealth { get; }
        public int NewHealth { get; }
        public object Source { get; }

        public HealingAppliedEventArgs(int amount, int oldHealth, int newHealth, object source)
        {
            Amount = amount;
            OldHealth = oldHealth;
            NewHealth = newHealth;
            Source = source;
        }
    }

    /// <summary>
    /// Event arguments for health statistics changes.
    /// </summary>
    public class HealthStatsChangedEventArgs : EventArgs
    {
        public HealthStats Stats { get; }
        public GameObject Target { get; }

        public HealthStatsChangedEventArgs(HealthStats stats, GameObject target)
        {
            Stats = stats;
            Target = target;
        }
    }

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