using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Laboratory.Subsystems.AIDirector
{
    #region Core AI Director Data

    [Serializable]
    public class PlayerProfile
    {
        public string playerId;
        public DateTime registrationDate;
        public DateTime sessionStartTime;
        public DateTime lastActivity;
        public DateTime lastAnalysisUpdate;

        public SkillLevel skillLevel = SkillLevel.Beginner;
        public LearningStyle learningStyle = LearningStyle.Mixed;
        public float engagementScore = 0.5f;
        public float confidenceLevel = 0.5f;
        public float learningVelocity = 1f;
        public float progressionScore = 0f;
        public DifficultyLevel preferredChallengeLevel = DifficultyLevel.Medium;
        public float collaborationFrequency = 0f;

        public List<string> interests = new();
        public List<string> milestonesAchieved = new();
        public List<string> strugglingAreas = new();
        public List<string> strengthAreas = new();

        public bool isEducationalContext = false;
        public string gradeLevel;
        public string currentFocus;

        public PlayerBehaviorPattern behaviorPattern = new();
        public PersonalizationSettings personalizationSettings = new();
        public Dictionary<string, object> customAttributes = new();
    }

    [Serializable]
    public class DirectorContext
    {
        public string playerId;
        public DateTime lastUpdate;

        public float engagement;
        public float skillLevel;
        public float progressRate;
        public float socialActivity;
        public float timeInSession;
        public float challengeLevel;
        public float frustrationLevel;
        public float flowState;

        public ContextType currentContextType;
        public List<string> activeGoals = new();
        public List<string> recentActions = new();
        public Dictionary<string, float> contextMetrics = new();

        public enum ContextType
        {
            PlayerSkill,
            Engagement,
            Progress,
            Time,
            Social,
            Educational,
            Emotional
        }
    }

    [Serializable]
    public class DirectorDecision
    {
        public string decisionId;
        public DirectorDecisionType decisionType;
        public string playerId;
        public DateTime timestamp;
        public DirectorPriority priority;
        public string reasoning;
        public Dictionary<string, object> parameters = new();
        public float confidence;
        public bool wasExecuted;
        public DateTime executionTime;
        public DirectorDecisionResult result;
    }

    [Serializable]
    public class DirectorEvent
    {
        public DirectorEventType eventType;
        public string playerId;
        public DateTime timestamp;
        public Dictionary<string, object> data = new();
        public string sessionId;
        public string contextId;
        public float significance;
    }

    [Serializable]
    public class DirectorState
    {
        public DateTime lastUpdate;
        public int totalPlayers;
        public int activePlayers;
        public float globalEngagement;
        public float globalDifficulty;
        public Dictionary<string, int> globalDiscoveries = new();
        public Dictionary<string, float> systemMetrics = new();
        public List<string> activeNarratives = new();
        public EmergentTrends emergentTrends = new();
    }

    public enum DirectorDecisionType
    {
        AdjustDifficulty,
        TriggerNarrative,
        SpawnContent,
        ProvideHint,
        EncourageCollaboration,
        CelebrateAchievement,
        CreateChallenge,
        OfferChoice,
        ModifyEnvironment,
        InitiateConversation
    }

    public enum DirectorEventType
    {
        PlayerAction,
        Discovery,
        Achievement,
        Collaboration,
        Struggle,
        Milestone,
        SessionStart,
        SessionEnd,
        ErrorOccurred,
        GoalCompleted
    }

    public enum DirectorPriority
    {
        Low,
        Medium,
        High,
        Critical,
        Immediate
    }

    public enum SkillLevel
    {
        Beginner,
        Novice,
        Intermediate,
        Advanced,
        Expert
    }

    public enum LearningStyle
    {
        Visual,
        Auditory,
        Kinesthetic,
        ReadingWriting,
        Mixed
    }

    public enum DifficultyLevel
    {
        VeryEasy,
        Easy,
        Medium,
        Hard,
        VeryHard
    }

    #endregion

    #region AI Decision Making

    [Serializable]
    public class DecisionMatrix
    {
        public Dictionary<DirectorContext.ContextType, float> difficultyWeights = new();
        public Dictionary<string, float> narrativeWeights = new();
        public Dictionary<string, float> engagementWeights = new();
        public Dictionary<string, float> learningWeights = new();
        public float adaptationThreshold = 0.7f;
        public float interventionThreshold = 0.3f;
    }

    [Serializable]
    public class DirectorRule
    {
        public string ruleId;
        public string ruleName;
        public string description;
        public RuleCondition condition;
        public RuleAction action;
        public float priority;
        public bool isEnabled = true;
        public int usageCount;
        public float successRate;
        public DateTime lastUsed;

        public bool IsApplicable(PlayerProfile profile, DirectorContext context)
        {
            return condition.Evaluate(profile, context);
        }

        public DirectorDecision GenerateDecision(PlayerProfile profile, DirectorContext context)
        {
            return action.GenerateDecision(profile, context, this);
        }
    }

    [Serializable]
    public class RuleCondition
    {
        public ConditionType conditionType;
        public string parameter;
        public ComparisonOperator comparisonOperator;
        public float threshold;
        public List<RuleCondition> subConditions = new();
        public LogicalOperator logicalOperator = LogicalOperator.And;

        public bool Evaluate(PlayerProfile profile, DirectorContext context)
        {
            var result = EvaluateSingle(profile, context);

            foreach (var subCondition in subConditions)
            {
                var subResult = subCondition.Evaluate(profile, context);

                result = logicalOperator switch
                {
                    LogicalOperator.And => result && subResult,
                    LogicalOperator.Or => result || subResult,
                    LogicalOperator.Not => !subResult,
                    _ => result
                };
            }

            return result;
        }

        private bool EvaluateSingle(PlayerProfile profile, DirectorContext context)
        {
            var value = GetParameterValue(profile, context, parameter);

            return comparisonOperator switch
            {
                ComparisonOperator.Equal => Math.Abs(value - threshold) < 0.01f,
                ComparisonOperator.NotEqual => Math.Abs(value - threshold) >= 0.01f,
                ComparisonOperator.GreaterThan => value > threshold,
                ComparisonOperator.GreaterThanOrEqual => value >= threshold,
                ComparisonOperator.LessThan => value < threshold,
                ComparisonOperator.LessThanOrEqual => value <= threshold,
                _ => false
            };
        }

        private float GetParameterValue(PlayerProfile profile, DirectorContext context, string param)
        {
            return param switch
            {
                "engagement" => context.engagement,
                "skillLevel" => context.skillLevel,
                "progressRate" => context.progressRate,
                "timeInSession" => context.timeInSession,
                "confidenceLevel" => profile.confidenceLevel,
                "frustrationLevel" => context.frustrationLevel,
                _ => 0f
            };
        }
    }

    [Serializable]
    public class RuleAction
    {
        public DirectorDecisionType actionType;
        public string actionDescription;
        public Dictionary<string, object> actionParameters = new();
        public float actionStrength = 1f;

        public DirectorDecision GenerateDecision(PlayerProfile profile, DirectorContext context, DirectorRule rule)
        {
            return new DirectorDecision
            {
                decisionId = Guid.NewGuid().ToString(),
                decisionType = actionType,
                playerId = profile.playerId,
                timestamp = DateTime.Now,
                priority = CalculatePriority(profile, context),
                reasoning = GenerateReasoning(profile, context, rule),
                parameters = new Dictionary<string, object>(actionParameters),
                confidence = CalculateConfidence(profile, context)
            };
        }

        private DirectorPriority CalculatePriority(PlayerProfile profile, DirectorContext context)
        {
            if (context.engagement < 0.2f || context.frustrationLevel > 0.8f)
                return DirectorPriority.Critical;
            else if (context.engagement < 0.4f || context.progressRate < 0.3f)
                return DirectorPriority.High;
            else if (context.flowState > 0.7f)
                return DirectorPriority.Low;
            else
                return DirectorPriority.Medium;
        }

        private string GenerateReasoning(PlayerProfile profile, DirectorContext context, DirectorRule rule)
        {
            return $"Rule '{rule.ruleName}' triggered: {actionDescription}";
        }

        private float CalculateConfidence(PlayerProfile profile, DirectorContext context)
        {
            // Calculate confidence based on data quality and pattern consistency
            var dataQuality = CalculateDataQuality(profile, context);
            var patternConsistency = CalculatePatternConsistency(profile);
            return (dataQuality + patternConsistency) / 2f;
        }

        private float CalculateDataQuality(PlayerProfile profile, DirectorContext context)
        {
            var timeSinceLastUpdate = DateTime.Now - context.lastUpdate;
            if (timeSinceLastUpdate.TotalMinutes > 10)
                return 0.3f;
            else if (timeSinceLastUpdate.TotalMinutes > 5)
                return 0.7f;
            else
                return 1f;
        }

        private float CalculatePatternConsistency(PlayerProfile profile)
        {
            return profile.behaviorPattern.consistency;
        }
    }

    public enum ConditionType
    {
        Engagement,
        SkillLevel,
        ProgressRate,
        TimeInSession,
        ConfidenceLevel,
        FrustrationLevel,
        CollaborationActivity,
        Custom
    }

    public enum ComparisonOperator
    {
        Equal,
        NotEqual,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual
    }

    public enum LogicalOperator
    {
        And,
        Or,
        Not
    }

    #endregion

    #region Narrative and Content Generation

    [Serializable]
    public class NarrativeEvent
    {
        public string narrativeId;
        public string playerId;
        public NarrativeType narrativeType;
        public string title;
        public string description;
        public DateTime triggerTime;
        public TimeSpan duration;
        public List<NarrativeChoice> choices = new();
        public Dictionary<string, object> narrativeData = new();
        public bool isCompleted;
        public string completionResult;
    }

    [Serializable]
    public class NarrativeChoice
    {
        public string choiceId;
        public string choiceText;
        public string description;
        public Dictionary<string, object> consequences = new();
        public bool isAvailable = true;
        public List<string> requirements = new();
    }

    [Serializable]
    public class EmergentStory
    {
        public string storyId;
        public string storyType;
        public string title;
        public string synopsis;
        public List<string> involvedPlayers = new();
        public DateTime startTime;
        public TimeSpan estimatedDuration;
        public StoryComplexity complexity;
        public float playerImpact;
        public Dictionary<string, object> storyElements = new();
    }

    [Serializable]
    public class ActiveStoryline
    {
        public string storylineId;
        public string playerId;
        public string narrativeType;
        public DateTime startTime;
        public TimeSpan estimatedDuration;
        public float progress;
        public bool isActive = true;
        public bool isCompleted;
        public bool hasExpired;
        public List<string> completedEvents = new();
        public string currentEvent;
    }

    [Serializable]
    public class NarrativeOpportunity
    {
        public string opportunityId;
        public string narrativeType;
        public string trigger;
        public float relevanceScore;
        public TimeSpan estimatedDuration;
        public List<string> requiredElements = new();
        public Dictionary<string, object> opportunityData = new();
    }

    [Serializable]
    public class ContentAdaptation
    {
        public string adaptationId;
        public string playerId;
        public ContentAdaptationType adaptationType;
        public string reason;
        public Dictionary<string, object> adaptationParameters = new();
        public DateTime adaptationTime;
        public float expectedImpact;
    }

    public enum NarrativeType
    {
        Discovery,
        Achievement,
        Collaboration,
        Challenge,
        Mystery,
        Celebration,
        Tutorial,
        Reflection
    }

    public enum StoryComplexity
    {
        Simple,
        Moderate,
        Complex,
        Epic
    }

    public enum ContentAdaptationType
    {
        DifficultyAdjustment,
        ContentVariation,
        PacingChange,
        StyleAdaptation,
        ModalityChange,
        PersonalizationUpdate
    }

    #endregion

    #region Player Analysis

    [Serializable]
    public class PlayerAnalysis
    {
        public string playerId;
        public DateTime analysisTime;
        public SkillLevel estimatedSkillLevel;
        public float engagementScore;
        public float learningVelocity;
        public DifficultyLevel preferredChallengeLevel;
        public float collaborationFrequency;
        public LearningStyle preferredLearningStyle;
        public List<string> strengthAreas = new();
        public List<string> improvementAreas = new();
        public PlayerBehaviorPattern behaviorPattern = new();
        public PredictedOutcomes predictions = new();
        public float analysisConfidence;
    }

    [Serializable]
    public class PlayerBehaviorPattern
    {
        public float consistency;
        public float explorationTendency;
        public float socialInteraction;
        public float persistenceLevel;
        public float creativityIndex;
        public float riskTaking;
        public List<string> commonActionSequences = new();
        public Dictionary<string, float> activityPreferences = new();
        public TimePattern timePattern = new();
    }

    [Serializable]
    public class TimePattern
    {
        public TimeSpan averageSessionLength;
        public TimeSpan preferredSessionTime;
        public float sessionConsistency;
        public List<string> preferredDays = new();
        public Dictionary<string, float> hourlyActivity = new();
    }

    [Serializable]
    public class PredictedOutcomes
    {
        public float likelihoodOfContinuation;
        public float expectedProgressRate;
        public float riskOfDisengagement;
        public float collaborationPotential;
        public List<string> likelyNextActions = new();
        public Dictionary<string, float> outcomeConfidences = new();
    }

    [Serializable]
    public class PersonalizationSettings
    {
        public DifficultyLevel preferredDifficulty = DifficultyLevel.Medium;
        public float hintFrequency = 0.5f;
        public bool enableCollaborativeFeatures = true;
        public bool enableNarrativeElements = true;
        public float pacePreference = 1f;
        public List<string> preferredContentTypes = new();
        public Dictionary<string, object> customPreferences = new();
    }

    #endregion

    #region Difficulty and Scaffolding

    [Serializable]
    public class DifficultyAdjustment
    {
        public string playerId;
        public DifficultyAdjustmentType adjustmentType;
        public float magnitude;
        public string reason;
        public DateTime timestamp;
        public TimeSpan duration;
        public bool isTemporary = true;
        public Dictionary<string, object> adjustmentParameters = new();
    }

    [Serializable]
    public class EducationalScaffolding
    {
        public string scaffoldingId;
        public string playerId;
        public ScaffoldingType scaffoldingType;
        public string content;
        public DateTime providedTime;
        public bool wasAccepted;
        public bool wasEffective;
        public Dictionary<string, object> scaffoldingData = new();
    }

    [Serializable]
    public class HintSystem
    {
        public string hintId;
        public string playerId;
        public HintType hintType;
        public string hintContent;
        public float urgency;
        public bool isContextual = true;
        public List<string> triggerConditions = new();
        public DateTime availableTime;
        public TimeSpan expirationTime;
    }

    public enum DifficultyAdjustmentType
    {
        Increase,
        Decrease,
        Reset,
        Adaptive,
        Custom
    }

    public enum ScaffoldingType
    {
        Hint,
        Explanation,
        Example,
        Guidance,
        Support,
        Encouragement,
        Clarification
    }

    public enum HintType
    {
        Procedural,
        Conceptual,
        Strategic,
        Motivational,
        Navigational,
        Social
    }

    #endregion

    #region Emergent Systems

    [Serializable]
    public class EmergentTrends
    {
        public DateTime lastUpdate;
        public List<string> emergingInterests = new();
        public List<string> popularActivities = new();
        public Dictionary<string, float> collaborationPatterns = new();
        public List<string> commonChallenges = new();
        public float overallEngagementTrend;
        public Dictionary<string, float> skillProgressionRates = new();
    }

    [Serializable]
    public class CollaborativeOpportunity
    {
        public string opportunityId;
        public List<string> potentialParticipants = new();
        public string collaborationType;
        public string suggestedActivity;
        public float compatibilityScore;
        public DateTime suggestedTime;
        public Dictionary<string, object> opportunityDetails = new();
    }

    [Serializable]
    public class DirectorDecisionResult
    {
        public bool wasSuccessful;
        public float effectivenessScore;
        public string outcome;
        public Dictionary<string, object> metrics = new();
        public List<string> observedEffects = new();
        public DateTime evaluationTime;
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Player behavior analysis and profiling service
    /// </summary>
    public interface IPlayerAnalysisService
    {
        Task<bool> InitializeAsync();
        void InitializePlayer(string playerId, PlayerRegistrationData registrationData);
        void TrackPlayerAction(string playerId, DirectorEvent directorEvent);
        void TrackDiscovery(string playerId, string discoveryType);
        void TrackCollaboration(string playerId, string collaborationType);
        PlayerAnalysis AnalyzePlayer(string playerId);
        PlayerBehaviorPattern GetBehaviorPattern(string playerId);
    }

    /// <summary>
    /// Dynamic difficulty adaptation service
    /// </summary>
    public interface IDifficultyAdaptationService
    {
        Task<bool> InitializeAsync();
        void ApplyDifficultyAdjustment(string playerId, DifficultyAdjustment adjustment);
        DifficultyLevel GetCurrentDifficulty(string playerId);
        DifficultyAdjustment RecommendAdjustment(string playerId, PlayerProfile profile, DirectorContext context);
        void ResetDifficulty(string playerId);
    }

    /// <summary>
    /// Narrative generation and storytelling service
    /// </summary>
    public interface INarrativeGenerationService
    {
        Task<bool> InitializeAsync();
        NarrativeEvent GenerateNarrative(string playerId, string narrativeType);
        void TriggerAchievementNarrative(string playerId, string achievementType);
        void GenerateMilestoneCelebration(string playerId, string milestoneType);
        void CelebrateAchievement(string playerId, string achievementType, string celebrationType);
        List<NarrativeOpportunity> AnalyzeDiscoveryOpportunities(string playerId, string discoveryType);
    }

    /// <summary>
    /// Content orchestration and spawning service
    /// </summary>
    public interface IContentOrchestrationService
    {
        Task<bool> InitializeAsync();
        void SpawnContent(string playerId, string contentType, object spawnParameters);
        void EncourageCollaboration(string playerId, string collaborationType, List<string> suggestedPartners);
        List<ContentAdaptation> GenerateAdaptations(PlayerProfile profile, DirectorContext context);
        List<CollaborativeOpportunity> FindCollaborativeOpportunities(string playerId);
    }

    /// <summary>
    /// Emergent storytelling and content generation service
    /// </summary>
    public interface IEmergentStorytellingService
    {
        Task<bool> InitializeAsync();
        List<EmergentStory> GenerateEmergentContent();
        List<ActiveStoryline> GenerateCollaborativeStorylines(string playerId, string collaborationType);
        void CompleteStoryline(ActiveStoryline storyline);
        EmergentTrends AnalyzeEmergentTrends();
    }

    /// <summary>
    /// Player behavior prediction service
    /// </summary>
    public interface IBehaviorPredictionService
    {
        Task<bool> InitializeAsync();
        PredictedOutcomes PredictPlayerOutcomes(string playerId, PlayerProfile profile);
        float PredictEngagementChange(string playerId, DirectorDecision decision);
        List<string> PredictNextActions(string playerId);
        float PredictCollaborationSuccess(List<string> playerIds);
    }

    /// <summary>
    /// Educational scaffolding and support service
    /// </summary>
    public interface IEducationalScaffoldingService
    {
        Task<bool> InitializeAsync();
        void ProvideSupport(string playerId, string struggleType);
        void ProvideHint(string playerId, string hintType, string hintContent);
        EducationalScaffolding GenerateScaffolding(string playerId, string context);
        bool ShouldProvideScaffolding(string playerId, PlayerProfile profile, DirectorContext context);
    }

    #endregion

    #region Request/Response Classes

    [Serializable]
    public class PlayerRegistrationData
    {
        public string playerName;
        public int age;
        public string gradeLevel;
        public LearningStyle learningStyle = LearningStyle.Mixed;
        public List<string> interests = new();
        public bool isEducationalContext = false;
        public string educatorId;
        public string classroomId;
        public Dictionary<string, object> customData = new();
    }

    [Serializable]
    public class DirectorQuery
    {
        public string playerId;
        public DirectorQueryType queryType;
        public Dictionary<string, object> queryParameters = new();
        public DateTime queryTime;
    }

    [Serializable]
    public class DirectorResponse
    {
        public bool success;
        public string message;
        public Dictionary<string, object> responseData = new();
        public List<DirectorDecision> recommendations = new();
        public DateTime responseTime;
    }

    public enum DirectorQueryType
    {
        GetRecommendations,
        AnalyzePlayer,
        PredictOutcomes,
        GenerateContent,
        EvaluateDecision
    }

    #endregion
}