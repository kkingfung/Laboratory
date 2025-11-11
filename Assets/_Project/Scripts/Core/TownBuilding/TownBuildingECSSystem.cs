using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities;
using Laboratory.Core.Equipment;

namespace Laboratory.Core.TownBuilding
{
    /// <summary>
    /// Town Building System - Monster Town Construction and Management
    /// FEATURES: Building placement, resource management, facility upgrades, layout optimization
    /// PERFORMANCE: Efficient spatial management for large towns with 100+ buildings
    /// INTEGRATION: Connects all systems through building functionality
    /// </summary>

    #region Building Components

    /// <summary>
    /// Core building component for all structures
    /// </summary>
    public struct BuildingComponent : IComponentData
    {
        public BuildingType Type;
        public BuildingTier Tier;
        public BuildingStatus Status;
        public int BuildingID;
        public Entity OwnerPlayer;
        public float3 PlacementPosition;
        public quaternion PlacementRotation;
        public float ConstructionProgress;
        public float Efficiency;
        public int UpgradeLevel;
        public float Durability;
        public float MaintenanceCost;
        public bool RequiresPower;
        public bool RequiresWater;
    }

    /// <summary>
    /// Building production and functionality
    /// </summary>
    public struct BuildingProductionComponent : IComponentData
    {
        // Resource generation
        public ResourceType ProducedResource;
        public float ProductionRate;
        public float ProductionEfficiency;
        public int StorageCapacity;
        public int CurrentStorage;

        // Service provision
        public ServiceType ProvidedService;
        public int ServiceCapacity;
        public int CurrentServiceLoad;
        public float ServiceQuality;

        // Special functions
        public bool ProcessesCreatures;
        public int MaxCreatures;
        public int CurrentCreatures;
        public float ProcessingSpeed;
    }

    /// <summary>
    /// Building connections and infrastructure
    /// </summary>
    public struct BuildingConnectionComponent : IComponentData
    {
        // Utilities
        public bool HasPowerConnection;
        public bool HasWaterConnection;
        public bool HasDataConnection;
        public float PowerConsumption;
        public float WaterConsumption;

        // Transport links
        public Entity NearestRoad;
        public float RoadDistance;
        public Entity ConnectedDistrict;
        public int TransportEfficiency;

        // Network effects
        public FixedList64Bytes<Entity> ConnectedBuildings;
        public float NetworkBonus;
        public bool IsHubBuilding;
    }

    /// <summary>
    /// Resource storage and management
    /// </summary>
    public struct ResourceStorageComponent : IComponentData
    {
        // Resource amounts
        public int Food;
        public int Materials;
        public int Energy;
        public int Research;
        public int Currency;
        public int SpecialItems;

        // Storage limits
        public int MaxFood;
        public int MaxMaterials;
        public int MaxEnergy;
        public int MaxResearch;
        public int MaxCurrency;
        public int MaxSpecialItems;

        // Transfer rates
        public float ResourceTransferRate;
        public bool CanExportResources;
        public bool CanImportResources;
    }

    /// <summary>
    /// Town district management
    /// </summary>
    public struct DistrictComponent : IComponentData
    {
        public DistrictType Type;
        public float3 CenterPosition;
        public float Radius;
        public int BuildingCount;
        public int MaxBuildings;
        public float DistrictEfficiency;
        public float HappinessLevel;
        public int Population;
        public int MaxPopulation;
        public float DevelopmentLevel;
        public uint SpecialBonuses; // Bitfield for active bonuses
    }

    #endregion

    #region Enums

    public enum BuildingType : byte
    {
        // Essential Facilities
        BreedingCenter,
        TrainingGround,
        ResearchLab,
        MonsterHabitat,
        EquipmentShop,
        SocialHub,

        // Activity Centers
        RacingTrack,
        CombatArena,
        PuzzleAcademy,
        StrategyCenter,
        MusicStudio,
        AdventureGuild,
        PlatformCourse,
        CraftingWorkshop,

        // Infrastructure
        PowerPlant,
        WaterTreatment,
        ResourceStorage,
        TransportHub,
        TownHall,
        Hospital,
        Market,
        Road,

        // Decorative
        Park,
        Monument,
        Garden,
        Fountain,

        // Special
        Portal,
        Observatory,
        Archive,
        Sanctuary
    }

    public enum BuildingTier : byte
    {
        Basic = 1,
        Advanced = 2,
        Expert = 3,
        Master = 4,
        Legendary = 5
    }

    public enum BuildingStatus : byte
    {
        Planning,
        UnderConstruction,
        Operational,
        Upgrading,
        Maintenance,
        Damaged,
        Abandoned
    }

    public enum ResourceType : byte
    {
        Food,
        Materials,
        Energy,
        Research,
        Currency,
        SpecialItems
    }

    public enum ServiceType : byte
    {
        None,
        Healthcare,
        Education,
        Entertainment,
        Security,
        Transport,
        Utilities,
        Research,
        Commerce
    }

    public enum DistrictType : byte
    {
        Residential,
        Commercial,
        Industrial,
        Recreational,
        Research,
        Administrative,
        Special
    }

    #endregion

    #region Town Building Systems

    /// <summary>
    /// Core building management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class TownBuildingSystem : SystemBase
    {
        private EntityQuery buildingQuery;
        private EntityQuery constructionQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            buildingQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<BuildingComponent>(),
                ComponentType.ReadOnly<LocalTransform>()
            });

            constructionQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<BuildingComponent>(),
                ComponentType.ReadOnly<BuildingProductionComponent>()
            });

            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update building construction and operation
            var buildingUpdateJob = new BuildingUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };

            Dependency = buildingUpdateJob.ScheduleParallel(buildingQuery, Dependency);
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }



    [BurstCompile]
    [BurstCompile]
    public partial struct BuildingUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
            ref BuildingComponent building,
            in LocalTransform transform)
        {
            switch (building.Status)
            {
                case BuildingStatus.UnderConstruction:
                    UpdateConstruction(ref building, DeltaTime);
                    break;

                case BuildingStatus.Operational:
                    UpdateOperation(ref building, DeltaTime);
                    break;

                case BuildingStatus.Upgrading:
                    UpdateUpgrade(ref building, DeltaTime);
                    break;

                case BuildingStatus.Maintenance:
                    UpdateMaintenance(ref building, DeltaTime);
                    break;
            }

            // Handle durability degradation
            if (building.Status == BuildingStatus.Operational)
            {
                float degradationRate = CalculateDegradationRate(building.Type, building.Tier);
                building.Durability = math.max(0f, building.Durability - degradationRate * DeltaTime);

                // Building needs maintenance
                if (building.Durability < 0.3f && building.Status != BuildingStatus.Maintenance)
                {
                    building.Status = BuildingStatus.Maintenance;
                }
            }
        }


        private void UpdateConstruction(ref BuildingComponent building, float deltaTime)
        {
            float constructionSpeed = CalculateConstructionSpeed(building.Type, building.Tier);
            building.ConstructionProgress += constructionSpeed * deltaTime;

            if (building.ConstructionProgress >= 1f)
            {
                building.Status = BuildingStatus.Operational;
                building.ConstructionProgress = 1f;
                building.Durability = 1f;
                building.Efficiency = CalculateBaseEfficiency(building.Type, building.Tier);
            }
        }


        private void UpdateOperation(ref BuildingComponent building, float deltaTime)
        {
            // Buildings operating efficiently
            building.Efficiency = math.clamp(building.Efficiency, 0.1f, 2.0f);

            // Efficiency affects all building functions
            float efficiencyDecay = 0.001f * deltaTime; // Very slow efficiency decay
            building.Efficiency = math.max(0.5f, building.Efficiency - efficiencyDecay);
        }


        private void UpdateUpgrade(ref BuildingComponent building, float deltaTime)
        {
            float upgradeSpeed = CalculateUpgradeSpeed(building.Type, building.UpgradeLevel);
            building.ConstructionProgress += upgradeSpeed * deltaTime;

            if (building.ConstructionProgress >= 1f)
            {
                building.UpgradeLevel++;
                building.Status = BuildingStatus.Operational;
                building.ConstructionProgress = 0f;
                building.Efficiency += 0.2f; // Efficiency boost from upgrade
            }
        }


        private void UpdateMaintenance(ref BuildingComponent building, float deltaTime)
        {
            float maintenanceSpeed = 0.5f; // Fixed maintenance rate
            building.ConstructionProgress += maintenanceSpeed * deltaTime;

            if (building.ConstructionProgress >= 1f)
            {
                building.Status = BuildingStatus.Operational;
                building.ConstructionProgress = 0f;
                building.Durability = 1f; // Full repair
            }
        }


        private float CalculateConstructionSpeed(BuildingType type, BuildingTier tier)
        {
            float baseSpeed = type switch
            {
                BuildingType.Road => 0.5f, // Fast construction
                BuildingType.Park => 0.3f,
                BuildingType.MonsterHabitat => 0.2f,
                BuildingType.BreedingCenter => 0.1f, // Slow, complex construction
                BuildingType.ResearchLab => 0.08f,
                BuildingType.PowerPlant => 0.05f, // Very slow infrastructure
                _ => 0.2f
            };

            float tierMultiplier = (int)tier switch
            {
                1 => 1.0f,
                2 => 0.8f,
                3 => 0.6f,
                4 => 0.4f,
                5 => 0.2f,
                _ => 1.0f
            };

            return baseSpeed * tierMultiplier;
        }


        private float CalculateUpgradeSpeed(BuildingType type, int level)
        {
            float baseUpgradeSpeed = 0.1f;
            float levelPenalty = level * 0.02f; // Harder to upgrade higher levels
            return math.max(0.01f, baseUpgradeSpeed - levelPenalty);
        }


        private float CalculateBaseEfficiency(BuildingType type, BuildingTier tier)
        {
            float baseEfficiency = (int)tier * 0.2f + 0.6f; // 0.8 to 1.6 range
            return math.clamp(baseEfficiency, 0.5f, 2.0f);
        }


        private float CalculateDegradationRate(BuildingType type, BuildingTier tier)
        {
            float baseDegradation = type switch
            {
                BuildingType.Road => 0.0001f, // Roads degrade slowly
                BuildingType.Monument => 0.00001f, // Monuments are very durable
                BuildingType.PowerPlant => 0.0003f, // Infrastructure needs maintenance
                BuildingType.EquipmentShop => 0.0002f, // Commercial buildings
                _ => 0.0001f
            };

            float tierBonus = (int)tier * 0.5f; // Higher tiers are more durable
            return baseDegradation / (1f + tierBonus);
        }
    }

    /// <summary>
    /// Resource production and management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TownBuildingSystem))]
    public partial class ResourceProductionSystem : SystemBase
    {
        private EntityQuery productionBuildingQuery;

        protected override void OnCreate()
        {
            productionBuildingQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadOnly<BuildingComponent>(),
                ComponentType.ReadWrite<BuildingProductionComponent>(),
                ComponentType.ReadWrite<ResourceStorageComponent>()
            });
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var productionJob = new ResourceProductionJob
            {
                DeltaTime = deltaTime
            };

            Dependency = productionJob.ScheduleParallel(productionBuildingQuery, Dependency);
        }
    }



    [BurstCompile]
    [BurstCompile]
    public partial struct ResourceProductionJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(in BuildingComponent building,
            ref BuildingProductionComponent production,
            ref ResourceStorageComponent storage)
        {
            if (building.Status != BuildingStatus.Operational)
                return;

            // Calculate production based on building efficiency
            float effectiveProductionRate = production.ProductionRate * building.Efficiency * DeltaTime;

            // Produce resources based on building type
            switch (building.Type)
            {
                case BuildingType.BreedingCenter:
                    // Produces research points
                    storage.Research = math.min(storage.MaxResearch,
                        storage.Research + (int)(effectiveProductionRate * 2f));
                    break;

                case BuildingType.EquipmentShop:
                    // Generates currency
                    storage.Currency = math.min(storage.MaxCurrency,
                        storage.Currency + (int)(effectiveProductionRate * 5f));
                    break;

                case BuildingType.PowerPlant:
                    // Produces energy
                    storage.Energy = math.min(storage.MaxEnergy,
                        storage.Energy + (int)(effectiveProductionRate * 10f));
                    break;

                case BuildingType.CraftingWorkshop:
                    // Produces materials and equipment
                    storage.Materials = math.min(storage.MaxMaterials,
                        storage.Materials + (int)(effectiveProductionRate * 3f));
                    break;

                case BuildingType.MonsterHabitat:
                    // Produces food and happiness
                    storage.Food = math.min(storage.MaxFood,
                        storage.Food + (int)(effectiveProductionRate * 4f));
                    break;
            }

            // Update service capacity based on building
            UpdateServiceCapacity(ref production, building);
        }


        private void UpdateServiceCapacity(ref BuildingProductionComponent production, BuildingComponent building)
        {
            production.ServiceCapacity = building.Type switch
            {
                BuildingType.Hospital => 50 + building.UpgradeLevel * 20,
                BuildingType.BreedingCenter => 10 + building.UpgradeLevel * 5,
                BuildingType.TrainingGround => 20 + building.UpgradeLevel * 10,
                BuildingType.ResearchLab => 5 + building.UpgradeLevel * 3,
                BuildingType.EquipmentShop => 100 + building.UpgradeLevel * 50,
                _ => 10
            };

            production.ServiceQuality = building.Efficiency * (1f + building.UpgradeLevel * 0.1f);
        }
    }

    /// <summary>
    /// District management and city planning system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DistrictManagementSystem : SystemBase
    {
        private EntityQuery districtQuery;

        protected override void OnCreate()
        {
            districtQuery = GetEntityQuery(ComponentType.ReadWrite<DistrictComponent>());
        }

        protected override void OnUpdate()
        {
            foreach (var district in SystemAPI.Query<RefRW<DistrictComponent>>())
            {
                UpdateDistrictMetrics(ref district.ValueRW);
            }
        }

        private void UpdateDistrictMetrics(ref DistrictComponent district)
        {
            // Calculate district efficiency based on building density and types
            float optimalDensity = 0.7f; // 70% capacity is optimal
            float currentDensity = (float)district.BuildingCount / district.MaxBuildings;

            if (currentDensity <= optimalDensity)
            {
                district.DistrictEfficiency = currentDensity / optimalDensity;
            }
            else
            {
                // Overcrowding penalty
                district.DistrictEfficiency = optimalDensity / currentDensity;
            }

            // Update happiness based on district type and efficiency
            district.HappinessLevel = district.Type switch
            {
                DistrictType.Recreational => district.DistrictEfficiency * 1.5f,
                DistrictType.Residential => district.DistrictEfficiency * 1.2f,
                DistrictType.Commercial => district.DistrictEfficiency * 1.0f,
                DistrictType.Industrial => district.DistrictEfficiency * 0.8f,
                _ => district.DistrictEfficiency
            };

            district.HappinessLevel = math.clamp(district.HappinessLevel, 0.1f, 2.0f);

            // Update development level
            float targetDevelopment = currentDensity * district.DistrictEfficiency;
            district.DevelopmentLevel = math.lerp(district.DevelopmentLevel, targetDevelopment, 0.01f);
        }
    }

    #endregion

    #region Building Authoring

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

    #endregion
}