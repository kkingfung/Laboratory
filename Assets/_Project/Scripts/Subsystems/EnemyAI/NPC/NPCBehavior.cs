using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.EnemyAI.NPC
{
    /// <summary>
    /// Base NPC behavior component for managing NPC interactions and AI
    /// </summary>
    public class NPCBehavior : MonoBehaviour
    {
        #region Fields
        
        [Header("NPC Identity")]
        [SerializeField] private string npcID;
        [SerializeField] private string npcName = "NPC";
        [SerializeField] private NPCType npcType = NPCType.Merchant;
        
        [Header("Behavior Settings")]
        [SerializeField] private float interactionRadius = 2.0f;
        [SerializeField] private bool canInteract = true;
        [SerializeField] private bool requiresLineOfSight = true;
        
        [Header("Dialogue")]
        [SerializeField] private List<string> dialogueOptions = new List<string>();
        [SerializeField] private string defaultDialogue = "Hello there!";
        
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 1.0f;
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private bool isStationary = true;
        
        private int currentPatrolIndex = 0;
        private bool isPlayerInRange = false;
        private GameObject currentPlayer;
        
        #endregion
        
        #region Properties
        
        public string NPCID => npcID;
        public string NPCName => npcName;
        public NPCType Type => npcType;
        public float InteractionRadius => interactionRadius;
        public bool CanInteract => canInteract && isPlayerInRange;
        public bool IsPlayerInRange => isPlayerInRange;
        public float MovementSpeed => moveSpeed;
        public NPCState currentState { get; set; } = NPCState.Idle;
        public NPCQuest quest { get; set; }
        public float playerReputation { get; set; } = 0f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (string.IsNullOrEmpty(npcID))
            {
                npcID = System.Guid.NewGuid().ToString();
            }
        }
        
        private void Start()
        {
            InitializeNPC();
        }
        
        private void Update()
        {
            CheckForPlayerInteraction();
            
            if (!isStationary)
            {
                HandleMovement();
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw interaction radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
            
            // Draw patrol points
            if (patrolPoints != null && patrolPoints.Length > 1)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                        
                        // Draw lines between patrol points
                        if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                        }
                        else if (i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                        }
                    }
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Interact with this NPC
        /// </summary>
        public virtual bool Interact(GameObject player)
        {
            if (!CanInteract)
                return false;
                
            Debug.Log($"[{npcName}] Interacting with player");
            
            // Handle different types of interactions based on NPC type
            switch (npcType)
            {
                case NPCType.Merchant:
                    OpenShop();
                    break;
                case NPCType.QuestGiver:
                    ShowQuests();
                    break;
                case NPCType.Guard:
                    ShowDialogue();
                    break;
                default:
                    ShowDialogue();
                    break;
            }
            
            return true;
        }
        
        /// <summary>
        /// Set the NPC's dialogue options
        /// </summary>
        public void SetDialogue(List<string> newDialogue)
        {
            dialogueOptions = newDialogue ?? new List<string>();
        }
        
        /// <summary>
        /// Add a dialogue option
        /// </summary>
        public void AddDialogue(string dialogue)
        {
            if (!string.IsNullOrEmpty(dialogue))
            {
                dialogueOptions.Add(dialogue);
            }
        }
        
        /// <summary>
        /// Set patrol points for movement
        /// </summary>
        public void SetPatrolPoints(Transform[] points)
        {
            patrolPoints = points;
            currentPatrolIndex = 0;
            isStationary = points == null || points.Length <= 1;
        }
        
        /// <summary>
        /// Enable or disable interactions
        /// </summary>
        public void SetInteractionEnabled(bool enabled)
        {
            canInteract = enabled;
        }
        
        /// <summary>
        /// Get the next patrol point for movement
        /// </summary>
        public Transform GetNextPatrolPoint()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return null;
            return patrolPoints[currentPatrolIndex];
        }
        
        /// <summary>
        /// Try to attack a target
        /// </summary>
        public bool TryAttack(GameObject target)
        {
            Debug.Log($"[{npcName}] Attempting to attack {target.name}");
            // Basic attack logic - can be overridden
            return true;
        }
        
        /// <summary>
        /// Start a conversation with the player
        /// </summary>
        public void StartConversation(GameObject player)
        {
            Debug.Log($"[{npcName}] Starting conversation with {player.name}");
            ShowDialogue();
        }
        
        /// <summary>
        /// Handle being attacked
        /// </summary>
        public void OnAttacked(GameObject attacker)
        {
            Debug.Log($"[{npcName}] Was attacked by {attacker.name}");
            playerReputation -= 10f;
        }
        
        /// <summary>
        /// Handle being helped by player
        /// </summary>
        public void OnHelped(GameObject helper)
        {
            Debug.Log($"[{npcName}] Was helped by {helper.name}");
            playerReputation += 5f;
        }
        
        /// <summary>
        /// Complete the current quest
        /// </summary>
        public void CompleteQuest()
        {
            if (quest != null)
            {
                Debug.Log($"[{npcName}] Quest '{quest.questName}' completed!");
                quest = null;
                playerReputation += 20f;
            }
        }
        
        /// <summary>
        /// Check if this NPC can offer a quest
        /// </summary>
        public bool CanOfferQuest()
        {
            return npcType == NPCType.QuestGiver && quest == null && playerReputation >= 0f;
        }
        
        /// <summary>
        /// Increase player reputation with this NPC
        /// </summary>
        public void IncreaseReputation(float amount)
        {
            playerReputation += amount;
            Debug.Log($"[{npcName}] Reputation increased by {amount}. Total: {playerReputation}");
        }
        
        /// <summary>
        /// React to witnessed violence
        /// </summary>
        public void ReactToWitnessedViolence(GameObject perpetrator)
        {
            Debug.Log($"[{npcName}] Witnessed violence from {perpetrator.name}");
            playerReputation -= 5f;
            currentState = NPCState.Alarmed;
        }
        
        #endregion
        
        #region Protected Virtual Methods
        
        protected virtual void InitializeNPC()
        {
            // Override in derived classes for specific initialization
        }
        
        protected virtual void ShowDialogue()
        {
            string dialogue = dialogueOptions.Count > 0 ? 
                dialogueOptions[Random.Range(0, dialogueOptions.Count)] : 
                defaultDialogue;
                
            Debug.Log($"[{npcName}] {dialogue}");
        }
        
        protected virtual void OpenShop()
        {
            Debug.Log($"[{npcName}] Opening shop interface");
            // Implement shop opening logic
        }
        
        protected virtual void ShowQuests()
        {
            Debug.Log($"[{npcName}] Showing available quests");
            // Implement quest interface logic
        }
        
        #endregion
        
        #region Private Methods
        
        private void CheckForPlayerInteraction()
        {
            // Find player in interaction radius
            var playersInRange = Physics.OverlapSphere(transform.position, interactionRadius);
            GameObject nearestPlayer = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var collider in playersInRange)
            {
                if (collider.CompareTag("Player"))
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < nearestDistance)
                    {
                        if (!requiresLineOfSight || HasLineOfSight(collider.transform))
                        {
                            nearestDistance = distance;
                            nearestPlayer = collider.gameObject;
                        }
                    }
                }
            }
            
            // Update player in range status
            bool wasPlayerInRange = isPlayerInRange;
            isPlayerInRange = nearestPlayer != null;
            currentPlayer = nearestPlayer;
            
            // Trigger events on range change
            if (isPlayerInRange && !wasPlayerInRange)
            {
                OnPlayerEnterRange(currentPlayer);
            }
            else if (!isPlayerInRange && wasPlayerInRange)
            {
                OnPlayerExitRange();
            }
        }
        
        private bool HasLineOfSight(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            Ray ray = new Ray(transform.position + Vector3.up * 0.5f, direction.normalized);
            
            if (Physics.Raycast(ray, out RaycastHit hit, direction.magnitude))
            {
                return hit.transform == target;
            }
            
            return true;
        }
        
        private void HandleMovement()
        {
            if (patrolPoints == null || patrolPoints.Length <= 1)
                return;
                
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            if (targetPoint == null)
            {
                MoveToNextPatrolPoint();
                return;
            }
            
            // Move towards current patrol point
            Vector3 direction = (targetPoint.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Check if reached patrol point
            if (Vector3.Distance(transform.position, targetPoint.position) < 0.5f)
            {
                MoveToNextPatrolPoint();
            }
        }
        
        private void MoveToNextPatrolPoint()
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }
        
        private void OnPlayerEnterRange(GameObject player)
        {
            Debug.Log($"[{npcName}] Player entered interaction range");
        }
        
        private void OnPlayerExitRange()
        {
            Debug.Log($"[{npcName}] Player left interaction range");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Types of NPCs in the game
    /// </summary>
    public enum NPCType
    {
        Merchant,
        QuestGiver,
        Guard,
        Civilian,
        Trainer,
        Vendor
    }
    
    /// <summary>
    /// States that NPCs can be in
    /// </summary>
    public enum NPCState
    {
        Idle,
        Patrolling,
        Talking,
        Alarmed,
        Hostile,
        Fleeing,
        Dead
    }
    
    /// <summary>
    /// Basic quest data for NPCs
    /// </summary>
    [System.Serializable]
    public class NPCQuest
    {
        public string questName;
        public string questDescription;
        public bool isCompleted;
        public float rewardReputation;
        
        public NPCQuest(string name, string description)
        {
            questName = name;
            questDescription = description;
            isCompleted = false;
            rewardReputation = 10f;
        }
    }
}
