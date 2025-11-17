using System.Collections.Concurrent;
using Unity.Netcode;
using UnityEngine;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Netcode for GameObjects implementation of chat transport.
    /// Handles chat message distribution using Unity's Netcode RPC system.
    /// </summary>
    public class NetcodeChatTransport : NetworkBehaviour, INetworkChatTransport
    {
        #region Fields

        /// <summary>Thread-safe queue for incoming chat messages.</summary>
        private readonly ConcurrentQueue<(string sender, string message)> _incomingMessages = new();

        #endregion

        #region Public Methods

        /// <summary>
        /// Sends a chat message to all connected clients.
        /// Routes through server to ensure message delivery to all clients.
        /// </summary>
        /// <param name="sender">The name of the message sender.</param>
        /// <param name="message">The message content to send.</param>
        public void SendChatMessage(string sender, string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (IsServer)
            {
                // If this is the server, broadcast directly without RPC
                BroadcastChatMessageClientRpc(sender, message);
            }
            else
            {
                // Otherwise, send to server via ServerRpc
                SendChatMessageServerRpc(sender, message);
            }
        }

        /// <summary>
        /// Attempts to retrieve the next available chat message from the queue.
        /// </summary>
        /// <param name="sender">Output parameter for the message sender's name.</param>
        /// <param name="message">Output parameter for the message content.</param>
        /// <returns>True if a message was successfully retrieved; otherwise, false.</returns>
        public bool TryReceiveChatMessage(out string sender, out string message)
        {
            if (_incomingMessages.TryDequeue(out var result))
            {
                sender = result.sender;
                message = result.message;
                return true;
            }

            sender = null!;
            message = null!;
            return false;
        }

        #endregion

        #region Network RPC Methods

        /// <summary>
        /// Server RPC that receives chat messages from clients and broadcasts them.
        /// </summary>
        /// <param name="sender">The name of the message sender.</param>
        /// <param name="message">The message content.</param>
        /// <param name="rpcParams">Server RPC parameters.</param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SendChatMessageServerRpc(string sender, string message, ServerRpcParams rpcParams = default)
        {
            BroadcastChatMessageClientRpc(sender, message);
        }

        /// <summary>
        /// Client RPC that delivers chat messages to all clients.
        /// Enqueues message locally for processing by chat systems.
        /// </summary>
        /// <param name="sender">The name of the message sender.</param>
        /// <param name="message">The message content.</param>
        /// <param name="rpcParams">Client RPC parameters.</param>
        [ClientRpc]
        private void BroadcastChatMessageClientRpc(string sender, string message, ClientRpcParams rpcParams = default)
        {
            _incomingMessages.Enqueue((sender, message));
        }

        #endregion
    }
}
