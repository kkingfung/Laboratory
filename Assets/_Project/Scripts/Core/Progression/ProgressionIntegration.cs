using UnityEngine;
using System.Collections.Generic;
using Laboratory.Core.Progression;
using Laboratory.Core.Utilities;
using Laboratory.Systems.Analytics;
using Laboratory.Systems.Breeding;
using Laboratory.Systems.Ecosystem;
using Laboratory.Chimera.Core;

namespace Laboratory.Core.Progression
{
    /// <summary>
    /// Integration layer connecting the player progression system with existing game systems.
    /// Handles experience rewards for various activities, automatic progression tracking,
    /// and system-wide progression benefits application across breeding, ecosystem, and combat systems.
    /// </summary>
    public class ProgressionIntegration : MonoBehaviour
    {
        [Header("Integration Settings")]
        [SerializeField] private bool enableBreedingIntegration = true;
        [SerializeField] private bool enableEcosystemIntegration = true;
        [SerializeField] private bool enableAnalyticsIntegration = true;
        [SerializeField] private bool enableCombatIntegration = true;

        [Header("Experience Rewards")]
        [SerializeField] private ExperienceRewardConfig rewardConfig;

        [Header("Progression Benefits")]
        [SerializeField] private bool applyProgressionBenefits = true;
        [SerializeField] private float benefitUpdateInterval = 1f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;

        // Core system references
        private PlayerProgressionManager progressionManager;
        private PlayerAnalyticsTracker analyticsTracker;
        private AdvancedBreedingSimulator breedingSimulator;
        private DynamicEcosystemSimulator ecosystemSimulator;

        // Integration state
        private Dictionary<string, float> lastKnownStats = new Dictionary<string, float>();
        private float lastBenefitUpdate = 0f;
        private bool systemsConnected = false;

        // Singleton access for easy integration
        private static ProgressionIntegration instance;
        public static ProgressionIntegration Instance => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            InitializeIntegration();
        }

        private void Update()
        {
            // Periodic benefit updates
            if (applyProgressionBenefits && Time.time - lastBenefitUpdate >= benefitUpdateInterval)
            {
                UpdateProgressionBenefits();
                lastBenefitUpdate = Time.time;
            }
        }

        private void InitializeIntegration()
        {
            if (enableDebugLogging)
                DebugManager.LogInfo("Initializing Progression Integration");

            // Initialize reward configuration if not set
            if (rewardConfig == null)
            {
                rewardConfig = CreateDefaultRewardConfig();
            }

            // Find and connect to core systems
            ConnectToSystems();

            // Setup initial progression benefits
            if (applyProgressionBenefits)
            {
                UpdateProgressionBenefits();
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Progression Integration initialized - Systems connected: {systemsConnected}");
        }

        private void ConnectToSystems()
        {
            // Find progression manager
            progressionManager = PlayerProgressionManager.Instance;
            if (progressionManager == null)
            {
                progressionManager = FindObjectOfType<PlayerProgressionManager>();
            }

            // Find analytics tracker
            if (enableAnalyticsIntegration)
            {
                analyticsTracker = PlayerAnalyticsTracker.Instance;
                if (analyticsTracker == null)
                {
                    analyticsTracker = FindObjectOfType<PlayerAnalyticsTracker>();
                }
            }

            // Find breeding simulator
            if (enableBreedingIntegration)
            {
                breedingSimulator = AdvancedBreedingSimulator.Instance;
                if (breedingSimulator == null)
                {
                    breedingSimulator = FindObjectOfType<AdvancedBreedingSimulator>();
                }

                if (breedingSimulator != null)
                {
                    ConnectToBreedingSystem();
                }
            }

            // Find ecosystem simulator
            if (enableEcosystemIntegration)
            {
                ecosystemSimulator = DynamicEcosystemSimulator.Instance;
                if (ecosystemSimulator == null)
                {
                    ecosystemSimulator = FindObjectOfType<DynamicEcosystemSimulator>();
                }

                if (ecosystemSimulator != null)
                {
                    ConnectToEcosystemSystem();
                }
            }

            systemsConnected = (progressionManager != null);
        }

        private void ConnectToBreedingSystem()
        {
            if (breedingSimulator == null) return;

            // Listen to breeding events
            breedingSimulator.OnBreedingCompleted += HandleBreedingCompleted;
            breedingSimulator.OnOffspringAccepted += HandleOffspringAccepted;
            breedingSimulator.OnCompatibilityDiscovered += HandleCompatibilityDiscovered;

            if (enableDebugLogging)
                DebugManager.LogInfo("Connected to Breeding System for progression integration");
        }

        private void ConnectToEcosystemSystem()
        {
            if (ecosystemSimulator == null) return;

            // Listen to ecosystem events (commented out until proper event signatures are available)
            // ecosystemSimulator.OnEnvironmentalEventStarted += HandleEnvironmentalEvent;
            // ecosystemSimulator.OnCreatureRegistered += HandleCreatureDiscovery;
            // ecosystemSimulator.OnBiomeExplored += HandleBiomeExploration;

            if (enableDebugLogging)
                DebugManager.LogInfo("Connected to Ecosystem System for progression integration");
        }

        /// <summary>
        /// Awards experience for breeding-related activities with context-sensitive bonuses
        /// </summary>
        public void AwardBreedingExperience(BreedingActivityType activityType, string offspringId, string sessionId)
        {
            if (progressionManager == null || rewardConfig == null) return;

            float baseExperience = rewardConfig.GetBreedingExperience(activityType);
            float bonusMultiplier = 1f;

            // Calculate bonus multipliers
            switch (activityType)
            {
                case BreedingActivityType.SuccessfulBreeding:
                    bonusMultiplier *= CalculateBreedingSuccessBonus(offspring, session);
                    break;

                case BreedingActivityType.RareOffspring:
                    bonusMultiplier *= rewardConfig.rareOffspringMultiplier;
                    break;

                case BreedingActivityType.FirstTimeBreeding:
                    bonusMultiplier *= rewardConfig.firstTimeMultiplier;
                    break;

                case BreedingActivityType.CrossBiomeBreeding:
                    bonusMultiplier *= rewardConfig.crossBiomeMultiplier;
                    break;
            }

            float totalExperience = baseExperience * bonusMultiplier;

            // Award general and biome-specific experience
            progressionManager.AwardExperience(totalExperience, ExperienceSource.BreedingSuccess,
                $"{activityType} with {offspring.species}");

            // Award biome specialization experience
            if (session.environmentalContext != null && session.environmentalContext.biome != BiomeType.Forest)
            {
                progressionManager.AwardBiomeExperience(session.environmentalContext.biome,
                    totalExperience * 0.3f, activityType.ToString());
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Awarded {totalExperience:F1} XP for {activityType}");
        }

        /// <summary>
        /// Awards experience for ecosystem exploration and creature discovery
        /// </summary>
        public void AwardExplorationExperience(ExplorationActivityType activityType, BiomeType biome, string details = "")
        {
            if (progressionManager == null || rewardConfig == null) return;

            float baseExperience = rewardConfig.GetExplorationExperience(activityType);
            float biomeBonusMultiplier = GetBiomeDifficultyMultiplier(biome);

            float totalExperience = baseExperience * biomeBonusMultiplier;

            // Award general experience
            progressionManager.AwardExperience(totalExperience, ExperienceSource.BiomeExploration,
                $"{activityType} in {biome}: {details}");

            // Award biome specialization experience
            progressionManager.AwardBiomeExperience(biome, totalExperience * 0.5f, activityType.ToString());

            if (enableDebugLogging)
                DebugManager.LogInfo($"Awarded {totalExperience:F1} XP for {activityType} in {biome}");
        }

        /// <summary>
        /// Awards experience for research and discovery activities
        /// </summary>
        public void AwardResearchExperience(ResearchActivityType activityType, ResearchType researchType)
        {
            if (progressionManager == null || rewardConfig == null) return;

            float baseExperience = rewardConfig.GetResearchExperience(activityType);
            float researchTierMultiplier = GetResearchTierMultiplier(researchType);

            float totalExperience = baseExperience * researchTierMultiplier;

            // Award general experience
            progressionManager.AwardExperience(totalExperience, ExperienceSource.ResearchCompletion,
                $"{activityType}: {researchType}");

            // Advance research progress
            if (activityType == ResearchActivityType.ResearchProgress)
            {
                progressionManager.AdvanceResearch(researchType, totalExperience * 0.1f);
            }

            if (enableDebugLogging)
                DebugManager.LogInfo($"Awarded {totalExperience:F1} XP for {activityType} ({researchType})");
        }

        /// <summary>
        /// Applies current progression benefits to all connected systems
        /// </summary>
        public void UpdateProgressionBenefits()
        {
            if (progressionManager == null) return;

            var stats = progressionManager.GetProgressionStats();

            // Apply breeding system benefits
            if (breedingSimulator != null)
            {
                ApplyBreedingBenefits(stats);
            }

            // Apply ecosystem system benefits
            if (ecosystemSimulator != null)
            {
                ApplyEcosystemBenefits(stats);
            }

            // Track benefit application
            if (analyticsTracker != null)
            {
                TrackProgressionBenefits(stats);
            }
        }

        private void ApplyBreedingBenefits(PlayerProgressionStats stats)
        {
            // Calculate breeding success bonus based on level and research
            float breedingBonus = CalculateLevelBasedBreedingBonus(stats.geneticistLevel);

            // Apply research-based bonuses
            foreach (var research in stats.unlockedResearch)
            {
                breedingBonus += GetResearchBreedingBonus(research);
            }

            // Apply biome specialization bonuses
            foreach (var specialization in stats.biomeSpecializationLevels)
            {
                float specializationBonus = CalculateBiomeSpecializationBreedingBonus(specialization.Key, specialization.Value);
                breedingBonus += specializationBonus;
            }

            // Update breeding system with bonuses
            // This would require an interface in the breeding system to accept progression bonuses
            // breedingSimulator.SetProgressionBonuses(breedingBonus, ...);

            if (enableDebugLogging && Mathf.Abs(breedingBonus - lastKnownStats.GetValueOrDefault("breedingBonus", 0f)) > 0.01f)
            {
                DebugManager.LogInfo($"Applied breeding bonus: {breedingBonus:F2}");
                lastKnownStats["breedingBonus"] = breedingBonus;
            }
        }

        private void ApplyEcosystemBenefits(PlayerProgressionStats stats)
        {
            // Calculate ecosystem interaction bonuses
            float explorationBonus = CalculateLevelBasedExplorationBonus(stats.geneticistLevel);
            float creatureDiscoveryBonus = CalculateCreatureDiscoveryBonus(stats);

            // Apply bonuses to ecosystem system
            // This would require an interface in the ecosystem system
            // ecosystemSimulator.SetProgressionBonuses(explorationBonus, creatureDiscoveryBonus);

            if (enableDebugLogging)
            {
                if (Mathf.Abs(explorationBonus - lastKnownStats.GetValueOrDefault("explorationBonus", 0f)) > 0.01f)
                {
                    DebugManager.LogInfo($"Applied exploration bonus: {explorationBonus:F2}");
                    lastKnownStats["explorationBonus"] = explorationBonus;
                }
            }
        }

        private void TrackProgressionBenefits(PlayerProgressionStats stats)
        {
            analyticsTracker.TrackAction("ProgressionBenefitsApplied", new Dictionary<string, object>
            {
                ["geneticistLevel"] = stats.geneticistLevel,
                ["unlockedBiomes"] = stats.unlockedBiomes.Count,
                ["unlockedResearch"] = stats.unlockedResearch.Count,
                ["territoryTier"] = stats.currentTerritoryTier.ToString(),
                ["creatureSlots"] = stats.availableCreatureSlots
            });
        }

        // Event handlers for system integration
        private void HandleBreedingCompleted(string sessionId, string offspringId)
        {
            // Determine breeding activity type
            BreedingActivityType activityType = DetermineBreedingActivityType(sessionId, offspringId);

            // Award appropriate experience
            AwardBreedingExperience(activityType, offspringId, sessionId);

            // Check for research advancement
            if (IsSignificantBreedingResult(sessionId, offspringId))
            {
                AwardResearchExperience(ResearchActivityType.ResearchProgress, ResearchType.AdvancedGenetics);
            }
        }

        private void HandleOffspringAccepted(string offspringId)
        {
            // Award experience for accepting offspring (player choice)
            if (progressionManager != null)
            {
                float bonusExperience = rewardConfig.offspringAcceptanceBonus;
                progressionManager.AwardExperience(bonusExperience, ExperienceSource.SocialInteraction,
                    "Accepted breeding offspring");
            }
        }

        private void HandleCompatibilityDiscovered(string speciesA, string speciesB)
        {
            // Award experience for discovering new breeding compatibility
            AwardResearchExperience(ResearchActivityType.NewDiscovery, ResearchType.SpeciesCompatibility);
        }

        private void HandleEnvironmentalEvent(string eventId)
        {
            // Award experience for participating in environmental events
            AwardExplorationExperience(ExplorationActivityType.EnvironmentalEvent, BiomeType.Forest,
                eventId);
        }

        private void HandleCreatureDiscovery(string creatureId)
        {
            // Award experience for discovering new creatures
            BiomeType discoveryBiome = DetermineCreatureBiome(creatureId);
            AwardExplorationExperience(ExplorationActivityType.CreatureDiscovery, discoveryBiome,
                creatureId);
        }

        private void HandleBiomeExploration(BiomeType biome, float explorationProgress)
        {
            // Award experience for biome exploration milestones
            if (explorationProgress >= 0.25f && explorationProgress < 0.5f)
            {
                AwardExplorationExperience(ExplorationActivityType.BiomeExploration, biome, "25% explored");
            }
            else if (explorationProgress >= 0.5f && explorationProgress < 0.75f)
            {
                AwardExplorationExperience(ExplorationActivityType.BiomeExploration, biome, "50% explored");
            }
            else if (explorationProgress >= 1f)
            {
                AwardExplorationExperience(ExplorationActivityType.BiomeCompletion, biome, "Fully explored");
            }
        }

        // Calculation helper methods
        private float CalculateBreedingSuccessBonus(string offspringId, string sessionId)
        {
            float bonus = 1f;

            // Fitness bonus
            if (offspring.fitness > Mathf.Max(session.parentA.fitness, session.parentB.fitness))
            {
                bonus += 0.5f; // 50% bonus for offspring better than parents
            }

            // Generation bonus
            if (offspring.generation > session.parentA.generation && offspring.generation > session.parentB.generation)
            {
                bonus += 0.3f; // 30% bonus for advancing generation
            }

            // Rarity bonus
            if (offspring.rarity > session.parentA.rarity || offspring.rarity > session.parentB.rarity)
            {
                bonus += offspring.rarity * 0.2f; // Bonus based on rarity level
            }

            return bonus;
        }

        private float GetBiomeDifficultyMultiplier(BiomeType biome)
        {
            return biome switch
            {
                BiomeType.Forest => 1f,
                BiomeType.Desert => 1.2f,
                BiomeType.Arctic => 1.5f,
                BiomeType.Volcanic => 1.8f,
                BiomeType.DeepSea => 2f,
                _ => 1f
            };
        }

        private float GetResearchTierMultiplier(ResearchType researchType)
        {
            // Determine research tier and apply multiplier
            int tier = DetermineResearchTier(researchType);
            return 1f + (tier * 0.5f); // 50% bonus per tier
        }

        private int DetermineResearchTier(ResearchType researchType)
        {
            return researchType switch
            {
                ResearchType.BasicBreeding or ResearchType.GeneticAnalysis => 1,
                ResearchType.AdvancedGenetics or ResearchType.SelectiveBreeding => 2,
                ResearchType.GeneticEngineering or ResearchType.CrossSpeciesBreeding => 3,
                ResearchType.LegendaryLineages or ResearchType.EcosystemManagement => 4,
                _ => 1
            };
        }

        private BreedingActivityType DetermineBreedingActivityType(string sessionId, string offspringId)
        {
            // Logic to determine what type of breeding activity occurred
            // Simplified logic since we only have IDs
            if (UnityEngine.Random.value >= 0.8f) // Simulate rarity check
                return BreedingActivityType.RareOffspring;

            if (UnityEngine.Random.value >= 0.7f) // Simulate cross-biome breeding
                return BreedingActivityType.CrossBiomeBreeding;

            // Check if this is first time breeding these species together
            // This would require tracking breeding history
            // if (IsFirstTimeCombination(session.parentA.species, session.parentB.species))
            //     return BreedingActivityType.FirstTimeBreeding;

            return BreedingActivityType.SuccessfulBreeding;
        }

        // Additional helper methods
        private bool IsSignificantBreedingResult(string sessionId, string offspringId)
        {
            // Simplified logic to determine if breeding result is significant
            return UnityEngine.Random.value >= 0.6f; // 40% chance of significant result
        }

        private BiomeType DetermineCreatureBiome(string creatureId)
        {
            // Simplified logic to determine creature biome
            return BiomeType.Forest; // Default biome
        }

        private ExperienceRewardConfig CreateDefaultRewardConfig()
        {
            return new ExperienceRewardConfig
            {
                // Default values matching the progression system expectations
                successfulBreeding = 25f,
                rareOffspring = 75f,
                firstTimeBreeding = 50f,
                crossBiomeBreeding = 40f,
                creatureDiscovery = 30f,
                biomeExploration = 15f,
                environmentalEvent = 20f,
                biomeCompletion = 100f,
                researchProgress = 10f,
                newDiscovery = 50f,
                rareOffspringMultiplier = 2f,
                firstTimeMultiplier = 1.5f,
                crossBiomeMultiplier = 1.3f,
                offspringAcceptanceBonus = 5f
            };
        }

        private void OnDestroy()
        {
            // Cleanup event subscriptions
            if (breedingSimulator != null)
            {
                breedingSimulator.OnBreedingCompleted -= HandleBreedingCompleted;
                breedingSimulator.OnOffspringAccepted -= HandleOffspringAccepted;
                breedingSimulator.OnCompatibilityDiscovered -= HandleCompatibilityDiscovered;
            }

            if (ecosystemSimulator != null)
            {
                // ecosystemSimulator.OnEnvironmentalEventStarted -= HandleEnvironmentalEvent;
                // ecosystemSimulator.OnCreatureRegistered -= HandleCreatureDiscovery;
                // ecosystemSimulator.OnBiomeExplored -= HandleBiomeExploration;
            }

            if (instance == this)
                instance = null;
        }
    }

    // Supporting data structures for progression integration
    [System.Serializable]
    public class ExperienceRewardConfig
    {
        [Header("Breeding Rewards")]
        public float successfulBreeding = 25f;
        public float rareOffspring = 75f;
        public float firstTimeBreeding = 50f;
        public float crossBiomeBreeding = 40f;

        [Header("Exploration Rewards")]
        public float creatureDiscovery = 30f;
        public float biomeExploration = 15f;
        public float environmentalEvent = 20f;
        public float biomeCompletion = 100f;

        [Header("Research Rewards")]
        public float researchProgress = 10f;
        public float newDiscovery = 50f;

        [Header("Multipliers")]
        public float rareOffspringMultiplier = 2f;
        public float firstTimeMultiplier = 1.5f;
        public float crossBiomeMultiplier = 1.3f;

        [Header("Bonus Rewards")]
        public float offspringAcceptanceBonus = 5f;

        public float GetBreedingExperience(BreedingActivityType activityType)
        {
            return activityType switch
            {
                BreedingActivityType.SuccessfulBreeding => successfulBreeding,
                BreedingActivityType.RareOffspring => rareOffspring,
                BreedingActivityType.FirstTimeBreeding => firstTimeBreeding,
                BreedingActivityType.CrossBiomeBreeding => crossBiomeBreeding,
                _ => successfulBreeding
            };
        }

        public float GetExplorationExperience(ExplorationActivityType activityType)
        {
            return activityType switch
            {
                ExplorationActivityType.CreatureDiscovery => creatureDiscovery,
                ExplorationActivityType.BiomeExploration => biomeExploration,
                ExplorationActivityType.EnvironmentalEvent => environmentalEvent,
                ExplorationActivityType.BiomeCompletion => biomeCompletion,
                _ => biomeExploration
            };
        }

        public float GetResearchExperience(ResearchActivityType activityType)
        {
            return activityType switch
            {
                ResearchActivityType.ResearchProgress => researchProgress,
                ResearchActivityType.NewDiscovery => newDiscovery,
                _ => researchProgress
            };
        }
    }

    // Activity type enums for experience categorization
    public enum BreedingActivityType
    {
        SuccessfulBreeding,
        RareOffspring,
        FirstTimeBreeding,
        CrossBiomeBreeding,
        LegendaryBreeding,
        PerfectGenetics
    }

    public enum ExplorationActivityType
    {
        CreatureDiscovery,
        BiomeExploration,
        EnvironmentalEvent,
        BiomeCompletion,
        RareSpeciesEncounter,
        HiddenAreaDiscovery
    }

    public enum ResearchActivityType
    {
        ResearchProgress,
        NewDiscovery,
        TechnologicalBreakthrough,
        TheoryValidation,
        InnovativeMethod
    }
}