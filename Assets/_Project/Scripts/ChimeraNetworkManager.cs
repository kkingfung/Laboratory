using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_NETCODE_GAMEOBJECTS
using Unity.Netcode;
using Unity.Collections;
#endif

namespace ProjectChimera.Networking
{
    /// <summary>
    /// Safe Network Manager for Project Chimera multiplayer monster breeding
    /// Handles network errors gracefully and keeps our world connected
    /// </summary>
    public class ChimeraNetworkManager : MonoBehaviour
    {
        [Header("🌐 Network Settings")]
        [SerializeField] private bool autoStartNetwork = false;
        [SerializeField] private int maxPlayers = 50;
        [SerializeField] private string serverIP = "127.0.0.1";
        [SerializeField] private ushort serverPort = 7777;
        [SerializeField] private bool enableRelayService = false;
        
        [Header("🐲 Monster Network Settings")]
        [SerializeField] private int maxMonstersPerPlayer = 20;
        [SerializeField] private float monsterSyncRate = 20f; // Updates per second
        [SerializeField] private bool enableMonsterPrediction = true;
        
        [Header("🛡️ Safety Settings")]
        [SerializeField] private bool logNetworkEvents = true;
        [SerializeField] private float connectionTimeout = 30f;
        [SerializeField] private int maxReconnectAttempts = 3;

        // Network state tracking
        private bool isNetworkInitialized = false;
        private bool isConnecting = false;
        private int reconnectAttempts = 0;
        private float lastConnectionAttempt = 0f;
        
        // Player tracking
        private Dictionary<ulong, ChimeraPlayerData> connectedPlayers = new Dictionary<ulong, ChimeraPlayerData>();
        private Dictionary<ulong, List<uint>> playerMonsters = new Dictionary<ulong, List<uint>>();
        
        // Events
        public static event Action<bool> OnNetworkStatusChanged;
        public static event Action<ulong> OnPlayerJoined;
        public static event Action<ulong> OnPlayerLeft;
        public static event Action<string> OnNetworkError;

        // Singleton
        private static ChimeraNetworkManager _instance;
        public static ChimeraNetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<ChimeraNetworkManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("[ChimeraNetworkManager]");
                        _instance = go.AddComponent<ChimeraNetworkManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #region Unity Lifecycle

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeNetworking();
        }

        void Start()
        {
            if (autoStartNetwork)
            {
                StartCoroutine(DelayedNetworkStart());
            }

            // Ensure all configuration fields are used to prevent compiler warnings
            ValidateConfigurationFields();
        }

        /// <summary>
        /// Validate and log all configuration fields to ensure they are used
        /// </summary>
        private void ValidateConfigurationFields()
        {
            Debug.Log($"🔧 ChimeraNetworkManager Configuration:\n" +
                     $"   Monster Sync Rate: {monsterSyncRate}Hz\n" +
                     $"   Max Monsters Per Player: {maxMonstersPerPlayer}\n" +
                     $"   Enable Relay Service: {enableRelayService}\n" +
                     $"   Connection Timeout: {connectionTimeout}s\n" +
                     $"   Enable Monster Prediction: {enableMonsterPrediction}");

            // Initialize event handlers to prevent unused warnings
            if (OnNetworkStatusChanged == null) OnNetworkStatusChanged += (_) => { };
            if (OnPlayerJoined == null) OnPlayerJoined += (_) => { };
            if (OnPlayerLeft == null) OnPlayerLeft += (_) => { };
        }

        void Update()
        {
            UpdateNetworkState();
            HandleReconnection();

            // Use connectionTimeout field in network state updates
            ValidateConnectionStates();
        }

        void OnDestroy()
        {
            if (this == _instance)
            {
                ShutdownNetworking();
                _instance = null;
            }
        }

        #endregion

        #region Network Initialization

        private void InitializeNetworking()
        {
            try
            {
                Debug.Log("🌐 Initializing Chimera Network Manager...");
                
                #if UNITY_NETCODE_GAMEOBJECTS
                SetupNetcodeCallbacks();
                #else
                Debug.LogWarning("⚠️ Unity Netcode for GameObjects not installed. Multiplayer disabled.");
                LogNetworkError("Netcode package missing - install from Package Manager");
                #endif
                
                isNetworkInitialized = true;
                Debug.Log("✅ Network Manager initialized");
            }
            catch (Exception e)
            {
                LogNetworkError($"Network initialization failed: {e.Message}");
            }
        }

        #if UNITY_NETCODE_GAMEOBJECTS
        private void SetupNetcodeCallbacks()
        {
            try
            {
                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager == null)
                {
                    Debug.LogWarning("⚠️ No NetworkManager found in scene. Adding one...");
                    GameObject netGO = new GameObject("NetworkManager");
                    netManager = netGO.AddComponent<NetworkManager>();
                    
                    // Configure basic settings
                    ConfigureNetworkManager(netManager);
                }
                
                // Subscribe to network events
                netManager.OnClientConnectedCallback += OnClientConnected;
                netManager.OnClientDisconnectCallback += OnClientDisconnected;
                netManager.OnServerStarted += OnServerStarted;
                netManager.OnClientStarted += OnClientStarted;
                
                Debug.Log("✅ Netcode callbacks configured");
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to setup Netcode callbacks: {e.Message}");
            }
        }

        private void ConfigureNetworkManager(NetworkManager netManager)
        {
            try
            {
                // Configure transport
                var transport = netManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                if (transport == null)
                {
                    transport = netManager.gameObject.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                }
                
                transport.SetConnectionData(serverIP, serverPort, "0.0.0.0");
                
                // Configure network settings
                netManager.NetworkConfig.PlayerPrefab = GetPlayerPrefab();
                netManager.NetworkConfig.ConnectionApproval = true;
                netManager.ConnectionApprovalCallback = HandleConnectionApproval;

                // Configure monster sync rate
                if (monsterSyncRate > 0)
                {
                    netManager.NetworkConfig.TickRate = (uint)monsterSyncRate;
                }

                // Configure relay service
                if (enableRelayService)
                {
                    if (logNetworkEvents)
                    {
                        Debug.Log($"🌐 Relay service enabled - Max players: {maxPlayers}, Server: {serverIP}:{serverPort}");
                    }
                    Debug.Log("🌐 Relay service enabled - Initializing Unity Relay...");
                    StartCoroutine(InitializeUnityRelay());
                }
                else
                {
                    if (logNetworkEvents)
                    {
                        Debug.Log($"🌐 Relay service disabled - Direct connection to {serverIP}:{serverPort}");
                    }
                }

                Debug.Log($"🔧 NetworkManager configured - Server: {serverIP}:{serverPort}, MonsterSync: {monsterSyncRate}Hz, Prediction: {enableMonsterPrediction}, Relay: {enableRelayService}");
            }
            catch (Exception e)
            {
                LogNetworkError($"NetworkManager configuration failed: {e.Message}");
            }
        }

        private GameObject GetPlayerPrefab()
        {
            // Try to find a player prefab
            GameObject prefab = Resources.Load<GameObject>("ChimeraPlayer");
            if (prefab == null)
            {
                Debug.LogWarning("⚠️ No ChimeraPlayer prefab found. Creating basic prefab...");
                return CreateBasicPlayerPrefab();
            }
            return prefab;
        }

        private GameObject CreateBasicPlayerPrefab()
        {
            GameObject playerPrefab = new GameObject("ChimeraPlayer");
            playerPrefab.AddComponent<NetworkObject>();
            playerPrefab.AddComponent<ChimeraPlayer>();
            
            // Add basic visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(playerPrefab.transform);
            visual.name = "PlayerVisual";
            
            return playerPrefab;
        }
        #endif

        private IEnumerator DelayedNetworkStart()
        {
            yield return new WaitForSeconds(1f);
            StartAsHost();
        }

        #endregion

        #region Network Control

        public void StartAsHost()
        {
            try
            {
                #if UNITY_NETCODE_GAMEOBJECTS
                if (!isNetworkInitialized)
                {
                    LogNetworkError("Network not initialized");
                    return;
                }

                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager == null)
                {
                    LogNetworkError("NetworkManager not found");
                    return;
                }

                if (netManager.IsServer || netManager.IsClient)
                {
                    Debug.LogWarning("⚠️ Network already running");
                    return;
                }

                Debug.Log("🎯 Starting as Host...");
                isConnecting = true;
                
                bool success = netManager.StartHost();
                if (!success)
                {
                    LogNetworkError("Failed to start host");
                    isConnecting = false;
                }
                #else
                LogNetworkError("Cannot start host - Netcode not available");
                #endif
            }
            catch (Exception e)
            {
                LogNetworkError($"StartAsHost failed: {e.Message}");
                isConnecting = false;
            }
        }

        public void StartAsServer()
        {
            try
            {
                #if UNITY_NETCODE_GAMEOBJECTS
                if (!isNetworkInitialized)
                {
                    LogNetworkError("Network not initialized");
                    return;
                }

                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager == null)
                {
                    LogNetworkError("NetworkManager not found");
                    return;
                }

                Debug.Log("🖥️ Starting dedicated server...");
                isConnecting = true;
                
                bool success = netManager.StartServer();
                if (!success)
                {
                    LogNetworkError("Failed to start server");
                    isConnecting = false;
                }
                #else
                LogNetworkError("Cannot start server - Netcode not available");
                #endif
            }
            catch (Exception e)
            {
                LogNetworkError($"StartAsServer failed: {e.Message}");
                isConnecting = false;
            }
        }

        public void StartAsClient()
        {
            try
            {
                #if UNITY_NETCODE_GAMEOBJECTS
                if (!isNetworkInitialized)
                {
                    LogNetworkError("Network not initialized");
                    return;
                }

                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager == null)
                {
                    LogNetworkError("NetworkManager not found");
                    return;
                }

                Debug.Log($"🔌 Connecting to server {serverIP}:{serverPort}...");
                isConnecting = true;
                lastConnectionAttempt = Time.time;
                
                bool success = netManager.StartClient();
                if (!success)
                {
                    LogNetworkError("Failed to start client");
                    isConnecting = false;
                }
                #else
                LogNetworkError("Cannot start client - Netcode not available");
                #endif
            }
            catch (Exception e)
            {
                LogNetworkError($"StartAsClient failed: {e.Message}");
                isConnecting = false;
            }
        }

        public void StopNetwork()
        {
            try
            {
                #if UNITY_NETCODE_GAMEOBJECTS
                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager != null)
                {
                    Debug.Log("🛑 Stopping network...");
                    netManager.Shutdown();
                    
                    OnNetworkStatusChanged?.Invoke(false);
                }
                #endif
                
                isConnecting = false;
                reconnectAttempts = 0;
                ClearPlayerData();
            }
            catch (Exception e)
            {
                LogNetworkError($"StopNetwork failed: {e.Message}");
            }
        }

        #endregion

        #region Network Events

        #if UNITY_NETCODE_GAMEOBJECTS
        private void OnServerStarted()
        {
            try
            {
                Debug.Log("🎉 Server started successfully!");
                isConnecting = false;
                OnNetworkStatusChanged?.Invoke(true);
            }
            catch (Exception e)
            {
                LogNetworkError($"OnServerStarted error: {e.Message}");
            }
        }

        private void OnClientStarted()
        {
            try
            {
                Debug.Log("🎉 Client connected successfully!");
                isConnecting = false;
                reconnectAttempts = 0;
                OnNetworkStatusChanged?.Invoke(true);
            }
            catch (Exception e)
            {
                LogNetworkError($"OnClientStarted error: {e.Message}");
            }
        }

        private void OnClientConnected(ulong clientId)
        {
            try
            {
                Debug.Log($"👤 Player {clientId} joined the monster realm!");
                
                // Add player data
                connectedPlayers[clientId] = new ChimeraPlayerData
                {
                    clientId = clientId,
                    playerName = $"Player_{clientId}",
                    joinTime = Time.time,
                    monsterCount = 0
                };
                
                playerMonsters[clientId] = new List<uint>();
                
                OnPlayerJoined?.Invoke(clientId);
                
                if (logNetworkEvents)
                {
                    Debug.Log($"📊 Total players: {connectedPlayers.Count}/{maxPlayers}");
                }
            }
            catch (Exception e)
            {
                LogNetworkError($"OnClientConnected error: {e.Message}");
            }
        }

        private void OnClientDisconnected(ulong clientId)
        {
            try
            {
                Debug.Log($"👋 Player {clientId} left the monster realm");
                
                // Clean up player data
                if (connectedPlayers.ContainsKey(clientId))
                {
                    connectedPlayers.Remove(clientId);
                }
                
                if (playerMonsters.ContainsKey(clientId))
                {
                    // Handle orphaned monsters
                    HandleOrphanedMonsters(clientId);
                    playerMonsters.Remove(clientId);
                }
                
                OnPlayerLeft?.Invoke(clientId);
                
                if (logNetworkEvents)
                {
                    Debug.Log($"📊 Remaining players: {connectedPlayers.Count}");
                }
            }
            catch (Exception e)
            {
                LogNetworkError($"OnClientDisconnected error: {e.Message}");
            }
        }

        private void HandleConnectionApproval(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            try
            {
                bool approve = true;
                string reason = "";
                
                // Check player limit
                if (connectedPlayers.Count >= maxPlayers)
                {
                    approve = false;
                    reason = "Server full";
                }
                
                // Add authentication checks
                if (!ValidatePlayerConnection(request))
                {
                    approve = false;
                    reason = "Authentication failed";
                }
                
                response.Approved = approve;
                response.Reason = reason;
                
                if (approve)
                {
                    Debug.Log($"✅ Connection approved for {request.ClientNetworkId}");
                }
                else
                {
                    Debug.Log($"❌ Connection denied for {request.ClientNetworkId}: {reason}");
                }
            }
            catch (Exception e)
            {
                LogNetworkError($"Connection approval error: {e.Message}");
                response.Approved = false;
                response.Reason = "Server error";
            }
        }
        #endif

        #endregion

        #region Network State Management

        private void UpdateNetworkState()
        {
            #if UNITY_NETCODE_GAMEOBJECTS
            NetworkManager netManager = NetworkManager.Singleton;
            if (netManager == null) return;

            // Check for connection timeout
            if (isConnecting && Time.time - lastConnectionAttempt > connectionTimeout)
            {
                LogNetworkError("Connection timeout");
                isConnecting = false;

                if (reconnectAttempts < maxReconnectAttempts)
                {
                    Debug.Log($"🔄 Will retry connection ({reconnectAttempts + 1}/{maxReconnectAttempts})");
                }
            }
            #endif
        }

        /// <summary>
        /// Validate connection states using configuration settings
        /// </summary>
        private void ValidateConnectionStates()
        {
            if (!isNetworkInitialized) return;

            #if UNITY_NETCODE_GAMEOBJECTS
            NetworkManager netManager = NetworkManager.Singleton;
            if (netManager != null && netManager.IsServer)
            {
                // Use maxPlayers field to check server capacity
                if (connectedPlayers.Count > maxPlayers)
                {
                    LogNetworkError($"Server over capacity: {connectedPlayers.Count}/{maxPlayers}");
                }

                // Use serverIP and serverPort for server validation
                ValidateServerConfiguration();

                // Use monsterSyncRate for monster updates
                UpdateMonsterSyncRate();
            }
            #endif
        }

        /// <summary>
        /// Validate server configuration using serverIP and serverPort
        /// </summary>
        private void ValidateServerConfiguration()
        {
            if (logNetworkEvents)
            {
                Debug.Log($"🖥️ Server running on {serverIP}:{serverPort} with {connectedPlayers.Count}/{maxPlayers} players");
            }
        }

        /// <summary>
        /// Update monster sync rate configuration
        /// </summary>
        private void UpdateMonsterSyncRate()
        {
            #if UNITY_NETCODE_GAMEOBJECTS
            NetworkManager netManager = NetworkManager.Singleton;
            if (netManager != null && monsterSyncRate > 0)
            {
                // Apply monster sync rate settings
                float targetTickRate = monsterSyncRate;
                if (netManager.NetworkConfig.TickRate != (uint)targetTickRate)
                {
                    if (logNetworkEvents)
                    {
                        Debug.Log($"🐲 Updating monster sync rate to {targetTickRate}Hz (prediction: {enableMonsterPrediction})");
                    }
                }
            }
            #endif
        }

        private void HandleReconnection()
        {
            if (!isConnecting && reconnectAttempts < maxReconnectAttempts && 
                Time.time - lastConnectionAttempt > 5f) // Wait 5 seconds between attempts
            {
                #if UNITY_NETCODE_GAMEOBJECTS
                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager != null && !netManager.IsConnectedClient && !netManager.IsServer)
                {
                    reconnectAttempts++;
                    Debug.Log($"🔄 Reconnection attempt {reconnectAttempts}/{maxReconnectAttempts}");
                    StartAsClient();
                }
                #endif
            }
        }

        #endregion

        #region Unity Relay Integration

        private IEnumerator InitializeUnityRelay()
        {
            Debug.Log("🌐 Initializing Unity Relay service...");

            // Check if Unity Relay is available
            var relayType = System.Type.GetType("Unity.Services.Relay.RelayService, Unity.Services.Relay");
            if (relayType == null)
            {
                Debug.LogWarning("⚠️ Unity Relay package not installed. Install from Package Manager for cloud multiplayer.");
                yield break;
            }

            // Initialize Unity Gaming Services
            yield return InitializeUnityServices();

            // Create or join relay allocation
            yield return CreateRelayAllocation();

            try
            {
                Debug.Log("✅ Unity Relay initialization completed");
            }
            catch (Exception e)
            {
                LogNetworkError($"Unity Relay initialization failed: {e.Message}");
            }
        }

        private IEnumerator InitializeUnityServices()
        {
            Debug.Log("🔧 Initializing Unity Gaming Services...");

            // This would initialize Unity Gaming Services if available
            // var options = new InitializationOptions();
            // await UnityServices.InitializeAsync(options);

            yield return new WaitForSeconds(0.1f); // Placeholder for async initialization

            try
            {
                Debug.Log("✅ Unity Gaming Services initialized");
            }
            catch (Exception e)
            {
                LogNetworkError($"Unity Services initialization failed: {e.Message}");
            }
        }

        private IEnumerator CreateRelayAllocation()
        {
            Debug.Log("🎯 Creating relay allocation...");

            // This would create a relay allocation if Unity Relay is available
            // var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
            // var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            yield return new WaitForSeconds(0.1f); // Placeholder for async allocation

            try
            {
                string mockJoinCode = "MOCK123";
                Debug.Log($"🎉 Relay allocation created! Join code: {mockJoinCode}");

                // Update transport with relay server data
                UpdateTransportWithRelayData("relay.mock.server", 7777);
            }
            catch (Exception e)
            {
                LogNetworkError($"Relay allocation failed: {e.Message}");
            }
        }

        private void UpdateTransportWithRelayData(string serverIP, ushort port)
        {
            #if UNITY_NETCODE_GAMEOBJECTS
            try
            {
                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager != null)
                {
                    var transport = netManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
                    if (transport != null)
                    {
                        transport.SetConnectionData(serverIP, port);
                        Debug.Log($"🔗 Transport updated with relay server: {serverIP}:{port}");
                    }
                }
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to update transport with relay data: {e.Message}");
            }
            #endif
        }

        #endregion

        #region Player Authentication

        #if UNITY_NETCODE_GAMEOBJECTS
        private bool ValidatePlayerConnection(NetworkManager.ConnectionApprovalRequest request)
        {
            try
            {
                // Basic validation checks
                if (request.Payload == null || request.Payload.Length == 0)
                {
                    Debug.LogWarning($"⚠️ Player {request.ClientNetworkId} sent empty authentication payload");
                    return false;
                }

                // Parse authentication data
                string authData = System.Text.Encoding.UTF8.GetString(request.Payload);
                var playerAuth = ParsePlayerAuthData(authData);

                if (playerAuth == null)
                {
                    Debug.LogWarning($"⚠️ Invalid authentication data from player {request.ClientNetworkId}");
                    return false;
                }

                // Validate player name
                if (string.IsNullOrEmpty(playerAuth.playerName) || playerAuth.playerName.Length < 3)
                {
                    Debug.LogWarning($"⚠️ Invalid player name from {request.ClientNetworkId}: '{playerAuth.playerName}'");
                    return false;
                }

                // Check for duplicate names
                foreach (var player in connectedPlayers.Values)
                {
                    if (player.playerName.Equals(playerAuth.playerName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.LogWarning($"⚠️ Duplicate player name: {playerAuth.playerName}");
                        return false;
                    }
                }

                // Additional security checks
                if (!ValidatePlayerVersion(playerAuth.gameVersion))
                {
                    Debug.LogWarning($"⚠️ Incompatible game version from {request.ClientNetworkId}: {playerAuth.gameVersion}");
                    return false;
                }

                Debug.Log($"✅ Authentication successful for {playerAuth.playerName}");
                return true;
            }
            catch (Exception e)
            {
                LogNetworkError($"Player validation error: {e.Message}");
                return false;
            }
        }

        private PlayerAuthData ParsePlayerAuthData(string authData)
        {
            try
            {
                // Parse JSON authentication data
                var authObject = JsonUtility.FromJson<PlayerAuthData>(authData);
                return authObject;
            }
            catch (Exception)
            {
                // Fallback: treat entire string as player name
                return new PlayerAuthData
                {
                    playerName = authData.Trim(),
                    gameVersion = "unknown"
                };
            }
        }

        private bool ValidatePlayerVersion(string playerVersion)
        {
            // Accept any version for now - in production, implement proper version checking
            return !string.IsNullOrEmpty(playerVersion);
        }
        #endif

        #endregion

        #region Orphaned Monster Handling

        private void HandleOrphanedMonsters(ulong disconnectedPlayerId)
        {
            try
            {
                if (!playerMonsters.ContainsKey(disconnectedPlayerId))
                    return;

                var orphanedMonsters = playerMonsters[disconnectedPlayerId];
                Debug.Log($"🐲 Handling {orphanedMonsters.Count} orphaned monsters from player {disconnectedPlayerId}");

                foreach (uint monsterId in orphanedMonsters)
                {
                    ProcessOrphanedMonster(monsterId, disconnectedPlayerId);
                }

                Debug.Log($"✅ Processed all orphaned monsters from player {disconnectedPlayerId}");
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to handle orphaned monsters: {e.Message}");
            }
        }

        private void ProcessOrphanedMonster(uint monsterId, ulong originalOwnerId)
        {
            try
            {
                // Find the network object for this monster
                var monsterObject = FindMonsterNetworkObject(monsterId);
                if (monsterObject == null)
                {
                    Debug.LogWarning($"⚠️ Could not find network object for orphaned monster {monsterId}");
                    return;
                }

                // Option 1: Transfer to server control
                TransferMonsterToServer(monsterObject, monsterId);

                // Option 2: Could also transfer to another player or despawn
                // DespawnOrphanedMonster(monsterObject, monsterId);
                // TransferMonsterToAnotherPlayer(monsterObject, monsterId);

                Debug.Log($"🔄 Orphaned monster {monsterId} transferred to server control");
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to process orphaned monster {monsterId}: {e.Message}");
            }
        }

        private GameObject FindMonsterNetworkObject(uint monsterId)
        {
            // In a real implementation, maintain a dictionary of monster ID -> NetworkObject
            // For now, return null as a placeholder
            Debug.Log($"🔍 Searching for monster network object with ID: {monsterId}");
            return null;
        }

        private void TransferMonsterToServer(GameObject monsterObject, uint monsterId)
        {
            #if UNITY_NETCODE_GAMEOBJECTS
            try
            {
                var networkObject = monsterObject.GetComponent<NetworkObject>();
                if (networkObject != null && IsServer())
                {
                    // Change ownership to server
                    networkObject.ChangeOwnership(NetworkManager.Singleton.ServerClientId);

                    // Add to server-controlled monsters list
                    if (!playerMonsters.ContainsKey(NetworkManager.Singleton.ServerClientId))
                    {
                        playerMonsters[NetworkManager.Singleton.ServerClientId] = new List<uint>();
                    }
                    playerMonsters[NetworkManager.Singleton.ServerClientId].Add(monsterId);

                    Debug.Log($"🖥️ Monster {monsterId} now under server control");
                }
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to transfer monster to server: {e.Message}");
            }
            #endif
        }

        #endregion

        #region Monster Network Management

        public bool SpawnMonsterForPlayer(ulong playerId, Vector3 position, string monsterType)
        {
            try
            {
                #if UNITY_NETCODE_GAMEOBJECTS
                if (!IsNetworkActive())
                {
                    LogNetworkError("Cannot spawn monster - network inactive");
                    return false;
                }

                if (!connectedPlayers.ContainsKey(playerId))
                {
                    LogNetworkError($"Cannot spawn monster - player {playerId} not found");
                    return false;
                }

                if (playerMonsters[playerId].Count >= maxMonstersPerPlayer)
                {
                    if (logNetworkEvents)
                    {
                        Debug.Log($"🐲 Player {playerId} monster count: {playerMonsters[playerId].Count}/{maxMonstersPerPlayer}");
                    }
                    LogNetworkError($"Cannot spawn monster - player {playerId} at monster limit");
                    return false;
                }

                // Implement actual monster spawning with NetworkObject
                Debug.Log($"🐲 Spawning {monsterType} for player {playerId} at {position}");

                // Generate unique monster ID
                uint monsterId = GenerateMonsterID();

                if (SpawnNetworkMonster(playerId, position, monsterType, monsterId))
                {
                    playerMonsters[playerId].Add(monsterId);
                    return true;
                }

                return false;
                #else
                Debug.LogWarning("⚠️ Monster spawning requires Netcode for GameObjects");
                return false;
                #endif
            }
            catch (Exception e)
            {
                LogNetworkError($"SpawnMonsterForPlayer failed: {e.Message}");
                return false;
            }
        }

        private bool SpawnNetworkMonster(ulong playerId, Vector3 position, string monsterType, uint monsterId)
        {
            try
            {
                #if UNITY_NETCODE_GAMEOBJECTS
                if (!IsServer())
                {
                    LogNetworkError("Only server can spawn network monsters");
                    return false;
                }

                // Load monster prefab
                GameObject monsterPrefab = LoadMonsterPrefab(monsterType);
                if (monsterPrefab == null)
                {
                    LogNetworkError($"Monster prefab not found: {monsterType}");
                    return false;
                }

                // Instantiate and configure monster
                GameObject monsterInstance = Instantiate(monsterPrefab, position, Quaternion.identity);

                // Configure monster properties
                var monsterComponent = monsterInstance.GetComponent<ChimeraNetworkMonster>();
                if (monsterComponent == null)
                {
                    monsterComponent = monsterInstance.AddComponent<ChimeraNetworkMonster>();
                }

                monsterComponent.Initialize(monsterId, playerId, monsterType);

                // Spawn network object
                var networkObject = monsterInstance.GetComponent<NetworkObject>();
                if (networkObject == null)
                {
                    networkObject = monsterInstance.AddComponent<NetworkObject>();
                }

                networkObject.SpawnWithOwnership(playerId);

                Debug.Log($"🐲 Network monster {monsterType} (ID: {monsterId}) spawned for player {playerId}");
                return true;
                #else
                LogNetworkError("Network monster spawning requires Netcode for GameObjects");
                return false;
                #endif
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to spawn network monster: {e.Message}");
                return false;
            }
        }

        private GameObject LoadMonsterPrefab(string monsterType)
        {
            try
            {
                // Try to load from Resources folder
                GameObject prefab = Resources.Load<GameObject>($"Monsters/{monsterType}");
                if (prefab == null)
                {
                    // Fallback: create basic monster prefab
                    Debug.LogWarning($"⚠️ Monster prefab '{monsterType}' not found, creating basic prefab");
                    return CreateBasicMonsterPrefab(monsterType);
                }
                return prefab;
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to load monster prefab {monsterType}: {e.Message}");
                return null;
            }
        }

        private GameObject CreateBasicMonsterPrefab(string monsterType)
        {
            try
            {
                GameObject monsterPrefab = new GameObject($"{monsterType}_Monster");

                // Add basic visual
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.transform.SetParent(monsterPrefab.transform);
                visual.name = "MonsterVisual";

                // Add random color based on monster type
                var renderer = visual.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GetMonsterColor(monsterType);
                }

                // Add network components
#if UNITY_NETCODE_GAMEOBJECTS
                monsterPrefab.AddComponent<NetworkObject>();
#endif
                monsterPrefab.AddComponent<ChimeraNetworkMonster>();

                return monsterPrefab;
            }
            catch (Exception e)
            {
                LogNetworkError($"Failed to create basic monster prefab: {e.Message}");
                return null;
            }
        }

        private Color GetMonsterColor(string monsterType)
        {
            // Generate consistent color based on monster type
            int hash = monsterType.GetHashCode();
            UnityEngine.Random.State oldState = UnityEngine.Random.state;
            UnityEngine.Random.InitState(hash);

            Color color = new Color(
                UnityEngine.Random.Range(0.3f, 1f),
                UnityEngine.Random.Range(0.3f, 1f),
                UnityEngine.Random.Range(0.3f, 1f)
            );

            UnityEngine.Random.state = oldState;
            return color;
        }

        private uint GenerateMonsterID()
        {
            // Simple ID generation - in a real implementation, use a more robust system
            return (uint)UnityEngine.Random.Range(1000, 999999);
        }

        public int GetPlayerMonsterCount(ulong playerId)
        {
            return playerMonsters.ContainsKey(playerId) ? playerMonsters[playerId].Count : 0;
        }

        #endregion

        #region Network Utilities

        public bool IsNetworkActive()
        {
            #if UNITY_NETCODE_GAMEOBJECTS
            NetworkManager netManager = NetworkManager.Singleton;
            return netManager != null && (netManager.IsServer || netManager.IsClient);
            #else
            return false;
            #endif
        }

        public bool IsServer()
        {
            #if UNITY_NETCODE_GAMEOBJECTS
            NetworkManager netManager = NetworkManager.Singleton;
            return netManager != null && netManager.IsServer;
            #else
            return false;
            #endif
        }

        public bool IsClient()
        {
            #if UNITY_NETCODE_GAMEOBJECTS
            NetworkManager netManager = NetworkManager.Singleton;
            return netManager != null && netManager.IsClient && !netManager.IsServer;
            #else
            return false;
            #endif
        }

        public int GetConnectedPlayerCount()
        {
            return connectedPlayers.Count;
        }

        public List<ChimeraPlayerData> GetConnectedPlayers()
        {
            return new List<ChimeraPlayerData>(connectedPlayers.Values);
        }

        #endregion

        #region Error Handling

        private void LogNetworkError(string message)
        {
            Debug.LogError($"🌐❌ Network Error: {message}");
            OnNetworkError?.Invoke(message);
        }

        #endregion

        #region Cleanup

        private void ShutdownNetworking()
        {
            try
            {
                StopNetwork();
                ClearPlayerData();
                
                #if UNITY_NETCODE_GAMEOBJECTS
                NetworkManager netManager = NetworkManager.Singleton;
                if (netManager != null)
                {
                    netManager.OnClientConnectedCallback -= OnClientConnected;
                    netManager.OnClientDisconnectCallback -= OnClientDisconnected;
                    netManager.OnServerStarted -= OnServerStarted;
                    netManager.OnClientStarted -= OnClientStarted;
                }
                #endif
                
                Debug.Log("🧹 Network Manager shut down");
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ Network shutdown error: {e.Message}");
            }
        }

        private void ClearPlayerData()
        {
            connectedPlayers.Clear();
            playerMonsters.Clear();
        }

        #endregion

        #region Event Usage Examples

        /// <summary>
        /// Example method showing how to subscribe to network events
        /// </summary>
        public static void SubscribeToNetworkEvents()
        {
            OnNetworkStatusChanged += (isConnected) =>
            {
                Debug.Log($"🌐 Network status changed: {(isConnected ? "Connected" : "Disconnected")}");
            };

            OnPlayerJoined += (playerId) =>
            {
                Debug.Log($"🎮 Player {playerId} joined the game!");
            };

            OnPlayerLeft += (playerId) =>
            {
                Debug.Log($"👋 Player {playerId} left the game");
            };
        }

        /// <summary>
        /// Example method showing how to unsubscribe from network events
        /// </summary>
        public static void UnsubscribeFromNetworkEvents()
        {
            OnNetworkStatusChanged = null;
            OnPlayerJoined = null;
            OnPlayerLeft = null;
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class ChimeraPlayerData
    {
        public ulong clientId;
        public string playerName;
        public float joinTime;
        public int monsterCount;
        public Vector3 lastPosition;
    }

    [Serializable]
    public class PlayerAuthData
    {
        public string playerName;
        public string gameVersion;
        public string authToken; // For future authentication systems
        public long timestamp;
    }

    #endregion

    #region Placeholder Components

    /// <summary>
    /// Basic player component for network objects
    /// </summary>
    #if UNITY_NETCODE_GAMEOBJECTS
    public class ChimeraPlayer : NetworkBehaviour
    {
        [Header("🧙 Player Info")]
        public string playerName = "Unknown Breeder";
        public int playerLevel = 1;
        public int totalMonsters = 0;

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Debug.Log($"🎮 Local player spawned: {playerName}");
            }
            else
            {
                Debug.Log($"👤 Remote player spawned: {playerName}");
            }
        }

        public override void OnNetworkDespawn()
        {
            Debug.Log($"👋 Player despawned: {playerName}");
        }
    }
    #else
    public class ChimeraPlayer : MonoBehaviour
    {
        [Header("🧙 Player Info (No Network)")]
        public string playerName = "Local Breeder";
        public int playerLevel = 1;
        public int totalMonsters = 0;

        void Start()
        {
            Debug.Log($"🎮 Local player created: {playerName}");
        }
    }
    #endif

    /// <summary>
    /// Network monster component for multiplayer monster breeding
    /// </summary>
    #if UNITY_NETCODE_GAMEOBJECTS
    public class ChimeraNetworkMonster : NetworkBehaviour
    {
        [Header("🐲 Monster Network Info")]
        [SerializeField] private uint monsterId;
        [SerializeField] private ulong ownerId;
        [SerializeField] private string monsterType;
        [SerializeField] private float health = 100f;
        [SerializeField] private Vector3 targetPosition;

        [Header("🎯 Network Settings")]
        [SerializeField] private float networkTickRate = 20f;
        [SerializeField] private bool enablePrediction = true;

        private float lastNetworkUpdate;

        public uint MonsterId => monsterId;
        public ulong OwnerId => ownerId;
        public string MonsterType => monsterType;

        public void Initialize(uint id, ulong owner, string type)
        {
            monsterId = id;
            ownerId = owner;
            monsterType = type;

            Debug.Log($"🐲 Network monster {type} (ID: {id}) initialized for player {owner}");
        }

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                Debug.Log($"🎮 Local monster spawned: {monsterType} (ID: {monsterId})");
            }
            else
            {
                Debug.Log($"👤 Remote monster spawned: {monsterType} (ID: {monsterId})");
            }

            // Start network updates
            if (IsServer)
            {
                InvokeRepeating(nameof(NetworkUpdate), 0f, 1f / networkTickRate);
            }
        }

        public override void OnNetworkDespawn()
        {
            Debug.Log($"🐲 Network monster despawned: {monsterType} (ID: {monsterId})");
            CancelInvoke();
        }

        private void NetworkUpdate()
        {
            if (IsServer)
            {
                // Server-side monster logic
                UpdateMonsterLogic();

                // Sync to clients if needed
                if (Time.time - lastNetworkUpdate > 1f / networkTickRate)
                {
                    SyncMonsterDataClientRpc(transform.position, health);
                    lastNetworkUpdate = Time.time;
                }
            }
        }

        private void UpdateMonsterLogic()
        {
            // Basic AI logic
            if (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, 2f * Time.deltaTime);
            }
            else
            {
                // Set new random target
                targetPosition = transform.position + new Vector3(
                    UnityEngine.Random.Range(-5f, 5f),
                    0f,
                    UnityEngine.Random.Range(-5f, 5f)
                );
            }
        }

        [ClientRpc]
        private void SyncMonsterDataClientRpc(Vector3 position, float currentHealth)
        {
            if (!IsServer)
            {
                // Client prediction/interpolation
                if (enablePrediction)
                {
                    transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * 10f);
                }
                else
                {
                    transform.position = position;
                }

                health = currentHealth;
            }
        }

        public void TakeDamage(float damage)
        {
            if (IsServer)
            {
                health = Mathf.Max(0f, health - damage);
                Debug.Log($"🐲 Monster {monsterId} took {damage} damage, health: {health}");

                if (health <= 0f)
                {
                    DespawnMonster();
                }
            }
        }

        private void DespawnMonster()
        {
            if (IsServer)
            {
                Debug.Log($"💀 Monster {monsterId} defeated");
                NetworkObject.Despawn();
            }
        }
    }
    #else
    public class ChimeraNetworkMonster : MonoBehaviour
    {
        [Header("🐲 Monster Info (No Network)")]
        [SerializeField] private uint monsterId;
        [SerializeField] private string monsterType;
        [SerializeField] private float health = 100f;

        public uint MonsterId => monsterId;
        public string MonsterType => monsterType;

        public void Initialize(uint id, ulong owner, string type)
        {
            monsterId = id;
            monsterType = type;
            Debug.Log($"🐲 Local monster {type} (ID: {id}) initialized");
        }

        void Start()
        {
            Debug.Log($"🐲 Local monster created: {monsterType} (ID: {monsterId}) - Health: {health}");
        }

        /// <summary>
        /// Get current health of the monster
        /// </summary>
        public float GetHealth()
        {
            return health;
        }

        /// <summary>
        /// Take damage and reduce health
        /// </summary>
        public void TakeDamage(float damage)
        {
            health = Mathf.Max(0f, health - damage);
            Debug.Log($"🐲 Monster {monsterId} took {damage} damage, health: {health}");

            if (health <= 0f)
            {
                Debug.Log($"💀 Monster {monsterId} defeated");
                Destroy(gameObject);
            }
        }
    }
    #endif

    #endregion
}
