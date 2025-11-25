using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace Laboratory.Tools
{
    /// <summary>
    /// Hot reload system for instant ScriptableObject and configuration updates
    /// Dramatically speeds up iteration by reloading configs without entering play mode
    /// Monitors file changes and automatically reloads affected assets
    /// </summary>
#if UNITY_EDITOR
    [InitializeOnLoad]
    public class HotReloadSystem
    {
        private static readonly Dictionary<string, DateTime> _fileWatchList = new Dictionary<string, DateTime>();
        private static readonly HashSet<string> _reloadQueue = new HashSet<string>();
        private static FileSystemWatcher _fileWatcher;
        private static bool _isEnabled = true;
        private static float _lastReloadTime = 0f;
        private static int _reloadCount = 0;

        // Configuration
        private const float RELOAD_DEBOUNCE_TIME = 0.5f; // Seconds to wait after last file change
        private static readonly string[] WATCHED_EXTENSIONS = { ".asset", ".json", ".txt", ".xml" };
        private static readonly string[] WATCHED_FOLDERS =
        {
            "Assets/_Project/Configs",
            "Assets/_Project/Data",
            "Assets/_Project/Resources/Configs"
        };

        static HotReloadSystem()
        {
            EditorApplication.update += OnEditorUpdate;
            InitializeFileWatcher();

            Debug.Log("[HotReloadSystem] Initialized - Monitoring ScriptableObject changes");
        }

        #region File Watching

        private static void InitializeFileWatcher()
        {
            try
            {
                string projectPath = Path.GetFullPath(Application.dataPath + "/..");

                _fileWatcher = new FileSystemWatcher(projectPath)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = true
                };

                _fileWatcher.Changed += OnFileChanged;
                _fileWatcher.Created += OnFileChanged;
                _fileWatcher.Renamed += OnFileRenamed;

                Debug.Log("[HotReloadSystem] File watcher initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HotReloadSystem] Failed to initialize file watcher: {ex.Message}");
            }
        }

        private static void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!_isEnabled) return;
            if (!ShouldWatchFile(e.FullPath)) return;

            string relativePath = GetRelativePath(e.FullPath);

            lock (_reloadQueue)
            {
                if (!_reloadQueue.Contains(relativePath))
                {
                    _reloadQueue.Add(relativePath);
                    _lastReloadTime = Time.realtimeSinceStartup;
                }
            }
        }

        private static void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (!_isEnabled) return;

            OnFileChanged(sender, e);
        }

        private static bool ShouldWatchFile(string fullPath)
        {
            string extension = Path.GetExtension(fullPath).ToLower();
            if (!WATCHED_EXTENSIONS.Contains(extension))
                return false;

            string relativePath = GetRelativePath(fullPath);

            return WATCHED_FOLDERS.Any(folder => relativePath.StartsWith(folder, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetRelativePath(string fullPath)
        {
            string projectPath = Path.GetFullPath(Application.dataPath + "/..");
            if (fullPath.StartsWith(projectPath))
            {
                return fullPath.Substring(projectPath.Length + 1).Replace('\\', '/');
            }
            return fullPath;
        }

        #endregion

        #region Hot Reload Logic

        private static void OnEditorUpdate()
        {
            if (!_isEnabled) return;
            if (_reloadQueue.Count == 0) return;

            // Debounce: wait for file changes to settle
            float timeSinceLastChange = Time.realtimeSinceStartup - _lastReloadTime;
            if (timeSinceLastChange < RELOAD_DEBOUNCE_TIME)
                return;

            List<string> filesToReload;
            lock (_reloadQueue)
            {
                filesToReload = new List<string>(_reloadQueue);
                _reloadQueue.Clear();
            }

            ReloadFiles(filesToReload);
        }

        private static void ReloadFiles(List<string> files)
        {
            try
            {
                foreach (var file in files)
                {
                    ReloadAsset(file);
                }

                AssetDatabase.Refresh();
                _reloadCount += files.Count;

                Debug.Log($"[HotReloadSystem] Reloaded {files.Count} asset(s). Total reloads: {_reloadCount}");

                // Notify listeners
                OnAssetsReloaded?.Invoke(files);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HotReloadSystem] Reload failed: {ex.Message}");
            }
        }

        private static void ReloadAsset(string assetPath)
        {
            try
            {
                // Load the asset
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

                if (asset == null)
                {
                    Debug.LogWarning($"[HotReloadSystem] Asset not found: {assetPath}");
                    return;
                }

                // Special handling for ScriptableObjects
                if (asset is ScriptableObject scriptableObject)
                {
                    ReloadScriptableObject(scriptableObject, assetPath);
                }

                // Mark asset as dirty to trigger Unity's internal reload
                EditorUtility.SetDirty(asset);

                Debug.Log($"[HotReloadSystem] Reloaded: {assetPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HotReloadSystem] Failed to reload {assetPath}: {ex.Message}");
            }
        }

        private static void ReloadScriptableObject(ScriptableObject so, string assetPath)
        {
            // Trigger OnEnable for runtime reinitialization
            var onEnableMethod = so.GetType().GetMethod("OnEnable",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            onEnableMethod?.Invoke(so, null);

            // Notify systems that use this ScriptableObject
            NotifyScriptableObjectChanged(so);
        }

        private static void NotifyScriptableObjectChanged(ScriptableObject so)
        {
            // Find all components/systems that reference this SO
            var components = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

            foreach (var component in components)
            {
                var type = component.GetType();
                var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                          System.Reflection.BindingFlags.NonPublic |
                                          System.Reflection.BindingFlags.Instance);

                foreach (var field in fields)
                {
                    if (field.FieldType == so.GetType() || field.FieldType.IsAssignableFrom(so.GetType()))
                    {
                        var fieldValue = field.GetValue(component);
                        if (fieldValue == so)
                        {
                            // Found a component using this SO, try to reinitialize it
                            var reinitMethod = type.GetMethod("OnConfigReloaded",
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance);

                            reinitMethod?.Invoke(component, new object[] { so });
                        }
                    }
                }
            }
        }

        #endregion

        #region Public API

        public static event Action<List<string>> OnAssetsReloaded;

        public static void Enable()
        {
            _isEnabled = true;
            Debug.Log("[HotReloadSystem] Enabled");
        }

        public static void Disable()
        {
            _isEnabled = false;
            Debug.Log("[HotReloadSystem] Disabled");
        }

        public static void Toggle()
        {
            _isEnabled = !_isEnabled;
            Debug.Log($"[HotReloadSystem] {(_isEnabled ? "Enabled" : "Disabled")}");
        }

        public static bool IsEnabled => _isEnabled;

        public static int GetReloadCount() => _reloadCount;

        public static void ResetStats()
        {
            _reloadCount = 0;
            Debug.Log("[HotReloadSystem] Stats reset");
        }

        public static void AddWatchFolder(string folderPath)
        {
            var list = WATCHED_FOLDERS.ToList();
            if (!list.Contains(folderPath))
            {
                list.Add(folderPath);
                Debug.Log($"[HotReloadSystem] Added watch folder: {folderPath}");
            }
        }

        #endregion

        #region Editor Menu

        [MenuItem("Chimera/Hot Reload/Enable", false, 100)]
        private static void EnableHotReload()
        {
            Enable();
        }

        [MenuItem("Chimera/Hot Reload/Disable", false, 101)]
        private static void DisableHotReload()
        {
            Disable();
        }

        [MenuItem("Chimera/Hot Reload/Toggle", false, 102)]
        private static void ToggleHotReload()
        {
            Toggle();
        }

        [MenuItem("Chimera/Hot Reload/Reload All Configs Now", false, 120)]
        private static void ForceReloadAllConfigs()
        {
            var configFiles = AssetDatabase.FindAssets("t:ScriptableObject", WATCHED_FOLDERS)
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .ToList();

            ReloadFiles(configFiles);

            Debug.Log($"[HotReloadSystem] Force reloaded {configFiles.Count} configs");
        }

        [MenuItem("Chimera/Hot Reload/Show Stats", false, 140)]
        private static void ShowStats()
        {
            EditorUtility.DisplayDialog("Hot Reload Stats",
                $"Enabled: {_isEnabled}\n" +
                $"Total Reloads: {_reloadCount}\n" +
                $"Watched Folders: {WATCHED_FOLDERS.Length}\n" +
                $"Queue Size: {_reloadQueue.Count}",
                "OK");
        }

        [MenuItem("Chimera/Hot Reload/Reset Stats", false, 141)]
        private static void ResetStatsMenu()
        {
            ResetStats();
        }

        #endregion

        #region Cleanup

        ~HotReloadSystem()
        {
            _fileWatcher?.Dispose();
        }

        #endregion
    }

    /// <summary>
    /// Add this interface to MonoBehaviours that want to be notified of config reloads
    /// </summary>
    public interface IHotReloadable
    {
        void OnConfigReloaded(ScriptableObject config);
    }

    /// <summary>
    /// Editor window for Hot Reload system monitoring
    /// </summary>
    public class HotReloadWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private List<string> _recentReloads = new List<string>();
        private const int MAX_RECENT_RELOADS = 50;

        [MenuItem("Chimera/Hot Reload/Open Dashboard", false, 150)]
        private static void ShowWindow()
        {
            var window = GetWindow<HotReloadWindow>("Hot Reload Dashboard");
            window.minSize = new Vector2(400, 300);
            window.Show();
        }

        private void OnEnable()
        {
            HotReloadSystem.OnAssetsReloaded += OnAssetsReloaded;
        }

        private void OnDisable()
        {
            HotReloadSystem.OnAssetsReloaded -= OnAssetsReloaded;
        }

        private void OnAssetsReloaded(List<string> assets)
        {
            foreach (var asset in assets)
            {
                _recentReloads.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {asset}");
            }

            while (_recentReloads.Count > MAX_RECENT_RELOADS)
            {
                _recentReloads.RemoveAt(_recentReloads.Count - 1);
            }

            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // Header
            EditorGUILayout.LabelField("Hot Reload Dashboard", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Status
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status:", GUILayout.Width(60));
            GUI.color = HotReloadSystem.IsEnabled ? Color.green : Color.red;
            EditorGUILayout.LabelField(HotReloadSystem.IsEnabled ? "ENABLED" : "DISABLED", EditorStyles.boldLabel);
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Stats
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Statistics", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Total Reloads: {HotReloadSystem.GetReloadCount()}");
            EditorGUILayout.LabelField($"Recent Reloads: {_recentReloads.Count}");
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);

            // Controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(HotReloadSystem.IsEnabled ? "Disable" : "Enable", GUILayout.Height(30)))
            {
                HotReloadSystem.Toggle();
            }
            if (GUILayout.Button("Reload All Configs", GUILayout.Height(30)))
            {
                HotReloadSystem.Toggle();
                EditorApplication.ExecuteMenuItem("Chimera/Hot Reload/Reload All Configs Now");
            }
            if (GUILayout.Button("Reset Stats", GUILayout.Height(30)))
            {
                HotReloadSystem.ResetStats();
                _recentReloads.Clear();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Recent reloads list
            EditorGUILayout.LabelField("Recent Reloads:", EditorStyles.boldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_recentReloads.Count == 0)
            {
                EditorGUILayout.HelpBox("No reloads yet. Edit a ScriptableObject to see hot reload in action!", MessageType.Info);
            }
            else
            {
                foreach (var reload in _recentReloads)
                {
                    EditorGUILayout.LabelField(reload, EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
#endif
}
