using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// ECS-integrated Building System for Monster Town.
    /// Handles building construction, management, and lifecycle using Unity ECS patterns.
    ///
    /// Integrates seamlessly with existing Chimera ECS architecture.
    /// </summary>
    public class BuildingSystem : IBuildingSystem
    {
        private readonly EntityManager entityManager;
        private readonly IEventBus eventBus;

        // Building management
        private Vector2 townBounds;
        private float gridSize;
        private bool useGridPlacement;
        private readonly List<Entity> allBuildings = new();
        private readonly Dictionary<BuildingType, List<Entity>> buildingsByType = new();

        // ECS Queries
        private EntityQuery buildingQuery;
        private EntityQuery constructionQuery;

        // Construction management
        private readonly Dictionary<Entity, BuildingConstructionData> activeConstructions = new();

        public BuildingSystem(EntityManager entityManager, IEventBus eventBus)
        {
            this.entityManager = entityManager;
            this.eventBus = eventBus;

            InitializeECSQueries();
        }

        #region Initialization

        public void Initialize(Vector2 townBounds, float gridSize, bool useGrid)
        {
            this.townBounds = townBounds;
            this.gridSize = gridSize;
            this.useGridPlacement = useGrid;

            Debug.Log($"🏗️ Building System initialized: Bounds {townBounds}, Grid {gridSize}, UseGrid {useGrid}");
        }

        private void InitializeECSQueries()
        {
            // Query for all buildings
            buildingQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );

            // Query for buildings under construction
            constructionQuery = entityManager.CreateEntityQuery(
                ComponentType.ReadOnly<BuildingComponent>(),
                ComponentType.ReadOnly<ConstructionComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            );
        }

        #endregion

        #region IBuildingSystem Implementation

        public async UniTask<Entity> ConstructBuilding(BuildingConfig config, Vector3 position)
        {
            try
            {
                // Validate construction
                if (!CanPlaceBuilding(config, position))
                {
                    Debug.LogWarning($"Cannot place {config.buildingType} at {position}");
                    return Entity.Null;
                }

                // Snap to grid if enabled
                if (useGridPlacement)
                {
                    position = SnapToGrid(position);
                }

                // Create building entity
                var buildingEntity = CreateBuildingEntity(config, position);

                if (buildingEntity == Entity.Null)
                {
                    Debug.LogError($"Failed to create building entity for {config.buildingType}");
                    return Entity.Null;
                }

                // Start construction process
                if (config.constructionTime > 0f)
                {
                    await StartConstructionProcess(buildingEntity, config);
                }
                else
                {
                    CompleteBuildingConstruction(buildingEntity, config);
                }

                // Track building
                TrackBuilding(buildingEntity, config.buildingType);

                // Fire events
                eventBus?.Publish(new BuildingConstructedEvent(config.buildingType, buildingEntity, position));

                Debug.Log($"🏗️ Successfully constructed {config.buildingType} at {position}");
                return buildingEntity;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to construct building {config.buildingType}: {ex}");
                return Entity.Null;
            }
        }

        public bool CanPlaceBuilding(BuildingConfig config, Vector3 position)
        {
            // Check bounds
            if (!IsWithinTownBounds(position))
            {
                return false;
            }

            // Check for overlaps
            if (HasBuildingOverlap(position, config.size))
            {
                return false;
            }

            // Check prerequisites
            if (!HasRequiredBuildings(config.requiredBuildings))
            {
                return false;
            }

            return true;
        }

        public void DestroyBuilding(Entity building)
        {
            if (!entityManager.Exists(building))
                return;

            if (entityManager.HasComponent<BuildingComponent>(building))
            {
                var buildingComp = entityManager.GetComponentData<BuildingComponent>(building);

                // Remove from tracking
                UntrackBuilding(building, buildingComp.buildingType);

                // Clean up any active construction
                if (activeConstructions.ContainsKey(building))
                {
                    activeConstructions.Remove(building);
                }

                // Fire event
                eventBus?.Publish(new BuildingDestroyedEvent(building, buildingComp.buildingType));

                Debug.Log($"🏗️ Destroyed building {buildingComp.buildingType}");
            }

            entityManager.DestroyEntity(building);
        }

        public IReadOnlyList<Entity> GetAllBuildings()
        {
            return allBuildings.AsReadOnly();
        }

        #endregion

        #region Building Entity Creation

        private Entity CreateBuildingEntity(BuildingConfig config, Vector3 position)
        {
            // Create building archetype
            var archetype = entityManager.CreateArchetype(
                typeof(BuildingComponent),
                typeof(BuildingStatsComponent),
                typeof(BuildingFunctionsComponent),
                typeof(LocalTransform)
            );

            var buildingEntity = entityManager.CreateEntity(archetype);

            // Set building component
            var buildingComponent = new BuildingComponent
            {
                buildingType = config.buildingType,
                level = 1,
                isConstructed = config.constructionTime <= 0f,
                capacity = config.capacity,
                efficiency = 1f,
                lastMaintenanceTime = DateTime.UtcNow.ToBinary()
            };

            // Set building stats
            var statsComponent = new BuildingStatsComponent
            {
                maxHealth = 100f,
                currentHealth = 100f,
                energyConsumption = CalculateEnergyConsumption(config),
                maintenanceCost = CalculateMaintenanceCost(config),
                constructionProgress = config.constructionTime <= 0f ? 1f : 0f
            };

            // Set building functions
            var functionsComponent = new BuildingFunctionsComponent();
            InitializeBuildingFunctions(ref functionsComponent, config);

            // Set transform
            var transform = LocalTransform.FromPosition(position);

            // Apply components
            entityManager.SetComponentData(buildingEntity, buildingComponent);
            entityManager.SetComponentData(buildingEntity, statsComponent);
            entityManager.SetComponentData(buildingEntity, functionsComponent);
            entityManager.SetComponentData(buildingEntity, transform);

            // Add construction component if needed
            if (config.constructionTime > 0f)
            {
                entityManager.AddComponent<ConstructionComponent>(buildingEntity);
                entityManager.SetComponentData(buildingEntity, new ConstructionComponent
                {
                    totalTime = config.constructionTime,
                    remainingTime = config.constructionTime,
                    isUnderConstruction = true
                });
            }

            // Spawn visual prefab if available
            if (config.buildingPrefab != null)
            {
                SpawnBuildingVisual(buildingEntity, config, position);
            }

            return buildingEntity;
        }

        private void InitializeBuildingFunctions(ref BuildingFunctionsComponent functionsComponent, BuildingConfig config)
        {
            functionsComponent.canGenerateResources = HasFunction(config, FunctionType.ResourceGeneration);
            functionsComponent.canStoreMonsters = HasFunction(config, FunctionType.MonsterStorage);
            functionsComponent.canHostActivities = HasFunction(config, FunctionType.ActivityHosting);
            functionsComponent.canResearch = HasFunction(config, FunctionType.Research);
            functionsComponent.canTrain = HasFunction(config, FunctionType.Training);
            functionsComponent.canBreed = HasFunction(config, FunctionType.Breeding);
            functionsComponent.canHeal = HasFunction(config, FunctionType.Medical);
            functionsComponent.canSocialize = HasFunction(config, FunctionType.Social);
            functionsComponent.canStore = HasFunction(config, FunctionType.Storage);
        }

        private bool HasFunction(BuildingConfig config, FunctionType functionType)
        {
            if (config.functions == null) return false;

            foreach (var function in config.functions)
            {
                if (function.functionType == functionType)
                    return true;
            }
            return false;
        }

        private void SpawnBuildingVisual(Entity buildingEntity, BuildingConfig config, Vector3 position)
        {
            var visualGameObject = UnityEngine.Object.Instantiate(config.buildingPrefab, position, Quaternion.identity);

            // Link visual to entity (you might want to use GameObjectEntity or similar)
            var visualLinker = visualGameObject.GetComponent<BuildingVisualLinker>();
            if (visualLinker == null)
            {
                visualLinker = visualGameObject.AddComponent<BuildingVisualLinker>();
            }

            visualLinker.LinkToEntity(buildingEntity);
        }

        #endregion

        #region Construction Process

        private async UniTask StartConstructionProcess(Entity buildingEntity, BuildingConfig config)
        {
            var constructionData = new BuildingConstructionData
            {
                entity = buildingEntity,
                config = config,
                startTime = Time.time,
                totalTime = config.constructionTime
            };

            activeConstructions[buildingEntity] = constructionData;

            // Play construction effects
            PlayConstructionEffects(config, entityManager.GetComponentData<LocalTransform>(buildingEntity).Position);

            // Wait for construction to complete
            await UniTask.Delay(TimeSpan.FromSeconds(config.constructionTime));

            // Complete construction
            if (activeConstructions.ContainsKey(buildingEntity))
            {
                CompleteBuildingConstruction(buildingEntity, config);
                activeConstructions.Remove(buildingEntity);
            }
        }

        private void CompleteBuildingConstruction(Entity buildingEntity, BuildingConfig config)
        {
            if (!entityManager.Exists(buildingEntity))
                return;

            // Update building component
            var buildingComp = entityManager.GetComponentData<BuildingComponent>(buildingEntity);
            buildingComp.isConstructed = true;
            entityManager.SetComponentData(buildingEntity, buildingComp);

            // Update stats
            var statsComp = entityManager.GetComponentData<BuildingStatsComponent>(buildingEntity);
            statsComp.constructionProgress = 1f;
            entityManager.SetComponentData(buildingEntity, statsComp);

            // Remove construction component
            if (entityManager.HasComponent<ConstructionComponent>(buildingEntity))
            {
                entityManager.RemoveComponent<ConstructionComponent>(buildingEntity);
            }

            // Play completion effects
            PlayConstructionCompletionEffects(config, entityManager.GetComponentData<LocalTransform>(buildingEntity).Position);

            Debug.Log($"🏗️ Construction completed for {config.buildingType}");
        }

        private void PlayConstructionEffects(BuildingConfig config, Vector3 position)
        {
            // Play construction sound
            if (config.constructionSound != null)
            {
                AudioSource.PlayClipAtPoint(config.constructionSound, position);
            }

            // Spawn construction particle effect
            if (config.constructionEffect != null)
            {
                var effect = UnityEngine.Object.Instantiate(config.constructionEffect, position, Quaternion.identity);
                UnityEngine.Object.Destroy(effect.gameObject, 5f);
            }
        }

        private void PlayConstructionCompletionEffects(BuildingConfig config, Vector3 position)
        {
            // Could play completion sound, spawn completion particles, etc.
            Debug.Log($"🎉 Building construction completed at {position}");
        }

        #endregion

        #region Placement Validation

        private bool IsWithinTownBounds(Vector3 position)
        {
            // Assuming town center is at origin for simplicity
            return Mathf.Abs(position.x) <= townBounds.x / 2f &&
                   Mathf.Abs(position.z) <= townBounds.y / 2f;
        }

        private bool HasBuildingOverlap(Vector3 position, Vector3 size)
        {
            // Check overlap with existing buildings
            var bounds = new Bounds(position, size);

            using var buildings = buildingQuery.ToEntityArray(Allocator.Temp);
            using var transforms = buildingQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            for (int i = 0; i < buildings.Length; i++)
            {
                var buildingPos = transforms[i].Position;
                var buildingBounds = new Bounds(buildingPos, Vector3.one * 5f); // Default size, should get from component

                if (bounds.Intersects(buildingBounds))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasRequiredBuildings(BuildingType[] requiredBuildings)
        {
            if (requiredBuildings == null || requiredBuildings.Length == 0)
                return true;

            foreach (var requiredType in requiredBuildings)
            {
                if (!buildingsByType.ContainsKey(requiredType) || buildingsByType[requiredType].Count == 0)
                {
                    return false;
                }
            }

            return true;
        }

        private Vector3 SnapToGrid(Vector3 position)
        {
            float snappedX = Mathf.Round(position.x / gridSize) * gridSize;
            float snappedZ = Mathf.Round(position.z / gridSize) * gridSize;
            return new Vector3(snappedX, position.y, snappedZ);
        }

        #endregion

        #region Building Tracking

        private void TrackBuilding(Entity building, BuildingType buildingType)
        {
            allBuildings.Add(building);

            if (!buildingsByType.ContainsKey(buildingType))
            {
                buildingsByType[buildingType] = new List<Entity>();
            }

            buildingsByType[buildingType].Add(building);
        }

        private void UntrackBuilding(Entity building, BuildingType buildingType)
        {
            allBuildings.Remove(building);

            if (buildingsByType.ContainsKey(buildingType))
            {
                buildingsByType[buildingType].Remove(building);
            }
        }

        #endregion

        #region Helper Methods

        private float CalculateEnergyConsumption(BuildingConfig config)
        {
            // Base energy consumption based on building type and functions
            float baseConsumption = config.buildingType switch
            {
                BuildingType.BreedingCenter => 10f,
                BuildingType.TrainingGrounds => 15f,
                BuildingType.ResearchLab => 20f,
                BuildingType.ActivityCenter => 25f,
                _ => 5f
            };

            return baseConsumption;
        }

        private float CalculateMaintenanceCost(BuildingConfig config)
        {
            // Base maintenance cost
            return config.constructionCost?.ToTownResources().coins * 0.01f ?? 1f;
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            allBuildings.Clear();
            buildingsByType.Clear();
            activeConstructions.Clear();

            if (buildingQuery.IsCreated)
                buildingQuery.Dispose();
            if (constructionQuery.IsCreated)
                constructionQuery.Dispose();
        }

        #endregion
    }

    #region ECS Components

    /// <summary>
    /// Core building component for ECS
    /// </summary>
    public struct BuildingComponent : IComponentData
    {
        public BuildingType buildingType;
        public int level;
        public bool isConstructed;
        public int capacity;
        public float efficiency;
        public long lastMaintenanceTime; // DateTime.ToBinary()
    }

    /// <summary>
    /// Building statistics component
    /// </summary>
    public struct BuildingStatsComponent : IComponentData
    {
        public float maxHealth;
        public float currentHealth;
        public float energyConsumption;
        public float maintenanceCost;
        public float constructionProgress; // 0-1
    }

    /// <summary>
    /// Building functions component
    /// </summary>
    public struct BuildingFunctionsComponent : IComponentData
    {
        public bool canGenerateResources;
        public bool canStoreMonsters;
        public bool canHostActivities;
        public bool canResearch;
        public bool canTrain;
        public bool canBreed;
        public bool canHeal;
        public bool canSocialize;
        public bool canStore;
    }

    /// <summary>
    /// Construction status component
    /// </summary>
    public struct ConstructionComponent : IComponentData
    {
        public float totalTime;
        public float remainingTime;
        public bool isUnderConstruction;
    }

    #endregion

    #region Supporting Classes

    /// <summary>
    /// Construction tracking data
    /// </summary>
    public struct BuildingConstructionData
    {
        public Entity entity;
        public BuildingConfig config;
        public float startTime;
        public float totalTime;
    }

    /// <summary>
    /// Visual linker for connecting GameObjects to ECS entities
    /// </summary>
    public class BuildingVisualLinker : MonoBehaviour
    {
        public Entity linkedEntity;

        public void LinkToEntity(Entity entity)
        {
            linkedEntity = entity;
        }
    }

    /// <summary>
    /// Building destroyed event
    /// </summary>
    public class BuildingDestroyedEvent
    {
        public Entity BuildingEntity { get; private set; }
        public BuildingType BuildingType { get; private set; }

        public BuildingDestroyedEvent(Entity entity, BuildingType buildingType)
        {
            BuildingEntity = entity;
            BuildingType = buildingType;
        }
    }

    #endregion
}