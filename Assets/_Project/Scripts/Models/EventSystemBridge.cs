using System;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Enums;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models
{
    /// <summary>
    /// Bridge component that migrates old event systems to the UnifiedEventBus.
    /// This provides backward compatibility while transitioning to the new architecture.
    /// </summary>
    public class EventSystemBridge : MonoBehaviour, IDisposable
    {
        #region Fields

        private IEventBus _eventBus;
        private bool _disposed = false;

        private IDisposable _damageSubscription;
        private IDisposable _deathSubscription;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeBridge();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the bridge system. Call this after services are available.
        /// </summary>
        public void InitializeBridge()
        {
            if (_disposed) return;

            // Get the unified event bus
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
            }

            if (_eventBus == null)
            {
                Debug.LogError("EventSystemBridge: Could not resolve IEventBus from ServiceContainer");
                return;
            }

            SetupStaticEventBridges();

            Debug.Log("EventSystemBridge: Successfully initialized event migration bridges");
        }

        /// <summary>
        /// Manually set the damage event bus for bridging (useful for testing).
        /// </summary>
        public void SetDamageEventBus(object damageEventBus)
        {
            BridgeDamageEventBus();
        }

        /// <summary>
        /// Bridge damage event bus systems.
        /// </summary>
        private void BridgeDamageEventBus()
        {
            // Additional damage event bridging logic can be added here
            Debug.Log("EventSystemBridge: Damage event bus bridging configured");
        }

        #endregion

        #region Private Methods

        private void SetupStaticEventBridges()
        {
            // Bridge static MessageBus events to UnifiedEventBus
            BridgeStaticMessageBus();
        }

        private void BridgeStaticMessageBus()
        {
            // Bridge static MessageBus events
            Laboratory.Models.ECS.Components.MessageBus.OnDamage += OnStaticDamageEvent;
            Laboratory.Models.ECS.Components.MessageBus.OnDeath += OnStaticDeathEvent;

            Debug.Log("EventSystemBridge: Bridged static MessageBus to UnifiedEventBus");
        }

        #endregion

        #region Event Bridge Handlers

        private void OnStaticDamageEvent(Laboratory.Models.ECS.Components.DamageEvent staticEvent)
        {
            // Convert static damage event to unified format
            var unifiedEvent = new Laboratory.Core.Events.Messages.DamageEvent(
                target: null, // Static event has limited context
                source: null,
                amount: staticEvent.DamageAmount,
                damageType: Laboratory.Core.Enums.DamageType.Physical,
                direction: staticEvent.HitDirection
            );

            _eventBus?.Publish(unifiedEvent);
            Debug.Log($"EventSystemBridge: Converted static damage event - Target: {staticEvent.TargetClientId}, Amount: {staticEvent.DamageAmount}");
        }

        private void OnStaticDeathEvent(Laboratory.Models.ECS.Components.DeathEvent staticEvent)
        {
            // Convert static death event to unified format
            var unifiedEvent = new Laboratory.Core.Events.Messages.DeathEvent(
                target: null,
                source: null
            );

            _eventBus?.Publish(unifiedEvent);
            Debug.Log($"EventSystemBridge: Converted static death event - Victim: {staticEvent.VictimClientId}, Killer: {staticEvent.KillerClientId}");
        }

        #endregion

        #region Migration Utilities

        private void OnUnifiedDeathEvent(Laboratory.Core.Events.Messages.DeathEvent unifiedEvent)
        {
            // Forward to static MessageBus for systems still using it
            // Note: Unified DeathEvent doesn't contain client IDs, so we can't create a complete static event
            // This is a limitation of the event bridge - consider enhancing the unified DeathEvent class
            if (unifiedEvent.Target != null)
            {
                Debug.LogWarning($"EventSystemBridge: Cannot fully convert unified DeathEvent to static format - missing client ID data");
                // Could create a default static event here if needed
            }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                // Unsubscribe from static events
                Laboratory.Models.ECS.Components.MessageBus.OnDamage -= OnStaticDamageEvent;
                Laboratory.Models.ECS.Components.MessageBus.OnDeath -= OnStaticDeathEvent;

                // Dispose subscriptions
                _damageSubscription?.Dispose();
                _deathSubscription?.Dispose();

                Debug.Log("EventSystemBridge: Disposed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"EventSystemBridge: Error during disposal: {ex}");
            }
            finally
            {
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Static utility class for easy event system migration.
    /// </summary>
    public static class EventMigrationUtility
    {
        /// <summary>
        /// Create and setup an EventSystemBridge in the current scene.
        /// </summary>
        public static EventSystemBridge CreateEventBridge(string gameObjectName = "EventSystemBridge")
        {
            var bridgeObject = new GameObject(gameObjectName);
            var bridge = bridgeObject.AddComponent<EventSystemBridge>();
            
            // Don't destroy on load to persist across scenes
            UnityEngine.Object.DontDestroyOnLoad(bridgeObject);
            
            return bridge;
        }

        /// <summary>
        /// Find or create an EventSystemBridge in the scene.
        /// </summary>
        public static EventSystemBridge GetOrCreateEventBridge()
        {
            var existingBridge = UnityEngine.Object.FindFirstObjectByType<EventSystemBridge>();
            if (existingBridge != null)
            {
                return existingBridge;
            }

            return CreateEventBridge();
        }
    }
}
