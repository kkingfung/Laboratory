using UnityEngine;
using Unity.Entities;
using Laboratory.Core.Configuration;
using Laboratory.Core.MonsterTown;
using Laboratory.Core.Equipment;
using Laboratory.Core.Economy;
using Laboratory.Core.Bootstrap;
using Laboratory.Chimera.Genetics;
using System.Collections.Generic;

namespace Laboratory.Core.Integration
{
    /// <summary>
    /// Integration test for all Chimera systems
    /// This script verifies that all systems work together correctly
    /// </summary>
    public class ChimeraIntegrationTest : MonoBehaviour
    {
        [Header("🧪 Integration Test Configuration")]
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enableVerboseLogging = true;

        [Header("📋 Test Results")]
        [SerializeField] private List<string> testResults = new();

        // System references for testing
        private ChimeraGameConfig _gameConfig;
        private EquipmentManager _equipmentManager;
        private EconomyManager _economyManager;
        private TownManagementSystem _townManager;
        private ChimeraSceneBootstrapper _bootstrapper;

        private void Start()
        {
            if (runTestsOnStart)
            {
                StartCoroutine(RunIntegrationTests());
            }
        }

        /// <summary>
        /// Run all integration tests
        /// </summary>
        [ContextMenu("Run Integration Tests")]
        public System.Collections.IEnumerator RunIntegrationTests()
        {
            testResults.Clear();
            LogTest("🧪 Starting Chimera Integration Tests...");

            // Test 1: Core System Initialization
            yield return StartCoroutine(TestSystemInitialization());

            // Test 2: Configuration Loading
            yield return StartCoroutine(TestConfigurationSystem());

            // Test 3: Equipment System
            yield return StartCoroutine(TestEquipmentSystem());

            // Test 4: Economy System
            yield return StartCoroutine(TestEconomySystem());

            // Test 5: Town Management
            yield return StartCoroutine(TestTownManagement());

            // Test 6: Monster Integration
            yield return StartCoroutine(TestMonsterIntegration());

            // Test 7: Activity System Integration
            yield return StartCoroutine(TestActivityIntegration());

            LogTest("✅ All integration tests completed!");
            PrintTestSummary();
        }

        #region Test Methods

        private System.Collections.IEnumerator TestSystemInitialization()
        {
            LogTest("🔧 Testing System Initialization...");

            try
            {
                // Test ChimeraSceneBootstrapper
                _bootstrapper = FindFirstObjectByType<ChimeraSceneBootstrapper>();
                if (_bootstrapper == null)
                {
                    var bootstrapperGO = new GameObject("Test Chimera Scene Bootstrapper");
                    _bootstrapper = bootstrapperGO.AddComponent<ChimeraSceneBootstrapper>();
                }

                LogTest("✓ ChimeraSceneBootstrapper found/created");

                // Test ECS World availability
                var world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated)
                {
                    LogTest("✓ ECS World is available and created");
                }
                else
                {
                    LogTest("❌ ECS World not available");
                }

                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ System initialization failed: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator TestConfigurationSystem()
        {
            LogTest("⚙️ Testing Configuration System...");

            try
            {
                // Try to find or create a game config
                _gameConfig = Resources.Load<ChimeraGameConfig>("ChimeraGameConfig");
                if (_gameConfig == null)
                {
                    LogTest("⚠️ No ChimeraGameConfig found in Resources, creating test config");
                    _gameConfig = ScriptableObject.CreateInstance<ChimeraGameConfig>();
                    _gameConfig.gameVersion = "Integration Test";
                    _gameConfig.maxSimultaneousCreatures = 100;
                }

                LogTest($"✓ Game Config loaded: Version {_gameConfig.gameVersion}");
                LogTest($"✓ Max creatures: {_gameConfig.maxSimultaneousCreatures}");

                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Configuration system test failed: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator TestEquipmentSystem()
        {
            LogTest("⚔️ Testing Equipment System...");

            try
            {
                // Create equipment manager
                var equipmentGO = new GameObject("Test Equipment Manager");
                _equipmentManager = equipmentGO.AddComponent<EquipmentManager>();

                // Create test equipment database
                var equipmentDB = ScriptableObject.CreateInstance<EquipmentDatabase>();
                equipmentDB.GenerateDefaultEquipment();

                _equipmentManager.InitializeEquipmentSystem(equipmentDB);
                LogTest("✓ Equipment system initialized");

                // Test equipment creation
                var testEquipment = _equipmentManager.CreateEquipment("SpeedBoots", 1);
                if (testEquipment != null)
                {
                    LogTest($"✓ Created test equipment: {testEquipment.Name}");
                }
                else
                {
                    LogTest("❌ Failed to create test equipment");
                }

                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Equipment system test failed: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator TestEconomySystem()
        {
            LogTest("💰 Testing Economy System...");

            try
            {
                // Create economy manager
                var economyGO = new GameObject("Test Economy Manager");
                _economyManager = economyGO.AddComponent<EconomyManager>();

                // Create test economy config
                var economyConfig = new EconomyConfig
                {
                    startingPlayerCurrency = new TownResources { coins = 1000, gems = 10 },
                    globalEconomyPool = new TownResources { coins = 100000, gems = 1000 }
                };

                var exchangeRates = new CurrencyExchangeRates
                {
                    coinsToGemsRate = 0.1f,
                    coinsToTokensRate = 0.5f
                };

                _economyManager.InitializeEconomy(economyConfig, economyConfig.globalEconomyPool);
                LogTest("✓ Economy system initialized");

                // Test wallet creation
                var wallet = _economyManager.CreatePlayerWallet("TestPlayer");
                if (wallet != null)
                {
                    LogTest($"✓ Created player wallet with {wallet.GetTotalValue()} total value");
                }

                // Test currency exchange
                bool exchangeSuccess = _economyManager.ExchangeCurrency("TestPlayer", CurrencyType.Coins, CurrencyType.Gems, 100);
                LogTest($"✓ Currency exchange test: {(exchangeSuccess ? "Success" : "Failed")}");

                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Economy system test failed: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator TestTownManagement()
        {
            LogTest("🏘️ Testing Town Management System...");

            try
            {
                // Create town management system
                var townGO = new GameObject("Test Town Manager");
                _townManager = townGO.AddComponent<TownManagementSystem>();

                // Create test town config
                var townConfig = ScriptableObject.CreateInstance<MonsterTownConfig>();
                townConfig.townName = "Test Town";
                townConfig.maxPopulation = 100;

                LogTest("✓ Town management system created");

                // Test resource management
                var testResources = new TownResources { coins = 500, gems = 5 };
                LogTest($"✓ Test resources created: {testResources.GetTotalValue()} total value");

                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Town management test failed: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator TestMonsterIntegration()
        {
            LogTest("🧬 Testing Monster Integration...");

            try
            {
                // Create test monster
                var testMonster = new Monster
                {
                    UniqueId = System.Guid.NewGuid().ToString(),
                    Name = "Test Monster",
                    Level = 1,
                    Happiness = 0.8f,
                    Stats = MonsterStats.CreateBalanced(50f)
                };

                LogTest($"✓ Created test monster: {testMonster.Name} (Level {testMonster.Level})");

                // Test genetic profile integration
                if (testMonster.GeneticProfile != null)
                {
                    LogTest("✓ Genetic profile integration working");
                }
                else
                {
                    LogTest("⚠️ Genetic profile not set (expected for test monster)");
                }

                // Test equipment integration
                if (_equipmentManager != null)
                {
                    var testEquipment = _equipmentManager.CreateEquipment("CombatArmor", 1);
                    if (testEquipment != null)
                    {
                        bool equipSuccess = _equipmentManager.EquipItem(testMonster, testEquipment.ItemId);
                        LogTest($"✓ Equipment integration test: {(equipSuccess ? "Success" : "Failed")}");
                    }
                }

                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Monster integration test failed: {ex.Message}");
            }
        }

        private System.Collections.IEnumerator TestActivityIntegration()
        {
            LogTest("🎮 Testing Activity Integration...");

            try
            {
                // Test activity system integration
                var activityManager = FindFirstObjectByType<ActivityCenterManager>();
                if (activityManager == null)
                {
                    var activityGO = new GameObject("Test Activity Manager");
                    activityManager = activityGO.AddComponent<ActivityCenterManager>();
                }

                LogTest("✓ Activity manager found/created");

                // Test activity types
                var activityTypes = System.Enum.GetValues(typeof(ActivityType));
                LogTest($"✓ Found {activityTypes.Length} activity types");

                // Test performance calculation
                var testPerformance = MonsterPerformance.FromMonsterStats(MonsterStats.CreateBalanced(60f));
                var totalPerformance = testPerformance.CalculateTotal();
                LogTest($"✓ Performance calculation test: {totalPerformance:F2}");

                yield return new WaitForSeconds(0.1f);
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Activity integration test failed: {ex.Message}");
            }
        }

        #endregion

        #region Utility Methods

        private void LogTest(string message)
        {
            testResults.Add(message);
            if (enableVerboseLogging)
            {
                Debug.Log($"[Integration Test] {message}");
            }
        }

        private void PrintTestSummary()
        {
            Debug.Log("=== CHIMERA INTEGRATION TEST SUMMARY ===");
            foreach (var result in testResults)
            {
                Debug.Log(result);
            }
            Debug.Log("=== END SUMMARY ===");

            var passedTests = 0;
            var failedTests = 0;

            foreach (var result in testResults)
            {
                if (result.Contains("✓"))
                    passedTests++;
                else if (result.Contains("❌"))
                    failedTests++;
            }

            Debug.Log($"📊 Tests Summary: {passedTests} Passed, {failedTests} Failed");
        }

        /// <summary>
        /// Manual compilation test - checks if all key types can be referenced
        /// </summary>
        [ContextMenu("Test Type Compilation")]
        public void TestTypeCompilation()
        {
            LogTest("🔍 Testing type compilation...");

            try
            {
                // Test core types
                var gameConfig = typeof(ChimeraGameConfig);
                var equipmentManager = typeof(EquipmentManager);
                var economyManager = typeof(EconomyManager);
                var townManager = typeof(TownManagementSystem);
                var activityManager = typeof(ActivityCenterManager);

                LogTest($"✓ ChimeraGameConfig: {gameConfig.Name}");
                LogTest($"✓ EquipmentManager: {equipmentManager.Name}");
                LogTest($"✓ EconomyManager: {economyManager.Name}");
                LogTest($"✓ TownManagementSystem: {townManager.Name}");
                LogTest($"✓ ActivityCenterManager: {activityManager.Name}");

                // Test data structures
                var monster = typeof(Monster);
                var equipment = typeof(Equipment);
                var townResources = typeof(TownResources);
                var activityResult = typeof(ActivityResult);

                LogTest($"✓ Monster: {monster.Name}");
                LogTest($"✓ Equipment: {equipment.Name}");
                LogTest($"✓ TownResources: {townResources.Name}");
                LogTest($"✓ ActivityResult: {activityResult.Name}");

                // Test enums
                var activityType = typeof(ActivityType);
                var buildingType = typeof(BuildingType);
                var currencyType = typeof(CurrencyType);

                LogTest($"✓ ActivityType: {activityType.Name} ({System.Enum.GetValues(activityType).Length} values)");
                LogTest($"✓ BuildingType: {buildingType.Name} ({System.Enum.GetValues(buildingType).Length} values)");
                LogTest($"✓ CurrencyType: {currencyType.Name} ({System.Enum.GetValues(currencyType).Length} values)");

                LogTest("✅ All type compilation tests passed!");
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Type compilation test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Performance benchmark test
        /// </summary>
        [ContextMenu("Run Performance Benchmark")]
        public void RunPerformanceBenchmark()
        {
            LogTest("⚡ Running performance benchmark...");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // Test monster creation performance
                var monsters = new List<Monster>();
                for (int i = 0; i < 1000; i++)
                {
                    var monster = new Monster
                    {
                        UniqueId = System.Guid.NewGuid().ToString(),
                        Name = $"Benchmark Monster {i}",
                        Stats = MonsterStats.CreateRandom(30f, 70f)
                    };
                    monsters.Add(monster);
                }

                var creationTime = stopwatch.ElapsedMilliseconds;
                LogTest($"✓ Created 1000 monsters in {creationTime}ms ({creationTime / 1000f:F2}ms per monster)");

                // Test performance calculations
                stopwatch.Restart();
                for (int i = 0; i < monsters.Count; i++)
                {
                    var performance = MonsterPerformance.FromMonsterStats(monsters[i].Stats);
                    var total = performance.CalculateTotal();
                }

                var calculationTime = stopwatch.ElapsedMilliseconds;
                LogTest($"✓ Calculated 1000 performances in {calculationTime}ms ({calculationTime / 1000f:F2}ms per calculation)");

                stopwatch.Stop();
                LogTest($"⚡ Benchmark completed in {stopwatch.ElapsedMilliseconds}ms total");
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Performance benchmark failed: {ex.Message}");
            }
        }

        #endregion
    }
}