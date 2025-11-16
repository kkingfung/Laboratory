using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Backend;

namespace Laboratory.Multiplayer
{
    /// <summary>
    /// Complete multiplayer session pipeline manager.
    /// Orchestrates: Authentication → Matchmaking → Lobby → Server Connection → Anti-Cheat
    /// Provides unified API for multiplayer flow with state management.
    /// </summary>
    public class MultiplayerSessionManager : MonoBehaviour
    {
        #region Configuration

        [Header("Auto-Initialize")]
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool requireAuthentication = true;

        [Header("Session Settings")]
        [SerializeField] private float sessionTimeout = 300f; // 5 minutes
        [SerializeField] private bool autoReconnect = true;
        [SerializeField] private int maxReconnectAttempts = 3;

        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        #endregion

        #region Private Fields

        private static MultiplayerSessionManager _instance;

        // Pipeline state
        private MultiplayerSessionState _currentState = MultiplayerSessionState.Disconnected;
        private SessionData _sessionData;

        // Reconnection
        private int _reconnectAttempts = 0;
        private float _lastStateChangeTime = 0f;

        // Statistics
        private int _totalSessionsStarted = 0;
        private int _totalSessionsCompleted = 0;
        private int _totalSessionsFailed = 0;
        private int _reconnectSuccesses = 0;

        // Events
        public event Action<MultiplayerSessionState> OnStateChanged;
        public event Action<SessionData> OnSessionStarted;
        public event Action<string> OnSessionFailed;
        public event Action OnSessionEnded;
        public event Action<string> OnPlayerKicked;
        public event Action<float> OnMatchmakingProgress;

        #endregion

        #region Properties

        public static MultiplayerSessionManager Instance => _instance;
        public MultiplayerSessionState CurrentState => _currentState;
        public SessionData CurrentSession => _sessionData;
        public bool IsInSession => _currentState >= MultiplayerSessionState.InLobby && _currentState <= MultiplayerSessionState.InGame;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                if (autoInitialize)
                {
                    Initialize();
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // Check for session timeout
            if (IsInSession && Time.time - _lastStateChangeTime > sessionTimeout)
            {
                LogWarning("Session timeout");
                EndSession("Session timeout");
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Log("Initializing Multiplayer Session Manager...");

            // Subscribe to system events
            SubscribeToEvents();

            ChangeState(MultiplayerSessionState.Disconnected);

            Log("Initialized");
        }

        private void SubscribeToEvents()
        {
            // Authentication events
            if (UserAuthenticationSystem.Instance != null)
            {
                UserAuthenticationSystem.Instance.OnLoginSuccess += HandleLoginSuccess;
                UserAuthenticationSystem.Instance.OnLogout += HandleLogout;
            }

            // Matchmaking events
            if (MatchmakingSystem.Instance != null)
            {
                MatchmakingSystem.Instance.OnMatchFound += HandleMatchFound;
                MatchmakingSystem.Instance.OnQueueCancelled += HandleQueueCancelled;
                MatchmakingSystem.Instance.OnMatchmakingFailed += HandleMatchmakingFailed;
                MatchmakingSystem.Instance.OnQueueTimeUpdated += OnMatchmakingProgress;
            }

            // Lobby events
            if (LobbySystem.Instance != null)
            {
                LobbySystem.Instance.OnLobbyJoined += HandleLobbyJoined;
                LobbySystem.Instance.OnLobbyLeft += HandleLobbyLeft;
                LobbySystem.Instance.OnMatchStarting += HandleMatchStarting;
                LobbySystem.Instance.OnLobbyError += HandleLobbyError;
            }

            // Server events
            if (DedicatedServerManager.Instance != null)
            {
                DedicatedServerManager.Instance.OnPlayerConnected += HandleServerPlayerConnected;
                DedicatedServerManager.Instance.OnPlayerDisconnected += HandleServerPlayerDisconnected;
            }

            // Anti-cheat events
            if (ServerSideAntiCheat.Instance != null)
            {
                ServerSideAntiCheat.Instance.OnPlayerKicked += HandlePlayerKicked;
                ServerSideAntiCheat.Instance.OnPlayerBanned += HandlePlayerBanned;
            }
        }

        #endregion

        #region Session Flow - Quick Play

        /// <summary>
        /// Start quick play session (full pipeline).
        /// </summary>
        public void StartQuickPlay(Action<SessionData> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(QuickPlayFlowCoroutine(onSuccess, onError));
        }

        private IEnumerator QuickPlayFlowCoroutine(Action<SessionData> onSuccess, Action<string> onError)
        {
            _totalSessionsStarted++;

            // Step 1: Ensure authenticated
            if (requireAuthentication && !IsAuthenticated())
            {
                string error = "User not authenticated";
                HandleSessionError(error);
                onError?.Invoke(error);
                yield break;
            }

            ChangeState(MultiplayerSessionState.Authenticating);

            // Step 2: Join matchmaking queue
            ChangeState(MultiplayerSessionState.Matchmaking);

            bool matchFound = false;
            bool matchFailed = false;
            string matchError = null;
            MatchInfo matchInfo = null;

            MatchmakingSystem.Instance.QuickPlay(
                match =>
                {
                    matchInfo = match;
                    matchFound = true;
                },
                error =>
                {
                    matchError = error;
                    matchFailed = true;
                });

            // Wait for match
            while (!matchFound && !matchFailed)
            {
                yield return null;
            }

            if (matchFailed)
            {
                HandleSessionError($"Matchmaking failed: {matchError}");
                onError?.Invoke(matchError);
                yield break;
            }

            // Step 3: Join lobby (if match has lobby)
            if (matchInfo.matchId != null)
            {
                ChangeState(MultiplayerSessionState.JoiningLobby);

                bool lobbyJoined = false;
                bool lobbyFailed = false;
                string lobbyError = null;

                // In a real implementation, matchInfo would contain lobby ID
                // For now, we'll simulate joining
                yield return new WaitForSeconds(0.5f);
                lobbyJoined = true;

                if (lobbyFailed)
                {
                    HandleSessionError($"Lobby join failed: {lobbyError}");
                    onError?.Invoke(lobbyError);
                    yield break;
                }

                if (!lobbyJoined)
                {
                    HandleSessionError("Failed to join lobby");
                    onError?.Invoke("Failed to join lobby");
                    yield break;
                }

                ChangeState(MultiplayerSessionState.InLobby);
            }

            // Step 4: Connect to server
            ChangeState(MultiplayerSessionState.ConnectingToServer);

            bool connected = false;
            bool connectFailed = false;
            string connectError = null;

            MatchmakingSystem.Instance.ConnectToMatch(
                () => connected = true,
                error =>
                {
                    connectError = error;
                    connectFailed = true;
                });

            // Wait for connection
            while (!connected && !connectFailed)
            {
                yield return null;
            }

            if (connectFailed)
            {
                HandleSessionError($"Server connection failed: {connectError}");
                onError?.Invoke(connectError);
                yield break;
            }

            // Step 5: Initialize anti-cheat
            if (ServerSideAntiCheat.Instance != null)
            {
                string userId = UserAuthenticationSystem.Instance?.UserId ?? "anonymous";
                ServerSideAntiCheat.Instance.RegisterPlayer(userId, Vector3.zero);
            }

            // Step 6: Enter game
            ChangeState(MultiplayerSessionState.InGame);

            // Create session data
            _sessionData = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                matchInfo = matchInfo,
                startTime = DateTime.UtcNow,
                userId = UserAuthenticationSystem.Instance?.UserId ?? "anonymous"
            };

            _totalSessionsCompleted++;

            OnSessionStarted?.Invoke(_sessionData);
            onSuccess?.Invoke(_sessionData);

            Log($"Session started: {_sessionData.sessionId}");
        }

        #endregion

        #region Session Flow - Ranked

        /// <summary>
        /// Start ranked session.
        /// </summary>
        public void StartRanked(Action<SessionData> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(RankedFlowCoroutine(onSuccess, onError));
        }

        private IEnumerator RankedFlowCoroutine(Action<SessionData> onSuccess, Action<string> onError)
        {
            _totalSessionsStarted++;

            // Ensure authenticated (required for ranked)
            if (!IsAuthenticated())
            {
                string error = "User not authenticated - required for ranked";
                HandleSessionError(error);
                onError?.Invoke(error);
                yield break;
            }

            ChangeState(MultiplayerSessionState.Matchmaking);

            bool matchFound = false;
            bool matchFailed = false;
            string matchError = null;
            MatchInfo matchInfo = null;

            MatchmakingSystem.Instance.RankedPlay(
                match =>
                {
                    matchInfo = match;
                    matchFound = true;
                },
                error =>
                {
                    matchError = error;
                    matchFailed = true;
                });

            // Wait for match
            while (!matchFound && !matchFailed)
            {
                yield return null;
            }

            if (matchFailed)
            {
                HandleSessionError($"Ranked matchmaking failed: {matchError}");
                onError?.Invoke(matchError);
                yield break;
            }

            // Continue with same flow as quick play
            yield return QuickPlayFlowCoroutine(onSuccess, onError);
        }

        #endregion

        #region Session Flow - Party

        /// <summary>
        /// Start party session with friends.
        /// </summary>
        public void StartParty(string[] partyMembers, Action<SessionData> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(PartyFlowCoroutine(partyMembers, onSuccess, onError));
        }

        private IEnumerator PartyFlowCoroutine(string[] partyMembers, Action<SessionData> onSuccess, Action<string> onError)
        {
            _totalSessionsStarted++;

            if (!IsAuthenticated())
            {
                string error = "User not authenticated";
                HandleSessionError(error);
                onError?.Invoke(error);
                yield break;
            }

            ChangeState(MultiplayerSessionState.Matchmaking);

            bool matchFound = false;
            bool matchFailed = false;
            string matchError = null;
            MatchInfo matchInfo = null;

            MatchmakingSystem.Instance.PartyPlay(partyMembers,
                match =>
                {
                    matchInfo = match;
                    matchFound = true;
                },
                error =>
                {
                    matchError = error;
                    matchFailed = true;
                });

            // Wait for match
            while (!matchFound && !matchFailed)
            {
                yield return null;
            }

            if (matchFailed)
            {
                HandleSessionError($"Party matchmaking failed: {matchError}");
                onError?.Invoke(matchError);
                yield break;
            }

            // Continue with same flow
            yield return QuickPlayFlowCoroutine(onSuccess, onError);
        }

        #endregion

        #region Session Flow - Custom Lobby

        /// <summary>
        /// Create custom lobby.
        /// </summary>
        public void CreateLobby(string lobbyName, int maxPlayers = 8, Action<SessionData> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(CreateLobbyFlowCoroutine(lobbyName, maxPlayers, onSuccess, onError));
        }

        private IEnumerator CreateLobbyFlowCoroutine(string lobbyName, int maxPlayers, Action<SessionData> onSuccess, Action<string> onError)
        {
            _totalSessionsStarted++;

            if (!IsAuthenticated())
            {
                string error = "User not authenticated";
                HandleSessionError(error);
                onError?.Invoke(error);
                yield break;
            }

            ChangeState(MultiplayerSessionState.CreatingLobby);

            bool lobbyCreated = false;
            bool lobbyFailed = false;
            string lobbyError = null;
            Lobby lobby = null;

            LobbySystem.Instance.CreateLobby(lobbyName, maxPlayers, LobbyVisibility.Public, null,
                createdLobby =>
                {
                    lobby = createdLobby;
                    lobbyCreated = true;
                },
                error =>
                {
                    lobbyError = error;
                    lobbyFailed = true;
                });

            // Wait for lobby creation
            while (!lobbyCreated && !lobbyFailed)
            {
                yield return null;
            }

            if (lobbyFailed)
            {
                HandleSessionError($"Lobby creation failed: {lobbyError}");
                onError?.Invoke(lobbyError);
                yield break;
            }

            ChangeState(MultiplayerSessionState.InLobby);

            // Create session data
            _sessionData = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                lobbyId = lobby.lobbyId,
                startTime = DateTime.UtcNow,
                userId = UserAuthenticationSystem.Instance?.UserId ?? "anonymous"
            };

            _totalSessionsCompleted++;

            OnSessionStarted?.Invoke(_sessionData);
            onSuccess?.Invoke(_sessionData);

            Log($"Custom lobby created: {lobbyName}");
        }

        /// <summary>
        /// Join existing lobby.
        /// </summary>
        public void JoinLobby(string lobbyId, Action<SessionData> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(JoinLobbyFlowCoroutine(lobbyId, onSuccess, onError));
        }

        private IEnumerator JoinLobbyFlowCoroutine(string lobbyId, Action<SessionData> onSuccess, Action<string> onError)
        {
            _totalSessionsStarted++;

            if (!IsAuthenticated())
            {
                string error = "User not authenticated";
                HandleSessionError(error);
                onError?.Invoke(error);
                yield break;
            }

            ChangeState(MultiplayerSessionState.JoiningLobby);

            bool lobbyJoined = false;
            bool lobbyFailed = false;
            string lobbyError = null;

            LobbySystem.Instance.JoinLobby(lobbyId, null,
                lobby => lobbyJoined = true,
                error =>
                {
                    lobbyError = error;
                    lobbyFailed = true;
                });

            // Wait for lobby join
            while (!lobbyJoined && !lobbyFailed)
            {
                yield return null;
            }

            if (lobbyFailed)
            {
                HandleSessionError($"Lobby join failed: {lobbyError}");
                onError?.Invoke(lobbyError);
                yield break;
            }

            ChangeState(MultiplayerSessionState.InLobby);

            // Create session data
            _sessionData = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                lobbyId = lobbyId,
                startTime = DateTime.UtcNow,
                userId = UserAuthenticationSystem.Instance?.UserId ?? "anonymous"
            };

            _totalSessionsCompleted++;

            OnSessionStarted?.Invoke(_sessionData);
            onSuccess?.Invoke(_sessionData);

            Log($"Joined lobby: {lobbyId}");
        }

        #endregion

        #region Session Management

        /// <summary>
        /// End current session.
        /// </summary>
        public void EndSession(string reason = "User requested")
        {
            if (!IsInSession)
            {
                LogWarning("No active session to end");
                return;
            }

            Log($"Ending session: {reason}");

            // Leave matchmaking if in queue
            if (_currentState == MultiplayerSessionState.Matchmaking)
            {
                MatchmakingSystem.Instance?.CancelQueue();
            }

            // Leave lobby if in one
            if (_currentState == MultiplayerSessionState.InLobby || _currentState == MultiplayerSessionState.CreatingLobby || _currentState == MultiplayerSessionState.JoiningLobby)
            {
                LobbySystem.Instance?.LeaveLobby();
            }

            // Disconnect from server
            if (_currentState == MultiplayerSessionState.InGame || _currentState == MultiplayerSessionState.ConnectingToServer)
            {
                MatchmakingSystem.Instance?.LeaveMatch();

                // Unregister from anti-cheat
                if (ServerSideAntiCheat.Instance != null && UserAuthenticationSystem.Instance != null)
                {
                    string userId = UserAuthenticationSystem.Instance.UserId;
                    ServerSideAntiCheat.Instance.UnregisterPlayer(userId);
                }
            }

            _sessionData = null;
            ChangeState(MultiplayerSessionState.Disconnected);

            OnSessionEnded?.Invoke();

            Log("Session ended");
        }

        /// <summary>
        /// Reconnect to last session.
        /// </summary>
        public void Reconnect(Action onSuccess = null, Action<string> onError = null)
        {
            if (!autoReconnect)
            {
                onError?.Invoke("Auto-reconnect disabled");
                return;
            }

            if (_reconnectAttempts >= maxReconnectAttempts)
            {
                string error = "Max reconnect attempts reached";
                HandleSessionError(error);
                onError?.Invoke(error);
                return;
            }

            _reconnectAttempts++;
            Log($"Reconnect attempt {_reconnectAttempts}/{maxReconnectAttempts}");

            StartCoroutine(ReconnectCoroutine(onSuccess, onError));
        }

        private IEnumerator ReconnectCoroutine(Action onSuccess, Action<string> onError)
        {
            ChangeState(MultiplayerSessionState.Reconnecting);

            // Attempt to rejoin last session
            // Implementation depends on session persistence

            yield return new WaitForSeconds(2f);

            // Simulate reconnect success
            bool reconnectSuccess = UnityEngine.Random.value > 0.5f;

            if (reconnectSuccess)
            {
                _reconnectAttempts = 0;
                _reconnectSuccesses++;
                ChangeState(MultiplayerSessionState.InGame);
                onSuccess?.Invoke();
                Log("Reconnect successful");
            }
            else
            {
                string error = "Reconnect failed";
                HandleSessionError(error);
                onError?.Invoke(error);
            }
        }

        #endregion

        #region State Management

        private void ChangeState(MultiplayerSessionState newState)
        {
            if (_currentState == newState) return;

            MultiplayerSessionState oldState = _currentState;
            _currentState = newState;
            _lastStateChangeTime = Time.time;

            OnStateChanged?.Invoke(newState);

            Log($"State: {oldState} → {newState}");
        }

        #endregion

        #region Event Handlers

        private void HandleLoginSuccess(UserSession session)
        {
            Log("User authenticated");
        }

        private void HandleLogout()
        {
            if (IsInSession)
            {
                EndSession("User logged out");
            }
        }

        private void HandleMatchFound(MatchInfo match)
        {
            Log($"Match found: {match.matchId}");
        }

        private void HandleQueueCancelled(string reason)
        {
            Log($"Queue cancelled: {reason}");
            ChangeState(MultiplayerSessionState.Disconnected);
        }

        private void HandleMatchmakingFailed(string error)
        {
            HandleSessionError($"Matchmaking failed: {error}");
        }

        private void HandleLobbyJoined(Lobby lobby)
        {
            Log($"Lobby joined: {lobby.lobbyName}");
        }

        private void HandleLobbyLeft()
        {
            Log("Lobby left");
            if (_currentState == MultiplayerSessionState.InLobby)
            {
                ChangeState(MultiplayerSessionState.Disconnected);
            }
        }

        private void HandleMatchStarting()
        {
            Log("Match starting...");
            ChangeState(MultiplayerSessionState.ConnectingToServer);
        }

        private void HandleLobbyError(string error)
        {
            HandleSessionError($"Lobby error: {error}");
        }

        private void HandleServerPlayerConnected(PlayerConnection player)
        {
            Log($"Player connected: {player.username}");
        }

        private void HandleServerPlayerDisconnected(string playerId)
        {
            Log($"Player disconnected: {playerId}");
        }

        private void HandlePlayerKicked(string playerId)
        {
            LogWarning($"Player kicked: {playerId}");
            OnPlayerKicked?.Invoke(playerId);

            // If local player, end session
            if (UserAuthenticationSystem.Instance != null && playerId == UserAuthenticationSystem.Instance.UserId)
            {
                EndSession("Kicked from server");
            }
        }

        private void HandlePlayerBanned(string playerId)
        {
            LogWarning($"Player banned: {playerId}");

            // If local player, end session
            if (UserAuthenticationSystem.Instance != null && playerId == UserAuthenticationSystem.Instance.UserId)
            {
                EndSession("Banned from server");
            }
        }

        private void HandleSessionError(string error)
        {
            _totalSessionsFailed++;
            LogError($"Session error: {error}");
            OnSessionFailed?.Invoke(error);
        }

        #endregion

        #region Helpers

        private bool IsAuthenticated()
        {
            return UserAuthenticationSystem.Instance != null && UserAuthenticationSystem.Instance.IsAuthenticated;
        }

        private void Log(string message)
        {
            if (verboseLogging)
            {
                Debug.Log($"[MultiplayerSessionManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[MultiplayerSessionManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[MultiplayerSessionManager] {message}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get session statistics.
        /// </summary>
        public MultiplayerSessionStats GetStats()
        {
            return new MultiplayerSessionStats
            {
                currentState = _currentState,
                totalSessionsStarted = _totalSessionsStarted,
                totalSessionsCompleted = _totalSessionsCompleted,
                totalSessionsFailed = _totalSessionsFailed,
                reconnectSuccesses = _reconnectSuccesses,
                reconnectAttempts = _reconnectAttempts,
                isInSession = IsInSession
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Start Quick Play")]
        private void StartQuickPlayMenu()
        {
            StartQuickPlay();
        }

        [ContextMenu("End Session")]
        private void EndSessionMenu()
        {
            EndSession("Manual");
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Multiplayer Session Statistics ===\n" +
                      $"Current State: {stats.currentState}\n" +
                      $"Sessions Started: {stats.totalSessionsStarted}\n" +
                      $"Sessions Completed: {stats.totalSessionsCompleted}\n" +
                      $"Sessions Failed: {stats.totalSessionsFailed}\n" +
                      $"Reconnect Successes: {stats.reconnectSuccesses}\n" +
                      $"In Session: {stats.isInSession}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Session data.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        public string sessionId;
        public string userId;
        public DateTime startTime;
        public MatchInfo matchInfo;
        public string lobbyId;
    }

    /// <summary>
    /// Multiplayer session statistics.
    /// </summary>
    [Serializable]
    public struct MultiplayerSessionStats
    {
        public MultiplayerSessionState currentState;
        public int totalSessionsStarted;
        public int totalSessionsCompleted;
        public int totalSessionsFailed;
        public int reconnectSuccesses;
        public int reconnectAttempts;
        public bool isInSession;
    }

    /// <summary>
    /// Multiplayer session states.
    /// </summary>
    public enum MultiplayerSessionState
    {
        Disconnected,
        Authenticating,
        Matchmaking,
        CreatingLobby,
        JoiningLobby,
        InLobby,
        ConnectingToServer,
        InGame,
        Reconnecting
    }

    #endregion
}
