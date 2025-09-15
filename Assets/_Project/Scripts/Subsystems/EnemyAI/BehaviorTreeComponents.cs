using System;
using UnityEngine;

namespace Laboratory.Subsystems.EnemyAI
{
    /// <summary>
    /// Enumeration for NPC difficulty levels
    /// </summary>
    public enum NPCDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert,
        Nightmare
    }
    
    /// <summary>
    /// Base class for behavior tree nodes
    /// </summary>
    public abstract class ActionNode
    {
        public abstract BehaviorTreeStatus Execute();
    }
    
    /// <summary>
    /// Enhanced NPC Behavior class
    /// </summary>
    public class EnhancedNPCBehavior : MonoBehaviour
    {
        [SerializeField] private NPCDifficulty difficulty = NPCDifficulty.Normal;
        
        public NPCDifficulty Difficulty => difficulty;
        
        public virtual void SetDifficulty(NPCDifficulty newDifficulty)
        {
            difficulty = newDifficulty;
        }
    }
    
    /// <summary>
    /// Status enum for behavior tree execution
    /// </summary>
    public enum BehaviorTreeStatus
    {
        Success,
        Failure,
        Running
    }
}
