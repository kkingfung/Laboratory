using UnityEngine;

namespace Laboratory.Core.NPC
{
    // Simple quest item that can be collected
    public class QuestItem : MonoBehaviour
    {
        [Header("Quest Item")]
        public string itemName = "Quest Item";
        public string questName = ""; // Which quest this item belongs to
        public bool autoCompleteQuest = true;
        
        [Header("Visual Settings")]
        public float bobHeight = 0.5f;
        public float bobSpeed = 2f;
        public bool rotateItem = true;
        public float rotationSpeed = 45f;
        
        [Header("Audio")]
        public AudioClip pickupSound;
        
        private Vector3 startPosition;
        private bool hasBeenCollected = false;
        private AudioSource audioSource;
        
        void Start()
        {
            startPosition = transform.position;
            audioSource = GetComponent<AudioSource>();
            
            // Add collider if it doesn't exist
            if (GetComponent<Collider2D>() == null)
            {
                CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
            }
        }
        
        void Update()
        {
            if (hasBeenCollected) return;
            
            // Bob up and down
            if (bobHeight > 0)
            {
                float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
                transform.position = new Vector3(startPosition.x, newY, startPosition.z);
            }
            
            // Rotate the item
            if (rotateItem)
            {
                transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            if (hasBeenCollected) return;
            
            if (other.CompareTag("Player"))
            {
                CollectItem();
            }
        }
        
        void CollectItem()
        {
            hasBeenCollected = true;
            
            // Play pickup sound
            if (audioSource != null && pickupSound != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }
            
            // Show pickup message
            ShowPickupMessage();
            
            if (autoCompleteQuest)
            {
                // Find NPC with matching quest and complete it
                CompleteMatchingQuest();
            }
            
            // Add to player inventory (if you have one)
            PlayerInventory inventory = FindFirstObjectByType<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddItem(itemName);
            }
            
            // Destroy the item (with delay for sound)
            Destroy(gameObject, pickupSound != null ? pickupSound.length : 0f);
        }
        
        void CompleteMatchingQuest()
        {
            NPCBehavior[] npcs = FindObjectsByType<NPCBehavior>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                if (npc.quest != null && npc.quest.questName == questName && !npc.quest.isCompleted)
                {
                    npc.CompleteQuest();
                    Debug.Log($"Auto-completed quest: {questName}");
                    break;
                }
            }
        }
        
        void ShowPickupMessage()
        {
            // Create a message above the item
            GameObject messageObj = new GameObject("PickupMessage");
            messageObj.transform.position = transform.position + Vector3.up * 2f;
            
            TextMesh textMesh = messageObj.AddComponent<TextMesh>();
            textMesh.text = $"Collected: {itemName}";
            textMesh.fontSize = 16;
            textMesh.color = Color.green;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            
            // Float upward and fade
            StartCoroutine(FloatAndFade(messageObj));
        }
        
        System.Collections.IEnumerator FloatAndFade(GameObject messageObj)
        {
            TextMesh textMesh = messageObj.GetComponent<TextMesh>();
            Vector3 startPos = messageObj.transform.position;
            Color startColor = textMesh.color;
            
            float duration = 2f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                
                // Float upward
                messageObj.transform.position = startPos + Vector3.up * progress * 1.5f;
                
                // Fade out
                Color newColor = startColor;
                newColor.a = 1f - progress;
                textMesh.color = newColor;
                
                yield return null;
            }
            
            Destroy(messageObj);
        }
        
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 1f);
            
            // Draw quest name if assigned
            if (!string.IsNullOrEmpty(questName))
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, $"Quest: {questName}");
#endif
            }
        }
    }

    // Simple inventory system for quest items
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory")]
        public System.Collections.Generic.List<string> items = new System.Collections.Generic.List<string>();
        public int maxItems = 20;
        
        public void AddItem(string itemName)
        {
            if (items.Count < maxItems)
            {
                items.Add(itemName);
                Debug.Log($"Added {itemName} to inventory. Total items: {items.Count}");
            }
            else
            {
                Debug.Log("Inventory full!");
            }
        }
        
        public bool HasItem(string itemName)
        {
            return items.Contains(itemName);
        }
        
        public bool RemoveItem(string itemName)
        {
            if (items.Contains(itemName))
            {
                items.Remove(itemName);
                Debug.Log($"Removed {itemName} from inventory");
                return true;
            }
            return false;
        }
        
        public void ClearInventory()
        {
            items.Clear();
            Debug.Log("Inventory cleared");
        }
    }
}
