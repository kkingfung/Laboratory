namespace Laboratory.Gameplay.Lobby
{
    /// <summary>
    /// Enumeration representing the various states of the matchmaking process.
    /// Used to track and manage the progression of finding and connecting to game matches.
    /// </summary>
    public enum MatchmakingState
    {
        /// <summary>
        /// Initial state - no matchmaking activity.
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// Currently searching for an available match.
        /// </summary>
        Searching = 1,
        
        /// <summary>
        /// A match has been found but not yet connected.
        /// </summary>
        Found = 2,
        
        /// <summary>
        /// Attempting to connect to the found match.
        /// </summary>
        Connecting = 3,
        
        /// <summary>
        /// Successfully connected to the match.
        /// </summary>
        Connected = 4,
        
        /// <summary>
        /// Matchmaking failed for any reason.
        /// </summary>
        Failed = 5,
        
        /// <summary>
        /// User cancelled the matchmaking process.
        /// </summary>
        Cancelled = 6,
        
        /// <summary>
        /// Waiting in queue for a match.
        /// </summary>
        Queued = 7,
        
        /// <summary>
        /// Connection timed out.
        /// </summary>
        Timeout = 8
    }
    
    /// <summary>
    /// Event data for matchmaking state changes.
    /// </summary>
    public struct MatchmakingStateChangedEvent
    {
        public MatchmakingState PreviousState;
        public MatchmakingState CurrentState;
        public string Reason;
        
        public MatchmakingStateChangedEvent(MatchmakingState previousState, MatchmakingState currentState, string reason = "")
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Reason = reason;
        }
    }
}
