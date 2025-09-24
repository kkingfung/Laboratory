using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.AI.Pathfinding;

namespace Laboratory.AI.Utilities
{
    /// <summary>
    /// Performance profiler integration for the Enhanced Pathfinding System
    /// Provides detailed performance metrics and Unity Profiler integration
    /// </summary>
    public class PathfindingProfiler : MonoBehaviour
    {
        [Header("🔍 Pathfinding Performance Monitor")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool showDebugGUI = true;
        [SerializeField] private int maxRecordedFrames = 300;
        [SerializeField] private float updateInterval = 1f;
        
        [Header("📊 Statistics")]
        [SerializeField] private bool trackPathRequests = true;
        [SerializeField] private bool trackPathCalculations = true;
        [SerializeField] private bool trackMemoryUsage = true;
        
        // Performance tracking
        private List<float> pathCalculationTimes = new List<float>();
        private List<int> activeAgentCounts = new List<int>();
        private List<float> memoryUsageHistory = new List<float>();
        
        // Current frame stats
        private int pathRequestsThisFrame = 0;
        private int pathsCalculatedThisFrame = 0;
        private float averagePathTime = 0f;
        private int totalActiveAgents = 0;
        
        // Timing
        private float lastUpdateTime = 0f;
        
        void Start()
        {
            if (enableProfiling)
            {
                InvokeRepeating(nameof(UpdateStatistics), 0f, updateInterval);
                Debug.Log("✅ PathfindingProfiler started monitoring performance");
            }
        }
        
        void Update()
        {
            if (enableProfiling)
            {
                CollectFrameData();
            }
        }
        
        private void CollectFrameData()
        {
            var potentialAgents = FindObjectsOfType<MonoBehaviour>()
                .Where(mb => mb is IPathfindingAgent)
                .Count();
                
            totalActiveAgents = potentialAgents;
            
            // Track memory usage
            if (trackMemoryUsage)
            {
                float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
                memoryUsageHistory.Add(memoryMB);
                
                if (memoryUsageHistory.Count > maxRecordedFrames)
                {
                    memoryUsageHistory.RemoveAt(0);
                }
            }
        }
        
        private void UpdateStatistics()
        {
            // Calculate averages
            if (pathCalculationTimes.Count > 0)
            {
                averagePathTime = pathCalculationTimes.Average();
            }
            
            // Store active agent count
            activeAgentCounts.Add(totalActiveAgents);
            if (activeAgentCounts.Count > maxRecordedFrames)
            {
                activeAgentCounts.RemoveAt(0);
            }
            
            // Reset frame counters
            pathRequestsThisFrame = 0;
            pathsCalculatedThisFrame = 0;
        }
        
        // Public methods for external systems to report performance data
        public void ReportPathCalculationTime(float timeMs)
        {
            if (!enableProfiling) return;
            
            pathCalculationTimes.Add(timeMs);
            if (pathCalculationTimes.Count > maxRecordedFrames)
            {
                pathCalculationTimes.RemoveAt(0);
            }
            
            pathsCalculatedThisFrame++;
        }
        
        public void ReportPathRequest()
        {
            if (!enableProfiling) return;
            pathRequestsThisFrame++;
        }
        
        void OnGUI()
        {
            if (!showDebugGUI || !enableProfiling) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUIStyle boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label("🔍 Pathfinding Performance", boldStyle);
            GUILayout.Space(5);
            
            GUILayout.Label($"Active Agents: {totalActiveAgents}");
            GUILayout.Label($"Path Requests/Frame: {pathRequestsThisFrame}");
            GUILayout.Label($"Paths Calculated/Frame: {pathsCalculatedThisFrame}");
            GUILayout.Label($"Avg Path Time: {averagePathTime:F2}ms");
            
            if (memoryUsageHistory.Count > 0)
            {
                float currentMemory = memoryUsageHistory.LastOrDefault();
                GUILayout.Label($"Memory Usage: {currentMemory:F1}MB");
            }
            
            GUILayout.Space(5);
            if (GUILayout.Button("Clear Stats"))
            {
                ClearStatistics();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        public void ClearStatistics()
        {
            pathCalculationTimes.Clear();
            activeAgentCounts.Clear();
            memoryUsageHistory.Clear();
            Debug.Log("🧹 PathfindingProfiler statistics cleared");
        }
        
        // Get performance data for external systems
        public Dictionary<string, object> GetPerformanceData()
        {
            var data = new Dictionary<string, object>();
            
            data["activeAgents"] = totalActiveAgents;
            data["averagePathTime"] = averagePathTime;
            data["pathRequestsPerFrame"] = pathRequestsThisFrame;
            data["pathsCalculatedPerFrame"] = pathsCalculatedThisFrame;
            
            if (memoryUsageHistory.Count > 0)
            {
                data["currentMemoryMB"] = memoryUsageHistory.Last();
                data["averageMemoryMB"] = memoryUsageHistory.Average();
            }
            
            return data;
        }
        
        #if UNITY_EDITOR
        [UnityEditor.MenuItem("🐲 Chimera/Laboratory Fix/Create PathfindingProfiler")]
        public static void CreatePathfindingProfiler()
        {
            GameObject profilerGO = new GameObject("PathfindingProfiler");
            profilerGO.AddComponent<PathfindingProfiler>();
            Debug.Log("✅ Created PathfindingProfiler GameObject");
        }
        #endif
    }
}
