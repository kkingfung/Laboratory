using MessagePipe;
using UniRx;

namespace Infrastructure.UI.ViewModels
{
    public class NotificationViewModel
    {
        private readonly IMessageBroker _messageBroker;

        public NotificationViewModel(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker;
        }

        public void SendNotification(string message)
        {
            _messageBroker.Publish(new Infrastructure.Events.NotificationEvent(message));
        }
    }
}
