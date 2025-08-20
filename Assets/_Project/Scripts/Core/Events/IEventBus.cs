using System;
using MessagePipe;
using UniRx;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Unified event bus interface that combines MessagePipe with UniRx convenience methods.
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
        /// Observes events of type T as an IObservable for reactive programming.
        /// </summary>
        UniRx.IObservable<T> Observe<T>() where T : class;
        
        /// <summary>
        /// Subscribes to events of type T on the main thread (Unity thread-safe).
        /// </summary>
        IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class;
    }
}
