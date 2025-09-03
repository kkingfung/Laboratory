using NUnit.Framework;
using Laboratory.Core.Bootstrap;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Services;
using Laboratory.Core.State;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Threading;
using System;

#nullable enable

namespace Laboratory.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests for the Core Architecture Bootstrap system.
    /// Tests the entire initialization flow and service interactions.
    /// </summary>
    [TestFixture]
    public class BootstrapIntegrationTests
    {
        private GameObject? _bootstrapGameObject;
        private GameBootstrap? _bootstrap;
        private CancellationTokenSource? _cancellationTokenSource;

        [SetUp]
        public void Setup()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            // Create a test GameObject with GameBootstrap
            _bootstrapGameObject = new GameObject("TestGameBootstrap");
            _bootstrap = _bootstrapGameObject.AddComponent<GameBootstrap>();
        }

        [TearDown]
        public void TearDown()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            if (_bootstrapGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_bootstrapGameObject);
            }
            
            // Clean up global state
            GlobalServiceProvider.Shutdown();
        }

        #region Bootstrap Initialization Tests

        [UnityTest]
        public IEnumerator Bootstrap_FullInitialization_ShouldInitializeAllServices()
        {
            // Arrange
            bool initializationCompleted = false;

            // Act - trigger manual initialization to control timing
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            // Wait for initialization to complete
            yield return new WaitUntil(() => initializationCompleted);

            // Assert - Check that GlobalServiceProvider is initialized
            Assert.That(GlobalServiceProvider.IsInitialized, Is.True);

            // Verify core services are registered
            Assert.DoesNotThrow(() => GlobalServiceProvider.Resolve<IEventBus>());
            Assert.DoesNotThrow(() => GlobalServiceProvider.Resolve<IGameStateService>());
            Assert.DoesNotThrow(() => GlobalServiceProvider.Resolve<IAssetService>());
            Assert.DoesNotThrow(() => GlobalServiceProvider.Resolve<IConfigService>());
            Assert.DoesNotThrow(() => GlobalServiceProvider.Resolve<ISceneService>());
        }

        [UnityTest]
        public IEnumerator Bootstrap_ServiceResolution_ShouldProvideValidInstances()
        {
            // Arrange
            bool initializationCompleted = false;

            // Act
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            // Assert - Verify service instances are not null and of correct type
            var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
            var stateService = GlobalServiceProvider.Resolve<IGameStateService>();
            var assetService = GlobalServiceProvider.Resolve<IAssetService>();

            Assert.That(eventBus, Is.Not.Null);
            Assert.That(eventBus, Is.InstanceOf<UnifiedEventBus>());

            Assert.That(stateService, Is.Not.Null);
            Assert.That(stateService, Is.InstanceOf<GameStateService>());

            Assert.That(assetService, Is.Not.Null);
            Assert.That(assetService, Is.InstanceOf<AssetService>());
        }

        #endregion

        #region Service Interaction Tests

        [UnityTest]
        public IEnumerator Services_EventBusIntegration_ShouldCommunicateCorrectly()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
            var stateService = GlobalServiceProvider.Resolve<IGameStateService>();

            // Act - Request a state change and verify event system works
            bool eventReceived = false;
            eventBus.Subscribe<Laboratory.Core.Events.Messages.GameStateChangedEvent>(
                evt => eventReceived = true);

            stateService.ApplyRemoteStateChange(GameState.MainMenu, suppressEvents: false);

            // Small delay to allow event processing
            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.That(eventReceived, Is.True);
        }

        [UnityTest]
        public IEnumerator Services_StateServiceIntegration_ShouldManageStateCorrectly()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            var stateService = GlobalServiceProvider.Resolve<IGameStateService>();

            // Act
            var initialState = stateService.Current;
            stateService.ApplyRemoteStateChange(GameState.Loading);
            var newState = stateService.Current;

            // Assert
            Assert.That(initialState, Is.EqualTo(GameState.None));
            Assert.That(newState, Is.EqualTo(GameState.Loading));
        }

        #endregion

        #region Error Handling Tests

        [UnityTest]
        public IEnumerator Bootstrap_WithCancellation_ShouldHandleGracefully()
        {
            // Arrange
            var shortCancellationToken = new CancellationTokenSource();
            bool initializationCompleted = false;
            bool cancellationHandled = false;

            // Act - Cancel immediately to test cancellation handling
            shortCancellationToken.Cancel();

            _bootstrap!.InitializeManuallyAsync(shortCancellationToken.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            // Wait a moment for cancellation to be processed
            yield return new WaitForSeconds(0.5f);

            // Assert - Should handle cancellation without crashing
            Assert.That(initializationCompleted || cancellationHandled, Is.True);
            
            shortCancellationToken.Dispose();
        }

        #endregion

        #region Service Container Integration Tests

        [UnityTest]
        public IEnumerator ServiceContainer_DependencyInjection_ShouldInjectCorrectly()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            var services = _bootstrap.Services;

            // Act - Test that services have their dependencies injected
            var gameStateService = services.Resolve<IGameStateService>();
            var assetService = services.Resolve<IAssetService>();

            // Assert - Services should be functional (dependencies injected)
            Assert.That(gameStateService.Current, Is.EqualTo(GameState.MainMenu)); // Should be set by bootstrap
            Assert.DoesNotThrow(() => assetService.IsAssetCached("test"));
        }

        [UnityTest]
        public IEnumerator ServiceContainer_SingletonLifetime_ShouldReturnSameInstance()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            // Act
            var eventBus1 = GlobalServiceProvider.Resolve<IEventBus>();
            var eventBus2 = GlobalServiceProvider.Resolve<IEventBus>();

            // Assert
            Assert.That(eventBus1, Is.SameAs(eventBus2));
        }

        #endregion

        #region Performance Tests

        [UnityTest]
        public IEnumerator Bootstrap_InitializationTime_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            bool initializationCompleted = false;

            // Act
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => 
                {
                    stopwatch.Stop();
                    initializationCompleted = true;
                })
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            // Assert - Initialization should complete within 5 seconds
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000));
            Debug.Log($"Bootstrap initialization completed in {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Validation Tests

        [UnityTest]
        public IEnumerator Bootstrap_CoreServiceValidation_ShouldPassValidation()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            // Act
            bool validationResult = GlobalServiceProvider.ValidateCoreServices();

            // Assert
            Assert.That(validationResult, Is.True);
        }

        [UnityTest]
        public IEnumerator Bootstrap_EventSystemValidation_ShouldPassValidation()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            // Act
            bool eventSystemTest = GlobalServiceProvider.TestEventSystem();

            // Assert
            Assert.That(eventSystemTest, Is.True);
        }

        #endregion

        #region Resource Management Tests

        [UnityTest]
        public IEnumerator Bootstrap_Cleanup_ShouldDisposeCorrectly()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            var services = _bootstrap.Services;
            Assert.That(services, Is.Not.Null);

            // Act - Simulate application shutdown
            GlobalServiceProvider.Shutdown();

            // Assert - Should handle cleanup gracefully
            Assert.That(GlobalServiceProvider.IsInitialized, Is.False);
            
            // Services should still be accessible through the bootstrap
            Assert.DoesNotThrow(() => services.IsRegistered<IEventBus>());
        }

        #endregion

        #region Stress Tests

        [UnityTest]
        public IEnumerator Bootstrap_MultipleServiceResolutions_ShouldHandleLoad()
        {
            // Arrange
            bool initializationCompleted = false;
            _bootstrap!.InitializeManuallyAsync(_cancellationTokenSource!.Token)
                .ContinueWith(() => initializationCompleted = true)
                .Forget();

            yield return new WaitUntil(() => initializationCompleted);

            // Act - Resolve services many times rapidly
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            for (int i = 0; i < 1000; i++)
            {
                var eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                var stateService = GlobalServiceProvider.Resolve<IGameStateService>();
                var assetService = GlobalServiceProvider.Resolve<IAssetService>();
                
                Assert.That(eventBus, Is.Not.Null);
                Assert.That(stateService, Is.Not.Null);
                Assert.That(assetService, Is.Not.Null);
                
                if (i % 100 == 0)
                {
                    yield return null; // Allow Unity to process other things
                }
            }
            
            stopwatch.Stop();

            // Assert - Should handle 1000 resolutions quickly
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000));
            Debug.Log($"1000 service resolutions completed in {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion
    }
}
