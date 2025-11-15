using System;

#nullable enable

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Main event bus interface for publish/subscribe messaging.
    /// Simplified version to avoid circular dependencies.
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
    }
}