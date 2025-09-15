#nullable enable
using System;

namespace Laboratory.Core.Events.Messages
{
    #region Matchmaking Events
    
    /// <summary>
    /// Event fired when matchmaking state changes.
    /// </summary>
    public class MatchmakingStateChangedEvent
    {
        /// <summary>Previous matchmaking state.</summary>
        public Laboratory.Core.MatchmakingState PreviousState { get; }
        
        /// <summary>New matchmaking state.</summary>
        public Laboratory.Core.MatchmakingState CurrentState { get; }
        
        /// <summary>Timestamp when the state change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public MatchmakingStateChangedEvent(
            Laboratory.Core.MatchmakingState previousState, 
            Laboratory.Core.MatchmakingState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when matchmaking starts.
    /// </summary>
    public class MatchmakingStartedEvent
    {
        /// <summary>Timestamp when matchmaking started.</summary>
        public DateTime StartedAt { get; }
        
        /// <summary>Maximum expected matchmaking duration in seconds.</summary>
        public float TimeoutSeconds { get; }
        
        public MatchmakingStartedEvent(float timeoutSeconds)
        {
            StartedAt = DateTime.UtcNow;
            TimeoutSeconds = timeoutSeconds;
        }
    }
    
    /// <summary>
    /// Event fired when a match is found.
    /// </summary>
    public class MatchFoundEvent
    {
        /// <summary>Match identifier or join code.</summary>
        public string MatchId { get; }
        
        /// <summary>Server host address (if applicable).</summary>
        public string? ServerHost { get; }
        
        /// <summary>Server port (if applicable).</summary>
        public int? ServerPort { get; }
        
        /// <summary>Whether this client is the host.</summary>
        public bool IsHost { get; }
        
        /// <summary>Timestamp when match was found.</summary>
        public DateTime FoundAt { get; }
        
        public MatchFoundEvent(string matchId, bool isHost, string? serverHost = null, int? serverPort = null)
        {
            MatchId = matchId;
            IsHost = isHost;
            ServerHost = serverHost;
            ServerPort = serverPort;
            FoundAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when matchmaking is cancelled.
    /// </summary>
    public class MatchmakingCancelledEvent
    {
        /// <summary>Reason for cancellation.</summary>
        public string Reason { get; }
        
        /// <summary>Whether cancellation was user-initiated.</summary>
        public bool UserInitiated { get; }
        
        /// <summary>Timestamp when cancellation occurred.</summary>
        public DateTime CancelledAt { get; }
        
        public MatchmakingCancelledEvent(string reason, bool userInitiated = true)
        {
            Reason = reason;
            UserInitiated = userInitiated;
            CancelledAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when matchmaking fails.
    /// </summary>
    public class MatchmakingFailedEvent
    {
        /// <summary>Error message describing the failure.</summary>
        public string ErrorMessage { get; }
        
        /// <summary>Exception that caused the failure (if any).</summary>
        public Exception? Exception { get; }
        
        /// <summary>Whether the operation can be retried.</summary>
        public bool CanRetry { get; }
        
        /// <summary>Timestamp when failure occurred.</summary>
        public DateTime FailedAt { get; }
        
        public MatchmakingFailedEvent(string errorMessage, Exception? exception = null, bool canRetry = true)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            CanRetry = canRetry;
            FailedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when attempting to join a match via join code.
    /// </summary>
    public class MatchJoinAttemptEvent
    {
        /// <summary>Join code being used.</summary>
        public string JoinCode { get; }
        
        /// <summary>Timestamp when join attempt started.</summary>
        public DateTime AttemptedAt { get; }
        
        public MatchJoinAttemptEvent(string joinCode)
        {
            JoinCode = joinCode;
            AttemptedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when successfully joining a match.
    /// </summary>
    public class MatchJoinedEvent
    {
        /// <summary>Join code used to join the match.</summary>
        public string JoinCode { get; }
        
        /// <summary>Host information (if available).</summary>
        public string? HostInfo { get; }
        
        /// <summary>Timestamp when join completed.</summary>
        public DateTime JoinedAt { get; }
        
        public MatchJoinedEvent(string joinCode, string? hostInfo = null)
        {
            JoinCode = joinCode;
            HostInfo = hostInfo;
            JoinedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when creating a relay allocation for hosting.
    /// </summary>
    public class RelayAllocationCreatedEvent
    {
        /// <summary>Allocation ID from Unity Relay.</summary>
        public string AllocationId { get; }
        
        /// <summary>Join code for other players.</summary>
        public string JoinCode { get; }
        
        /// <summary>Maximum number of players for this allocation.</summary>
        public int MaxPlayers { get; }
        
        /// <summary>Timestamp when allocation was created.</summary>
        public DateTime CreatedAt { get; }
        
        public RelayAllocationCreatedEvent(string allocationId, string joinCode, int maxPlayers)
        {
            AllocationId = allocationId;
            JoinCode = joinCode;
            MaxPlayers = maxPlayers;
            CreatedAt = DateTime.UtcNow;
        }
    }
    
    #endregion
}
