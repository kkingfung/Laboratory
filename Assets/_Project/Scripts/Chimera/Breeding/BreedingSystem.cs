using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Events;

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
        /// Attempts to breed two creatures and produce offspring
        /// </summary>
        public async UniTask<BreedingResult> BreedCreaturesAsync(
            CreatureInstance parent1, 
            CreatureInstance parent2, 
            BreedingEnvironment environment,
            IProgress<float> progress = null,
            CancellationToken cancellation = default)
        {
            if (parent1 == null || parent2 == null)
                throw new ArgumentNullException("Parent creatures cannot be null");
                
            progress?.Report(0f);
            
            try
            {
                // Allow for cancellation and add small delay for async nature
                await UniTask.Delay(1, cancellationToken: cancellation);
                // Phase 1: Validate breeding compatibility
                progress?.Report(0.1f);
                var compatibilityCheck = ValidateBreedingCompatibility(parent1, parent2, environment);
                if (!compatibilityCheck.IsCompatible)
                {
                    var failureResult = new BreedingResult
                    {
                        Success = false,
                        FailureReason = compatibilityCheck.Reason,
                        Parent1 = parent1,
                        Parent2 = parent2
                    };
                    
                    // Fire breeding failure event
                    _eventBus?.Publish(new BreedingFailedEvent(parent1, parent2, failureResult.FailureReason));
                    
                    return failureResult;
                }
                
                // Phase 2: Calculate breeding success chance
                progress?.Report(0.2f);
                float breedingChance = CalculateBreedingSuccessChance(parent1, parent2, environment);
                
                // Phase 3: Determine if breeding succeeds
                progress?.Report(0.3f);
                if (UnityEngine.Random.value > breedingChance)
                {
                    var failureResult = new BreedingResult
                    {
                        Success = false,
                        FailureReason = "Natural breeding failure - try again later",
                        Parent1 = parent1,
                        Parent2 = parent2,
                        BreedingChance = breedingChance
                    };
                    
                    // Fire breeding failure event
                    _eventBus?.Publish(new BreedingFailedEvent(parent1, parent2, failureResult.FailureReason, breedingChance));
                    
                    return failureResult;
                }
                
                // Phase 4: Generate offspring
                progress?.Report(0.5f);
                int offspringCount = CalculateOffspringCount(parent1, parent2, environment);
                var offspring = new CreatureInstance[offspringCount];
                
                for (int i = 0; i < offspringCount; i++)
                {
                    offspring[i] = GenerateOffspring(parent1, parent2, environment);
                    
                    // Small delay between offspring generation for async behavior
                    if (i < offspringCount - 1)
                    {
                        await UniTask.Delay(1, cancellationToken: cancellation);
                    }
                }
                
                progress?.Report(1f);
                
                var result = new BreedingResult
                {
                    Success = true,
                    Parent1 = parent1,
                    Parent2 = parent2,
                    Offspring = offspring,
                    BreedingChance = breedingChance,
                    Environment = environment
                };
                
                // Fire breeding success event
                _eventBus?.Publish(new BreedingSuccessfulEvent(result));
                
                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Breeding system error: {ex}");
                return new BreedingResult
                {
                    Success = false,
                    FailureReason = $"System error: {ex.Message}",
                    Parent1 = parent1,
                    Parent2 = parent2
                };
            }
        }
        
        private CompatibilityResult ValidateBreedingCompatibility(
            CreatureInstance parent1, 
            CreatureInstance parent2, 
            BreedingEnvironment environment)
        {
            if (!parent1.Definition.CanBreedWith(parent2.Definition))
                return new CompatibilityResult(false, "Species are not genetically compatible");
                
            if (!parent1.IsAdult || !parent2.IsAdult)
                return new CompatibilityResult(false, "One or both creatures are not yet mature");
                
            return new CompatibilityResult(true, "Compatible for breeding");
        }
        
        private float CalculateBreedingSuccessChance(
            CreatureInstance parent1, 
            CreatureInstance parent2, 
            BreedingEnvironment environment)
        {
            float baseChance = (parent1.Definition.fertilityRate + parent2.Definition.fertilityRate) * 0.5f;
            return Mathf.Clamp(baseChance, 0.1f, 0.9f);
        }
        
        private int CalculateOffspringCount(CreatureInstance parent1, CreatureInstance parent2, BreedingEnvironment environment)
        {
            return parent1.Definition.size switch
            {
                CreatureSize.Tiny => UnityEngine.Random.Range(2, 6),
                CreatureSize.Small => UnityEngine.Random.Range(1, 4),
                CreatureSize.Medium => UnityEngine.Random.Range(1, 3),
                _ => 1
            };
        }
        
        private CreatureInstance GenerateOffspring(CreatureInstance parent1, CreatureInstance parent2, BreedingEnvironment environment)
        {
            var environmentalFactors = Laboratory.Chimera.Genetics.EnvironmentalFactors.FromBiome(environment.BiomeType);
            var offspringGenetics = Laboratory.Chimera.Genetics.GeneticProfile.CreateOffspring(parent1.GeneticProfile, parent2.GeneticProfile, environmentalFactors);
            
            var baseDefinition = UnityEngine.Random.value < 0.5f ? parent1.Definition : parent2.Definition;
            
            return new CreatureInstance
            {
                Definition = baseDefinition,
                GeneticProfile = offspringGenetics,
                AgeInDays = 0,
                CurrentHealth = baseDefinition.baseStats.health,
                Happiness = 0.7f,
                Experience = 0,
                Level = 1,
                IsWild = false,
                BirthDate = DateTime.UtcNow
            };
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            
            // Clean up any resources if needed
            // Currently no cleanup required, but pattern is established
        }
    }
    
    public interface IBreedingSystem : IDisposable
    {
        UniTask<BreedingResult> BreedCreaturesAsync(
            CreatureInstance parent1, 
            CreatureInstance parent2, 
            BreedingEnvironment environment,
            IProgress<float> progress = null,
            CancellationToken cancellation = default);
    }
    
    public class BreedingResult
    {
        public bool Success { get; set; }
        public string FailureReason { get; set; }
        public CreatureInstance Parent1 { get; set; }
        public CreatureInstance Parent2 { get; set; }
        public CreatureInstance[] Offspring { get; set; } = Array.Empty<CreatureInstance>();
        public float BreedingChance { get; set; }
        public BreedingEnvironment Environment { get; set; }
    }
    
    [Serializable]
    public class BreedingEnvironment
    {
        public BiomeType BiomeType { get; set; } = BiomeType.Forest;
        public float Temperature { get; set; } = 22f;
        public float FoodAvailability { get; set; } = 1f;
        public float PredatorPressure { get; set; } = 0.3f;
        public float PopulationDensity { get; set; } = 0.4f;
    }
    
    public readonly struct CompatibilityResult
    {
        public readonly bool IsCompatible;
        public readonly string Reason;
        
        public CompatibilityResult(bool isCompatible, string reason)
        {
            IsCompatible = isCompatible;
            Reason = reason;
        }
    }
    
    // Simple creature instance for breeding
    public class CreatureInstance
    {
        public CreatureDefinition Definition { get; set; }
        public Laboratory.Chimera.Genetics.GeneticProfile GeneticProfile { get; set; }
        public string UniqueId { get; set; } = Guid.NewGuid().ToString();
        public int AgeInDays { get; set; }
        public int CurrentHealth { get; set; }
        public float Happiness { get; set; } = 0.5f;
        public int Experience { get; set; }
        public int Level { get; set; } = 1;
        public bool IsWild { get; set; } = true;
        public DateTime BirthDate { get; set; } = DateTime.UtcNow;
        
        public bool IsAdult => AgeInDays >= (Definition?.maturationAge ?? 90);
    }
}
