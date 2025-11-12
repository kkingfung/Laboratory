using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using Laboratory.Chimera.Core;
using Laboratory.Core.Diagnostics;

namespace Laboratory.Core.Progression
{
    /// <summary>
    /// ScriptableObject configuration for the player progression system.
    /// Defines experience curves, biome unlock requirements, research trees,
    /// territory expansion costs, and facility configurations in a designer-friendly format.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerProgressionConfig", menuName = "🧬 Laboratory/Progression/Player Progression Config")]
    public class PlayerProgressionConfig : ScriptableObject
    {
        [Header("🎯 Core Progression Settings")]
        [SerializeField] private ProgressionCurveSettings progressionCurve = new ProgressionCurveSettings();
        [SerializeField] private CreatureSlotSettings creatureSlotSettings = new CreatureSlotSettings();

        [Header("🌍 Biome Configuration")]
        [SerializeField] private BiomeProgressionData[] biomeConfigurations = new BiomeProgressionData[0];

        [Header("🔬 Research Tree Configuration")]
        [SerializeField] private ResearchConfiguration[] researchTree = new ResearchConfiguration[0];

        [Header("🏗️ Territory Expansion Settings")]
        [SerializeField] private TerritoryConfiguration[] territoryTiers = new TerritoryConfiguration[0];

        [Header("🏭 Facility Configurations")]
        [SerializeField] private FacilityConfiguration[] facilityTypes = new FacilityConfiguration[0];

        [Header("💰 Experience Rewards")]
        [SerializeField] private ExperienceRewardSettings experienceRewards = new ExperienceRewardSettings();

        [Header("🎨 UI and Visual Settings")]
        [SerializeField] private ProgressionUISettings uiSettings = new ProgressionUISettings();

        [Header("🎮 Gameplay Balance")]
        [SerializeField] private ProgressionBalanceSettings balanceSettings = new ProgressionBalanceSettings();

        // Public accessors
        public ProgressionCurveSettings ProgressionCurve => progressionCurve;
        public CreatureSlotSettings CreatureSlots => creatureSlotSettings;
        public ExperienceRewardSettings ExperienceRewards => experienceRewards;
        public ProgressionUISettings UISettings => uiSettings;
        public ProgressionBalanceSettings BalanceSettings => balanceSettings;

        /// <summary>
        /// Gets biome configuration for the specified biome type
        /// </summary>
        public BiomeProgressionData GetBiomeConfiguration(BiomeType biomeType)
        {
            return biomeConfigurations.FirstOrDefault(b => b.biomeType == biomeType);
        }

        /// <summary>
        /// Gets research configuration for the specified research type
        /// </summary>
        public ResearchConfiguration GetResearchConfiguration(ResearchType researchType)
        {
            return researchTree.FirstOrDefault(r => r.researchType == researchType);
        }

        /// <summary>
        /// Gets territory configuration for the specified tier
        /// </summary>
        public TerritoryConfiguration GetTerritoryConfiguration(TerritoryTier tier)
        {
            return territoryTiers.FirstOrDefault(t => t.tier == tier);
        }

        /// <summary>
        /// Gets facility configuration for the specified facility type
        /// </summary>
        public FacilityConfiguration GetFacilityConfiguration(FacilityType facilityType)
        {
            return facilityTypes.FirstOrDefault(f => f.facilityType == facilityType);
        }

        /// <summary>
        /// Gets all unlocked biomes at the specified level
        /// </summary>
        public List<BiomeType> GetUnlockedBiomesAtLevel(int level)
        {
            return biomeConfigurations
                .Where(b => b.unlockRequirements.minimumLevel <= level)
                .Select(b => b.biomeType)
                .ToList();
        }

        /// <summary>
        /// Gets all available research at the specified level
        /// </summary>
        public List<ResearchType> GetAvailableResearchAtLevel(int level)
        {
            return researchTree
                .Where(r => r.minimumLevel <= level)
                .Select(r => r.researchType)
                .ToList();
        }

        /// <summary>
        /// Validates the progression configuration for consistency and balance
        /// </summary>
        public bool ValidateConfiguration()
        {
            bool isValid = true;

            // Validate progression curve
            if (progressionCurve.baseExperienceRequirement <= 0)
            {
                DebugManager.LogError("Base experience requirement must be greater than 0");
                isValid = false;
            }

            // Validate biome configurations
            foreach (var biome in biomeConfigurations)
            {
                if (biome.unlockRequirements.minimumLevel < 1 || biome.unlockRequirements.minimumLevel > 100)
                {
                    DebugManager.LogError($"Invalid minimum level for biome {biome.biomeType}: {biome.unlockRequirements.minimumLevel}");
                    isValid = false;
                }
            }

            // Validate research tree dependencies
            foreach (var research in researchTree)
            {
                foreach (var prerequisite in research.prerequisites)
                {
                    if (!researchTree.Any(r => r.researchType == prerequisite))
                    {
                        DebugManager.LogError($"Research {research.researchType} has invalid prerequisite: {prerequisite}");
                        isValid = false;
                    }
                }
            }

            // Validate territory tiers
            for (int i = 1; i < territoryTiers.Length; i++)
            {
                if (territoryTiers[i].investmentCost <= territoryTiers[i - 1].investmentCost)
                {
                    DebugManager.LogError($"Territory tier {territoryTiers[i].tier} investment cost should be higher than previous tier");
                    isValid = false;
                }
            }

            return isValid;
        }

        /// <summary>
        /// Calculates the total progression time estimate in hours
        /// </summary>
        public float EstimateTotalProgressionTime()
        {
            float totalExperience = ProgressionUtilities.GetExperienceRequiredForLevel(100);
            float averageExperiencePerHour = experienceRewards.GetAverageExperiencePerHour();
            return totalExperience / averageExperiencePerHour;
        }

        private void OnValidate()
        {
            // Auto-validate when edited in inspector
            ValidateConfiguration();
        }

        // Editor utilities
        [ContextMenu("Generate Default Configuration")]
        private void GenerateDefaultConfiguration()
        {
            GenerateDefaultBiomes();
            GenerateDefaultResearch();
            GenerateDefaultTerritories();
            GenerateDefaultFacilities();
        }

        private void GenerateDefaultBiomes()
        {
            biomeConfigurations = new BiomeProgressionData[]
            {
                new BiomeProgressionData
                {
                    biomeType = BiomeType.Forest,
                    displayName = "Verdant Forest",
                    description = "A lush forest teeming with diverse plant and animal life",
                    unlockRequirements = new BiomeUnlockRequirements { minimumLevel = 1, experienceReward = 0f },
                    specializationBonuses = new BiomeSpecializationBonus[]
                    {
                        new BiomeSpecializationBonus { level = 5, bonusType = BiomeBonusType.BreedingSuccess, value = 0.1f },
                        new BiomeSpecializationBonus { level = 10, bonusType = BiomeBonusType.ExperienceMultiplier, value = 0.15f }
                    }
                },
                new BiomeProgressionData
                {
                    biomeType = BiomeType.Desert,
                    displayName = "Scorching Desert",
                    description = "An arid wasteland where only the hardiest creatures survive",
                    unlockRequirements = new BiomeUnlockRequirements { minimumLevel = 20, experienceReward = 200f },
                    specializationBonuses = new BiomeSpecializationBonus[]
                    {
                        new BiomeSpecializationBonus { level = 5, bonusType = BiomeBonusType.ResistanceTraining, value = 0.2f },
                        new BiomeSpecializationBonus { level = 15, bonusType = BiomeBonusType.RareSpeciesAccess, value = 1f }
                    }
                },
                new BiomeProgressionData
                {
                    biomeType = BiomeType.Arctic,
                    displayName = "Frozen Tundra",
                    description = "A harsh frozen landscape where adaptation is key to survival",
                    unlockRequirements = new BiomeUnlockRequirements { minimumLevel = 40, experienceReward = 400f },
                    specializationBonuses = new BiomeSpecializationBonus[]
                    {
                        new BiomeSpecializationBonus { level = 10, bonusType = BiomeBonusType.ColdAdaptation, value = 0.3f },
                        new BiomeSpecializationBonus { level = 20, bonusType = BiomeBonusType.LegendaryAccess, value = 1f }
                    }
                },
                new BiomeProgressionData
                {
                    biomeType = BiomeType.Volcanic,
                    displayName = "Volcanic Peaks",
                    description = "Dangerous volcanic terrain with unique fire-adapted creatures",
                    unlockRequirements = new BiomeUnlockRequirements { minimumLevel = 60, experienceReward = 600f },
                    specializationBonuses = new BiomeSpecializationBonus[]
                    {
                        new BiomeSpecializationBonus { level = 15, bonusType = BiomeBonusType.HeatResistance, value = 0.4f },
                        new BiomeSpecializationBonus { level = 25, bonusType = BiomeBonusType.ExtremeBreeding, value = 1f }
                    }
                },
                new BiomeProgressionData
                {
                    biomeType = BiomeType.DeepSea,
                    displayName = "Abyssal Depths",
                    description = "The mysterious deep ocean with ancient aquatic life forms",
                    unlockRequirements = new BiomeUnlockRequirements { minimumLevel = 80, experienceReward = 800f },
                    specializationBonuses = new BiomeSpecializationBonus[]
                    {
                        new BiomeSpecializationBonus { level = 20, bonusType = BiomeBonusType.AquaticMastery, value = 0.5f },
                        new BiomeSpecializationBonus { level = 30, bonusType = BiomeBonusType.AncientLineages, value = 1f }
                    }
                }
            };
        }

        private void GenerateDefaultResearch()
        {
            researchTree = new ResearchConfiguration[]
            {
                // Tier 1
                new ResearchConfiguration
                {
                    researchType = ResearchType.BasicBreeding,
                    displayName = "Basic Breeding Techniques",
                    description = "Learn fundamental breeding principles and compatibility",
                    tier = 1,
                    minimumLevel = 1,
                    requiredPoints = 0f,
                    prerequisites = new ResearchType[0],
                    benefits = new ResearchBenefit[]
                    {
                        new ResearchBenefit { benefitType = ResearchBenefitType.BreedingSuccessRate, value = 0.1f, description = "+10% breeding success rate" }
                    }
                },
                new ResearchConfiguration
                {
                    researchType = ResearchType.GeneticAnalysis,
                    displayName = "Genetic Analysis Tools",
                    description = "Advanced tools for analyzing creature genetics",
                    tier = 1,
                    minimumLevel = 5,
                    requiredPoints = 100f,
                    prerequisites = new ResearchType[] { ResearchType.BasicBreeding },
                    benefits = new ResearchBenefit[]
                    {
                        new ResearchBenefit { benefitType = ResearchBenefitType.SpecialToolsUnlock, value = 1f, description = "Unlock genetic analysis interface" }
                    }
                },

                // Tier 2
                new ResearchConfiguration
                {
                    researchType = ResearchType.AdvancedGenetics,
                    displayName = "Advanced Genetic Manipulation",
                    description = "Deeper understanding of genetic inheritance patterns",
                    tier = 2,
                    minimumLevel = 25,
                    requiredPoints = 300f,
                    prerequisites = new ResearchType[] { ResearchType.GeneticAnalysis },
                    benefits = new ResearchBenefit[]
                    {
                        new ResearchBenefit { benefitType = ResearchBenefitType.BreedingSuccessRate, value = 0.15f, description = "+15% breeding success rate" },
                        new ResearchBenefit { benefitType = ResearchBenefitType.CreatureSlotIncrease, value = 2f, description = "+2 creature slots" }
                    }
                },

                // Tier 3
                new ResearchConfiguration
                {
                    researchType = ResearchType.GeneticEngineering,
                    displayName = "Genetic Engineering Mastery",
                    description = "Master-level genetic manipulation techniques",
                    tier = 3,
                    minimumLevel = 50,
                    requiredPoints = 500f,
                    prerequisites = new ResearchType[] { ResearchType.AdvancedGenetics, ResearchType.SelectiveBreeding },
                    benefits = new ResearchBenefit[]
                    {
                        new ResearchBenefit { benefitType = ResearchBenefitType.LegendaryCreatureAccess, value = 1f, description = "Access to legendary creature breeding" },
                        new ResearchBenefit { benefitType = ResearchBenefitType.ExperienceMultiplier, value = 0.25f, description = "+25% experience gain" }
                    }
                }
            };
        }

        private void GenerateDefaultTerritories()
        {
            territoryTiers = new TerritoryConfiguration[]
            {
                new TerritoryConfiguration
                {
                    tier = TerritoryTier.StartingFacility,
                    displayName = "Starting Facility",
                    description = "A basic breeding facility with essential amenities",
                    investmentCost = 0f,
                    minimumLevel = 1,
                    creatureCapacityBonus = 2,
                    facilitySlots = 1,
                    benefits = new TerritoryBenefit[]
                    {
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.BasicBreeding, value = 1f }
                    }
                },
                new TerritoryConfiguration
                {
                    tier = TerritoryTier.RanchUpgrade,
                    displayName = "Ranch Upgrade",
                    description = "Expanded facility with specialized breeding environments",
                    investmentCost = 1000f,
                    minimumLevel = 15,
                    creatureCapacityBonus = 8,
                    facilitySlots = 2,
                    benefits = new TerritoryBenefit[]
                    {
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.SpecializedEnvironments, value = 1f },
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.BreedingEfficiency, value = 0.2f }
                    }
                },
                new TerritoryConfiguration
                {
                    tier = TerritoryTier.BiomeOutpost,
                    displayName = "Biome Research Outpost",
                    description = "Multi-environment facility supporting diverse biomes",
                    investmentCost = 5000f,
                    minimumLevel = 35,
                    creatureCapacityBonus = 20,
                    facilitySlots = 4,
                    benefits = new TerritoryBenefit[]
                    {
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.MultiBiomeSupport, value = 1f },
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.ResearchBonus, value = 0.3f }
                    }
                },
                new TerritoryConfiguration
                {
                    tier = TerritoryTier.RegionalHub,
                    displayName = "Regional Breeding Hub",
                    description = "Advanced cross-biome breeding center with research facilities",
                    investmentCost = 15000f,
                    minimumLevel = 60,
                    creatureCapacityBonus = 50,
                    facilitySlots = 8,
                    benefits = new TerritoryBenefit[]
                    {
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.CrossBiomeBreeding, value = 1f },
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.AdvancedResearch, value = 0.5f }
                    }
                },
                new TerritoryConfiguration
                {
                    tier = TerritoryTier.ContinentalNetwork,
                    displayName = "Continental Network",
                    description = "Massive facility network with automated breeding assistance",
                    investmentCost = 50000f,
                    minimumLevel = 85,
                    creatureCapacityBonus = 100,
                    facilitySlots = 15,
                    benefits = new TerritoryBenefit[]
                    {
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.AutomatedBreeding, value = 1f },
                        new TerritoryBenefit { benefitType = TerritoryBenefitType.GlobalAccess, value = 1f }
                    }
                }
            };
        }

        private void GenerateDefaultFacilities()
        {
            facilityTypes = new FacilityConfiguration[]
            {
                new FacilityConfiguration
                {
                    facilityType = FacilityType.BasicBreedingFacility,
                    displayName = "Basic Breeding Facility",
                    description = "Standard facility for basic creature breeding",
                    constructionCost = 0f,
                    maintenanceCost = 10f,
                    creatureCapacity = 2,
                    supportedBiomes = new BiomeType[] { BiomeType.Forest }
                },
                new FacilityConfiguration
                {
                    facilityType = FacilityType.SpecializedBreedingLab,
                    displayName = "Specialized Breeding Laboratory",
                    description = "Advanced lab with environmental controls for specialized breeding",
                    constructionCost = 2000f,
                    maintenanceCost = 50f,
                    creatureCapacity = 6,
                    supportedBiomes = new BiomeType[] { BiomeType.Forest, BiomeType.Desert }
                },
                new FacilityConfiguration
                {
                    facilityType = FacilityType.BiomeResearchStation,
                    displayName = "Biome Research Station",
                    description = "Research facility specializing in biome-specific adaptations",
                    constructionCost = 5000f,
                    maintenanceCost = 100f,
                    creatureCapacity = 10,
                    supportedBiomes = System.Enum.GetValues(typeof(BiomeType)).Cast<BiomeType>().ToArray()
                }
            };
        }
    }

    // Configuration data structures
    [System.Serializable]
    public class ProgressionCurveSettings
    {
        [Header("Experience Curve")]
        public float baseExperienceRequirement = 100f;
        public float experienceGrowthRate = 1.15f;
        public int maxLevel = 100;

        [Header("Level Curve Visualization")]
        public AnimationCurve experienceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    [System.Serializable]
    public class CreatureSlotSettings
    {
        [Header("Slot Progression")]
        public int baseSlots = 3;
        public int maxSlots = 25;
        public int slotsPerLevel = 1;

        [Header("Bonus Slot Levels")]
        public int[] bonusSlotLevels = { 20, 40, 60, 80, 100 };
        public int slotsPerBonus = 2;
    }

    [System.Serializable]
    public class ExperienceRewardSettings
    {
        [Header("Base Experience Values")]
        public float breedingSuccess = 25f;
        public float creatureDiscovery = 50f;
        public float biomeExploration = 15f;
        public float researchCompletion = 100f;
        public float questCompletion = 75f;
        public float rareDiscovery = 150f;
        public float socialInteraction = 10f;
        public float territoryExpansion = 200f;
        public float achievementUnlock = 100f;

        [Header("Multipliers")]
        public float difficultyMultiplier = 1.5f;
        public float rareCreatureMultiplier = 2.0f;
        public float firstTimeMultiplier = 1.25f;

        public float GetAverageExperiencePerHour()
        {
            // Estimate based on typical gameplay activities
            return (breedingSuccess * 2 + creatureDiscovery * 1 + biomeExploration * 4 + questCompletion * 0.5f) * 60f / 30f; // 30-minute cycles
        }
    }

    [System.Serializable]
    public class ProgressionUISettings
    {
        [Header("Visual Settings")]
        public Color experienceBarColor = Color.green;
        public Color levelUpColor = Color.gold;
        public Color specializationColor = Color.blue;

        [Header("Animation Settings")]
        public float experienceAnimationSpeed = 2f;
        public float levelUpAnimationDuration = 1.5f;
        public bool showProgressionParticles = true;

        [Header("Notification Settings")]
        public bool showLevelUpNotifications = true;
        public bool showResearchUnlockNotifications = true;
        public bool showBiomeUnlockNotifications = true;
        public float notificationDuration = 3f;
    }

    [System.Serializable]
    public class ProgressionBalanceSettings
    {
        [Header("Balance Factors")]
        [Range(0.1f, 3f)] public float globalExperienceMultiplier = 1f;
        [Range(0.5f, 2f)] public float difficultyScaling = 1f;
        [Range(0.1f, 2f)] public float socialExperienceBonus = 1.2f;

        [Header("Catch-up Mechanics")]
        public bool enableCatchUpExperience = true;
        public float catchUpThreshold = 0.5f; // Activate when 50% behind average
        public float catchUpMultiplier = 1.5f;

        [Header("Plateau Prevention")]
        public bool enablePlateauPrevention = true;
        public float plateauDetectionTime = 1800f; // 30 minutes
        public float plateauExperienceBonus = 1.3f;
    }

    // Additional configuration structures
    [System.Serializable]
    public class BiomeProgressionData
    {
        public BiomeType biomeType;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite biomeIcon;
        public Color biomeColor = Color.white;
        public BiomeUnlockRequirements unlockRequirements;
        public BiomeSpecializationBonus[] specializationBonuses;
    }

    [System.Serializable]
    public class ResearchConfiguration
    {
        public ResearchType researchType;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite researchIcon;
        public int tier = 1;
        public int minimumLevel = 1;
        public float requiredPoints = 100f;
        public ResearchType[] prerequisites;
        public ResearchBenefit[] benefits;
    }

    [System.Serializable]
    public class TerritoryConfiguration
    {
        public TerritoryTier tier;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite territoryIcon;
        public float investmentCost;
        public int minimumLevel;
        public int creatureCapacityBonus;
        public int facilitySlots;
        public TerritoryBenefit[] benefits;
    }

    [System.Serializable]
    public class FacilityConfiguration
    {
        public FacilityType facilityType;
        public string displayName;
        [TextArea(2, 4)] public string description;
        public Sprite facilityIcon;
        public float constructionCost;
        public float maintenanceCost;
        public int creatureCapacity;
        public BiomeType[] supportedBiomes;
        public FacilityUpgradeOption[] upgradeOptions;
    }

    [System.Serializable]
    public class BiomeSpecializationBonus
    {
        public int level;
        public BiomeBonusType bonusType;
        public float value;
        public string description;
    }

    [System.Serializable]
    public class TerritoryBenefit
    {
        public TerritoryBenefitType benefitType;
        public float value;
        public string description;
    }

    [System.Serializable]
    public class FacilityUpgradeOption
    {
        public FacilityUpgradeType upgradeType;
        public string displayName;
        public float cost;
        public float benefit;
        public string description;
    }

    // Additional enums
    public enum BiomeBonusType
    {
        BreedingSuccess,
        ExperienceMultiplier,
        ResistanceTraining,
        RareSpeciesAccess,
        ColdAdaptation,
        LegendaryAccess,
        HeatResistance,
        ExtremeBreeding,
        AquaticMastery,
        AncientLineages
    }

    public enum TerritoryBenefitType
    {
        BasicBreeding,
        SpecializedEnvironments,
        BreedingEfficiency,
        MultiBiomeSupport,
        ResearchBonus,
        CrossBiomeBreeding,
        AdvancedResearch,
        AutomatedBreeding,
        GlobalAccess
    }
}