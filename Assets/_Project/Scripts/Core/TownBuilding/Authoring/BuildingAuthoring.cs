using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Laboratory.Core.TownBuilding.Components;
using Laboratory.Core.TownBuilding.Types;
using Laboratory.Core.ECS.Components;

namespace Laboratory.Core.TownBuilding.Authoring
{
    /// <summary>
    /// MonoBehaviour authoring for building placement
    /// </summary>
    public class BuildingAuthoring : MonoBehaviour
    {
        [Header("Building Configuration")]
        public BuildingType buildingType = BuildingType.MonsterHabitat;
        public BuildingTier buildingTier = BuildingTier.Basic;
        [Range(1, 10)] public int upgradeLevel = 1;

        [Header("Production Settings")]
        public ResourceType producedResource = ResourceType.Food;
        [Range(1f, 100f)] public float productionRate = 10f;
        [Range(100, 10000)] public int storageCapacity = 1000;

        [Header("Service Settings")]
        public ServiceType providedService = ServiceType.None;
        [Range(1, 1000)] public int serviceCapacity = 50;

        [Header("Requirements")]
        public bool requiresPower = false;
        public bool requiresWater = false;
        [Range(0f, 100f)] public float powerConsumption = 10f;
        [Range(0f, 100f)] public float waterConsumption = 5f;

        [ContextMenu("Create Building Entity")]
        public void CreateBuildingEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add building component
            entityManager.AddComponentData(entity, new BuildingComponent
            {
                Type = buildingType,
                Tier = buildingTier,
                Status = BuildingStatus.UnderConstruction,
                BuildingID = buildingType.GetHashCode(),
                OwnerPlayer = Entity.Null,
                PlacementPosition = transform.position,
                PlacementRotation = transform.rotation,
                ConstructionProgress = 0f,
                Efficiency = 1f,
                UpgradeLevel = upgradeLevel,
                Durability = 1f,
                RequiresPower = requiresPower,
                RequiresWater = requiresWater
            });

            // Add production component
            entityManager.AddComponentData(entity, new BuildingProductionComponent
            {
                ProducedResource = producedResource,
                ProductionRate = productionRate,
                ProductionEfficiency = 1f,
                StorageCapacity = storageCapacity,
                CurrentStorage = 0,
                ProvidedService = providedService,
                ServiceCapacity = serviceCapacity,
                ServiceQuality = 1f,
                ProcessesCreatures = IsCreatureBuilding(buildingType),
                MaxCreatures = CalculateMaxCreatures(buildingType, buildingTier),
                ProcessingSpeed = 1f
            });

            // Add resource storage
            entityManager.AddComponentData(entity, new ResourceStorageComponent
            {
                MaxFood = storageCapacity,
                MaxMaterials = storageCapacity,
                MaxEnergy = storageCapacity,
                MaxResearch = storageCapacity,
                MaxCurrency = storageCapacity,
                MaxSpecialItems = storageCapacity / 10,
                ResourceTransferRate = 10f,
                CanExportResources = true,
                CanImportResources = true
            });

            // Add connection component
            entityManager.AddComponentData(entity, new BuildingConnectionComponent
            {
                HasPowerConnection = !requiresPower, // Start connected if not required
                HasWaterConnection = !requiresWater,
                PowerConsumption = powerConsumption,
                WaterConsumption = waterConsumption,
                TransportEfficiency = 100,
                NetworkBonus = 0f,
                IsHubBuilding = IsHubBuilding(buildingType)
            });

            // Link to transform
            entityManager.AddComponentData(entity, LocalTransform.FromPositionRotation(transform.position, transform.rotation));

            // Link to GameObject
            entityManager.AddComponentData(entity, new GameObjectLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy
            });

            Debug.Log($"✅ Created {buildingTier} {buildingType} building entity");
        }

        private bool IsCreatureBuilding(BuildingType type)
        {
            return type switch
            {
                BuildingType.BreedingCenter or
                BuildingType.TrainingGround or
                BuildingType.MonsterHabitat or
                BuildingType.Hospital => true,
                _ => false
            };
        }

        private int CalculateMaxCreatures(BuildingType type, BuildingTier tier)
        {
            int baseCapacity = type switch
            {
                BuildingType.BreedingCenter => 20,
                BuildingType.TrainingGround => 50,
                BuildingType.MonsterHabitat => 100,
                BuildingType.Hospital => 30,
                _ => 0
            };

            return baseCapacity * (int)tier;
        }

        private bool IsHubBuilding(BuildingType type)
        {
            return type switch
            {
                BuildingType.TownHall or
                BuildingType.TransportHub or
                BuildingType.PowerPlant or
                BuildingType.WaterTreatment => true,
                _ => false
            };
        }

        private void OnDrawGizmos()
        {
            // Draw building footprint
            var color = buildingType switch
            {
                BuildingType.BreedingCenter => Color.magenta,
                BuildingType.TrainingGround => Color.yellow,
                BuildingType.ResearchLab => Color.blue,
                BuildingType.MonsterHabitat => Color.green,
                BuildingType.EquipmentShop => Color.cyan,
                BuildingType.PowerPlant => Color.red,
                _ => Color.white
            };

            Gizmos.color = color;
            var size = buildingTier switch
            {
                BuildingTier.Basic => Vector3.one * 2f,
                BuildingTier.Advanced => Vector3.one * 3f,
                BuildingTier.Expert => Vector3.one * 4f,
                BuildingTier.Master => Vector3.one * 5f,
                BuildingTier.Legendary => Vector3.one * 6f,
                _ => Vector3.one * 2f
            };

            Gizmos.DrawWireCube(transform.position, size);

            // Draw connections if required
            if (requiresPower)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 3f);
            }

            if (requiresWater)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1f);
            }
        }
    }
}