using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Reflection;
using System;

namespace Laboratory.Shared.Types
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
            /// Extract all configuration data from ScriptableObject using reflection (call once in OnCreate)
            /// </summary>
            public static ChimeraConfigData Extract(ScriptableObject config)
            {
                if (config == null)
                {
                    Debug.LogError("[BurstCompatibleConfigs] Configuration is null, using defaults");
                    return CreateDefault();
                }

                try
                {
                    return new ChimeraConfigData
                    {
                        world = WorldConfigData.ExtractFromObject(config),
                        genetics = GeneticsConfigData.ExtractFromObject(config),
                        behavior = BehaviorConfigData.ExtractFromObject(config),
                        breeding = BreedingConfigData.ExtractFromObject(config),
                        social = SocialConfigData.ExtractFromObject(config),
                        ecosystem = EcosystemConfigData.ExtractFromObject(config),
                        performance = PerformanceConfigData.ExtractFromObject(config)
                    };
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BurstCompatibleConfigs] Failed to extract config data: {ex.Message}");
                    return CreateDefault();
                }
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

            public static WorldConfigData ExtractFromObject(ScriptableObject configObject)
            {
                var data = CreateDefault();

                if (configObject == null)
                    return data;

                try
                {
                    var type = configObject.GetType();

                    // Extract basic world settings using reflection
                    ExtractFloatField(configObject, type, "worldRadius", ref data.worldRadius);
                    ExtractFloatField(configObject, type, "WorldRadius", ref data.worldRadius);
                    ExtractIntField(configObject, type, "maxCreatures", ref data.maxCreatures);
                    ExtractIntField(configObject, type, "MaxCreatures", ref data.maxCreatures);
                    ExtractIntField(configObject, type, "targetCreatureDensity", ref data.targetCreatureDensity);
                    ExtractFloatField(configObject, type, "simulationSpeed", ref data.simulationSpeed);
                    ExtractFloatField(configObject, type, "SimulationSpeed", ref data.simulationSpeed);

                    // Time & seasons
                    ExtractFloatField(configObject, type, "dayLength", ref data.dayLength);
                    ExtractFloatField(configObject, type, "DayLength", ref data.dayLength);
                    ExtractFloatField(configObject, type, "seasonLength", ref data.seasonLength);
                    ExtractFloatField(configObject, type, "SeasonLength", ref data.seasonLength);
                    ExtractBoolField(configObject, type, "enableSeasons", ref data.enableSeasons);

                    // Climate
                    ExtractFloatField(configObject, type, "baseTemperature", ref data.baseTemperature);
                    ExtractFloatField(configObject, type, "temperatureVariation", ref data.temperatureVariation);
                    ExtractFloatField(configObject, type, "climateChangeRate", ref data.climateChangeRate);

                    // Try to extract seasonal breeding curve
                    ExtractAnimationCurve(configObject, type, "seasonalBreedingModifier", ref data.seasonalBreedingSamples);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BurstCompatibleConfigs] Could not extract all world config fields: {ex.Message}");
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

            public static GeneticsConfigData ExtractFromObject(ScriptableObject configObject)
            {
                var data = CreateDefault();

                if (configObject == null)
                    return data;

                try
                {
                    var type = configObject.GetType();

                    // Extract genetic evolution settings
                    ExtractFloatField(configObject, type, "baseMutationRate", ref data.baseMutationRate);
                    ExtractFloatField(configObject, type, "beneficialMutationChance", ref data.beneficialMutationChance);
                    ExtractFloatField(configObject, type, "environmentalPressureStrength", ref data.environmentalPressureStrength);
                    ExtractBoolField(configObject, type, "enableNaturalSelection", ref data.enableNaturalSelection);
                    ExtractBoolField(configObject, type, "enableGeneticDrift", ref data.enableGeneticDrift);

                    // Inheritance
                    ExtractFloatField(configObject, type, "traitVariationRange", ref data.traitVariationRange);
                    ExtractFloatField(configObject, type, "epigeneticInfluence", ref data.epigeneticInfluence);

                    // Fitness weights
                    ExtractFloatField(configObject, type, "survivalWeight", ref data.survivalWeight);
                    ExtractFloatField(configObject, type, "reproductionWeight", ref data.reproductionWeight);
                    ExtractFloatField(configObject, type, "resourceEfficiencyWeight", ref data.resourceEfficiencyWeight);
                    ExtractFloatField(configObject, type, "adaptabilityWeight", ref data.adaptabilityWeight);

                    // Try to extract dominance expression curve
                    ExtractAnimationCurve(configObject, type, "dominanceExpression", ref data.dominanceExpressionSamples);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BurstCompatibleConfigs] Could not extract all genetics config fields: {ex.Message}");
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
        /// Behavior weight set - Burst compatible
        /// </summary>
        public struct BehaviorWeightSet
        {
            public float idle;
            public float foraging;
            public float exploring;
            public float social;
            public float territorial;
            public float breeding;
            public float migrating;
            public float parenting;
        }

        /// <summary>
        /// Behavior settings - Burst compatible
        /// </summary>
        public struct BehaviorConfigData
        {
            // Decision Making
            public float decisionUpdateInterval;
            public float stressInfluenceOnDecisions;
            public float behaviorRandomness;

            // Behavior Weights
            public BehaviorWeightSet defaultWeights;
            public BehaviorWeightSet juvenileModifiers;
            public BehaviorWeightSet elderModifiers;

            public static BehaviorConfigData ExtractFromObject(ScriptableObject configObject)
            {
                var data = CreateDefault();

                if (configObject == null)
                    return data;

                try
                {
                    var type = configObject.GetType();

                    // Extract behavior settings
                    ExtractFloatField(configObject, type, "decisionUpdateInterval", ref data.decisionUpdateInterval);
                    ExtractFloatField(configObject, type, "stressInfluenceOnDecisions", ref data.stressInfluenceOnDecisions);
                    ExtractFloatField(configObject, type, "behaviorRandomness", ref data.behaviorRandomness);

                    // Extract weight sets if available
                    ExtractBehaviorWeightSet(configObject, type, "defaultWeights", ref data.defaultWeights);
                    ExtractBehaviorWeightSet(configObject, type, "juvenileModifiers", ref data.juvenileModifiers);
                    ExtractBehaviorWeightSet(configObject, type, "elderModifiers", ref data.elderModifiers);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BurstCompatibleConfigs] Could not extract all behavior config fields: {ex.Message}");
                }

                return data;
            }

            public static BehaviorConfigData CreateDefault()
            {
                return new BehaviorConfigData
                {
                    decisionUpdateInterval = 2f,
                    stressInfluenceOnDecisions = 0.3f,
                    behaviorRandomness = 0.1f,
                    defaultWeights = new BehaviorWeightSet
                    {
                        idle = 0.2f,
                        foraging = 0.3f,
                        exploring = 0.15f,
                        social = 0.15f,
                        territorial = 0.1f,
                        breeding = 0.05f,
                        migrating = 0.03f,
                        parenting = 0.02f
                    },
                    juvenileModifiers = new BehaviorWeightSet
                    {
                        idle = 0.8f,
                        foraging = 1.2f,
                        exploring = 1.8f,
                        social = 1.5f,
                        territorial = 0.3f,
                        breeding = 0f,
                        migrating = 0.5f,
                        parenting = 0f
                    },
                    elderModifiers = new BehaviorWeightSet
                    {
                        idle = 1.5f,
                        foraging = 1.1f,
                        exploring = 0.6f,
                        social = 0.9f,
                        territorial = 1.3f,
                        breeding = 0.4f,
                        migrating = 0.7f,
                        parenting = 1.2f
                    }
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

            public static BreedingConfigData ExtractFromObject(ScriptableObject configObject)
            {
                var data = CreateDefault();

                if (configObject == null)
                    return data;

                try
                {
                    var type = configObject.GetType();

                    // Extract breeding settings
                    ExtractFloatField(configObject, type, "minBreedingAge", ref data.minBreedingAge);
                    ExtractFloatField(configObject, type, "MinBreedingAge", ref data.minBreedingAge);
                    ExtractFloatField(configObject, type, "breedingCooldown", ref data.breedingCooldown);
                    ExtractFloatField(configObject, type, "BreedingCooldown", ref data.breedingCooldown);
                    ExtractFloatField(configObject, type, "fertilityThreshold", ref data.fertilityThreshold);
                    ExtractFloatField(configObject, type, "FertilityThreshold", ref data.fertilityThreshold);
                    ExtractFloatField(configObject, type, "maxBreedingDistance", ref data.maxBreedingDistance);
                    ExtractFloatField(configObject, type, "MaxBreedingDistance", ref data.maxBreedingDistance);
                    ExtractFloatField(configObject, type, "pregnancyDuration", ref data.pregnancyDuration);
                    ExtractFloatField(configObject, type, "PregnancyDuration", ref data.pregnancyDuration);
                    ExtractFloatField(configObject, type, "parentalCareDuration", ref data.parentalCareDuration);
                    ExtractFloatField(configObject, type, "ParentalCareDuration", ref data.parentalCareDuration);
                    ExtractIntField(configObject, type, "maxOffspringCount", ref data.maxOffspringCount);
                    ExtractIntField(configObject, type, "MaxOffspringCount", ref data.maxOffspringCount);
                    ExtractFloatField(configObject, type, "geneticCompatibilityWeight", ref data.geneticCompatibilityWeight);
                    ExtractFloatField(configObject, type, "GeneticCompatibilityWeight", ref data.geneticCompatibilityWeight);
                    ExtractFloatField(configObject, type, "territoryRequirement", ref data.territoryRequirement);
                    ExtractFloatField(configObject, type, "TerritoryRequirement", ref data.territoryRequirement);
                    ExtractBoolField(configObject, type, "requireMutualAttraction", ref data.requireMutualAttraction);
                    ExtractBoolField(configObject, type, "RequireMutualAttraction", ref data.requireMutualAttraction);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BurstCompatibleConfigs] Could not extract all breeding config fields: {ex.Message}");
                }

                return data;
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

            public static SocialConfigData ExtractFromObject(ScriptableObject configObject)
            {
                var data = CreateDefault();

                if (configObject == null)
                    return data;

                try
                {
                    var type = configObject.GetType();

                    // Extract social/territory settings
                    ExtractFloatField(configObject, type, "territorySize", ref data.territorySize);
                    ExtractFloatField(configObject, type, "TerritorySize", ref data.territorySize);
                    ExtractFloatField(configObject, type, "territoryOverlapTolerance", ref data.territoryOverlapTolerance);
                    ExtractFloatField(configObject, type, "TerritoryOverlapTolerance", ref data.territoryOverlapTolerance);
                    ExtractFloatField(configObject, type, "packRadius", ref data.packRadius);
                    ExtractFloatField(configObject, type, "PackRadius", ref data.packRadius);
                    ExtractIntField(configObject, type, "maxPackSize", ref data.maxPackSize);
                    ExtractIntField(configObject, type, "MaxPackSize", ref data.maxPackSize);
                    ExtractFloatField(configObject, type, "socialInteractionRadius", ref data.socialInteractionRadius);
                    ExtractFloatField(configObject, type, "SocialInteractionRadius", ref data.socialInteractionRadius);
                    ExtractFloatField(configObject, type, "bondingRate", ref data.bondingRate);
                    ExtractFloatField(configObject, type, "BondingRate", ref data.bondingRate);
                    ExtractFloatField(configObject, type, "conflictThreshold", ref data.conflictThreshold);
                    ExtractFloatField(configObject, type, "ConflictThreshold", ref data.conflictThreshold);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BurstCompatibleConfigs] Could not extract all social config fields: {ex.Message}");
                }

                return data;
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

            public static EcosystemConfigData ExtractFromObject(ScriptableObject configObject)
            {
                var data = CreateDefault();

                if (configObject == null)
                    return data;

                try
                {
                    var type = configObject.GetType();

                    // Extract ecosystem settings
                    ExtractFloatField(configObject, type, "resourceRegenerationRate", ref data.resourceRegenerationRate);
                    ExtractFloatField(configObject, type, "ResourceRegenerationRate", ref data.resourceRegenerationRate);
                    ExtractFloatField(configObject, type, "predatorPreyRatio", ref data.predatorPreyRatio);
                    ExtractFloatField(configObject, type, "PredatorPreyRatio", ref data.predatorPreyRatio);
                    ExtractFloatField(configObject, type, "extinctionThreshold", ref data.extinctionThreshold);
                    ExtractFloatField(configObject, type, "ExtinctionThreshold", ref data.extinctionThreshold);
                    ExtractBoolField(configObject, type, "enablePopulationControl", ref data.enablePopulationControl);
                    ExtractBoolField(configObject, type, "EnablePopulationControl", ref data.enablePopulationControl);
                    ExtractFloatField(configObject, type, "migrationThreshold", ref data.migrationThreshold);
                    ExtractFloatField(configObject, type, "MigrationThreshold", ref data.migrationThreshold);
                    ExtractFloatField(configObject, type, "habitatQualityWeight", ref data.habitatQualityWeight);
                    ExtractFloatField(configObject, type, "HabitatQualityWeight", ref data.habitatQualityWeight);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BurstCompatibleConfigs] Could not extract all ecosystem config fields: {ex.Message}");
                }

                return data;
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

            // Flow field specific settings
            public int maxFlowFields;
            public int maxFlowFieldRequestsPerFrame;

            public static PerformanceConfigData ExtractFromObject(ScriptableObject configObject)
            {
                var data = CreateDefault();

                if (configObject == null)
                    return data;

                try
                {
                    var type = configObject.GetType();

                    // Extract performance settings
                    ExtractFloatField(configObject, type, "spatialHashCellSize", ref data.spatialHashCellSize);
                    ExtractFloatField(configObject, type, "spatialCellSize", ref data.spatialHashCellSize);
                    ExtractIntField(configObject, type, "maxEntitiesPerBatch", ref data.maxEntitiesPerBatch);
                    ExtractIntField(configObject, type, "jobBatchSize", ref data.jobBatchSize);
                    ExtractBoolField(configObject, type, "enableLOD", ref data.enableLOD);
                    ExtractFloatField(configObject, type, "lodDistance1", ref data.lodDistance1);
                    ExtractFloatField(configObject, type, "lodDistance2", ref data.lodDistance2);
                    ExtractIntField(configObject, type, "maxFlowFields", ref data.maxFlowFields);
                    ExtractIntField(configObject, type, "maxFlowFieldRequestsPerFrame", ref data.maxFlowFieldRequestsPerFrame);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BurstCompatibleConfigs] Could not extract all performance config fields: {ex.Message}");
                }

                return data;
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
                    lodDistance2 = 100f,
                    maxFlowFields = 100,
                    maxFlowFieldRequestsPerFrame = 5
                };
            }
        }

        // Reflection helper methods for extracting configuration values
        private static void ExtractFloatField(ScriptableObject obj, Type type, string fieldName, ref float target)
        {
            try
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(float))
                {
                    target = (float)field.GetValue(obj);
                    return;
                }

                var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(float) && property.CanRead)
                {
                    target = (float)property.GetValue(obj);
                }
            }
            catch
            {
                // Silently continue with default value
            }
        }

        private static void ExtractIntField(ScriptableObject obj, Type type, string fieldName, ref int target)
        {
            try
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(int))
                {
                    target = (int)field.GetValue(obj);
                    return;
                }

                var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(int) && property.CanRead)
                {
                    target = (int)property.GetValue(obj);
                }
            }
            catch
            {
                // Silently continue with default value
            }
        }

        private static void ExtractBoolField(ScriptableObject obj, Type type, string fieldName, ref bool target)
        {
            try
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(bool))
                {
                    target = (bool)field.GetValue(obj);
                    return;
                }

                var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(bool) && property.CanRead)
                {
                    target = (bool)property.GetValue(obj);
                }
            }
            catch
            {
                // Silently continue with default value
            }
        }

        private static void ExtractAnimationCurve(ScriptableObject obj, Type type, string fieldName, ref NativeArray<float> targetArray)
        {
            try
            {
                AnimationCurve curve = null;

                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(AnimationCurve))
                {
                    curve = (AnimationCurve)field.GetValue(obj);
                }
                else
                {
                    var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (property != null && property.PropertyType == typeof(AnimationCurve) && property.CanRead)
                    {
                        curve = (AnimationCurve)property.GetValue(obj);
                    }
                }

                if (curve != null && targetArray.IsCreated)
                {
                    int sampleCount = targetArray.Length;
                    for (int i = 0; i < sampleCount; i++)
                    {
                        float t = i / (float)(sampleCount - 1);
                        targetArray[i] = curve.Evaluate(t);
                    }
                }
            }
            catch
            {
                // Silently continue with default curve values
            }
        }

        private static void ExtractBehaviorWeightSet(ScriptableObject obj, Type type, string fieldName, ref BehaviorWeightSet target)
        {
            try
            {
                object weightSetObj = null;

                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    weightSetObj = field.GetValue(obj);
                }
                else
                {
                    var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (property != null && property.CanRead)
                    {
                        weightSetObj = property.GetValue(obj);
                    }
                }

                if (weightSetObj != null)
                {
                    var weightType = weightSetObj.GetType();
                    ExtractFloatFromObject(weightSetObj, weightType, "idle", ref target.idle);
                    ExtractFloatFromObject(weightSetObj, weightType, "foraging", ref target.foraging);
                    ExtractFloatFromObject(weightSetObj, weightType, "exploring", ref target.exploring);
                    ExtractFloatFromObject(weightSetObj, weightType, "social", ref target.social);
                    ExtractFloatFromObject(weightSetObj, weightType, "territorial", ref target.territorial);
                    ExtractFloatFromObject(weightSetObj, weightType, "breeding", ref target.breeding);
                    ExtractFloatFromObject(weightSetObj, weightType, "migrating", ref target.migrating);
                    ExtractFloatFromObject(weightSetObj, weightType, "parenting", ref target.parenting);
                }
            }
            catch
            {
                // Silently continue with default weight values
            }
        }

        private static void ExtractFloatFromObject(object obj, Type type, string fieldName, ref float target)
        {
            try
            {
                var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null && field.FieldType == typeof(float))
                {
                    target = (float)field.GetValue(obj);
                    return;
                }

                var property = type.GetProperty(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (property != null && property.PropertyType == typeof(float) && property.CanRead)
                {
                    target = (float)property.GetValue(obj);
                }
            }
            catch
            {
                // Silently continue with default value
            }
        }
    }
}
