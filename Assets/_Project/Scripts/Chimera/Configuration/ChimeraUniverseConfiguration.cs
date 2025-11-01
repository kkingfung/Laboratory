using UnityEngine;
using Unity.Collections;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;

namespace Laboratory.Chimera.Configuration
{
    /// <summary>
    /// THE MASTER CONFIGURATION - Controls EVERYTHING in the Chimera universe
    /// Drop this into Resources/Configs/ and designers can control the entire game without touching code!
    /// </summary>
    [CreateAssetMenu(fileName = "ChimeraUniverse", menuName = "Project Chimera/Universe Configuration", order = 0)]
    public class ChimeraUniverseConfiguration : ScriptableObject
    {
        [Header("🌍 WORLD SETTINGS")]
        [SerializeField] private WorldSettings worldSettings = new WorldSettings();

        [Header("🧬 GENETIC EVOLUTION")]
        [SerializeField] private GeneticEvolutionSettings geneticSettings = new GeneticEvolutionSettings();

        [Header("🧠 BEHAVIOR & AI")]
        [SerializeField] private BehaviorSettings behaviorSettings = new BehaviorSettings();

        [Header("💕 BREEDING & REPRODUCTION")]
        [SerializeField] private BreedingSettings breedingSettings = new BreedingSettings();

        [Header("🏰 TERRITORY & SOCIAL")]
        [SerializeField] private SocialTerritorySettings socialSettings = new SocialTerritorySettings();

        [Header("🌱 ECOSYSTEM & ENVIRONMENT")]
        [SerializeField] private EcosystemSettings ecosystemSettings = new EcosystemSettings();

        [Header("⚡ PERFORMANCE OPTIMIZATION")]
        [SerializeField] private PerformanceSettings performanceSettings = new PerformanceSettings();

        [Header("🎮 PLAYER INTERACTION")]
        [SerializeField] private PlayerInteractionSettings playerSettings = new PlayerInteractionSettings();

        // Public accessors with validation
        public WorldSettings World => worldSettings;
        public GeneticEvolutionSettings Genetics => geneticSettings;
        public BehaviorSettings Behavior => behaviorSettings;
        public BreedingSettings Breeding => breedingSettings;
        public SocialTerritorySettings Social => socialSettings;
        public EcosystemSettings Ecosystem => ecosystemSettings;
        public PerformanceSettings Performance => performanceSettings;
        public PlayerInteractionSettings Player => playerSettings;

        private void OnValidate()
        {
            // Auto-validate settings to prevent designer mistakes
            worldSettings.Validate();
            geneticSettings.Validate();
            performanceSettings.Validate();
        }

        /// <summary>
        /// Create default configuration with sensible values
        /// </summary>
        public static ChimeraUniverseConfiguration CreateDefault()
        {
            var config = CreateInstance<ChimeraUniverseConfiguration>();
            config.worldSettings = WorldSettings.CreateDefault();
            config.geneticSettings = GeneticEvolutionSettings.CreateDefault();
            config.behaviorSettings = BehaviorSettings.CreateDefault();
            config.breedingSettings = BreedingSettings.CreateDefault();
            config.socialSettings = SocialTerritorySettings.CreateDefault();
            config.ecosystemSettings = EcosystemSettings.CreateDefault();
            config.performanceSettings = PerformanceSettings.CreateDefault();
            config.playerSettings = PlayerInteractionSettings.CreateDefault();
            return config;
        }
    }

    [System.Serializable]
    public class WorldSettings
    {
        [Header("🗺️ World Scale")]
        public float worldRadius = 500f;
        public int maxCreatures = 5000;
        public int targetCreatureDensity = 10; // creatures per 100 square units
        public float simulationSpeed = 1f;

        [Header("⏰ Time & Seasons")]
        public float dayLength = 300f; // seconds
        public float seasonLength = 30f; // days
        public bool enableSeasons = true;
        public AnimationCurve seasonalBreedingModifier = AnimationCurve.Constant(0, 1, 1);

        [Header("🌡️ Climate")]
        public float baseTemperature = 20f;
        public float temperatureVariation = 10f;
        public float climateChangeRate = 0.001f;

        public void Validate()
        {
            worldRadius = Mathf.Max(50f, worldRadius);
            maxCreatures = Mathf.Max(10, maxCreatures);
            simulationSpeed = Mathf.Clamp(simulationSpeed, 0.1f, 10f);
            dayLength = Mathf.Max(10f, dayLength);
        }

        public static WorldSettings CreateDefault()
        {
            var settings = new WorldSettings();
            settings.seasonalBreedingModifier = new AnimationCurve(
                new Keyframe(0f, 2f),    // Spring - high breeding
                new Keyframe(0.25f, 1.5f), // Summer - medium
                new Keyframe(0.5f, 0.5f),  // Fall - low
                new Keyframe(0.75f, 0.3f), // Winter - very low
                new Keyframe(1f, 2f)       // Back to spring
            );
            return settings;
        }
    }

    [System.Serializable]
    public class GeneticEvolutionSettings
    {
        [Header("🔬 Mutation & Evolution")]
        [Range(0.001f, 0.1f)] public float baseMutationRate = 0.02f;
        [Range(0f, 1f)] public float beneficialMutationChance = 0.3f;
        [Range(0f, 1f)] public float environmentalPressureStrength = 0.5f;
        public bool enableNaturalSelection = true;
        public bool enableGeneticDrift = true;

        [Header("🧬 Inheritance")]
        [Range(0f, 0.5f)] public float traitVariationRange = 0.1f;
        public AnimationCurve dominanceExpression = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float epigeneticInfluence = 0.2f;

        [Header("🏆 Fitness Calculation")]
        [Range(0f, 1f)] public float survivalWeight = 0.3f;
        [Range(0f, 1f)] public float reproductionWeight = 0.4f;
        [Range(0f, 1f)] public float resourceEfficiencyWeight = 0.2f;
        [Range(0f, 1f)] public float adaptabilityWeight = 0.1f;

        [Header("🎯 Selection Pressure")]
        public EnvironmentalPressure[] environmentalPressures = new EnvironmentalPressure[]
        {
            new EnvironmentalPressure { biome = BiomeType.Desert, favoredTrait = "HeatTolerance", intensity = 0.8f },
            new EnvironmentalPressure { biome = BiomeType.Mountain, favoredTrait = "ColdTolerance", intensity = 0.7f },
            new EnvironmentalPressure { biome = BiomeType.Ocean, favoredTrait = "WaterAffinity", intensity = 0.9f }
        };

        public void Validate()
        {
            baseMutationRate = Mathf.Clamp(baseMutationRate, 0.001f, 0.1f);
            // Ensure fitness weights sum to roughly 1.0
            float totalWeight = survivalWeight + reproductionWeight + resourceEfficiencyWeight + adaptabilityWeight;
            if (Mathf.Abs(totalWeight - 1f) > 0.1f)
            {
                UnityEngine.Debug.LogWarning("Fitness weights should sum to 1.0 for balanced selection");
            }
        }

        public static GeneticEvolutionSettings CreateDefault()
        {
            var settings = new GeneticEvolutionSettings();
            settings.dominanceExpression = AnimationCurve.EaseInOut(0, 0, 1, 1);
            return settings;
        }
    }

    [System.Serializable]
    public class BehaviorSettings
    {
        [Header("🧠 Decision Making")]
        public float decisionUpdateInterval = 2f;
        [Range(0f, 1f)] public float personalityStability = 0.8f; // How consistent personality is
        public float stressInfluenceOnDecisions = 0.3f;
        public AnimationCurve confidenceVsRisk = AnimationCurve.EaseInOut(0, 1, 1, 0);

        [Header("🎯 Behavior Weights")]
        public BehaviorWeightSet defaultWeights = new BehaviorWeightSet();
        public BehaviorWeightSet juvenileModifiers = new BehaviorWeightSet();
        public BehaviorWeightSet elderModifiers = new BehaviorWeightSet();

        [Header("😊 Emotional System")]
        public float emotionalDecayRate = 0.01f;
        public float stressAccumulationRate = 0.05f;
        public AnimationCurve satisfactionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float emotionalContagionRadius = 10f;

        [Header("📚 Learning & Memory")]
        public bool enableLearning = true;
        public float memoryCapacity = 50f; // how many experiences they remember
        public float learningRate = 0.1f;
        public float memoryDecayRate = 0.001f;

        [Header("🎲 Randomness")]
        [Range(0f, 0.5f)] public float behaviorRandomness = 0.1f; // prevents predictability
        public float curiosityRandomness = 0.2f;

        public static BehaviorSettings CreateDefault()
        {
            var settings = new BehaviorSettings();
            settings.confidenceVsRisk = AnimationCurve.EaseInOut(0, 1, 1, 0);
            settings.satisfactionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

            // Set up default behavior weights
            settings.defaultWeights = new BehaviorWeightSet
            {
                idle = 0.2f,
                foraging = 0.3f,
                exploring = 0.15f,
                social = 0.15f,
                territorial = 0.1f,
                breeding = 0.05f,
                migrating = 0.03f,
                parenting = 0.02f
            };

            settings.juvenileModifiers = new BehaviorWeightSet
            {
                idle = 0.8f,
                foraging = 1.2f,
                exploring = 1.8f,
                social = 1.5f,
                territorial = 0.3f,
                breeding = 0f,
                migrating = 0.5f,
                parenting = 0f
            };

            settings.elderModifiers = new BehaviorWeightSet
            {
                idle = 1.5f,
                foraging = 1.1f,
                exploring = 0.6f,
                social = 0.9f,
                territorial = 1.3f,
                breeding = 0.4f,
                migrating = 0.7f,
                parenting = 1.2f
            };

            return settings;
        }
    }

    [System.Serializable]
    public class BreedingSettings
    {
        [Header("💕 Mating System")]
        public float breedingSeasonLength = 0.3f; // fraction of year
        public float courtshipDuration = 60f; // seconds
        public float gestationTime = 180f; // seconds
        public float breedingCooldown = 600f; // seconds between breeding attempts

        [Header("👶 Offspring")]
        public Vector2Int offspringRange = new Vector2Int(1, 4);
        public float multipleOffspringChance = 0.3f;
        public AnimationCurve offspringByAge = AnimationCurve.Linear(0.2f, 0.5f, 0.8f, 3f);
        public float juvenileMaturationTime = 120f;

        [Header("🎯 Mate Selection")]
        public float maxBreedingDistance = 25f;
        [Range(0f, 1f)] public float geneticDiversityPreference = 0.6f;
        [Range(0f, 1f)] public float fitnessPreference = 0.7f;
        [Range(0f, 1f)] public float territoryRequirement = 0.8f; // % territory needed to breed
        public float selectivenessIncrease = 0.1f; // gets pickier with age

        [Header("🧬 Genetic Compatibility")]
        public float hybridViabilityThreshold = 0.3f;
        public float inbreedingDepressionRate = 0.2f;
        public int maxInbreedingGenerations = 3;
        public float outbreedingBonus = 0.2f;

        [Header("👪 Parental Care")]
        public float parentalCareRadius = 15f;
        public float parentalCareDuration = 300f;
        public float protectiveAggressionBonus = 2f;
        public bool enableCooperativeBreeding = true;

        public static BreedingSettings CreateDefault()
        {
            var settings = new BreedingSettings();
            settings.offspringByAge = new AnimationCurve(
                new Keyframe(0.2f, 0.5f),  // Young adults - few offspring
                new Keyframe(0.5f, 2f),    // Prime adults - most offspring
                new Keyframe(0.8f, 3f),    // Late prime - maximum offspring
                new Keyframe(1f, 0.5f)     // Elders - few offspring
            );
            return settings;
        }
    }

    [System.Serializable]
    public class SocialTerritorySettings
    {
        [Header("🏠 Territory System")]
        public float baseTerritoryRadius = 12f;
        public AnimationCurve territorySizeByAggression = AnimationCurve.Linear(0.1f, 5f, 1f, 25f);
        public float territoryEstablishmentTime = 120f;
        public float territoryMaintenanceEnergy = 0.01f; // energy cost per second

        [Header("⚔️ Territorial Conflicts")]
        public float conflictDetectionRadius = 2f; // how close before conflict
        public float conflictEscalationTime = 30f;
        public float retreatDistance = 20f;
        public AnimationCurve fightVsFlight = AnimationCurve.EaseInOut(0, 0, 1, 1); // based on size difference

        [Header("👥 Social Groups")]
        public int maxPackSize = 12;
        public float packFormationRadius = 20f;
        public float socialBondDecayRate = 0.002f;
        public float leadershipChallengeCooldown = 300f;
        public bool enablePackHunting = true;
        public bool enablePackBreeding = true;

        [Header("🤝 Relationship Dynamics")]
        public float relationshipFormationRate = 0.1f;
        public float maxRelationshipStrength = 1f;
        public float friendshipThreshold = 0.6f;
        public float rivalryThreshold = -0.4f;
        public int maxRememberedRelationships = 20;

        [Header("📞 Communication")]
        public float communicationRadius = 30f;
        public bool enableScent = true;
        public float scentDecayTime = 300f;
        public bool enableVocalizations = true;
        public float vocalizationRange = 40f;

        public static SocialTerritorySettings CreateDefault()
        {
            var settings = new SocialTerritorySettings();
            settings.territorySizeByAggression = AnimationCurve.Linear(0.1f, 5f, 1f, 25f);
            settings.fightVsFlight = AnimationCurve.EaseInOut(0, 0, 1, 1);
            return settings;
        }
    }

    [System.Serializable]
    public class EcosystemSettings
    {
        [Header("🌍 Biome Configuration")]
        public BiomeData[] biomes = new BiomeData[]
        {
            new BiomeData { type = BiomeType.Grassland, resourceAbundance = 0.8f, carryingCapacity = 20, debugColor = Color.green },
            new BiomeData { type = BiomeType.Forest, resourceAbundance = 0.9f, carryingCapacity = 25, debugColor = Color.green },
            new BiomeData { type = BiomeType.Desert, resourceAbundance = 0.3f, carryingCapacity = 8, debugColor = Color.yellow },
            new BiomeData { type = BiomeType.Mountain, resourceAbundance = 0.5f, carryingCapacity = 12, debugColor = Color.gray },
            new BiomeData { type = BiomeType.Ocean, resourceAbundance = 0.7f, carryingCapacity = 30, debugColor = Color.blue }
        };

        [Header("🌿 Resource Management")]
        public float baseResourceRegeneration = 0.1f; // per day
        public float resourceDepletionRate = 0.05f;
        public float resourceVariability = 0.3f; // seasonal variation
        public int maxResourceNodesPerBiome = 50;

        [Header("🦋 Ecosystem Dynamics")]
        public bool enablePredatorPrey = true;
        public bool enableCompetition = true;
        public bool enableSymbiosis = false; // future feature
        public float ecosystemStabilityTarget = 0.7f;
        public float populationCrashThreshold = 0.1f;
        public float populationBoomThreshold = 2f;

        [Header("🌡️ Environmental Events")]
        public bool enableRandomEvents = true;
        public float naturalDisasterChance = 0.01f; // per day
        public float seasonalMigrationTrigger = 0.4f; // resource depletion level
        public bool enableClimateChange = false;

        public static EcosystemSettings CreateDefault()
        {
            return new EcosystemSettings();
        }
    }

    [System.Serializable]
    public class PerformanceSettings
    {
        [Header("⚡ Job Batching")]
        public int maxBehaviorUpdatesPerFrame = 1000;
        public int maxGeneticCalculationsPerFrame = 100;
        public int maxBreedingChecksPerFrame = 200;
        public int maxSocialUpdatesPerFrame = 500;

        [Header("📍 Spatial Optimization")]
        public float spatialHashCellSize = 20f;
        public int maxEntitiesPerSpatialCell = 30;
        public bool enableSpatialCulling = true;
        public float maxInteractionDistance = 50f;

        [Header("🎯 Level of Detail")]
        public float highDetailDistance = 30f;
        public float mediumDetailDistance = 100f;
        public float lowDetailDistance = 300f;
        public bool enableBehaviorLOD = true;
        public bool enableGeneticLOD = true;

        [Header("💾 Memory Management")]
        public int initialEntityCapacity = 5000;
        public int entityCapacityGrowthStep = 1000;
        public bool enableMemoryPooling = true;
        public float unusedMemoryCleanupInterval = 60f;

        public void Validate()
        {
            maxBehaviorUpdatesPerFrame = Mathf.Max(100, maxBehaviorUpdatesPerFrame);
            spatialHashCellSize = Mathf.Max(5f, spatialHashCellSize);
            initialEntityCapacity = Mathf.Max(100, initialEntityCapacity);
        }

        public static PerformanceSettings CreateDefault()
        {
            return new PerformanceSettings();
        }
    }

    [System.Serializable]
    public class PlayerInteractionSettings
    {
        [Header("🎮 Interaction Methods")]
        public bool enableCreatureSelection = true;
        public bool enableCreatureInspection = true;
        public bool enableBreedingPrograms = true;
        public bool enableTerritoryManagement = false; // future feature

        [Header("🔬 Research & Discovery")]
        public bool enableGeneticResearch = true;
        public bool enableBehaviorStudy = true;
        public bool enableEcosystemMonitoring = true;
        public float discoveryRewardMultiplier = 1f;

        [Header("🎯 Conservation Actions")]
        public bool enableSpeciesProtection = true;
        public bool enableHabitatRestoration = true;
        public bool enableAntiPoaching = false; // future feature

        [Header("⚖️ Game Balance")]
        public float playerInfluenceRadius = 100f;
        public float interventionCost = 1f;
        public bool limitPlayerActions = true;
        public int maxSimultaneousActions = 5;

        public static PlayerInteractionSettings CreateDefault()
        {
            return new PlayerInteractionSettings();
        }
    }

    // Supporting data structures
    [System.Serializable]
    public struct BehaviorWeightSet
    {
        [Range(0f, 2f)] public float idle;
        [Range(0f, 2f)] public float foraging;
        [Range(0f, 2f)] public float exploring;
        [Range(0f, 2f)] public float social;
        [Range(0f, 2f)] public float territorial;
        [Range(0f, 2f)] public float breeding;
        [Range(0f, 2f)] public float migrating;
        [Range(0f, 2f)] public float parenting;
    }

    [System.Serializable]
    public struct EnvironmentalPressure
    {
        public BiomeType biome;
        public string favoredTrait;
        [Range(0f, 2f)] public float intensity;
        public string description;
    }

    [System.Serializable]
    public struct BiomeData
    {
        public BiomeType type;
        [Range(0f, 2f)] public float resourceAbundance;
        public int carryingCapacity;
        public Color debugColor;
        public string description;
    }
}