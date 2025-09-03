using System;

namespace MessagePipe
{
    public interface IPublisher<T>
    {
        void Publish(T message);
    }
    
    public interface ISubscriber<T>
    {
        void Subscribe(Action<T> handler);
    }
    
    public static class MessagePipeStubs
    {
        // Placeholder for MessagePipe functionality
    }
}