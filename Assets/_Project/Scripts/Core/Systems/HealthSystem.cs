using System;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Health;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UnityEngine;
using CoreHealthChangedEventArgs = Laboratory.Core.Health.HealthChangedEventArgs;
using CoreDeathEventArgs = Laboratory.Core.Health.DeathEventArgs;
using EventHealthChangedEventArgs = Laboratory.Core.Events.HealthChangedEventArgs;
using EventDeathEventArgs = Laboratory.Core.Events.DeathEventArgs;

#nullable enable

namespace Laboratory.Core.Systems
{
    /// <summary>
    /// Implementation of IHealthSystem that provides centralized health management.
    /// Integrates with the unified event system and service architecture.
    /// </summary>
    public class HealthSystem : IHealthSystem, IDisposable
    {
        #region Fields
        
        private readonly List<IHealthComponent> _healthComponents = new();
        private readonly IEventBus _eventBus;
        private readonly Dictionary<IHealthComponent, HealthComponentInfo> _componentInfo = new();
        private bool _disposed = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets the total number of registered health components.
        /// </summary>
        public int RegisteredComponentCount => _healthComponents.Count;
        
        /// <summary>
        /// Gets the number of alive (non-dead) health components.
        /// </summary>
        public int AliveComponentCount => _healthComponents.Count(c => !c.IsDead);
        
        #endregion
        
        #region Events
        
        public event Action<IHealthComponent, DamageRequest>? OnDamageApplied;
        public event Action<IHealthComponent, int>? OnHealingApplied;
        public event Action<IHealthComponent>? OnComponentDeath;
        
        #endregion
        
        #region Constructor
        
        public HealthSystem(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            Debug.Log("[HealthSystem] Health system initialized");
        }
        
        #endregion
        
        #region IHealthSystem Implementation
        
        public void RegisterHealthComponent(IHealthComponent healthComponent)
        {
            ThrowIfDisposed();
            
            if (healthComponent == null)
                throw new ArgumentNullException(nameof(healthComponent));
                
            if (_healthComponents.Contains(healthComponent))
            {
                Debug.LogWarning($"[HealthSystem] Health component already registered: {healthComponent}");
                return;
            }
            
            _healthComponents.Add(healthComponent);
            _componentInfo[healthComponent] = new HealthComponentInfo
            {
                RegistrationTime = DateTime.UtcNow,
                TotalDamageReceived = 0,
                TotalHealingReceived = 0,
                DamageEvents = 0,
                HealingEvents = 0
            };
            
            // Subscribe to component events
            healthComponent.OnHealthChanged += (args) => HandleHealthChanged(healthComponent, args);
            healthComponent.OnDamageTaken += (damage) => HandleDamageTaken(healthComponent, damage);
            healthComponent.OnDeath += (args) => HandleComponentDeath(healthComponent, args);
            
            Debug.Log($"[HealthSystem] Registered health component: {healthComponent} (Total: {_healthComponents.Count})");
            _eventBus.Publish(new HealthComponentRegisteredEvent(healthComponent));
        }
        
        public void UnregisterHealthComponent(IHealthComponent healthComponent)
        {
            ThrowIfDisposed();
            
            if (healthComponent == null)
                return;
                
            if (_healthComponents.Remove(healthComponent))
            {
                _componentInfo.Remove(healthComponent);
                
                Debug.Log($"[HealthSystem] Unregistered health component: {healthComponent} (Total: {_healthComponents.Count})");
                _eventBus.Publish(new HealthComponentUnregisteredEvent(healthComponent));
            }
        }
        
        public IReadOnlyList<IHealthComponent> GetAllHealthComponents()
        {
            ThrowIfDisposed();
            return _healthComponents.AsReadOnly();
        }
        
        public bool ApplyDamage(IHealthComponent target, DamageRequest damageRequest)
        {
            ThrowIfDisposed();
            
            if (target == null || damageRequest == null)
                return false;
                
            if (!_healthComponents.Contains(target))
            {
                Debug.LogWarning($"[HealthSystem] Attempted to damage unregistered health component: {target}");
                return false;
            }
            
            if (target.IsDead)
            {
                Debug.LogWarning($"[HealthSystem] Attempted to damage dead component: {target}");
                return false;
            }
            
            try
            {
                // Apply damage through the component
                bool damageApplied = target.TakeDamage(damageRequest);
                
                if (damageApplied)
                {
                    // Update statistics
                    if (_componentInfo.TryGetValue(target, out var info))
                    {
                        info.TotalDamageReceived += Mathf.RoundToInt(damageRequest.Amount);
                        info.DamageEvents++;
                        info.LastDamageTime = DateTime.UtcNow;
                    }
                    
                    // Fire events
                    OnDamageApplied?.Invoke(target, damageRequest);
                    _eventBus.Publish(new DamageAppliedEvent(target, damageRequest));
                    
                    Debug.Log($"[HealthSystem] Applied {damageRequest.Amount} damage to {target} (Health: {target.CurrentHealth}/{target.MaxHealth})");
                }
                
                return damageApplied;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HealthSystem] Error applying damage to {target}: {ex.Message}");
                return false;
            }
        }
        
        public bool ApplyHealing(IHealthComponent target, int amount, object? source = null)
        {
            ThrowIfDisposed();
            
            if (target == null || amount <= 0)
                return false;
                
            if (!_healthComponents.Contains(target))
            {
                Debug.LogWarning($"[HealthSystem] Attempted to heal unregistered health component: {target}");
                return false;
            }
            
            if (target.IsDead)
            {
                Debug.LogWarning($"[HealthSystem] Attempted to heal dead component: {target}");
                return false;
            }
            
            try
            {
                // Apply healing through the component
                bool healingApplied = target.Heal(amount);
                
                if (healingApplied)
                {
                    // Update statistics
                    if (_componentInfo.TryGetValue(target, out var info))
                    {
                        info.TotalHealingReceived += amount;
                        info.HealingEvents++;
                        info.LastHealTime = DateTime.UtcNow;
                    }
                    
                    // Fire events
                    OnHealingApplied?.Invoke(target, amount);
                    _eventBus.Publish(new HealingAppliedEvent(target, amount, source));
                    
                    Debug.Log($"[HealthSystem] Applied {amount} healing to {target} (Health: {target.CurrentHealth}/{target.MaxHealth})");
                }
                
                return healingApplied;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HealthSystem] Error applying healing to {target}: {ex.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Gets all alive health components.
        /// </summary>
        public IEnumerable<IHealthComponent> GetAliveComponents()
        {
            ThrowIfDisposed();
            return _healthComponents.Where(c => !c.IsDead);
        }
        
        /// <summary>
        /// Gets all dead health components.
        /// </summary>
        public IEnumerable<IHealthComponent> GetDeadComponents()
        {
            ThrowIfDisposed();
            return _healthComponents.Where(c => c.IsDead);
        }
        
        /// <summary>
        /// Gets health components within a damage range of a position.
        /// </summary>
        public IEnumerable<IHealthComponent> GetComponentsInRange(Vector3 position, float range)
        {
            ThrowIfDisposed();
            
            return _healthComponents.Where(component =>
            {
                if (component is MonoBehaviour mb && mb != null)
                {
                    return Vector3.Distance(mb.transform.position, position) <= range;
                }
                return false;
            });
        }
        
        /// <summary>
        /// Applies area damage to all components within range.
        /// </summary>
        public int ApplyAreaDamage(Vector3 position, float range, DamageRequest damageRequest)
        {
            ThrowIfDisposed();
            
            var targetsInRange = GetComponentsInRange(position, range).ToList();
            int damageCount = 0;
            
            foreach (var target in targetsInRange)
            {
                if (ApplyDamage(target, damageRequest))
                {
                    damageCount++;
                }
            }
            
            if (damageCount > 0)
            {
                Debug.Log($"[HealthSystem] Applied area damage to {damageCount} targets at {position} (Range: {range})");
                _eventBus.Publish(new AreaDamageAppliedEvent(position, range, damageCount, damageRequest));
            }
            
            return damageCount;
        }
        
        /// <summary>
        /// Gets statistics for a specific health component.
        /// </summary>
        public HealthComponentInfo? GetComponentStatistics(IHealthComponent component)
        {
            ThrowIfDisposed();
            return _componentInfo.TryGetValue(component, out var info) ? info : null;
        }
        
        /// <summary>
        /// Gets overall health system statistics.
        /// </summary>
        public HealthSystemStatistics GetSystemStatistics()
        {
            ThrowIfDisposed();
            
            return new HealthSystemStatistics
            {
                TotalComponents = _healthComponents.Count,
                AliveComponents = AliveComponentCount,
                DeadComponents = _healthComponents.Count - AliveComponentCount,
                TotalDamageEvents = _componentInfo.Values.Sum(i => i.DamageEvents),
                TotalHealingEvents = _componentInfo.Values.Sum(i => i.HealingEvents),
                TotalDamageDealt = _componentInfo.Values.Sum(i => i.TotalDamageReceived),
                TotalHealingDealt = _componentInfo.Values.Sum(i => i.TotalHealingReceived)
            };
        }
        
        #endregion
        
        #region Private Event Handlers
        
        private void HandleHealthChanged(IHealthComponent component, CoreHealthChangedEventArgs args)
        {
            _eventBus.Publish(new HealthChangedEvent(component, args.NewHealth, component.MaxHealth));
        }
        
        private void HandleDamageTaken(IHealthComponent component, DamageRequest damage)
        {
            _eventBus.Publish(new DamageTakenEvent(component, (int)damage.Amount));
        }
        
        private void HandleComponentDeath(IHealthComponent component, CoreDeathEventArgs args)
        {
            Debug.Log($"[HealthSystem] Component died: {component}");
            OnComponentDeath?.Invoke(component);
            _eventBus.Publish(new ComponentDeathEvent(component));
        }
        
        #endregion
        
        #region Private Utility Methods
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HealthSystem));
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                _healthComponents.Clear();
                _componentInfo.Clear();
                
                Debug.Log("[HealthSystem] Health system disposed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HealthSystem] Error during disposal: {ex.Message}");
            }
            
            _disposed = true;
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    /// <summary>
    /// Information and statistics about a health component.
    /// </summary>
    public class HealthComponentInfo
    {
        public DateTime RegistrationTime { get; set; }
        public DateTime? LastDamageTime { get; set; }
        public DateTime? LastHealTime { get; set; }
        public int TotalDamageReceived { get; set; }
        public int TotalHealingReceived { get; set; }
        public int DamageEvents { get; set; }
        public int HealingEvents { get; set; }
    }
    
    /// <summary>
    /// Overall health system statistics.
    /// </summary>
    public struct HealthSystemStatistics
    {
        public int TotalComponents { get; set; }
        public int AliveComponents { get; set; }
        public int DeadComponents { get; set; }
        public int TotalDamageEvents { get; set; }
        public int TotalHealingEvents { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalHealingDealt { get; set; }
    }
    
    #endregion
    
    #region Event Classes
    
    /// <summary>Event fired when a health component is registered.</summary>
    public class HealthComponentRegisteredEvent
    {
        public IHealthComponent Component { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public HealthComponentRegisteredEvent(IHealthComponent component)
        {
            Component = component;
        }
    }
    
    /// <summary>Event fired when a health component is unregistered.</summary>
    public class HealthComponentUnregisteredEvent
    {
        public IHealthComponent Component { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public HealthComponentUnregisteredEvent(IHealthComponent component)
        {
            Component = component;
        }
    }
    
    /// <summary>Event fired when damage is applied to a component.</summary>
    public class DamageAppliedEvent
    {
        public IHealthComponent Target { get; }
        public DamageRequest DamageRequest { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public DamageAppliedEvent(IHealthComponent target, DamageRequest damageRequest)
        {
            Target = target;
            DamageRequest = damageRequest;
        }
    }
    
    /// <summary>Event fired when healing is applied to a component.</summary>
    public class HealingAppliedEvent
    {
        public IHealthComponent Target { get; }
        public int Amount { get; }
        public object? Source { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public HealingAppliedEvent(IHealthComponent target, int amount, object? source)
        {
            Target = target;
            Amount = amount;
            Source = source;
        }
    }
    
    /// <summary>Event fired when a component's health changes.</summary>
    public class HealthChangedEvent
    {
        public IHealthComponent Component { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public HealthChangedEvent(IHealthComponent component, int currentHealth, int maxHealth)
        {
            Component = component;
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
        }
    }
    
    /// <summary>Event fired when a component takes damage.</summary>
    public class DamageTakenEvent
    {
        public IHealthComponent Component { get; }
        public int Damage { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public DamageTakenEvent(IHealthComponent component, int damage)
        {
            Component = component;
            Damage = damage;
        }
    }
    
    /// <summary>Event fired when a component dies.</summary>
    public class ComponentDeathEvent
    {
        public IHealthComponent Component { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public ComponentDeathEvent(IHealthComponent component)
        {
            Component = component;
        }
    }
    
    /// <summary>Event fired when area damage is applied.</summary>
    public class AreaDamageAppliedEvent
    {
        public Vector3 Position { get; }
        public float Range { get; }
        public int TargetsHit { get; }
        public DamageRequest DamageRequest { get; }
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        
        public AreaDamageAppliedEvent(Vector3 position, float range, int targetsHit, DamageRequest damageRequest)
        {
            Position = position;
            Range = range;
            TargetsHit = targetsHit;
            DamageRequest = damageRequest;
        }
    }
    
    #endregion
}
