using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Subsystems.Analytics
{
    #region Core Analytics Events

    [Serializable]
    public abstract class AnalyticsEvent
    {
        public DateTime timestamp;
        public string sessionId;
        public abstract string EventType { get; }
    }

    [Serializable]
    public class PlayerActionEvent : AnalyticsEvent
    {
        public string actionType;
        public Dictionary<string, object> parameters = new();
        public override string EventType => "PlayerAction";
    }

    [Serializable]
    public class DiscoveryAnalyticsEvent : AnalyticsEvent
    {
        public string discoveryType;
        public string discoveredItem;
        public bool isWorldFirst;
        public override string EventType => "Discovery";
    }

    [Serializable]
    public class EducationalProgressEvent : AnalyticsEvent
    {
        public string lessonId;
        public string conceptMastered;
        public float confidenceLevel;
        public override string EventType => "EducationalProgress";
    }

    [Serializable]
    public class PerformanceMetrics : AnalyticsEvent
    {
        public float frameRate;
        public float memoryUsage;
        public int activeEntities;
        public float networkLatency;
        public float loadTime;
        public override string EventType => "Performance";
    }

    #endregion

    #region Session Data

    [Serializable]
    public class AnalyticsSessionData
    {
        public string sessionId;
        public DateTime startTime;
        public DateTime endTime;
        public List<PlayerActionEvent> playerActions = new();
        public List<DiscoveryAnalyticsEvent> discoveries = new();
        public List<PerformanceMetrics> performanceSnapshots = new();
    }

    [Serializable]
    public class AnalyticsSessionSummary
    {
        public string sessionId;
        public TimeSpan sessionDuration;
        public int totalActions;
        public int totalDiscoveries;
        public int breedingAttempts;
        public float averagePerformance;
        public float educationalProgress;
    }

    #endregion

    #region Behavior Analytics

    [Serializable]
    public class PlayerBehaviorPattern
    {
        public string patternType;
        public string description;
        public float frequency;
        public DateTime lastOccurrence;
        public Dictionary<string, float> parameters = new();
    }

    [Serializable]
    public class BreedingPatternAnalytics
    {
        public Dictionary<string, int> speciesCombinations = new();
        public Dictionary<string, float> successRates = new();
        public Dictionary<string, int> preferredTraits = new();
        public float averageBreedingTime;
        public int totalBreedingAttempts;
    }

    #endregion

    #region Discovery Analytics

    [Serializable]
    public class DiscoveryRarityMetrics
    {
        public Dictionary<string, float> traitRarityScores = new();
        public Dictionary<string, int> discoveryFrequency = new();
        public Dictionary<string, DateTime> firstDiscoveryDates = new();
        public int totalUniqueDiscoveries;
        public float averageDiscoveryRate;
    }

    [Serializable]
    public class GeneticDiscoveryAnalytics
    {
        public Dictionary<string, int> traitCombinationCounts = new();
        public Dictionary<string, float> mutationRates = new();
        public Dictionary<string, int> playerFirstDiscoveries = new();
        public float geneticDiversityIndex;
        public int extinctLineages;
    }

    #endregion

    #region Performance Analytics

    [Serializable]
    public class PerformanceAnalyticsSummary
    {
        public float averageFrameRate;
        public float minFrameRate;
        public float maxFrameRate;
        public float averageMemoryUsage;
        public float peakMemoryUsage;
        public int averageEntityCount;
        public int maxEntityCount;
        public float averageNetworkLatency;
        public List<PerformanceAnomaly> anomalies = new();
    }

    [Serializable]
    public class PerformanceAnomaly
    {
        public DateTime timestamp;
        public string anomalyType;
        public float severity;
        public string description;
        public Dictionary<string, float> metrics = new();
    }

    #endregion

    #region Educational Analytics

    [Serializable]
    public class EducationalProgressSummary
    {
        public Dictionary<string, float> conceptMastery = new();
        public Dictionary<string, int> lessonCompletions = new();
        public float overallConfidence;
        public TimeSpan totalLearningTime;
        public List<string> strugglingConcepts = new();
        public List<string> masteredConcepts = new();
    }

    [Serializable]
    public class LearningPathAnalytics
    {
        public string studentId;
        public Dictionary<string, DateTime> conceptFirstExposure = new();
        public Dictionary<string, DateTime> conceptMastery = new();
        public Dictionary<string, int> conceptAttempts = new();
        public float learningVelocity;
        public string preferredLearningStyle;
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Tracks player behavior patterns for engagement analysis
    /// </summary>
    public interface IPlayerBehaviorTracker
    {
        void ProcessPlayerAction(PlayerActionEvent actionEvent);
        void UpdateBehaviorPatterns();
        List<PlayerBehaviorPattern> GetBehaviorPatterns();
        BreedingPatternAnalytics GetBreedingPatterns();
        float GetEngagementScore();
    }

    /// <summary>
    /// Monitors system performance for optimization
    /// </summary>
    public interface IPerformanceMonitor
    {
        PerformanceMetrics CollectCurrentMetrics();
        PerformanceAnalyticsSummary GetAnalyticsSummary();
        void CheckForAnomalies();
        float GetAveragePerformance();
        List<PerformanceAnomaly> GetRecentAnomalies();
    }

    /// <summary>
    /// Tracks educational progress for school environments
    /// </summary>
    public interface IEducationalAnalytics
    {
        void RecordProgress(EducationalProgressEvent progressEvent);
        EducationalProgressSummary GetSessionProgress();
        LearningPathAnalytics GetLearningPath(string studentId);
        float GetOverallProgress();
        List<string> GetRecommendations();
    }

    /// <summary>
    /// Manages discovery rarity and balancing metrics
    /// </summary>
    public interface IDiscoveryMetrics
    {
        void RecordDiscovery(DiscoveryAnalyticsEvent discoveryEvent);
        void UpdateRarityMetrics();
        DiscoveryRarityMetrics GetRarityMetrics();
        GeneticDiscoveryAnalytics GetGeneticDiscoveryAnalytics();
        float GetDiscoveryRarityScore(string itemType, string itemName);
        List<string> GetRecommendedDiscoveryTargets();
    }

    #endregion

    #region Enums

    public enum AnalyticsEventType
    {
        PlayerAction,
        Discovery,
        Performance,
        Educational,
        System,
        Error
    }

    public enum PlayerEngagementLevel
    {
        Low,
        Medium,
        High,
        Expert
    }

    public enum DiscoveryDifficulty
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum PerformanceIssueType
    {
        FrameRate,
        Memory,
        Network,
        Loading,
        EntityCount
    }

    #endregion
}