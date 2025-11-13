using System;

namespace Laboratory.Core.Events
{
    /// <summary>
    /// Base interface for all events in the event bus system
    /// </summary>
    public interface IEvent
    {
        /// <summary>
        /// Timestamp when the event was created
        /// </summary>
        DateTime Timestamp { get; }

        /// <summary>
        /// Unique identifier for this event instance
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// Priority level for event processing
        /// </summary>
        EventPriority Priority { get; }
    }

    /// <summary>
    /// Event priority levels for processing order
    /// </summary>
    public enum EventPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
}