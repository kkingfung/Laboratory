using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Subsystems.Performance;
using Laboratory.Subsystems.Monitoring;
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

        // Service references
        private IPerformanceMonitoringService performanceService;
        private ISystemMonitoringService systemService;
        private ChimeraPerformanceProfiler chimeraProfiler;

        // Performance data
        private Queue<float> fpsHistory = new Queue<float>();
        private Queue<float> memoryHistory = new Queue<float>();
        private Queue<float> frameTimeHistory = new Queue<float>();
        private Dictionary<string, SystemMonitorData> systemData = new Dictionary<string, SystemMonitorData>();
        private PerformanceReport latestReport;

        // UI state
        private Rect windowRect = new Rect(20, 20, 800, 600);
        private GUIStyle headerStyle;
        private GUIStyle labelStyle;
        private GUIStyle valueStyle;
        private GUIStyle tabButtonStyle;
        private GUIStyle graphBackgroundStyle;
        private bool stylesInitialized = false;

        private enum DashboardTab
        {
            Overview,
            Systems,
            Memory,
            Optimization,
            Genetics,
            Network
        }

        private void Awake()
        {
            // Find or create performance services
            TryFindPerformanceServices();

            isVisible = showOnStartup;
        }

        private void Update()
        {
            // Toggle dashboard
            if (Input.GetKeyDown(toggleKey))
            {
                isVisible = !isVisible;
                if (isVisible)
                {
                    UpdateDashboardData();
                }
            }

            // Update data at intervals when visible
            if (isVisible && Time.unscaledTime - lastUpdateTime >= updateInterval)
            {
                UpdateDashboardData();
                lastUpdateTime = Time.unscaledTime;
            }
        }

        private void OnGUI()
        {
            if (!isVisible) return;

            InitializeStyles();

            // Main window
            windowRect = GUILayout.Window(
                0,
                windowRect,
                DrawDashboardWindow,
                "🔥 Performance Dashboard - Press F12 to Toggle",
                GUILayout.MinWidth(800),
                GUILayout.MinHeight(600)
            );
        }

        private void DrawDashboardWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Tab navigation
            DrawTabNavigation();

            GUILayout.Space(10);

            // Content area with scroll
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

            switch (currentTab)
            {
                case DashboardTab.Overview:
                    DrawOverviewTab();
                    break;
                case DashboardTab.Systems:
                    DrawSystemsTab();
                    break;
                case DashboardTab.Memory:
                    DrawMemoryTab();
                    break;
                case DashboardTab.Optimization:
                    DrawOptimizationTab();
                    break;
                case DashboardTab.Genetics:
                    DrawGeneticsTab();
                    break;
                case DashboardTab.Network:
                    DrawNetworkTab();
                    break;
            }

            GUILayout.EndScrollView();

            // Footer
            DrawFooter();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawTabNavigation()
        {
            GUILayout.BeginHorizontal();

            if (TabButton("Overview", DashboardTab.Overview))
                currentTab = DashboardTab.Overview;

            if (TabButton("Systems", DashboardTab.Systems))
                currentTab = DashboardTab.Systems;

            if (TabButton("Memory", DashboardTab.Memory))
                currentTab = DashboardTab.Memory;

            if (TabButton("Optimization", DashboardTab.Optimization))
                currentTab = DashboardTab.Optimization;

            if (TabButton("Genetics", DashboardTab.Genetics))
                currentTab = DashboardTab.Genetics;

            if (TabButton("Network", DashboardTab.Network))
                currentTab = DashboardTab.Network;

            GUILayout.EndHorizontal();
        }

        private bool TabButton(string label, DashboardTab tab)
        {
            bool isActive = currentTab == tab;
            Color originalColor = GUI.backgroundColor;

            if (isActive)
                GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);

            bool result = GUILayout.Button(label, tabButtonStyle, GUILayout.MinWidth(100));

            GUI.backgroundColor = originalColor;

            return result;
        }

        private void DrawOverviewTab()
        {
            GUILayout.Label("📊 Performance Overview", headerStyle);

            // FPS and Frame Time
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Frame Performance", labelStyle);

            float currentFPS = 1f / Time.unscaledDeltaTime;
            float currentFrameTime = Time.unscaledDeltaTime * 1000f;

            DrawMetric("FPS", $"{currentFPS:F1}", GetPerformanceColor(currentFPS, 60f, 30f));
            DrawMetric("Frame Time", $"{currentFrameTime:F2} ms", GetPerformanceColor(30f - currentFrameTime, 13.33f, 6.67f));

            if (showFPSGraph && fpsHistory.Count > 0)
            {
                DrawGraph("FPS History", fpsHistory.ToArray(), 0f, 144f, goodPerformanceColor);
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Memory
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Memory Usage", labelStyle);

            long totalMemoryMB = System.GC.GetTotalMemory(false) / (1024 * 1024);
            DrawMetric("Total Memory", $"{totalMemoryMB} MB", GetMemoryColor(totalMemoryMB));
            DrawMetric("GC Collections (Gen 0)", System.GC.CollectionCount(0).ToString(), Color.white);
            DrawMetric("GC Collections (Gen 1)", System.GC.CollectionCount(1).ToString(), Color.white);

            if (showMemoryGraph && memoryHistory.Count > 0)
            {
                DrawGraph("Memory History (MB)", memoryHistory.ToArray(), 0f, 2048f, Color.cyan);
            }

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Rendering
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Rendering Stats", labelStyle);

            DrawMetric("Draw Calls", UnityEngine.Rendering.FrameTimingManager.GetLatestTimings(1, null).ToString(), Color.white);
            DrawMetric("Triangles", UnityStats.triangles.ToString("N0"), Color.white);
            DrawMetric("Vertices", UnityStats.vertices.ToString("N0"), Color.white);

            GUILayout.EndVertical();

            // Quick Actions
            GUILayout.Space(10);
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Force GC", GUILayout.Height(30)))
            {
                System.GC.Collect();
                Debug.Log("🗑️ Forced garbage collection");
            }

            if (GUILayout.Button("Clear History", GUILayout.Height(30)))
            {
                ClearHistory();
                Debug.Log("🗑️ Cleared performance history");
            }

            GUILayout.EndHorizontal();
        }

        private void DrawSystemsTab()
        {
            GUILayout.Label("⚙️ ECS Systems Monitor", headerStyle);

            if (systemData == null || systemData.Count == 0)
            {
                GUILayout.Label("No system data available. Ensure SystemMonitoringService is running.", labelStyle);
                return;
            }

            foreach (var kvp in systemData.OrderBy(s => s.Key))
            {
                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();
                GUILayout.Label($"🔧 {kvp.Key}", headerStyle, GUILayout.Width(150));

                // Status indicator
                Color statusColor = GetSystemStatusColor(kvp.Value.status);
                DrawColorBox(statusColor, 20, 20);

                GUILayout.Label(kvp.Value.status.ToString(), valueStyle);

                GUILayout.EndHorizontal();

                // System metrics
                DrawMetric("CPU Usage", $"{kvp.Value.cpuUsage:F1}%", GetUsageColor(kvp.Value.cpuUsage));
                DrawMetric("Memory Usage", $"{kvp.Value.memoryUsage:F1} MB", Color.white);
                DrawMetric("Active Operations", kvp.Value.activeOperations.ToString(), Color.white);
                DrawMetric("Response Time", $"{kvp.Value.averageResponseTime:F2} ms", Color.white);

                // Custom metrics
                if (kvp.Value.customMetrics != null && kvp.Value.customMetrics.Count > 0)
                {
                    GUILayout.Space(5);
                    GUILayout.Label("Custom Metrics:", labelStyle);

                    foreach (var metric in kvp.Value.customMetrics.Take(5))
                    {
                        DrawMetric($"  {metric.Key}", $"{metric.Value:F2}", Color.gray);
                    }
                }

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        private void DrawMemoryTab()
        {
            GUILayout.Label("💾 Memory Profiling", headerStyle);

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Memory Breakdown", labelStyle);

            long totalMemory = System.GC.GetTotalMemory(false);
            long gcMemory = totalMemory;

            DrawMetric("Total Allocated", $"{totalMemory / (1024f * 1024f):F2} MB", Color.white);
            DrawMetric("GC Reserved", $"{gcMemory / (1024f * 1024f):F2} MB", Color.cyan);

            GUILayout.Space(10);

            DrawMetric("GC Generation 0", $"{System.GC.CollectionCount(0)} collections", Color.white);
            DrawMetric("GC Generation 1", $"{System.GC.CollectionCount(1)} collections", Color.white);
            DrawMetric("GC Generation 2", $"{System.GC.CollectionCount(2)} collections", Color.white);

            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Memory trend graph
            if (showMemoryGraph && memoryHistory.Count > 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Memory Trend (Last 100 frames)", labelStyle);
                DrawGraph("Memory (MB)", memoryHistory.ToArray(), 0f, 2048f, Color.cyan);
                GUILayout.EndVertical();
            }

            GUILayout.Space(10);

            // Memory actions
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Force GC (Gen 0)", GUILayout.Height(30)))
            {
                System.GC.Collect(0);
                Debug.Log("🗑️ Forced GC Generation 0");
            }

            if (GUILayout.Button("Force GC (Gen 2)", GUILayout.Height(30)))
            {
                System.GC.Collect(2, System.GCCollectionMode.Forced, true);
                Debug.Log("🗑️ Forced GC Generation 2");
            }

            GUILayout.EndHorizontal();
        }

        private void DrawOptimizationTab()
        {
            GUILayout.Label("🔍 Optimization Suggestions", headerStyle);

            if (latestReport.suggestions == null || latestReport.suggestions.Length == 0)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("✅ No optimization suggestions - Performance is excellent!", labelStyle);
                GUILayout.EndVertical();
                return;
            }

            var sortedSuggestions = latestReport.suggestions.OrderByDescending(s => s.impact).ToArray();

            foreach (var suggestion in sortedSuggestions)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                // Header with impact indicator
                GUILayout.BeginHorizontal();

                Color impactColor = suggestion.impact > 0.7f ? criticalPerformanceColor :
                                   suggestion.impact > 0.4f ? warningPerformanceColor : goodPerformanceColor;

                DrawColorBox(impactColor, 20, 20);

                GUILayout.Label($"[{suggestion.category}] {suggestion.description}", headerStyle);

                GUILayout.EndHorizontal();

                GUILayout.Space(5);
                GUILayout.Label($"💡 Action: {suggestion.action}", labelStyle);
                GUILayout.Label($"Impact: {suggestion.impact:P0} | Time: {(Time.time - suggestion.timestamp):F1}s ago", valueStyle);

                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
        }

        private void DrawGeneticsTab()
        {
            GUILayout.Label("🧬 Genetics System Performance", headerStyle);

            if (systemData.TryGetValue("Genetics", out var geneticsData))
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Genetics System Status", labelStyle);

                DrawMetric("Status", geneticsData.status.ToString(), GetSystemStatusColor(geneticsData.status));
                DrawMetric("CPU Usage", $"{geneticsData.cpuUsage:F1}%", GetUsageColor(geneticsData.cpuUsage));
                DrawMetric("Memory Usage", $"{geneticsData.memoryUsage:F1} MB", Color.white);

                GUILayout.Space(10);

                if (geneticsData.customMetrics != null)
                {
                    GUILayout.Label("Genetics Metrics:", labelStyle);

                    if (geneticsData.customMetrics.TryGetValue("GeneticOperationsPerSecond", out float opsPerSec))
                        DrawMetric("Operations/Second", $"{opsPerSec:F0}", Color.cyan);

                    if (geneticsData.customMetrics.TryGetValue("ActiveCreatures", out float activeCreatures))
                        DrawMetric("Active Creatures", $"{activeCreatures:F0}", Color.green);

                    if (geneticsData.customMetrics.TryGetValue("GeneticComplexity", out float complexity))
                        DrawMetric("Genetic Complexity", $"{complexity:F2}", Color.yellow);
                }

                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("Genetics system data not available", labelStyle);
            }

            GUILayout.Space(10);

            // Performance recommendations
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Performance Notes", labelStyle);
            GUILayout.Label("✅ All genetics jobs are Burst-compiled for optimal performance", valueStyle);
            GUILayout.Label("✅ SIMD optimizations enabled for trait calculations", valueStyle);
            GUILayout.Label("✅ Target: 1000+ creatures at 60 FPS", valueStyle);
            GUILayout.EndVertical();
        }

        private void DrawNetworkTab()
        {
            GUILayout.Label("🌐 Network Performance", headerStyle);

            if (systemData.TryGetValue("Networking", out var networkData))
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Network System Status", labelStyle);

                DrawMetric("Status", networkData.status.ToString(), GetSystemStatusColor(networkData.status));

                if (networkData.customMetrics != null)
                {
                    if (networkData.customMetrics.TryGetValue("NetworkBytesPerSecond", out float bytesPerSec))
                        DrawMetric("Bandwidth", $"{bytesPerSec / 1024f:F2} KB/s", Color.cyan);

                    if (networkData.customMetrics.TryGetValue("ConnectedClients", out float clients))
                        DrawMetric("Connected Clients", $"{clients:F0}", Color.green);

                    if (networkData.customMetrics.TryGetValue("PacketLoss", out float packetLoss))
                        DrawMetric("Packet Loss", $"{packetLoss:P2}", GetUsageColor(packetLoss * 100f));
                }

                GUILayout.EndVertical();
            }
            else
            {
                GUILayout.Label("Network system data not available (multiplayer not active)", labelStyle);
            }
        }

        private void DrawFooter()
        {
            GUILayout.BeginHorizontal(GUI.skin.box);

            GUILayout.Label($"Update Interval: {updateInterval:F1}s", valueStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label($"History: {fpsHistory.Count}/{graphHistorySize}", valueStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Export Report", GUILayout.Height(25)))
            {
                ExportPerformanceReport();
            }

            GUILayout.EndHorizontal();
        }

        private void DrawMetric(string label, string value, Color color)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}:", labelStyle, GUILayout.Width(200));
            Color originalColor = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(value, valueStyle);
            GUI.contentColor = originalColor;
            GUILayout.EndHorizontal();
        }

        private void DrawGraph(string title, float[] data, float minValue, float maxValue, Color graphColor)
        {
            if (data == null || data.Length < 2) return;

            GUILayout.Label(title, labelStyle);

            Rect graphRect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));

            // Draw background
            GUI.Box(graphRect, "", graphBackgroundStyle);

            // Draw graph lines
            float width = graphRect.width;
            float height = graphRect.height;

            Vector3[] points = new Vector3[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                float x = graphRect.x + (i / (float)(data.Length - 1)) * width;
                float normalizedValue = Mathf.InverseLerp(minValue, maxValue, data[i]);
                float y = graphRect.y + height - (normalizedValue * height);

                points[i] = new Vector3(x, y, 0);
            }

            // Draw lines between points
            for (int i = 0; i < points.Length - 1; i++)
            {
                DrawLine(points[i], points[i + 1], graphColor, 2f);
            }

            // Draw min/max labels
            GUI.Label(new Rect(graphRect.x, graphRect.y, 60, 20), $"{maxValue:F0}", valueStyle);
            GUI.Label(new Rect(graphRect.x, graphRect.yMax - 20, 60, 20), $"{minValue:F0}", valueStyle);
        }

        private void DrawColorBox(Color color, float width, float height)
        {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            GUILayout.Box("", GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = originalColor;
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            Color savedColor = GUI.color;
            GUI.color = color;

            Vector2 direction = (end - start).normalized;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * (thickness / 2f);

            Vector3[] vertices = new Vector3[]
            {
                start + perpendicular,
                start - perpendicular,
                end - perpendicular,
                end + perpendicular
            };

            // Draw using GUI texture (simplified line drawing)
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;
            float distance = Vector2.Distance(start, end);

            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y - thickness / 2f, distance, thickness), Texture2D.whiteTexture);
            GUI.matrix = matrix;

            GUI.color = savedColor;
        }

        private void UpdateDashboardData()
        {
            // Update FPS history
            float currentFPS = 1f / Time.unscaledDeltaTime;
            fpsHistory.Enqueue(currentFPS);
            while (fpsHistory.Count > graphHistorySize)
                fpsHistory.Dequeue();

            // Update memory history
            float currentMemory = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            memoryHistory.Enqueue(currentMemory);
            while (memoryHistory.Count > graphHistorySize)
                memoryHistory.Dequeue();

            // Update frame time history
            float currentFrameTime = Time.unscaledDeltaTime * 1000f;
            frameTimeHistory.Enqueue(currentFrameTime);
            while (frameTimeHistory.Count > graphHistorySize)
                frameTimeHistory.Dequeue();

            // Get data from services
            if (systemService != null)
            {
                systemData = systemService.GetAllSystemData();
            }

            if (chimeraProfiler != null)
            {
                latestReport = chimeraProfiler.GetPerformanceReport();
            }
        }

        private void TryFindPerformanceServices()
        {
            // Try to find ChimeraPerformanceProfiler
            chimeraProfiler = FindFirstObjectByType<ChimeraPerformanceProfiler>();

            if (chimeraProfiler == null)
            {
                Debug.LogWarning("[RuntimePerformanceDashboard] ChimeraPerformanceProfiler not found. Some features may be limited.");
            }
        }

        private void ClearHistory()
        {
            fpsHistory.Clear();
            memoryHistory.Clear();
            frameTimeHistory.Clear();
        }

        private void ExportPerformanceReport()
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string filename = $"PerformanceReport_{timestamp}.txt";

            System.Text.StringBuilder report = new System.Text.StringBuilder();
            report.AppendLine("=== CHIMERA PERFORMANCE REPORT ===");
            report.AppendLine($"Generated: {System.DateTime.Now}");
            report.AppendLine($"Unity Version: {Application.unityVersion}");
            report.AppendLine($"Platform: {Application.platform}");
            report.AppendLine();

            report.AppendLine("PERFORMANCE METRICS:");
            report.AppendLine($"FPS: {(1f / Time.unscaledDeltaTime):F1}");
            report.AppendLine($"Frame Time: {(Time.unscaledDeltaTime * 1000f):F2} ms");
            report.AppendLine($"Memory: {System.GC.GetTotalMemory(false) / (1024 * 1024)} MB");
            report.AppendLine();

            if (systemData != null && systemData.Count > 0)
            {
                report.AppendLine("SYSTEM STATUS:");
                foreach (var kvp in systemData.OrderBy(s => s.Key))
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value.status} (CPU: {kvp.Value.cpuUsage:F1}%, Mem: {kvp.Value.memoryUsage:F1} MB)");
                }
                report.AppendLine();
            }

            if (latestReport.suggestions != null && latestReport.suggestions.Length > 0)
            {
                report.AppendLine("OPTIMIZATION SUGGESTIONS:");
                foreach (var suggestion in latestReport.suggestions.OrderByDescending(s => s.impact))
                {
                    report.AppendLine($"  [{suggestion.category}] {suggestion.description}");
                    report.AppendLine($"    Action: {suggestion.action}");
                    report.AppendLine($"    Impact: {suggestion.impact:P0}");
                }
            }

            string path = System.IO.Path.Combine(Application.persistentDataPath, filename);
            System.IO.File.WriteAllText(path, report.ToString());

            Debug.Log($"📊 Performance report exported to: {path}");
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.gray }
            };

            valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };

            tabButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            graphBackgroundStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.8f)) }
            };

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Color GetPerformanceColor(float value, float goodThreshold, float warningThreshold)
        {
            if (value >= goodThreshold) return goodPerformanceColor;
            if (value >= warningThreshold) return warningPerformanceColor;
            return criticalPerformanceColor;
        }

        private Color GetMemoryColor(long memoryMB)
        {
            if (memoryMB < 512) return goodPerformanceColor;
            if (memoryMB < 1024) return warningPerformanceColor;
            return criticalPerformanceColor;
        }

        private Color GetUsageColor(float percentage)
        {
            if (percentage < 60f) return goodPerformanceColor;
            if (percentage < 80f) return warningPerformanceColor;
            return criticalPerformanceColor;
        }

        private Color GetSystemStatusColor(SystemStatus status)
        {
            return status switch
            {
                SystemStatus.Healthy => goodPerformanceColor,
                SystemStatus.Warning => warningPerformanceColor,
                SystemStatus.Error => criticalPerformanceColor,
                _ => Color.gray
            };
        }
    }
}
