using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Laboratory.Core.TownBuilding.Types;

namespace Laboratory.Core.TownBuilding.Components
{
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
}