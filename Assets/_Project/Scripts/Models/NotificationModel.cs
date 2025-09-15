using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Models
{
    /// <summary>
    /// Types of notifications
    /// </summary>
    public enum NotificationType
    {
        Info,
        Warning,
        Error,
        Success
    }

    /// <summary>
    /// Model for handling notification messages through the unified event bus.
    /// </summary>
    public class NotificationModel
    {
        private readonly IEventBus _eventBus;

        public NotificationModel(IEventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public NotificationModel()
        {
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance.TryResolve<IEventBus>(out _eventBus);
            }
        }

        public void SendNotification(string message)
        {
            if (_eventBus != null)
            {
                _eventBus.Publish(new NotificationEvent(message));
            }
        }

        public void SendNotification(string message, string title, NotificationType type = NotificationType.Info, float duration = 5f)
        {
            if (_eventBus != null)
            {
                _eventBus.Publish(new NotificationEvent(message, title, type, duration));
            }
        }
    }

    public class NotificationEvent
    {
        public string Message { get; }
        public string Title { get; }
        public NotificationType Type { get; }
        public float Duration { get; }

        public NotificationEvent(string message)
        {
            Message = message;
            Title = "Notification";
            Type = NotificationType.Info;
            Duration = 5f;
        }

        public NotificationEvent(string message, string title, NotificationType type, float duration)
        {
            Message = message;
            Title = title;
            Type = type;
            Duration = duration;
        }
    }
}
