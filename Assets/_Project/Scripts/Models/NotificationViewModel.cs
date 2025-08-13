using MessagePipe;
using UniRx;

namespace Laboratory.Models.ViewModels
{
    /// <summary>
    /// ViewModel for handling notification messages through the message broker.
    /// </summary>
    public class NotificationViewModel
    {
        #region Fields

        private readonly IMessageBroker _messageBroker;

        #endregion

        #region Constructor

        public NotificationViewModel(IMessageBroker messageBroker)
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
            _messageBroker.Publish(new Laboratory.UI.Events.NotificationEvent(message));
        }

        #endregion

        #region Private Methods

        // No private methods currently.

        #endregion

        #region Inner Classes, Enums

        // No inner classes or enums currently.

        #endregion
    }
}
