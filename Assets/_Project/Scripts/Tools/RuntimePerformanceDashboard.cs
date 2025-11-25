using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Performance;

namespace Laboratory.Tools
{
    /// <summary>
    /// Runtime performance profiling dashboard with real-time metrics visualization
    /// Integrates with existing performance monitoring services for comprehensive profiling
    /// Works in both editor and runtime builds - toggle with F12
    /// </summary>
    public class RuntimePerformanceDashboard : MonoBehaviour
    {
        [Header("Dashboard Settings")]
        [SerializeField] private KeyCode toggleKey = KeyCode.F12;
        [SerializeField] private bool showOnStartup = false;
        [SerializeField] private float updateInterval = 0.5f;

        [Header("Display Settings")]
        [SerializeField] private bool showFPSGraph = true;
        [SerializeField] private bool showMemoryGraph = true;
        [SerializeField] private int graphHistorySize = 100;
        [SerializeField] private Color goodPerformanceColor = Color.green;
        [SerializeField] private Color warningPerformanceColor = Color.yellow;
        [SerializeField] private Color criticalPerformanceColor = Color.red;

        // Dashboard state
        private bool isVisible = false;
        private DashboardTab currentTab = DashboardTab.Overview;
        private Vector2 scrollPosition;
        private float lastUpdateTime = 0f;

        // Performance data
        private Queue<float> fpsHistory = new Queue<float>();
        private Queue<float> memoryHistory = new Queue<float>();
        private Queue<float> frameTimeHistory = new Queue<float>();
        private Dictionary<string, float> systemPerformance = new Dictionary<string, float>();

        // UI state
        private Rect windowRect = new Rect(20, 20, 800, 600);
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle tabButtonStyle;

        private enum DashboardTab
        {
            Overview,
            Performance,
            Memory,
            Systems
        }

        private void Awake()
        {
            if (showOnStartup)
                isVisible = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
            }

            if (isVisible && Time.time - lastUpdateTime > updateInterval)
            {
                UpdatePerformanceData();
                lastUpdateTime = Time.time;
            }
        }

        private void UpdatePerformanceData()
        {
            // Update FPS
            float fps = 1f / Time.unscaledDeltaTime;
            fpsHistory.Enqueue(fps);
            if (fpsHistory.Count > graphHistorySize)
                fpsHistory.Dequeue();

            // Update memory
            float memoryMB = System.GC.GetTotalMemory(false) / 1024f / 1024f;
            memoryHistory.Enqueue(memoryMB);
            if (memoryHistory.Count > graphHistorySize)
                memoryHistory.Dequeue();

            // Update frame time
            float frameTime = Time.unscaledDeltaTime * 1000f; // Convert to ms
            frameTimeHistory.Enqueue(frameTime);
            if (frameTimeHistory.Count > graphHistorySize)
                frameTimeHistory.Dequeue();
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            InitializeStyles();
            windowRect = GUI.Window(0, windowRect, DrawDashboard, "Performance Dashboard", headerStyle);
        }

        private void InitializeStyles()
        {
            if (headerStyle == null)
            {
                headerStyle = new GUIStyle(GUI.skin.window);
                headerStyle.fontSize = 14;
                headerStyle.fontStyle = FontStyle.Bold;

                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.fontSize = 12;

                valueStyle = new GUIStyle(GUI.skin.label);
                valueStyle.fontSize = 12;
                valueStyle.fontStyle = FontStyle.Bold;

                tabButtonStyle = new GUIStyle(GUI.skin.button);
                tabButtonStyle.fontSize = 11;
            }
        }

        private void DrawDashboard(int windowID)
        {
            GUILayout.BeginVertical();

            // Tab buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Overview", tabButtonStyle))
                currentTab = DashboardTab.Overview;
            if (GUILayout.Button("Performance", tabButtonStyle))
                currentTab = DashboardTab.Performance;
            if (GUILayout.Button("Memory", tabButtonStyle))
                currentTab = DashboardTab.Memory;
            if (GUILayout.Button("Systems", tabButtonStyle))
                currentTab = DashboardTab.Systems;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            switch (currentTab)
            {
                case DashboardTab.Overview:
                    DrawOverviewTab();
                    break;
                case DashboardTab.Performance:
                    DrawPerformanceTab();
                    break;
                case DashboardTab.Memory:
                    DrawMemoryTab();
                    break;
                case DashboardTab.Systems:
                    DrawSystemsTab();
                    break;
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        private void DrawOverviewTab()
        {
            GUILayout.Label("Performance Overview", headerStyle);
            GUILayout.Space(5);

            // Current FPS
            float currentFPS = fpsHistory.Count > 0 ? fpsHistory.Last() : 0f;
            Color fpsColor = GetPerformanceColor(currentFPS, 30f, 60f);
            GUI.color = fpsColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("FPS:", labelStyle);
            GUILayout.Label($"{currentFPS:F1}", valueStyle);
            GUILayout.EndHorizontal();
            GUI.color = Color.white;

            // Frame time
            float currentFrameTime = frameTimeHistory.Count > 0 ? frameTimeHistory.Last() : 0f;
            Color frameTimeColor = GetPerformanceColor(33.3f - currentFrameTime, 0f, 16.6f); // Inverted scale
            GUI.color = frameTimeColor;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Frame Time:", labelStyle);
            GUILayout.Label($"{currentFrameTime:F2} ms", valueStyle);
            GUILayout.EndHorizontal();
            GUI.color = Color.white;

            // Memory
            float currentMemory = memoryHistory.Count > 0 ? memoryHistory.Last() : 0f;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Memory:", labelStyle);
            GUILayout.Label($"{currentMemory:F1} MB", valueStyle);
            GUILayout.EndHorizontal();
        }

        private void DrawPerformanceTab()
        {
            GUILayout.Label("Performance Metrics", headerStyle);

            if (fpsHistory.Count > 0)
            {
                float avgFPS = fpsHistory.Average();
                float minFPS = fpsHistory.Min();
                float maxFPS = fpsHistory.Max();

                GUILayout.Label($"FPS - Avg: {avgFPS:F1}, Min: {minFPS:F1}, Max: {maxFPS:F1}");
            }

            if (frameTimeHistory.Count > 0)
            {
                float avgFrameTime = frameTimeHistory.Average();
                float minFrameTime = frameTimeHistory.Min();
                float maxFrameTime = frameTimeHistory.Max();

                GUILayout.Label($"Frame Time - Avg: {avgFrameTime:F2}ms, Min: {minFrameTime:F2}ms, Max: {maxFrameTime:F2}ms");
            }

            // Simple performance graph representation
            GUILayout.Label("FPS Graph (last 20 samples):");
            DrawSimpleGraph(fpsHistory.TakeLast(20).ToArray(), 0f, 120f);
        }

        private void DrawMemoryTab()
        {
            GUILayout.Label("Memory Usage", headerStyle);

            if (memoryHistory.Count > 0)
            {
                float currentMemory = memoryHistory.Last();
                float avgMemory = memoryHistory.Average();
                float maxMemory = memoryHistory.Max();

                GUILayout.Label($"Current: {currentMemory:F1} MB");
                GUILayout.Label($"Average: {avgMemory:F1} MB");
                GUILayout.Label($"Peak: {maxMemory:F1} MB");
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Force Garbage Collection"))
            {
                System.GC.Collect();
            }
        }

        private void DrawSystemsTab()
        {
            GUILayout.Label("System Performance", headerStyle);

            GUILayout.Label("Active GameObjects: " + FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length);
            GUILayout.Label("Time Scale: " + Time.timeScale);
            GUILayout.Label("Target Frame Rate: " + Application.targetFrameRate);
            GUILayout.Label("VSync Count: " + QualitySettings.vSyncCount);
            GUILayout.Label("Quality Level: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }

        private void DrawSimpleGraph(float[] values, float minValue, float maxValue)
        {
            if (values.Length == 0) return;

            GUILayout.BeginHorizontal();
            foreach (float value in values)
            {
                float normalizedValue = Mathf.Clamp01((value - minValue) / (maxValue - minValue));
                Color barColor = GetPerformanceColor(value, 30f, 60f);

                GUI.color = barColor;
                GUILayout.Box("", GUILayout.Width(10), GUILayout.Height(normalizedValue * 50f + 5f));
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();
        }

        private Color GetPerformanceColor(float value, float warningThreshold, float goodThreshold)
        {
            if (value >= goodThreshold)
                return goodPerformanceColor;
            else if (value >= warningThreshold)
                return warningPerformanceColor;
            else
                return criticalPerformanceColor;
        }
    }
}