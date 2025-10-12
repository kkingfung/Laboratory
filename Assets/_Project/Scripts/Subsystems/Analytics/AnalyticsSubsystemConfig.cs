using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Analytics
{
    /// <summary>
    /// Configuration ScriptableObject for the Analytics Subsystem.
    /// Controls all analytics collection, privacy settings, and performance parameters.
    /// </summary>
    [CreateAssetMenu(fileName = "AnalyticsSubsystemConfig", menuName = "Project Chimera/Subsystems/Analytics Config")]
    public class AnalyticsSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Enable real-time analytics processing")]
        public bool enableRealTimeAnalytics = true;

        [Tooltip("Save session data to disk")]
        public bool saveSessionData = true;

        [Tooltip("Background processing interval in milliseconds")]
        [Range(1000, 10000)]
        public int processingIntervalMs = 5000;

        [Header("Privacy & Compliance")]
        [Tooltip("Requires parental consent for educational analytics (COPPA compliance)")]
        public bool requiresParentalConsent = true;

        [Tooltip("Anonymize all personal data")]
        public bool anonymizeData = true;

        [Tooltip("Data retention period in days")]
        [Range(1, 365)]
        public int dataRetentionDays = 30;

        [Header("Player Behavior Tracking")]
        [Tooltip("Track detailed player actions")]
        public bool trackPlayerActions = true;

        [Tooltip("Track breeding patterns")]
        public bool trackBreedingPatterns = true;

        [Tooltip("Track exploration behavior")]
        public bool trackExplorationBehavior = true;

        [Tooltip("Minimum action frequency to consider as pattern")]
        [Range(0.1f, 10f)]
        public float behaviorPatternThreshold = 2f;

        [Header("Performance Monitoring")]
        [Tooltip("Monitor frame rate")]
        public bool monitorFrameRate = true;

        [Tooltip("Monitor memory usage")]
        public bool monitorMemoryUsage = true;

        [Tooltip("Monitor entity count")]
        public bool monitorEntityCount = true;

        [Tooltip("Monitor network performance")]
        public bool monitorNetworkPerformance = true;

        [Tooltip("Frame rate threshold for anomaly detection")]
        [Range(10f, 60f)]
        public float frameRateAnomalyThreshold = 30f;

        [Tooltip("Memory usage threshold in MB for anomaly detection")]
        [Range(100f, 4000f)]
        public float memoryAnomalyThreshold = 1000f;

        [Header("Discovery Analytics")]
        [Tooltip("Track genetic discoveries")]
        public bool trackGeneticDiscoveries = true;

        [Tooltip("Track species discoveries")]
        public bool trackSpeciesDiscoveries = true;

        [Tooltip("Track world-first discoveries")]
        public bool trackWorldFirstDiscoveries = true;

        [Tooltip("Rarity calculation window in hours")]
        [Range(1f, 168f)]
        public float rarityCalculationWindowHours = 24f;

        [Header("Educational Analytics")]
        [Tooltip("Track learning progress")]
        public bool trackLearningProgress = true;

        [Tooltip("Track concept mastery")]
        public bool trackConceptMastery = true;

        [Tooltip("Generate learning recommendations")]
        public bool generateRecommendations = true;

        [Tooltip("Confidence threshold for concept mastery")]
        [Range(0.5f, 1f)]
        public float masteryConfidenceThreshold = 0.8f;

        [Header("Analytics Targets")]
        [Tooltip("External analytics service URLs")]
        public List<string> analyticsServiceUrls = new List<string>();

        [Tooltip("API keys for analytics services (stored securely)")]
        [SerializeField] private List<string> apiKeys = new List<string>();

        [Tooltip("Batch size for analytics uploads")]
        [Range(1, 100)]
        public int uploadBatchSize = 10;

        [Tooltip("Upload interval in seconds")]
        [Range(30, 3600)]
        public int uploadIntervalSeconds = 300;

        [Header("Debug Settings")]
        [Tooltip("Enable detailed analytics logging")]
        public bool enableDebugLogging = false;

        [Tooltip("Save analytics logs to file")]
        public bool saveAnalyticsLogs = false;

        [Tooltip("Generate test analytics data")]
        public bool generateTestData = false;

        [Header("Genetic Balancing")]
        [Tooltip("Traits to monitor for rarity balancing")]
        public List<string> monitoredTraits = new List<string>
        {
            "Strength", "Agility", "Intelligence", "Vitality", "Adaptability", "Social"
        };

        [Tooltip("Species to track for population balancing")]
        public List<string> monitoredSpecies = new List<string>();

        [Tooltip("Breeding combination complexity thresholds")]
        public List<BreedingComplexityThreshold> complexityThresholds = new List<BreedingComplexityThreshold>
        {
            new BreedingComplexityThreshold { traitCount = 3, rarityMultiplier = 1f },
            new BreedingComplexityThreshold { traitCount = 5, rarityMultiplier = 2f },
            new BreedingComplexityThreshold { traitCount = 8, rarityMultiplier = 4f }
        };

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            processingIntervalMs = Mathf.Max(1000, processingIntervalMs);
            dataRetentionDays = Mathf.Max(1, dataRetentionDays);
            uploadBatchSize = Mathf.Max(1, uploadBatchSize);
            uploadIntervalSeconds = Mathf.Max(30, uploadIntervalSeconds);

            // Ensure monitored traits list has defaults
            if (monitoredTraits.Count == 0)
            {
                monitoredTraits.AddRange(new[] { "Strength", "Agility", "Intelligence", "Vitality", "Adaptability", "Social" });
            }

            // Ensure complexity thresholds are sorted
            complexityThresholds.Sort((a, b) => a.traitCount.CompareTo(b.traitCount));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the API key for a specific analytics service
        /// </summary>
        public string GetApiKey(int serviceIndex)
        {
            if (serviceIndex >= 0 && serviceIndex < apiKeys.Count)
                return apiKeys[serviceIndex];
            return string.Empty;
        }

        /// <summary>
        /// Checks if a specific analytics feature is enabled
        /// </summary>
        public bool IsFeatureEnabled(string featureName)
        {
            return featureName.ToLower() switch
            {
                "player_actions" => trackPlayerActions,
                "breeding_patterns" => trackBreedingPatterns,
                "exploration" => trackExplorationBehavior,
                "frame_rate" => monitorFrameRate,
                "memory" => monitorMemoryUsage,
                "entities" => monitorEntityCount,
                "network" => monitorNetworkPerformance,
                "genetic_discoveries" => trackGeneticDiscoveries,
                "species_discoveries" => trackSpeciesDiscoveries,
                "world_first" => trackWorldFirstDiscoveries,
                "learning_progress" => trackLearningProgress,
                "concept_mastery" => trackConceptMastery,
                "recommendations" => generateRecommendations,
                _ => false
            };
        }

        /// <summary>
        /// Gets breeding complexity multiplier for a trait count
        /// </summary>
        public float GetComplexityMultiplier(int traitCount)
        {
            float multiplier = 1f;

            foreach (var threshold in complexityThresholds)
            {
                if (traitCount >= threshold.traitCount)
                    multiplier = threshold.rarityMultiplier;
                else
                    break;
            }

            return multiplier;
        }

        /// <summary>
        /// Checks if data should be retained based on age
        /// </summary>
        public bool ShouldRetainData(System.DateTime dataTimestamp)
        {
            var dataAge = System.DateTime.Now - dataTimestamp;
            return dataAge.TotalDays <= dataRetentionDays;
        }

        #endregion
    }

    [System.Serializable]
    public class BreedingComplexityThreshold
    {
        [Tooltip("Number of traits in breeding combination")]
        public int traitCount;

        [Tooltip("Rarity multiplier for this complexity level")]
        public float rarityMultiplier = 1f;
    }
}