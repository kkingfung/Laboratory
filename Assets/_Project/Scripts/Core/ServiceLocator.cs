using System;
using System.Collections.Generic;

namespace Laboratory.Core
{
    /// <summary>
    /// Simple service locator for dependency management.
    /// </summary>
    public class ServiceLocator
    {
        #region Fields

        private static ServiceLocator _instance;
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        #endregion

        #region Singleton

        /// <summary>
        /// Gets the singleton instance of the ServiceLocator.
        /// </summary>
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ServiceLocator();
                }
                return _instance;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a service instance for the specified type.
        /// </summary>
        public void Register<T>(T instance)
        {
            _services[typeof(T)] = instance;
        }

        /// <summary>
        /// Gets a registered service instance by type.
        /// </summary>
        public T Get<T>()
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return (T)service;
            }
            throw new InvalidOperationException($"Service of type {typeof(T)} not registered.");
        }

        /// <summary>
        /// Resolves a registered service instance by type. Alias for Get&lt;T&gt;().
        /// </summary>
        public T Resolve<T>()
        {
            return Get<T>();
        }

        /// <summary>
        /// Checks if a service of the specified type is registered.
        /// </summary>
        public bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Tries to resolve a registered service instance by type.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve</typeparam>
        /// <param name="service">The resolved service instance, or default if not found</param>
        /// <returns>True if the service was found and resolved, false otherwise</returns>
        public bool TryResolve<T>(out T service)
        {
            if (_services.TryGetValue(typeof(T), out var serviceObj))
            {
                service = (T)serviceObj;
                return true;
            }
            service = default(T);
            return false;
        }

        #endregion
    }
}
