using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core;
using Laboratory.Infrastructure.AsyncUtils;
using MessagePipe;
using Unity.Entities;
using UnityEngine;
using UniRx;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Handles incoming network messages from NetworkClient and processes them asynchronously.
    /// Parses messages, updates ECS world state, and publishes events via message broker.
    /// </summary>
    public class NetworkMessageHandler : IDisposable
    {
        #region Fields

        /// <summary>Network client for receiving data.</summary>
        private readonly NetworkClient _networkClient;
        
        /// <summary>Message broker for publishing parsed events.</summary>
        private readonly IMessageBroker _messageBroker;
        
        /// <summary>ECS entity manager for world updates.</summary>
        private readonly EntityManager _entityManager;

        /// <summary>Thread-safe queue for incoming raw message data.</summary>
        private readonly ConcurrentQueue<byte[]> _incomingMessages = new ConcurrentQueue<byte[]>();
        
        /// <summary>Cancellation token source for stopping message processing.</summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>Game state manager reference for state synchronization.</summary>
        private GameStateManager _gameStateManager;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the network message handler with required dependencies.
        /// </summary>
        /// <param name="networkClient">Network client for receiving data.</param>
        /// <param name="messageBroker">Message broker for event publishing.</param>
        /// <param name="entityManager">ECS entity manager for world updates.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public NetworkMessageHandler(NetworkClient networkClient, IMessageBroker messageBroker, EntityManager entityManager)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
            _entityManager = entityManager;

            // Subscribe to network client data events
            _networkClient.DataReceived += OnDataReceived;

            // Start processing incoming messages asynchronously
            ProcessMessagesAsync(_cts.Token).Forget();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the game state manager reference for state synchronization.
        /// </summary>
        /// <param name="gameStateManager">Game state manager instance.</param>
        public void SetGameStateManager(GameStateManager gameStateManager)
        {
            _gameStateManager = gameStateManager;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles raw data received from network client.
        /// Enqueues data for processing on main thread.
        /// </summary>
        /// <param name="data">Raw message data received from server.</param>
        private void OnDataReceived(byte[] data)
        {
            _incomingMessages.Enqueue(data);
        }

        /// <summary>
        /// Continuously processes incoming messages from the queue.
        /// Runs on background thread to avoid blocking main thread.
        /// </summary>
        /// <param name="token">Cancellation token for stopping the processing loop.</param>
        /// <returns>UniTaskVoid representing the processing loop.</returns>
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
                    // Small delay to prevent busy waiting
                    await UniTask.Delay(10, cancellationToken: token);
                }
            }
        }

        /// <summary>
        /// Parses and handles individual network messages based on message type.
        /// </summary>
        /// <param name="data">Raw message data to process.</param>
        /// <returns>Task representing the message handling operation.</returns>
        private async UniTask HandleMessageAsync(byte[] data)
        {
            if (data.Length == 0)
            {
                Debug.LogWarning("NetworkMessageHandler: Received empty message data");
                return;
            }

            // Extract message type from first byte
            byte messageType = data[0];

            switch (messageType)
            {
                case 1:
                    await HandlePlayerStateUpdate(data);
                    break;
                case 2:
                    await HandleChatMessage(data);
                    break;
                case (byte)RPCSerializer.RPCType.GameStateSync:
                    await HandleGameStateSync(data);
                    break;
                default:
                    Debug.LogWarning($"NetworkMessageHandler: Unknown message type {messageType}");
                    break;
            }
        }

        /// <summary>
        /// Handles player state update messages.
        /// Updates ECS entities with new player positions and status.
        /// </summary>
        /// <param name="data">Player state update message data.</param>
        /// <returns>Task representing the update operation.</returns>
        private async UniTask HandlePlayerStateUpdate(byte[] data)
        {
            // TODO: Implement player state deserialization
            // Expected format: [messageType][playerId][health][posX][posY][posZ]
            
            await UniTask.Yield();
            
            // Update ECS entities or publish event for other systems
            Debug.Log("NetworkMessageHandler: Processed PlayerStateUpdate message");
        }

        /// <summary>
        /// Handles chat message data.
        /// Publishes chat events for UI systems to display.
        /// </summary>
        /// <param name="data">Chat message data.</param>
        /// <returns>Task representing the chat processing operation.</returns>
        private async UniTask HandleChatMessage(byte[] data)
        {
            // TODO: Implement chat message deserialization
            // Expected format: [messageType][senderId][messageLength][messageBytes]
            
            await UniTask.Yield();
            
            Debug.Log("NetworkMessageHandler: Processed ChatMessage");
        }

        /// <summary>
        /// Handles game state synchronization messages.
        /// Applies remote game state changes without broadcasting.
        /// </summary>
        /// <param name="data">Game state sync message data.</param>
        /// <returns>Task representing the state sync operation.</returns>
        private async UniTask HandleGameStateSync(byte[] data)
        {
            if (RPCSerializer.TryDeserializeGameState(data, out var state))
            {
                if (_gameStateManager != null)
                {
                    // Apply remote state without broadcasting to prevent loops
                    _gameStateManager.ApplyRemoteState(state);
                    Debug.Log($"NetworkMessageHandler: Applied game state sync - {state}");
                }
            }
            else
            {
                Debug.LogWarning("NetworkMessageHandler: Failed to deserialize game state sync");
            }

            await UniTask.Yield();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>Tracks disposal state to prevent double disposal.</summary>
        private bool _disposed = false;

        /// <summary>
        /// Disposes resources and stops message processing.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _cts.Cancel();
            
            if (_networkClient != null)
            {
                _networkClient.DataReceived -= OnDataReceived;
            }
            
            _cts.Dispose();
        }

        #endregion
    }
}
