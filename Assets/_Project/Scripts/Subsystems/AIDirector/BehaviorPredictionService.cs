using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Concrete implementation of behavior prediction service
    /// Predicts player behavior patterns and outcomes using machine learning-inspired algorithms
    /// </summary>
    public class BehaviorPredictionService : IBehaviorPredictionService
    {
        #region Fields

        private readonly AIDirectorSubsystemConfig _config;
        private Dictionary<string, PlayerBehaviorModel> _playerModels;
        private Dictionary<string, List<PlayerAction>> _actionHistory;
        private Dictionary<string, PredictionAccuracy> _predictionAccuracy;
        private Dictionary<DirectorDecisionType, OutcomeModel> _decisionOutcomeModels;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public BehaviorPredictionService(AIDirectorSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IBehaviorPredictionService Implementation

        public async Task<bool> InitializeAsync()
        {
            await Task.CompletedTask; // Synchronous initialization, but async for interface compatibility

            try
            {
                _playerModels = new Dictionary<string, PlayerBehaviorModel>();
                _actionHistory = new Dictionary<string, List<PlayerAction>>();
                _predictionAccuracy = new Dictionary<string, PredictionAccuracy>();
                _decisionOutcomeModels = new Dictionary<DirectorDecisionType, OutcomeModel>();

                // Initialize decision outcome models
                InitializeDecisionOutcomeModels();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[BehaviorPredictionService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BehaviorPredictionService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public PredictedOutcomes PredictPlayerOutcomes(string playerId, PlayerProfile profile)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId) || profile == null)
                return new PredictedOutcomes();

            var model = GetOrCreatePlayerModel(playerId);
            UpdatePlayerModel(model, profile);

            var predictions = new PredictedOutcomes
            {
                likelihoodOfContinuation = PredictContinuationLikelihood(model, profile),
                expectedProgressRate = PredictProgressRate(model, profile),
                riskOfDisengagement = PredictDisengagementRisk(model, profile),
                collaborationPotential = PredictCollaborationPotential(model, profile),
                likelyNextActions = PredictNextActions(model, profile),
                outcomeConfidences = CalculateOutcomeConfidences(model, profile)
            };

            if (_config.enableDebugLogging)
                Debug.Log($"[BehaviorPredictionService] Generated predictions for {playerId}: continuation={predictions.likelihoodOfContinuation:F2}, progress={predictions.expectedProgressRate:F2}");

            return predictions;
        }

        public float PredictEngagementChange(string playerId, DirectorDecision decision)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId) || decision == null)
                return 0f;

            var model = GetOrCreatePlayerModel(playerId);
            var outcomeModel = _decisionOutcomeModels.GetValueOrDefault(decision.decisionType, new OutcomeModel());

            var engagementChange = CalculateEngagementImpact(model, decision, outcomeModel);

            if (_config.enableDebugLogging)
                Debug.Log($"[BehaviorPredictionService] Predicted engagement change for {playerId} from {decision.decisionType}: {engagementChange:F2}");

            return engagementChange;
        }

        public List<string> PredictNextActions(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new List<string>();

            var model = GetOrCreatePlayerModel(playerId);
            var predictions = GenerateActionPredictions(model);

            if (_config.enableDebugLogging)
                Debug.Log($"[BehaviorPredictionService] Predicted {predictions.Count} next actions for {playerId}");

            return predictions;
        }

        public float PredictCollaborationSuccess(List<string> playerIds)
        {
            if (!_isInitialized || playerIds == null || playerIds.Count < 2)
                return 0f;

            var compatibilityScore = CalculateGroupCompatibility(playerIds);
            var collaborationHistory = AnalyzeCollaborationHistory(playerIds);
            var skillBalance = CalculateSkillBalance(playerIds);

            var successProbability = (compatibilityScore + collaborationHistory + skillBalance) / 3f;

            if (_config.enableDebugLogging)
                Debug.Log($"[BehaviorPredictionService] Predicted collaboration success for {playerIds.Count} players: {successProbability:F2}");

            return successProbability;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Records a player action to improve prediction accuracy
        /// </summary>
        public void RecordPlayerAction(string playerId, string actionType, DateTime timestamp, Dictionary<string, object> actionData = null)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var action = new PlayerAction
            {
                playerId = playerId,
                actionType = actionType,
                timestamp = timestamp,
                actionData = actionData ?? new Dictionary<string, object>()
            };

            if (!_actionHistory.ContainsKey(playerId))
                _actionHistory[playerId] = new List<PlayerAction>();

            _actionHistory[playerId].Add(action);

            // Keep history manageable
            if (_actionHistory[playerId].Count > 500)
                _actionHistory[playerId].RemoveAt(0);

            // Update prediction accuracy if we have a previous prediction
            UpdatePredictionAccuracy(playerId, action);
        }

        /// <summary>
        /// Gets prediction accuracy metrics for a player
        /// </summary>
        public PredictionAccuracy GetPredictionAccuracy(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new PredictionAccuracy();

            return _predictionAccuracy.GetValueOrDefault(playerId, new PredictionAccuracy());
        }

        #endregion

        #region Private Methods

        private void InitializeDecisionOutcomeModels()
        {
            _decisionOutcomeModels[DirectorDecisionType.AdjustDifficulty] = new OutcomeModel
            {
                baseEngagementImpact = 0.1f,
                variabilityFactor = 0.3f,
                playerTypeModifiers = new Dictionary<string, float>
                {
                    ["beginner"] = 0.2f,
                    ["intermediate"] = 0.1f,
                    ["advanced"] = 0.05f
                }
            };

            _decisionOutcomeModels[DirectorDecisionType.TriggerNarrative] = new OutcomeModel
            {
                baseEngagementImpact = 0.15f,
                variabilityFactor = 0.2f,
                playerTypeModifiers = new Dictionary<string, float>
                {
                    ["story_lover"] = 0.3f,
                    ["task_focused"] = 0.05f
                }
            };

            _decisionOutcomeModels[DirectorDecisionType.ProvideHint] = new OutcomeModel
            {
                baseEngagementImpact = 0.05f,
                variabilityFactor = 0.4f,
                playerTypeModifiers = new Dictionary<string, float>
                {
                    ["struggling"] = 0.2f,
                    ["independent"] = -0.1f
                }
            };

            _decisionOutcomeModels[DirectorDecisionType.EncourageCollaboration] = new OutcomeModel
            {
                baseEngagementImpact = 0.12f,
                variabilityFactor = 0.5f,
                playerTypeModifiers = new Dictionary<string, float>
                {
                    ["social"] = 0.25f,
                    ["solitary"] = -0.05f
                }
            };

            _decisionOutcomeModels[DirectorDecisionType.CelebrateAchievement] = new OutcomeModel
            {
                baseEngagementImpact = 0.08f,
                variabilityFactor = 0.1f,
                playerTypeModifiers = new Dictionary<string, float>
                {
                    ["achievement_oriented"] = 0.2f,
                    ["intrinsically_motivated"] = 0.1f
                }
            };

            _decisionOutcomeModels[DirectorDecisionType.SpawnContent] = new OutcomeModel
            {
                baseEngagementImpact = 0.1f,
                variabilityFactor = 0.3f,
                playerTypeModifiers = new Dictionary<string, float>
                {
                    ["explorer"] = 0.2f,
                    ["completionist"] = 0.15f
                }
            };
        }

        private PlayerBehaviorModel GetOrCreatePlayerModel(string playerId)
        {
            if (!_playerModels.ContainsKey(playerId))
            {
                _playerModels[playerId] = new PlayerBehaviorModel
                {
                    playerId = playerId,
                    actionPatterns = new Dictionary<string, ActionPattern>(),
                    engagementTrends = new List<float>(),
                    sessionPatterns = new Dictionary<string, float>(),
                    lastModelUpdate = DateTime.Now
                };
            }

            return _playerModels[playerId];
        }

        private void UpdatePlayerModel(PlayerBehaviorModel model, PlayerProfile profile)
        {
            model.lastModelUpdate = DateTime.Now;

            // Update engagement trends
            model.engagementTrends.Add(profile.engagementScore);
            if (model.engagementTrends.Count > 20) // Keep last 20 data points
                model.engagementTrends.RemoveAt(0);

            // Update action patterns from history
            if (_actionHistory.TryGetValue(model.playerId, out var actions))
            {
                UpdateActionPatterns(model, actions);
            }

            // Update session patterns
            UpdateSessionPatterns(model, profile);
        }

        private void UpdateActionPatterns(PlayerBehaviorModel model, List<PlayerAction> actions)
        {
            // Analyze recent actions (last 50)
            var recentActions = actions.Count > 50 ? actions.GetRange(actions.Count - 50, 50) : actions;

            foreach (var action in recentActions)
            {
                if (!model.actionPatterns.ContainsKey(action.actionType))
                {
                    model.actionPatterns[action.actionType] = new ActionPattern
                    {
                        actionType = action.actionType,
                        frequency = 0,
                        averageTimeBetween = TimeSpan.Zero,
                        successRate = 0.5f,
                        lastOccurrence = action.timestamp
                    };
                }

                var pattern = model.actionPatterns[action.actionType];
                pattern.frequency++;
                pattern.lastOccurrence = action.timestamp;

                // Update success rate if we have outcome data
                if (action.actionData.TryGetValue("success", out var success) && success is bool isSuccessful)
                {
                    pattern.successRate = (pattern.successRate + (isSuccessful ? 1f : 0f)) / 2f;
                }
            }

            // Calculate average time between actions
            foreach (var pattern in model.actionPatterns.Values)
            {
                var actionTimes = recentActions
                    .Where(a => a.actionType == pattern.actionType)
                    .Select(a => a.timestamp)
                    .OrderBy(t => t)
                    .ToList();

                if (actionTimes.Count > 1)
                {
                    var totalTime = actionTimes.Last() - actionTimes.First();
                    pattern.averageTimeBetween = TimeSpan.FromTicks(totalTime.Ticks / (actionTimes.Count - 1));
                }
            }
        }

        private void UpdateSessionPatterns(PlayerBehaviorModel model, PlayerProfile profile)
        {
            var sessionLength = DateTime.Now - profile.sessionStartTime;
            var hour = DateTime.Now.Hour.ToString();

            model.sessionPatterns["current_session_length"] = (float)sessionLength.TotalMinutes;
            model.sessionPatterns[$"hour_{hour}_activity"] = model.sessionPatterns.GetValueOrDefault($"hour_{hour}_activity", 0f) + 1f;
        }

        // Prediction methods
        private float PredictContinuationLikelihood(PlayerBehaviorModel model, PlayerProfile profile)
        {
            var engagementFactor = profile.engagementScore;
            var sessionLengthFactor = CalculateSessionLengthFactor(profile.sessionStartTime);
            var trendFactor = CalculateEngagementTrend(model);

            var likelihood = (engagementFactor + sessionLengthFactor + trendFactor) / 3f;
            return math.clamp(likelihood, 0f, 1f);
        }

        private float PredictProgressRate(PlayerBehaviorModel model, PlayerProfile profile)
        {
            var baseLearningVelocity = profile.learningVelocity;
            var engagementBonus = profile.engagementScore * 0.5f;
            var consistencyBonus = profile.behaviorPattern.consistency * 0.3f;

            var predictedRate = baseLearningVelocity + engagementBonus + consistencyBonus;
            return math.clamp(predictedRate, 0f, 3f);
        }

        private float PredictDisengagementRisk(PlayerBehaviorModel model, PlayerProfile profile)
        {
            var lowEngagement = 1f - profile.engagementScore;
            var longSession = CalculateSessionFatigueRisk(profile.sessionStartTime);
            var negativeEngagementTrend = CalculateNegativeEngagementTrend(model);
            var frustrationLevel = profile.customAttributes.GetValueOrDefault("frustrationLevel", 0f);

            if (frustrationLevel is float frustration)
            {
                var riskScore = (lowEngagement + longSession + negativeEngagementTrend + frustration) / 4f;
                return math.clamp(riskScore, 0f, 1f);
            }

            var basicRiskScore = (lowEngagement + longSession + negativeEngagementTrend) / 3f;
            return math.clamp(basicRiskScore, 0f, 1f);
        }

        private float PredictCollaborationPotential(PlayerBehaviorModel model, PlayerProfile profile)
        {
            var socialInteraction = profile.behaviorPattern.socialInteraction;
            var collaborationHistory = profile.collaborationFrequency;
            var educationalContext = profile.isEducationalContext ? 0.2f : 0f;

            var potential = (socialInteraction + collaborationHistory + educationalContext) / 2.2f;
            return math.clamp(potential, 0f, 1f);
        }

        private List<string> PredictNextActions(PlayerBehaviorModel model, PlayerProfile profile)
        {
            var predictions = new List<string>();

            // Predict based on action patterns
            foreach (var pattern in model.actionPatterns.Values)
            {
                var timeSinceLastAction = DateTime.Now - pattern.lastOccurrence;
                var expectedInterval = pattern.averageTimeBetween;

                if (timeSinceLastAction >= expectedInterval.Multiply(0.8)) // 80% of expected interval
                {
                    predictions.Add(pattern.actionType);
                }
            }

            // Add behavior-based predictions
            if (profile.behaviorPattern.explorationTendency > 0.6f)
                predictions.Add("exploration");

            if (profile.behaviorPattern.socialInteraction > 0.5f)
                predictions.Add("collaboration");

            if (profile.engagementScore > 0.7f)
                predictions.Add("advanced_task");

            return predictions.Take(5).ToList(); // Return top 5 predictions
        }

        private Dictionary<string, float> CalculateOutcomeConfidences(PlayerBehaviorModel model, PlayerProfile profile)
        {
            var confidences = new Dictionary<string, float>();

            // Calculate confidence based on data quality and consistency
            var dataQuality = CalculateDataQuality(model);
            var patternConsistency = profile.behaviorPattern.consistency;
            var baseConfidence = (dataQuality + patternConsistency) / 2f;

            confidences["continuation"] = math.clamp(baseConfidence + 0.1f, 0f, 1f);
            confidences["progress"] = math.clamp(baseConfidence, 0f, 1f);
            confidences["disengagement"] = math.clamp(baseConfidence - 0.1f, 0f, 1f);
            confidences["collaboration"] = math.clamp(baseConfidence + (profile.behaviorPattern.socialInteraction * 0.2f), 0f, 1f);

            return confidences;
        }

        // Helper calculation methods
        private float CalculateSessionLengthFactor(DateTime sessionStart)
        {
            var sessionLength = DateTime.Now - sessionStart;
            var optimalLength = 30; // minutes

            if (sessionLength.TotalMinutes <= optimalLength)
                return (float)(sessionLength.TotalMinutes / optimalLength);
            else
                return math.max(0.2f, 1f - (float)(sessionLength.TotalMinutes - optimalLength) / 60f);
        }

        private float CalculateEngagementTrend(PlayerBehaviorModel model)
        {
            if (model.engagementTrends.Count < 3)
                return 0.5f; // Neutral if insufficient data

            var recent = model.engagementTrends.TakeLast(5).ToList();
            var older = model.engagementTrends.Take(model.engagementTrends.Count - 5).ToList();

            if (older.Count == 0)
                return 0.5f;

            var recentAvg = recent.Average();
            var olderAvg = older.Average();

            return math.clamp(recentAvg - olderAvg + 0.5f, 0f, 1f);
        }

        private float CalculateSessionFatigueRisk(DateTime sessionStart)
        {
            var sessionLength = DateTime.Now - sessionStart;
            var fatigueThreshold = 90; // minutes

            if (sessionLength.TotalMinutes <= fatigueThreshold)
                return 0f;
            else
                return math.min(1f, (float)(sessionLength.TotalMinutes - fatigueThreshold) / 60f);
        }

        private float CalculateNegativeEngagementTrend(PlayerBehaviorModel model)
        {
            if (model.engagementTrends.Count < 3)
                return 0f;

            var trend = CalculateEngagementTrend(model);
            return math.max(0f, 0.5f - trend); // Invert positive trend to get negative risk
        }

        private float CalculateDataQuality(PlayerBehaviorModel model)
        {
            var recencyFactor = CalculateRecencyFactor(model.lastModelUpdate);
            var volumeFactor = CalculateVolumeFactor(model.actionPatterns.Count);
            var consistencyFactor = CalculateConsistencyFactor(model.engagementTrends);

            return (recencyFactor + volumeFactor + consistencyFactor) / 3f;
        }

        private float CalculateRecencyFactor(DateTime lastUpdate)
        {
            var timeSinceUpdate = DateTime.Now - lastUpdate;
            if (timeSinceUpdate.TotalMinutes <= 5)
                return 1f;
            else if (timeSinceUpdate.TotalMinutes <= 15)
                return 0.7f;
            else
                return 0.3f;
        }

        private float CalculateVolumeFactor(int actionPatternCount)
        {
            return math.min(1f, actionPatternCount / 10f); // Optimal at 10+ action types
        }

        private float CalculateConsistencyFactor(List<float> engagementTrends)
        {
            if (engagementTrends.Count < 3)
                return 0.5f;

            var variance = CalculateVariance(engagementTrends);
            return math.max(0f, 1f - variance); // Lower variance = higher consistency
        }

        private float CalculateVariance(List<float> values)
        {
            if (values.Count == 0)
                return 0f;

            var mean = values.Average();
            var squaredDifferences = values.Select(x => (x - mean) * (x - mean));
            return squaredDifferences.Average();
        }

        private float CalculateEngagementImpact(PlayerBehaviorModel model, DirectorDecision decision, OutcomeModel outcomeModel)
        {
            var baseImpact = outcomeModel.baseEngagementImpact;
            var playerTypeModifier = GetPlayerTypeModifier(model, outcomeModel);
            var contextModifier = CalculateContextModifier(decision);

            var totalImpact = baseImpact + playerTypeModifier + contextModifier;
            var variability = UnityEngine.Random.Range(-outcomeModel.variabilityFactor, outcomeModel.variabilityFactor);

            return totalImpact + variability;
        }

        private float GetPlayerTypeModifier(PlayerBehaviorModel model, OutcomeModel outcomeModel)
        {
            // Simplified player type detection
            var playerType = DeterminePlayerType(model);
            return outcomeModel.playerTypeModifiers.GetValueOrDefault(playerType, 0f);
        }

        private string DeterminePlayerType(PlayerBehaviorModel model)
        {
            if (model.actionPatterns.Count == 0)
                return "unknown";

            var explorationActions = model.actionPatterns.GetValueOrDefault("exploration", new ActionPattern()).frequency;
            var collaborationActions = model.actionPatterns.GetValueOrDefault("collaboration", new ActionPattern()).frequency;
            var achievementActions = model.actionPatterns.GetValueOrDefault("achievement", new ActionPattern()).frequency;

            if (explorationActions > collaborationActions && explorationActions > achievementActions)
                return "explorer";
            else if (collaborationActions > explorationActions)
                return "social";
            else if (achievementActions > 0)
                return "achievement_oriented";
            else
                return "general";
        }

        private float CalculateContextModifier(DirectorDecision decision)
        {
            // Modify impact based on decision context
            var confidenceModifier = (decision.confidence - 0.5f) * 0.1f;
            var priorityModifier = ((int)decision.priority - 2) * 0.05f; // Medium = 2

            return confidenceModifier + priorityModifier;
        }

        private float CalculateGroupCompatibility(List<string> playerIds)
        {
            if (playerIds.Count < 2)
                return 0f;

            var totalCompatibility = 0f;
            var comparisons = 0;

            for (int i = 0; i < playerIds.Count; i++)
            {
                for (int j = i + 1; j < playerIds.Count; j++)
                {
                    var compatibility = CalculatePairCompatibility(playerIds[i], playerIds[j]);
                    totalCompatibility += compatibility;
                    comparisons++;
                }
            }

            return comparisons > 0 ? totalCompatibility / comparisons : 0f;
        }

        private float CalculatePairCompatibility(string playerId1, string playerId2)
        {
            // Simplified compatibility calculation
            // In a real implementation, this would consider player profiles, play styles, etc.
            var model1 = _playerModels.GetValueOrDefault(playerId1, null);
            var model2 = _playerModels.GetValueOrDefault(playerId2, null);

            if (model1 == null || model2 == null)
                return 0.5f; // Neutral compatibility if no data

            // Compare action patterns
            var commonActions = 0;
            var totalActions = new HashSet<string>();

            foreach (var action in model1.actionPatterns.Keys)
                totalActions.Add(action);
            foreach (var action in model2.actionPatterns.Keys)
                totalActions.Add(action);

            foreach (var action in model1.actionPatterns.Keys)
            {
                if (model2.actionPatterns.ContainsKey(action))
                    commonActions++;
            }

            var actionCompatibility = totalActions.Count > 0 ? (float)commonActions / totalActions.Count : 0.5f;
            return math.clamp(actionCompatibility, 0f, 1f);
        }

        private float AnalyzeCollaborationHistory(List<string> playerIds)
        {
            // Simplified collaboration history analysis
            // Return moderate score as baseline
            return 0.6f;
        }

        private float CalculateSkillBalance(List<string> playerIds)
        {
            // Simplified skill balance calculation
            // In a real implementation, this would analyze player skill levels and complementarity
            return 0.7f;
        }

        private void UpdatePredictionAccuracy(string playerId, PlayerAction actualAction)
        {
            if (!_predictionAccuracy.ContainsKey(playerId))
            {
                _predictionAccuracy[playerId] = new PredictionAccuracy();
            }

            var accuracy = _predictionAccuracy[playerId];
            accuracy.totalPredictions++;

            // Check if we predicted this action
            var model = _playerModels.GetValueOrDefault(playerId, null);
            if (model != null)
            {
                var predictions = GenerateActionPredictions(model);
                if (predictions.Contains(actualAction.actionType))
                {
                    accuracy.correctPredictions++;
                }
            }

            accuracy.accuracyRate = (float)accuracy.correctPredictions / accuracy.totalPredictions;
        }

        private List<string> GenerateActionPredictions(PlayerBehaviorModel model)
        {
            var predictions = new List<string>();

            foreach (var pattern in model.actionPatterns.Values)
            {
                var timeSinceLastAction = DateTime.Now - pattern.lastOccurrence;
                if (timeSinceLastAction >= pattern.averageTimeBetween.Multiply(0.8))
                {
                    predictions.Add(pattern.actionType);
                }
            }

            return predictions;
        }

        #endregion

        #region Helper Classes

        private class PlayerBehaviorModel
        {
            public string playerId;
            public Dictionary<string, ActionPattern> actionPatterns;
            public List<float> engagementTrends;
            public Dictionary<string, float> sessionPatterns;
            public DateTime lastModelUpdate;
        }

        private class ActionPattern
        {
            public string actionType;
            public int frequency;
            public TimeSpan averageTimeBetween;
            public float successRate;
            public DateTime lastOccurrence;
        }

        private class PlayerAction
        {
            public string playerId;
            public string actionType;
            public DateTime timestamp;
            public Dictionary<string, object> actionData;
        }

        private class OutcomeModel
        {
            public float baseEngagementImpact;
            public float variabilityFactor;
            public Dictionary<string, float> playerTypeModifiers = new();
        }

        public class PredictionAccuracy
        {
            public int totalPredictions;
            public int correctPredictions;
            public float accuracyRate;
        }

        #endregion
    }

    #region Extension Methods

    public static class TimeSpanExtensions
    {
        public static TimeSpan Multiply(this TimeSpan timeSpan, double factor)
        {
            return TimeSpan.FromTicks((long)(timeSpan.Ticks * factor));
        }
    }

    public static class LinqExtensions
    {
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            var list = source.ToList();
            return list.Skip(Math.Max(0, list.Count - count));
        }

        public static float Average(this IEnumerable<float> source)
        {
            var sum = 0f;
            var count = 0;
            foreach (var value in source)
            {
                sum += value;
                count++;
            }
            return count > 0 ? sum / count : 0f;
        }
    }

    #endregion
}