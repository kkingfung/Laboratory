using System;

#nullable enable

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Event fired when a notification should be displayed to the user.
    /// Used by the notification system to display messages, alerts, and status updates.
    /// </summary>
    public class NotificationEvent
    {
        /// <summary>The message to display in the notification.</summary>
        public string Message { get; }
        
        /// <summary>The title of the notification (optional).</summary>
        public string? Title { get; }
        
        /// <summary>The type/severity of the notification.</summary>
        public NotificationType Type { get; }
        
        /// <summary>How long to display the notification (in seconds). 0 = permanent.</summary>
        public float Duration { get; }
        
        /// <summary>Whether the notification can be dismissed by the user.</summary>
        public bool Dismissible { get; }
        
        /// <summary>Priority level for notification ordering.</summary>
        public NotificationPriority Priority { get; }
        
        /// <summary>Timestamp when the notification was created.</summary>
        public DateTime CreatedAt { get; }
        
        /// <summary>Unique identifier for this notification.</summary>
        public string Id { get; }
        
        /// <summary>Optional action to perform when notification is clicked.</summary>
        public Action? OnClick { get; }
        
        public NotificationEvent(
            string message, 
            string? title = null,
            NotificationType type = NotificationType.Info,
            float duration = 5f,
            bool dismissible = true,
            NotificationPriority priority = NotificationPriority.Normal,
            Action? onClick = null)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Title = title;
            Type = type;
            Duration = Math.Max(0f, duration);
            Dismissible = dismissible;
            Priority = priority;
            CreatedAt = DateTime.UtcNow;
            Id = Guid.NewGuid().ToString();
            OnClick = onClick;
        }
    }
    
    /// <summary>
    /// Types of notifications that can be displayed.
    /// </summary>
    public enum NotificationType
    {
        /// <summary>General information notification.</summary>
        Info = 0,
        
        /// <summary>Success notification for completed actions.</summary>
        Success = 1,
        
        /// <summary>Warning notification for potential issues.</summary>
        Warning = 2,
        
        /// <summary>Error notification for failed actions.</summary>
        Error = 3,
        
        /// <summary>Debug notification for development purposes.</summary>
        Debug = 4
    }
    
    /// <summary>
    /// Priority levels for notification display ordering.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>Low priority - displayed after other notifications.</summary>
        Low = 0,
        
        /// <summary>Normal priority - standard display order.</summary>
        Normal = 1,
        
        /// <summary>High priority - displayed before normal notifications.</summary>
        High = 2,
        
        /// <summary>Critical priority - displayed immediately and prominently.</summary>
        Critical = 3
    }
}
