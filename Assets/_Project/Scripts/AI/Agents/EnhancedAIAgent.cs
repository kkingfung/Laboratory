using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Laboratory.AI.Pathfinding;

namespace Laboratory.AI.Agents
{
    /// <summary>
    /// Enhanced AI Agent that integrates with the Advanced Pathfinding System.
    /// Provides high-level movement and navigation controls for AI entities.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnhancedAIAgent : MonoBehaviour, IPathfindingAgent
    {
        [Header("Agent Configuration")]
        [SerializeField] private AgentType agentType = AgentType.Medium;
        [SerializeField] private PathfindingMode preferredMode = PathfindingMode.Auto;
        [SerializeField] private float pathUpdateFrequency = 0.3f;
        [SerializeField] private float stuckDetectionThreshold = 0.1f;
        [SerializeField] private float stuckDetectionTime = 2f;

        [Header("Movement")]
        [SerializeField] private float baseSpeed = 3.5f;
        [SerializeField] private float sprintSpeed = 6f;
        [SerializeField] private float acceleration = 8f;
        [SerializeField] private float stoppingDistance = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        [SerializeField] private bool enablePathVisualization = true;

        // Core components
        private NavMeshAgent navAgent;
        private Rigidbody rb;

        // Pathfinding integration
        private Vector3[] currentPath;
        private int currentWaypointIndex = 0;
        private Vector3 currentDestination;
        private bool hasValidPath = false;
        private float lastPathRequest = 0f;
        private bool pathRequested = false;

        // Movement state
        private bool isSprinting = false;
        private bool isMoving = false;
        private bool isStopped = false;
        private Vector3 lastPosition;
        private float timeSinceLastMovement = 0f;

        // Performance optimization
        private float lastPositionUpdate = 0f;
        private const float POSITION_UPDATE_INTERVAL = 0.1f;

        public enum AgentType
        {
            Small,
            Medium,
            Large,
            Flying
        }

        #region Properties

        public Vector3 Position => transform.position;
        public Vector3 Destination => currentDestination;
        public bool IsMoving => isMoving;
        public bool HasReachedDestination => HasReachedCurrentDestination();
        public bool HasValidPath => hasValidPath && currentPath != null;
        public float CurrentSpeed => navAgent.velocity.magnitude;
        public AgentType CurrentAgentType => agentType;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
            ConfigureAgent();
        }

        private void Start()
        {
            RegisterWithPathfindingSystem();
            lastPosition = transform.position;
        }

        private void Update()
        {
            UpdateMovement();
            UpdateStuckDetection();
            UpdateDebugInfo();
        }

        private void OnDestroy()
        {
            UnregisterFromPathfindingSystem();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            navAgent = GetComponent<NavMeshAgent>();
            rb = GetComponent<Rigidbody>();

            if (navAgent == null)
            {
                Debug.LogError($"NavMeshAgent not found on {gameObject.name}!", this);
                enabled = false;
                return;
            }
        }

        private void ConfigureAgent()
        {
            // Configure NavMeshAgent based on agent type
            switch (agentType)
            {
                case AgentType.Small:
                    navAgent.radius = 0.3f;
                    navAgent.height = 1f;
                    baseSpeed = 2.5f;
                    break;
                
                case AgentType.Medium:
                    navAgent.radius = 0.5f;
                    navAgent.height = 2f;
                    baseSpeed = 3.5f;
                    break;
                
                case AgentType.Large:
                    navAgent.radius = 1f;
                    navAgent.height = 3f;
                    baseSpeed = 2f;
                    break;
                
                case AgentType.Flying:
                    navAgent.radius = 0.5f;
                    navAgent.height = 2f;
                    baseSpeed = 5f;
                    break;
            }

            navAgent.speed = baseSpeed;
            navAgent.acceleration = acceleration;
            navAgent.stoppingDistance = stoppingDistance;
            navAgent.autoBraking = true;
            navAgent.autoRepath = false; // We handle repathing through enhanced system
        }

        private void RegisterWithPathfindingSystem()
        {
            if (EnhancedPathfindingSystem.Instance != null)
            {
                EnhancedPathfindingSystem.Instance.RegisterAgent(this);
            }
        }

        private void UnregisterFromPathfindingSystem()
        {
            if (EnhancedPathfindingSystem.Instance != null)
            {
                EnhancedPathfindingSystem.Instance.UnregisterAgent(this);
            }
        }

        #endregion

        #region Public Movement API

        /// <summary>
        /// Set a destination for the agent to move to
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            currentDestination = destination;
            RequestNewPath();
        }

        /// <summary>
        /// Stop the agent's movement
        /// </summary>
        public void Stop()
        {
            isStopped = true;
            navAgent.ResetPath();
            hasValidPath = false;
            currentPath = null;
        }

        /// <summary>
        /// Resume movement (if destination is set)
        /// </summary>
        public void Resume()
        {
            isStopped = false;
            if (currentDestination != Vector3.zero)
            {
                RequestNewPath();
            }
        }

        /// <summary>
        /// Set movement speed
        /// </summary>
        public void SetSpeed(float speed)
        {
            baseSpeed = speed;
            navAgent.speed = isSprinting ? sprintSpeed : baseSpeed;
        }

        /// <summary>
        /// Enable/disable sprinting
        /// </summary>
        public void SetSprinting(bool sprint)
        {
            isSprinting = sprint;
            navAgent.speed = sprint ? sprintSpeed : baseSpeed;
        }

        /// <summary>
        /// Set the agent type (affects movement parameters)
        /// </summary>
        public void SetAgentType(AgentType type)
        {
            agentType = type;
            ConfigureAgent();
        }

        /// <summary>
        /// Warp the agent to a position instantly
        /// </summary>
        public void Warp(Vector3 position)
        {
            if (navAgent.isOnNavMesh)
            {
                navAgent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
            
            Stop(); // Clear current path
        }

        #endregion

        #region Movement Update

        private void UpdateMovement()
        {
            if (isStopped || !hasValidPath) 
            {
                isMoving = false;
                return;
            }

            UpdatePathFollowing();
            UpdateMovementState();
        }

        private void UpdatePathFollowing()
        {
            if (currentPath == null || currentPath.Length == 0) return;

            // Check if we've reached the current waypoint
            if (currentWaypointIndex < currentPath.Length)
            {
                Vector3 targetWaypoint = currentPath[currentWaypointIndex];
                float distanceToWaypoint = Vector3.Distance(transform.position, targetWaypoint);

                if (distanceToWaypoint < 1.5f)
                {
                    currentWaypointIndex++;
                    
                    // If we've reached the end of the path
                    if (currentWaypointIndex >= currentPath.Length)
                    {
                        hasValidPath = false;
                        isMoving = false;
                        return;
                    }
                }

                // Move to current waypoint using NavMeshAgent
                if (navAgent.isOnNavMesh && navAgent.destination != targetWaypoint)
                {
                    navAgent.SetDestination(targetWaypoint);
                }
            }
        }

        private void UpdateMovementState()
        {
            float currentSpeed = navAgent.velocity.magnitude;
            isMoving = currentSpeed > 0.1f;

            // Update position tracking for stuck detection
            if (Time.time - lastPositionUpdate > POSITION_UPDATE_INTERVAL)
            {
                UpdatePositionTracking();
                lastPositionUpdate = Time.time;
            }
        }

        private void UpdatePositionTracking()
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            
            if (distanceMoved < stuckDetectionThreshold && isMoving)
            {
                timeSinceLastMovement += Time.deltaTime;
            }
            else
            {
                timeSinceLastMovement = 0f;
                lastPosition = transform.position;
            }
        }

        #endregion

        #region Pathfinding Integration

        private void RequestNewPath()
        {
            if (EnhancedPathfindingSystem.Instance == null || 
                currentDestination == Vector3.zero ||
                Time.time - lastPathRequest < pathUpdateFrequency)
            {
                return;
            }

            lastPathRequest = Time.time;
            pathRequested = true;
            
            EnhancedPathfindingSystem.Instance.RequestPath(
                transform.position,
                currentDestination,
                this,
                preferredMode
            );
        }

        private bool HasReachedCurrentDestination()
        {
            if (currentDestination == Vector3.zero) return true;
            
            float distance = Vector3.Distance(transform.position, currentDestination);
            return distance <= stoppingDistance * 2f;
        }

        #endregion

        #region Stuck Detection

        private void UpdateStuckDetection()
        {
            if (timeSinceLastMovement > stuckDetectionTime && isMoving)
            {
                HandleStuckSituation();
            }
        }

        private void HandleStuckSituation()
        {
            Debug.LogWarning($"Agent {gameObject.name} appears to be stuck, requesting new path.");
            
            // Try to find a nearby valid position
            Vector3 randomOffset = Random.insideUnitSphere * 2f;
            randomOffset.y = 0;
            Vector3 unstuckPosition = transform.position + randomOffset;
            
            if (NavMesh.SamplePosition(unstuckPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                Warp(hit.position);
                Resume();
            }
            
            timeSinceLastMovement = 0f;
        }

        public bool IsStuck()
        {
            return timeSinceLastMovement > stuckDetectionTime && isMoving;
        }

        #endregion

        #region Utility Methods

        public float GetCurrentSpeed()
        {
            return navAgent.velocity.magnitude;
        }

        public float GetPathProgress()
        {
            if (currentPath == null || currentPath.Length <= 1) return 1f;
            
            return (float)currentWaypointIndex / (currentPath.Length - 1);
        }

        public Vector3 GetVelocity()
        {
            return navAgent.velocity;
        }

        public bool IsOnNavMesh()
        {
            return navAgent.isOnNavMesh;
        }

        #endregion

        #region IPathfindingAgent Implementation

        public Vector3 GetPosition()
        {
            return transform.position;
        }

        public Vector3 GetDestination()
        {
            return currentDestination;
        }

        public bool NeedsPathUpdate()
        {
            if (!pathRequested) return false;
            
            return Time.time - lastPathRequest > pathUpdateFrequency ||
                   Vector3.Distance(transform.position, currentDestination) > 5f ||
                   !hasValidPath;
        }

        public bool ShouldForcePathUpdate()
        {
            return IsStuck() || 
                   (!hasValidPath && currentDestination != Vector3.zero) ||
                   (currentPath != null && currentWaypointIndex >= currentPath.Length);
        }

        public void OnPathCalculated(Vector3[] path, bool success)
        {
            pathRequested = false;
            
            if (success && path != null && path.Length > 0)
            {
                currentPath = path;
                currentWaypointIndex = 0;
                hasValidPath = true;
                
                if (showDebugInfo)
                {
                    Debug.Log($"Path calculated for {gameObject.name}: {path.Length} waypoints");
                }
            }
            else
            {
                hasValidPath = false;
                
                // Fallback to direct NavMesh pathfinding
                if (navAgent.isOnNavMesh && currentDestination != Vector3.zero)
                {
                    navAgent.SetDestination(currentDestination);
                }
                
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Path calculation failed for {gameObject.name}");
                }
            }
        }

        public void DrawDebugPath()
        {
            if (!enablePathVisualization || currentPath == null) return;
            
            // Draw the path
            for (int i = 0; i < currentPath.Length - 1; i++)
            {
                Debug.DrawLine(currentPath[i], currentPath[i + 1], Color.blue, 0.1f);
            }
            
            // Draw current target waypoint
            if (currentWaypointIndex < currentPath.Length)
            {
                Debug.DrawLine(transform.position, currentPath[currentWaypointIndex], Color.yellow, 0.1f);
            }
            
            // Draw destination
            if (currentDestination != Vector3.zero)
            {
                Debug.DrawLine(transform.position, currentDestination, Color.green, 0.1f);
            }
        }

        #endregion

        #region Debug and Visualization

        private void UpdateDebugInfo()
        {
            if (!showDebugInfo) return;
            
            // Draw debug information in scene view
            DrawDebugPath();
        }

        private void OnDrawGizmosSelected()
        {
            if (!enablePathVisualization) return;
            
            // Draw agent radius
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, navAgent ? navAgent.radius : 0.5f);
            
            // Draw current path
            if (currentPath != null && currentPath.Length > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < currentPath.Length - 1; i++)
                {
                    Gizmos.DrawLine(currentPath[i], currentPath[i + 1]);
                }
                
                // Highlight current waypoint
                if (currentWaypointIndex < currentPath.Length)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(currentPath[currentWaypointIndex], 0.5f);
                }
            }
            
            // Draw destination
            if (currentDestination != Vector3.zero)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(currentDestination, 0.3f);
                Gizmos.DrawLine(transform.position, currentDestination);
            }
            
            // Draw stuck detection
            if (IsStuck())
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2, Vector3.one * 0.5f);
            }
        }

        public string GetDebugInfo()
        {
            return $"Enhanced AI Agent - {gameObject.name}\n" +
                   $"Type: {agentType}\n" +
                   $"Speed: {GetCurrentSpeed():F1} / {baseSpeed}\n" +
                   $"Moving: {isMoving}\n" +
                   $"Has Path: {hasValidPath}\n" +
                   $"Waypoint: {currentWaypointIndex}/{(currentPath?.Length ?? 0)}\n" +
                   $"Distance to Dest: {Vector3.Distance(transform.position, currentDestination):F1}\n" +
                   $"Stuck: {IsStuck()}";
        }

        #endregion
    }
}
