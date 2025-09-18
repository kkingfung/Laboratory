using System;
using NUnit.Framework;
using UnityEngine;
using Laboratory.Core.DI;

namespace Laboratory.Tests.Unit.Core.DI
{
    /// <summary>
    /// Comprehensive unit tests for the Service Container dependency injection system.
    /// Tests service registration, resolution, lifetime management, and error handling.
    /// </summary>
    public class ServiceContainerTests
    {
        #region Test Interfaces and Classes

        public interface ITestService
        {
            string GetValue();
        }

        public interface ITestServiceWithDependency
        {
            string GetValue();
            ITestService TestService { get; }
        }

        public class TestService : ITestService
        {
            public string GetValue() => "TestService";
        }

        public class TestServiceWithDependency : ITestServiceWithDependency
        {
            public ITestService TestService { get; }

            public TestServiceWithDependency(ITestService testService)
            {
                TestService = testService;
            }

            public string GetValue() => $"TestServiceWithDependency({TestService.GetValue()})";
        }

        public class DisposableTestService : ITestService, IDisposable
        {
            public bool IsDisposed { get; private set; }
            public string GetValue() => IsDisposed ? "Disposed" : "DisposableTestService";
            public void Dispose() => IsDisposed = true;
        }

        #endregion

        #region Test Setup

        private ServiceContainer _container;

        [SetUp]
        public void SetUp()
        {
            _container = new ServiceContainer();
        }

        [TearDown]
        public void TearDown()
        {
            _container?.Dispose();
        }

        #endregion

        #region Registration Tests

        [Test]
        public void Register_SimpleService_RegistersSuccessfully()
        {
            // Act
            _container.Register<ITestService, TestService>();

            // Assert
            Assert.IsTrue(_container.IsRegistered<ITestService>(), "Service should be registered");
        }

        [Test]
        public void Register_SingletonLifetime_ReturnsSameInstance()
        {
            // Arrange
            _container.Register<ITestService, TestService>(ServiceLifetime.Singleton);

            // Act
            var instance1 = _container.Resolve<ITestService>();
            var instance2 = _container.Resolve<ITestService>();

            // Assert
            Assert.AreSame(instance1, instance2, "Singleton should return same instance");
        }

        [Test]
        public void Register_TransientLifetime_ReturnsDifferentInstances()
        {
            // Arrange
            _container.Register<ITestService, TestService>(ServiceLifetime.Transient);

            // Act
            var instance1 = _container.Resolve<ITestService>();
            var instance2 = _container.Resolve<ITestService>();

            // Assert
            Assert.AreNotSame(instance1, instance2, "Transient should return different instances");
        }

        [Test]
        public void RegisterInstance_ExistingInstance_ReturnsRegisteredInstance()
        {
            // Arrange
            var instance = new TestService();

            // Act
            _container.RegisterInstance<ITestService>(instance);
            var resolved = _container.Resolve<ITestService>();

            // Assert
            Assert.AreSame(instance, resolved, "Should return the registered instance");
        }

        #endregion

        #region Resolution Tests

        [Test]
        public void Resolve_RegisteredService_ReturnsCorrectType()
        {
            // Arrange
            _container.Register<ITestService, TestService>();

            // Act
            var service = _container.Resolve<ITestService>();

            // Assert
            Assert.IsNotNull(service, "Resolved service should not be null");
            Assert.IsInstanceOf<TestService>(service, "Should resolve to correct implementation");
            Assert.AreEqual("TestService", service.GetValue(), "Service should function correctly");
        }

        [Test]
        public void TryResolve_RegisteredService_ReturnsTrue()
        {
            // Arrange
            _container.Register<ITestService, TestService>();

            // Act
            bool success = _container.TryResolve<ITestService>(out var service);

            // Assert
            Assert.IsTrue(success, "TryResolve should return true for registered service");
            Assert.IsNotNull(service, "Resolved service should not be null");
            Assert.IsInstanceOf<TestService>(service, "Should resolve to correct implementation");
        }

        [Test]
        public void TryResolve_UnregisteredService_ReturnsFalse()
        {
            // Act
            bool success = _container.TryResolve<ITestService>(out var service);

            // Assert
            Assert.IsFalse(success, "TryResolve should return false for unregistered service");
            Assert.IsNull(service, "Service should be null when resolution fails");
        }

        #endregion

        #region Dependency Injection Tests

        [Test]
        public void Resolve_ServiceWithDependencies_InjectsDependenciesCorrectly()
        {
            // Arrange
            _container.Register<ITestService, TestService>();
            _container.Register<ITestServiceWithDependency, TestServiceWithDependency>();

            // Act
            var service = _container.Resolve<ITestServiceWithDependency>();

            // Assert
            Assert.IsNotNull(service, "Service should be resolved");
            Assert.IsNotNull(service.TestService, "Dependency should be injected");
            Assert.IsInstanceOf<TestService>(service.TestService, "Dependency should be correct type");
            Assert.AreEqual("TestServiceWithDependency(TestService)", service.GetValue(), "Service should function with dependencies");
        }

        #endregion

        #region Performance Tests

        [Test]
        [Performance]
        public void Resolve_Performance_HandlesMultipleResolutionsQuickly()
        {
            // Arrange
            _container.Register<ITestService, TestService>(ServiceLifetime.Transient);
            const int resolutionCount = 1000;

            // Act
            var startTime = Time.realtimeSinceStartup;
            for (int i = 0; i < resolutionCount; i++)
            {
                var service = _container.Resolve<ITestService>();
                Assert.IsNotNull(service, $"Resolution {i} should succeed");
            }
            var endTime = Time.realtimeSinceStartup;

            // Assert
            var duration = endTime - startTime;
            Assert.Less(duration, 0.1f, "1000 service resolutions should complete in under 100ms");
        }

        #endregion
    }
}