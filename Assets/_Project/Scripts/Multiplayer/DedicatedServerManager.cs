using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Multiplayer
{
    /// <summary>
    /// Dedicated server architecture manager.
    /// Handles server lifecycle, health monitoring, and backend communication.
    /// Optimized for headless server deployment.
    /// </summary>
    public class DedicatedServerManager : MonoBehaviour
    {
        #region Configuration

        [Header("Server Settings")]
        [SerializeField] private string serverName = "Project Chimera Server";
        [SerializeField] private string serverId;
        [SerializeField] private int maxPlayers = 32;
        [SerializeField] private int port = 7777;
        [SerializeField] private bool isHeadless = false;

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string registerEndpoint = "/servers/register";
        [SerializeField] private string heartbeatEndpoint = "/servers/heartbeat";
        [SerializeField] private string shutdownEndpoint = "/servers/shutdown";

        [Header("Health Monitoring")]
        [SerializeField] private bool enableHealthChecks = true;
        [SerializeField] private float heartbeatInterval = 30f; // 30 seconds
        [SerializeField] private float healthCheckInterval = 5f;

        [Header("Auto-Shutdown")]
        [SerializeField] private bool enableAutoShutdown = true;
        [SerializeField] private float idleShutdownTime = 600f; // 10 minutes
        [SerializeField] private bool shutdownWhenEmpty = true;

        #endregion

        #region Private Fields

        private static DedicatedServerManager _instance;

        // Server state
        private bool _isServerRunning = false;
        private DateTime _serverStartTime;
        private float _lastHeartbeatTime = 0f;
        private float _lastHealthCheckTime = 0f;
        private float _lastPlayerActivityTime = 0f;

        // Connected players
        private readonly Dictionary<string, PlayerConnection> _connectedPlayers = new Dictionary<string, PlayerConnection>();

        // Health metrics
        private ServerHealth _currentHealth;

        // Statistics
        private int _totalConnections = 0;
        private int _totalDisconnections = 0;
        private long _totalBytesReceived = 0;
        private long _totalBytesSent = 0;

        // Events
        public event Action OnServerStarted;
        public event Action OnServerStopped;
        public event Action<PlayerConnection> OnPlayerConnected;
        public event Action<string> OnPlayerDisconnected;
        public event Action<ServerHealth> OnHealthUpdated;

        #endregion

        #region Properties

        public static DedicatedServerManager Instance => _instance;
        public bool IsServerRunning => _isServerRunning;
        public int ConnectedPlayerCount => _connectedPlayers.Count;
        public float Uptime => _isServerRunning ? (float)(DateTime.UtcNow - _serverStartTime).TotalSeconds : 0f;
        public string ServerId => serverId;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Auto-start if headless or command line arg
            if (isHeadless || SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                StartServer();
            }
        }

        private void Update()
        {
            if (!_isServerRunning) return;

            // Heartbeat
            if (enableHealthChecks && Time.time - _lastHeartbeatTime >= heartbeatInterval)
            {
                SendHeartbeat();
            }

            // Health check
            if (enableHealthChecks && Time.time - _lastHealthCheckTime >= healthCheckInterval)
            {
                UpdateHealth();
            }

            // Auto-shutdown checks
            if (enableAutoShutdown)
            {
                CheckAutoShutdown();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isServerRunning)
            {
                StopServer();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            UnityEngine.Debug.Log("[DedicatedServerManager] Initializing...");

            // Generate server ID if not set
            if (string.IsNullOrEmpty(serverId))
            {
                serverId = $"server_{Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            // Detect headless mode
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                isHeadless = true;
                UnityEngine.Debug.Log("[DedicatedServerManager] Running in headless mode");
            }

            // Optimize for headless
            if (isHeadless)
            {
                Application.targetFrameRate = 60;
                QualitySettings.vSyncCount = 0;
            }

            UnityEngine.Debug.Log($"[DedicatedServerManager] Initialized: {serverId}");
        }

        #endregion

        #region Server Lifecycle

        /// <summary>
        /// Start the dedicated server.
        /// </summary>
        public void StartServer()
        {
            if (_isServerRunning)
            {
                UnityEngine.Debug.LogWarning("[DedicatedServerManager] Server already running");
                return;
            }

            _serverStartTime = DateTime.UtcNow;
            _isServerRunning = true;
            _lastPlayerActivityTime = Time.time;

            // Initialize health
            _currentHealth = new ServerHealth
            {
                serverId = serverId,
                isHealthy = true,
                cpuUsage = 0f,
                memoryUsage = 0f,
                playerCount = 0
            };

            // Register with backend
            StartCoroutine(RegisterServer());

            OnServerStarted?.Invoke();

            UnityEngine.Debug.Log($"[DedicatedServerManager] Server started: {serverName} ({serverId})");
        }

        /// <summary>
        /// Stop the dedicated server.
        /// </summary>
        public void StopServer()
        {
            if (!_isServerRunning)
            {
                UnityEngine.Debug.LogWarning("[DedicatedServerManager] Server not running");
                return;
            }

            // Disconnect all players
            foreach (var player in _connectedPlayers.Values)
            {
                DisconnectPlayer(player.playerId, "Server shutting down");
            }

            _connectedPlayers.Clear();

            // Notify backend
            StartCoroutine(NotifyShutdown());

            _isServerRunning = false;

            OnServerStopped?.Invoke();

            UnityEngine.Debug.Log("[DedicatedServerManager] Server stopped");

            // Quit application if headless
            if (isHeadless)
            {
                Application.Quit();
            }
        }

        /// <summary>
        /// Restart the server.
        /// </summary>
        public void RestartServer()
        {
            StopServer();
            StartCoroutine(DelayedStartServer());
        }

        private IEnumerator DelayedStartServer()
        {
            yield return new WaitForSeconds(2f);
            StartServer();
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Register a player connection.
        /// </summary>
        public void RegisterPlayer(string playerId, string username, string ipAddress)
        {
            if (_connectedPlayers.ContainsKey(playerId))
            {
                UnityEngine.Debug.LogWarning($"[DedicatedServerManager] Player already connected: {playerId}");
                return;
            }

            var connection = new PlayerConnection
            {
                playerId = playerId,
                username = username,
                ipAddress = ipAddress,
                connectTime = DateTime.UtcNow,
                lastActivityTime = DateTime.UtcNow
            };

            _connectedPlayers[playerId] = connection;
            _totalConnections++;
            _lastPlayerActivityTime = Time.time;

            OnPlayerConnected?.Invoke(connection);

            UnityEngine.Debug.Log($"[DedicatedServerManager] Player connected: {username} ({playerId})");
        }

        /// <summary>
        /// Unregister a player connection.
        /// </summary>
        public void UnregisterPlayer(string playerId)
        {
            if (_connectedPlayers.Remove(playerId))
            {
                _totalDisconnections++;

                OnPlayerDisconnected?.Invoke(playerId);

                UnityEngine.Debug.Log($"[DedicatedServerManager] Player disconnected: {playerId}");
            }
        }

        /// <summary>
        /// Disconnect a player.
        /// </summary>
        public void DisconnectPlayer(string playerId, string reason)
        {
            if (_connectedPlayers.TryGetValue(playerId, out var player))
            {
                UnityEngine.Debug.Log($"[DedicatedServerManager] Disconnecting player: {player.username}, Reason: {reason}");

                // Implement actual disconnect logic here (depends on networking solution)

                UnregisterPlayer(playerId);
            }
        }

        /// <summary>
        /// Update player activity timestamp.
        /// </summary>
        public void UpdatePlayerActivity(string playerId)
        {
            if (_connectedPlayers.TryGetValue(playerId, out var player))
            {
                player.lastActivityTime = DateTime.UtcNow;
                _lastPlayerActivityTime = Time.time;
            }
        }

        #endregion

        #region Health Monitoring

        private void UpdateHealth()
        {
            _lastHealthCheckTime = Time.time;

            _currentHealth.timestamp = DateTime.UtcNow;
            _currentHealth.playerCount = _connectedPlayers.Count;
            _currentHealth.uptime = Uptime;
            _currentHealth.cpuUsage = GetCPUUsage();
            _currentHealth.memoryUsage = GetMemoryUsage();
            _currentHealth.fps = (int)(1f / Time.deltaTime);

            // Determine health status
            _currentHealth.isHealthy =
                _currentHealth.cpuUsage < 90f &&
                _currentHealth.memoryUsage < 90f &&
                _currentHealth.fps > 20;

            OnHealthUpdated?.Invoke(_currentHealth);
        }

        private float GetCPUUsage()
        {
            // Simplified CPU usage (actual implementation would use platform-specific APIs)
            return UnityEngine.Random.Range(10f, 50f);
        }

        private float GetMemoryUsage()
        {
            long totalMemory = 16L * 1024L * 1024L * 1024L; // Assume 16GB
            long usedMemory = GC.GetTotalMemory(false);

            return (usedMemory / (float)totalMemory) * 100f;
        }

        #endregion

        #region Backend Communication

        private IEnumerator RegisterServer()
        {
            var requestData = new ServerRegistrationRequest
            {
                serverId = serverId,
                serverName = serverName,
                maxPlayers = maxPlayers,
                port = port,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + registerEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.Log($"[DedicatedServerManager] Server registered with backend");
                }
                else
                {
                    UnityEngine.Debug.LogError($"[DedicatedServerManager] Server registration failed: {request.error}");
                }
            }
        }

        private void SendHeartbeat()
        {
            StartCoroutine(SendHeartbeatCoroutine());
        }

        private IEnumerator SendHeartbeatCoroutine()
        {
            _lastHeartbeatTime = Time.time;

            var requestData = new ServerHeartbeatRequest
            {
                serverId = serverId,
                health = _currentHealth,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + heartbeatEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 5;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.LogWarning($"[DedicatedServerManager] Heartbeat failed: {request.error}");
                }
            }
        }

        private IEnumerator NotifyShutdown()
        {
            var requestData = new ServerShutdownRequest
            {
                serverId = serverId,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + shutdownEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 5;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    UnityEngine.Debug.Log("[DedicatedServerManager] Shutdown notification sent");
                }
            }
        }

        #endregion

        #region Auto-Shutdown

        private void CheckAutoShutdown()
        {
            // Shutdown when empty
            if (shutdownWhenEmpty && _connectedPlayers.Count == 0)
            {
                float idleTime = Time.time - _lastPlayerActivityTime;

                if (idleTime >= idleShutdownTime)
                {
                    UnityEngine.Debug.Log($"[DedicatedServerManager] Auto-shutdown: Server idle for {idleTime:F0}s");
                    StopServer();
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get server statistics.
        /// </summary>
        public ServerStats GetStats()
        {
            return new ServerStats
            {
                serverId = serverId,
                serverName = serverName,
                isRunning = _isServerRunning,
                uptime = Uptime,
                connectedPlayers = _connectedPlayers.Count,
                maxPlayers = maxPlayers,
                totalConnections = _totalConnections,
                totalDisconnections = _totalDisconnections,
                totalBytesReceived = _totalBytesReceived,
                totalBytesSent = _totalBytesSent
            };
        }

        /// <summary>
        /// Get current server health.
        /// </summary>
        public ServerHealth GetHealth()
        {
            return _currentHealth;
        }

        /// <summary>
        /// Get connected players.
        /// </summary>
        public PlayerConnection[] GetConnectedPlayers()
        {
            var players = new PlayerConnection[_connectedPlayers.Count];
            _connectedPlayers.Values.CopyTo(players, 0);
            return players;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Start Server")]
        private void StartServerMenu()
        {
            StartServer();
        }

        [ContextMenu("Stop Server")]
        private void StopServerMenu()
        {
            StopServer();
        }

        [ContextMenu("Restart Server")]
        private void RestartServerMenu()
        {
            RestartServer();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            UnityEngine.Debug.Log($"=== Server Statistics ===\n" +
                      $"Server ID: {stats.serverId}\n" +
                      $"Server Name: {stats.serverName}\n" +
                      $"Running: {stats.isRunning}\n" +
                      $"Uptime: {stats.uptime / 3600f:F1}h\n" +
                      $"Players: {stats.connectedPlayers}/{stats.maxPlayers}\n" +
                      $"Total Connections: {stats.totalConnections}\n" +
                      $"Total Disconnections: {stats.totalDisconnections}");
        }

        [ContextMenu("Print Health")]
        private void PrintHealth()
        {
            UnityEngine.Debug.Log($"=== Server Health ===\n" +
                      $"Healthy: {_currentHealth.isHealthy}\n" +
                      $"CPU: {_currentHealth.cpuUsage:F1}%\n" +
                      $"Memory: {_currentHealth.memoryUsage:F1}%\n" +
                      $"FPS: {_currentHealth.fps}\n" +
                      $"Players: {_currentHealth.playerCount}\n" +
                      $"Uptime: {_currentHealth.uptime / 3600f:F1}h");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Player connection data.
    /// </summary>
    [Serializable]
    public class PlayerConnection
    {
        public string playerId;
        public string username;
        public string ipAddress;
        public DateTime connectTime;
        public DateTime lastActivityTime;
    }

    /// <summary>
    /// Server health metrics.
    /// </summary>
    [Serializable]
    public class ServerHealth
    {
        public string serverId;
        public DateTime timestamp;
        public bool isHealthy;
        public float cpuUsage;
        public float memoryUsage;
        public int playerCount;
        public float uptime;
        public int fps;
    }

    /// <summary>
    /// Server statistics.
    /// </summary>
    [Serializable]
    public struct ServerStats
    {
        public string serverId;
        public string serverName;
        public bool isRunning;
        public float uptime;
        public int connectedPlayers;
        public int maxPlayers;
        public int totalConnections;
        public int totalDisconnections;
        public long totalBytesReceived;
        public long totalBytesSent;
    }

    // Request structures
    [Serializable] class ServerRegistrationRequest { public string serverId; public string serverName; public int maxPlayers; public int port; public DateTime timestamp; }
    [Serializable] class ServerHeartbeatRequest { public string serverId; public ServerHealth health; public DateTime timestamp; }
    [Serializable] class ServerShutdownRequest { public string serverId; public DateTime timestamp; }

    #endregion
}
