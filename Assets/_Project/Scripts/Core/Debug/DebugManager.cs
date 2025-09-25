using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Core.Debug
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
            if (!enableMemoryTracking) return;

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

            // TODO: Get actual ECS metrics
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            if (world != null && world.EntityManager.IsCreated)
            {
                var allEntities = world.EntityManager.GetAllEntities();
                SetDebugData("ECS.EntityCount", allEntities.Length);
                allEntities.Dispose();
            }
        }

        private void UpdateGeneticsMetrics()
        {
            if (!showGenetics) return;

            // TODO: Connect to actual genetics system
            SetDebugData("Genetics.TotalCreatures", GetTotalCreatureCount());
            SetDebugData("Genetics.ActiveBreeding", GetActiveBreedingCount());
            SetDebugData("Genetics.AverageFitness", GetAverageFitness());
        }

        private void UpdateAIMetrics()
        {
            if (!showAI) return;

            // TODO: Connect to actual AI system
            SetDebugData("AI.ActiveAgents", GetActiveAIAgentCount());
            SetDebugData("AI.PathfindingRequests", GetPathfindingRequestCount());
            SetDebugData("AI.BehaviorTreeUpdates", GetBehaviorTreeUpdateCount());
        }

        private void UpdateNetworkingMetrics()
        {
            if (!showNetworking) return;

            // TODO: Connect to actual networking system
            SetDebugData("Network.ConnectedPlayers", GetConnectedPlayerCount());
            SetDebugData("Network.PacketsPerSecond", GetPacketsPerSecond());
            SetDebugData("Network.Latency", GetNetworkLatency());
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

        // Mock data methods - replace with actual system calls
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
        [UnityEditor.MenuItem("Laboratory/Debug/Toggle Debug Console")]
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

        [UnityEditor.MenuItem("Laboratory/Debug/Create Debug Manager")]
        private static void CreateDebugManager()
        {
            GameObject debugManagerGO = new GameObject("Debug Manager");
            debugManagerGO.AddComponent<DebugManager>();
            UnityEditor.Selection.activeGameObject = debugManagerGO;
            UnityEngine.Debug.Log("Debug Manager created and selected");
        }
    }
}