using Unity.Entities;

namespace Laboratory.AI.ECS
{
    /// <summary>
    /// Types of AI behavior for creatures
    /// </summary>
    public enum AIBehaviorType : byte
    {
        /// <summary>No behavior set</summary>
        None = 0,
        /// <summary>Idle behavior - standing around</summary>
        Idle = 1,
        /// <summary>Wandering randomly</summary>
        Wander = 2,
        /// <summary>Patrolling between points</summary>
        Patrol = 3,
        /// <summary>Following a target</summary>
        Follow = 4,
        /// <summary>Fleeing from a threat</summary>
        Flee = 5,
        /// <summary>Attacking a target</summary>
        Attack = 6,
        /// <summary>Defending a position or ally</summary>
        Defend = 7,
        /// <summary>Searching for something</summary>
        Search = 8,
        /// <summary>Investigating a disturbance</summary>
        Investigate = 9,
        /// <summary>Returning to home position</summary>
        ReturnHome = 10,
        /// <summary>Gathering resources</summary>
        Gather = 11,
        /// <summary>Socializing with others</summary>
        Social = 12,
        /// <summary>Resting or sleeping</summary>
        Rest = 13,
        /// <summary>Eating or feeding</summary>
        Feed = 14,
        /// <summary>Breeding behavior</summary>
        Breed = 15,
        /// <summary>Territorial behavior</summary>
        Territory = 16,
        /// <summary>Custom scripted behavior</summary>
        Custom = 255
    }

    /// <summary>
    /// ECS component for AI behavior type
    /// </summary>
    public struct AIBehaviorComponent : IComponentData
    {
        public AIBehaviorType CurrentBehavior;
        public AIBehaviorType PreviousBehavior;
        public float BehaviorTimer;
        public float BehaviorDuration;
        public bool IsBehaviorComplete;
    }
}