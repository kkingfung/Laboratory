using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Collections;
using Laboratory.Chimera.Core;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using Random = UnityEngine.Random;

namespace Laboratory.Chimera.Genetics
{
    /// <summary>
    /// Advanced genetic system for Project Chimera creature breeding.
    /// Implements Mendelian inheritance with dominant/recessive traits,
    /// mutations, environmental adaptation, and emergent trait combinations.
    /// </summary>
    [Serializable]
    public partial class GeneticProfile
    {
        [SerializeField] private Gene[] genes = Array.Empty<Gene>();
        [SerializeField] private Mutation[] mutations = Array.Empty<Mutation>();
        [SerializeField] private float mutationRate = 0.02f; // 2% chance per gene
        [SerializeField] private int generationNumber = 1;
        [SerializeField] private string lineageId = "";
        [SerializeField] private string speciesId = "DefaultSpecies";

        // PERFORMANCE OPTIMIZATION - Enum-based trait storage
        [System.NonSerialized] private Dictionary<TraitType, float> _traitValuesByEnum;
        
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
        /// Get trait values as enum-based dictionary - PERFORMANCE OPTIMIZED
        /// </summary>
        public Dictionary<TraitType, float> GetTraitValuesByEnum()
        {
            if (_traitValuesByEnum == null)
            {
                _traitValuesByEnum = new Dictionary<TraitType, float>();
                foreach (var gene in genes)
                {
                    if (gene.value.HasValue)
                    {
                        _traitValuesByEnum[gene.traitType] = gene.value.Value;
                    }
                }
            }
            return _traitValuesByEnum;
        }

        /// <summary>
        /// Trait expressions for backward compatibility
        /// </summary>
        public Dictionary<TraitType, TraitExpression> TraitExpressions
        {
            get
            {
                var expressions = new Dictionary<TraitType, TraitExpression>();
                foreach (var gene in genes)
                {
                    if (gene.value.HasValue)
                    {
                        expressions[gene.traitType] = new TraitExpression(
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

        /// <summary>
        /// Get trait value using performance-optimized enum lookup
        /// </summary>
        public float GetTraitValue(TraitType traitID, float defaultValue = 0.5f)
        {
            var traitValues = GetTraitValuesByEnum();
            return traitValues.TryGetValue(traitID, out float value) ? value : defaultValue;
        }

        /// <summary>
        /// Get trait value using string name (converts to enum internally)
        /// </summary>
        public float GetTraitValue(string traitName, float defaultValue = 0.5f)
        {
            if (System.Enum.TryParse<TraitType>(traitName, true, out var traitType))
            {
                return GetTraitValue(traitType, defaultValue);
            }
            return defaultValue;
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
        public static GeneticProfile CreateRandom()
        {
            var traitTypes = new[]
            {
                TraitType.Strength, TraitType.Agility, TraitType.Intelligence, TraitType.Stamina, TraitType.Sociability,
                TraitType.Size, TraitType.Speed, TraitType.ColorPattern, TraitType.BodyMarkings, TraitType.Aggression,
                TraitType.Curiosity, TraitType.Loyalty
            };

            var genes = new List<Gene>();
            foreach (var traitType in traitTypes)
            {
                genes.Add(new Gene
                {
                    traitName = traitType.ToString(),
                    traitType = traitType,
                    dominance = Random.Range(0.3f, 0.8f),
                    value = Random.Range(0.2f, 0.9f),
                    expression = GeneExpression.Normal,
                    isActive = true
                });
            }

            return new GeneticProfile(genes.ToArray());
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

        // NOTE: ApplyModifiers moved to GeneticProfileExtensions in main Chimera assembly
        // to avoid circular dependency between Genetics and Creatures assemblies.
        // See Laboratory.Chimera/GeneticProfileExtensions.cs

        /*
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

                switch (gene.traitType)
                {
                    case TraitType.Strength:
                        modifiedStats.attack = Mathf.RoundToInt(modifiedStats.attack * modifier);
                        break;
                    case TraitType.Vitality:
                        modifiedStats.health = Mathf.RoundToInt(modifiedStats.health * modifier);
                        break;
                    case TraitType.Agility:
                        modifiedStats.speed = Mathf.RoundToInt(modifiedStats.speed * modifier);
                        break;
                    case TraitType.Stamina:
                        modifiedStats.defense = Mathf.RoundToInt(modifiedStats.defense * modifier);
                        break;
                    case TraitType.Intelligence:
                        modifiedStats.intelligence = Mathf.RoundToInt(modifiedStats.intelligence * modifier);
                        break;
                    case TraitType.Sociability:
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
        */

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
                    traitType = TraitType.Adaptability,
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
        [SerializeField] private Dictionary<TraitType, float> traitBiases = new Dictionary<TraitType, float>();
        
        public float temperature = 20f; // Celsius
        public float humidity = 50f; // Percentage
        public float foodAvailability = 1f; // 0-1 scale
        public float predatorPressure = 0.5f; // 0-1 scale
        public float socialDensity = 0.5f; // How crowded the environment is
        
        /// <summary>
        /// Gets the environmental bias for a specific trait using enum key
        /// </summary>
        public float GetTraitBias(TraitType traitType)
        {
            return traitBiases.TryGetValue(traitType, out float bias) ? bias : 0f;
        }

        /// <summary>
        /// Gets the environmental bias for a specific trait using string name
        /// </summary>
        public float GetTraitBias(string traitName)
        {
            if (System.Enum.TryParse<TraitType>(traitName, true, out var traitType))
            {
                return GetTraitBias(traitType);
            }
            return 0f;
        }

        /// <summary>
        /// Sets environmental pressure that favors certain traits using enum key
        /// </summary>
        public void SetTraitBias(TraitType traitType, float bias)
        {
            traitBiases[traitType] = Mathf.Clamp(bias, -0.5f, 0.5f);
        }

        /// <summary>
        /// Sets environmental pressure that favors certain traits using string name
        /// </summary>
        public void SetTraitBias(string traitName, float bias)
        {
            if (System.Enum.TryParse<TraitType>(traitName, true, out var traitType))
            {
                SetTraitBias(traitType, bias);
            }
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
        public static EnvironmentalFactors FromBiome(BiomeType biome)
        {
            var factors = new EnvironmentalFactors();
            
            switch (biome)
            {
                case BiomeType.Desert:
                    factors.temperature = 45f;
                    factors.humidity = 15f;
                    factors.foodAvailability = 0.3f;
                    factors.SetTraitBias("Heat Resistance", 0.3f);
                    factors.SetTraitBias("Water Conservation", 0.4f);
                    break;
                    
                case BiomeType.Arctic:
                    factors.temperature = -10f;
                    factors.humidity = 40f;
                    factors.foodAvailability = 0.4f;
                    factors.SetTraitBias("Cold Resistance", 0.3f);
                    factors.SetTraitBias("Thick Fur", 0.3f);
                    break;
                    
                case BiomeType.Ocean:
                    factors.temperature = 15f;
                    factors.humidity = 100f;
                    factors.foodAvailability = 0.7f;
                    factors.SetTraitBias("Swimming", 0.4f);
                    factors.SetTraitBias("Pressure Resistance", 0.2f);
                    break;
                    
                case BiomeType.Forest:
                    factors.temperature = 22f;
                    factors.humidity = 65f;
                    factors.foodAvailability = 0.8f;
                    factors.SetTraitBias("Climbing", 0.2f);
                    factors.SetTraitBias("Camouflage", 0.2f);
                    break;
                    
                case BiomeType.Mountain:
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

    // Extension of the GeneticProfile class
    public partial class GeneticProfile
    {
        #region Ancestral Trait Activation (from Temporal Genetics)

        /// <summary>
        /// Activates dormant ancestral traits under stress conditions
        /// </summary>
        public bool TryActivateAncestralTrait(float stressLevel, BiomeType currentBiome)
        {
            if (stressLevel < 0.7f) return false; // Stress threshold

            var ancestralTraits = GetPossibleAncientTraits(currentBiome);
            if (ancestralTraits.Length == 0) return false;

            // Higher stress = higher activation chance
            float activationChance = Mathf.Clamp01((stressLevel - 0.7f) * 0.3f);

            if (Random.value < activationChance)
            {
                var selectedTrait = ancestralTraits[Random.Range(0, ancestralTraits.Length)];
                ActivateAncestralTrait(selectedTrait);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Adds an ancestral trait to this genetic profile
        /// </summary>
        private void ActivateAncestralTrait(Gene ancestralTrait)
        {
            var newGene = ancestralTrait;
            newGene.expression = GeneExpression.Enhanced; // Stress-activated traits are over-expressed
            newGene.mutationGeneration = generationNumber;
            newGene.isMutation = true; // Mark as reactivated ancestral trait

            var genesList = genes.ToList();
            genesList.Add(newGene);
            genes = genesList.ToArray();

            UnityEngine.Debug.Log($"Ancestral trait '{ancestralTrait.traitName}' activated due to stress in lineage {lineageId}");
        }

        /// <summary>
        /// Gets possible ancient traits for a specific biome
        /// </summary>
        private Gene[] GetPossibleAncientTraits(BiomeType biome)
        {
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
                case BiomeType.Volcanic:
                    traits.AddRange(CreateAncientTraits("Heat Immunity", "Lava Walking", "Fire Breathing"));
                    break;
                case BiomeType.Swamp:
                    traits.AddRange(CreateAncientTraits("Poison Immunity", "Amphibious", "Mud Camouflage"));
                    break;
                default:
                    traits.AddRange(CreateAncientTraits("Ancient Wisdom", "Longevity", "Primal Instincts"));
                    break;
            }

            return traits.ToArray();
        }

        /// <summary>
        /// Creates ancient trait genes from trait names
        /// </summary>
        private Gene[] CreateAncientTraits(params string[] traitNames)
        {
            return traitNames.Select(traitName =>
            {
                var dominantValue = Random.Range(0.7f, 0.9f);
                var recessiveValue = Random.Range(0.1f, 0.3f);

                var dominantAllele = new Allele(traitName + "_Dom", dominantValue, true, true);
                var recessiveAllele = new Allele(traitName + "_Rec", recessiveValue, true, false);

                return new Gene(
                    Guid.NewGuid().ToString(),
                    traitName,
                    Laboratory.Core.Enums.TraitType.Physical, // Default to physical
                    dominantAllele,
                    recessiveAllele
                )
                {
                    expression = GeneExpression.Normal,
                    mutationGeneration = -1, // Mark as ancient
                    isMutation = false,
                    expressionStrength = Random.Range(0.8f, 1.0f) // Ancient traits are usually strongly expressed
                };
            }).ToArray();
        }

        /// <summary>
        /// Calculates stress level based on environmental conditions
        /// </summary>
        public float CalculateStressLevel(EnvironmentalFactors environment)
        {
            float stress = 0f;

            // Temperature stress
            float optimalTemp = GetOptimalTemperature();
            float tempDiff = Mathf.Abs(environment.temperature - optimalTemp);
            stress += Mathf.Clamp01(tempDiff / 30f) * 0.3f;

            // Food availability stress
            stress += Mathf.Clamp01((1f - environment.foodAvailability) * 0.4f);

            // Predator pressure stress
            stress += environment.predatorPressure * 0.3f;

            return Mathf.Clamp01(stress);
        }

        /// <summary>
        /// Gets optimal temperature for this genetic profile
        /// </summary>
        private float GetOptimalTemperature()
        {
            float temp = 20f; // Default temperate

            // Adjust based on genetic traits
            foreach (var gene in genes)
            {
                switch (gene.traitName)
                {
                    case "Heat Resistance":
                    case "Desert Adaptation":
                        temp += 10f * gene.dominance;
                        break;
                    case "Cold Resistance":
                    case "Arctic Adaptation":
                        temp -= 15f * gene.dominance;
                        break;
                }
            }

            return temp;
        }

        #endregion
    }
}
