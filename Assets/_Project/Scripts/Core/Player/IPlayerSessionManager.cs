using System;
using System.Collections.Generic;

namespace Laboratory.Core.Player
{
    /// <summary>
    /// Interface for managing player sessions and context.
    /// Provides centralized player identification and session management.
    /// </summary>
    public interface IPlayerSessionManager
    {
        /// <summary>Gets the current active player ID</summary>
        string GetCurrentPlayerId();

        /// <summary>Sets the current active player</summary>
        void SetCurrentPlayer(string playerId);

        /// <summary>Gets all active player IDs</summary>
        List<string> GetActivePlayerIds();

        /// <summary>Checks if a player is currently active</summary>
        bool IsPlayerActive(string playerId);

        /// <summary>Gets player session data</summary>
        PlayerSession GetPlayerSession(string playerId);

        /// <summary>Event fired when current player changes</summary>
        event Action<string, string> OnCurrentPlayerChanged; // oldPlayerId, newPlayerId

        /// <summary>Event fired when player session starts</summary>
        event Action<PlayerSession> OnPlayerSessionStarted;

        /// <summary>Event fired when player session ends</summary>
        event Action<PlayerSession> OnPlayerSessionEnded;
    }

    /// <summary>Player session data</summary>
    [System.Serializable]
    public class PlayerSession
    {
        public string playerId;
        public DateTime sessionStartTime;
        public DateTime lastActivityTime;
        public bool isActive;
        public Dictionary<string, object> sessionData = new();

        public TimeSpan SessionDuration => DateTime.Now - sessionStartTime;
        public TimeSpan TimeSinceLastActivity => DateTime.Now - lastActivityTime;

        public PlayerSession(string playerId)
        {
            this.playerId = playerId;
            sessionStartTime = DateTime.Now;
            lastActivityTime = DateTime.Now;
            isActive = true;
        }

        public void UpdateActivity()
        {
            lastActivityTime = DateTime.Now;
        }

        public void EndSession()
        {
            isActive = false;
        }
    }
}