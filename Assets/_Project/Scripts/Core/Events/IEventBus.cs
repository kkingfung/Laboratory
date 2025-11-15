using System;
using R3;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Main event bus interface for publish/subscribe messaging with enhanced features.
    /// </summary>
    public interface IEventBus : IDisposable
    {
        /// <summary>
        /// Publishes an event to all subscribers.
        /// </summary>
        void Publish<T>(T message) where T : class;

        /// <summary>
        /// Subscribes to events of type T.
        /// </summary>
        /// <returns>Disposable subscription</returns>
        IDisposable Subscribe<T>(Action<T> handler) where T : class;

        /// <summary>
        /// Gets an observable for events of type T.
        /// </summary>
        Observable<T> AsObservable<T>() where T : class;

        /// <summary>
        /// Observes events of type T (compatibility method).
        /// </summary>
        object Observe<T>() where T : class;

        /// <summary>
        /// Subscribes to events on the main thread.
        /// </summary>
        IDisposable SubscribeOnMainThread<T>(Action<T> handler) where T : class;

        /// <summary>
        /// Subscribes to events with a predicate filter.
        /// </summary>
        IDisposable SubscribeWhere<T>(Func<T, bool> predicate, Action<T> handler) where T : class;

        /// <summary>
        /// Subscribes to the first event of type T only.
        /// </summary>
        IDisposable SubscribeFirst<T>(Action<T> handler) where T : class;

        /// <summary>
        /// Gets the number of subscribers for a specific event type.
        /// </summary>
        int GetSubscriberCount<T>() where T : class;

        /// <summary>
        /// Clears all subscriptions for a specific event type.
        /// </summary>
        void ClearSubscriptions<T>() where T : class;
    }
}
