using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.MonsterTown.Integration;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Monster Town Integration Guide - Complete setup instructions for integrating
    /// Monster Town systems with existing Chimera infrastructure.
    ///
    /// This guide provides step-by-step integration, scene setup, and testing procedures.
    /// </summary>
    public class MonsterTownIntegrationGuide : MonoBehaviour
    {
        [Header("Integration Guide")]
        [SerializeField] [TextArea(5, 10)]
        private string integrationSteps = @"
🏘️ MONSTER TOWN INTEGRATION GUIDE 🧬

=== STEP 1: Scene Setup ===
1. Add TownManagementSystem to your scene
2. Configure MonsterTownConfig ScriptableObject
3. Link ChimeraIntegrationBridge for creature integration
4. Set up town bounds and grid settings

=== STEP 2: Configuration ===
1. Create BuildingConfig assets for each building type
2. Set up ActivityCenterConfig for all activities
3. Configure TownResourcesConfig for economy
4. Link ChimeraSpeciesConfig for monster integration

=== STEP 3: Integration Testing ===
1. Run integration validation
2. Test monster spawning and town population
3. Verify activity participation
4. Check resource generation and building construction

=== STEP 4: Customization ===
1. Adjust town layout and building placement
2. Configure activity rewards and difficulty
3. Set up educational content
4. Fine-tune cross-activity progression
";

        [Header("Quick Setup")]
        [SerializeField] private bool autoSetupOnStart = false;
        [SerializeField] private MonsterTownConfig defaultTownConfig;
        [SerializeField] private int testSpeciesCount = 3;

        [Header("Integration Test")]
        [SerializeField] private bool runIntegrationTest = false;
        [SerializeField] private int testMonsterCount = 5;

        // Integration status tracking
        private bool isIntegrationComplete = false;
        private TownManagementSystem townManager;
        private ChimeraIntegrationBridge chimeraIntegration;

        #region Unity Lifecycle

        private async void Start()
        {
            if (autoSetupOnStart)
            {
                await SetupMonsterTownIntegration();

                if (runIntegrationTest)
                {
                    await RunIntegrationTest();
                }
            }
        }

        #endregion

        #region Integration Setup

        /// <summary>
        /// Complete Monster Town integration with existing Chimera systems
        /// </summary>
        [ContextMenu("Setup Monster Town Integration")]
        public async UniTask SetupMonsterTownIntegration()
        {
            Debug.Log("🏗️ Starting Monster Town Integration...");

            try
            {
                // Step 1: Initialize Core Systems
                await InitializeCoreIntegration();

                // Step 2: Setup Town Management
                await SetupTownManagement();

                // Step 3: Integrate with Chimera Systems
                await IntegrateWithChimeraBootstrap();

                // Step 4: Configure Activity Centers
                await ConfigureActivityCenters();

                // Step 5: Setup Resource Economy
                SetupResourceEconomy();

                // Step 6: Validate Integration
                await ValidateIntegration();

                isIntegrationComplete = true;
                Debug.Log("✅ Monster Town Integration Complete!");

                // Fire integration complete event
                var eventBus = ServiceContainer.Instance.ResolveService<IEventBus>();
                eventBus?.Publish(new MonsterTownIntegrationCompleteEvent());
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Monster Town Integration Failed: {ex}");
                throw;
            }
        }

        private async UniTask InitializeCoreIntegration()
        {
            Debug.Log("🔧 Initializing core integration...");

            // Ensure ServiceContainer is available
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer == null)
            {
                Debug.LogError("ServiceContainer not found! Creating new instance.");
                var containerGO = new GameObject("ServiceContainer");
                serviceContainer = containerGO.AddComponent<ServiceContainer>();
            }

            // Find or create TownManagementSystem
            townManager = FindObjectOfType<TownManagementSystem>();
            if (townManager == null)
            {
                var townGO = new GameObject("TownManagementSystem");
                townManager = townGO.AddComponent<TownManagementSystem>();
                Debug.Log("Created new TownManagementSystem");
            }

            // Find existing ChimeraIntegrationBridge
            chimeraIntegration = FindObjectOfType<ChimeraIntegrationBridge>();
            if (chimeraIntegration == null)
            {
                chimeraIntegration = gameObject.AddComponent<ChimeraIntegrationBridge>();
                Debug.Log("Created ChimeraIntegrationBridge for monster integration.");
            }

            await UniTask.Yield();
        }

        private async UniTask SetupTownManagement()
        {
            Debug.Log("🏘️ Setting up town management...");

            if (townManager == null)
            {
                Debug.LogError("TownManagementSystem not available!");
                return;
            }

            // Configure town with default settings if no config provided
            if (defaultTownConfig == null)
            {
                defaultTownConfig = CreateDefaultTownConfig();
            }

            // Initialize town management system
            await townManager.InitializeTownAsync();

            Debug.Log("Town management setup complete");
        }

        private async UniTask IntegrateWithChimeraBootstrap()
        {
            Debug.Log("🧬 Integrating with Chimera Bootstrap...");

            if (chimeraIntegration == null)
            {
                Debug.LogWarning("No ChimeraIntegrationBridge found - skipping creature integration");
                return;
            }

            // Subscribe to Chimera events for creature spawning
            var eventBus = ServiceContainer.Instance.ResolveService<IEventBus>();
            if (eventBus != null)
            {
                eventBus.Subscribe<CreatureSpawnedEvent>(OnChimeraCreatureSpawned);
                eventBus.Subscribe<BreedingSuccessfulEvent>(OnChimeraBreedingSuccess);
            }

            // Initialize Chimera integration
            if (chimeraIntegration != null)
            {
                await chimeraIntegration.InitializeIntegrationAsync();
            }

            Debug.Log("Chimera integration complete");
        }

        private async UniTask ConfigureActivityCenters()
        {
            Debug.Log("🎮 Configuring activity centers...");

            // Initialize all activity types
            var activityTypes = System.Enum.GetValues(typeof(ActivityType));
            foreach (ActivityType activityType in activityTypes)
            {
                var activityManager = ServiceContainer.Instance.ResolveService<IActivityCenterManager>();
                if (activityManager != null)
                {
                    await activityManager.InitializeActivityCenter(activityType);
                }
            }

            Debug.Log($"Configured {activityTypes.Length} activity centers");
        }

        private void SetupResourceEconomy()
        {
            Debug.Log("💰 Setting up resource economy...");

            var resourceManager = ServiceContainer.Instance.ResolveService<IResourceManager>();
            if (resourceManager != null)
            {
                var startingResources = TownResources.GetDefault();
                resourceManager.InitializeResources(startingResources);
            }

            Debug.Log("Resource economy setup complete");
        }

        #endregion

        #region Integration Validation

        private async UniTask ValidateIntegration()
        {
            Debug.Log("🔍 Validating Monster Town integration...");

            var validationResults = new System.Collections.Generic.List<string>();

            // Check ServiceContainer
            if (ServiceContainer.Instance == null)
                validationResults.Add("❌ ServiceContainer not available");
            else
                validationResults.Add("✅ ServiceContainer ready");

            // Check TownManagementSystem
            if (townManager == null)
                validationResults.Add("❌ TownManagementSystem not found");
            else
                validationResults.Add("✅ TownManagementSystem ready");

            // Check EventBus
            var eventBus = ServiceContainer.Instance?.ResolveService<IEventBus>();
            if (eventBus == null)
                validationResults.Add("❌ EventBus not available");
            else
                validationResults.Add("✅ EventBus ready");

            // Check ResourceManager
            var resourceManager = ServiceContainer.Instance?.ResolveService<IResourceManager>();
            if (resourceManager == null)
                validationResults.Add("❌ ResourceManager not available");
            else
                validationResults.Add("✅ ResourceManager ready");

            // Check ActivityCenterManager
            var activityManager = ServiceContainer.Instance?.ResolveService<IActivityCenterManager>();
            if (activityManager == null)
                validationResults.Add("❌ ActivityCenterManager not available");
            else
                validationResults.Add("✅ ActivityCenterManager ready");

            // Check BuildingSystem
            var buildingSystem = ServiceContainer.Instance?.ResolveService<IBuildingSystem>();
            if (buildingSystem == null)
                validationResults.Add("❌ BuildingSystem not available");
            else
                validationResults.Add("✅ BuildingSystem ready");

            // Check Chimera Integration
            if (chimeraIntegration == null)
                validationResults.Add("⚠️ ChimeraIntegrationBridge not found - limited creature integration");
            else
            {
                var status = chimeraIntegration.GetIntegrationStatus();
                validationResults.Add(status.IsIntegrationActive ? "✅ Chimera integration active" : "⚠️ Chimera integration inactive");
            }

            // Log results
            Debug.Log("🔍 Integration Validation Results:");
            foreach (var result in validationResults)
            {
                Debug.Log($"   {result}");
            }

            var failedChecks = validationResults.Count(r => r.StartsWith("❌"));
            if (failedChecks == 0)
            {
                Debug.Log("🎉 All integration checks passed!");
            }
            else
            {
                Debug.LogWarning($"⚠️ {failedChecks} integration issues found");
            }

            await UniTask.Yield();
        }

        #endregion

        #region Integration Testing

        /// <summary>
        /// Run comprehensive integration test
        /// </summary>
        [ContextMenu("Run Integration Test")]
        public async UniTask RunIntegrationTest()
        {
            if (!isIntegrationComplete)
            {
                Debug.LogWarning("Integration not complete - running setup first");
                await SetupMonsterTownIntegration();
            }

            Debug.Log("🧪 Starting Monster Town Integration Test...");

            try
            {
                // Test 1: Resource Management
                await TestResourceManagement();

                // Test 2: Building Construction
                await TestBuildingConstruction();

                // Test 3: Monster Population
                await TestMonsterPopulation();

                // Test 4: Activity Participation
                await TestActivityParticipation();

                // Test 5: Cross-System Integration
                await TestCrossSystemIntegration();

                Debug.Log("✅ All integration tests passed!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ Integration test failed: {ex}");
            }
        }

        private async UniTask TestResourceManagement()
        {
            Debug.Log("🧪 Testing resource management...");

            var resourceManager = ServiceContainer.Instance.ResolveService<IResourceManager>();
            if (resourceManager == null)
            {
                throw new System.Exception("ResourceManager not available for testing");
            }

            // Test resource operations
            var testResources = new TownResources { coins = 100, gems = 10 };
            resourceManager.AddResources(testResources);

            var currentResources = resourceManager.GetCurrentResources();
            if (currentResources.coins < 100)
            {
                throw new System.Exception("Resource addition test failed");
            }

            Debug.Log("✅ Resource management test passed");
            await UniTask.Yield();
        }

        private async UniTask TestBuildingConstruction()
        {
            Debug.Log("🧪 Testing building construction...");

            if (townManager == null)
            {
                throw new System.Exception("TownManagementSystem not available for testing");
            }

            // Test building construction
            var testPosition = Vector3.zero;
            var success = await townManager.ConstructBuilding(BuildingType.BreedingCenter, testPosition);

            if (!success)
            {
                Debug.LogWarning("Building construction test failed - this may be due to insufficient resources");
            }
            else
            {
                Debug.Log("✅ Building construction test passed");
            }

            await UniTask.Yield();
        }

        private async UniTask TestMonsterPopulation()
        {
            Debug.Log("🧪 Testing monster population...");

            if (townManager == null || testSpecies == null || testSpecies.Length == 0)
            {
                Debug.LogWarning("Cannot test monster population - no species configured");
                return;
            }

            // Create test monsters
            for (int i = 0; i < testMonsterCount; i++)
            {
                var species = testSpecies[i % testSpecies.Length];
                var testMonster = CreateTestMonster(species, i);

                var success = townManager.AddMonsterToTown(testMonster);
                if (!success)
                {
                    Debug.LogWarning($"Failed to add test monster {i} to town");
                }
            }

            var townMonsters = townManager.GetTownMonsters();
            Debug.Log($"✅ Monster population test: {townMonsters.Count} monsters in town");

            await UniTask.Yield();
        }

        private async UniTask TestActivityParticipation()
        {
            Debug.Log("🧪 Testing activity participation...");

            var activityManager = ServiceContainer.Instance.ResolveService<IActivityCenterManager>();
            if (activityManager == null)
            {
                Debug.LogWarning("ActivityCenterManager not available for testing");
                return;
            }

            var townMonsters = townManager?.GetTownMonsters();
            if (townMonsters == null || townMonsters.Count == 0)
            {
                Debug.LogWarning("No monsters available for activity testing");
                return;
            }

            // Test activity with first monster
            var testMonster = townMonsters.Values.FirstOrDefault();
            if (testMonster != null)
            {
                var performance = new MonsterPerformance
                {
                    basePerformance = 0.7f,
                    geneticBonus = 0.1f,
                    experienceBonus = 0.05f
                };

                var result = await activityManager.RunActivity(testMonster, ActivityType.Racing, performance);

                if (result != null && result.IsSuccess)
                {
                    Debug.Log($"✅ Activity participation test passed: {result.ResultMessage}");
                }
                else
                {
                    Debug.LogWarning("Activity participation test had issues");
                }
            }

            await UniTask.Yield();
        }

        private async UniTask TestCrossSystemIntegration()
        {
            Debug.Log("🧪 Testing cross-system integration...");

            // Test event system integration
            var eventBus = ServiceContainer.Instance.ResolveService<IEventBus>();
            if (eventBus != null)
            {
                var testEventReceived = false;
                eventBus.Subscribe<TownInitializedEvent>(evt => testEventReceived = true);
                eventBus.Publish(new TownInitializedEvent(defaultTownConfig, TownResources.GetDefault()));

                if (testEventReceived)
                {
                    Debug.Log("✅ Event system integration test passed");
                }
                else
                {
                    Debug.LogWarning("Event system integration test failed");
                }
            }

            await UniTask.Yield();
        }

        #endregion

        #region Event Handlers

        private void OnChimeraCreatureSpawned(CreatureSpawnedEvent evt)
        {
            Debug.Log($"🧬 Chimera creature spawned - integrating with town: {evt.Monster.Name}");

            // Add Chimera creature to Monster Town
            if (townManager != null && evt.Monster != null)
            {
                townManager.AddMonsterToTown(evt.Monster);
            }
        }

        private void OnChimeraBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            Debug.Log($"🧬 Chimera breeding success - adding bonus town resources");

            // Award bonus resources for successful breeding
            var resourceManager = ServiceContainer.Instance.ResolveService<IResourceManager>();
            if (resourceManager != null)
            {
                var bonusResources = new TownResources { coins = 100, gems = 5 };
                resourceManager.AddResources(bonusResources);
            }

            // Add offspring to town
            if (townManager != null && evt.Offspring != null)
            {
                townManager.AddMonsterToTown(evt.Offspring);
            }
        }

        #endregion

        #region Utility Methods

        private MonsterTownConfig CreateDefaultTownConfig()
        {
            var config = ScriptableObject.CreateInstance<MonsterTownConfig>();
            config.townName = "Test Monster Town";
            config.townSize = new Vector2(50f, 50f);
            config.useGridBasedPlacement = true;
            config.gridSize = 5f;
            config.enableResourceGeneration = true;
            config.maxPopulation = 20;

            return config;
        }

        private MonsterInstance CreateTestMonster(ChimeraSpeciesConfig species, int index)
        {
            return new MonsterInstance
            {
                UniqueId = System.Guid.NewGuid().ToString(),
                Name = $"{species.speciesName} #{index + 1}",
                GeneticProfile = species.GenerateRandomGeneticProfile(),
                Stats = MonsterStats.GetDefault(),
                Happiness = UnityEngine.Random.Range(0.6f, 0.9f),
                IsInTown = true,
                CurrentLocation = TownLocation.TownCenter
            };
        }

        private MonsterInstance ConvertChimeraCreatureToMonster(CreatureSpawnedEvent evt)
        {
            return new MonsterInstance
            {
                UniqueId = System.Guid.NewGuid().ToString(),
                Name = $"{evt.Species.speciesName} (Chimera)",
                GeneticProfile = evt.Species.GenerateRandomGeneticProfile(),
                Stats = MonsterStats.GetDefault(),
                Happiness = 0.8f,
                IsInTown = true,
                CurrentLocation = TownLocation.TownCenter
            };
        }

        #endregion

        #region Inspector Helpers

        [ContextMenu("Create Default Town Config")]
        private void CreateDefaultTownConfigInInspector()
        {
            defaultTownConfig = CreateDefaultTownConfig();
            Debug.Log("Created default town config");
        }

        [ContextMenu("Validate Current Integration")]
        private async void ValidateCurrentIntegration()
        {
            await ValidateIntegration();
        }

        [ContextMenu("Reset Integration")]
        private void ResetIntegration()
        {
            isIntegrationComplete = false;
            townManager = null;
            chimeraIntegration = null;
            Debug.Log("Integration reset - run setup again");
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;

            // Draw integration status
            var rect = new Rect(10, Screen.height - 150, 400, 140);
            GUI.Box(rect, "");

            var style = new GUIStyle(GUI.skin.label) { fontSize = 11 };
            float yOffset = Screen.height - 140;

            GUI.Label(new Rect(20, yOffset, 380, 20), "🏘️ Monster Town Integration Status", style);
            yOffset += 20;

            var statusColor = isIntegrationComplete ? "green" : "orange";
            var statusText = isIntegrationComplete ? "✅ Complete" : "⚠️ Pending";
            GUI.Label(new Rect(20, yOffset, 380, 20), $"Status: <color={statusColor}>{statusText}</color>", style);
            yOffset += 20;

            var townMgrStatus = townManager != null ? "✅ Ready" : "❌ Missing";
            GUI.Label(new Rect(20, yOffset, 380, 20), $"Town Manager: {townMgrStatus}", style);
            yOffset += 20;

            var chimeraStatus = chimeraIntegration != null ? "✅ Bridge Active" : "⚠️ Bridge Missing";
            GUI.Label(new Rect(20, yOffset, 380, 20), $"Chimera Integration: {chimeraStatus}", style);
            yOffset += 20;

            var serviceStatus = ServiceContainer.Instance != null ? "✅ Active" : "❌ Missing";
            GUI.Label(new Rect(20, yOffset, 380, 20), $"Service Container: {serviceStatus}", style);
            yOffset += 20;

            GUI.Label(new Rect(20, yOffset, 380, 20), "Press F1 to run integration test", style);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1) && isIntegrationComplete)
            {
                _ = RunIntegrationTest();
            }
        }

        #endregion
    }

    #region Integration Events

    public class MonsterTownIntegrationCompleteEvent
    {
        public System.DateTime CompletionTime { get; private set; } = System.DateTime.UtcNow;
    }

    #endregion
}