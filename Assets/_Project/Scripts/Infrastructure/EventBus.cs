using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Simple event bus system for decoupled communication
    /// </summary>
    public class EventBus : MonoBehaviour
    {
        private static EventBus _instance;
        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<EventBus>();
                    if (_instance == null)
                    {
                        GameObject eventBusObj = new GameObject("EventBus");
                        _instance = eventBusObj.AddComponent<EventBus>();
                        DontDestroyOnLoad(eventBusObj);
                    }
                }
                return _instance;
            }
        }

        private Dictionary<Type, List<object>> _eventSubscriptions = new Dictionary<Type, List<object>>();

        /// <summary>
        /// Subscribe to an event type
        /// </summary>
        public void Subscribe<T>(Action<T> handler) where T : class
        {
            Type eventType = typeof(T);
            if (!_eventSubscriptions.ContainsKey(eventType))
            {
                _eventSubscriptions[eventType] = new List<object>();
            }
            _eventSubscriptions[eventType].Add(handler);
        }

        /// <summary>
        /// Unsubscribe from an event type
        /// </summary>
        public void Unsubscribe<T>(Action<T> handler) where T : class
        {
            Type eventType = typeof(T);
            if (_eventSubscriptions.ContainsKey(eventType))
            {
                _eventSubscriptions[eventType].Remove(handler);
            }
        }

        /// <summary>
        /// Publish an event
        /// </summary>
        public void Publish<T>(T eventData) where T : class
        {
            Type eventType = typeof(T);
            if (_eventSubscriptions.ContainsKey(eventType))
            {
                foreach (var subscription in _eventSubscriptions[eventType])
                {
                    if (subscription is Action<T> handler)
                    {
                        try
                        {
                            handler.Invoke(eventData);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"Error handling event {eventType.Name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clear all subscriptions
        /// </summary>
        public void ClearAllSubscriptions()
        {
            _eventSubscriptions.Clear();
        }

        /// <summary>
        /// Clear subscriptions for a specific event type
        /// </summary>
        public void ClearSubscriptions<T>() where T : class
        {
            Type eventType = typeof(T);
            if (_eventSubscriptions.ContainsKey(eventType))
            {
                _eventSubscriptions[eventType].Clear();
            }
        }

        private void OnDestroy()
        {
            ClearAllSubscriptions();
        }
    }
}
