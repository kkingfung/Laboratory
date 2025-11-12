using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Laboratory.Tools
{
    /// <summary>
    /// Advanced asset management system with hot reload for prefabs, scenes, and materials.
    /// Complements HotReloadSystem.cs with support for non-ScriptableObject assets.
    /// Provides asset dependency tracking, batch updates, and memory optimization.
    /// </summary>
    public class AssetManagementSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Hot Reload Settings")]
        [SerializeField] private bool enableHotReload = true;
        [SerializeField] private bool watchPrefabs = true;
        [SerializeField] private bool watchScenes = true;
        [SerializeField] private bool watchMaterials = true;
        [SerializeField] private bool watchTextures = false;
        [SerializeField] private bool watchAudioClips = false;

        [Header("Performance")]
        [SerializeField] private float updateCheckInterval = 0.5f;
        [SerializeField] private int maxAssetsPerFrame = 10;
        [SerializeField] private bool logReloadEvents = true;

        [Header("Memory Management")]
        [SerializeField] private bool autoUnloadUnusedAssets = true;
        [SerializeField] private float unusedAssetCheckInterval = 30f;
        [SerializeField] private int memoryThresholdMB = 500;

        #endregion

        #region Private Fields

        private static AssetManagementSystem _instance;
        private readonly Dictionary<string, AssetInfo> _trackedAssets = new Dictionary<string, AssetInfo>();
        private readonly Dictionary<string, HashSet<UnityEngine.Object>> _assetDependents = new Dictionary<string, HashSet<UnityEngine.Object>>();
        private readonly Queue<string> _reloadQueue = new Queue<string>();
        private readonly HashSet<string> _loadedAssetPaths = new HashSet<string>();

#if UNITY_EDITOR
        private FileSystemWatcher _prefabWatcher;
        private FileSystemWatcher _sceneWatcher;
        private FileSystemWatcher _materialWatcher;
        private FileSystemWatcher _textureWatcher;
        private FileSystemWatcher _audioWatcher;
#endif

        private float _lastUpdateCheck;
        private float _lastMemoryCheck;
        private int _assetsReloadedThisFrame;

        // Statistics
        private int _totalReloads;
        private int _totalDependencyUpdates;
        private long _memorySavedBytes;

        #endregion

        #region Properties

        public static AssetManagementSystem Instance => _instance;
        public bool IsEnabled => enableHotReload;
        public int TrackedAssetCount => _trackedAssets.Count;
        public int QueuedReloads => _reloadQueue.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!enableHotReload) return;

            // Process reload queue
            ProcessReloadQueue();

            // Periodic update checks
            if (Time.unscaledTime - _lastUpdateCheck >= updateCheckInterval)
            {
                _lastUpdateCheck = Time.unscaledTime;
                CheckForAssetUpdates();
            }

            // Memory management
            if (autoUnloadUnusedAssets && Time.unscaledTime - _lastMemoryCheck >= unusedAssetCheckInterval)
            {
                _lastMemoryCheck = Time.unscaledTime;
                UnloadUnusedAssetsIfNeeded();
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            StopWatching();
#endif
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[AssetManagementSystem] Initializing...");

#if UNITY_EDITOR
            if (enableHotReload)
            {
                StartWatching();
            }
#endif

            // Scan for existing assets
            ScanProjectAssets();

            Debug.Log($"[AssetManagementSystem] Initialized. Tracking {_trackedAssets.Count} assets.");
        }

        private void ScanProjectAssets()
        {
#if UNITY_EDITOR
            string[] assetGuids = AssetDatabase.FindAssets("t:Prefab t:SceneAsset t:Material");

            foreach (string guid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (ShouldTrackAsset(path))
                {
                    RegisterAsset(path);
                }
            }
#endif
        }

        #endregion

        #region File Watching

#if UNITY_EDITOR
        private void StartWatching()
        {
            string projectPath = Application.dataPath;

            if (watchPrefabs)
            {
                _prefabWatcher = CreateWatcher(projectPath, "*.prefab");
            }

            if (watchScenes)
            {
                _sceneWatcher = CreateWatcher(projectPath, "*.unity");
            }

            if (watchMaterials)
            {
                _materialWatcher = CreateWatcher(projectPath, "*.mat");
            }

            if (watchTextures)
            {
                _textureWatcher = CreateWatcher(projectPath, "*.png");
                // Could add more texture extensions: *.jpg, *.tga, etc.
            }

            if (watchAudioClips)
            {
                _audioWatcher = CreateWatcher(projectPath, "*.wav");
                // Could add more audio extensions: *.mp3, *.ogg, etc.
            }

            Debug.Log("[AssetManagementSystem] File watching started");
        }

        private FileSystemWatcher CreateWatcher(string path, string filter)
        {
            var watcher = new FileSystemWatcher
            {
                Path = path,
                Filter = filter,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            watcher.Changed += OnAssetChanged;
            watcher.Created += OnAssetCreated;
            watcher.Deleted += OnAssetDeleted;
            watcher.Renamed += OnAssetRenamed;

            watcher.EnableRaisingEvents = true;

            return watcher;
        }

        private void StopWatching()
        {
            _prefabWatcher?.Dispose();
            _sceneWatcher?.Dispose();
            _materialWatcher?.Dispose();
            _textureWatcher?.Dispose();
            _audioWatcher?.Dispose();

            Debug.Log("[AssetManagementSystem] File watching stopped");
        }

        private void OnAssetChanged(object sender, FileSystemEventArgs e)
        {
            if (!ShouldTrackAsset(e.FullPath)) return;
            QueueReload(GetRelativeAssetPath(e.FullPath));
        }

        private void OnAssetCreated(object sender, FileSystemEventArgs e)
        {
            if (!ShouldTrackAsset(e.FullPath)) return;
            string path = GetRelativeAssetPath(e.FullPath);
            RegisterAsset(path);
        }

        private void OnAssetDeleted(object sender, FileSystemEventArgs e)
        {
            if (!ShouldTrackAsset(e.FullPath)) return;
            string path = GetRelativeAssetPath(e.FullPath);
            UnregisterAsset(path);
        }

        private void OnAssetRenamed(object sender, RenamedEventArgs e)
        {
            string oldPath = GetRelativeAssetPath(e.OldFullPath);
            string newPath = GetRelativeAssetPath(e.FullPath);

            if (_trackedAssets.ContainsKey(oldPath))
            {
                UnregisterAsset(oldPath);
                RegisterAsset(newPath);
            }
        }
#endif

        #endregion

        #region Asset Registration

        public void RegisterAsset(string assetPath)
        {
            if (_trackedAssets.ContainsKey(assetPath)) return;

#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null) return;

            var info = new AssetInfo
            {
                path = assetPath,
                assetType = asset.GetType(),
                lastModified = File.GetLastWriteTimeUtc(assetPath),
                dependentCount = 0
            };

            _trackedAssets[assetPath] = info;
            _loadedAssetPaths.Add(assetPath);

            if (logReloadEvents)
            {
                Debug.Log($"[AssetManagementSystem] Registered: {assetPath}");
            }
#endif
        }

        public void UnregisterAsset(string assetPath)
        {
            if (!_trackedAssets.ContainsKey(assetPath)) return;

            _trackedAssets.Remove(assetPath);
            _loadedAssetPaths.Remove(assetPath);
            _assetDependents.Remove(assetPath);

            if (logReloadEvents)
            {
                Debug.Log($"[AssetManagementSystem] Unregistered: {assetPath}");
            }
        }

        #endregion

        #region Dependency Tracking

        /// <summary>
        /// Register a dependent object that should be updated when the asset changes.
        /// </summary>
        public void RegisterDependent(string assetPath, UnityEngine.Object dependent)
        {
            if (string.IsNullOrEmpty(assetPath) || dependent == null) return;

            if (!_assetDependents.ContainsKey(assetPath))
            {
                _assetDependents[assetPath] = new HashSet<UnityEngine.Object>();
            }

            _assetDependents[assetPath].Add(dependent);

            if (_trackedAssets.TryGetValue(assetPath, out var info))
            {
                info.dependentCount = _assetDependents[assetPath].Count;
            }
        }

        /// <summary>
        /// Unregister a dependent object.
        /// </summary>
        public void UnregisterDependent(string assetPath, UnityEngine.Object dependent)
        {
            if (!_assetDependents.ContainsKey(assetPath)) return;

            _assetDependents[assetPath].Remove(dependent);

            if (_trackedAssets.TryGetValue(assetPath, out var info))
            {
                info.dependentCount = _assetDependents[assetPath].Count;
            }

            // Clean up empty sets
            if (_assetDependents[assetPath].Count == 0)
            {
                _assetDependents.Remove(assetPath);
            }
        }

        /// <summary>
        /// Get all dependents of an asset.
        /// </summary>
        public List<UnityEngine.Object> GetDependents(string assetPath)
        {
            if (!_assetDependents.ContainsKey(assetPath))
                return new List<UnityEngine.Object>();

            // Filter out null references (destroyed objects)
            var validDependents = _assetDependents[assetPath].Where(d => d != null).ToList();
            return validDependents;
        }

        #endregion

        #region Hot Reload

        private void QueueReload(string assetPath)
        {
            if (!_reloadQueue.Contains(assetPath))
            {
                _reloadQueue.Enqueue(assetPath);

                if (logReloadEvents)
                {
                    Debug.Log($"[AssetManagementSystem] Queued reload: {assetPath}");
                }
            }
        }

        private void ProcessReloadQueue()
        {
            _assetsReloadedThisFrame = 0;

            while (_reloadQueue.Count > 0 && _assetsReloadedThisFrame < maxAssetsPerFrame)
            {
                string assetPath = _reloadQueue.Dequeue();
                ReloadAsset(assetPath);
                _assetsReloadedThisFrame++;
            }
        }

        private void ReloadAsset(string assetPath)
        {
#if UNITY_EDITOR
            if (!_trackedAssets.ContainsKey(assetPath))
            {
                RegisterAsset(assetPath);
                return;
            }

            var info = _trackedAssets[assetPath];

            // Check if file actually changed
            DateTime currentModified = File.GetLastWriteTimeUtc(assetPath);
            if (currentModified <= info.lastModified)
            {
                return; // No change
            }

            // Reimport asset
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            // Update tracked info
            info.lastModified = currentModified;
            info.reloadCount++;

            // Update dependents
            UpdateDependents(assetPath);

            _totalReloads++;

            if (logReloadEvents)
            {
                Debug.Log($"[AssetManagementSystem] Reloaded: {assetPath} (#{info.reloadCount})");
            }

            // Special handling for scenes
            if (assetPath.EndsWith(".unity"))
            {
                ReloadSceneIfActive(assetPath);
            }
#endif
        }

        private void UpdateDependents(string assetPath)
        {
#if UNITY_EDITOR
            var dependents = GetDependents(assetPath);
            if (dependents.Count == 0) return;

            var newAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (newAsset == null) return;

            foreach (var dependent in dependents)
            {
                if (dependent == null) continue;

                // Update references based on type
                if (dependent is GameObject go)
                {
                    UpdateGameObjectReferences(go, assetPath, newAsset);
                }
                else if (dependent is Component component)
                {
                    UpdateComponentReferences(component, assetPath, newAsset);
                }

                EditorUtility.SetDirty(dependent);
            }

            _totalDependencyUpdates += dependents.Count;

            if (logReloadEvents)
            {
                Debug.Log($"[AssetManagementSystem] Updated {dependents.Count} dependents of {assetPath}");
            }
#endif
        }

#if UNITY_EDITOR
        private void UpdateGameObjectReferences(GameObject go, string assetPath, UnityEngine.Object newAsset)
        {
            // Update prefab references
            if (newAsset is GameObject prefab)
            {
                // Check if this GameObject is an instance of the prefab
                var prefabInstance = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (prefabInstance != null && AssetDatabase.GetAssetPath(prefabInstance) == assetPath)
                {
                    // Prefab instance will be updated automatically by Unity
                    return;
                }
            }

            // Update material references in renderers
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                UpdateMaterialReferences(renderer, assetPath, newAsset);
            }
        }

        private void UpdateComponentReferences(Component component, string assetPath, UnityEngine.Object newAsset)
        {
            if (component is Renderer renderer)
            {
                UpdateMaterialReferences(renderer, assetPath, newAsset);
            }
            else if (component is AudioSource audioSource && newAsset is AudioClip audioClip)
            {
                audioSource.clip = audioClip;
            }
        }

        private void UpdateMaterialReferences(Renderer renderer, string assetPath, UnityEngine.Object newAsset)
        {
            if (!(newAsset is Material newMaterial)) return;

            var materials = renderer.sharedMaterials;
            bool updated = false;

            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null && AssetDatabase.GetAssetPath(materials[i]) == assetPath)
                {
                    materials[i] = newMaterial;
                    updated = true;
                }
            }

            if (updated)
            {
                renderer.sharedMaterials = materials;
            }
        }

        private void ReloadSceneIfActive(string scenePath)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == scenePath)
            {
                Debug.Log($"[AssetManagementSystem] Active scene modified: {scenePath}. Consider reloading.");
                // Auto-reload could be dangerous, so just log for now
                // EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }
#endif

        #endregion

        #region Asset Update Checking

        private void CheckForAssetUpdates()
        {
            // Check if any tracked assets have been modified externally
            var assetsToReload = new List<string>();

            foreach (var kvp in _trackedAssets)
            {
                string path = kvp.Key;
                var info = kvp.Value;

                if (!File.Exists(path)) continue;

                DateTime currentModified = File.GetLastWriteTimeUtc(path);
                if (currentModified > info.lastModified)
                {
                    assetsToReload.Add(path);
                }
            }

            foreach (string path in assetsToReload)
            {
                QueueReload(path);
            }
        }

        #endregion

        #region Memory Management

        private void UnloadUnusedAssetsIfNeeded()
        {
            long currentMemory = GC.GetTotalMemory(false);
            long thresholdBytes = memoryThresholdMB * 1024L * 1024L;

            if (currentMemory > thresholdBytes)
            {
                long beforeMemory = currentMemory;

                // Unload unused Unity assets
                Resources.UnloadUnusedAssets();

                // Force garbage collection
                GC.Collect();

                long afterMemory = GC.GetTotalMemory(false);
                long freedMemory = beforeMemory - afterMemory;
                _memorySavedBytes += freedMemory;

                Debug.Log($"[AssetManagementSystem] Unloaded unused assets. Freed: {freedMemory / 1024f / 1024f:F2} MB");
            }
        }

        /// <summary>
        /// Manually unload unused assets.
        /// </summary>
        public void UnloadUnusedAssets()
        {
            long beforeMemory = GC.GetTotalMemory(false);
            Resources.UnloadUnusedAssets();
            GC.Collect();
            long afterMemory = GC.GetTotalMemory(false);
            long freedMemory = beforeMemory - afterMemory;
            _memorySavedBytes += freedMemory;

            Debug.Log($"[AssetManagementSystem] Manually unloaded unused assets. Freed: {freedMemory / 1024f / 1024f:F2} MB");
        }

        #endregion

        #region Helper Methods

        private bool ShouldTrackAsset(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (path.Contains("/.") || path.Contains("\\.")) return false; // Hidden files
            if (path.Contains("/Editor/") || path.Contains("\\Editor\\")) return false; // Editor-only assets

            string extension = Path.GetExtension(path).ToLower();

            if (watchPrefabs && extension == ".prefab") return true;
            if (watchScenes && extension == ".unity") return true;
            if (watchMaterials && extension == ".mat") return true;
            if (watchTextures && (extension == ".png" || extension == ".jpg" || extension == ".tga")) return true;
            if (watchAudioClips && (extension == ".wav" || extension == ".mp3" || extension == ".ogg")) return true;

            return false;
        }

        private string GetRelativeAssetPath(string fullPath)
        {
            string dataPath = Application.dataPath;
            if (fullPath.StartsWith(dataPath))
            {
                return "Assets" + fullPath.Substring(dataPath.Length).Replace("\\", "/");
            }
            return fullPath;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get statistics about the asset management system.
        /// </summary>
        public AssetManagementStats GetStats()
        {
            return new AssetManagementStats
            {
                trackedAssetCount = _trackedAssets.Count,
                totalReloads = _totalReloads,
                totalDependencyUpdates = _totalDependencyUpdates,
                queuedReloads = _reloadQueue.Count,
                memorySavedMB = _memorySavedBytes / 1024f / 1024f,
                currentMemoryMB = GC.GetTotalMemory(false) / 1024f / 1024f
            };
        }

        /// <summary>
        /// Get information about a specific asset.
        /// </summary>
        public AssetInfo GetAssetInfo(string assetPath)
        {
            return _trackedAssets.TryGetValue(assetPath, out var info) ? info : null;
        }

        /// <summary>
        /// Get all tracked assets of a specific type.
        /// </summary>
        public List<AssetInfo> GetAssetsByType(Type assetType)
        {
            return _trackedAssets.Values.Where(info => info.assetType == assetType).ToList();
        }

        /// <summary>
        /// Force reload an asset immediately.
        /// </summary>
        public void ForceReloadAsset(string assetPath)
        {
            ReloadAsset(assetPath);
        }

        /// <summary>
        /// Clear all tracked assets and reset the system.
        /// </summary>
        public void Reset()
        {
            _trackedAssets.Clear();
            _assetDependents.Clear();
            _reloadQueue.Clear();
            _loadedAssetPaths.Clear();
            _totalReloads = 0;
            _totalDependencyUpdates = 0;
            _memorySavedBytes = 0;

            Debug.Log("[AssetManagementSystem] Reset complete");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Asset Management Statistics ===\n" +
                      $"Tracked Assets: {stats.trackedAssetCount}\n" +
                      $"Total Reloads: {stats.totalReloads}\n" +
                      $"Dependency Updates: {stats.totalDependencyUpdates}\n" +
                      $"Queued Reloads: {stats.queuedReloads}\n" +
                      $"Memory Saved: {stats.memorySavedMB:F2} MB\n" +
                      $"Current Memory: {stats.currentMemoryMB:F2} MB");
        }

        [ContextMenu("Unload Unused Assets")]
        private void UnloadUnusedAssetsMenu()
        {
            UnloadUnusedAssets();
        }

        [ContextMenu("Rescan Project Assets")]
        private void RescanProjectAssets()
        {
            Reset();
            ScanProjectAssets();
            Debug.Log($"[AssetManagementSystem] Rescanned. Now tracking {_trackedAssets.Count} assets.");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Information about a tracked asset.
    /// </summary>
    [Serializable]
    public class AssetInfo
    {
        public string path;
        public Type assetType;
        public DateTime lastModified;
        public int dependentCount;
        public int reloadCount;
    }

    /// <summary>
    /// Statistics for the asset management system.
    /// </summary>
    [Serializable]
    public struct AssetManagementStats
    {
        public int trackedAssetCount;
        public int totalReloads;
        public int totalDependencyUpdates;
        public int queuedReloads;
        public float memorySavedMB;
        public float currentMemoryMB;
    }

    #endregion
}
