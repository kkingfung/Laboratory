using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Laboratory.Subsystems.Analytics
{
    /// <summary>
    /// Player behavior tracking service implementation
    /// </summary>
    public class PlayerBehaviorTracker : IPlayerBehaviorTracker
    {
        private readonly AnalyticsSubsystemConfig _config;
        private readonly Dictionary<string, List<PlayerActionEvent>> _actionHistory = new();
        private readonly Dictionary<string, PlayerBehaviorPattern> _behaviorPatterns = new();
        private readonly BreedingPatternAnalytics _breedingPatterns = new();

        public PlayerBehaviorTracker(AnalyticsSubsystemConfig config)
        {
            _config = config;
        }

        public void ProcessPlayerAction(PlayerActionEvent actionEvent)
        {
            if (!_config.trackPlayerActions)
                return;

            // Add to action history
            if (!_actionHistory.ContainsKey(actionEvent.actionType))
                _actionHistory[actionEvent.actionType] = new List<PlayerActionEvent>();

            _actionHistory[actionEvent.actionType].Add(actionEvent);

            // Process breeding-specific actions
            if (actionEvent.actionType == "BreedingAttempt" && _config.trackBreedingPatterns)
            {
                ProcessBreedingAction(actionEvent);
            }

            // Update patterns in real-time for frequent actions
            if (_actionHistory[actionEvent.actionType].Count % 10 == 0)
            {
                UpdatePatternForAction(actionEvent.actionType);
            }
        }

        public void UpdateBehaviorPatterns()
        {
            foreach (var actionType in _actionHistory.Keys)
            {
                UpdatePatternForAction(actionType);
            }
        }

        public List<PlayerBehaviorPattern> GetBehaviorPatterns()
        {
            return _behaviorPatterns.Values.ToList();
        }

        public BreedingPatternAnalytics GetBreedingPatterns()
        {
            return _breedingPatterns;
        }

        public float GetEngagementScore()
        {
            var totalActions = _actionHistory.Values.Sum(list => list.Count);
            var uniqueActionTypes = _actionHistory.Keys.Count;
            var timeSpan = DateTime.Now - GetFirstActionTime();

            if (timeSpan.TotalHours == 0)
                return 0f;

            var actionsPerHour = totalActions / (float)timeSpan.TotalHours;
            var diversityScore = Mathf.Clamp01(uniqueActionTypes / 10f);

            return Mathf.Clamp01(actionsPerHour / 100f) * diversityScore;
        }

        private void ProcessBreedingAction(PlayerActionEvent actionEvent)
        {
            _breedingPatterns.totalBreedingAttempts++;

            if (actionEvent.parameters.TryGetValue("parent1Species", out var p1) &&
                actionEvent.parameters.TryGetValue("parent2Species", out var p2))
            {
                var combination = $"{p1}_{p2}";
                if (!_breedingPatterns.speciesCombinations.ContainsKey(combination))
                    _breedingPatterns.speciesCombinations[combination] = 0;
                _breedingPatterns.speciesCombinations[combination]++;
            }

            if (actionEvent.parameters.TryGetValue("successful", out var success) && success is bool isSuccessful)
            {
                var combination = $"{actionEvent.parameters.GetValueOrDefault("parent1Species", "Unknown")}_{actionEvent.parameters.GetValueOrDefault("parent2Species", "Unknown")}";
                if (!_breedingPatterns.successRates.ContainsKey(combination))
                    _breedingPatterns.successRates[combination] = 0f;

                var currentSuccesses = _breedingPatterns.successRates[combination] * _breedingPatterns.speciesCombinations.GetValueOrDefault(combination, 1);
                var totalAttempts = _breedingPatterns.speciesCombinations.GetValueOrDefault(combination, 1);

                if (isSuccessful)
                    currentSuccesses++;

                _breedingPatterns.successRates[combination] = currentSuccesses / totalAttempts;
            }
        }

        private void UpdatePatternForAction(string actionType)
        {
            if (!_actionHistory.TryGetValue(actionType, out var actions) || actions.Count == 0)
                return;

            var frequency = CalculateFrequency(actions);
            var lastOccurrence = actions.Max(a => a.timestamp);

            if (frequency >= _config.behaviorPatternThreshold)
            {
                _behaviorPatterns[actionType] = new PlayerBehaviorPattern
                {
                    patternType = actionType,
                    description = GeneratePatternDescription(actionType, frequency),
                    frequency = frequency,
                    lastOccurrence = lastOccurrence,
                    parameters = ExtractPatternParameters(actions)
                };
            }
        }

        private float CalculateFrequency(List<PlayerActionEvent> actions)
        {
            if (actions.Count < 2)
                return 0f;

            var timeSpan = actions.Max(a => a.timestamp) - actions.Min(a => a.timestamp);
            if (timeSpan.TotalHours == 0)
                return 0f;

            return (float)(actions.Count / timeSpan.TotalHours);
        }

        private string GeneratePatternDescription(string actionType, float frequency)
        {
            var frequencyDescription = frequency switch
            {
                > 10f => "Very Frequent",
                > 5f => "Frequent",
                > 2f => "Regular",
                _ => "Occasional"
            };

            return $"{frequencyDescription} {actionType} behavior ({frequency:F1} times per hour)";
        }

        private Dictionary<string, float> ExtractPatternParameters(List<PlayerActionEvent> actions)
        {
            var parameters = new Dictionary<string, float>();

            // Extract common numerical parameters
            foreach (var action in actions)
            {
                foreach (var param in action.parameters)
                {
                    if (param.Value is float floatValue)
                    {
                        if (!parameters.ContainsKey(param.Key))
                            parameters[param.Key] = 0f;
                        parameters[param.Key] += floatValue;
                    }
                    else if (param.Value is int intValue)
                    {
                        if (!parameters.ContainsKey(param.Key))
                            parameters[param.Key] = 0f;
                        parameters[param.Key] += intValue;
                    }
                }
            }

            // Calculate averages
            foreach (var key in parameters.Keys.ToList())
            {
                parameters[key] /= actions.Count;
            }

            return parameters;
        }

        private DateTime GetFirstActionTime()
        {
            var allActions = _actionHistory.Values.SelectMany(list => list);
            return allActions.Any() ? allActions.Min(a => a.timestamp) : DateTime.Now;
        }
    }

    /// <summary>
    /// Performance monitoring service implementation
    /// </summary>
    public class PerformanceMonitor : IPerformanceMonitor
    {
        private readonly AnalyticsSubsystemConfig _config;
        private readonly List<PerformanceMetrics> _metricsHistory = new();
        private readonly List<PerformanceAnomaly> _anomalies = new();

        public PerformanceMonitor(AnalyticsSubsystemConfig config)
        {
            _config = config;
        }

        public PerformanceMetrics CollectCurrentMetrics()
        {
            var metrics = new PerformanceMetrics
            {
                frameRate = _config.monitorFrameRate ? 1f / Time.unscaledDeltaTime : 0f,
                memoryUsage = _config.monitorMemoryUsage ? GC.GetTotalMemory(false) / (1024f * 1024f) : 0f,
                activeEntities = _config.monitorEntityCount ? GetActiveEntityCount() : 0,
                networkLatency = _config.monitorNetworkPerformance ? GetNetworkLatency() : 0f,
                loadTime = Time.realtimeSinceStartup,
                timestamp = DateTime.Now
            };

            _metricsHistory.Add(metrics);

            // Keep only recent metrics
            var cutoffTime = DateTime.Now.AddHours(-_config.rarityCalculationWindowHours);
            _metricsHistory.RemoveAll(m => m.timestamp < cutoffTime);

            return metrics;
        }

        public PerformanceAnalyticsSummary GetAnalyticsSummary()
        {
            if (_metricsHistory.Count == 0)
                return new PerformanceAnalyticsSummary();

            return new PerformanceAnalyticsSummary
            {
                averageFrameRate = _metricsHistory.Average(m => m.frameRate),
                minFrameRate = _metricsHistory.Min(m => m.frameRate),
                maxFrameRate = _metricsHistory.Max(m => m.frameRate),
                averageMemoryUsage = _metricsHistory.Average(m => m.memoryUsage),
                peakMemoryUsage = _metricsHistory.Max(m => m.memoryUsage),
                averageEntityCount = (int)_metricsHistory.Average(m => m.activeEntities),
                maxEntityCount = _metricsHistory.Max(m => m.activeEntities),
                averageNetworkLatency = _metricsHistory.Average(m => m.networkLatency),
                anomalies = _anomalies.ToList()
            };
        }

        public void CheckForAnomalies()
        {
            if (_metricsHistory.Count == 0)
                return;

            var latestMetrics = _metricsHistory.Last();

            // Check frame rate anomalies
            if (latestMetrics.frameRate < _config.frameRateAnomalyThreshold)
            {
                RecordAnomaly("LowFrameRate", 1f - (latestMetrics.frameRate / _config.frameRateAnomalyThreshold),
                    $"Frame rate dropped to {latestMetrics.frameRate:F1} FPS", latestMetrics);
            }

            // Check memory anomalies
            if (latestMetrics.memoryUsage > _config.memoryAnomalyThreshold)
            {
                RecordAnomaly("HighMemoryUsage", latestMetrics.memoryUsage / _config.memoryAnomalyThreshold - 1f,
                    $"Memory usage exceeded threshold: {latestMetrics.memoryUsage:F1} MB", latestMetrics);
            }

            // Clean up old anomalies
            var cutoffTime = DateTime.Now.AddHours(-24);
            _anomalies.RemoveAll(a => a.timestamp < cutoffTime);
        }

        public float GetAveragePerformance()
        {
            if (_metricsHistory.Count == 0)
                return 1f;

            var frameRateScore = Mathf.Clamp01(_metricsHistory.Average(m => m.frameRate) / 60f);
            var memoryScore = Mathf.Clamp01(1f - (_metricsHistory.Average(m => m.memoryUsage) / _config.memoryAnomalyThreshold));

            return (frameRateScore + memoryScore) / 2f;
        }

        public List<PerformanceAnomaly> GetRecentAnomalies()
        {
            var recentCutoff = DateTime.Now.AddHours(-1);
            return _anomalies.Where(a => a.timestamp >= recentCutoff).ToList();
        }

        private void RecordAnomaly(string type, float severity, string description, PerformanceMetrics metrics)
        {
            var anomaly = new PerformanceAnomaly
            {
                timestamp = DateTime.Now,
                anomalyType = type,
                severity = severity,
                description = description,
                metrics = new Dictionary<string, float>
                {
                    ["frameRate"] = metrics.frameRate,
                    ["memoryUsage"] = metrics.memoryUsage,
                    ["activeEntities"] = metrics.activeEntities,
                    ["networkLatency"] = metrics.networkLatency
                }
            };

            _anomalies.Add(anomaly);

            if (_config.enableDebugLogging)
            {
                Debug.LogWarning($"[PerformanceMonitor] Anomaly detected: {description} (Severity: {severity:F2})");
            }
        }

        private int GetActiveEntityCount()
        {
            // This would integrate with ECS entity count
            // For now, return a placeholder
            return UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().Length;
        }

        private float GetNetworkLatency()
        {
            // This would integrate with network monitoring
            // For now, return a placeholder
            return UnityEngine.Random.Range(10f, 100f);
        }
    }

    /// <summary>
    /// Educational analytics service implementation
    /// </summary>
    public class EducationalAnalytics : IEducationalAnalytics
    {
        private readonly AnalyticsSubsystemConfig _config;
        private readonly Dictionary<string, EducationalProgressEvent> _progressEvents = new();
        private readonly Dictionary<string, LearningPathAnalytics> _learningPaths = new();
        private readonly EducationalProgressSummary _sessionProgress = new();

        public EducationalAnalytics(AnalyticsSubsystemConfig config)
        {
            _config = config;
        }

        public void RecordProgress(EducationalProgressEvent progressEvent)
        {
            if (!_config.trackLearningProgress)
                return;

            _progressEvents[progressEvent.conceptMastered] = progressEvent;

            // Update session progress
            _sessionProgress.conceptMastery[progressEvent.conceptMastered] = progressEvent.confidenceLevel;

            if (!_sessionProgress.lessonCompletions.ContainsKey(progressEvent.lessonId))
                _sessionProgress.lessonCompletions[progressEvent.lessonId] = 0;
            _sessionProgress.lessonCompletions[progressEvent.lessonId]++;

            // Check for mastery
            if (progressEvent.confidenceLevel >= _config.masteryConfidenceThreshold)
            {
                if (!_sessionProgress.masteredConcepts.Contains(progressEvent.conceptMastered))
                    _sessionProgress.masteredConcepts.Add(progressEvent.conceptMastered);

                _sessionProgress.strugglingConcepts.Remove(progressEvent.conceptMastered);
            }
            else if (progressEvent.confidenceLevel < 0.5f)
            {
                if (!_sessionProgress.strugglingConcepts.Contains(progressEvent.conceptMastered))
                    _sessionProgress.strugglingConcepts.Add(progressEvent.conceptMastered);
            }

            // Update overall confidence
            if (_sessionProgress.conceptMastery.Count > 0)
            {
                _sessionProgress.overallConfidence = _sessionProgress.conceptMastery.Values.Average();
            }
        }

        public EducationalProgressSummary GetSessionProgress()
        {
            return _sessionProgress;
        }

        public LearningPathAnalytics GetLearningPath(string studentId)
        {
            return _learningPaths.GetValueOrDefault(studentId, new LearningPathAnalytics { studentId = studentId });
        }

        public float GetOverallProgress()
        {
            return _sessionProgress.overallConfidence;
        }

        public List<string> GetRecommendations()
        {
            if (!_config.generateRecommendations)
                return new List<string>();

            var recommendations = new List<string>();

            // Recommend focusing on struggling concepts
            foreach (var concept in _sessionProgress.strugglingConcepts)
            {
                recommendations.Add($"Practice {concept} more - current confidence: {_sessionProgress.conceptMastery.GetValueOrDefault(concept, 0f):P0}");
            }

            // Recommend advanced concepts for mastered areas
            foreach (var concept in _sessionProgress.masteredConcepts.Take(3))
            {
                recommendations.Add($"Try advanced {concept} concepts - you've mastered the basics!");
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Discovery metrics service implementation
    /// </summary>
    public class DiscoveryMetrics : IDiscoveryMetrics
    {
        private readonly AnalyticsSubsystemConfig _config;
        private readonly Dictionary<string, List<DiscoveryAnalyticsEvent>> _discoveries = new();
        private readonly DiscoveryRarityMetrics _rarityMetrics = new();
        private readonly GeneticDiscoveryAnalytics _geneticAnalytics = new();

        public DiscoveryMetrics(AnalyticsSubsystemConfig config)
        {
            _config = config;
        }

        public void RecordDiscovery(DiscoveryAnalyticsEvent discoveryEvent)
        {
            var key = $"{discoveryEvent.discoveryType}_{discoveryEvent.discoveredItem}";

            if (!_discoveries.ContainsKey(key))
                _discoveries[key] = new List<DiscoveryAnalyticsEvent>();

            _discoveries[key].Add(discoveryEvent);

            // Update rarity metrics
            if (!_rarityMetrics.discoveryFrequency.ContainsKey(key))
            {
                _rarityMetrics.discoveryFrequency[key] = 0;
                _rarityMetrics.firstDiscoveryDates[key] = discoveryEvent.timestamp;
            }

            _rarityMetrics.discoveryFrequency[key]++;
            _rarityMetrics.totalUniqueDiscoveries = _discoveries.Keys.Count;

            // Update genetic analytics for genetic discoveries
            if (discoveryEvent.discoveryType == "Trait" || discoveryEvent.discoveryType == "Mutation")
            {
                UpdateGeneticAnalytics(discoveryEvent);
            }
        }

        public void UpdateRarityMetrics()
        {
            var now = DateTime.Now;
            var windowStart = now.AddHours(-_config.rarityCalculationWindowHours);

            foreach (var kvp in _discoveries)
            {
                var recentDiscoveries = kvp.Value.Where(d => d.timestamp >= windowStart).ToList();
                var frequency = recentDiscoveries.Count;

                // Calculate rarity score (inverse of frequency)
                var rarityScore = frequency == 0 ? 1f : 1f / frequency;
                _rarityMetrics.traitRarityScores[kvp.Key] = rarityScore;
            }

            // Calculate average discovery rate
            var totalRecentDiscoveries = _discoveries.Values
                .SelectMany(list => list)
                .Count(d => d.timestamp >= windowStart);

            _rarityMetrics.averageDiscoveryRate = totalRecentDiscoveries / (float)_config.rarityCalculationWindowHours;
        }

        public DiscoveryRarityMetrics GetRarityMetrics()
        {
            return _rarityMetrics;
        }

        public GeneticDiscoveryAnalytics GetGeneticDiscoveryAnalytics()
        {
            return _geneticAnalytics;
        }

        public float GetDiscoveryRarityScore(string itemType, string itemName)
        {
            var key = $"{itemType}_{itemName}";
            return _rarityMetrics.traitRarityScores.GetValueOrDefault(key, 1f);
        }

        public List<string> GetRecommendedDiscoveryTargets()
        {
            var recommendations = new List<string>();

            // Recommend rarest undiscovered combinations
            var monitoredTraits = _config.monitoredTraits;
            var discoveredTraits = _discoveries.Keys
                .Where(k => k.StartsWith("Trait_"))
                .Select(k => k.Substring(6))
                .ToHashSet();

            foreach (var trait in monitoredTraits)
            {
                if (!discoveredTraits.Contains(trait))
                {
                    recommendations.Add($"Try to discover the {trait} trait - not found yet!");
                }
            }

            // Recommend rare combinations
            var rareItems = _rarityMetrics.traitRarityScores
                .Where(kvp => kvp.Value > 0.8f)
                .OrderByDescending(kvp => kvp.Value)
                .Take(3)
                .Select(kvp => kvp.Key);

            foreach (var item in rareItems)
            {
                recommendations.Add($"Focus on {item} - very rare discovery!");
            }

            return recommendations;
        }

        private void UpdateGeneticAnalytics(DiscoveryAnalyticsEvent discoveryEvent)
        {
            if (discoveryEvent.discoveryType == "Mutation")
            {
                var trait = discoveryEvent.discoveredItem;
                if (!_geneticAnalytics.mutationRates.ContainsKey(trait))
                    _geneticAnalytics.mutationRates[trait] = 0f;

                _geneticAnalytics.mutationRates[trait] += 0.01f; // Increment mutation rate
            }

            // Track first discoveries per player (would need player ID)
            if (discoveryEvent.isWorldFirst)
            {
                var playerId = "CurrentPlayer"; // Would get from session
                if (!_geneticAnalytics.playerFirstDiscoveries.ContainsKey(playerId))
                    _geneticAnalytics.playerFirstDiscoveries[playerId] = 0;
                _geneticAnalytics.playerFirstDiscoveries[playerId]++;
            }

            // Calculate genetic diversity index
            var uniqueTraits = _discoveries.Keys.Count(k => k.StartsWith("Trait_"));
            var totalPossibleTraits = _config.monitoredTraits.Count * 10; // Assume 10 variants per trait
            _geneticAnalytics.geneticDiversityIndex = (float)uniqueTraits / totalPossibleTraits;
        }
    }
}