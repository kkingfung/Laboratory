using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace Laboratory.Core.NPC
{
    // Manager class to handle all NPCs and their collective behaviors
    public class NPCManager : MonoBehaviour
    {
        [Header("NPC Management")]
        public List<NPCBehavior> allNPCs = new List<NPCBehavior>();
        public float witnessRange = 8f; // Range for NPCs to witness events
        
        [Header("Group Behaviors")]
        public bool enableGroupReactions = true;
        public float groupFleeChance = 0.6f; // Chance for witnesses to flee
        public float groupPanicChance = 0.3f; // Chance for witnesses to panic
        
        [Header("UI Elements")]
        public Text interactionPrompt; // UI text for interaction hints
        public GameObject questNotificationPrefab;
        public Transform notificationParent;
        
        private static NPCManager instance;
        public static NPCManager Instance
        {
            get
            {
                if (instance == null)
                    instance = FindFirstObjectByType<NPCManager>();
                return instance;
            }
        }
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            // Find all NPCs in scene
            RefreshNPCList();
        }
        
        void Start()
        {
            if (interactionPrompt != null)
                interactionPrompt.gameObject.SetActive(false);
        }
        
        public void RefreshNPCList()
        {
            allNPCs.Clear();
            allNPCs.AddRange(FindObjectsByType<NPCBehavior>(FindObjectsSortMode.None));
            Debug.Log($"Found {allNPCs.Count} NPCs in scene");
        }
        
        public void RegisterNPC(NPCBehavior npc)
        {
            if (!allNPCs.Contains(npc))
            {
                allNPCs.Add(npc);
            }
        }
        
        // Called when an NPC is attacked
        public void OnNPCAttacked(NPCBehavior attackedNPC, Transform attacker)
        {
            if (!enableGroupReactions) return;
            
            // Find witnesses
            List<NPCBehavior> witnesses = GetWitnessesNear(attackedNPC.transform.position);
            
            foreach (var witness in witnesses)
            {
                if (witness == attackedNPC) continue; // Skip the victim
                if (witness.currentState == NPCState.Hostile) continue; // Skip already hostile NPCs
                
                // Determine witness reaction
                float reactionRoll = Random.Range(0f, 1f);
                
                if (reactionRoll < groupFleeChance)
                {
                    witness.OnAttacked(attacker); // Make witness flee
                }
                else if (reactionRoll < groupFleeChance + groupPanicChance)
                {
                    // Make witness panic without being directly attacked
                    witness.ReactToWitnessedViolence();
                }
            }
            
            ShowNotification($"{attackedNPC.npcName} was attacked!", Color.red);
        }
        
        // Called when an NPC is helped
        public void OnNPCHelped(NPCBehavior helpedNPC, Transform helper)
        {
            ShowNotification($"You helped {helpedNPC.npcName}!", Color.green);
            
            // Positive group reaction - nearby NPCs become more friendly
            List<NPCBehavior> witnesses = GetWitnessesNear(helpedNPC.transform.position);
            
            foreach (var witness in witnesses)
            {
                if (witness == helpedNPC) continue;
                witness.IncreaseReputation();
            }
        }
        
        // Called when a quest is completed
        public void OnQuestCompleted(NPCBehavior questGiver, NPCQuest quest)
        {
            ShowNotification($"Quest completed: {quest.questName}!", Color.yellow);
            
            // Give reputation boost with nearby NPCs
            List<NPCBehavior> nearbyNPCs = GetWitnessesNear(questGiver.transform.position);
            
            foreach (var npc in nearbyNPCs)
            {
                npc.IncreaseReputation();
            }
        }
        
        List<NPCBehavior> GetWitnessesNear(Vector3 position)
        {
            List<NPCBehavior> witnesses = new List<NPCBehavior>();
            
            foreach (var npc in allNPCs)
            {
                if (npc == null) continue;
                
                float distance = Vector3.Distance(npc.transform.position, position);
                if (distance <= witnessRange)
                {
                    witnesses.Add(npc);
                }
            }
            
            return witnesses;
        }
        
        public void ShowInteractionPrompt(string message, bool show)
        {
            if (interactionPrompt == null) return;
            
            interactionPrompt.gameObject.SetActive(show);
            if (show)
            {
                interactionPrompt.text = message;
            }
        }
        
        void ShowNotification(string message, Color color)
        {
            if (questNotificationPrefab == null || notificationParent == null) 
            {
                Debug.Log($"[NPC Manager] {message}");
                return;
            }
            
            GameObject notification = Instantiate(questNotificationPrefab, notificationParent);
            Text notificationText = notification.GetComponentInChildren<Text>();
            
            if (notificationText != null)
            {
                notificationText.text = message;
                notificationText.color = color;
            }
            
            // Auto-destroy after 3 seconds
            Destroy(notification, 3f);
        }
        
        // Get NPC reputation/relationship status
        public int GetOverallReputation()
        {
            int totalReputation = 0;
            int npcCount = 0;
            
            foreach (var npc in allNPCs)
            {
                if (npc != null)
                {
                    totalReputation += npc.playerReputation;
                    npcCount++;
                }
            }
            
            return npcCount > 0 ? totalReputation / npcCount : 0;
        }
        
        // Get list of available quests
        public List<NPCQuest> GetAvailableQuests()
        {
            List<NPCQuest> availableQuests = new List<NPCQuest>();
            
            foreach (var npc in allNPCs)
            {
                if (npc != null && npc.quest != null && !npc.quest.isCompleted && npc.CanOfferQuest())
                {
                    availableQuests.Add(npc.quest);
                }
            }
            
            return availableQuests;
        }
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            foreach (var npc in allNPCs)
            {
                if (npc != null)
                {
                    DrawWireCircle(npc.transform.position, witnessRange);
                }
            }
        }
        
        void DrawWireCircle(Vector3 center, float radius)
        {
            int segments = 32;
            float angleStep = 360f / segments;
            
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}
