using System;
using MessagePipe;
using UniRx;

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Reactive Event Bus based on MessagingPipe and UniRx.
    /// Simplifies publishing and subscribing to events with IDisposable support.
    /// </summary>
    public class ReactiveEventBus : IDisposable
    {
        #region Fields

        private readonly IMessageBroker _messageBroker;

        #endregion

        #region Constructor

        public ReactiveEventBus(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        #endregion

        #region Public Methods

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
        public System.IObservable<T> Observe<T>()
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

        #endregion

        #region Private Methods

        // No private methods currently.

        #endregion

        #region Inner Classes, Enums

        private class MessagingPipeObservable<T> : System.IObservable<T>
        {
            private readonly IMessageBroker _broker;

            public MessagingPipeObservable(IMessageBroker broker)
            {
                _broker = broker;
            }

            public IDisposable Subscribe(System.IObserver<T> observer)
            {
                return _broker.Subscribe(observer.OnNext);
            }
        }

        #endregion
    }
}
