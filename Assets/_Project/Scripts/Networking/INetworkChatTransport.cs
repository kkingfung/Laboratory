using System;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Interface for transporting chat messages across a network.
    /// Provides methods for sending and receiving chat messages in a decoupled manner.
    /// </summary>
    public interface INetworkChatTransport
    {
        /// <summary>
        /// Sends a chat message to all connected clients.
        /// </summary>
        /// <param name="sender">The name of the message sender.</param>
        /// <param name="message">The message content to send.</param>
        void SendChatMessage(string sender, string message);

        /// <summary>
        /// Attempts to retrieve the next available chat message from the queue.
        /// </summary>
        /// <param name="sender">Output parameter for the message sender's name.</param>
        /// <param name="message">Output parameter for the message content.</param>
        /// <returns>True if a message was successfully retrieved; otherwise, false.</returns>
        bool TryReceiveChatMessage(out string sender, out string message);
    }
}
