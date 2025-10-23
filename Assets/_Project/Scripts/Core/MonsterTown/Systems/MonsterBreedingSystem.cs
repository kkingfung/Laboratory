using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown.Systems
{
    /// <summary>
    /// Monster Town Breeding System - handles breeding mechanics specific to town gameplay
    /// Integrates with existing Chimera breeding system for genetic calculations
    /// </summary>
    public class MonsterBreedingSystem : MonoBehaviour
    {
        [Header("Breeding Configuration")]
        [SerializeField] private float breedingCooldownHours = 24f;
        [SerializeField] private int maxBreedingPairsPerDay = 3;
        [SerializeField] private float successBaseRate = 0.8f;
        [SerializeField] private bool enableTownBonuses = true;

        [Header("Resource Costs")]
        [SerializeField] private int breedingCostCoins = 100;
        [SerializeField] private int breedingCostGems = 5;

        // System dependencies
        private IEventBus eventBus;
        private IResourceManager resourceManager;

        // Breeding state
        private Dictionary<string, DateTime> lastBreedingTime = new();
        private Dictionary<string, int> dailyBreedingCount = new();
        private DateTime lastDayReset = DateTime.Now.Date;

        #region Unity Lifecycle

        private void Awake()
        {
            eventBus = ServiceContainer.Instance?.ResolveService<IEventBus>();
            resourceManager = ServiceContainer.Instance?.ResolveService<IResourceManager>();
        }

        private void Start()
        {
            if (eventBus != null)
            {
                // Subscribe to relevant events
                eventBus.Subscribe<MonsterAddedToTownEvent>(OnMonsterAddedToTown);
            }
        }

        private void Update()
        {
            // Reset daily breeding counts at midnight
            if (DateTime.Now.Date > lastDayReset)
            {
                dailyBreedingCount.Clear();
                lastDayReset = DateTime.Now.Date;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Attempt to breed two monsters and produce offspring
        /// </summary>
        public async Task<BreedingResult> BreedMonstersAsync(MonsterInstance parent1, MonsterInstance parent2)
        {
            // Validate breeding attempt
            var validation = ValidateBreeding(parent1, parent2);
            if (!validation.IsValid)
            {
                return new BreedingResult
                {
                    Success = false,
                    ErrorMessage = validation.ErrorMessage
                };
            }

            // Deduct resources
            if (!TryDeductBreedingCosts())
            {
                return new BreedingResult
                {
                    Success = false,
                    ErrorMessage = "Insufficient resources for breeding"
                };
            }

            // Calculate breeding success
            float successRate = CalculateBreedingSuccessRate(parent1, parent2);
            bool success = UnityEngine.Random.Range(0f, 1f) <= successRate;

            if (!success)
            {
                // Record failed attempt
                RecordBreedingAttempt(parent1, parent2);
                return new BreedingResult
                {
                    Success = false,
                    ErrorMessage = "Breeding attempt failed"
                };
            }

            // Generate offspring
            var offspring = await GenerateOffspring(parent1, parent2);

            // Record successful breeding
            RecordBreedingAttempt(parent1, parent2);

            // Fire breeding success event
            eventBus?.Publish(new BreedingSuccessfulEvent(parent1, parent2, offspring));

            return new BreedingResult
            {
                Success = true,
                Offspring = offspring
            };
        }

        /// <summary>
        /// Check if two monsters can breed
        /// </summary>
        public BreedingValidation ValidateBreeding(MonsterInstance parent1, MonsterInstance parent2)
        {
            if (parent1 == null || parent2 == null)
                return new BreedingValidation(false, "One or both parents are null");

            if (parent1.UniqueId == parent2.UniqueId)
                return new BreedingValidation(false, "Cannot breed monster with itself");

            if (!AreCompatibleSpecies(parent1, parent2))
                return new BreedingValidation(false, "Incompatible species for breeding");

            if (IsOnBreedingCooldown(parent1) || IsOnBreedingCooldown(parent2))
                return new BreedingValidation(false, "One or both parents are on breeding cooldown");

            if (HasReachedDailyBreedingLimit(parent1) || HasReachedDailyBreedingLimit(parent2))
                return new BreedingValidation(false, "Daily breeding limit reached for one or both parents");

            if (!HasSufficientResources())
                return new BreedingValidation(false, "Insufficient resources for breeding");

            return new BreedingValidation(true, "Breeding is possible");
        }

        /// <summary>
        /// Get breeding cooldown remaining for a monster
        /// </summary>
        public TimeSpan GetBreedingCooldownRemaining(MonsterInstance monster)
        {
            if (!lastBreedingTime.TryGetValue(monster.UniqueId, out DateTime lastBreeding))
                return TimeSpan.Zero;

            var cooldownEnd = lastBreeding.AddHours(breedingCooldownHours);
            var remaining = cooldownEnd - DateTime.Now;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// Create a random monster with specified name prefix
        /// </summary>
        public async Task<Monster> CreateRandomMonster(string namePrefix = "Monster")
        {
            await Task.Delay(100); // Simulate creation time

            var monster = new Monster
            {
                UniqueId = Guid.NewGuid().ToString(),
                Name = $"{namePrefix}-{UnityEngine.Random.Range(1000, 9999)}",
                Level = 1,
                Happiness = UnityEngine.Random.Range(0.6f, 0.9f),
                GeneticProfile = CreateRandomGeneticProfile(),
                Stats = MonsterStats.CreateRandom(40f, 70f),
                ActivityExperience = new Dictionary<ActivityType, float>(),
                Equipment = new List<Equipment>(),
                CurrentLocation = TownLocation.TownCenter,
                LastActivityTime = DateTime.UtcNow
            };

            Debug.Log($"🧬 Created random monster: {monster.Name}");
            return monster;
        }

        /// <summary>
        /// Check if two monsters can breed (simplified version for Monster class)
        /// </summary>
        public bool CanBreed(Monster parent1, Monster parent2)
        {
            if (parent1 == null || parent2 == null) return false;
            if (parent1.UniqueId == parent2.UniqueId) return false;
            if (parent1.Level < 2 || parent2.Level < 2) return false;
            if (parent1.Happiness < 0.5f || parent2.Happiness < 0.5f) return false;
            return true;
        }

        /// <summary>
        /// Create offspring from two parent monsters (simplified version for Monster class)
        /// </summary>
        public async Task<Monster> CreateOffspring(Monster parent1, Monster parent2)
        {
            await Task.Delay(200); // Simulate breeding time

            var offspring = new Monster
            {
                UniqueId = Guid.NewGuid().ToString(),
                Name = GenerateOffspringName(parent1, parent2),
                Level = 1,
                Happiness = 0.8f,
                GeneticProfile = CombineGenetics(parent1.GeneticProfile, parent2.GeneticProfile),
                Stats = CreateHybridStats(parent1.Stats, parent2.Stats),
                ActivityExperience = new Dictionary<ActivityType, float>(),
                Equipment = new List<Equipment>(),
                CurrentLocation = TownLocation.BreedingCenter,
                LastActivityTime = DateTime.UtcNow
            };

            Debug.Log($"🧬 Breeding successful: {offspring.Name} born from {parent1.Name} and {parent2.Name}");
            return offspring;
        }

        #endregion

        #region Private Methods

        private bool AreCompatibleSpecies(MonsterInstance parent1, MonsterInstance parent2)
        {
            // For now, allow all species to breed (simplified)
            // In a more complex system, this would check species compatibility
            return true;
        }

        private bool IsOnBreedingCooldown(MonsterInstance monster)
        {
            return GetBreedingCooldownRemaining(monster) > TimeSpan.Zero;
        }

        private bool HasReachedDailyBreedingLimit(MonsterInstance monster)
        {
            if (!dailyBreedingCount.TryGetValue(monster.UniqueId, out int count))
                return false;

            return count >= maxBreedingPairsPerDay;
        }

        private bool HasSufficientResources()
        {
            if (resourceManager == null) return true; // Skip check if no resource manager

            // Check if player has enough resources
            // This would integrate with the actual resource system
            return true; // Simplified for now
        }

        private bool TryDeductBreedingCosts()
        {
            if (resourceManager == null) return true; // Skip if no resource manager

            var cost = new TownResources
            {
                coins = breedingCostCoins,
                gems = breedingCostGems
            };

            // Try to deduct resources
            // This would integrate with the actual resource system
            return true; // Simplified for now
        }

        private float CalculateBreedingSuccessRate(MonsterInstance parent1, MonsterInstance parent2)
        {
            float baseRate = successBaseRate;

            // Town bonuses
            if (enableTownBonuses)
            {
                // Higher level parents have better success rate
                float levelBonus = (parent1.Level + parent2.Level) * 0.01f;
                baseRate += levelBonus;

                // Happiness bonus
                float happinessBonus = (parent1.Happiness + parent2.Happiness) * 0.001f;
                baseRate += happinessBonus;

                // Equipment bonuses could be added here
            }

            // Clamp between 0 and 1
            return Mathf.Clamp01(baseRate);
        }

        private async Task<MonsterInstance> GenerateOffspring(MonsterInstance parent1, MonsterInstance parent2)
        {
            // Create offspring with combined traits
            var offspring = new MonsterInstance
            {
                UniqueId = System.Guid.NewGuid().ToString(),
                Name = GenerateOffspringName(parent1, parent2),
                Species = DetermineOffspringSpecies(parent1, parent2),
                Level = 1,
                Experience = 0,
                Happiness = 50f, // Start with neutral happiness
                Energy = 100f,   // Start with full energy
                GeneticProfile = CombineGenetics(parent1.Genetics, parent2.Genetics),
                BirthTime = DateTime.Now,
                Generation = Mathf.Max(parent1.Generation, parent2.Generation) + 1
            };

            // Simulate some breeding time
            await Task.Delay(100);

            return offspring;
        }

        private string GenerateOffspringName(MonsterInstance parent1, MonsterInstance parent2)
        {
            string[] prefixes = { "Little", "Baby", "Young", "Tiny", "Small" };
            string[] suffixes = { "ling", "pup", "kit", "cub", "sprite" };

            var prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
            var suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Length)];

            return $"{prefix} {suffix}";
        }

        private string DetermineOffspringSpecies(MonsterInstance parent1, MonsterInstance parent2)
        {
            // For now, randomly pick one parent's species
            // In a more complex system, this could create hybrid species
            return UnityEngine.Random.Range(0, 2) == 0 ? parent1.Species : parent2.Species;
        }

        private IGeneticProfile CombineGenetics(IGeneticProfile genetics1, IGeneticProfile genetics2)
        {
            // Create a new BasicGeneticProfile with combined traits
            var combined = new BasicGeneticProfile();

            // Combine traits using the GetTraitValue method
            string[] traitNames = { "Strength", "Agility", "Intelligence", "Vitality", "Social", "Creativity" };

            foreach (var traitName in traitNames)
            {
                float value1 = genetics1.GetTraitValue(traitName);
                float value2 = genetics2.GetTraitValue(traitName);
                float combinedValue = (value1 + value2) / 2f + UnityEngine.Random.Range(-0.1f, 0.1f);
                combinedValue = Mathf.Clamp01(combinedValue);
                combined.Traits[traitName] = combinedValue;
            }

            // Set overall fitness as average of traits
            combined.OverallFitness = combined.Traits.Values.Average();

            return combined;
        }

        private void RecordBreedingAttempt(MonsterInstance parent1, MonsterInstance parent2)
        {
            var now = DateTime.Now;

            // Record last breeding time
            lastBreedingTime[parent1.UniqueId] = now;
            lastBreedingTime[parent2.UniqueId] = now;

            // Increment daily breeding count
            dailyBreedingCount[parent1.UniqueId] = dailyBreedingCount.GetValueOrDefault(parent1.UniqueId, 0) + 1;
            dailyBreedingCount[parent2.UniqueId] = dailyBreedingCount.GetValueOrDefault(parent2.UniqueId, 0) + 1;
        }

        private void OnMonsterAddedToTown(MonsterAddedToTownEvent evt)
        {
            // Initialize breeding data for new monsters
            if (!lastBreedingTime.ContainsKey(evt.Monster.UniqueId))
            {
                lastBreedingTime[evt.Monster.UniqueId] = DateTime.MinValue; // Allow immediate breeding for new monsters
                dailyBreedingCount[evt.Monster.UniqueId] = 0;
            }
        }

        /// <summary>
        /// Create a random genetic profile for new monsters
        /// </summary>
        private IGeneticProfile CreateRandomGeneticProfile()
        {
            var profile = new BasicGeneticProfile();

            // Add random traits
            var traitNames = new[] { "strength", "agility", "intelligence", "vitality", "social", "adaptability" };
            foreach (var trait in traitNames)
            {
                profile.Traits[trait] = UnityEngine.Random.Range(0.3f, 0.8f);
            }

            // Calculate overall fitness
            var averageTrait = 0f;
            foreach (var trait in profile.Traits.Values)
            {
                averageTrait += trait;
            }
            profile.OverallFitness = averageTrait / profile.Traits.Count;

            return profile;
        }

        /// <summary>
        /// Generate offspring name from two parents (Monster version)
        /// </summary>
        private string GenerateOffspringName(Monster parent1, Monster parent2)
        {
            var prefixes = new[] { "Neo", "Hybrid", "Gen", "Cross", "Fusion" };
            var prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
            var number = UnityEngine.Random.Range(100, 999);
            return $"{prefix}-{number}";
        }

        /// <summary>
        /// Create hybrid stats from two parent monsters
        /// </summary>
        private MonsterStats CreateHybridStats(MonsterStats parent1, MonsterStats parent2)
        {
            return new MonsterStats
            {
                strength = (parent1.strength + parent2.strength) / 2f + UnityEngine.Random.Range(-5f, 5f),
                agility = (parent1.agility + parent2.agility) / 2f + UnityEngine.Random.Range(-5f, 5f),
                vitality = (parent1.vitality + parent2.vitality) / 2f + UnityEngine.Random.Range(-5f, 5f),
                speed = (parent1.speed + parent2.speed) / 2f + UnityEngine.Random.Range(-5f, 5f),
                intelligence = (parent1.intelligence + parent2.intelligence) / 2f + UnityEngine.Random.Range(-5f, 5f),
                adaptability = (parent1.adaptability + parent2.adaptability) / 2f + UnityEngine.Random.Range(-5f, 5f),
                social = (parent1.social + parent2.social) / 2f + UnityEngine.Random.Range(-5f, 5f),
                charisma = (parent1.charisma + parent2.charisma) / 2f + UnityEngine.Random.Range(-5f, 5f)
            };
        }

        #endregion
    }

    #region Data Structures

    [System.Serializable]
    public class BreedingResult
    {
        public bool Success;
        public string ErrorMessage;
        public MonsterInstance Offspring;
    }

    [System.Serializable]
    public class BreedingValidation
    {
        public bool IsValid;
        public string ErrorMessage;

        public BreedingValidation(bool isValid, string errorMessage)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }

    // Events
    public class BreedingSuccessfulEvent
    {
        public MonsterInstance Parent1 { get; }
        public MonsterInstance Parent2 { get; }
        public MonsterInstance Offspring { get; }
        public DateTime Timestamp { get; }

        public BreedingSuccessfulEvent(MonsterInstance parent1, MonsterInstance parent2, MonsterInstance offspring)
        {
            Parent1 = parent1;
            Parent2 = parent2;
            Offspring = offspring;
            Timestamp = DateTime.Now;
        }
    }

    public class MonsterAddedToTownEvent
    {
        public MonsterInstance Monster { get; }
        public DateTime Timestamp { get; }

        public MonsterAddedToTownEvent(MonsterInstance monster)
        {
            Monster = monster;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}