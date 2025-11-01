namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Unified AI behavior type enumeration for ECS and MonoBehaviour systems
    /// Maps to existing AIBehaviorState for compatibility
    /// </summary>
    public enum AIBehaviorType : byte
    {
        None = 0,
        Idle = 1,
        Patrol = 2,
        Explore = 3,
        Follow = 4,
        Hunt = 5,
        Aggressive = 6,
        Combat = 7,
        Fleeing = 8,
        Feed = 9,
        Rest = 10,
        Social = 11,
        Mate = 12,
        Nurture = 13,
        Defend = 14,
        Learn = 15,
        Play = 16,
        Wander = 17,
        Alert = 18,
        Investigate = 19,
        Guardian = 20,
        Guard = 20, // Alias for Guardian
        Companion = 21,
        Neutral = 22,
        Passive = 23,
        Territorial = 24,
        Predator = 25,
        Defensive = 26,
        Herbivore = 27,
        Wild = 28,
        Foraging = 29,
        Flee = 30,
        Custom = 255
    }

    /// <summary>
    /// AI state flags for bitwise operations
    /// </summary>
    [System.Flags]
    public enum AIStateFlags : uint
    {
        None = 0,
        IsMoving = 1 << 0,
        HasTarget = 1 << 1,
        InCombat = 1 << 2,
        IsBlocked = 1 << 3,
        NeedsRecalculation = 1 << 4,
        IsInterrupted = 1 << 5,
        IsPriorityOverride = 1 << 6,
        IsEmergencyState = 1 << 7,
        IsLearning = 1 << 8,
        IsSocial = 1 << 9,
        IsBreeding = 1 << 10,
        IsHungry = 1 << 11,
        IsTired = 1 << 12,
        IsInjured = 1 << 13,
        IsTerritory = 1 << 14,
        IsGrouped = 1 << 15
    }

    /// <summary>
    /// AI transition types
    /// </summary>
    public enum AITransitionType : byte
    {
        Immediate = 0,
        Smooth = 1,
        Interrupt = 2,
        Queue = 3,
        Blend = 4,
        Emergency = 5
    }
}