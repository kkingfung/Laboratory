using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Infrastructure;
using MessagePipe;
using Unity.Entities;
using UnityEngine;
using UniRx;
// FIXME: tidyup after 8/29
namespace Infrastructure
{
    /// <summary>
    /// Handles incoming network messages from NetworkClient,
    /// parses them, and updates ECS world or publishes events.
    /// </summary>
    public class NetworkMessageHandler : IDisposable
    {
        #region Fields

        private readonly NetworkClient _networkClient;
        private readonly IMessageBroker _messageBroker;
        private readonly EntityManager _entityManager;

        private readonly ConcurrentQueue<byte[]> _incomingMessages = new ConcurrentQueue<byte[]>();
        private CancellationTokenSource _cts = new CancellationTokenSource();

        #endregion

        #region Constructor

        public NetworkMessageHandler(NetworkClient networkClient, IMessageBroker messageBroker, EntityManager entityManager)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));

            _networkClient.DataReceived += OnDataReceived;

            // Start processing incoming messages
            ProcessMessagesAsync(_cts.Token).Forget();
        }

        #endregion

        #region Private Methods

        private async UniTask HandleGameStateSync(byte[] data)
        {
            if (RPCSerializer.TryDeserializeGameState(data, out var state))
            {
                // Apply remote state without broadcasting again
                _gameStateManager.ApplyRemoteState(state);
                Debug.Log($"Received game state sync: {state}");
            }
            switch (messageType)
            {
                //... other cases
                case (byte)RPCSerializer.RPCType.GameStateSync:
                    await HandleGameStateSync(data);
                    break;
            }

            await UniTask.Yield();
        }

        private void OnDataReceived(byte[] data)
        {
            // Enqueue received raw data for processing on main thread
            _incomingMessages.Enqueue(data);
        }

        private async UniTaskVoid ProcessMessagesAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                if (_incomingMessages.TryDequeue(out var data))
                {
                    try
                    {
                        await HandleMessageAsync(data);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"NetworkMessageHandler: Error handling message - {ex}");
                    }
                }
                else
                {
                    await UniTask.Delay(10, cancellationToken: token); // avoid busy loop
                }
            }
        }

        private async UniTask HandleMessageAsync(byte[] data)
        {
            // Example: Deserialize message type from data
            // You should implement your own protocol here

            // For demo, assume first byte is message type
            byte messageType = data[0];

            switch (messageType)
            {
                case 1:
                    await HandlePlayerStateUpdate(data);
                    break;
                case 2:
                    await HandleChatMessage(data);
                    break;
                // Add more cases for your message types
                default:
                    Debug.LogWarning($"NetworkMessageHandler: Unknown message type {messageType}");
                    break;
            }
        }

        private async UniTask HandlePlayerStateUpdate(byte[] data)
        {
            // Deserialize player state update from bytes
            // TODO: Replace with your deserialization logic
            // Example: data layout [1][playerId][hp][posX][posY][posZ]

            // This is just a stub - simulate processing delay
            await UniTask.Yield();

            // Update ECS entities or publish event
            // You might use EntityManager or MessagingPipe here

            Debug.Log("NetworkMessageHandler: Processed PlayerStateUpdate message");
        }

        private async UniTask HandleChatMessage(byte[] data)
        {
            // Deserialize and process chat message

            await UniTask.Yield();

            Debug.Log("NetworkMessageHandler: Processed ChatMessage");
        }

        #endregion

        #region IDisposable

        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            _networkClient.DataReceived -= OnDataReceived;
            _cts.Dispose();
        }

        #endregion
    }
}
