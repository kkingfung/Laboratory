using Unity.Entities;

namespace Laboratory.AI.ECS
{
    /// <summary>
    /// Types of AI behavior for creatures
    /// </summary>
    public enum AIBehaviorType : byte
    {
        /// <summary>Idle behavior - standing around</summary>
        Idle = 0,
        /// <summary>Wandering randomly</summary>
        Wander = 1,
        /// <summary>Patrolling between points</summary>
        Patrol = 2,
        /// <summary>Following a target</summary>
        Follow = 3,
        /// <summary>Fleeing from a threat</summary>
        Flee = 4,
        /// <summary>Attacking a target</summary>
        Attack = 5,
        /// <summary>Defending a position or ally</summary>
        Defend = 6,
        /// <summary>Searching for something</summary>
        Search = 7,
        /// <summary>Investigating a disturbance</summary>
        Investigate = 8,
        /// <summary>Returning to home position</summary>
        ReturnHome = 9,
        /// <summary>Gathering resources</summary>
        Gather = 10,
        /// <summary>Socializing with others</summary>
        Social = 11,
        /// <summary>Resting or sleeping</summary>
        Rest = 12,
        /// <summary>Eating or feeding</summary>
        Feed = 13,
        /// <summary>Breeding behavior</summary>
        Breed = 14,
        /// <summary>Territorial behavior</summary>
        Territory = 15,
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