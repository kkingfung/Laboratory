using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProvisions;
using UnityEngine.SceneManagement;

namespace Laboratory.Tools
{
    /// <summary>
    /// Advanced async asset loading system with staging, prioritization, and memory management.
    /// Provides smooth loading experiences with progress tracking and cancellation support.
    /// Optimizes startup times with intelligent asset batching and preloading.
    /// </summary>
    public class AsyncAssetLoader : MonoBehaviour
    {
        #region Configuration

        [Header("Loading Settings")]
        [SerializeField] private int maxConcurrentLoads = 5;
        [SerializeField] private float frameBudgetMs = 10f;
        [SerializeField] private bool enableProgressTracking = true;
        [SerializeField] private bool logLoadOperations = true;

        [Header("Memory Management")]
        [SerializeField] private long maxMemoryUsageMB = 1000;
        [SerializeField] private bool autoUnloadUnused = true;
        [SerializeField] private float unusedAssetLifetime = 60f;

        [Header("Preloading")]
        [SerializeField] private bool enablePreloading = true;
        [SerializeField] private List<string> preloadAssetKeys = new List<string>();

        #endregion

        #region Private Fields

        private static AsyncAssetLoader _instance;

        // Load queues by priority
        private readonly Dictionary<LoadPriority, Queue<LoadRequest>> _loadQueues = new Dictionary<LoadPriority, Queue<LoadRequest>>
        {
            { LoadPriority.Critical, new Queue<LoadRequest>() },
            { LoadPriority.High, new Queue<LoadRequest>() },
            { LoadPriority.Normal, new Queue<LoadRequest>() },
            { LoadPriority.Low, new Queue<LoadRequest>() },
            { LoadPriority.Background, new Queue<LoadRequest>() }
        };

        private readonly List<LoadRequest> _activeLoads = new List<LoadRequest>();
        private readonly Dictionary<string, LoadedAsset> _loadedAssets = new Dictionary<string, LoadedAsset>();
        private readonly Dictionary<string, List<Action<UnityEngine.Object>>> _pendingCallbacks = new Dictionary<string, List<Action<UnityEngine.Object>>>();

        // Staging system
        private readonly List<LoadStage> _stages = new List<LoadStage>();
        private int _currentStageIndex = 0;
        private bool _isLoadingStages = false;

        // Statistics
        private int _totalLoadsRequested;
        private int _totalLoadsCompleted;
        private int _totalLoadsFailed;
        private int _totalCacheHits;
        private long _totalBytesLoaded;

        // Events
        public event Action<LoadRequest> OnLoadStarted;
        public event Action<LoadRequest, UnityEngine.Object> OnLoadCompleted;
        public event Action<LoadRequest, string> OnLoadFailed;
        public event Action<LoadStage> OnStageStarted;
        public event Action<LoadStage> OnStageCompleted;
        public event Action OnAllStagesCompleted;

        #endregion

        #region Properties

        public static AsyncAssetLoader Instance => _instance;
        public bool IsLoading => _activeLoads.Count > 0 || _isLoadingStages;
        public int ActiveLoadCount => _activeLoads.Count;
        public int QueuedLoadCount => _loadQueues.Values.Sum(q => q.Count);
        public int LoadedAssetCount => _loadedAssets.Count;

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
            ProcessLoadQueue();
            UpdateActiveLoads();
            CleanupUnusedAssets();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[AsyncAssetLoader] Initializing...");

            if (enablePreloading && preloadAssetKeys.Count > 0)
            {
                StartCoroutine(PreloadAssetsCoroutine());
            }

            Debug.Log("[AsyncAssetLoader] Initialized");
        }

        private IEnumerator PreloadAssetsCoroutine()
        {
            Debug.Log($"[AsyncAssetLoader] Preloading {preloadAssetKeys.Count} assets...");

            foreach (var key in preloadAssetKeys)
            {
                LoadAssetAsync<UnityEngine.Object>(key, LoadPriority.High);
                yield return null; // Spread over multiple frames
            }

            Debug.Log("[AsyncAssetLoader] Preloading complete");
        }

        #endregion

        #region Asset Loading

        /// <summary>
        /// Load an asset asynchronously with priority.
        /// </summary>
        public void LoadAssetAsync<T>(string assetKey, LoadPriority priority = LoadPriority.Normal, Action<T> onComplete = null) where T : UnityEngine.Object
        {
            _totalLoadsRequested++;

            // Check cache first
            if (_loadedAssets.TryGetValue(assetKey, out var cached))
            {
                _totalCacheHits++;
                cached.lastAccessTime = Time.time;
                cached.referenceCount++;

                onComplete?.Invoke(cached.asset as T);

                if (logLoadOperations)
                {
                    Debug.Log($"[AsyncAssetLoader] Cache hit: {assetKey}");
                }

                return;
            }

            // Check if already being loaded
            if (_pendingCallbacks.ContainsKey(assetKey))
            {
                _pendingCallbacks[assetKey].Add(obj => onComplete?.Invoke(obj as T));
                return;
            }

            // Create new load request
            var request = new LoadRequest
            {
                assetKey = assetKey,
                assetType = typeof(T),
                priority = priority,
                requestTime = Time.time,
                onComplete = obj => onComplete?.Invoke(obj as T)
            };

            // Queue for loading
            _loadQueues[priority].Enqueue(request);
            _pendingCallbacks[assetKey] = new List<Action<UnityEngine.Object>> { obj => onComplete?.Invoke(obj as T) };

            if (logLoadOperations)
            {
                Debug.Log($"[AsyncAssetLoader] Queued: {assetKey} (Priority: {priority})");
            }
        }

        /// <summary>
        /// Load multiple assets asynchronously.
        /// </summary>
        public void LoadAssetsBatchAsync<T>(List<string> assetKeys, LoadPriority priority = LoadPriority.Normal, Action<List<T>> onComplete = null) where T : UnityEngine.Object
        {
            var loadedAssets = new List<T>();
            int remaining = assetKeys.Count;

            if (remaining == 0)
            {
                onComplete?.Invoke(loadedAssets);
                return;
            }

            foreach (var key in assetKeys)
            {
                LoadAssetAsync<T>(key, priority, asset =>
                {
                    if (asset != null)
                    {
                        loadedAssets.Add(asset);
                    }

                    remaining--;
                    if (remaining == 0)
                    {
                        onComplete?.Invoke(loadedAssets);
                    }
                });
            }
        }

        /// <summary>
        /// Get a loaded asset from cache (instant, no loading).
        /// </summary>
        public T GetLoadedAsset<T>(string assetKey) where T : UnityEngine.Object
        {
            if (_loadedAssets.TryGetValue(assetKey, out var cached))
            {
                cached.lastAccessTime = Time.time;
                cached.referenceCount++;
                return cached.asset as T;
            }

            return null;
        }

        /// <summary>
        /// Check if an asset is already loaded.
        /// </summary>
        public bool IsAssetLoaded(string assetKey)
        {
            return _loadedAssets.ContainsKey(assetKey);
        }

        /// <summary>
        /// Unload a specific asset.
        /// </summary>
        public void UnloadAsset(string assetKey)
        {
            if (_loadedAssets.TryGetValue(assetKey, out var cached))
            {
                cached.referenceCount--;

                if (cached.referenceCount <= 0)
                {
                    Resources.UnloadAsset(cached.asset);
                    _loadedAssets.Remove(assetKey);

                    if (logLoadOperations)
                    {
                        Debug.Log($"[AsyncAssetLoader] Unloaded: {assetKey}");
                    }
                }
            }
        }

        #endregion

        #region Staged Loading

        /// <summary>
        /// Define loading stages for progressive asset loading.
        /// </summary>
        public void DefineLoadingStages(params LoadStage[] stages)
        {
            _stages.Clear();
            _stages.AddRange(stages);
            _currentStageIndex = 0;

            if (logLoadOperations)
            {
                Debug.Log($"[AsyncAssetLoader] Defined {stages.Length} loading stages");
            }
        }

        /// <summary>
        /// Start loading all defined stages.
        /// </summary>
        public void StartStagedLoading()
        {
            if (_stages.Count == 0)
            {
                Debug.LogWarning("[AsyncAssetLoader] No stages defined for staged loading");
                return;
            }

            _isLoadingStages = true;
            _currentStageIndex = 0;
            StartCoroutine(LoadStagesCoroutine());
        }

        private IEnumerator LoadStagesCoroutine()
        {
            for (int i = 0; i < _stages.Count; i++)
            {
                _currentStageIndex = i;
                var stage = _stages[i];

                stage.startTime = Time.time;
                stage.isComplete = false;
                OnStageStarted?.Invoke(stage);

                if (logLoadOperations)
                {
                    Debug.Log($"[AsyncAssetLoader] Starting stage {i + 1}/{_stages.Count}: {stage.name}");
                }

                // Load all assets in this stage
                int totalAssets = stage.assetKeys.Count;
                int loadedAssets = 0;

                foreach (var key in stage.assetKeys)
                {
                    LoadAssetAsync<UnityEngine.Object>(key, stage.priority, asset =>
                    {
                        loadedAssets++;
                        stage.progress = (float)loadedAssets / totalAssets;
                    });
                }

                // Wait for all assets in this stage to load
                while (loadedAssets < totalAssets)
                {
                    stage.progress = (float)loadedAssets / totalAssets;
                    yield return null;
                }

                stage.endTime = Time.time;
                stage.isComplete = true;
                OnStageCompleted?.Invoke(stage);

                if (logLoadOperations)
                {
                    Debug.Log($"[AsyncAssetLoader] Completed stage {i + 1}/{_stages.Count}: {stage.name} ({stage.endTime - stage.startTime:F2}s)");
                }

                // Optional delay between stages
                if (stage.delayAfterComplete > 0)
                {
                    yield return new WaitForSeconds(stage.delayAfterComplete);
                }
            }

            _isLoadingStages = false;
            OnAllStagesCompleted?.Invoke();

            if (logLoadOperations)
            {
                Debug.Log("[AsyncAssetLoader] All stages completed");
            }
        }

        /// <summary>
        /// Get the current stage progress (0-1).
        /// </summary>
        public float GetCurrentStageProgress()
        {
            if (!_isLoadingStages || _currentStageIndex >= _stages.Count)
                return 1f;

            return _stages[_currentStageIndex].progress;
        }

        /// <summary>
        /// Get the overall progress across all stages (0-1).
        /// </summary>
        public float GetOverallProgress()
        {
            if (_stages.Count == 0) return 1f;

            float totalProgress = 0f;
            for (int i = 0; i < _stages.Count; i++)
            {
                if (i < _currentStageIndex)
                {
                    totalProgress += 1f;
                }
                else if (i == _currentStageIndex)
                {
                    totalProgress += _stages[i].progress;
                }
            }

            return totalProgress / _stages.Count;
        }

        #endregion

        #region Load Queue Processing

        private void ProcessLoadQueue()
        {
            // Process queues by priority
            foreach (var priority in Enum.GetValues(typeof(LoadPriority)).Cast<LoadPriority>())
            {
                var queue = _loadQueues[priority];

                while (_activeLoads.Count < maxConcurrentLoads && queue.Count > 0)
                {
                    var request = queue.Dequeue();
                    StartLoadRequest(request);
                }

                // Only process lower priorities if higher priorities are empty
                if (queue.Count > 0)
                    break;
            }
        }

        private void StartLoadRequest(LoadRequest request)
        {
            request.isActive = true;
            _activeLoads.Add(request);

            OnLoadStarted?.Invoke(request);

            // Start loading via Resources (in production, use Addressables)
            StartCoroutine(LoadAssetCoroutine(request));
        }

        private IEnumerator LoadAssetCoroutine(LoadRequest request)
        {
            ResourceRequest resourceRequest = Resources.LoadAsync(request.assetKey, request.assetType);

            while (!resourceRequest.isDone)
            {
                request.progress = resourceRequest.progress;
                yield return null;
            }

            var asset = resourceRequest.asset;

            if (asset != null)
            {
                // Cache the asset
                var loadedAsset = new LoadedAsset
                {
                    asset = asset,
                    loadTime = Time.time,
                    lastAccessTime = Time.time,
                    size = EstimateAssetSize(asset),
                    referenceCount = 1
                };

                _loadedAssets[request.assetKey] = loadedAsset;
                _totalBytesLoaded += loadedAsset.size;
                _totalLoadsCompleted++;

                // Invoke callbacks
                if (_pendingCallbacks.TryGetValue(request.assetKey, out var callbacks))
                {
                    foreach (var callback in callbacks)
                    {
                        callback?.Invoke(asset);
                    }
                    _pendingCallbacks.Remove(request.assetKey);
                }

                OnLoadCompleted?.Invoke(request, asset);

                if (logLoadOperations)
                {
                    Debug.Log($"[AsyncAssetLoader] Loaded: {request.assetKey} ({Time.time - request.requestTime:F2}s)");
                }
            }
            else
            {
                _totalLoadsFailed++;
                OnLoadFailed?.Invoke(request, "Asset not found or failed to load");

                Debug.LogError($"[AsyncAssetLoader] Failed to load: {request.assetKey}");
            }

            _activeLoads.Remove(request);
        }

        private void UpdateActiveLoads()
        {
            // Update progress for active loads (already handled in coroutine)
        }

        #endregion

        #region Memory Management

        private void CleanupUnusedAssets()
        {
            if (!autoUnloadUnused) return;

            var currentTime = Time.time;
            var assetsToUnload = new List<string>();

            foreach (var kvp in _loadedAssets)
            {
                var asset = kvp.Value;
                if (asset.referenceCount <= 0 && (currentTime - asset.lastAccessTime) > unusedAssetLifetime)
                {
                    assetsToUnload.Add(kvp.Key);
                }
            }

            foreach (var key in assetsToUnload)
            {
                UnloadAsset(key);
            }

            // Check memory threshold
            long currentMemoryMB = GC.GetTotalMemory(false) / 1024 / 1024;
            if (currentMemoryMB > maxMemoryUsageMB)
            {
                Resources.UnloadUnusedAssets();
                Debug.LogWarning($"[AsyncAssetLoader] Memory threshold exceeded ({currentMemoryMB}MB). Unloading unused assets.");
            }
        }

        private long EstimateAssetSize(UnityEngine.Object asset)
        {
            // Rough estimation (in production, use more accurate methods)
            if (asset is Texture2D texture)
            {
                return texture.width * texture.height * 4; // Assume RGBA32
            }
            else if (asset is Mesh mesh)
            {
                return mesh.vertexCount * 32; // Rough estimate
            }
            else if (asset is AudioClip audio)
            {
                return audio.samples * audio.channels * 2; // Assume 16-bit
            }
            else
            {
                return 1024; // Default 1KB
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current loading statistics.
        /// </summary>
        public LoaderStats GetStats()
        {
            return new LoaderStats
            {
                totalLoadsRequested = _totalLoadsRequested,
                totalLoadsCompleted = _totalLoadsCompleted,
                totalLoadsFailed = _totalLoadsFailed,
                totalCacheHits = _totalCacheHits,
                activeLoadCount = _activeLoads.Count,
                queuedLoadCount = QueuedLoadCount,
                loadedAssetCount = _loadedAssets.Count,
                totalBytesLoaded = _totalBytesLoaded,
                cacheHitRate = _totalLoadsRequested > 0 ? (float)_totalCacheHits / _totalLoadsRequested : 0f
            };
        }

        /// <summary>
        /// Cancel all pending loads.
        /// </summary>
        public void CancelAllLoads()
        {
            foreach (var queue in _loadQueues.Values)
            {
                queue.Clear();
            }

            _pendingCallbacks.Clear();

            Debug.Log("[AsyncAssetLoader] Cancelled all pending loads");
        }

        /// <summary>
        /// Clear all cached assets.
        /// </summary>
        public void ClearCache()
        {
            foreach (var kvp in _loadedAssets)
            {
                Resources.UnloadAsset(kvp.Value.asset);
            }

            _loadedAssets.Clear();
            Resources.UnloadUnusedAssets();

            Debug.Log("[AsyncAssetLoader] Cache cleared");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Async Asset Loader Statistics ===\n" +
                      $"Total Requests: {stats.totalLoadsRequested}\n" +
                      $"Completed: {stats.totalLoadsCompleted}\n" +
                      $"Failed: {stats.totalLoadsFailed}\n" +
                      $"Cache Hits: {stats.totalCacheHits}\n" +
                      $"Cache Hit Rate: {stats.cacheHitRate:P1}\n" +
                      $"Active Loads: {stats.activeLoadCount}\n" +
                      $"Queued Loads: {stats.queuedLoadCount}\n" +
                      $"Loaded Assets: {stats.loadedAssetCount}\n" +
                      $"Total Bytes: {stats.totalBytesLoaded / 1024f / 1024f:F2} MB");
        }

        [ContextMenu("Clear Cache")]
        private void ClearCacheMenu()
        {
            ClearCache();
        }

        [ContextMenu("Cancel All Loads")]
        private void CancelAllLoadsMenu()
        {
            CancelAllLoads();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// A request to load an asset.
    /// </summary>
    [Serializable]
    public class LoadRequest
    {
        public string assetKey;
        public Type assetType;
        public LoadPriority priority;
        public float requestTime;
        public float progress;
        public bool isActive;
        public Action<UnityEngine.Object> onComplete;
    }

    /// <summary>
    /// A loaded asset with metadata.
    /// </summary>
    [Serializable]
    public class LoadedAsset
    {
        public UnityEngine.Object asset;
        public float loadTime;
        public float lastAccessTime;
        public long size;
        public int referenceCount;
    }

    /// <summary>
    /// A stage in the loading process.
    /// </summary>
    [Serializable]
    public class LoadStage
    {
        public string name;
        public List<string> assetKeys;
        public LoadPriority priority = LoadPriority.Normal;
        public float progress;
        public float startTime;
        public float endTime;
        public bool isComplete;
        public float delayAfterComplete;

        public LoadStage(string name, List<string> assetKeys, LoadPriority priority = LoadPriority.Normal)
        {
            this.name = name;
            this.assetKeys = assetKeys;
            this.priority = priority;
        }
    }

    /// <summary>
    /// Statistics for the async asset loader.
    /// </summary>
    [Serializable]
    public struct LoaderStats
    {
        public int totalLoadsRequested;
        public int totalLoadsCompleted;
        public int totalLoadsFailed;
        public int totalCacheHits;
        public int activeLoadCount;
        public int queuedLoadCount;
        public int loadedAssetCount;
        public long totalBytesLoaded;
        public float cacheHitRate;
    }

    /// <summary>
    /// Priority levels for asset loading.
    /// </summary>
    public enum LoadPriority
    {
        Critical,   // Game-critical assets (must load immediately)
        High,       // Important assets (next frame content)
        Normal,     // Standard assets
        Low,        // Optional assets (effects, etc.)
        Background  // Preload for future use
    }

    #endregion
}
