using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Editor
{
    /// <summary>
    /// Performance analysis tool that monitors and reports on game performance,
    /// identifies bottlenecks, and suggests optimizations.
    /// </summary>
    public class PerformanceProfilerWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool isRecording = false;
        private float recordingTime = 0f;
        private List<PerformanceFrame> frameData = new List<PerformanceFrame>();
        private PerformanceReport lastReport;

        [System.Serializable]
        private class PerformanceFrame
        {
            public float frameTime;
            public int triangleCount;
            public int drawCalls;
            public float memoryUsage;
            public int audioSources;
            public float cpuTime;
            public float gpuTime;
            public System.DateTime timestamp;
        }

        [System.Serializable]
        private class PerformanceReport
        {
            public float averageFPS;
            public float minFPS;
            public float maxFPS;
            public float averageFrameTime;
            public int averageTriangles;
            public int averageDrawCalls;
            public float averageMemory;
            public List<string> recommendations = new List<string>();
            public List<string> warnings = new List<string>();
        }

        [MenuItem("🧪 Laboratory/Performance/Performance Profiler")]
        public static void ShowWindow()
        {
            PerformanceProfilerWindow window = GetWindow<PerformanceProfilerWindow>("Performance Profiler");
            window.minSize = new Vector2(500, 600);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Performance Profiler", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawRecordingControls();
            DrawCurrentStats();
            DrawPerformanceReport();
            DrawRecommendations();
        }

        private void DrawRecordingControls()
        {
            EditorGUILayout.LabelField("Recording Controls", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            
            if (!isRecording)
            {
                if (GUILayout.Button("Start Recording"))
                {
                    StartRecording();
                }
            }
            else
            {
                if (GUILayout.Button("Stop Recording"))
                {
                    StopRecording();
                }
            }
            
            if (GUILayout.Button("Clear Data"))
            {
                ClearData();
            }
            
            if (GUILayout.Button("Generate Report"))
            {
                GenerateReport();
            }
            
            GUILayout.EndHorizontal();
            
            if (isRecording)
            {
                EditorGUILayout.HelpBox($"Recording... {recordingTime:F1}s ({frameData.Count} frames)", MessageType.Info);
            }
            
            EditorGUILayout.Space();
        }

        private void DrawCurrentStats()
        {
            EditorGUILayout.LabelField("Current Frame Stats", EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                var currentFrame = CaptureCurrentFrame();
                
                EditorGUILayout.LabelField($"FPS: {1f / currentFrame.frameTime:F1}");
                EditorGUILayout.LabelField($"Frame Time: {currentFrame.frameTime * 1000:F2} ms");
                EditorGUILayout.LabelField($"Triangles: {currentFrame.triangleCount:N0}");
                EditorGUILayout.LabelField($"Draw Calls: {currentFrame.drawCalls}");
                EditorGUILayout.LabelField($"Memory: {currentFrame.memoryUsage:F1} MB");
                EditorGUILayout.LabelField($"Audio Sources: {currentFrame.audioSources}");
                
                // Visual indicators
                DrawPerformanceBar("Frame Time", currentFrame.frameTime * 1000, 16.67f, 33.33f); // 60fps and 30fps thresholds
                DrawPerformanceBar("Triangle Count", currentFrame.triangleCount, 50000, 100000);
                DrawPerformanceBar("Draw Calls", currentFrame.drawCalls, 50, 100);
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live stats", MessageType.Info);
            }
            
            EditorGUILayout.Space();
        }

        private void DrawPerformanceBar(string label, float current, float warning, float critical)
        {
            EditorGUILayout.LabelField(label);
            
            Rect rect = GUILayoutUtility.GetRect(18, 18, "TextField");
            rect.height = 4;
            
            Color barColor = Color.green;
            if (current > critical) barColor = Color.red;
            else if (current > warning) barColor = Color.yellow;
            
            float percentage = Mathf.Clamp01(current / critical);
            
            EditorGUI.DrawRect(rect, Color.gray);
            
            Rect fillRect = new Rect(rect.x, rect.y, rect.width * percentage, rect.height);
            EditorGUI.DrawRect(fillRect, barColor);
        }

        private void DrawPerformanceReport()
        {
            if (lastReport == null) return;
            
            EditorGUILayout.LabelField("Performance Report", EditorStyles.boldLabel);
            
            EditorGUILayout.LabelField($"Average FPS: {lastReport.averageFPS:F1}");
            EditorGUILayout.LabelField($"Min FPS: {lastReport.minFPS:F1}");
            EditorGUILayout.LabelField($"Max FPS: {lastReport.maxFPS:F1}");
            EditorGUILayout.LabelField($"Average Frame Time: {lastReport.averageFrameTime * 1000:F2} ms");
            EditorGUILayout.LabelField($"Average Triangles: {lastReport.averageTriangles:N0}");
            EditorGUILayout.LabelField($"Average Draw Calls: {lastReport.averageDrawCalls}");
            EditorGUILayout.LabelField($"Average Memory: {lastReport.averageMemory:F1} MB");
            
            EditorGUILayout.Space();
        }

        private void DrawRecommendations()
        {
            if (lastReport == null || (lastReport.recommendations.Count == 0 && lastReport.warnings.Count == 0)) 
                return;
            
            EditorGUILayout.LabelField("Optimization Recommendations", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            // Warnings
            if (lastReport.warnings.Count > 0)
            {
                EditorGUILayout.LabelField("⚠️ Warnings:", EditorStyles.boldLabel);
                foreach (string warning in lastReport.warnings)
                {
                    EditorGUILayout.HelpBox(warning, MessageType.Warning);
                }
                EditorGUILayout.Space();
            }
            
            // Recommendations
            if (lastReport.recommendations.Count > 0)
            {
                EditorGUILayout.LabelField("💡 Recommendations:", EditorStyles.boldLabel);
                foreach (string recommendation in lastReport.recommendations)
                {
                    EditorGUILayout.HelpBox(recommendation, MessageType.Info);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void OnEditorUpdate()
        {
            if (isRecording && Application.isPlaying)
            {
                recordingTime += Time.unscaledDeltaTime;
                
                // Capture frame data every few frames to avoid overhead
                if (Time.frameCount % 5 == 0)
                {
                    frameData.Add(CaptureCurrentFrame());
                }
                
                // Auto-stop after 30 seconds
                if (recordingTime > 30f)
                {
                    StopRecording();
                }
                
                Repaint();
            }
        }

        private PerformanceFrame CaptureCurrentFrame()
        {
            return new PerformanceFrame
            {
                frameTime = Time.unscaledDeltaTime,
                triangleCount = UnityStats.triangles,
                drawCalls = UnityStats.drawCalls,
                memoryUsage = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                audioSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None).Length,
                cpuTime = Time.unscaledDeltaTime,
                gpuTime = 0f, // GPU time requires more complex profiling
                timestamp = System.DateTime.Now
            };
        }

        private void StartRecording()
        {
            isRecording = true;
            recordingTime = 0f;
            frameData.Clear();
            UnityEngine.Debug.Log("[Performance Profiler] Started recording performance data");
        }

        private void StopRecording()
        {
            isRecording = false;
            GenerateReport();
            UnityEngine.Debug.Log($"[Performance Profiler] Stopped recording. Captured {frameData.Count} frames over {recordingTime:F1} seconds");
        }

        private void ClearData()
        {
            frameData.Clear();
            lastReport = null;
            recordingTime = 0f;
            UnityEngine.Debug.Log("[Performance Profiler] Cleared all performance data");
        }

        private void GenerateReport()
        {
            if (frameData.Count == 0)
            {
                EditorUtility.DisplayDialog("No Data", "No performance data to analyze. Start recording first.", "OK");
                return;
            }

            lastReport = new PerformanceReport();
            
            // Calculate averages
            var frameTimes = frameData.Select(f => f.frameTime).ToList();
            lastReport.averageFrameTime = frameTimes.Average();
            lastReport.averageFPS = 1f / lastReport.averageFrameTime;
            lastReport.minFPS = 1f / frameTimes.Max();
            lastReport.maxFPS = 1f / frameTimes.Min();
            
            lastReport.averageTriangles = (int)frameData.Average(f => f.triangleCount);
            lastReport.averageDrawCalls = (int)frameData.Average(f => f.drawCalls);
            lastReport.averageMemory = frameData.Average(f => f.memoryUsage);
            
            // Analyze and generate recommendations
            AnalyzePerformance(lastReport);
            
            UnityEngine.Debug.Log($"[Performance Profiler] Generated report: Avg FPS {lastReport.averageFPS:F1}, Avg Triangles {lastReport.averageTriangles:N0}");
        }

        private void AnalyzePerformance(PerformanceReport report)
        {
            report.recommendations.Clear();
            report.warnings.Clear();
            
            // FPS Analysis
            if (report.averageFPS < 30f)
            {
                report.warnings.Add("Low FPS detected! Average FPS is below 30.");
                
                if (report.averageTriangles > 100000)
                {
                    report.recommendations.Add("High triangle count detected. Consider using LOD (Level of Detail) systems or mesh optimization.");
                }
                
                if (report.averageDrawCalls > 100)
                {
                    report.recommendations.Add("High draw call count. Consider texture atlasing, mesh combining, or GPU instancing.");
                }
            }
            else if (report.averageFPS < 60f)
            {
                report.warnings.Add("Moderate FPS. Consider optimization for better performance.");
            }
            
            // Triangle Count Analysis
            if (report.averageTriangles > 150000)
            {
                report.warnings.Add("Very high triangle count detected!");
                report.recommendations.Add("Implement LOD system for distant objects.");
                report.recommendations.Add("Use occlusion culling to hide objects not visible to camera.");
                report.recommendations.Add("Consider mesh optimization tools to reduce triangle count.");
            }
            else if (report.averageTriangles > 75000)
            {
                report.recommendations.Add("Triangle count is getting high. Monitor closely and consider LOD system.");
            }
            
            // Draw Call Analysis
            if (report.averageDrawCalls > 150)
            {
                report.warnings.Add("Very high draw call count!");
                report.recommendations.Add("Use texture atlases to combine multiple textures into one.");
                report.recommendations.Add("Combine meshes that use the same material.");
                report.recommendations.Add("Use GPU instancing for repeated objects.");
                report.recommendations.Add("Consider static batching for non-moving objects.");
            }
            else if (report.averageDrawCalls > 75)
            {
                report.recommendations.Add("Draw call count is moderate. Consider batching optimizations.");
            }
            
            // Memory Analysis
            if (report.averageMemory > 1000f) // 1GB
            {
                report.warnings.Add("High memory usage detected!");
                report.recommendations.Add("Check for memory leaks in scripts.");
                report.recommendations.Add("Optimize texture sizes and compression.");
                report.recommendations.Add("Use object pooling for frequently created/destroyed objects.");
            }
            else if (report.averageMemory > 500f) // 500MB
            {
                report.recommendations.Add("Monitor memory usage. Consider texture optimization.");
            }
            
            // Frame Time Consistency
            var frameTimes = frameData.Select(f => f.frameTime).ToList();
            float frameTimeVariance = CalculateVariance(frameTimes);
            
            if (frameTimeVariance > 0.01f) // High variance in frame times
            {
                report.warnings.Add("Inconsistent frame times detected (frame drops/stuttering).");
                report.recommendations.Add("Profile scripts for expensive operations in Update methods.");
                report.recommendations.Add("Consider spreading heavy calculations across multiple frames.");
                report.recommendations.Add("Check for garbage collection spikes.");
            }
            
            // Audio Sources
            int avgAudioSources = (int)frameData.Average(f => f.audioSources);
            if (avgAudioSources > 50)
            {
                report.recommendations.Add("Many audio sources detected. Consider audio source pooling.");
            }
            
            // Scene-specific recommendations
            AddSceneSpecificRecommendations(report);
        }

        private void AddSceneSpecificRecommendations(PerformanceReport report)
        {
            // Check for common performance issues in the scene
            
            // Lights
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int realtimeLights = lights.Count(l => l.lightmapBakeType == LightmapBakeType.Realtime);
            if (realtimeLights > 8)
            {
                report.recommendations.Add($"Many realtime lights detected ({realtimeLights}). Consider baking some lights or using Light Probes.");
            }
            
            // Cameras
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            if (cameras.Length > 3)
            {
                report.recommendations.Add($"Multiple cameras detected ({cameras.Length}). Each camera adds rendering overhead.");
            }
            
            // Particle Systems
            ParticleSystem[] particles = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            if (particles.Length > 10)
            {
                report.recommendations.Add($"Many particle systems detected ({particles.Length}). Consider particle pooling or reducing max particles.");
            }
            
            // Rigidbodies
            Rigidbody[] rigidbodies = FindObjectsByType<Rigidbody>(FindObjectsSortMode.None);
            if (rigidbodies.Length > 50)
            {
                report.recommendations.Add($"Many Rigidbodies detected ({rigidbodies.Length}). Consider optimizing physics or using simpler colliders.");
            }
            
            // Missing LOD Groups
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int renderersWithoutLOD = renderers.Count(r => r.GetComponent<LODGroup>() == null && 
                                                          r.bounds.size.magnitude > 5f);
            if (renderersWithoutLOD > 10)
            {
                report.recommendations.Add($"Large objects without LOD detected ({renderersWithoutLOD}). Consider adding LOD Groups.");
            }
        }

        private float CalculateVariance(List<float> values)
        {
            if (values.Count == 0) return 0f;
            
            float mean = values.Average();
            float sumSquaredDifferences = values.Sum(val => (val - mean) * (val - mean));
            return sumSquaredDifferences / values.Count;
        }
    }
}
