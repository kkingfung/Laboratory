using UnityEngine;
using Laboratory.Chimera.Configuration;
using Laboratory.Chimera.Genetics;
using System.Collections.Generic;
using System.Linq;

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

        // Cached integrated values
        private Dictionary<string, float> _integratedTraitValues = new Dictionary<string, float>();
        private Dictionary<BiomeType, BiomeIntegrationData> _integratedBiomeData = new Dictionary<BiomeType, BiomeIntegrationData>();
        private bool _integrationCacheValid = false;

        private void OnValidate()
        {
            // Invalidate cache when configuration changes
            _integrationCacheValid = false;
        }

        /// <summary>
        /// Get integrated trait value combining unified config and trait library
        /// </summary>
        public float GetIntegratedTraitValue(string traitName, float defaultValue = 0.5f)
        {
            ValidateIntegrationCache();

            if (_integratedTraitValues.TryGetValue(traitName, out var value))
                return value;

            return defaultValue;
        }

        /// <summary>
        /// Get integrated biome data combining unified config and biome configs
        /// </summary>
        public BiomeIntegrationData GetIntegratedBiomeData(BiomeType biomeType)
        {
            ValidateIntegrationCache();

            if (_integratedBiomeData.TryGetValue(biomeType, out var data))
                return data;

            return CreateDefaultBiomeData(biomeType);
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
                enhancedSettings.availableTraits = traitLibrary.GetAllTraitDefinitions();
                enhancedSettings.traitInheritanceRules = traitLibrary.GetInheritanceRules();
                enhancedSettings.mutationProbabilities = traitLibrary.GetMutationProbabilities();

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
        public GeneticDataComponent CreateIntegratedGeneticData(string speciesName = "")
        {
            ValidateIntegrationCache();

            var geneticData = new GeneticDataComponent
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
                    ApplySpeciesModifications(ref geneticData, speciesData);
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
                Debug.Log($"🔗 Configuration integration cache rebuilt with {_integratedTraitValues.Count} traits and {_integratedBiomeData.Count} biomes");
        }

        private void RebuildIntegrationCache()
        {
            _integratedTraitValues.Clear();
            _integratedBiomeData.Clear();

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
                var libraryTrait = allTraits.FirstOrDefault(t => t.name == mapping.libraryTraitName);
                if (libraryTrait != null)
                {
                    // Combine existing value with library trait value
                    if (_integratedTraitValues.TryGetValue(mapping.unifiedTraitName, out var existingValue))
                    {
                        _integratedTraitValues[mapping.unifiedTraitName] =
                            (existingValue + libraryTrait.baseValue * mapping.weight) / 2f;
                    }
                    else
                    {
                        _integratedTraitValues[mapping.unifiedTraitName] =
                            libraryTrait.baseValue * mapping.weight;
                    }
                }
            }
        }

        private void IntegrateBiomeConfiguration()
        {
            var biomeTypes = System.Enum.GetValues(typeof(BiomeType)).Cast<BiomeType>();

            foreach (var biomeType in biomeTypes)
            {
                var biomeData = biomeConfig.GetBiomeData(biomeType.ToString());
                var unifiedBiome = unifiedConfig?.Ecosystem.biomes.FirstOrDefault(b => b.type == biomeType);

                var integratedData = new BiomeIntegrationData
                {
                    biomeType = biomeType,
                    resourceAbundance = unifiedBiome?.resourceAbundance ?? biomeData?.resourceDensity ?? 0.5f,
                    carryingCapacity = unifiedBiome?.carryingCapacity ?? biomeData?.maxCreatures ?? 20,
                    temperature = biomeData?.averageTemperature ?? GetDefaultTemperature(biomeType),
                    humidity = biomeData?.humidity ?? GetDefaultHumidity(biomeType),
                    predatorDanger = biomeData?.predatorLevel ?? 0.3f,
                    resourceTypes = biomeData?.availableResources ?? GetDefaultResources(biomeType),
                    specialFeatures = biomeData?.specialFeatures ?? new List<string>()
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
                foreach (var kvp in _integratedTraitValues.ToList())
                {
                    var modifiedValue = ApplyGeneticEvolutionModifier(kvp.Value, kvp.Key);
                    _integratedTraitValues[kvp.Key] = modifiedValue;
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

        private void ApplySpeciesModifications(ref GeneticDataComponent genetics, SpeciesData speciesData)
        {
            genetics.Size *= speciesData.sizeModifier;
            genetics.Speed *= speciesData.speedModifier;
            genetics.Aggression *= speciesData.aggressionModifier;
            genetics.Sociability *= speciesData.socialModifier;
            genetics.Intelligence *= speciesData.intelligenceModifier;
        }

        private float CalculateIntegratedFitness(GeneticDataComponent genetics)
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

        private uint CalculateGeneticHash(GeneticDataComponent genetics)
        {
            uint hash = 0;
            hash ^= (uint)(genetics.Aggression * 1000) << 0;
            hash ^= (uint)(genetics.Sociability * 1000) << 4;
            hash ^= (uint)(genetics.Intelligence * 1000) << 8;
            hash ^= (uint)(genetics.Size * 1000) << 12;
            return hash;
        }

        private BiomeType DetermineBestBiome(GeneticDataComponent genetics)
        {
            float bestScore = 0f;
            BiomeType bestBiome = BiomeType.Grassland;

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

        private float CalculateBiomeAffinityScore(GeneticDataComponent genetics, BiomeIntegrationData biome)
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

        private BiomeIntegrationData CreateDefaultBiomeData(BiomeType biomeType)
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

        private float GetDefaultTemperature(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return 40f;
                case BiomeType.Tundra: return -5f;
                case BiomeType.Mountain: return 8f;
                case BiomeType.Ocean: return 18f;
                case BiomeType.Swamp: return 28f;
                default: return 22f;
            }
        }

        private float GetDefaultHumidity(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Desert: return 0.1f;
                case BiomeType.Ocean: return 1.0f;
                case BiomeType.Swamp: return 0.95f;
                case BiomeType.Forest: return 0.8f;
                case BiomeType.Tundra: return 0.4f;
                default: return 0.6f;
            }
        }

        private List<string> GetDefaultResources(BiomeType biome)
        {
            switch (biome)
            {
                case BiomeType.Forest:
                    return new List<string> { "Wood", "Fruits", "Small Game", "Herbs" };
                case BiomeType.Desert:
                    return new List<string> { "Cacti", "Minerals", "Insects" };
                case BiomeType.Ocean:
                    return new List<string> { "Fish", "Seaweed", "Coral" };
                case BiomeType.Mountain:
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
                integratedTraitsCount = _integratedTraitValues.Count,
                integratedBiomesCount = _integratedBiomeData.Count,
                traitLibraryConnected = traitLibrary != null,
                biomeConfigConnected = biomeConfig != null,
                speciesConfigConnected = speciesConfig != null,
                unifiedConfigConnected = unifiedConfig != null
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
        public BiomeType biomeType;
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
        public Dictionary<string, float> speciesCompatibility = new Dictionary<string, float>();

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
        public Dictionary<string, InheritanceRule> traitInheritanceRules = new Dictionary<string, InheritanceRule>();
        public Dictionary<string, float> mutationProbabilities = new Dictionary<string, float>();

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
        public TraitType type;
    }
    public class InheritanceRule { }
    public class SpeciesData
    {
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