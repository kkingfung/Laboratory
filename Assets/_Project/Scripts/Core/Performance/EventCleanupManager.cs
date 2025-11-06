using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Automatic event subscription cleanup manager to prevent memory leaks.
    /// Tracks all event subscriptions and ensures they're properly disposed.
    /// Inherit from CleanupBehaviour instead of MonoBehaviour for automatic cleanup.
    /// </summary>
    public abstract class CleanupBehaviour : MonoBehaviour
    {
        private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
        private readonly List<Action> _cleanupActions = new List<Action>();

        /// <summary>
        /// Register a disposable subscription for automatic cleanup
        /// </summary>
        protected void RegisterSubscription(IDisposable subscription)
        {
            if (subscription != null)
            {
                _subscriptions.Add(subscription);
            }
        }

        /// <summary>
        /// Register a custom cleanup action
        /// </summary>
        protected void RegisterCleanup(Action cleanupAction)
        {
            if (cleanupAction != null)
            {
                _cleanupActions.Add(cleanupAction);
            }
        }

        /// <summary>
        /// Manually trigger cleanup (normally called automatically in OnDestroy)
        /// </summary>
        protected void PerformCleanup()
        {
            // Dispose all subscriptions
            foreach (var subscription in _subscriptions)
            {
                try
                {
                    subscription?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CleanupBehaviour] Error disposing subscription: {ex.Message}");
                }
            }
            _subscriptions.Clear();

            // Execute cleanup actions
            foreach (var action in _cleanupActions)
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CleanupBehaviour] Error executing cleanup action: {ex.Message}");
                }
            }
            _cleanupActions.Clear();
        }

        protected virtual void OnDestroy()
        {
            PerformCleanup();
        }
    }

    /// <summary>
    /// Disposable event subscription wrapper
    /// </summary>
    public class EventSubscription : IDisposable
    {
        private Action _unsubscribeAction;
        private bool _disposed = false;

        public EventSubscription(Action unsubscribeAction)
        {
            _unsubscribeAction = unsubscribeAction;
        }

        public void Dispose()
        {
            if (!_disposed && _unsubscribeAction != null)
            {
                _unsubscribeAction.Invoke();
                _unsubscribeAction = null;
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Extension methods for event subscription tracking
    /// </summary>
    public static class EventSubscriptionExtensions
    {
        /// <summary>
        /// Subscribe to an event with automatic cleanup tracking
        /// </summary>
        public static IDisposable SubscribeWithCleanup<T>(
            this Action<T> eventHandler,
            Action<T> handler,
            Action<Action<T>> subscribe,
            Action<Action<T>> unsubscribe)
        {
            subscribe(handler);
            return new EventSubscription(() => unsubscribe(handler));
        }

        /// <summary>
        /// Subscribe to a parameterless event with automatic cleanup
        /// </summary>
        public static IDisposable SubscribeWithCleanup(
            this Action eventHandler,
            Action handler,
            Action<Action> subscribe,
            Action<Action> unsubscribe)
        {
            subscribe(handler);
            return new EventSubscription(() => unsubscribe(handler));
        }
    }

    /// <summary>
    /// Example usage of CleanupBehaviour for preventing memory leaks
    /// </summary>
    public class ExampleCleanupUsage : CleanupBehaviour
    {
        private void Start()
        {
            // Example 1: Register event subscription
            var subscription = SomeEventBus.Subscribe<GameEvent>(OnGameEvent);
            RegisterSubscription(subscription);

            // Example 2: Register Unity event
            var button = GetComponent<UnityEngine.UI.Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
                RegisterCleanup(() => button.onClick.RemoveListener(OnButtonClick));
            }

            // Example 3: Register custom cleanup
            RegisterCleanup(() => Debug.Log("Custom cleanup executed"));
        }

        private void OnGameEvent(GameEvent evt)
        {
            // Handle event
        }

        private void OnButtonClick()
        {
            // Handle button click
        }

        // OnDestroy automatically called by CleanupBehaviour - no need to implement!
    }

    /// <summary>
    /// Global event subscription tracker for debugging memory leaks
    /// </summary>
    public static class EventSubscriptionTracker
    {
        private static readonly Dictionary<Type, int> _activeSubscriptions = new Dictionary<Type, int>();
        private static readonly object _lock = new object();

        public static void TrackSubscription<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (!_activeSubscriptions.ContainsKey(type))
                {
                    _activeSubscriptions[type] = 0;
                }
                _activeSubscriptions[type]++;
            }
        }

        public static void TrackUnsubscription<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_activeSubscriptions.ContainsKey(type))
                {
                    _activeSubscriptions[type]--;
                    if (_activeSubscriptions[type] <= 0)
                    {
                        _activeSubscriptions.Remove(type);
                    }
                }
            }
        }

        public static void PrintActiveSubscriptions()
        {
            lock (_lock)
            {
                Debug.Log("=== Active Event Subscriptions ===");
                foreach (var kvp in _activeSubscriptions)
                {
                    if (kvp.Value > 0)
                    {
                        Debug.Log($"{kvp.Key.Name}: {kvp.Value} active subscriptions");
                    }
                }
            }
        }

        public static int GetSubscriptionCount<T>()
        {
            lock (_lock)
            {
                var type = typeof(T);
                return _activeSubscriptions.TryGetValue(type, out var count) ? count : 0;
            }
        }

        public static void Clear()
        {
            lock (_lock)
            {
                _activeSubscriptions.Clear();
            }
        }
    }

    // Placeholder classes for example
    public class GameEvent { }
    public static class SomeEventBus
    {
        public static IDisposable Subscribe<T>(Action<T> handler) => null;
    }
}
