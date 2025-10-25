using Unity.Entities;

namespace Laboratory.Shared.Types
{
    /// <summary>
    /// Shared AI behavior types used across all assemblies
    /// </summary>
    public enum AIBehaviorType : byte
    {
        None = 0,
        Idle = 1,
        Wander = 2,
        Follow = 3,
        Flee = 4,
        Attack = 5,
        Guard = 6,
        Patrol = 7,
        Search = 8,
        Hunt = 9,
        Forage = 10,
        Rest = 11,
        Social = 12,
        Mate = 13,
        Territorial = 14,
        Custom = 255
    }

    /// <summary>
    /// AI state tracking component for ECS
    /// </summary>
    public struct AIStateComponent : IComponentData
    {
        public AIBehaviorType currentBehavior;
        public AIBehaviorType previousBehavior;
        public AIBehaviorType queuedBehavior;
        public float behaviorIntensity;
        public float behaviorStartTime;
        public float behaviorDuration;
        public bool isTransitioning;
    }
}