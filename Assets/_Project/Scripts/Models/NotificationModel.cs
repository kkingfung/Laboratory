using MessagePipe;
using UniRx;
using Laboratory.Core.Events;

namespace Laboratory.Models
{
    /// <summary>
    /// Model for handling notification messages through the message broker.
    /// </summary>
    public class NotificationModel
    {
        #region Fields

        private readonly IMessageBroker _messageBroker;

        #endregion

        #region Constructor

        public NotificationModel(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a notification message through the event bus.
        /// </summary>
        /// <param name="message">The notification message to send.</param>
        public void SendNotification(string message)
        {
            _messageBroker.Publish(new NotificationEvent(message));
        }

        #endregion
    }
}
