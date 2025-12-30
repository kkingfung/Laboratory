using System;
using Unity.Entities;

namespace Laboratory.Core.ECS
{
    /// <summary>
    /// Core ECS component definitions for Project Chimera
    /// These components are placed in the Laboratory.Core assembly
    /// to be accessible by both Core (for debugging) and Chimera (for creature system)
    /// without creating circular dependencies.
    ///
    /// MIGRATION NOTE: Many component definitions have been moved to Laboratory.Chimera.ECS
    /// Use the Chimera versions for new code. This file contains simplified versions
    /// for backward compatibility with core debugging systems.
    /// </summary>

    // Core creature data - basic identification (unique to Core)
    public struct CreatureData : IComponentData
    {
        public int speciesID;
        public int generation;
        public int age;
        public uint geneticSeed;
        public bool isAlive;
    }

    // Basic creature statistics - simple version for core systems (unique to Core)
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

    // Genetic traits buffer - for core genetic calculations (unique to Core)
    public struct CreatureGeneticTrait : IBufferElementData
    {
        public int traitName; // Hashed string for performance
        public float value;
        public float dominance;
    }

    // Visual appearance data - for rendering systems (unique to Core)
    public struct CreatureVisualData : IComponentData
    {
        public float baseScale;
        public uint colorSeed;
        public int speciesVisualID;
    }

    // Simulation tag for filtering (unique to Core)
    public struct CreatureSimulationTag : IComponentData { }

    /// <summary>
    /// DEPRECATED: Use Laboratory.Chimera.ECS.CreatureLifecycleComponent instead.
    /// This simplified version is kept for backward compatibility only.
    /// </summary>
    [Obsolete("Use Laboratory.Chimera.ECS.CreatureLifecycleComponent for new code. This simplified version may be removed in future versions.")]
    public struct CreatureLifecycleComponent : IComponentData
    {
        public int currentStage; // Using int to avoid enum conflicts
        public float stageProgress;
    }

    /// <summary>
    /// DEPRECATED: Use Laboratory.Chimera.ECS.GameObjectLinkComponent instead.
    /// This simplified version is kept for backward compatibility only.
    /// </summary>
    [Obsolete("Use Laboratory.Chimera.ECS.GameObjectLinkComponent for new code. This simplified version may be removed in future versions.")]
    public struct GameObjectLinkComponent : IComponentData
    {
        public int instanceID;
    }

    /// <summary>
    /// DEPRECATED: Use Laboratory.Chimera.ECS.DeadTag instead.
    /// </summary>
    [Obsolete("Use Laboratory.Chimera.ECS.DeadTag for new code.")]
    public struct DeadTag : IComponentData { }

    /// <summary>
    /// DEPRECATED: Use Laboratory.Chimera.AI.AIBehaviorType instead.
    /// This is a minimal subset kept for backward compatibility.
    /// </summary>
    [Obsolete("Use Laboratory.Chimera.AI.AIBehaviorType for new code. This enum may be removed in future versions.")]
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

    /// <summary>
    /// DEPRECATED: Use Laboratory.Chimera.ECS.AIState instead.
    /// WARNING: This enum has different values than the canonical version!
    /// </summary>
    [Obsolete("Use Laboratory.Chimera.ECS.AIState for new code. WARNING: Enum values differ between versions!")]
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
}
