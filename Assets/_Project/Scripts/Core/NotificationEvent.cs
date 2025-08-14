namespace Laboratory.Core.Events
{
    /// <summary>
    /// Event structure for system-wide notifications that need to be displayed to the user.
    /// Used with notification UI systems to show temporary messages, alerts, and status updates.
    /// Immutable structure ensuring thread-safe event passing.
    /// </summary>
    public readonly struct NotificationEvent
    {
        #region Properties
        
        /// <summary>
        /// Gets the notification message content to be displayed to the user.
        /// </summary>
        public string Message { get; }
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initializes a new notification event with the specified message.
        /// </summary>
        /// <param name="message">The message content to be displayed in the notification</param>
        public NotificationEvent(string message)
        {
            Message = message;
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Returns a string representation of the notification event.
        /// </summary>
        /// <returns>The message content of the notification</returns>
        public override string ToString()
        {
            return Message ?? string.Empty;
        }
        
        /// <summary>
        /// Determines whether the specified object is equal to the current notification event.
        /// </summary>
        /// <param name="obj">The object to compare with the current notification event</param>
        /// <returns>True if the specified object is equal to the current notification event; otherwise, false</returns>
        public override bool Equals(object obj)
        {
            return obj is NotificationEvent other && Message == other.Message;
        }
        
        /// <summary>
        /// Returns the hash code for this notification event.
        /// </summary>
        /// <returns>A hash code for the current notification event</returns>
        public override int GetHashCode()
        {
            return Message?.GetHashCode() ?? 0;
        }
        
        #endregion
        
        #region Static Methods
        
        /// <summary>
        /// Creates a notification event with the specified message.
        /// </summary>
        /// <param name="message">The message content for the notification</param>
        /// <returns>A new NotificationEvent instance</returns>
        public static NotificationEvent Create(string message)
        {
            return new NotificationEvent(message);
        }
        
        /// <summary>
        /// Creates an empty notification event.
        /// </summary>
        /// <returns>A NotificationEvent with an empty message</returns>
        public static NotificationEvent Empty => new NotificationEvent(string.Empty);
        
        #endregion
        
        #region Operators
        
        /// <summary>
        /// Determines whether two notification events are equal.
        /// </summary>
        /// <param name="left">The first notification event to compare</param>
        /// <param name="right">The second notification event to compare</param>
        /// <returns>True if the events are equal; otherwise, false</returns>
        public static bool operator ==(NotificationEvent left, NotificationEvent right)
        {
            return left.Message == right.Message;
        }
        
        /// <summary>
        /// Determines whether two notification events are not equal.
        /// </summary>
        /// <param name="left">The first notification event to compare</param>
        /// <param name="right">The second notification event to compare</param>
        /// <returns>True if the events are not equal; otherwise, false</returns>
        public static bool operator !=(NotificationEvent left, NotificationEvent right)
        {
            return !(left == right);
        }
        
        #endregion
    }
}
