using UnityEngine;
using ProjectChimera.AI.Pathfinding;
using ProjectChimera.AI.Agents;

namespace ProjectChimera.AI.Testing
{
    /// <summary>
    /// Simple test script to verify the Enhanced Pathfinding System is working.
    /// Add this to any GameObject to test basic functionality.
    /// </summary>
    public class SimplePathfindingTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool testOnStart = true;
        [SerializeField] private bool createTestAgent = true;
        [SerializeField] private Vector3 testDestination = new Vector3(10, 0, 10);

        private void Start()
        {
            if (testOnStart)
            {
                RunBasicTest();
            }
        }

        [ContextMenu("Run Basic Test")]
        public void RunBasicTest()
        {
            Debug.Log("=== Enhanced Pathfinding Basic Test ===");

            // Test 1: Check if Enhanced Pathfinding System exists
            if (EnhancedPathfindingSystem.Instance != null)
            {
                Debug.Log("[AI] Enhanced Pathfinding System found!");
                var system = EnhancedPathfindingSystem.Instance;
                Debug.Log($"[AI] System Status - Agents: {system.RegisteredAgentCount}, Cached: {system.CachedPathCount}");
            }
            else
            {
                Debug.LogError("[AI] Enhanced Pathfinding System NOT found!");
                Debug.Log("[AI] Create an empty GameObject and add the PathfindingSystemSetup component, then click 'Setup Pathfinding System'");
                return;
            }

            // Test 2: Check NavMesh
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out var hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                Debug.Log("[AI] NavMesh found and accessible!");
            }
            else
            {
                Debug.LogError("[AI] NavMesh not found! Please bake NavMesh: Window ¡÷ AI ¡÷ Navigation ¡÷ Bake");
                return;
            }

            // Test 3: Create test agent if requested
            if (createTestAgent)
            {
                CreateAndTestAgent();
            }

            Debug.Log("[AI] Basic test completed!");
        }

        private void CreateAndTestAgent()
        {
            Debug.Log("[AI] Creating test agent...");

            // Create test agent GameObject
            GameObject testAgent = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            testAgent.name = "PathfindingTestAgent";
            testAgent.transform.position = transform.position;

            // Add NavMeshAgent
            var navAgent = testAgent.AddComponent<UnityEngine.AI.NavMeshAgent>();
            navAgent.radius = 0.5f;
            navAgent.height = 2f;
            navAgent.speed = 3.5f;

            // Add EnhancedAIAgent
            var enhancedAgent = testAgent.AddComponent<EnhancedAIAgent>();
            enhancedAgent.SetAgentType(ProjectChimera.AI.Pathfinding.AgentType.Medium);

            // Test path request
            Vector3 destination = testDestination;
            if (UnityEngine.AI.NavMesh.SamplePosition(destination, out var hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
            {
                destination = hit.position;
                enhancedAgent.SetDestination(destination);
                Debug.Log($"[AI] Test agent created and moving to: {destination}");
                
                // Add a colored material for visibility
                var renderer = testAgent.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.green;
                }
            }
            else
            {
                Debug.LogWarning("[AI] Could not find valid destination for test agent");
                DestroyImmediate(testAgent);
            }
        }

        [ContextMenu("Cleanup Test Agents")]
        public void CleanupTestAgents()
        {
            var testAgents = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            int cleaned = 0;

            foreach (var obj in testAgents)
            {
                if (obj.name.Contains("PathfindingTestAgent"))
                {
                    DestroyImmediate(obj);
                    cleaned++;
                }
            }

            Debug.Log($"[AI] Cleaned up {cleaned} test agents");
        }

        [ContextMenu("Show Pathfinding System Status")]
        public void ShowSystemStatus()
        {
            if (EnhancedPathfindingSystem.Instance == null)
            {
                Debug.LogError("[AI] Enhanced Pathfinding System not found!");
                return;
            }

            var system = EnhancedPathfindingSystem.Instance;
            Debug.Log("=== Enhanced Pathfinding System Status ===");
            Debug.Log($"[AI] Registered Agents: {system.RegisteredAgentCount}");
            Debug.Log($"[AI] Pending Requests: {system.PendingRequestCount}");
            Debug.Log($"[AI] Cached Paths: {system.CachedPathCount}");
            Debug.Log($"[AI] Total Paths Calculated: {system.TotalPathsCalculated}");

            // List all enhanced agents
            var agents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
            Debug.Log($"[AI] Enhanced AI Agents in scene: {agents.Length}");
            
            foreach (var agent in agents)
            {
                Debug.Log($"   - {agent.gameObject.name}: " +
                         $"Moving={agent.IsMoving}, " +
                         $"HasPath={agent.HasValidPath}, " +
                         $"Speed={agent.CurrentSpeed:F1}");
            }
        }

        private void OnGUI()
        {
            // Simple on-screen status display
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Pathfinding Test", GUI.skin.label);
            
            if (EnhancedPathfindingSystem.Instance != null)
            {
                var system = EnhancedPathfindingSystem.Instance;
                GUILayout.Label($"System: Active");
                GUILayout.Label($"Agents: {system.RegisteredAgentCount}");
                GUILayout.Label($"Pending: {system.PendingRequestCount}");
                GUILayout.Label($"Cached: {system.CachedPathCount}");
            }
            else
            {
                GUILayout.Label("System: Not Found");
            }

            if (GUILayout.Button("Run Test"))
            {
                RunBasicTest();
            }

            if (GUILayout.Button("Cleanup"))
            {
                CleanupTestAgents();
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}
