using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Configuration;
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
        [SerializeField] private string speciesId = "DefaultSpecies";
        
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
        /// Generation number for compatibility
        /// </summary>
        public int GenerationNumber => generationNumber;
        
        /// <summary>
        /// Unique lineage identifier for tracking breeding lines
        /// </summary>
        public string LineageId => lineageId;

        /// <summary>
        /// Species identifier for compatibility and breeding restrictions
        /// </summary>
        public string SpeciesId => speciesId;

        /// <summary>
        /// Profile ID for compatibility
        /// </summary>
        public string ProfileId => lineageId;
        
        /// <summary>
        /// Trait expressions for compatibility
        /// </summary>
        public Dictionary<string, TraitExpression> TraitExpressions
        {
            get
            {
                var expressions = new Dictionary<string, TraitExpression>();
                foreach (var gene in genes)
                {
                    if (gene.value.HasValue)
                    {
                        expressions[gene.traitName] = new TraitExpression(
                            gene.traitName, 
                            gene.value.Value, 
                            gene.traitType)
                        {
                            dominanceStrength = gene.dominance
                        };
                    }
                }
                return expressions;
            }
        }
        
        public GeneticProfile()
        {
            lineageId = Guid.NewGuid().ToString("N")[..8];
            speciesId = "DefaultSpecies";
        }

        public GeneticProfile(Gene[] initialGenes, int generation = 1, string parentLineage = "", string species = "DefaultSpecies")
        {
            genes = initialGenes ?? Array.Empty<Gene>();
            generationNumber = generation;
            speciesId = species;
            lineageId = string.IsNullOrEmpty(parentLineage) ?
                Guid.NewGuid().ToString("N")[..8] :
                $"{parentLineage}-{generation:D2}";
        }
        
        /// <summary>
        /// Creates a random genetic profile with basic traits
        /// </summary>
        public static GeneticProfile CreateRandom(Laboratory.Chimera.Creatures.CreatureStats baseStats)
        {
            var traits = new[]
            {
                "Strength", "Agility", "Intelligence", "Constitution", "Charisma", 
                "Size", "Speed", "Color", "Pattern", "Aggression", "Curiosity", "Loyalty"
            };
            
            var genes = new List<Gene>();
            foreach (var trait in traits)
            {
                genes.Add(new Gene
                {
                    traitName = trait,
                    traitType = GetTraitTypeForName(trait),
                    dominance = Random.Range(0.3f, 0.8f),
                    value = Random.Range(0.2f, 0.9f),
                    expression = GeneExpression.Normal,
                    isActive = true
                });
            }
            
            return new GeneticProfile(genes.ToArray());
        }
        
        private static TraitType GetTraitTypeForName(string traitName)
        {
            return traitName switch
            {
                "Strength" or "Agility" or "Constitution" or "Size" or "Speed" => TraitType.Physical,
                "Intelligence" or "Curiosity" => TraitType.Mental,
                "Charisma" or "Loyalty" => TraitType.Social,
                "Color" or "Pattern" => TraitType.Physical,
                "Aggression" => TraitType.Behavioral,
                _ => TraitType.Physical
            };
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
        /// Calculate genetic similarity between two profiles
        /// </summary>
        public float GetGeneticSimilarity(GeneticProfile other)
        {
            if (other?.Genes == null || genes == null) return 0f;
            
            var commonTraits = genes.Where(g1 => 
                other.Genes.Any(g2 => g2.traitName == g1.traitName))
                .ToArray();
                
            if (commonTraits.Length == 0) return 0f;
            
            float totalSimilarity = 0f;
            foreach (var gene1 in commonTraits)
            {
                var gene2 = other.Genes.FirstOrDefault(g => g.traitName == gene1.traitName);
                if (!string.IsNullOrEmpty(gene2.traitName) && gene2.value.HasValue && gene1.value.HasValue)
                {
                    float similarity = 1f - Mathf.Abs(gene1.value.Value - gene2.value.Value);
                    totalSimilarity += similarity;
                }
            }
            
            return totalSimilarity / commonTraits.Length;
        }
        
        /// <summary>
        /// Trigger a mutation for environmental adaptation
        /// </summary>
        public void TriggerMutation(string adaptationType)
        {
            var mutation = new Mutation
            {
                mutationType = MutationType.NewTrait,
                affectedTrait = adaptationType,
                severity = UnityEngine.Random.Range(0.1f, 0.3f),
                generation = generationNumber,
                isHarmful = false // Environmental adaptations are beneficial
            };
            
            var mutationsList = mutations.ToList();
            mutationsList.Add(mutation);
            mutations = mutationsList.ToArray();
            
            // Add or modify the corresponding gene
            var existingGene = genes.FirstOrDefault(g => g.traitName == adaptationType);
            if (!string.IsNullOrEmpty(existingGene.traitName))
            {
                // Enhance existing gene
                existingGene.value = Mathf.Min(1f, (existingGene.value ?? 0.5f) + mutation.severity);
                existingGene.expression = GeneExpression.Enhanced;
            }
            else
            {
                // Create new gene
                var newGene = new Gene
                {
                    traitName = adaptationType,
                    traitType = TraitType.Physical,
                    dominance = 0.7f,
                    value = 0.5f + mutation.severity,
                    expression = GeneExpression.Normal,
                    isActive = true
                };
                
                var genesList = genes.ToList();
                genesList.Add(newGene);
                genes = genesList.ToArray();
            }
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
        
        /// <summary>
        /// Gets trait names for compatibility
        /// </summary>
        public List<string> GetTraitNames()
        {
            return genes.Where(g => g.isActive).Select(g => g.traitName).ToList();
        }
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
        /// Creates default environmental factors
        /// </summary>
        public static EnvironmentalFactors CreateDefault()
        {
            return new EnvironmentalFactors()
            {
                temperature = 22f,
                humidity = 60f,
                foodAvailability = 0.8f,
                predatorPressure = 0.3f,
                socialDensity = 0.5f
            };
        }
        
        /// <summary>
        /// Creates environmental factors based on biome conditions
        /// </summary>
        public static EnvironmentalFactors FromBiome(Laboratory.Chimera.Core.BiomeType biome)
        {
            var factors = new EnvironmentalFactors();
            
            switch (biome)
            {
                case Laboratory.Chimera.Core.BiomeType.Desert:
                    factors.temperature = 45f;
                    factors.humidity = 15f;
                    factors.foodAvailability = 0.3f;
                    factors.SetTraitBias("Heat Resistance", 0.3f);
                    factors.SetTraitBias("Water Conservation", 0.4f);
                    break;
                    
                case Laboratory.Chimera.Core.BiomeType.Arctic:
                    factors.temperature = -10f;
                    factors.humidity = 40f;
                    factors.foodAvailability = 0.4f;
                    factors.SetTraitBias("Cold Resistance", 0.3f);
                    factors.SetTraitBias("Thick Fur", 0.3f);
                    break;
                    
                case Laboratory.Chimera.Core.BiomeType.Ocean:
                    factors.temperature = 15f;
                    factors.humidity = 100f;
                    factors.foodAvailability = 0.7f;
                    factors.SetTraitBias("Swimming", 0.4f);
                    factors.SetTraitBias("Pressure Resistance", 0.2f);
                    break;
                    
                case Laboratory.Chimera.Core.BiomeType.Forest:
                    factors.temperature = 22f;
                    factors.humidity = 65f;
                    factors.foodAvailability = 0.8f;
                    factors.SetTraitBias("Climbing", 0.2f);
                    factors.SetTraitBias("Camouflage", 0.2f);
                    break;
                    
                case Laboratory.Chimera.Core.BiomeType.Mountain:
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
