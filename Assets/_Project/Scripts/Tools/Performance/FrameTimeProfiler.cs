using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;
using Laboratory.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Tools.Performance
{
    /// <summary>
    /// Frame Time Profiler - Detailed breakdown of frame time with bottleneck detection
    ///
    /// Features:
    /// - Detailed frame time breakdown (CPU, rendering, scripts, physics, etc.)
    /// - Automatic bottleneck detection
    /// - FPS history graphing
    /// - Spike detection and analysis
    /// - Performance budget tracking
    /// - Category-based profiling (Update, FixedUpdate, LateUpdate, Rendering)
    /// - Actionable optimization suggestions
    ///
    /// Usage:
    /// - Open window via Tools > Frame Time Profiler
    /// - View real-time frame time breakdown
    /// - Identify performance bottlenecks
    /// - Get optimization recommendations
    /// </summary>
    public class FrameTimeProfiler : MonoBehaviour
    {
        private static FrameTimeProfiler _instance;
        public static FrameTimeProfiler Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("FrameTimeProfiler");
                    _instance = go.AddComponent<FrameTimeProfiler>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Profiler Settings")]
        [Tooltip("Enable profiling")]
        public bool enableProfiling = true;

        [Tooltip("Target frame rate")]
        public int targetFPS = GameConstants.TARGET_FPS;

        [Tooltip("Maximum history frames")]
        public int maxHistoryFrames = 300;

        [Tooltip("Spike detection threshold (ms above average)")]
        public float spikeThresholdMs = GameConstants.PERFORMANCE_WARNING_THRESHOLD_MS;

        [Header("Display")]
        [Tooltip("Show on-screen overlay")]
        public bool showOverlay = true;

        [Tooltip("Overlay position")]
        public OverlayPosition overlayPosition = OverlayPosition.TopRight;

        [Tooltip("Show detailed breakdown")]
        public bool showDetailedBreakdown = true;

        // Frame timing data
        private List<FrameData> _frameHistory = new List<FrameData>();
        private FrameData _currentFrame;

        // Category timers
        private Dictionary<string, CategoryTimer> _categoryTimers = new Dictionary<string, CategoryTimer>();
        private Stopwatch _frameStopwatch = new Stopwatch();

        // Statistics
        private float _averageFrameTimeMs = GameConstants.FRAME_BUDGET_MS;
        private float _minFrameTimeMs = 0f;
        private float _maxFrameTimeMs = 0f;
        private int _spikeCount = 0;
        private List<FrameSpike> _recentSpikes = new List<FrameSpike>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize default categories
            RegisterCategory("Update");
            RegisterCategory("FixedUpdate");
            RegisterCategory("LateUpdate");
            RegisterCategory("Rendering");
            RegisterCategory("Physics");
            RegisterCategory("Animation");
            RegisterCategory("AI");
            RegisterCategory("Networking");
        }

        private void Update()
        {
            if (!enableProfiling) return;

            StartFrame();
            StartCategory("Update");
        }

        private void LateUpdate()
        {
            if (!enableProfiling) return;

            EndCategory("Update");
            StartCategory("LateUpdate");
        }

        private void OnGUI()
        {
            if (!enableProfiling) return;

            EndCategory("LateUpdate");
            EndFrame();

            if (showOverlay)
            {
                DrawOverlay();
            }
        }

        /// <summary>
        /// Start frame timing
        /// </summary>
        private void StartFrame()
        {
            _currentFrame = new FrameData
            {
                frameNumber = Time.frameCount,
                timestamp = Time.realtimeSinceStartup,
                categoryTimes = new Dictionary<string, float>()
            };

            _frameStopwatch.Restart();
        }

        /// <summary>
        /// End frame timing
        /// </summary>
        private void EndFrame()
        {
            _frameStopwatch.Stop();
            _currentFrame.totalTimeMs = (float)_frameStopwatch.Elapsed.TotalMilliseconds;
            _currentFrame.fps = 1000f / _currentFrame.totalTimeMs;

            // Copy category times
            foreach (var kvp in _categoryTimers)
            {
                _currentFrame.categoryTimes[kvp.Key] = kvp.Value.totalTimeMs;
                kvp.Value.Reset(); // Reset for next frame
            }

            // Detect spikes
            if (_frameHistory.Count > 10)
            {
                if (_currentFrame.totalTimeMs > _averageFrameTimeMs + spikeThresholdMs)
                {
                    _spikeCount++;
                    _recentSpikes.Add(new FrameSpike
                    {
                        frameNumber = _currentFrame.frameNumber,
                        frameTimeMs = _currentFrame.totalTimeMs,
                        averageAtTimeMs = _averageFrameTimeMs,
                        timestamp = Time.realtimeSinceStartup
                    });

                    // Keep only last 20 spikes
                    if (_recentSpikes.Count > 20)
                    {
                        _recentSpikes.RemoveAt(0);
                    }
                }
            }

            // Add to history
            _frameHistory.Add(_currentFrame);

            // Trim history
            while (_frameHistory.Count > maxHistoryFrames)
            {
                _frameHistory.RemoveAt(0);
            }

            // Update statistics
            UpdateStatistics();
        }

        /// <summary>
        /// Register a profiling category
        /// </summary>
        public void RegisterCategory(string categoryName)
        {
            if (!_categoryTimers.ContainsKey(categoryName))
            {
                _categoryTimers[categoryName] = new CategoryTimer { categoryName = categoryName };
            }
        }

        /// <summary>
        /// Start timing a category
        /// </summary>
        public void StartCategory(string categoryName)
        {
            if (!enableProfiling) return;

            if (!_categoryTimers.ContainsKey(categoryName))
            {
                RegisterCategory(categoryName);
            }

            _categoryTimers[categoryName].Start();
        }

        /// <summary>
        /// End timing a category
        /// </summary>
        public void EndCategory(string categoryName)
        {
            if (!enableProfiling || !_categoryTimers.ContainsKey(categoryName)) return;

            _categoryTimers[categoryName].Stop();
        }

        /// <summary>
        /// Record a timed section
        /// </summary>
        public void RecordSection(string categoryName, string sectionName, float timeMs)
        {
            if (!enableProfiling) return;

            if (!_categoryTimers.ContainsKey(categoryName))
            {
                RegisterCategory(categoryName);
            }

            _categoryTimers[categoryName].AddSection(sectionName, timeMs);
        }

        private void UpdateStatistics()
        {
            if (_frameHistory.Count == 0) return;

            var frameTimes = _frameHistory.Select(f => f.totalTimeMs).ToList();
            _averageFrameTimeMs = frameTimes.Average();
            _minFrameTimeMs = frameTimes.Min();
            _maxFrameTimeMs = frameTimes.Max();
        }

        /// <summary>
        /// Get current frame statistics
        /// </summary>
        public FrameStats GetStats()
        {
            return new FrameStats
            {
                currentFPS = _currentFrame.fps,
                targetFPS = targetFPS,
                averageFrameTimeMs = _averageFrameTimeMs,
                minFrameTimeMs = _minFrameTimeMs,
                maxFrameTimeMs = _maxFrameTimeMs,
                spikeCount = _spikeCount,
                frameCount = _frameHistory.Count
            };
        }

        /// <summary>
        /// Get frame history
        /// </summary>
        public List<FrameData> GetFrameHistory()
        {
            return new List<FrameData>(_frameHistory);
        }

        /// <summary>
        /// Get recent spikes
        /// </summary>
        public List<FrameSpike> GetRecentSpikes()
        {
            return new List<FrameSpike>(_recentSpikes);
        }

        /// <summary>
        /// Get category breakdown
        /// </summary>
        public Dictionary<string, float> GetCategoryBreakdown()
        {
            if (_frameHistory.Count == 0) return new Dictionary<string, float>();

            var breakdown = new Dictionary<string, float>();
            var recentFrames = _frameHistory.TakeLast(60); // Last 60 frames (1 second at 60 FPS)

            foreach (var category in _categoryTimers.Keys)
            {
                var avgTime = recentFrames
                    .Where(f => f.categoryTimes.ContainsKey(category))
                    .Average(f => f.categoryTimes[category]);
                breakdown[category] = avgTime;
            }

            return breakdown;
        }

        /// <summary>
        /// Get bottlenecks (slowest categories)
        /// </summary>
        public List<KeyValuePair<string, float>> GetBottlenecks(int count = 5)
        {
            var breakdown = GetCategoryBreakdown();
            return breakdown
                .OrderByDescending(kvp => kvp.Value)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get optimization suggestions
        /// </summary>
        public List<OptimizationSuggestion> GetOptimizationSuggestions()
        {
            var suggestions = new List<OptimizationSuggestion>();
            var targetFrameTime = 1000f / targetFPS;
            var breakdown = GetCategoryBreakdown();

            foreach (var kvp in breakdown)
            {
                float percentOfFrame = (kvp.Value / targetFrameTime) * 100f;

                if (percentOfFrame > 30f) // Category taking >30% of frame budget
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        severity = percentOfFrame > 50f ? "Critical" : "High",
                        category = kvp.Key,
                        description = $"{kvp.Key} taking {percentOfFrame:F1}% of frame budget ({kvp.Value:F2}ms)",
                        recommendation = GetRecommendation(kvp.Key)
                    });
                }
            }

            // Check for spikes
            if (_spikeCount > 10 && _recentSpikes.Count > 0)
            {
                suggestions.Add(new OptimizationSuggestion
                {
                    severity = "Medium",
                    category = "Performance",
                    description = $"{_spikeCount} frame spikes detected",
                    recommendation = "Investigate spike causes - possibly GC allocations or loading"
                });
            }

            return suggestions;
        }

        private string GetRecommendation(string category)
        {
            return category switch
            {
                "Update" => "Optimize MonoBehaviour Update loops - consider reducing active object count",
                "Rendering" => "Reduce draw calls, enable GPU instancing, optimize shaders",
                "Physics" => "Reduce physics timestep, use simpler colliders, optimize raycasts",
                "Animation" => "Use animator culling, reduce skinned mesh quality",
                "AI" => "Implement timeslicing, reduce AI update frequency, optimize pathfinding",
                "Networking" => "Batch network messages, reduce update frequency, compress data",
                _ => "Profile this category in detail to identify specific bottlenecks"
            };
        }

        private void DrawOverlay()
        {
            int x = overlayPosition switch
            {
                OverlayPosition.TopLeft or OverlayPosition.BottomLeft => 10,
                OverlayPosition.TopRight or OverlayPosition.BottomRight => Screen.width - 260,
                _ => 10
            };

            int y = overlayPosition switch
            {
                OverlayPosition.TopLeft or OverlayPosition.TopRight => 10,
                OverlayPosition.BottomLeft or OverlayPosition.BottomRight => Screen.height - (showDetailedBreakdown ? 320 : 160),
                _ => 10
            };

            int height = showDetailedBreakdown ? 320 : 160;
            GUI.Box(new Rect(x, y, 250, height), "");

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = Color.white;

            int lineHeight = 15;
            int currentY = y + 10;

            var stats = GetStats();

            GUI.Label(new Rect(x + 10, currentY, 230, lineHeight), "=== Frame Profiler ===", labelStyle);
            currentY += lineHeight + 5;

            // FPS with color coding
            Color fpsColor = stats.currentFPS >= targetFPS ? Color.green : (stats.currentFPS >= targetFPS * 0.8f ? Color.yellow : Color.red);
            labelStyle.normal.textColor = fpsColor;
            GUI.Label(new Rect(x + 10, currentY, 230, lineHeight), $"FPS: {stats.currentFPS:F1} / {targetFPS}", labelStyle);
            currentY += lineHeight;

            labelStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + 10, currentY, 230, lineHeight), $"Frame: {stats.averageFrameTimeMs:F2}ms (avg)", labelStyle);
            currentY += lineHeight;

            GUI.Label(new Rect(x + 10, currentY, 230, lineHeight), $"Min/Max: {stats.minFrameTimeMs:F2}/{stats.maxFrameTimeMs:F2}ms", labelStyle);
            currentY += lineHeight;

            GUI.Label(new Rect(x + 10, currentY, 230, lineHeight), $"Spikes: {stats.spikeCount}", labelStyle);
            currentY += lineHeight + 5;

            if (showDetailedBreakdown)
            {
                GUI.Label(new Rect(x + 10, currentY, 230, lineHeight), "Category Breakdown:", labelStyle);
                currentY += lineHeight;

                var bottlenecks = GetBottlenecks(5);
                foreach (var kvp in bottlenecks)
                {
                    float percentOfBudget = (kvp.Value / (1000f / targetFPS)) * 100f;
                    Color catColor = percentOfBudget > 30f ? Color.red : (percentOfBudget > 15f ? Color.yellow : Color.green);

                    labelStyle.normal.textColor = catColor;
                    labelStyle.fontSize = 10;
                    GUI.Label(new Rect(x + 10, currentY, 230, lineHeight), $"  {kvp.Key}: {kvp.Value:F2}ms ({percentOfBudget:F0}%)", labelStyle);
                    currentY += lineHeight;
                }
            }
        }

        /// <summary>
        /// Clear all profiling data
        /// </summary>
        public void ClearData()
        {
            _frameHistory.Clear();
            _recentSpikes.Clear();
            _spikeCount = 0;

            foreach (var timer in _categoryTimers.Values)
            {
                timer.Reset();
            }
        }
    }

    /// <summary>
    /// Data for a single frame
    /// </summary>
    [Serializable]
    public class FrameData
    {
        public int frameNumber;
        public float timestamp;
        public float totalTimeMs;
        public float fps;
        public Dictionary<string, float> categoryTimes;
    }

    /// <summary>
    /// Frame timing statistics
    /// </summary>
    [Serializable]
    public struct FrameStats
    {
        public float currentFPS;
        public int targetFPS;
        public float averageFrameTimeMs;
        public float minFrameTimeMs;
        public float maxFrameTimeMs;
        public int spikeCount;
        public int frameCount;
    }

    /// <summary>
    /// Frame spike information
    /// </summary>
    [Serializable]
    public struct FrameSpike
    {
        public int frameNumber;
        public float frameTimeMs;
        public float averageAtTimeMs;
        public float timestamp;
    }

    /// <summary>
    /// Optimization suggestion
    /// </summary>
    [Serializable]
    public struct OptimizationSuggestion
    {
        public string severity;
        public string category;
        public string description;
        public string recommendation;
    }

    /// <summary>
    /// Timer for a profiling category
    /// </summary>
    public class CategoryTimer
    {
        public string categoryName;
        public float totalTimeMs;
        public Dictionary<string, float> sections = new Dictionary<string, float>();
        private Stopwatch _stopwatch = new Stopwatch();

        public void Start()
        {
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _stopwatch.Stop();
            totalTimeMs += (float)_stopwatch.Elapsed.TotalMilliseconds;
        }

        public void AddSection(string sectionName, float timeMs)
        {
            if (!sections.ContainsKey(sectionName))
            {
                sections[sectionName] = 0f;
            }
            sections[sectionName] += timeMs;
            totalTimeMs += timeMs;
        }

        public void Reset()
        {
            totalTimeMs = 0f;
            sections.Clear();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor window for Frame Time Profiler
    /// </summary>
    public class FrameTimeProfilerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabs = { "Overview", "Breakdown", "Spikes", "Suggestions" };
        private bool _autoRefresh = true;
        private float _lastRefreshTime = 0f;

        [MenuItem("Tools/Project Chimera/Frame Time Profiler")]
        public static void ShowWindow()
        {
            var window = GetWindow<FrameTimeProfilerWindow>("Frame Profiler");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            if (_autoRefresh && EditorApplication.isPlaying && Time.time - _lastRefreshTime > 0.1f)
            {
                _lastRefreshTime = Time.time;
                Repaint();
            }

            EditorGUILayout.BeginVertical();

            // Header
            EditorGUILayout.LabelField("Frame Time Profiler", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Controls
            EditorGUILayout.BeginHorizontal();
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);

            if (GUILayout.Button("Clear Data") && Application.isPlaying)
            {
                FrameTimeProfiler.Instance.ClearData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see profiling data", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.Space();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            switch (_selectedTab)
            {
                case 0: DrawOverview(); break;
                case 1: DrawBreakdown(); break;
                case 2: DrawSpikes(); break;
                case 3: DrawSuggestions(); break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawOverview()
        {
            var stats = FrameTimeProfiler.Instance.GetStats();
            float targetFrameTime = 1000f / stats.targetFPS;

            EditorGUILayout.LabelField("Performance Overview", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // FPS
            Color originalColor = GUI.color;
            GUI.color = stats.currentFPS >= stats.targetFPS ? Color.green : Color.yellow;
            EditorGUILayout.LabelField($"Current FPS: {stats.currentFPS:F1}", EditorStyles.boldLabel);
            GUI.color = originalColor;

            EditorGUILayout.LabelField($"Target FPS: {stats.targetFPS}");
            EditorGUILayout.LabelField($"Average Frame Time: {stats.averageFrameTimeMs:F2} ms");
            EditorGUILayout.LabelField($"Min Frame Time: {stats.minFrameTimeMs:F2} ms");
            EditorGUILayout.LabelField($"Max Frame Time: {stats.maxFrameTimeMs:F2} ms");
            EditorGUILayout.LabelField($"Frame Spikes: {stats.spikeCount}");

            // Frame budget bar
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Frame Budget ({targetFrameTime:F2}ms)");
            Rect rect = GUILayoutUtility.GetRect(18, 18);
            float budgetPercent = Mathf.Clamp01(stats.averageFrameTimeMs / targetFrameTime);
            EditorGUI.ProgressBar(rect, budgetPercent, $"{stats.averageFrameTimeMs:F2} ms");
        }

        private void DrawBreakdown()
        {
            var breakdown = FrameTimeProfiler.Instance.GetCategoryBreakdown();
            var stats = FrameTimeProfiler.Instance.GetStats();
            float targetFrameTime = 1000f / stats.targetFPS;

            EditorGUILayout.LabelField("Category Breakdown", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            foreach (var kvp in breakdown.OrderByDescending(k => k.Value))
            {
                EditorGUILayout.BeginVertical("box");

                float percentOfBudget = (kvp.Value / targetFrameTime) * 100f;

                Color originalColor = GUI.color;
                GUI.color = percentOfBudget > 30f ? Color.red : (percentOfBudget > 15f ? Color.yellow : Color.green);

                EditorGUILayout.LabelField(kvp.Key, EditorStyles.boldLabel);
                GUI.color = originalColor;

                EditorGUILayout.LabelField($"Time: {kvp.Value:F2} ms ({percentOfBudget:F1}% of budget)");

                // Progress bar
                Rect rect = GUILayoutUtility.GetRect(18, 18);
                float barPercent = Mathf.Clamp01(kvp.Value / targetFrameTime);
                EditorGUI.ProgressBar(rect, barPercent, $"{kvp.Value:F2} ms");

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawSpikes()
        {
            var spikes = FrameTimeProfiler.Instance.GetRecentSpikes();

            EditorGUILayout.LabelField($"Recent Frame Spikes ({spikes.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (spikes.Count == 0)
            {
                EditorGUILayout.HelpBox("No spikes detected", MessageType.Info);
                return;
            }

            foreach (var spike in spikes.OrderByDescending(s => s.frameTimeMs))
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox($"Frame #{spike.frameNumber}", MessageType.Warning);
                EditorGUILayout.LabelField($"Frame Time: {spike.frameTimeMs:F2} ms");
                EditorGUILayout.LabelField($"Average at Time: {spike.averageAtTimeMs:F2} ms");
                EditorGUILayout.LabelField($"Spike Amount: +{spike.frameTimeMs - spike.averageAtTimeMs:F2} ms");
                EditorGUILayout.LabelField($"Timestamp: {spike.timestamp:F2}s");
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawSuggestions()
        {
            var suggestions = FrameTimeProfiler.Instance.GetOptimizationSuggestions();

            EditorGUILayout.LabelField($"Optimization Suggestions ({suggestions.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (suggestions.Count == 0)
            {
                EditorGUILayout.HelpBox("Performance is good! No suggestions at this time.", MessageType.Info);
                return;
            }

            foreach (var suggestion in suggestions)
            {
                MessageType messageType = suggestion.severity switch
                {
                    "Critical" => MessageType.Error,
                    "High" => MessageType.Warning,
                    _ => MessageType.Info
                };

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox($"[{suggestion.severity}] {suggestion.category}", messageType);
                EditorGUILayout.LabelField(suggestion.description, EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Recommendation:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(suggestion.recommendation, EditorStyles.wordWrappedLabel);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
    }
#endif
}
