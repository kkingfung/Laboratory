using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Events;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Breeding;

namespace Laboratory.Chimera.Breeding
{
    /// <summary>
    /// Core breeding system for Project Chimera - handles creature mating,
    /// genetic combination, and offspring generation with realistic breeding mechanics.
    /// </summary>
    public class BreedingSystem : IBreedingSystem, IDisposable
    {
        private readonly IEventBus _eventBus;
        private bool _disposed = false;
        
        public BreedingSystem(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }
        
        /// <summary>
        /// Calculates environmental influence on breeding success based on biome conditions.
        /// Factors include food availability, stress levels, predator pressure, and population density.
        /// Environmental conditions can significantly boost or hinder breeding outcomes.
        /// </summary>
        /// <param name="environment">Breeding environment data, null defaults to neutral conditions</param>
        /// <returns>Environmental modifier (0.1 to 2.0, where 1.0 = neutral, >1.0 = beneficial, <1.0 = detrimental)</returns>
        private float CalculateEnvironmentalFactor(BreedingEnvironment environment)
        {
            if (environment == null) return 1f; // Neutral environment

            float environmentFactor = 1f;

            // Base environmental breeding success multiplier
            environmentFactor *= environment.BreedingSuccessMultiplier;

            // Food scarcity reduces breeding success, abundance improves it
            environmentFactor *= Mathf.Lerp(0.6f, 1.2f, environment.FoodAvailability);

            // Stress and predator pressure create negative breeding conditions
            float stressFactor = 1f - (environment.StressLevel * 0.3f); // Max 30% reduction
            float predatorFactor = 1f - (environment.PredatorPressure * 0.2f); // Max 20% reduction
            environmentFactor *= stressFactor * predatorFactor;

            // Comfortable environments encourage breeding
            environmentFactor *= Mathf.Lerp(0.8f, 1.1f, environment.ComfortLevel);

            // Overcrowding reduces breeding success due to competition and stress
            float populationFactor = environment.PopulationDensity;
            if (populationFactor > 0.7f) // Above 70% capacity
            {
                environmentFactor *= Mathf.Lerp(1f, 0.7f, (populationFactor - 0.7f) / 0.3f);
            }

            // Clamp to reasonable bounds (10% to 200% of base success rate)
            return Mathf.Clamp(environmentFactor, 0.1f, 2f);
        }
        
        #region IBreedingSystem Implementation
        
        public BreedingResult AttemptBreeding(CreatureInstance parent1, CreatureInstance parent2)
        {
            return AttemptBreeding(parent1, parent2, null);
        }
        
        public BreedingResult AttemptBreeding(CreatureInstance parent1, CreatureInstance parent2, BreedingEnvironment environment)
        {
            if (parent1 == null || parent2 == null)
                return BreedingResult.CreateFailure("Parent creatures cannot be null");
                
            try
            {
                // Phase 1: Validate breeding compatibility
                var compatibilityResult = ValidateBreedingCompatibility(parent1, parent2);
                if (!compatibilityResult.Success)
                    return compatibilityResult;
                    
                // Phase 2: Calculate breeding success chance (including environment factors)
                float successChance = CalculateBreedingSuccessChance(parent1, parent2, environment);
                bool breedingSuccessful = UnityEngine.Random.value <= successChance;
                
                if (!breedingSuccessful)
                    return BreedingResult.CreateFailure("Breeding attempt failed due to low compatibility");
                
                // Phase 3: Generate offspring genetics (with environmental influences)
                var offspringGenetics = CombineGeneticsFromParents(parent1, parent2, environment);
                
                // Phase 4: Create offspring creature instance
                var offspring = CreateOffspring(parent1, parent2, offspringGenetics);
                
                // Phase 5: Fire breeding events
                _eventBus?.Publish(new BreedingSuccessEvent(parent1, parent2, offspring));
                
                return BreedingResult.CreateSuccess(offspring, successChance);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Breeding system error: {ex.Message}");
                return BreedingResult.CreateFailure($"Internal breeding error: {ex.Message}");
            }
        }
        
        public async UniTask<BreedingResult> AttemptBreedingAsync(CreatureInstance parent1, CreatureInstance parent2, CancellationToken cancellationToken = default)
        {
            if (parent1 == null || parent2 == null)
                return BreedingResult.CreateFailure("Parent creatures cannot be null");
                
            try
            {
                // Simulate breeding process time (can be used for animations/UI)
                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), cancellationToken: cancellationToken);
                
                return AttemptBreeding(parent1, parent2);
            }
            catch (OperationCanceledException)
            {
                return BreedingResult.CreateFailure("Breeding operation was cancelled");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Async breeding system error: {ex.Message}");
                return BreedingResult.CreateFailure($"Internal breeding error: {ex.Message}");
            }
        }
        
        public async UniTask<BreedingResult> BreedCreaturesAsync(CreatureInstance parent1, CreatureInstance parent2, CancellationToken cancellationToken = default)
        {
            // This method implements the interface requirement and delegates to AttemptBreedingAsync
            return await AttemptBreedingAsync(parent1, parent2, cancellationToken);
        }
        
        public float CalculateBreedingSuccessChance(CreatureInstance parent1, CreatureInstance parent2)
        {
            return CalculateBreedingSuccessChance(parent1, parent2, null);
        }
        
        public float CalculateBreedingSuccessChance(CreatureInstance parent1, CreatureInstance parent2, BreedingEnvironment environment)
        {
            if (parent1 == null || parent2 == null)
                return 0f;
                
            // Base success rate
            float baseSuccessRate = 0.7f;
            
            // Factor in species compatibility
            float speciesCompatibility = CalculateSpeciesCompatibility(parent1, parent2);
            
            // Factor in health and age
            float healthFactor = (parent1.HealthPercentage + parent2.HealthPercentage) / 2f;
            float ageFactor = CalculateAgeFactor(parent1, parent2);
            
            // Factor in genetic diversity (higher diversity = better breeding)
            float geneticDiversityFactor = CalculateGeneticDiversity(parent1, parent2);
            
            // Apply environmental modifiers if environment is provided
            float environmentalFactor = 1f;
            if (environment != null)
            {
                environmentalFactor = CalculateEnvironmentalFactor(environment);
            }
            
            float finalChance = baseSuccessRate * speciesCompatibility * healthFactor * ageFactor * geneticDiversityFactor * environmentalFactor;
            
            return Mathf.Clamp01(finalChance);
        }
        
        public bool CanBreed(CreatureInstance parent1, CreatureInstance parent2)
        {
            if (parent1 == null || parent2 == null)
                return false;
                
            // Check if both creatures are alive
            if (!parent1.IsAlive || !parent2.IsAlive)
                return false;
                
            // Check if creatures are not the same instance
            if (parent1.InstanceId == parent2.InstanceId)
                return false;
                
            // Check maturity (simplified - assuming creatures need to be adult)
            if (parent1.Age < parent1.Species.MaturityAge || parent2.Age < parent2.Species.MaturityAge)
                return false;
                
            // Check species compatibility
            return CalculateSpeciesCompatibility(parent1, parent2) > 0.1f;
        }
        
        #endregion
        
        #region Private Helper Methods
        
        private BreedingResult ValidateBreedingCompatibility(CreatureInstance parent1, CreatureInstance parent2)
        {
            if (!CanBreed(parent1, parent2))
                return BreedingResult.CreateFailure("Creatures are not compatible for breeding");
                
            return BreedingResult.CreateSuccess(null, 1.0f);
        }
        
        private float CalculateSpeciesCompatibility(CreatureInstance parent1, CreatureInstance parent2)
        {
            if (parent1.Species.Id == parent2.Species.Id)
                return 1.0f; // Same species - perfect compatibility
                
            // Different species - check genetic similarity
            // This is a simplified version - in a real system you'd compare genetic markers
            float geneticSimilarity = CompareGeneticSimilarity(parent1.Genetics, parent2.Genetics);
            
            // Species can breed if they're genetically similar enough
            return geneticSimilarity > 0.3f ? geneticSimilarity * 0.8f : 0f;
        }
        
        /// <summary>
        /// Compares genetic similarity between two creatures for breeding compatibility assessment.
        /// Analyzes base stat distributions to determine how genetically related the creatures are.
        /// Used to calculate species compatibility and inbreeding risk.
        /// </summary>
        /// <param name="genetics1">First creature's genetic profile</param>
        /// <param name="genetics2">Second creature's genetic profile</param>
        /// <returns>Similarity score (0.0 = completely different, 1.0 = identical genetics)</returns>
        private float CompareGeneticSimilarity(CreatureGenetics genetics1, CreatureGenetics genetics2)
        {
            if (genetics1 == null || genetics2 == null)
                return 0f;

            // Simplified genetic similarity based on statistical trait comparison
            // In a complete system, this would analyze individual genes and allele frequencies
            float similarity = 0f;
            int comparisons = 0;

            // Compare base statistical traits (strength, agility, intelligence, endurance)
            // CreatureStats is a struct, so check for valid data using total power
            if (genetics1.BaseStats.GetTotalPower() > 0 && genetics2.BaseStats.GetTotalPower() > 0)
            {
                similarity += CompareStatsSimilarity(genetics1.BaseStats, genetics2.BaseStats);
                comparisons++;
            }

            // Return averaged similarity across all successful comparisons
            return comparisons > 0 ? similarity / comparisons : 0f;
        }
        
        /// <summary>
        /// Calculates normalized similarity between two creature stat profiles.
        /// Uses relative difference comparison to handle creatures of different power levels fairly.
        /// Essential for determining breeding compatibility across different species and generations.
        /// </summary>
        /// <param name="stats1">First creature's base statistics</param>
        /// <param name="stats2">Second creature's base statistics</param>
        /// <returns>Average similarity across all stats (0.0 = completely different, 1.0 = identical)</returns>
        private float CompareStatsSimilarity(CreatureStats stats1, CreatureStats stats2)
        {
            // Normalized relative difference calculation prevents bias toward high/low stat creatures
            // Formula: 1 - |diff| / max(stat1, stat2, 1) ensures meaningful comparison
            float strengthSim = 1f - Mathf.Abs(stats1.attack - stats2.attack) / Mathf.Max(stats1.attack, stats2.attack, 1f);
            float agilitySim = 1f - Mathf.Abs(stats1.speed - stats2.speed) / Mathf.Max(stats1.speed, stats2.speed, 1f);
            float intelligenceSim = 1f - Mathf.Abs(stats1.intelligence - stats2.intelligence) / Mathf.Max(stats1.intelligence, stats2.intelligence, 1f);
            float enduranceSim = 1f - Mathf.Abs(stats1.defense - stats2.defense) / Mathf.Max(stats1.defense, stats2.defense, 1f);

            // Return weighted average (all stats equally important for breeding compatibility)
            return (strengthSim + agilitySim + intelligenceSim + enduranceSim) / 4f;
        }
        
        private float CalculateAgeFactor(CreatureInstance parent1, CreatureInstance parent2)
        {
            // Optimal breeding age is between maturity and middle age
            float optimalAgeFactor1 = CalculateOptimalAgeFactor(parent1);
            float optimalAgeFactor2 = CalculateOptimalAgeFactor(parent2);
            
            return (optimalAgeFactor1 + optimalAgeFactor2) / 2f;
        }
        
        private float CalculateOptimalAgeFactor(CreatureInstance creature)
        {
            float ageRatio = creature.Age / creature.Species.MaxLifespan;
            
            if (ageRatio < 0.2f) return 0.5f; // Too young
            if (ageRatio < 0.6f) return 1.0f; // Optimal breeding age
            if (ageRatio < 0.8f) return 0.8f; // Still good
            return 0.3f; // Too old
        }
        
        /// <summary>
        /// Calculates genetic diversity factor for breeding success and offspring health.
        /// Balances the need for compatibility with the benefits of genetic diversity.
        /// Prevents inbreeding while ensuring species can still reproduce successfully.
        /// </summary>
        /// <param name="parent1">First parent creature</param>
        /// <param name="parent2">Second parent creature</param>
        /// <returns>Diversity factor (0.4-1.0, where 1.0 = optimal genetic diversity)</returns>
        private float CalculateGeneticDiversity(CreatureInstance parent1, CreatureInstance parent2)
        {
            // Calculate genetic similarity to assess diversity level
            float similarity = CompareGeneticSimilarity(parent1.Genetics, parent2.Genetics);

            // Genetic diversity sweet spot analysis:
            // - Too different: species incompatibility, low success rate
            // - Moderate difference: optimal diversity, healthy offspring
            // - Too similar: inbreeding risk, genetic problems
            if (similarity < 0.3f) return 0.5f; // Too genetically distant
            if (similarity < 0.7f) return 1.0f; // Optimal genetic diversity range
            if (similarity < 0.9f) return 0.8f; // Acceptable but slightly inbred
            return 0.4f; // High inbreeding risk, poor offspring viability
        }
        
        private CreatureGenetics CombineGeneticsFromParents(CreatureInstance parent1, CreatureInstance parent2, BreedingEnvironment environment = null)
        {
            // Create new genetics by combining parent genetics
            var combinedGenetics = new CreatureGenetics();
            
            // Combine base stats (average with some random variation)
            combinedGenetics.BaseStats = CombineStats(parent1.Genetics.BaseStats, parent2.Genetics.BaseStats);
            
            // Inherit traits from both parents
            combinedGenetics.InheritedTraits = CombineTraits(parent1.Genetics.InheritedTraits, parent2.Genetics.InheritedTraits);
            
            // Add potential mutations (influenced by environment)
            ApplyRandomMutations(combinedGenetics, environment);
            
            return combinedGenetics;
        }
        
        private CreatureStats CombineStats(CreatureStats stats1, CreatureStats stats2)
        {
            var combinedStats = new CreatureStats();
            
            // Combine stats with random inheritance and slight variation
            combinedStats.attack = Mathf.RoundToInt(CombineStatValue(stats1.attack, stats2.attack));
            combinedStats.speed = Mathf.RoundToInt(CombineStatValue(stats1.speed, stats2.speed));
            combinedStats.intelligence = Mathf.RoundToInt(CombineStatValue(stats1.intelligence, stats2.intelligence));
            combinedStats.defense = Mathf.RoundToInt(CombineStatValue(stats1.defense, stats2.defense));
            
            // Set other stats (health and charisma)
            combinedStats.health = Mathf.RoundToInt(CombineStatValue(stats1.health, stats2.health));
            combinedStats.charisma = Mathf.RoundToInt(CombineStatValue(stats1.charisma, stats2.charisma));
            
            return combinedStats;
        }
        
        private float CombineStatValue(float stat1, float stat2)
        {
            // Random inheritance from either parent, with some variation
            float inheritanceRatio = UnityEngine.Random.Range(0.3f, 0.7f);
            float baseCombined = stat1 * inheritanceRatio + stat2 * (1f - inheritanceRatio);
            
            // Add small random variation (±10%)
            float variation = UnityEngine.Random.Range(-0.1f, 0.1f);
            
            return Mathf.Max(0.1f, baseCombined * (1f + variation));
        }
        
        private System.Collections.Generic.List<string> CombineTraits(System.Collections.Generic.List<string> traits1, System.Collections.Generic.List<string> traits2)
        {
            var combinedTraits = new System.Collections.Generic.List<string>();
            
            // Randomly inherit traits from both parents
            if (traits1 != null)
            {
                foreach (var trait in traits1)
                {
                    if (UnityEngine.Random.value > 0.5f) // 50% chance to inherit each trait
                        combinedTraits.Add(trait);
                }
            }
            
            if (traits2 != null)
            {
                foreach (var trait in traits2)
                {
                    if (UnityEngine.Random.value > 0.5f && !combinedTraits.Contains(trait))
                        combinedTraits.Add(trait);
                }
            }
            
            return combinedTraits;
        }
        
        private void ApplyRandomMutations(CreatureGenetics genetics, BreedingEnvironment environment = null)
        {
            float mutationChance = 0.05f; // 5% base chance for mutations
            
            // Environmental factors can influence mutation rate
            if (environment != null)
            {
                mutationChance *= environment.MutationRateModifier;
            }
            
            if (UnityEngine.Random.value <= mutationChance)
            {
                // Apply random stat mutation
                int statToMutate = UnityEngine.Random.Range(0, 4);
                float mutationStrength = UnityEngine.Random.Range(-0.2f, 0.2f);
                
                var currentStats = genetics.BaseStats;
                
                switch (statToMutate)
                {
                    case 0: currentStats.attack = Mathf.RoundToInt(currentStats.attack * (1f + mutationStrength)); break;
                    case 1: currentStats.speed = Mathf.RoundToInt(currentStats.speed * (1f + mutationStrength)); break;
                    case 2: currentStats.intelligence = Mathf.RoundToInt(currentStats.intelligence * (1f + mutationStrength)); break;
                    case 3: currentStats.defense = Mathf.RoundToInt(currentStats.defense * (1f + mutationStrength)); break;
                }
                
                // Ensure stats don't go below minimum
                currentStats.attack = Mathf.Max(1, currentStats.attack);
                currentStats.speed = Mathf.Max(1, currentStats.speed);
                currentStats.intelligence = Mathf.Max(1, currentStats.intelligence);
                currentStats.defense = Mathf.Max(1, currentStats.defense);
                
                genetics.BaseStats = currentStats;
            }
        }
        
        private GeneticProfile CreateGeneticProfileFromGenetics(CreatureGenetics genetics)
        {
            // Create a list to hold the genes we'll create
            var genesList = new List<Gene>();
            
            // Convert base stats to genes
            if (genetics?.BaseStats != null)
            {
                genesList.Add(CreateGeneFromStat(TraitType.Strength, genetics.BaseStats.attack));
                genesList.Add(CreateGeneFromStat(TraitType.Agility, genetics.BaseStats.speed));
                genesList.Add(CreateGeneFromStat(TraitType.Intelligence, genetics.BaseStats.intelligence));
                genesList.Add(CreateGeneFromStat(TraitType.Stamina, genetics.BaseStats.defense));
            }
            
            // Create the genetic profile with the genes array and generation info
            var profile = new GeneticProfile(genesList.ToArray(), 1); // Generation 1 for offspring
            
            return profile;
        }
        
        private Gene CreateGeneFromStat(TraitType traitType, float statValue)
        {
            // Normalize stat value to 0-1 range (assuming max stat is around 50)
            float normalizedValue = Mathf.Clamp01(statValue / 50f);

            // Use trait type display name for UI purposes only
            string displayName = traitType.ToString();

            return new Gene(
                Guid.NewGuid().ToString(),
                displayName,
                traitType,
                Allele.CreateDominant(displayName, normalizedValue),
                Allele.CreateRecessive(displayName, normalizedValue * 0.8f)
            );
        }
        
        private CreatureInstance CreateOffspring(CreatureInstance parent1, CreatureInstance parent2, CreatureGenetics genetics)
        {
            // Determine offspring species (usually same as parents if same species)
            var offspringSpecies = DetermineOffspringSpecies(parent1.Species, parent2.Species);
            
            // Create offspring definition based on parents (simplified - use parent1's definition for now)
            var offspringDefinition = parent1.Definition;
            
            // Create genetic profile from the combined genetics
            var offspringGeneticProfile = CreateGeneticProfileFromGenetics(genetics);
            
            // Create new creature instance using the proper constructor
            var offspring = new CreatureInstance(offspringDefinition, offspringGeneticProfile)
            {
                AgeInDays = 0, // Born as infant
                Happiness = 0.8f, // Born happy
                IsWild = false, // Bred creatures are not wild
                Experience = 0,
                Level = 1
            };
            
            // Set health to maximum for newborn
            if (offspringDefinition != null)
            {
                offspring.CurrentHealth = offspringDefinition.baseStats.health;
            }
            
            // Set parents
            offspring.Parents.Add(parent1.InstanceId);
            offspring.Parents.Add(parent2.InstanceId);
            
            return offspring;
        }
        
        private CreatureSpecies DetermineOffspringSpecies(CreatureSpecies species1, CreatureSpecies species2)
        {
            // If same species, offspring is same species
            if (species1.Id == species2.Id)
                return species1;
                
            // Different species - for now, randomly choose one parent's species
            // In a more complex system, this could create hybrid species
            return UnityEngine.Random.value > 0.5f ? species1 : species2;
        }
        
        #endregion
        
        #region IDisposable Implementation
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
        }
        
        #endregion
    }
    
    #region Event Classes
    
    public class BreedingSuccessEvent : BaseEvent
    {
        public CreatureInstance Parent1 { get; }
        public CreatureInstance Parent2 { get; }
        public CreatureInstance Offspring { get; }
        
        public BreedingSuccessEvent(CreatureInstance parent1, CreatureInstance parent2, CreatureInstance offspring)
        {
            Parent1 = parent1 ?? throw new ArgumentNullException(nameof(parent1));
            Parent2 = parent2 ?? throw new ArgumentNullException(nameof(parent2));
            Offspring = offspring ?? throw new ArgumentNullException(nameof(offspring));
        }
    }
    
    #endregion
}