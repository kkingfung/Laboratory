using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;
using System;

namespace Laboratory.Chimera.Breeding
{
    /// <summary>
    /// Advanced genetic prediction system that analyzes parent genetics to predict
    /// offspring traits, mutation probabilities, and breeding success chances.
    /// Uses Mendelian inheritance, polygenic interactions, and environmental factors.
    /// </summary>
    public class GeneticPredictor
    {
        [Header("Prediction Settings")]
        [SerializeField] private float predictionAccuracy = 0.85f;
        [SerializeField] private bool enablePolygeneticAnalysis = true;
        [SerializeField] private bool enableEpistaticInteractions = true;
        [SerializeField] private bool enableEnvironmentalFactors = true;
        
        // Prediction caching for performance
        private Dictionary<string, BreedingPrediction> predictionCache = new Dictionary<string, BreedingPrediction>();
        private const int MAX_CACHE_SIZE = 100;
        
        public BreedingPrediction PredictOffspring(GeneticProfile parent1, GeneticProfile parent2)
        {
            if (parent1 == null || parent2 == null)
                return null;
                
            // Check cache first
            string cacheKey = GenerateCacheKey(parent1, parent2);
            if (predictionCache.ContainsKey(cacheKey))
                return predictionCache[cacheKey];
            
            var prediction = new BreedingPrediction
            {
                Parent1Profile = parent1,
                Parent2Profile = parent2,
                PredictedTraits = new List<TraitPrediction>(),
                ProbabilityDistribution = new Dictionary<string, float>(),
                ConfidenceScore = 0f
            };
            
            // Predict individual traits
            PredictIndividualTraits(prediction, parent1, parent2);
            
            // Analyze polygenic interactions
            if (enablePolygeneticAnalysis)
                AnalyzePolygenicEffects(prediction, parent1, parent2);
            
            // Calculate epistatic interactions
            if (enableEpistaticInteractions)
                AnalyzeEpistaticInteractions(prediction, parent1, parent2);
            
            // Factor in environmental influences
            if (enableEnvironmentalFactors)
                ApplyEnvironmentalFactors(prediction, parent1, parent2);
            
            // Calculate overall probabilities
            CalculateBreedingProbabilities(prediction);
            
            // Calculate confidence score
            prediction.ConfidenceScore = CalculateOverallConfidence(prediction);
            
            // Cache the result
            CachePrediction(cacheKey, prediction);
            
            return prediction;
        }
        
        #region Individual Trait Prediction
        
        private void PredictIndividualTraits(BreedingPrediction prediction, GeneticProfile parent1, GeneticProfile parent2)
        {
            // Get all unique trait types from both parents
            var allTraitTypes = GetAllTraitTypes(parent1, parent2);

            foreach (var traitType in allTraitTypes)
            {
                var traitPrediction = PredictSingleTrait(traitType, parent1, parent2);
                if (traitPrediction != null)
                {
                    prediction.PredictedTraits.Add(traitPrediction);
                }
            }
        }
        
        private TraitPrediction PredictSingleTrait(Laboratory.Core.Enums.TraitType traitType, GeneticProfile parent1, GeneticProfile parent2)
        {
            var parent1Trait = GetTraitExpression(parent1, traitType);
            var parent2Trait = GetTraitExpression(parent2, traitType);

            // If neither parent has this trait, return null
            if (parent1Trait == null && parent2Trait == null)
                return null;

            // Handle cases where only one parent has the trait
            if (parent1Trait == null)
                parent1Trait = CreateDefaultTrait(traitType);
            if (parent2Trait == null)
                parent2Trait = CreateDefaultTrait(traitType);
            
            var prediction = new TraitPrediction
            {
                TraitType = traitType,
                Parent1Value = parent1Trait.Value,
                Parent2Value = parent2Trait.Value,
                Parent1Dominance = parent1Trait.DominanceStrength,
                Parent2Dominance = parent2Trait.DominanceStrength
            };
            
            // Calculate predicted value using Mendelian inheritance
            CalculateMendelianInheritance(prediction, parent1Trait, parent2Trait);
            
            // Add mutation probability
            prediction.MutationChance = CalculateMutationProbability(parent1Trait, parent2Trait);
            
            // Calculate confidence based on dominance clarity
            prediction.Confidence = CalculateTraitConfidence(parent1Trait, parent2Trait);
            
            return prediction;
        }
        
        private void CalculateMendelianInheritance(TraitPrediction prediction, TraitExpression parent1, TraitExpression parent2)
        {
            // Determine dominance relationships
            float dominanceRatio = parent1.DominanceStrength / (parent1.DominanceStrength + parent2.DominanceStrength);
            
            // Basic Mendelian ratio (accounting for dominance)
            if (Mathf.Abs(parent1.DominanceStrength - parent2.DominanceStrength) > 0.3f)
            {
                // Clear dominant/recessive relationship
                if (parent1.DominanceStrength > parent2.DominanceStrength)
                {
                    prediction.PredictedValue = parent1.Value * 0.75f + parent2.Value * 0.25f;
                    prediction.IsDominant = true;
                    prediction.DominantParent = 1;
                }
                else
                {
                    prediction.PredictedValue = parent2.Value * 0.75f + parent1.Value * 0.25f;
                    prediction.IsDominant = true;
                    prediction.DominantParent = 2;
                }
            }
            else
            {
                // Co-dominance or incomplete dominance
                prediction.PredictedValue = (parent1.Value + parent2.Value) / 2f;
                prediction.IsDominant = false;
                prediction.DominantParent = 0;
            }
            
            // Add some random variation (genetic recombination)
            float variation = UnityEngine.Random.Range(-0.1f, 0.1f);
            prediction.PredictedValue = Mathf.Clamp01(prediction.PredictedValue + variation);
            
            // Calculate parent contributions
            prediction.Parent1Contribution = dominanceRatio;
            prediction.Parent2Contribution = 1f - dominanceRatio;
        }
        
        #endregion
        
        #region Polygenic Analysis
        
        private void AnalyzePolygenicEffects(BreedingPrediction prediction, GeneticProfile parent1, GeneticProfile parent2)
        {
            // Analyze how multiple genes affect the same traits
            var polygenicGroups = new Dictionary<string, List<TraitPrediction>>
            {
                ["Physical"] = prediction.PredictedTraits.Where(t => IsPhysicalTrait(t.TraitType)).ToList(),
                ["Mental"] = prediction.PredictedTraits.Where(t => IsMentalTrait(t.TraitType)).ToList(),
                ["Special"] = prediction.PredictedTraits.Where(t => IsSpecialTrait(t.TraitType)).ToList()
            };
            
            foreach (var group in polygenicGroups)
            {
                if (group.Value.Count > 1)
                {
                    ApplyPolygenicInteractions(group.Value);
                }
            }
        }
        
        private void ApplyPolygenicInteractions(List<TraitPrediction> relatedTraits)
        {
            // Calculate synergistic or antagonistic effects
            float averageValue = relatedTraits.Average(t => t.PredictedValue);
            float synergisticBonus = 0f;
            
            // Check for synergistic combinations
            if (averageValue > 0.7f)
            {
                // High values in related traits boost each other
                synergisticBonus = 0.1f * (averageValue - 0.7f);
            }
            else if (averageValue < 0.3f)
            {
                // Low values can have compensatory effects
                synergisticBonus = -0.05f * (0.3f - averageValue);
            }
            
            // Apply the bonus to all traits in the group
            foreach (var trait in relatedTraits)
            {
                trait.PredictedValue = Mathf.Clamp01(trait.PredictedValue + synergisticBonus);
                trait.PolygenicEffect = synergisticBonus;
            }
        }
        
        #endregion
        
        #region Epistatic Interactions
        
        private void AnalyzeEpistaticInteractions(BreedingPrediction prediction, GeneticProfile parent1, GeneticProfile parent2)
        {
            // Analyze how genes affect the expression of other genes
            var epistaticPairs = new List<(Laboratory.Core.Enums.TraitType, Laboratory.Core.Enums.TraitType)>
            {
                (Laboratory.Core.Enums.TraitType.Intelligence, Laboratory.Core.Enums.TraitType.Sociability),
                (Laboratory.Core.Enums.TraitType.Strength, Laboratory.Core.Enums.TraitType.CombatSkill),
                (Laboratory.Core.Enums.TraitType.Speed, Laboratory.Core.Enums.TraitType.Intelligence),
                (Laboratory.Core.Enums.TraitType.Vitality, Laboratory.Core.Enums.TraitType.Stamina),
                (Laboratory.Core.Enums.TraitType.MagicalAffinity, Laboratory.Core.Enums.TraitType.Elemental)
            };
            
            foreach (var pair in epistaticPairs)
            {
                var trait1 = prediction.PredictedTraits.FirstOrDefault(t => t.TraitType == pair.Item1);
                var trait2 = prediction.PredictedTraits.FirstOrDefault(t => t.TraitType == pair.Item2);
                
                if (trait1 != null && trait2 != null)
                {
                    ApplyEpistaticEffect(trait1, trait2);
                }
            }
        }
        
        private void ApplyEpistaticEffect(TraitPrediction primaryTrait, TraitPrediction affectedTrait)
        {
            // Primary trait influences the expression of the affected trait
            float epistaticEffect = 0f;
            
            if (primaryTrait.PredictedValue > 0.8f)
            {
                // High primary trait enhances affected trait
                epistaticEffect = 0.15f * (primaryTrait.PredictedValue - 0.8f);
            }
            else if (primaryTrait.PredictedValue < 0.2f)
            {
                // Low primary trait suppresses affected trait
                epistaticEffect = -0.1f * (0.2f - primaryTrait.PredictedValue);
            }
            
            affectedTrait.PredictedValue = Mathf.Clamp01(affectedTrait.PredictedValue + epistaticEffect);
            affectedTrait.EpistaticEffect = epistaticEffect;
        }
        
        #endregion
        
        #region Environmental Factors
        
        private void ApplyEnvironmentalFactors(BreedingPrediction prediction, GeneticProfile parent1, GeneticProfile parent2)
        {
            // Get current environmental conditions
            var currentBiome = GetCurrentBiome();
            if (currentBiome == null) return;
            
            // Apply biome-specific modifications
            foreach (var trait in prediction.PredictedTraits)
            {
                float environmentalEffect = CalculateEnvironmentalEffect(trait.TraitType, currentBiome);
                trait.PredictedValue = Mathf.Clamp01(trait.PredictedValue + environmentalEffect);
                trait.EnvironmentalEffect = environmentalEffect;
            }
        }
        
        private float CalculateEnvironmentalEffect(Laboratory.Core.Enums.TraitType traitType, BiomeConfiguration biome)
        {
            // Different biomes favor different traits
            return traitType switch
            {
                Laboratory.Core.Enums.TraitType.Strength when biome.BiomeType == BiomeType.Mountain => 0.1f,
                Laboratory.Core.Enums.TraitType.Agility when biome.BiomeType == BiomeType.Forest => 0.1f,
                Laboratory.Core.Enums.TraitType.Intelligence when biome.BiomeType == BiomeType.Arctic => 0.05f,
                Laboratory.Core.Enums.TraitType.Stamina when biome.BiomeType == BiomeType.Desert => 0.15f,
                Laboratory.Core.Enums.TraitType.Adaptability when biome.BiomeType == BiomeType.Magical => 0.2f,
                _ => 0f
            };
        }
        
        #endregion
        
        #region Breeding Probabilities
        
        private void CalculateBreedingProbabilities(BreedingPrediction prediction)
        {
            // Calculate various breeding outcome probabilities
            prediction.RareTraitProbability = CalculateRareTraitProbability(prediction);
            prediction.SuperiorStatsProbability = CalculateSuperiorStatsProbability(prediction);
            prediction.MutationProbability = CalculateOverallMutationProbability(prediction);
            prediction.HybridVigorProbability = CalculateHybridVigorProbability(prediction);
            
            // Store in probability distribution
            prediction.ProbabilityDistribution["RareTraits"] = prediction.RareTraitProbability;
            prediction.ProbabilityDistribution["SuperiorStats"] = prediction.SuperiorStatsProbability;
            prediction.ProbabilityDistribution["Mutations"] = prediction.MutationProbability;
            prediction.ProbabilityDistribution["HybridVigor"] = prediction.HybridVigorProbability;
        }
        
        private float CalculateRareTraitProbability(BreedingPrediction prediction)
        {
            // Probability of getting rare traits or trait combinations
            float baseRarity = 0.1f;
            float parentRarityBonus = 0f;
            
            // Check parent rarity
            float parent1Rarity = CalculateGeneticRarity(prediction.Parent1Profile);
            float parent2Rarity = CalculateGeneticRarity(prediction.Parent2Profile);
            parentRarityBonus = (parent1Rarity + parent2Rarity) * 0.3f;
            
            return Mathf.Clamp01(baseRarity + parentRarityBonus);
        }
        
        private float CalculateSuperiorStatsProbability(BreedingPrediction prediction)
        {
            // Probability of offspring having superior stats to both parents
            float averageParent1Stats = CalculateAverageStats(prediction.Parent1Profile);
            float averageParent2Stats = CalculateAverageStats(prediction.Parent2Profile);
            float averageOffspringStats = prediction.PredictedTraits.Average(t => t.PredictedValue);
            
            float parentAverage = (averageParent1Stats + averageParent2Stats) / 2f;
            float improvement = averageOffspringStats - parentAverage;
            
            return Mathf.Clamp01(improvement * 2f);
        }
        
        private float CalculateOverallMutationProbability(BreedingPrediction prediction)
        {
            if (prediction.PredictedTraits.Count == 0) return 0f;
            
            return prediction.PredictedTraits.Average(t => t.MutationChance);
        }
        
        private float CalculateHybridVigorProbability(BreedingPrediction prediction)
        {
            // Probability of hybrid vigor (heterosis)
            float geneticDiversity = CalculateGeneticDiversity(prediction.Parent1Profile, prediction.Parent2Profile);
            return Mathf.Clamp01(geneticDiversity * 0.8f);
        }
        
        #endregion
        
        #region Utility Methods
        
        private HashSet<Laboratory.Core.Enums.TraitType> GetAllTraitTypes(GeneticProfile parent1, GeneticProfile parent2)
        {
            var traitTypes = new HashSet<Laboratory.Core.Enums.TraitType>();

            if (parent1?.TraitExpressions != null)
            {
                foreach (var trait in parent1.TraitExpressions)
                    traitTypes.Add(trait.Key);
            }

            if (parent2?.TraitExpressions != null)
            {
                foreach (var trait in parent2.TraitExpressions)
                    traitTypes.Add(trait.Key);
            }

            return traitTypes;
        }
        
        private TraitExpression GetTraitExpression(GeneticProfile profile, Laboratory.Core.Enums.TraitType traitType)
        {
            return profile?.TraitExpressions?.TryGetValue(traitType, out var trait) == true ? trait : null;
        }

        private float GetTraitValue(GeneticProfile genetics, TraitType traitType)
        {
            // Try to get the specific trait value first
            if (genetics?.TraitExpressions?.TryGetValue(traitType, out var traitExpression) == true)
            {
                return traitExpression.Value;
            }

            // Fall back to finding genes of this specific trait type
            if (genetics?.Genes == null) return 0f;

            var matchingGenes = genetics.Genes.Where(g => g.traitType == traitType).ToArray();
            if (matchingGenes.Length == 0) return 0f;

            return matchingGenes.Average(g => g.GetTraitValue());
        }

        private TraitExpression CreateDefaultTrait(Laboratory.Core.Enums.TraitType traitType)
        {
            return new TraitExpression(
                traitType.ToString(),
                0.5f,
                traitType
            )
            {
                dominanceStrength = 0.5f
            };
        }
        
        private float CalculateMutationProbability(TraitExpression parent1, TraitExpression parent2)
        {
            float baseMutationRate = 0.05f;
            float environmentalFactor = GetEnvironmentalMutationFactor();
            float parentAgeFactor = GetParentAgeFactor();
            
            return Mathf.Clamp01(baseMutationRate * environmentalFactor * parentAgeFactor);
        }
        
        private float CalculateTraitConfidence(TraitExpression parent1, TraitExpression parent2)
        {
            // Higher confidence when dominance is clear
            float dominanceDifference = Mathf.Abs(parent1.DominanceStrength - parent2.DominanceStrength);
            float baseConfidence = predictionAccuracy;
            float dominanceBonus = dominanceDifference * 0.2f;
            
            return Mathf.Clamp01(baseConfidence + dominanceBonus);
        }
        
        private float CalculateOverallConfidence(BreedingPrediction prediction)
        {
            if (prediction.PredictedTraits.Count == 0) return 0f;
            
            return prediction.PredictedTraits.Average(t => t.Confidence);
        }
        
        private bool IsPhysicalTrait(Laboratory.Core.Enums.TraitType traitType)
        {
            var category = traitType.GetCategory();
            return category == Laboratory.Core.Enums.TraitCategory.Physical ||
                   category == Laboratory.Core.Enums.TraitCategory.Metabolic;
        }

        private bool IsMentalTrait(Laboratory.Core.Enums.TraitType traitType)
        {
            var category = traitType.GetCategory();
            return category == Laboratory.Core.Enums.TraitCategory.Mental ||
                   category == Laboratory.Core.Enums.TraitCategory.Behavioral ||
                   category == Laboratory.Core.Enums.TraitCategory.Social;
        }

        private bool IsSpecialTrait(Laboratory.Core.Enums.TraitType traitType)
        {
            var category = traitType.GetCategory();
            return category == Laboratory.Core.Enums.TraitCategory.Special ||
                   category == Laboratory.Core.Enums.TraitCategory.Mutation;
        }
        
        private float CalculateGeneticRarity(GeneticProfile genetics)
        {
            if (genetics?.Mutations == null) return 0f;
            
            float rarityScore = 0f;
            foreach (var mutation in genetics.Mutations)
            {
                rarityScore += mutation.severity * 0.25f;
            }
            
            return Mathf.Clamp01(rarityScore);
        }
        
        private float CalculateAverageStats(GeneticProfile genetics)
        {
            if (genetics?.TraitExpressions == null || genetics.TraitExpressions.Count == 0) return 0f;
            
            var importantTraits = new[] { TraitType.Strength, TraitType.Speed, TraitType.Intelligence, TraitType.Stamina };
            var values = importantTraits.Select(trait => GetTraitValue(genetics, trait)).ToArray();
            
            return values.Length > 0 ? values.Average() : 0f;
        }
        
        
        private float CalculateGeneticDiversity(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Calculate how genetically diverse the parents are
            var commonTraits = GetCommonTraits(parent1, parent2);
            float diversityScore = 0f;
            
            foreach (var traitType in commonTraits)
            {
                var trait1 = GetTraitExpression(parent1, traitType);
                var trait2 = GetTraitExpression(parent2, traitType);
                
                if (trait1 != null && trait2 != null)
                {
                    float difference = Mathf.Abs(trait1.Value - trait2.Value);
                    diversityScore += difference;
                }
            }
            
            return commonTraits.Count > 0 ? diversityScore / commonTraits.Count : 0f;
        }
        
        private HashSet<Laboratory.Core.Enums.TraitType> GetCommonTraits(GeneticProfile parent1, GeneticProfile parent2)
        {
            var traits1 = parent1?.TraitExpressions?.Keys != null ? new HashSet<Laboratory.Core.Enums.TraitType>(parent1.TraitExpressions.Keys) : new HashSet<Laboratory.Core.Enums.TraitType>();
            var traits2 = parent2?.TraitExpressions?.Keys != null ? new HashSet<Laboratory.Core.Enums.TraitType>(parent2.TraitExpressions.Keys) : new HashSet<Laboratory.Core.Enums.TraitType>();

            traits1.IntersectWith(traits2);
            return traits1;
        }
        
        private BiomeConfiguration GetCurrentBiome()
        {
            // This would integrate with your biome system
            return null; // Placeholder
        }
        
        private float GetEnvironmentalMutationFactor()
        {
            // Environmental factors that affect mutation rates
            // Factors like temperature, radiation, season, etc.
            float environmentalFactor = 1.0f;

            // Simulate seasonal effects
            float seasonFactor = 0.8f + 0.4f * Mathf.Sin(Time.time * 0.1f); // Varies between 0.8-1.2
            environmentalFactor *= seasonFactor;

            // Simulate random environmental stress
            float stressFactor = UnityEngine.Random.Range(0.9f, 1.1f);
            environmentalFactor *= stressFactor;

            return Mathf.Clamp(environmentalFactor, 0.5f, 2.0f);
        }

        private float GetParentAgeFactor()
        {
            // Older parents might have slightly higher mutation rates
            // This would normally use actual parent age data

            // Simulate age effect - assume random age between 1-10 generations
            float simulatedAge = UnityEngine.Random.Range(1f, 10f);

            // Age factor increases mutation rate slightly with age
            float ageFactor = 1.0f + (simulatedAge - 1f) * 0.02f; // +2% per generation

            return Mathf.Clamp(ageFactor, 1.0f, 1.2f);
        }
        
        #endregion
        
        #region Caching
        
        private string GenerateCacheKey(GeneticProfile parent1, GeneticProfile parent2)
        {
            return $"{parent1.ProfileId}_{parent2.ProfileId}";
        }
        
        private void CachePrediction(string key, BreedingPrediction prediction)
        {
            if (predictionCache.Count >= MAX_CACHE_SIZE)
            {
                // Remove oldest entry
                var oldestKey = predictionCache.Keys.First();
                predictionCache.Remove(oldestKey);
            }
            
            predictionCache[key] = prediction;
        }
        
        public void ClearPredictionCache()
        {
            predictionCache.Clear();
        }
        
        #endregion
    }
    
    /// <summary>
    /// Advanced breeding compatibility analyzer that evaluates how well two creatures
    /// will breed together based on genetics, health, environmental factors, and lineage.
    /// </summary>
    public class BreedingCompatibilityAnalyzer
    {
        [Header("Compatibility Weights")]
        [SerializeField] private float geneticDiversityWeight = 0.3f;
        [SerializeField] private float traitSynergyWeight = 0.25f;
        [SerializeField] private float healthWeight = 0.2f;
        [SerializeField] private float environmentalWeight = 0.15f;
        [SerializeField] private float lineageWeight = 0.1f;
        
        public BreedingCompatibility AnalyzeCompatibility(GeneticProfile parent1, GeneticProfile parent2)
        {
            if (parent1 == null || parent2 == null)
                return new BreedingCompatibility { OverallCompatibility = 0f };
            
            var compatibility = new BreedingCompatibility();
            
            // Analyze different compatibility factors
            compatibility.GeneticDiversity = AnalyzeGeneticDiversity(parent1, parent2);
            compatibility.TraitSynergy = AnalyzeTraitSynergy(parent1, parent2);
            compatibility.HealthCompatibility = AnalyzeHealthCompatibility(parent1, parent2);
            compatibility.EnvironmentalSuitability = AnalyzeEnvironmentalSuitability(parent1, parent2);
            compatibility.LineageCompatibility = AnalyzeLineageCompatibility(parent1, parent2);
            compatibility.MutationPotential = AnalyzeMutationPotential(parent1, parent2);
            
            // Calculate overall compatibility score
            compatibility.OverallCompatibility = CalculateOverallCompatibility(compatibility);
            
            // Generate compatibility explanation
            compatibility.CompatibilityExplanation = GenerateCompatibilityExplanation(compatibility);
            
            return compatibility;
        }
        
        private float AnalyzeGeneticDiversity(GeneticProfile parent1, GeneticProfile parent2)
        {
            var commonTraits = GetCommonTraitTypes(parent1, parent2);
            if (commonTraits.Count == 0) return 0.5f;
            
            float totalDiversity = 0f;
            
            foreach (var traitType in commonTraits)
            {
                var trait1 = GetTraitExpression(parent1, traitType);
                var trait2 = GetTraitExpression(parent2, traitType);
                
                if (trait1 != null && trait2 != null)
                {
                    float valueDifference = Mathf.Abs(trait1.Value - trait2.Value);
                    float dominanceDifference = Mathf.Abs(trait1.DominanceStrength - trait2.DominanceStrength);
                    
                    totalDiversity += (valueDifference + dominanceDifference) / 2f;
                }
            }
            
            return totalDiversity / commonTraits.Count;
        }
        
        private float AnalyzeTraitSynergy(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Analyze how well the traits work together
            var synergyPairs = new List<(Laboratory.Core.Enums.TraitType, Laboratory.Core.Enums.TraitType)>
            {
                (Laboratory.Core.Enums.TraitType.Size, Laboratory.Core.Enums.TraitType.Strength),
                (Laboratory.Core.Enums.TraitType.Intelligence, Laboratory.Core.Enums.TraitType.Aggression),
                (Laboratory.Core.Enums.TraitType.Speed, Laboratory.Core.Enums.TraitType.Agility),
                (Laboratory.Core.Enums.TraitType.Intelligence, Laboratory.Core.Enums.TraitType.MagicalAffinity)
            };
            
            float totalSynergy = 0f;
            int validPairs = 0;
            
            foreach (var pair in synergyPairs)
            {
                var trait1_p1 = GetTraitValue(parent1, pair.Item1);
                var trait2_p1 = GetTraitValue(parent1, pair.Item2);
                var trait1_p2 = GetTraitValue(parent2, pair.Item1);
                var trait2_p2 = GetTraitValue(parent2, pair.Item2);
                
                if (trait1_p1 > 0 && trait2_p1 > 0 && trait1_p2 > 0 && trait2_p2 > 0)
                {
                    float parent1Synergy = trait1_p1 * trait2_p1;
                    float parent2Synergy = trait1_p2 * trait2_p2;
                    float combinedSynergy = (parent1Synergy + parent2Synergy) / 2f;
                    
                    totalSynergy += combinedSynergy;
                    validPairs++;
                }
            }
            
            return validPairs > 0 ? totalSynergy / validPairs : 0.5f;
        }
        
        private float AnalyzeHealthCompatibility(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Check for genetic health compatibility using available traits
            float health1 = GetTraitValue(parent1, Laboratory.Core.Enums.TraitType.Vitality);
            float health2 = GetTraitValue(parent2, Laboratory.Core.Enums.TraitType.Vitality);
            float constitution1 = GetTraitValue(parent1, Laboratory.Core.Enums.TraitType.Stamina);
            float constitution2 = GetTraitValue(parent2, Laboratory.Core.Enums.TraitType.Stamina);
            
            float averageHealth = (health1 + health2) / 2f;
            float averageConstitution = (constitution1 + constitution2) / 2f;
            
            // Bonus for both parents having good health
            float healthBonus = 0f;
            if (health1 > 0.7f && health2 > 0.7f)
                healthBonus = 0.2f;
            
            return Mathf.Clamp01(averageHealth * 0.6f + averageConstitution * 0.4f + healthBonus);
        }
        
        private float AnalyzeEnvironmentalSuitability(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Check how well they adapt to current environment
            float adaptation1 = GetTraitValue(parent1, Laboratory.Core.Enums.TraitType.Environmental);
            float adaptation2 = GetTraitValue(parent2, Laboratory.Core.Enums.TraitType.Environmental);
            
            return (adaptation1 + adaptation2) / 2f;
        }
        
        private float AnalyzeLineageCompatibility(GeneticProfile parent1, GeneticProfile parent2)
        {
            // Check for inbreeding or beneficial lineage mixing
            float generationDifference = Mathf.Abs(parent1.GenerationNumber - parent2.GenerationNumber);
            
            // Optimal is 1-2 generation difference
            if (generationDifference >= 1 && generationDifference <= 2)
                return 1.0f;
            else if (generationDifference == 0)
                return 0.7f; // Same generation is okay
            else
                return Mathf.Clamp01(1f - (generationDifference - 2f) * 0.1f);
        }
        
        private float AnalyzeMutationPotential(GeneticProfile parent1, GeneticProfile parent2)
        {
            int mutation1Count = parent1.Mutations?.Count ?? 0;
            int mutation2Count = parent2.Mutations?.Count ?? 0;
            
            // Some mutations increase potential for beneficial outcomes
            float mutationBonus = 0f;
            if (mutation1Count > 0 || mutation2Count > 0)
            {
                mutationBonus = 0.1f + (mutation1Count + mutation2Count) * 0.05f;
            }
            
            return Mathf.Clamp01(0.5f + mutationBonus);
        }
        
        private float CalculateOverallCompatibility(BreedingCompatibility compatibility)
        {
            return compatibility.GeneticDiversity * geneticDiversityWeight +
                   compatibility.TraitSynergy * traitSynergyWeight +
                   compatibility.HealthCompatibility * healthWeight +
                   compatibility.EnvironmentalSuitability * environmentalWeight +
                   compatibility.LineageCompatibility * lineageWeight;
        }
        
        private string GenerateCompatibilityExplanation(BreedingCompatibility compatibility)
        {
            var explanations = new List<string>();
            
            if (compatibility.GeneticDiversity > 0.8f)
                explanations.Add("Excellent genetic diversity");
            else if (compatibility.GeneticDiversity < 0.3f)
                explanations.Add("Low genetic diversity");
            
            if (compatibility.TraitSynergy > 0.7f)
                explanations.Add("Strong trait synergy");
            
            if (compatibility.HealthCompatibility > 0.8f)
                explanations.Add("Excellent health compatibility");
            else if (compatibility.HealthCompatibility < 0.4f)
                explanations.Add("Health concerns");
            
            if (compatibility.MutationPotential > 0.7f)
                explanations.Add("High mutation potential");
            
            return explanations.Count > 0 ? string.Join(", ", explanations) : "Standard compatibility";
        }
        
        #region Utility Methods
        
        private HashSet<Laboratory.Core.Enums.TraitType> GetCommonTraitTypes(GeneticProfile parent1, GeneticProfile parent2)
        {
            var traits1 = parent1?.TraitExpressions?.Keys != null ? new HashSet<Laboratory.Core.Enums.TraitType>(parent1.TraitExpressions.Keys) : new HashSet<Laboratory.Core.Enums.TraitType>();
            var traits2 = parent2?.TraitExpressions?.Keys != null ? new HashSet<Laboratory.Core.Enums.TraitType>(parent2.TraitExpressions.Keys) : new HashSet<Laboratory.Core.Enums.TraitType>();

            traits1.IntersectWith(traits2);
            return traits1;
        }
        
        private TraitExpression GetTraitExpression(GeneticProfile profile, Laboratory.Core.Enums.TraitType traitType)
        {
            return profile?.TraitExpressions?.TryGetValue(traitType, out var trait) == true ? trait : null;
        }

        private float GetTraitValue(GeneticProfile profile, Laboratory.Core.Enums.TraitType traitType)
        {
            var trait = GetTraitExpression(profile, traitType);
            return trait?.Value ?? 0f;
        }
        
        #endregion
    }
    
    #region Supporting Data Structures
    
    [System.Serializable]
    public class BreedingPrediction
    {
        public GeneticProfile Parent1Profile;
        public GeneticProfile Parent2Profile;
        public List<TraitPrediction> PredictedTraits;
        public Dictionary<string, float> ProbabilityDistribution;
        public float ConfidenceScore;
        public float RareTraitProbability;
        public float SuperiorStatsProbability;
        public float MutationProbability;
        public float HybridVigorProbability;
    }
    
    [System.Serializable]
    public class TraitPrediction
    {
        public Laboratory.Core.Enums.TraitType TraitType;
        public float PredictedValue;
        public float Parent1Value;
        public float Parent2Value;
        public float Parent1Dominance;
        public float Parent2Dominance;
        public float Parent1Contribution;
        public float Parent2Contribution;
        public float MutationChance;
        public float Confidence;
        public bool IsDominant;
        public int DominantParent; // 0 = co-dominant, 1 = parent1, 2 = parent2
        public float PolygenicEffect;
        public float EpistaticEffect;
        public float EnvironmentalEffect;
    }
    
    [System.Serializable]
    public class BreedingCompatibility
    {
        public float OverallCompatibility;
        public float GeneticDiversity;
        public float TraitSynergy;
        public float HealthCompatibility;
        public float EnvironmentalSuitability;
        public float LineageCompatibility;
        public float MutationPotential;
        public string CompatibilityExplanation;
    }
    
    // Placeholder for BiomeConfiguration
    public class BiomeConfiguration
    {
        public BiomeType BiomeType;
    }
    
    #endregion
}