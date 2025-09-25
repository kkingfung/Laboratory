namespace Laboratory.Chimera.AI
{
    /// <summary>
    /// Defines the different behavioral states for Chimera creature AI
    /// </summary>
    public enum AIBehaviorState
    {
        /// <summary>Standing still, minimal activity</summary>
        Idle = 0,
        
        /// <summary>Moving around in a defined area</summary>
        Patrol = 1,
        
        /// <summary>Searching for food, mates, or items</summary>
        Explore = 2,
        
        /// <summary>Following a specific target</summary>
        Follow = 3,
        
        /// <summary>Actively hunting or pursuing a target</summary>
        Hunt = 4,
        
        /// <summary>Engaged in combat</summary>
        Combat = 5,
        
        /// <summary>Fleeing from danger</summary>
        Flee = 6,
        
        /// <summary>Eating or consuming resources</summary>
        Feed = 7,
        
        /// <summary>Resting or sleeping</summary>
        Rest = 8,
        
        /// <summary>Engaged in social behavior with other creatures</summary>
        Social = 9,
        
        /// <summary>Looking for a mate</summary>
        Mate = 10,
        
        /// <summary>Caring for offspring</summary>
        Nurture = 11,
        
        /// <summary>Defending territory or resources</summary>
        Defend = 12,
        
        /// <summary>Learning new behaviors or skills</summary>
        Learn = 13,
        
        /// <summary>Playing or engaging in non-survival activities</summary>
        Play = 14,
        
        /// <summary>Stunned or temporarily incapacitated</summary>
        Stunned = 15,
        
        /// <summary>Dead or dying</summary>
        Dead = 16,

        /// <summary>Chasing a target aggressively</summary>
        Chase = 17,

        /// <summary>Performing an attack action</summary>
        Attack = 18,

        /// <summary>Searching for targets or items in an area</summary>
        Search = 19,

        /// <summary>Returning to home position or patrol route</summary>
        Return = 20,

        /// <summary>Guarding a specific location or target</summary>
        Guard = 21
    }


    /// <summary>
    /// Helper methods for AI behavior state management
    /// </summary>
    public static class AIBehaviorStateExtensions
    {
        /// <summary>
        /// Checks if this state allows movement
        /// </summary>
        public static bool AllowsMovement(this AIBehaviorState state)
        {
            return state switch
            {
                AIBehaviorState.Patrol or
                AIBehaviorState.Explore or
                AIBehaviorState.Follow or
                AIBehaviorState.Hunt or
                AIBehaviorState.Combat or
                AIBehaviorState.Flee or
                AIBehaviorState.Feed or
                AIBehaviorState.Social or
                AIBehaviorState.Mate or
                AIBehaviorState.Defend or
                AIBehaviorState.Play or
                AIBehaviorState.Chase or
                AIBehaviorState.Attack or
                AIBehaviorState.Search or
                AIBehaviorState.Return or
                AIBehaviorState.Guard => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Checks if this state is considered aggressive
        /// </summary>
        public static bool IsAggressive(this AIBehaviorState state)
        {
            return state switch
            {
                AIBehaviorState.Hunt or
                AIBehaviorState.Combat or
                AIBehaviorState.Defend or
                AIBehaviorState.Chase or
                AIBehaviorState.Attack or
                AIBehaviorState.Guard => true,
                _ => false
            };
        }
        
        /// <summary>
        /// Checks if this state is vulnerable to interruption
        /// </summary>
        public static bool IsInterruptible(this AIBehaviorState state)
        {
            return state switch
            {
                AIBehaviorState.Stunned or 
                AIBehaviorState.Dead => false,
                _ => true
            };
        }
        
        /// <summary>
        /// Gets the priority level of this behavior state
        /// Higher values mean higher priority
        /// </summary>
        public static int GetPriority(this AIBehaviorState state)
        {
            return state switch
            {
                AIBehaviorState.Dead => 100,
                AIBehaviorState.Stunned => 95,
                AIBehaviorState.Flee => 90,
                AIBehaviorState.Combat => 85,
                AIBehaviorState.Attack => 84,
                AIBehaviorState.Hunt => 80,
                AIBehaviorState.Chase => 78,
                AIBehaviorState.Defend => 75,
                AIBehaviorState.Guard => 73,
                AIBehaviorState.Feed => 70,
                AIBehaviorState.Mate => 65,
                AIBehaviorState.Nurture => 60,
                AIBehaviorState.Search => 55,
                AIBehaviorState.Social => 50,
                AIBehaviorState.Follow => 45,
                AIBehaviorState.Explore => 40,
                AIBehaviorState.Learn => 35,
                AIBehaviorState.Play => 30,
                AIBehaviorState.Return => 28,
                AIBehaviorState.Patrol => 25,
                AIBehaviorState.Rest => 20,
                AIBehaviorState.Idle => 10,
                _ => 0
            };
        }
        
        /// <summary>
        /// Checks if this state can transition to another state
        /// </summary>
        public static bool CanTransitionTo(this AIBehaviorState fromState, AIBehaviorState toState)
        {
            // Dead creatures can't transition to anything
            if (fromState == AIBehaviorState.Dead) return false;
            
            // Can always transition to higher priority states
            if (toState.GetPriority() > fromState.GetPriority()) return true;
            
            // Special rules for certain states
            return fromState switch
            {
                AIBehaviorState.Stunned => toState == AIBehaviorState.Idle || toState == AIBehaviorState.Flee,
                AIBehaviorState.Combat => toState == AIBehaviorState.Flee || toState == AIBehaviorState.Dead,
                _ => true
            };
        }
    }
}
