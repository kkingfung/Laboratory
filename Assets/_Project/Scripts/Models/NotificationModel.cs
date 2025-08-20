using MessagePipe;
using Laboratory.Core.Events;

namespace Laboratory.Models
{
    /// <summary>
    /// Model for handling notification messages through the message broker.
    /// </summary>
    public class NotificationModel
    {
        #region Fields

        private readonly IPublisher<NotificationEvent> _publisher;

        #endregion

        #region Constructor

        public NotificationModel(IPublisher<NotificationEvent> publisher)
        {
            _publisher = publisher;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a notification message through the event bus.
        /// </summary>
        /// <param name="message">The notification message to send.</param>
        public void SendNotification(string message)
        {
            _publisher.Publish(new NotificationEvent(message));
        }

        #endregion
    }
}
