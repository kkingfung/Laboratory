using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Configuration ScriptableObject for the AI Director Subsystem.
    /// Controls intelligent game direction, content adaptation, and emergent storytelling.
    /// </summary>
    [CreateAssetMenu(fileName = "AIDirectorSubsystemConfig", menuName = "Project Chimera/Subsystems/AI Director Config")]
    public class AIDirectorSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Director update interval in milliseconds")]
        [Range(1000, 30000)]
        public int directorUpdateIntervalMs = 5000;

        [Tooltip("Enable debug logging for AI Director operations")]
        public bool enableDebugLogging = false;

        [Tooltip("Maximum events processed per update cycle")]
        [Range(1, 100)]
        public int maxEventsPerUpdate = 20;

        [Tooltip("Maximum decisions per player per update")]
        [Range(1, 10)]
        public int maxDecisionsPerPlayer = 3;

        [Tooltip("Player analysis update interval in minutes")]
        [Range(1, 60)]
        public int playerAnalysisIntervalMinutes = 5;

        [Header("Player Analysis")]
        [Tooltip("Enable real-time player behavior analysis")]
        public bool enablePlayerAnalysis = true;

        [Tooltip("Engagement weight in decision making")]
        [Range(0f, 1f)]
        public float engagementWeight = 0.4f;

        [Tooltip("Skill level weight in decision making")]
        [Range(0f, 1f)]
        public float skillWeight = 0.3f;

        [Tooltip("Progress rate weight in decision making")]
        [Range(0f, 1f)]
        public float progressWeight = 0.2f;

        [Tooltip("Social activity weight in decision making")]
        [Range(0f, 1f)]
        public float socialWeight = 0.1f;

        [Tooltip("Educational goals weight in decision making")]
        [Range(0f, 1f)]
        public float educationalWeight = 0.5f;

        [Tooltip("Engagement decay rate per minute")]
        [Range(0.001f, 0.1f)]
        public float engagementDecayRate = 0.01f;

        [Header("Difficulty Adaptation")]
        [Tooltip("Enable dynamic difficulty adjustment")]
        public bool enableDynamicDifficulty = true;

        [Tooltip("Minimum difficulty level")]
        public DifficultyLevel minimumDifficulty = DifficultyLevel.VeryEasy;

        [Tooltip("Maximum difficulty level")]
        public DifficultyLevel maximumDifficulty = DifficultyLevel.VeryHard;

        [Tooltip("Difficulty adjustment magnitude")]
        [Range(0.1f, 1f)]
        public float difficultyAdjustmentMagnitude = 0.2f;

        [Tooltip("Difficulty adjustment frequency limit (minutes)")]
        [Range(1f, 30f)]
        public float difficultyAdjustmentCooldown = 5f;

        [Tooltip("Engagement threshold for difficulty increase")]
        [Range(0.5f, 1f)]
        public float difficultyIncreaseThreshold = 0.8f;

        [Tooltip("Engagement threshold for difficulty decrease")]
        [Range(0f, 0.5f)]
        public float difficultyDecreaseThreshold = 0.3f;

        [Header("Narrative Generation")]
        [Tooltip("Enable AI-driven narrative generation")]
        public bool enableNarrativeGeneration = true;

        [Tooltip("Enable emergent story generation")]
        public bool enableEmergentStoryGeneration = true;

        [Tooltip("Enable collaborative storylines")]
        public bool enableCollaborativeStorylines = true;

        [Tooltip("Narrative trigger sensitivity")]
        [Range(0.1f, 1f)]
        public float narrativeTriggerSensitivity = 0.6f;

        [Tooltip("Maximum active narratives per player")]
        [Range(1, 10)]
        public int maxActiveNarrativesPerPlayer = 3;

        [Tooltip("Narrative completion bonus weight")]
        [Range(0.1f, 2f)]
        public float narrativeCompletionBonus = 1.5f;

        [Header("Content Orchestration")]
        [Tooltip("Enable intelligent content spawning")]
        public bool enableIntelligentContentSpawning = true;

        [Tooltip("Enable collaborative content suggestions")]
        public bool enableCollaborativeContentSuggestions = true;

        [Tooltip("Content adaptation aggressiveness")]
        [Range(0.1f, 1f)]
        public float contentAdaptationAggressiveness = 0.5f;

        [Tooltip("Minimum time between content adaptations (minutes)")]
        [Range(1f, 60f)]
        public float contentAdaptationCooldown = 10f;

        [Tooltip("Content relevance threshold")]
        [Range(0.1f, 1f)]
        public float contentRelevanceThreshold = 0.7f;

        [Header("Educational Scaffolding")]
        [Tooltip("Enable educational scaffolding")]
        public bool enableEducationalScaffolding = true;

        [Tooltip("Struggle detection threshold (consecutive failures)")]
        [Range(2, 10)]
        public int struggleDetectionThreshold = 3;

        [Tooltip("Hint provision frequency")]
        [Range(0.1f, 1f)]
        public float hintProvisionFrequency = 0.5f;

        [Tooltip("Scaffolding effectiveness tracking")]
        public bool trackScaffoldingEffectiveness = true;

        [Tooltip("Maximum scaffolding instances per session")]
        [Range(1, 20)]
        public int maxScaffoldingPerSession = 10;

        [Header("Behavior Prediction")]
        [Tooltip("Enable player behavior prediction")]
        public bool enableBehaviorPrediction = true;

        [Tooltip("Prediction accuracy threshold")]
        [Range(0.5f, 0.95f)]
        public float predictionAccuracyThreshold = 0.7f;

        [Tooltip("Prediction window size (minutes)")]
        [Range(5f, 120f)]
        public float predictionWindowMinutes = 30f;

        [Tooltip("Enable predictive interventions")]
        public bool enablePredictiveInterventions = true;

        [Header("Collaboration Enhancement")]
        [Tooltip("Enable collaboration orchestration")]
        public bool enableCollaborationOrchestration = true;

        [Tooltip("Collaboration matching algorithm")]
        public CollaborationMatchingAlgorithm collaborationMatchingAlgorithm = CollaborationMatchingAlgorithm.SkillComplementarity;

        [Tooltip("Minimum collaboration compatibility score")]
        [Range(0.1f, 1f)]
        public float minimumCollaborationCompatibility = 0.6f;

        [Tooltip("Maximum collaboration group size")]
        [Range(2, 8)]
        public int maxCollaborationGroupSize = 4;

        [Tooltip("Collaboration suggestion frequency (minutes)")]
        [Range(5f, 60f)]
        public float collaborationSuggestionFrequency = 15f;

        [Header("Emergent Systems")]
        [Tooltip("Enable emergent trend analysis")]
        public bool enableEmergentTrendAnalysis = true;

        [Tooltip("Trend analysis window (hours)")]
        [Range(1f, 168f)]
        public float trendAnalysisWindowHours = 24f;

        [Tooltip("Trend significance threshold")]
        [Range(0.1f, 1f)]
        public float trendSignificanceThreshold = 0.3f;

        [Tooltip("Enable cross-player influence modeling")]
        public bool enableCrossPlayerInfluence = true;

        [Header("AI Decision Making")]
        [Tooltip("Decision confidence threshold")]
        [Range(0.1f, 1f)]
        public float decisionConfidenceThreshold = 0.6f;

        [Tooltip("Enable decision outcome tracking")]
        public bool enableDecisionOutcomeTracking = true;

        [Tooltip("Decision success learning rate")]
        [Range(0.01f, 0.5f)]
        public float decisionLearningRate = 0.1f;

        [Tooltip("Enable adaptive rule weights")]
        public bool enableAdaptiveRuleWeights = true;

        [Header("Director Rules")]
        [Tooltip("AI Director decision rules")]
        public List<DirectorRule> directorRules = new List<DirectorRule>();

        [Header("Performance")]
        [Tooltip("Enable AI performance optimization")]
        public bool enableAIPerformanceOptimization = true;

        [Tooltip("Analysis processing batch size")]
        [Range(1, 50)]
        public int analysisProcessingBatchSize = 10;

        [Tooltip("Decision cache duration (minutes)")]
        [Range(1f, 60f)]
        public float decisionCacheDurationMinutes = 5f;

        [Tooltip("Enable background AI processing")]
        public bool enableBackgroundAIProcessing = true;

        [Header("Privacy and Ethics")]
        [Tooltip("Enable privacy-preserving analytics")]
        public bool enablePrivacyPreservingAnalytics = true;

        [Tooltip("Data anonymization level")]
        public DataAnonymizationLevel dataAnonymizationLevel = DataAnonymizationLevel.Medium;

        [Tooltip("Enable ethical AI guidelines")]
        public bool enableEthicalAIGuidelines = true;

        [Tooltip("Bias detection and mitigation")]
        public bool enableBiasDetection = true;

        [Header("Accessibility")]
        [Tooltip("Enable accessibility-aware AI direction")]
        public bool enableAccessibilityAwareDirection = true;

        [Tooltip("Adaptation for learning differences")]
        public bool enableLearningDifferenceAdaptation = true;

        [Tooltip("Multi-modal content support")]
        public bool enableMultiModalContentSupport = true;

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable update intervals
            directorUpdateIntervalMs = Mathf.Max(1000, directorUpdateIntervalMs);
            playerAnalysisIntervalMinutes = Mathf.Max(1, playerAnalysisIntervalMinutes);

            // Ensure weight values sum to reasonable totals
            ValidateWeights();

            // Ensure thresholds are properly ordered
            difficultyDecreaseThreshold = Mathf.Min(difficultyDecreaseThreshold, 0.5f);
            difficultyIncreaseThreshold = Mathf.Max(difficultyIncreaseThreshold, 0.5f);

            // Ensure reasonable batch sizes and limits
            maxEventsPerUpdate = Mathf.Max(1, maxEventsPerUpdate);
            maxDecisionsPerPlayer = Mathf.Max(1, maxDecisionsPerPlayer);
            analysisProcessingBatchSize = Mathf.Max(1, analysisProcessingBatchSize);

            // Ensure director rules have defaults
            if (directorRules.Count == 0)
            {
                directorRules.AddRange(CreateDefaultDirectorRules());
            }

            // Validate collaboration settings
            maxCollaborationGroupSize = Mathf.Max(2, maxCollaborationGroupSize);
            minimumCollaborationCompatibility = Mathf.Clamp(minimumCollaborationCompatibility, 0.1f, 1f);

            // Validate prediction settings
            predictionAccuracyThreshold = Mathf.Clamp(predictionAccuracyThreshold, 0.5f, 0.95f);
            predictionWindowMinutes = Mathf.Clamp(predictionWindowMinutes, 5f, 120f);
        }

        private void ValidateWeights()
        {
            // Normalize weights if they exceed reasonable bounds
            var totalWeight = engagementWeight + skillWeight + progressWeight + socialWeight;
            if (totalWeight > 1.2f) // Allow some tolerance
            {
                var scale = 1f / totalWeight;
                engagementWeight *= scale;
                skillWeight *= scale;
                progressWeight *= scale;
                socialWeight *= scale;
            }
        }

        private List<DirectorRule> CreateDefaultDirectorRules()
        {
            return new List<DirectorRule>
            {
                CreateEngagementBoostRule(),
                CreateDifficultyAdjustmentRule(),
                CreateCollaborationRule(),
                CreateHintProvisionRule(),
                CreateAchievementCelebrationRule(),
                CreateStruggleInterventionRule()
            };
        }

        private DirectorRule CreateEngagementBoostRule()
        {
            return new DirectorRule
            {
                ruleId = "engagement_boost",
                ruleName = "Engagement Boost",
                description = "Triggers engaging content when player engagement is low",
                priority = 0.8f,
                isEnabled = true,
                condition = new RuleCondition
                {
                    conditionType = ConditionType.Engagement,
                    parameter = "engagement",
                    comparisonOperator = ComparisonOperator.LessThan,
                    threshold = 0.4f
                },
                action = new RuleAction
                {
                    actionType = DirectorDecisionType.TriggerNarrative,
                    actionDescription = "Trigger engaging narrative content",
                    actionParameters = new Dictionary<string, object>
                    {
                        ["narrativeType"] = "discovery",
                        ["urgency"] = "medium"
                    }
                }
            };
        }

        private DirectorRule CreateDifficultyAdjustmentRule()
        {
            return new DirectorRule
            {
                ruleId = "difficulty_adjustment",
                ruleName = "Difficulty Adjustment",
                description = "Adjusts difficulty based on player performance",
                priority = 0.7f,
                isEnabled = true,
                condition = new RuleCondition
                {
                    conditionType = ConditionType.FrustrationLevel,
                    parameter = "frustrationLevel",
                    comparisonOperator = ComparisonOperator.GreaterThan,
                    threshold = 0.7f
                },
                action = new RuleAction
                {
                    actionType = DirectorDecisionType.AdjustDifficulty,
                    actionDescription = "Reduce difficulty to prevent frustration",
                    actionParameters = new Dictionary<string, object>
                    {
                        ["adjustmentType"] = DifficultyAdjustmentType.Decrease,
                        ["magnitude"] = 0.2f
                    }
                }
            };
        }

        private DirectorRule CreateCollaborationRule()
        {
            return new DirectorRule
            {
                ruleId = "collaboration_encouragement",
                ruleName = "Collaboration Encouragement",
                description = "Encourages collaboration when beneficial",
                priority = 0.6f,
                isEnabled = true,
                condition = new RuleCondition
                {
                    conditionType = ConditionType.CollaborationActivity,
                    parameter = "socialActivity",
                    comparisonOperator = ComparisonOperator.LessThan,
                    threshold = 0.3f
                },
                action = new RuleAction
                {
                    actionType = DirectorDecisionType.EncourageCollaboration,
                    actionDescription = "Suggest collaborative activities",
                    actionParameters = new Dictionary<string, object>
                    {
                        ["collaborationType"] = "discovery",
                        ["groupSize"] = 2
                    }
                }
            };
        }

        private DirectorRule CreateHintProvisionRule()
        {
            return new DirectorRule
            {
                ruleId = "hint_provision",
                ruleName = "Hint Provision",
                description = "Provides hints when player is struggling",
                priority = 0.9f,
                isEnabled = true,
                condition = new RuleCondition
                {
                    conditionType = ConditionType.ProgressRate,
                    parameter = "progressRate",
                    comparisonOperator = ComparisonOperator.LessThan,
                    threshold = 0.2f
                },
                action = new RuleAction
                {
                    actionType = DirectorDecisionType.ProvideHint,
                    actionDescription = "Provide contextual hint",
                    actionParameters = new Dictionary<string, object>
                    {
                        ["hintType"] = "contextual",
                        ["urgency"] = "high"
                    }
                }
            };
        }

        private DirectorRule CreateAchievementCelebrationRule()
        {
            return new DirectorRule
            {
                ruleId = "achievement_celebration",
                ruleName = "Achievement Celebration",
                description = "Celebrates significant achievements",
                priority = 0.5f,
                isEnabled = true,
                condition = new RuleCondition
                {
                    conditionType = ConditionType.ConfidenceLevel,
                    parameter = "confidenceLevel",
                    comparisonOperator = ComparisonOperator.GreaterThan,
                    threshold = 0.8f
                },
                action = new RuleAction
                {
                    actionType = DirectorDecisionType.CelebrateAchievement,
                    actionDescription = "Celebrate player achievement",
                    actionParameters = new Dictionary<string, object>
                    {
                        ["celebrationType"] = "narrative",
                        ["intensity"] = "high"
                    }
                }
            };
        }

        private DirectorRule CreateStruggleInterventionRule()
        {
            return new DirectorRule
            {
                ruleId = "struggle_intervention",
                ruleName = "Struggle Intervention",
                description = "Intervenes when player shows signs of struggle",
                priority = 1.0f,
                isEnabled = true,
                condition = new RuleCondition
                {
                    conditionType = ConditionType.FrustrationLevel,
                    parameter = "frustrationLevel",
                    comparisonOperator = ComparisonOperator.GreaterThan,
                    threshold = 0.8f,
                    subConditions = new List<RuleCondition>
                    {
                        new RuleCondition
                        {
                            conditionType = ConditionType.Engagement,
                            parameter = "engagement",
                            comparisonOperator = ComparisonOperator.LessThan,
                            threshold = 0.3f
                        }
                    },
                    logicalOperator = LogicalOperator.And
                },
                action = new RuleAction
                {
                    actionType = DirectorDecisionType.ProvideHint,
                    actionDescription = "Emergency intervention for struggling player",
                    actionParameters = new Dictionary<string, object>
                    {
                        ["hintType"] = "supportive",
                        ["urgency"] = "critical",
                        ["includeEncouragement"] = true
                    }
                }
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets director rule by ID
        /// </summary>
        public DirectorRule GetDirectorRule(string ruleId)
        {
            return directorRules.Find(rule => rule.ruleId == ruleId);
        }

        /// <summary>
        /// Gets enabled director rules
        /// </summary>
        public List<DirectorRule> GetEnabledRules()
        {
            return directorRules.FindAll(rule => rule.isEnabled);
        }

        /// <summary>
        /// Calculates decision priority based on context
        /// </summary>
        public DirectorPriority CalculateDecisionPriority(float engagement, float frustration, float progress)
        {
            if (frustration > 0.8f || engagement < 0.2f)
                return DirectorPriority.Critical;
            else if (frustration > 0.6f || engagement < 0.4f || progress < 0.3f)
                return DirectorPriority.High;
            else if (engagement > 0.8f && progress > 0.7f)
                return DirectorPriority.Low;
            else
                return DirectorPriority.Medium;
        }

        /// <summary>
        /// Determines if difficulty adjustment is needed
        /// </summary>
        public bool ShouldAdjustDifficulty(float engagement, float skillLevel, float progressRate)
        {
            return enableDynamicDifficulty &&
                   (engagement < difficultyDecreaseThreshold || engagement > difficultyIncreaseThreshold) &&
                   (progressRate < 0.3f || progressRate > 0.9f);
        }

        /// <summary>
        /// Calculates collaboration compatibility score
        /// </summary>
        public float CalculateCollaborationCompatibility(PlayerProfile player1, PlayerProfile player2)
        {
            if (!enableCollaborationOrchestration)
                return 0f;

            var skillBalance = 1f - Mathf.Abs((float)player1.skillLevel - (float)player2.skillLevel) / (float)SkillLevel.Expert;
            var interestOverlap = CalculateInterestOverlap(player1.interests, player2.interests);
            var learningStyleCompatibility = CalculateLearningStyleCompatibility(player1.learningStyle, player2.learningStyle);

            return collaborationMatchingAlgorithm switch
            {
                CollaborationMatchingAlgorithm.SkillComplementarity => (skillBalance + interestOverlap) / 2f,
                CollaborationMatchingAlgorithm.InterestBased => interestOverlap,
                CollaborationMatchingAlgorithm.LearningStyleMatch => learningStyleCompatibility,
                CollaborationMatchingAlgorithm.Balanced => (skillBalance + interestOverlap + learningStyleCompatibility) / 3f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Determines if narrative should be triggered
        /// </summary>
        public bool ShouldTriggerNarrative(float engagement, int activeNarratives, string narrativeType)
        {
            if (!enableNarrativeGeneration || activeNarratives >= maxActiveNarrativesPerPlayer)
                return false;

            var baseThreshold = narrativeTriggerSensitivity;
            var adjustedThreshold = baseThreshold * (1f - engagement * 0.5f); // Lower threshold for lower engagement

            return UnityEngine.Random.value < adjustedThreshold;
        }

        /// <summary>
        /// Gets recommended scaffolding for player state
        /// </summary>
        public ScaffoldingType GetRecommendedScaffolding(float engagement, float frustration, float progress)
        {
            if (frustration > 0.8f)
                return ScaffoldingType.Support;
            else if (engagement < 0.3f)
                return ScaffoldingType.Encouragement;
            else if (progress < 0.2f)
                return ScaffoldingType.Hint;
            else if (progress > 0.8f)
                return ScaffoldingType.Guidance;
            else
                return ScaffoldingType.Explanation;
        }

        /// <summary>
        /// Validates AI decision parameters
        /// </summary>
        public bool ValidateDecisionParameters(DirectorDecision decision)
        {
            if (decision.confidence < decisionConfidenceThreshold)
                return false;

            if (decision.priority == DirectorPriority.Critical && decision.confidence < 0.8f)
                return false;

            return true;
        }

        #endregion

        #region Helper Methods

        private float CalculateInterestOverlap(List<string> interests1, List<string> interests2)
        {
            if (interests1.Count == 0 || interests2.Count == 0)
                return 0f;

            var overlap = interests1.Intersect(interests2).Count();
            var total = interests1.Union(interests2).Count();

            return total > 0 ? (float)overlap / total : 0f;
        }

        private float CalculateLearningStyleCompatibility(LearningStyle style1, LearningStyle style2)
        {
            if (style1 == style2)
                return 1f;
            else if (style1 == LearningStyle.Mixed || style2 == LearningStyle.Mixed)
                return 0.8f;
            else
                return 0.3f; // Different styles can still be compatible
        }

        #endregion
    }

    #region Configuration Enums

    public enum CollaborationMatchingAlgorithm
    {
        SkillComplementarity,
        InterestBased,
        LearningStyleMatch,
        Balanced,
        Random
    }

    public enum DataAnonymizationLevel
    {
        None,
        Low,
        Medium,
        High,
        Complete
    }

    #endregion
}