using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
#if UNITY_SERVICES_CORE
using Unity.Services.Core;
using Unity.Services.Authentication;
#endif
#if UNITY_SERVICES_RELAY
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using Laboratory.Core.Services;
using CoreMatchmakingState = Laboratory.Core.Enums.MatchmakingState;

#nullable enable

namespace Laboratory.Gameplay.Lobby
{
    /// <summary>
    /// Handles matchmaking logic, queueing, and starting matches.
    /// </summary>
    public class MatchmakingManager : MonoBehaviour
    {
        #region Singleton

        public static MatchmakingManager? Instance { get; private set; }

        #endregion

        #region Fields

        [Header("Matchmaking Settings")]
        [SerializeField] private float matchmakingTimeout = 30f;
        [SerializeField] private bool useRelayService = false;
        [SerializeField] private int maxPlayersPerMatch = 4;
        [SerializeField] private string fallbackServerAddress = "127.0.0.1";
        [SerializeField] private ushort fallbackServerPort = 7777;

        private float _timer;
        private bool _isSearching;
        private string? _currentJoinCode;
        private string? _currentAllocationId;
        
        // Services
        private IEventBus? _eventBus;
        private INetworkService? _networkService;

        private CancellationTokenSource cts = new();

        #endregion

        #region Properties

        /// <summary>
        /// Indicates if matchmaking is currently in progress.
        /// </summary>
        public bool IsSearching => _isSearching;
        
        /// <summary>
        /// Current join code (if hosting or joined a match).
        /// </summary>
        public string? CurrentJoinCode => _currentJoinCode;
        
        /// <summary>
        /// Whether Unity Relay service is enabled and available.
        /// </summary>
        public bool IsRelayEnabled => useRelayService;

        public event Action<CoreMatchmakingState>? OnMatchmakingStateChanged;

        public CoreMatchmakingState CurrentState { get; private set; } = CoreMatchmakingState.Idle;

        #endregion

        #region Unity Override Methods

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize services
            InitializeServices();

            await InitializeUnityServicesAsync();
        }

        private void Update()
        {
            if (_isSearching)
            {
                _timer += Time.deltaTime;
                if (_timer >= matchmakingTimeout)
                {
                    CancelMatchmaking();
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the matchmaking process.
        /// </summary>
        public async void StartMatchmaking()
        {
            if (_isSearching) return;

            cts = new CancellationTokenSource();

            SetState(CoreMatchmakingState.Searching);
            _eventBus?.Publish(new MatchmakingStartedEvent(matchmakingTimeout));

            try
            {
                if (useRelayService)
                {
                    // Relay-based matchmaking
                    await StartRelayMatchmakingAsync();
                }
                else
                {
                    // Direct connection matchmaking (fallback)
                    await StartDirectMatchmakingAsync();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Matchmaking cancelled.");
                _eventBus?.Publish(new MatchmakingCancelledEvent("User cancelled", true));
                SetState(CoreMatchmakingState.Idle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Matchmaking failed: {ex}");
                _eventBus?.Publish(new MatchmakingFailedEvent(ex.Message, ex));
                SetState(CoreMatchmakingState.Failed);
            }
        }

        /// <summary>
        /// Cancels the matchmaking process.
        /// </summary>
        public void CancelMatchmaking()
        {
            if (!_isSearching) return;

            cts.Cancel();
            
            _eventBus?.Publish(new MatchmakingCancelledEvent("User cancelled matchmaking", true));
            SetState(CoreMatchmakingState.Idle);
        }

        /// <summary>
        /// Called when a match is found.
        /// </summary>
        public void OnMatchFound()
        {
            _isSearching = false;
            _timer = 0f;
            // Add logic to transition to game, notify players, etc.
        }

        /// <summary>
        /// Joins a match using the provided join code.
        /// </summary>
        public async UniTask JoinMatch(string joinCode)
        {
            if (CurrentState != CoreMatchmakingState.Idle) return;

            cts = new CancellationTokenSource();

            SetState(CoreMatchmakingState.Searching);
            _eventBus?.Publish(new MatchJoinAttemptEvent(joinCode));

            try
            {
                if (useRelayService)
                {
                    await JoinRelayMatchAsync(joinCode);
                }
                else
                {
                    await JoinDirectMatchAsync(joinCode);
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Join matchmaking cancelled.");
                _eventBus?.Publish(new MatchmakingCancelledEvent("Join cancelled", true));
                SetState(CoreMatchmakingState.Idle);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Join failed: {ex}");
                _eventBus?.Publish(new MatchmakingFailedEvent($"Failed to join match: {ex.Message}", ex));
                SetState(CoreMatchmakingState.Failed);
            }
        }

        #endregion

        #region Private Methods

        private void SetState(CoreMatchmakingState state)
        {
            var previousState = CurrentState;
            CurrentState = state;
            
            // Update internal flags
            _isSearching = (state == CoreMatchmakingState.Searching);
            _timer = _isSearching ? 0f : _timer;
            
            // Publish events
            OnMatchmakingStateChanged?.Invoke(state);
            _eventBus?.Publish(new MatchmakingStateChangedEvent(previousState, state));
            
            Debug.Log($"[MatchmakingManager] State changed: {previousState} -> {state}");
        }

        /// <summary>
        /// Starts Relay-based matchmaking by creating an allocation.
        /// </summary>
        private async UniTask StartRelayMatchmakingAsync()
        {
            Debug.Log("[MatchmakingManager] Starting Relay-based matchmaking...");

            try
            {
#if UNITY_SERVICES_RELAY
                // Create allocation for max players
                var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayersPerMatch - 1);
                _currentAllocationId = allocation.AllocationId;

                // Get join code for other players
                _currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
#else
                // Fallback when Unity Services are not available
                Debug.LogWarning("[MatchmakingManager] Unity Relay Services not available. Using fallback networking.");
                _currentAllocationId = System.Guid.NewGuid().ToString();
                _currentJoinCode = "FALLBACK_" + UnityEngine.Random.Range(1000, 9999).ToString();

                // Simulate async delay for consistency with the real Relay implementation
                await UniTask.Delay(100);

                // Create a mock allocation object for the rest of the code
                var allocation = new MockRelayAllocation
                {
                    AllocationId = _currentAllocationId,
                    RelayServer = new MockRelayServer { IpV4 = fallbackServerAddress, Port = fallbackServerPort },
                    AllocationIdBytes = System.Text.Encoding.UTF8.GetBytes(_currentAllocationId),
                    Key = new byte[32],
                    ConnectionData = new byte[16]
                };
#endif
                
                Debug.Log($"[MatchmakingManager] Relay allocation created. Join code: {_currentJoinCode}");
                
                // Publish event
                _eventBus?.Publish(new RelayAllocationCreatedEvent(
                    _currentAllocationId, 
                    _currentJoinCode, 
                    maxPlayersPerMatch));
                
                // Configure Unity Transport with Relay data
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(
                    allocation.RelayServer.IpV4,
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes,
                    allocation.Key,
                    allocation.ConnectionData);
                
                SetState(CoreMatchmakingState.MatchFound);
                _eventBus?.Publish(new MatchFoundEvent(_currentJoinCode, true));
                
                // Start hosting
                if (!NetworkManager.Singleton.StartHost())
                {
                    throw new Exception("Failed to start host with Relay");
                }
                
                Debug.Log("[MatchmakingManager] Host started with Relay successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingManager] Relay matchmaking failed: {ex.Message}");
                throw;
            }
            
        }

        /// <summary>
        /// Starts direct connection matchmaking (fallback when Relay is not available).
        /// </summary>
        private async UniTask StartDirectMatchmakingAsync()
        {
            Debug.Log("[MatchmakingManager] Starting direct connection matchmaking...");
            
            // Simulate matchmaking delay for realistic behavior
            await UniTask.Delay(1000, cancellationToken: cts.Token);
            
            // Generate a simple join code (IP:Port format or random code)
            _currentJoinCode = GenerateDirectJoinCode();
            
            Debug.Log($"[MatchmakingManager] Direct connection match created. Join info: {_currentJoinCode}");
            
            // Configure Unity Transport for direct connection
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            
            // Try to set connection data with fallback for missing packages
            try
            {
                // Use reflection to avoid compilation errors when packages are missing
                var setConnectionDataMethod = transport.GetType().GetMethod("SetConnectionData", new[] { typeof(string), typeof(ushort) });
                if (setConnectionDataMethod != null)
                {
                    setConnectionDataMethod.Invoke(transport, new object[] { fallbackServerAddress, fallbackServerPort });
                }
                else
                {
                    Debug.LogWarning("[MatchmakingManager] SetConnectionData method not found - using basic transport configuration");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchmakingManager] Failed to configure transport: {ex.Message}");
            }
            
            SetState(CoreMatchmakingState.MatchFound);
            _eventBus?.Publish(new MatchFoundEvent(_currentJoinCode, true, fallbackServerAddress, fallbackServerPort));
            
            // Start hosting
            if (!NetworkManager.Singleton.StartHost())
            {
                throw new Exception("Failed to start host with direct connection");
            }
            
            Debug.Log("[MatchmakingManager] Host started with direct connection successfully");
        }

        /// <summary>
        /// Joins a Relay-based match using a join code.
        /// </summary>
        private async UniTask JoinRelayMatchAsync(string joinCode)
        {
            Debug.Log($"[MatchmakingManager] Joining Relay match with code: {joinCode}");

            try
            {
#if UNITY_SERVICES_RELAY
                // Join allocation using the join code
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                Debug.Log($"[MatchmakingManager] Successfully joined Relay allocation");

                // Configure Unity Transport with Relay data
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(
                    joinAllocation.RelayServer.IpV4,
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes,
                    joinAllocation.Key,
                    joinAllocation.ConnectionData);

                _currentJoinCode = joinCode;

                SetState(CoreMatchmakingState.MatchFound);
                _eventBus?.Publish(new MatchFoundEvent(joinCode, false));

                // Start as client
                if (!NetworkManager.Singleton.StartClient())
                {
                    throw new Exception("Failed to start client with Relay");
                }

                _eventBus?.Publish(new MatchJoinedEvent(joinCode, "Relay Server"));
                Debug.Log("[MatchmakingManager] Client started with Relay successfully");
#else
                // Fallback implementation when Unity Services packages are not available
                Debug.LogWarning("[MatchmakingManager] Unity Relay packages not available. Using fallback direct connection.");
                await JoinDirectMatchAsync(joinCode);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingManager] Relay join failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Joins a direct connection match using connection info.
        /// </summary>
        private async UniTask JoinDirectMatchAsync(string connectionInfo)
        {
            Debug.Log($"[MatchmakingManager] Joining direct match with info: {connectionInfo}");
            
            // Parse connection info (could be IP:Port or encoded connection data)
            var (host, port) = ParseDirectConnectionInfo(connectionInfo);
            
            // Simulate connection delay
            await UniTask.Delay(500, cancellationToken: cts.Token);
            
            // Configure Unity Transport for direct connection
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            
            // Try to set connection data with fallback for missing packages
            try
            {
                // Use reflection to avoid compilation errors when packages are missing
                var setConnectionDataMethod = transport.GetType().GetMethod("SetConnectionData", new[] { typeof(string), typeof(ushort) });
                if (setConnectionDataMethod != null)
                {
                    setConnectionDataMethod.Invoke(transport, new object[] { host, port });
                }
                else
                {
                    Debug.LogWarning("[MatchmakingManager] SetConnectionData method not found - using basic transport configuration");
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchmakingManager] Failed to configure transport: {ex.Message}");
            }
            
            _currentJoinCode = connectionInfo;
            
            SetState(CoreMatchmakingState.MatchFound);
            _eventBus?.Publish(new MatchFoundEvent(connectionInfo, false, host, port));
            
            // Start as client
            if (!NetworkManager.Singleton.StartClient())
            {
                throw new Exception("Failed to start client with direct connection");
            }
            
            _eventBus?.Publish(new MatchJoinedEvent(connectionInfo, $"{host}:{port}"));
            Debug.Log($"[MatchmakingManager] Client started successfully, connecting to {host}:{port}");
        }

        /// <summary>
        /// Initializes required services from the global service provider.
        /// </summary>
        private void InitializeServices()
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
                _networkService = serviceContainer.ResolveService<INetworkService>();

                if (_eventBus == null)
                    Debug.LogWarning("[MatchmakingManager] EventBus service not found. Events will not be published.");
                if (_networkService == null)
                    Debug.LogWarning("[MatchmakingManager] NetworkService not found. Some features may be limited.");
            }
            else
            {
                Debug.LogWarning("[MatchmakingManager] ServiceContainer not initialized. Some features will be limited.");
            }
        }

        /// <summary>
        /// Generates a join code for direct connection matchmaking.
        /// </summary>
        private string GenerateDirectJoinCode()
        {
            // For direct connection, use IP:Port format or generate a simple code
            // In production, you might want to use a more sophisticated system
            return $"{fallbackServerAddress}:{fallbackServerPort}";
        }

        /// <summary>
        /// Parses direct connection info to extract host and port.
        /// </summary>
        private (string host, ushort port) ParseDirectConnectionInfo(string connectionInfo)
        {
            try
            {
                // Try to parse as IP:Port format
                var parts = connectionInfo.Split(':');
                if (parts.Length == 2 && ushort.TryParse(parts[1], out var port))
                {
                    return (parts[0], port);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MatchmakingManager] Failed to parse connection info '{connectionInfo}': {ex.Message}");
            }
            
            // Fallback to default connection settings
            Debug.LogWarning($"[MatchmakingManager] Using fallback connection: {fallbackServerAddress}:{fallbackServerPort}");
            return (fallbackServerAddress, fallbackServerPort);
        }

        private async UniTask InitializeUnityServicesAsync()
        {
            Debug.Log("[MatchmakingManager] Initializing Unity Services...");

            try
            {
#if UNITY_SERVICES_CORE
                if (!UnityServices.State.Equals(ServicesInitializationState.Initialized))
                {
                    await UnityServices.InitializeAsync();
                    Debug.Log("[MatchmakingManager] Unity Services initialized successfully");
                }

                // Sign in anonymously if not already signed in
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                    Debug.Log($"[MatchmakingManager] Signed in anonymously. Player ID: {AuthenticationService.Instance.PlayerId}");
                }

                _eventBus?.Publish(new SystemInitializedEvent("UnityServices"));
#else
                // Fallback when packages are not available
                Debug.LogWarning("[MatchmakingManager] Unity Services packages not available. Running in offline mode.");
                useRelayService = false; // Force disable Relay when services unavailable

                _eventBus?.Publish(new SystemInitializedEvent("MatchmakingManager-OfflineMode"));
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingManager] Failed to initialize Unity Services: {ex.Message}");
                Debug.LogWarning("[MatchmakingManager] Falling back to direct connection mode.");

                useRelayService = false; // Disable Relay on initialization failure
                _eventBus?.Publish(new SystemErrorEvent("UnityServices", ex.Message));
            }

            await UniTask.Yield(); // Make this truly async
        }

        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Gets current matchmaking status information.
        /// </summary>
        public MatchmakingStatus GetStatus()
        {
            return new MatchmakingStatus
            {
                State = CurrentState,
                IsSearching = _isSearching,
                JoinCode = _currentJoinCode,
                TimeRemaining = _isSearching ? Mathf.Max(0, matchmakingTimeout - _timer) : 0f,
                UseRelay = useRelayService
            };
        }
        
        /// <summary>
        /// Forces cleanup of current matchmaking session.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                CancelMatchmaking();
                
                if (NetworkManager.Singleton != null)
                {
                    if (NetworkManager.Singleton.IsHost)
                        NetworkManager.Singleton.Shutdown();
                    else if (NetworkManager.Singleton.IsClient)
                        NetworkManager.Singleton.Shutdown();
                }
                
                _currentJoinCode = null;
                // _currentAllocationId = null; // Only used when Relay service is available
                
                Debug.Log("[MatchmakingManager] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MatchmakingManager] Error during cleanup: {ex.Message}");
            }
        }
        
        #endregion
    }
    
    #region Helper Classes

    /// <summary>
    /// Status information for matchmaking operations.
    /// </summary>
    [System.Serializable]
    public class MatchmakingStatus
    {
        public CoreMatchmakingState State;
        public bool IsSearching;
        public string? JoinCode;
        public float TimeRemaining;
        public bool UseRelay;
    }

#if !UNITY_SERVICES_RELAY
    /// <summary>
    /// Mock allocation class for fallback when Unity Relay Services are not available.
    /// </summary>
    public class MockRelayAllocation
    {
        public string AllocationId { get; set; } = string.Empty;
        public MockRelayServer RelayServer { get; set; } = new MockRelayServer();
        public byte[] AllocationIdBytes { get; set; } = new byte[16];
        public byte[] Key { get; set; } = new byte[32];
        public byte[] ConnectionData { get; set; } = new byte[16];
    }

    /// <summary>
    /// Mock relay server class for fallback when Unity Relay Services are not available.
    /// </summary>
    public class MockRelayServer
    {
        public string IpV4 { get; set; } = "127.0.0.1";
        public ushort Port { get; set; } = 7777;
    }
#endif

    #endregion
}
