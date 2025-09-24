using UnityEngine;
using UnityEditor;
using Laboratory.AI.Pathfinding;
using Laboratory.AI.Agents;
using Laboratory.Chimera.AI;
using Laboratory.Subsystems.EnemyAI;

namespace Laboratory.AI.Editor
{
    /// <summary>
    /// Custom editor window for managing the Enhanced Pathfinding System.
    /// Provides easy setup, monitoring, and debugging tools.
    /// </summary>
    public class PathfindingManagerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showSystemInfo = true;
        private bool showAgentList = true;
        private bool showPerformance = true;
        private bool showDebugTools = true;
        
        private string testAgentCount = "10";
        private string testDuration = "30";

        [MenuItem("Laboratory/AI/Pathfinding Manager")]
        public static void ShowWindow()
        {
            PathfindingManagerWindow window = GetWindow<PathfindingManagerWindow>();
            window.titleContent = new GUIContent("Pathfinding Manager");
            window.Show();
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.LabelField("Enhanced Pathfinding System Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawSystemSetup();
            EditorGUILayout.Space();
            
            if (Application.isPlaying)
            {
                DrawRuntimeInfo();
            }
            else
            {
                DrawEditorInfo();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSystemSetup()
        {
            EditorGUILayout.LabelField("System Setup", EditorStyles.boldLabel);
            
            var system = FindFirstObjectByType<EnhancedPathfindingSystem>();
            
            if (system == null)
            {
                EditorGUILayout.HelpBox("Enhanced Pathfinding System not found in scene!", MessageType.Warning);
                
                if (GUILayout.Button("Create Enhanced Pathfinding System"))
                {
                    CreatePathfindingSystem();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Enhanced Pathfinding System found and ready!", MessageType.Info);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Select System"))
                {
                    Selection.activeObject = system.gameObject;
                }
                if (GUILayout.Button("Remove System"))
                {
                    if (EditorUtility.DisplayDialog("Remove System", 
                        "Are you sure you want to remove the Enhanced Pathfinding System?", "Yes", "No"))
                    {
                        DestroyImmediate(system.gameObject);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space();
            
            // AI Agent Management
            EditorGUILayout.LabelField("AI Agent Management", EditorStyles.boldLabel);
            
            var allNavAgents = FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
            var enhancedAgents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
            
            EditorGUILayout.LabelField($"NavMesh Agents in scene: {allNavAgents.Length}");
            EditorGUILayout.LabelField($"Enhanced AI Agents: {enhancedAgents.Length}");
            
            if (allNavAgents.Length > enhancedAgents.Length)
            {
                EditorGUILayout.HelpBox($"{allNavAgents.Length - enhancedAgents.Length} agents can be upgraded to Enhanced AI Agents", MessageType.Info);
                
                if (GUILayout.Button("Upgrade All NavMesh Agents"))
                {
                    UpgradeAllAgents();
                }
            }
            
            if (GUILayout.Button("Find and Fix Agent Issues"))
            {
                FindAndFixAgentIssues();
            }
        }

        private void DrawRuntimeInfo()
        {
            var system = EnhancedPathfindingSystem.Instance;
            
            if (system == null)
            {
                EditorGUILayout.HelpBox("Enhanced Pathfinding System not running!", MessageType.Error);
                return;
            }

            // System Information
            showSystemInfo = EditorGUILayout.Foldout(showSystemInfo, "System Information");
            if (showSystemInfo)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Registered Agents: {system.RegisteredAgentCount}");
                EditorGUILayout.LabelField($"Pending Requests: {system.PendingRequestCount}");
                EditorGUILayout.LabelField($"Cached Paths: {system.CachedPathCount}");
                EditorGUILayout.LabelField($"Total Paths Calculated: {system.TotalPathsCalculated}");
                EditorGUILayout.EndVertical();
            }

            // Agent List
            showAgentList = EditorGUILayout.Foldout(showAgentList, "Active Agents");
            if (showAgentList)
            {
                EditorGUILayout.BeginVertical("box");
                var agents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
                
                if (agents.Length == 0)
                {
                    EditorGUILayout.LabelField("No Enhanced AI Agents found");
                }
                else
                {
                    foreach (var agent in agents)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(agent.name, GUILayout.Width(150));
                        EditorGUILayout.LabelField($"Moving: {agent.IsMoving}", GUILayout.Width(80));
                        EditorGUILayout.LabelField($"Speed: {agent.CurrentSpeed:F1}", GUILayout.Width(80));
                        
                        if (GUILayout.Button("Select", GUILayout.Width(60)))
                        {
                            Selection.activeObject = agent.gameObject;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            // Performance Monitoring
            showPerformance = EditorGUILayout.Foldout(showPerformance, "Performance");
            if (showPerformance)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"FPS: {1f / Time.deltaTime:F1}");
                EditorGUILayout.LabelField($"Frame Time: {Time.deltaTime * 1000:F1}ms");
                
                if (GUILayout.Button("Run Performance Test"))
                {
                    RunPerformanceTest();
                }
                EditorGUILayout.EndVertical();
            }

            // Debug Tools
            showDebugTools = EditorGUILayout.Foldout(showDebugTools, "Debug Tools");
            if (showDebugTools)
            {
                EditorGUILayout.BeginVertical("box");
                
                if (GUILayout.Button("Clear All Agent Paths"))
                {
                    ClearAllAgentPaths();
                }
                
                if (GUILayout.Button("Force Path Recalculation"))
                {
                    ForcePathRecalculation();
                }
                
                if (GUILayout.Button("Test Random Movement"))
                {
                    TestRandomMovement();
                }
                
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEditorInfo()
        {
            EditorGUILayout.LabelField("Editor Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Enter Play Mode to see runtime information and performance metrics.", MessageType.Info);
            
            // Setup Tools
            EditorGUILayout.LabelField("Setup Tools", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Validate Scene Setup"))
            {
                ValidateSceneSetup();
            }
            
            if (GUILayout.Button("Create Test Scenario"))
            {
                CreateTestScenario();
            }
            
            EditorGUILayout.Space();
            
            // Test Configuration
            EditorGUILayout.LabelField("Test Configuration", EditorStyles.boldLabel);
            testAgentCount = EditorGUILayout.TextField("Test Agent Count:", testAgentCount);
            testDuration = EditorGUILayout.TextField("Test Duration (seconds):", testDuration);
        }

        private void CreatePathfindingSystem()
        {
            GameObject systemGO = new GameObject("Enhanced Pathfinding System");
            systemGO.AddComponent<EnhancedPathfindingSystem>();
            
            // Also add the setup script for easy access
            systemGO.AddComponent<Laboratory.AI.PathfindingSystemSetup>();
            
            EditorUtility.SetDirty(systemGO);
            Selection.activeObject = systemGO;
            
            Debug.Log("Enhanced Pathfinding System created!");
        }

        private void UpgradeAllAgents()
        {
            var allNavAgents = FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
            int upgradedCount = 0;
            
            foreach (var navAgent in allNavAgents)
            {
                if (navAgent.GetComponent<EnhancedAIAgent>() == null)
                {
                    var enhancedAgent = navAgent.gameObject.AddComponent<EnhancedAIAgent>();
                    ConfigureAgentType(enhancedAgent, navAgent);
                    upgradedCount++;
                    EditorUtility.SetDirty(navAgent.gameObject);
                }
            }
            
            Debug.Log($"Upgraded {upgradedCount} agents to Enhanced AI Agents!");
        }

        private void ConfigureAgentType(EnhancedAIAgent enhancedAgent, UnityEngine.AI.NavMeshAgent navAgent)
        {
            // Auto-configure based on NavMeshAgent properties
            if (navAgent.radius <= 0.4f)
            {
                enhancedAgent.SetAgentType(EnhancedAIAgent.AgentType.Small);
            }
            else if (navAgent.radius >= 0.8f)
            {
                enhancedAgent.SetAgentType(EnhancedAIAgent.AgentType.Large);
            }
            else
            {
                enhancedAgent.SetAgentType(EnhancedAIAgent.AgentType.Medium);
            }
            
            enhancedAgent.SetSpeed(navAgent.speed);
        }

        private void FindAndFixAgentIssues()
        {
            var agents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
            int fixedCount = 0;
            
            foreach (var agent in agents)
            {
                // Check for missing NavMeshAgent
                if (agent.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
                {
                    agent.gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>();
                    fixedCount++;
                    Debug.Log($"Added missing NavMeshAgent to {agent.name}");
                }
                
                // Check for missing Animator on AI controllers
                var chimeraAI = agent.GetComponent<EnhancedChimeraMonsterAI>();
                var enemyAI = agent.GetComponent<EnhancedEnemyController>();
                
                if ((chimeraAI != null || enemyAI != null) && agent.GetComponent<Animator>() == null)
                {
                    agent.gameObject.AddComponent<Animator>();
                    fixedCount++;
                    Debug.Log($"Added missing Animator to {agent.name}");
                }
                
                EditorUtility.SetDirty(agent.gameObject);
            }
            
            Debug.Log($"Fixed {fixedCount} agent issues!");
        }

        private void ValidateSceneSetup()
        {
            Debug.Log("=== Scene Setup Validation ===");
            
            // Check for Enhanced Pathfinding System
            var system = FindFirstObjectByType<EnhancedPathfindingSystem>();
            if (system == null)
            {
                Debug.LogWarning("❌ Enhanced Pathfinding System not found!");
            }
            else
            {
                Debug.Log("✅ Enhanced Pathfinding System found");
            }
            
            // Check NavMesh
            var navMeshData = UnityEngine.AI.NavMesh.CalculateTriangulation();
            if (navMeshData.vertices.Length == 0)
            {
                Debug.LogWarning("❌ No NavMesh found! Please bake NavMesh (Window → AI → Navigation)");
            }
            else
            {
                Debug.Log($"✅ NavMesh found with {navMeshData.vertices.Length} vertices");
            }
            
            // Check AI Agents
            var enhancedAgents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
            var navAgents = FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
            
            Debug.Log($"📊 Found {enhancedAgents.Length} Enhanced AI Agents");
            Debug.Log($"📊 Found {navAgents.Length} NavMesh Agents");
            
            if (navAgents.Length > enhancedAgents.Length)
            {
                Debug.LogWarning($"⚠️ {navAgents.Length - enhancedAgents.Length} NavMesh Agents can be upgraded");
            }
            
            // Check for player
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                Debug.LogWarning("❌ No GameObject with 'Player' tag found!");
            }
            else
            {
                Debug.Log("✅ Player found");
            }
            
            Debug.Log("=== Validation Complete ===");
        }

        private void CreateTestScenario()
        {
            // Create a simple test scenario with some AI agents
            GameObject testParent = new GameObject("Pathfinding Test Scenario");
            
            // Create test agents
            for (int i = 0; i < 5; i++)
            {
                GameObject agentGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                agentGO.name = $"Test Agent {i}";
                agentGO.transform.SetParent(testParent.transform);
                agentGO.transform.position = new Vector3(i * 3f, 0, 0);
                
                // Add components
                var navAgent = agentGO.AddComponent<UnityEngine.AI.NavMeshAgent>();
                var enhancedAgent = agentGO.AddComponent<EnhancedAIAgent>();
                var animator = agentGO.AddComponent<Animator>();
                
                // Configure
                navAgent.radius = 0.5f;
                navAgent.height = 2f;
                navAgent.speed = 3.5f;
                
                enhancedAgent.SetAgentType(EnhancedAIAgent.AgentType.Medium);
                
                EditorUtility.SetDirty(agentGO);
            }
            
            // Create target points
            GameObject targetParent = new GameObject("Target Points");
            targetParent.transform.SetParent(testParent.transform);
            
            for (int i = 0; i < 3; i++)
            {
                GameObject targetGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                targetGO.name = $"Target {i}";
                targetGO.transform.SetParent(targetParent.transform);
                targetGO.transform.position = new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-10f, 10f));
                targetGO.transform.localScale = Vector3.one * 0.5f;
                
                var renderer = targetGO.GetComponent<Renderer>();
                renderer.material.color = Color.red;
                
                EditorUtility.SetDirty(targetGO);
            }
            
            Selection.activeObject = testParent;
            Debug.Log("Test scenario created! Check the scene hierarchy.");
        }

        private void RunPerformanceTest()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Performance test can only be run in Play Mode!");
                return;
            }
            
            Debug.Log("Performance test started - check console for results...");
            
            var setupScript = FindFirstObjectByType<Laboratory.AI.PathfindingSystemSetup>();
            if (setupScript != null)
            {
                setupScript.TestPathfindingPerformance();
            }
            else
            {
                Debug.LogError("PathfindingSystemSetup script not found!");
            }
        }

        private void ClearAllAgentPaths()
        {
            var agents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
            foreach (var agent in agents)
            {
                agent.Stop();
            }
            Debug.Log($"Cleared paths for {agents.Length} agents");
        }

        private void ForcePathRecalculation()
        {
            var agents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
            foreach (var agent in agents)
            {
                if (agent.HasValidPath)
                {
                    // Force recalculation by setting destination again
                    Vector3 currentDest = agent.Destination;
                    agent.Stop();
                    agent.SetDestination(currentDest);
                }
            }
            Debug.Log($"Forced path recalculation for {agents.Length} agents");
        }

        private void TestRandomMovement()
        {
            var agents = FindObjectsByType<EnhancedAIAgent>(FindObjectsSortMode.None);
            foreach (var agent in agents)
            {
                Vector3 randomDest = agent.transform.position + Random.insideUnitSphere * 10f;
                randomDest.y = 0;
                agent.SetDestination(randomDest);
            }
            Debug.Log($"Set random destinations for {agents.Length} agents");
        }

        private void OnInspectorUpdate()
        {
            // Refresh the window periodically when in play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
