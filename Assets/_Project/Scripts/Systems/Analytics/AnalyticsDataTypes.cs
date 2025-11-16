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
        Aggression,
        RiskTaking
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

    /// <summary>
    /// Analytics session data for tracking player sessions
    /// </summary>
    [Serializable]
    public class AnalyticsSessionData
    {
        public uint sessionId;
        public float startTime;
        public float endTime;
        public float duration;
        public float totalPauseTime;
        public int totalActions;
        public int uniqueActionTypes;
        public ArchetypeType playerArchetype;
        public List<PlayerAction> actions;
        public EngagementMetrics engagementMetrics;
        public Dictionary<string, float> behaviorMetrics;
        public Dictionary<Laboratory.Chimera.Social.Types.EmotionalState, float> emotionalProfile;
    }

    /// <summary>
    /// Player behavior analysis data (alias for BehaviorAnalysisStats)
    /// </summary>
    [Serializable]
    public struct PlayerBehaviorAnalysis
    {
        public ArchetypeType currentArchetype;
        public PlayerBehaviorTrait dominantTrait;
        public float traitDiversity;
        public int patternCount;
        public int insightCount;
    }

    /// <summary>
    /// Behavior focus metrics for adaptive systems
    /// </summary>
    [Serializable]
    public struct BehaviorFocusMetrics
    {
        public float explorationFocus;
        public float socialFocus;
        public float competitiveFocus;
        public float creativeFocus;
    }
}
