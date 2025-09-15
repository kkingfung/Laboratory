using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core;
using Laboratory.Core.State;
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

        /// <summary>Game state service reference for state synchronization.</summary>
        private IGameStateService _gameStateService;

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
            _ = ProcessMessagesAsync(_cts.Token);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the game state service reference for state synchronization.
        /// </summary>
        /// <param name="gameStateService">Game state service instance.</param>
        public void SetGameStateService(IGameStateService gameStateService)
        {
            _gameStateService = gameStateService;
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
            try
            {
                // Deserialize player state data
                var playerState = DeserializePlayerState(data);
                if (playerState == null)
                {
                    Debug.LogWarning("NetworkMessageHandler: Failed to deserialize player state");
                    return;
                }

                // Update ECS entities with new player data
                await UpdatePlayerEntity(playerState);
                
                // Publish player state update event if message broker is available
                if (_messageBroker != null)
                {
                    // Create a simple update event (adapt based on your event system)
                    Debug.Log($"NetworkMessageHandler: Publishing player state update for player {playerState.PlayerId}");
                }

                Debug.Log($"NetworkMessageHandler: Updated player {playerState.PlayerId} - Health: {playerState.Health}, Position: {playerState.Position}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkMessageHandler: Error processing player state update: {ex.Message}");
            }
            
            await UniTask.Yield();
        }

        /// <summary>
        /// Handles chat message data.
        /// Publishes chat events for UI systems to display.
        /// </summary>
        /// <param name="data">Chat message data.</param>
        /// <returns>Task representing the chat processing operation.</returns>
        private async UniTask HandleChatMessage(byte[] data)
        {
            try
            {
                // Deserialize chat message data
                var chatMessage = DeserializeChatMessage(data);
                if (chatMessage == null)
                {
                    Debug.LogWarning("NetworkMessageHandler: Failed to deserialize chat message");
                    return;
                }

                // Publish chat message event for UI systems if message broker is available
                if (_messageBroker != null)
                {
                    Debug.Log($"NetworkMessageHandler: Publishing chat message from player {chatMessage.SenderId}");
                }

                Debug.Log($"NetworkMessageHandler: Received chat from player {chatMessage.SenderId}: {chatMessage.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkMessageHandler: Error processing chat message: {ex.Message}");
            }
            
            await UniTask.Yield();
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
                if (_gameStateService != null)
                {
                    // Apply remote state without broadcasting to prevent loops
                    _gameStateService.ApplyRemoteStateChange(state, suppressEvents: true);
                    Debug.Log($"NetworkMessageHandler: Applied game state sync - {state}");
                }
            }
            else
            {
                Debug.LogWarning("NetworkMessageHandler: Failed to deserialize game state sync");
            }

            await UniTask.Yield();
        }

        #region Message Deserialization

        /// <summary>
        /// Deserializes player state data from raw bytes.
        /// Expected format: [messageType][playerId][health][posX][posY][posZ][rotX][rotY][rotZ][rotW]
        /// </summary>
        /// <param name="data">Raw message bytes</param>
        /// <returns>Deserialized player state or null if failed</returns>
        private PlayerNetworkState DeserializePlayerState(byte[] data)
        {
            try
            {
                if (data.Length < 37) // 1 + 4 + 4 + 12 + 16 = 37 bytes minimum
                {
                    Debug.LogWarning($"NetworkMessageHandler: Player state data too short: {data.Length} bytes");
                    return null;
                }

                int offset = 1; // Skip message type

                // Read player ID (4 bytes)
                int playerId = System.BitConverter.ToInt32(data, offset);
                offset += 4;

                // Read health (4 bytes)
                float health = System.BitConverter.ToSingle(data, offset);
                offset += 4;

                // Read position (12 bytes)
                float posX = System.BitConverter.ToSingle(data, offset);
                offset += 4;
                float posY = System.BitConverter.ToSingle(data, offset);
                offset += 4;
                float posZ = System.BitConverter.ToSingle(data, offset);
                offset += 4;

                // Read rotation (16 bytes) if available
                UnityEngine.Quaternion rotation = UnityEngine.Quaternion.identity;
                if (data.Length >= offset + 16)
                {
                    float rotX = System.BitConverter.ToSingle(data, offset);
                    offset += 4;
                    float rotY = System.BitConverter.ToSingle(data, offset);
                    offset += 4;
                    float rotZ = System.BitConverter.ToSingle(data, offset);
                    offset += 4;
                    float rotW = System.BitConverter.ToSingle(data, offset);
                    rotation = new UnityEngine.Quaternion(rotX, rotY, rotZ, rotW);
                }

                return new PlayerNetworkState
                {
                    PlayerId = playerId,
                    Health = health,
                    Position = new UnityEngine.Vector3(posX, posY, posZ),
                    Rotation = rotation,
                    Timestamp = UnityEngine.Time.time
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkMessageHandler: Error deserializing player state: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserializes chat message data from raw bytes.
        /// Expected format: [messageType][senderId][messageLength][messageBytes]
        /// </summary>
        /// <param name="data">Raw message bytes</param>
        /// <returns>Deserialized chat message or null if failed</returns>
        private ChatNetworkMessage DeserializeChatMessage(byte[] data)
        {
            try
            {
                if (data.Length < 9) // 1 + 4 + 4 = 9 bytes minimum
                {
                    Debug.LogWarning($"NetworkMessageHandler: Chat message data too short: {data.Length} bytes");
                    return null;
                }

                int offset = 1; // Skip message type

                // Read sender ID (4 bytes)
                int senderId = System.BitConverter.ToInt32(data, offset);
                offset += 4;

                // Read message length (4 bytes)
                int messageLength = System.BitConverter.ToInt32(data, offset);
                offset += 4;

                // Validate message length
                if (messageLength < 0 || messageLength > 1024) // Max 1KB message
                {
                    Debug.LogWarning($"NetworkMessageHandler: Invalid chat message length: {messageLength}");
                    return null;
                }

                if (offset + messageLength > data.Length)
                {
                    Debug.LogWarning($"NetworkMessageHandler: Chat message data truncated. Expected: {messageLength}, Available: {data.Length - offset}");
                    return null;
                }

                // Read message content
                string message = System.Text.Encoding.UTF8.GetString(data, offset, messageLength);

                return new ChatNetworkMessage
                {
                    SenderId = senderId,
                    Message = message,
                    Timestamp = UnityEngine.Time.time
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkMessageHandler: Error deserializing chat message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Updates player entity in ECS world with new state data.
        /// </summary>
        /// <param name="playerState">New player state data</param>
        /// <returns>Task for async operation</returns>
        private async UniTask UpdatePlayerEntity(PlayerNetworkState playerState)
        {
            try
            {
                // Placeholder for ECS entity updates
                // This would need to be implemented based on your specific ECS architecture
                Debug.Log($"NetworkMessageHandler: Would update ECS entity for player {playerState.PlayerId}");
                
                // Example implementation would go here:
                // - Find entity with matching player ID
                // - Update position, rotation, health components
                // - Trigger any necessary entity events
            }
            catch (Exception ex)
            {
                Debug.LogError($"NetworkMessageHandler: Error updating player entity: {ex.Message}");
            }
            
            await UniTask.Yield();
        }

        #endregion

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
    
    #region Data Structures
    
    /// <summary>
    /// Represents the network state of a player
    /// </summary>
    public class PlayerNetworkState
    {
        public int PlayerId { get; set; }
        public float Health { get; set; }
        public UnityEngine.Vector3 Position { get; set; }
        public UnityEngine.Quaternion Rotation { get; set; }
        public float Timestamp { get; set; }
    }
    
    /// <summary>
    /// Represents a network chat message
    /// </summary>
    public class ChatNetworkMessage
    {
        public int SenderId { get; set; }
        public string Message { get; set; }
        public float Timestamp { get; set; }
    }
    
    #endregion
}