using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.DI
{
    /// <summary>
    /// Concrete implementation of the service container with constructor injection support.
    /// </summary>
    public class ServiceContainer : IServiceContainer, IDisposable
    {
        #region Fields

        private readonly Dictionary<Type, ServiceDescriptor> _services = new();
        private readonly Dictionary<Type, object> _singletonInstances = new();
        private readonly Stack<Type> _resolutionStack = new();
        private bool _disposed = false;

        #endregion

        #region Registration Methods

        public void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TInterface
        {
            ThrowIfDisposed();
            
            _services[typeof(TInterface)] = new ServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = typeof(TImplementation),
                Lifetime = lifetime
            };
        }

        public void Register<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            ThrowIfDisposed();
            
            _services[typeof(T)] = new ServiceDescriptor
            {
                ServiceType = typeof(T),
                ImplementationType = typeof(T),
                Lifetime = lifetime
            };
        }

        public void RegisterInstance<T>(T instance) where T : class
        {
            ThrowIfDisposed();
            
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _services[typeof(T)] = new ServiceDescriptor
            {
                ServiceType = typeof(T),
                Instance = instance,
                Lifetime = ServiceLifetime.Singleton
            };
            
            _singletonInstances[typeof(T)] = instance;
        }

        public void RegisterFactory<T>(Func<IServiceContainer, T> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where T : class
        {
            ThrowIfDisposed();
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _services[typeof(T)] = new ServiceDescriptor
            {
                ServiceType = typeof(T),
                Factory = container => factory(container),
                Lifetime = lifetime
            };
        }

        #endregion

        #region Resolution Methods

        public T Resolve<T>() where T : class
        {
            ThrowIfDisposed();
            
            return (T)ResolveInternal(typeof(T));
        }

        public bool TryResolve<T>(out T? service) where T : class
        {
            ThrowIfDisposed();
            
            try
            {
                service = Resolve<T>();
                return true;
            }
            catch
            {
                service = null;
                return false;
            }
        }

        private object ResolveInternal(Type serviceType)
        {
            // Check for circular dependencies
            if (_resolutionStack.Contains(serviceType))
            {
                var stackTrace = string.Join(" -> ", _resolutionStack);
                throw new InvalidOperationException($"Circular dependency detected: {stackTrace} -> {serviceType.Name}");
            }

            _resolutionStack.Push(serviceType);

            try
            {
                // Check if service is registered
                if (!_services.TryGetValue(serviceType, out var descriptor))
                {
                    throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered.");
                }

                // Handle singleton instances
                if (descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    if (_singletonInstances.TryGetValue(serviceType, out var singletonInstance))
                    {
                        return singletonInstance;
                    }
                }

                // Create instance based on descriptor type
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
                    instance = CreateInstance(descriptor.ImplementationType);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid service descriptor for {serviceType.Name}");
                }

                // Cache singleton instances
                if (descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    _singletonInstances[serviceType] = instance;
                }

                return instance;
            }
            finally
            {
                _resolutionStack.Pop();
            }
        }

        private object CreateInstance(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            
            // Find constructor with most parameters (greedy injection)
            ConstructorInfo? bestConstructor = null;
            int maxParams = -1;
            
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (parameters.Length > maxParams && CanResolveAllParameters(parameters))
                {
                    bestConstructor = constructor;
                    maxParams = parameters.Length;
                }
            }

            if (bestConstructor == null)
            {
                throw new InvalidOperationException(
                    $"No suitable constructor found for {type.Name}. " +
                    "Ensure all constructor parameters are registered services.");
            }

            // Resolve constructor parameters
            var parameters = bestConstructor.GetParameters();
            var args = new object[parameters.Length];
            
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = ResolveInternal(parameters[i].ParameterType);
            }

            return Activator.CreateInstance(type, args)!;
        }

        private bool CanResolveAllParameters(ParameterInfo[] parameters)
        {
            foreach (var param in parameters)
            {
                if (!_services.ContainsKey(param.ParameterType))
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Scope Management

        public IServiceScope CreateScope()
        {
            ThrowIfDisposed();
            return new ServiceScope(this);
        }

        #endregion

        #region Utility Methods

        public bool IsRegistered<T>() where T : class
        {
            ThrowIfDisposed();
            return _services.ContainsKey(typeof(T));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            // Dispose singleton instances that implement IDisposable
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
                        Debug.LogError($"Error disposing service {instance.GetType().Name}: {ex}");
                    }
                }
            }

            _singletonInstances.Clear();
            _services.Clear();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceContainer));
        }

        #endregion
    }

    /// <summary>
    /// Implementation of service scope for scoped service lifetime management.
    /// </summary>
    internal class ServiceScope : IServiceScope
    {
        private readonly ServiceContainer _parentContainer;
        private readonly Dictionary<Type, object> _scopedInstances = new();
        private bool _disposed = false;

        public IServiceContainer Services => _parentContainer;

        public ServiceScope(ServiceContainer parentContainer)
        {
            _parentContainer = parentContainer;
        }

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var instance in _scopedInstances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error disposing scoped service {instance.GetType().Name}: {ex}");
                    }
                }
            }

            _scopedInstances.Clear();
            _disposed = true;
        }
    }
}
