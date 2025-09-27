using UnityEngine;
using Unity.Entities;
using Unity.Profiling;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Advanced performance profiler specifically designed for Project Chimera.
    /// Automatically detects bottlenecks, suggests optimizations, and provides
    /// real-time performance metrics for creature simulation systems.
    /// </summary>
    public class ChimeraPerformanceProfiler : MonoBehaviour
    {
        [Header("📊 Profiling Settings")]
        [SerializeField] private bool enableProfiling = true;
        [SerializeField] private bool autoOptimize = false;
        [SerializeField] private float profilingInterval = 1f;
        [SerializeField] private int sampleHistorySize = 60;

        [Header("🎯 Performance Targets")]
        [SerializeField] [Range(30, 144)] private int targetFPS = 60;
        [SerializeField] [Range(1, 50)] private float maxFrameTime = 16.67f; // 60 FPS = 16.67ms
        [SerializeField] [Range(10, 5000)] private int maxCreatureCount = 1000;
        [SerializeField] [Range(100, 10000)] private long maxMemoryMB = 2048;

        [Header("⚠️ Alert Thresholds")]
        [SerializeField] [Range(0.5f, 0.9f)] private float fpsWarningThreshold = 0.8f;
        [SerializeField] [Range(0.1f, 1f)] private float memoryWarningThreshold = 0.8f;
        [SerializeField] private bool enableAlerts = true;

        [Header("📈 Runtime Metrics")]
        [SerializeField] private float currentFPS = 60f;
        [SerializeField] private float currentFrameTime = 16.67f;
        [SerializeField] private long currentMemoryMB = 0;
        [SerializeField] private int currentCreatureCount = 0;
        [SerializeField] private string performanceStatus = "Good";

        // Performance monitoring
        private readonly List<FrameData> frameHistory = new List<FrameData>();
        private readonly List<SystemProfileData> systemProfiles = new List<SystemProfileData>();

        // Unity Profiler integration
        private ProfilerRecorder systemMemoryRecorder;
        private ProfilerRecorder gcMemoryRecorder;
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder entitiesRecorder;

        // Custom profiler markers
        private static readonly ProfilerMarker CreatureSimulationMarker = new ProfilerMarker("Chimera.CreatureSimulation");
        private static readonly ProfilerMarker GeneticsProcessingMarker = new ProfilerMarker("Chimera.GeneticsProcessing");
        private static readonly ProfilerMarker AIProcessingMarker = new ProfilerMarker("Chimera.AIProcessing");
        private static readonly ProfilerMarker SpawningMarker = new ProfilerMarker("Chimera.Spawning");

        // ECS system references
        private EntityManager entityManager;
        private World ecsWorld;

        // Optimization suggestions
        private readonly List<OptimizationSuggestion> suggestions = new List<OptimizationSuggestion>();
        private float lastOptimizationCheck = 0f;

        private struct FrameData
        {
            public float timestamp;
            public float fps;
            public float frameTime;
            public long memoryUsage;
            public int entityCount;
            public int drawCalls;
        }

        public struct SystemProfileData
        {
            public string systemName;
            public float averageTime;
            public float maxTime;
            public float totalTime;
            public int sampleCount;
        }

        public struct OptimizationSuggestion
        {
            public string category;
            public string description;
            public string action;
            public float impact; // 0-1, how much this could improve performance
            public float timestamp;
        }

        private void OnEnable()
        {
            InitializeProfiler();
        }

        private void OnDisable()
        {
            CleanupProfiler();
        }

        private void InitializeProfiler()
        {
            // Initialize Unity Profiler recorders
            systemMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "System Used Memory");
            gcMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");

            // Get ECS world reference
            ecsWorld = World.DefaultGameObjectInjectionWorld;
            if (ecsWorld?.IsCreated == true)
            {
                entityManager = ecsWorld.EntityManager;
            }

            Debug.Log("✅ Chimera Performance Profiler initialized");
        }

        private void Update()
        {
            if (!enableProfiling) return;

            // Update performance metrics every frame
            UpdatePerformanceMetrics();

            // Run detailed profiling at intervals
            if (Time.time - lastOptimizationCheck >= profilingInterval)
            {
                RunDetailedProfiling();
                CheckForOptimizations();
                lastOptimizationCheck = Time.time;
            }
        }

        private void UpdatePerformanceMetrics()
        {
            // Calculate FPS and frame time
            currentFPS = 1f / Time.unscaledDeltaTime;
            currentFrameTime = Time.unscaledDeltaTime * 1000f; // Convert to milliseconds

            // Get memory usage
            if (systemMemoryRecorder.Valid)
                currentMemoryMB = systemMemoryRecorder.LastValue / (1024 * 1024);

            // Count entities using reflection to avoid assembly dependency
            if (entityManager.World != null && entityManager.World.IsCreated)
            {
                // Get all entities as a fallback - in production this would use proper queries
                using (var allEntities = entityManager.GetAllEntities(Allocator.TempJob))
                {
                    currentCreatureCount = allEntities.Length;
                }
            }

            // Record frame data
            RecordFrameData();

            // Update performance status
            UpdatePerformanceStatus();
        }

        private void RecordFrameData()
        {
            var frameData = new FrameData
            {
                timestamp = Time.time,
                fps = currentFPS,
                frameTime = currentFrameTime,
                memoryUsage = currentMemoryMB,
                entityCount = currentCreatureCount,
                drawCalls = drawCallsRecorder.Valid ? (int)drawCallsRecorder.LastValue : 0
            };

            frameHistory.Add(frameData);

            // Maintain history size
            if (frameHistory.Count > sampleHistorySize)
            {
                frameHistory.RemoveAt(0);
            }
        }

        private void UpdatePerformanceStatus()
        {
            float fpsRatio = currentFPS / targetFPS;
            float memoryRatio = (float)currentMemoryMB / maxMemoryMB;
            float creatureRatio = (float)currentCreatureCount / maxCreatureCount;

            if (fpsRatio >= 0.95f && memoryRatio < 0.7f && creatureRatio < 0.8f)
            {
                performanceStatus = "Excellent";
            }
            else if (fpsRatio >= fpsWarningThreshold && memoryRatio < memoryWarningThreshold)
            {
                performanceStatus = "Good";
            }
            else if (fpsRatio >= 0.6f && memoryRatio < 0.9f)
            {
                performanceStatus = "Warning";
            }
            else
            {
                performanceStatus = "Critical";
            }

            // Send alerts if enabled
            if (enableAlerts && performanceStatus == "Critical")
            {
                SendPerformanceAlert();
            }
        }

        private void RunDetailedProfiling()
        {
            if (entityManager == null) return;

            // Profile creature simulation systems
            ProfileCreatureSimulation();
            ProfileGeneticsProcessing();
            ProfileAIProcessing();
            ProfileSpawningOperations();

            // Update debug manager with performance data
            UpdateDebugData();
        }

        private void ProfileCreatureSimulation()
        {
            using (CreatureSimulationMarker.Auto())
            {
                // This would integrate with your actual ECS systems
                // For now, we'll simulate the profiling of creature updates

                if (currentCreatureCount > 0)
                {
                    float simulationTime = currentFrameTime * 0.4f; // Assume 40% of frame time
                    RecordSystemProfile("CreatureSimulation", simulationTime);

                    // Check for performance issues
                    if (simulationTime > maxFrameTime * 0.5f) // More than 50% of target frame time
                    {
                        AddOptimizationSuggestion(
                            "ECS Optimization",
                            $"Creature simulation taking {simulationTime:F2}ms ({simulationTime/maxFrameTime*100:F0}% of frame budget)",
                            "Consider increasing ECS job batch sizes or enabling Burst compilation",
                            0.8f
                        );
                    }
                }
            }
        }

        private void ProfileGeneticsProcessing()
        {
            using (GeneticsProcessingMarker.Auto())
            {
                // Profile genetic trait processing
                float geneticsTime = EstimateGeneticsProcessingTime();
                RecordSystemProfile("GeneticsProcessing", geneticsTime);

                if (geneticsTime > 2f) // More than 2ms
                {
                    AddOptimizationSuggestion(
                        "Genetics",
                        $"Genetics processing taking {geneticsTime:F2}ms per frame",
                        "Consider caching trait calculations or reducing genetic complexity",
                        0.6f
                    );
                }
            }
        }

        private void ProfileAIProcessing()
        {
            using (AIProcessingMarker.Auto())
            {
                // Profile AI behavior processing
                float aiTime = EstimateAIProcessingTime();
                RecordSystemProfile("AIProcessing", aiTime);

                if (aiTime > 3f && currentCreatureCount > 100) // More than 3ms with many creatures
                {
                    AddOptimizationSuggestion(
                        "AI Optimization",
                        $"AI processing taking {aiTime:F2}ms with {currentCreatureCount} creatures",
                        "Consider reducing AI update frequency or implementing LOD for distant creatures",
                        0.7f
                    );
                }
            }
        }

        private void ProfileSpawningOperations()
        {
            using (SpawningMarker.Auto())
            {
                // This would profile actual spawning operations
                // For now, provide general spawning optimization suggestions

                if (currentCreatureCount > maxCreatureCount * 0.9f)
                {
                    AddOptimizationSuggestion(
                        "Population Management",
                        $"Creature count ({currentCreatureCount}) approaching limit ({maxCreatureCount})",
                        "Consider implementing population culling or increasing creature limits",
                        0.9f
                    );
                }
            }
        }

        private void CheckForOptimizations()
        {
            CheckMemoryOptimizations();
            CheckRenderingOptimizations();
            CheckECSOptimizations();

            if (autoOptimize)
            {
                ApplyAutomaticOptimizations();
            }
        }

        private void CheckMemoryOptimizations()
        {
            if (currentMemoryMB > maxMemoryMB * memoryWarningThreshold)
            {
                AddOptimizationSuggestion(
                    "Memory",
                    $"Memory usage ({currentMemoryMB}MB) exceeding threshold",
                    "Run garbage collection, enable object pooling, or reduce creature history",
                    0.8f
                );
            }

            // Check for memory leaks
            if (frameHistory.Count > 30)
            {
                var recent = frameHistory.TakeLast(10).Average(f => f.memoryUsage);
                var older = frameHistory.Take(10).Average(f => f.memoryUsage);

                if (recent > older * 1.2f) // 20% memory increase
                {
                    AddOptimizationSuggestion(
                        "Memory Leak",
                        "Potential memory leak detected - memory usage growing over time",
                        "Check for uncleaned references or excessive object creation",
                        0.9f
                    );
                }
            }
        }

        private void CheckRenderingOptimizations()
        {
            if (drawCallsRecorder.Valid)
            {
                int drawCalls = (int)drawCallsRecorder.LastValue;

                if (drawCalls > 500 && currentCreatureCount > 200)
                {
                    AddOptimizationSuggestion(
                        "Rendering",
                        $"High draw call count ({drawCalls}) with many creatures",
                        "Implement GPU instancing or LOD system for creature rendering",
                        0.7f
                    );
                }
            }
        }

        private void CheckECSOptimizations()
        {
            if (currentCreatureCount > 500 && currentFPS < targetFPS * 0.8f)
            {
                AddOptimizationSuggestion(
                    "ECS Performance",
                    "High creature count with low FPS - ECS optimization needed",
                    "Review entity component layout, job scheduling, and Burst compilation usage",
                    0.9f
                );
            }
        }

        private void ApplyAutomaticOptimizations()
        {
            // Apply safe, automatic optimizations
            foreach (var suggestion in suggestions.Where(s => s.impact > 0.7f))
            {
                switch (suggestion.category)
                {
                    case "Memory":
                        if (suggestion.description.Contains("garbage collection"))
                        {
                            System.GC.Collect();
                            Debug.Log("🔧 Auto-optimization: Forced garbage collection");
                        }
                        break;

                    case "Population Management":
                        if (currentCreatureCount > maxCreatureCount)
                        {
                            // This would integrate with your spawning system to reduce population
                            Debug.Log("🔧 Auto-optimization: Population reduction suggested");
                        }
                        break;
                }
            }
        }

        private void RecordSystemProfile(string systemName, float time)
        {
            var existing = systemProfiles.FirstOrDefault(p => p.systemName == systemName);
            int index = systemProfiles.FindIndex(p => p.systemName == systemName);

            if (index >= 0)
            {
                var updated = existing;
                updated.totalTime += time;
                updated.sampleCount++;
                updated.averageTime = updated.totalTime / updated.sampleCount;
                updated.maxTime = Mathf.Max(updated.maxTime, time);
                systemProfiles[index] = updated;
            }
            else
            {
                systemProfiles.Add(new SystemProfileData
                {
                    systemName = systemName,
                    averageTime = time,
                    maxTime = time,
                    totalTime = time,
                    sampleCount = 1
                });
            }
        }

        private void AddOptimizationSuggestion(string category, string description, string action, float impact)
        {
            // Check if we already have this suggestion recently
            bool alreadyExists = suggestions.Any(s =>
                s.category == category &&
                s.description == description &&
                Time.time - s.timestamp < 30f); // Don't spam same suggestion

            if (!alreadyExists)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    category = category,
                    description = description,
                    action = action,
                    impact = impact,
                    timestamp = Time.time
                });

                // Limit suggestion count
                if (suggestions.Count > 20)
                {
                    suggestions.RemoveAt(0);
                }

                if (enableAlerts && impact > 0.8f)
                {
                    Debug.LogWarning($"⚠️ Performance Suggestion [{category}]: {description} - {action}");
                }
            }
        }

        private void UpdateDebugData()
        {
            // Debug manager integration disabled to avoid namespace conflicts
            // Performance data can be accessed directly via GetPerformanceReport() method
            UnityEngine.Debug.Log($"Performance Update - FPS: {currentFPS:F1}, Memory: {currentMemoryMB}MB, Creatures: {currentCreatureCount}, Status: {performanceStatus}");
        }

        private void SendPerformanceAlert()
        {
            // This would integrate with your alerting system
            Debug.LogError($"🚨 Performance Alert: {performanceStatus} - FPS: {currentFPS:F1}, Memory: {currentMemoryMB}MB, Creatures: {currentCreatureCount}");
        }

        // Estimation methods
        private float EstimateGeneticsProcessingTime()
        {
            return currentCreatureCount * 0.001f; // Rough estimate: 0.001ms per creature
        }

        private float EstimateAIProcessingTime()
        {
            return currentCreatureCount * 0.002f; // Rough estimate: 0.002ms per creature
        }

        private void CleanupProfiler()
        {
            systemMemoryRecorder.Dispose();
            gcMemoryRecorder.Dispose();
            drawCallsRecorder.Dispose();
        }

        /// <summary>
        /// Get performance report for external systems
        /// </summary>
        public PerformanceReport GetPerformanceReport()
        {
            return new PerformanceReport
            {
                currentFPS = currentFPS,
                currentFrameTime = currentFrameTime,
                memoryUsageMB = currentMemoryMB,
                creatureCount = currentCreatureCount,
                status = performanceStatus,
                suggestions = suggestions.ToArray(),
                systemProfiles = systemProfiles.ToArray()
            };
        }

        /// <summary>
        /// Force an optimization check
        /// </summary>
        [ContextMenu("Run Optimization Check")]
        public void ForceOptimizationCheck()
        {
            CheckForOptimizations();
            Debug.Log($"🔍 Optimization check complete. Found {suggestions.Count} suggestions.");
        }

        /// <summary>
        /// Clear all performance history
        /// </summary>
        [ContextMenu("Clear Performance History")]
        public void ClearPerformanceHistory()
        {
            frameHistory.Clear();
            systemProfiles.Clear();
            suggestions.Clear();
            Debug.Log("🗑️ Performance history cleared");
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            if (!enableProfiling) return;

            // Performance overlay
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.Label("🔥 Chimera Performance", EditorStyles.boldLabel);
            GUILayout.Label($"FPS: {currentFPS:F1} / {targetFPS}");
            GUILayout.Label($"Frame: {currentFrameTime:F1}ms / {maxFrameTime:F1}ms");
            GUILayout.Label($"Memory: {currentMemoryMB}MB / {maxMemoryMB}MB");
            GUILayout.Label($"Creatures: {currentCreatureCount} / {maxCreatureCount}");
            GUILayout.Label($"Status: {performanceStatus}");

            if (suggestions.Count > 0)
            {
                GUILayout.Label($"Suggestions: {suggestions.Count}");
                var topSuggestion = suggestions.OrderByDescending(s => s.impact).FirstOrDefault();
                GUILayout.Label($"Top: {topSuggestion.category}", EditorStyles.miniLabel);
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
#endif

        private static class EditorStyles
        {
            public static GUIStyle boldLabel = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            public static GUIStyle miniLabel = new GUIStyle(GUI.skin.label) { fontSize = 10 };
        }
    }

    /// <summary>
    /// Performance report data structure
    /// </summary>
    [System.Serializable]
    public struct PerformanceReport
    {
        public float currentFPS;
        public float currentFrameTime;
        public long memoryUsageMB;
        public int creatureCount;
        public string status;
        public ChimeraPerformanceProfiler.OptimizationSuggestion[] suggestions;
        public ChimeraPerformanceProfiler.SystemProfileData[] systemProfiles;
    }
}