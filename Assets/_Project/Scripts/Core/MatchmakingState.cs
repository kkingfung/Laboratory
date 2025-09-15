using System;

namespace Laboratory.Core
{
    /// <summary>
    /// Represents the current state of the matchmaking process.
    /// </summary>
    public enum MatchmakingState
    {
        /// <summary>Not currently matchmaking.</summary>
        Idle = 0,
        
        /// <summary>Searching for a match.</summary>
        Searching = 1,
        
        /// <summary>Found a match, waiting for confirmation.</summary>
        MatchFound = 2,
        
        /// <summary>Joining the match.</summary>
        Joining = 3,
        
        /// <summary>Successfully connected to match.</summary>
        Connected = 4,
        
        /// <summary>Matchmaking failed.</summary>
        Failed = 5,
        
        /// <summary>Matchmaking was cancelled.</summary>
        Cancelled = 6,
        
        /// <summary>Waiting in lobby before match starts.</summary>
        WaitingInLobby = 7,
        
        /// <summary>Match is starting.</summary>
        MatchStarting = 8
    }
    
    /// <summary>
    /// Extension methods for MatchmakingState enum.
    /// </summary>
    public static class MatchmakingStateExtensions
    {
        /// <summary>
        /// Checks if the state represents an active matchmaking process.
        /// </summary>
        public static bool IsActive(this MatchmakingState state)
        {
            return state == MatchmakingState.Searching ||
                   state == MatchmakingState.MatchFound ||
                   state == MatchmakingState.Joining;
        }
        
        /// <summary>
        /// Checks if the state represents a completed matchmaking process.
        /// </summary>
        public static bool IsCompleted(this MatchmakingState state)
        {
            return state == MatchmakingState.Connected ||
                   state == MatchmakingState.WaitingInLobby ||
                   state == MatchmakingState.MatchStarting;
        }
        
        /// <summary>
        /// Checks if the state represents a terminal state (failed or cancelled).
        /// </summary>
        public static bool IsTerminal(this MatchmakingState state)
        {
            return state == MatchmakingState.Failed ||
                   state == MatchmakingState.Cancelled;
        }
        
        /// <summary>
        /// Gets a human-readable description of the state.
        /// </summary>
        public static string GetDescription(this MatchmakingState state)
        {
            return state switch
            {
                MatchmakingState.Idle => "Ready to start matchmaking",
                MatchmakingState.Searching => "Searching for players...",
                MatchmakingState.MatchFound => "Match found! Preparing to join...",
                MatchmakingState.Joining => "Joining match...",
                MatchmakingState.Connected => "Connected to match",
                MatchmakingState.Failed => "Matchmaking failed",
                MatchmakingState.Cancelled => "Matchmaking cancelled",
                MatchmakingState.WaitingInLobby => "Waiting for other players",
                MatchmakingState.MatchStarting => "Match starting...",
                _ => "Unknown state"
            };
        }
    }
}
