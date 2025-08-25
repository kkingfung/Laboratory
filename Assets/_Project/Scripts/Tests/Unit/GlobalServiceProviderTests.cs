using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Laboratory.Core.DI;

#nullable enable

namespace Laboratory.Core.Tests.Unit
{
    /// <summary>
    /// Unit tests for the GlobalServiceProvider static service locator.
    /// </summary>
    public class GlobalServiceProviderTests
    {
        private ServiceContainer? _container;

        [SetUp]
        public void SetUp()
        {
            // Ensure clean state
            GlobalServiceProvider.Shutdown();
            _container = new ServiceContainer();
        }

        [TearDown]
        public void TearDown()
        {
            GlobalServiceProvider.Shutdown();
            _container?.Dispose();
            _container = null;
        }

        #region Initialization Tests

        [Test]
        public void Initialize_ValidContainer_InitializesSuccessfully()
        {
            // Act
            GlobalServiceProvider.Initialize(_container!);

            // Assert
            Assert.IsTrue(GlobalServiceProvider.IsInitialized);
            Assert.AreSame(_container, GlobalServiceProvider.Instance);
        }

        [Test]
        public void Initialize_NullContainer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                GlobalServiceProvider.Initialize(null!));
        }

        [Test]
        public void Initialize_AlreadyInitialized_LogsWarningAndReplaces()
        {
            // Arrange
            var firstContainer = new ServiceContainer();
            GlobalServiceProvider.Initialize(firstContainer);

            // Act
            LogAssert.Expect(LogType.Warning, "GlobalServiceProvider is already initialized. Replacing existing instance.");
            GlobalServiceProvider.Initialize(_container!);

            // Assert
            Assert.AreSame(_container, GlobalServiceProvider.Instance);
            
            // Cleanup
            firstContainer.Dispose();
        }

        #endregion

        #region Property Tests

        [Test]
        public void IsInitialized_WhenNotInitialized_ReturnsFalse()
        {
            // Assert
            Assert.IsFalse(GlobalServiceProvider.IsInitialized);
        }

        [Test]
        public void IsInitialized_WhenInitialized_ReturnsTrue()
        {
            // Arrange
            GlobalServiceProvider.Initialize(_container!);

            // Assert
            Assert.IsTrue(GlobalServiceProvider.IsInitialized);
        }

        [Test]
        public void Instance_WhenNotInitialized_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => 
            {
                var _ = GlobalServiceProvider.Instance;
            });
            
            StringAssert.Contains("has not been initialized", ex.Message);
        }

        [Test]
        public void Instance_WhenInitialized_ReturnsContainer()
        {
            // Arrange
            GlobalServiceProvider.Initialize(_container!);

            // Act
            var instance = GlobalServiceProvider.Instance;

            // Assert
            Assert.AreSame(_container, instance);
        }

        #endregion

        #region Service Resolution Tests

        [Test]
        public void Resolve_WithInitializedProvider_ResolvesService()
        {
            // Arrange
            _container!.Register<ITestService, TestService>();
            GlobalServiceProvider.Initialize(_container);

            // Act
            var service = GlobalServiceProvider.Resolve<ITestService>();

            // Assert
            Assert.IsNotNull(service);
            Assert.IsInstanceOf<TestService>(service);
        }

        [Test]
        public void TryResolve_WithInitializedProvider_ResolvesService()
        {
            // Arrange
            _container!.Register<ITestService, TestService>();
            GlobalServiceProvider.Initialize(_container);

            // Act
            var success = GlobalServiceProvider.TryResolve<ITestService>(out var service);

            // Assert
            Assert.IsTrue(success);
            Assert.IsNotNull(service);
        }

        [Test]
        public void TryResolve_WhenNotInitialized_ReturnsFalse()
        {
            // Act
            var success = GlobalServiceProvider.TryResolve<ITestService>(out var service);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(service);
        }

        [Test]
        public void IsRegistered_WithRegisteredService_ReturnsTrue()
        {
            // Arrange
            _container!.Register<ITestService, TestService>();
            GlobalServiceProvider.Initialize(_container);

            // Act
            var isRegistered = GlobalServiceProvider.IsRegistered<ITestService>();

            // Assert
            Assert.IsTrue(isRegistered);
        }

        [Test]
        public void IsRegistered_WithUnregisteredService_ReturnsFalse()
        {
            // Arrange
            GlobalServiceProvider.Initialize(_container!);

            // Act
            var isRegistered = GlobalServiceProvider.IsRegistered<ITestService>();

            // Assert
            Assert.IsFalse(isRegistered);
        }

        [Test]
        public void IsRegistered_WhenNotInitialized_ReturnsFalse()
        {
            // Act
            var isRegistered = GlobalServiceProvider.IsRegistered<ITestService>();

            // Assert
            Assert.IsFalse(isRegistered);
        }

        #endregion

        #region Shutdown Tests

        [Test]
        public void Shutdown_WhenInitialized_ClearsInstance()
        {
            // Arrange
            GlobalServiceProvider.Initialize(_container!);
            Assert.IsTrue(GlobalServiceProvider.IsInitialized);

            // Act
            GlobalServiceProvider.Shutdown();

            // Assert
            Assert.IsFalse(GlobalServiceProvider.IsInitialized);
        }

        [Test]
        public void Shutdown_WhenNotInitialized_DoesNotThrow()
        {
            // Act & Assert (should not throw)
            GlobalServiceProvider.Shutdown();
        }

        #endregion

#if UNITY_EDITOR
        #region Validation Tests

        [Test]
        public void ValidateCoreServices_WhenNotInitialized_ReturnsFalse()
        {
            // Act
            LogAssert.Expect(LogType.Error, "VALIDATION FAILED: GlobalServiceProvider is not initialized");
            var result = GlobalServiceProvider.ValidateCoreServices();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void TestEventSystem_WhenNotInitialized_ReturnsFalse()
        {
            // Act
            LogAssert.Expect(LogType.Error, "Cannot test event system: GlobalServiceProvider not initialized");
            var result = GlobalServiceProvider.TestEventSystem();

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void GetDiagnosticInfo_WhenNotInitialized_ReturnsNotInitializedMessage()
        {
            // Act
            var diagnostics = GlobalServiceProvider.GetDiagnosticInfo();

            // Assert
            StringAssert.Contains("NOT INITIALIZED", diagnostics);
        }

        [Test]
        public void GetDiagnosticInfo_WhenInitialized_ReturnsInfo()
        {
            // Arrange
            GlobalServiceProvider.Initialize(_container!);

            // Act
            var diagnostics = GlobalServiceProvider.GetDiagnosticInfo();

            // Assert
            StringAssert.Contains("Initialized: True", diagnostics);
            StringAssert.Contains("Container Type: ServiceContainer", diagnostics);
        }

        #endregion
#endif
    }

    #region Test Services

    public interface ITestService
    {
        string GetValue();
    }

    public class TestService : ITestService
    {
        public string GetValue() => "Test";
    }

    #endregion
}
