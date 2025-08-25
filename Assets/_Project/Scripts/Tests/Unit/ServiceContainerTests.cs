using System;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.DI;

#nullable enable

namespace Laboratory.Core.Tests.Unit
{
    /// <summary>
    /// Comprehensive unit tests for the ServiceContainer dependency injection implementation.
    /// </summary>
    public class ServiceContainerTests
    {
        private ServiceContainer? _container;

        [SetUp]
        public void SetUp()
        {
            _container = new ServiceContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
            _container = null;
        }

        #region Registration Tests

        [Test]
        public void Register_InterfaceImplementation_RegistersSuccessfully()
        {
            // Arrange & Act
            _container!.Register<ITestService, TestService>();

            // Assert
            Assert.IsTrue(_container.IsRegistered<ITestService>());
        }

        [Test]
        public void Register_ConcreteType_RegistersSuccessfully()
        {
            // Arrange & Act
            _container!.Register<TestService>();

            // Assert
            Assert.IsTrue(_container.IsRegistered<TestService>());
        }

        [Test]
        public void RegisterInstance_ValidInstance_RegistersSuccessfully()
        {
            // Arrange
            var instance = new TestService();

            // Act
            _container!.RegisterInstance<ITestService>(instance);

            // Assert
            Assert.IsTrue(_container.IsRegistered<ITestService>());
            Assert.AreSame(instance, _container.Resolve<ITestService>());
        }

        [Test]
        public void RegisterInstance_NullInstance_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _container!.RegisterInstance<ITestService>(null!));
        }

        [Test]
        public void RegisterFactory_ValidFactory_RegistersSuccessfully()
        {
            // Arrange
            var factoryCalled = false;
            Func<IServiceContainer, ITestService> factory = container => 
            {
                factoryCalled = true;
                return new TestService();
            };

            // Act
            _container!.RegisterFactory<ITestService>(factory);

            // Assert
            Assert.IsTrue(_container.IsRegistered<ITestService>());
            var resolved = _container.Resolve<ITestService>();
            Assert.IsNotNull(resolved);
            Assert.IsTrue(factoryCalled);
        }

        [Test]
        public void RegisterFactory_NullFactory_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                _container!.RegisterFactory<ITestService>(null!));
        }

        #endregion

        #region Resolution Tests

        [Test]
        public void Resolve_RegisteredService_ReturnsInstance()
        {
            // Arrange
            _container!.Register<ITestService, TestService>();

            // Act
            var service = _container.Resolve<ITestService>();

            // Assert
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<TestService>(service);
        }

        [Test]
        public void Resolve_UnregisteredService_ThrowsInvalidOperationException()
        {
            // Arrange, Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                _container!.Resolve<ITestService>());
            
            StringAssert.Contains("not registered", ex.Message);
        }

        [Test]
        public void Resolve_SingletonService_ReturnsSameInstance()
        {
            // Arrange
            _container!.Register<ITestService, TestService>(ServiceLifetime.Singleton);

            // Act
            var service1 = _container.Resolve<ITestService>();
            var service2 = _container.Resolve<ITestService>();

            // Assert
            Assert.AreSame(service1, service2);
        }

        [Test]
        public void Resolve_TransientService_ReturnsDifferentInstances()
        {
            // Arrange
            _container!.Register<ITestService, TestService>(ServiceLifetime.Transient);

            // Act
            var service1 = _container.Resolve<ITestService>();
            var service2 = _container.Resolve<ITestService>();

            // Assert
            Assert.AreNotSame(service1, service2);
        }

        [Test]
        public void TryResolve_RegisteredService_ReturnsTrueAndService()
        {
            // Arrange
            _container!.Register<ITestService, TestService>();

            // Act
            var success = _container.TryResolve<ITestService>(out var service);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(service);
        }

        [Test]
        public void TryResolve_UnregisteredService_ReturnsFalseAndNull()
        {
            // Act
            var success = _container!.TryResolve<ITestService>(out var service);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(service);
        }

        #endregion

        #region Constructor Injection Tests

        [Test]
        public void Resolve_ServiceWithDependencies_InjectsDependencies()
        {
            // Arrange
            _container!.Register<ITestService, TestService>();
            _container.Register<IServiceWithDependency, ServiceWithDependency>();

            // Act
            var service = _container.Resolve<IServiceWithDependency>();

            // Assert
            Assert.IsNotNull(service);
            Assert.IsNotNull(((ServiceWithDependency)service).Dependency);
        }

        [Test]
        public void Resolve_ServiceWithUnregisteredDependency_ThrowsInvalidOperationException()
        {
            // Arrange
            _container!.Register<IServiceWithDependency, ServiceWithDependency>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => 
                _container.Resolve<IServiceWithDependency>());
        }

        #endregion

        #region Circular Dependency Tests

        [Test]
        public void Resolve_CircularDependency_ThrowsInvalidOperationException()
        {
            // Arrange
            _container!.Register<ICircularServiceA, CircularServiceA>();
            _container.Register<ICircularServiceB, CircularServiceB>();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
                _container.Resolve<ICircularServiceA>());
            
            StringAssert.Contains("Circular dependency", ex.Message);
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void Dispose_DisposableSingletonServices_DisposesServices()
        {
            // Arrange
            _container!.Register<IDisposableTestService, DisposableTestService>(ServiceLifetime.Singleton);
            var service = _container.Resolve<IDisposableTestService>() as DisposableTestService;

            // Act
            _container.Dispose();

            // Assert
            Assert.IsTrue(service!.IsDisposed);
        }

        [Test]
        public void Operations_AfterDisposal_ThrowObjectDisposedException()
        {
            // Arrange
            _container!.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => 
                _container.Register<ITestService, TestService>());
        }

        #endregion

        #region Scope Tests

        [Test]
        public void CreateScope_ValidContainer_ReturnsScope()
        {
            // Act
            using var scope = _container!.CreateScope();

            // Assert
            Assert.IsNotNull(scope);
            Assert.AreSame(_container, scope.Services);
        }

        #endregion
    }

    #region Test Interfaces and Implementations

    public interface ITestService
    {
        string GetValue();
    }

    public class TestService : ITestService
    {
        public string GetValue() => "Test";
    }

    public interface IServiceWithDependency
    {
        ITestService Dependency { get; }
    }

    public class ServiceWithDependency : IServiceWithDependency
    {
        public ITestService Dependency { get; }

        public ServiceWithDependency(ITestService dependency)
        {
            Dependency = dependency;
        }
    }

    public interface IDisposableTestService : IDisposable
    {
        bool IsDisposed { get; }
    }

    public class DisposableTestService : IDisposableTestService
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    // Circular dependency test services
    public interface ICircularServiceA { }
    public interface ICircularServiceB { }

    public class CircularServiceA : ICircularServiceA
    {
        public CircularServiceA(ICircularServiceB serviceB) { }
    }

    public class CircularServiceB : ICircularServiceB
    {
        public CircularServiceB(ICircularServiceA serviceA) { }
    }

    #endregion
}
