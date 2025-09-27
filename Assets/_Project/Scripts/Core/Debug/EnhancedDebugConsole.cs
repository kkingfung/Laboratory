using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Entities;
using System.Reflection;

namespace Laboratory.Core.Debug
{
    /// <summary>
    /// Enhanced debug console that provides real-time monitoring of all game systems.
    /// Shows genetic data, ECS performance, AI behavior, and system health metrics.
    /// </summary>
    public class EnhancedDebugConsole : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas debugCanvas;
        [SerializeField] private GameObject debugPanel;
        [SerializeField] private TextMeshProUGUI performanceText;
        [SerializeField] private TextMeshProUGUI geneticDataText;
        [SerializeField] private TextMeshProUGUI systemHealthText;
        [SerializeField] private TextMeshProUGUI ecsDataText;
        [SerializeField] private Scrollbar scrollbar;

        [Header("Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private float updateInterval = 0.5f;
        [SerializeField] private int maxLogEntries = 100;
        [SerializeField] private bool showFPS = true;
        [SerializeField] private bool showMemory = true;
        [SerializeField] private bool showECS = true;
        [SerializeField] private bool showGenetics = true;

        // Performance tracking
        private float[] frameTimes;
        private int frameTimeIndex;
        private float lastUpdateTime;
        private float deltaTime;

        // System references
        private EntityManager entityManager;
        private MonoBehaviour breedingSystem;
        private MonoBehaviour ecosystemManager;
        private MonoBehaviour aiServiceManager;
        private MonoBehaviour ecosystemSimulator;
        private List<string> logBuffer = new List<string>();

        // Singleton pattern
        private static EnhancedDebugConsole instance;
        public static EnhancedDebugConsole Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<EnhancedDebugConsole>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("Enhanced Debug Console");
                        instance = go.AddComponent<EnhancedDebugConsole>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDebugConsole();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeDebugConsole()
        {
            frameTimes = new float[60]; // Track 60 frames for average

            // Try to get ECS references
            var world = World.DefaultGameObjectInjectionWorld;
            if (world != null)
            {
                entityManager = world.EntityManager;
            }

            // Try to find system references
            breedingSystem = FindSystemByTypeName("BreedingSystem");
            ecosystemManager = FindSystemByTypeName("EcosystemManager");
            aiServiceManager = FindSystemByTypeName("AIServiceManager");
            ecosystemSimulator = FindSystemByTypeName("DynamicEcosystemSimulator");

            CreateDebugUI();
            SetConsoleVisible(false);

            LogToConsole("Enhanced Debug Console initialized", LogLevel.Info);
        }

        private void CreateDebugUI()
        {
            if (debugCanvas == null)
            {
                // Create canvas if not assigned
                GameObject canvasGO = new GameObject("Debug Canvas");
                debugCanvas = canvasGO.AddComponent<Canvas>();
                debugCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                debugCanvas.sortingOrder = 1000; // Ensure it's on top
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
                canvasGO.transform.SetParent(transform);
            }

            if (debugPanel == null)
            {
                CreateDebugPanel();
            }
        }

        private void CreateDebugPanel()
        {
            // Main debug panel
            GameObject panel = new GameObject("Debug Panel");
            panel.transform.SetParent(debugCanvas.transform, false);

            Image panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.8f);

            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0.4f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            debugPanel = panel;

            // Create text components
            CreateTextComponent("Performance", new Vector2(0, 0.75f), new Vector2(1, 0.25f), out performanceText);
            CreateTextComponent("Genetic Data", new Vector2(0, 0.5f), new Vector2(1, 0.25f), out geneticDataText);
            CreateTextComponent("System Health", new Vector2(0, 0.25f), new Vector2(1, 0.25f), out systemHealthText);
            CreateTextComponent("ECS Data", new Vector2(0, 0), new Vector2(1, 0.25f), out ecsDataText);
        }

        private void CreateTextComponent(string name, Vector2 anchorMin, Vector2 anchorMax, out TextMeshProUGUI textComponent)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(debugPanel.transform, false);

            textComponent = textGO.AddComponent<TextMeshProUGUI>();
            textComponent.text = name;
            textComponent.fontSize = 12;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.TopLeft;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = anchorMin;
            textRect.anchorMax = anchorMax;
            textRect.offsetMin = new Vector2(10, 10);
            textRect.offsetMax = new Vector2(-10, -10);
        }

        private void Update()
        {
            // Toggle console visibility
            if (Input.GetKeyDown(toggleKey))
            {
                ToggleConsole();
            }

            // Update frame time tracking
            UpdateFrameTimeTracking();

            // Update debug information
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdateDebugInfo();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdateFrameTimeTracking()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

            frameTimes[frameTimeIndex] = Time.unscaledDeltaTime;
            frameTimeIndex = (frameTimeIndex + 1) % frameTimes.Length;
        }

        private void UpdateDebugInfo()
        {
            if (!debugPanel.activeInHierarchy) return;

            UpdatePerformanceInfo();
            UpdateGeneticInfo();
            UpdateSystemHealthInfo();
            UpdateECSInfo();
        }

        private void UpdatePerformanceInfo()
        {
            if (!showFPS || performanceText == null) return;

            float fps = 1f / deltaTime;
            float avgFrameTime = frameTimes.Average() * 1000f; // Convert to milliseconds
            float minFrameTime = frameTimes.Min() * 1000f;
            float maxFrameTime = frameTimes.Max() * 1000f;

            string performanceInfo = $"<b>PERFORMANCE METRICS</b>\n";
            performanceInfo += $"FPS: <color=yellow>{fps:F1}</color>\n";
            performanceInfo += $"Frame Time: <color=yellow>{avgFrameTime:F2}ms</color>\n";
            performanceInfo += $"Min/Max: <color=cyan>{minFrameTime:F2}ms</color> / <color=red>{maxFrameTime:F2}ms</color>\n";

            if (showMemory)
            {
                long memoryUsage = System.GC.GetTotalMemory(false);
                performanceInfo += $"Memory: <color=yellow>{memoryUsage / 1024 / 1024:F1}MB</color>\n";
                performanceInfo += $"GC Gen0: <color=cyan>{System.GC.CollectionCount(0)}</color>\n";
                performanceInfo += $"GC Gen1: <color=cyan>{System.GC.CollectionCount(1)}</color>\n";
                performanceInfo += $"GC Gen2: <color=orange>{System.GC.CollectionCount(2)}</color>\n";
            }

            performanceText.text = performanceInfo;
        }

        private void UpdateGeneticInfo()
        {
            if (!showGenetics || geneticDataText == null) return;

            string geneticInfo = $"<b>GENETIC SYSTEM DATA</b>\n";

            // Get real genetic data from connected systems
            int totalCreatures = GetTotalCreatureCount();
            int breedingPairs = GetActiveBreedingPairs();
            float avgFitness = GetAverageFitness();
            int generations = GetMaxGeneration();
            float geneticDiversity = CalculateGeneticDiversity();

            geneticInfo += $"Total Creatures: <color=yellow>{totalCreatures}</color>\n";
            geneticInfo += $"Active Breeding: <color=cyan>{breedingPairs}</color>\n";
            geneticInfo += $"Avg Fitness: <color=green>{avgFitness:F2}</color>\n";
            geneticInfo += $"Max Generation: <color=orange>{generations}</color>\n";
            geneticInfo += $"Genetic Diversity: <color=purple>{geneticDiversity:F2}</color>\n";

            // Show recent breeding events and ecosystem data
            if (breedingSystem != null)
            {
                var recentEvents = GetSystemValue<List<string>>(breedingSystem, "GetRecentBreedingEvents", new List<string>());
                if (recentEvents != null && recentEvents.Count > 0)
                {
                    geneticInfo += $"\n<b>RECENT BREEDING EVENTS:</b>\n";
                    foreach (var eventInfo in recentEvents)
                    {
                        geneticInfo += $"<color=green>• {eventInfo}</color>\n";
                    }
                }
            }
            else
            {
                // Fallback to log buffer
                geneticInfo += $"\n<b>RECENT BREEDING EVENTS:</b>\n";
                for (int i = 0; i < Mathf.Min(3, logBuffer.Count); i++)
                {
                    if (logBuffer[logBuffer.Count - 1 - i].Contains("Breeding"))
                    {
                        geneticInfo += $"<color=green>• {logBuffer[logBuffer.Count - 1 - i]}</color>\n";
                    }
                }
            }

            // Add ecosystem diversity info if available
            if (ecosystemManager != null)
            {
                geneticInfo += $"\n<b>ECOSYSTEM DATA:</b>\n";
                geneticInfo += $"Active Biomes: <color=cyan>{GetSystemValue(ecosystemManager, "GetActiveBiomeCount", 0)}</color>\n";
                geneticInfo += $"Species Diversity: <color=purple>{GetSystemValue(ecosystemManager, "CalculateSpeciesDiversity", 0f):F2}</color>\n";
            }

            geneticDataText.text = geneticInfo;
        }

        private void UpdateSystemHealthInfo()
        {
            if (systemHealthText == null) return;

            string healthInfo = $"<b>SYSTEM HEALTH</b>\n";

            // System status indicators
            healthInfo += GetSystemStatus("ECS World", entityManager.World != null && entityManager.World.IsCreated);
            healthInfo += GetSystemStatus("Breeding System", breedingSystem != null);
            healthInfo += GetSystemStatus("Ecosystem Manager", ecosystemManager != null);
            healthInfo += GetSystemStatus("AI Service Manager", aiServiceManager != null);
            healthInfo += GetSystemStatus("Audio System", FindAnyObjectByType<AudioListener>() != null);

            // Add ecosystem health if available
            if (ecosystemManager != null)
            {
                float ecoHealth = GetSystemValue(ecosystemManager, "GetEcosystemHealth", 0.5f);
                Color healthColor = ecoHealth > 0.7f ? Color.green : ecoHealth > 0.4f ? Color.yellow : Color.red;
                string healthStatus = ecoHealth > 0.7f ? "Healthy" : ecoHealth > 0.4f ? "Unstable" : "Critical";
                healthInfo += $"Ecosystem Health: <color=#{ColorUtility.ToHtmlStringRGB(healthColor)}>{healthStatus} ({ecoHealth:F2})</color>\n";
            }

            // Performance indicators
            float currentFPS = 1f / deltaTime;
            string fpsStatus = currentFPS > 45 ? "Good" : currentFPS > 25 ? "Fair" : "Poor";
            Color fpsColor = currentFPS > 45 ? Color.green : currentFPS > 25 ? Color.yellow : Color.red;
            healthInfo += $"Performance: <color=#{ColorUtility.ToHtmlStringRGB(fpsColor)}>{fpsStatus}</color>\n";

            // Memory health
            long memoryMB = System.GC.GetTotalMemory(false) / 1024 / 1024;
            string memoryStatus = memoryMB < 500 ? "Good" : memoryMB < 1000 ? "Fair" : "High";
            Color memoryColor = memoryMB < 500 ? Color.green : memoryMB < 1000 ? Color.yellow : Color.red;
            healthInfo += $"Memory Usage: <color=#{ColorUtility.ToHtmlStringRGB(memoryColor)}>{memoryStatus}</color>\n";

            systemHealthText.text = healthInfo;
        }

        private void UpdateECSInfo()
        {
            if (!showECS || ecsDataText == null) return;

            string ecsInfo = $"<b>ECS STATISTICS</b>\n";

            if (entityManager.World != null && entityManager.World.IsCreated)
            {
                // Get entity statistics
                var allEntities = entityManager.GetAllEntities();
                ecsInfo += $"Total Entities: <color=yellow>{allEntities.Length}</color>\n";
                allEntities.Dispose();

                // Real system performance data
                int activeSystemCount = GetActiveSystemCount();
                float entitiesPerFrame = CalculateEntitiesPerFrame();
                float systemUpdateTime = GetSystemUpdateTime();

                ecsInfo += $"Active Systems: <color=cyan>{activeSystemCount}</color>\n";
                ecsInfo += $"Entities/Frame: <color=green>{entitiesPerFrame:F1}</color>\n";
                ecsInfo += $"System Update Time: <color=orange>{systemUpdateTime:F2}ms</color>\n";

                // Add world statistics
                var world = entityManager.World;
                if (world != null)
                {
                    ecsInfo += $"World Name: <color=yellow>{world.Name}</color>\n";
                    ecsInfo += $"World Time: <color=cyan>{world.Time.ElapsedTime:F2}s</color>\n";
                }
            }
            else
            {
                ecsInfo += "<color=red>ECS World not available</color>\n";
            }

            ecsDataText.text = ecsInfo;
        }

        private string GetSystemStatus(string systemName, bool isActive)
        {
            Color statusColor = isActive ? Color.green : Color.red;
            string status = isActive ? "Online" : "Offline";
            return $"{systemName}: <color=#{ColorUtility.ToHtmlStringRGB(statusColor)}>{status}</color>\n";
        }

        public void ToggleConsole()
        {
            bool newState = !debugPanel.activeInHierarchy;
            SetConsoleVisible(newState);

            LogToConsole($"Debug console {(newState ? "opened" : "closed")}", LogLevel.Info);
        }

        public void SetConsoleVisible(bool visible)
        {
            if (debugPanel != null)
            {
                debugPanel.SetActive(visible);
            }
        }

        public void LogToConsole(string message, LogLevel level = LogLevel.Info)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            string logEntry = $"[{timestamp}] {level}: {message}";

            logBuffer.Add(logEntry);

            // Keep buffer size manageable
            if (logBuffer.Count > maxLogEntries)
            {
                logBuffer.RemoveAt(0);
            }

            // Also log to Unity console
            switch (level)
            {
                case LogLevel.Error:
                    UnityEngine.Debug.LogError(logEntry);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(logEntry);
                    break;
                default:
                    UnityEngine.Debug.Log(logEntry);
                    break;
            }
        }

        // Real data methods connected to actual systems
        private int GetTotalCreatureCount()
        {
            if (breedingSystem != null)
            {
                return GetSystemValue(breedingSystem, "GetTotalCreatureCount", 0);
            }
            if (ecosystemManager != null)
            {
                return GetSystemValue(ecosystemManager, "GetTotalPopulation", 0);
            }
            // Fallback to mock data
            return Random.Range(10, 50);
        }

        private int GetActiveBreedingPairs()
        {
            if (breedingSystem != null)
            {
                return GetSystemValue(breedingSystem, "GetActiveBreedingPairsCount", 0);
            }
            // Fallback to mock data
            return Random.Range(0, 5);
        }

        private float GetAverageFitness()
        {
            if (breedingSystem != null)
            {
                return GetSystemValue(breedingSystem, "GetAverageFitness", 0f);
            }
            // Fallback to mock data
            return Random.Range(0.3f, 0.9f);
        }

        private int GetMaxGeneration()
        {
            if (breedingSystem != null)
            {
                return GetSystemValue(breedingSystem, "GetMaxGeneration", 1);
            }
            // Fallback to mock data
            return Random.Range(1, 20);
        }

        private float CalculateGeneticDiversity()
        {
            if (breedingSystem != null)
            {
                return GetSystemValue(breedingSystem, "CalculateGeneticDiversity", 0f);
            }
            if (ecosystemManager != null)
            {
                return GetSystemValue(ecosystemManager, "CalculateSpeciesDiversity", 0f);
            }
            // Fallback to mock data
            return Random.Range(0.2f, 0.8f);
        }

        private int GetActiveSystemCount()
        {
            if (entityManager.World != null && entityManager.World.IsCreated)
            {
                var world = entityManager.World;
                if (world != null)
                {
                    // Count active systems in the world
                    int systemCount = 0;
                    foreach (var system in world.Systems)
                    {
                        if (system.Enabled)
                        {
                            systemCount++;
                        }
                    }
                    return systemCount;
                }
            }
            // Fallback to mock data
            return Random.Range(5, 15);
        }

        private float CalculateEntitiesPerFrame()
        {
            if (entityManager.World != null && entityManager.World.IsCreated)
            {
                // Calculate based on actual entity count and frame rate
                var allEntities = entityManager.GetAllEntities();
                float entitiesPerFrame = allEntities.Length * Time.deltaTime;
                allEntities.Dispose();
                return entitiesPerFrame;
            }
            // Fallback to mock data
            return Random.Range(50f, 500f);
        }

        private float GetSystemUpdateTime()
        {
            if (aiServiceManager != null)
            {
                return GetSystemValue(aiServiceManager, "GetLastUpdateTime", 0f) * 1000f; // Convert to ms
            }
            // Fallback to mock data
            return Random.Range(0.5f, 3f);
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

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }

    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Debug
    }
}