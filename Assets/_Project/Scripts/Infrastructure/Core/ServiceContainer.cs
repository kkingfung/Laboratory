using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Enhanced dependency injection container for Project Chimera.
    /// Provides service registration and resolution for loose coupling between subsystems.
    /// Enhanced with scoped services, lazy loading, and lifecycle management.
    /// </summary>
    public class ServiceContainer : MonoBehaviour
    {
        private static ServiceContainer _instance;
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<Type, Func<object>> _factories = new();
        private readonly Dictionary<Type, ServiceScope> _serviceScopes = new();
        private readonly Dictionary<Type, List<object>> _scopedServices = new();
        private readonly HashSet<Type> _initializingServices = new();

        /// <summary>
        /// Singleton instance of the service container
        /// </summary>
        public static ServiceContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ServiceContainer>();

                    if (_instance == null)
                    {
                        var go = new GameObject("ServiceContainer");
                        _instance = go.AddComponent<ServiceContainer>();
                        DontDestroyOnLoad(go);
                    }
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[ServiceContainer] Initialized");
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Registers a service instance
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
            Debug.Log($"[ServiceContainer] Registered service: {type.Name}");
        }

        /// <summary>
        /// Registers a service factory
        /// </summary>
        public void RegisterFactory<T>(Func<T> factory) where T : class
        {
            var type = typeof(T);
            _factories[type] = () => factory();
            Debug.Log($"[ServiceContainer] Registered factory: {type.Name}");
        }

        /// <summary>
        /// Resolves a service instance
        /// </summary>
        public T Resolve<T>() where T : class
        {
            var type = typeof(T);

            // Check for circular dependencies
            if (_initializingServices.Contains(type))
            {
                Debug.LogError($"[ServiceContainer] Circular dependency detected for: {type.Name}");
                return null;
            }

            // Try to get existing instance
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }

            // Try to create from factory
            if (_factories.TryGetValue(type, out var factory))
            {
                _initializingServices.Add(type);
                try
                {
                    var instance = factory() as T;

                    // Handle scoped services
                    if (_serviceScopes.TryGetValue(type, out var scope))
                    {
                        HandleScopedService(type, instance, scope);
                    }
                    else
                    {
                        _services[type] = instance; // Cache as singleton
                    }

                    return instance;
                }
                finally
                {
                    _initializingServices.Remove(type);
                }
            }

            Debug.LogWarning($"[ServiceContainer] Service not found: {type.Name}");
            return null;
        }

        /// <summary>
        /// Registers a scoped service
        /// </summary>
        public void RegisterScoped<T>(Func<T> factory, ServiceScope scope = ServiceScope.Singleton) where T : class
        {
            var type = typeof(T);
            _factories[type] = () => factory();
            _serviceScopes[type] = scope;
            Debug.Log($"[ServiceContainer] Registered scoped service: {type.Name} ({scope})");
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool IsRegistered<T>() where T : class
        {
            var type = typeof(T);
            return _services.ContainsKey(type) || _factories.ContainsKey(type);
        }

        /// <summary>
        /// Gets all registered service types
        /// </summary>
        public Type[] GetRegisteredTypes()
        {
            var types = new HashSet<Type>();
            types.UnionWith(_services.Keys);
            types.UnionWith(_factories.Keys);
            return types.ToArray();
        }

        private void HandleScopedService<T>(Type type, T instance, ServiceScope scope) where T : class
        {
            switch (scope)
            {
                case ServiceScope.Singleton:
                    _services[type] = instance;
                    break;

                case ServiceScope.Transient:
                    // Don't cache transient services
                    break;

                case ServiceScope.Scoped:
                    // Add to scoped services list
                    if (!_scopedServices.ContainsKey(type))
                        _scopedServices[type] = new List<object>();
                    _scopedServices[type].Add(instance);
                    break;
            }
        }

        /// <summary>
        /// Tries to resolve a service, returns null if not found
        /// </summary>
        public T TryResolve<T>() where T : class
        {
            try
            {
                return Resolve<T>();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Checks if a service is registered
        /// </summary>
        public bool IsRegistered<T>() where T : class
        {
            var type = typeof(T);
            return _services.ContainsKey(type) || _factories.ContainsKey(type);
        }

        /// <summary>
        /// Unregisters a service
        /// </summary>
        public void Unregister<T>() where T : class
        {
            var type = typeof(T);
            _services.Remove(type);
            _factories.Remove(type);
            Debug.Log($"[ServiceContainer] Unregistered service: {type.Name}");
        }

        /// <summary>
        /// Clears all registered services
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _factories.Clear();
            Debug.Log("[ServiceContainer] Cleared all services");
        }

        /// <summary>
        /// Gets all registered service types for debugging
        /// </summary>
        public Type[] GetRegisteredTypes()
        {
            var types = new List<Type>();
            types.AddRange(_services.Keys);
            types.AddRange(_factories.Keys);
            return types.ToArray();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Clear();
                _instance = null;
            }
        }

        #region Extension Methods for Common Patterns

        /// <summary>
        /// Registers a singleton service
        /// </summary>
        public void RegisterSingleton<T>(T instance) where T : class
        {
            Register<T>(instance);
        }

        /// <summary>
        /// Registers a transient service (new instance each time)
        /// </summary>
        public void RegisterTransient<T>(Func<T> factory) where T : class
        {
            _factories[typeof(T)] = () => factory();
        }

        /// <summary>
        /// Registers a scoped service (same instance within scope)
        /// </summary>
        public void RegisterScoped<T>(Func<T> factory) where T : class
        {
            // For simplicity, treating scoped as singleton for now
            RegisterFactory<T>(factory);
        }

        #endregion
    }

    /// <summary>Service lifetime scope</summary>
    public enum ServiceScope
    {
        /// <summary>Single instance shared across application</summary>
        Singleton,
        /// <summary>New instance created each time</summary>
        Transient,
        /// <summary>Instance shared within a scope (e.g., scene)</summary>
        Scoped
    }

    /// <summary>
    /// Static helper for easier service access
    /// </summary>
    public static class Services
    {
        /// <summary>
        /// Gets a service instance
        /// </summary>
        public static T Get<T>() where T : class
        {
            return ServiceContainer.Instance.Resolve<T>();
        }

        /// <summary>
        /// Tries to get a service instance
        /// </summary>
        public static T TryGet<T>() where T : class
        {
            return ServiceContainer.Instance.TryResolve<T>();
        }

        /// <summary>
        /// Checks if a service is available
        /// </summary>
        public static bool IsAvailable<T>() where T : class
        {
            return ServiceContainer.Instance.IsRegistered<T>();
        }
    }
}