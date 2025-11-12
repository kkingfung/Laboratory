using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Laboratory.Tools;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Editor window for Memory Profiler.
    /// Provides visualization of memory snapshots, leak detection, and allocation tracking.
    /// Menu: Chimera/Memory Profiler
    /// </summary>
    public class MemoryProfilerWindow : EditorWindow
    {
        #region Window Setup

        [MenuItem("Chimera/Memory Profiler")]
        public static void ShowWindow()
        {
            var window = GetWindow<MemoryProfilerWindow>("Memory Profiler");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }

        #endregion

        #region Private Fields

        private MemoryProfiler _profiler;
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Overview", "Snapshots", "Leak Detection", "Allocation Tracking", "Settings" };

        // Snapshot visualization
        private List<MemorySnapshot> _cachedSnapshots;
        private MemorySnapshot _selectedSnapshot;
        private int _compareSnapshotIndex1 = -1;
        private int _compareSnapshotIndex2 = -1;

        // Graph settings
        private bool _showTotalMemory = true;
        private bool _showGCMemory = true;
        private bool _showManagedMemory = true;
        private bool _showTextureMemory = false;
        private bool _showMeshMemory = false;
        private Rect _graphRect;

        // Leak detection
        private List<MemoryLeak> _cachedLeaks;

        // Allocation tracking
        private string _newTrackerName = "";
        private readonly List<string> _activeTrackers = new List<string>();

        // Stats
        private MemoryProfilerStats _cachedStats;
        private double _lastStatsRefresh;
        private const double StatsRefreshInterval = 0.5;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _warningStyle;
        private GUIStyle _errorStyle;
        private bool _stylesInitialized;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            FindOrCreateProfiler();
            RefreshData();
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (_profiler == null)
            {
                DrawProfilerNotFoundGUI();
                return;
            }

            DrawHeader();
            DrawTabs();

            EditorGUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0: DrawOverviewTab(); break;
                case 1: DrawSnapshotsTab(); break;
                case 2: DrawLeakDetectionTab(); break;
                case 3: DrawAllocationTrackingTab(); break;
                case 4: DrawSettingsTab(); break;
            }
        }

        private void Update()
        {
            // Auto-refresh
            if (EditorApplication.timeSinceStartup - _lastStatsRefresh > StatsRefreshInterval)
            {
                RefreshData();
                Repaint();
            }
        }

        #endregion

        #region Initialization

        private void InitializeStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                margin = new RectOffset(0, 0, 10, 10)
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(10, 10, 5, 5)
            };

            _warningStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = new Color(1f, 0.5f, 0f) }
            };

            _errorStyle = new GUIStyle(EditorStyles.label)
            {
                normal = { textColor = Color.red },
                fontStyle = FontStyle.Bold
            };

            _stylesInitialized = true;
        }

        private void FindOrCreateProfiler()
        {
            _profiler = MemoryProfiler.Instance;

            if (_profiler == null)
            {
                _profiler = FindObjectOfType<MemoryProfiler>();
            }
        }

        private void RefreshData()
        {
            if (_profiler != null)
            {
                _cachedStats = _profiler.GetStats();
                _cachedSnapshots = _profiler.GetAllSnapshots();
                _cachedLeaks = _profiler.GetDetectedLeaks();
                _lastStatsRefresh = EditorApplication.timeSinceStartup;
            }
        }

        #endregion

        #region GUI - Header

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Memory Profiler", _headerStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Take Snapshot", _buttonStyle, GUILayout.Width(120)))
            {
                _profiler.TakeSnapshot();
                RefreshData();
            }

            if (GUILayout.Button("Force GC", _buttonStyle, GUILayout.Width(100)))
            {
                _profiler.ForceGCAndSnapshot();
                RefreshData();
            }

            if (GUILayout.Button("Refresh", _buttonStyle, GUILayout.Width(80)))
            {
                RefreshData();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Current memory status bar
            DrawMemoryStatusBar();
        }

        private void DrawMemoryStatusBar()
        {
            EditorGUILayout.BeginHorizontal(_boxStyle);

            EditorGUILayout.LabelField($"Current: {_cachedStats.currentMemoryMB} MB", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Peak: {_cachedStats.peakMemoryMB} MB", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Snapshots: {_cachedStats.snapshotCount}", GUILayout.Width(120));

            if (_cachedStats.detectedLeaksCount > 0)
            {
                EditorGUILayout.LabelField($"⚠ Leaks: {_cachedStats.detectedLeaksCount}", _errorStyle, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
        }

        #endregion

        #region GUI - Tabs

        private void DrawOverviewTab()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Memory Summary
            EditorGUILayout.LabelField("Memory Summary", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_cachedSnapshots != null && _cachedSnapshots.Count > 0)
            {
                var latest = _cachedSnapshots.Last();
                DrawMemoryStat("Total Reserved", latest.totalReservedMemory);
                DrawMemoryStat("Total Used", latest.totalUsedMemory, latest.totalMemoryDelta);
                DrawMemoryStat("GC Reserved", latest.gcReservedMemory);
                DrawMemoryStat("GC Used", latest.gcUsedMemory, latest.gcMemoryDelta);
                DrawMemoryStat("Managed Heap", latest.managedHeapSize, latest.managedMemoryDelta);
                DrawMemoryStat("Mono Heap", latest.monoHeapSize);
                DrawMemoryStat("Mono Used", latest.monoUsedSize);
            }
            else
            {
                EditorGUILayout.HelpBox("No snapshots taken yet. Click 'Take Snapshot' to begin profiling.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Asset Memory
            EditorGUILayout.LabelField("Asset Memory", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_cachedSnapshots != null && _cachedSnapshots.Count > 0)
            {
                var latest = _cachedSnapshots.Last();
                DrawMemoryStat("Textures", latest.textureMemory);
                DrawMemoryStat("Meshes", latest.meshMemory);
                DrawMemoryStat("Audio", latest.audioMemory);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Object Counts
            EditorGUILayout.LabelField("Object Counts", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            if (_cachedSnapshots != null && _cachedSnapshots.Count > 0)
            {
                var latest = _cachedSnapshots.Last();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Total Objects:", GUILayout.Width(200));
                EditorGUILayout.LabelField(latest.totalObjectCount.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("GameObjects:", GUILayout.Width(200));
                EditorGUILayout.LabelField(latest.gameObjectCount.ToString(), EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Memory Graph
            EditorGUILayout.LabelField("Memory History", EditorStyles.boldLabel);
            DrawMemoryGraph();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSnapshotsTab()
        {
            EditorGUILayout.BeginHorizontal();

            // Snapshot comparison
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.LabelField("Compare Snapshots", EditorStyles.boldLabel);

            if (_cachedSnapshots != null && _cachedSnapshots.Count >= 2)
            {
                string[] snapshotNames = _cachedSnapshots.Select((s, i) => $"#{i} - {s.timestamp:HH:mm:ss}").ToArray();

                _compareSnapshotIndex1 = EditorGUILayout.Popup("Snapshot 1", _compareSnapshotIndex1, snapshotNames);
                _compareSnapshotIndex2 = EditorGUILayout.Popup("Snapshot 2", _compareSnapshotIndex2, snapshotNames);

                if (GUILayout.Button("Compare", GUILayout.Height(30)))
                {
                    if (_compareSnapshotIndex1 >= 0 && _compareSnapshotIndex2 >= 0)
                    {
                        var diff = _profiler.CompareSnapshots(
                            _cachedSnapshots[_compareSnapshotIndex1],
                            _cachedSnapshots[_compareSnapshotIndex2]);

                        if (diff != null)
                        {
                            ShowSnapshotComparison(diff);
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Need at least 2 snapshots to compare.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();

            // Snapshot list
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Snapshot History", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_cachedSnapshots != null && _cachedSnapshots.Count > 0)
            {
                for (int i = _cachedSnapshots.Count - 1; i >= 0; i--)
                {
                    DrawSnapshotRow(_cachedSnapshots[i], i);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No snapshots available.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeakDetectionTab()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Memory Leak Detection", EditorStyles.boldLabel);

            if (!_cachedStats.isLeakDetectionEnabled)
            {
                EditorGUILayout.HelpBox("Leak detection is disabled. Enable it in Settings tab.", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Detected Leaks: {_cachedLeaks?.Count ?? 0}", EditorStyles.boldLabel);

            if (GUILayout.Button("Clear Leaks", GUILayout.Width(100)))
            {
                _profiler.ClearDetectedLeaks();
                RefreshData();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (_cachedLeaks != null && _cachedLeaks.Count > 0)
            {
                foreach (var leak in _cachedLeaks)
                {
                    DrawLeakInfo(leak);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No memory leaks detected. This is good! ✓", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawAllocationTrackingTab()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Allocation Tracking", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Track memory allocations for specific operations or categories.", MessageType.Info);

            EditorGUILayout.Space(10);

            // Start new tracker
            EditorGUILayout.LabelField("Start New Tracker", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Category:", GUILayout.Width(80));
            _newTrackerName = EditorGUILayout.TextField(_newTrackerName);

            if (GUILayout.Button("Start Tracking", GUILayout.Width(120)))
            {
                if (!string.IsNullOrEmpty(_newTrackerName))
                {
                    _profiler.StartTrackingAllocations(_newTrackerName);
                    _activeTrackers.Add(_newTrackerName);
                    _newTrackerName = "";
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Active trackers
            if (_activeTrackers.Count > 0)
            {
                EditorGUILayout.LabelField("Active Trackers", EditorStyles.boldLabel);

                for (int i = _activeTrackers.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal(_boxStyle);
                    EditorGUILayout.LabelField(_activeTrackers[i], EditorStyles.boldLabel);

                    if (GUILayout.Button("Stop", GUILayout.Width(60)))
                    {
                        var result = _profiler.StopTrackingAllocations(_activeTrackers[i]);
                        _activeTrackers.RemoveAt(i);

                        if (result != null)
                        {
                            EditorUtility.DisplayDialog("Tracking Complete",
                                $"Category: {result.category}\n" +
                                $"Duration: {result.duration:F2}s\n" +
                                $"Allocated: {result.totalAllocated / 1024f / 1024f:F2} MB\n" +
                                $"Rate: {(result.totalAllocated / result.duration) / 1024f / 1024f:F2} MB/s",
                                "OK");
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No active trackers. Start one above to begin tracking allocations.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsTab()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Memory Profiler Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Settings are configured on the MemoryProfiler component in the scene.", MessageType.Info);

            if (_profiler != null)
            {
                var serializedObject = new SerializedObject(_profiler);
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableProfiling"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLeakDetection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("snapshotInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxSnapshots"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("logMemoryWarnings"));

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Leak Detection", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("leakThresholdMB"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minSnapshotsForLeakDetection"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("leakGrowthRateThreshold"));

                EditorGUILayout.Space(10);

                EditorGUILayout.LabelField("Memory Thresholds", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("warningThresholdMB"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("criticalThresholdMB"));

                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(20);

            // Export report
            if (GUILayout.Button("Export Memory Report", GUILayout.Height(40)))
            {
                string report = _profiler.GenerateMemoryReport();
                string path = EditorUtility.SaveFilePanel("Save Memory Report", "", "memory_report.txt", "txt");

                if (!string.IsNullOrEmpty(path))
                {
                    System.IO.File.WriteAllText(path, report);
                    EditorUtility.DisplayDialog("Export Complete", $"Memory report saved to:\n{path}", "OK");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region GUI - Helper Methods

        private void DrawMemoryStat(string label, long bytes, long delta = 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));
            EditorGUILayout.LabelField(FormatBytes(bytes), EditorStyles.boldLabel, GUILayout.Width(100));

            if (delta != 0)
            {
                string deltaStr = (delta > 0 ? "+" : "") + FormatBytes(delta);
                var style = delta > 0 ? _warningStyle : EditorStyles.label;
                EditorGUILayout.LabelField(deltaStr, style, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSnapshotRow(MemorySnapshot snapshot, int index)
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Snapshot #{index}", EditorStyles.boldLabel, GUILayout.Width(100));
            EditorGUILayout.LabelField($"{snapshot.timestamp:yyyy-MM-dd HH:mm:ss}", GUILayout.Width(150));
            EditorGUILayout.LabelField($"Frame: {snapshot.frameCount}", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Total: {FormatBytes(snapshot.totalUsedMemory)}", GUILayout.Width(150));

            if (snapshot.totalMemoryDelta != 0)
            {
                string delta = (snapshot.totalMemoryDelta > 0 ? "+" : "") + FormatBytes(snapshot.totalMemoryDelta);
                var style = snapshot.totalMemoryDelta > 0 ? _warningStyle : EditorStyles.label;
                EditorGUILayout.LabelField(delta, style, GUILayout.Width(100));
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawLeakInfo(MemoryLeak leak)
        {
            EditorGUILayout.BeginVertical(_boxStyle);

            EditorGUILayout.LabelField($"⚠ {leak.category}", _errorStyle);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Detected:", GUILayout.Width(100));
            EditorGUILayout.LabelField(leak.detectedAt.ToString("HH:mm:ss"));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Growth:", GUILayout.Width(100));
            EditorGUILayout.LabelField(FormatMemoryOrCount(leak.currentValue - leak.initialValue, leak.isObjectCountLeak));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Growth Rate:", GUILayout.Width(100));
            string unit = leak.isObjectCountLeak ? "objects/sec" : "MB/sec";
            EditorGUILayout.LabelField($"{leak.growthRate:F2} {unit}", _warningStyle);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawMemoryGraph()
        {
            if (_cachedSnapshots == null || _cachedSnapshots.Count < 2)
            {
                EditorGUILayout.HelpBox("Need at least 2 snapshots to display graph.", MessageType.Info);
                return;
            }

            // Graph controls
            EditorGUILayout.BeginHorizontal();
            _showTotalMemory = GUILayout.Toggle(_showTotalMemory, "Total", GUILayout.Width(80));
            _showGCMemory = GUILayout.Toggle(_showGCMemory, "GC", GUILayout.Width(80));
            _showManagedMemory = GUILayout.Toggle(_showManagedMemory, "Managed", GUILayout.Width(80));
            _showTextureMemory = GUILayout.Toggle(_showTextureMemory, "Textures", GUILayout.Width(80));
            _showMeshMemory = GUILayout.Toggle(_showMeshMemory, "Meshes", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // Draw graph
            _graphRect = GUILayoutUtility.GetRect(position.width - 20, 200);
            DrawGraph(_graphRect);
        }

        private void DrawGraph(Rect rect)
        {
            // Background
            EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.1f));

            // Border
            Handles.color = Color.gray;
            Handles.DrawLine(new Vector3(rect.xMin, rect.yMin), new Vector3(rect.xMax, rect.yMin));
            Handles.DrawLine(new Vector3(rect.xMax, rect.yMin), new Vector3(rect.xMax, rect.yMax));
            Handles.DrawLine(new Vector3(rect.xMax, rect.yMax), new Vector3(rect.xMin, rect.yMax));
            Handles.DrawLine(new Vector3(rect.xMin, rect.yMax), new Vector3(rect.xMin, rect.yMin));

            // Find max value for scaling
            long maxValue = 0;
            foreach (var snapshot in _cachedSnapshots)
            {
                if (_showTotalMemory) maxValue = Math.Max(maxValue, snapshot.totalUsedMemory);
                if (_showGCMemory) maxValue = Math.Max(maxValue, snapshot.gcUsedMemory);
                if (_showManagedMemory) maxValue = Math.Max(maxValue, snapshot.managedHeapSize);
                if (_showTextureMemory) maxValue = Math.Max(maxValue, snapshot.textureMemory);
                if (_showMeshMemory) maxValue = Math.Max(maxValue, snapshot.meshMemory);
            }

            if (maxValue == 0) return;

            // Draw lines
            if (_showTotalMemory) DrawGraphLine(rect, _cachedSnapshots, s => s.totalUsedMemory, maxValue, Color.white);
            if (_showGCMemory) DrawGraphLine(rect, _cachedSnapshots, s => s.gcUsedMemory, maxValue, Color.yellow);
            if (_showManagedMemory) DrawGraphLine(rect, _cachedSnapshots, s => s.managedHeapSize, maxValue, Color.cyan);
            if (_showTextureMemory) DrawGraphLine(rect, _cachedSnapshots, s => s.textureMemory, maxValue, Color.green);
            if (_showMeshMemory) DrawGraphLine(rect, _cachedSnapshots, s => s.meshMemory, maxValue, Color.magenta);
        }

        private void DrawGraphLine(Rect rect, List<MemorySnapshot> snapshots, Func<MemorySnapshot, long> valueSelector, long maxValue, Color color)
        {
            if (snapshots.Count < 2) return;

            Handles.color = color;

            float stepX = rect.width / (snapshots.Count - 1);

            for (int i = 0; i < snapshots.Count - 1; i++)
            {
                float x1 = rect.xMin + i * stepX;
                float x2 = rect.xMin + (i + 1) * stepX;

                float y1 = rect.yMax - (valueSelector(snapshots[i]) / (float)maxValue) * rect.height;
                float y2 = rect.yMax - (valueSelector(snapshots[i + 1]) / (float)maxValue) * rect.height;

                Handles.DrawLine(new Vector3(x1, y1), new Vector3(x2, y2));
            }
        }

        private void ShowSnapshotComparison(MemoryDiff diff)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("SNAPSHOT COMPARISON");
            sb.AppendLine($"Time Span: {diff.timeSpan:F1}s");
            sb.AppendLine();
            sb.AppendLine($"Total Memory Change: {FormatBytes(diff.totalMemoryChange)}");
            sb.AppendLine($"GC Memory Change: {FormatBytes(diff.gcMemoryChange)}");
            sb.AppendLine($"Managed Memory Change: {FormatBytes(diff.managedMemoryChange)}");
            sb.AppendLine($"Texture Memory Change: {FormatBytes(diff.textureMemoryChange)}");
            sb.AppendLine($"Mesh Memory Change: {FormatBytes(diff.meshMemoryChange)}");
            sb.AppendLine($"Audio Memory Change: {FormatBytes(diff.audioMemoryChange)}");
            sb.AppendLine($"GameObject Count Change: {diff.gameObjectCountChange}");

            EditorUtility.DisplayDialog("Snapshot Comparison", sb.ToString(), "OK");
        }

        private void DrawProfilerNotFoundGUI()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("MemoryProfiler not found in scene.", MessageType.Warning);

            if (GUILayout.Button("Create MemoryProfiler", GUILayout.Height(40)))
            {
                var go = new GameObject("MemoryProfiler");
                _profiler = go.AddComponent<MemoryProfiler>();
                EditorUtility.DisplayDialog("Profiler Created",
                    "MemoryProfiler has been created in the scene.",
                    "OK");
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024f:F2} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / 1024f / 1024f:F2} MB";
            else
                return $"{bytes / 1024f / 1024f / 1024f:F2} GB";
        }

        private string FormatMemoryOrCount(long value, bool isCount)
        {
            return isCount ? $"{value} objects" : FormatBytes(value);
        }

        #endregion
    }
}
