using UnityEngine;
using System.Collections.Generic;
using Laboratory.Subsystems.EnemyAI.NPC;

namespace Laboratory.Core.NPC
{
    public enum PatrolType
    {
        Stationary,
        RandomWalk,
        PatrolPoints,
        FollowPath
    }

    public class NPCMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        public PatrolType patrolType = PatrolType.RandomWalk;
        public float moveSpeed = 2f;
        public float waitTime = 2f;
        public float wanderRadius = 5f;
        
        [Header("Patrol Points")]
        public Transform[] patrolPoints;
        public bool loopPatrol = true;
        
        [Header("Path Following")]
        public LineRenderer pathRenderer;
        public bool drawPath = true;
        
        // Private variables
        private Vector2 startPosition;
        private Vector2 currentTarget;
        private int currentPatrolIndex = 0;
        private bool patrolForward = true;
        private float waitTimer = 0f;
        private bool isWaiting = false;
        private NPCBehavior npcBehavior;
        private Rigidbody2D rb;
        
        void Start()
        {
            startPosition = transform.position;
            currentTarget = startPosition;
            npcBehavior = GetComponent<NPCBehavior>();
            rb = GetComponent<Rigidbody2D>();
            
            if (patrolType == PatrolType.PatrolPoints && patrolPoints.Length > 0)
            {
                currentTarget = patrolPoints[0].position;
            }
            
            SetupPathRenderer();
        }
        
        void Update()
        {
            // Only move if NPC is in idle state
            if (npcBehavior != null && npcBehavior.currentState != NPCState.Idle)
                return;
                
            HandleMovement();
        }
        
        void HandleMovement()
        {
            if (isWaiting)
            {
                waitTimer -= Time.deltaTime;
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    SetNextTarget();
                }
                return;
            }
            
            float distanceToTarget = Vector2.Distance(transform.position, currentTarget);
            
            if (distanceToTarget < 0.5f)
            {
                // Reached target
                OnTargetReached();
            }
            else
            {
                // Move towards target
                MoveTowardsTarget();
            }
        }
        
        void MoveTowardsTarget()
        {
            Vector2 direction = (currentTarget - (Vector2)transform.position).normalized;
            
            if (rb != null)
            {
                rb.linearVelocity = direction * moveSpeed;
            }
            else
            {
                transform.Translate(direction * moveSpeed * Time.deltaTime);
            }
            
            // Face movement direction
            FaceDirection(direction);
        }
        
        void OnTargetReached()
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
                
            if (waitTime > 0f)
            {
                isWaiting = true;
                waitTimer = waitTime + Random.Range(-0.5f, 0.5f); // Add some randomness
            }
            else
            {
                SetNextTarget();
            }
        }
        
        void SetNextTarget()
        {
            switch (patrolType)
            {
                case PatrolType.Stationary:
                    currentTarget = startPosition;
                    break;
                    
                case PatrolType.RandomWalk:
                    SetRandomTarget();
                    break;
                    
                case PatrolType.PatrolPoints:
                    SetPatrolPointTarget();
                    break;
                    
                case PatrolType.FollowPath:
                    SetPathTarget();
                    break;
            }
        }
        
        void SetRandomTarget()
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(1f, wanderRadius);
            Vector2 randomTarget = startPosition + randomDirection * randomDistance;
            
            // Make sure the target is walkable (you might want to add collision checking here)
            currentTarget = randomTarget;
        }
        
        void SetPatrolPointTarget()
        {
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                currentTarget = startPosition;
                return;
            }
            
            if (loopPatrol)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
            else
            {
                if (patrolForward)
                {
                    currentPatrolIndex++;
                    if (currentPatrolIndex >= patrolPoints.Length - 1)
                    {
                        currentPatrolIndex = patrolPoints.Length - 1;
                        patrolForward = false;
                    }
                }
                else
                {
                    currentPatrolIndex--;
                    if (currentPatrolIndex <= 0)
                    {
                        currentPatrolIndex = 0;
                        patrolForward = true;
                    }
                }
            }
            
            if (currentPatrolIndex >= 0 && currentPatrolIndex < patrolPoints.Length)
            {
                currentTarget = patrolPoints[currentPatrolIndex].position;
            }
        }
        
        void SetPathTarget()
        {
            // Similar to patrol points but could use more complex pathfinding
            SetPatrolPointTarget();
        }
        
        void FaceDirection(Vector2 direction)
        {
            if (direction.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (direction.x < 0)
                transform.localScale = new Vector3(-1, 1, 1);
        }
        
        void SetupPathRenderer()
        {
            if (pathRenderer != null && drawPath && patrolPoints != null && patrolPoints.Length > 0)
            {
                pathRenderer.positionCount = patrolPoints.Length;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    pathRenderer.SetPosition(i, patrolPoints[i].position);
                }
                
                pathRenderer.startWidth = 0.1f;
                pathRenderer.endWidth = 0.1f;
                pathRenderer.startColor = Color.cyan;
                pathRenderer.endColor = Color.cyan;
            }
        }
        
        // Public methods for external control
        public void StopMovement()
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            isWaiting = true;
        }
        
        public void ResumeMovement()
        {
            isWaiting = false;
        }
        
        public void SetCustomTarget(Vector2 target)
        {
            currentTarget = target;
            isWaiting = false;
        }
        
        void OnDrawGizmosSelected()
        {
            // Draw wander radius for random walk
            if (patrolType == PatrolType.RandomWalk)
            {
                Gizmos.color = Color.green;
                DrawWireCircle(startPosition, wanderRadius);
            }
            
            // Draw patrol points
            if (patrolType == PatrolType.PatrolPoints && patrolPoints != null)
            {
                Gizmos.color = Color.blue;
                for (int i = 0; i < patrolPoints.Length; i++)
                {
                    if (patrolPoints[i] != null)
                    {
                        Gizmos.DrawWireSphere(patrolPoints[i].position, 0.3f);
                        Gizmos.DrawLine(transform.position, patrolPoints[i].position);
                        
                        // Draw connections between patrol points
                        if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                        }
                        else if (loopPatrol && i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                        {
                            Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                        }
                    }
                }
            }
            
            // Draw current target
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(currentTarget, Vector3.one * 0.5f);
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
