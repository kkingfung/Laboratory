using UnityEngine;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.ECS;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using ChimeraBiomeType = Laboratory.Shared.Types.BiomeType;
using CoreTraitType = Laboratory.Core.Enums.TraitType;

namespace Laboratory.Chimera.Integration
{
    /// <summary>
    /// CONFIGURATION INTEGRATOR - Merges unified config with specialized existing configs
    /// PURPOSE: Allow unified system to leverage detailed existing configurations
    /// FEATURES: Dynamic trait mapping, biome integration, species configuration merging
    /// </summary>
    [CreateAssetMenu(fileName = "ChimeraConfigIntegrator", menuName = "Project Chimera/Configuration Integrator")]
    public class ChimeraConfigurationIntegrator : ScriptableObject
    {
        [Header("🔗 CONFIGURATION SOURCES")]
        [SerializeField] private ChimeraUniverseConfiguration unifiedConfig;
        [SerializeField] private GeneticTraitLibrary traitLibrary;
        [SerializeField] private ChimeraBiomeConfig biomeConfig;
        [SerializeField] private ChimeraSpeciesConfig speciesConfig;
        [SerializeField] private CreatureSpeciesConfig creatureSpeciesConfig;

        [Header("⚙️ INTEGRATION SETTINGS")]
        [SerializeField] private bool prioritizeUnifiedConfig = true;
        [SerializeField] private bool enableTraitLibraryIntegration = true;
        [SerializeField] private bool enableBiomeConfigIntegration = true;
        [SerializeField] private bool enableSpeciesConfigIntegration = true;
        [SerializeField] private bool logIntegrationDetails = false;

        [Header("🧬 TRAIT MAPPING")]
        [SerializeField] private List<TraitMapping> traitMappings = new List<TraitMapping>
        {
            new TraitMapping { unifiedTraitName = "Aggression", libraryTraitName = "Territorial Aggression", weight = 1.0f },
            new TraitMapping { unifiedTraitName = "Sociability", libraryTraitName = "Pack Bonding", weight = 1.0f },
            new TraitMapping { unifiedTraitName = "Intelligence", libraryTraitName = "Problem Solving", weight = 0.8f },
            new TraitMapping { unifiedTraitName = "Intelligence", libraryTraitName = "Learning Speed", weight = 0.6f },
            new TraitMapping { unifiedTraitName = "Curiosity", libraryTraitName = "Exploration Drive", weight = 1.0f },
            new TraitMapping { unifiedTraitName = "Size", libraryTraitName = "Body Mass", weight = 1.0f },
            new TraitMapping { unifiedTraitName = "Speed", libraryTraitName = "Movement Speed", weight = 0.9f },
            new TraitMapping { unifiedTraitName = "Speed", libraryTraitName = "Reflex Speed", weight = 0.7f },
        };

        // Cached integrated values - PERFORMANCE OPTIMIZED
        private Dictionary<CoreTraitType, float> _integratedTraitValues = new Dictionary<CoreTraitType, float>();
        private Dictionary<ChimeraBiomeType, BiomeIntegrationData> _integratedBiomeData = new Dictionary<ChimeraBiomeType, BiomeIntegrationData>();
        private bool _integrationCacheValid = false;
        private bool _biomeDataInitialized = false;

        private void Awake()
        {
            InitializeBiomeData();
        }

        private void OnDestroy()
        {
            DisposeBiomeData();
        }

        private void OnValidate()
        {
            // Invalidate cache when configuration changes
            _integrationCacheValid = false;
        }

        private void InitializeBiomeData()
        {
            if (!_biomeDataInitialized)
            {
                _integratedBiomeData = new Dictionary<ChimeraBiomeType, BiomeIntegrationData>();
                _biomeDataInitialized = true;
            }
        }

        private void DisposeBiomeData()
        {
            if (_biomeDataInitialized)
            {
                _integratedBiomeData.Clear();
                _biomeDataInitialized = false;
            }
        }

        /// <summary>
        /// Get integrated trait value combining unified config and trait library
        /// </summary>
        public float GetIntegratedTraitValue(string traitName, float defaultValue = 0.5f)
        {
            ValidateIntegrationCache();

            // Convert string to TraitID for optimized lookup
            if (System.Enum.TryParse<CoreTraitType>(traitName, true, out var traitID))
            {
                return _integratedTraitValues.TryGetValue(traitID, out var value) ? value : defaultValue;
            }
            return defaultValue;
        }

        /// <summary>
        /// Get integrated trait value using TraitID (performance optimized)
        /// </summary>
        public float GetIntegratedTraitValue(CoreTraitType traitID, float defaultValue = 0.5f)
        {
            ValidateIntegrationCache();
            return _integratedTraitValues.TryGetValue(traitID, out var value) ? value : defaultValue;
        }

        /// <summary>
        /// Get integrated biome data combining unified config and biome configs
        /// </summary>
        public BiomeIntegrationData GetIntegratedBiomeData(ChimeraBiomeType biomeType)
        {
            ValidateIntegrationCache();
            InitializeBiomeData();

            // Direct array access for performance
            var data = _integratedBiomeData[biomeType];

            // Check if data was initialized
            if (data.biomeType == biomeType)
                return data;

            // Create and cache default data
            data = CreateDefaultBiomeData(biomeType);
            _integratedBiomeData[biomeType] = data;
            return data;
        }

        /// <summary>
        /// Get breeding settings enhanced with species-specific data
        /// </summary>
        public EnhancedBreedingSettings GetEnhancedBreedingSettings(string speciesName)
        {
            var baseSettings = unifiedConfig?.Breeding ?? BreedingSettings.CreateDefault();
            var enhancedSettings = new EnhancedBreedingSettings(baseSettings);

            // Enhance with species-specific data
            if (enableSpeciesConfigIntegration && speciesConfig != null)
            {
                var speciesData = speciesConfig.GetSpeciesData(speciesName);
                if (speciesData != null)
                {
                    enhancedSettings.gestationTime *= speciesData.gestationModifier;
                    enhancedSettings.offspringRange = speciesData.offspringRange;
                    enhancedSettings.maxBreedingDistance *= speciesData.territoryModifier;
                    enhancedSettings.geneticDiversityPreference *= speciesData.diversityPreference;
                }
            }

            return enhancedSettings;
        }

        /// <summary>
        /// Get genetics settings enhanced with trait library definitions
        /// </summary>
        public EnhancedGeneticSettings GetEnhancedGeneticSettings()
        {
            var baseSettings = unifiedConfig?.Genetics ?? GeneticEvolutionSettings.CreateDefault();
            var enhancedSettings = new EnhancedGeneticSettings(baseSettings);

            // Enhance with trait library data
            if (enableTraitLibraryIntegration && traitLibrary != null)
            {
                enhancedSettings.availableTraits = ConvertTraitDefinitions(traitLibrary.GetAllTraitDefinitions());
                enhancedSettings.traitInheritanceRules = ConvertInheritanceRules(traitLibrary.GetInheritanceRules());
                enhancedSettings.mutationProbabilities = ConvertMutationProbabilities(traitLibrary.GetMutationProbabilities());

                // Override mutation rate based on trait library recommendations
                var recommendedMutationRate = traitLibrary.GetRecommendedMutationRate();
                if (recommendedMutationRate > 0)
                {
                    enhancedSettings.baseMutationRate = recommendedMutationRate;
                }
            }

            return enhancedSettings;
        }

        /// <summary>
        /// Create a genetics data component using integrated trait values
        /// </summary>
        public ChimeraGeneticDataComponent CreateIntegratedGeneticData(string speciesName = "")
        {
            ValidateIntegrationCache();

            var geneticData = new ChimeraGeneticDataComponent
            {
                Aggression = GetIntegratedTraitValue("Aggression", 0.5f),
                Sociability = GetIntegratedTraitValue("Sociability", 0.5f),
                Curiosity = GetIntegratedTraitValue("Curiosity", 0.5f),
                Caution = GetIntegratedTraitValue("Caution", 0.5f),
                Intelligence = GetIntegratedTraitValue("Intelligence", 0.5f),
                Metabolism = GetIntegratedTraitValue("Metabolism", 1.0f),
                Fertility = GetIntegratedTraitValue("Fertility", 0.7f),
                Dominance = GetIntegratedTraitValue("Dominance", 0.5f),
                Size = GetIntegratedTraitValue("Size", 1.0f),
                Speed = GetIntegratedTraitValue("Speed", 1.0f),
                Stamina = GetIntegratedTraitValue("Stamina", 1.0f),
                Camouflage = GetIntegratedTraitValue("Camouflage", 0.5f),
                HeatTolerance = GetIntegratedTraitValue("HeatTolerance", 0.5f),
                ColdTolerance = GetIntegratedTraitValue("ColdTolerance", 0.5f),
                WaterAffinity = GetIntegratedTraitValue("WaterAffinity", 0.5f),
                Adaptability = GetIntegratedTraitValue("Adaptability", 0.6f)
            };

            // Apply species-specific modifications
            if (!string.IsNullOrEmpty(speciesName) && enableSpeciesConfigIntegration && speciesConfig != null)
            {
                var speciesData = speciesConfig.GetSpeciesData(speciesName);
                if (speciesData != null)
                {
                    ApplySpeciesModifications(ref geneticData, ConvertSpeciesData(speciesData));
                }
            }

            // Calculate derived values
            geneticData.OverallFitness = CalculateIntegratedFitness(geneticData);
            geneticData.GeneticHash = CalculateGeneticHash(geneticData);
            geneticData.NativeBiome = DetermineBestBiome(geneticData);

            return geneticData;
        }

        private void ValidateIntegrationCache()
        {
            if (_integrationCacheValid) return;

            RebuildIntegrationCache();
            _integrationCacheValid = true;

            if (logIntegrationDetails)
                UnityEngine.Debug.Log($"🔗 Configuration integration cache rebuilt with optimized trait arrays and biome data");
        }

        private void RebuildIntegrationCache()
        {
            InitializeBiomeData();
            // Reset trait values to defaults
            _integratedTraitValues = new Dictionary<CoreTraitType, float>();

            // Integrate trait data
            if (enableTraitLibraryIntegration && traitLibrary != null)
            {
                IntegrateTraitLibrary();
            }

            // Integrate biome data
            if (enableBiomeConfigIntegration && biomeConfig != null)
            {
                IntegrateBiomeConfiguration();
            }

            // Apply unified config overrides if prioritized
            if (prioritizeUnifiedConfig && unifiedConfig != null)
            {
                ApplyUnifiedConfigOverrides();
            }
        }

        private void IntegrateTraitLibrary()
        {
            var allTraits = traitLibrary.GetAllTraitDefinitions();

            foreach (var mapping in traitMappings)
            {
                var libraryTrait = allTraits.FirstOrDefault(t => t.traitName == mapping.libraryTraitName);
                if (libraryTrait != null)
                {
                    if (System.Enum.TryParse<CoreTraitType>(mapping.unifiedTraitName, true, out var traitID))
                    {
                        var existingValue = _integratedTraitValues.TryGetValue(traitID, out var value) ? value : 0f;

                        // Combine existing value with library trait value
                        if (existingValue > 0f)
                        {
                            var combinedValue = (existingValue + libraryTrait.baseValue * mapping.weight) / 2f;
                            _integratedTraitValues[traitID] = combinedValue;
                        }
                        else
                        {
                            _integratedTraitValues[traitID] = libraryTrait.baseValue * mapping.weight;
                        }
                    }
                }
            }
        }

        private void IntegrateBiomeConfiguration()
        {
            var biomeTypes = System.Enum.GetValues(typeof(ChimeraBiomeType)).Cast<ChimeraBiomeType>();

            foreach (var biomeType in biomeTypes)
            {
                var biomeData = biomeConfig.GetBiomeData(biomeType.ToString());
                var unifiedBiome = unifiedConfig?.Ecosystem.biomes.FirstOrDefault(b => b.type == biomeType);

                var integratedData = new BiomeIntegrationData
                {
                    biomeType = biomeType,
                    resourceAbundance = unifiedBiome?.resourceAbundance ?? biomeData.resourceAbundance,
                    carryingCapacity = unifiedBiome?.carryingCapacity ?? biomeData.carryingCapacity,
                    temperature = GetDefaultTemperature(biomeType),
                    humidity = GetDefaultHumidity(biomeType),
                    predatorDanger = 0.3f,
                    resourceTypes = GetDefaultResources(biomeType),
                    specialFeatures = new List<string>()
                };

                _integratedBiomeData[biomeType] = integratedData;
            }
        }

        private void ApplyUnifiedConfigOverrides()
        {
            // Override trait values with unified config if they exist
            // This allows the unified config to have final say on values
            if (unifiedConfig.Genetics != null)
            {
                // Apply unified genetic settings as baseline modifiers
                foreach (CoreTraitType traitID in System.Enum.GetValues(typeof(CoreTraitType)))
                {
                    var currentValue = _integratedTraitValues.TryGetValue(traitID, out var value) ? value : 0.5f;
                    var traitName = traitID.ToString();
                    var modifiedValue = ApplyGeneticEvolutionModifier(currentValue, traitName);
                    _integratedTraitValues[traitID] = modifiedValue;
                }
            }
        }

        private float ApplyGeneticEvolutionModifier(float baseValue, string traitName)
        {
            // Apply unified config modifiers based on genetic evolution settings
            var genetics = unifiedConfig.Genetics;

            switch (traitName)
            {
                case "Intelligence":
                    return baseValue * (1f + genetics.adaptabilityWeight);
                case "Aggression":
                    return baseValue * (1f + genetics.survivalWeight);
                case "Fertility":
                    return baseValue * (1f + genetics.reproductionWeight);
                case "Metabolism":
                    return baseValue * (1f + genetics.resourceEfficiencyWeight);
                default:
                    return baseValue;
            }
        }

        private void ApplySpeciesModifications(ref ChimeraGeneticDataComponent genetics, SpeciesData speciesData)
        {
            genetics.Size *= speciesData.sizeModifier;
            genetics.Speed *= speciesData.speedModifier;
            genetics.Aggression *= speciesData.aggressionModifier;
            genetics.Sociability *= speciesData.socialModifier;
            genetics.Intelligence *= speciesData.intelligenceModifier;
        }

        private float CalculateIntegratedFitness(ChimeraGeneticDataComponent genetics)
        {
            // Use unified config weights if available
            var weights = unifiedConfig?.Genetics;
            if (weights != null)
            {
                return (genetics.Aggression * weights.survivalWeight +
                        genetics.Fertility * weights.reproductionWeight +
                        genetics.Metabolism * weights.resourceEfficiencyWeight +
                        genetics.Adaptability * weights.adaptabilityWeight);
            }

            // Fallback to balanced calculation
            return (genetics.Aggression + genetics.Intelligence + genetics.Adaptability + genetics.Fertility) / 4f;
        }

        private uint CalculateGeneticHash(ChimeraGeneticDataComponent genetics)
        {
            uint hash = 0;
            hash ^= (uint)(genetics.Aggression * 1000) << 0;
            hash ^= (uint)(genetics.Sociability * 1000) << 4;
            hash ^= (uint)(genetics.Intelligence * 1000) << 8;
            hash ^= (uint)(genetics.Size * 1000) << 12;
            return hash;
        }

        private ChimeraBiomeType DetermineBestBiome(ChimeraGeneticDataComponent genetics)
        {
            float bestScore = 0f;
            ChimeraBiomeType bestBiome = ChimeraBiomeType.Grassland;

            foreach (var biomeData in _integratedBiomeData.Values)
            {
                float score = CalculateBiomeAffinityScore(genetics, biomeData);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestBiome = biomeData.biomeType;
                }
            }

            return bestBiome;
        }

        private float CalculateBiomeAffinityScore(ChimeraGeneticDataComponent genetics, BiomeIntegrationData biome)
        {
            float score = 0f;

            // Temperature affinity
            if (biome.temperature > 25f) score += genetics.HeatTolerance;
            if (biome.temperature < 10f) score += genetics.ColdTolerance;

            // Humidity affinity
            if (biome.humidity > 0.7f) score += genetics.WaterAffinity;

            // Resource availability
            score += biome.resourceAbundance * genetics.Metabolism;

            // Predator danger
            score += (1f - biome.predatorDanger) * (1f - genetics.Aggression);

            return score;
        }

        private BiomeIntegrationData CreateDefaultBiomeData(ChimeraBiomeType biomeType)
        {
            return new BiomeIntegrationData
            {
                biomeType = biomeType,
                resourceAbundance = 0.5f,
                carryingCapacity = 20,
                temperature = GetDefaultTemperature(biomeType),
                humidity = GetDefaultHumidity(biomeType),
                predatorDanger = 0.3f,
                resourceTypes = GetDefaultResources(biomeType),
                specialFeatures = new List<string>()
            };
        }

        private float GetDefaultTemperature(ChimeraBiomeType biome)
        {
            switch (biome)
            {
                case ChimeraBiomeType.Desert: return 40f;
                case ChimeraBiomeType.Tundra: return -5f;
                case ChimeraBiomeType.Mountain: return 8f;
                case ChimeraBiomeType.Ocean: return 18f;
                case ChimeraBiomeType.Swamp: return 28f;
                default: return 22f;
            }
        }

        private float GetDefaultHumidity(ChimeraBiomeType biome)
        {
            switch (biome)
            {
                case ChimeraBiomeType.Desert: return 0.1f;
                case ChimeraBiomeType.Ocean: return 1.0f;
                case ChimeraBiomeType.Swamp: return 0.95f;
                case ChimeraBiomeType.Forest: return 0.8f;
                case ChimeraBiomeType.Tundra: return 0.4f;
                default: return 0.6f;
            }
        }

        private List<string> GetDefaultResources(ChimeraBiomeType biome)
        {
            switch (biome)
            {
                case ChimeraBiomeType.Forest:
                    return new List<string> { "Wood", "Fruits", "Small Game", "Herbs" };
                case ChimeraBiomeType.Desert:
                    return new List<string> { "Cacti", "Minerals", "Insects" };
                case ChimeraBiomeType.Ocean:
                    return new List<string> { "Fish", "Seaweed", "Coral" };
                case ChimeraBiomeType.Mountain:
                    return new List<string> { "Stone", "Rare Minerals", "Mountain Game" };
                default:
                    return new List<string> { "Plants", "Water", "Small Game" };
            }
        }

        /// <summary>
        /// Force rebuild of integration cache (useful for runtime changes)
        /// </summary>
        public void ForceIntegrationRebuild()
        {
            _integrationCacheValid = false;
            ValidateIntegrationCache();
        }

        /// <summary>
        /// Get integration statistics for debugging
        /// </summary>
        public IntegrationStats GetIntegrationStats()
        {
            ValidateIntegrationCache();

            return new IntegrationStats
            {
                integratedTraitsCount = System.Enum.GetValues(typeof(CoreTraitType)).Length,
                integratedBiomesCount = 17, // Total biome count
                traitLibraryConnected = traitLibrary != null,
                biomeConfigConnected = biomeConfig != null,
                speciesConfigConnected = speciesConfig != null,
                unifiedConfigConnected = unifiedConfig != null
            };
        }

        // Type conversion methods for trait library integration
        private List<TraitDefinition> ConvertTraitDefinitions(Laboratory.Chimera.Configuration.TraitDefinition[] configTraits)
        {
            var integrationTraits = new List<TraitDefinition>();

            foreach (var configTrait in configTraits)
            {
                integrationTraits.Add(new TraitDefinition
                {
                    name = configTrait.traitName,
                    defaultValue = configTrait.defaultValue,
                    minValue = configTrait.minValue,
                    maxValue = configTrait.maxValue,
                    category = configTrait.category,
                    isPhysical = configTrait.isPhysical,
                    isBehavioral = configTrait.isBehavioral,
                    inheritanceWeight = configTrait.inheritanceWeight
                });
            }

            return integrationTraits;
        }

        private Dictionary<CoreTraitType, InheritanceRule> ConvertInheritanceRules(Laboratory.Chimera.Configuration.TraitInheritanceRules configRules)
        {
            var integrationRules = new Dictionary<CoreTraitType, InheritanceRule>();

            if (configRules != null)
            {
                // Apply general inheritance rules to all trait types
                foreach (CoreTraitType traitType in System.Enum.GetValues(typeof(CoreTraitType)))
                {
                    integrationRules[traitType] = new InheritanceRule
                    {
                        inheritanceType = configRules.dominanceThreshold > 0.5f ? InheritanceType.Dominant : InheritanceType.Blended,
                        dominanceWeight = configRules.dominanceThreshold,
                        mutationChance = configRules.mutationRate,
                        blendFactor = configRules.parentWeightBalance
                    };
                }
            }

            return integrationRules;
        }

        private Dictionary<CoreTraitType, float> ConvertMutationProbabilities(Laboratory.Chimera.Configuration.TraitMutationProbabilities configMutations)
        {
            var integrationMutations = new Dictionary<CoreTraitType, float>();

            if (configMutations != null)
            {
                integrationMutations[CoreTraitType.Size] = configMutations.physicalTraits;
                integrationMutations[CoreTraitType.Aggression] = configMutations.behavioralTraits;
                integrationMutations[CoreTraitType.Camouflage] = configMutations.cosmeticTraits;
                integrationMutations[CoreTraitType.Adaptability] = configMutations.specialTraits;
                integrationMutations[CoreTraitType.Intelligence] = configMutations.globalModifier;
            }

            return integrationMutations;
        }

        private SpeciesData ConvertSpeciesData(Laboratory.Chimera.Configuration.SpeciesData configSpeciesData)
        {
            return new SpeciesData
            {
                speciesName = configSpeciesData.speciesName,
                sizeModifier = configSpeciesData.sizeModifier,
                speedModifier = configSpeciesData.speedModifier,
                aggressionModifier = configSpeciesData.aggressionModifier,
                socialModifier = configSpeciesData.socialModifier,
                intelligenceModifier = configSpeciesData.intelligenceModifier,
                description = configSpeciesData.description,
                rarity = configSpeciesData.rarity
            };
        }
    }

    // Supporting data structures
    [System.Serializable]
    public class TraitMapping
    {
        public string unifiedTraitName;
        public string libraryTraitName;
        [Range(0f, 2f)] public float weight = 1f;
    }

    [System.Serializable]
    public class BiomeIntegrationData
    {
        public ChimeraBiomeType biomeType;
        public float resourceAbundance;
        public int carryingCapacity;
        public float temperature;
        public float humidity;
        public float predatorDanger;
        public List<string> resourceTypes;
        public List<string> specialFeatures;
    }

    [System.Serializable]
    public class EnhancedBreedingSettings : BreedingSettings
    {
        public List<string> supportedSpecies = new List<string>();
        public Dictionary<SystemType, float> speciesCompatibility = new Dictionary<SystemType, float>();

        public EnhancedBreedingSettings(BreedingSettings baseSettings)
        {
            // Copy all base settings
            breedingSeasonLength = baseSettings.breedingSeasonLength;
            courtshipDuration = baseSettings.courtshipDuration;
            gestationTime = baseSettings.gestationTime;
            breedingCooldown = baseSettings.breedingCooldown;
            offspringRange = baseSettings.offspringRange;
            maxBreedingDistance = baseSettings.maxBreedingDistance;
            geneticDiversityPreference = baseSettings.geneticDiversityPreference;
            fitnessPreference = baseSettings.fitnessPreference;
        }
    }

    [System.Serializable]
    public class EnhancedGeneticSettings : GeneticEvolutionSettings
    {
        public List<TraitDefinition> availableTraits = new List<TraitDefinition>();
        public Dictionary<CoreTraitType, InheritanceRule> traitInheritanceRules = new Dictionary<CoreTraitType, InheritanceRule>();
        public Dictionary<CoreTraitType, float> mutationProbabilities = new Dictionary<CoreTraitType, float>();

        public EnhancedGeneticSettings(GeneticEvolutionSettings baseSettings)
        {
            // Copy all base settings
            baseMutationRate = baseSettings.baseMutationRate;
            beneficialMutationChance = baseSettings.beneficialMutationChance;
            environmentalPressureStrength = baseSettings.environmentalPressureStrength;
            enableNaturalSelection = baseSettings.enableNaturalSelection;
            enableGeneticDrift = baseSettings.enableGeneticDrift;
        }
    }

    public struct IntegrationStats
    {
        public int integratedTraitsCount;
        public int integratedBiomesCount;
        public bool traitLibraryConnected;
        public bool biomeConfigConnected;
        public bool speciesConfigConnected;
        public bool unifiedConfigConnected;
    }

    // Placeholder classes for existing system types (would be defined in their respective files)
    public class TraitDefinition
    {
        public string name;
        public float baseValue;
        public float defaultValue;
        public float minValue;
        public float maxValue;
        public string category;
        public bool isPhysical;
        public bool isBehavioral;
        public float inheritanceWeight;
        public Laboratory.Core.Enums.TraitType type;
    }
    /// <summary>
    /// Types of genetic inheritance patterns
    /// </summary>
    public enum InheritanceType
    {
        Dominant,
        Recessive,
        Codominant,
        Blended,
        XLinked,
        Polygenic
    }

    /// <summary>
    /// Rules for how traits are inherited and expressed
    /// </summary>
    public class InheritanceRule
    {
        public InheritanceType inheritanceType = InheritanceType.Dominant;
        public float dominanceWeight = 1.0f;
        public float mutationChance = 0.02f;
        public float blendFactor = 0.5f;
    }
    public class SpeciesData
    {
        public string speciesName = "DefaultSpecies";
        public string description = "A default species";
        public float rarity = 0.5f;
        public float sizeModifier = 1f;
        public float speedModifier = 1f;
        public float aggressionModifier = 1f;
        public float socialModifier = 1f;
        public float intelligenceModifier = 1f;
        public float gestationModifier = 1f;
        public Vector2Int offspringRange = new Vector2Int(1, 3);
        public float territoryModifier = 1f;
        public float diversityPreference = 0.6f;
    }
}