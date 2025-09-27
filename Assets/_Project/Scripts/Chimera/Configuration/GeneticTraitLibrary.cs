using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;

namespace Laboratory.Chimera.Configuration
{
    /// <summary>
    /// Types of genetic traits
    /// </summary>
    public enum TraitType
    {
        Physical,   // Size, strength, agility
        Mental,     // Intelligence, memory
        Behavioral, // Aggression, curiosity
        Social,     // Pack behavior, loyalty
        Cosmetic,   // Color, patterns
        Special,    // Magical abilities
        Combat,     // Fighting abilities
        Utility,    // Special skills
        Sensory,    // Vision, hearing, smell
        Metabolic   // Digestion, energy, healing
    }
    /// <summary>
    /// Central library for genetic traits and their properties
    /// Manages trait definitions, inheritance patterns, and mutation rules
    /// </summary>
    [CreateAssetMenu(fileName = "GeneticTraitLibrary", menuName = "Chimera/Genetic Trait Library")]
    public class GeneticTraitLibrary : ScriptableObject
    {
        [Header("Core Traits")]
        [SerializeField] private TraitDefinition[] coreTraits;
        
        [Header("Physical Traits")]
        [SerializeField] private TraitDefinition[] physicalTraits;
        
        [Header("Behavioral Traits")]
        [SerializeField] private TraitDefinition[] behavioralTraits;
        
        [Header("Special Traits")]
        [SerializeField] private TraitDefinition[] specialTraits;
        
        [Header("Mutation Settings")]
        [SerializeField] private float baseMutationRate = 0.05f;
        [SerializeField] private float rarityMutationModifier = 2f;
        [SerializeField] private int maxMutationsPerGeneration = 3;
        
        // Cached collections for performance
        private Dictionary<string, TraitDefinition> traitLookup;
        private Dictionary<TraitType, TraitDefinition[]> traitsByType;
        private bool isInitialized = false;
        
        #region Initialization
        
        private void OnEnable()
        {
            InitializeLibrary();
        }
        
        private void InitializeLibrary()
        {
            if (isInitialized) return;
            
            BuildTraitLookup();
            BuildTraitsByType();
            ValidateTraits();
            
            isInitialized = true;
        }
        
        private void BuildTraitLookup()
        {
            traitLookup = new Dictionary<string, TraitDefinition>();
            
            AddTraitsToLookup(coreTraits);
            AddTraitsToLookup(physicalTraits);
            AddTraitsToLookup(behavioralTraits);
            AddTraitsToLookup(specialTraits);
        }
        
        private void AddTraitsToLookup(TraitDefinition[] traits)
        {
            if (traits == null) return;
            
            foreach (var trait in traits)
            {
                if (trait != null && !string.IsNullOrEmpty(trait.traitName))
                {
                    traitLookup[trait.traitName] = trait;
                }
            }
        }
        
        private void BuildTraitsByType()
        {
            traitsByType = new Dictionary<TraitType, TraitDefinition[]>();
            
            foreach (TraitType type in Enum.GetValues(typeof(TraitType)))
            {
                var traitsOfType = GetAllTraits().Where(t => t.traitType == type).ToArray();
                traitsByType[type] = traitsOfType;
            }
        }
        
        private void ValidateTraits()
        {
            var allTraits = GetAllTraits();
            var duplicateNames = allTraits.GroupBy(t => t.traitName)
                                         .Where(g => g.Count() > 1)
                                         .Select(g => g.Key);
            
            foreach (var duplicateName in duplicateNames)
            {
                UnityEngine.Debug.LogWarning($"[GeneticTraitLibrary] Duplicate trait name found: {duplicateName}");
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Get a trait definition by name
        /// </summary>
        public TraitDefinition GetTrait(string traitName)
        {
            InitializeLibrary();
            return traitLookup.TryGetValue(traitName, out var trait) ? trait : null;
        }
        
        /// <summary>
        /// Get all traits of a specific type
        /// </summary>
        public TraitDefinition[] GetTraitsByType(TraitType type)
        {
            InitializeLibrary();
            return traitsByType.TryGetValue(type, out var traits) ? traits : Array.Empty<TraitDefinition>();
        }
        
        /// <summary>
        /// Get all available traits
        /// </summary>
        public TraitDefinition[] GetAllTraits()
        {
            var allTraits = new List<TraitDefinition>();
            
            if (coreTraits != null) allTraits.AddRange(coreTraits);
            if (physicalTraits != null) allTraits.AddRange(physicalTraits);
            if (behavioralTraits != null) allTraits.AddRange(behavioralTraits);
            if (specialTraits != null) allTraits.AddRange(specialTraits);
            
            return allTraits.Where(t => t != null).ToArray();
        }
        
        /// <summary>
        /// Generate a random trait for mutation
        /// </summary>
        public TraitDefinition GetRandomTrait(RarityLevel maxRarity = RarityLevel.Legendary)
        {
            var availableTraits = GetAllTraits()
                .Where(t => t.rarity <= maxRarity)
                .ToArray();
            
            if (availableTraits.Length == 0) return null;
            
            // Weight by rarity (rarer traits have lower probability)
            var weights = availableTraits.Select(t => GetRarityWeight(t.rarity)).ToArray();
            return availableTraits[GetWeightedRandomIndex(weights)];
        }
        
        /// <summary>
        /// Generate a random trait of a specific type
        /// </summary>
        public TraitDefinition GetRandomTraitOfType(TraitType type, RarityLevel maxRarity = RarityLevel.Legendary)
        {
            var availableTraits = GetTraitsByType(type)
                .Where(t => t.rarity <= maxRarity)
                .ToArray();
            
            if (availableTraits.Length == 0) return null;
            
            var weights = availableTraits.Select(t => GetRarityWeight(t.rarity)).ToArray();
            return availableTraits[GetWeightedRandomIndex(weights)];
        }
        
        /// <summary>
        /// Calculate mutation probability for a genetic profile
        /// </summary>
        public float CalculateMutationProbability(GeneticProfile genetics)
        {
            float probability = baseMutationRate;
            
            if (genetics != null)
            {
                // Higher generation = slightly higher mutation rate
                probability += genetics.Generation * 0.005f;
                
                // Rare traits increase mutation potential using the rarity modifier
                var rareTraitCount = genetics.Genes?.Count(g => g.isActive && GetTrait(g.traitName)?.rarity >= RarityLevel.Rare) ?? 0;
                probability += rareTraitCount * 0.01f * rarityMutationModifier;
            }
            
            return Mathf.Clamp01(probability);
        }
        
        /// <summary>
        /// Generate mutations for offspring
        /// </summary>
        public Gene[] GenerateMutations(GeneticProfile parent1, GeneticProfile parent2)
        {
            var mutations = new List<Gene>();
            
            float mutationProbability = (CalculateMutationProbability(parent1) + CalculateMutationProbability(parent2)) * 0.5f;
            
            int maxMutations = Mathf.Min(maxMutationsPerGeneration, 
                                       Mathf.CeilToInt(mutationProbability * 10f));
            
            for (int i = 0; i < maxMutations; i++)
            {
                if (UnityEngine.Random.value < mutationProbability)
                {
                    var mutationTrait = GetRandomTrait(RarityLevel.Epic);
                    if (mutationTrait != null)
                    {
                        var mutationGene = GenerateMutationGene(mutationTrait);
                        mutations.Add(mutationGene);
                    }
                }
            }
            
            return mutations.ToArray();
        }
        
        /// <summary>
        /// Check if two traits are compatible for inheritance
        /// </summary>
        public bool AreTraitsCompatible(string trait1Name, string trait2Name)
        {
            var trait1 = GetTrait(trait1Name);
            var trait2 = GetTrait(trait2Name);

            if (trait1 == null || trait2 == null) return true;

            // Same trait type is always compatible
            if (trait1.traitType == trait2.traitType) return true;

            // Check for conflicting traits
            return !trait1.conflictingTraits.Contains(trait2Name) &&
                   !trait2.conflictingTraits.Contains(trait1Name);
        }

        /// <summary>
        /// Create a rich genetic profile with diverse traits suitable for the given biome
        /// </summary>
        public GeneticProfile CreateRichGeneticProfile(BiomeType biome, int generation = 1)
        {
            var genes = new List<Gene>();

            // Add core traits first
            genes.AddRange(CreateCoreGenes(biome));

            // Add biome-specific traits
            genes.AddRange(CreateBiomeSpecificGenes(biome));

            // Add some random special traits
            genes.AddRange(CreateRandomSpecialGenes());

            return new GeneticProfile(genes.ToArray(), generation);
        }

        /// <summary>
        /// Apply random mutations to a genetic profile
        /// </summary>
        public void ApplyRandomMutations(GeneticProfile genetics)
        {
            if (genetics?.Genes == null) return;

            // Calculate mutation probability
            float mutationChance = CalculateMutationProbability(genetics);

            // Use reflection to access the private genes field
            var genesField = typeof(GeneticProfile).GetField("genes",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (genesField != null)
            {
                var genesArray = (Gene[])genesField.GetValue(genetics);
                if (genesArray != null)
                {
                    // Try to mutate each gene
                    for (int i = 0; i < genesArray.Length; i++)
                    {
                        if (UnityEngine.Random.value < mutationChance)
                        {
                            var gene = genesArray[i];
                            // Mutate the gene value slightly
                            float mutationStrength = UnityEngine.Random.Range(-0.2f, 0.2f);
                            float currentValue = gene.value ?? 0.5f; // Handle nullable value
                            gene.value = Mathf.Clamp01(currentValue + mutationStrength);

                            // Mark as mutation if it's significant
                            if (Mathf.Abs(mutationStrength) > 0.1f)
                            {
                                gene.isMutation = true;
                                gene.mutationGeneration = genetics.Generation;
                            }

                            genesArray[i] = gene; // Update the gene in the array
                        }
                    }
                }
            }

            // Occasionally trigger an environmental mutation
            if (UnityEngine.Random.value < mutationChance * 0.3f)
            {
                var newTrait = GetRandomTrait(RarityLevel.Rare);
                if (newTrait != null)
                {
                    // Use the existing TriggerMutation method
                    genetics.TriggerMutation(newTrait.traitName);
                    UnityEngine.Debug.Log($"Triggered environmental mutation: {newTrait.traitName}");
                }
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private float GetRarityWeight(RarityLevel rarity)
        {
            return rarity switch
            {
                RarityLevel.Common => 1.0f,
                RarityLevel.Uncommon => 0.7f,
                RarityLevel.Rare => 0.4f,
                RarityLevel.Epic => 0.2f,
                RarityLevel.Legendary => 0.05f,
                _ => 1.0f
            };
        }
        
        private int GetWeightedRandomIndex(float[] weights)
        {
            float totalWeight = weights.Sum();
            float randomValue = UnityEngine.Random.value * totalWeight;
            
            float currentWeight = 0f;
            for (int i = 0; i < weights.Length; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                    return i;
            }
            
            return weights.Length - 1; // Fallback
        }
        
        private Gene GenerateMutationGene(TraitDefinition traitDef)
        {
            return new Gene
            {
                traitName = traitDef.traitName,
                value = UnityEngine.Random.Range(traitDef.minValue, traitDef.maxValue),
                dominance = UnityEngine.Random.Range(0f, 1f),
                isActive = true,
                isMutation = true,
                mutationGeneration = 1
            };
        }

        private Gene[] CreateCoreGenes(BiomeType biome)
        {
            var coreGenes = new List<Gene>();

            // Basic physical traits
            coreGenes.Add(CreateGene("Strength", UnityEngine.Random.Range(0.3f, 0.8f)));
            coreGenes.Add(CreateGene("Agility", UnityEngine.Random.Range(0.3f, 0.8f)));
            coreGenes.Add(CreateGene("Intelligence", UnityEngine.Random.Range(0.2f, 0.9f)));
            coreGenes.Add(CreateGene("Constitution", UnityEngine.Random.Range(0.4f, 0.9f)));

            // Basic colors
            coreGenes.Add(CreateGene("PrimaryColor", UnityEngine.Random.value));
            coreGenes.Add(CreateGene("SecondaryColor", UnityEngine.Random.value));

            return coreGenes.ToArray();
        }

        private Gene[] CreateBiomeSpecificGenes(BiomeType biome)
        {
            var biomeGenes = new List<Gene>();

            switch (biome)
            {
                case BiomeType.Arctic:
                    biomeGenes.Add(CreateGene("ColdResistance", UnityEngine.Random.Range(0.7f, 1.0f)));
                    biomeGenes.Add(CreateGene("ThickFur", UnityEngine.Random.Range(0.6f, 1.0f)));
                    break;

                case BiomeType.Desert:
                    biomeGenes.Add(CreateGene("HeatResistance", UnityEngine.Random.Range(0.7f, 1.0f)));
                    biomeGenes.Add(CreateGene("WaterConservation", UnityEngine.Random.Range(0.6f, 1.0f)));
                    break;

                case BiomeType.Forest:
                    biomeGenes.Add(CreateGene("Camouflage", UnityEngine.Random.Range(0.5f, 0.8f)));
                    biomeGenes.Add(CreateGene("Climbing", UnityEngine.Random.Range(0.4f, 0.9f)));
                    break;

                case BiomeType.Ocean:
                    biomeGenes.Add(CreateGene("Swimming", UnityEngine.Random.Range(0.8f, 1.0f)));
                    biomeGenes.Add(CreateGene("WaterBreathing", UnityEngine.Random.Range(0.7f, 1.0f)));
                    break;

                case BiomeType.Mountain:
                    biomeGenes.Add(CreateGene("HighAltitudeAdaptation", UnityEngine.Random.Range(0.6f, 0.9f)));
                    biomeGenes.Add(CreateGene("RockClimbing", UnityEngine.Random.Range(0.5f, 0.8f)));
                    break;

                default: // Temperate
                    biomeGenes.Add(CreateGene("Adaptability", UnityEngine.Random.Range(0.5f, 0.8f)));
                    biomeGenes.Add(CreateGene("Versatility", UnityEngine.Random.Range(0.4f, 0.7f)));
                    break;
            }

            return biomeGenes.ToArray();
        }

        private Gene[] CreateRandomSpecialGenes()
        {
            var specialGenes = new List<Gene>();

            // Add a few random behavioral traits
            var behavioralTraits = new[] { "Aggression", "Curiosity", "Loyalty", "Playfulness" };
            for (int i = 0; i < 2; i++)
            {
                var trait = behavioralTraits[UnityEngine.Random.Range(0, behavioralTraits.Length)];
                specialGenes.Add(CreateGene(trait, UnityEngine.Random.Range(0.2f, 0.8f)));
            }

            // Occasionally add a rare trait
            if (UnityEngine.Random.value < 0.3f)
            {
                var rareTrait = GetRandomTrait(RarityLevel.Rare);
                if (rareTrait != null)
                {
                    specialGenes.Add(CreateGene(rareTrait.traitName, UnityEngine.Random.Range(rareTrait.minValue, rareTrait.maxValue)));
                }
            }

            return specialGenes.ToArray();
        }

        private Gene CreateGene(string traitName, float value)
        {
            return new Gene
            {
                traitName = traitName,
                value = Mathf.Clamp01(value),
                dominance = UnityEngine.Random.Range(0.3f, 0.7f),
                isActive = true,
                isMutation = false,
                mutationGeneration = 0
            };
        }
        
        #endregion
        
        #region Default Trait Definitions
        
        /// <summary>
        /// Create default trait library for testing
        /// </summary>
        [ContextMenu("Generate Default Traits")]
        private void GenerateDefaultTraits()
        {
            coreTraits = new TraitDefinition[]
            {
                CreateTraitDefinition("Strength", TraitType.Physical, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Agility", TraitType.Physical, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Intelligence", TraitType.Mental, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Constitution", TraitType.Physical, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Charisma", TraitType.Social, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Perception", TraitType.Mental, RarityLevel.Common, 0f, 1f)
            };
            
            physicalTraits = new TraitDefinition[]
            {
                CreateTraitDefinition("Size", TraitType.Physical, RarityLevel.Common, 0.5f, 2f),
                CreateTraitDefinition("Speed", TraitType.Physical, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Endurance", TraitType.Physical, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("PrimaryColor", TraitType.Cosmetic, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("SecondaryColor", TraitType.Cosmetic, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Pattern", TraitType.Cosmetic, RarityLevel.Uncommon, 0f, 1f)
            };
            
            behavioralTraits = new TraitDefinition[]
            {
                CreateTraitDefinition("Aggression", TraitType.Behavioral, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Curiosity", TraitType.Behavioral, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Loyalty", TraitType.Social, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Independence", TraitType.Behavioral, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Playfulness", TraitType.Social, RarityLevel.Common, 0f, 1f),
                CreateTraitDefinition("Territorial", TraitType.Behavioral, RarityLevel.Uncommon, 0f, 1f)
            };
            
            specialTraits = new TraitDefinition[]
            {
                CreateTraitDefinition("Regeneration", TraitType.Special, RarityLevel.Rare, 0f, 1f),
                CreateTraitDefinition("Camouflage", TraitType.Special, RarityLevel.Rare, 0f, 1f),
                CreateTraitDefinition("Telepathy", TraitType.Special, RarityLevel.Epic, 0f, 1f),
                CreateTraitDefinition("ElementalAffinity", TraitType.Special, RarityLevel.Epic, 0f, 1f),
                CreateTraitDefinition("TimeManipulation", TraitType.Special, RarityLevel.Legendary, 0f, 1f)
            };
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
        
        private TraitDefinition CreateTraitDefinition(string name, TraitType type, RarityLevel rarity, float min, float max)
        {
            return new TraitDefinition
            {
                traitName = name,
                traitType = type,
                rarity = rarity,
                minValue = min,
                maxValue = max,
                inheritanceProbability = 0.5f,
                description = $"Auto-generated {name} trait",
                conflictingTraits = Array.Empty<string>()
            };
        }
        
        #endregion
    }
    
    /// <summary>
    /// Definition for a genetic trait
    /// </summary>
    [Serializable]
    public class TraitDefinition
    {
        [Header("Basic Information")]
        public string traitName;
        public TraitType traitType;
        public RarityLevel rarity;
        
        [Header("Value Range")]
        public float minValue;
        public float maxValue;
        
        [Header("Inheritance")]
        [Range(0f, 1f)]
        public float inheritanceProbability = 0.5f;
        [Range(0f, 1f)]
        public float dominanceModifier = 1f;
        
        [Header("Description")]
        [TextArea(2, 4)]
        public string description;
        
        [Header("Compatibility")]
        public string[] conflictingTraits = Array.Empty<string>();
        public string[] synergisticTraits = Array.Empty<string>();
        
        public bool IsValidValue(float value)
        {
            return value >= minValue && value <= maxValue;
        }
        
        public float ClampValue(float value)
        {
            return Mathf.Clamp(value, minValue, maxValue);
        }
    }
}
