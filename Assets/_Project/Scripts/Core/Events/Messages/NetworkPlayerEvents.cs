#nullable enable
using System;
using Unity.Collections;

namespace Laboratory.Core.Events.Messages
{
    #region Network Player Events
    
    /// <summary>
    /// Event fired when a player's name changes across the network.
    /// </summary>
    public class PlayerNameChangedEvent
    {
        /// <summary>Network object ID of the player.</summary>
        public ulong NetworkObjectId { get; }
        
        /// <summary>Previous player name.</summary>
        public string PreviousName { get; }
        
        /// <summary>New player name.</summary>
        public string NewName { get; }
        
        /// <summary>Timestamp when the change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public PlayerNameChangedEvent(ulong networkObjectId, string previousName, string newName)
        {
            NetworkObjectId = networkObjectId;
            PreviousName = previousName ?? string.Empty;
            NewName = newName ?? string.Empty;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when a player's score changes.
    /// </summary>
    public class PlayerScoreChangedEvent
    {
        /// <summary>Network object ID of the player.</summary>
        public ulong NetworkObjectId { get; }
        
        /// <summary>Player's name for display purposes.</summary>
        public string PlayerName { get; }
        
        /// <summary>Previous score value.</summary>
        public int PreviousScore { get; }
        
        /// <summary>New score value.</summary>
        public int NewScore { get; }
        
        /// <summary>Score difference (positive or negative).</summary>
        public int ScoreDelta => NewScore - PreviousScore;
        
        /// <summary>Timestamp when the change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public PlayerScoreChangedEvent(ulong networkObjectId, string playerName, int previousScore, int newScore)
        {
            NetworkObjectId = networkObjectId;
            PlayerName = playerName ?? string.Empty;
            PreviousScore = previousScore;
            NewScore = newScore;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when a player's kill count changes.
    /// </summary>
    public class PlayerKillsChangedEvent
    {
        /// <summary>Network object ID of the player.</summary>
        public ulong NetworkObjectId { get; }
        
        /// <summary>Player's name for display purposes.</summary>
        public string PlayerName { get; }
        
        /// <summary>Previous kill count.</summary>
        public int PreviousKills { get; }
        
        /// <summary>New kill count.</summary>
        public int NewKills { get; }
        
        /// <summary>Number of new kills gained.</summary>
        public int KillsGained => NewKills - PreviousKills;
        
        /// <summary>Timestamp when the change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public PlayerKillsChangedEvent(ulong networkObjectId, string playerName, int previousKills, int newKills)
        {
            NetworkObjectId = networkObjectId;
            PlayerName = playerName ?? string.Empty;
            PreviousKills = previousKills;
            NewKills = newKills;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when a player's death count changes.
    /// </summary>
    public class PlayerDeathsChangedEvent
    {
        /// <summary>Network object ID of the player.</summary>
        public ulong NetworkObjectId { get; }
        
        /// <summary>Player's name for display purposes.</summary>
        public string PlayerName { get; }
        
        /// <summary>Previous death count.</summary>
        public int PreviousDeaths { get; }
        
        /// <summary>New death count.</summary>
        public int NewDeaths { get; }
        
        /// <summary>Number of new deaths.</summary>
        public int DeathsGained => NewDeaths - PreviousDeaths;
        
        /// <summary>Timestamp when the change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public PlayerDeathsChangedEvent(ulong networkObjectId, string playerName, int previousDeaths, int newDeaths)
        {
            NetworkObjectId = networkObjectId;
            PlayerName = playerName ?? string.Empty;
            PreviousDeaths = previousDeaths;
            NewDeaths = newDeaths;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Event fired when a player's assist count changes.
    /// </summary>
    public class PlayerAssistsChangedEvent
    {
        /// <summary>Network object ID of the player.</summary>
        public ulong NetworkObjectId { get; }
        
        /// <summary>Player's name for display purposes.</summary>
        public string PlayerName { get; }
        
        /// <summary>Previous assist count.</summary>
        public int PreviousAssists { get; }
        
        /// <summary>New assist count.</summary>
        public int NewAssists { get; }
        
        /// <summary>Number of new assists gained.</summary>
        public int AssistsGained => NewAssists - PreviousAssists;
        
        /// <summary>Timestamp when the change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public PlayerAssistsChangedEvent(ulong networkObjectId, string playerName, int previousAssists, int newAssists)
        {
            NetworkObjectId = networkObjectId;
            PlayerName = playerName ?? string.Empty;
            PreviousAssists = previousAssists;
            NewAssists = newAssists;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    /// <summary>
    /// Combined event fired when any player statistics change.
    /// Useful for updating overall leaderboards and UI.
    /// </summary>
    public class PlayerStatisticsChangedEvent
    {
        /// <summary>Network object ID of the player.</summary>
        public ulong NetworkObjectId { get; }
        
        /// <summary>Player's name for display purposes.</summary>
        public string PlayerName { get; }
        
        /// <summary>Current score.</summary>
        public int Score { get; }
        
        /// <summary>Current kill count.</summary>
        public int Kills { get; }
        
        /// <summary>Current death count.</summary>
        public int Deaths { get; }
        
        /// <summary>Current assist count.</summary>
        public int Assists { get; }
        
        /// <summary>Current kill-death ratio.</summary>
        public float KillDeathRatio { get; }
        
        /// <summary>Type of statistic that triggered this event.</summary>
        public string StatisticType { get; }
        
        /// <summary>Timestamp when the change occurred.</summary>
        public DateTime ChangedAt { get; }
        
        public PlayerStatisticsChangedEvent(
            ulong networkObjectId, 
            string playerName, 
            int score, 
            int kills, 
            int deaths, 
            int assists, 
            float killDeathRatio,
            string statisticType)
        {
            NetworkObjectId = networkObjectId;
            PlayerName = playerName ?? string.Empty;
            Score = score;
            Kills = kills;
            Deaths = deaths;
            Assists = assists;
            KillDeathRatio = killDeathRatio;
            StatisticType = statisticType ?? string.Empty;
            ChangedAt = DateTime.UtcNow;
        }
    }
    
    #endregion
}
