using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.DI
{
    /// <summary>
    /// Simple dependency injection container for Unity projects.
    /// Provides service registration and resolution functionality.
    /// </summary>
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, ServiceDescriptor> _services = new();
        private readonly Dictionary<Type, object> _singletonInstances = new();
        private bool _disposed = false;
        
        /// <summary>
        /// Singleton instance of the service container.
        /// </summary>
        public static ServiceContainer Instance { get; } = new ServiceContainer();
        
        #region IServiceContainer Implementation
        
        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TInterface : class
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = lifetime
            };
            
            _services[typeof(TInterface)] = descriptor;
        }
        
        public void Register<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            ThrowIfDisposed();
            
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(T),
                ImplementationType = typeof(T),
                Lifetime = lifetime
            };
            
            _services[typeof(T)] = descriptor;
        }
        
        public void RegisterInstance<T>(T instance) where T : class
        {
            ThrowIfDisposed();
            
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(T),
                Instance = instance,
                Lifetime = ServiceLifetime.Singleton
            };
            
            _services[typeof(T)] = descriptor;
            _singletonInstances[typeof(T)] = instance;
        }
        
        public void RegisterFactory<T>(Func<IServiceContainer, T> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where T : class
        {
            ThrowIfDisposed();
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            
            var descriptor = new ServiceDescriptor
            {
                ServiceType = typeof(T),
                Factory = container => factory(container),
                Lifetime = lifetime
            };
            
            _services[typeof(T)] = descriptor;
        }
        
        public T Resolve<T>() where T : class
        {
            ThrowIfDisposed();
            
            if (TryResolve<T>(out var service) && service != null)
            {
                return service;
            }
            
            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }
        
        public bool TryResolve<T>(out T? service) where T : class
        {
            ThrowIfDisposed();
            
            service = null;
            
            if (!_services.TryGetValue(typeof(T), out var descriptor))
            {
                return false;
            }
            
            try
            {
                var instance = CreateInstance(descriptor);
                service = instance as T;
                return service != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error resolving service {typeof(T).Name}: {ex}");
                return false;
            }
        }
        
        public IServiceScope CreateScope()
        {
            ThrowIfDisposed();
            return new ServiceScope(this);
        }
        
        public bool IsRegistered<T>() where T : class
        {
            ThrowIfDisposed();
            return _services.ContainsKey(typeof(T));
        }
        
        public int GetRegisteredServiceCount()
        {
            ThrowIfDisposed();
            return _services.Count;
        }
        
        #endregion
        
        #region Helper Methods
        
        private object CreateInstance(ServiceDescriptor descriptor)
        {
            // Check if we have a cached singleton instance
            if (descriptor.Lifetime == ServiceLifetime.Singleton && 
                _singletonInstances.TryGetValue(descriptor.ServiceType, out var cachedInstance))
            {
                return cachedInstance;
            }
            
            object instance;
            
            if (descriptor.Instance != null)
            {
                instance = descriptor.Instance;
            }
            else if (descriptor.Factory != null)
            {
                instance = descriptor.Factory(this);
            }
            else if (descriptor.ImplementationType != null)
            {
                instance = CreateInstanceOfType(descriptor.ImplementationType);
            }
            else
            {
                throw new InvalidOperationException($"Cannot create instance of {descriptor.ServiceType.Name}");
            }
            
            // Cache singleton instances
            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _singletonInstances[descriptor.ServiceType] = instance;
            }
            
            return instance;
        }
        
        private object CreateInstanceOfType(Type type)
        {
            // Find the best constructor (prefer one with dependencies we can resolve)
            var constructors = type.GetConstructors();
            var defaultConstructor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            
            if (defaultConstructor != null)
            {
                return Activator.CreateInstance(type)!;
            }
            
            // Try to find a constructor with resolvable dependencies
            foreach (var constructor in constructors.OrderBy(c => c.GetParameters().Length))
            {
                var parameters = constructor.GetParameters();
                var args = new object[parameters.Length];
                bool canResolve = true;
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    var paramType = parameters[i].ParameterType;
                    if (_services.ContainsKey(paramType))
                    {
                        var paramDescriptor = _services[paramType];
                        args[i] = CreateInstance(paramDescriptor);
                    }
                    else
                    {
                        canResolve = false;
                        break;
                    }
                }
                
                if (canResolve)
                {
                    return Activator.CreateInstance(type, args)!;
                }
            }
            
            throw new InvalidOperationException(
                $"Cannot create instance of {type.Name}. No suitable constructor found or dependencies not registered.");
        }
        
        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceContainer));
        }
        
        #endregion
        
        #region Convenience Methods
        
        /// <summary>
        /// Registers a singleton service of type T.
        /// </summary>
        public ServiceContainer RegisterSingleton<T>() where T : class
        {
            Register<T>(ServiceLifetime.Singleton);
            return this;
        }
        
        /// <summary>
        /// Registers a singleton service with an implementation.
        /// </summary>
        public ServiceContainer RegisterSingleton<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Register<TInterface, TImplementation>(ServiceLifetime.Singleton);
            return this;
        }
        
        /// <summary>
        /// Registers a transient service of type T.
        /// </summary>
        public ServiceContainer RegisterTransient<T>() where T : class
        {
            Register<T>(ServiceLifetime.Transient);
            return this;
        }
        
        /// <summary>
        /// Registers a transient service with an implementation.
        /// </summary>
        public ServiceContainer RegisterTransient<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface
        {
            Register<TInterface, TImplementation>(ServiceLifetime.Transient);
            return this;
        }
        
        /// <summary>
        /// Clears all registrations.
        /// </summary>
        public void Clear()
        {
            ThrowIfDisposed();
            _services.Clear();
            _singletonInstances.Clear();
        }
        
        #endregion
        
        public void Dispose()
        {
            if (_disposed) return;
            
            // Dispose all singleton instances that implement IDisposable
            foreach (var instance in _singletonInstances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error disposing service instance: {ex}");
                    }
                }
            }
            
            _services.Clear();
            _singletonInstances.Clear();
            _disposed = true;
        }
    }
    
    /// <summary>
    /// Simple service scope implementation.
    /// </summary>
    internal class ServiceScope : IServiceScope
    {
        public ServiceScope(IServiceContainer services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }
        
        public IServiceContainer Services { get; private set; }
        
        public void Dispose()
        {
            // In a more advanced implementation, this would dispose scoped services
            // For now, we'll keep it simple
        }
    }
}
