using System;
using System.Collections.Concurrent;
using System.Threading;
using Cysharp.Threading.Tasks;
using Laboratory.Core;
using Laboratory.Core.State;
using Laboratory.Core.Events;
using Laboratory.Core.Infrastructure;
using Laboratory.Infrastructure.AsyncUtils;
using Unity.Entities;
using UnityEngine;
using R3;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Handles incoming network messages from NetworkClient and processes them asynchronously.
    /// Parses messages, updates ECS world state, and publishes events via UnifiedEventBus.
    /// </summary>
    public class NetworkMessageHandler : IDisposable
    {
        #region Fields

        /// <summary>Network client for receiving data.</summary>
        private readonly NetworkClient _networkClient;
        
        /// <summary>Event bus for publishing parsed events.</summary>
        private readonly IEventBus _eventBus;
        
        /// <summary>ECS entity manager for world updates.</summary>
        private readonly EntityManager _entityManager;

        /// <summary>Thread-safe queue for incoming raw message data.</summary>
        private readonly ConcurrentQueue<byte[]> _incomingMessages = new ConcurrentQueue<byte[]>();
        
        /// <summary>Cancellation token source for stopping message processing.</summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>Game state service reference for state synchronization.</summary>
        private IGameStateService _gameStateService;

        /// <summary>Disposable subscriptions for cleanup.</summary>
        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        /// <summary>Debug logging flag.</summary>
        private readonly bool _enableDebugLogs;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the network message handler with required dependencies.
        /// </summary>
        /// <param name="networkClient">Network client for receiving data.</param>
        /// <param name="eventBus">Event bus for event publishing.</param>
        /// <param name="entityManager">ECS entity manager for world updates.</param>
        /// <param name="enableDebugLogs">Whether to enable debug logging.</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null.</exception>
        public NetworkMessageHandler(NetworkClient networkClient, IEventBus eventBus, EntityManager entityManager, bool enableDebugLogs = false)
        {
            _networkClient = networkClient ?? throw new ArgumentNullException(nameof(networkClient));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _entityManager = entityManager;
            _enableDebugLogs = enableDebugLogs;

            // Subscribe to network client data events
            _networkClient.DataReceived += OnDataReceived;

            // Get game state service
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _gameStateService = serviceContainer.ResolveService<IGameStateService>();
            }

            // Start message processing loop
            StartMessageProcessing();

            if (_enableDebugLogs)
                Debug.Log("[NetworkMessageHandler] Initialized and ready for message processing");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the message processing loop.
        /// </summary>
        public void StartMessageProcessing()
        {
            if (_cts.IsCancellationRequested)
            {
                _cts = new CancellationTokenSource();
            }

            _ = ProcessMessagesAsync(_cts.Token);

            if (_enableDebugLogs)
                Debug.Log("[NetworkMessageHandler] Message processing started");
        }

        /// <summary>
        /// Stops the message processing loop.
        /// </summary>
        public void StopMessageProcessing()
        {
            _cts?.Cancel();

            if (_enableDebugLogs)
                Debug.Log("[NetworkMessageHandler] Message processing stopped");
        }

        /// <summary>
        /// Manually processes a single message (for testing).
        /// </summary>
        /// <param name="messageData">Raw message data to process.</param>
        public void ProcessMessage(byte[] messageData)
        {
            if (messageData == null || messageData.Length == 0) return;

            try
            {
                var message = ParseMessage(messageData);
                if (message != null)
                {
                    HandleParsedMessage(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error processing manual message: {ex.Message}");
                _eventBus?.Publish(new NetworkMessageProcessingErrorEvent(ex, messageData));
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles incoming data from the network client.
        /// </summary>
        private void OnDataReceived(byte[] data)
        {
            if (data == null || data.Length == 0) return;

            _incomingMessages.Enqueue(data);

            if (_enableDebugLogs)
                Debug.Log($"[NetworkMessageHandler] Queued message: {data.Length} bytes");
        }

        /// <summary>
        /// Asynchronously processes incoming messages.
        /// </summary>
        private async UniTask ProcessMessagesAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    while (_incomingMessages.TryDequeue(out var messageData))
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        await ProcessSingleMessageAsync(messageData, cancellationToken);
                    }

                    // Small delay to prevent tight loop
                    await UniTask.Delay(1, cancellationToken: cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkMessageHandler] Error in message processing loop: {ex.Message}");
                    _eventBus?.Publish(new NetworkMessageProcessingErrorEvent(ex, null));
                }
            }

            if (_enableDebugLogs)
                Debug.Log("[NetworkMessageHandler] Message processing loop ended");
        }

        /// <summary>
        /// Processes a single message asynchronously.
        /// </summary>
        private async UniTask ProcessSingleMessageAsync(byte[] messageData, CancellationToken cancellationToken)
        {
            try
            {
                // Parse the message
                var message = ParseMessage(messageData);
                if (message == null) return;

                // Handle the parsed message
                HandleParsedMessage(message);

                // Publish message processed event
                _eventBus?.Publish(new NetworkMessageProcessedEvent(message));

                if (_enableDebugLogs)
                    Debug.Log($"[NetworkMessageHandler] Processed message: {message.MessageType}");

                // Small delay for heavy processing
                if (message.RequiresHeavyProcessing)
                {
                    await UniTask.Yield(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error processing message: {ex.Message}");
                _eventBus?.Publish(new NetworkMessageProcessingErrorEvent(ex, messageData));
            }
        }

        /// <summary>
        /// Parses raw message data into a structured message object.
        /// </summary>
        private NetworkMessage ParseMessage(byte[] data)
        {
            if (data == null || data.Length < 4) return null;

            try
            {
                // Simple message format: [MessageType:4][Data:remaining]
                var messageType = (NetworkMessageType)BitConverter.ToInt32(data, 0);
                var messageData = new byte[data.Length - 4];
                Array.Copy(data, 4, messageData, 0, messageData.Length);

                return new NetworkMessage
                {
                    MessageType = messageType,
                    Data = messageData,
                    Timestamp = DateTime.Now,
                    RequiresHeavyProcessing = IsHeavyProcessingMessage(messageType)
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error parsing message: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Handles a parsed network message.
        /// </summary>
        private void HandleParsedMessage(NetworkMessage message)
        {
            switch (message.MessageType)
            {
                case NetworkMessageType.PlayerJoined:
                    HandlePlayerJoined(message);
                    break;
                case NetworkMessageType.PlayerLeft:
                    HandlePlayerLeft(message);
                    break;
                case NetworkMessageType.GameStateUpdate:
                    HandleGameStateUpdate(message);
                    break;
                case NetworkMessageType.EntityUpdate:
                    HandleEntityUpdate(message);
                    break;
                case NetworkMessageType.ChatMessage:
                    HandleChatMessage(message);
                    break;
                default:
                    if (_enableDebugLogs)
                        Debug.LogWarning($"[NetworkMessageHandler] Unhandled message type: {message.MessageType}");
                    break;
            }
        }

        /// <summary>
        /// Determines if a message type requires heavy processing.
        /// </summary>
        private bool IsHeavyProcessingMessage(NetworkMessageType messageType)
        {
            return messageType == NetworkMessageType.EntityUpdate || 
                   messageType == NetworkMessageType.GameStateUpdate;
        }

        #region Message Handlers

        private void HandlePlayerJoined(NetworkMessage message)
        {
            try
            {
                // Parse player data from message
                var playerData = ParsePlayerData(message.Data);
                
                if (playerData != null)
                {
                    // Create player entity in ECS world
                    var playerEntity = _entityManager.CreateEntity();
                    
                    // Add player components (example - adjust based on your actual component types)
                    if (_entityManager.HasComponent<Unity.Transforms.LocalToWorld>(playerEntity))
                    {
                        _entityManager.SetComponentData(playerEntity, new Unity.Transforms.LocalToWorld
                        {
                            Value = Unity.Mathematics.float4x4.TRS(
                                playerData.Position,
                                playerData.Rotation,
                                new Unity.Mathematics.float3(1f, 1f, 1f)
                            )
                        });
                    }
                    
                    if (_enableDebugLogs)
                        Debug.Log($"[NetworkMessageHandler] Player {playerData.PlayerId} joined at position {playerData.Position}");
                }
                
                _eventBus?.Publish(new PlayerJoinedEvent(message.Data));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error handling player joined: {ex.Message}");
            }
        }

        private void HandlePlayerLeft(NetworkMessage message)
        {
            try
            {
                // Parse player ID from message
                var playerId = ParsePlayerId(message.Data);
                
                if (playerId.HasValue)
                {
                    // Find and destroy player entity
                    // Note: This is a simplified example - you'd typically have a proper player lookup system
                    var query = _entityManager.CreateEntityQuery(typeof(Unity.Transforms.LocalToWorld));
                    var entities = query.ToEntityArray(Unity.Collections.Allocator.TempJob);
                    
                    foreach (var entity in entities)
                    {
                        // You would check for a PlayerID component here
                        // For now, this is just an example structure
                        // if (_entityManager.GetComponentData<PlayerComponent>(entity).PlayerId == playerId)
                        // {
                        //     _entityManager.DestroyEntity(entity);
                        //     break;
                        // }
                    }
                    
                    entities.Dispose();
                    
                    if (_enableDebugLogs)
                        Debug.Log($"[NetworkMessageHandler] Player {playerId} left the game");
                }
                
                _eventBus?.Publish(new PlayerLeftEvent(message.Data));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error handling player left: {ex.Message}");
            }
        }

        private void HandleGameStateUpdate(NetworkMessage message)
        {
            try
            {
                // Parse game state data
                var gameStateData = ParseGameStateData(message.Data);
                
                if (gameStateData != null && _gameStateService != null)
                {
                    // Update game state service
                    if (gameStateData.GameMode.HasValue)
                    {
                        // Example: _gameStateService.SetGameMode(gameStateData.GameMode.Value);
                    }
                    
                    if (gameStateData.TimeRemaining.HasValue)
                    {
                        // Example: _gameStateService.SetTimeRemaining(gameStateData.TimeRemaining.Value);
                    }
                    
                    if (gameStateData.Score.HasValue)
                    {
                        // Example: _gameStateService.UpdateScore(gameStateData.Score.Value);
                    }
                    
                    if (_enableDebugLogs)
                        Debug.Log($"[NetworkMessageHandler] Game state updated - Mode: {gameStateData.GameMode}, Time: {gameStateData.TimeRemaining}");
                }
                
                _eventBus?.Publish(new NetworkGameStateUpdateEvent(message.Data));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error handling game state update: {ex.Message}");
            }
        }

        private void HandleEntityUpdate(NetworkMessage message)
        {
            try
            {
                // Parse entity update data
                var entityUpdateData = ParseEntityUpdateData(message.Data);
                
                if (entityUpdateData != null)
                {
                    // Find entity by network ID
                    var query = _entityManager.CreateEntityQuery(typeof(Unity.Transforms.LocalToWorld));
                    var entities = query.ToEntityArray(Unity.Collections.Allocator.TempJob);
                    
                    foreach (var entity in entities)
                    {
                        // You would check for a NetworkID component here
                        // For now, this is just an example structure
                        // if (_entityManager.HasComponent<NetworkIDComponent>(entity))
                        // {
                        //     var networkId = _entityManager.GetComponentData<NetworkIDComponent>(entity).ID;
                        //     if (networkId == entityUpdateData.EntityId)
                        //     {
                        //         // Update entity transform
                        //         if (_entityManager.HasComponent<Unity.Transforms.LocalToWorld>(entity))
                        //         {
                        //             _entityManager.SetComponentData(entity, new Unity.Transforms.LocalToWorld
                        //             {
                        //                 Value = Unity.Mathematics.float4x4.TRS(
                        //                     entityUpdateData.Position,
                        //                     entityUpdateData.Rotation,
                        //                     new Unity.Mathematics.float3(1f, 1f, 1f)
                        //                 )
                        //             });
                        //         }
                        //         break;
                        //     }
                        // }
                    }
                    
                    entities.Dispose();
                    
                    if (_enableDebugLogs)
                        Debug.Log($"[NetworkMessageHandler] Entity {entityUpdateData.EntityId} updated at position {entityUpdateData.Position}");
                }
                
                _eventBus?.Publish(new NetworkEntityUpdateEvent(message.Data));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error handling entity update: {ex.Message}");
            }
        }

        private void HandleChatMessage(NetworkMessage message)
        {
            try
            {
                // Parse chat message data
                var chatData = ParseChatMessageData(message.Data);
                
                if (chatData != null)
                {
                    // Log chat message (in a real implementation, you might want to store or display this)
                    if (_enableDebugLogs)
                        Debug.Log($"[NetworkMessageHandler] Chat from {chatData.PlayerName}: {chatData.Message}");
                    
                    // You could also update a chat UI system here
                    // Example: _chatUIService?.AddChatMessage(chatData.PlayerName, chatData.Message);
                }
                
                _eventBus?.Publish(new NetworkChatMessageEvent(message.Data));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error handling chat message: {ex.Message}");
            }
        }

        #endregion

        #region Message Parsing Methods

        /// <summary>
        /// Parses player data from raw message bytes.
        /// </summary>
        private PlayerData ParsePlayerData(byte[] data)
        {
            if (data == null || data.Length < 36) return null; // Minimum size for player data
            
            try
            {
                var playerId = BitConverter.ToInt32(data, 0);
                var posX = BitConverter.ToSingle(data, 4);
                var posY = BitConverter.ToSingle(data, 8);
                var posZ = BitConverter.ToSingle(data, 12);
                var rotX = BitConverter.ToSingle(data, 16);
                var rotY = BitConverter.ToSingle(data, 20);
                var rotZ = BitConverter.ToSingle(data, 24);
                var rotW = BitConverter.ToSingle(data, 28);
                
                // Parse player name if present
                string playerName = "Unknown";
                if (data.Length > 32)
                {
                    var nameLength = BitConverter.ToInt32(data, 32);
                    if (nameLength > 0 && data.Length >= 36 + nameLength)
                    {
                        playerName = System.Text.Encoding.UTF8.GetString(data, 36, nameLength);
                    }
                }
                
                return new PlayerData
                {
                    PlayerId = playerId,
                    Position = new Unity.Mathematics.float3(posX, posY, posZ),
                    Rotation = new Unity.Mathematics.quaternion(rotX, rotY, rotZ, rotW),
                    PlayerName = playerName
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error parsing player data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses player ID from raw message bytes.
        /// </summary>
        private int? ParsePlayerId(byte[] data)
        {
            if (data == null || data.Length < 4) return null;
            
            try
            {
                return BitConverter.ToInt32(data, 0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error parsing player ID: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses game state data from raw message bytes.
        /// </summary>
        private GameStateData ParseGameStateData(byte[] data)
        {
            if (data == null || data.Length < 12) return null;
            
            try
            {
                var gameMode = BitConverter.ToInt32(data, 0);
                var timeRemaining = BitConverter.ToSingle(data, 4);
                var score = BitConverter.ToInt32(data, 8);
                
                return new GameStateData
                {
                    GameMode = gameMode,
                    TimeRemaining = timeRemaining,
                    Score = score
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error parsing game state data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses entity update data from raw message bytes.
        /// </summary>
        private EntityUpdateData ParseEntityUpdateData(byte[] data)
        {
            if (data == null || data.Length < 32) return null;
            
            try
            {
                var entityId = BitConverter.ToInt32(data, 0);
                var posX = BitConverter.ToSingle(data, 4);
                var posY = BitConverter.ToSingle(data, 8);
                var posZ = BitConverter.ToSingle(data, 12);
                var rotX = BitConverter.ToSingle(data, 16);
                var rotY = BitConverter.ToSingle(data, 20);
                var rotZ = BitConverter.ToSingle(data, 24);
                var rotW = BitConverter.ToSingle(data, 28);
                
                return new EntityUpdateData
                {
                    EntityId = entityId,
                    Position = new Unity.Mathematics.float3(posX, posY, posZ),
                    Rotation = new Unity.Mathematics.quaternion(rotX, rotY, rotZ, rotW)
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error parsing entity update data: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Parses chat message data from raw message bytes.
        /// </summary>
        private ChatMessageData ParseChatMessageData(byte[] data)
        {
            if (data == null || data.Length < 8) return null;
            
            try
            {
                var playerId = BitConverter.ToInt32(data, 0);
                var nameLength = BitConverter.ToInt32(data, 4);
                
                if (data.Length < 8 + nameLength + 4) return null;
                
                var playerName = System.Text.Encoding.UTF8.GetString(data, 8, nameLength);
                var messageLength = BitConverter.ToInt32(data, 8 + nameLength);
                
                if (data.Length < 8 + nameLength + 4 + messageLength) return null;
                
                var message = System.Text.Encoding.UTF8.GetString(data, 8 + nameLength + 4, messageLength);
                
                return new ChatMessageData
                {
                    PlayerId = playerId,
                    PlayerName = playerName,
                    Message = message
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkMessageHandler] Error parsing chat message data: {ex.Message}");
                return null;
            }
        }

        #endregion

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            StopMessageProcessing();
            
            if (_networkClient != null)
            {
                _networkClient.DataReceived -= OnDataReceived;
            }
            
            _disposables?.Dispose();
            _cts?.Dispose();

            if (_enableDebugLogs)
                Debug.Log("[NetworkMessageHandler] Disposed");
        }

        #endregion
    }

    #region Network Message Types and Classes

    /// <summary>
    /// Types of network messages that can be received.
    /// </summary>
    public enum NetworkMessageType
    {
        Unknown = 0,
        PlayerJoined = 1,
        PlayerLeft = 2,
        GameStateUpdate = 3,
        EntityUpdate = 4,
        ChatMessage = 5
    }

    /// <summary>
    /// Represents a parsed network message.
    /// </summary>
    public class NetworkMessage
    {
        public NetworkMessageType MessageType { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public DateTime Timestamp { get; set; }
        public bool RequiresHeavyProcessing { get; set; }
    }

    /// <summary>
    /// Event published when a message is successfully processed.
    /// </summary>
    public class NetworkMessageProcessedEvent
    {
        public NetworkMessage Message { get; }
        public DateTime ProcessedAt { get; }

        public NetworkMessageProcessedEvent(NetworkMessage message)
        {
            Message = message;
            ProcessedAt = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when an error occurs during message processing.
    /// </summary>
    public class NetworkMessageProcessingErrorEvent
    {
        public Exception Exception { get; }
        public byte[] MessageData { get; }
        public DateTime ErrorTime { get; }

        public NetworkMessageProcessingErrorEvent(Exception exception, byte[] messageData)
        {
            Exception = exception;
            MessageData = messageData;
            ErrorTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when a player joins.
    /// </summary>
    public class PlayerJoinedEvent
    {
        public byte[] PlayerData { get; }
        public DateTime JoinTime { get; }

        public PlayerJoinedEvent(byte[] playerData)
        {
            PlayerData = playerData;
            JoinTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when a player leaves.
    /// </summary>
    public class PlayerLeftEvent
    {
        public byte[] PlayerData { get; }
        public DateTime LeaveTime { get; }

        public PlayerLeftEvent(byte[] playerData)
        {
            PlayerData = playerData;
            LeaveTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when game state is updated from network.
    /// </summary>
    public class NetworkGameStateUpdateEvent
    {
        public byte[] StateData { get; }
        public DateTime UpdateTime { get; }

        public NetworkGameStateUpdateEvent(byte[] stateData)
        {
            StateData = stateData;
            UpdateTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when entity data is updated from network.
    /// </summary>
    public class NetworkEntityUpdateEvent
    {
        public byte[] EntityData { get; }
        public DateTime UpdateTime { get; }

        public NetworkEntityUpdateEvent(byte[] entityData)
        {
            EntityData = entityData;
            UpdateTime = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when a chat message is received.
    /// </summary>
    public class NetworkChatMessageEvent
    {
        public byte[] MessageData { get; }
        public DateTime ReceivedTime { get; }

        public NetworkChatMessageEvent(byte[] messageData)
        {
            MessageData = messageData;
            ReceivedTime = DateTime.Now;
        }
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Represents player data parsed from network messages.
    /// </summary>
    public class PlayerData
    {
        public int PlayerId { get; set; }
        public Unity.Mathematics.float3 Position { get; set; }
        public Unity.Mathematics.quaternion Rotation { get; set; }
        public string PlayerName { get; set; } = "Unknown";
    }

    /// <summary>
    /// Represents game state data parsed from network messages.
    /// </summary>
    public class GameStateData
    {
        public int? GameMode { get; set; }
        public float? TimeRemaining { get; set; }
        public int? Score { get; set; }
    }

    /// <summary>
    /// Represents entity update data parsed from network messages.
    /// </summary>
    public class EntityUpdateData
    {
        public int EntityId { get; set; }
        public Unity.Mathematics.float3 Position { get; set; }
        public Unity.Mathematics.quaternion Rotation { get; set; }
    }

    /// <summary>
    /// Represents chat message data parsed from network messages.
    /// </summary>
    public class ChatMessageData
    {
        public int PlayerId { get; set; }
        public string PlayerName { get; set; } = "Unknown";
        public string Message { get; set; } = "";
    }

    #endregion
}
