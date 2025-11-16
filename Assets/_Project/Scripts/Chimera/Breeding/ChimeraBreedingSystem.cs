using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Creatures;

namespace Laboratory.Chimera.Breeding
{
    /// <summary>
    /// System managing the breeding of chimera creatures
    /// Handles genetic combinations, trait inheritance, and offspring generation
    /// </summary>
    public partial class ChimeraBreedingSystem : SystemBase
    {
        #region Fields

        private readonly Dictionary<uint, BreedingRequest> _activeBreedingRequests = new Dictionary<uint, BreedingRequest>();
        private readonly List<ECSBreedingResult> _completedBreedings = new List<ECSBreedingResult>();

        #endregion

        #region Unity Lifecycle

        protected override void OnCreate()
        {
            base.OnCreate();
        }

        protected override void OnUpdate()
        {
            ProcessBreedingRequests();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initiate breeding between two creatures
        /// </summary>
        public ECSBreedingResult StartBreeding(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2)
        {
            if (!CanBreed(parent1, parent2))
            {
                return new ECSBreedingResult { Success = false, ErrorMessage = "Creatures cannot breed" };
            }

            var request = new BreedingRequest
            {
                RequestId = (uint)UnityEngine.Random.Range(1000, 999999),
                Parent1 = parent1,
                Parent2 = parent2,
                StartTime = Time.time,
                Duration = CalculateBreedingDuration(parent1, parent2)
            };

            _activeBreedingRequests[request.RequestId] = request;

            return new ECSBreedingResult
            {
                Success = true,
                RequestId = request.RequestId,
                EstimatedCompletionTime = request.StartTime + request.Duration
            };
        }

        /// <summary>
        /// Check if two creatures can breed
        /// </summary>
        public bool CanBreed(CreatureInstanceComponent creature1, CreatureInstanceComponent creature2)
        {
            // Basic breeding compatibility checks
            if (creature1.SpeciesId != creature2.SpeciesId) return false;
            if (!creature1.IsAlive || !creature2.IsAlive) return false;
            if (creature1.Age < 18f || creature2.Age < 18f) return false; // Maturity age

            return true;
        }

        /// <summary>
        /// Get breeding compatibility score between two creatures
        /// </summary>
        public float GetCompatibilityScore(CreatureInstanceComponent creature1, CreatureInstanceComponent creature2)
        {
            if (!CanBreed(creature1, creature2)) return 0f;

            // Basic compatibility calculation
            float healthScore = (creature1.Health / creature1.MaxHealth + creature2.Health / creature2.MaxHealth) / 2f;
            float moodScore = (creature1.Mood + creature2.Mood) / 2f;

            return (healthScore + moodScore) / 2f;
        }

        /// <summary>
        /// Get all active breeding requests
        /// </summary>
        public Dictionary<uint, BreedingRequest> GetActiveRequests()
        {
            return new Dictionary<uint, BreedingRequest>(_activeBreedingRequests);
        }

        /// <summary>
        /// Cancel a breeding request
        /// </summary>
        public bool CancelBreeding(uint requestId)
        {
            return _activeBreedingRequests.Remove(requestId);
        }

        #endregion

        #region Private Methods

        private void ProcessBreedingRequests()
        {
            var completedRequests = new List<uint>();

            foreach (var kvp in _activeBreedingRequests)
            {
                var request = kvp.Value;

                if (Time.time >= request.StartTime + request.Duration)
                {
                    var result = CompleteBreeding(request);
                    _completedBreedings.Add(result);
                    completedRequests.Add(kvp.Key);
                }
            }

            foreach (var requestId in completedRequests)
            {
                _activeBreedingRequests.Remove(requestId);
            }
        }

        private ECSBreedingResult CompleteBreeding(BreedingRequest request)
        {
            // Generate offspring based on parents
            var offspring = GenerateOffspring(request.Parent1, request.Parent2);

            return new ECSBreedingResult
            {
                Success = true,
                RequestId = request.RequestId,
                Offspring = offspring,
                CompletedAt = Time.time
            };
        }

        private CreatureInstanceComponent GenerateOffspring(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2)
        {
            // Create offspring with inherited traits
            return new CreatureInstanceComponent
            {
                CreatureId = (uint)UnityEngine.Random.Range(100000, 999999),
                SpeciesId = parent1.SpeciesId,
                Health = (parent1.MaxHealth + parent2.MaxHealth) / 2f,
                MaxHealth = (parent1.MaxHealth + parent2.MaxHealth) / 2f,
                Energy = (parent1.MaxEnergy + parent2.MaxEnergy) / 2f,
                MaxEnergy = (parent1.MaxEnergy + parent2.MaxEnergy) / 2f,
                Level = 1,
                Experience = 0f,
                Age = 0f,
                MaxAge = (parent1.MaxAge + parent2.MaxAge) / 2f,
                Mood = 0.8f, // Newborns start happy
                Hunger = 0.5f,
                IsAlive = true,
                IsOwned = parent1.IsOwned || parent2.IsOwned,
                OwnerId = parent1.IsOwned ? parent1.OwnerId : parent2.OwnerId,
                CreationTime = Time.timeAsDouble
            };
        }

        private float CalculateBreedingDuration(CreatureInstanceComponent parent1, CreatureInstanceComponent parent2)
        {
            // Base duration affected by creature health and compatibility
            float baseDuration = 30f; // 30 seconds base
            float healthModifier = (parent1.Health / parent1.MaxHealth + parent2.Health / parent2.MaxHealth) / 2f;

            return baseDuration * (2f - healthModifier); // Healthier creatures breed faster
        }

        #endregion
    }

    /// <summary>
    /// Breeding request data
    /// </summary>
    [System.Serializable]
    public struct BreedingRequest
    {
        public uint RequestId;
        public CreatureInstanceComponent Parent1;
        public CreatureInstanceComponent Parent2;
        public float StartTime;
        public float Duration;
    }

    /// <summary>
    /// ECS breeding result data (internal use only)
    /// </summary>
    [System.Serializable]
    public struct ECSBreedingResult
    {
        public bool Success;
        public uint RequestId;
        public string ErrorMessage;
        public CreatureInstanceComponent Offspring;
        public float EstimatedCompletionTime;
        public float CompletedAt;
    }
}