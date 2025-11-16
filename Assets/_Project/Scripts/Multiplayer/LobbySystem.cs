using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Multiplayer
{
    /// <summary>
    /// Lobby system for pre-match player gathering.
    /// Handles lobby creation, joining, player ready states, and match start.
    /// Supports public and private lobbies with customization.
    /// </summary>
    public class LobbySystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string createEndpoint = "/lobbies/create";
        [SerializeField] private string joinEndpoint = "/lobbies/join";
        [SerializeField] private string leaveEndpoint = "/lobbies/leave";
        [SerializeField] private string listEndpoint = "/lobbies/list";
        [SerializeField] private string updateEndpoint = "/lobbies/update";

        [Header("Lobby Settings")]
        [SerializeField] private int defaultMaxPlayers = 8;
        [SerializeField] private float lobbyUpdateInterval = 1f;
        [SerializeField] private bool autoStartWhenReady = true;

        [Header("Timeouts")]
        [SerializeField] private float playerReadyTimeout = 60f;
        [SerializeField] private float lobbyInactiveTimeout = 300f; // 5 minutes

        #endregion

        #region Private Fields

        private static LobbySystem _instance;

        // Lobby state
        private Lobby _currentLobby;
        private bool _isInLobby = false;
        private bool _isHost = false;
        private float _lastUpdateTime = 0f;

        // Statistics
        private int _totalLobbiesCreated = 0;
        private int _totalLobbiesJoined = 0;
        private int _totalLobbiesLeft = 0;

        // Events
        public event Action<Lobby> OnLobbyCreated;
        public event Action<Lobby> OnLobbyJoined;
        public event Action OnLobbyLeft;
        public event Action<Lobby> OnLobbyUpdated;
        public event Action<LobbyPlayer> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<string, bool> OnPlayerReadyChanged;
        public event Action OnMatchStarting;
        public event Action<string> OnLobbyError;

        #endregion

        #region Properties

        public static LobbySystem Instance => _instance;
        public bool IsInLobby => _isInLobby;
        public bool IsHost => _isHost;
        public Lobby CurrentLobby => _currentLobby;
        public int PlayerCount => _currentLobby?.players.Count ?? 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Debug.Log("[LobbySystem] Initialized");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!_isInLobby) return;

            // Auto-update lobby state
            if (Time.time - _lastUpdateTime >= lobbyUpdateInterval)
            {
                UpdateLobby();
            }

            // Check for player ready timeout
            if (_isHost && _currentLobby != null)
            {
                CheckPlayerReadyTimeouts();
            }

            // Check for lobby inactivity timeout
            if (_currentLobby != null)
            {
                CheckLobbyInactivityTimeout();
            }

            // Auto-start if all players ready
            if (autoStartWhenReady && _isHost && AllPlayersReady())
            {
                StartMatch();
            }
        }

        private void OnApplicationQuit()
        {
            if (_isInLobby)
            {
                LeaveLobby();
            }
        }

        #endregion

        #region Lobby Creation

        /// <summary>
        /// Create a new lobby.
        /// </summary>
        public void CreateLobby(string lobbyName, int maxPlayers = 0, LobbyVisibility visibility = LobbyVisibility.Public, Dictionary<string, string> settings = null, Action<Lobby> onSuccess = null, Action<string> onError = null)
        {
            if (_isInLobby)
            {
                string error = "Already in a lobby";
                OnLobbyError?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            if (Backend.UserAuthenticationSystem.Instance == null || !Backend.UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                string error = "User not authenticated";
                OnLobbyError?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            if (maxPlayers <= 0)
                maxPlayers = defaultMaxPlayers;

            StartCoroutine(CreateLobbyCoroutine(lobbyName, maxPlayers, visibility, settings, onSuccess, onError));
        }

        private IEnumerator CreateLobbyCoroutine(string lobbyName, int maxPlayers, LobbyVisibility visibility, Dictionary<string, string> settings, Action<Lobby> onSuccess, Action<string> onError)
        {
            var requestData = new CreateLobbyRequest
            {
                lobbyName = lobbyName,
                hostId = Backend.UserAuthenticationSystem.Instance.UserId,
                maxPlayers = maxPlayers,
                visibility = visibility,
                settings = settings ?? new Dictionary<string, string>(),
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + createEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LobbyResponse>(request.downloadHandler.text);

                        _currentLobby = response.lobby;
                        _isInLobby = true;
                        _isHost = true;
                        _totalLobbiesCreated++;

                        OnLobbyCreated?.Invoke(_currentLobby);
                        onSuccess?.Invoke(_currentLobby);

                        Debug.Log($"[LobbySystem] Lobby created: {_currentLobby.lobbyName} ({_currentLobby.lobbyId})");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse lobby response: {ex.Message}";
                        OnLobbyError?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Failed to create lobby: {request.error}";
                    OnLobbyError?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[LobbySystem] {error}");
                }
            }
        }

        #endregion

        #region Lobby Joining

        /// <summary>
        /// Join a lobby by ID.
        /// </summary>
        public void JoinLobby(string lobbyId, string password = null, Action<Lobby> onSuccess = null, Action<string> onError = null)
        {
            if (_isInLobby)
            {
                string error = "Already in a lobby";
                OnLobbyError?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            if (Backend.UserAuthenticationSystem.Instance == null || !Backend.UserAuthenticationSystem.Instance.IsAuthenticated)
            {
                string error = "User not authenticated";
                OnLobbyError?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            StartCoroutine(JoinLobbyCoroutine(lobbyId, password, onSuccess, onError));
        }

        private IEnumerator JoinLobbyCoroutine(string lobbyId, string password, Action<Lobby> onSuccess, Action<string> onError)
        {
            var requestData = new JoinLobbyRequest
            {
                lobbyId = lobbyId,
                userId = Backend.UserAuthenticationSystem.Instance.UserId,
                username = Backend.UserAuthenticationSystem.Instance.CurrentUser?.username ?? "Player",
                password = password,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + joinEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LobbyResponse>(request.downloadHandler.text);

                        _currentLobby = response.lobby;
                        _isInLobby = true;
                        _isHost = _currentLobby.hostId == Backend.UserAuthenticationSystem.Instance.UserId;
                        _totalLobbiesJoined++;

                        OnLobbyJoined?.Invoke(_currentLobby);
                        onSuccess?.Invoke(_currentLobby);

                        Debug.Log($"[LobbySystem] Joined lobby: {_currentLobby.lobbyName} ({_currentLobby.lobbyId})");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse lobby response: {ex.Message}";
                        OnLobbyError?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Failed to join lobby: {request.error}";
                    OnLobbyError?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[LobbySystem] {error}");
                }
            }
        }

        /// <summary>
        /// List available lobbies.
        /// </summary>
        public void ListLobbies(Action<Lobby[]> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(ListLobbiesCoroutine(onSuccess, onError));
        }

        private IEnumerator ListLobbiesCoroutine(Action<Lobby[]> onSuccess, Action<string> onError)
        {
            string url = backendUrl + listEndpoint;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                if (Backend.UserAuthenticationSystem.Instance != null && Backend.UserAuthenticationSystem.Instance.IsAuthenticated)
                {
                    request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                }

                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LobbyListResponse>(request.downloadHandler.text);

                        onSuccess?.Invoke(response.lobbies);

                        Debug.Log($"[LobbySystem] Found {response.lobbies.Length} lobbies");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse lobby list: {ex.Message}";
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Failed to list lobbies: {request.error}";
                    onError?.Invoke(error);
                    Debug.LogError($"[LobbySystem] {error}");
                }
            }
        }

        #endregion

        #region Lobby Management

        /// <summary>
        /// Leave the current lobby.
        /// </summary>
        public void LeaveLobby()
        {
            if (!_isInLobby)
            {
                Debug.LogWarning("[LobbySystem] Not in a lobby");
                return;
            }

            StartCoroutine(LeaveLobbyCoroutine());
        }

        private IEnumerator LeaveLobbyCoroutine()
        {
            var requestData = new LeaveLobbyRequest
            {
                lobbyId = _currentLobby.lobbyId,
                userId = Backend.UserAuthenticationSystem.Instance.UserId,
                timestamp = DateTime.UtcNow
            };

            string json = JsonUtility.ToJson(requestData);
            string url = backendUrl + leaveEndpoint;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 5;

                yield return request.SendWebRequest();

                _isInLobby = false;
                _isHost = false;
                _currentLobby = null;
                _totalLobbiesLeft++;

                OnLobbyLeft?.Invoke();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[LobbySystem] Left lobby");
                }
                else
                {
                    Debug.LogWarning($"[LobbySystem] Leave request failed: {request.error}");
                }
            }
        }

        /// <summary>
        /// Update lobby state from server.
        /// </summary>
        private void UpdateLobby()
        {
            if (!_isInLobby) return;

            StartCoroutine(UpdateLobbyCoroutine());
        }

        private IEnumerator UpdateLobbyCoroutine()
        {
            _lastUpdateTime = Time.time;

            string url = $"{backendUrl}{updateEndpoint}?lobbyId={_currentLobby.lobbyId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {Backend.UserAuthenticationSystem.Instance.AuthToken}");
                request.timeout = 5;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LobbyResponse>(request.downloadHandler.text);

                        // Check for new players
                        if (_currentLobby != null && _currentLobby.players != null)
                        {
                            var oldPlayerIds = _currentLobby.players.Select(p => p.userId).ToHashSet();
                            foreach (var player in response.lobby.players)
                            {
                                if (!oldPlayerIds.Contains(player.userId))
                                {
                                    OnPlayerJoined?.Invoke(player);
                                    Debug.Log($"[LobbySystem] Player joined: {player.username}");
                                }
                            }
                        }

                        _currentLobby = response.lobby;

                        OnLobbyUpdated?.Invoke(_currentLobby);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[LobbySystem] Failed to parse lobby update: {ex.Message}");
                    }
                }
            }
        }

        #endregion

        #region Player Ready States

        /// <summary>
        /// Set local player ready state.
        /// </summary>
        public void SetPlayerReady(bool ready)
        {
            if (!_isInLobby)
            {
                Debug.LogWarning("[LobbySystem] Not in a lobby");
                return;
            }

            string userId = Backend.UserAuthenticationSystem.Instance.UserId;

            // Update local state
            var player = _currentLobby.players.FirstOrDefault(p => p.userId == userId);
            if (player != null)
            {
                player.isReady = ready;

                OnPlayerReadyChanged?.Invoke(userId, ready);

                // Notify backend (implement if needed)
                Debug.Log($"[LobbySystem] Player ready state: {ready}");
            }
        }

        /// <summary>
        /// Check if all players are ready.
        /// </summary>
        public bool AllPlayersReady()
        {
            if (!_isInLobby || _currentLobby.players.Count == 0)
                return false;

            return _currentLobby.players.All(p => p.isReady);
        }

        /// <summary>
        /// Check for players who have not marked ready within the timeout period.
        /// </summary>
        private void CheckPlayerReadyTimeouts()
        {
            if (_currentLobby == null || _currentLobby.players == null)
                return;

            var playersToKick = new List<LobbyPlayer>();

            foreach (var player in _currentLobby.players)
            {
                if (!player.isReady)
                {
                    float timeSinceJoin = (float)(DateTime.UtcNow - player.joinedAt).TotalSeconds;
                    if (timeSinceJoin >= playerReadyTimeout)
                    {
                        playersToKick.Add(player);
                        Debug.LogWarning($"[LobbySystem] Player {player.username} timed out (not ready after {playerReadyTimeout}s)");
                    }
                }
            }

            // Kick timed out players (host action)
            foreach (var player in playersToKick)
            {
                _currentLobby.players.Remove(player);
                OnPlayerLeft?.Invoke(player.userId);
                Debug.Log($"[LobbySystem] Kicked player {player.username} for ready timeout");
            }
        }

        /// <summary>
        /// Check if the lobby has been inactive for too long.
        /// </summary>
        private void CheckLobbyInactivityTimeout()
        {
            if (_currentLobby == null)
                return;

            float lobbyAge = (float)(DateTime.UtcNow - _currentLobby.createdAt).TotalSeconds;

            // Check if no players are ready after the inactivity timeout
            if (lobbyAge >= lobbyInactiveTimeout && _currentLobby.players.All(p => !p.isReady))
            {
                Debug.LogWarning($"[LobbySystem] Lobby inactive for {lobbyInactiveTimeout}s, no players ready. Leaving lobby.");
                LeaveLobby();
            }
        }

        #endregion

        #region Match Start

        /// <summary>
        /// Start the match (host only).
        /// </summary>
        public void StartMatch()
        {
            if (!_isHost)
            {
                Debug.LogWarning("[LobbySystem] Only host can start match");
                return;
            }

            if (!AllPlayersReady())
            {
                Debug.LogWarning("[LobbySystem] Not all players ready");
                return;
            }

            OnMatchStarting?.Invoke();

            Debug.Log("[LobbySystem] Starting match...");

            // Implement actual match start logic here
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get lobby statistics.
        /// </summary>
        public LobbyStats GetStats()
        {
            return new LobbyStats
            {
                totalLobbiesCreated = _totalLobbiesCreated,
                totalLobbiesJoined = _totalLobbiesJoined,
                totalLobbiesLeft = _totalLobbiesLeft,
                isInLobby = _isInLobby,
                isHost = _isHost,
                currentPlayerCount = PlayerCount
            };
        }

        /// <summary>
        /// Quick join first available lobby.
        /// </summary>
        public void QuickJoin(Action<Lobby> onSuccess = null, Action<string> onError = null)
        {
            ListLobbies(
                lobbies =>
                {
                    if (lobbies.Length > 0)
                    {
                        JoinLobby(lobbies[0].lobbyId, null, onSuccess, onError);
                    }
                    else
                    {
                        onError?.Invoke("No available lobbies");
                    }
                },
                onError
            );
        }

        #endregion

        #region Context Menu

        [ContextMenu("Create Test Lobby")]
        private void CreateTestLobby()
        {
            CreateLobby("Test Lobby", 8);
        }

        [ContextMenu("List Lobbies")]
        private void ListLobbiesMenu()
        {
            ListLobbies();
        }

        [ContextMenu("Leave Lobby")]
        private void LeaveLobbyMenu()
        {
            LeaveLobby();
        }

        [ContextMenu("Set Ready")]
        private void SetReadyMenu()
        {
            SetPlayerReady(true);
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Lobby Statistics ===\n" +
                      $"Lobbies Created: {stats.totalLobbiesCreated}\n" +
                      $"Lobbies Joined: {stats.totalLobbiesJoined}\n" +
                      $"Lobbies Left: {stats.totalLobbiesLeft}\n" +
                      $"In Lobby: {stats.isInLobby}\n" +
                      $"Is Host: {stats.isHost}\n" +
                      $"Current Players: {stats.currentPlayerCount}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Lobby data.
    /// </summary>
    [Serializable]
    public class Lobby
    {
        public string lobbyId;
        public string lobbyName;
        public string hostId;
        public int maxPlayers;
        public LobbyVisibility visibility;
        public List<LobbyPlayer> players = new List<LobbyPlayer>();
        public Dictionary<string, string> settings;
        public DateTime createdAt;
    }

    /// <summary>
    /// Lobby player data.
    /// </summary>
    [Serializable]
    public class LobbyPlayer
    {
        public string userId;
        public string username;
        public bool isReady;
        public DateTime joinedAt;
    }

    /// <summary>
    /// Lobby statistics.
    /// </summary>
    [Serializable]
    public struct LobbyStats
    {
        public int totalLobbiesCreated;
        public int totalLobbiesJoined;
        public int totalLobbiesLeft;
        public bool isInLobby;
        public bool isHost;
        public int currentPlayerCount;
    }

    // Request/Response structures
    [Serializable] class CreateLobbyRequest { public string lobbyName; public string hostId; public int maxPlayers; public LobbyVisibility visibility; public Dictionary<string, string> settings; public DateTime timestamp; }
    [Serializable] class JoinLobbyRequest { public string lobbyId; public string userId; public string username; public string password; public DateTime timestamp; }
    [Serializable] class LeaveLobbyRequest { public string lobbyId; public string userId; public DateTime timestamp; }
    [Serializable] class LobbyResponse { public Lobby lobby; }
    [Serializable] class LobbyListResponse { public Lobby[] lobbies; }

    /// <summary>
    /// Lobby visibility types.
    /// </summary>
    public enum LobbyVisibility
    {
        Public,
        Private,
        FriendsOnly
    }

    #endregion
}
