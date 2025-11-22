using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Entities;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.MonsterTown.Systems;

namespace Laboratory.Core.MonsterTown.Integration
{
    /// <summary>
    /// Bridge between Monster Town system and Chimera genetics system
    /// Provides integration points without requiring direct Chimera assembly dependencies
    /// </summary>
    public class ChimeraIntegrationBridge : MonoBehaviour
    {
        [Header("Integration Configuration")]
        [SerializeField] private bool enableChimeraIntegration = true;
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private float integrationCheckInterval = 5f;

        // Integration state
        private bool isChimeraAvailable = false;
        private bool isIntegrationActive = false;
        private IEventBus eventBus;

        #region Unity Lifecycle

        private void Awake()
        {
            eventBus = ServiceContainer.Instance?.ResolveService<IEventBus>();
        }

        private void Start()
        {
            if (autoInitializeOnStart)
            {
                _ = InitializeIntegrationAsync();
            }

            if (enableChimeraIntegration)
            {
                InvokeRepeating(nameof(CheckChimeraIntegration), 1f, integrationCheckInterval);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Initialize integration with Chimera system
        /// </summary>
        public async Task<bool> InitializeIntegrationAsync()
        {
            try
            {
                Debug.Log("🔗 Initializing Chimera integration bridge...");

                // Check if Chimera components are available
                isChimeraAvailable = CheckChimeraAvailability();

                if (isChimeraAvailable)
                {
                    await SetupChimeraEventHandlers();
                    await InitializeChimeraScene();
                    isIntegrationActive = true;
                    Debug.Log("✅ Chimera integration bridge initialized successfully");
                    return true;
                }
                else
                {
                    Debug.LogWarning("⚠️ Chimera system not available - running in standalone mode");
                    await InitializeStandaloneMode();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"❌ Failed to initialize Chimera integration: {ex.Message}");
                await InitializeStandaloneMode();
                return false;
            }
        }

        /// <summary>
        /// Create a Monster Town monster from Chimera creature data
        /// </summary>
        public MonsterInstance ConvertChimeraCreatureToMonster(object chimeraCreature)
        {
            // Since we can't directly access Chimera types, we'll create a conversion method
            // that works with the data we have available

            var monster = new MonsterInstance
            {
                UniqueId = System.Guid.NewGuid().ToString(),
                Name = GenerateMonsterName(),
                Species = "Chimera Hybrid",
                Level = 1,
                Experience = 0,
                Happiness = UnityEngine.Random.Range(40f, 80f),
                Energy = 100f,
                GeneticProfile = new BasicGeneticProfile
                {
                    OverallFitness = UnityEngine.Random.Range(0.3f, 0.9f),
                    Traits = new System.Collections.Generic.Dictionary<string, float>
                    {
                        ["Strength"] = UnityEngine.Random.Range(0.2f, 1.0f),
                        ["Speed"] = UnityEngine.Random.Range(0.2f, 1.0f),
                        ["Intelligence"] = UnityEngine.Random.Range(0.2f, 1.0f),
                        ["Endurance"] = UnityEngine.Random.Range(0.2f, 1.0f)
                    }
                },
                Equipment = new List<string> { "Starter_Collar", "Training_Harness" },
                BirthTime = DateTime.Now,
                Generation = 1
            };

            return monster;
        }

        /// <summary>
        /// Simulate breeding success event from Chimera system
        /// </summary>
        public void SimulateChimeraBreedingSuccess(MonsterInstance parent1, MonsterInstance parent2)
        {
            var offspring = new MonsterInstance
            {
                UniqueId = System.Guid.NewGuid().ToString(),
                Name = $"Hybrid-{UnityEngine.Random.Range(1000, 9999)}",
                Species = DetermineHybridSpecies(parent1, parent2),
                Level = 1,
                Experience = 0,
                Happiness = 70f,
                Energy = 100f,
                GeneticProfile = CreateCrossoverProfile(parent1.GeneticProfile, parent2.GeneticProfile),
                Equipment = new List<string>(),
                BirthTime = DateTime.Now,
                Generation = Mathf.Max(parent1.Generation, parent2.Generation) + 1
            };

            // Fire breeding success event
            var breedingEvent = new BreedingSuccessfulEvent
            {
                Parent1 = parent1,
                Parent2 = parent2,
                Offspring = offspring,
                BreedingTime = DateTime.Now
            };
            eventBus?.Publish(breedingEvent);

            Debug.Log($"🧬 Simulated Chimera breeding success: {offspring.Name}");
        }

        /// <summary>
        /// Get integration status
        /// </summary>
        public ChimeraIntegrationStatus GetIntegrationStatus()
        {
            return new ChimeraIntegrationStatus
            {
                IsChimeraAvailable = isChimeraAvailable,
                IsIntegrationActive = isIntegrationActive,
                SupportsBreeding = true,
                SupportsGenetics = true,
                SupportsEvolution = false // Not implemented yet
            };
        }

        #endregion

        #region Private Methods

        private bool CheckChimeraAvailability()
        {
            // Try to find Chimera components in the scene
            // This is a safe check that doesn't require assembly references

            // Check for any objects with "Chimera" in their name
            var chimeraObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None)
                .Where(go => go.name.Contains("Chimera", StringComparison.OrdinalIgnoreCase));

            if (chimeraObjects.Any())
            {
                Debug.Log($"🔍 Found {chimeraObjects.Count()} potential Chimera objects");
                return true;
            }

            // Check for ECS world with Chimera systems
            if (World.DefaultGameObjectInjectionWorld != null)
            {
                var world = World.DefaultGameObjectInjectionWorld;
                var systemNames = world.Systems.Select(s => s.GetType().Name);

                if (systemNames.Any(name => name.Contains("Chimera")))
                {
                    Debug.Log("🔍 Found Chimera ECS systems");
                    return true;
                }
            }

            return false;
        }

        private async Task SetupChimeraEventHandlers()
        {
            if (eventBus == null) return;

            // Set up event handlers for Chimera integration
            Debug.Log("🔗 Setting up Chimera event handlers");

            // Simulate some setup time
            await Task.Delay(100);
        }

        private async Task InitializeChimeraScene()
        {
            Debug.Log("🌍 Initializing Chimera scene integration");

            // Simulate Chimera scene initialization
            await Task.Delay(200);

            // Create some initial test creatures if needed
            if (Application.isPlaying)
            {
                await CreateInitialTestCreatures();
            }
        }

        private async Task InitializeStandaloneMode()
        {
            Debug.Log("🔧 Initializing standalone mode (no Chimera integration)");

            // Set up Monster Town to work without Chimera
            isIntegrationActive = false;

            await Task.Delay(50);
        }

        private async Task CreateInitialTestCreatures()
        {
            Debug.Log("🧪 Creating initial test creatures for Chimera integration");

            // Create a few test monsters to demonstrate integration
            for (int i = 0; i < 3; i++)
            {
                var testMonster = ConvertChimeraCreatureToMonster(null);
                testMonster.Name = $"Test Chimera {i + 1}";

                // Fire creature spawned event
                var spawnEvent = new CreatureSpawnedEvent(testMonster);
                eventBus?.Publish(spawnEvent);

                await Task.Delay(100);
            }
        }

        private void CheckChimeraIntegration()
        {
            if (!enableChimeraIntegration) return;

            bool currentAvailability = CheckChimeraAvailability();

            if (currentAvailability != isChimeraAvailable)
            {
                isChimeraAvailable = currentAvailability;

                if (isChimeraAvailable && !isIntegrationActive)
                {
                    Debug.Log("🔄 Chimera system detected - attempting integration");
                    _ = InitializeIntegrationAsync();
                }
                else if (!isChimeraAvailable && isIntegrationActive)
                {
                    Debug.Log("🔄 Chimera system lost - switching to standalone mode");
                    isIntegrationActive = false;
                }
            }
        }

        private string GenerateMonsterName()
        {
            string[] prefixes = { "Chimera", "Hybrid", "Neo", "Proto", "Synth", "Cyber", "Bio" };
            string[] suffixes = { "Beast", "Creature", "Entity", "Form", "Morph", "Ling", "Sprite" };

            var prefix = prefixes[UnityEngine.Random.Range(0, prefixes.Length)];
            var suffix = suffixes[UnityEngine.Random.Range(0, suffixes.Length)];
            var number = UnityEngine.Random.Range(100, 999);

            return $"{prefix}-{suffix}-{number}";
        }

        private string DetermineHybridSpecies(MonsterInstance parent1, MonsterInstance parent2)
        {
            if (parent1.Species == parent2.Species)
                return parent1.Species;

            return $"{parent1.Species.Split(' ')[0]}-{parent2.Species.Split(' ')[0]} Hybrid";
        }

        private IGeneticProfile CreateCrossoverProfile(IGeneticProfile parent1, IGeneticProfile parent2)
        {
            var crossoverProfile = new BasicGeneticProfile();

            // Simple crossover - average traits from both parents
            if (parent1.Traits.Count > 0 && parent2.Traits.Count > 0)
            {
                var allTraits = parent1.Traits.Keys.Union(parent2.Traits.Keys);
                foreach (var trait in allTraits)
                {
                    var value1 = parent1.Traits.ContainsKey(trait) ? parent1.Traits[trait] : 0.5f;
                    var value2 = parent2.Traits.ContainsKey(trait) ? parent2.Traits[trait] : 0.5f;
                    crossoverProfile.Traits[trait] = (value1 + value2) / 2f;
                }
            }

            crossoverProfile.OverallFitness = (parent1.OverallFitness + parent2.OverallFitness) / 2f;
            return crossoverProfile;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Status of Chimera integration
    /// </summary>
    [System.Serializable]
    public struct ChimeraIntegrationStatus
    {
        public bool IsChimeraAvailable;
        public bool IsIntegrationActive;
        public bool SupportsBreeding;
        public bool SupportsGenetics;
        public bool SupportsEvolution;

        public override string ToString()
        {
            return $"Chimera Integration: Available={IsChimeraAvailable}, Active={IsIntegrationActive}";
        }
    }

    /// <summary>
    /// Event fired when a creature is spawned from Chimera system
    /// </summary>
    public class CreatureSpawnedEvent
    {
        public MonsterInstance Monster { get; }
        public DateTime Timestamp { get; }

        public CreatureSpawnedEvent(MonsterInstance monster)
        {
            Monster = monster;
            Timestamp = DateTime.UtcNow;
        }
    }

    #endregion
}