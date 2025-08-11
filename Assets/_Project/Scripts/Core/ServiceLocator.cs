using System;
using System.Collections.Generic;

namespace Infrastructure
{
    /// <summary>
    /// Simple Service Locator for managing singletons and services.
    /// Use Register<T>(instance) and Resolve<T>() to access services.
    /// </summary>
    public class ServiceLocator
    {
        #region Fields

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        #endregion

        #region Register / Resolve

        /// <summary>
        /// Register a service instance of type T.
        /// </summary>
        public void Register<T>(T service)
        {
            var type = typeof(T);
            if (_services.ContainsKey(type))
            {
                throw new InvalidOperationException($"Service of type {type} is already registered.");
            }
            _services[type] = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Resolve a service instance of type T.
        /// Throws if not registered.
        /// </summary>
        public T Resolve<T>()
        {
            var type = typeof(T);
            if (!_services.TryGetValue(type, out var service))
            {
                throw new InvalidOperationException($"Service of type {type} is not registered.");
            }
            return (T)service;
        }

        /// <summary>
        /// Try to resolve a service, returns true if found.
        /// </summary>
        public bool TryResolve<T>(out T service)
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var obj))
            {
                service = (T)obj;
                return true;
            }
            service = default!;
            return false;
        }

        /// <summary>
        /// Clear all registered services.
        /// </summary>
        public void Clear()
        {
            _services.Clear();
        }

        #endregion
    }
}
