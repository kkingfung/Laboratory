using Unity.Entities;

namespace Laboratory.Core.ECS
{
    /// <summary>
    /// Core ECS component definitions for Project Chimera
    /// These components are placed in the Laboratory.Core assembly
    /// to be accessible by both Core (for debugging) and Chimera (for creature system)
    /// without creating circular dependencies.
    ///
    /// Note: Many component definitions have been moved to Laboratory.Chimera.ECS
    /// to avoid namespace conflicts and provide richer functionality.
    /// </summary>

    // Core creature data - basic identification
    public struct CreatureData : IComponentData
    {
        public int speciesID;
        public int generation;
        public int age;
        public uint geneticSeed;
        public bool isAlive;
    }

    // Basic creature statistics - simple version for core systems
    public struct CreatureStats : IComponentData
    {
        public float health;
        public float maxHealth;
        public float attack;
        public float defense;
        public float speed;
        public float intelligence;
        public float charisma;
    }

    // Genetic traits buffer - for core genetic calculations
    public struct CreatureGeneticTrait : IBufferElementData
    {
        public int traitName; // Hashed string for performance
        public float value;
        public float dominance;
    }

    // Visual appearance data - for rendering systems
    public struct CreatureVisualData : IComponentData
    {
        public float baseScale;
        public uint colorSeed;
        public int speciesVisualID;
    }

    // Simulation tag for filtering
    public struct CreatureSimulationTag : IComponentData { }

    // Core lifecycle component - simplified version
    public struct CreatureLifecycleComponent : IComponentData
    {
        public int currentStage; // Using int to avoid enum conflicts
        public float stageProgress;
    }

    // GameObject linking - for bridging ECS and GameObject systems
    public struct GameObjectLinkComponent : IComponentData
    {
        public int instanceID;
    }

    // Tags
    public struct DeadTag : IComponentData { }

    // AI behavior types - core subset for basic systems
    // Note: Full AIBehaviorType enum is defined in Laboratory.Chimera.AI.AIBehaviorType
    // This is a minimal subset for ECS components to avoid circular dependencies
    public enum AIBehaviorType : byte
    {
        None = 0,
        Companion = 21,
        Aggressive = 6,
        Defensive = 26,
        Passive = 23,
        Guard = 20,
        Wild = 28,
        Predator = 25,
        Herbivore = 27
    }

    // AI states - core subset for basic systems
    // Note: Full AIBehaviorState enum is defined in Laboratory.Chimera.AI.AIBehaviorState
    // This is a minimal subset for ECS components to avoid circular dependencies
    public enum AIState : byte
    {
        Idle = 0,
        Moving = 1,
        Following = 3,
        Patrolling = 2,
        Pursuing = 4,
        Combat = 5,
        Feeding = 7,
        Resting = 8
    }

    // Biome types enum - core version for basic environmental systems
    public enum BiomeType : byte
    {
        Forest,
        Desert,
        Tundra,
        Ocean,
        Mountain,
        Grassland,
        Swamp,
        Cave
    }
}