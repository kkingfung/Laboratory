using System;

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Base implementation of the IEvent interface
    /// Provides common functionality for all events
    /// </summary>
    public abstract class BaseEvent : IEvent
    {
        /// <summary>
        /// Timestamp when the event was created
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Unique identifier for this event instance
        /// </summary>
        public Guid EventId { get; }

        /// <summary>
        /// Priority level for event processing
        /// </summary>
        public EventPriority Priority { get; protected set; }

        /// <summary>
        /// Creates a new base event with the current timestamp
        /// </summary>
        /// <param name="priority">Priority level for this event</param>
        protected BaseEvent(EventPriority priority = EventPriority.Normal)
        {
            Timestamp = DateTime.UtcNow;
            EventId = Guid.NewGuid();
            Priority = priority;
        }

        /// <summary>
        /// Returns a string representation of the event
        /// </summary>
        public override string ToString()
        {
            return $"{GetType().Name} [ID: {EventId:N}] [Priority: {Priority}] [Time: {Timestamp:HH:mm:ss.fff}]";
        }
    }
}