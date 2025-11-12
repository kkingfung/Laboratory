using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Laboratory.Tools;

namespace Laboratory.Editor.Tools
{
    /// <summary>
    /// Editor window for Asset Management System.
    /// Provides visual interface for tracking, reloading, and managing project assets.
    /// Menu: Chimera/Asset Management
    /// </summary>
    public class AssetManagementWindow : EditorWindow
    {
        #region Window Setup

        [MenuItem("Chimera/Asset Management")]
        public static void ShowWindow()
        {
            var window = GetWindow<AssetManagementWindow>("Asset Management");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        #endregion

        #region Private Fields

        private AssetManagementSystem _system;
        private Vector2 _scrollPosition;
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Overview", "Tracked Assets", "Dependencies", "Memory", "Settings" };

        // Filters
        private string _searchFilter = "";
        private Type _typeFilter;
        private bool _showOnlyWithDependents = false;

        // Sorting
        private AssetSortMode _sortMode = AssetSortMode.Path;
        private bool _sortAscending = true;

        // Selection
        private AssetInfo _selectedAsset;
        private List<UnityEngine.Object> _selectedDependents;

        // Stats refresh
        private AssetManagementStats _cachedStats;
        private double _lastStatsRefresh;
        private const double StatsRefreshInterval = 0.5;

        // Styles
        private GUIStyle _headerStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _buttonStyle;
        private bool _stylesInitialized;

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            FindOrCreateSystem();
            RefreshStats();
        }

        private void OnGUI()
        {
            InitializeStyles();

            if (_system == null)
            {
                DrawSystemNotFoundGUI();
                return;
            }

            DrawHeader();
            DrawTabs();

            EditorGUILayout.Space(10);

            switch (_selectedTab)
            {
                case 0: DrawOverviewTab(); break;
                case 1: DrawTrackedAssetsTab(); break;
                case 2: DrawDependenciesTab(); break;
                case 3: DrawMemoryTab(); break;
                case 4: DrawSettingsTab(); break;
            }
        }

        private void Update()
        {
            // Auto-refresh stats
            if (EditorApplication.timeSinceStartup - _lastStatsRefresh > StatsRefreshInterval)
            {
                RefreshStats();
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

            _stylesInitialized = true;
        }

        private void FindOrCreateSystem()
        {
            _system = AssetManagementSystem.Instance;

            if (_system == null)
            {
                // Try to find in scene
                _system = FindObjectOfType<AssetManagementSystem>();
            }
        }

        private void RefreshStats()
        {
            if (_system != null)
            {
                _cachedStats = _system.GetStats();
                _lastStatsRefresh = EditorApplication.timeSinceStartup;
            }
        }

        #endregion

        #region GUI - Header

        private void DrawHeader()
        {
            EditorGUILayout.LabelField("Asset Management System", _headerStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Refresh", _buttonStyle, GUILayout.Width(100)))
            {
                RefreshStats();
            }

            if (GUILayout.Button("Rescan Assets", _buttonStyle, GUILayout.Width(120)))
            {
                _system.Reset();
                _system.GetType().GetMethod("ScanProjectAssets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(_system, null);
                RefreshStats();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
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

            // Quick Stats
            EditorGUILayout.LabelField("Quick Statistics", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            DrawStatRow("Tracked Assets", _cachedStats.trackedAssetCount.ToString());
            DrawStatRow("Total Reloads", _cachedStats.totalReloads.ToString());
            DrawStatRow("Dependency Updates", _cachedStats.totalDependencyUpdates.ToString());
            DrawStatRow("Queued Reloads", _cachedStats.queuedReloads.ToString());
            DrawStatRow("Memory Saved", $"{_cachedStats.memorySavedMB:F2} MB");
            DrawStatRow("Current Memory", $"{_cachedStats.currentMemoryMB:F2} MB");

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // System Status
            EditorGUILayout.LabelField("System Status", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            DrawStatusRow("Hot Reload", _system.IsEnabled);
            DrawStatusRow("Prefab Watching", true); // Could expose this from system
            DrawStatusRow("Scene Watching", true);
            DrawStatusRow("Material Watching", true);

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Quick Actions
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            if (GUILayout.Button("Unload Unused Assets", GUILayout.Height(30)))
            {
                _system.UnloadUnusedAssets();
                RefreshStats();
            }

            if (GUILayout.Button("Clear All Tracking", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Clear Tracking",
                    "Are you sure you want to clear all tracked assets?",
                    "Yes", "No"))
                {
                    _system.Reset();
                    RefreshStats();
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawTrackedAssetsTab()
        {
            // Filters
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Search:", GUILayout.Width(60));
            _searchFilter = EditorGUILayout.TextField(_searchFilter);

            EditorGUILayout.LabelField("Sort By:", GUILayout.Width(60));
            _sortMode = (AssetSortMode)EditorGUILayout.EnumPopup(_sortMode, GUILayout.Width(120));

            if (GUILayout.Button(_sortAscending ? "▲" : "▼", GUILayout.Width(30)))
            {
                _sortAscending = !_sortAscending;
            }

            _showOnlyWithDependents = GUILayout.Toggle(_showOnlyWithDependents, "Has Dependents", GUILayout.Width(120));

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Asset List
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            var assets = GetFilteredAndSortedAssets();

            if (assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No assets match the current filters.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.LabelField($"Showing {assets.Count} assets", EditorStyles.miniLabel);

                foreach (var asset in assets)
                {
                    DrawAssetRow(asset);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawDependenciesTab()
        {
            EditorGUILayout.BeginHorizontal();

            // Left panel - Asset selection
            EditorGUILayout.BeginVertical(GUILayout.Width(position.width * 0.4f));
            EditorGUILayout.LabelField("Select Asset", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(position.height - 100));

            var assetsWithDependents = GetFilteredAndSortedAssets()
                .Where(a => a.dependentCount > 0)
                .ToList();

            foreach (var asset in assetsWithDependents)
            {
                bool isSelected = _selectedAsset == asset;
                if (GUILayout.Toggle(isSelected, $"{System.IO.Path.GetFileName(asset.path)} ({asset.dependentCount})", EditorStyles.radioButton))
                {
                    if (!isSelected)
                    {
                        _selectedAsset = asset;
                        _selectedDependents = _system.GetDependents(asset.path);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Right panel - Dependents
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Dependents", EditorStyles.boldLabel);

            if (_selectedAsset != null && _selectedDependents != null)
            {
                EditorGUILayout.LabelField($"Asset: {_selectedAsset.path}", EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Dependents: {_selectedDependents.Count}", EditorStyles.miniLabel);

                EditorGUILayout.Space(5);

                foreach (var dependent in _selectedDependents)
                {
                    if (dependent == null) continue;

                    EditorGUILayout.BeginHorizontal(_boxStyle);
                    EditorGUILayout.ObjectField(dependent, typeof(UnityEngine.Object), true);

                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeObject = dependent;
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Select an asset to view its dependents.", MessageType.Info);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawMemoryTab()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Memory Management", EditorStyles.boldLabel);

            // Memory Stats
            EditorGUILayout.BeginVertical(_boxStyle);
            DrawStatRow("Current Memory Usage", $"{_cachedStats.currentMemoryMB:F2} MB");
            DrawStatRow("Total Memory Saved", $"{_cachedStats.memorySavedMB:F2} MB");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // Memory Actions
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(_boxStyle);

            if (GUILayout.Button("Unload Unused Assets", GUILayout.Height(30)))
            {
                _system.UnloadUnusedAssets();
                RefreshStats();
            }

            if (GUILayout.Button("Force Garbage Collection", GUILayout.Height(30)))
            {
                GC.Collect();
                RefreshStats();
                EditorUtility.DisplayDialog("GC Complete", "Garbage collection completed.", "OK");
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }

        private void DrawSettingsTab()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            EditorGUILayout.LabelField("Asset Management Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Settings are configured on the AssetManagementSystem component in the scene.", MessageType.Info);

            if (_system != null)
            {
                var serializedObject = new SerializedObject(_system);
                serializedObject.Update();

                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableHotReload"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("watchPrefabs"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("watchScenes"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("watchMaterials"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("watchTextures"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("watchAudioClips"));

                EditorGUILayout.Space(10);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("updateCheckInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxAssetsPerFrame"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("logReloadEvents"));

                EditorGUILayout.Space(10);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("autoUnloadUnusedAssets"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("unusedAssetCheckInterval"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("memoryThresholdMB"));

                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
        }

        #endregion

        #region GUI - Helper Methods

        private void DrawStatRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatusRow(string label, bool status)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(label, GUILayout.Width(200));
            EditorGUILayout.LabelField(status ? "✓ Enabled" : "✗ Disabled",
                status ? EditorStyles.boldLabel : EditorStyles.label);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetRow(AssetInfo asset)
        {
            EditorGUILayout.BeginHorizontal(_boxStyle);

            // Asset icon and name
            var assetObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.path);
            EditorGUILayout.ObjectField(assetObj, typeof(UnityEngine.Object), false, GUILayout.Width(200));

            // Path
            EditorGUILayout.LabelField(asset.path, GUILayout.ExpandWidth(true));

            // Stats
            EditorGUILayout.LabelField($"Reloads: {asset.reloadCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"Deps: {asset.dependentCount}", GUILayout.Width(60));

            // Actions
            if (GUILayout.Button("Reload", GUILayout.Width(60)))
            {
                _system.ForceReloadAsset(asset.path);
            }

            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = assetObj;
                EditorGUIUtility.PingObject(assetObj);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSystemNotFoundGUI()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("AssetManagementSystem not found in scene.", MessageType.Warning);

            if (GUILayout.Button("Create AssetManagementSystem", GUILayout.Height(40)))
            {
                var go = new GameObject("AssetManagementSystem");
                _system = go.AddComponent<AssetManagementSystem>();
                EditorUtility.DisplayDialog("System Created",
                    "AssetManagementSystem has been created in the scene.",
                    "OK");
            }
        }

        #endregion

        #region Filtering and Sorting

        private List<AssetInfo> GetFilteredAndSortedAssets()
        {
            if (_system == null) return new List<AssetInfo>();

            var assets = new List<AssetInfo>();

            // Get all tracked assets via reflection (since TrackedAssets is private)
            var trackedAssetsField = typeof(AssetManagementSystem).GetField("_trackedAssets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (trackedAssetsField != null)
            {
                var trackedAssets = trackedAssetsField.GetValue(_system) as Dictionary<string, AssetInfo>;
                if (trackedAssets != null)
                {
                    assets.AddRange(trackedAssets.Values);
                }
            }

            // Apply filters
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                assets = assets.Where(a => a.path.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }

            if (_showOnlyWithDependents)
            {
                assets = assets.Where(a => a.dependentCount > 0).ToList();
            }

            if (_typeFilter != null)
            {
                assets = assets.Where(a => a.assetType == _typeFilter).ToList();
            }

            // Apply sorting
            assets = SortAssets(assets);

            return assets;
        }

        private List<AssetInfo> SortAssets(List<AssetInfo> assets)
        {
            IOrderedEnumerable<AssetInfo> sorted = null;

            switch (_sortMode)
            {
                case AssetSortMode.Path:
                    sorted = _sortAscending
                        ? assets.OrderBy(a => a.path)
                        : assets.OrderByDescending(a => a.path);
                    break;

                case AssetSortMode.Type:
                    sorted = _sortAscending
                        ? assets.OrderBy(a => a.assetType?.Name ?? "")
                        : assets.OrderByDescending(a => a.assetType?.Name ?? "");
                    break;

                case AssetSortMode.ReloadCount:
                    sorted = _sortAscending
                        ? assets.OrderBy(a => a.reloadCount)
                        : assets.OrderByDescending(a => a.reloadCount);
                    break;

                case AssetSortMode.DependentCount:
                    sorted = _sortAscending
                        ? assets.OrderBy(a => a.dependentCount)
                        : assets.OrderByDescending(a => a.dependentCount);
                    break;

                case AssetSortMode.LastModified:
                    sorted = _sortAscending
                        ? assets.OrderBy(a => a.lastModified)
                        : assets.OrderByDescending(a => a.lastModified);
                    break;
            }

            return sorted?.ToList() ?? assets;
        }

        #endregion

        #region Enums

        private enum AssetSortMode
        {
            Path,
            Type,
            ReloadCount,
            DependentCount,
            LastModified
        }

        #endregion
    }
}
