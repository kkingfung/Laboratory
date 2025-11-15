using UnityEngine;
using Laboratory.AI.Pathfinding;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Laboratory.AI
{
    /// <summary>
    /// Utility script to help set up and test the Enhanced Pathfinding System.
    /// Add this to a GameObject in your scene to automatically create the pathfinding system.
    /// </summary>
    public class PathfindingSystemSetup : MonoBehaviour
    {
        [Header("Auto Setup")]
        [SerializeField] private bool createSystemOnStart = true;
        [SerializeField] private bool findAndUpgradeExistingAI = true;

        [Header("System Configuration")]
        [SerializeField] private PathfindingMode defaultMode = PathfindingMode.Auto;
        [SerializeField] private int maxAgentsPerFrame = 10;
        [SerializeField] private float pathUpdateInterval = 0.2f;
        [SerializeField] private bool enableFlowFields = true;
        [SerializeField] private bool enableGroupPathfinding = true;

        [Header("Performance Settings")]
        [SerializeField] private int maxPathRequestsPerFrame = 5;
        [SerializeField] private float pathCacheLifetime = 5f;
        [SerializeField] private int maxCachedPaths = 100;

        [Header("Debug")]
        [SerializeField] private bool showDebugPaths = true;
        [SerializeField] private bool enablePerformanceLogging = true;

        private void Start()
        {
            if (createSystemOnStart)
            {
                SetupPathfindingSystem();
            }

            if (findAndUpgradeExistingAI)
            {
                UpgradeExistingAIAgents();
            }
        }

        [ContextMenu("Setup Pathfinding System")]
        public void SetupPathfindingSystem()
        {
            GameObject systemGO = GameObject.Find("Enhanced Pathfinding System");
            
            if (systemGO != null)
            {
                Debug.Log("✅ Enhanced Pathfinding System already exists!");
                return;
            }

            // Create system GameObject
            systemGO = new GameObject("Enhanced Pathfinding System");
            systemGO.transform.SetParent(transform);
            
            // Add a generic component to track the system
            var systemTracker = systemGO.AddComponent<PathfindingSystemTracker>();
            systemTracker.Configure(defaultMode, maxAgentsPerFrame, pathUpdateInterval, enableFlowFields,
                                   enableGroupPathfinding, pathCacheLifetime, maxCachedPaths, showDebugPaths, maxPathRequestsPerFrame);

            Debug.Log("✅ Enhanced Pathfinding System created and configured!");
        }

        [ContextMenu("Upgrade Existing AI Agents")]
        public void UpgradeExistingAIAgents()
        {
            int upgradedCount = 0;

            var allNavAgents = FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
            
            foreach (var navAgent in allNavAgents)
            {
                // Check if already has pathfinding capability
                if (navAgent.GetComponent<IPathfindingAgent>() == null)
                {
                    // Add a generic pathfinding agent component
                    var genericAgent = navAgent.gameObject.AddComponent<GenericPathfindingAgent>();
                    
                    // Configure based on existing NavMeshAgent settings
                    ConfigureGenericAgent(genericAgent, navAgent);
                    
                    upgradedCount++;
                    Debug.Log($"✅ Upgraded {navAgent.gameObject.name} with Generic Pathfinding Agent");
                }
            }

            Debug.Log($"🎉 Upgraded {upgradedCount} AI agents with pathfinding capability!");
        }

        private void ConfigureGenericAgent(GenericPathfindingAgent agent, UnityEngine.AI.NavMeshAgent navAgent)
        {
            // Auto-configure based on NavMeshAgent properties
            agent.baseSpeed = navAgent.speed;
            agent.stoppingDistance = navAgent.stoppingDistance;
            agent.acceleration = navAgent.acceleration;
        }

        [ContextMenu("Test Pathfinding Performance")]
        public void TestPathfindingPerformance()
        {
            var systemGO = GameObject.Find("Enhanced Pathfinding System");
            if (systemGO == null)
            {
                Debug.LogError("❌ Enhanced Pathfinding System not found! Create the system first.");
                return;
            }

            StartCoroutine(RunPerformanceTest());
        }

        private IEnumerator RunPerformanceTest()
        {
            Debug.Log("🧪 Starting pathfinding performance test...");

            // Create test agents
            var testAgents = CreateTestAgents(20);
            
            // Wait for agents to register and start pathfinding
            yield return new WaitForSeconds(1f);

            // Give random destinations to all agents
            foreach (var agent in testAgents)
            {
                if (agent != null)
                {
                    Vector3 randomDestination = Random.insideUnitSphere * 50f;
                    randomDestination.y = 0;
                    agent.SetDestination(randomDestination);
                }
            }

            // Monitor performance for 10 seconds
            float testDuration = 10f;
            float startTime = Time.time;
            
            while (Time.time - startTime < testDuration)
            {
                yield return new WaitForSeconds(1f);
                
                int activeAgents = testAgents.Count(a => a != null && a.IsMoving());
                Debug.Log($"📊 Performance Test - Active Agents: {activeAgents}, FPS: {1f / Time.deltaTime:F1}");
            }

            Debug.Log($"✅ Performance test completed!");

            // Clean up test agents
            foreach (var agent in testAgents)
            {
                if (agent != null)
                {
                    DestroyImmediate(agent.gameObject);
                }
            }
        }

        private GenericPathfindingAgent[] CreateTestAgents(int count)
        {
            var agents = new GenericPathfindingAgent[count];
            
            for (int i = 0; i < count; i++)
            {
                GameObject agentGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                agentGO.name = $"Test Agent {i}";
                agentGO.transform.position = Random.insideUnitSphere * 20f;
                agentGO.transform.position = new Vector3(agentGO.transform.position.x, 0, agentGO.transform.position.z);

                // Add required components
                var navAgent = agentGO.AddComponent<UnityEngine.AI.NavMeshAgent>();
                var pathfindingAgent = agentGO.AddComponent<GenericPathfindingAgent>();
                
                // Configure
                navAgent.radius = 0.5f;
                navAgent.height = 2f;
                navAgent.speed = Random.Range(2f, 5f);
                
                pathfindingAgent.baseSpeed = navAgent.speed;
                
                agents[i] = pathfindingAgent;
            }
            
            return agents;
        }

        [ContextMenu("Show System Status")]
        public void ShowSystemStatus()
        {
            var systemGO = GameObject.Find("Enhanced Pathfinding System");
            if (systemGO == null)
            {
                Debug.LogWarning("⚠️ Enhanced Pathfinding System not found!");
                return;
            }

            Debug.Log("📊 === Enhanced Pathfinding System Status ===");
            
            // Find all pathfinding agents in scene
            var allAgents = FindObjectsByType<GenericPathfindingAgent>(FindObjectsSortMode.None);
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            var pathfindingAgentsList = new List<MonoBehaviour>();
            for (int i = 0; i < allMonoBehaviours.Length; i++)
            {
                if (allMonoBehaviours[i] is IPathfindingAgent)
                {
                    pathfindingAgentsList.Add(allMonoBehaviours[i]);
                }
            }
            var pathfindingAgents = pathfindingAgentsList.ToArray();
                
            Debug.Log($"Generic Pathfinding Agents: {allAgents.Length}");
            Debug.Log($"IPathfindingAgent implementations: {pathfindingAgents.Length}");
            
            foreach (var agent in allAgents)
            {
                Debug.Log($"  • {agent.gameObject.name}: Moving={agent.IsMoving()}, HasDestination={agent.HasDestination()}");
            }
        }

        private void OnGUI()
        {
            if (!enablePerformanceLogging) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("🔧 Enhanced Pathfinding System");
            
            var systemGO = GameObject.Find("Enhanced Pathfinding System");
            if (systemGO != null)
            {
                var allAgents = FindObjectsByType<GenericPathfindingAgent>(FindObjectsSortMode.None);
                GUILayout.Label($"Agents: {allAgents.Length}");
                GUILayout.Label($"FPS: {1f / Time.deltaTime:F1}");
                
                if (GUILayout.Button("Show Status"))
                {
                    ShowSystemStatus();
                }
                
                if (GUILayout.Button("Test Performance"))
                {
                    TestPathfindingPerformance();
                }
            }
            else
            {
                GUILayout.Label("System not found!");
                if (GUILayout.Button("Create System"))
                {
                    SetupPathfindingSystem();
                }
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
    public class PathfindingSystemTracker : MonoBehaviour
    {
        [Header("System Settings")]
        public PathfindingMode mode = PathfindingMode.Auto;
        public int maxAgentsPerFrame = 10;
        public float pathUpdateInterval = 0.2f;
        public bool enableFlowFields = true;
        public bool enableGroupPathfinding = true;

        [Header("Performance Settings")]
        public float pathCacheLifetime = 5f;
        public int maxCachedPaths = 100;

        [Header("Debug")]
        public bool showDebugPaths = true;

        [Header("Request Limiting")]
        public int maxPathRequestsPerFrame = 5;

        public void Configure(PathfindingMode defaultMode, int maxAgents, float updateInterval, bool flowFields,
                            bool groupPathfinding, float cacheLifetime, int cachedPaths, bool debugPaths, int maxRequests)
        {
            mode = defaultMode;
            maxAgentsPerFrame = maxAgents;
            pathUpdateInterval = updateInterval;
            enableFlowFields = flowFields;
            enableGroupPathfinding = groupPathfinding;
            pathCacheLifetime = cacheLifetime;
            maxCachedPaths = cachedPaths;
            showDebugPaths = debugPaths;
            maxPathRequestsPerFrame = maxRequests;

            Debug.Log($"📝 Pathfinding system configured - Mode: {mode}, Max Agents: {maxAgents}, Max Requests/Frame: {maxRequests}");
        }
    }

    public class GenericPathfindingAgent : MonoBehaviour, IPathfindingAgent
    {
        [Header("Agent Settings")]
        public float baseSpeed = 3.5f;
        public float stoppingDistance = 0.5f;
        public float acceleration = 8f;
        public AgentType agentType = AgentType.Medium;

        private UnityEngine.AI.NavMeshAgent navAgent;
        private Vector3 currentDestination;
        private bool hasDestination = false;
        private PathfindingStatus status = PathfindingStatus.Idle;
        private PathfindingMode pathfindingMode = PathfindingMode.Auto;

        void Start()
        {
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent == null)
            {
                navAgent = gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
            }
        }

        // IPathfindingAgent implementation
        public AgentType AgentType => agentType;
        public PathfindingMode PathfindingMode => pathfindingMode;
        public PathfindingStatus Status => status;
        public Vector3 Position => transform.position;
        public Vector3 Destination => currentDestination;

        public void RequestPath(Vector3 destination, PathfindingMode mode = PathfindingMode.Auto)
        {
            currentDestination = destination;
            hasDestination = true;
            pathfindingMode = mode;
            status = PathfindingStatus.Computing;

            if (navAgent != null)
            {
                navAgent.SetDestination(destination);
                status = PathfindingStatus.Following;
            }
        }

        public void CancelPath()
        {
            hasDestination = false;
            status = PathfindingStatus.Cancelled;

            if (navAgent != null)
            {
                navAgent.ResetPath();
            }
        }

        public bool HasReachedDestination()
        {
            if (!hasDestination || navAgent == null)
                return false;

            return !navAgent.pathPending && navAgent.remainingDistance <= stoppingDistance;
        }

        public void OnPathCalculated(Vector3[] path, bool success)
        {
            if (success && path != null && path.Length > 0 && navAgent != null)
            {
                navAgent.SetPath(new UnityEngine.AI.NavMeshPath());
                var navPath = new UnityEngine.AI.NavMeshPath();
                navPath.corners = path;
                navAgent.SetPath(navPath);
                status = PathfindingStatus.Following;
            }
            else
            {
                status = PathfindingStatus.Failed;
            }
        }

        // Public methods
        public void SetDestination(Vector3 destination)
        {
            currentDestination = destination;
            hasDestination = true;
            
            if (navAgent != null)
            {
                navAgent.SetDestination(destination);
            }
        }

        public bool IsMoving()
        {
            return navAgent != null && navAgent.velocity.magnitude > 0.1f;
        }

        public bool HasDestination()
        {
            return hasDestination;
        }
    }
}
