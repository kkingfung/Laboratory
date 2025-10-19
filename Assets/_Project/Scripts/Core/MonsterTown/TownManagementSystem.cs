using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.Activities.Components;
using Laboratory.Core;
using Laboratory.Core.MonsterTown.Systems;
using Laboratory.Core.MonsterTown.Integration;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// Core Monster Town Management System - integrates seamlessly with existing Chimera infrastructure.
    /// Handles town building, resource management, and activity coordination.
    ///
    /// Extends existing ServiceContainer pattern and ECS architecture.
    /// </summary>
    public class TownManagementSystem : MonoBehaviour, ITownManager
    {
        [Header("Town Configuration")]
        [SerializeField] private MonsterTownConfig townConfig;
        [SerializeField] private BuildingConfig[] availableBuildings;
        [SerializeField] private bool autoInitializeOnStart = true;

        [Header("Town Layout")]
        [SerializeField] private Transform townCenter;
        [SerializeField] private Vector2 townBounds = new Vector2(100f, 100f);
        [SerializeField] private LayerMask buildingLayerMask = 1;
        [SerializeField] private bool useGridBasedPlacement = true;
        [SerializeField] private float gridSize = 5f;

        [Header("Resource Management")]
        [SerializeField] private TownResourcesConfig startingResources;
        [SerializeField] private bool enableResourceGeneration = true;
        [SerializeField] private float resourceUpdateInterval = 1f;

        [Header("Integration")]
        [SerializeField] private bool integratWithExistingChimera = true;
        [SerializeField] private bool enableActivityCenters = true;
        [SerializeField] private bool enableBreedingFacilities = true;

        // Core Systems
        private EntityManager entityManager;
        private IEventBus eventBus;
        private MonsterBreedingSystem breedingSystem;
        private ChimeraIntegrationBridge chimeraIntegration;
        private IBuildingSystem buildingSystem;
        private IResourceManager resourceManager;
        private IActivityCenterManager activityCenterManager;

        // Town State
        private TownResources currentResources;
        private Dictionary<BuildingType, List<Entity>> townBuildings = new();
        private Dictionary<string, MonsterInstance> townMonsters = new();
        private List<Entity> activityCenters = new();
        private bool isInitialized = false;
        private float lastResourceUpdate;

        // Events
        public event Action<TownResources> OnResourcesChanged;
        public event Action<BuildingConstructedEvent> OnBuildingConstructed;
        public event Action<MonsterInstance> OnMonsterAddedToTown;
        public event Action<ActivityCompletedEvent> OnActivityCompleted;

        #region Unity Lifecycle

        private async void Start()
        {
            if (autoInitializeOnStart)
            {
                await InitializeTownAsync();
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            UpdateResourceGeneration();
            UpdateActivityCenters();
            HandleTownMaintenance();
        }

        private void OnDrawGizmos()
        {
            if (townCenter == null) return;

            // Draw town bounds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(townCenter.position, new Vector3(townBounds.x, 1f, townBounds.y));

            // Draw grid if enabled
            if (useGridBasedPlacement && Application.isPlaying)
            {
                DrawTownGrid();
            }

            // Draw building positions
            if (Application.isPlaying && isInitialized)
            {
                DrawBuildingGizmos();
            }
        }

        #endregion

        #region Public API - ITownManager Implementation

        /// <summary>
        /// Initialize the complete Monster Town system
        /// </summary>
        public async UniTask InitializeTownAsync()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[TownManager] Town already initialized!");
                return;
            }

            Debug.Log("🏘️ Initializing Monster Town...");

            try
            {
                // Step 1: Initialize Core Systems
                await InitializeCoreSystemsAsync();

                // Step 2: Validate Configuration
                ValidateConfiguration();

                // Step 3: Initialize Resources
                InitializeResources();

                // Step 4: Setup Building System
                InitializeBuildingSystem();

                // Step 5: Setup Activity Centers
                if (enableActivityCenters)
                {
                    await InitializeActivityCentersAsync();
                }

                // Step 6: Integrate with Existing Chimera
                if (integratWithExistingChimera)
                {
                    IntegrateWithChimeraSystem();
                }

                // Step 7: Build Initial Town
                await BuildInitialTownAsync();

                isInitialized = true;
                Debug.Log("🏘️ Monster Town initialization complete!");

                // Fire initialization event
                eventBus?.Publish(new TownInitializedEvent(townConfig, currentResources));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Monster Town: {ex}");
                throw;
            }
        }

        /// <summary>
        /// Add a monster to the town's population
        /// </summary>
        public bool AddMonsterToTown(MonsterInstance monster)
        {
            if (monster == null || townMonsters.ContainsKey(monster.UniqueId))
                return false;

            townMonsters[monster.UniqueId] = monster;
            monster.CurrentLocation = TownLocation.TownCenter;
            monster.IsInTown = true;

            OnMonsterAddedToTown?.Invoke(monster);
            eventBus?.Publish(new MonsterAddedToTownEvent(monster));

            Debug.Log($"🧬 Monster {monster.Name} added to town population");
            return true;
        }

        /// <summary>
        /// Send a monster to participate in an activity
        /// </summary>
        public async UniTask<ActivityResult> SendMonsterToActivity(string monsterId, ActivityType activityType)
        {
            if (!townMonsters.TryGetValue(monsterId, out var monster))
            {
                Debug.LogError($"Monster {monsterId} not found in town");
                return ActivityResult.Failed("Monster not found in town");
            }

            if (activityCenterManager == null)
            {
                Debug.LogError("Activity Center Manager not initialized");
                return ActivityResult.Failed("Activity centers not available");
            }

            // Calculate monster performance for this activity
            var performance = CalculateMonsterPerformance(monster, activityType);

            // Send to activity
            var result = await activityCenterManager.RunActivity(monster, activityType, performance);

            // Process results
            await ProcessActivityResult(monster, result);

            OnActivityCompleted?.Invoke(new ActivityCompletedEvent(monster, activityType, result));

            return result;
        }

        /// <summary>
        /// Construct a new building in the town
        /// </summary>
        public async UniTask<bool> ConstructBuilding(BuildingType buildingType, Vector3 position)
        {
            var buildingConfig = GetBuildingConfig(buildingType);
            if (buildingConfig == null)
            {
                Debug.LogError($"Building config not found for {buildingType}");
                return false;
            }

            // Check resources
            if (!CanAffordBuilding(buildingConfig))
            {
                Debug.LogWarning($"Insufficient resources to build {buildingType}");
                return false;
            }

            // Check placement
            if (!IsValidBuildingPosition(position, buildingConfig))
            {
                Debug.LogWarning($"Invalid building position for {buildingType}");
                return false;
            }

            // Deduct resources
            DeductResources(buildingConfig.constructionCost);

            // Build
            var buildingEntity = await buildingSystem.ConstructBuilding(buildingConfig, position);

            if (buildingEntity != Entity.Null)
            {
                // Track building
                if (!townBuildings.ContainsKey(buildingType))
                    townBuildings[buildingType] = new List<Entity>();

                townBuildings[buildingType].Add(buildingEntity);

                // Fire events
                var constructedEvent = new BuildingConstructedEvent(buildingType, buildingEntity, position);
                OnBuildingConstructed?.Invoke(constructedEvent);
                eventBus?.Publish(constructedEvent);

                Debug.Log($"🏗️ Built {buildingType} at {position}");
                return true;
            }

            // Refund on failure
            AddResources(buildingConfig.constructionCost);
            return false;
        }

        /// <summary>
        /// Get current town resources
        /// </summary>
        public TownResources GetCurrentResources() => currentResources;

        /// <summary>
        /// Get all monsters currently in town
        /// </summary>
        public IReadOnlyDictionary<string, MonsterInstance> GetTownMonsters() => townMonsters;

        /// <summary>
        /// Get buildings of a specific type
        /// </summary>
        public IReadOnlyList<Entity> GetBuildingsOfType(BuildingType buildingType)
        {
            return townBuildings.TryGetValue(buildingType, out var buildings) ? buildings : new List<Entity>();
        }

        #endregion

        #region Core System Initialization

        private async UniTask InitializeCoreSystemsAsync()
        {
            Debug.Log("Initializing town core systems...");

            // Get Entity Manager
            entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            // Get or create services from ServiceContainer
            var serviceContainer = ServiceContainer.Instance;

            // Event Bus
            eventBus = serviceContainer.ResolveService<IEventBus>();
            if (eventBus == null)
            {
                eventBus = new UnifiedEventBus();
                serviceContainer.RegisterService<IEventBus>(eventBus);
                Debug.Log("Created new event bus for town");
            }

            // Breeding System (if exists)
            if (enableBreedingFacilities)
            {
                breedingSystem = GetComponent<MonsterBreedingSystem>() ?? gameObject.AddComponent<MonsterBreedingSystem>();
            }

            // Building System
            buildingSystem = new BuildingSystem(entityManager, eventBus);
            serviceContainer.RegisterService<IBuildingSystem>(buildingSystem);

            // Resource Manager
            resourceManager = new ResourceManager(eventBus);
            serviceContainer.RegisterService<IResourceManager>(resourceManager);

            // Activity Center Manager
            if (enableActivityCenters)
            {
                activityCenterManager = new ActivityCenterManager(eventBus);
                serviceContainer.RegisterService<IActivityCenterManager>(activityCenterManager);
            }

            await UniTask.Yield();
            Debug.Log("Town core systems initialized");
        }

        private void ValidateConfiguration()
        {
            if (townConfig == null)
            {
                Debug.LogError("[TownManager] No town configuration assigned!");
                throw new InvalidOperationException("Town configuration is required");
            }

            if (availableBuildings == null || availableBuildings.Length == 0)
            {
                Debug.LogWarning("[TownManager] No buildings configured");
            }

            if (townCenter == null)
            {
                townCenter = transform;
                Debug.Log("Using TownManager transform as town center");
            }
        }

        #endregion

        #region Resource Management

        private void InitializeResources()
        {
            currentResources = startingResources != null ?
                startingResources.ToTownResources() :
                TownResources.GetDefault();

            resourceManager.InitializeResources(currentResources);
            OnResourcesChanged?.Invoke(currentResources);

            Debug.Log($"💰 Town resources initialized: {currentResources}");
        }

        private void UpdateResourceGeneration()
        {
            if (!enableResourceGeneration) return;
            if (Time.time - lastResourceUpdate < resourceUpdateInterval) return;

            var generatedResources = CalculateResourceGeneration();
            if (generatedResources.HasAnyResource())
            {
                AddResources(generatedResources);
            }

            lastResourceUpdate = Time.time;
        }

        private TownResources CalculateResourceGeneration()
        {
            var generated = new TownResources();

            // Resource generation based on buildings
            foreach (var buildingPair in townBuildings)
            {
                var buildingType = buildingPair.Key;
                var buildings = buildingPair.Value;

                foreach (var building in buildings)
                {
                    if (entityManager.Exists(building))
                    {
                        // Generate resources based on building type
                        generated += GetBuildingResourceGeneration(buildingType);
                    }
                }
            }

            // Bonus from happy monsters
            var happinessBonus = CalculateHappinessResourceBonus();
            generated *= (1f + happinessBonus);

            return generated;
        }

        private void AddResources(TownResources resources)
        {
            currentResources += resources;
            resourceManager.UpdateResources(currentResources);
            OnResourcesChanged?.Invoke(currentResources);
        }

        private void DeductResources(TownResources cost)
        {
            currentResources -= cost;
            resourceManager.UpdateResources(currentResources);
            OnResourcesChanged?.Invoke(currentResources);
        }

        private bool CanAffordBuilding(BuildingConfig config)
        {
            return currentResources.CanAfford(config.constructionCost);
        }

        #endregion

        #region Activity & Monster Management

        private async UniTask InitializeActivityCentersAsync()
        {
            Debug.Log("🎮 Initializing Activity Centers...");

            // Initialize each activity type
            var activityTypes = Enum.GetValues(typeof(ActivityType));
            foreach (ActivityType activityType in activityTypes)
            {
                await activityCenterManager.InitializeActivityCenter(activityType);
            }

            Debug.Log($"Initialized {activityTypes.Length} activity centers");
        }

        private MonsterPerformance CalculateMonsterPerformance(MonsterInstance monster, ActivityType activityType)
        {
            // Use monster's genetic traits to calculate performance
            var genetics = monster.GeneticProfile;

            return activityType switch
            {
                ActivityType.Racing => new MonsterPerformance
                {
                    basePerformance = (genetics.GetTraitValue("Agility") * 0.6f + genetics.GetTraitValue("Vitality") * 0.4f),
                    geneticBonus = CalculateGeneticBonus(genetics, activityType),
                    equipmentBonus = CalculateEquipmentBonus(monster, activityType),
                    experienceBonus = monster.GetActivityExperience(activityType) * 0.1f
                },

                ActivityType.Combat => new MonsterPerformance
                {
                    basePerformance = (genetics.GetTraitValue("Strength") * 0.5f + genetics.GetTraitValue("Vitality") * 0.3f + genetics.GetTraitValue("Agility") * 0.2f),
                    geneticBonus = CalculateGeneticBonus(genetics, activityType),
                    equipmentBonus = CalculateEquipmentBonus(monster, activityType),
                    experienceBonus = monster.GetActivityExperience(activityType) * 0.1f
                },

                ActivityType.Puzzle => new MonsterPerformance
                {
                    basePerformance = (genetics.GetTraitValue("Intellect") * 0.7f + genetics.GetTraitValue("Charm") * 0.3f),
                    geneticBonus = CalculateGeneticBonus(genetics, activityType),
                    equipmentBonus = CalculateEquipmentBonus(monster, activityType),
                    experienceBonus = monster.GetActivityExperience(activityType) * 0.1f
                },

                _ => new MonsterPerformance { basePerformance = 0.5f }
            };
        }

        private async UniTask ProcessActivityResult(MonsterInstance monster, ActivityResult result)
        {
            if (result.IsSuccess)
            {
                // Award experience
                monster.AddActivityExperience(result.ActivityType, result.ExperienceGained);

                // Award resources
                AddResources(result.ResourcesEarned);

                // Improve monster stats slightly
                monster.ImproveStatsFromActivity(result.ActivityType, result.PerformanceRating);

                // Update happiness
                monster.AdjustHappiness(result.HappinessChange);

                Debug.Log($"🎉 {monster.Name} succeeded at {result.ActivityType}! Earned {result.ResourcesEarned}");
            }
            else
            {
                // Small happiness penalty for failure
                monster.AdjustHappiness(-0.1f);
                Debug.Log($"😞 {monster.Name} failed at {result.ActivityType}");
            }

            await UniTask.Yield();
        }

        #endregion

        #region Building & Layout Management

        private void InitializeBuildingSystem()
        {
            buildingSystem.Initialize(townBounds, gridSize, useGridBasedPlacement);
            Debug.Log("🏗️ Building system initialized");
        }

        private async UniTask BuildInitialTownAsync()
        {
            Debug.Log("🏘️ Building initial town structures...");

            if (townConfig.initialBuildings != null)
            {
                foreach (var initialBuilding in townConfig.initialBuildings)
                {
                    var position = CalculateInitialBuildingPosition(initialBuilding.buildingType);
                    await ConstructBuilding(initialBuilding.buildingType, position);

                    await UniTask.Yield(); // Spread across frames
                }
            }

            Debug.Log("Initial town construction complete");
        }

        private Vector3 CalculateInitialBuildingPosition(BuildingType buildingType)
        {
            // Simple spiral placement for initial buildings
            var offset = GetBuildingTypeOffset(buildingType);
            return townCenter.position + offset;
        }

        private Vector3 GetBuildingTypeOffset(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.BreedingCenter => new Vector3(-10f, 0f, 0f),
                BuildingType.TrainingGrounds => new Vector3(10f, 0f, 0f),
                BuildingType.ResearchLab => new Vector3(0f, 0f, -10f),
                BuildingType.MonsterHabitat => new Vector3(0f, 0f, 10f),
                BuildingType.ActivityCenter => new Vector3(15f, 0f, 15f),
                _ => Vector3.zero
            };
        }

        private bool IsValidBuildingPosition(Vector3 position, BuildingConfig config)
        {
            // Check bounds
            var townPos = townCenter.position;
            var bounds = new Bounds(townPos, new Vector3(townBounds.x, 10f, townBounds.y));
            if (!bounds.Contains(position)) return false;

            // Check for overlaps
            var size = config.size;
            var overlap = Physics.OverlapBox(position, size / 2f, Quaternion.identity, buildingLayerMask);
            return overlap.Length == 0;
        }

        #endregion

        #region Helper Methods

        private BuildingConfig GetBuildingConfig(BuildingType buildingType)
        {
            return Array.Find(availableBuildings, b => b.buildingType == buildingType);
        }

        private TownResources GetBuildingResourceGeneration(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.BreedingCenter => new TownResources { coins = 2 },
                BuildingType.TrainingGrounds => new TownResources { activityTokens = 1 },
                BuildingType.ResearchLab => new TownResources { gems = 1 },
                _ => new TownResources()
            };
        }

        private float CalculateHappinessResourceBonus()
        {
            float totalHappiness = 0f;
            foreach (var monster in townMonsters.Values)
            {
                totalHappiness += monster.Happiness;
            }

            return townMonsters.Count > 0 ? (totalHappiness / townMonsters.Count - 0.5f) * 0.5f : 0f;
        }

        private float CalculateGeneticBonus(GeneticProfile genetics, ActivityType activityType)
        {
            // Implementation depends on how genetics are structured
            return 0.1f; // Placeholder
        }

        private float CalculateEquipmentBonus(MonsterInstance monster, ActivityType activityType)
        {
            // Implementation depends on equipment system
            return 0.05f; // Placeholder
        }

        private void IntegrateWithChimeraSystem()
        {
            // Subscribe to integration events
            chimeraIntegration = GetComponent<ChimeraIntegrationBridge>() ?? gameObject.AddComponent<ChimeraIntegrationBridge>();
            eventBus.Subscribe<CreatureSpawnedEvent>(OnChimeraCreatureSpawned);
            eventBus.Subscribe<BreedingSuccessfulEvent>(OnChimeraBreedingSuccess);

            Debug.Log("🔗 Integrated with existing Chimera system");
        }

        private void OnChimeraCreatureSpawned(CreatureSpawnedEvent evt)
        {
            // Convert Chimera creature to Monster Town monster
            var monster = evt.Monster;
            if (monster != null)
            {
                AddMonsterToTown(monster);
            }
        }

        private void OnChimeraBreedingSuccess(BreedingSuccessfulEvent evt)
        {
            // Breeding in town gives bonus resources
            AddResources(new TownResources { coins = 50, gems = 5 });

            // Add offspring to town
            if (evt.Offspring != null)
            {
                AddMonsterToTown(evt.Offspring);
            }
        }

        private MonsterInstance ConvertChimeraCreatureToMonster(Entity creatureEntity)
        {
            // Implementation depends on entity component structure
            // For now, return a placeholder
            return new MonsterInstance
            {
                UniqueId = Guid.NewGuid().ToString(),
                Name = "Converted Creature",
                IsInTown = true,
                CurrentLocation = TownLocation.TownCenter
            };
        }

        private void UpdateActivityCenters()
        {
            // Update activity center states
            activityCenterManager?.Update(Time.deltaTime);
        }

        private void HandleTownMaintenance()
        {
            // Handle periodic town maintenance tasks
            // Could include happiness decay, building maintenance costs, etc.
        }

        private void DrawTownGrid()
        {
            var center = townCenter.position;
            var halfX = townBounds.x / 2f;
            var halfZ = townBounds.y / 2f;

            Gizmos.color = Color.gray;

            // Draw grid lines
            for (float x = -halfX; x <= halfX; x += gridSize)
            {
                Gizmos.DrawLine(
                    new Vector3(center.x + x, center.y, center.z - halfZ),
                    new Vector3(center.x + x, center.y, center.z + halfZ)
                );
            }

            for (float z = -halfZ; z <= halfZ; z += gridSize)
            {
                Gizmos.DrawLine(
                    new Vector3(center.x - halfX, center.y, center.z + z),
                    new Vector3(center.x + halfX, center.y, center.z + z)
                );
            }
        }

        private void DrawBuildingGizmos()
        {
            foreach (var buildingPair in townBuildings)
            {
                Gizmos.color = GetBuildingTypeColor(buildingPair.Key);

                foreach (var building in buildingPair.Value)
                {
                    if (entityManager.Exists(building))
                    {
                        var pos = entityManager.GetComponentData<Unity.Transforms.LocalTransform>(building).Position;
                        Gizmos.DrawCube(pos, Vector3.one * 2f);
                    }
                }
            }
        }

        private Color GetBuildingTypeColor(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.BreedingCenter => Color.pink,
                BuildingType.TrainingGrounds => Color.red,
                BuildingType.ResearchLab => Color.blue,
                BuildingType.MonsterHabitat => Color.green,
                BuildingType.ActivityCenter => Color.purple,
                _ => Color.white
            };
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            if (isInitialized)
            {
                buildingSystem?.Dispose();
                resourceManager?.Dispose();
                activityCenterManager?.Dispose();
            }
        }

        #endregion
    }
}