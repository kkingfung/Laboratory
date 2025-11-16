using System;
using System.Collections.Generic;
using Laboratory.Core.Enums;

namespace Laboratory.Systems.Analytics
{
    /// <summary>
    /// Data types for the Analytics system
    /// </summary>

    /// <summary>
    /// Represents a player action with metadata
    /// </summary>
    [Serializable]
    public struct PlayerAction
    {
        public uint actionId;
        public string actionType;
        public float timestamp;
        public string context;
        public Dictionary<ParamKey, object> parameters;
    }

    /// <summary>
    /// Player behavior traits for classification
    /// </summary>
    public enum PlayerBehaviorTrait
    {
        Exploration,
        Combat,
        Social,
        Collection,
        Creativity,
        Strategy,
        Patience,
        Aggression
    }

    /// <summary>
    /// Player archetype types
    /// </summary>
    public enum ArchetypeType
    {
        Explorer,
        Achiever,
        Socializer,
        Collector,
        Balanced,
        Unknown
    }

    /// <summary>
    /// Player archetype classification
    /// </summary>
    [Serializable]
    public struct PlayerArchetype
    {
        public ArchetypeType archetypeType;
        public float confidence;
        public PlayerBehaviorTrait primaryTrait;
    }

    /// <summary>
    /// Behavior insight from analysis
    /// </summary>
    [Serializable]
    public struct BehaviorInsight
    {
        public string insightType;
        public string description;
        public float confidence;
        public float timestamp;
    }

    /// <summary>
    /// Player profile for Analytics system
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string playerId;
        public string creationDate;
        public float totalPlayTime;
        public int totalSessions;
        public string lastPlayDate;
        public ArchetypeType dominantArchetype;
    }

    /// <summary>
    /// Engagement metrics for session analysis
    /// </summary>
    [Serializable]
    public struct EngagementMetrics
    {
        public float sessionDuration;
        public float actionsPerMinute;
        public float averageActionInterval;
        public float peakEngagementPeriod;
        public float sessionQuality;
    }
}
