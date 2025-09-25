using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Entities;
using Laboratory.Chimera.Breeding;
using Laboratory.Chimera.Genetics;
using Laboratory.Core.Systems;

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
        private BreedingSystem breedingSystem;
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

            // Try to find breeding system
            breedingSystem = FindFirstObjectByType<BreedingSystem>();

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

            // Mock genetic data - replace with actual system data when available
            int totalCreatures = GetTotalCreatureCount();
            int breedingPairs = GetActiveBreedingPairs();
            float avgFitness = GetAverageFitness();
            int generations = GetMaxGeneration();

            geneticInfo += $"Total Creatures: <color=yellow>{totalCreatures}</color>\n";
            geneticInfo += $"Active Breeding: <color=cyan>{breedingPairs}</color>\n";
            geneticInfo += $"Avg Fitness: <color=green>{avgFitness:F2}</color>\n";
            geneticInfo += $"Max Generation: <color=orange>{generations}</color>\n";
            geneticInfo += $"Genetic Diversity: <color=purple>{CalculateGeneticDiversity():F2}</color>\n";

            // Show recent breeding events
            geneticInfo += $"\n<b>RECENT BREEDING EVENTS:</b>\n";
            for (int i = 0; i < Mathf.Min(3, logBuffer.Count); i++)
            {
                if (logBuffer[logBuffer.Count - 1 - i].Contains("Breeding"))
                {
                    geneticInfo += $"<color=green>• {logBuffer[logBuffer.Count - 1 - i]}</color>\n";
                }
            }

            geneticDataText.text = geneticInfo;
        }

        private void UpdateSystemHealthInfo()
        {
            if (systemHealthText == null) return;

            string healthInfo = $"<b>SYSTEM HEALTH</b>\n";

            // System status indicators
            healthInfo += GetSystemStatus("ECS World", entityManager != null);
            healthInfo += GetSystemStatus("Breeding System", breedingSystem != null);
            healthInfo += GetSystemStatus("Audio System", AudioListener.allAudioListeners.Length > 0);

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

            if (entityManager != null && entityManager.IsCreated)
            {
                // Get entity statistics
                var allEntities = entityManager.GetAllEntities();
                ecsInfo += $"Total Entities: <color=yellow>{allEntities.Length}</color>\n";
                allEntities.Dispose();

                // Mock system performance data
                ecsInfo += $"Active Systems: <color=cyan>{GetActiveSystemCount()}</color>\n";
                ecsInfo += $"Entities/Frame: <color=green>{CalculateEntitiesPerFrame():F1}</color>\n";
                ecsInfo += $"System Update Time: <color=orange>{GetSystemUpdateTime():F2}ms</color>\n";
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

        // Mock data methods - replace with actual system calls
        private int GetTotalCreatureCount()
        {
            // TODO: Get from actual creature management system
            return Random.Range(10, 50);
        }

        private int GetActiveBreedingPairs()
        {
            // TODO: Get from breeding system
            return Random.Range(0, 5);
        }

        private float GetAverageFitness()
        {
            // TODO: Calculate from genetic system
            return Random.Range(0.3f, 0.9f);
        }

        private int GetMaxGeneration()
        {
            // TODO: Get from genetic system
            return Random.Range(1, 20);
        }

        private float CalculateGeneticDiversity()
        {
            // TODO: Calculate actual genetic diversity
            return Random.Range(0.2f, 0.8f);
        }

        private int GetActiveSystemCount()
        {
            // TODO: Get actual ECS system count
            return Random.Range(5, 15);
        }

        private float CalculateEntitiesPerFrame()
        {
            // TODO: Calculate actual entities processed per frame
            return Random.Range(50f, 500f);
        }

        private float GetSystemUpdateTime()
        {
            // TODO: Get actual system update time
            return Random.Range(0.5f, 3f);
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