using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Ecosystem.Data;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Ecosystem.Systems
{
    /// <summary>
    /// Manages species interactions including predation, competition, symbiosis, and territorial behavior
    /// </summary>
    public class SpeciesInteractionSystem : MonoBehaviour
    {
        [Header("Interaction Configuration")]
        [SerializeField] private float interactionUpdateInterval = 3.0f;
        [SerializeField] private float interactionRange = 25.0f;
        [SerializeField] private float baseInteractionProbability = 0.1f;
        [SerializeField] private bool enableAdvancedInteractions = true;

        [Header("Population Effects")]
        [SerializeField] private float populationPressureThreshold = 0.8f;
        [SerializeField] private float extinctionThreshold = 0.05f;
        [SerializeField] private bool enablePopulationDynamics = true;

        [Header("Behavioral Patterns")]
        [SerializeField] private float territorialRadius = 10.0f;
        [SerializeField] private float migrationPressureThreshold = 0.7f;
        [SerializeField] private bool enableTerritorialBehavior = true;

        private Dictionary<uint, SpeciesData> speciesDatabase = new();
        private Dictionary<uint, List<SpeciesInteraction>> activeInteractions = new();
        private Dictionary<uint, PopulationData> populationStats = new();
        private Dictionary<(uint, uint), InteractionHistory> interactionHistory = new();

        // Dependencies
        private ResourceFlowSystem resourceSystem;

        // Events
        public System.Action<uint, uint, Laboratory.Chimera.Ecosystem.Data.InteractionType, float> OnInteractionOccurred;
        public System.Action<uint, float> OnPopulationChanged;
        public System.Action<uint> OnSpeciesExtinction;
        public System.Action<uint, Vector2, Vector2> OnMigrationTriggered;
        public System.Action<uint, uint, float> OnTerritorialConflict;

        private void Awake()
        {
            resourceSystem = FindObjectOfType<ResourceFlowSystem>();
            InitializeSpeciesDatabase();
        }

        private void Start()
        {
            StartCoroutine(InteractionUpdateLoop());
        }

        private void InitializeSpeciesDatabase()
        {
            // Initialize with example species data
            // In a real implementation, this would load from the creature genetics system
            var exampleSpecies = new SpeciesData[]
            {
                new SpeciesData
                {
                    SpeciesId = 1,
                    Name = "Forest Herbivore",
                    TrophicLevel = Laboratory.Chimera.Ecosystem.Data.TrophicLevel.PrimaryConsumer,
                    PreferredBiomes = new List<BiomeType> { BiomeType.Forest, BiomeType.Grassland },
                    PrimaryResources = new List<Laboratory.Chimera.Ecosystem.Data.ResourceType> { Laboratory.Chimera.Ecosystem.Data.ResourceType.Food, Laboratory.Chimera.Ecosystem.Data.ResourceType.Water, Laboratory.Chimera.Ecosystem.Data.ResourceType.Shelter },
                    SocialBehavior = SocialBehaviorType.Herd,
                    TerritorialLevel = 0.3f,
                    AggressionLevel = 0.2f,
                    PopulationSize = 150,
                    CarryingCapacity = 300,
                    ReproductionRate = 0.15f
                },
                new SpeciesData
                {
                    SpeciesId = 2,
                    Name = "Forest Predator",
                    TrophicLevel = Laboratory.Chimera.Ecosystem.Data.TrophicLevel.SecondaryConsumer,
                    PreferredBiomes = new List<BiomeType> { BiomeType.Forest },
                    PrimaryResources = new List<Laboratory.Chimera.Ecosystem.Data.ResourceType> { Laboratory.Chimera.Ecosystem.Data.ResourceType.Food, Laboratory.Chimera.Ecosystem.Data.ResourceType.Territory },
                    SocialBehavior = SocialBehaviorType.Solitary,
                    TerritorialLevel = 0.8f,
                    AggressionLevel = 0.7f,
                    PopulationSize = 25,
                    CarryingCapacity = 50,
                    ReproductionRate = 0.08f
                },
                new SpeciesData
                {
                    SpeciesId = 3,
                    Name = "Desert Scavenger",
                    TrophicLevel = Laboratory.Chimera.Ecosystem.Data.TrophicLevel.Omnivore,
                    PreferredBiomes = new List<BiomeType> { BiomeType.Desert, BiomeType.Tropical },
                    PrimaryResources = new List<Laboratory.Chimera.Ecosystem.Data.ResourceType> { Laboratory.Chimera.Ecosystem.Data.ResourceType.Food, Laboratory.Chimera.Ecosystem.Data.ResourceType.Water },
                    SocialBehavior = SocialBehaviorType.Pack,
                    TerritorialLevel = 0.5f,
                    AggressionLevel = 0.4f,
                    PopulationSize = 80,
                    CarryingCapacity = 120,
                    ReproductionRate = 0.12f
                }
            };

            foreach (var species in exampleSpecies)
            {
                speciesDatabase[species.SpeciesId] = species;
                populationStats[species.SpeciesId] = new PopulationData
                {
                    SpeciesId = species.SpeciesId,
                    CurrentPopulation = species.PopulationSize,
                    MaxPopulation = species.CarryingCapacity,
                    GrowthRate = species.ReproductionRate,
                    LastUpdate = System.DateTime.Now
                };
            }

            UnityEngine.Debug.Log($"🦎 Initialized {speciesDatabase.Count} species in interaction system");
        }

        private IEnumerator InteractionUpdateLoop()
        {
            while (true)
            {
                ProcessSpeciesInteractions();
                UpdatePopulationDynamics();
                CheckExtinctionThresholds();
                ProcessTerritorialBehavior();
                CheckMigrationPressure();

                yield return new WaitForSeconds(interactionUpdateInterval);
            }
        }

        private void ProcessSpeciesInteractions()
        {
            var speciesList = speciesDatabase.Keys.ToList();

            for (int i = 0; i < speciesList.Count; i++)
            {
                for (int j = i + 1; j < speciesList.Count; j++)
                {
                    var species1Id = speciesList[i];
                    var species2Id = speciesList[j];

                    if (AreSpeciesInRange(species1Id, species2Id))
                    {
                        ProcessPairwiseInteraction(species1Id, species2Id);
                    }
                }
            }
        }

        private bool AreSpeciesInRange(uint species1Id, uint species2Id)
        {
            // Simplified range check - in real implementation would check actual creature positions
            if (!speciesDatabase.TryGetValue(species1Id, out var species1) ||
                !speciesDatabase.TryGetValue(species2Id, out var species2))
                return false;

            // Check if species share preferred biomes
            return species1.PreferredBiomes.Any(b => species2.PreferredBiomes.Contains(b));
        }

        private void ProcessPairwiseInteraction(uint species1Id, uint species2Id)
        {
            if (!speciesDatabase.TryGetValue(species1Id, out var species1) ||
                !speciesDatabase.TryGetValue(species2Id, out var species2))
                return;

            var interactionType = DetermineInteractionType(species1, species2);
            var interactionStrength = CalculateInteractionStrength(species1, species2, interactionType);

            if (Random.value < baseInteractionProbability * interactionStrength)
            {
                ExecuteInteraction(species1Id, species2Id, interactionType, interactionStrength);
            }
        }

        private Laboratory.Chimera.Ecosystem.Data.InteractionType DetermineInteractionType(SpeciesData species1, SpeciesData species2)
        {
            // Predation check
            if (IsPredatorPrey(species1, species2))
                return Laboratory.Chimera.Ecosystem.Data.InteractionType.Predation;

            // Competition check
            if (CompeteForResources(species1, species2))
                return Laboratory.Chimera.Ecosystem.Data.InteractionType.Competition;

            // Territorial check
            if (species1.TerritorialLevel > 0.5f || species2.TerritorialLevel > 0.5f)
                return Laboratory.Chimera.Ecosystem.Data.InteractionType.Territorial;

            // Symbiosis possibilities
            if (species1.TrophicLevel != species2.TrophicLevel &&
                species1.PreferredBiomes.Any(b => species2.PreferredBiomes.Contains(b)))
            {
                return Random.value < 0.3f ? Laboratory.Chimera.Ecosystem.Data.InteractionType.Mutualism : Laboratory.Chimera.Ecosystem.Data.InteractionType.Commensalism;
            }

            return Laboratory.Chimera.Ecosystem.Data.InteractionType.Neutralism;
        }

        private bool IsPredatorPrey(SpeciesData species1, SpeciesData species2)
        {
            var level1 = (int)species1.TrophicLevel;
            var level2 = (int)species2.TrophicLevel;

            // Secondary consumers can prey on primary consumers, etc.
            return Mathf.Abs(level1 - level2) == 1 &&
                   (species1.TrophicLevel == Laboratory.Chimera.Ecosystem.Data.TrophicLevel.SecondaryConsumer ||
                    species1.TrophicLevel == Laboratory.Chimera.Ecosystem.Data.TrophicLevel.TertiaryConsumer ||
                    species2.TrophicLevel == Laboratory.Chimera.Ecosystem.Data.TrophicLevel.SecondaryConsumer ||
                    species2.TrophicLevel == Laboratory.Chimera.Ecosystem.Data.TrophicLevel.TertiaryConsumer);
        }

        private bool CompeteForResources(SpeciesData species1, SpeciesData species2)
        {
            // Check if species compete for the same resources
            return species1.PrimaryResources.Any(r => species2.PrimaryResources.Contains(r)) &&
                   species1.TrophicLevel == species2.TrophicLevel;
        }

        private float CalculateInteractionStrength(SpeciesData species1, SpeciesData species2, Laboratory.Chimera.Ecosystem.Data.InteractionType type)
        {
            float baseStrength = 1.0f;

            switch (type)
            {
                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Predation:
                    baseStrength = (species1.AggressionLevel + species2.AggressionLevel) * 0.5f;
                    break;
                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Competition:
                    baseStrength = CalculateResourceOverlap(species1, species2);
                    break;
                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Territorial:
                    baseStrength = Mathf.Max(species1.TerritorialLevel, species2.TerritorialLevel);
                    break;
                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Mutualism:
                    baseStrength = 0.6f; // Moderate positive interaction
                    break;
                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Commensalism:
                    baseStrength = 0.3f; // Weak interaction
                    break;
                default:
                    baseStrength = 0.1f;
                    break;
            }

            // Apply population pressure modifier
            var pop1Pressure = GetPopulationPressure(species1.SpeciesId);
            var pop2Pressure = GetPopulationPressure(species2.SpeciesId);
            float populationModifier = 1.0f + (pop1Pressure + pop2Pressure) * 0.5f;

            return baseStrength * populationModifier;
        }

        private float CalculateResourceOverlap(SpeciesData species1, SpeciesData species2)
        {
            var sharedResources = species1.PrimaryResources.Intersect(species2.PrimaryResources).Count();
            var totalResources = species1.PrimaryResources.Union(species2.PrimaryResources).Count();
            return totalResources > 0 ? (float)sharedResources / totalResources : 0f;
        }

        private float GetPopulationPressure(uint speciesId)
        {
            if (populationStats.TryGetValue(speciesId, out var stats))
            {
                return stats.CurrentPopulation / (float)stats.MaxPopulation;
            }
            return 0f;
        }

        private void ExecuteInteraction(uint species1Id, uint species2Id, Laboratory.Chimera.Ecosystem.Data.InteractionType type, float strength)
        {
            var effects = CalculateInteractionEffects(species1Id, species2Id, type, strength);

            // Apply population effects
            ApplyPopulationEffects(species1Id, effects.Species1Effect);
            ApplyPopulationEffects(species2Id, effects.Species2Effect);

            // Record interaction
            RecordInteraction(species1Id, species2Id, type, strength, effects);

            OnInteractionOccurred?.Invoke(species1Id, species2Id, type, strength);

            UnityEngine.Debug.Log($"🦎 {type} interaction between species {species1Id} and {species2Id} (strength: {strength:F2})");
        }

        private (float Species1Effect, float Species2Effect) CalculateInteractionEffects(
            uint species1Id, uint species2Id, Laboratory.Chimera.Ecosystem.Data.InteractionType type, float strength)
        {
            float effect1 = 0f, effect2 = 0f;

            switch (type)
            {
                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Predation:
                    // Determine which is predator and which is prey
                    var species1 = speciesDatabase[species1Id];
                    var species2 = speciesDatabase[species2Id];

                    if ((int)species1.TrophicLevel > (int)species2.TrophicLevel)
                    {
                        effect1 = strength * 0.1f;  // Predator benefits
                        effect2 = -strength * 0.2f; // Prey suffers
                    }
                    else
                    {
                        effect1 = -strength * 0.2f; // Prey suffers
                        effect2 = strength * 0.1f;  // Predator benefits
                    }
                    break;

                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Competition:
                    effect1 = -strength * 0.15f;
                    effect2 = -strength * 0.15f;
                    break;

                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Mutualism:
                    effect1 = strength * 0.1f;
                    effect2 = strength * 0.1f;
                    break;

                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Commensalism:
                    effect1 = strength * 0.05f;
                    effect2 = 0f;
                    break;

                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Parasitism:
                    effect1 = strength * 0.08f;
                    effect2 = -strength * 0.12f;
                    break;

                case Laboratory.Chimera.Ecosystem.Data.InteractionType.Territorial:
                    var aggression1 = speciesDatabase[species1Id].AggressionLevel;
                    var aggression2 = speciesDatabase[species2Id].AggressionLevel;

                    if (aggression1 > aggression2)
                    {
                        effect1 = strength * 0.05f;
                        effect2 = -strength * 0.1f;
                    }
                    else if (aggression2 > aggression1)
                    {
                        effect1 = -strength * 0.1f;
                        effect2 = strength * 0.05f;
                    }
                    else
                    {
                        effect1 = -strength * 0.08f;
                        effect2 = -strength * 0.08f;
                    }
                    break;
            }

            return (effect1, effect2);
        }

        private void ApplyPopulationEffects(uint speciesId, float effect)
        {
            if (populationStats.TryGetValue(speciesId, out var stats))
            {
                float populationChange = stats.CurrentPopulation * effect;
                stats.CurrentPopulation = Mathf.Max(0, stats.CurrentPopulation + populationChange);
                stats.LastUpdate = System.DateTime.Now;

                populationStats[speciesId] = stats;
                OnPopulationChanged?.Invoke(speciesId, stats.CurrentPopulation);
            }
        }

        private void RecordInteraction(uint species1Id, uint species2Id, Laboratory.Chimera.Ecosystem.Data.InteractionType type, float strength,
            (float Species1Effect, float Species2Effect) effects)
        {
            var interaction = new SpeciesInteraction
            {
                Species1Id = species1Id,
                Species2Id = species2Id,
                Type = type,
                Strength = strength,
                EffectOnSpecies1 = effects.Species1Effect,
                EffectOnSpecies2 = effects.Species2Effect,
                LocationInfluence = Vector2.zero, // Would be calculated from actual positions
                IsActive = true,
                LastInteractionTime = Time.time
            };

            if (!activeInteractions.ContainsKey(species1Id))
                activeInteractions[species1Id] = new List<SpeciesInteraction>();
            if (!activeInteractions.ContainsKey(species2Id))
                activeInteractions[species2Id] = new List<SpeciesInteraction>();

            activeInteractions[species1Id].Add(interaction);
            activeInteractions[species2Id].Add(interaction);

            // Update interaction history
            var key = species1Id < species2Id ? (species1Id, species2Id) : (species2Id, species1Id);
            if (!interactionHistory.ContainsKey(key))
            {
                interactionHistory[key] = new InteractionHistory
                {
                    Species1Id = key.Item1,
                    Species2Id = key.Item2,
                    InteractionCounts = new Dictionary<Laboratory.Chimera.Ecosystem.Data.InteractionType, int>(),
                    LastInteractionType = type,
                    TotalInteractions = 0
                };
            }

            var history = interactionHistory[key];
            history.InteractionCounts[type] = history.InteractionCounts.GetValueOrDefault(type, 0) + 1;
            history.LastInteractionType = type;
            history.TotalInteractions++;
            interactionHistory[key] = history;
        }

        private void UpdatePopulationDynamics()
        {
            if (!enablePopulationDynamics) return;

            foreach (var kvp in populationStats.ToList())
            {
                var speciesId = kvp.Key;
                var stats = kvp.Value;

                // Apply natural growth/decline
                float growthRate = CalculateModifiedGrowthRate(speciesId);
                float populationChange = stats.CurrentPopulation * growthRate * interactionUpdateInterval;

                // Apply carrying capacity pressure
                float carryingCapacityPressure = stats.CurrentPopulation / (float)stats.MaxPopulation;
                if (carryingCapacityPressure > 1.0f)
                {
                    populationChange *= (2.0f - carryingCapacityPressure); // Reduce growth when over capacity
                }

                stats.CurrentPopulation = Mathf.Max(0, stats.CurrentPopulation + populationChange);
                stats.LastUpdate = System.DateTime.Now;

                populationStats[speciesId] = stats;
                OnPopulationChanged?.Invoke(speciesId, stats.CurrentPopulation);
            }
        }

        private float CalculateModifiedGrowthRate(uint speciesId)
        {
            if (!speciesDatabase.TryGetValue(speciesId, out var species)) return 0f;

            float baseRate = species.ReproductionRate;

            // Apply resource availability modifier
            float resourceModifier = CalculateResourceAvailabilityModifier(speciesId);

            // Apply interaction effects
            float interactionModifier = CalculateInteractionModifier(speciesId);

            return baseRate * resourceModifier * interactionModifier;
        }

        private float CalculateResourceAvailabilityModifier(uint speciesId)
        {
            if (!speciesDatabase.TryGetValue(speciesId, out var species)) return 1f;

            float totalModifier = 0f;
            int resourceCount = 0;

            foreach (var resourceType in species.PrimaryResources)
            {
                // Check resource availability across preferred biomes
                foreach (var biome in species.PreferredBiomes)
                {
                    // Simplified resource check - would query actual biome locations
                    var location = Vector2.zero; // Placeholder
                    float availability = resourceSystem?.GetResourceAvailability(location, resourceType) ?? 100f;
                    totalModifier += Mathf.Clamp01(availability / 100f);
                    resourceCount++;
                }
            }

            return resourceCount > 0 ? totalModifier / resourceCount : 1f;
        }

        private float CalculateInteractionModifier(uint speciesId)
        {
            if (!activeInteractions.TryGetValue(speciesId, out var interactions)) return 1f;

            float totalEffect = 0f;

            foreach (var interaction in interactions)
            {
                float effect = interaction.Species1Id == speciesId ?
                    interaction.EffectOnSpecies1 : interaction.EffectOnSpecies2;
                totalEffect += effect;
            }

            return 1f + totalEffect;
        }

        private void CheckExtinctionThresholds()
        {
            foreach (var kvp in populationStats.ToList())
            {
                var speciesId = kvp.Key;
                var stats = kvp.Value;

                float populationRatio = stats.CurrentPopulation / (float)stats.MaxPopulation;

                if (populationRatio <= extinctionThreshold)
                {
                    OnSpeciesExtinction?.Invoke(speciesId);
                    UnityEngine.Debug.LogWarning($"💀 Species {speciesId} is facing extinction (population: {stats.CurrentPopulation})");
                }
            }
        }

        private void ProcessTerritorialBehavior()
        {
            if (!enableTerritorialBehavior) return;

            foreach (var species in speciesDatabase.Values)
            {
                if (species.TerritorialLevel > 0.5f)
                {
                    ProcessSpeciesTerritorialBehavior(species);
                }
            }
        }

        private void ProcessSpeciesTerritorialBehavior(SpeciesData species)
        {
            // Check for territorial conflicts with other species
            foreach (var otherSpecies in speciesDatabase.Values)
            {
                if (otherSpecies.SpeciesId == species.SpeciesId) continue;

                // Check if territories overlap
                bool hasOverlap = species.PreferredBiomes.Any(b => otherSpecies.PreferredBiomes.Contains(b));

                if (hasOverlap && Random.value < species.TerritorialLevel * 0.1f)
                {
                    float conflictIntensity = (species.TerritorialLevel + species.AggressionLevel) * 0.5f;
                    OnTerritorialConflict?.Invoke(species.SpeciesId, otherSpecies.SpeciesId, conflictIntensity);
                }
            }
        }

        private void CheckMigrationPressure()
        {
            foreach (var kvp in populationStats)
            {
                var speciesId = kvp.Key;
                var stats = kvp.Value;

                float pressure = stats.CurrentPopulation / (float)stats.MaxPopulation;

                if (pressure > migrationPressureThreshold)
                {
                    TriggerMigration(speciesId, pressure);
                }
            }
        }

        private void TriggerMigration(uint speciesId, float pressure)
        {
            // Simplified migration trigger
            var sourceLocation = Vector2.zero; // Would be calculated from actual population distribution
            var destinationLocation = FindSuitableMigrationDestination(speciesId);

            OnMigrationTriggered?.Invoke(speciesId, sourceLocation, destinationLocation);
            UnityEngine.Debug.Log($"🦋 Migration triggered for species {speciesId} due to population pressure: {pressure:F2}");
        }

        private Vector2 FindSuitableMigrationDestination(uint speciesId)
        {
            // Simplified destination finding - would analyze actual biome distribution
            return new Vector2(Random.Range(-50f, 50f), Random.Range(-50f, 50f));
        }

        public List<SpeciesInteraction> GetInteractionsForSpecies(uint speciesId)
        {
            return activeInteractions.GetValueOrDefault(speciesId, new List<SpeciesInteraction>());
        }

        public PopulationData GetPopulationData(uint speciesId)
        {
            return populationStats.GetValueOrDefault(speciesId, new PopulationData());
        }

        public Dictionary<uint, float> GetAllPopulations()
        {
            return populationStats.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.CurrentPopulation);
        }

        public void RegisterSpecies(SpeciesData species)
        {
            speciesDatabase[species.SpeciesId] = species;
            populationStats[species.SpeciesId] = new PopulationData
            {
                SpeciesId = species.SpeciesId,
                CurrentPopulation = species.PopulationSize,
                MaxPopulation = species.CarryingCapacity,
                GrowthRate = species.ReproductionRate,
                LastUpdate = System.DateTime.Now
            };

            UnityEngine.Debug.Log($"🦎 Registered new species: {species.Name} (ID: {species.SpeciesId})");
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }

    // Additional data structures for species interactions
    [System.Serializable]
    public struct SpeciesData
    {
        public uint SpeciesId;
        public string Name;
        public Laboratory.Chimera.Ecosystem.Data.TrophicLevel TrophicLevel;
        public List<BiomeType> PreferredBiomes;
        public List<Laboratory.Chimera.Ecosystem.Data.ResourceType> PrimaryResources;
        public SocialBehaviorType SocialBehavior;
        public float TerritorialLevel;
        public float AggressionLevel;
        public float PopulationSize;
        public float CarryingCapacity;
        public float ReproductionRate;
    }

    [System.Serializable]
    public struct PopulationData
    {
        public uint SpeciesId;
        public float CurrentPopulation;
        public float MaxPopulation;
        public float GrowthRate;
        public System.DateTime LastUpdate;
    }

    [System.Serializable]
    public struct InteractionHistory
    {
        public uint Species1Id;
        public uint Species2Id;
        public Dictionary<Laboratory.Chimera.Ecosystem.Data.InteractionType, int> InteractionCounts;
        public Laboratory.Chimera.Ecosystem.Data.InteractionType LastInteractionType;
        public int TotalInteractions;
    }

    public enum SocialBehaviorType
    {
        Solitary,
        Pair,
        Pack,
        Herd,
        Colony,
        Swarm
    }
}