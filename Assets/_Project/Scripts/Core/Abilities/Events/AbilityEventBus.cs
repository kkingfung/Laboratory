using System;
using UnityEngine;
using UnityEngine.Events;

namespace Laboratory.Core.Abilities.Events
{

    /// <summary>
    /// Event fired when an ability execution fails
    /// </summary>
    [System.Serializable]
    public class AbilityExecutionFailedEvent
    {
        public GameObject Source { get; }
        public int AbilityIndex { get; }
        public string FailureReason { get; }
        public float Timestamp { get; }

        public AbilityExecutionFailedEvent(GameObject source, int abilityIndex, string failureReason)
        {
            Source = source;
            AbilityIndex = abilityIndex;
            FailureReason = failureReason;
            Timestamp = Time.time;
        }
    }

    /// <summary>
    /// Unity Events for ability system
    /// </summary>
    [System.Serializable] public class AbilityActivatedUnityEvent : UnityEvent<AbilityActivatedEvent> { }
    [System.Serializable] public class AbilityStateChangedUnityEvent : UnityEvent<AbilityStateChangedEvent> { }
    [System.Serializable] public class AbilityCooldownCompleteUnityEvent : UnityEvent<AbilityCooldownCompleteEvent> { }
    [System.Serializable] public class AbilityExecutionFailedUnityEvent : UnityEvent<AbilityExecutionFailedEvent> { }

    /// <summary>
    /// Global event bus for ability system events.
    /// Provides centralized event publishing and subscription for ability-related events.
    /// </summary>
    public static class AbilityEventBus
    {
        #region Events

        /// <summary>
        /// Event fired when any ability is activated
        /// </summary>
        public static AbilityActivatedUnityEvent OnAbilityActivated { get; } = new AbilityActivatedUnityEvent();

        /// <summary>
        /// Event fired when any ability's state changes
        /// </summary>
        public static AbilityStateChangedUnityEvent OnAbilityStateChanged { get; } = new AbilityStateChangedUnityEvent();

        /// <summary>
        /// Event fired when any ability's cooldown completes
        /// </summary>
        public static AbilityCooldownCompleteUnityEvent OnAbilityCooldownComplete { get; } = new AbilityCooldownCompleteUnityEvent();

        /// <summary>
        /// Event fired when any ability execution fails
        /// </summary>
        public static AbilityExecutionFailedUnityEvent OnAbilityExecutionFailed { get; } = new AbilityExecutionFailedUnityEvent();

        #endregion

        #region Publishing Methods

        /// <summary>
        /// Publishes an ability activated event
        /// </summary>
        public static void PublishAbilityActivated(GameObject source, int abilityIndex)
        {
            var evt = new AbilityActivatedEvent(abilityIndex, source);
            OnAbilityActivated.Invoke(evt);
        }

        /// <summary>
        /// Publishes an ability state changed event
        /// </summary>
        public static void PublishAbilityStateChanged(GameObject source, int abilityIndex, bool isOnCooldown, float cooldownRemaining)
        {
            var evt = new AbilityStateChangedEvent(abilityIndex, isOnCooldown, cooldownRemaining, source);
            OnAbilityStateChanged.Invoke(evt);
        }

        /// <summary>
        /// Publishes an ability cooldown complete event
        /// </summary>
        public static void PublishAbilityCooldownComplete(GameObject source, int abilityIndex)
        {
            var evt = new AbilityCooldownCompleteEvent(abilityIndex, source);
            OnAbilityCooldownComplete.Invoke(evt);
        }

        /// <summary>
        /// Publishes an ability execution failed event
        /// </summary>
        public static void PublishAbilityExecutionFailed(GameObject source, int abilityIndex, string failureReason)
        {
            var evt = new AbilityExecutionFailedEvent(source, abilityIndex, failureReason);
            OnAbilityExecutionFailed.Invoke(evt);
        }

        #endregion

        #region Subscription Helpers

        /// <summary>
        /// Subscribes to ability activated events from a specific source
        /// </summary>
        public static void SubscribeToAbilityActivated(GameObject source, Action<AbilityActivatedEvent> callback)
        {
            OnAbilityActivated.AddListener(evt =>
            {
                if (evt.Source == source)
                    callback?.Invoke(evt);
            });
        }

        /// <summary>
        /// Subscribes to ability state changed events from a specific source
        /// </summary>
        public static void SubscribeToAbilityStateChanged(GameObject source, Action<AbilityStateChangedEvent> callback)
        {
            OnAbilityStateChanged.AddListener(evt =>
            {
                if (evt.Source == source)
                    callback?.Invoke(evt);
            });
        }

        /// <summary>
        /// Subscribes to ability cooldown complete events from a specific source
        /// </summary>
        public static void SubscribeToAbilityCooldownComplete(GameObject source, Action<AbilityCooldownCompleteEvent> callback)
        {
            OnAbilityCooldownComplete.AddListener(evt =>
            {
                if (evt.Source == source)
                    callback?.Invoke(evt);
            });
        }

        /// <summary>
        /// Subscribes to ability execution failed events from a specific source
        /// </summary>
        public static void SubscribeToAbilityExecutionFailed(GameObject source, Action<AbilityExecutionFailedEvent> callback)
        {
            OnAbilityExecutionFailed.AddListener(evt =>
            {
                if (evt.Source == source)
                    callback?.Invoke(evt);
            });
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clears all event subscriptions. Use with caution.
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnAbilityActivated.RemoveAllListeners();
            OnAbilityStateChanged.RemoveAllListeners();
            OnAbilityCooldownComplete.RemoveAllListeners();
            OnAbilityExecutionFailed.RemoveAllListeners();
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Gets the number of listeners for each event type
        /// </summary>
        public static AbilityEventBusStats GetStats()
        {
            return new AbilityEventBusStats
            {
                AbilityActivatedListeners = OnAbilityActivated.GetPersistentEventCount(),
                AbilityStateChangedListeners = OnAbilityStateChanged.GetPersistentEventCount(),
                AbilityCooldownCompleteListeners = OnAbilityCooldownComplete.GetPersistentEventCount(),
                AbilityExecutionFailedListeners = OnAbilityExecutionFailed.GetPersistentEventCount()
            };
        }

        #endregion
    }

    /// <summary>
    /// Statistics for the ability event bus
    /// </summary>
    [System.Serializable]
    public struct AbilityEventBusStats
    {
        public int AbilityActivatedListeners;
        public int AbilityStateChangedListeners;
        public int AbilityCooldownCompleteListeners;
        public int AbilityExecutionFailedListeners;

        public int TotalListeners => AbilityActivatedListeners + AbilityStateChangedListeners + 
                                   AbilityCooldownCompleteListeners + AbilityExecutionFailedListeners;

        public override string ToString()
        {
            return $"AbilityEventBus Stats - Total: {TotalListeners} " +
                   $"(Activated: {AbilityActivatedListeners}, " +
                   $"StateChanged: {AbilityStateChangedListeners}, " +
                   $"CooldownComplete: {AbilityCooldownCompleteListeners}, " +
                   $"ExecutionFailed: {AbilityExecutionFailedListeners})";
        }
    }
}
