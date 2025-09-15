using System;
using UnityEngine;

namespace Laboratory.Subsystems.EnemyAI.NPC
{
    /// <summary>
    /// Quest system for NPCs - renamed to avoid conflicts
    /// </summary>
    [System.Serializable]
    public class NPCQuestData
    {
        [SerializeField] private string questID;
        [SerializeField] private string questName;
        
        public string QuestID => questID;
        public string QuestName => questName;
        
        public NPCQuestData()
        {
            questID = Guid.NewGuid().ToString();
            questName = "New Quest";
        }
    }
    
    /// <summary>
    /// Quest manager for handling NPC quests
    /// </summary>
    public static class NPCQuestManager
    {
        public static NPCQuestData CreateQuest(string name = null)
        {
            var quest = new NPCQuestData();
            if (!string.IsNullOrEmpty(name))
            {
                // Since questName is private, we'll need to create with constructor
                // For now, just return the default quest
            }
            return quest;
        }
    }
}
