using System;
using UnityEngine;
using UnityEngine.Events;
using Laboratory.Core.Events;

namespace Laboratory.Core.Abilities.Events
{
    /// <summary>
    /// Centralized event bus for ability-related events.
    /// Provides a unified way to publish and subscribe to ability events throughout the game.
    /// </summary>
    public static class AbilityEventBus
    {
        #region Event Declarations

        /// <summary>
        /// Event fired when any ability is activated.
        /// </summary>
        public static readonly UnityEvent<AbilityActivatedEvent> OnAbilityActivated = new();

        /// <summary>
        /// Event fired when any ability state changes (cooldown, etc.).
        /// </summary>
        public static readonly UnityEvent<AbilityStateChangedEvent> OnAbilityStateChanged = new();

        /// <summary>
        /// Event fired when any ability cooldown completes.
        /// </summary>
        public static readonly UnityEvent<AbilityCooldownCompleteEvent> OnAbilityCooldownComplete = new();

        #endregion

        #region Publishing Methods

        /// <summary>
        /// Publishes an ability activated event.
        /// </summary>
        public static void PublishAbilityActivated(AbilityActivatedEvent evt)
        {
            OnAbilityActivated.Invoke(evt);
            
            // Also publish to global event bus if available
            if (UnifiedEventBus.Instance != null)
            {
                UnifiedEventBus.Instance.Publish(evt);
            }
        }

        /// <summary>
        /// Publishes an ability state changed event.
        /// </summary>
        public static void PublishAbilityStateChanged(AbilityStateChangedEvent evt)
        {
            OnAbilityStateChanged.Invoke(evt);
            
            if (UnifiedEventBus.Instance != null)
            {
                UnifiedEventBus.Instance.Publish(evt);
            }
        }

        /// <summary>
        /// Publishes an ability cooldown complete event.
        /// </summary>
        public static void PublishAbilityCooldownComplete(AbilityCooldownCompleteEvent evt)
        {
            OnAbilityCooldownComplete.Invoke(evt);
            
            if (UnifiedEventBus.Instance != null)
            {
                UnifiedEventBus.Instance.Publish(evt);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Clears all event subscriptions. Should only be called during cleanup.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnAbilityActivated.RemoveAllListeners();
            OnAbilityStateChanged.RemoveAllListeners();
            OnAbilityCooldownComplete.RemoveAllListeners();
        }

        /// <summary>
        /// Gets the total number of listeners across all events.
        /// Useful for debugging and memory leak detection.
        /// </summary>
        public static int GetTotalListenerCount()
        {
            return OnAbilityActivated.GetPersistentEventCount() +
                   OnAbilityStateChanged.GetPersistentEventCount() +
                   OnAbilityCooldownComplete.GetPersistentEventCount();
        }

        #endregion

        #region Debug Methods

#if UNITY_EDITOR || DEBUG
        /// <summary>
        /// Logs all active subscriptions for debugging.
        /// Only available in editor or debug builds.
        /// </summary>
        public static void LogActiveSubscriptions()
        {
            Debug.Log($"[AbilityEventBus] Active Subscriptions:" +
                     $"\n  OnAbilityActivated: {OnAbilityActivated.GetPersistentEventCount()}" +
                     $"\n  OnAbilityStateChanged: {OnAbilityStateChanged.GetPersistentEventCount()}" +
                     $"\n  OnAbilityCooldownComplete: {OnAbilityCooldownComplete.GetPersistentEventCount()}");
        }
#endif

        #endregion
    }
}
