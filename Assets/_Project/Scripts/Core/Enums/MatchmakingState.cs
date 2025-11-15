namespace Laboratory.Core.Enums
{
    /// <summary>
    /// States of the matchmaking system
    /// </summary>
    public enum MatchmakingState : byte
    {
        /// <summary>Not actively matchmaking</summary>
        Idle = 0,
        /// <summary>Searching for matches</summary>
        Searching = 1,
        /// <summary>Match found, connecting to lobby</summary>
        MatchFound = 2,
        /// <summary>Connected to lobby</summary>
        InLobby = 3,
        /// <summary>Loading into match</summary>
        Loading = 4,
        /// <summary>In an active match</summary>
        InMatch = 5,
        /// <summary>Matchmaking cancelled by user</summary>
        Cancelled = 6,
        /// <summary>Matchmaking failed due to error</summary>
        Failed = 7,
        /// <summary>Disconnected from match</summary>
        Disconnected = 8
    }
}