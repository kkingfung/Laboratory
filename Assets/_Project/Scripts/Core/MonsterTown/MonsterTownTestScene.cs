using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Infrastructure;
using System.Collections.Generic;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Monster Town Test Scene - Complete test environment for validating Monster Town integration.
    ///
    /// This prefab sets up everything needed to test the Monster Town system:
    /// - Town management with buildings and resources
    /// - Monster population and breeding
    /// - Activity centers across all genres
    /// - Integration with Chimera genetics system
    ///
    /// Drop this into any scene to get a fully functional Monster Town test environment!
    /// </summary>
    public class MonsterTownTestScene : MonoBehaviour
    {
        [Header("🏘️ Monster Town Test Configuration")]
        [SerializeField] private bool autoInitializeOnStart = true;
        [SerializeField] private bool runFullTestSuite = true;
        [SerializeField] private bool enableVerboseLogging = true;

        [Header("🧬 Test Species Configuration")]
        [SerializeField] private ChimeraSpeciesConfig[] testSpecies;
        [SerializeField] private int initialMonsterCount = 8;
        [SerializeField] private bool enableRandomBreeding = true;

        [Header("🏗️ Building Test Configuration")]
        [SerializeField] private bool constructInitialBuildings = true;
        [SerializeField] private BuildingType[] testBuildingTypes = {
            BuildingType.BreedingCenter,
            BuildingType.TrainingGrounds,
            BuildingType.ActivityCenter,
            BuildingType.ResearchLab,
            BuildingType.MonsterHabitat
        };

        [Header("🎮 Activity Test Configuration")]
        [SerializeField] private bool testAllActivities = true;
        [SerializeField] private ActivityType[] testActivityTypes = {
            ActivityType.Racing,
            ActivityType.Combat,
            ActivityType.Puzzle,
            ActivityType.Strategy,
            ActivityType.Adventure,
            ActivityType.Music
        };

        [Header("💰 Economy Test Configuration")]
        [SerializeField] private bool testResourceGeneration = true;
        [SerializeField] private TownResources testStartingResources = new TownResources
        {
            coins = 5000,
            gems = 100,
            activityTokens = 200,
            materials = 1000,
            energy = 500
        };

        // Test state tracking
        private Dictionary<string, TestResult> testResults = new();
        private List<MonsterInstance> testMonsters = new();
        private bool isTestEnvironmentReady = false;
        private float testStartTime;

        // System references
        private TownManagementSystem townManager;
        private ChimeraSceneBootstrap chimeraBootstrap;
        private MonsterTownIntegrationGuide integrationGuide;

        #region Unity Lifecycle

        private async void Start()
        {
            if (autoInitializeOnStart)
            {
                await InitializeTestEnvironment();

                if (runFullTestSuite)
                {
                    await RunFullTestSuite();
                }
            }
        }

        private void Update()
        {
            if (isTestEnvironmentReady)
            {
                HandleTestInput();
                UpdateTestMonitoring();
            }
        }

        private void OnGUI()
        {
            if (isTestEnvironmentReady)
            {
                DrawTestStatusGUI();
            }
        }

        #endregion

        #region Test Environment Setup

        /// <summary>
        /// Initialize complete Monster Town test environment
        /// </summary>
        [ContextMenu("Initialize Test Environment")]
        public async UniTask InitializeTestEnvironment()
        {
            testStartTime = Time.time;
            LogTest("🧪 Initializing Monster Town Test Environment...");

            try
            {
                // Step 1: Setup core systems
                await SetupCoreTestSystems();

                // Step 2: Configure test town
                await ConfigureTestTown();

                // Step 3: Populate with test monsters
                await PopulateTestMonsters();

                // Step 4: Build test facilities
                if (constructInitialBuildings)
                {
                    await ConstructTestBuildings();
                }

                // Step 5: Setup activity centers
                await SetupTestActivities();

                // Step 6: Initialize economy
                SetupTestEconomy();

                isTestEnvironmentReady = true;
                LogTest($"✅ Test environment ready in {Time.time - testStartTime:F2} seconds!");

                RecordTestResult("Environment Setup", true, "Test environment initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Test environment setup failed: {ex}");
                RecordTestResult("Environment Setup", false, ex.Message);
            }
        }

        private async UniTask SetupCoreTestSystems()
        {
            LogTest("🔧 Setting up core test systems...");

            // Find or create integration guide
            integrationGuide = FindObjectOfType<MonsterTownIntegrationGuide>();
            if (integrationGuide == null)
            {
                var guideGO = new GameObject("MonsterTownIntegrationGuide");
                integrationGuide = guideGO.AddComponent<MonsterTownIntegrationGuide>();
            }

            // Run integration setup
            await integrationGuide.SetupMonsterTownIntegration();

            // Get system references
            townManager = FindObjectOfType<TownManagementSystem>();
            chimeraBootstrap = FindObjectOfType<ChimeraSceneBootstrap>();

            LogTest("Core systems setup complete");
        }

        private async UniTask ConfigureTestTown()
        {
            LogTest("🏘️ Configuring test town...");

            if (townManager == null)
            {
                throw new System.Exception("TownManagementSystem not available");
            }

            // Town should already be initialized by integration guide
            // Just verify it's working
            var currentResources = townManager.GetCurrentResources();
            LogTest($"Town resources: {currentResources}");

            await UniTask.Yield();
        }

        private async UniTask PopulateTestMonsters()
        {
            LogTest($"🧬 Populating test town with {initialMonsterCount} monsters...");

            if (testSpecies == null || testSpecies.Length == 0)
            {
                LogTest("⚠️ No test species configured - creating default species");
                testSpecies = new ChimeraSpeciesConfig[] { CreateDefaultTestSpecies() };
            }

            testMonsters.Clear();

            for (int i = 0; i < initialMonsterCount; i++)
            {
                var species = testSpecies[i % testSpecies.Length];
                var monster = CreateTestMonster(species, i);

                if (townManager.AddMonsterToTown(monster))
                {
                    testMonsters.Add(monster);
                    LogTest($"Added test monster: {monster.Name}");
                }

                // Spread creation across frames
                if (i % 2 == 0)
                {
                    await UniTask.Yield();
                }
            }

            LogTest($"✅ Town populated with {testMonsters.Count} monsters");
        }

        private async UniTask ConstructTestBuildings()
        {
            LogTest($"🏗️ Constructing {testBuildingTypes.Length} test buildings...");

            int successCount = 0;
            for (int i = 0; i < testBuildingTypes.Length; i++)
            {
                var buildingType = testBuildingTypes[i];
                var position = CalculateTestBuildingPosition(i);

                var success = await townManager.ConstructBuilding(buildingType, position);
                if (success)
                {
                    successCount++;
                    LogTest($"✅ Built {buildingType} at {position}");
                }
                else
                {
                    LogTest($"❌ Failed to build {buildingType}");
                }

                await UniTask.Yield();
            }

            LogTest($"✅ Building construction complete: {successCount}/{testBuildingTypes.Length} successful");
        }

        private async UniTask SetupTestActivities()
        {
            LogTest("🎮 Setting up test activity centers...");

            var activityManager = ServiceContainer.Instance.ResolveService<IActivityCenterManager>();
            if (activityManager == null)
            {
                LogTest("⚠️ ActivityCenterManager not available");
                return;
            }

            foreach (var activityType in testActivityTypes)
            {
                await activityManager.InitializeActivityCenter(activityType);
                LogTest($"✅ Activity center ready: {activityType}");
            }

            LogTest($"Activity centers setup complete: {testActivityTypes.Length} types available");
        }

        private void SetupTestEconomy()
        {
            LogTest("💰 Setting up test economy...");

            var resourceManager = ServiceContainer.Instance.ResolveService<IResourceManager>();
            if (resourceManager != null)
            {
                resourceManager.AddResources(testStartingResources);
                LogTest($"Added test resources: {testStartingResources}");
            }

            LogTest("Test economy setup complete");
        }

        #endregion

        #region Test Suite Execution

        /// <summary>
        /// Run comprehensive test suite for all Monster Town features
        /// </summary>
        [ContextMenu("Run Full Test Suite")]
        public async UniTask RunFullTestSuite()
        {
            if (!isTestEnvironmentReady)
            {
                LogTest("⚠️ Test environment not ready - initializing first");
                await InitializeTestEnvironment();
            }

            LogTest("🧪 Starting comprehensive Monster Town test suite...");

            testResults.Clear();

            try
            {
                // Test 1: Basic Integration
                await TestBasicIntegration();

                // Test 2: Monster Management
                await TestMonsterManagement();

                // Test 3: Building System
                await TestBuildingSystem();

                // Test 4: Resource Economy
                await TestResourceEconomy();

                // Test 5: Activity Participation
                await TestActivityParticipation();

                // Test 6: Cross-System Integration
                await TestCrossSystemIntegration();

                // Test 7: Performance Validation
                await TestPerformanceValidation();

                // Generate test report
                GenerateTestReport();

                LogTest("✅ Full test suite completed!");
            }
            catch (System.Exception ex)
            {
                LogTest($"❌ Test suite failed: {ex}");
                RecordTestResult("Test Suite", false, ex.Message);
            }
        }

        private async UniTask TestBasicIntegration()
        {
            LogTest("🧪 Testing basic integration...");

            try
            {
                // Check all core services are available
                var serviceContainer = ServiceContainer.Instance;
                var eventBus = serviceContainer?.ResolveService<IEventBus>();
                var resourceManager = serviceContainer?.ResolveService<IResourceManager>();
                var activityManager = serviceContainer?.ResolveService<IActivityCenterManager>();
                var buildingSystem = serviceContainer?.ResolveService<IBuildingSystem>();

                bool allServicesReady = serviceContainer != null && eventBus != null &&
                                       resourceManager != null && activityManager != null &&
                                       buildingSystem != null;

                RecordTestResult("Basic Integration", allServicesReady,
                    allServicesReady ? "All core services initialized" : "Some services missing");

                await UniTask.Yield();
            }
            catch (System.Exception ex)
            {
                RecordTestResult("Basic Integration", false, ex.Message);
            }
        }

        private async UniTask TestMonsterManagement()
        {
            LogTest("🧪 Testing monster management...");

            try
            {
                var townMonsters = townManager.GetTownMonsters();
                bool hasMonsters = townMonsters.Count > 0;

                // Test adding a new monster
                var newMonster = CreateTestMonster(testSpecies[0], 999);
                bool addSuccess = townManager.AddMonsterToTown(newMonster);

                var finalCount = townManager.GetTownMonsters().Count;
                bool countIncreased = finalCount > townMonsters.Count;

                bool testPassed = hasMonsters && addSuccess && countIncreased;

                RecordTestResult("Monster Management", testPassed,
                    $"Initial: {townMonsters.Count}, Final: {finalCount}, Add Success: {addSuccess}");

                await UniTask.Yield();
            }
            catch (System.Exception ex)
            {
                RecordTestResult("Monster Management", false, ex.Message);
            }
        }

        private async UniTask TestBuildingSystem()
        {
            LogTest("🧪 Testing building system...");

            try
            {
                var initialBuildings = townManager.GetBuildingsOfType(BuildingType.MonsterHabitat);
                var initialCount = initialBuildings.Count;

                // Try to build a new habitat
                var buildPosition = Vector3.right * 20f;
                bool buildSuccess = await townManager.ConstructBuilding(BuildingType.MonsterHabitat, buildPosition);

                var finalBuildings = townManager.GetBuildingsOfType(BuildingType.MonsterHabitat);
                var finalCount = finalBuildings.Count;

                bool testPassed = buildSuccess && finalCount > initialCount;

                RecordTestResult("Building System", testPassed,
                    $"Build Success: {buildSuccess}, Count Change: {initialCount} -> {finalCount}");
            }
            catch (System.Exception ex)
            {
                RecordTestResult("Building System", false, ex.Message);
            }
        }

        private async UniTask TestResourceEconomy()
        {
            LogTest("🧪 Testing resource economy...");

            try
            {
                var resourceManager = ServiceContainer.Instance.ResolveService<IResourceManager>();
                var initialResources = resourceManager.GetCurrentResources();

                // Test adding resources
                var testAddition = new TownResources { coins = 500, gems = 25 };
                resourceManager.AddResources(testAddition);

                var afterAddition = resourceManager.GetCurrentResources();
                bool additionWorked = afterAddition.coins > initialResources.coins;

                // Test deducting resources
                var testDeduction = new TownResources { coins = 100 };
                resourceManager.DeductResources(testDeduction);

                var afterDeduction = resourceManager.GetCurrentResources();
                bool deductionWorked = afterDeduction.coins < afterAddition.coins;

                bool testPassed = additionWorked && deductionWorked;

                RecordTestResult("Resource Economy", testPassed,
                    $"Addition: {additionWorked}, Deduction: {deductionWorked}");

                await UniTask.Yield();
            }
            catch (System.Exception ex)
            {
                RecordTestResult("Resource Economy", false, ex.Message);
            }
        }

        private async UniTask TestActivityParticipation()
        {
            LogTest("🧪 Testing activity participation...");

            try
            {
                if (testMonsters.Count == 0)
                {
                    RecordTestResult("Activity Participation", false, "No test monsters available");
                    return;
                }

                var testMonster = testMonsters[0];
                var activityManager = ServiceContainer.Instance.ResolveService<IActivityCenterManager>();

                var performance = new MonsterPerformance
                {
                    basePerformance = 0.7f,
                    geneticBonus = 0.1f,
                    experienceBonus = 0.05f
                };

                // Test racing activity
                var result = await activityManager.RunActivity(testMonster, ActivityType.Racing, performance);

                bool testPassed = result != null && result.ActivityType == ActivityType.Racing;

                RecordTestResult("Activity Participation", testPassed,
                    result != null ? $"Activity result: {result.ResultMessage}" : "No result returned");
            }
            catch (System.Exception ex)
            {
                RecordTestResult("Activity Participation", false, ex.Message);
            }
        }

        private async UniTask TestCrossSystemIntegration()
        {
            LogTest("🧪 Testing cross-system integration...");

            try
            {
                bool eventSystemWorking = false;
                var eventBus = ServiceContainer.Instance.ResolveService<IEventBus>();

                if (eventBus != null)
                {
                    eventBus.Subscribe<TownInitializedEvent>(evt => eventSystemWorking = true);
                    eventBus.Publish(new TownInitializedEvent(null, TownResources.Zero));

                    // Give event system time to process
                    await UniTask.Delay(100);
                }

                bool chimeraIntegration = chimeraBootstrap != null;
                bool serviceContainerWorking = ServiceContainer.Instance != null;

                bool testPassed = eventSystemWorking && chimeraIntegration && serviceContainerWorking;

                RecordTestResult("Cross-System Integration", testPassed,
                    $"Events: {eventSystemWorking}, Chimera: {chimeraIntegration}, Services: {serviceContainerWorking}");
            }
            catch (System.Exception ex)
            {
                RecordTestResult("Cross-System Integration", false, ex.Message);
            }
        }

        private async UniTask TestPerformanceValidation()
        {
            LogTest("🧪 Testing performance validation...");

            try
            {
                var startTime = Time.realtimeSinceStartup;
                var frameCount = Time.frameCount;

                // Simulate some load
                for (int i = 0; i < 100; i++)
                {
                    var testMonster = testMonsters[i % testMonsters.Count];
                    var performance = new MonsterPerformance { basePerformance = 0.5f };

                    // Don't await to test concurrent operations
                    _ = ServiceContainer.Instance.ResolveService<IActivityCenterManager>()
                        ?.RunActivity(testMonster, ActivityType.Puzzle, performance);

                    if (i % 10 == 0)
                        await UniTask.Yield();
                }

                var endTime = Time.realtimeSinceStartup;
                var endFrameCount = Time.frameCount;

                var totalTime = endTime - startTime;
                var frameRate = (endFrameCount - frameCount) / totalTime;

                bool performanceOk = frameRate > 30f && totalTime < 10f;

                RecordTestResult("Performance Validation", performanceOk,
                    $"Time: {totalTime:F2}s, FPS: {frameRate:F1}, Operations: 100");
            }
            catch (System.Exception ex)
            {
                RecordTestResult("Performance Validation", false, ex.Message);
            }
        }

        #endregion

        #region Utility Methods

        private MonsterInstance CreateTestMonster(ChimeraSpeciesConfig species, int index)
        {
            return new MonsterInstance
            {
                UniqueId = System.Guid.NewGuid().ToString(),
                Name = $"{species.speciesName} Test #{index}",
                GeneticProfile = species.GenerateRandomGeneticProfile(),
                Stats = new MonsterStats
                {
                    strength = Random.Range(30f, 70f),
                    agility = Random.Range(30f, 70f),
                    intelligence = Random.Range(30f, 70f),
                    vitality = Random.Range(30f, 70f),
                    social = Random.Range(30f, 70f),
                    adaptability = Random.Range(30f, 70f)
                },
                Happiness = Random.Range(0.6f, 0.9f),
                IsInTown = true,
                CurrentLocation = TownLocation.TownCenter
            };
        }

        private ChimeraSpeciesConfig CreateDefaultTestSpecies()
        {
            var species = ScriptableObject.CreateInstance<ChimeraSpeciesConfig>();
            species.speciesName = "Test Species";
            return species;
        }

        private Vector3 CalculateTestBuildingPosition(int index)
        {
            float angle = (index * 60f) * Mathf.Deg2Rad;
            float radius = 15f;
            return new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
        }

        private void RecordTestResult(string testName, bool success, string details)
        {
            testResults[testName] = new TestResult
            {
                TestName = testName,
                Success = success,
                Details = details,
                Timestamp = System.DateTime.UtcNow
            };

            var status = success ? "✅" : "❌";
            LogTest($"{status} {testName}: {details}");
        }

        private void GenerateTestReport()
        {
            LogTest("\n🧪 === MONSTER TOWN TEST REPORT ===");

            int passed = 0;
            int total = testResults.Count;

            foreach (var result in testResults.Values)
            {
                var status = result.Success ? "✅ PASS" : "❌ FAIL";
                LogTest($"{status} {result.TestName}: {result.Details}");

                if (result.Success) passed++;
            }

            var successRate = total > 0 ? (passed * 100f / total) : 0f;
            LogTest($"\n📊 Test Summary: {passed}/{total} tests passed ({successRate:F1}%)");

            if (successRate >= 80f)
            {
                LogTest("🎉 Monster Town integration is working excellently!");
            }
            else if (successRate >= 60f)
            {
                LogTest("⚠️ Monster Town integration has some issues but is functional");
            }
            else
            {
                LogTest("❌ Monster Town integration needs attention");
            }
        }

        private void LogTest(string message)
        {
            if (enableVerboseLogging)
            {
                Debug.Log($"[MonsterTownTest] {message}");
            }
        }

        #endregion

        #region Input & Monitoring

        private void HandleTestInput()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                _ = RunFullTestSuite();
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                GenerateTestReport();
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                _ = TestRandomActivity();
            }
        }

        private async UniTask TestRandomActivity()
        {
            if (testMonsters.Count == 0) return;

            var randomMonster = testMonsters[Random.Range(0, testMonsters.Count)];
            var randomActivity = testActivityTypes[Random.Range(0, testActivityTypes.Length)];

            var performance = new MonsterPerformance
            {
                basePerformance = Random.Range(0.3f, 0.9f),
                geneticBonus = Random.Range(0f, 0.2f)
            };

            var result = await townManager.SendMonsterToActivity(randomMonster.UniqueId, randomActivity);

            LogTest($"🎲 Random activity test: {randomMonster.Name} -> {randomActivity} = {(result.IsSuccess ? "SUCCESS" : "FAILED")}");
        }

        private void UpdateTestMonitoring()
        {
            // Could add real-time monitoring here
        }

        private void DrawTestStatusGUI()
        {
            var rect = new Rect(10, 10, 500, 200);
            GUI.Box(rect, "");

            var style = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            float yOffset = 20;

            GUI.Label(new Rect(20, yOffset, 480, 20), "🧪 Monster Town Test Environment", style);
            yOffset += 25;

            GUI.Label(new Rect(20, yOffset, 480, 20), $"Test Environment: {(isTestEnvironmentReady ? "✅ Ready" : "⚠️ Not Ready")}", style);
            yOffset += 20;

            GUI.Label(new Rect(20, yOffset, 480, 20), $"Test Monsters: {testMonsters.Count}", style);
            yOffset += 20;

            GUI.Label(new Rect(20, yOffset, 480, 20), $"Town Population: {townManager?.GetTownMonsters().Count ?? 0}", style);
            yOffset += 20;

            var resourceManager = ServiceContainer.Instance?.ResolveService<IResourceManager>();
            var resources = resourceManager?.GetCurrentResources();
            if (resources.HasValue)
            {
                GUI.Label(new Rect(20, yOffset, 480, 20), $"Town Coins: {resources.Value.coins}", style);
                yOffset += 20;
            }

            GUI.Label(new Rect(20, yOffset, 480, 20), $"Test Results: {testResults.Count} completed", style);
            yOffset += 20;

            GUI.Label(new Rect(20, yOffset, 480, 20), "Controls: F2=Full Test, F3=Report, F4=Random Activity", style);
        }

        #endregion
    }

    #region Test Data Structures

    [System.Serializable]
    public struct TestResult
    {
        public string TestName;
        public bool Success;
        public string Details;
        public System.DateTime Timestamp;
    }

    #endregion
}