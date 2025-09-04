using UnityEngine;
using System.Collections;

namespace Laboratory.Core.NPC
{
    public enum NPCState
    {
        Idle,
        Talking,
        Fleeing,
        Panicking,
        Thankful,
        Hostile
    }

    public enum NPCPersonality
    {
        Coward,    // Always runs away
        Fighter,   // Sometimes fights back
        Neutral,   // Mixed reactions
        Brave      // Rarely runs, might help others
    }

    [System.Serializable]
    public class NPCQuest
    {
        public string questName;
        public string description;
        public bool isCompleted;
        public string thankYouMessage;
        public GameObject rewardItem; // Optional reward prefab
    }

    public class NPCBehavior : MonoBehaviour
    {
        [Header("NPC Settings")]
        public string npcName = "NPC";
        public NPCPersonality personality = NPCPersonality.Neutral;
        public float detectionRange = 5f;
        public float fleeSpeed = 6f;
        public float walkSpeed = 3f;
        
        [Header("Audio")]
        public AudioClip[] screamSounds;
        public AudioClip[] thankfulSounds;
        public AudioClip[] greetingSounds;
        
        [Header("Quest")]
        public NPCQuest quest;
        
        [Header("Reactions")]
        public float panicDuration = 3f;
        public float fleeDuration = 5f;
        public Color hostileColor = Color.red;
        public Color thankfulColor = Color.green;
        public Color normalColor = Color.white;
        
        [Header("Reputation System")]
        public int playerReputation = 0; // -10 to +10 scale
        public int minReputationForQuest = -5;
        
        // Private variables
        [HideInInspector] public NPCState currentState = NPCState.Idle;
        private Transform player;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private AudioSource audioSource;
        private Animator animator;
        private Vector2 originalPosition;
        private float stateTimer;
        private bool hasBeenAttacked = false;
        private bool hasBeenHelped = false;
        
        // Animation hash IDs for performance
        private int walkingHash;
        private int fleeingHash;
        private int panicHash;
        private int idleHash;
        
        void Start()
        {
            // Get components
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            audioSource = GetComponent<AudioSource>();
            animator = GetComponent<Animator>();
            
            // Store original position
            originalPosition = transform.position;
            
            // Find player
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            
            // Cache animation hashes
            if (animator != null)
            {
                walkingHash = Animator.StringToHash("Walking");
                fleeingHash = Animator.StringToHash("Fleeing");
                panicHash = Animator.StringToHash("Panicking");
                idleHash = Animator.StringToHash("Idle");
            }
            
            SetState(NPCState.Idle);
            
            // Register with NPC Manager
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.RegisterNPC(this);
            }
        }
        
        void Update()
        {
            if (player == null) return;
            
            float distanceToPlayer = Vector2.Distance(transform.position, player.position);
            
            // Handle state timer
            if (stateTimer > 0)
            {
                stateTimer -= Time.deltaTime;
                if (stateTimer <= 0)
                {
                    OnStateTimerExpired();
                }
            }
            
            // State-specific behavior
            switch (currentState)
            {
                case NPCState.Idle:
                    HandleIdleState(distanceToPlayer);
                    break;
                case NPCState.Fleeing:
                    HandleFleeingState();
                    break;
                case NPCState.Panicking:
                    HandlePanicState();
                    break;
                case NPCState.Talking:
                    HandleTalkingState();
                    break;
            }
        }
        
        void HandleIdleState(float distanceToPlayer)
        {
            // Return to original position if too far away
            if (Vector2.Distance(transform.position, originalPosition) > 1f)
            {
                MoveTowards(originalPosition, walkSpeed);
                SetAnimation("Walking");
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                SetAnimation("Idle");
            }
            
            // Check for interaction range
            if (distanceToPlayer <= detectionRange && !hasBeenAttacked)
            {
                // Player is nearby, face them
                FaceTarget(player.position);
            }
        }
        
        void HandleFleeingState()
        {
            // Run away from player
            if (player != null)
            {
                Vector2 fleeDirection = (transform.position - player.position).normalized;
                rb.linearVelocity = fleeDirection * fleeSpeed;
                FaceDirection(fleeDirection);
                SetAnimation("Fleeing");
            }
        }
        
        void HandlePanicState()
        {
            // Random movement while panicking
            if (Random.Range(0f, 1f) < 0.1f) // 10% chance each frame to change direction
            {
                Vector2 randomDirection = Random.insideUnitCircle.normalized;
                rb.linearVelocity = randomDirection * fleeSpeed * 0.7f;
                FaceDirection(randomDirection);
            }
            SetAnimation("Panicking");
        }
        
        void HandleTalkingState()
        {
            // Stop movement and face player
            rb.linearVelocity = Vector2.zero;
            if (player != null)
                FaceTarget(player.position);
            SetAnimation("Idle");
        }
        
        public void OnAttacked(Transform attacker)
        {
            if (hasBeenAttacked) return; // Prevent multiple reactions
            
            hasBeenAttacked = true;
            
            // Play scream sound
            PlayRandomSound(screamSounds);
            
            // React based on personality
            switch (personality)
            {
                case NPCPersonality.Coward:
                    ReactToAttack_Flee();
                    break;
                case NPCPersonality.Fighter:
                    if (Random.Range(0f, 1f) < 0.3f) // 30% chance to fight
                        ReactToAttack_Hostile();
                    else
                        ReactToAttack_Flee();
                    break;
                case NPCPersonality.Neutral:
                    if (Random.Range(0f, 1f) < 0.7f) // 70% chance to flee
                        ReactToAttack_Flee();
                    else
                        ReactToAttack_Panic();
                    break;
                case NPCPersonality.Brave:
                    if (Random.Range(0f, 1f) < 0.4f) // 40% chance to flee
                        ReactToAttack_Flee();
                    else
                        ReactToAttack_Hostile();
                    break;
            }
            
            // Change color to indicate state
            if (currentState == NPCState.Fleeing || currentState == NPCState.Panicking)
                ChangeColor(Color.yellow);
            else if (currentState == NPCState.Hostile)
                ChangeColor(hostileColor);
                
            // Notify NPC Manager
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.OnNPCAttacked(this, attacker);
            }
        }
        
        void ReactToAttack_Flee()
        {
            SetState(NPCState.Fleeing);
            stateTimer = fleeDuration;
            DecreaseReputation();
            Debug.Log($"{npcName} is running away!");
        }
        
        void ReactToAttack_Panic()
        {
            SetState(NPCState.Panicking);
            stateTimer = panicDuration;
            DecreaseReputation();
            Debug.Log($"{npcName} is panicking!");
        }
        
        void ReactToAttack_Hostile()
        {
            SetState(NPCState.Hostile);
            DecreaseReputation();
            Debug.Log($"{npcName} is now hostile!");
            // Add hostile behavior here (attack back, call guards, etc.)
        }
        
        public void OnHelped(Transform helper)
        {
            if (hasBeenAttacked) return; // Can't help after being attacked
            
            hasBeenHelped = true;
            SetState(NPCState.Thankful);
            ChangeColor(thankfulColor);
            PlayRandomSound(thankfulSounds);
            IncreaseReputation();
            
            Debug.Log($"{npcName} is thankful for your help!");
            
            // Show thank you message
            ShowMessage("Thank you for helping me!");
            
            // Notify NPC Manager
            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.OnNPCHelped(this, helper);
            }
        }
        
        public bool CanInteract()
        {
            return currentState != NPCState.Fleeing && 
                   currentState != NPCState.Panicking && 
                   currentState != NPCState.Hostile;
        }
        
        public void StartConversation()
        {
            if (!CanInteract()) return;
            
            SetState(NPCState.Talking);
            PlayRandomSound(greetingSounds);
            
            if (quest != null && !quest.isCompleted && CanOfferQuest())
            {
                ShowQuestDialog();
            }
            else if (hasBeenHelped)
            {
                ShowMessage("Thanks again for your help!");
            }
            else if (playerReputation < 0)
            {
                ShowMessage("I don't trust you...");
            }
            else
            {
                ShowMessage($"Hello! I'm {npcName}.");
            }
        }
        
        public void CompleteQuest()
        {
            if (quest != null && !quest.isCompleted)
            {
                quest.isCompleted = true;
                SetState(NPCState.Thankful);
                ChangeColor(thankfulColor);
                PlayRandomSound(thankfulSounds);
                IncreaseReputation();
                
                ShowMessage(quest.thankYouMessage);
                
                // Give reward if available
                if (quest.rewardItem != null)
                {
                    Instantiate(quest.rewardItem, transform.position + Vector3.up, Quaternion.identity);
                }
                
                Debug.Log($"Quest '{quest.questName}' completed!");
                
                // Notify NPC Manager
                if (NPCManager.Instance != null)
                {
                    NPCManager.Instance.OnQuestCompleted(this, quest);
                }
            }
        }
        
        void ShowQuestDialog()
        {
            ShowMessage($"Quest: {quest.questName}\n{quest.description}");
        }
        
        void ShowMessage(string message)
        {
            // Create a simple UI message above the NPC
            GameObject messageObj = new GameObject("NPCMessage");
            messageObj.transform.SetParent(transform);
            messageObj.transform.localPosition = Vector3.up * 2f;
            
            // Add TextMesh component for 3D text
            TextMesh textMesh = messageObj.AddComponent<TextMesh>();
            textMesh.text = message;
            textMesh.fontSize = 20;
            textMesh.color = Color.white;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            
            // Auto-destroy after 3 seconds
            Destroy(messageObj, 3f);
        }
        
        void OnStateTimerExpired()
        {
            switch (currentState)
            {
                case NPCState.Fleeing:
                case NPCState.Panicking:
                    SetState(NPCState.Idle);
                    ChangeColor(normalColor);
                    break;
            }
        }
        
        void SetState(NPCState newState)
        {
            currentState = newState;
        }
        
        void MoveTowards(Vector2 target, float speed)
        {
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            rb.linearVelocity = direction * speed;
            FaceDirection(direction);
        }
        
        void FaceTarget(Vector2 target)
        {
            Vector2 direction = (target - (Vector2)transform.position).normalized;
            FaceDirection(direction);
        }
        
        void FaceDirection(Vector2 direction)
        {
            if (direction.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (direction.x < 0)
                transform.localScale = new Vector3(-1, 1, 1);
        }
        
        void SetAnimation(string animationName)
        {
            if (animator != null)
            {
                animator.SetBool(walkingHash, animationName == "Walking");
                animator.SetBool(fleeingHash, animationName == "Fleeing");
                animator.SetBool(panicHash, animationName == "Panicking");
                animator.SetBool(idleHash, animationName == "Idle");
            }
        }
        
        void ChangeColor(Color newColor)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = newColor;
        }
        
        void PlayRandomSound(AudioClip[] sounds)
        {
            if (audioSource != null && sounds != null && sounds.Length > 0)
            {
                AudioClip clip = sounds[Random.Range(0, sounds.Length)];
                if (clip != null)
                    audioSource.PlayOneShot(clip);
            }
        }
        
        // Reputation system methods
        public void IncreaseReputation()
        {
            playerReputation = Mathf.Clamp(playerReputation + 1, -10, 10);
            Debug.Log($"{npcName} reputation increased to {playerReputation}");
        }
        
        public void DecreaseReputation()
        {
            playerReputation = Mathf.Clamp(playerReputation - 2, -10, 10);
            Debug.Log($"{npcName} reputation decreased to {playerReputation}");
        }
        
        public void ReactToWitnessedViolence()
        {
            if (currentState == NPCState.Idle)
            {
                DecreaseReputation();
                
                switch (personality)
                {
                    case NPCPersonality.Coward:
                        ReactToAttack_Flee();
                        break;
                    case NPCPersonality.Brave:
                        // Brave NPCs might become hostile to the attacker
                        if (Random.Range(0f, 1f) < 0.3f)
                            ReactToAttack_Hostile();
                        break;
                    default:
                        ReactToAttack_Panic();
                        break;
                }
            }
        }
        
        public bool CanOfferQuest()
        {
            return quest != null && !quest.isCompleted && playerReputation >= minReputationForQuest;
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw detection range
            Gizmos.color = Color.yellow;
            DrawWireCircle(transform.position, detectionRange);
            
            // Draw original position
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.5f);
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
