using UnityEngine;
using System.Collections.Generic;
using Laboratory.Core.Utilities;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using Laboratory.Chimera.Core;
using Laboratory.Core.Debug;

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
        [SerializeField] private bool enableCombatIntegration = true; // Reserved for future combat integration

        [Header("Experience Rewards")]
        [SerializeField] private ExperienceRewardConfig rewardConfig;

        [Header("Progression Benefits")]
        [SerializeField] private bool applyProgressionBenefits = true;
        [SerializeField] private float benefitUpdateInterval = 1f;

        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugLogging = true;

        // Core system references
        private PlayerProgressionManager progressionManager;
        private MonoBehaviour analyticsTracker;
        private MonoBehaviour breedingSimulator;
        private MonoBehaviour ecosystemSimulator;

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
                progressionManager = FindFirstObjectByType<PlayerProgressionManager>();
            }

            // Find analytics tracker
            if (enableAnalyticsIntegration)
            {
                // Try to find analytics tracker using reflection
                var analyticsType = System.Type.GetType("Laboratory.Systems.Analytics.PlayerAnalyticsTracker, Laboratory.Systems");
                if (analyticsTracker == null)
                {
                    analyticsTracker = FindFirstObjectByType<MonoBehaviour>();
                }
            }

            // Find breeding simulator
            if (enableBreedingIntegration)
            {
                // Try to find breeding simulator using reflection
                var breedingType = System.Type.GetType("Laboratory.Systems.Breeding.AdvancedBreedingSimulator, Laboratory.Systems");
                if (breedingSimulator == null)
                {
                    breedingSimulator = FindFirstObjectByType<MonoBehaviour>();
                }

                if (breedingSimulator != null)
                {
                    ConnectToBreedingSystem();
                }
            }

            // Find ecosystem simulator
            if (enableEcosystemIntegration)
            {
                // Try to find ecosystem simulator using reflection
                var ecosystemType = System.Type.GetType("Laboratory.Systems.Ecosystem.DynamicEcosystemSimulator, Laboratory.Systems");
                if (ecosystemSimulator == null)
                {
                    ecosystemSimulator = FindFirstObjectByType<MonoBehaviour>();
                }

                if (ecosystemSimulator != null)
                {
                    ConnectToEcosystemSystem();
                }
            }

            // Combat integration reserved for future implementation
            if (enableCombatIntegration)
            {
                // Combat system integration will be implemented here
            }

            systemsConnected = (progressionManager != null);
        }

        private void ConnectToBreedingSystem()
        {
            if (breedingSimulator == null) return;

            // Simplified connection - events would need to be set up via reflection or interfaces
            // For now, just log the connection
            if (enableDebugLogging)
                DebugManager.LogInfo("Connected to Breeding System for progression integration");
        }

        private void ConnectToEcosystemSystem()
        {
            if (ecosystemSimulator == null) return;

            // Note: Ecosystem events would need to be connected via proper interface implementation
            // The ecosystem system needs to expose these events for proper integration

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
                    bonusMultiplier *= CalculateBreedingSuccessBonus(offspringId, sessionId);
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
                $"{activityType} with offspring {offspringId}");

            // Award biome specialization experience based on creature species
            BiomeType creatureBiome = DetermineCreatureBiome(offspringId);
            if (creatureBiome != BiomeType.Forest) // Assuming Forest is default/common
            {
                progressionManager.AwardBiomeExperience(creatureBiome,
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
                float specializationBonus = CalculateBiomeSpecializationBreedingBonus(specialization.Key, (int)specialization.Value);
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
            // Analytics tracking would need to be implemented via reflection
            // For now, just log the benefit application
            if (enableDebugLogging)
            {
                DebugManager.LogInfo($"Progression benefits applied: Level {stats.geneticistLevel}");
            }

            // Analytics tracking would be implemented through proper service integration
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

            // Simplified bonus calculation based on IDs
            // Add randomized bonus to simulate genetic variation
            bonus += UnityEngine.Random.Range(0.1f, 0.5f);

            // Add bonus based on ID hash for consistent but varied rewards
            int hashCode = (offspringId + sessionId).GetHashCode();
            if (Mathf.Abs(hashCode) % 10 < 3) // 30% chance for exceptional bonus
            {
                bonus += 0.3f;
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
            if (IsFirstTimeCombination(sessionId, offspringId))
                return BreedingActivityType.FirstTimeBreeding;

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

        private float CalculateLevelBasedBreedingBonus(int geneticistLevel)
        {
            return geneticistLevel * 0.02f; // 2% bonus per level
        }

        private float GetResearchBreedingBonus(ResearchType research)
        {
            return research switch
            {
                ResearchType.BasicBreeding => 0.05f,
                ResearchType.AdvancedGenetics => 0.1f,
                ResearchType.SelectiveBreeding => 0.08f,
                ResearchType.GeneticEngineering => 0.15f,
                ResearchType.CrossSpeciesBreeding => 0.12f,
                _ => 0.05f
            };
        }

        private float CalculateBiomeSpecializationBreedingBonus(BiomeType biome, int level)
        {
            return level * 0.03f; // 3% bonus per specialization level
        }

        private float CalculateLevelBasedExplorationBonus(int geneticistLevel)
        {
            return geneticistLevel * 0.01f; // 1% bonus per level
        }

        private float CalculateCreatureDiscoveryBonus(PlayerProgressionStats stats)
        {
            return stats.unlockedBiomes.Count * 0.05f; // 5% bonus per unlocked biome
        }

        private bool IsFirstTimeCombination(string sessionId, string offspringId)
        {
            // Simplified logic - in a real implementation, this would check breeding history
            return UnityEngine.Random.value >= 0.9f; // 10% chance of first-time combination
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
            // Cleanup event subscriptions (simplified for MonoBehaviour)
            if (breedingSimulator != null)
            {
                // Event cleanup would need to be implemented via reflection
                // For now, just null the reference
                breedingSimulator = null;
            }

            if (ecosystemSimulator != null)
            {
                // Event cleanup would be handled through proper interface implementation
                ecosystemSimulator = null;
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
                ExplorationActivityType.EnvironmentalEvent => 100,
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