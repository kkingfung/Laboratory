using System;
using MessagingPipe;
using UniRx;

namespace Infrastructure
{
    /// <summary>
    /// Reactive Event Bus based on MessagingPipe and UniRx.
    /// Simplifies publishing and subscribing to events with IDisposable support.
    /// </summary>
    public class ReactiveEventBus : IDisposable
    {
        private readonly IMessageBroker _messageBroker;

        public ReactiveEventBus(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        /// <summary>
        /// Publish an event message.
        /// </summary>
        public void Publish<T>(T message)
        {
            _messageBroker.Publish(message);
        }

        /// <summary>
        /// Subscribe to event of type T.
        /// Returns IDisposable for unsubscription.
        /// </summary>
        public IDisposable Subscribe<T>(Action<T> handler)
        {
            return _messageBroker.Subscribe(handler);
        }

        /// <summary>
        /// Subscribe to event of type T with UniRx Reactive Extensions.
        /// </summary>
        public IObservable<T> Observe<T>()
        {
            // MessagingPipe's IMessageBroker doesn't directly provide IObservable,
            // but we can create an observable wrapper.
            return new MessagingPipeObservable<T>(_messageBroker);
        }

        /// <summary>
        /// Dispose pattern to clean up resources if needed.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose in IMessageBroker itself
            // If you extend this with more disposables, clean here
        }

        #region Private Observable Wrapper

        private class MessagingPipeObservable<T> : IObservable<T>
        {
            private readonly IMessageBroker _broker;

            public MessagingPipeObservable(IMessageBroker broker)
            {
                _broker = broker;
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                return _broker.Subscribe(observer.OnNext);
            }
        }

        #endregion
    }
}
