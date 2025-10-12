using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;
using System.Linq;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// Concrete implementation of player behavior analysis service
    /// Analyzes player behavior patterns, skill development, and engagement metrics
    /// </summary>
    public class PlayerAnalysisService : IPlayerAnalysisService
    {
        #region Fields

        private readonly AIDirectorSubsystemConfig _config;
        private Dictionary<string, PlayerProfile> _playerProfiles;
        private Dictionary<string, List<DirectorEvent>> _playerEventHistory;
        private Dictionary<string, PlayerAnalysis> _playerAnalyses;
        private Dictionary<string, DateTime> _lastAnalysisTime;
        private bool _isInitialized;

        #endregion

        #region Constructor

        public PlayerAnalysisService(AIDirectorSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region IPlayerAnalysisService Implementation

        public async Task<bool> InitializeAsync()
        {
            try
            {
                _playerProfiles = new Dictionary<string, PlayerProfile>();
                _playerEventHistory = new Dictionary<string, List<DirectorEvent>>();
                _playerAnalyses = new Dictionary<string, PlayerAnalysis>();
                _lastAnalysisTime = new Dictionary<string, DateTime>();

                _isInitialized = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[PlayerAnalysisService] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerAnalysisService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void InitializePlayer(string playerId, PlayerRegistrationData registrationData)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var profile = new PlayerProfile
            {
                playerId = playerId,
                registrationDate = DateTime.Now,
                sessionStartTime = DateTime.Now,
                lastActivity = DateTime.Now,
                learningStyle = registrationData.learningStyle,
                interests = new List<string>(registrationData.interests),
                isEducationalContext = registrationData.isEducationalContext,
                gradeLevel = registrationData.gradeLevel
            };

            _playerProfiles[playerId] = profile;
            _playerEventHistory[playerId] = new List<DirectorEvent>();
            _lastAnalysisTime[playerId] = DateTime.Now;

            if (_config.enableDebugLogging)
                Debug.Log($"[PlayerAnalysisService] Initialized player: {playerId}");
        }

        public void TrackPlayerAction(string playerId, DirectorEvent directorEvent)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId) || directorEvent == null)
                return;

            if (!_playerEventHistory.ContainsKey(playerId))
            {
                _playerEventHistory[playerId] = new List<DirectorEvent>();
            }

            _playerEventHistory[playerId].Add(directorEvent);

            // Keep event history manageable
            var maxHistorySize = 1000;
            if (_playerEventHistory[playerId].Count > maxHistorySize)
            {
                _playerEventHistory[playerId].RemoveAt(0);
            }

            // Update player profile
            UpdatePlayerProfileFromEvent(playerId, directorEvent);
        }

        public void TrackDiscovery(string playerId, string discoveryType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var directorEvent = new DirectorEvent
            {
                eventType = DirectorEventType.Discovery,
                playerId = playerId,
                timestamp = DateTime.Now,
                data = new Dictionary<string, object>
                {
                    ["discoveryType"] = discoveryType,
                    ["significance"] = CalculateDiscoverySignificance(discoveryType)
                }
            };

            TrackPlayerAction(playerId, directorEvent);
        }

        public void TrackCollaboration(string playerId, string collaborationType)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return;

            var directorEvent = new DirectorEvent
            {
                eventType = DirectorEventType.Collaboration,
                playerId = playerId,
                timestamp = DateTime.Now,
                data = new Dictionary<string, object>
                {
                    ["collaborationType"] = collaborationType
                }
            };

            TrackPlayerAction(playerId, directorEvent);

            // Update collaboration frequency
            if (_playerProfiles.TryGetValue(playerId, out var profile))
            {
                profile.collaborationFrequency = math.min(profile.collaborationFrequency + 0.1f, 1f);
            }
        }

        public PlayerAnalysis AnalyzePlayer(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return null;

            if (!_playerProfiles.ContainsKey(playerId))
                return null;

            var profile = _playerProfiles[playerId];
            var analysis = GeneratePlayerAnalysis(playerId, profile);

            _playerAnalyses[playerId] = analysis;
            _lastAnalysisTime[playerId] = DateTime.Now;

            return analysis;
        }

        public PlayerBehaviorPattern GetBehaviorPattern(string playerId)
        {
            if (!_isInitialized || string.IsNullOrEmpty(playerId))
                return new PlayerBehaviorPattern();

            if (_playerProfiles.TryGetValue(playerId, out var profile))
            {
                return profile.behaviorPattern;
            }

            return new PlayerBehaviorPattern();
        }

        #endregion

        #region Private Methods

        private void UpdatePlayerProfileFromEvent(string playerId, DirectorEvent directorEvent)
        {
            if (!_playerProfiles.TryGetValue(playerId, out var profile))
                return;

            profile.lastActivity = directorEvent.timestamp;

            // Update engagement based on event type
            var engagementDelta = CalculateEngagementDelta(directorEvent);
            profile.engagementScore = math.clamp(profile.engagementScore + engagementDelta, 0f, 1f);

            // Update behavior pattern
            UpdateBehaviorPattern(profile, directorEvent);
        }

        private float CalculateEngagementDelta(DirectorEvent directorEvent)
        {
            return directorEvent.eventType switch
            {
                DirectorEventType.Discovery => 0.15f,
                DirectorEventType.Achievement => 0.12f,
                DirectorEventType.Collaboration => 0.10f,
                DirectorEventType.Milestone => 0.08f,
                DirectorEventType.PlayerAction => 0.02f,
                DirectorEventType.Struggle => -0.05f,
                _ => 0.01f
            };
        }

        private void UpdateBehaviorPattern(PlayerProfile profile, DirectorEvent directorEvent)
        {
            var pattern = profile.behaviorPattern;

            // Update exploration tendency
            if (directorEvent.eventType == DirectorEventType.Discovery)
            {
                pattern.explorationTendency = math.min(pattern.explorationTendency + 0.05f, 1f);
            }

            // Update social interaction
            if (directorEvent.eventType == DirectorEventType.Collaboration)
            {
                pattern.socialInteraction = math.min(pattern.socialInteraction + 0.08f, 1f);
            }

            // Update persistence level
            if (directorEvent.eventType == DirectorEventType.Struggle)
            {
                var actionType = directorEvent.data.GetValueOrDefault("actionType", "").ToString();
                if (actionType.Contains("retry") || actionType.Contains("continue"))
                {
                    pattern.persistenceLevel = math.min(pattern.persistenceLevel + 0.1f, 1f);
                }
            }

            // Update activity preferences
            var activityType = GetActivityTypeFromEvent(directorEvent);
            if (!string.IsNullOrEmpty(activityType))
            {
                if (!pattern.activityPreferences.ContainsKey(activityType))
                    pattern.activityPreferences[activityType] = 0f;

                pattern.activityPreferences[activityType] = math.min(
                    pattern.activityPreferences[activityType] + 0.05f, 1f);
            }

            // Update consistency
            UpdateConsistency(pattern, directorEvent);
        }

        private string GetActivityTypeFromEvent(DirectorEvent directorEvent)
        {
            if (directorEvent.data.TryGetValue("actionType", out var actionType))
            {
                var action = actionType.ToString();
                if (action.Contains("breeding")) return "breeding";
                if (action.Contains("research")) return "research";
                if (action.Contains("exploration")) return "exploration";
                if (action.Contains("collaboration")) return "collaboration";
            }

            return directorEvent.eventType switch
            {
                DirectorEventType.Discovery => "discovery",
                DirectorEventType.Collaboration => "collaboration",
                DirectorEventType.Achievement => "achievement",
                _ => ""
            };
        }

        private void UpdateConsistency(PlayerBehaviorPattern pattern, DirectorEvent directorEvent)
        {
            // Simple consistency calculation based on action timing
            var now = directorEvent.timestamp;
            var hour = now.Hour;

            if (!pattern.timePattern.hourlyActivity.ContainsKey(hour.ToString()))
                pattern.timePattern.hourlyActivity[hour.ToString()] = 0f;

            pattern.timePattern.hourlyActivity[hour.ToString()] += 0.1f;

            // Calculate consistency as variance in hourly activity
            var activities = pattern.timePattern.hourlyActivity.Values;
            if (activities.Count > 1)
            {
                var mean = 0f;
                foreach (var activity in activities) mean += activity;
                mean /= activities.Count;

                var variance = 0f;
                foreach (var activity in activities)
                    variance += (activity - mean) * (activity - mean);
                variance /= activities.Count;

                // Lower variance = higher consistency
                pattern.consistency = math.max(0f, 1f - variance);
            }
        }

        private PlayerAnalysis GeneratePlayerAnalysis(string playerId, PlayerProfile profile)
        {
            var analysis = new PlayerAnalysis
            {
                playerId = playerId,
                analysisTime = DateTime.Now,
                analysisConfidence = CalculateAnalysisConfidence(profile)
            };

            // Analyze skill level
            analysis.estimatedSkillLevel = EstimateSkillLevel(profile);

            // Calculate engagement score
            analysis.engagementScore = CalculateEngagementScore(profile);

            // Calculate learning velocity
            analysis.learningVelocity = CalculateLearningVelocity(profile);

            // Determine preferred challenge level
            analysis.preferredChallengeLevel = DeterminePreferredChallengeLevel(profile);

            // Set collaboration frequency
            analysis.collaborationFrequency = profile.collaborationFrequency;

            // Determine preferred learning style
            analysis.preferredLearningStyle = DeterminePreferredLearningStyle(profile);

            // Identify strengths and improvement areas
            IdentifyStrengthsAndImprovements(profile, analysis);

            // Set behavior pattern
            analysis.behaviorPattern = profile.behaviorPattern;

            // Generate predictions
            analysis.predictions = GeneratePredictions(profile);

            return analysis;
        }

        private float CalculateAnalysisConfidence(PlayerProfile profile)
        {
            var sessionTime = (float)(DateTime.Now - profile.sessionStartTime).TotalMinutes;
            var activityFrequency = (float)(DateTime.Now - profile.lastActivity).TotalMinutes;

            // More session time and recent activity = higher confidence
            var timeConfidence = math.min(sessionTime / 60f, 1f); // Normalize to 1 hour
            var activityConfidence = math.max(0f, 1f - activityFrequency / 10f); // Decay over 10 minutes

            return (timeConfidence + activityConfidence) / 2f;
        }

        private SkillLevel EstimateSkillLevel(PlayerProfile profile)
        {
            var baseSkill = (float)profile.skillLevel;
            var engagementBonus = profile.engagementScore * 0.5f;
            var progressBonus = profile.progressionScore * 0.3f;
            var learningBonus = profile.learningVelocity * 0.2f;

            var totalSkill = baseSkill + engagementBonus + progressBonus + learningBonus;

            return totalSkill switch
            {
                < 1.5f => SkillLevel.Beginner,
                < 2.5f => SkillLevel.Novice,
                < 3.5f => SkillLevel.Intermediate,
                < 4.5f => SkillLevel.Advanced,
                _ => SkillLevel.Expert
            };
        }

        private float CalculateEngagementScore(PlayerProfile profile)
        {
            var baseEngagement = profile.engagementScore;
            var timeFactor = CalculateTimeFactor(profile.lastActivity);
            var sessionFactor = CalculateSessionFactor(profile.sessionStartTime);

            return math.clamp(baseEngagement * timeFactor * sessionFactor, 0f, 1f);
        }

        private float CalculateTimeFactor(DateTime lastActivity)
        {
            var timeSinceActivity = DateTime.Now - lastActivity;
            var decayRate = _config.engagementDecayRate;
            return math.exp(-(float)timeSinceActivity.TotalMinutes * decayRate);
        }

        private float CalculateSessionFactor(DateTime sessionStart)
        {
            var sessionLength = DateTime.Now - sessionStart;
            var optimalSessionLength = 30; // minutes

            // Engagement peaks around 30 minutes, then gradually decreases
            if (sessionLength.TotalMinutes <= optimalSessionLength)
            {
                return (float)(sessionLength.TotalMinutes / optimalSessionLength);
            }
            else
            {
                var overtime = sessionLength.TotalMinutes - optimalSessionLength;
                return math.max(0.3f, 1f - (float)(overtime / 60f)); // Gradual decrease over 1 hour
            }
        }

        private float CalculateLearningVelocity(PlayerProfile profile)
        {
            if (_playerEventHistory.TryGetValue(profile.playerId, out var events))
            {
                var recentEvents = events.FindAll(e =>
                    (DateTime.Now - e.timestamp).TotalMinutes <= 30);

                var discoveryCount = recentEvents.Where(e => e.eventType == DirectorEventType.Discovery).Count();
                var achievementCount = recentEvents.Where(e => e.eventType == DirectorEventType.Achievement).Count();

                // Learning velocity based on recent discoveries and achievements
                return math.min((discoveryCount * 0.2f) + (achievementCount * 0.15f), 2f);
            }

            return profile.learningVelocity;
        }

        private DifficultyLevel DeterminePreferredChallengeLevel(PlayerProfile profile)
        {
            var skillFactor = (float)profile.skillLevel / 4f; // Normalize to 0-1
            var engagementFactor = profile.engagementScore;
            var confidenceFactor = profile.confidenceLevel;

            var challengeScore = (skillFactor + engagementFactor + confidenceFactor) / 3f;

            return challengeScore switch
            {
                < 0.2f => DifficultyLevel.VeryEasy,
                < 0.4f => DifficultyLevel.Easy,
                < 0.6f => DifficultyLevel.Medium,
                < 0.8f => DifficultyLevel.Hard,
                _ => DifficultyLevel.VeryHard
            };
        }

        private LearningStyle DeterminePreferredLearningStyle(PlayerProfile profile)
        {
            var pattern = profile.behaviorPattern;

            // Analyze activity preferences to infer learning style
            var visualPreference = pattern.activityPreferences.GetValueOrDefault("exploration", 0f) +
                                 pattern.activityPreferences.GetValueOrDefault("discovery", 0f);

            var kinestheticPreference = pattern.activityPreferences.GetValueOrDefault("breeding", 0f) +
                                      pattern.activityPreferences.GetValueOrDefault("collaboration", 0f);

            var readingPreference = pattern.activityPreferences.GetValueOrDefault("research", 0f);

            if (visualPreference > kinestheticPreference && visualPreference > readingPreference)
                return LearningStyle.Visual;
            else if (kinestheticPreference > readingPreference)
                return LearningStyle.Kinesthetic;
            else if (readingPreference > 0.3f)
                return LearningStyle.ReadingWriting;
            else
                return LearningStyle.Mixed;
        }

        private void IdentifyStrengthsAndImprovements(PlayerProfile profile, PlayerAnalysis analysis)
        {
            var pattern = profile.behaviorPattern;

            // Identify strengths
            if (pattern.explorationTendency > 0.7f)
                analysis.strengthAreas.Add("exploration");
            if (pattern.socialInteraction > 0.6f)
                analysis.strengthAreas.Add("collaboration");
            if (pattern.persistenceLevel > 0.8f)
                analysis.strengthAreas.Add("persistence");
            if (profile.engagementScore > 0.7f)
                analysis.strengthAreas.Add("engagement");

            // Identify improvement areas
            if (pattern.socialInteraction < 0.3f)
                analysis.improvementAreas.Add("collaboration");
            if (pattern.persistenceLevel < 0.4f)
                analysis.improvementAreas.Add("persistence");
            if (profile.confidenceLevel < 0.5f)
                analysis.improvementAreas.Add("confidence");
            if (profile.learningVelocity < 0.5f)
                analysis.improvementAreas.Add("learning_pace");
        }

        private PredictedOutcomes GeneratePredictions(PlayerProfile profile)
        {
            var predictions = new PredictedOutcomes();

            // Predict likelihood of continuation
            var engagementFactor = profile.engagementScore;
            var sessionFactor = CalculateSessionFactor(profile.sessionStartTime);
            predictions.likelihoodOfContinuation = (engagementFactor + sessionFactor) / 2f;

            // Predict expected progress rate
            predictions.expectedProgressRate = profile.learningVelocity * profile.engagementScore;

            // Predict risk of disengagement
            var timeSinceActivity = (float)(DateTime.Now - profile.lastActivity).TotalMinutes;
            var disengagementRisk = math.min(timeSinceActivity / 15f, 1f); // Risk increases over 15 minutes
            predictions.riskOfDisengagement = disengagementRisk * (1f - profile.engagementScore);

            // Predict collaboration potential
            predictions.collaborationPotential = profile.behaviorPattern.socialInteraction *
                                               (profile.isEducationalContext ? 1.2f : 1f);

            // Predict likely next actions
            GenerateLikelyNextActions(profile, predictions);

            return predictions;
        }

        private void GenerateLikelyNextActions(PlayerProfile profile, PredictedOutcomes predictions)
        {
            var pattern = profile.behaviorPattern;

            // Base predictions on behavior pattern
            if (pattern.explorationTendency > 0.6f)
                predictions.likelyNextActions.Add("exploration");

            if (pattern.socialInteraction > 0.5f)
                predictions.likelyNextActions.Add("collaboration");

            if (profile.engagementScore > 0.7f)
                predictions.likelyNextActions.Add("discovery_attempt");

            if (pattern.activityPreferences.GetValueOrDefault("breeding", 0f) > 0.4f)
                predictions.likelyNextActions.Add("breeding");

            if (pattern.activityPreferences.GetValueOrDefault("research", 0f) > 0.4f)
                predictions.likelyNextActions.Add("research");

            // Add confidence scores
            foreach (var action in predictions.likelyNextActions)
            {
                var confidence = CalculateActionConfidence(profile, action);
                predictions.outcomeConfidences[action] = confidence;
            }
        }

        private float CalculateActionConfidence(PlayerProfile profile, string action)
        {
            var baseConfidence = 0.5f;
            var activityPreference = profile.behaviorPattern.activityPreferences.GetValueOrDefault(action, 0f);
            var engagementBonus = profile.engagementScore * 0.3f;

            return math.clamp(baseConfidence + activityPreference + engagementBonus, 0f, 1f);
        }

        private float CalculateDiscoverySignificance(string discoveryType)
        {
            return discoveryType switch
            {
                "rare_mutation" => 0.9f,
                "new_species" => 0.8f,
                "genetic_trait" => 0.6f,
                "behavioral_pattern" => 0.5f,
                "environmental_factor" => 0.4f,
                _ => 0.3f
            };
        }

        #endregion
    }
}