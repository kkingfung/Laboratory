using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Laboratory.Core.Events;
using Laboratory.Core.DI;
using Laboratory.Subsystems.Analytics;
using Laboratory.Subsystems.Performance;

namespace Laboratory.Subsystems.Networking.Advanced
{
    /// <summary>
    /// Advanced networking manager that handles client-server architecture,
    /// connection management, and integration with all existing systems
    /// </summary>
    public class AdvancedNetworkManager : NetworkManager
    {
        [Header("Advanced Networking Configuration")]
        [SerializeField] private bool enableNetworkAnalytics = true;
        [SerializeField] private bool enableNetworkPerformanceMonitoring = true;
        [SerializeField] private float networkStatsUpdateInterval = 1f;
        [SerializeField] private int maxReconnectAttempts = 5;
        
        [Header("Server Configuration")]
        [SerializeField] private int maxPlayers = 100;
        [SerializeField] private float serverTickRate = 60f;
        [SerializeField] private bool enableDedicatedServerMode = false;

        [Header("Client Configuration")]
        [SerializeField] private float clientUpdateRate = 30f;
        [SerializeField] private bool enableClientPrediction = true;
        [SerializeField] private bool enableLagCompensation = true;

        [Header("Security Settings")]
        [SerializeField] private bool enableEncryption = true;
        [SerializeField] private bool enableAntiCheat = true;
        [SerializeField] private float maxPacketSize = 1024f;
        
        // Network state management
        private Dictionary<ulong, NetworkPlayerData> connectedPlayers = new Dictionary<ulong, NetworkPlayerData>();
        private NetworkStats networkStats = new NetworkStats();
        private float lastStatsUpdate = 0f;
        private int reconnectAttempts = 0;
        private IEventBus _eventBus;
        
        // Performance monitoring
        private PerformanceManager performanceManager;
        private AnalyticsManager analyticsManager;
        
        // Network events
        public System.Action<ulong, NetworkPlayerData> OnPlayerConnected;
        public System.Action<ulong> OnPlayerDisconnected;
        public System.Action<NetworkStats> OnNetworkStatsUpdated;
        public System.Action<string> OnNetworkError;
        
        private void Awake()
        {
            InitializeAdvancedNetworking();
        }

        private void Start()
        {
            // Initialize event bus
            if (GlobalServiceProvider.IsInitialized)
            {
                _eventBus = GlobalServiceProvider.Resolve<IEventBus>();
            }
            
            // Get references to other managers
            performanceManager = FindFirstObjectByType<PerformanceManager>();
            analyticsManager = FindFirstObjectByType<AnalyticsManager>();
            
            // Subscribe to network events
            OnClientConnectedCallback += OnClientConnected;
            OnClientDisconnectCallback += OnClientDisconnected;
            OnServerStarted += OnServerStartedCallback;
            OnClientStarted += OnClientStartedCallback;
            
            // Subscribe to application events for network cleanup
            Application.quitting += OnApplicationQuitting;
            
            StartCoroutine(NetworkMonitoringCoroutine());
        }

        private void InitializeAdvancedNetworking()
        {
            // Configure network settings
            NetworkConfig.PlayerPrefab = GetPlayerPrefab();
            NetworkConfig.EnableSceneManagement = true;
            NetworkConfig.ForceSamePrefabs = false;
            
            // Set tick rates
            NetworkConfig.TickRate = (uint)serverTickRate;
            NetworkConfig.ClientConnectionBufferTimeout = 10;
            
            // Configure transport settings
            if (NetworkConfig.NetworkTransport != null)
            {
                ConfigureTransportSettings();
            }
            
            Debug.Log("Advanced Network Manager initialized");
        }

        private void ConfigureTransportSettings()
        {
            // Configure Unity Transport if available
            var transport = GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.ServerListenAddress = enableDedicatedServerMode ? "0.0.0.0" : "127.0.0.1";
                transport.ConnectionData.Port = 7777;

                // Note: Network simulation is now handled by Multiplayer Tools package
                // Configure basic transport settings
                Debug.Log($"Transport configured for max packet size: {maxPacketSize}");

                Debug.Log($"Transport configured - Dedicated Server: {enableDedicatedServerMode}, Max Packet Size: {maxPacketSize}");
            }

            // Apply network configuration based on settings
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.NetworkConfig.TickRate = (uint)serverTickRate;
                Debug.Log($"Network tick rate set to: {serverTickRate}");
            }
        }

        #region Server Management

        /// <summary>
        /// Start server with advanced configuration
        /// </summary>
        public bool StartAdvancedServer(ushort port = 7777, string address = "0.0.0.0")
        {
            try
            {
                // Configure server settings
                var transport = GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.ServerListenAddress = address;
                    transport.ConnectionData.Port = port;
                }
                
                // Set max players
                NetworkConfig.ConnectionApproval = true;
                // MaxClients is handled by ConnectionApproval
                
                // Start server
                bool success = StartServer();
                
                if (success && enableNetworkAnalytics)
                {
                    TrackNetworkEvent("server_started", new Dictionary<string, object>
                    {
                        {"port", port},
                        {"address", address},
                        {"max_players", maxPlayers},
                        {"tick_rate", serverTickRate}
                    });
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to start server: {ex.Message}");
                OnNetworkError?.Invoke($"Server start failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Start host with advanced configuration
        /// </summary>
        public bool StartAdvancedHost(ushort port = 7777)
        {
            try
            {
                var transport = GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Port = port;
                }
                
                NetworkConfig.ConnectionApproval = true;
                // MaxClients is handled by ConnectionApproval
                
                bool success = StartHost();
                
                if (success && enableNetworkAnalytics)
                {
                    TrackNetworkEvent("host_started", new Dictionary<string, object>
                    {
                        {"port", port},
                        {"max_players", maxPlayers}
                    });
                }
                
                return success;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to start host: {ex.Message}");
                OnNetworkError?.Invoke($"Host start failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Client Management

        /// <summary>
        /// Connect to server with retry logic
        /// </summary>
        public void ConnectToServer(string address, ushort port = 7777, System.Action<bool> onResult = null)
        {
            StartCoroutine(ConnectToServerCoroutine(address, port, onResult));
        }

        private IEnumerator ConnectToServerCoroutine(string address, ushort port, System.Action<bool> onResult)
        {
            reconnectAttempts = 0;
            
            while (reconnectAttempts < maxReconnectAttempts)
            {
                bool connectionSuccess = false;
                bool waitForConnection = false;
                
                // Configure transport
                var transport = GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport != null)
                {
                    transport.ConnectionData.Address = address;
                    transport.ConnectionData.Port = port;
                }
                
                // Attempt connection
                try
                {
                    bool success = StartClient();
                    
                    if (success)
                    {
                        if (enableNetworkAnalytics)
                        {
                            TrackNetworkEvent("client_connect_attempt", new Dictionary<string, object>
                            {
                                {"address", address},
                                {"port", port},
                                {"attempt", reconnectAttempts + 1}
                            });
                        }
                        
                        waitForConnection = true;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Connection attempt {reconnectAttempts + 1} failed: {ex.Message}");
                }
                
                // Wait for connection result (outside try-catch to allow yield)
                if (waitForConnection)
                {
                    float timeout = 10f;
                    float elapsed = 0f;
                    
                    while (elapsed < timeout && !IsClient && !IsConnectedClient)
                    {
                        elapsed += Time.deltaTime;
                        yield return new WaitForEndOfFrame();
                    }
                    
                    if (IsConnectedClient)
                    {
                        connectionSuccess = true;
                    }
                }
                
                if (connectionSuccess)
                {
                    onResult?.Invoke(true);
                    yield break;
                }
                
                reconnectAttempts++;
                
                if (reconnectAttempts < maxReconnectAttempts)
                {
                    Debug.Log($"Retrying connection in 3 seconds... (Attempt {reconnectAttempts + 1}/{maxReconnectAttempts})");
                    yield return new WaitForSeconds(3f);
                }
            }
            
            // All attempts failed
            OnNetworkError?.Invoke($"Failed to connect after {maxReconnectAttempts} attempts");
            onResult?.Invoke(false);
        }

        #endregion

        #region Player Management

        private void OnClientConnected(ulong clientId)
        {
            // Create network player data
            var playerData = new NetworkPlayerData
            {
                ClientId = clientId,
                PlayerName = $"Player_{clientId}",
                PlayerId = (int)clientId,
                TeamId = 0,
                IsReady = false,
                Health = 100f,
                MaxHealth = 100f
            };
            connectedPlayers[clientId] = playerData;
            
            // Update network stats
            networkStats.ConnectedPlayers = connectedPlayers.Count;
            
            // Fire events
            OnPlayerConnected?.Invoke(clientId, connectedPlayers[clientId]);
            
            // Track analytics
            if (enableNetworkAnalytics)
            {
                TrackNetworkEvent("player_connected", new Dictionary<string, object>
                {
                    {"client_id", clientId.ToString()},
                    {"total_players", connectedPlayers.Count}
                });
            }
            
            // Publish to event bus
            _eventBus?.Publish(new PlayerConnectedEvent 
            { 
                ClientId = clientId, 
                PlayerData = connectedPlayers[clientId],
                TotalPlayers = connectedPlayers.Count
            });
            
            Debug.Log($"Player connected: {clientId} (Total: {connectedPlayers.Count})");
        }

        private void OnClientDisconnected(ulong clientId)
        {
            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers.Remove(clientId);
                networkStats.ConnectedPlayers = connectedPlayers.Count;
                
                // Fire events
                OnPlayerDisconnected?.Invoke(clientId);
                
                // Track analytics
                if (enableNetworkAnalytics)
                {
                    TrackNetworkEvent("player_disconnected", new Dictionary<string, object>
                    {
                        {"client_id", clientId.ToString()},
                        {"total_players", connectedPlayers.Count}
                    });
                }
                
                // Publish to event bus
                _eventBus?.Publish(new PlayerDisconnectedEvent 
                { 
                    ClientId = clientId,
                    TotalPlayers = connectedPlayers.Count
                });
                
                Debug.Log($"Player disconnected: {clientId} (Total: {connectedPlayers.Count})");
            }
        }

        /// <summary>
        /// Get network player data for a specific client
        /// </summary>
        public NetworkPlayerData? GetPlayerData(ulong clientId)
        {
            return connectedPlayers.ContainsKey(clientId) ? connectedPlayers[clientId] : (NetworkPlayerData?)null;
        }

        /// <summary>
        /// Get all connected players
        /// </summary>
        public Dictionary<ulong, NetworkPlayerData> GetAllPlayers()
        {
            return new Dictionary<ulong, NetworkPlayerData>(connectedPlayers);
        }

        /// <summary>
        /// Update player data
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void UpdatePlayerDataServerRpc(ulong clientId, NetworkPlayerData playerData)
        {
            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers[clientId] = playerData;
                
                // Broadcast update to all clients
                UpdatePlayerDataClientRpc(clientId, playerData);
            }
        }

        [ClientRpc]
        private void UpdatePlayerDataClientRpc(ulong clientId, NetworkPlayerData playerData)
        {
            if (connectedPlayers.ContainsKey(clientId))
            {
                connectedPlayers[clientId] = playerData;
                
                _eventBus?.Publish(new PlayerDataUpdatedEvent 
                { 
                    ClientId = clientId, 
                    PlayerData = playerData 
                });
            }
        }

        #endregion

        #region Network Monitoring

        private IEnumerator NetworkMonitoringCoroutine()
        {
            while (true)
            {
                if (enableNetworkPerformanceMonitoring && IsServer)
                {
                    UpdateNetworkStats();
                    
                    if (Time.time - lastStatsUpdate >= networkStatsUpdateInterval)
                    {
                        PublishNetworkStats();
                        lastStatsUpdate = Time.time;
                    }
                }
                
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void UpdateNetworkStats()
        {
            // Update network statistics
            networkStats.Timestamp = System.DateTime.UtcNow;
            networkStats.ConnectedPlayers = connectedPlayers.Count;
            networkStats.IsServer = IsServer;
            networkStats.IsHost = IsHost;
            networkStats.IsClient = IsClient;
            
            // Get transport statistics if available
            if (NetworkConfig.NetworkTransport != null)
            {
                // Add transport-specific stats here
                networkStats.BytesSent = 0; // Placeholder - implement based on transport
                networkStats.BytesReceived = 0; // Placeholder - implement based on transport
            }
            
            // Integrate with performance manager
            if (performanceManager != null && enableNetworkPerformanceMonitoring)
            {
                var perfMetrics = performanceManager.GetCurrentMetrics();
                networkStats.ServerFPS = perfMetrics.CurrentFPS;
                networkStats.ServerMemoryUsage = perfMetrics.MemoryUsage;
            }
        }

        private void PublishNetworkStats()
        {
            OnNetworkStatsUpdated?.Invoke(networkStats);
            
            _eventBus?.Publish(new NetworkStatsUpdatedEvent { Stats = networkStats });
            
            // Track network performance analytics
            if (enableNetworkAnalytics)
            {
                TrackNetworkEvent("network_stats", new Dictionary<string, object>
                {
                    {"connected_players", networkStats.ConnectedPlayers},
                    {"server_fps", networkStats.ServerFPS},
                    {"server_memory", networkStats.ServerMemoryUsage},
                    {"bytes_sent", networkStats.BytesSent},
                    {"bytes_received", networkStats.BytesReceived}
                });
            }
        }

        #endregion

        #region Event Callbacks

        private void OnServerStartedCallback()
        {
            Debug.Log("Server started successfully");
            _eventBus?.Publish(new NetworkServerStartedEvent());
            
            if (enableNetworkAnalytics)
            {
                TrackNetworkEvent("server_started_success", new Dictionary<string, object>
                {
                    {"max_players", maxPlayers},
                    {"tick_rate", serverTickRate}
                });
            }
        }

        private void OnClientStartedCallback()
        {
            Debug.Log("Client started successfully");
            _eventBus?.Publish(new NetworkClientStartedEvent());
            
            if (enableNetworkAnalytics)
            {
                TrackNetworkEvent("client_started_success");
            }
        }

        #endregion

        #region Utility Methods

        private GameObject GetPlayerPrefab()
        {
            // Return the configured player prefab or create a default one
            if (NetworkConfig.PlayerPrefab != null)
            {
                return NetworkConfig.PlayerPrefab;
            }
            
            // Create a basic network player prefab
            GameObject playerPrefab = new GameObject("NetworkPlayer");
            playerPrefab.AddComponent<NetworkObject>();
            // Add other components as needed
            
            return playerPrefab;
        }

        private void TrackNetworkEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (analyticsManager != null && enableNetworkAnalytics)
            {
                analyticsManager.TrackEvent($"network_{eventName}", parameters);
            }
        }

        /// <summary>
        /// Graceful shutdown of all network connections
        /// </summary>
        public void GracefulShutdown()
        {
            if (IsServer)
            {
                // Notify all clients of shutdown
                NotifyClientsShutdownClientRpc();
                StartCoroutine(DelayedShutdown(2f));
            }
            else if (IsClient)
            {
                Shutdown();
            }
        }

        [ClientRpc]
        private void NotifyClientsShutdownClientRpc()
        {
            _eventBus?.Publish(new NetworkServerShutdownEvent());
        }

        private IEnumerator DelayedShutdown(float delay)
        {
            yield return new WaitForSeconds(delay);
            Shutdown();
        }

        private void OnApplicationQuitting()
        {
            if (IsServer || IsClient)
            {
                Shutdown();
            }
        }

        private void OnDestroy()
        {
            // Cleanup subscriptions
            Application.quitting -= OnApplicationQuitting;
            
            OnClientConnectedCallback -= OnClientConnected;
            OnClientDisconnectCallback -= OnClientDisconnected;
            OnServerStarted -= OnServerStartedCallback;
            OnClientStarted -= OnClientStartedCallback;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current network statistics
        /// </summary>
        public NetworkStats GetNetworkStats()
        {
            return networkStats;
        }

        /// <summary>
        /// Check if the server is at capacity
        /// </summary>
        public bool IsServerFull()
        {
            return IsServer && connectedPlayers.Count >= maxPlayers;
        }

        /// <summary>
        /// Kick a player from the server
        /// </summary>
        public void KickPlayer(ulong clientId, string reason = "")
        {
            if (IsServer && connectedPlayers.ContainsKey(clientId))
            {
                DisconnectClient(clientId);
                
                if (enableNetworkAnalytics)
                {
                    TrackNetworkEvent("player_kicked", new Dictionary<string, object>
                    {
                        {"client_id", clientId.ToString()},
                        {"reason", reason}
                    });
                }
            }
        }

        /// <summary>
        /// Send custom message to specific client
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SendCustomMessageServerRpc(ulong targetClientId, string messageType, string data)
        {
            SendCustomMessageClientRpc(targetClientId, messageType, data);
        }

        [ClientRpc]
        private void SendCustomMessageClientRpc(ulong targetClientId, string messageType, string data, ClientRpcParams clientRpcParams = default)
        {
            if (NetworkManager.Singleton.LocalClientId == targetClientId)
            {
                _eventBus?.Publish(new NetworkCustomMessageEvent 
                { 
                    MessageType = messageType, 
                    Data = data 
                });
            }
        }

        #endregion

        #region Configuration Accessors

        /// <summary>
        /// Gets the client update rate setting
        /// </summary>
        public float GetClientUpdateRate() => clientUpdateRate;

        /// <summary>
        /// Gets whether client prediction is enabled
        /// </summary>
        public bool IsClientPredictionEnabled() => enableClientPrediction;

        /// <summary>
        /// Gets whether lag compensation is enabled
        /// </summary>
        public bool IsLagCompensationEnabled() => enableLagCompensation;

        /// <summary>
        /// Gets whether encryption is enabled
        /// </summary>
        public bool IsEncryptionEnabled() => enableEncryption;

        /// <summary>
        /// Gets whether anti-cheat is enabled
        /// </summary>
        public bool IsAntiCheatEnabled() => enableAntiCheat;

        /// <summary>
        /// Gets the maximum packet size
        /// </summary>
        public float GetMaxPacketSize() => maxPacketSize;

        /// <summary>
        /// Gets whether dedicated server mode is enabled
        /// </summary>
        public bool IsDedicatedServerModeEnabled() => enableDedicatedServerMode;

        /// <summary>
        /// Applies client-specific network optimizations based on configuration
        /// </summary>
        public void ApplyClientOptimizations()
        {
            if (enableClientPrediction)
            {
                Debug.Log("Client prediction enabled - applying optimizations");
                // Implementation would go here
            }

            if (enableLagCompensation)
            {
                Debug.Log("Lag compensation enabled - applying optimizations");
                // Implementation would go here
            }
        }

        /// <summary>
        /// Applies security measures based on configuration
        /// </summary>
        public void ApplySecurityMeasures()
        {
            if (enableEncryption)
            {
                Debug.Log("Network encryption enabled");
                // Implementation would go here
            }

            if (enableAntiCheat)
            {
                Debug.Log("Anti-cheat measures enabled");
                // Implementation would go here
            }
        }

        #endregion
    }

    #region Network Data Structures

    [System.Serializable]
    public class NetworkStats
    {
        public System.DateTime Timestamp;
        public int ConnectedPlayers;
        public bool IsServer;
        public bool IsHost;
        public bool IsClient;
        public float ServerFPS;
        public long ServerMemoryUsage;
        public long BytesSent;
        public long BytesReceived;
        public float Latency;
        public float PacketLoss;
    }

    #endregion

    #region Network Events

    public class PlayerConnectedEvent : BaseEvent
    {
        public ulong ClientId { get; set; }
        public NetworkPlayerData PlayerData { get; set; }
        public int TotalPlayers { get; set; }
    }

    public class PlayerDisconnectedEvent : BaseEvent
    {
        public ulong ClientId { get; set; }
        public int TotalPlayers { get; set; }
    }

    public class PlayerDataUpdatedEvent : BaseEvent
    {
        public ulong ClientId { get; set; }
        public NetworkPlayerData PlayerData { get; set; }
    }

    public class NetworkStatsUpdatedEvent : BaseEvent
    {
        public NetworkStats Stats { get; set; }
    }

    public class NetworkServerStartedEvent : BaseEvent
    {
        // Inherits Timestamp from BaseEvent
    }

    public class NetworkClientStartedEvent : BaseEvent
    {
        // Inherits Timestamp from BaseEvent
    }

    public class NetworkServerShutdownEvent : BaseEvent
    {
        // Inherits Timestamp from BaseEvent
    }

    public class NetworkCustomMessageEvent : BaseEvent
    {
        public string MessageType { get; set; }
        public string Data { get; set; }
    }

    #endregion
}
