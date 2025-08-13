// FIXME: tidyup after 8/29
namespace Infrastructure.Events
{
    public readonly struct NotificationEvent
    {
        public string Message { get; }
        public NotificationEvent(string message)
        {
            Message = message;
        }
    }
}
