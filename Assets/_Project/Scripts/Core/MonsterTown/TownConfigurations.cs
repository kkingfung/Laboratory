using UnityEngine;
using System;
using Laboratory.Chimera.Configuration;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// ScriptableObject configurations for Monster Town - follows existing Chimera pattern
    /// Designer-friendly configuration system that integrates with existing infrastructure
    /// </summary>

    #region Core Town Configuration

    /// <summary>
    /// Master town configuration - extends existing ScriptableObject patterns
    /// </summary>
    [CreateAssetMenu(fileName = "New Monster Town Config", menuName = "Chimera/Monster Town/Town Configuration", order = 1)]
    public class MonsterTownConfig : ScriptableObject
    {
        [Header("Basic Town Info")]
        [SerializeField] public string townName = "New Monster Town";
        [SerializeField] [TextArea(3, 5)] public string description = "A thriving monster breeding community";
        [SerializeField] public Sprite townIcon;

        [Header("Town Layout")]
        [SerializeField] public Vector2 townSize = new Vector2(100f, 100f);
        [SerializeField] public bool useGridBasedPlacement = true;
        [SerializeField] [Range(1f, 10f)] public float gridSize = 5f;
        [SerializeField] public LayerMask buildingLayerMask = 1;

        [Header("Initial Setup")]
        [SerializeField] public TownResourcesConfig startingResources;
        [SerializeField] public InitialBuildingConfig[] initialBuildings;
        [SerializeField] public int maxPopulation = 50;

        [Header("Gameplay Settings")]
        [SerializeField] public bool enableResourceGeneration = true;
        [SerializeField] [Range(0.1f, 10f)] public float resourceGenerationRate = 1f;
        [SerializeField] public bool enableHappinessSystem = true;
        [SerializeField] public bool enableSeasonalEffects = true;

        [Header("Activity Integration")]
        [SerializeField] public ActivityCenterConfig[] availableActivities;
        [SerializeField] public bool unlockAllActivitiesAtStart = false;
        [SerializeField] [Range(0f, 1f)] public float activitySuccessBaseRate = 0.6f;

        [Header("Progression")]
        [SerializeField] public TownUpgradeConfig[] townUpgrades;
        [SerializeField] public BuildingUnlockConfig[] buildingUnlocks;

        [Header("Integration")]
        [SerializeField] public bool integrateWithChimeraBreeding = true;
        [SerializeField] public bool enableMultiplayerFeatures = false;
        [SerializeField] public ChimeraBiomeConfig[] compatibleBiomes;

        /// <summary>
        /// Validate configuration and provide helpful feedback
        /// </summary>
        public void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(townName))
            {
                Debug.LogWarning($"[{name}] Town name is empty");
            }

            if (startingResources == null)
            {
                Debug.LogWarning($"[{name}] No starting resources configured - using defaults");
            }

            if (initialBuildings == null || initialBuildings.Length == 0)
            {
                Debug.LogWarning($"[{name}] No initial buildings configured");
            }

            if (availableActivities == null || availableActivities.Length == 0)
            {
                Debug.LogWarning($"[{name}] No activities configured - town will have limited functionality");
            }

            Debug.Log($"[{name}] Configuration validated successfully");
        }
    }

    /// <summary>
    /// Building configuration - designer-friendly building setup
    /// </summary>
    [CreateAssetMenu(fileName = "New Building Config", menuName = "Chimera/Monster Town/Building Configuration", order = 2)]
    public class BuildingConfig : ScriptableObject
    {
        [Header("Basic Building Info")]
        [SerializeField] public BuildingType buildingType;
        [SerializeField] public string buildingName = "New Building";
        [SerializeField] [TextArea(2, 4)] public string description = "";
        [SerializeField] public Sprite buildingIcon;
        [SerializeField] public GameObject buildingPrefab;

        [Header("Construction")]
        [SerializeField] public TownResourcesConfig constructionCost;
        [SerializeField] public float constructionTime = 5f; // seconds
        [SerializeField] public Vector3 size = Vector3.one * 5f;
        [SerializeField] public bool requiresFoundation = true;

        [Header("Functionality")]
        [SerializeField] public BuildingFunction[] functions;
        [SerializeField] public TownResourcesConfig resourceGeneration;
        [SerializeField] [Range(0f, 10f)] public float generationInterval = 60f; // seconds
        [SerializeField] public int capacity = 10; // monsters or items

        [Header("Requirements")]
        [SerializeField] public BuildingType[] requiredBuildings;
        [SerializeField] public int minimumTownLevel = 1;
        [SerializeField] public TownResourcesConfig unlockCost;

        [Header("Upgrades")]
        [SerializeField] public BuildingUpgrade[] upgrades;
        [SerializeField] public int maxUpgradeLevel = 5;

        [Header("Visual & Audio")]
        [SerializeField] public Material[] buildingMaterials;
        [SerializeField] public AudioClip constructionSound;
        [SerializeField] public AudioClip ambientSound;
        [SerializeField] public ParticleSystem constructionEffect;

        /// <summary>
        /// Check if building can be constructed with current resources
        /// </summary>
        public bool CanConstruct(TownResources currentResources)
        {
            return constructionCost == null || currentResources.CanAfford(constructionCost.ToTownResources());
        }

        /// <summary>
        /// Get construction cost as TownResources
        /// </summary>
        public TownResources GetConstructionCost()
        {
            return constructionCost?.ToTownResources() ?? TownResources.Zero;
        }
    }

    /// <summary>
    /// Activity center configuration
    /// </summary>
    [CreateAssetMenu(fileName = "New Activity Config", menuName = "Chimera/Monster Town/Activity Configuration", order = 3)]
    public class ActivityCenterConfig : ScriptableObject
    {
        [Header("Activity Info")]
        [SerializeField] public ActivityType activityType;
        [SerializeField] public string activityName = "New Activity";
        [SerializeField] [TextArea(3, 5)] public string description = "";
        [SerializeField] public Sprite activityIcon;
        [SerializeField] public GameObject activityPrefab;

        [Header("Gameplay")]
        [SerializeField] public GenreGameplayConfig gameplayConfig;
        [SerializeField] [Range(0.1f, 2f)] public float difficultyMultiplier = 1f;
        [SerializeField] public TownResourcesConfig entryCost;
        [SerializeField] public float activityDuration = 30f; // seconds

        [Header("Performance Calculation")]
        [SerializeField] public StatWeight[] statWeights;
        [SerializeField] [Range(0f, 1f)] public float geneticInfluence = 0.6f;
        [SerializeField] [Range(0f, 1f)] public float experienceInfluence = 0.3f;
        [SerializeField] [Range(0f, 1f)] public float equipmentInfluence = 0.1f;

        [Header("Rewards")]
        [SerializeField] public RewardTier[] rewardTiers;
        [SerializeField] public TownResourcesConfig baseRewards;
        [SerializeField] [Range(0f, 2f)] public float rewardMultiplier = 1f;

        [Header("Requirements")]
        [SerializeField] public BuildingType requiredBuilding = BuildingType.ActivityCenter;
        [SerializeField] public int minimumMonsterLevel = 1;
        [SerializeField] public float minimumHappiness = 0.5f;

        [Header("Educational Content")]
        [SerializeField] public bool hasEducationalContent = true;
        [SerializeField] [TextArea(3, 5)] public string educationalDescription = "";
        [SerializeField] public string[] learningObjectives;

        /// <summary>
        /// Calculate performance based on monster stats and configuration
        /// </summary>
        public float CalculatePerformance(MonsterStats stats, float experience, float equipmentBonus)
        {
            float statScore = 0f;
            float totalWeight = 0f;

            foreach (var weight in statWeights)
            {
                statScore += GetStatValue(stats, weight.statType) * weight.weight;
                totalWeight += weight.weight;
            }

            if (totalWeight > 0)
                statScore /= totalWeight;

            return Mathf.Clamp01(
                statScore * geneticInfluence +
                experience * experienceInfluence +
                equipmentBonus * equipmentInfluence
            );
        }

        private float GetStatValue(MonsterStats stats, StatType statType)
        {
            return statType switch
            {
                StatType.Strength => stats.strength,
                StatType.Agility => stats.agility,
                StatType.Vitality => stats.vitality,
                StatType.Intelligence => stats.intelligence,
                StatType.Social => stats.social,
                StatType.Adaptability => stats.adaptability,
                StatType.Speed => stats.speed,
                StatType.Charisma => stats.charisma,
                _ => 50f
            };
        }
    }

    #endregion

    #region Configuration Data Structures

    /// <summary>
    /// Town resources configuration - designer-friendly resource setup
    /// </summary>
    [CreateAssetMenu(fileName = "New Town Resources", menuName = "Chimera/Monster Town/Resources Configuration", order = 4)]
    public class TownResourcesConfig : ScriptableObject
    {
        [Header("Basic Resources")]
        [SerializeField] public int coins = 0;
        [SerializeField] public int gems = 0;
        [SerializeField] public int activityTokens = 0;
        [SerializeField] public int geneticSamples = 0;
        [SerializeField] public int materials = 0;
        [SerializeField] public int energy = 0;

        /// <summary>
        /// Convert to runtime TownResources struct
        /// </summary>
        public TownResources ToTownResources()
        {
            return new TownResources
            {
                coins = this.coins,
                gems = this.gems,
                activityTokens = this.activityTokens,
                geneticSamples = this.geneticSamples,
                materials = this.materials,
                energy = this.energy
            };
        }

        /// <summary>
        /// Create from runtime TownResources
        /// </summary>
        public void FromTownResources(TownResources resources)
        {
            coins = resources.coins;
            gems = resources.gems;
            activityTokens = resources.activityTokens;
            geneticSamples = resources.geneticSamples;
            materials = resources.materials;
            energy = resources.energy;
        }
    }

    /// <summary>
    /// Initial building configuration for town setup
    /// </summary>
    [Serializable]
    public struct InitialBuildingConfig
    {
        public BuildingType buildingType;
        public Vector3 relativePosition; // Relative to town center
        public bool constructImmediately;
        public string notes;
    }

    /// <summary>
    /// Building function configuration
    /// </summary>
    [Serializable]
    public struct BuildingFunction
    {
        public FunctionType functionType;
        public float efficiency;
        public int capacity;
        public TownResourcesConfig resourceCost;
        public string description;
    }

    /// <summary>
    /// Building upgrade configuration
    /// </summary>
    [Serializable]
    public struct BuildingUpgrade
    {
        public int level;
        public string upgradeName;
        public TownResourcesConfig upgradeCost;
        public float efficiencyBonus;
        public int capacityBonus;
        public string description;
    }

    /// <summary>
    /// Genre-specific gameplay configuration
    /// </summary>
    [Serializable]
    public struct GenreGameplayConfig
    {
        public GameplayMechanic[] mechanics;
        public float baseSuccessRate;
        public float skillCeiling;
        public int maxAttempts;
        public bool allowRetries;
    }

    /// <summary>
    /// Stat weight for performance calculation
    /// </summary>
    [Serializable]
    public struct StatWeight
    {
        public StatType statType;
        [Range(0f, 1f)] public float weight;
        public string description;
    }

    /// <summary>
    /// Reward tier configuration
    /// </summary>
    [Serializable]
    public struct RewardTier
    {
        public string tierName;
        [Range(0f, 1f)] public float minimumPerformance;
        public TownResourcesConfig rewards;
        public float experienceMultiplier;
        public string description;
    }

    /// <summary>
    /// Town upgrade configuration
    /// </summary>
    [Serializable]
    public struct TownUpgradeConfig
    {
        public int level;
        public string upgradeName;
        public TownResourcesConfig cost;
        public string[] unlockedFeatures;
        public int populationIncrease;
        public string description;
    }

    /// <summary>
    /// Building unlock configuration
    /// </summary>
    [Serializable]
    public struct BuildingUnlockConfig
    {
        public BuildingType buildingType;
        public int requiredTownLevel;
        public TownResourcesConfig unlockCost;
        public BuildingType[] prerequisiteBuildings;
        public string unlockCondition;
    }

    /// <summary>
    /// Gameplay mechanic definition
    /// </summary>
    [Serializable]
    public struct GameplayMechanic
    {
        public MechanicType mechanicType;
        public float influence;
        public string description;
    }

    #endregion

    #region Enums for Configuration

    /// <summary>
    /// Building function types
    /// </summary>
    public enum FunctionType
    {
        ResourceGeneration,
        MonsterStorage,
        ActivityHosting,
        Research,
        Training,
        Breeding,
        Medical,
        Social,
        Storage
    }

    /// <summary>
    /// Monster stat types for configuration
    /// </summary>
    public enum StatType
    {
        Strength,
        Agility,
        Vitality,
        Intelligence,
        Social,
        Adaptability,
        Speed,
        Charisma
    }

    /// <summary>
    /// Gameplay mechanic types
    /// </summary>
    public enum MechanicType
    {
        TimingBased,
        StrategyBased,
        ReflexBased,
        PuzzleBased,
        SocialBased,
        CreativityBased,
        EnduranceBased,
        PrecisionBased
    }

    #endregion
}