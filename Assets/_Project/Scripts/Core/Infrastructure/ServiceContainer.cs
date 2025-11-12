using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Core dependency injection container for Project Chimera.
    /// Provides basic service registration and resolution.
    /// </summary>
    public class ServiceContainer : MonoBehaviour
    {
        private static ServiceContainer _instance;
        private readonly Dictionary<Type, object> _services = new();

        public static ServiceContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ServiceContainer>();
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
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Register a service instance
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
        }

        /// <summary>
        /// Resolve a service instance
        /// </summary>
        public T ResolveService<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            return null;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsServiceRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void UnregisterService<T>()
        {
            _services.Remove(typeof(T));
        }

        /// <summary>
        /// Clear all services
        /// </summary>
        public void ClearServices()
        {
            _services.Clear();
        }

        /// <summary>
        /// Get all registered services of a specific type
        /// </summary>
        public IEnumerable<T> GetServices<T>() where T : class
        {
            var results = new List<T>();
            foreach (var service in _services.Values)
            {
                if (service is T typedService)
                {
                    results.Add(typedService);
                }
            }
            return results;
        }

        /// <summary>
        /// Resolve a service by name or key
        /// </summary>
        public T ResolveService<T>(string name) where T : class
        {
            // For now, just return the first service of type T
            // In a full implementation, this would use a name-based lookup
            return ResolveService<T>();
        }

        /// <summary>
        /// Try to resolve a service instance, returns false if not found
        /// </summary>
        public bool TryResolve<T>(out T service) where T : class
        {
            service = ResolveService<T>();
            return service != null;
        }

        /// <summary>
        /// Register a service instance (alias for RegisterService)
        /// </summary>
        public void RegisterInstance<T>(T instance) where T : class
        {
            RegisterService<T>(instance);
        }

        /// <summary>
        /// Try to resolve a service with specific name/key
        /// </summary>
        public bool TryResolveService<T>(string name, out T service) where T : class
        {
            // For now, just return the first service of type T
            // In a full implementation, this would use a name-based lookup
            service = ResolveService<T>();
            return service != null;
        }

        /// <summary>
        /// Register a service with generic registration pattern
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            RegisterService<T>(service);
        }
    }
}