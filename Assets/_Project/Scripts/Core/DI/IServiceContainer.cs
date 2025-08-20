using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.DI
{
    /// <summary>
    /// Service lifetime options for dependency injection.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>Single instance shared across the application.</summary>
        Singleton,
        /// <summary>New instance created each time service is requested.</summary>
        Transient,
        /// <summary>Single instance per scope (useful for per-scene services).</summary>
        Scoped
    }

    /// <summary>
    /// Dependency injection container interface.
    /// </summary>
    public interface IServiceContainer : IDisposable
    {
        /// <summary>
        /// Registers a service implementation for an interface.
        /// </summary>
        void Register<TInterface, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where TImplementation : class, TInterface;

        /// <summary>
        /// Registers a concrete service type.
        /// </summary>
        void Register<T>(ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class;

        /// <summary>
        /// Registers a service instance directly.
        /// </summary>
        void RegisterInstance<T>(T instance) where T : class;

        /// <summary>
        /// Registers a factory function for creating service instances.
        /// </summary>
        void RegisterFactory<T>(Func<IServiceContainer, T> factory, ServiceLifetime lifetime = ServiceLifetime.Singleton)
            where T : class;

        /// <summary>
        /// Resolves a service instance by type.
        /// </summary>
        T Resolve<T>() where T : class;

        /// <summary>
        /// Attempts to resolve a service instance.
        /// </summary>
        bool TryResolve<T>(out T? service) where T : class;

        /// <summary>
        /// Creates a new scope for scoped services.
        /// </summary>
        IServiceScope CreateScope();

        /// <summary>
        /// Checks if a service type is registered.
        /// </summary>
        bool IsRegistered<T>() where T : class;
    }

    /// <summary>
    /// Represents a service registration scope.
    /// </summary>
    public interface IServiceScope : IDisposable
    {
        IServiceContainer Services { get; }
    }

    /// <summary>
    /// Service descriptor containing registration information.
    /// </summary>
    internal class ServiceDescriptor
    {
        public Type ServiceType { get; set; } = null!;
        public Type? ImplementationType { get; set; }
        public object? Instance { get; set; }
        public Func<IServiceContainer, object>? Factory { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }
}
