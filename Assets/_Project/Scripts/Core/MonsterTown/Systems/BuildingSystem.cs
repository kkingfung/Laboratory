using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;

namespace Laboratory.Core.MonsterTown
{
    /// <summary>
    /// ECS-integrated building system for Monster Town.
    /// Handles building construction, placement validation, and lifecycle management.
    /// </summary>
    public class BuildingSystem : IBuildingSystem
    {
        private readonly EntityManager _entityManager;
        private readonly IEventBus _eventBus;

        // Building management
        private readonly List<Entity> _allBuildings = new();
        private readonly Dictionary<BuildingType, List<Entity>> _buildingsByType = new();

        // Placement system
        private Vector2 _townBounds;
        private float _gridSize;
        private bool _useGridPlacement;
        private bool[,] _gridOccupancy;
        private int _gridWidth;
        private int _gridHeight;

        // Building prefabs/templates
        private readonly Dictionary<BuildingType, GameObject> _buildingPrefabs = new();

        public BuildingSystem(EntityManager entityManager, IEventBus eventBus)
        {
            _entityManager = entityManager ?? throw new ArgumentNullException(nameof(entityManager));
            _eventBus = eventBus;
        }

        #region IBuildingSystem Implementation

        public void Initialize(Vector2 townBounds, float gridSize, bool useGrid)
        {
            _townBounds = townBounds;
            _gridSize = gridSize;
            _useGridPlacement = useGrid;

            if (_useGridPlacement)
            {
                _gridWidth = Mathf.CeilToInt(_townBounds.x / _gridSize);
                _gridHeight = Mathf.CeilToInt(_townBounds.y / _gridSize);
                _gridOccupancy = new bool[_gridWidth, _gridHeight];

                Debug.Log($"🏗️ Building system initialized with {_gridWidth}x{_gridHeight} grid");
            }
            else
            {
                Debug.Log("🏗️ Building system initialized with free placement");
            }

            LoadBuildingPrefabs();
        }

        public async UniTask<Entity> ConstructBuilding(BuildingConfig config, Vector3 position)
        {
            if (config == null)
            {
                Debug.LogError("BuildingConfig is null");
                return Entity.Null;
            }

            if (!CanPlaceBuilding(config, position))
            {
                Debug.LogWarning($"Cannot place {config.buildingType} at {position}");
                return Entity.Null;
            }

            try
            {
                // Create building entity
                var buildingEntity = CreateBuildingEntity(config, position);

                if (buildingEntity == Entity.Null)
                {
                    Debug.LogError($"Failed to create entity for {config.buildingType}");
                    return Entity.Null;
                }

                // Mark grid position as occupied
                if (_useGridPlacement)
                {
                    MarkGridPosition(position, config.size, true);
                }

                // Track building
                TrackBuilding(config.buildingType, buildingEntity);

                // Create visual representation
                await CreateBuildingVisual(buildingEntity, config, position);

                // Fire events
                _eventBus?.Publish(new BuildingConstructedEvent(config.buildingType, buildingEntity, position));

                Debug.Log($"🏗️ Successfully constructed {config.buildingType} at {position}");
                return buildingEntity;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error constructing building {config.buildingType}: {ex.Message}");
                return Entity.Null;
            }
        }

        public bool CanPlaceBuilding(BuildingConfig config, Vector3 position)
        {
            if (config == null) return false;

            // Check bounds
            if (!IsWithinTownBounds(position, config.size))
                return false;

            // Check grid occupancy
            if (_useGridPlacement && !IsGridPositionFree(position, config.size))
                return false;

            // Check building-specific restrictions
            if (!MeetsBuildingRequirements(config, position))
                return false;

            return true;
        }

        public void DestroyBuilding(Entity building)
        {
            if (building == Entity.Null || !_entityManager.Exists(building))
                return;

            try
            {
                // Get building info before destroying
                var buildingType = GetBuildingType(building);
                var position = _entityManager.GetComponentData<LocalTransform>(building).Position;

                // Remove from tracking
                UntrackBuilding(buildingType, building);

                // Free grid position
                if (_useGridPlacement && _entityManager.HasComponent<BuildingComponent>(building))
                {
                    var buildingData = _entityManager.GetComponentData<BuildingComponent>(building);
                    MarkGridPosition(position, buildingData.size, false);
                }

                // Destroy entity
                _entityManager.DestroyEntity(building);

                // Fire events
                _eventBus?.Publish(new BuildingDestroyedEvent(buildingType, position));

                Debug.Log($"🏗️ Destroyed building {buildingType} at {position}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error destroying building: {ex.Message}");
            }
        }

        public IReadOnlyList<Entity> GetAllBuildings()
        {
            return _allBuildings.AsReadOnly();
        }

        public void Dispose()
        {
            // Clean up all tracked buildings
            foreach (var building in _allBuildings.ToArray())
            {
                if (_entityManager.Exists(building))
                {
                    _entityManager.DestroyEntity(building);
                }
            }

            _allBuildings.Clear();
            _buildingsByType.Clear();
            _buildingPrefabs.Clear();

            Debug.Log("🏗️ Building system disposed");
        }

        #endregion

        #region Entity Creation

        private Entity CreateBuildingEntity(BuildingConfig config, Vector3 position)
        {
            var entity = _entityManager.CreateEntity();

            // Add core components
            _entityManager.AddComponentData(entity, new LocalTransform
            {
                Position = position,
                Rotation = quaternion.identity,
                Scale = 1f
            });

            // Add building-specific data
            _entityManager.AddComponentData(entity, new BuildingComponent
            {
                buildingType = config.buildingType,
                level = 1,
                health = config.maxHealth,
                maxHealth = config.maxHealth,
                size = config.size,
                isConstructed = true,
                constructionTime = DateTime.UtcNow.ToBinary()
            });

            // Add resource generation if applicable
            if (config.resourceGeneration.HasAnyResource())
            {
                _entityManager.AddComponentData(entity, new ResourceGeneratorComponent
                {
                    generationRate = config.resourceGeneration,
                    lastGenerationTime = Time.time
                });
            }

            // Add activity center component if needed
            if (IsActivityBuilding(config.buildingType))
            {
                _entityManager.AddComponentData(entity, new ActivityCenterComponent
                {
                    activityType = GetActivityTypeForBuilding(config.buildingType),
                    isActive = true,
                    capacity = config.capacity
                });
            }

            return entity;
        }

        private async UniTask CreateBuildingVisual(Entity buildingEntity, BuildingConfig config, Vector3 position)
        {
            // Try to load and instantiate prefab
            if (_buildingPrefabs.TryGetValue(config.buildingType, out var prefab))
            {
                var visualGO = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                visualGO.name = $"{config.buildingType}_Visual";

                // Link visual to entity (if needed for hybrid approach)
                var visualLink = visualGO.GetComponent<BuildingVisualLink>();
                if (visualLink == null)
                {
                    visualLink = visualGO.AddComponent<BuildingVisualLink>();
                }
                visualLink.LinkedEntity = buildingEntity;
            }
            else
            {
                // Create basic visual representation
                var visualGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualGO.transform.position = position;
                visualGO.transform.localScale = config.size;
                visualGO.name = $"{config.buildingType}_BasicVisual";

                // Set color based on building type
                var renderer = visualGO.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = GetBuildingTypeColor(config.buildingType);
                }
            }

            await UniTask.Yield();
        }

        #endregion

        #region Placement Validation

        private bool IsWithinTownBounds(Vector3 position, Vector3 size)
        {
            var halfSize = size / 2f;
            var bounds = new Bounds(Vector3.zero, new Vector3(_townBounds.x, 10f, _townBounds.y));

            return bounds.Contains(position + halfSize) && bounds.Contains(position - halfSize);
        }

        private bool IsGridPositionFree(Vector3 worldPosition, Vector3 size)
        {
            if (!_useGridPlacement) return true;

            var gridPos = WorldToGrid(worldPosition);
            var gridSize = WorldSizeToGridSize(size);

            for (int x = gridPos.x; x < gridPos.x + gridSize.x; x++)
            {
                for (int y = gridPos.y; y < gridPos.y + gridSize.y; y++)
                {
                    if (x < 0 || x >= _gridWidth || y < 0 || y >= _gridHeight)
                        return false;

                    if (_gridOccupancy[x, y])
                        return false;
                }
            }

            return true;
        }

        private bool MeetsBuildingRequirements(BuildingConfig config, Vector3 position)
        {
            // Check if building type has special placement requirements
            return config.buildingType switch
            {
                BuildingType.BreedingCenter => !HasNearbyBuilding(position, BuildingType.CombatArena, 20f), // Breeding centers away from combat
                BuildingType.MonsterHabitat => HasNearbyBuilding(position, BuildingType.BreedingCenter, 30f), // Habitats near breeding
                BuildingType.ResearchLab => !HasNearbyBuilding(position, BuildingType.MusicStudio, 15f), // Labs need quiet
                _ => true // No special requirements
            };
        }

        #endregion

        #region Helper Methods

        private void LoadBuildingPrefabs()
        {
            // In a real implementation, this would load prefabs from Resources or Addressables
            // For now, we'll use procedural generation
            Debug.Log("🏗️ Building prefabs loaded (procedural generation)");
        }

        private void TrackBuilding(BuildingType buildingType, Entity building)
        {
            _allBuildings.Add(building);

            if (!_buildingsByType.ContainsKey(buildingType))
                _buildingsByType[buildingType] = new List<Entity>();

            _buildingsByType[buildingType].Add(building);
        }

        private void UntrackBuilding(BuildingType buildingType, Entity building)
        {
            _allBuildings.Remove(building);

            if (_buildingsByType.TryGetValue(buildingType, out var buildings))
            {
                buildings.Remove(building);
            }
        }

        private BuildingType GetBuildingType(Entity building)
        {
            if (_entityManager.HasComponent<BuildingComponent>(building))
            {
                return _entityManager.GetComponentData<BuildingComponent>(building).buildingType;
            }
            return BuildingType.BreedingCenter; // Default fallback
        }

        private Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            var gridX = Mathf.FloorToInt((worldPosition.x + _townBounds.x / 2f) / _gridSize);
            var gridY = Mathf.FloorToInt((worldPosition.z + _townBounds.y / 2f) / _gridSize);
            return new Vector2Int(gridX, gridY);
        }

        private Vector2Int WorldSizeToGridSize(Vector3 worldSize)
        {
            return new Vector2Int(
                Mathf.CeilToInt(worldSize.x / _gridSize),
                Mathf.CeilToInt(worldSize.z / _gridSize)
            );
        }

        private void MarkGridPosition(Vector3 worldPosition, Vector3 size, bool occupied)
        {
            if (!_useGridPlacement) return;

            var gridPos = WorldToGrid(worldPosition);
            var gridSize = WorldSizeToGridSize(size);

            for (int x = gridPos.x; x < gridPos.x + gridSize.x; x++)
            {
                for (int y = gridPos.y; y < gridPos.y + gridSize.y; y++)
                {
                    if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
                    {
                        _gridOccupancy[x, y] = occupied;
                    }
                }
            }
        }

        private bool HasNearbyBuilding(Vector3 position, BuildingType buildingType, float maxDistance)
        {
            if (!_buildingsByType.TryGetValue(buildingType, out var buildings))
                return false;

            foreach (var building in buildings)
            {
                if (_entityManager.Exists(building))
                {
                    var buildingPos = _entityManager.GetComponentData<LocalTransform>(building).Position;
                    if (Vector3.Distance(position, buildingPos) <= maxDistance)
                        return true;
                }
            }

            return false;
        }

        private bool IsActivityBuilding(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.ActivityCenter or
                BuildingType.RacingTrack or
                BuildingType.CombatArena or
                BuildingType.PuzzleAcademy or
                BuildingType.StrategyCommand or
                BuildingType.MusicStudio or
                BuildingType.AdventureGuild or
                BuildingType.CraftingWorkshop => true,
                _ => false
            };
        }

        private ActivityType GetActivityTypeForBuilding(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.RacingTrack => ActivityType.Racing,
                BuildingType.CombatArena => ActivityType.Combat,
                BuildingType.PuzzleAcademy => ActivityType.Puzzle,
                BuildingType.StrategyCommand => ActivityType.Strategy,
                BuildingType.MusicStudio => ActivityType.Music,
                BuildingType.AdventureGuild => ActivityType.Adventure,
                BuildingType.CraftingWorkshop => ActivityType.Crafting,
                _ => ActivityType.Racing // Default
            };
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
                BuildingType.RacingTrack => Color.yellow,
                BuildingType.CombatArena => Color.red,
                BuildingType.PuzzleAcademy => Color.cyan,
                BuildingType.MusicStudio => Color.magenta,
                _ => Color.gray
            };
        }

        #endregion
    }

    #region ECS Components

    /// <summary>
    /// Core building component for ECS entities
    /// </summary>
    public struct BuildingComponent : IComponentData
    {
        public BuildingType buildingType;
        public int level;
        public float health;
        public float maxHealth;
        public Vector3 size;
        public bool isConstructed;
        public long constructionTime; // DateTime.ToBinary()
    }

    /// <summary>
    /// Resource generation component for buildings
    /// </summary>
    public struct ResourceGeneratorComponent : IComponentData
    {
        public TownResources generationRate;
        public float lastGenerationTime;
    }

    /// <summary>
    /// Activity center component for activity buildings
    /// </summary>
    public struct ActivityCenterComponent : IComponentData
    {
        public ActivityType activityType;
        public bool isActive;
        public int capacity;
        public int currentOccupancy;
    }

    /// <summary>
    /// Building statistics component for performance metrics
    /// </summary>
    public struct BuildingStatsComponent : IComponentData
    {
        public float efficiency;
        public float maintenanceCost;
        public float happiness;
        public int level;
        public float uptime;
        public float lastMaintenanceTime;
    }

    /// <summary>
    /// Building functions component for special abilities
    /// </summary>
    public struct BuildingFunctionsComponent : IComponentData
    {
        public bool canHouseMonsters;
        public bool generatesResources;
        public bool providesHappiness;
        public bool isActivityCenter;
        public bool requiresMaintenance;
        public float specialBonus;
    }

    /// <summary>
    /// Construction progress component for buildings under construction
    /// </summary>
    public struct ConstructionComponent : IComponentData
    {
        public float constructionProgress; // 0.0 to 1.0
        public float constructionTimeRemaining;
        public bool isUnderConstruction;
        public TownResources totalCost;
        public TownResources remainingCost;
    }

    #endregion

    #region Visual Integration

    /// <summary>
    /// Links visual GameObject to ECS entity for hybrid approach
    /// </summary>
    public class BuildingVisualLink : MonoBehaviour
    {
        public Entity LinkedEntity { get; set; }

        private void OnDestroy()
        {
            // Clean up entity when visual is destroyed
            if (LinkedEntity != Entity.Null)
            {
                var entityManager = World.DefaultGameObjectInjectionWorld?.EntityManager;
                if (entityManager != null && entityManager.Exists(LinkedEntity))
                {
                    entityManager.DestroyEntity(LinkedEntity);
                }
            }
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// Building destroyed event
    /// </summary>
    public class BuildingDestroyedEvent
    {
        public BuildingType BuildingType { get; private set; }
        public Vector3 Position { get; private set; }

        public BuildingDestroyedEvent(BuildingType buildingType, Vector3 position)
        {
            BuildingType = buildingType;
            Position = position;
        }
    }

    #endregion
}