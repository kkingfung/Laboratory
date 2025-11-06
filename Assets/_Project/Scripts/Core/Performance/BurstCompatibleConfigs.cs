using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Chimera.Configuration;
using Laboratory.Core.Enums;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Burst-compatible configuration data structs.
    /// These replace managed ScriptableObject references in ECS jobs.
    /// All data is extracted from ScriptableObjects once on the main thread,
    /// then passed to jobs as unmanaged structs for maximum performance.
    /// </summary>
    public static class BurstCompatibleConfigs
    {
        /// <summary>
        /// Master configuration data container - extracted from ChimeraUniverseConfiguration
        /// </summary>
        public struct ChimeraConfigData
        {
            public WorldConfigData world;
            public GeneticsConfigData genetics;
            public BehaviorConfigData behavior;
            public BreedingConfigData breeding;
            public SocialConfigData social;
            public EcosystemConfigData ecosystem;
            public PerformanceConfigData performance;

            /// <summary>
            /// Extract all configuration data from ScriptableObject (call once in OnCreate)
            /// </summary>
            public static ChimeraConfigData Extract(ChimeraUniverseConfiguration config)
            {
                if (config == null)
                {
                    Debug.LogError("[BurstCompatibleConfigs] Configuration is null, using defaults");
                    return CreateDefault();
                }

                return new ChimeraConfigData
                {
                    world = WorldConfigData.Extract(config.World),
                    genetics = GeneticsConfigData.Extract(config.Genetics),
                    behavior = BehaviorConfigData.Extract(config.Behavior),
                    breeding = BreedingConfigData.Extract(config.Breeding),
                    social = SocialConfigData.Extract(config.Social),
                    ecosystem = EcosystemConfigData.Extract(config.Ecosystem),
                    performance = PerformanceConfigData.Extract(config.Performance)
                };
            }

            public static ChimeraConfigData CreateDefault()
            {
                return new ChimeraConfigData
                {
                    world = WorldConfigData.CreateDefault(),
                    genetics = GeneticsConfigData.CreateDefault(),
                    behavior = BehaviorConfigData.CreateDefault(),
                    breeding = BreedingConfigData.CreateDefault(),
                    social = SocialConfigData.CreateDefault(),
                    ecosystem = EcosystemConfigData.CreateDefault(),
                    performance = PerformanceConfigData.CreateDefault()
                };
            }

            /// <summary>
            /// Dispose all NativeCollections (call in OnDestroy)
            /// </summary>
            public void Dispose()
            {
                world.Dispose();
                genetics.Dispose();
            }
        }

        /// <summary>
        /// World settings - Burst compatible
        /// </summary>
        public struct WorldConfigData
        {
            public float worldRadius;
            public int maxCreatures;
            public int targetCreatureDensity;
            public float simulationSpeed;

            // Time & Seasons
            public float dayLength;
            public float seasonLength;
            public bool enableSeasons;

            // Seasonal breeding curve baked into lookup table
            public NativeArray<float> seasonalBreedingSamples;
            public int seasonalSampleCount;

            // Climate
            public float baseTemperature;
            public float temperatureVariation;
            public float climateChangeRate;

            public static WorldConfigData Extract(WorldSettings settings)
            {
                var data = new WorldConfigData
                {
                    worldRadius = settings.worldRadius,
                    maxCreatures = settings.maxCreatures,
                    targetCreatureDensity = settings.targetCreatureDensity,
                    simulationSpeed = settings.simulationSpeed,
                    dayLength = settings.dayLength,
                    seasonLength = settings.seasonLength,
                    enableSeasons = settings.enableSeasons,
                    baseTemperature = settings.baseTemperature,
                    temperatureVariation = settings.temperatureVariation,
                    climateChangeRate = settings.climateChangeRate
                };

                // Bake AnimationCurve into lookup table
                data.seasonalSampleCount = 64;
                data.seasonalBreedingSamples = new NativeArray<float>(64, Allocator.Persistent);

                for (int i = 0; i < 64; i++)
                {
                    float t = i / 63f;
                    data.seasonalBreedingSamples[i] = settings.seasonalBreedingModifier.Evaluate(t);
                }

                return data;
            }

            public static WorldConfigData CreateDefault()
            {
                var data = new WorldConfigData
                {
                    worldRadius = 500f,
                    maxCreatures = 5000,
                    targetCreatureDensity = 10,
                    simulationSpeed = 1f,
                    dayLength = 300f,
                    seasonLength = 30f,
                    enableSeasons = true,
                    baseTemperature = 20f,
                    temperatureVariation = 10f,
                    climateChangeRate = 0.001f,
                    seasonalSampleCount = 64,
                    seasonalBreedingSamples = new NativeArray<float>(64, Allocator.Persistent)
                };

                // Default seasonal curve (spring high, winter low)
                for (int i = 0; i < 64; i++)
                {
                    float t = i / 63f;
                    data.seasonalBreedingSamples[i] = 1.25f + math.sin(t * 2f * math.PI) * 0.75f;
                }

                return data;
            }

            public float EvaluateSeasonalBreeding(float normalizedTime)
            {
                if (!enableSeasons) return 1f;

                float index = normalizedTime * (seasonalSampleCount - 1);
                int i0 = (int)math.floor(index);
                int i1 = math.min(i0 + 1, seasonalSampleCount - 1);
                float blend = index - i0;

                return math.lerp(seasonalBreedingSamples[i0], seasonalBreedingSamples[i1], blend);
            }

            public void Dispose()
            {
                if (seasonalBreedingSamples.IsCreated)
                {
                    seasonalBreedingSamples.Dispose();
                }
            }
        }

        /// <summary>
        /// Genetics settings - Burst compatible
        /// </summary>
        public struct GeneticsConfigData
        {
            // Mutation & Evolution
            public float baseMutationRate;
            public float beneficialMutationChance;
            public float environmentalPressureStrength;
            public bool enableNaturalSelection;
            public bool enableGeneticDrift;

            // Inheritance
            public float traitVariationRange;
            public float epigeneticInfluence;

            // Dominance curve baked into lookup table
            public NativeArray<float> dominanceExpressionSamples;
            public int dominanceSampleCount;

            // Fitness Calculation
            public float survivalWeight;
            public float reproductionWeight;
            public float resourceEfficiencyWeight;
            public float adaptabilityWeight;

            public static GeneticsConfigData Extract(GeneticEvolutionSettings settings)
            {
                var data = new GeneticsConfigData
                {
                    baseMutationRate = settings.baseMutationRate,
                    beneficialMutationChance = settings.beneficialMutationChance,
                    environmentalPressureStrength = settings.environmentalPressureStrength,
                    enableNaturalSelection = settings.enableNaturalSelection,
                    enableGeneticDrift = settings.enableGeneticDrift,
                    traitVariationRange = settings.traitVariationRange,
                    epigeneticInfluence = settings.epigeneticInfluence,
                    survivalWeight = settings.survivalWeight,
                    reproductionWeight = settings.reproductionWeight,
                    resourceEfficiencyWeight = settings.resourceEfficiencyWeight,
                    adaptabilityWeight = settings.adaptabilityWeight
                };

                // Bake dominance expression curve
                data.dominanceSampleCount = 32;
                data.dominanceExpressionSamples = new NativeArray<float>(32, Allocator.Persistent);

                for (int i = 0; i < 32; i++)
                {
                    float t = i / 31f;
                    data.dominanceExpressionSamples[i] = settings.dominanceExpression.Evaluate(t);
                }

                return data;
            }

            public static GeneticsConfigData CreateDefault()
            {
                var data = new GeneticsConfigData
                {
                    baseMutationRate = 0.02f,
                    beneficialMutationChance = 0.3f,
                    environmentalPressureStrength = 0.5f,
                    enableNaturalSelection = true,
                    enableGeneticDrift = true,
                    traitVariationRange = 0.1f,
                    epigeneticInfluence = 0.2f,
                    survivalWeight = 0.3f,
                    reproductionWeight = 0.4f,
                    resourceEfficiencyWeight = 0.2f,
                    adaptabilityWeight = 0.1f,
                    dominanceSampleCount = 32,
                    dominanceExpressionSamples = new NativeArray<float>(32, Allocator.Persistent)
                };

                // Default dominance curve (sigmoid)
                for (int i = 0; i < 32; i++)
                {
                    float t = i / 31f;
                    data.dominanceExpressionSamples[i] = 1f / (1f + math.exp(-10f * (t - 0.5f)));
                }

                return data;
            }

            public float EvaluateDominance(float t)
            {
                float index = t * (dominanceSampleCount - 1);
                int i0 = (int)math.floor(index);
                int i1 = math.min(i0 + 1, dominanceSampleCount - 1);
                float blend = index - i0;

                return math.lerp(dominanceExpressionSamples[i0], dominanceExpressionSamples[i1], blend);
            }

            public void Dispose()
            {
                if (dominanceExpressionSamples.IsCreated)
                {
                    dominanceExpressionSamples.Dispose();
                }
            }
        }

        /// <summary>
        /// Behavior settings - Burst compatible
        /// </summary>
        public struct BehaviorConfigData
        {
            // State Transition
            public float idleStateWeight;
            public float patrolStateWeight;
            public float feedingStateWeight;
            public float socialStateWeight;
            public float stateTransitionSpeed;
            public float stateRetentionBias;

            // Movement
            public float wanderRadius;
            public float maxWanderDistance;
            public float territoryCheckInterval;

            // Detection
            public float detectionRadius;
            public float memoryDuration;

            public static BehaviorConfigData Extract(BehaviorSettings settings)
            {
                return new BehaviorConfigData
                {
                    idleStateWeight = settings.IdleStateWeight,
                    patrolStateWeight = settings.PatrolStateWeight,
                    feedingStateWeight = settings.FeedingStateWeight,
                    socialStateWeight = settings.SocialStateWeight,
                    stateTransitionSpeed = settings.StateTransitionSpeed,
                    stateRetentionBias = settings.StateRetentionBias,
                    wanderRadius = settings.WanderRadius,
                    maxWanderDistance = settings.MaxWanderDistance,
                    territoryCheckInterval = settings.TerritoryCheckInterval,
                    detectionRadius = settings.DetectionRadius,
                    memoryDuration = settings.MemoryDuration
                };
            }

            public static BehaviorConfigData CreateDefault()
            {
                return new BehaviorConfigData
                {
                    idleStateWeight = 0.3f,
                    patrolStateWeight = 0.3f,
                    feedingStateWeight = 0.2f,
                    socialStateWeight = 0.2f,
                    stateTransitionSpeed = 0.1f,
                    stateRetentionBias = 0.8f,
                    wanderRadius = 20f,
                    maxWanderDistance = 50f,
                    territoryCheckInterval = 5f,
                    detectionRadius = 15f,
                    memoryDuration = 30f
                };
            }
        }

        /// <summary>
        /// Breeding settings - Burst compatible
        /// </summary>
        public struct BreedingConfigData
        {
            public float minBreedingAge;
            public float breedingCooldown;
            public float fertilityThreshold;
            public float maxBreedingDistance;
            public float pregnancyDuration;
            public float parentalCareDuration;
            public int maxOffspringCount;
            public float geneticCompatibilityWeight;
            public float territoryRequirement;
            public bool requireMutualAttraction;

            public static BreedingConfigData Extract(BreedingSettings settings)
            {
                return new BreedingConfigData
                {
                    minBreedingAge = settings.MinBreedingAge,
                    breedingCooldown = settings.BreedingCooldown,
                    fertilityThreshold = settings.FertilityThreshold,
                    maxBreedingDistance = settings.MaxBreedingDistance,
                    pregnancyDuration = settings.PregnancyDuration,
                    parentalCareDuration = settings.ParentalCareDuration,
                    maxOffspringCount = settings.MaxOffspringCount,
                    geneticCompatibilityWeight = settings.GeneticCompatibilityWeight,
                    territoryRequirement = settings.TerritoryRequirement,
                    requireMutualAttraction = settings.RequireMutualAttraction
                };
            }

            public static BreedingConfigData CreateDefault()
            {
                return new BreedingConfigData
                {
                    minBreedingAge = 10f,
                    breedingCooldown = 100f,
                    fertilityThreshold = 0.5f,
                    maxBreedingDistance = 10f,
                    pregnancyDuration = 50f,
                    parentalCareDuration = 30f,
                    maxOffspringCount = 3,
                    geneticCompatibilityWeight = 0.7f,
                    territoryRequirement = 50f,
                    requireMutualAttraction = true
                };
            }
        }

        /// <summary>
        /// Social/Territory settings - Burst compatible
        /// </summary>
        public struct SocialConfigData
        {
            public float territorySize;
            public float territoryOverlapTolerance;
            public float packRadius;
            public int maxPackSize;
            public float socialInteractionRadius;
            public float bondingRate;
            public float conflictThreshold;

            public static SocialConfigData Extract(SocialTerritorySettings settings)
            {
                return new SocialConfigData
                {
                    territorySize = settings.TerritorySize,
                    territoryOverlapTolerance = settings.TerritoryOverlapTolerance,
                    packRadius = settings.PackRadius,
                    maxPackSize = settings.MaxPackSize,
                    socialInteractionRadius = settings.SocialInteractionRadius,
                    bondingRate = settings.BondingRate,
                    conflictThreshold = settings.ConflictThreshold
                };
            }

            public static SocialConfigData CreateDefault()
            {
                return new SocialConfigData
                {
                    territorySize = 100f,
                    territoryOverlapTolerance = 0.2f,
                    packRadius = 20f,
                    maxPackSize = 10,
                    socialInteractionRadius = 15f,
                    bondingRate = 0.01f,
                    conflictThreshold = 0.5f
                };
            }
        }

        /// <summary>
        /// Ecosystem settings - Burst compatible
        /// </summary>
        public struct EcosystemConfigData
        {
            public float resourceRegenerationRate;
            public float predatorPreyRatio;
            public float extinctionThreshold;
            public bool enablePopulationControl;
            public float migrationThreshold;
            public float habitatQualityWeight;

            public static EcosystemConfigData Extract(EcosystemSettings settings)
            {
                return new EcosystemConfigData
                {
                    resourceRegenerationRate = settings.ResourceRegenerationRate,
                    predatorPreyRatio = settings.PredatorPreyRatio,
                    extinctionThreshold = settings.ExtinctionThreshold,
                    enablePopulationControl = settings.EnablePopulationControl,
                    migrationThreshold = settings.MigrationThreshold,
                    habitatQualityWeight = settings.HabitatQualityWeight
                };
            }

            public static EcosystemConfigData CreateDefault()
            {
                return new EcosystemConfigData
                {
                    resourceRegenerationRate = 0.1f,
                    predatorPreyRatio = 0.1f,
                    extinctionThreshold = 5f,
                    enablePopulationControl = true,
                    migrationThreshold = 0.3f,
                    habitatQualityWeight = 0.5f
                };
            }
        }

        /// <summary>
        /// Performance settings - Burst compatible
        /// </summary>
        public struct PerformanceConfigData
        {
            public float spatialHashCellSize;
            public int maxEntitiesPerBatch;
            public int jobBatchSize;
            public bool enableLOD;
            public float lodDistance1;
            public float lodDistance2;

            public static PerformanceConfigData Extract(PerformanceSettings settings)
            {
                return new PerformanceConfigData
                {
                    spatialHashCellSize = settings.spatialHashCellSize,
                    maxEntitiesPerBatch = settings.maxEntitiesPerBatch,
                    jobBatchSize = settings.jobBatchSize,
                    enableLOD = settings.enableLOD,
                    lodDistance1 = settings.lodDistance1,
                    lodDistance2 = settings.lodDistance2
                };
            }

            public static PerformanceConfigData CreateDefault()
            {
                return new PerformanceConfigData
                {
                    spatialHashCellSize = 25f,
                    maxEntitiesPerBatch = 1000,
                    jobBatchSize = 32,
                    enableLOD = true,
                    lodDistance1 = 30f,
                    lodDistance2 = 100f
                };
            }
        }
    }
}
