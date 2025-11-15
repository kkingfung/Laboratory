using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Content
{
    /// <summary>
    /// Asset bundle management system for dynamic content loading.
    /// Handles remote bundle fetching, caching, dependency resolution, and memory management.
    /// Enables content updates without rebuilding the app.
    /// </summary>
    public class AssetBundleManager : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string bundleBaseUrl = "https://cdn.projectchimera.com/bundles";
        [SerializeField] private string manifestUrl = "https://cdn.projectchimera.com/bundles/manifest.json";

        [Header("Cache Settings")]
        [SerializeField] private bool enableCaching = true;
        [SerializeField] private long maxCacheSize = 1024L * 1024L * 1024L; // 1GB
        [SerializeField] private int cacheVersioning = 1;

        [Header("Loading Settings")]
        [SerializeField] private int maxConcurrentDownloads = 3;
        [SerializeField] private bool preloadDependencies = true;
        [SerializeField] private float downloadTimeout = 300f; // 5 minutes

        [Header("Memory Management")]
        [SerializeField] private bool autoUnloadUnused = true;
        [SerializeField] private float unloadCheckInterval = 60f; // 1 minute
        [SerializeField] private float bundleUnloadDelay = 120f; // 2 minutes

        #endregion

        #region Private Fields

        private static AssetBundleManager _instance;

        // Bundle tracking
        private readonly Dictionary<string, LoadedBundle> _loadedBundles = new Dictionary<string, LoadedBundle>();
        private readonly Dictionary<string, BundleManifestEntry> _bundleManifest = new Dictionary<string, BundleManifestEntry>();
        private readonly Queue<BundleDownload> _downloadQueue = new Queue<BundleDownload>();
        private readonly List<BundleDownload> _activeDownloads = new List<BundleDownload>();

        // Reference counting
        private readonly Dictionary<string, int> _bundleReferences = new Dictionary<string, int>();
        private readonly Dictionary<string, float> _bundleLastUsed = new Dictionary<string, float>();

        // State
        private bool _manifestLoaded = false;
        private float _lastUnloadCheck = 0f;

        // Statistics
        private int _totalBundlesLoaded = 0;
        private int _totalBundlesUnloaded = 0;
        private long _totalBytesDownloaded = 0;
        private int _cacheHits = 0;
        private int _cacheMisses = 0;

        // Events
        public event Action OnManifestLoaded;
        public event Action<string> OnManifestLoadFailed;
        public event Action<string, float> OnBundleDownloadProgress;
        public event Action<string> OnBundleLoaded;
        public event Action<string> OnBundleUnloaded;
        public event Action<string, string> OnBundleLoadFailed;

        #endregion

        #region Properties

        public static AssetBundleManager Instance => _instance;
        public bool IsManifestLoaded => _manifestLoaded;
        public int LoadedBundleCount => _loadedBundles.Count;
        public int QueuedDownloadCount => _downloadQueue.Count;
        public int ActiveDownloadCount => _activeDownloads.Count;

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
            // Process download queue
            ProcessDownloadQueue();

            // Auto-unload unused bundles
            if (autoUnloadUnused && Time.time - _lastUnloadCheck >= unloadCheckInterval)
            {
                UnloadUnusedBundles();
                _lastUnloadCheck = Time.time;
            }
        }

        private void OnApplicationQuit()
        {
            UnloadAllBundles(true);
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[AssetBundleManager] Initializing...");

            // Setup cache
            if (enableCaching)
            {
                Caching.compressionEnabled = true;
            }

            Debug.Log("[AssetBundleManager] Initialized");
        }

        #endregion

        #region Manifest

        /// <summary>
        /// Load bundle manifest from remote.
        /// </summary>
        public void LoadManifest(Action onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(LoadManifestCoroutine(onSuccess, onError));
        }

        private IEnumerator LoadManifestCoroutine(Action onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(manifestUrl))
            {
                request.timeout = 30;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var manifest = JsonUtility.FromJson<BundleManifest>(request.downloadHandler.text);

                        _bundleManifest.Clear();

                        foreach (var entry in manifest.bundles)
                        {
                            _bundleManifest[entry.bundleName] = entry;
                        }

                        _manifestLoaded = true;

                        OnManifestLoaded?.Invoke();
                        onSuccess?.Invoke();

                        Debug.Log($"[AssetBundleManager] Manifest loaded: {_bundleManifest.Count} bundles");
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse manifest: {ex.Message}";
                        OnManifestLoadFailed?.Invoke(error);
                        onError?.Invoke(error);
                        Debug.LogError($"[AssetBundleManager] {error}");
                    }
                }
                else
                {
                    string error = $"Failed to load manifest: {request.error}";
                    OnManifestLoadFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[AssetBundleManager] {error}");
                }
            }
        }

        #endregion

        #region Bundle Loading

        /// <summary>
        /// Load an asset bundle.
        /// </summary>
        public void LoadBundle(string bundleName, Action<AssetBundle> onSuccess = null, Action<string> onError = null)
        {
            if (!_manifestLoaded)
            {
                string error = "Manifest not loaded";
                onError?.Invoke(error);
                Debug.LogError($"[AssetBundleManager] {error}");
                return;
            }

            // Check if already loaded
            if (_loadedBundles.TryGetValue(bundleName, out var loadedBundle))
            {
                IncrementReference(bundleName);
                onSuccess?.Invoke(loadedBundle.bundle);
                return;
            }

            // Check if in manifest
            if (!_bundleManifest.TryGetValue(bundleName, out var manifestEntry))
            {
                string error = $"Bundle not found in manifest: {bundleName}";
                OnBundleLoadFailed?.Invoke(bundleName, error);
                onError?.Invoke(error);
                Debug.LogError($"[AssetBundleManager] {error}");
                return;
            }

            // Queue download
            var download = new BundleDownload
            {
                bundleName = bundleName,
                manifestEntry = manifestEntry,
                onSuccess = onSuccess,
                onError = onError
            };

            _downloadQueue.Enqueue(download);
        }

        /// <summary>
        /// Load multiple bundles.
        /// </summary>
        public void LoadBundles(string[] bundleNames, Action<Dictionary<string, AssetBundle>> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(LoadBundlesCoroutine(bundleNames, onSuccess, onError));
        }

        private IEnumerator LoadBundlesCoroutine(string[] bundleNames, Action<Dictionary<string, AssetBundle>> onSuccess, Action<string> onError)
        {
            var loadedBundles = new Dictionary<string, AssetBundle>();
            int remaining = bundleNames.Length;
            bool failed = false;
            string errorMessage = null;

            foreach (var bundleName in bundleNames)
            {
                LoadBundle(bundleName,
                    bundle =>
                    {
                        loadedBundles[bundleName] = bundle;
                        remaining--;
                    },
                    error =>
                    {
                        failed = true;
                        errorMessage = error;
                        remaining--;
                    });
            }

            // Wait for all to complete
            while (remaining > 0)
            {
                yield return null;
            }

            if (failed)
            {
                onError?.Invoke(errorMessage);
            }
            else
            {
                onSuccess?.Invoke(loadedBundles);
            }
        }

        #endregion

        #region Download Processing

        private void ProcessDownloadQueue()
        {
            // Start downloads up to max concurrent
            while (_downloadQueue.Count > 0 && _activeDownloads.Count < maxConcurrentDownloads)
            {
                var download = _downloadQueue.Dequeue();
                _activeDownloads.Add(download);
                StartCoroutine(DownloadBundleCoroutine(download));
            }
        }

        private IEnumerator DownloadBundleCoroutine(BundleDownload download)
        {
            string bundleName = download.bundleName;
            var manifestEntry = download.manifestEntry;

            // Load dependencies first
            if (preloadDependencies && manifestEntry.dependencies != null)
            {
                foreach (var dependency in manifestEntry.dependencies)
                {
                    if (!_loadedBundles.ContainsKey(dependency))
                    {
                        yield return LoadBundleSync(dependency);
                    }
                }
            }

            string url = $"{bundleBaseUrl}/{bundleName}";
            string hash = manifestEntry.hash;

            UnityWebRequest request;

            if (enableCaching && !string.IsNullOrEmpty(hash))
            {
                // Use cached version
                var cachedVersions = new List<Hash128>();
                Caching.GetCachedVersions(bundleName, cachedVersions);

                if (cachedVersions.Count > 0)
                {
                    _cacheHits++;
                    request = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(hash), 0);
                }
                else
                {
                    _cacheMisses++;
                    request = UnityWebRequestAssetBundle.GetAssetBundle(url, Hash128.Parse(hash), 0);
                }
            }
            else
            {
                request = UnityWebRequestAssetBundle.GetAssetBundle(url);
            }

            request.timeout = (int)downloadTimeout;

            var operation = request.SendWebRequest();

            // Track progress
            while (!operation.isDone)
            {
                OnBundleDownloadProgress?.Invoke(bundleName, operation.progress);
                yield return null;
            }

            _activeDownloads.Remove(download);

            if (request.result == UnityWebRequest.Result.Success)
            {
                var bundle = DownloadHandlerAssetBundle.GetContent(request);

                if (bundle != null)
                {
                    _loadedBundles[bundleName] = new LoadedBundle
                    {
                        bundle = bundle,
                        loadTime = Time.time,
                        size = manifestEntry.size
                    };

                    IncrementReference(bundleName);
                    _totalBundlesLoaded++;
                    _totalBytesDownloaded += manifestEntry.size;

                    OnBundleLoaded?.Invoke(bundleName);
                    download.onSuccess?.Invoke(bundle);

                    Debug.Log($"[AssetBundleManager] Bundle loaded: {bundleName} ({manifestEntry.size / 1024}KB)");
                }
                else
                {
                    string error = "Failed to extract bundle";
                    OnBundleLoadFailed?.Invoke(bundleName, error);
                    download.onError?.Invoke(error);
                }
            }
            else
            {
                string error = $"Download failed: {request.error}";
                OnBundleLoadFailed?.Invoke(bundleName, error);
                download.onError?.Invoke(error);
                Debug.LogError($"[AssetBundleManager] {bundleName}: {error}");
            }

            request.Dispose();
        }

        private IEnumerator LoadBundleSync(string bundleName)
        {
            bool done = false;
            AssetBundle result = null;

            LoadBundle(bundleName,
                bundle => { result = bundle; done = true; },
                error => { done = true; });

            while (!done)
            {
                yield return null;
            }

            yield return result;
        }

        #endregion

        #region Bundle Unloading

        /// <summary>
        /// Unload a bundle.
        /// </summary>
        public void UnloadBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (!_loadedBundles.TryGetValue(bundleName, out var loadedBundle))
            {
                Debug.LogWarning($"[AssetBundleManager] Bundle not loaded: {bundleName}");
                return;
            }

            DecrementReference(bundleName);

            // Only unload if no references
            if (GetReferenceCount(bundleName) <= 0)
            {
                loadedBundle.bundle.Unload(unloadAllLoadedObjects);
                _loadedBundles.Remove(bundleName);
                _bundleReferences.Remove(bundleName);
                _bundleLastUsed.Remove(bundleName);

                _totalBundlesUnloaded++;

                OnBundleUnloaded?.Invoke(bundleName);

                Debug.Log($"[AssetBundleManager] Bundle unloaded: {bundleName}");
            }
        }

        /// <summary>
        /// Unload all bundles.
        /// </summary>
        public void UnloadAllBundles(bool unloadAllLoadedObjects = false)
        {
            foreach (var bundleName in _loadedBundles.Keys.ToArray())
            {
                _bundleReferences[bundleName] = 1; // Force unload
                UnloadBundle(bundleName, unloadAllLoadedObjects);
            }

            Debug.Log("[AssetBundleManager] All bundles unloaded");
        }

        /// <summary>
        /// Unload unused bundles (no references, not used recently).
        /// </summary>
        private void UnloadUnusedBundles()
        {
            var bundlesToUnload = new List<string>();

            foreach (var kvp in _loadedBundles)
            {
                string bundleName = kvp.Key;
                int refCount = GetReferenceCount(bundleName);

                if (refCount <= 0)
                {
                    float lastUsed = _bundleLastUsed.ContainsKey(bundleName) ? _bundleLastUsed[bundleName] : 0f;
                    float unusedTime = Time.time - lastUsed;

                    if (unusedTime >= bundleUnloadDelay)
                    {
                        bundlesToUnload.Add(bundleName);
                    }
                }
            }

            foreach (var bundleName in bundlesToUnload)
            {
                UnloadBundle(bundleName, false);
            }

            if (bundlesToUnload.Count > 0)
            {
                Debug.Log($"[AssetBundleManager] Auto-unloaded {bundlesToUnload.Count} unused bundles");
            }
        }

        #endregion

        #region Reference Counting

        private void IncrementReference(string bundleName)
        {
            if (!_bundleReferences.ContainsKey(bundleName))
            {
                _bundleReferences[bundleName] = 0;
            }

            _bundleReferences[bundleName]++;
            _bundleLastUsed[bundleName] = Time.time;
        }

        private void DecrementReference(string bundleName)
        {
            if (_bundleReferences.ContainsKey(bundleName))
            {
                _bundleReferences[bundleName]--;
            }
        }

        private int GetReferenceCount(string bundleName)
        {
            return _bundleReferences.ContainsKey(bundleName) ? _bundleReferences[bundleName] : 0;
        }

        #endregion

        #region Asset Loading

        /// <summary>
        /// Load an asset from a bundle.
        /// </summary>
        public void LoadAsset<T>(string bundleName, string assetName, Action<T> onSuccess = null, Action<string> onError = null) where T : UnityEngine.Object
        {
            LoadBundle(bundleName,
                bundle =>
                {
                    var asset = bundle.LoadAsset<T>(assetName);

                    if (asset != null)
                    {
                        onSuccess?.Invoke(asset);
                    }
                    else
                    {
                        string error = $"Asset not found: {assetName}";
                        onError?.Invoke(error);
                    }
                },
                onError);
        }

        /// <summary>
        /// Load an asset asynchronously.
        /// </summary>
        public void LoadAssetAsync<T>(string bundleName, string assetName, Action<T> onSuccess = null, Action<string> onError = null) where T : UnityEngine.Object
        {
            LoadBundle(bundleName,
                bundle =>
                {
                    StartCoroutine(LoadAssetAsyncCoroutine(bundle, assetName, onSuccess, onError));
                },
                onError);
        }

        private IEnumerator LoadAssetAsyncCoroutine<T>(AssetBundle bundle, string assetName, Action<T> onSuccess, Action<string> onError) where T : UnityEngine.Object
        {
            var request = bundle.LoadAssetAsync<T>(assetName);

            yield return request;

            if (request.asset != null)
            {
                onSuccess?.Invoke(request.asset as T);
            }
            else
            {
                string error = $"Asset not found: {assetName}";
                onError?.Invoke(error);
            }
        }

        #endregion

        #region Cache Management

        /// <summary>
        /// Clear asset bundle cache.
        /// </summary>
        public void ClearCache()
        {
            Caching.ClearCache();
            Debug.Log("[AssetBundleManager] Cache cleared");
        }

        /// <summary>
        /// Get cache size in bytes.
        /// </summary>
        public long GetCacheSize()
        {
            long totalSize = 0;

            var caches = new List<string>();
            Caching.GetAllCachePaths(caches);
            foreach (var cache in caches)
            {
                if (Directory.Exists(cache))
                {
                    var files = Directory.GetFiles(cache, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        totalSize += new FileInfo(file).Length;
                    }
                }
            }

            return totalSize;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get asset bundle statistics.
        /// </summary>
        public AssetBundleStats GetStats()
        {
            return new AssetBundleStats
            {
                loadedBundleCount = _loadedBundles.Count,
                totalBundlesLoaded = _totalBundlesLoaded,
                totalBundlesUnloaded = _totalBundlesUnloaded,
                totalBytesDownloaded = _totalBytesDownloaded,
                cacheHits = _cacheHits,
                cacheMisses = _cacheMisses,
                queuedDownloads = _downloadQueue.Count,
                activeDownloads = _activeDownloads.Count,
                cacheSize = GetCacheSize()
            };
        }

        /// <summary>
        /// Check if bundle is loaded.
        /// </summary>
        public bool IsBundleLoaded(string bundleName)
        {
            return _loadedBundles.ContainsKey(bundleName);
        }

        /// <summary>
        /// Get loaded bundle.
        /// </summary>
        public AssetBundle GetBundle(string bundleName)
        {
            return _loadedBundles.TryGetValue(bundleName, out var loadedBundle) ? loadedBundle.bundle : null;
        }

        #endregion

        #region Context Menu

        [ContextMenu("Load Manifest")]
        private void LoadManifestMenu()
        {
            LoadManifest();
        }

        [ContextMenu("Unload All Bundles")]
        private void UnloadAllBundlesMenu()
        {
            UnloadAllBundles(false);
        }

        [ContextMenu("Clear Cache")]
        private void ClearCacheMenu()
        {
            ClearCache();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Asset Bundle Statistics ===\n" +
                      $"Loaded Bundles: {stats.loadedBundleCount}\n" +
                      $"Total Loaded: {stats.totalBundlesLoaded}\n" +
                      $"Total Unloaded: {stats.totalBundlesUnloaded}\n" +
                      $"Downloaded: {stats.totalBytesDownloaded / 1024 / 1024}MB\n" +
                      $"Cache Hits: {stats.cacheHits}\n" +
                      $"Cache Misses: {stats.cacheMisses}\n" +
                      $"Cache Size: {stats.cacheSize / 1024 / 1024}MB\n" +
                      $"Queued: {stats.queuedDownloads}\n" +
                      $"Active: {stats.activeDownloads}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Loaded bundle data.
    /// </summary>
    [Serializable]
    public class LoadedBundle
    {
        public AssetBundle bundle;
        public float loadTime;
        public long size;
    }

    /// <summary>
    /// Bundle download request.
    /// </summary>
    public class BundleDownload
    {
        public string bundleName;
        public BundleManifestEntry manifestEntry;
        public Action<AssetBundle> onSuccess;
        public Action<string> onError;
    }

    /// <summary>
    /// Bundle manifest.
    /// </summary>
    [Serializable]
    public class BundleManifest
    {
        public BundleManifestEntry[] bundles;
    }

    /// <summary>
    /// Bundle manifest entry.
    /// </summary>
    [Serializable]
    public class BundleManifestEntry
    {
        public string bundleName;
        public string hash;
        public long size;
        public string[] dependencies;
    }

    /// <summary>
    /// Asset bundle statistics.
    /// </summary>
    [Serializable]
    public struct AssetBundleStats
    {
        public int loadedBundleCount;
        public int totalBundlesLoaded;
        public int totalBundlesUnloaded;
        public long totalBytesDownloaded;
        public int cacheHits;
        public int cacheMisses;
        public int queuedDownloads;
        public int activeDownloads;
        public long cacheSize;
    }

    #endregion
}
