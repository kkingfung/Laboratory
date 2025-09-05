using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.DI;
using Laboratory.Core.Systems;

namespace Laboratory.Core.Health.Services
{
    /// <summary>
    /// Centralized health system service that coordinates all health-related operations.
    /// Provides registration, management, and monitoring of health components across the game.
    /// Integrates with the unified service architecture and event system.
    /// </summary>
    public class HealthSystemService : IHealthSystem, IDisposable
    {
        #region Fields

        private readonly List<IHealthComponent> _registeredComponents = new();
        private readonly Dictionary<GameObject, IHealthComponent> _componentByGameObject = new();
        private readonly List<IDisposable> _subscriptions = new();
        
        private IEventBus _eventBus;
        // Remove direct dependency on Infrastructure.DamageManager
        // private DamageManager _damageManager;
        private bool _isDisposed = false;

        #endregion

        #region Properties

        /// <summary>Total number of registered health components.</summary>
        public int TotalComponents => _registeredComponents.Count;

        /// <summary>Number of alive health components.</summary>
        public int AliveComponents => _registeredComponents.Count(c => c.IsAlive);

        /// <summary>Health system statistics.</summary>
        public HealthSystemStats Statistics { get; private set; } = new();

        #endregion

        #region Events

        public event Action<IHealthComponent, DamageRequest> OnDamageApplied;
        public event Action<IHealthComponent, int> OnHealingApplied;
        public event Action<IHealthComponent> OnComponentDeath;
        public event Action<IHealthComponent> OnComponentRegistered;
        public event Action<IHealthComponent> OnComponentUnregistered;

        #endregion

        #region Constructor

        public HealthSystemService()
        {
            Initialize();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Get dependencies from service container
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.TryResolve<IEventBus>(out _eventBus);
            }

            // _damageManager = DamageManager.Instance; // Removed to break cycle

            Statistics = new HealthSystemStats();

            if (_eventBus != null)
            {
                // Subscribe to system-wide health events
                var damageEventSub = _eventBus.Subscribe<DamageAppliedEvent>(OnGlobalDamageEvent);
                var deathEventSub = _eventBus.Subscribe<DeathEvent>(OnGlobalDeathEvent);
                
                _subscriptions.Add(damageEventSub);
                _subscriptions.Add(deathEventSub);
            }

            Debug.Log("[HealthSystemService] Initialized with event system integration");
        }

        #endregion

        #region IHealthSystem Implementation

        public void RegisterHealthComponent(IHealthComponent healthComponent)
        {
            if (healthComponent == null)
                throw new ArgumentNullException(nameof(healthComponent));

            if (_registeredComponents.Contains(healthComponent))
            {
                Debug.LogWarning($"Health component already registered: {healthComponent}");
                return;
            }

            _registeredComponents.Add(healthComponent);

            // Map by GameObject if possible
            if (healthComponent is MonoBehaviour monoBehaviour)
            {
                _componentByGameObject[monoBehaviour.gameObject] = healthComponent;
            }

            // Subscribe to component events
            healthComponent.OnHealthChanged += OnComponentHealthChanged;
            healthComponent.OnDeath += OnComponentDeathInternal;

            Statistics.TotalRegistrations++;
            OnComponentRegistered?.Invoke(healthComponent);

            _eventBus?.Publish(new HealthComponentRegisteredEvent(healthComponent));

            Debug.Log($"[HealthSystemService] Registered health component: {healthComponent}");
        }

        public void UnregisterHealthComponent(IHealthComponent healthComponent)
        {
            if (healthComponent == null) return;

            if (_registeredComponents.Remove(healthComponent))
            {
                // Unsubscribe from component events
                healthComponent.OnHealthChanged -= OnComponentHealthChanged;
                healthComponent.OnDeath -= OnComponentDeathInternal;

                // Remove from GameObject mapping
                if (healthComponent is MonoBehaviour monoBehaviour)
                {
                    _componentByGameObject.Remove(monoBehaviour.gameObject);
                }

                Statistics.TotalUnregistrations++;
                OnComponentUnregistered?.Invoke(healthComponent);

                _eventBus?.Publish(new HealthComponentUnregisteredEvent(healthComponent));

                Debug.Log($"[HealthSystemService] Unregistered health component: {healthComponent}");
            }
        }

        public bool ApplyDamage(IHealthComponent target, DamageRequest damageRequest)
        {
            if (target == null || damageRequest == null) return false;

            bool damageApplied = target.TakeDamage(damageRequest);

            if (damageApplied)
            {
                Statistics.TotalDamageApplications++;
                Statistics.TotalDamageDealt += damageRequest.Amount;
                OnDamageApplied?.Invoke(target, damageRequest);
            }

            return damageApplied;
        }

        public bool ApplyHealing(IHealthComponent target, int amount, object source = null)
        {
            if (target == null || amount <= 0) return false;

            bool healingApplied = target.Heal(amount, source);

            if (healingApplied)
            {
                Statistics.TotalHealingApplications++;
                Statistics.TotalHealingApplied += amount;
                OnHealingApplied?.Invoke(target, amount);
            }

            return healingApplied;
        }

        public IReadOnlyList<IHealthComponent> GetAllHealthComponents()
        {
            return _registeredComponents.AsReadOnly();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets health component associated with a specific GameObject.
        /// </summary>
        public IHealthComponent GetHealthComponent(GameObject gameObject)
        {
            return _componentByGameObject.TryGetValue(gameObject, out var component) ? component : null;
        }

        /// <summary>
        /// Gets all health components that match a predicate.
        /// </summary>
        public IEnumerable<IHealthComponent> GetHealthComponentsWhere(Func<IHealthComponent, bool> predicate)
        {
            return _registeredComponents.Where(predicate);
        }

        /// <summary>
        /// Gets health components within a specific health range.
        /// </summary>
        public IEnumerable<IHealthComponent> GetHealthComponentsInRange(float minHealthPercentage, float maxHealthPercentage)
        {
            return _registeredComponents.Where(c => 
                c.HealthPercentage >= minHealthPercentage && 
                c.HealthPercentage <= maxHealthPercentage);
        }

        /// <summary>
        /// Applies area damage to all registered components within range.
        /// NOTE: This method now uses events instead of direct DamageManager dependency.
        /// </summary>
        public int ApplyAreaDamage(Vector3 center, float radius, DamageRequest baseDamageRequest, LayerMask targetLayers = default)
        {
            // Use event-based damage application instead of direct DamageManager
            var areaDamageEvent = new AreaDamageEvent
            {
                Center = center,
                Radius = radius,
                BaseDamageRequest = baseDamageRequest,
                TargetLayers = targetLayers
            };
            
            _eventBus?.Publish(areaDamageEvent);
            
            // Return a count based on registered components in range
            // This is an approximation since we can't directly call DamageManager
            return GetComponentsInRadius(center, radius).Count();
        }

        /// <summary>
        /// Resets health system statistics.
        /// </summary>
        public void ResetStatistics()
        {
            Statistics = new HealthSystemStats();
        }

        /// <summary>
        /// Gets components within a radius (helper for area damage).
        /// </summary>
        private IEnumerable<IHealthComponent> GetComponentsInRadius(Vector3 center, float radius)
        {
            return _registeredComponents.Where(component =>
            {
                if (component is MonoBehaviour mb && mb != null)
                {
                    return Vector3.Distance(mb.transform.position, center) <= radius;
                }
                return false;
            });
        }

        #endregion

        #region Event Handlers

        private void OnComponentHealthChanged(HealthChangedEventArgs args)
        {
            // Update statistics
            if (args.HealthDelta < 0) // Damage
            {
                Statistics.LastDamageTime = Time.time;
            }
            else if (args.HealthDelta > 0) // Healing
            {
                Statistics.LastHealingTime = Time.time;
            }
        }

        private void OnComponentDeathInternal(DeathEventArgs args)
        {
            Statistics.TotalDeaths++;
            Statistics.LastDeathTime = Time.time;

            // Find the component that died
            var deadComponent = _registeredComponents.FirstOrDefault(c => 
                ReferenceEquals(args.Source, c) || (c is MonoBehaviour mb && ReferenceEquals(mb, args.Source)));

            if (deadComponent != null)
            {
                OnComponentDeath?.Invoke(deadComponent);
            }
        }

        private void OnGlobalDamageEvent(DamageAppliedEvent damageEvent)
        {
            // Track system-wide damage events for statistics
            Statistics.LastSystemDamageTime = Time.time;
        }

        private void OnGlobalDeathEvent(DeathEvent deathEvent)
        {
            // Track system-wide death events
            Statistics.LastSystemDeathTime = Time.time;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed) return;

            // Unsubscribe from all events
            foreach (var subscription in _subscriptions)
            {
                subscription?.Dispose();
            }
            _subscriptions.Clear();

            // Clear component registrations
            foreach (var component in _registeredComponents.ToList())
            {
                UnregisterHealthComponent(component);
            }

            _registeredComponents.Clear();
            _componentByGameObject.Clear();

            // Clear event handlers
            OnDamageApplied = null;
            OnHealingApplied = null;
            OnComponentDeath = null;
            OnComponentRegistered = null;
            OnComponentUnregistered = null;

            _isDisposed = true;

            Debug.Log("[HealthSystemService] Disposed");
        }

        #endregion
    }

    #region Supporting Data Classes

    /// <summary>
    /// Health system statistics and monitoring data.
    /// </summary>
    [Serializable]
    public class HealthSystemStats
    {
        public int TotalRegistrations { get; set; }
        public int TotalUnregistrations { get; set; }
        public int TotalDamageApplications { get; set; }
        public int TotalHealingApplications { get; set; }
        public float TotalDamageDealt { get; set; }
        public int TotalHealingApplied { get; set; }
        public int TotalDeaths { get; set; }
        
        public float LastDamageTime { get; set; } = -1f;
        public float LastHealingTime { get; set; } = -1f;
        public float LastDeathTime { get; set; } = -1f;
        public float LastSystemDamageTime { get; set; } = -1f;
        public float LastSystemDeathTime { get; set; } = -1f;

        public override string ToString()
        {
            return $"HealthSystemStats(Registrations: {TotalRegistrations}, Deaths: {TotalDeaths}, " +
                   $"Damage: {TotalDamageDealt:F1}, Healing: {TotalHealingApplied})";
        }
    }

    /// <summary>
    /// Event fired when a health component is registered with the system.
    /// </summary>
    public class HealthComponentRegisteredEvent
    {
        public IHealthComponent Component { get; }
        public DateTime Timestamp { get; }

        public HealthComponentRegisteredEvent(IHealthComponent component)
        {
            Component = component;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event fired when a health component is unregistered from the system.
    /// </summary>
    public class HealthComponentUnregisteredEvent
    {
        public IHealthComponent Component { get; }
        public DateTime Timestamp { get; }

        public HealthComponentUnregisteredEvent(IHealthComponent component)
        {
            Component = component;
            Timestamp = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event for requesting area damage through the event system.
    /// </summary>
    public class AreaDamageEvent
    {
        public Vector3 Center { get; set; }
        public float Radius { get; set; }
        public DamageRequest BaseDamageRequest { get; set; }
        public LayerMask TargetLayers { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Event fired when damage is successfully applied to any health component.
    /// </summary>
    public class DamageAppliedEvent
    {
        public GameObject Target { get; set; }
        public GameObject Source { get; set; }
        public float Amount { get; set; }
        public DamageType Type { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 SourcePosition { get; set; }
        public float Timestamp { get; set; } = Time.time;
    }

    #endregion
}
