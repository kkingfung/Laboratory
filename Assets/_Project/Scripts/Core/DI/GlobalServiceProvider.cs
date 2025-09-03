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

#if UNITY_EDITOR
        /// <summary>
        /// Validates that all core services are properly registered.
        /// This is useful for development and debugging.
        /// </summary>
        /// <returns>True if all core services are registered, false otherwise</returns>
        public static bool ValidateCoreServices()
        {
            if (!IsInitialized)
            {
                Debug.LogError("VALIDATION FAILED: GlobalServiceProvider is not initialized");
                return false;
            }

            bool allServicesRegistered = true;
            
            // Check Laboratory.Core.Events.IEventBus
            if (!_instance!.IsRegistered<Laboratory.Core.Events.IEventBus>())
            {
                Debug.LogError($"VALIDATION FAILED: Core service IEventBus is not registered");
                allServicesRegistered = false;
            }
            else
            {
                Debug.Log($"✅ Core service IEventBus is properly registered");
            }
            
            // Check Laboratory.Core.State.IGameStateService
            if (!_instance!.IsRegistered<Laboratory.Core.State.IGameStateService>())
            {
                Debug.LogError($"VALIDATION FAILED: Core service IGameStateService is not registered");
                allServicesRegistered = false;
            }
            else
            {
                Debug.Log($"✅ Core service IGameStateService is properly registered");
            }
            
            // Check Laboratory.Core.Services.IAssetService
            if (!_instance!.IsRegistered<Laboratory.Core.Services.IAssetService>())
            {
                Debug.LogError($"VALIDATION FAILED: Core service IAssetService is not registered");
                allServicesRegistered = false;
            }
            else
            {
                Debug.Log($"✅ Core service IAssetService is properly registered");
            }
            
            // Check Laboratory.Core.Services.IConfigService
            if (!_instance!.IsRegistered<Laboratory.Core.Services.IConfigService>())
            {
                Debug.LogError($"VALIDATION FAILED: Core service IConfigService is not registered");
                allServicesRegistered = false;
            }
            else
            {
                Debug.Log($"✅ Core service IConfigService is properly registered");
            }
            
            // Check Laboratory.Core.Services.ISceneService
            if (!_instance!.IsRegistered<Laboratory.Core.Services.ISceneService>())
            {
                Debug.LogError($"VALIDATION FAILED: Core service ISceneService is not registered");
                allServicesRegistered = false;
            }
            else
            {
                Debug.Log($"✅ Core service ISceneService is properly registered");
            }

            if (allServicesRegistered)
            {
                Debug.Log("🎉 All core services validation passed!");
            }

            return allServicesRegistered;
        }

        /// <summary>
        /// Tests the event system functionality by publishing and subscribing to a test event.
        /// </summary>
        /// <returns>True if event system is working correctly, false otherwise</returns>
        public static bool TestEventSystem()
        {
            if (!IsInitialized)
            {
                Debug.LogError("Cannot test event system: GlobalServiceProvider not initialized");
                return false;
            }

            try
            {
                if (!TryResolve<Laboratory.Core.Events.IEventBus>(out var eventBus) || eventBus == null)
                {
                    Debug.LogError("Cannot test event system: IEventBus not available");
                    return false;
                }

                bool eventReceived = false;
                var testEvent = new ValidationTestEvent { Message = "ServiceProviderValidationTest" };

                var subscription = eventBus.Subscribe<ValidationTestEvent>(evt => eventReceived = true);
                eventBus.Publish(testEvent);
                subscription.Dispose();

                if (eventReceived)
                {
                    Debug.Log("✅ Event system validation passed");
                    return true;
                }
                else
                {
                    Debug.LogError("❌ Event system validation failed: Event not received");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Event system validation failed with exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets diagnostic information about the service provider state.
        /// </summary>
        /// <returns>Diagnostic information string</returns>
        public static string GetDiagnosticInfo()
        {
            if (!IsInitialized)
            {
                return "GlobalServiceProvider: NOT INITIALIZED";
            }

            var info = new System.Text.StringBuilder();
            info.AppendLine("GlobalServiceProvider Diagnostic Info:");
            info.AppendLine($"- Initialized: {IsInitialized}");
            info.AppendLine($"- Container Type: {_instance!.GetType().Name}");
            
            // Add service registration info if available
            if (_instance is ServiceContainer container)
            {
                info.AppendLine($"- Registered Services Count: {container.GetRegisteredServiceCount()}");
            }

            return info.ToString();
        }
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Test event for validation purposes.
    /// </summary>
    internal class ValidationTestEvent
    {
        public string Message { get; set; } = "";
    }
#endif
}
