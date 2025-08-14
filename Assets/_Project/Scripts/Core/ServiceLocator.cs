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

        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

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
        /// Checks if a service of the specified type is registered.
        /// </summary>
        public bool IsRegistered<T>()
        {
            return _services.ContainsKey(typeof(T));
        }

        #endregion
    }
}
