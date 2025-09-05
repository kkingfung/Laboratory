using System;
using R3;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Unified event bus interface that provides reactive event handling without external dependencies.
    /// Uses R3 for reliable, performant event management in Unity projects.
    /// </summary>
    public interface IEventBus : IDisposable
    {
        /// <summary>
        /// Publishes an event message to all subscribers.
        /// </summary>
        void Publish<T>(T message) where T : class;
        
        /// <summary>
        /// Subscribes to events of type T with an action handler.
        /// </summary>
        IDisposable Subscribe<T>(Action<T> handler) where T : class;
        
        /// <summary>
        /// Observes events of type T as an Observable for reactive programming.
        /// Returns an observable object (implementation-specific type).
        /// </summary>
        object Observe<T>() where T : class;
        
        /// <summary>
        /// Subscribes to events of type T on the main thread (Unity thread-safe).
        /// </summary>
        IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class;
        
        /// <summary>
        /// Subscribes with filtering predicate.
        /// </summary>
        IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> handler) where T : class;
        
        /// <summary>
        /// Subscribes for only the first occurrence of an event.
        /// </summary>
        IDisposable SubscribeFirst<T>(Action<T> handler) where T : class;
        
        /// <summary>
        /// Gets count of active subscribers for a specific event type.
        /// </summary>
        int GetSubscriberCount<T>() where T : class;
        
        /// <summary>
        /// Clears all subscriptions for a specific event type.
        /// </summary>
        void ClearSubscriptions<T>() where T : class;
    }
}
