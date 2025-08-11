using System.Collections.Concurrent;
using Unity.Netcode;
using UnityEngine;

public class NetcodeChatTransport : NetworkBehaviour, INetworkChatTransport
{
    private readonly ConcurrentQueue<(string sender, string message)> _incomingMessages = new();

    /// <summary>
    /// Called locally to send a chat message to other players.
    /// </summary>
    public void SendChatMessage(string sender, string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        // Call ServerRpc to send message to server
        SendChatMessageServerRpc(sender, message);
    }

    /// <summary>
    /// ServerRpc runs on server and broadcasts message to all clients.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendChatMessageServerRpc(string sender, string message, ServerRpcParams rpcParams = default)
    {
        BroadcastChatMessageClientRpc(sender, message);
    }

    /// <summary>
    /// ClientRpc runs on all clients to receive a chat message.
    /// Enqueues message locally for processing.
    /// </summary>
    [ClientRpc]
    private void BroadcastChatMessageClientRpc(string sender, string message, ClientRpcParams rpcParams = default)
    {
        _incomingMessages.Enqueue((sender, message));
    }

    /// <summary>
    /// Called by ECS system to dequeue received chat messages.
    /// </summary>
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
}
