using System;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;
using Laboratory.Gameplay.Combat;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Bridge component that migrates old event systems to the UnifiedEventBus.
    /// This provides backward compatibility while transitioning to the new architecture.
    /// </summary>
    public class EventSystemBridge : MonoBehaviour, IDisposable
    {
        #region Fields

        private IEventBus _eventBus;
        private DamageEventBus _oldDamageEventBus;
        private bool _disposed = false;

        // Legacy event subscriptions
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
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Services?.TryResolve<IEventBus>(out _eventBus);
            }

            if (_eventBus == null)
            {
                Debug.LogError("EventSystemBridge: Could not resolve IEventBus from GlobalServiceProvider");
                return;
            }

            SetupLegacyEventBridges();
            SetupStaticEventBridges();

            Debug.Log("EventSystemBridge: Successfully initialized event migration bridges");
        }

        /// <summary>
        /// Manually set the damage event bus for bridging (useful for testing).
        /// </summary>
        public void SetDamageEventBus(DamageEventBus damageEventBus)
        {
            _oldDamageEventBus = damageEventBus;
            BridgeDamageEventBus();
        }

        #endregion

        #region Private Methods

        private void SetupLegacyEventBridges()
        {
            // Bridge old DamageEventBus to UnifiedEventBus
            BridgeDamageEventBus();
        }

        private void SetupStaticEventBridges()
        {
            // Bridge static MessageBus events to UnifiedEventBus
            BridgeStaticMessageBus();
        }

        private void BridgeDamageEventBus()
        {
            // Find existing DamageEventBus instances
            if (_oldDamageEventBus == null)
            {
                var damageEventBusComponent = FindObjectOfType<MonoBehaviour>()?.GetComponent<DamageEventBus>();
                if (damageEventBusComponent != null)
                {
                    _oldDamageEventBus = damageEventBusComponent;
                }
            }

            if (_oldDamageEventBus != null)
            {
                // Subscribe to old events and republish on new system
                _oldDamageEventBus.Subscribe(OnLegacyDamageEvent);
                Debug.Log("EventSystemBridge: Bridged DamageEventBus to UnifiedEventBus");
            }
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

        private void OnLegacyDamageEvent(Laboratory.Gameplay.Combat.DamageEventBus.DamageEvent legacyEvent)
        {
            // Convert legacy damage event to new unified format
            var unifiedEvent = new Messages.DamageEvent(
                target: null, // Legacy event doesn't have GameObject references
                source: null,
                amount: legacyEvent.Amount,
                type: Laboratory.Core.Health.DamageType.Normal, // Default type
                direction: Vector3.zero,
                targetClientId: (ulong)legacyEvent.TargetId,
                attackerClientId: (ulong)legacyEvent.SourceId
            );

            _eventBus?.Publish(unifiedEvent);
            Debug.Log($"EventSystemBridge: Converted legacy damage event - Target: {legacyEvent.TargetId}, Amount: {legacyEvent.Amount}");
        }

        private void OnStaticDamageEvent(Laboratory.Models.ECS.Components.DamageEvent staticEvent)
        {
            // Convert static damage event to unified format
            var unifiedEvent = new Messages.DamageEvent(
                target: null, // Static event has limited context
                source: null,
                amount: staticEvent.DamageAmount,
                type: Laboratory.Core.Health.DamageType.Normal,
                direction: staticEvent.HitDirection,
                targetClientId: staticEvent.TargetClientId,
                attackerClientId: staticEvent.AttackerClientId
            );

            _eventBus?.Publish(unifiedEvent);
            Debug.Log($"EventSystemBridge: Converted static damage event - Target: {staticEvent.TargetClientId}, Amount: {staticEvent.DamageAmount}");
        }

        private void OnStaticDeathEvent(Laboratory.Models.ECS.Components.DeathEvent staticEvent)
        {
            // Convert static death event to unified format
            var unifiedEvent = new Messages.DeathEvent(
                target: null,
                source: null,
                victimClientId: staticEvent.VictimClientId,
                killerClientId: staticEvent.KillerClientId
            );

            _eventBus?.Publish(unifiedEvent);
            Debug.Log($"EventSystemBridge: Converted static death event - Victim: {staticEvent.VictimClientId}, Killer: {staticEvent.KillerClientId}");
        }

        #endregion

        #region Migration Utilities

        /// <summary>
        /// Subscribes to new unified events and forwards to legacy handlers for backward compatibility.
        /// </summary>
        public void SetupBackwardCompatibility()
        {
            if (_eventBus == null) return;

            // Subscribe to new events and forward to old systems if needed
            _damageSubscription = _eventBus.Subscribe<Messages.DamageEvent>(OnUnifiedDamageEvent);
            _deathSubscription = _eventBus.Subscribe<Messages.DeathEvent>(OnUnifiedDeathEvent);

            Debug.Log("EventSystemBridge: Setup backward compatibility forwarding");
        }

        private void OnUnifiedDamageEvent(Messages.DamageEvent unifiedEvent)
        {
            // Forward to legacy systems if they still exist and need the events
            if (_oldDamageEventBus != null && unifiedEvent.TargetClientId > 0 && unifiedEvent.AttackerClientId > 0)
            {
                var legacyEvent = new Laboratory.Gameplay.Combat.DamageEventBus.DamageEvent(
                    (int)unifiedEvent.AttackerClientId,
                    (int)unifiedEvent.TargetClientId,
                    unifiedEvent.Amount
                );

                _oldDamageEventBus.Publish(legacyEvent);
            }
        }

        private void OnUnifiedDeathEvent(Messages.DeathEvent unifiedEvent)
        {
            // Forward to static MessageBus for systems still using it
            if (unifiedEvent.VictimClientId > 0 && unifiedEvent.KillerClientId > 0)
            {
                var staticEvent = new Laboratory.Models.ECS.Components.DeathEvent(
                    unifiedEvent.VictimClientId,
                    unifiedEvent.KillerClientId
                );

                Laboratory.Models.ECS.Components.MessageBus.Publish(staticEvent);
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

                // Unsubscribe from legacy events
                if (_oldDamageEventBus != null)
                {
                    _oldDamageEventBus.Unsubscribe(OnLegacyDamageEvent);
                }

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
            var existingBridge = UnityEngine.Object.FindObjectOfType<EventSystemBridge>();
            if (existingBridge != null)
            {
                return existingBridge;
            }

            return CreateEventBridge();
        }
    }
}
