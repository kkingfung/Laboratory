using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;

namespace Laboratory.Subsystems.Genetics
{
    /// <summary>
    /// Calculates trait expression and stat modifiers from genetic profiles.
    /// Handles complex genetic interactions, environmental factors, and discovery of novel traits.
    /// </summary>
    public class TraitExpressionCalculator : MonoBehaviour, ITraitService
    {
        [Header("Configuration")]
        [SerializeField] private bool enableComplexInteractions = true;
        [SerializeField] private bool enableEnvironmentalModifiers = true;
        [SerializeField] private bool trackNovelTraits = true;

        private GeneticsSubsystemConfig _config;
        private readonly Dictionary<string, TraitExpression> _cachedExpressions = new();
        private readonly HashSet<string> _discoveredTraits = new();

        // Events
        public event Action<TraitDiscoveryEvent> OnTraitDiscovered;

        // Properties
        public bool IsInitialized { get; private set; }

        #region Initialization

        public async Task InitializeAsync(GeneticsSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            enableComplexInteractions = _config.EnableAdvancedInheritance;
            enableEnvironmentalModifiers = _config.EnableEnvironmentalEffects;

            // Load any pre-discovered traits
            LoadKnownTraits();

            IsInitialized = true;
            await Task.CompletedTask;

            Debug.Log("[TraitExpressionCalculator] Initialized successfully");
        }

        #endregion

        #region Core Expression Calculation

        /// <summary>
        /// Calculates final trait values from genetic profile
        /// </summary>
        public TraitExpressionResult CalculateTraitExpression(
            GeneticProfile profile,
            EnvironmentalFactors environment = null)
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("TraitExpressionCalculator not initialized");
            }

            if (profile?.Genes == null)
            {
                return new TraitExpressionResult
                {
                    expressedTraits = new Dictionary<string, float>(),
                    traitExpressions = new Dictionary<string, TraitExpression>(),
                    overallFitness = 0f,
                    dominantTraits = new List<string>(),
                    recessiveTraits = new List<string>(),
                    novelTraits = new List<string>()
                };
            }

            var result = new TraitExpressionResult
            {
                expressedTraits = new Dictionary<string, float>(),
                traitExpressions = new Dictionary<string, TraitExpression>(),
                dominantTraits = new List<string>(),
                recessiveTraits = new List<string>(),
                novelTraits = new List<string>()
            };

            // Process each gene
            foreach (var gene in profile.Genes.Where(g => g.isActive))
            {
                var expression = CalculateGeneExpression(gene, profile, environment);

                if (expression != null)
                {
                    result.traitExpressions[gene.traitName] = expression;
                    result.expressedTraits[gene.traitName] = expression.value;

                    // Categorize traits
                    if (expression.dominanceStrength > 0.7f)
                    {
                        result.dominantTraits.Add(gene.traitName);
                    }
                    else if (expression.dominanceStrength < 0.3f)
                    {
                        result.recessiveTraits.Add(gene.traitName);
                    }

                    // Check for novel traits
                    if (trackNovelTraits && IsNovelTrait(gene.traitName, expression.value))
                    {
                        result.novelTraits.Add(gene.traitName);
                        RegisterTraitDiscovery(profile.ProfileId, gene.traitName, expression);
                    }
                }
            }

            // Calculate complex interactions if enabled
            if (enableComplexInteractions)
            {
                ProcessTraitInteractions(result, profile, environment);
            }

            // Calculate overall fitness
            result.overallFitness = CalculateOverallFitness(result, environment);

            return result;
        }

        /// <summary>
        /// Gets trait modifiers for creature stats
        /// </summary>
        public StatModifier[] GetStatModifiers(GeneticProfile profile)
        {
            if (profile?.Genes == null)
                return new StatModifier[0];

            var modifiers = new List<StatModifier>();

            foreach (var gene in profile.Genes.Where(g => g.isActive && g.value.HasValue))
            {
                var geneModifiers = GetStatModifiersForGene(gene);
                modifiers.AddRange(geneModifiers);
            }

            // Combine modifiers for the same stat
            return CombineStatModifiers(modifiers);
        }

        /// <summary>
        /// Discovers new trait combinations
        /// </summary>
        public TraitDiscovery AnalyzeTraitCombinations(GeneticProfile profile)
        {
            if (profile?.Genes == null)
                return new TraitDiscovery
                {
                    newCombinations = new List<string>(),
                    rareTraits = new List<string>(),
                    discoveryScore = 0f,
                    isWorldFirst = false
                };

            var discovery = new TraitDiscovery
            {
                newCombinations = new List<string>(),
                rareTraits = new List<string>(),
                discoveryScore = 0f,
                isWorldFirst = false
            };

            // Analyze trait combinations
            var activeTrait​​s = profile.Genes.Where(g => g.isActive && g.value.HasValue).ToArray();

            // Look for rare combinations
            var combinations = GenerateTraitCombinations(activeTraits);
            foreach (var combination in combinations)
            {
                if (IsRareCombination(combination))
                {
                    discovery.newCombinations.Add(string.Join(" + ", combination));
                    discovery.discoveryScore += CalculateCombinationRarity(combination);
                }
            }

            // Identify rare individual traits
            foreach (var gene in activeTraits)
            {
                if (IsRareTrait(gene))
                {
                    discovery.rareTraits.Add(gene.traitName);
                    discovery.discoveryScore += CalculateTraitRarity(gene);
                }
            }

            // Check if this is a world-first discovery
            discovery.isWorldFirst = discovery.discoveryScore > 10f && discovery.newCombinations.Count > 0;

            return discovery;
        }

        #endregion

        #region Gene Expression Calculation

        private TraitExpression CalculateGeneExpression(
            Gene gene,
            GeneticProfile profile,
            EnvironmentalFactors environment)
        {
            if (!gene.value.HasValue)
                return null;

            // Base expression value
            var baseValue = gene.value.Value;

            // Apply gene expression modifier
            var expressionModifier = gene.expression switch
            {
                GeneExpression.Enhanced => 1.2f,
                GeneExpression.Suppressed => 0.8f,
                GeneExpression.Normal => 1.0f,
                _ => 1.0f
            };

            // Apply environmental modifiers if enabled
            var environmentalModifier = 1.0f;
            if (enableEnvironmentalModifiers && environment != null)
            {
                environmentalModifier = CalculateEnvironmentalModifier(gene, environment);
            }

            // Apply mutation effects
            var mutationModifier = CalculateMutationModifier(gene, profile);

            // Calculate final value
            var finalValue = baseValue * expressionModifier * environmentalModifier * mutationModifier;
            finalValue = Mathf.Clamp01(finalValue);

            // Create trait expression
            var expression = new TraitExpression(gene.traitName, finalValue, gene.traitType)
            {
                dominanceStrength = gene.dominance,
                expression = gene.expression,
                isActive = gene.isActive
            };

            return expression;
        }

        private float CalculateEnvironmentalModifier(Gene gene, EnvironmentalFactors environment)
        {
            var modifier = 1.0f;

            // Trait-specific environmental effects
            switch (gene.traitName.ToLower())
            {
                case "heat resistance":
                    modifier += (environment.temperature - 22f) / 50f; // Boost in hot climates
                    break;

                case "cold resistance":
                    modifier += (22f - environment.temperature) / 50f; // Boost in cold climates
                    break;

                case "water conservation":
                    modifier += (50f - environment.humidity) / 100f; // Boost in dry climates
                    break;

                case "speed":
                case "agility":
                    modifier += environment.predatorPressure * 0.3f; // Boost under predator pressure
                    break;

                case "size":
                case "strength":
                    modifier += environment.foodAvailability * 0.2f; // Boost with good food
                    break;

                case "intelligence":
                case "curiosity":
                    modifier += environment.socialDensity * 0.15f; // Boost in social environments
                    break;
            }

            // Apply general environmental bias
            var bias = environment.GetTraitBias(gene.traitName);
            modifier += bias;

            return Mathf.Clamp(modifier, 0.5f, 2.0f);
        }

        private float CalculateMutationModifier(Gene gene, GeneticProfile profile)
        {
            var modifier = 1.0f;

            // Check for mutations affecting this gene
            var relevantMutations = profile.Mutations.Where(m =>
                m.affectedTrait == gene.traitName || m.affectedGeneId == gene.traitName).ToArray();

            foreach (var mutation in relevantMutations)
            {
                var effect = mutation.isBeneficial ? mutation.severity : -mutation.severity;
                modifier += effect * 0.5f; // Mutations have moderate impact
            }

            return Mathf.Clamp(modifier, 0.3f, 2.0f);
        }

        #endregion

        #region Trait Interactions

        private void ProcessTraitInteractions(
            TraitExpressionResult result,
            GeneticProfile profile,
            EnvironmentalFactors environment)
        {
            // Synergistic combinations
            ProcessSynergisticTraits(result);

            // Antagonistic combinations
            ProcessAntagonisticTraits(result);

            // Polygenic traits (multiple genes affecting one trait)
            ProcessPolygenicTraits(result, profile);

            // Epistatic interactions (genes affecting other genes)
            ProcessEpistaticInteractions(result, profile);
        }

        private void ProcessSynergisticTraits(TraitExpressionResult result)
        {
            // Define synergistic trait combinations
            var synergies = new Dictionary<(string, string), float>
            {
                { ("Strength", "Size"), 0.2f },
                { ("Intelligence", "Curiosity"), 0.15f },
                { ("Speed", "Agility"), 0.25f },
                { ("Heat Resistance", "Water Conservation"), 0.1f },
                { ("Cold Resistance", "Thick Fur"), 0.2f }
            };

            foreach (var ((trait1, trait2), bonus) in synergies)
            {
                if (result.expressedTraits.ContainsKey(trait1) && result.expressedTraits.ContainsKey(trait2))
                {
                    var combinedStrength = result.expressedTraits[trait1] * result.expressedTraits[trait2];
                    var synergyBonus = combinedStrength * bonus;

                    result.expressedTraits[trait1] = Mathf.Clamp01(result.expressedTraits[trait1] + synergyBonus);
                    result.expressedTraits[trait2] = Mathf.Clamp01(result.expressedTraits[trait2] + synergyBonus);
                }
            }
        }

        private void ProcessAntagonisticTraits(TraitExpressionResult result)
        {
            // Define antagonistic trait combinations
            var antagonisms = new Dictionary<(string, string), float>
            {
                { ("Size", "Speed"), 0.15f },
                { ("Aggression", "Social"), 0.1f },
                { ("Curiosity", "Caution"), 0.2f }
            };

            foreach (var ((trait1, trait2), penalty) in antagonisms)
            {
                if (result.expressedTraits.ContainsKey(trait1) && result.expressedTraits.ContainsKey(trait2))
                {
                    var combinedStrength = result.expressedTraits[trait1] * result.expressedTraits[trait2];
                    var antagonismPenalty = combinedStrength * penalty;

                    result.expressedTraits[trait1] = Mathf.Clamp01(result.expressedTraits[trait1] - antagonismPenalty);
                    result.expressedTraits[trait2] = Mathf.Clamp01(result.expressedTraits[trait2] - antagonismPenalty);
                }
            }
        }

        private void ProcessPolygenicTraits(TraitExpressionResult result, GeneticProfile profile)
        {
            // Group genes by trait type
            var traitGroups = profile.Genes
                .Where(g => g.isActive && g.value.HasValue)
                .GroupBy(g => g.traitType)
                .Where(group => group.Count() > 1);

            foreach (var group in traitGroups)
            {
                var genes = group.ToArray();
                var averageValue = genes.Average(g => g.value.Value);
                var polygenicBonus = (genes.Length - 1) * 0.05f; // Bonus for having multiple related genes

                // Apply polygenic effect to all genes in the group
                foreach (var gene in genes)
                {
                    if (result.expressedTraits.ContainsKey(gene.traitName))
                    {
                        var blendedValue = (result.expressedTraits[gene.traitName] + averageValue) * 0.5f;
                        result.expressedTraits[gene.traitName] = Mathf.Clamp01(blendedValue + polygenicBonus);
                    }
                }
            }
        }

        private void ProcessEpistaticInteractions(TraitExpressionResult result, GeneticProfile profile)
        {
            // Look for modifier genes
            var modifierGenes = profile.Genes.Where(g =>
                g.isActive &&
                g.traitName.Contains("Modifier") &&
                g.value.HasValue).ToArray();

            foreach (var modifier in modifierGenes)
            {
                var modifierEffect = (modifier.value.Value - 0.5f) * 0.3f; // ±15% max effect

                // Apply to all other traits
                var targetTraits = result.expressedTraits.Keys.Where(k => k != modifier.traitName).ToArray();
                foreach (var trait in targetTraits)
                {
                    result.expressedTraits[trait] = Mathf.Clamp01(result.expressedTraits[trait] + modifierEffect);
                }
            }
        }

        #endregion

        #region Stat Modifiers

        private StatModifier[] GetStatModifiersForGene(Gene gene)
        {
            var modifiers = new List<StatModifier>();

            // Map genes to stats based on trait name
            switch (gene.traitName.ToLower())
            {
                case "strength":
                    modifiers.Add(new StatModifier("attack", CalculateStatMultiplier(gene.value.Value), 0f));
                    break;

                case "vitality":
                case "constitution":
                    modifiers.Add(new StatModifier("health", CalculateStatMultiplier(gene.value.Value), 0f));
                    modifiers.Add(new StatModifier("defense", CalculateStatMultiplier(gene.value.Value * 0.7f), 0f));
                    break;

                case "agility":
                case "speed":
                    modifiers.Add(new StatModifier("speed", CalculateStatMultiplier(gene.value.Value), 0f));
                    break;

                case "intelligence":
                    modifiers.Add(new StatModifier("intelligence", CalculateStatMultiplier(gene.value.Value), 0f));
                    break;

                case "charisma":
                case "social":
                    modifiers.Add(new StatModifier("charisma", CalculateStatMultiplier(gene.value.Value), 0f));
                    break;

                case "size":
                    modifiers.Add(new StatModifier("health", CalculateStatMultiplier(gene.value.Value * 0.8f), 0f));
                    modifiers.Add(new StatModifier("attack", CalculateStatMultiplier(gene.value.Value * 0.6f), 0f));
                    modifiers.Add(new StatModifier("speed", CalculateStatMultiplier(1.2f - gene.value.Value), 0f)); // Inverse relationship
                    break;
            }

            return modifiers.ToArray();
        }

        private float CalculateStatMultiplier(float geneValue)
        {
            // Convert gene value (0-1) to stat multiplier (0.5-1.5)
            return 0.5f + geneValue;
        }

        private StatModifier[] CombineStatModifiers(List<StatModifier> modifiers)
        {
            var combined = new Dictionary<string, (float multiplier, float additive)>();

            foreach (var modifier in modifiers)
            {
                if (combined.ContainsKey(modifier.statName))
                {
                    var existing = combined[modifier.statName];
                    combined[modifier.statName] = (
                        existing.multiplier * modifier.multiplier, // Multiply multipliers
                        existing.additive + modifier.additive     // Add additives
                    );
                }
                else
                {
                    combined[modifier.statName] = (modifier.multiplier, modifier.additive);
                }
            }

            return combined.Select(kvp => new StatModifier(kvp.Key, kvp.Value.multiplier, kvp.Value.additive))
                          .ToArray();
        }

        #endregion

        #region Discovery System

        private bool IsNovelTrait(string traitName, float value)
        {
            if (!trackNovelTraits)
                return false;

            var traitKey = $"{traitName}_{Mathf.RoundToInt(value * 100)}";
            return !_discoveredTraits.Contains(traitKey);
        }

        private void RegisterTraitDiscovery(string creatureId, string traitName, TraitExpression expression)
        {
            var traitKey = $"{traitName}_{Mathf.RoundToInt(expression.value * 100)}";
            _discoveredTraits.Add(traitKey);

            var discoveryEvent = new TraitDiscoveryEvent
            {
                creatureId = creatureId,
                traitName = traitName,
                traitType = expression.type,
                expressionValue = expression.value,
                generation = 1, // Would get from profile if available
                isWorldFirst = true, // Simplified for now
                discoverer = "Player", // Would get from current player
                timestamp = DateTime.UtcNow
            };

            OnTraitDiscovered?.Invoke(discoveryEvent);

            Debug.Log($"[TraitExpressionCalculator] Novel trait discovered: {traitName} " +
                     $"(value: {expression.value:F3}) in creature {creatureId}");
        }

        private void LoadKnownTraits()
        {
            // Load previously discovered traits from persistent storage
            // For now, add some common traits that are always known
            var commonTraits = new[]
            {
                "Strength", "Agility", "Intelligence", "Constitution", "Charisma",
                "Size", "Speed", "Color", "Pattern"
            };

            foreach (var trait in commonTraits)
            {
                for (int i = 0; i <= 100; i += 10) // Common value ranges
                {
                    _discoveredTraits.Add($"{trait}_{i}");
                }
            }
        }

        #endregion

        #region Trait Analysis

        private List<List<string>> GenerateTraitCombinations(Gene[] traits)
        {
            var combinations = new List<List<string>>();

            // Generate pairs
            for (int i = 0; i < traits.Length; i++)
            {
                for (int j = i + 1; j < traits.Length; j++)
                {
                    combinations.Add(new List<string> { traits[i].traitName, traits[j].traitName });
                }
            }

            // Generate triplets for high-value traits
            var highValueTraits = traits.Where(t => t.value.HasValue && t.value.Value > 0.7f).ToArray();
            for (int i = 0; i < highValueTraits.Length; i++)
            {
                for (int j = i + 1; j < highValueTraits.Length; j++)
                {
                    for (int k = j + 1; k < highValueTraits.Length; k++)
                    {
                        combinations.Add(new List<string>
                        {
                            highValueTraits[i].traitName,
                            highValueTraits[j].traitName,
                            highValueTraits[k].traitName
                        });
                    }
                }
            }

            return combinations;
        }

        private bool IsRareCombination(List<string> combination)
        {
            // Simple rarity check - in a real system, this would query a database
            var rareCombinations = new[]
            {
                new[] { "Bioluminescence", "Telepathy" },
                new[] { "Regeneration", "Camouflage" },
                new[] { "Size", "Speed", "Intelligence" }
            };

            return rareCombinations.Any(rare =>
                rare.All(trait => combination.Contains(trait)));
        }

        private float CalculateCombinationRarity(List<string> combination)
        {
            // Base rarity increases with combination size
            var baseRarity = combination.Count * 2f;

            // Bonus for specific rare traits
            var rareTraits = new[] { "Bioluminescence", "Telepathy", "Regeneration", "Camouflage" };
            var rareCount = combination.Count(trait => rareTraits.Contains(trait));

            return baseRarity + (rareCount * 5f);
        }

        private bool IsRareTrait(Gene gene)
        {
            // Traits with extreme values are rare
            if (gene.value.HasValue)
            {
                var value = gene.value.Value;
                if (value > 0.9f || value < 0.1f)
                    return true;
            }

            // Specific rare traits
            var rareTraitNames = new[]
            {
                "Bioluminescence", "Telepathy", "Regeneration", "Camouflage",
                "Sonic Attack", "Poison Resistance", "Night Vision", "Aquatic Breathing"
            };

            return rareTraitNames.Contains(gene.traitName);
        }

        private float CalculateTraitRarity(Gene gene)
        {
            var rarity = 1f;

            // Extreme values are rarer
            if (gene.value.HasValue)
            {
                var value = gene.value.Value;
                if (value > 0.9f)
                    rarity += (value - 0.9f) * 20f; // Up to +2 for perfect traits
                else if (value < 0.1f)
                    rarity += (0.1f - value) * 15f; // Up to +1.5 for very low traits
            }

            // Rare trait names get bonus
            if (IsRareTrait(gene))
                rarity += 3f;

            return rarity;
        }

        private float CalculateOverallFitness(TraitExpressionResult result, EnvironmentalFactors environment)
        {
            if (result.expressedTraits.Count == 0)
                return 0f;

            // Base fitness is average of all expressed traits
            var averageTraitValue = result.expressedTraits.Values.Average();

            // Bonus for having many traits
            var traitCountBonus = Mathf.Min(result.expressedTraits.Count * 0.02f, 0.2f);

            // Bonus for dominant traits
            var dominantBonus = result.dominantTraits.Count * 0.03f;

            // Environmental fitness bonus
            var environmentalBonus = 0f;
            if (environment != null)
            {
                // Bonus for having traits that match environment
                foreach (var trait in result.expressedTraits)
                {
                    var bias = environment.GetTraitBias(trait.Key);
                    if (bias > 0)
                    {
                        environmentalBonus += bias * trait.Value * 0.1f;
                    }
                }
            }

            var totalFitness = averageTraitValue + traitCountBonus + dominantBonus + environmentalBonus;
            return Mathf.Clamp01(totalFitness);
        }

        #endregion
    }
}