using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Chimera.Genetics.Advanced
{
    /// <summary>
    /// Advanced temporal genetics system that handles genetic memory, evolutionary pressure,
    /// and ancestral trait recovery for Project Chimera.
    ///
    /// Features:
    /// - Genetic Memory: Creatures can "remember" extinct traits from their lineage
    /// - Evolutionary Pressure Events: Environmental challenges that favor certain traits
    /// - Genetic Archaeology: Discovery of ancient DNA that introduces lost traits
    /// - Temporal Expression: Stress-induced expression of dormant ancestral genes
    /// </summary>
    public class TemporalGeneticSystem : MonoBehaviour
    {
        [Header("Temporal Genetics Configuration")]
        [SerializeField] private TemporalGeneticsConfig config;
        [SerializeField] private bool enableGeneticMemory = true;
        [SerializeField] private bool enableEvolutionaryPressure = true;
        [SerializeField] private bool enableGeneticArchaeology = true;

        [Header("Genetic Memory Settings")]
        [SerializeField] private int maxGenerationMemory = 10;
        [SerializeField] private float memoryDecayRate = 0.1f;
        [SerializeField] private float stressActivationThreshold = 0.7f;

        [Header("Evolutionary Pressure")]
        [SerializeField] private float pressureEventFrequency = 0.02f; // 2% chance per day
        [SerializeField] private float pressureIntensity = 0.3f;
        [SerializeField] private int pressureDuration = 30; // days

        [Header("Archaeological Discovery")]
        [SerializeField] private float ancientDNADiscoveryRate = 0.001f; // 0.1% chance per exploration
        [SerializeField] private int maxAncientTraitAge = 50; // generations

        // Active evolutionary pressures
        private List<EvolutionaryPressureEvent> activePressures = new List<EvolutionaryPressureEvent>();

        // Global genetic memory bank - stores traits from all lineages
        private Dictionary<string, AncestralGeneticRecord> globalGeneticMemory = new Dictionary<string, AncestralGeneticRecord>();

        // Ancient DNA discovered through archaeology
        private List<AncientDNAFragment> ancientDNABank = new List<AncientDNAFragment>();

        // Events
        public static event Action<EvolutionaryPressureEvent> OnEvolutionaryPressureBegin;
        public static event Action<EvolutionaryPressureEvent> OnEvolutionaryPressureEnd;
        public static event Action<AncientDNAFragment> OnAncientDNADiscovered;
        public static event Action<GeneticProfile, Gene> OnAncestralTraitActivated;

        void Start()
        {
            InitializeTemporalGenetics();
            InvokeRepeating(nameof(ProcessEvolutionaryPressure), 1f, 86400f); // Once per day
            InvokeRepeating(nameof(UpdateGeneticMemory), 10f, 3600f); // Once per hour
        }

        private void InitializeTemporalGenetics()
        {
            // Load any saved genetic memory data
            LoadGeneticMemoryData();

            // Initialize with some ancient DNA fragments for variety
            SeedAncientDNABank();

            Debug.Log("Temporal Genetics System initialized - Genetic memory and evolutionary pressure active");
        }

        #region Genetic Memory System

        /// <summary>
        /// Records genetic information for future memory retrieval
        /// </summary>
        public void RecordGeneticMemory(GeneticProfile profile, string lineageId)
        {
            if (!enableGeneticMemory || profile?.Genes == null) return;

            if (!globalGeneticMemory.ContainsKey(lineageId))
            {
                globalGeneticMemory[lineageId] = new AncestralGeneticRecord(lineageId);
            }

            var record = globalGeneticMemory[lineageId];
            record.RecordGeneration(profile, Time.time);

            // Clean up old memory beyond max generations
            record.CleanupOldGenerations(maxGenerationMemory);
        }

        /// <summary>
        /// Attempts to activate dormant ancestral traits under stress
        /// </summary>
        public bool TryActivateAncestralTrait(GeneticProfile profile, float stressLevel, string preferredTraitType = null)
        {
            if (!enableGeneticMemory || stressLevel < stressActivationThreshold) return false;

            var ancestralTraits = GetAccessibleAncestralTraits(profile.LineageId);
            if (ancestralTraits.Count == 0) return false;

            // Filter by preferred trait type if specified
            if (!string.IsNullOrEmpty(preferredTraitType))
            {
                ancestralTraits = ancestralTraits.Where(t => t.traitName.Contains(preferredTraitType)).ToList();
            }

            if (ancestralTraits.Count == 0) return false;

            // Select a random ancestral trait weighted by generation distance
            var selectedTrait = SelectWeightedAncestralTrait(ancestralTraits);

            // Activate the trait with some randomness
            if (UnityEngine.Random.value < CalculateActivationProbability(selectedTrait, stressLevel))
            {
                ActivateAncestralTrait(profile, selectedTrait);
                return true;
            }

            return false;
        }

        private List<Gene> GetAccessibleAncestralTraits(string lineageId)
        {
            var traits = new List<Gene>();

            if (globalGeneticMemory.TryGetValue(lineageId, out var record))
            {
                traits.AddRange(record.GetAllHistoricalGenes());
            }

            // Also check related lineages (cross-breeding memory)
            var relatedLineages = FindRelatedLineages(lineageId);
            foreach (var relatedId in relatedLineages)
            {
                if (globalGeneticMemory.TryGetValue(relatedId, out var relatedRecord))
                {
                    traits.AddRange(relatedRecord.GetAllHistoricalGenes());
                }
            }

            return traits.Distinct().ToList();
        }

        private Gene SelectWeightedAncestralTrait(List<Gene> traits)
        {
            if (traits.Count == 1) return traits[0];

            // Weight traits based on how recently they were expressed
            var weights = traits.Select(t => CalculateTraitWeight(t)).ToArray();
            var totalWeight = weights.Sum();

            var randomValue = UnityEngine.Random.value * totalWeight;
            var currentWeight = 0f;

            for (int i = 0; i < traits.Count; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                {
                    return traits[i];
                }
            }

            return traits[traits.Count - 1];
        }

        private float CalculateTraitWeight(Gene trait)
        {
            // More recent traits and beneficial mutations have higher weight
            float baseWeight = 1f;

            if (trait.isMutation && trait.isActive)
                baseWeight *= 1.5f;

            // Decay weight based on how long ago the trait was last seen
            var generationsSince = trait.mutationGeneration;
            baseWeight *= Mathf.Exp(-generationsSince * memoryDecayRate);

            return baseWeight;
        }

        private float CalculateActivationProbability(Gene trait, float stressLevel)
        {
            float baseProbability = 0.1f; // 10% base chance

            // Higher stress increases probability
            baseProbability *= stressLevel;

            // Beneficial traits more likely to activate
            if (trait.value.HasValue && trait.value.Value > 0.7f)
                baseProbability *= 1.5f;

            // Mutations have different activation rates
            if (trait.isMutation)
                baseProbability *= 0.7f; // Mutations slightly less likely

            return Mathf.Clamp01(baseProbability);
        }

        private void ActivateAncestralTrait(GeneticProfile profile, Gene ancestralTrait)
        {
            // Create a copy of the ancestral trait and add it to the profile
            var newGene = new Gene(ancestralTrait)
            {
                expression = GeneExpression.Enhanced, // Stress-activated traits are often over-expressed
                mutationGeneration = profile.Generation,
                isMutation = true // Mark as a reactivated ancestral trait
            };

            // Add to profile's gene list
            var genesList = profile.Genes.ToList();
            genesList.Add(newGene);

            // Use reflection to update the private genes field
            var genesField = typeof(GeneticProfile).GetField("genes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            genesField?.SetValue(profile, genesList.ToArray());

            Debug.Log($"Ancestral trait '{ancestralTrait.traitName}' activated due to stress in lineage {profile.LineageId}");
            OnAncestralTraitActivated?.Invoke(profile, newGene);
        }

        private List<string> FindRelatedLineages(string lineageId)
        {
            var related = new List<string>();

            // Look for lineages that share genetic material (cross-breeding)
            foreach (var kvp in globalGeneticMemory)
            {
                if (kvp.Key != lineageId && kvp.Key.Contains(lineageId))
                {
                    related.Add(kvp.Key);
                }
            }

            return related;
        }

        #endregion

        #region Evolutionary Pressure System

        private void ProcessEvolutionaryPressure()
        {
            if (!enableEvolutionaryPressure) return;

            // Update existing pressures
            UpdateActivePressures();

            // Chance to start new pressure event
            if (UnityEngine.Random.value < pressureEventFrequency)
            {
                StartRandomEvolutionaryPressure();
            }
        }

        private void UpdateActivePressures()
        {
            for (int i = activePressures.Count - 1; i >= 0; i--)
            {
                var pressure = activePressures[i];
                pressure.duration--;

                if (pressure.duration <= 0)
                {
                    EndEvolutionaryPressure(pressure);
                    activePressures.RemoveAt(i);
                }
                else
                {
                    activePressures[i] = pressure;
                }
            }
        }

        private void StartRandomEvolutionaryPressure()
        {
            var pressureTypes = Enum.GetValues(typeof(EvolutionaryPressureType)).Cast<EvolutionaryPressureType>().ToArray();
            var pressureType = pressureTypes[UnityEngine.Random.Range(0, pressureTypes.Length)];

            var pressure = new EvolutionaryPressureEvent
            {
                id = Guid.NewGuid().ToString(),
                type = pressureType,
                intensity = pressureIntensity + UnityEngine.Random.Range(-0.1f, 0.1f),
                duration = pressureDuration + UnityEngine.Random.Range(-10, 10),
                affectedBiomes = SelectRandomBiomes(),
                favoredTraits = GetFavoredTraitsForPressure(pressureType),
                startTime = Time.time
            };

            activePressures.Add(pressure);
            OnEvolutionaryPressureBegin?.Invoke(pressure);

            Debug.Log($"Evolutionary pressure '{pressureType}' began with intensity {pressure.intensity:F2}");
        }

        private BiomeType[] SelectRandomBiomes()
        {
            var allBiomes = Enum.GetValues(typeof(BiomeType)).Cast<BiomeType>().ToArray();
            var numBiomes = UnityEngine.Random.Range(1, Mathf.Min(3, allBiomes.Length));

            return allBiomes.OrderBy(x => UnityEngine.Random.value).Take(numBiomes).ToArray();
        }

        private string[] GetFavoredTraitsForPressure(EvolutionaryPressureType pressureType)
        {
            return pressureType switch
            {
                EvolutionaryPressureType.ClimateChange => new[] { "Heat Resistance", "Cold Resistance", "Adaptability" },
                EvolutionaryPressureType.ResourceScarcity => new[] { "Efficiency", "Foraging", "Conservation" },
                EvolutionaryPressureType.PredatorIncrease => new[] { "Speed", "Stealth", "Defensive", "Pack Behavior" },
                EvolutionaryPressureType.Disease => new[] { "Immunity", "Vitality", "Disease Resistance" },
                EvolutionaryPressureType.Pollution => new[] { "Toxin Resistance", "Purification", "Adaptability" },
                EvolutionaryPressureType.Overpopulation => new[] { "Competition", "Territorial", "Resource Sharing" },
                EvolutionaryPressureType.Isolation => new[] { "Self Sufficiency", "Longevity", "Independence" },
                _ => new[] { "Adaptability", "Resilience" }
            };
        }

        private void EndEvolutionaryPressure(EvolutionaryPressureEvent pressure)
        {
            OnEvolutionaryPressureEnd?.Invoke(pressure);
            Debug.Log($"Evolutionary pressure '{pressure.type}' ended after {pressureDuration - pressure.duration} days");
        }

        /// <summary>
        /// Gets the current evolutionary pressure modifier for a specific trait
        /// </summary>
        public float GetEvolutionaryPressureModifier(string traitName, BiomeType biome)
        {
            float totalModifier = 1f;

            foreach (var pressure in activePressures)
            {
                if (pressure.affectedBiomes.Contains(biome) && pressure.favoredTraits.Contains(traitName))
                {
                    totalModifier += pressure.intensity;
                }
            }

            return totalModifier;
        }

        /// <summary>
        /// Applies evolutionary pressure to breeding outcomes
        /// </summary>
        public void ApplyEvolutionaryPressure(GeneticProfile offspring, BiomeType environment)
        {
            if (!enableEvolutionaryPressure || activePressures.Count == 0) return;

            foreach (var gene in offspring.Genes)
            {
                var modifier = GetEvolutionaryPressureModifier(gene.traitName, environment);

                if (modifier > 1f && gene.value.HasValue)
                {
                    // Enhance favored traits
                    var enhancedValue = Mathf.Min(1f, gene.value.Value * modifier);

                    // Use reflection to update the gene value
                    var geneRef = (Gene)gene;
                    geneRef.value = enhancedValue;

                    if (modifier > 1.5f)
                    {
                        geneRef.expression = GeneExpression.Enhanced;
                    }
                }
            }
        }

        #endregion

        #region Genetic Archaeology System

        /// <summary>
        /// Attempt to discover ancient DNA during exploration
        /// </summary>
        public AncientDNAFragment TryDiscoverAncientDNA(Vector3 explorationLocation, BiomeType biome)
        {
            if (!enableGeneticArchaeology || UnityEngine.Random.value > ancientDNADiscoveryRate)
                return null;

            var fragment = GenerateAncientDNAFragment(explorationLocation, biome);
            ancientDNABank.Add(fragment);

            OnAncientDNADiscovered?.Invoke(fragment);
            Debug.Log($"Ancient DNA fragment '{fragment.traitName}' discovered at {explorationLocation}!");

            return fragment;
        }

        private AncientDNAFragment GenerateAncientDNAFragment(Vector3 location, BiomeType biome)
        {
            var ancientTraits = GetPossibleAncientTraits(biome);
            var selectedTrait = ancientTraits[UnityEngine.Random.Range(0, ancientTraits.Length)];

            return new AncientDNAFragment
            {
                id = Guid.NewGuid().ToString(),
                traitName = selectedTrait.traitName,
                gene = selectedTrait,
                discoveryLocation = location,
                estimatedAge = UnityEngine.Random.Range(10, maxAncientTraitAge),
                purity = UnityEngine.Random.Range(0.3f, 0.9f),
                discoveryTime = Time.time,
                biomeOrigin = biome
            };
        }

        private Gene[] GetPossibleAncientTraits(BiomeType biome)
        {
            // Return traits that would have been common in ancient times for this biome
            var traits = new List<Gene>();

            switch (biome)
            {
                case BiomeType.Forest:
                    traits.AddRange(CreateAncientTraits("Ancient Bark Skin", "Photosynthetic Boost", "Deep Root Network"));
                    break;
                case BiomeType.Desert:
                    traits.AddRange(CreateAncientTraits("Sand Camouflage", "Water Storage", "Heat Absorption"));
                    break;
                case BiomeType.Ocean:
                    traits.AddRange(CreateAncientTraits("Pressure Immunity", "Echolocation", "Bioluminescent Display"));
                    break;
                case BiomeType.Mountain:
                    traits.AddRange(CreateAncientTraits("Altitude Adaptation", "Rock Climbing", "Thin Air Breathing"));
                    break;
                case BiomeType.Arctic:
                    traits.AddRange(CreateAncientTraits("Antifreeze Blood", "Hibernation", "Thick Blubber"));
                    break;
                default:
                    traits.AddRange(CreateAncientTraits("Ancient Wisdom", "Longevity", "Primal Instincts"));
                    break;
            }

            return traits.ToArray();
        }

        private Gene[] CreateAncientTraits(params string[] traitNames)
        {
            return traitNames.Select(name => new Gene
            {
                geneId = $"ANCIENT_{name.Replace(" ", "_").ToUpper()}",
                traitName = name,
                traitType = TraitType.Physical,
                value = UnityEngine.Random.Range(0.6f, 0.95f), // Ancient traits were often strong
                dominance = UnityEngine.Random.Range(0.4f, 0.8f),
                expression = GeneExpression.Normal,
                isActive = true,
                isMutation = false
            }).ToArray();
        }

        /// <summary>
        /// Attempts to integrate ancient DNA into a genetic profile
        /// </summary>
        public bool TryIntegrateAncientDNA(GeneticProfile profile, AncientDNAFragment fragment)
        {
            if (fragment.purity < 0.5f)
            {
                Debug.Log("Ancient DNA fragment too degraded for integration");
                return false;
            }

            // Integration success based on purity and genetic compatibility
            float integrationChance = fragment.purity * 0.8f; // Max 80% chance

            if (UnityEngine.Random.value < integrationChance)
            {
                var ancientGene = new Gene(fragment.gene)
                {
                    expression = GeneExpression.Enhanced, // Ancient traits often express strongly
                    isMutation = true,
                    mutationGeneration = profile.Generation
                };

                // Add to profile
                var genesList = profile.Genes.ToList();
                genesList.Add(ancientGene);

                var genesField = typeof(GeneticProfile).GetField("genes",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                genesField?.SetValue(profile, genesList.ToArray());

                Debug.Log($"Successfully integrated ancient trait '{fragment.traitName}' into genetic profile");
                return true;
            }

            Debug.Log($"Failed to integrate ancient DNA fragment '{fragment.traitName}'");
            return false;
        }

        private void SeedAncientDNABank()
        {
            // Create some initial ancient DNA fragments for variety
            var biomes = Enum.GetValues(typeof(BiomeType)).Cast<BiomeType>().ToArray();

            for (int i = 0; i < 10; i++)
            {
                var randomBiome = biomes[UnityEngine.Random.Range(0, biomes.Length)];
                var randomLocation = new Vector3(
                    UnityEngine.Random.Range(-1000f, 1000f),
                    UnityEngine.Random.Range(0f, 100f),
                    UnityEngine.Random.Range(-1000f, 1000f)
                );

                var fragment = GenerateAncientDNAFragment(randomLocation, randomBiome);
                ancientDNABank.Add(fragment);
            }
        }

        #endregion

        #region Utility Methods

        private void UpdateGeneticMemory()
        {
            // Periodic cleanup and optimization of genetic memory
            foreach (var record in globalGeneticMemory.Values)
            {
                record.CleanupOldGenerations(maxGenerationMemory);
            }
        }

        private void LoadGeneticMemoryData()
        {
            // Load saved genetic memory from persistent storage
            // Implementation would depend on save system
        }

        private void SaveGeneticMemoryData()
        {
            // Save genetic memory to persistent storage
            // Implementation would depend on save system
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveGeneticMemoryData();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveGeneticMemoryData();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets all currently active evolutionary pressures
        /// </summary>
        public IReadOnlyList<EvolutionaryPressureEvent> GetActivePressures() => activePressures.AsReadOnly();

        /// <summary>
        /// Gets all discovered ancient DNA fragments
        /// </summary>
        public IReadOnlyList<AncientDNAFragment> GetAncientDNABank() => ancientDNABank.AsReadOnly();

        /// <summary>
        /// Force start a specific evolutionary pressure (for testing/events)
        /// </summary>
        public void ForceEvolutionaryPressure(EvolutionaryPressureType type, float intensity, int duration, BiomeType[] biomes)
        {
            var pressure = new EvolutionaryPressureEvent
            {
                id = Guid.NewGuid().ToString(),
                type = type,
                intensity = intensity,
                duration = duration,
                affectedBiomes = biomes,
                favoredTraits = GetFavoredTraitsForPressure(type),
                startTime = Time.time
            };

            activePressures.Add(pressure);
            OnEvolutionaryPressureBegin?.Invoke(pressure);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents an evolutionary pressure event affecting the ecosystem
    /// </summary>
    [Serializable]
    public struct EvolutionaryPressureEvent
    {
        public string id;
        public EvolutionaryPressureType type;
        public float intensity; // 0-1 scale
        public int duration; // in game days
        public BiomeType[] affectedBiomes;
        public string[] favoredTraits;
        public float startTime;
        public string description => GetPressureDescription();

        private string GetPressureDescription()
        {
            return type switch
            {
                EvolutionaryPressureType.ClimateChange => "Rising temperatures and changing weather patterns stress ecosystems",
                EvolutionaryPressureType.ResourceScarcity => "Food and water become increasingly scarce",
                EvolutionaryPressureType.PredatorIncrease => "Predator populations surge, increasing survival pressure",
                EvolutionaryPressureType.Disease => "A mysterious disease spreads through creature populations",
                EvolutionaryPressureType.Pollution => "Environmental toxins threaten creature health",
                EvolutionaryPressureType.Overpopulation => "Overcrowding leads to intense competition for resources",
                EvolutionaryPressureType.Isolation => "Populations become isolated, forcing self-reliance",
                _ => "Unknown evolutionary pressure affects the ecosystem"
            };
        }
    }

    /// <summary>
    /// Types of evolutionary pressure that can affect creature populations
    /// </summary>
    public enum EvolutionaryPressureType
    {
        ClimateChange,
        ResourceScarcity,
        PredatorIncrease,
        Disease,
        Pollution,
        Overpopulation,
        Isolation
    }

    /// <summary>
    /// Records genetic information across generations for memory system
    /// </summary>
    [Serializable]
    public class AncestralGeneticRecord
    {
        public string lineageId;
        public List<GenerationalGeneticSnapshot> generations = new List<GenerationalGeneticSnapshot>();

        public AncestralGeneticRecord(string id)
        {
            lineageId = id;
        }

        public void RecordGeneration(GeneticProfile profile, float timestamp)
        {
            var snapshot = new GenerationalGeneticSnapshot
            {
                generation = profile.Generation,
                genes = profile.Genes.ToArray(),
                timestamp = timestamp,
                mutations = profile.Mutations.ToArray()
            };

            generations.Add(snapshot);
        }

        public void CleanupOldGenerations(int maxGenerations)
        {
            if (generations.Count > maxGenerations)
            {
                generations = generations.OrderByDescending(g => g.generation).Take(maxGenerations).ToList();
            }
        }

        public List<Gene> GetAllHistoricalGenes()
        {
            return generations.SelectMany(g => g.genes).Distinct().ToList();
        }
    }

    /// <summary>
    /// Snapshot of genetic information from a specific generation
    /// </summary>
    [Serializable]
    public struct GenerationalGeneticSnapshot
    {
        public int generation;
        public Gene[] genes;
        public Mutation[] mutations;
        public float timestamp;
    }

    /// <summary>
    /// Represents ancient DNA discovered through exploration
    /// </summary>
    [Serializable]
    public struct AncientDNAFragment
    {
        public string id;
        public string traitName;
        public Gene gene;
        public Vector3 discoveryLocation;
        public int estimatedAge; // in generations
        public float purity; // 0-1, affects integration success
        public float discoveryTime;
        public BiomeType biomeOrigin;

        public string description => $"Ancient {traitName} gene fragment ({estimatedAge} generations old, {purity:P0} purity)";
    }

    #endregion
}