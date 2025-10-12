using System;
using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Events
{
    /// <summary>
    /// Configuration ScriptableObject for the Events & Celebrations Subsystem.
    /// Controls discovery celebrations, seasonal events, tournaments, and community celebrations.
    /// </summary>
    [CreateAssetMenu(fileName = "EventsSubsystemConfig", menuName = "Project Chimera/Subsystems/Events Config")]
    public class EventsSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Background processing interval in milliseconds")]
        [Range(1000, 10000)]
        public int backgroundProcessingIntervalMs = 3000;

        [Tooltip("Event history retention period in days")]
        [Range(7, 365)]
        public int eventHistoryRetentionDays = 90;

        [Tooltip("Enable debug logging for events")]
        public bool enableDebugLogging = false;

        [Header("Discovery Celebrations")]
        [Tooltip("Minimum celebration threshold (0-1)")]
        [Range(0f, 1f)]
        public float minimumCelebrationThreshold = 0.6f;

        [Tooltip("Significant discovery threshold (0-1)")]
        [Range(0f, 1f)]
        public float significantDiscoveryThreshold = 0.8f;

        [Tooltip("Mutation celebration threshold")]
        [Range(0f, 1f)]
        public float mutationCelebrationThreshold = 0.7f;

        [Tooltip("World first celebration duration multiplier")]
        [Range(1f, 5f)]
        public float worldFirstDurationMultiplier = 3f;

        [Tooltip("Celebration templates for different discovery types")]
        public List<CelebrationTemplate> celebrationTemplates = new List<CelebrationTemplate>();

        [Header("Seasonal Events")]
        [Tooltip("Seasonal events configuration")]
        public List<SeasonalEventData> seasonalEvents = new List<SeasonalEventData>();

        [Tooltip("Enable automatic seasonal event triggering")]
        public bool enableAutomaticSeasonalEvents = true;

        [Tooltip("Seasonal event participation bonus multiplier")]
        [Range(1f, 3f)]
        public float seasonalParticipationBonus = 1.5f;

        [Header("Tournaments")]
        [Tooltip("Enable tournament system")]
        public bool enableTournaments = true;

        [Tooltip("Maximum concurrent tournaments")]
        [Range(1, 10)]
        public int maxConcurrentTournaments = 3;

        [Tooltip("Default tournament duration in hours")]
        [Range(1f, 168f)]
        public float defaultTournamentDurationHours = 24f;

        [Tooltip("Minimum players for tournament")]
        [Range(2, 100)]
        public int minimumTournamentPlayers = 4;

        [Header("Community Events")]
        [Tooltip("Community event participation threshold")]
        [Range(0.1f, 1f)]
        public float communityParticipationThreshold = 0.3f;

        [Tooltip("Community event success bonus")]
        [Range(1f, 5f)]
        public float communitySuccessBonus = 2f;

        [Tooltip("Maximum active community events")]
        [Range(1, 5)]
        public int maxActiveCommunityEvents = 2;

        [Header("Visual Effects")]
        [Tooltip("Default celebration particle effect")]
        public ParticleSystem defaultCelebrationParticles;

        [Tooltip("World first celebration particle effect")]
        public ParticleSystem worldFirstParticles;

        [Tooltip("Celebration UI overlay prefab")]
        public GameObject celebrationUIOverlay;

        [Tooltip("Celebration duration in seconds")]
        [Range(1f, 30f)]
        public float baseCelebrationDuration = 5f;

        [Header("Audio Effects")]
        [Tooltip("Default celebration sound")]
        public AudioClip defaultCelebrationSound;

        [Tooltip("World first celebration sound")]
        public AudioClip worldFirstCelebrationSound;

        [Tooltip("Tournament victory sound")]
        public AudioClip tournamentVictorySound;

        [Tooltip("Community event sound")]
        public AudioClip communityEventSound;

        [Tooltip("Celebration volume")]
        [Range(0f, 1f)]
        public float celebrationVolume = 0.8f;

        [Header("Cooldowns")]
        [Tooltip("Discovery celebration cooldown in minutes")]
        [Range(1f, 60f)]
        public float discoveryCelebrationCooldownMinutes = 5f;

        [Tooltip("Player celebration cooldown in minutes")]
        [Range(1f, 30f)]
        public float playerCelebrationCooldownMinutes = 2f;

        [Tooltip("World first announcement cooldown in minutes")]
        [Range(5f, 120f)]
        public float worldFirstCooldownMinutes = 30f;

        [Header("Participation Rewards")]
        [Tooltip("Base research points for event participation")]
        [Range(1, 100)]
        public int baseEventParticipationPoints = 10;

        [Tooltip("Tournament participation points")]
        [Range(5, 200)]
        public int tournamentParticipationPoints = 50;

        [Tooltip("Tournament victory points")]
        [Range(50, 500)]
        public int tournamentVictoryPoints = 200;

        [Tooltip("Community event completion bonus")]
        [Range(10, 300)]
        public int communityEventCompletionBonus = 100;

        [Header("Achievement Integration")]
        [Tooltip("Celebrations required for achievement milestones")]
        public List<CelebrationMilestone> celebrationMilestones = new List<CelebrationMilestone>
        {
            new CelebrationMilestone { celebrationCount = 10, achievementName = "Celebration Novice" },
            new CelebrationMilestone { celebrationCount = 50, achievementName = "Discovery Enthusiast" },
            new CelebrationMilestone { celebrationCount = 100, achievementName = "Master Discoverer" }
        };

        [Tooltip("World first discoveries for special titles")]
        public List<WorldFirstMilestone> worldFirstMilestones = new List<WorldFirstMilestone>
        {
            new WorldFirstMilestone { worldFirstCount = 1, specialTitle = "Pioneer" },
            new WorldFirstMilestone { worldFirstCount = 5, specialTitle = "Trailblazer" },
            new WorldFirstMilestone { worldFirstCount = 10, specialTitle = "Legendary Explorer" }
        };

        [Header("Performance")]
        [Tooltip("Maximum celebrations per player per hour")]
        [Range(1, 50)]
        public int maxCelebrationsPerPlayerPerHour = 20;

        [Tooltip("Maximum concurrent visual effects")]
        [Range(5, 100)]
        public int maxConcurrentVisualEffects = 20;

        [Tooltip("Event processing batch size")]
        [Range(1, 20)]
        public int eventProcessingBatchSize = 5;

        [Header("Accessibility")]
        [Tooltip("Enable reduced motion celebrations")]
        public bool enableReducedMotionMode = true;

        [Tooltip("Enable hearing impaired visual indicators")]
        public bool enableHearingImpairedMode = true;

        [Tooltip("Celebration text size multiplier")]
        [Range(0.5f, 3f)]
        public float celebrationTextSizeMultiplier = 1f;

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            backgroundProcessingIntervalMs = Mathf.Max(1000, backgroundProcessingIntervalMs);
            eventHistoryRetentionDays = Mathf.Max(7, eventHistoryRetentionDays);
            baseCelebrationDuration = Mathf.Max(1f, baseCelebrationDuration);
            maxConcurrentTournaments = Mathf.Max(1, maxConcurrentTournaments);
            minimumTournamentPlayers = Mathf.Max(2, minimumTournamentPlayers);

            // Ensure thresholds are properly ordered
            significantDiscoveryThreshold = Mathf.Max(minimumCelebrationThreshold, significantDiscoveryThreshold);

            // Ensure celebration templates have defaults
            if (celebrationTemplates.Count == 0)
            {
                celebrationTemplates.AddRange(CreateDefaultCelebrationTemplates());
            }

            // Ensure milestone lists have defaults
            if (celebrationMilestones.Count == 0)
            {
                celebrationMilestones.AddRange(CreateDefaultCelebrationMilestones());
            }

            if (worldFirstMilestones.Count == 0)
            {
                worldFirstMilestones.AddRange(CreateDefaultWorldFirstMilestones());
            }
        }

        private List<CelebrationTemplate> CreateDefaultCelebrationTemplates()
        {
            return new List<CelebrationTemplate>
            {
                new CelebrationTemplate
                {
                    celebrationType = CelebrationType.TraitDiscovery,
                    templateName = "Trait Discovery",
                    titleFormat = "New Trait Discovered: {traitName}",
                    descriptionFormat = "You discovered the {traitName} trait!",
                    baseDurationMinutes = 3f,
                    enableWorldFirstVariant = true
                },
                new CelebrationTemplate
                {
                    celebrationType = CelebrationType.SpeciesDiscovery,
                    templateName = "Species Discovery",
                    titleFormat = "New Species Discovered: {speciesName}",
                    descriptionFormat = "You discovered a new species: {speciesName}!",
                    baseDurationMinutes = 5f,
                    enableWorldFirstVariant = true
                },
                new CelebrationTemplate
                {
                    celebrationType = CelebrationType.WorldFirst,
                    templateName = "World First",
                    titleFormat = "🌟 WORLD FIRST: {discoveryName}",
                    descriptionFormat = "You made the first discovery of {discoveryName} in the world!",
                    baseDurationMinutes = 10f,
                    enableWorldFirstVariant = false
                }
            };
        }

        private List<CelebrationMilestone> CreateDefaultCelebrationMilestones()
        {
            return new List<CelebrationMilestone>
            {
                new CelebrationMilestone { celebrationCount = 10, achievementName = "Celebration Novice" },
                new CelebrationMilestone { celebrationCount = 25, achievementName = "Discovery Enthusiast" },
                new CelebrationMilestone { celebrationCount = 50, achievementName = "Prolific Discoverer" },
                new CelebrationMilestone { celebrationCount = 100, achievementName = "Master Discoverer" },
                new CelebrationMilestone { celebrationCount = 250, achievementName = "Legendary Researcher" }
            };
        }

        private List<WorldFirstMilestone> CreateDefaultWorldFirstMilestones()
        {
            return new List<WorldFirstMilestone>
            {
                new WorldFirstMilestone { worldFirstCount = 1, specialTitle = "Pioneer" },
                new WorldFirstMilestone { worldFirstCount = 3, specialTitle = "Trailblazer" },
                new WorldFirstMilestone { worldFirstCount = 5, specialTitle = "Pathfinder" },
                new WorldFirstMilestone { worldFirstCount = 10, specialTitle = "Legendary Explorer" },
                new WorldFirstMilestone { worldFirstCount = 25, specialTitle = "World Shaper" }
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets celebration template for discovery type
        /// </summary>
        public CelebrationTemplate GetCelebrationTemplate(CelebrationType celebrationType)
        {
            return celebrationTemplates.Find(t => t.celebrationType == celebrationType);
        }

        /// <summary>
        /// Gets celebration template for discovery type
        /// </summary>
        public CelebrationTemplate GetCelebrationTemplate(DiscoveryType discoveryType)
        {
            var celebrationType = DiscoveryTypeToCelebrationType(discoveryType);
            return GetCelebrationTemplate(celebrationType);
        }

        /// <summary>
        /// Checks if discovery meets celebration threshold
        /// </summary>
        public bool MeetsCelebrationThreshold(float discoverySignificance, bool isWorldFirst = false)
        {
            if (isWorldFirst) return true;
            return discoverySignificance >= minimumCelebrationThreshold;
        }

        /// <summary>
        /// Calculates celebration duration
        /// </summary>
        public float CalculateCelebrationDuration(CelebrationType celebrationType, bool isWorldFirst = false)
        {
            var template = GetCelebrationTemplate(celebrationType);
            var baseDuration = template?.baseDurationMinutes ?? baseCelebrationDuration;

            if (isWorldFirst)
                baseDuration *= worldFirstDurationMultiplier;

            return baseDuration * 60f; // Convert to seconds
        }

        /// <summary>
        /// Gets seasonal event for current date
        /// </summary>
        public List<SeasonalEventData> GetSeasonalEventsForDate(DateTime date)
        {
            var activeEvents = new List<SeasonalEventData>();

            foreach (var seasonalEvent in seasonalEvents)
            {
                if (IsSeasonalEventActive(seasonalEvent, date))
                {
                    activeEvents.Add(seasonalEvent);
                }
            }

            return activeEvents;
        }

        /// <summary>
        /// Checks if player qualifies for celebration milestone
        /// </summary>
        public CelebrationMilestone GetCelebrationMilestone(int celebrationCount)
        {
            CelebrationMilestone milestone = null;

            foreach (var m in celebrationMilestones)
            {
                if (celebrationCount >= m.celebrationCount)
                    milestone = m;
            }

            return milestone;
        }

        /// <summary>
        /// Gets world first milestone for count
        /// </summary>
        public WorldFirstMilestone GetWorldFirstMilestone(int worldFirstCount)
        {
            WorldFirstMilestone milestone = null;

            foreach (var m in worldFirstMilestones)
            {
                if (worldFirstCount >= m.worldFirstCount)
                    milestone = m;
            }

            return milestone;
        }

        /// <summary>
        /// Checks if celebration is on cooldown
        /// </summary>
        public bool IsOnCooldown(DateTime lastCelebration, CelebrationType celebrationType)
        {
            var cooldownMinutes = celebrationType switch
            {
                CelebrationType.WorldFirst => worldFirstCooldownMinutes,
                _ => discoveryCelebrationCooldownMinutes
            };

            return DateTime.Now - lastCelebration < TimeSpan.FromMinutes(cooldownMinutes);
        }

        #endregion

        #region Private Methods

        private CelebrationType DiscoveryTypeToCelebrationType(DiscoveryType discoveryType)
        {
            return discoveryType switch
            {
                DiscoveryType.NewTrait => CelebrationType.TraitDiscovery,
                DiscoveryType.NewSpecies => CelebrationType.SpeciesDiscovery,
                DiscoveryType.Mutation => CelebrationType.MutationDiscovery,
                DiscoveryType.BreedingSuccess => CelebrationType.BreedingSuccess,
                DiscoveryType.Research => CelebrationType.ResearchPublication,
                _ => CelebrationType.Achievement
            };
        }

        private bool IsSeasonalEventActive(SeasonalEventData seasonalEvent, DateTime date)
        {
            if (seasonalEvent.trigger == null)
                return false;

            return seasonalEvent.trigger.triggerType switch
            {
                SeasonalTriggerType.DateBased => IsDateBasedEventActive(seasonalEvent.trigger, date),
                SeasonalTriggerType.WeatherBased => false, // Would need weather system integration
                SeasonalTriggerType.Manual => seasonalEvent.isActive,
                _ => false
            };
        }

        private bool IsDateBasedEventActive(SeasonalTrigger trigger, DateTime date)
        {
            if (trigger.recurring)
            {
                return date.Month == trigger.month && date.Day == trigger.day;
            }
            else
            {
                // For non-recurring events, would need to check if it's within the event period
                return false;
            }
        }

        #endregion
    }

    [System.Serializable]
    public class CelebrationMilestone
    {
        public int celebrationCount;
        public string achievementName;
        public string achievementDescription;
        public Color achievementColor = Color.gold;
    }

    [System.Serializable]
    public class WorldFirstMilestone
    {
        public int worldFirstCount;
        public string specialTitle;
        public string titleDescription;
        public Color titleColor = Color.cyan;
    }
}