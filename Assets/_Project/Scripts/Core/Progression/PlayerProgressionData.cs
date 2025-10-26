using UnityEngine;
using System.Collections.Generic;
using Laboratory.Chimera.Core;

namespace Laboratory.Core.Progression
{
    /// <summary>
    /// Core data structures for the player progression system, including experience tracking,
    /// biome specializations, research progress, territory expansion, and facility management.
    /// All classes are serializable for save/load functionality and analytics integration.
    /// </summary>

    [System.Serializable]
    public class PlayerProgressionData
    {
        [Header("Core Progression")]
        public int geneticistLevel = 1;
        public float currentExperience = 0f;
        public float totalExperienceEarned = 0f;

        [Header("Timestamps")]
        public float creationTime;
        public float lastUpdated;
        public float totalPlayTime = 0f;

        [Header("Achievements")]
        public List<string> unlockedAchievements = new List<string>();
        public Dictionary<string, float> progressionMilestones = new Dictionary<string, float>();
    }

    [System.Serializable]
    public class BiomeSpecializationData
    {
        [Header("Biome Information")]
        public BiomeType biomeType;
        public bool isUnlocked = false;

        [Header("Specialization Progress")]
        public float specializationLevel = 0f;
        public float totalExperience = 0f;
        public float lastExperienceGain = 0f;

        [Header("Biome-Specific Stats")]
        public int creaturesDiscovered = 0;
        public int successfulBreedings = 0;
        public float timeSpentInBiome = 0f;
        public List<string> discoveredSpecies = new List<string>();

        [Header("Specialization Benefits")]
        public float breedingSuccessBonus = 0f;
        public float experienceMultiplier = 1f;
        public float resourceGatheringBonus = 0f;
        public bool hasAdvancedToolsAccess = false;
    }

    [System.Serializable]
    public class ResearchProgress
    {
        [Header("Research Information")]
        public ResearchType researchType;
        public bool isUnlocked = false;

        [Header("Progress Tracking")]
        public float progressPoints = 0f;
        public float requiredPoints = 100f;
        public float completionTime = 0f;

        [Header("Prerequisites")]
        public List<ResearchType> prerequisites = new List<ResearchType>();
        public int minimumLevel = 1;
        public List<BiomeType> requiredBiomes = new List<BiomeType>();

        [Header("Benefits")]
        public List<ResearchBenefit> benefits = new List<ResearchBenefit>();
        public bool providesCreatureSlotBonus = false;
        public int bonusSlots = 0;
    }

    [System.Serializable]
    public class TerritoryExpansionData
    {
        [Header("Territory Status")]
        public TerritoryTier currentTier = TerritoryTier.StartingFacility;
        public float totalInvestment = 0f;
        public float lastExpansionTime = 0f;

        [Header("Facilities")]
        public List<FacilityData> facilitiesOwned = new List<FacilityData>();
        public int totalCreatureCapacity = 2;
        public int activeFacilities = 1;

        [Header("Expansion History")]
        public List<TerritoryExpansionRecord> expansionHistory = new List<TerritoryExpansionRecord>();
    }

    [System.Serializable]
    public class FacilityData
    {
        [Header("Facility Information")]
        public FacilityType facilityType;
        public BiomeType biomeLocation;
        public bool isActive = true;

        [Header("Capacity and Efficiency")]
        public int creatureCapacity = 2;
        public float breedingEfficiencyBonus = 0f;
        public float maintenanceCost = 0f;

        [Header("Specialization")]
        public List<BiomeType> specializedBiomes = new List<BiomeType>();
        public List<CreatureSpeciesType> supportedSpecies = new List<CreatureSpeciesType>();

        [Header("Upgrades")]
        public int upgradeLevel = 1;
        public List<FacilityUpgrade> appliedUpgrades = new List<FacilityUpgrade>();
        public float constructionTime = 0f;
    }

    [System.Serializable]
    public class ExperienceGain
    {
        public float amount;
        public ExperienceSource source;
        public string description;
        public float timestamp;
        public BiomeType biomeContext;
        public Dictionary<string, object> contextData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class PlayerProgressionStats
    {
        [Header("Core Stats")]
        public int geneticistLevel;
        public float currentExperience;
        public float experienceToNextLevel;
        public float totalExperienceEarned;

        [Header("Creature Management")]
        public int availableCreatureSlots;
        public int maxCreatureSlots;
        public int currentCreatureCount = 0;

        [Header("World Progress")]
        public List<BiomeType> unlockedBiomes = new List<BiomeType>();
        public Dictionary<BiomeType, float> biomeSpecializationLevels = new Dictionary<BiomeType, float>();
        public List<ResearchType> unlockedResearch = new List<ResearchType>();

        [Header("Territory Management")]
        public TerritoryTier currentTerritoryTier;
        public int totalFacilities;
        public float totalTerritoryInvestment;

        [Header("Achievements")]
        public int totalAchievements = 0;
        public float completionPercentage = 0f;
    }

    [System.Serializable]
    public class BiomeUnlockRequirements
    {
        public BiomeType biomeType;
        public int minimumLevel = 1;
        public List<ResearchType> requiredResearch = new List<ResearchType>();
        public List<BiomeType> prerequisiteBiomes = new List<BiomeType>();
        public float experienceReward = 100f;
        public string unlockConditionDescription;
    }

    [System.Serializable]
    public class TerritoryRequirements
    {
        public TerritoryTier tier;
        public int minimumLevel = 1;
        public float investmentCost = 1000f;
        public List<ResearchType> requiredResearch = new List<ResearchType>();
        public int minimumFacilities = 0;
        public float experienceReward = 200f;
        public string requirementDescription;
    }

    [System.Serializable]
    public class TerritoryExpansionRecord
    {
        public TerritoryTier tier;
        public float expansionTime;
        public float investmentCost;
        public string expansionReason;
    }

    [System.Serializable]
    public class ResearchBenefit
    {
        public ResearchBenefitType benefitType;
        public float value;
        public string description;
        public bool isPercentage = false;
    }

    [System.Serializable]
    public class FacilityUpgrade
    {
        public FacilityUpgradeType upgradeType;
        public float cost;
        public float benefit;
        public string description;
        public float installationTime;
    }

    // Enums for progression system
    public enum ExperienceSource
    {
        BreedingSuccess,
        CreatureDiscovery,
        BiomeExploration,
        ResearchCompletion,
        QuestCompletion,
        RareDiscovery,
        SocialInteraction,
        TerritoryExpansion,
        BiomeSpecialization,
        AchievementUnlock,
        TestReward
    }


    public enum ResearchType
    {
        // Tier 1 - Basic Research
        BasicBreeding,
        GeneticAnalysis,
        BiomeAdaptation,
        CreatureCare,

        // Tier 2 - Intermediate Research
        AdvancedGenetics,
        SelectiveBreeding,
        EnvironmentalOptimization,
        SpeciesCompatibility,

        // Tier 3 - Advanced Research
        GeneticEngineering,
        CrossSpeciesBreeding,
        ArtificialSelection,
        ExperienceOptimization,

        // Tier 4 - Master Research
        LegendaryLineages,
        EcosystemManagement,
        AdvancedFacilities,
        GlobalBreedingNetwork
    }

    public enum TerritoryTier
    {
        StartingFacility,    // 2 creature capacity, basic amenities
        RanchUpgrade,        // 8 creature capacity, specialized environments
        BiomeOutpost,        // Multi-environment facility, 20 creature capacity
        RegionalHub,         // Cross-biome breeding center, 50 creature capacity
        ContinentalNetwork   // Multiple facilities, automated assistance
    }

    public enum FacilityType
    {
        BasicBreedingFacility,
        SpecializedBreedingLab,
        BiomeResearchStation,
        GeneticAnalysisCenter,
        CreatureNursery,
        ExperimentalLaboratory,
        EcosystemSimulator,
        AdvancedBreedingComplex
    }

    public enum CreatureSpeciesType
    {
        CommonForestCreature,
        RareDesertSpecies,
        LegendaryArcticBeast,
        // Add more as needed
    }

    public enum ResearchBenefitType
    {
        BreedingSuccessRate,
        ExperienceMultiplier,
        CreatureSlotIncrease,
        BiomeEfficiencyBonus,
        FacilityUpgradeAccess,
        SpecialToolsUnlock,
        CrossBiomeBreeding,
        LegendaryCreatureAccess
    }

    public enum FacilityUpgradeType
    {
        CapacityIncrease,
        EfficiencyImprovement,
        EnvironmentalControl,
        AutomatedCare,
        SpecializationBonus,
        ResearchIntegration,
        SecurityEnhancement,
        QualityOfLifeImprovement
    }

    /// <summary>
    /// Static utility class for progression calculations and validation
    /// </summary>
    public static class ProgressionUtilities
    {
        public static float CalculateLevelProgress(float currentXP, int currentLevel)
        {
            float currentLevelXP = GetExperienceRequiredForLevel(currentLevel);
            float nextLevelXP = GetExperienceRequiredForLevel(currentLevel + 1);

            if (nextLevelXP <= currentLevelXP) return 1f;

            return (currentXP - currentLevelXP) / (nextLevelXP - currentLevelXP);
        }

        public static float GetExperienceRequiredForLevel(int level)
        {
            if (level <= 1) return 0f;

            float baseXP = 100f;
            float growthRate = 1.15f;
            float total = 0f;

            for (int i = 2; i <= level; i++)
            {
                total += baseXP * Mathf.Pow(growthRate, i - 2);
            }

            return total;
        }

        public static string GetLevelTitle(int level)
        {
            return level switch
            {
                >= 81 => "Grandmaster Geneticist",
                >= 61 => "Master Breeder",
                >= 41 => "Expert Researcher",
                >= 21 => "Skilled Apprentice",
                >= 1 => "Novice Geneticist",
                _ => "Unknown"
            };
        }

        public static string GetBiomeSpecializationTitle(BiomeType biome, float level)
        {
            string biomePrefix = biome switch
            {
                BiomeType.Forest => "Forest",
                BiomeType.Desert => "Desert",
                BiomeType.Arctic => "Arctic",
                BiomeType.Volcanic => "Volcanic",
                BiomeType.DeepSea => "Deep Sea",
                _ => biome.ToString()
            };

            string suffix = level switch
            {
                >= 50f => "Master",
                >= 30f => "Expert",
                >= 15f => "Specialist",
                >= 5f => "Naturalist",
                _ => "Explorer"
            };

            return $"{biomePrefix} {suffix}";
        }

        public static Color GetProgressColor(float progress)
        {
            return progress switch
            {
                >= 0.8f => Color.gold,
                >= 0.6f => Color.green,
                >= 0.4f => Color.yellow,
                >= 0.2f => Color.orange,
                _ => Color.red
            };
        }

        public static bool ValidateProgressionData(PlayerProgressionData data)
        {
            if (data == null) return false;
            if (data.geneticistLevel < 1 || data.geneticistLevel > 100) return false;
            if (data.currentExperience < 0f) return false;
            if (data.totalExperienceEarned < data.currentExperience) return false;

            return true;
        }
    }

    /// <summary>
    /// Progression notification data structure for UI display
    /// </summary>
    [System.Serializable]
    public class ProgressionNotification
    {
        public string title;
        public string message;
        public ProgressionNotificationType type;
        public UnityEngine.Sprite iconSprite;
        public float displayDuration = 3f;
        public bool requiresAcknowledgment = false;
        public System.DateTime timestamp = System.DateTime.Now;
    }

    /// <summary>
    /// Types of progression notifications
    /// </summary>
    public enum ProgressionNotificationType
    {
        LevelUp,
        ResearchUnlocked,
        BiomeUnlocked,
        BiomeSpecialization,
        TerritoryExpanded,
        AchievementUnlocked,
        MilestoneReached,
        Warning,
        Error
    }
}