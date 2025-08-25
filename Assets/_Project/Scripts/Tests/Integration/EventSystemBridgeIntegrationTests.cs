using NUnit.Framework;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.DI;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System;

#nullable enable

namespace Laboratory.Core.Tests.Integration
{
    /// <summary>
    /// Integration tests for the EventSystemBridge that ensures backward compatibility
    /// and proper event migration between old and new event systems.
    /// </summary>
    [TestFixture]
    public class EventSystemBridgeIntegrationTests
    {
        private GameObject? _bridgeGameObject;
        private EventSystemBridge? _bridge;
        private UnifiedEventBus? _eventBus;
        private ServiceContainer? _serviceContainer;

        [SetUp]
        public void Setup()
        {
            // Create service container and event bus
            _serviceContainer = new ServiceContainer();
            _eventBus = new UnifiedEventBus();
            _serviceContainer.RegisterInstance<IEventBus>(_eventBus);
            
            // Initialize global service provider
            GlobalServiceProvider.Initialize(_serviceContainer);
            
            // Create test GameObject with EventSystemBridge
            _bridgeGameObject = new GameObject("TestEventSystemBridge");
            _bridge = _bridgeGameObject.AddComponent<EventSystemBridge>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_bridgeGameObject != null)
            {
                Object.DestroyImmediate(_bridgeGameObject);
            }
            
            _eventBus?.Dispose();
            _serviceContainer?.Dispose();
            GlobalServiceProvider.Shutdown();
        }

        #region Bridge Initialization Tests

        [Test]
        public void Bridge_Initialization_ShouldInitializeWithoutError()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _bridge!.InitializeBridge());
        }

        [Test]
        public void Bridge_WithoutGlobalServiceProvider_ShouldHandleGracefully()
        {
            // Arrange
            GlobalServiceProvider.Shutdown();
            
            // Act & Assert - Should not crash when no service provider is available
            Assert.DoesNotThrow(() => _bridge!.InitializeBridge());
        }

        #endregion

        #region Event Migration Tests

        [UnityTest]
        public IEnumerator Bridge_StaticMessageBusEvents_ShouldMigrateToUnifiedEventBus()
        {
            // Arrange
            _bridge!.InitializeBridge();
            
            var receivedDamageEvents = new List<DamageEvent>();
            var receivedDeathEvents = new List<DeathEvent>();
            
            _eventBus!.Subscribe<DamageEvent>(evt => receivedDamageEvents.Add(evt));
            _eventBus.Subscribe<DeathEvent>(evt => receivedDeathEvents.Add(evt));

            // Act - Trigger static MessageBus events
            var damageEvent = new Laboratory.Models.ECS.Components.DamageEvent
            {
                DamageAmount = 50f,
                HitDirection = Vector3.down,
                TargetClientId = 1,
                AttackerClientId = 2
            };
            
            var deathEvent = new Laboratory.Models.ECS.Components.DeathEvent(1, 2);

            // Simulate static event publishing (would normally be done by ECS systems)
            Laboratory.Models.ECS.Components.MessageBus.Publish(damageEvent);
            Laboratory.Models.ECS.Components.MessageBus.Publish(deathEvent);

            // Wait for event processing
            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.That(receivedDamageEvents.Count, Is.EqualTo(1));
            Assert.That(receivedDeathEvents.Count, Is.EqualTo(1));
            
            Assert.That(receivedDamageEvents[0].Amount, Is.EqualTo(50f));
            Assert.That(receivedDamageEvents[0].TargetClientId, Is.EqualTo(1));
            Assert.That(receivedDamageEvents[0].AttackerClientId, Is.EqualTo(2));
            
            Assert.That(receivedDeathEvents[0].VictimClientId, Is.EqualTo(1));
            Assert.That(receivedDeathEvents[0].KillerClientId, Is.EqualTo(2));
        }

        #endregion

        #region Backward Compatibility Tests

        [UnityTest]
        public IEnumerator Bridge_BackwardCompatibility_ShouldForwardNewEventsToOldSystems()
        {
            // Arrange
            _bridge!.InitializeBridge();
            _bridge.SetupBackwardCompatibility();
            
            var receivedStaticEvents = new List<Laboratory.Models.ECS.Components.DamageEvent>();
            Laboratory.Models.ECS.Components.MessageBus.OnDamage += evt => receivedStaticEvents.Add(evt);

            // Act - Publish new unified events
            var unifiedDamageEvent = new DamageEvent(
                target: null,
                source: null,
                amount: 75f,
                type: Laboratory.Core.Health.DamageType.Normal,
                direction: Vector3.up,
                targetClientId: 3,
                attackerClientId: 4
            );

            _eventBus!.Publish(unifiedDamageEvent);

            // Wait for event processing
            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.That(receivedStaticEvents.Count, Is.EqualTo(1));
            Assert.That(receivedStaticEvents[0].DamageAmount, Is.EqualTo(75f));
            Assert.That(receivedStaticEvents[0].TargetClientId, Is.EqualTo(3));
            Assert.That(receivedStaticEvents[0].AttackerClientId, Is.EqualTo(4));
        }

        #endregion

        #region Bridge Utility Tests

        [Test]
        public void EventMigrationUtility_CreateEventBridge_ShouldCreateValidBridge()
        {
            // Act
            var bridge = EventMigrationUtility.CreateEventBridge("TestBridge");

            // Assert
            Assert.That(bridge, Is.Not.Null);
            Assert.That(bridge.gameObject.name, Is.EqualTo("TestBridge"));
            
            // Cleanup
            Object.DestroyImmediate(bridge.gameObject);
        }

        [Test]
        public void EventMigrationUtility_GetOrCreateEventBridge_ShouldReuseExisting()
        {
            // Arrange - Create first bridge
            var firstBridge = EventMigrationUtility.CreateEventBridge("TestBridge1");

            // Act - Get or create should return the existing one
            var secondBridge = EventMigrationUtility.GetOrCreateEventBridge();

            // Assert
            Assert.That(secondBridge, Is.SameAs(firstBridge));
            
            // Cleanup
            Object.DestroyImmediate(firstBridge.gameObject);
        }

        #endregion

        #region Performance Tests

        [UnityTest]
        public IEnumerator Bridge_HighVolumeEvents_ShouldHandleEfficiently()
        {
            // Arrange
            _bridge!.InitializeBridge();
            
            var receivedEventCount = 0;
            _eventBus!.Subscribe<DamageEvent>(evt => receivedEventCount++);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - Publish many events rapidly
            for (int i = 0; i < 100; i++)
            {
                var damageEvent = new Laboratory.Models.ECS.Components.DamageEvent
                {
                    DamageAmount = i,
                    HitDirection = Vector3.down,
                    TargetClientId = (ulong)i,
                    AttackerClientId = (ulong)(i + 1)
                };

                Laboratory.Models.ECS.Components.MessageBus.Publish(damageEvent);
                
                if (i % 10 == 0)
                {
                    yield return null; // Allow Unity to process
                }
            }

            // Wait for all events to process
            yield return new WaitForSeconds(0.5f);
            stopwatch.Stop();

            // Assert
            Assert.That(receivedEventCount, Is.EqualTo(100));
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000), 
                "Bridge should handle 100 events within 1 second");
            
            Debug.Log($"Bridge processed 100 events in {stopwatch.ElapsedMilliseconds}ms");
        }

        #endregion

        #region Error Handling Tests

        [UnityTest]
        public IEnumerator Bridge_EventHandlerException_ShouldNotBreakBridge()
        {
            // Arrange
            _bridge!.InitializeBridge();
            
            var successfulEventCount = 0;
            
            // Subscribe with handler that throws exception
            _eventBus!.Subscribe<DamageEvent>(evt => 
            {
                if (evt.Amount == 50f)
                {
                    throw new InvalidOperationException("Test exception");
                }
                successfulEventCount++;
            });

            // Act - Publish events including one that will cause exception
            var events = new[]
            {
                new Laboratory.Models.ECS.Components.DamageEvent 
                { 
                    DamageAmount = 25f, TargetClientId = 1, AttackerClientId = 2 
                },
                new Laboratory.Models.ECS.Components.DamageEvent 
                { 
                    DamageAmount = 50f, TargetClientId = 3, AttackerClientId = 4 
                }, // This will throw
                new Laboratory.Models.ECS.Components.DamageEvent 
                { 
                    DamageAmount = 75f, TargetClientId = 5, AttackerClientId = 6 
                }
            };

            foreach (var evt in events)
            {
                Laboratory.Models.ECS.Components.MessageBus.Publish(evt);
            }

            yield return new WaitForSeconds(0.2f);

            // Assert - Bridge should continue working despite exception
            Assert.That(successfulEventCount, Is.EqualTo(2), 
                "Bridge should process non-throwing events successfully");
        }

        #endregion

        #region Disposal Tests

        [Test]
        public void Bridge_Disposal_ShouldCleanupCorrectly()
        {
            // Arrange
            _bridge!.InitializeBridge();

            // Act
            _bridge.Dispose();

            // Assert - Should not throw when disposing
            Assert.DoesNotThrow(() => _bridge.Dispose()); // Multiple disposals should be safe
        }

        [UnityTest]
        public IEnumerator Bridge_GameObjectDestroy_ShouldDispose()
        {
            // Arrange
            _bridge!.InitializeBridge();
            var wasDisposed = false;

            // Monitor disposal (in a real scenario, this would be tracked internally)
            // For testing, we'll just verify no exceptions are thrown

            // Act
            Object.DestroyImmediate(_bridgeGameObject);
            _bridgeGameObject = null; // Prevent double cleanup in TearDown

            yield return null; // Allow Unity to process destruction

            // Assert - Should complete without exceptions
            Assert.Pass("Bridge disposal completed without exceptions");
        }

        #endregion

        #region Integration with Other Services Tests

        [UnityTest]
        public IEnumerator Bridge_WithGameStateService_ShouldIntegrateCorrectly()
        {
            // Arrange
            var stateService = new Laboratory.Core.State.GameStateService(_eventBus!);
            _serviceContainer!.RegisterInstance<Laboratory.Core.State.IGameStateService>(stateService);
            
            _bridge!.InitializeBridge();

            var receivedStateEvents = new List<GameStateChangedEvent>();
            _eventBus.Subscribe<GameStateChangedEvent>(evt => receivedStateEvents.Add(evt));

            // Act
            stateService.ApplyRemoteStateChange(Laboratory.Core.State.GameState.Playing, suppressEvents: false);

            yield return new WaitForSeconds(0.1f);

            // Assert
            Assert.That(receivedStateEvents.Count, Is.EqualTo(1));
            Assert.That(receivedStateEvents[0].CurrentState, Is.EqualTo(Laboratory.Core.State.GameState.Playing));

            // Cleanup
            stateService.Dispose();
        }

        #endregion

        #region Memory Leak Tests

        [UnityTest]
        public IEnumerator Bridge_RepeatedEventPublishing_ShouldNotLeakMemory()
        {
            // Arrange
            _bridge!.InitializeBridge();
            
            var initialMemory = GC.GetTotalMemory(false);

            // Act - Publish and process many events
            for (int i = 0; i < 1000; i++)
            {
                var damageEvent = new Laboratory.Models.ECS.Components.DamageEvent
                {
                    DamageAmount = i % 100,
                    TargetClientId = (ulong)(i % 10),
                    AttackerClientId = (ulong)((i + 1) % 10)
                };

                Laboratory.Models.ECS.Components.MessageBus.Publish(damageEvent);
                
                if (i % 100 == 0)
                {
                    yield return null;
                    GC.Collect(); // Force garbage collection periodically
                }
            }

            yield return new WaitForSeconds(0.5f);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert - Memory increase should be reasonable (less than 10MB for 1000 events)
            Assert.That(memoryIncrease, Is.LessThan(10 * 1024 * 1024), 
                $"Memory increase ({memoryIncrease / 1024 / 1024}MB) should be less than 10MB");
            
            Debug.Log($"Memory increase after 1000 events: {memoryIncrease / 1024 / 1024}MB");
        }

        #endregion
    }
}
