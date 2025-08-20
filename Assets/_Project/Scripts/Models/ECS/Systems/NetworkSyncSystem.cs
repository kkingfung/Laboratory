using System;
using Unity.Entities;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Core;
using Laboratory.Core.DI;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Models.ECS.Components;

#nullable enable

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// System responsible for synchronizing player state and input data with the network.
    /// This system gathers local player entities and sends their state to the server while
    /// processing incoming network messages to update entity states across clients.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class NetworkSyncSystem : SystemBase
    {
        #region Constants

        /// <summary>
        /// Interval between network state synchronization updates (in seconds)
        /// </summary>
        private const float NetworkSendInterval = 1f / 20f; // 20Hz update rate
        
        /// <summary>
        /// Maximum number of entities to process per frame to maintain performance
        /// </summary>
        private const int MaxEntitiesPerFrame = 100;
        
        #endregion

        #region Fields

        /// <summary>
        /// Network client interface for sending and receiving network messages
        /// </summary>
        private NetworkClient? _networkClient;
        
        /// <summary>
        /// Flag indicating whether the system is properly initialized
        /// </summary>
        private bool _isInitialized = false;
        
        /// <summary>
        /// Counter for tracking network message send frequency
        /// </summary>
        private float _networkSendTimer = 0f;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the system is created. Initializes the network client dependency
        /// and sets up the system for network synchronization operations.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            InitializeNetworkClient();
        }

        /// <summary>
        /// Called every frame during simulation. Handles network state synchronization
        /// for player entities and processes incoming network messages.
        /// </summary>
        protected override void OnUpdate()
        {
            if (!_isInitialized || _networkClient == null)
            {
                return;
            }

            UpdateNetworkSynchronization();
        }

        /// <summary>
        /// Called when the system is destroyed. Cleans up network resources and references.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupResources();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the network client dependency from the service locator
        /// </summary>
        private void InitializeNetworkClient()
        {
            try
            {
                if (GlobalServiceProvider.TryResolve<NetworkClient>(out var client))
                {
                    _networkClient = client;
                    _isInitialized = true;
                    Debug.Log("NetworkSyncSystem initialized successfully with NetworkClient");
                }
                else
                {
                    Debug.LogWarning("NetworkClient not found in ServiceLocator, network synchronization will be disabled");
                    _isInitialized = false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize NetworkSyncSystem: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Updates network synchronization by sending player state and processing incoming messages
        /// </summary>
        private void UpdateNetworkSynchronization()
        {
            try
            {
                if (!IsNetworkReady())
                {
                    return;
                }

                UpdateNetworkTimer();
                
                if (ShouldSendNetworkUpdate())
                {
                    SendPlayerStateUpdates();
                }

                ProcessIncomingNetworkMessages();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during network synchronization update: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the network client is ready for synchronization operations
        /// </summary>
        /// <returns>True if network is connected and ready</returns>
        private bool IsNetworkReady()
        {
            return _networkClient != null && _networkClient.IsConnected;
        }

        /// <summary>
        /// Updates the network send timer for controlling update frequency
        /// </summary>
        private void UpdateNetworkTimer()
        {
            _networkSendTimer += (float)SystemAPI.Time.DeltaTime;
        }

        /// <summary>
        /// Determines if it's time to send a network state update based on the configured interval
        /// </summary>
        /// <returns>True if a network update should be sent</returns>
        private bool ShouldSendNetworkUpdate()
        {
            return _networkSendTimer >= NetworkSendInterval;
        }

        /// <summary>
        /// Gathers player state data and sends updates to the server
        /// </summary>
        private void SendPlayerStateUpdates()
        {
            try
            {
                _networkSendTimer = 0f; // Reset timer
                int entityCount = 0;

                // Process player entities with state and input components
                Entities
                    .WithAll<PlayerStateComponent, PlayerInputComponent>()
                    .ForEach((Entity entity, in PlayerStateComponent state, in PlayerInputComponent input) =>
                    {
                        if (entityCount >= MaxEntitiesPerFrame)
                        {
                            return; // Skip remaining entities this frame
                        }

                        ProcessPlayerEntityForNetworkSync(entity, state, input);
                        entityCount++;
                    })
                    .WithoutBurst()
                    .Run();

                if (entityCount >= MaxEntitiesPerFrame)
                {
                    Debug.LogWarning($"Processed maximum entities per frame ({MaxEntitiesPerFrame}), remaining entities will be processed next update");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending player state updates: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes a single player entity for network synchronization
        /// </summary>
        /// <param name="entity">The entity to process</param>
        /// <param name="state">The player state component</param>
        /// <param name="input">The player input component</param>
        private void ProcessPlayerEntityForNetworkSync(Entity entity, PlayerStateComponent state, PlayerInputComponent input)
        {
            try
            {
                // Validate entity data before processing
                if (!IsValidPlayerEntity(entity, state, input))
                {
                    return;
                }

                // Serialize player state and input data
                var networkMessage = SerializePlayerState(entity, state, input);
                if (networkMessage != null)
                {
                    // Send the serialized data to the server asynchronously
                    SendNetworkMessageAsync(networkMessage).Forget();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing player entity {entity} for network sync: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that a player entity has valid data for network synchronization
        /// </summary>
        /// <param name="entity">The entity to validate</param>
        /// <param name="state">The player state component</param>
        /// <param name="input">The player input component</param>
        /// <returns>True if the entity data is valid for synchronization</returns>
        private bool IsValidPlayerEntity(Entity entity, PlayerStateComponent state, PlayerInputComponent input)
        {
            if (entity == Entity.Null)
            {
                Debug.LogWarning("Null entity encountered during network sync");
                return false;
            }

            // Add additional validation as needed based on your component structure
            // Example: Check if position values are valid, input is within expected ranges, etc.
            
            return true;
        }

        /// <summary>
        /// Serializes player state and input data into a network message format
        /// </summary>
        /// <param name="entity">The entity to serialize</param>
        /// <param name="state">The player state component</param>
        /// <param name="input">The player input component</param>
        /// <returns>Serialized message data, or null if serialization fails</returns>
        private byte[]? SerializePlayerState(Entity entity, PlayerStateComponent state, PlayerInputComponent input)
        {
            try
            {
                // TODO: Implement actual serialization based on your network protocol
                // This is a placeholder implementation that should be replaced with your
                // specific serialization logic (JSON, MessagePack, custom binary format, etc.)
                
                var stateData = new NetworkPlayerStateMessage
                {
                    EntityId = entity.Index,
                    Position = state.Position,
                    Rotation = state.Rotation,
                    Velocity = state.Velocity,
                    Health = state.CurrentHP,
                    InputMovement = input.MoveDirection,
                    InputAction = (byte)input.CurrentActions
                };

                return SerializeMessage(stateData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error serializing player state for entity {entity}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Serializes a network message object into byte array format
        /// </summary>
        /// <param name="message">The message object to serialize</param>
        /// <returns>Serialized message data</returns>
        private byte[] SerializeMessage(NetworkPlayerStateMessage message)
        {
            // TODO: Replace with your preferred serialization method
            // Examples: JSON, MessagePack, Protocol Buffers, custom binary serialization
            
            // Placeholder implementation - replace with actual serialization
            var json = JsonUtility.ToJson(message);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        /// <summary>
        /// Sends a network message asynchronously to the server
        /// </summary>
        /// <param name="messageData">The serialized message data to send</param>
        /// <returns>A task representing the async send operation</returns>
        private async UniTaskVoid SendNetworkMessageAsync(byte[] messageData)
        {
            try
            {
                if (_networkClient != null && messageData.Length > 0)
                {
                    await _networkClient.SendAsync(messageData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error sending network message: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes incoming network messages and applies updates to local entities
        /// </summary>
        private void ProcessIncomingNetworkMessages()
        {
            try
            {
                // TODO: Implement incoming message processing
                // This should handle messages from other players/server and update local entity states
                
                // Example implementation structure:
                // 1. Poll network client for incoming messages
                // 2. Deserialize message data
                // 3. Find corresponding entities
                // 4. Apply state updates to entities
                // 5. Handle interpolation/prediction as needed
                
                ProcessIncomingMessages();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing incoming network messages: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes queued incoming network messages from other clients
        /// </summary>
        private void ProcessIncomingMessages()
        {
            // TODO: Implement based on your network client's message queue system
            // This is a placeholder for the actual implementation
            
            // Example structure:
            // while (_networkClient.TryReceiveMessage(out var messageData))
            // {
            //     var message = DeserializeMessage(messageData);
            //     ApplyNetworkStateUpdate(message);
            // }
        }

        /// <summary>
        /// Cleans up network resources and references when the system is destroyed
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _networkClient = null;
                _isInitialized = false;
                _networkSendTimer = 0f;
                Debug.Log("NetworkSyncSystem resources cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during NetworkSyncSystem cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the network sync system is properly initialized and ready for use
        /// </summary>
        /// <returns>True if the system is initialized and network is connected</returns>
        public bool IsInitialized()
        {
            return _isInitialized && _networkClient != null;
        }

        /// <summary>
        /// Gets the current network client instance for external access
        /// </summary>
        /// <returns>The current network client instance, or null if not initialized</returns>
        public NetworkClient? GetNetworkClient()
        {
            return _isInitialized ? _networkClient : null;
        }

        /// <summary>
        /// Forces an immediate network synchronization update for all player entities
        /// </summary>
        public void ForceNetworkSync()
        {
            if (_isInitialized && IsNetworkReady())
            {
                _networkSendTimer = NetworkSendInterval; // Trigger immediate update
                Debug.Log("Forced immediate network synchronization");
            }
        }

        /// <summary>
        /// Gets the current network update rate in updates per second
        /// </summary>
        /// <returns>Network update frequency in Hz</returns>
        public float GetNetworkUpdateRate()
        {
            return 1f / NetworkSendInterval;
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Data structure representing a networked player state message
        /// </summary>
        [Serializable]
        private struct NetworkPlayerStateMessage
        {
            public int EntityId;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Velocity;
            public float Health;
            public Vector2 InputMovement;
            public byte InputAction;
        }

        #endregion
    }
}
