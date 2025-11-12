using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Creatures;
using Laboratory.Chimera.Genetics;
using Laboratory.Chimera.Core;
using Laboratory.Core.Events;
using Laboratory.Core.Enums;
using Laboratory.Shared.Types;

namespace Laboratory.Chimera.Testing
{
    /// <summary>
    /// Quick testing script to verify Project Chimera systems are working correctly.
    /// Attach to any GameObject to run basic breeding system tests.
    /// </summary>
    public class ChimeraQuickTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool enableDetailedLogging = true;

        private IBreedingSystem breedingSystem;
        private IEventBus eventBus;

        private async void Start()
        {
            if (runTestsOnStart)
            {
                await RunQuickTests();
            }
        }

        [ContextMenu("Run Quick Tests")]
        public async void RunQuickTestsFromMenu()
        {
            await RunQuickTests();
        }

        private async UniTask RunQuickTests()
        {
            Log("🧬 STARTING CHIMERA QUICK TESTS");
            
            // Initialize systems
            InitializeSystems();
            
            // Test 1: Basic system initialization
            TestSystemInitialization();
            
            // Test 2: Create test creatures
            var creatures = CreateTestCreatures();
            
            // Test 3: Test breeding compatibility
            TestBreedingCompatibility(creatures[0], creatures[1]);
            
            // Test 4: Perform actual breeding
            await TestBreeding(creatures[0], creatures[1]);
            
            // Test 5: Test genetic system
            TestGeneticSystem();
            
            // Test 6: Test event system
            TestEventSystem();
            
            Log("✅ ALL CHIMERA TESTS COMPLETED SUCCESSFULLY!");
        }

        private void InitializeSystems()
        {
            Log("Initializing Chimera systems...");
            
            eventBus = new UnifiedEventBus();
            breedingSystem = new BreedingSystem(eventBus);
            
            // Subscribe to breeding events
            eventBus.Subscribe<BreedingSuccessfulEvent>(OnBreedingSuccess);
            eventBus.Subscribe<BreedingFailedEvent>(OnBreedingFailure);
            
            Log("✅ Systems initialized");
        }

        private void TestSystemInitialization()
        {
            Log("Testing system initialization...");
            
            Assert(eventBus != null, "EventBus should be initialized");
            Assert(breedingSystem != null, "BreedingSystem should be initialized");
            
            Log("✅ System initialization test passed");
        }

        private CreatureInstance[] CreateTestCreatures()
        {
            Log("Creating test creatures...");
            
            var dragonDefinition = CreateTestCreatureDefinition("Test Dragon", 1);
            var drakeDefinition = CreateTestCreatureDefinition("Test Drake", 1);
            
            var dragon = CreateTestCreatureInstance(dragonDefinition);
            var drake = CreateTestCreatureInstance(drakeDefinition);
            
            Assert(dragon != null, "Dragon should be created");
            Assert(drake != null, "Drake should be created");
            Assert(dragon.IsAdult, "Dragon should be adult");
            Assert(drake.IsAdult, "Drake should be adult");
            
            Log($"✅ Created {dragon.Definition.speciesName} and {drake.Definition.speciesName}");
            
            return new[] { dragon, drake };
        }

        private void TestBreedingCompatibility(CreatureInstance creature1, CreatureInstance creature2)
        {
            Log("Testing breeding compatibility...");
            
            bool compatible = creature1.Definition.CanBreedWith(creature2.Definition);
            Assert(compatible, "Test creatures should be compatible for breeding");
            
            Log("✅ Breeding compatibility test passed");
        }

        private async UniTask TestBreeding(CreatureInstance parent1, CreatureInstance parent2)
        {
            Log("Testing breeding system...");
            
            var environment = new BreedingEnvironment
            {
                BiomeType = BiomeType.Forest,
                Temperature = 22f,
                FoodAvailability = 0.8f,
                PredatorPressure = 0.3f,
                PopulationDensity = 0.4f
            };
            
            var result = await breedingSystem.BreedCreaturesAsync(parent1, parent2);
            
            Assert(result != null, "Breeding result should not be null");
            Assert(result.Parent1 == parent1, "Result should reference correct parent1");
            Assert(result.Parent2 == parent2, "Result should reference correct parent2");
            
            if (result.Success)
            {
                Log($"✅ Breeding successful! Offspring produced");
                Assert(result.Offspring != null, "Successful breeding should produce offspring");

                var offspring = result.Offspring;
                Assert(offspring != null, "Offspring should not be null");
                Assert(offspring.GeneticProfile != null, "Offspring should have genetic profile");
                Assert(offspring.AgeInDays == 0, "Newborn should have age 0");
            }
            else
            {
                Log($"ℹ️ Breeding failed: {result.ErrorMessage}");
                Assert(!string.IsNullOrEmpty(result.ErrorMessage), "Failed breeding should have reason");
            }
            
            Log("✅ Breeding system test completed");
        }

        private void TestGeneticSystem()
        {
            Log("Testing genetic system...");
            
            // Create test genes
            var genes = new Gene[]
            {
                new Gene { traitName = "Strength", value = 0.8f, dominance = 0.7f, isActive = true },
                new Gene { traitName = "Speed", value = 0.6f, dominance = 0.5f, isActive = true },
                new Gene { traitName = "Intelligence", value = 0.9f, dominance = 0.8f, isActive = true }
            };
            
            var geneticProfile = new GeneticProfile(genes, 1);
            
            Assert(geneticProfile != null, "GeneticProfile should be created");
            Assert(geneticProfile.Genes.Count == 3, "Should have 3 genes");
            Assert(geneticProfile.Generation == 1, "Should be generation 1");
            Assert(!string.IsNullOrEmpty(geneticProfile.LineageId), "Should have lineage ID");
            
            // Test stat modifiers
            var baseStats = new CreatureStats
            {
                health = 100,
                attack = 20,
                defense = 15,
                speed = 10,
                intelligence = 5,
                charisma = 5
            };
            
            var modifiedStats = geneticProfile.ApplyModifiers(baseStats);
            Assert(modifiedStats.health >= baseStats.health, "Health should be maintained or improved");
            
            Log("✅ Genetic system test passed");
        }

        private void TestEventSystem()
        {
            Log("Testing event system...");
            
            bool eventReceived = false;
            
            eventBus.Subscribe<BreedingSuccessfulEvent>(evt => eventReceived = true);
            
            // Create a mock successful breeding result
            var mockResult = new BreedingResult
            {
                Success = true,
                Offspring = CreateTestCreatureInstance(CreateTestCreatureDefinition("Mock Offspring", 1))
            };
            
            eventBus.Publish(new BreedingSuccessfulEvent(mockResult));
            
            Assert(eventReceived, "Event should be received by subscriber");
            
            Log("✅ Event system test passed");
        }

        private CreatureDefinition CreateTestCreatureDefinition(string name, int compatibilityGroup)
        {
            var definition = ScriptableObject.CreateInstance<CreatureDefinition>();
            definition.speciesName = name;
            definition.breedingCompatibilityGroup = compatibilityGroup;
            definition.fertilityRate = 0.8f;
            definition.maturationAge = 90;
            definition.maxLifespan = 365 * 5;
            definition.baseStats = new CreatureStats
            {
                health = 100,
                attack = 20,
                defense = 15,
                speed = 10,
                intelligence = 5,
                charisma = 5
            };
            return definition;
        }

        private CreatureInstance CreateTestCreatureInstance(CreatureDefinition definition)
        {
            var genes = new Gene[]
            {
                new Gene { traitName = "Strength", value = Random.Range(0.3f, 0.9f), dominance = Random.Range(0.2f, 0.8f), isActive = true },
                new Gene { traitName = "Speed", value = Random.Range(0.3f, 0.9f), dominance = Random.Range(0.2f, 0.8f), isActive = true },
                new Gene { traitName = "Intelligence", value = Random.Range(0.3f, 0.9f), dominance = Random.Range(0.2f, 0.8f), isActive = true }
            };
            
            var genetics = new GeneticProfile(genes, 1);
            
            return new CreatureInstance
            {
                Definition = definition,
                GeneticProfile = genetics,
                AgeInDays = 120, // Adult age
                CurrentHealth = definition.baseStats.health,
                Happiness = 0.8f,
                Level = 1,
                IsWild = false
            };
        }

        private void OnBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            Log($"🎉 Breeding Success Event: {evt.Offspring.Length} offspring born!");
        }

        private void OnBreedingFailure(BreedingFailedEvent evt)
        {
            Log($"💔 Breeding Failure Event: {evt.Reason}");
        }

        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                UnityEngine.Debug.LogError($"❌ ASSERTION FAILED: {message}");
                throw new System.Exception($"Test assertion failed: {message}");
            }
        }

        private void Log(string message)
        {
            if (enableDetailedLogging)
            {
                UnityEngine.Debug.Log($"[Chimera Test] {message}");
            }
        }

        private void OnDestroy()
        {
            breedingSystem?.Dispose();
            eventBus?.Dispose();
        }
    }
}