using System;

// Minimal MessagePipe compatibility for compilation
// This is a basic implementation to resolve compilation errors
// For production, install the actual MessagePipe package

namespace MessagePipe
{
    /// <summary>
    /// Publisher interface for MessagePipe compatibility.
    /// </summary>
    public interface IPublisher<in T>
    {
        void Publish(T message);
    }

    /// <summary>
    /// Simple publisher implementation.
    /// </summary>
    public class SimplePublisher<T> : IPublisher<T>
    {
        public void Publish(T message)
        {
            // Basic implementation - in real usage, this would broadcast to subscribers
            // For now, just a no-op to resolve compilation
        }
    }

    /// <summary>
    /// MessagePipe services for dependency injection.
    /// </summary>
    public static class MessagePipeServices
    {
        public static IPublisher<T> GetPublisher<T>()
        {
            return new SimplePublisher<T>();
        }
    }
}
