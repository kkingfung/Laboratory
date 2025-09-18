using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Advanced genetic system for Project Chimera creature breeding.
    /// Implements Mendelian inheritance with dominant/recessive traits,
    /// mutations, environmental adaptation, and emergent trait combinations.
    /// </summary>
    [Serializable]
    public class GeneticProfile
    {
        [SerializeField] private Gene[] genes = Array.Empty<Gene>();
        [SerializeField] private Mutation[] mutations = Array.Empty<Mutation>();
        [SerializeField] private float mutationRate = 0.02f; // 2% chance per gene
        [SerializeField] private int generationNumber = 1;
        [SerializeField] private string lineageId = "";
        
        /// <summary>
        /// All genes in this genetic profile
        /// </summary>
        public IReadOnlyList<Gene> Genes => genes;
        
        /// <summary>
        /// Active mutations in this profile
        /// </summary>
        public IReadOnlyList<Mutation> Mutations => mutations;
        
        /// <summary>
        /// Generation number (1 = wild, 2+ = bred)
        /// </summary>
        public int Generation => generationNumber;
        
        /// <summary>
        /// Unique lineage identifier for tracking breeding lines
        /// </summary>
        public string LineageId => lineageId;
        
        public GeneticProfile()
        {
            lineageId = Guid.NewGuid().ToString("N")[..8];
        }
        
        public GeneticProfile(Gene[] initialGenes, int generation = 1, string parentLineage = "")
        {
            genes = initialGenes ?? Array.Empty<Gene>();
            generationNumber = generation;
            lineageId = string.IsNullOrEmpty(parentLineage) ? 
                Guid.NewGuid().ToString("N")[..8] : 
                $"{parentLineage}-{generation:D2}";
        }
        
        /// <summary>
        /// Creates offspring genetics from two parent profiles
        /// </summary>
        public static GeneticProfile CreateOffspring(GeneticProfile parent1, GeneticProfile parent2, 
            EnvironmentalFactors environment = null)
        {
            if (parent1 == null || parent2 == null)
                throw new ArgumentNullException("Parent genetic profiles cannot be null");
            
            var offspring = new GeneticProfile();
            offspring.generationNumber = Math.Max(parent1.generationNumber, parent2.generationNumber) + 1;
            offspring.lineageId = $"{parent1.lineageId}x{parent2.lineageId}";
            
            // Combine gene pools from both parents
            var allParentGenes = parent1.genes.Concat(parent2.genes)
                .GroupBy(g => g.traitName)
                .ToList();
            
            var offspringGenes = new List<Gene>();
            
            foreach (var geneGroup in allParentGenes)
            {
                var parentGenes = geneGroup.ToArray();
                var inheritedGene = InheritGene(parentGenes, environment);
                
                // Apply potential mutations
                if (Random.value < offspring.mutationRate)
                {
                    inheritedGene = ApplyMutation(inheritedGene, offspring);
                }
                
                offspringGenes.Add(inheritedGene);
            }
            
            offspring.genes = offspringGenes.ToArray();
            
            // Handle special inheritance patterns
            offspring.ProcessSpecialInheritance(parent1, parent2);
            
            return offspring;
        }
        
        /// <summary>
        /// Determines which version of a gene is inherited
        /// </summary>
        private static Gene InheritGene(Gene[] parentGenes, EnvironmentalFactors environment)
        {
            if (parentGenes.Length == 1)
                return parentGenes[0]; // Only one parent has this gene
            
            // Standard Mendelian inheritance with environmental influence
            var gene1 = parentGenes[0];
            var gene2 = parentGenes[parentGenes.Length > 1 ? 1 : 0];
            
            // Environmental pressure can influence inheritance probability
            float environmentalBias = environment?.GetTraitBias(gene1.traitName) ?? 0f;
            
            // Dominant genes have higher inheritance chance
            float gene1Chance = gene1.dominance + environmentalBias;
            float gene2Chance = gene2.dominance - environmentalBias;
            
            float totalChance = gene1Chance + gene2Chance;
            float randomValue = Random.value * totalChance;
            
            if (randomValue < gene1Chance)
            {
                return CreateHybridGene(gene1, gene2);
            }
            else
            {
                return CreateHybridGene(gene2, gene1);
            }
        }
        
        /// <summary>
        /// Creates a new gene that may blend traits from both parents
        /// </summary>
        private static Gene CreateHybridGene(Gene dominantGene, Gene recessiveGene)
        {
            var hybridGene = new Gene
            {
                traitName = dominantGene.traitName,
                traitType = dominantGene.traitType,
                dominance = (dominantGene.dominance + recessiveGene.dominance) * 0.5f,
                expression = dominantGene.expression,
                isActive = dominantGene.isActive
            };
            
            // Blend numerical values based on dominance
            float blendFactor = dominantGene.dominance / (dominantGene.dominance + recessiveGene.dominance);
            
            if (dominantGene.value.HasValue && recessiveGene.value.HasValue)
            {
                hybridGene.value = Mathf.Lerp(recessiveGene.value.Value, dominantGene.value.Value, blendFactor);
            }
            else
            {
                hybridGene.value = dominantGene.value ?? recessiveGene.value;
            }
            
            return hybridGene;
        }
        
        /// <summary>
        /// Applies a random mutation to a gene
        /// </summary>
        private static Gene ApplyMutation(Gene originalGene, GeneticProfile offspring)
        {
            var mutatedGene = new Gene(originalGene);
            
            // Determine mutation type
            var mutationType = (MutationType)Random.Range(0, Enum.GetValues(typeof(MutationType)).Length);
            
            var mutation = new Mutation
            {
                mutationType = mutationType,
                affectedTrait = originalGene.traitName,
                severity = Random.Range(0.1f, 0.3f), // Mild to moderate mutations
                generation = offspring.generationNumber,
                isHarmful = Random.value < 0.3f // 30% chance of harmful mutation
            };
            
            // Apply mutation effect
            switch (mutationType)
            {
                case MutationType.ValueShift:
                    if (mutatedGene.value.HasValue)
                    {
                        float change = Random.Range(-mutation.severity, mutation.severity);
                        mutatedGene.value = Mathf.Clamp01(mutatedGene.value.Value + change);
                    }
                    break;
                    
                case MutationType.DominanceChange:
                    float dominanceChange = Random.Range(-0.2f, 0.2f);
                    mutatedGene.dominance = Mathf.Clamp01(mutatedGene.dominance + dominanceChange);
                    break;
                    
                case MutationType.ExpressionChange:
                    mutatedGene.expression = Random.value > 0.5f ? 
                        GeneExpression.Enhanced : GeneExpression.Suppressed;
                    break;
                    
                case MutationType.NewTrait:
                    // This would create entirely new traits - very rare
                    mutation.severity *= 0.1f; // Much rarer
                    break;
            }
            
            // Add mutation to offspring's mutation list
            var mutationsList = offspring.mutations.ToList();
            mutationsList.Add(mutation);
            offspring.mutations = mutationsList.ToArray();
            
            return mutatedGene;
        }
        
        /// <summary>
        /// Handles special inheritance patterns like sex-linked traits, polygenic traits, etc.
        /// </summary>
        private void ProcessSpecialInheritance(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Handle sex-linked traits
            ProcessSexLinkedInheritance(parent1, parent2);
            
            // Handle polygenic traits (traits controlled by multiple genes)
            ProcessPolygenicTraits();
            
            // Handle epistatic interactions (genes affecting other genes)
            ProcessEpistaticInteractions();
        }
        
        private void ProcessSexLinkedInheritance(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Implementation for sex-linked traits would go here
            // For now, we'll skip this as it requires creature sex determination
        }
        
        private void ProcessPolygenicTraits()
        {
            // Combine multiple genes that control the same trait
            var traitGroups = genes.GroupBy(g => g.traitType).ToList();
            
            foreach (var group in traitGroups)
            {
                if (group.Count() > 1)
                {
                    // Multiple genes affecting same trait - blend their effects
                    var genesInGroup = group.ToArray();
                    for (int i = 0; i < genesInGroup.Length; i++)
                    {
                        var gene = genesInGroup[i];
                        if (gene.value.HasValue)
                        {
                            // Polygenic traits show additive effects
                            gene.value *= 0.8f; // Reduce individual impact
                        }
                    }
                }
            }
        }
        
        private void ProcessEpistaticInteractions()
        {
            // Handle genes that modify the expression of other genes
            var modifierGenes = genes.Where(g => g.traitName.Contains("Modifier")).ToArray();
            
            foreach (var modifier in modifierGenes)
            {
                if (modifier.isActive && modifier.value.HasValue)
                {
                    // Apply modifier effects to other genes
                    for (int i = 0; i < genes.Length; i++)
                    {
                        if (genes[i] != modifier && genes[i].value.HasValue)
                        {
                            float modifierEffect = (modifier.value.Value - 0.5f) * 0.2f; // ±10% max
                            genes[i].value = Mathf.Clamp01(genes[i].value.Value + modifierEffect);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Applies genetic modifiers to creature stats
        /// </summary>
        public Creatures.CreatureStats ApplyModifiers(Creatures.CreatureStats baseStats)
        {
            var modifiedStats = baseStats;
            
            foreach (var gene in genes.Where(g => g.isActive))
            {
                if (!gene.value.HasValue) continue;
                
                float modifier = CalculateGeneModifier(gene);
                
                switch (gene.traitName)
                {
                    case "Strength":
                        modifiedStats.attack = Mathf.RoundToInt(modifiedStats.attack * modifier);
                        break;
                    case "Vitality":
                        modifiedStats.health = Mathf.RoundToInt(modifiedStats.health * modifier);
                        break;
                    case "Agility":
                        modifiedStats.speed = Mathf.RoundToInt(modifiedStats.speed * modifier);
                        break;
                    case "Resilience":
                        modifiedStats.defense = Mathf.RoundToInt(modifiedStats.defense * modifier);
                        break;
                    case "Intellect":
                        modifiedStats.intelligence = Mathf.RoundToInt(modifiedStats.intelligence * modifier);
                        break;
                    case "Charm":
                        modifiedStats.charisma = Mathf.RoundToInt(modifiedStats.charisma * modifier);
                        break;
                }
            }
            
            return modifiedStats;
        }
        
        private float CalculateGeneModifier(Gene gene)
        {
            float baseModifier = 0.5f + (gene.value.Value * 0.5f); // 0.5 to 1.0 range
            
            // Expression affects the modifier
            switch (gene.expression)
            {
                case GeneExpression.Enhanced:
                    baseModifier *= 1.2f;
                    break;
                case GeneExpression.Suppressed:
                    baseModifier *= 0.8f;
                    break;
                case GeneExpression.Normal:
                default:
                    break;
            }
            
            return baseModifier;
        }
        
        /// <summary>
        /// Gets the purity of this genetic line (less mutations = higher purity)
        /// </summary>
        public float GetGeneticPurity()
        {
            if (mutations.Length == 0) return 1.0f;
            
            float harmfulMutations = mutations.Count(m => m.isHarmful);
            float totalMutations = mutations.Length;
            
            return Mathf.Clamp01(1.0f - (harmfulMutations / (totalMutations + 5f)));
        }
        
        /// <summary>
        /// Gets a string representation of the most significant traits
        /// </summary>
        public string GetTraitSummary(int maxTraits = 5)
        {
            var significantGenes = genes
                .Where(g => g.isActive && g.value.HasValue && g.value.Value > 0.7f)
                .OrderByDescending(g => g.value.Value)
                .Take(maxTraits)
                .Select(g => g.traitName);
                
            return string.Join(", ", significantGenes);
        }
    }
    
    [Serializable]
    public class Gene
    {
        public string traitName = "";
        public TraitType traitType = TraitType.Physical;
        [Range(0f, 1f)]
        public float dominance = 0.5f; // How dominant this version of the gene is
        public float? value = null; // Numerical value for the trait (0-1 range)
        public GeneExpression expression = GeneExpression.Normal;
        public bool isActive = true;
        
        public Gene() { }
        
        public Gene(Gene original)
        {
            traitName = original.traitName;
            traitType = original.traitType;
            dominance = original.dominance;
            value = original.value;
            expression = original.expression;
            isActive = original.isActive;
        }
    }
    
    [Serializable]
    public struct Mutation
    {
        public MutationType mutationType;
        public string affectedTrait;
        public float severity; // 0-1, how much the mutation affects the trait
        public int generation; // When this mutation occurred
        public bool isHarmful; // Whether this mutation is beneficial or harmful
    }
    
    public enum TraitType
    {
        Physical,   // Size, color, physical features
        Mental,     // Intelligence, memory, learning
        Magical,    // Magical abilities and resistances
        Social,     // Pack behavior, communication
        Combat,     // Fighting abilities, aggression
        Utility,    // Special abilities like flight, camouflage
        Metabolic,  // Digestion, energy efficiency
        Sensory,    // Vision, hearing, smell
        Reproductive // Fertility, parental care
    }
    
    public enum GeneExpression
    {
        Suppressed, // Gene is present but not fully expressed
        Normal,     // Standard expression level
        Enhanced    // Gene is over-expressed
    }
    
    public enum MutationType
    {
        ValueShift,      // Changes the numerical value of a trait
        DominanceChange, // Changes how dominant the gene is
        ExpressionChange, // Changes how the gene is expressed
        NewTrait        // Creates a completely new trait (very rare)
    }
    
    /// <summary>
    /// Environmental factors that can influence genetic expression and inheritance
    /// </summary>
    [Serializable]
    public class EnvironmentalFactors
    {
        [SerializeField] private Dictionary<string, float> traitBiases = new();
        
        public float temperature = 20f; // Celsius
        public float humidity = 50f; // Percentage
        public float foodAvailability = 1f; // 0-1 scale
        public float predatorPressure = 0.5f; // 0-1 scale
        public float socialDensity = 0.5f; // How crowded the environment is
        
        /// <summary>
        /// Gets the environmental bias for a specific trait
        /// </summary>
        public float GetTraitBias(string traitName)
        {
            return traitBiases.TryGetValue(traitName, out float bias) ? bias : 0f;
        }
        
        /// <summary>
        /// Sets environmental pressure that favors certain traits
        /// </summary>
        public void SetTraitBias(string traitName, float bias)
        {
            traitBiases[traitName] = Mathf.Clamp(bias, -0.5f, 0.5f);
        }
        
        /// <summary>
        /// Creates environmental factors based on biome conditions
        /// </summary>
        public static EnvironmentalFactors FromBiome(Creatures.BiomeType biome)
        {
            var factors = new EnvironmentalFactors();
            
            switch (biome)
            {
                case Creatures.BiomeType.Desert:
                    factors.temperature = 45f;
                    factors.humidity = 15f;
                    factors.foodAvailability = 0.3f;
                    factors.SetTraitBias("Heat Resistance", 0.3f);
                    factors.SetTraitBias("Water Conservation", 0.4f);
                    break;
                    
                case Creatures.BiomeType.Arctic:
                    factors.temperature = -10f;
                    factors.humidity = 40f;
                    factors.foodAvailability = 0.4f;
                    factors.SetTraitBias("Cold Resistance", 0.3f);
                    factors.SetTraitBias("Thick Fur", 0.3f);
                    break;
                    
                case Creatures.BiomeType.Ocean:
                    factors.temperature = 15f;
                    factors.humidity = 100f;
                    factors.foodAvailability = 0.7f;
                    factors.SetTraitBias("Swimming", 0.4f);
                    factors.SetTraitBias("Pressure Resistance", 0.2f);
                    break;
                    
                case Creatures.BiomeType.Forest:
                    factors.temperature = 22f;
                    factors.humidity = 65f;
                    factors.foodAvailability = 0.8f;
                    factors.SetTraitBias("Climbing", 0.2f);
                    factors.SetTraitBias("Camouflage", 0.2f);
                    break;
                    
                case Creatures.BiomeType.Mountain:
                    factors.temperature = 5f;
                    factors.humidity = 45f;
                    factors.foodAvailability = 0.5f;
                    factors.predatorPressure = 0.7f;
                    factors.SetTraitBias("Climbing", 0.3f);
                    factors.SetTraitBias("Lung Capacity", 0.2f);
                    break;
                    
                default:
                    // Temperate defaults already set
                    break;
            }
            
            return factors;
        }
    }
}
