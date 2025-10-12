using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Advanced extensions for ServiceContainer with automatic discovery and validation.
    /// Provides enhanced dependency management and lifecycle support.
    /// </summary>
    public static class ServiceContainerExtensions
    {
        /// <summary>
        /// Registers all services in an assembly automatically based on attributes
        /// </summary>
        public static void RegisterServicesFromAssembly(this ServiceContainer container, Assembly assembly = null)
        {
            assembly ??= Assembly.GetExecutingAssembly();

            var serviceTypes = assembly.GetTypes()
                .Where(type => type.GetCustomAttribute<ServiceAttribute>() != null)
                .ToArray();

            foreach (var serviceType in serviceTypes)
            {
                var attribute = serviceType.GetCustomAttribute<ServiceAttribute>();
                RegisterServiceByAttribute(container, serviceType, attribute);
            }

            Debug.Log($"[ServiceContainer] Auto-registered {serviceTypes.Length} services from assembly");
        }

        /// <summary>
        /// Validates all registered service dependencies
        /// </summary>
        public static ValidationResult ValidateServices(this ServiceContainer container)
        {
            var result = new ValidationResult();
            var registeredTypes = container.GetRegisteredTypes();

            foreach (var type in registeredTypes)
            {
                try
                {
                    ValidateServiceDependencies(container, type, result);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Validation failed for {type.Name}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Registers a service with automatic interface detection
        /// </summary>
        public static void RegisterAuto<T>(this ServiceContainer container, T instance, ServiceScope scope = ServiceScope.Singleton)
            where T : class
        {
            var implementationType = typeof(T);
            var interfaces = implementationType.GetInterfaces()
                .Where(i => i.IsPublic && !IsFrameworkInterface(i))
                .ToArray();

            // Register the concrete type
            container.RegisterScoped(() => instance, scope);

            // Register all public interfaces
            foreach (var interfaceType in interfaces)
            {
                container.RegisterScoped(() => instance, scope);
            }

            Debug.Log($"[ServiceContainer] Auto-registered {implementationType.Name} with {interfaces.Length} interfaces");
        }

        /// <summary>
        /// Creates a child container with scoped services
        /// </summary>
        public static ScopedServiceContainer CreateScope(this ServiceContainer parent, string scopeName = null)
        {
            return new ScopedServiceContainer(parent, scopeName ?? Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Registers services with health checking
        /// </summary>
        public static void RegisterWithHealthCheck<T>(this ServiceContainer container, Func<T> factory) where T : class, IHealthCheckable
        {
            container.RegisterFactory(() =>
            {
                var instance = factory();
                ServiceHealthMonitor.RegisterService(instance);
                return instance;
            });
        }

        /// <summary>
        /// Resolves all services of a specific interface type
        /// </summary>
        public static T[] ResolveAll<T>(this ServiceContainer container) where T : class
        {
            var interfaceType = typeof(T);
            var registeredTypes = container.GetRegisteredTypes();
            var implementingTypes = registeredTypes
                .Where(type => interfaceType.IsAssignableFrom(type) && type != interfaceType)
                .ToArray();

            var instances = new List<T>();
            foreach (var type in implementingTypes)
            {
                try
                {
                    var instance = container.Resolve(type) as T;
                    if (instance != null)
                    {
                        instances.Add(instance);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[ServiceContainer] Failed to resolve {type.Name}: {ex.Message}");
                }
            }

            return instances.ToArray();
        }

        /// <summary>
        /// Registers a service with automatic disposal
        /// </summary>
        public static void RegisterDisposable<T>(this ServiceContainer container, Func<T> factory) where T : class, IDisposable
        {
            container.RegisterFactory(() =>
            {
                var instance = factory();
                ServiceLifecycleManager.RegisterDisposable(instance);
                return instance;
            });
        }

        private static void RegisterServiceByAttribute(ServiceContainer container, Type serviceType, ServiceAttribute attribute)
        {
            var factory = CreateFactory(serviceType);

            if (attribute.InterfaceType != null)
            {
                // Register with specific interface
                container.RegisterScoped(factory, attribute.Scope);
            }
            else
            {
                // Register with auto-detected interfaces
                var interfaces = serviceType.GetInterfaces()
                    .Where(i => i.IsPublic && !IsFrameworkInterface(i))
                    .ToArray();

                foreach (var interfaceType in interfaces)
                {
                    container.RegisterScoped(factory, attribute.Scope);
                }
            }
        }

        private static Func<object> CreateFactory(Type serviceType)
        {
            return () =>
            {
                try
                {
                    return Activator.CreateInstance(serviceType);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceContainer] Failed to create instance of {serviceType.Name}: {ex.Message}");
                    return null;
                }
            };
        }

        private static void ValidateServiceDependencies(ServiceContainer container, Type serviceType, ValidationResult result)
        {
            var constructors = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            if (constructors.Length == 0) return;

            var primaryConstructor = constructors
                .OrderByDescending(c => c.GetParameters().Length)
                .First();

            foreach (var parameter in primaryConstructor.GetParameters())
            {
                if (!container.IsRegistered(parameter.ParameterType))
                {
                    result.Warnings.Add($"{serviceType.Name} depends on {parameter.ParameterType.Name} which is not registered");
                }
            }
        }

        private static bool IsFrameworkInterface(Type type)
        {
            return type.Namespace?.StartsWith("System") == true ||
                   type.Namespace?.StartsWith("Unity") == true ||
                   type.Namespace?.StartsWith("UnityEngine") == true;
        }

        // Extension method to support generic Resolve
        public static object Resolve(this ServiceContainer container, Type serviceType)
        {
            var method = typeof(ServiceContainer).GetMethod("Resolve");
            var genericMethod = method.MakeGenericMethod(serviceType);
            return genericMethod.Invoke(container, null);
        }

        public static bool IsRegistered(this ServiceContainer container, Type serviceType)
        {
            var method = typeof(ServiceContainer).GetMethod("IsRegistered");
            var genericMethod = method.MakeGenericMethod(serviceType);
            return (bool)genericMethod.Invoke(container, null);
        }
    }

    /// <summary>Attribute to mark services for automatic registration</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ServiceAttribute : Attribute
    {
        public Type InterfaceType { get; set; }
        public ServiceScope Scope { get; set; } = ServiceScope.Singleton;

        public ServiceAttribute(Type interfaceType = null, ServiceScope scope = ServiceScope.Singleton)
        {
            InterfaceType = interfaceType;
            Scope = scope;
        }
    }

    /// <summary>Interface for services that support health checking</summary>
    public interface IHealthCheckable
    {
        bool IsHealthy { get; }
        string GetHealthStatus();
    }

    /// <summary>Service validation results</summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public bool IsValid => Errors.Count == 0;
    }

    /// <summary>Scoped service container for scene-specific services</summary>
    public class ScopedServiceContainer : IDisposable
    {
        private readonly ServiceContainer _parent;
        private readonly Dictionary<Type, object> _scopedServices = new();
        private readonly string _scopeName;
        private bool _disposed = false;

        public ScopedServiceContainer(ServiceContainer parent, string scopeName)
        {
            _parent = parent;
            _scopeName = scopeName;
        }

        public T Resolve<T>() where T : class
        {
            var type = typeof(T);

            // Try scoped services first
            if (_scopedServices.TryGetValue(type, out var scopedInstance))
            {
                return scopedInstance as T;
            }

            // Fall back to parent container
            var instance = _parent.Resolve<T>();

            // Cache in scope if it's a scoped service
            _scopedServices[type] = instance;

            return instance;
        }

        public void Dispose()
        {
            if (_disposed) return;

            // Dispose all scoped services that implement IDisposable
            foreach (var service in _scopedServices.Values)
            {
                if (service is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ScopedContainer] Error disposing service: {ex.Message}");
                    }
                }
            }

            _scopedServices.Clear();
            _disposed = true;

            Debug.Log($"[ScopedContainer] Disposed scope: {_scopeName}");
        }
    }

    /// <summary>Monitors service health and lifecycle</summary>
    public static class ServiceHealthMonitor
    {
        private static readonly List<WeakReference> _healthCheckableServices = new();
        private static float _lastHealthCheck = 0f;
        private static readonly float HealthCheckInterval = 30f; // 30 seconds

        public static void RegisterService(IHealthCheckable service)
        {
            _healthCheckableServices.Add(new WeakReference(service));
        }

        public static void CheckAllServices()
        {
            if (Time.time - _lastHealthCheck < HealthCheckInterval) return;

            var unhealthyServices = new List<string>();

            // Clean up dead references and check health
            for (int i = _healthCheckableServices.Count - 1; i >= 0; i--)
            {
                var reference = _healthCheckableServices[i];
                if (!reference.IsAlive)
                {
                    _healthCheckableServices.RemoveAt(i);
                    continue;
                }

                var service = reference.Target as IHealthCheckable;
                if (service != null && !service.IsHealthy)
                {
                    unhealthyServices.Add($"{service.GetType().Name}: {service.GetHealthStatus()}");
                }
            }

            if (unhealthyServices.Count > 0)
            {
                Debug.LogWarning($"[ServiceHealthMonitor] Unhealthy services detected:\n{string.Join("\n", unhealthyServices)}");
            }

            _lastHealthCheck = Time.time;
        }
    }

    /// <summary>Manages service lifecycle and disposal</summary>
    public static class ServiceLifecycleManager
    {
        private static readonly List<WeakReference> _disposableServices = new();

        public static void RegisterDisposable(IDisposable service)
        {
            _disposableServices.Add(new WeakReference(service));
        }

        public static void DisposeAllServices()
        {
            var disposedCount = 0;

            for (int i = _disposableServices.Count - 1; i >= 0; i--)
            {
                var reference = _disposableServices[i];
                if (!reference.IsAlive)
                {
                    _disposableServices.RemoveAt(i);
                    continue;
                }

                var service = reference.Target as IDisposable;
                if (service != null)
                {
                    try
                    {
                        service.Dispose();
                        disposedCount++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceLifecycleManager] Error disposing service: {ex.Message}");
                    }
                }

                _disposableServices.RemoveAt(i);
            }

            Debug.Log($"[ServiceLifecycleManager] Disposed {disposedCount} services");
        }
    }
}