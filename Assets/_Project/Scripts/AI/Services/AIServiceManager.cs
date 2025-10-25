using Unity.Entities;
using UnityEngine;
using System.Collections.Generic;
using System;
using Laboratory.AI.ECS;
using Laboratory.AI.Pathfinding;

namespace Laboratory.AI.Services
{
    /// <summary>
    /// AI SERVICE MANAGER - Centralized service locator and dependency injection container
    /// PURPOSE: Manage service lifecycles, provide dependency injection, enable runtime service swapping
    /// FEATURES: Service registration, lazy loading, interface binding, singleton management
    /// ARCHITECTURE: Service locator pattern with dependency injection capabilities
    /// </summary>
    [DefaultExecutionOrder(-100)] // Initialize early
    public class AIServiceManager : MonoBehaviour
    {
        private static AIServiceManager _instance;
        public static AIServiceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AIServiceManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AI Service Manager");
                        _instance = go.AddComponent<AIServiceManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        // Service registry
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _serviceFactories = new Dictionary<Type, Func<object>>();
        private readonly HashSet<Type> _initializingServices = new HashSet<Type>();

        // Performance monitoring
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private float _performanceUpdateInterval = 1f;
        private float _lastPerformanceUpdate;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDefaultServices();
        }

        private void Update()
        {
            if (_enablePerformanceMonitoring && Time.time - _lastPerformanceUpdate >= _performanceUpdateInterval)
            {
                UpdatePerformanceMetrics();
                _lastPerformanceUpdate = Time.time;
            }
        }

        private void OnDestroy()
        {
            // Dispose all disposable services
            foreach (var service in _services.Values)
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _services.Clear();
            _serviceFactories.Clear();
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                Debug.LogWarning($"Service {type.Name} is already registered. Replacing existing service.");
            }
            _services[type] = service;
        }

        /// <summary>
        /// Register a service factory for lazy instantiation
        /// </summary>
        public void RegisterServiceFactory<T>(Func<T> factory) where T : class
        {
            var type = typeof(T);
            _serviceFactories[type] = () => factory();
        }

        /// <summary>
        /// Get a service instance
        /// </summary>
        public T GetService<T>() where T : class
        {
            var type = typeof(T);

            // Return existing service if available
            if (_services.TryGetValue(type, out var existing))
            {
                return existing as T;
            }

            // Check for circular dependency
            if (_initializingServices.Contains(type))
            {
                throw new InvalidOperationException($"Circular dependency detected for service {type.Name}");
            }

            // Try to create from factory
            if (_serviceFactories.TryGetValue(type, out var factory))
            {
                _initializingServices.Add(type);
                try
                {
                    var service = factory() as T;
                    _services[type] = service;
                    return service;
                }
                finally
                {
                    _initializingServices.Remove(type);
                }
            }

            // Service not found
            Debug.LogError($"Service {type.Name} is not registered");
            return null;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool HasService<T>() where T : class
        {
            var type = typeof(T);
            return _services.ContainsKey(type) || _serviceFactories.ContainsKey(type);
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void UnregisterService<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                if (service is IDisposable disposable)
                {
                    disposable.Dispose();
                }
                _services.Remove(type);
            }
            _serviceFactories.Remove(type);
        }

        /// <summary>
        /// Replace an existing service
        /// </summary>
        public void ReplaceService<T>(T newService) where T : class
        {
            UnregisterService<T>();
            RegisterService(newService);
        }

        /// <summary>
        /// Get all registered service types
        /// </summary>
        public Type[] GetRegisteredServices()
        {
            var services = new List<Type>();
            services.AddRange(_services.Keys);
            services.AddRange(_serviceFactories.Keys);
            return services.ToArray();
        }

        /// <summary>
        /// Initialize default service implementations
        /// </summary>
        private void InitializeDefaultServices()
        {
            // Register default service factories
            RegisterServiceFactory<IPathfindingService>(() => new ECSPathfindingService());
            RegisterServiceFactory<IAIBehaviorService>(() => new ECSBehaviorService());
            RegisterServiceFactory<IFormationService>(() => new ECSFormationService());
            RegisterServiceFactory<ISpatialAwarenessService>(() => new ECSSpatialService());
            RegisterServiceFactory<ICombatService>(() => new ECSCombatService());
            RegisterServiceFactory<IEnvironmentService>(() => new ECSEnvironmentService());
            RegisterServiceFactory<ICoordinationService>(() => new ECSCoordinationService());
            RegisterServiceFactory<IPerformanceService>(() => new PerformanceService());

            Debug.Log("AI Service Manager initialized with default services");
        }

        /// <summary>
        /// Update performance metrics for all services
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            var performanceService = GetService<IPerformanceService>();
            if (performanceService != null)
            {
                // Update system performance data
                foreach (var service in _services.Values)
                {
                    if (service is IPerformanceMonitorable monitorable)
                    {
                        monitorable.UpdatePerformanceMetrics(performanceService);
                    }
                }
            }
        }

        // Static convenience methods
        public static T Get<T>() where T : class => Instance.GetService<T>();
        public static void Register<T>(T service) where T : class => Instance.RegisterService(service);
        public static void RegisterFactory<T>(Func<T> factory) where T : class => Instance.RegisterServiceFactory(factory);
    }

    /// <summary>
    /// Interface for services that provide performance monitoring
    /// </summary>
    public interface IPerformanceMonitorable
    {
        void UpdatePerformanceMetrics(IPerformanceService performanceService);
    }

    /// <summary>
    /// Service initialization attribute for automatic registration
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceInitializeAttribute : Attribute
    {
        public int Priority { get; set; } = 0;
        public bool Singleton { get; set; } = true;
    }
}