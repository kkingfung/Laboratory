using UnityEngine;
using System.Collections.Generic;
using Unity.Entities;
using System.Reflection;

namespace Laboratory.Core.Diagnostics
{
    /// <summary>
    /// Debug manager that initializes and manages all debug systems.
    /// Provides easy access to debug functionality throughout the game.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Execute early
    public class DebugManager : MonoBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField] private bool enableDebugConsole = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private bool enableMemoryTracking = true;
        [SerializeField] private KeyCode debugToggleKey = KeyCode.F12;

        [Header("Debug Categories")]
        [SerializeField] private bool showFPS = true;
        [SerializeField] private bool showMemory = true;
        [SerializeField] private bool showECS = true;
        [SerializeField] private bool showGenetics = true;
        [SerializeField] private bool showAI = true;
        [SerializeField] private bool showNetworking = true;

        // Static reference for easy access
        private static DebugManager instance;
        public static DebugManager Instance => instance;

        // Debug systems
        private EnhancedDebugConsole debugConsole;
        private Dictionary<string, object> debugData = new Dictionary<string, object>();

        // System references
        private EntityManager entityManager;
        private MonoBehaviour breedingSystem;
        private MonoBehaviour ecosystemManager;
        private MonoBehaviour aiServiceManager;
        private MonoBehaviour ecosystemSimulator;
        private MonoBehaviour networkManager;
        private MonoBehaviour pathfindingSystem;

        // Performance tracking
        private float lastMemoryCheck;
        private float memoryCheckInterval = 5f; // Check memory every 5 seconds

        private void Awake()
        {
            // Singleton pattern
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDebugSystems();
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void InitializeDebugSystems()
        {
            UnityEngine.Debug.Log("🔧 Initializing Debug Systems...");

            // Initialize system references
            InitializeSystemReferences();

            // Initialize debug console
            if (enableDebugConsole)
            {
                InitializeDebugConsole();
            }

            // Initialize performance monitoring
            if (enablePerformanceMonitoring)
            {
                InitializePerformanceMonitoring();
            }

            // Register for application events
            Application.logMessageReceived += HandleLogMessage;

            LogDebug("Debug Manager initialized successfully");
        }

        private void InitializeSystemReferences()
        {
            // Get ECS references
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                entityManager = world.EntityManager;
            }

            // Find system references
            breedingSystem = FindSystemByTypeName("BreedingSystem");
            ecosystemManager = FindSystemByTypeName("EcosystemManager");
            aiServiceManager = FindSystemByTypeName("AIServiceManager");
            ecosystemSimulator = FindSystemByTypeName("DynamicEcosystemSimulator");
            networkManager = FindSystemByTypeName("NetworkingSystems");
            pathfindingSystem = FindSystemByTypeName("EnhancedPathfindingSystem");
        }

        private void InitializeDebugConsole()
        {
            debugConsole = EnhancedDebugConsole.Instance;
            if (debugConsole != null)
            {
                LogDebug("Enhanced Debug Console initialized");
            }
        }

        private void InitializePerformanceMonitoring()
        {
            // Start coroutine for continuous performance monitoring
            InvokeRepeating(nameof(UpdatePerformanceMetrics), 1f, 1f);
            LogDebug("Performance monitoring started");
        }

        private void UpdatePerformanceMetrics()
        {
            if (!enablePerformanceMonitoring) return;

            // Update memory tracking
            if (Time.time - lastMemoryCheck >= memoryCheckInterval)
            {
                UpdateMemoryMetrics();
                lastMemoryCheck = Time.time;
            }

            // Update other performance metrics
            UpdateFPSMetrics();
            UpdateSystemMetrics();
        }

        private void UpdateMemoryMetrics()
        {
            if (!enableMemoryTracking || !showMemory) return;

            long totalMemory = System.GC.GetTotalMemory(false);
            SetDebugData("Memory.Total", totalMemory);
            SetDebugData("Memory.TotalMB", totalMemory / 1024 / 1024);

            // Check for memory spikes
            if (totalMemory > 512 * 1024 * 1024) // 512MB threshold
            {
                LogWarning($"High memory usage detected: {totalMemory / 1024 / 1024}MB");
            }
        }

        private void UpdateFPSMetrics()
        {
            if (!showFPS) return;

            float fps = 1f / Time.unscaledDeltaTime;
            SetDebugData("Performance.FPS", fps);

            // Check for performance issues
            if (fps < 30f)
            {
                LogWarning($"Low FPS detected: {fps:F1}");
            }
        }

        private void UpdateSystemMetrics()
        {
            // Update system-specific metrics
            UpdateECSMetrics();
            UpdateGeneticsMetrics();
            UpdateAIMetrics();
            UpdateNetworkingMetrics();
        }

        private void UpdateECSMetrics()
        {
            if (!showECS) return;

            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world != null && world.IsCreated)
            {
                var entityManager = world.EntityManager;

                // Basic entity counts
                using (var allEntities = entityManager.GetAllEntities(Unity.Collections.Allocator.TempJob))
                {
                    SetDebugData("ECS.EntityCount", allEntities.Length);
                }

                // Creature-specific metrics
                var creatureQuery = entityManager.CreateEntityQuery(
                    Unity.Entities.ComponentType.ReadOnly<Laboratory.Core.ECS.CreatureData>(),
                    Unity.Entities.ComponentType.ReadOnly<Laboratory.Core.ECS.CreatureSimulationTag>()
                );

                if (!creatureQuery.IsEmpty)
                {
                    SetDebugData("ECS.CreatureEntities", creatureQuery.CalculateEntityCount());

                    // Count alive vs dead creatures
                    using (var creatureData = creatureQuery.ToComponentDataArray<Laboratory.Core.ECS.CreatureData>(Unity.Collections.Allocator.TempJob))
                    {
                        int aliveCount = 0;
                        int deadCount = 0;
                        int totalGenerations = 0;
                        float totalAge = 0f;

                        for (int i = 0; i < creatureData.Length; i++)
                        {
                            var creature = creatureData[i];
                            if (creature.isAlive)
                            {
                                aliveCount++;
                                totalAge += creature.age;
                            }
                            else
                            {
                                deadCount++;
                            }
                            totalGenerations += creature.generation;
                        }

                        SetDebugData("ECS.CreaturesAlive", aliveCount);
                        SetDebugData("ECS.CreaturesDead", deadCount);
                        SetDebugData("ECS.AverageAge", aliveCount > 0 ? totalAge / aliveCount : 0f);
                        SetDebugData("ECS.AverageGeneration", creatureData.Length > 0 ? (float)totalGenerations / creatureData.Length : 0f);
                    }
                }

                creatureQuery.Dispose();

                // AI-enabled creature metrics - removed due to component cleanup
                // var aiQuery = entityManager.CreateEntityQuery(
                //     Unity.Entities.ComponentType.ReadOnly<Laboratory.Core.ECS.CreatureAIComponent>()
                // );

                // AI component access removed due to assembly separation
                // Core assembly cannot access Chimera AI components
                SetDebugData("ECS.AIEntities", "N/A - Assembly Limitation");

                // AI state counting disabled due to assembly limitations
                // Previously counted AI states but Chimera components are not accessible from Core

                // System performance metrics
                var systemCount = world.Systems.Count;
                SetDebugData("ECS.SystemCount", systemCount);

                // Memory usage approximation
                long approximateMemory = 0;

                // Estimate based on entity count and average component size
                using (var allEntities = entityManager.GetAllEntities(Unity.Collections.Allocator.TempJob))
                {
                    // Rough estimate: 64 bytes per entity (very approximate)
                    approximateMemory = allEntities.Length * 64;
                }

                SetDebugData("ECS.EstimatedMemoryKB", approximateMemory / 1024);

                // World tick information
                SetDebugData("ECS.WorldTime", world.Time.ElapsedTime);
                SetDebugData("ECS.DeltaTime", world.Time.DeltaTime);

                // Check for system health
                bool allSystemsHealthy = true;
                foreach (var system in world.Systems)
                {
                    if (!system.Enabled)
                    {
                        allSystemsHealthy = false;
                        break;
                    }
                }
                SetDebugData("ECS.SystemsHealthy", allSystemsHealthy);

                // Performance warnings
                using (var allEntities = entityManager.GetAllEntities(Unity.Collections.Allocator.TempJob))
                {
                    if (allEntities.Length > 10000)
                    {
                        LogWarning($"High entity count detected: {allEntities.Length}. Consider performance optimization.");
                    }
                }
            }
            else
            {
                // No ECS world available
                SetDebugData("ECS.EntityCount", 0);
                SetDebugData("ECS.CreatureEntities", 0);
                SetDebugData("ECS.AIEntities", 0);
                SetDebugData("ECS.SystemCount", 0);
                SetDebugData("ECS.WorldTime", 0f);
                SetDebugData("ECS.SystemsHealthy", false);
            }
        }

        private void UpdateGeneticsMetrics()
        {
            if (!showGenetics) return;

            // Connect to actual genetics/breeding system
            if (breedingSystem != null)
            {
                SetDebugData("Genetics.TotalCreatures", GetSystemValue(breedingSystem, "GetTotalCreatureCount", 0));
                SetDebugData("Genetics.ActiveBreeding", GetSystemValue(breedingSystem, "GetActiveBreedingPairsCount", 0));
                SetDebugData("Genetics.AverageFitness", GetSystemValue(breedingSystem, "GetAverageFitness", 0f));
                SetDebugData("Genetics.MaxGeneration", GetSystemValue(breedingSystem, "GetMaxGeneration", 1));
            }
            else
            {
                // Fallback to mock data if system not available
                SetDebugData("Genetics.TotalCreatures", GetTotalCreatureCount());
                SetDebugData("Genetics.ActiveBreeding", GetActiveBreedingCount());
                SetDebugData("Genetics.AverageFitness", GetAverageFitness());
            }

            // Add ecosystem data if available
            if (ecosystemManager != null)
            {
                SetDebugData("Ecosystem.TotalPopulation", GetSystemValue(ecosystemManager, "GetTotalPopulation", 0));
                SetDebugData("Ecosystem.ActiveBiomes", GetSystemValue(ecosystemManager, "GetActiveBiomeCount", 0));
            }
        }

        private void UpdateAIMetrics()
        {
            if (!showAI) return;

            // Connect to actual AI system
            if (aiServiceManager != null)
            {
                SetDebugData("AI.ActiveAgents", GetSystemValue(aiServiceManager, "GetActiveAgentCount", 0));
                SetDebugData("AI.PathfindingRequests", GetSystemValue(aiServiceManager, "GetPathfindingRequestCount", 0));
                SetDebugData("AI.BehaviorTreeUpdates", GetSystemValue(aiServiceManager, "GetBehaviorTreeUpdateCount", 0));
                SetDebugData("AI.ServiceHealth", GetSystemValue(aiServiceManager, "GetServiceHealthStatus", "Unknown"));
            }
            else
            {
                // Check for pathfinding system separately
                if (pathfindingSystem != null)
                {
                    SetDebugData("AI.PathfindingRequests", GetSystemValue(pathfindingSystem, "GetActiveRequestCount", 0));
                    SetDebugData("AI.PathfindingCacheHits", GetSystemValue(pathfindingSystem, "GetCacheHitRate", 0f));
                }

                // Fallback to mock data
                SetDebugData("AI.ActiveAgents", GetActiveAIAgentCount());
                SetDebugData("AI.BehaviorTreeUpdates", GetBehaviorTreeUpdateCount());
            }
        }

        private void UpdateNetworkingMetrics()
        {
            if (!showNetworking) return;

            // Connect to actual networking system
            if (networkManager != null)
            {
                SetDebugData("Network.ConnectedPlayers", GetSystemValue(networkManager, "GetConnectedPlayerCount", 0));
                SetDebugData("Network.PacketsPerSecond", GetSystemValue(networkManager, "GetPacketsPerSecond", 0f));
                SetDebugData("Network.Latency", GetSystemValue(networkManager, "GetAverageLatency", 0f));
                SetDebugData("Network.BandwidthUsage", GetSystemValue(networkManager, "GetBandwidthUsage", 0f));
            }
            else
            {
                // Fallback to mock data
                SetDebugData("Network.ConnectedPlayers", GetConnectedPlayerCount());
                SetDebugData("Network.PacketsPerSecond", GetPacketsPerSecond());
                SetDebugData("Network.Latency", GetNetworkLatency());
            }
        }

        private void HandleLogMessage(string logString, string stackTrace, LogType type)
        {
            // Forward important logs to debug console
            if (debugConsole != null)
            {
                LogLevel level = type switch
                {
                    LogType.Error => LogLevel.Error,
                    LogType.Exception => LogLevel.Error,
                    LogType.Warning => LogLevel.Warning,
                    _ => LogLevel.Info
                };

                // Only forward errors and warnings to avoid spam
                if (level != LogLevel.Info)
                {
                    debugConsole.LogToConsole(logString, level);
                }
            }
        }

        // Public API for other systems to log debug information
        public static void LogDebug(string message)
        {
            Instance?.debugConsole?.LogToConsole(message, LogLevel.Debug);
        }

        public static void LogInfo(string message)
        {
            Instance?.debugConsole?.LogToConsole(message, LogLevel.Info);
        }

        public static void LogWarning(string message)
        {
            Instance?.debugConsole?.LogToConsole(message, LogLevel.Warning);
        }

        public static void LogError(string message)
        {
            Instance?.debugConsole?.LogToConsole(message, LogLevel.Error);
        }

        public static void SetDebugData(string key, object value)
        {
            if (Instance?.debugData != null)
            {
                Instance.debugData[key] = value;
            }
        }

        public static T GetDebugData<T>(string key, T defaultValue = default)
        {
            if (Instance?.debugData != null && Instance.debugData.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        // Genetics data getter methods for DebugConsoleEditorWindow
        public static int GetCurrentGeneration()
        {
            return GetDebugData<int>("Genetics.CurrentGeneration", 1);
        }

        public static float GetGeneticDiversity()
        {
            return GetDebugData<float>("Genetics.OverallDiversity", 0f);
        }

        // Helper methods for dynamic system access
        private MonoBehaviour FindSystemByTypeName(string typeName)
        {
            var allObjects = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                if (obj.GetType().Name == typeName)
                {
                    return obj;
                }
            }
            return null;
        }

        private T GetSystemValue<T>(MonoBehaviour system, string methodName, T defaultValue)
        {
            if (system == null) return defaultValue;

            try
            {
                var method = system.GetType().GetMethod(methodName);
                if (method != null)
                {
                    var result = method.Invoke(system, null);
                    if (result is T)
                        return (T)result;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogWarning($"Failed to get {methodName} from {system.GetType().Name}: {ex.Message}");
            }

            return defaultValue;
        }

        // Mock data methods - fallbacks when systems aren't available
        private int GetTotalCreatureCount() => Random.Range(10, 100);
        private int GetActiveBreedingCount() => Random.Range(0, 10);
        private float GetAverageFitness() => Random.Range(0.3f, 0.9f);
        private int GetActiveAIAgentCount() => Random.Range(5, 50);
        private int GetPathfindingRequestCount() => Random.Range(0, 20);
        private int GetBehaviorTreeUpdateCount() => Random.Range(10, 100);
        private int GetConnectedPlayerCount() => Random.Range(1, 10);
        private float GetPacketsPerSecond() => Random.Range(10f, 100f);
        private float GetNetworkLatency() => Random.Range(10f, 200f);

        private void Update()
        {
            // Handle debug toggle key
            if (Input.GetKeyDown(debugToggleKey))
            {
                debugConsole?.ToggleConsole();
            }
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= HandleLogMessage;

            if (instance == this)
            {
                instance = null;
            }
        }

        // Menu item for quick access
        [UnityEditor.MenuItem("🧪 Laboratory/Debug/Toggle Debug Console")]
        private static void ToggleDebugConsole()
        {
            if (Application.isPlaying && Instance?.debugConsole != null)
            {
                Instance.debugConsole.ToggleConsole();
            }
            else
            {
                UnityEngine.Debug.LogWarning("Debug console is only available during play mode");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Debug/Create Debug Manager")]
        private static void CreateDebugManager()
        {
            GameObject debugManagerGO = new GameObject("Debug Manager");
            debugManagerGO.AddComponent<DebugManager>();
            UnityEditor.Selection.activeGameObject = debugManagerGO;
            UnityEngine.Debug.Log("Debug Manager created and selected");
        }
    }
}