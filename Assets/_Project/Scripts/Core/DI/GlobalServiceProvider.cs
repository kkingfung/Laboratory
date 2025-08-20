using System;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.DI
{
    /// <summary>
    /// Global service provider that allows ECS systems and other components to access services.
    /// This is a singleton that holds a reference to the main service container.
    /// </summary>
    public static class GlobalServiceProvider
    {
        private static IServiceContainer? _instance;

        /// <summary>
        /// Gets the global service container instance.
        /// Throws an exception if not initialized.
        /// </summary>
        public static IServiceContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new InvalidOperationException(
                        "GlobalServiceProvider has not been initialized. " +
                        "Make sure GameBootstrap has run and called Initialize().");
                }
                return _instance;
            }
        }

        /// <summary>
        /// Checks if the global service provider has been initialized.
        /// </summary>
        public static bool IsInitialized => _instance != null;

        /// <summary>
        /// Initializes the global service provider with the given service container.
        /// This should only be called once during application startup.
        /// </summary>
        /// <param name="serviceContainer">The service container to use globally</param>
        public static void Initialize(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null)
                throw new ArgumentNullException(nameof(serviceContainer));

            if (_instance != null)
            {
                Debug.LogWarning("GlobalServiceProvider is already initialized. Replacing existing instance.");
            }

            _instance = serviceContainer;
            Debug.Log("GlobalServiceProvider initialized successfully.");
        }

        /// <summary>
        /// Clears the global service provider. Should only be called during application shutdown.
        /// </summary>
        public static void Shutdown()
        {
            if (_instance != null)
            {
                Debug.Log("GlobalServiceProvider shutting down.");
                _instance = null;
            }
        }

        /// <summary>
        /// Resolves a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve</typeparam>
        /// <returns>The resolved service instance</returns>
        public static T Resolve<T>() where T : class
        {
            return Instance.Resolve<T>();
        }

        /// <summary>
        /// Tries to resolve a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve</typeparam>
        /// <param name="service">The resolved service instance, or null if resolution failed</param>
        /// <returns>True if resolution succeeded, false otherwise</returns>
        public static bool TryResolve<T>(out T? service) where T : class
        {
            if (_instance != null)
            {
                return _instance.TryResolve(out service);
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Checks if a service of the specified type is registered.
        /// </summary>
        /// <typeparam name="T">The type of service to check</typeparam>
        /// <returns>True if the service is registered, false otherwise</returns>
        public static bool IsRegistered<T>() where T : class
        {
            return _instance?.IsRegistered<T>() ?? false;
        }
    }
}
