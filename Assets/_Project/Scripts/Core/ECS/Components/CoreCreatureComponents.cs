using Unity.Entities;
using Unity.Mathematics;
using Laboratory.Core.Activities;

namespace Laboratory.Core.ECS.Components
{
    /// <summary>
    /// Core creature component definitions for activity and progression systems
    /// This bridges the Core ECS components with Activity, Equipment, and Progression systems
    /// </summary>

    #region Core Genetic Data

    /// <summary>
    /// Genetic data component - extended from CreatureECSComponents for richer functionality
    /// </summary>
    public struct GeneticDataComponent : IComponentData
    {
        // Primary stats influenced by genetics
        public float Strength;
        public float Agility;
        public float Intelligence;
        public float Vitality;
        public float Speed;
        public float Stamina;

        // Behavioral traits
        public float Aggression;
        public float Curiosity;
        public float Sociability;
        public float Caution;
        public float Adaptability;
        public float Dominance;

        // Size and physical attributes
        public float Size;
        public float Weight;
        public float Height;

        // Genetic metadata
        public uint GeneticSeed;
        public int Generation;
        public float GeneticStability;
        public bool HasMutations;
    }

    /// <summary>
    /// Creature identity and basic information
    /// </summary>
    public struct CreatureIdentityComponent : IComponentData
    {
        public int SpeciesID;
        public int CreatureID;
        public uint NameHash; // Hashed name for performance
        public int Age;
        public int Generation;
        public bool IsAlive;
        public bool IsTamed;
        public bool IsWild;
        public Entity OwnerPlayer;
    }

    /// <summary>
    /// Creature movement and physics
    /// </summary>
    public struct CreatureMovementComponent : IComponentData
    {
        public float3 Position;
        public float3 Velocity;
        public float3 Acceleration;
        public quaternion Rotation;
        public float MovementSpeed;
        public float RotationSpeed;
        public float MaxSpeed;
        public bool IsMoving;
        public bool CanFly;
        public bool CanSwim;
        public bool CanClimb;
    }

    #endregion

    #region GameObject Bridge Components

    /// <summary>
    /// Enhanced GameObject linking component
    /// </summary>
    public struct GameObjectLinkComponent : IComponentData
    {
        public int InstanceID;
        public bool IsActive;
        public bool HasVisuals;
        public bool NeedsSync;
    }

    #endregion

    #region Monster Data Structures

    /// <summary>
    /// Monster data structure for discovery system compatibility
    /// </summary>
    public struct Monster
    {
        public string Name;
        public int SpeciesID;
        public MonsterStats Stats;
        public GeneticProfile Genetics;
        public bool IsAlive;
        public int Age;
        public int Generation;
    }

    /// <summary>
    /// Monster stats structure
    /// </summary>
    public struct MonsterStats
    {
        public float strength;
        public float agility;
        public float vitality;
        public float intelligence;
        public float speed;
        public float social;
    }

    /// <summary>
    /// Genetic profile for monster
    /// </summary>
    public struct GeneticProfile
    {
        public uint seed;
        public float dominance;
        public float recessiveness;
        public float mutationRate;
        public bool hasMutations;
    }

    #endregion

    #region Town Building Integration

    /// <summary>
    /// Town resources structure for systems integration
    /// </summary>
    public struct TownResources
    {
        public int Currency;
        public int Materials;
        public int Energy;
        public int Research;
        public int Food;
        public int Population;
    }

    #endregion

    #region System Tags

    /// <summary>
    /// Tags for system filtering and organization
    /// </summary>
    public struct ActivitySystemTag : IComponentData { }
    public struct EquipmentSystemTag : IComponentData { }
    public struct ProgressionSystemTag : IComponentData { }
    public struct DiscoverySystemTag : IComponentData { }
    public struct TownBuildingSystemTag : IComponentData { }

    #endregion
}