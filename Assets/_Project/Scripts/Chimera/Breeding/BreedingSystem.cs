using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Events;
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
        
        private float CalculateEnvironmentalFactor(BreedingEnvironment environment)
        {
            if (environment == null) return 1f;
            
            float environmentFactor = 1f;
            
            // Environmental conditions affect breeding success
            environmentFactor *= environment.BreedingSuccessMultiplier;
            
            // Food and resource availability
            environmentFactor *= Mathf.Lerp(0.6f, 1.2f, environment.FoodAvailability);
            
            // Stress and predator pressure reduce breeding success
            float stressFactor = 1f - (environment.StressLevel * 0.3f);
            float predatorFactor = 1f - (environment.PredatorPressure * 0.2f);
            environmentFactor *= stressFactor * predatorFactor;
            
            // Comfort level improves breeding
            environmentFactor *= Mathf.Lerp(0.8f, 1.1f, environment.ComfortLevel);
            
            // Population density can negatively affect breeding if too high
            float populationFactor = environment.PopulationDensity;
            if (populationFactor > 0.7f)
            {
                environmentFactor *= Mathf.Lerp(1f, 0.7f, (populationFactor - 0.7f) / 0.3f);
            }
            
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
        
        private float CompareGeneticSimilarity(CreatureGenetics genetics1, CreatureGenetics genetics2)
        {
            if (genetics1 == null || genetics2 == null)
                return 0f;
                
            // Simplified genetic similarity calculation
            // In a real system, this would compare actual genetic markers
            float similarity = 0f;
            int comparisons = 0;
            
            // Compare major genetic traits - CreatureStats is a struct so it's always "not null"
            // Check if the genetics have BaseStats (assumes they're valid if genetics object exists)
            if (genetics1.BaseStats.GetTotalPower() > 0 && genetics2.BaseStats.GetTotalPower() > 0)
            {
                similarity += CompareStatsSimilarity(genetics1.BaseStats, genetics2.BaseStats);
                comparisons++;
            }
            
            return comparisons > 0 ? similarity / comparisons : 0f;
        }
        
        private float CompareStatsSimilarity(CreatureStats stats1, CreatureStats stats2)
        {
            // Calculate how similar the base stats are (normalized comparison)
            float strengthSim = 1f - Mathf.Abs(stats1.Strength - stats2.Strength) / Mathf.Max(stats1.Strength, stats2.Strength, 1f);
            float agilitySim = 1f - Mathf.Abs(stats1.Agility - stats2.Agility) / Mathf.Max(stats1.Agility, stats2.Agility, 1f);
            float intelligenceSim = 1f - Mathf.Abs(stats1.Intelligence - stats2.Intelligence) / Mathf.Max(stats1.Intelligence, stats2.Intelligence, 1f);
            float enduranceSim = 1f - Mathf.Abs(stats1.Endurance - stats2.Endurance) / Mathf.Max(stats1.Endurance, stats2.Endurance, 1f);
            
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
        
        private float CalculateGeneticDiversity(CreatureInstance parent1, CreatureInstance parent2)
        {
            // Higher genetic diversity reduces inbreeding and improves offspring
            float similarity = CompareGeneticSimilarity(parent1.Genetics, parent2.Genetics);
            
            // Diversity is inverse of similarity, but we want some similarity for compatibility
            // Sweet spot is moderate similarity (around 0.5-0.7)
            if (similarity < 0.3f) return 0.5f; // Too different
            if (similarity < 0.7f) return 1.0f; // Good diversity
            if (similarity < 0.9f) return 0.8f; // Acceptable
            return 0.4f; // Too similar (potential inbreeding)
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
            combinedStats.attack = Mathf.RoundToInt(CombineStatValue(stats1.Strength, stats2.Strength));
            combinedStats.speed = Mathf.RoundToInt(CombineStatValue(stats1.Agility, stats2.Agility));
            combinedStats.intelligence = Mathf.RoundToInt(CombineStatValue(stats1.Intelligence, stats2.Intelligence));
            combinedStats.defense = Mathf.RoundToInt(CombineStatValue(stats1.Endurance, stats2.Endurance));
            
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
                genesList.Add(CreateGeneFromStat("Strength", genetics.BaseStats.Strength));
                genesList.Add(CreateGeneFromStat("Agility", genetics.BaseStats.Agility));
                genesList.Add(CreateGeneFromStat("Intelligence", genetics.BaseStats.Intelligence));
                genesList.Add(CreateGeneFromStat("Endurance", genetics.BaseStats.Endurance));
            }
            
            // Create the genetic profile with the genes array and generation info
            var profile = new GeneticProfile(genesList.ToArray(), 1); // Generation 1 for offspring
            
            return profile;
        }
        
        private Gene CreateGeneFromStat(string statName, float statValue)
        {
            // Normalize stat value to 0-1 range (assuming max stat is around 50)
            float normalizedValue = Mathf.Clamp01(statValue / 50f);
            
            return new Gene(
                Guid.NewGuid().ToString(),
                statName,
                TraitType.Physical,
                Allele.CreateDominant(statName, normalizedValue),
                Allele.CreateRecessive(statName, normalizedValue * 0.8f)
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