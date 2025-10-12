using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Core.Player
{
    /// <summary>
    /// Default implementation of player session management.
    /// Handles player identification, session tracking, and context resolution.
    /// </summary>
    public class PlayerSessionManager : MonoBehaviour, IPlayerSessionManager
    {
        [Header("Configuration")]
        [SerializeField] private bool debugLogging = false;
        [SerializeField] private float sessionTimeoutMinutes = 30f;

        private string _currentPlayerId = "DefaultPlayer";
        private readonly Dictionary<string, PlayerSession> _playerSessions = new();

        public event Action<string, string> OnCurrentPlayerChanged;
        public event Action<PlayerSession> OnPlayerSessionStarted;
        public event Action<PlayerSession> OnPlayerSessionEnded;

        private void Awake()
        {
            // Register as service
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<IPlayerSessionManager>(this);
            }

            // Create default player session
            StartPlayerSession(_currentPlayerId);
        }

        private void Start()
        {
            // Auto-cleanup inactive sessions
            InvokeRepeating(nameof(CleanupInactiveSessions), 60f, 60f);
        }

        public string GetCurrentPlayerId()
        {
            return _currentPlayerId;
        }

        public void SetCurrentPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
            {
                Debug.LogWarning("[PlayerSessionManager] Attempted to set empty player ID");
                return;
            }

            var oldPlayerId = _currentPlayerId;
            _currentPlayerId = playerId;

            // Start session for new player if needed
            if (!_playerSessions.ContainsKey(playerId))
            {
                StartPlayerSession(playerId);
            }
            else
            {
                // Update activity for existing session
                _playerSessions[playerId].UpdateActivity();
            }

            OnCurrentPlayerChanged?.Invoke(oldPlayerId, playerId);

            if (debugLogging)
            {
                Debug.Log($"[PlayerSessionManager] Current player changed: {oldPlayerId} -> {playerId}");
            }
        }

        public List<string> GetActivePlayerIds()
        {
            var activePlayerIds = new List<string>();
            foreach (var session in _playerSessions.Values)
            {
                if (session.isActive)
                {
                    activePlayerIds.Add(session.playerId);
                }
            }
            return activePlayerIds;
        }

        public bool IsPlayerActive(string playerId)
        {
            if (_playerSessions.TryGetValue(playerId, out var session))
            {
                return session.isActive;
            }
            return false;
        }

        public PlayerSession GetPlayerSession(string playerId)
        {
            _playerSessions.TryGetValue(playerId, out var session);
            return session;
        }

        /// <summary>Starts a new player session</summary>
        public void StartPlayerSession(string playerId)
        {
            if (string.IsNullOrEmpty(playerId))
                return;

            // End existing session if any
            if (_playerSessions.TryGetValue(playerId, out var existingSession))
            {
                existingSession.EndSession();
                OnPlayerSessionEnded?.Invoke(existingSession);
            }

            // Create new session
            var newSession = new PlayerSession(playerId);
            _playerSessions[playerId] = newSession;

            OnPlayerSessionStarted?.Invoke(newSession);

            if (debugLogging)
            {
                Debug.Log($"[PlayerSessionManager] Started session for player: {playerId}");
            }
        }

        /// <summary>Ends a player session</summary>
        public void EndPlayerSession(string playerId)
        {
            if (_playerSessions.TryGetValue(playerId, out var session))
            {
                session.EndSession();
                OnPlayerSessionEnded?.Invoke(session);

                if (debugLogging)
                {
                    Debug.Log($"[PlayerSessionManager] Ended session for player: {playerId} (Duration: {session.SessionDuration:hh\\:mm\\:ss})");
                }

                // If this was the current player, switch to another active player or default
                if (_currentPlayerId == playerId)
                {
                    var activePlayerIds = GetActivePlayerIds();
                    if (activePlayerIds.Count > 0)
                    {
                        SetCurrentPlayer(activePlayerIds[0]);
                    }
                    else
                    {
                        SetCurrentPlayer("DefaultPlayer");
                    }
                }
            }
        }

        /// <summary>Updates activity for current player</summary>
        public void UpdateCurrentPlayerActivity()
        {
            if (_playerSessions.TryGetValue(_currentPlayerId, out var session))
            {
                session.UpdateActivity();
            }
        }

        /// <summary>Sets session data for a player</summary>
        public void SetPlayerSessionData(string playerId, string key, object value)
        {
            if (_playerSessions.TryGetValue(playerId, out var session))
            {
                session.sessionData[key] = value;
            }
        }

        /// <summary>Gets session data for a player</summary>
        public T GetPlayerSessionData<T>(string playerId, string key, T defaultValue = default)
        {
            if (_playerSessions.TryGetValue(playerId, out var session) &&
                session.sessionData.TryGetValue(key, out var value))
            {
                try
                {
                    return (T)value;
                }
                catch (InvalidCastException)
                {
                    Debug.LogWarning($"[PlayerSessionManager] Failed to cast session data {key} for player {playerId}");
                }
            }
            return defaultValue;
        }

        private void CleanupInactiveSessions()
        {
            var sessionsToRemove = new List<string>();
            var timeout = TimeSpan.FromMinutes(sessionTimeoutMinutes);

            foreach (var kvp in _playerSessions)
            {
                var session = kvp.Value;
                if (!session.isActive || session.TimeSinceLastActivity > timeout)
                {
                    sessionsToRemove.Add(kvp.Key);
                }
            }

            foreach (var playerId in sessionsToRemove)
            {
                EndPlayerSession(playerId);
                _playerSessions.Remove(playerId);
            }

            if (debugLogging && sessionsToRemove.Count > 0)
            {
                Debug.Log($"[PlayerSessionManager] Cleaned up {sessionsToRemove.Count} inactive sessions");
            }
        }

        private void OnDestroy()
        {
            // End all active sessions
            foreach (var session in _playerSessions.Values)
            {
                if (session.isActive)
                {
                    session.EndSession();
                    OnPlayerSessionEnded?.Invoke(session);
                }
            }
        }

        #region Debug Methods

        [ContextMenu("Log Current Player")]
        private void LogCurrentPlayer()
        {
            Debug.Log($"Current Player: {_currentPlayerId}");
        }

        [ContextMenu("Log All Sessions")]
        private void LogAllSessions()
        {
            Debug.Log($"Active Sessions ({_playerSessions.Count}):");
            foreach (var session in _playerSessions.Values)
            {
                var status = session.isActive ? "Active" : "Inactive";
                Debug.Log($"  {session.playerId}: {status}, Duration: {session.SessionDuration:hh\\:mm\\:ss}, Last Activity: {session.TimeSinceLastActivity:hh\\:mm\\:ss} ago");
            }
        }

        #endregion
    }
}