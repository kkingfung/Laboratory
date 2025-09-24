using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Chimera.Breeding
{
    /// <summary>
    /// Result of a breeding operation
    /// </summary>
    [Serializable]
    public class BreedingResult
    {
        public bool Success;
        public CreatureInstance Offspring;
        public string ErrorMessage;
        public DateTime BreedingTime;
        public float CompatibilityScore;
        public CreatureInstance Parent1;
        public CreatureInstance Parent2;

        // Additional properties for UI compatibility
        public float BreedingChance => CompatibilityScore;
        public string FailureReason => ErrorMessage;
        
        public static BreedingResult CreateSuccess(CreatureInstance offspring, float compatibility, CreatureInstance parent1 = null, CreatureInstance parent2 = null)
        {
            return new BreedingResult
            {
                Success = true,
                Offspring = offspring,
                BreedingTime = DateTime.UtcNow,
                CompatibilityScore = compatibility,
                Parent1 = parent1,
                Parent2 = parent2
            };
        }
        
        public static BreedingResult CreateFailure(string errorMessage)
        {
            return new BreedingResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                BreedingTime = DateTime.UtcNow
            };
        }
    }
    
    /// <summary>
    /// Interface for breeding system implementations
    /// </summary>
    public interface IBreedingSystem : System.IDisposable
    {
        BreedingResult AttemptBreeding(CreatureInstance parent1, CreatureInstance parent2);
        Cysharp.Threading.Tasks.UniTask<BreedingResult> AttemptBreedingAsync(CreatureInstance parent1, CreatureInstance parent2, System.Threading.CancellationToken cancellationToken = default);
        Cysharp.Threading.Tasks.UniTask<BreedingResult> BreedCreaturesAsync(CreatureInstance parent1, CreatureInstance parent2, System.Threading.CancellationToken cancellationToken = default);
        float CalculateBreedingSuccessChance(CreatureInstance parent1, CreatureInstance parent2);
        bool CanBreed(CreatureInstance parent1, CreatureInstance parent2);
    }
    
    /// <summary>
    /// Breeding costs structure
    /// </summary>
    [Serializable]
    public class BreedingCosts
    {
        public int EnergyCost = 100;
        public int ResourceCost = 50;
        public float BreedingTime = 30f;
        public Dictionary<string, int> SpecialRequirements = new Dictionary<string, int>();
    }
    
    /// <summary>
    /// Helper class for compatibility with BreedingSystem
    /// </summary>
    [Serializable]
    public class CreatureSpecies
    {
        public string Id { get; set; }
        public float MaturityAge { get; set; }
        public float MaxLifespan { get; set; }
    }
    
    /// <summary>
    /// Helper class for compatibility with BreedingSystem
    /// </summary>
    [Serializable]
    public class CreatureGenetics
    {
        public Laboratory.Chimera.Creatures.CreatureStats BaseStats { get; set; }
        public System.Collections.Generic.List<string> InheritedTraits { get; set; }
        
        public CreatureGenetics()
        {
            BaseStats = new Laboratory.Chimera.Creatures.CreatureStats();
            InheritedTraits = new System.Collections.Generic.List<string>();
        }
    }
}
