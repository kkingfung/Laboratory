using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ProjectChimera.Tools.Performance
{
    /// <summary>
    /// Memory Pool Visualizer - Monitor and optimize object pooling efficiency
    ///
    /// Features:
    /// - Real-time pool hit/miss ratio tracking
    /// - Memory usage graphs per pool
    /// - Pool efficiency scoring
    /// - Automatic leak detection
    /// - Recommended pool size adjustments
    /// - Integration with ParticleVFXManager and other pooling systems
    ///
    /// Usage:
    /// - Open window via Tools > Memory Pool Visualizer
    /// - View real-time pooling efficiency
    /// - Identify memory leaks and under-utilized pools
    /// - Optimize pool sizes for best performance
    /// </summary>
    public class MemoryPoolVisualizer : MonoBehaviour
    {
        private static MemoryPoolVisualizer _instance;
        public static MemoryPoolVisualizer Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("MemoryPoolVisualizer");
                    _instance = go.AddComponent<MemoryPoolVisualizer>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("Visualizer Settings")]
        [Tooltip("Enable pool monitoring")]
        public bool enableMonitoring = true;

        [Tooltip("Sample interval (seconds)")]
        public float sampleInterval = 0.5f;

        [Tooltip("Show on-screen overlay")]
        public bool showOverlay = false;

        [Tooltip("Detect memory leaks")]
        public bool detectLeaks = true;

        [Tooltip("Leak detection threshold (objects not returned in seconds)")]
        public float leakThresholdSeconds = 30f;

        [Header("Optimization")]
        [Tooltip("Auto-suggest pool size adjustments")]
        public bool autoSuggestSizes = true;

        [Tooltip("Target hit rate (0-1)")]
        public float targetHitRate = 0.95f;

        // Pool tracking
        private Dictionary<string, PoolData> _pools = new Dictionary<string, PoolData>();
        private float _lastSampleTime = 0f;

        // Overall stats
        private int _totalPooledObjects = 0;
        private int _totalActiveObjects = 0;
        private float _overallHitRate = 0f;

        private void Update()
        {
            if (!enableMonitoring) return;

            if (Time.time - _lastSampleTime >= sampleInterval)
            {
                _lastSampleTime = Time.time;
                SamplePools();
            }
        }

        private void SamplePools()
        {
            _totalPooledObjects = 0;
            _totalActiveObjects = 0;
            int totalHits = 0;
            int totalRequests = 0;

            foreach (var pool in _pools.Values)
            {
                _totalPooledObjects += pool.totalSize;
                _totalActiveObjects += pool.activeCount;
                totalHits += pool.hits;
                totalRequests += pool.requests;

                // Calculate efficiency
                pool.hitRate = pool.requests > 0 ? (float)pool.hits / pool.requests : 0f;
                pool.utilizationRate = pool.totalSize > 0 ? (float)pool.activeCount / pool.totalSize : 0f;

                // Add sample to history
                var sample = new PoolSample
                {
                    timestamp = Time.time,
                    activeCount = pool.activeCount,
                    hitRate = pool.hitRate,
                    memoryKB = pool.estimatedMemoryKB
                };

                pool.history.Add(sample);

                // Trim history (keep last 300 samples)
                while (pool.history.Count > 300)
                {
                    pool.history.RemoveAt(0);
                }

                // Leak detection
                if (detectLeaks && pool.activeCount > 0)
                {
                    float timeSinceLastReturn = Time.time - pool.lastReturnTime;
                    if (timeSinceLastReturn > leakThresholdSeconds)
                    {
                        pool.possibleLeak = true;
                    }
                }

                // Auto-suggest sizes
                if (autoSuggestSizes && pool.requests > 100) // Need enough data
                {
                    if (pool.hitRate < targetHitRate)
                    {
                        pool.suggestedSize = Mathf.CeilToInt(pool.totalSize * 1.5f);
                    }
                    else if (pool.hitRate > 0.99f && pool.utilizationRate < 0.5f)
                    {
                        pool.suggestedSize = Mathf.Max(pool.peakActiveCount + 10, pool.totalSize / 2);
                    }
                }
            }

            _overallHitRate = totalRequests > 0 ? (float)totalHits / totalRequests : 0f;
        }

        /// <summary>
        /// Register a pool for monitoring
        /// </summary>
        public void RegisterPool(string poolName, int initialSize, float estimatedObjectSizeKB = 10f)
        {
            if (_pools.ContainsKey(poolName)) return;

            var pool = new PoolData
            {
                poolName = poolName,
                totalSize = initialSize,
                estimatedObjectSizeKB = estimatedObjectSizeKB,
                estimatedMemoryKB = initialSize * estimatedObjectSizeKB,
                history = new List<PoolSample>(),
                creationTime = Time.time
            };

            _pools[poolName] = pool;
            UnityEngine.Debug.Log($"[MemoryPoolVisualizer] Registered pool: {poolName} (size: {initialSize})");
        }

        /// <summary>
        /// Record a pool get operation
        /// </summary>
        public void RecordGet(string poolName, bool wasHit)
        {
            if (!enableMonitoring || !_pools.ContainsKey(poolName)) return;

            var pool = _pools[poolName];
            pool.requests++;

            if (wasHit)
            {
                pool.hits++;
            }
            else
            {
                pool.misses++;
            }

            pool.activeCount++;
            pool.peakActiveCount = Mathf.Max(pool.peakActiveCount, pool.activeCount);
        }

        /// <summary>
        /// Record a pool return operation
        /// </summary>
        public void RecordReturn(string poolName)
        {
            if (!enableMonitoring || !_pools.ContainsKey(poolName)) return;

            var pool = _pools[poolName];
            pool.activeCount = Mathf.Max(0, pool.activeCount - 1);
            pool.lastReturnTime = Time.time;
            pool.possibleLeak = false; // Clear leak flag on return
        }

        /// <summary>
        /// Update pool size
        /// </summary>
        public void UpdatePoolSize(string poolName, int newSize)
        {
            if (!_pools.ContainsKey(poolName)) return;

            var pool = _pools[poolName];
            pool.totalSize = newSize;
            pool.estimatedMemoryKB = newSize * pool.estimatedObjectSizeKB;
        }

        /// <summary>
        /// Get data for a specific pool
        /// </summary>
        public PoolData GetPoolData(string poolName)
        {
            return _pools.ContainsKey(poolName) ? _pools[poolName] : null;
        }

        /// <summary>
        /// Get all pool data
        /// </summary>
        public List<PoolData> GetAllPools()
        {
            return _pools.Values.ToList();
        }

        /// <summary>
        /// Get pools with potential leaks
        /// </summary>
        public List<PoolData> GetLeakyPools()
        {
            return _pools.Values.Where(p => p.possibleLeak).ToList();
        }

        /// <summary>
        /// Get inefficient pools (low hit rate)
        /// </summary>
        public List<PoolData> GetInefficientPools(float minHitRate = 0.8f)
        {
            return _pools.Values
                .Where(p => p.requests > 50 && p.hitRate < minHitRate)
                .OrderBy(p => p.hitRate)
                .ToList();
        }

        /// <summary>
        /// Get overall stats
        /// </summary>
        public PoolStats GetOverallStats()
        {
            return new PoolStats
            {
                totalPools = _pools.Count,
                totalPooledObjects = _totalPooledObjects,
                totalActiveObjects = _totalActiveObjects,
                overallHitRate = _overallHitRate,
                totalMemoryKB = _pools.Values.Sum(p => p.estimatedMemoryKB),
                leakyPoolCount = _pools.Values.Count(p => p.possibleLeak)
            };
        }

        /// <summary>
        /// Clear all pool data
        /// </summary>
        public void ClearData()
        {
            foreach (var pool in _pools.Values)
            {
                pool.history.Clear();
                pool.requests = 0;
                pool.hits = 0;
                pool.misses = 0;
                pool.possibleLeak = false;
            }
        }

        private void OnGUI()
        {
            if (!showOverlay || !enableMonitoring) return;

            int x = 10;
            int y = Screen.height - 260;

            GUI.Box(new Rect(x, y, 350, 250), "");

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 11;
            labelStyle.normal.textColor = Color.white;

            int lineHeight = 15;
            int currentY = y + 10;

            GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), "=== Memory Pool Stats ===", labelStyle);
            currentY += lineHeight + 5;

            var stats = GetOverallStats();
            GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), $"Total Pools: {stats.totalPools}", labelStyle);
            currentY += lineHeight;

            GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), $"Pooled Objects: {stats.totalPooledObjects}", labelStyle);
            currentY += lineHeight;

            GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), $"Active Objects: {stats.totalActiveObjects}", labelStyle);
            currentY += lineHeight;

            GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), $"Hit Rate: {stats.overallHitRate:P1}", labelStyle);
            currentY += lineHeight;

            GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), $"Memory: {stats.totalMemoryKB / 1024f:F2} MB", labelStyle);
            currentY += lineHeight + 5;

            // Show inefficient pools
            var inefficient = GetInefficientPools().Take(3);
            if (inefficient.Any())
            {
                labelStyle.normal.textColor = Color.yellow;
                GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), "Low Hit Rate Pools:", labelStyle);
                currentY += lineHeight;

                labelStyle.fontSize = 10;
                foreach (var pool in inefficient)
                {
                    GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), $"  {pool.poolName}: {pool.hitRate:P0}", labelStyle);
                    currentY += lineHeight;
                }
            }

            // Show leaks
            labelStyle.fontSize = 11;
            if (stats.leakyPoolCount > 0)
            {
                labelStyle.normal.textColor = Color.red;
                GUI.Label(new Rect(x + 10, currentY, 330, lineHeight), $"⚠ Potential Leaks: {stats.leakyPoolCount}", labelStyle);
            }
        }
    }

    /// <summary>
    /// Data for a single pool
    /// </summary>
    [Serializable]
    public class PoolData
    {
        public string poolName;
        public int totalSize;
        public int activeCount;
        public int peakActiveCount;
        public int requests;
        public int hits;
        public int misses;
        public float hitRate;
        public float utilizationRate;
        public float estimatedObjectSizeKB;
        public float estimatedMemoryKB;
        public float creationTime;
        public float lastReturnTime;
        public bool possibleLeak;
        public int suggestedSize;
        public List<PoolSample> history;
    }

    /// <summary>
    /// Single pool sample
    /// </summary>
    [Serializable]
    public struct PoolSample
    {
        public float timestamp;
        public int activeCount;
        public float hitRate;
        public float memoryKB;
    }

    /// <summary>
    /// Overall pool statistics
    /// </summary>
    [Serializable]
    public struct PoolStats
    {
        public int totalPools;
        public int totalPooledObjects;
        public int totalActiveObjects;
        public float overallHitRate;
        public float totalMemoryKB;
        public int leakyPoolCount;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor window for Memory Pool Visualizer
    /// </summary>
    public class MemoryPoolVisualizerWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabs = { "Overview", "Pools", "Leaks", "Optimization" };
        private bool _autoRefresh = true;
        private float _lastRefreshTime = 0f;

        [MenuItem("Tools/Project Chimera/Memory Pool Visualizer")]
        public static void ShowWindow()
        {
            var window = GetWindow<MemoryPoolVisualizerWindow>("Pool Visualizer");
            window.minSize = new Vector2(600, 400);
            window.Show();
        }

        private void OnGUI()
        {
            if (_autoRefresh && EditorApplication.isPlaying && Time.time - _lastRefreshTime > 0.5f)
            {
                _lastRefreshTime = Time.time;
                Repaint();
            }

            EditorGUILayout.BeginVertical();

            // Header
            EditorGUILayout.LabelField("Memory Pool Visualizer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Controls
            EditorGUILayout.BeginHorizontal();
            _autoRefresh = EditorGUILayout.Toggle("Auto Refresh", _autoRefresh);

            if (GUILayout.Button("Clear Data") && Application.isPlaying)
            {
                MemoryPoolVisualizer.Instance.ClearData();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see pool data", MessageType.Info);
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
                case 1: DrawPools(); break;
                case 2: DrawLeaks(); break;
                case 3: DrawOptimization(); break;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawOverview()
        {
            var stats = MemoryPoolVisualizer.Instance.GetOverallStats();

            EditorGUILayout.LabelField("Overall Statistics", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField($"Total Pools: {stats.totalPools}");
            EditorGUILayout.LabelField($"Total Pooled Objects: {stats.totalPooledObjects}");
            EditorGUILayout.LabelField($"Active Objects: {stats.totalActiveObjects}");
            EditorGUILayout.LabelField($"Overall Hit Rate: {stats.overallHitRate:P2}");
            EditorGUILayout.LabelField($"Total Memory: {stats.totalMemoryKB / 1024f:F2} MB");

            if (stats.leakyPoolCount > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox($"⚠ {stats.leakyPoolCount} pool(s) have potential leaks!", MessageType.Warning);
            }

            // Hit rate progress bar
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Overall Hit Rate");
            Rect rect = GUILayoutUtility.GetRect(18, 18);
            EditorGUI.ProgressBar(rect, stats.overallHitRate, $"{stats.overallHitRate:P1}");
        }

        private void DrawPools()
        {
            var pools = MemoryPoolVisualizer.Instance.GetAllPools();

            EditorGUILayout.LabelField($"Active Pools ({pools.Count})", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (pools.Count == 0)
            {
                EditorGUILayout.HelpBox("No pools registered", MessageType.Info);
                return;
            }

            foreach (var pool in pools.OrderByDescending(p => p.estimatedMemoryKB))
            {
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField(pool.poolName, EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Size: {pool.totalSize} (Peak Active: {pool.peakActiveCount})");
                EditorGUILayout.LabelField($"Active: {pool.activeCount} ({pool.utilizationRate:P0})");
                EditorGUILayout.LabelField($"Requests: {pool.requests} (Hits: {pool.hits}, Misses: {pool.misses})");
                EditorGUILayout.LabelField($"Hit Rate: {pool.hitRate:P2}");
                EditorGUILayout.LabelField($"Memory: {pool.estimatedMemoryKB / 1024f:F2} MB");

                // Hit rate bar
                Rect rect = GUILayoutUtility.GetRect(18, 18);
                Color barColor = pool.hitRate > 0.9f ? Color.green : (pool.hitRate > 0.7f ? Color.yellow : Color.red);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width * pool.hitRate, rect.height), barColor);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawLeaks()
        {
            var leaks = MemoryPoolVisualizer.Instance.GetLeakyPools();

            EditorGUILayout.LabelField("Potential Memory Leaks", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (leaks.Count == 0)
            {
                EditorGUILayout.HelpBox("No leaks detected! ✓", MessageType.Info);
                return;
            }

            foreach (var pool in leaks)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox($"⚠ {pool.poolName}", MessageType.Warning);
                EditorGUILayout.LabelField($"Active Objects: {pool.activeCount}");
                EditorGUILayout.LabelField($"Time Since Last Return: {Time.time - pool.lastReturnTime:F1}s");
                EditorGUILayout.LabelField("Possible Causes:");
                EditorGUILayout.LabelField("• Objects not being returned to pool");
                EditorGUILayout.LabelField("• Exception during object lifetime");
                EditorGUILayout.LabelField("• Missing pool.Return() calls");
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawOptimization()
        {
            var pools = MemoryPoolVisualizer.Instance.GetAllPools();
            var inefficient = pools.Where(p => p.suggestedSize > 0 || p.hitRate < 0.9f).ToList();

            EditorGUILayout.LabelField("Optimization Suggestions", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (inefficient.Count == 0)
            {
                EditorGUILayout.HelpBox("All pools are optimized! ✓", MessageType.Info);
                return;
            }

            foreach (var pool in inefficient.OrderBy(p => p.hitRate))
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField(pool.poolName, EditorStyles.boldLabel);

                if (pool.hitRate < 0.9f)
                {
                    EditorGUILayout.HelpBox($"Low hit rate: {pool.hitRate:P1}", MessageType.Warning);
                    EditorGUILayout.LabelField($"Current Size: {pool.totalSize}");

                    if (pool.suggestedSize > 0)
                    {
                        EditorGUILayout.LabelField($"Suggested Size: {pool.suggestedSize}", EditorStyles.boldLabel);
                    }

                    EditorGUILayout.LabelField("Recommendations:");
                    EditorGUILayout.LabelField("• Increase pool size");
                    EditorGUILayout.LabelField($"• Currently missing {pool.misses} requests");
                }

                if (pool.utilizationRate < 0.5f && pool.hitRate > 0.99f)
                {
                    EditorGUILayout.HelpBox("Over-allocated pool", MessageType.Info);
                    EditorGUILayout.LabelField("Recommendations:");
                    EditorGUILayout.LabelField("• Consider reducing pool size");
                    EditorGUILayout.LabelField($"• Peak usage is only {pool.peakActiveCount}/{pool.totalSize}");
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
    }
#endif
}
