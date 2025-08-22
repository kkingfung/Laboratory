using UnityEngine;

// Player interaction script to trigger NPC behaviors
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactionRange = 2f;
    public KeyCode interactionKey = KeyCode.E;
    public KeyCode attackKey = KeyCode.F;
    public KeyCode helpKey = KeyCode.H;
    public KeyCode questCompleteKey = KeyCode.Q;
    public LayerMask npcLayer = -1;
    
    [Header("UI Settings")]
    public string interactPrompt = "Press E to interact";
    public string attackPrompt = "Press F to attack";
    public string helpPrompt = "Press H to help";
    
    private NPCBehavior nearestNPC;
    
    void Update()
    {
        FindNearestNPC();
        HandleInteractionInput();
        UpdateUI();
    }
    
    void FindNearestNPC()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, npcLayer);
        
        nearestNPC = null;
        float nearestDistance = float.MaxValue;
        
        foreach (var collider in nearbyColliders)
        {
            NPCBehavior npc = collider.GetComponent<NPCBehavior>();
            if (npc != null && npc.CanInteract())
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestNPC = npc;
                }
            }
        }
    }
    
    void UpdateUI()
    {
        if (NPCManager.Instance != null)
        {
            if (nearestNPC != null && nearestNPC.CanInteract())
            {
                string prompt = interactPrompt;
                
                if (nearestNPC.CanOfferQuest())
                {
                    prompt = $"{interactPrompt} - {nearestNPC.quest.questName}";
                }
                
                NPCManager.Instance.ShowInteractionPrompt(prompt, true);
            }
            else
            {
                NPCManager.Instance.ShowInteractionPrompt("", false);
            }
        }
    }
    
    void HandleInteractionInput()
    {
        // Interact
        if (Input.GetKeyDown(interactionKey) && nearestNPC != null)
        {
            nearestNPC.StartConversation();
        }
        
        // Attack
        if (Input.GetKeyDown(attackKey))
        {
            Attack();
        }
        
        // Help
        if (Input.GetKeyDown(helpKey))
        {
            Help();
        }
        
        // Complete quest (when carrying quest item)
        if (Input.GetKeyDown(questCompleteKey))
        {
            CompleteQuest();
        }
    }
    
    // Call this method when player attacks
    public void Attack()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, interactionRange, npcLayer);
        
        foreach (var collider in hitColliders)
        {
            NPCBehavior npc = collider.GetComponent<NPCBehavior>();
            if (npc != null)
            {
                npc.OnAttacked(transform);
            }
        }
    }
    
    // Call this method when player helps an NPC
    public void Help()
    {
        if (nearestNPC != null)
        {
            nearestNPC.OnHelped(transform);
        }
    }
    
    // Call this method to complete a quest
    public void CompleteQuest()
    {
        if (nearestNPC != null)
        {
            nearestNPC.CompleteQuest();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        DrawWireCircle(transform.position, interactionRange);
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