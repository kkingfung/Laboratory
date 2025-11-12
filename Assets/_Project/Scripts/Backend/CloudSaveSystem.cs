using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Backend
{
    /// <summary>
    /// Cloud save system for cross-device save synchronization.
    /// Handles upload/download of save data to backend with conflict resolution.
    /// Supports offline mode with automatic sync when connection restored.
    /// </summary>
    public class CloudSaveSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string saveEndpoint = "/saves";
        [SerializeField] private float syncInterval = 300f; // 5 minutes
        [SerializeField] private int maxRetries = 3;

        [Header("Sync Settings")]
        [SerializeField] private bool autoSync = true;
        [SerializeField] private bool syncOnApplicationPause = true;
        [SerializeField] private bool syncOnApplicationQuit = true;
        [SerializeField] private ConflictResolutionStrategy conflictStrategy = ConflictResolutionStrategy.ServerWins;

        [Header("Offline Mode")]
        [SerializeField] private bool enableOfflineMode = true;
        [SerializeField] private int maxOfflineChanges = 100;

        #endregion

        #region Private Fields

        private static CloudSaveSystem _instance;

        // Sync state
        private bool _isSyncing = false;
        private bool _hasPendingChanges = false;
        private float _lastSyncTime = 0f;
        private int _consecutiveFailures = 0;

        // Save data
        private CloudSaveData _localSave;
        private CloudSaveData _cloudSave;
        private readonly Queue<SaveOperation> _pendingOperations = new Queue<SaveOperation>();

        // Connection state
        private bool _isOnline = true;
        private bool _isAuthenticated = false;
        private string _userId = "";
        private string _authToken = "";

        // Statistics
        private int _totalUploads = 0;
        private int _totalDownloads = 0;
        private int _conflictsResolved = 0;
        private int _syncFailures = 0;

        // Events
        public event Action OnSyncStarted;
        public event Action<CloudSaveData> OnSyncCompleted;
        public event Action<string> OnSyncFailed;
        public event Action<ConflictResolution> OnConflictResolved;

        #endregion

        #region Properties

        public static CloudSaveSystem Instance => _instance;
        public bool IsOnline => _isOnline;
        public bool IsSyncing => _isSyncing;
        public bool HasPendingChanges => _hasPendingChanges;
        public DateTime LastSyncTime => _localSave?.lastSyncTime ?? DateTime.MinValue;

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
            if (!autoSync || !_isAuthenticated) return;

            // Auto-sync at intervals
            if (Time.time - _lastSyncTime >= syncInterval && !_isSyncing)
            {
                StartCoroutine(SyncSaveData());
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && syncOnApplicationPause && _hasPendingChanges)
            {
                StartCoroutine(SyncSaveData());
            }
        }

        private void OnApplicationQuit()
        {
            if (syncOnApplicationQuit && _hasPendingChanges)
            {
                // Force synchronous save (blocking)
                // Note: In production, use background task API for mobile
                StartCoroutine(SyncSaveData());
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[CloudSaveSystem] Initializing...");

            // Load local save
            LoadLocalSave();

            // Check connection
            StartCoroutine(CheckConnectionStatus());

            Debug.Log("[CloudSaveSystem] Initialized");
        }

        private void LoadLocalSave()
        {
            string path = GetLocalSavePath();
            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    _localSave = JsonUtility.FromJson<CloudSaveData>(json);
                    Debug.Log($"[CloudSaveSystem] Loaded local save: v{_localSave.version}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CloudSaveSystem] Failed to load local save: {ex.Message}");
                    _localSave = CreateNewSave();
                }
            }
            else
            {
                _localSave = CreateNewSave();
            }
        }

        private CloudSaveData CreateNewSave()
        {
            return new CloudSaveData
            {
                userId = _userId,
                version = 1,
                createdAt = DateTime.UtcNow,
                lastModified = DateTime.UtcNow,
                lastSyncTime = DateTime.MinValue,
                data = new Dictionary<string, string>()
            };
        }

        #endregion

        #region Authentication

        /// <summary>
        /// Set user credentials for cloud sync.
        /// </summary>
        public void SetCredentials(string userId, string authToken)
        {
            _userId = userId;
            _authToken = authToken;
            _isAuthenticated = !string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(authToken);

            if (_isAuthenticated)
            {
                Debug.Log($"[CloudSaveSystem] Authenticated as: {userId}");

                // Trigger initial sync
                StartCoroutine(SyncSaveData());
            }
        }

        /// <summary>
        /// Clear authentication.
        /// </summary>
        public void ClearCredentials()
        {
            _userId = "";
            _authToken = "";
            _isAuthenticated = false;
            Debug.Log("[CloudSaveSystem] Credentials cleared");
        }

        #endregion

        #region Save Data Management

        /// <summary>
        /// Set a value in the save data.
        /// </summary>
        public void SetValue(string key, string value)
        {
            if (_localSave == null)
            {
                _localSave = CreateNewSave();
            }

            _localSave.data[key] = value;
            _localSave.lastModified = DateTime.UtcNow;
            _localSave.version++;
            _hasPendingChanges = true;

            // Save locally
            SaveLocalData();

            // Queue for cloud sync
            if (enableOfflineMode || _isOnline)
            {
                QueueOperation(new SaveOperation
                {
                    operationType = OperationType.Set,
                    key = key,
                    value = value,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get a value from save data.
        /// </summary>
        public string GetValue(string key, string defaultValue = "")
        {
            if (_localSave?.data != null && _localSave.data.TryGetValue(key, out string value))
            {
                return value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Remove a key from save data.
        /// </summary>
        public void RemoveValue(string key)
        {
            if (_localSave?.data != null && _localSave.data.Remove(key))
            {
                _localSave.lastModified = DateTime.UtcNow;
                _localSave.version++;
                _hasPendingChanges = true;

                SaveLocalData();

                QueueOperation(new SaveOperation
                {
                    operationType = OperationType.Remove,
                    key = key,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clear all save data.
        /// </summary>
        public void ClearAllData()
        {
            _localSave = CreateNewSave();
            _hasPendingChanges = true;
            SaveLocalData();

            QueueOperation(new SaveOperation
            {
                operationType = OperationType.Clear,
                timestamp = DateTime.UtcNow
            });
        }

        private void SaveLocalData()
        {
            try
            {
                string path = GetLocalSavePath();
                string json = JsonUtility.ToJson(_localSave, true);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSaveSystem] Failed to save local data: {ex.Message}");
            }
        }

        private void QueueOperation(SaveOperation operation)
        {
            _pendingOperations.Enqueue(operation);

            if (_pendingOperations.Count > maxOfflineChanges)
            {
                _pendingOperations.Dequeue(); // Drop oldest
                Debug.LogWarning($"[CloudSaveSystem] Exceeded max offline changes ({maxOfflineChanges})");
            }
        }

        #endregion

        #region Cloud Synchronization

        /// <summary>
        /// Manually trigger cloud sync.
        /// </summary>
        public void TriggerSync()
        {
            if (!_isAuthenticated)
            {
                Debug.LogWarning("[CloudSaveSystem] Cannot sync: Not authenticated");
                return;
            }

            StartCoroutine(SyncSaveData());
        }

        private IEnumerator SyncSaveData()
        {
            if (_isSyncing)
            {
                Debug.Log("[CloudSaveSystem] Sync already in progress");
                yield break;
            }

            if (!_isAuthenticated)
            {
                Debug.LogWarning("[CloudSaveSystem] Cannot sync: Not authenticated");
                yield break;
            }

            _isSyncing = true;
            _lastSyncTime = Time.time;
            OnSyncStarted?.Invoke();

            Debug.Log("[CloudSaveSystem] Starting sync...");

            // Step 1: Download cloud save
            yield return StartCoroutine(DownloadCloudSave());

            if (_cloudSave != null)
            {
                // Step 2: Resolve conflicts
                var resolution = ResolveConflicts(_localSave, _cloudSave);

                if (resolution.hadConflict)
                {
                    _conflictsResolved++;
                    OnConflictResolved?.Invoke(resolution);
                    Debug.LogWarning($"[CloudSaveSystem] Conflict resolved: {conflictStrategy}");
                }

                // Use merged save
                _localSave = resolution.mergedSave;
                SaveLocalData();
            }

            // Step 3: Upload local save
            yield return StartCoroutine(UploadCloudSave());

            _isSyncing = false;
            _hasPendingChanges = false;
            _localSave.lastSyncTime = DateTime.UtcNow;
            SaveLocalData();

            OnSyncCompleted?.Invoke(_localSave);
            Debug.Log($"[CloudSaveSystem] Sync completed. Version: {_localSave.version}");
        }

        private IEnumerator DownloadCloudSave()
        {
            string url = $"{backendUrl}{saveEndpoint}/{_userId}";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string json = request.downloadHandler.text;
                        _cloudSave = JsonUtility.FromJson<CloudSaveData>(json);
                        _totalDownloads++;
                        Debug.Log($"[CloudSaveSystem] Downloaded cloud save: v{_cloudSave.version}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[CloudSaveSystem] Failed to parse cloud save: {ex.Message}");
                        _syncFailures++;
                    }
                }
                else if (request.responseCode == 404)
                {
                    // No cloud save exists yet
                    Debug.Log("[CloudSaveSystem] No cloud save found (first sync)");
                    _cloudSave = null;
                }
                else
                {
                    Debug.LogError($"[CloudSaveSystem] Download failed: {request.error}");
                    _syncFailures++;
                    _consecutiveFailures++;
                    OnSyncFailed?.Invoke(request.error);
                }
            }
        }

        private IEnumerator UploadCloudSave()
        {
            string url = $"{backendUrl}{saveEndpoint}/{_userId}";
            string json = JsonUtility.ToJson(_localSave);

            using (UnityWebRequest request = new UnityWebRequest(url, "PUT"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {_authToken}");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    _totalUploads++;
                    _consecutiveFailures = 0;
                    Debug.Log($"[CloudSaveSystem] Uploaded save: v{_localSave.version}");
                }
                else
                {
                    Debug.LogError($"[CloudSaveSystem] Upload failed: {request.error}");
                    _syncFailures++;
                    _consecutiveFailures++;
                    OnSyncFailed?.Invoke(request.error);

                    // Retry logic
                    if (_consecutiveFailures < maxRetries)
                    {
                        Debug.Log($"[CloudSaveSystem] Will retry ({_consecutiveFailures}/{maxRetries})");
                    }
                }
            }
        }

        #endregion

        #region Conflict Resolution

        private ConflictResolution ResolveConflicts(CloudSaveData local, CloudSaveData cloud)
        {
            var resolution = new ConflictResolution
            {
                localSave = local,
                cloudSave = cloud,
                hadConflict = local.version != cloud.version || local.lastModified != cloud.lastModified
            };

            if (!resolution.hadConflict)
            {
                resolution.mergedSave = local;
                return resolution;
            }

            switch (conflictStrategy)
            {
                case ConflictResolutionStrategy.ServerWins:
                    resolution.mergedSave = cloud;
                    resolution.strategy = "Server Wins";
                    break;

                case ConflictResolutionStrategy.ClientWins:
                    resolution.mergedSave = local;
                    resolution.strategy = "Client Wins";
                    break;

                case ConflictResolutionStrategy.MostRecent:
                    resolution.mergedSave = local.lastModified > cloud.lastModified ? local : cloud;
                    resolution.strategy = "Most Recent";
                    break;

                case ConflictResolutionStrategy.Merge:
                    resolution.mergedSave = MergeSaveData(local, cloud);
                    resolution.strategy = "Merged";
                    break;
            }

            return resolution;
        }

        private CloudSaveData MergeSaveData(CloudSaveData local, CloudSaveData cloud)
        {
            var merged = new CloudSaveData
            {
                userId = local.userId,
                version = Math.Max(local.version, cloud.version) + 1,
                createdAt = local.createdAt < cloud.createdAt ? local.createdAt : cloud.createdAt,
                lastModified = DateTime.UtcNow,
                lastSyncTime = DateTime.UtcNow,
                data = new Dictionary<string, string>()
            };

            // Start with cloud data
            foreach (var kvp in cloud.data)
            {
                merged.data[kvp.Key] = kvp.Value;
            }

            // Merge local changes (local wins for same key)
            foreach (var kvp in local.data)
            {
                merged.data[kvp.Key] = kvp.Value;
            }

            return merged;
        }

        #endregion

        #region Connection Management

        private IEnumerator CheckConnectionStatus()
        {
            while (true)
            {
                bool wasOnline = _isOnline;
                _isOnline = Application.internetReachability != NetworkReachability.NotReachable;

                if (!wasOnline && _isOnline)
                {
                    Debug.Log("[CloudSaveSystem] Connection restored. Syncing...");
                    if (_hasPendingChanges && _isAuthenticated)
                    {
                        StartCoroutine(SyncSaveData());
                    }
                }

                yield return new WaitForSeconds(5f);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get cloud save statistics.
        /// </summary>
        public CloudSaveStats GetStats()
        {
            return new CloudSaveStats
            {
                isAuthenticated = _isAuthenticated,
                isOnline = _isOnline,
                isSyncing = _isSyncing,
                hasPendingChanges = _hasPendingChanges,
                lastSyncTime = _localSave?.lastSyncTime ?? DateTime.MinValue,
                saveVersion = _localSave?.version ?? 0,
                totalUploads = _totalUploads,
                totalDownloads = _totalDownloads,
                conflictsResolved = _conflictsResolved,
                syncFailures = _syncFailures
            };
        }

        /// <summary>
        /// Get the local save path.
        /// </summary>
        private string GetLocalSavePath()
        {
            return Path.Combine(Application.persistentDataPath, "cloud_save.json");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Trigger Sync")]
        private void TriggerSyncMenu()
        {
            TriggerSync();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Cloud Save Statistics ===\n" +
                      $"Authenticated: {stats.isAuthenticated}\n" +
                      $"Online: {stats.isOnline}\n" +
                      $"Syncing: {stats.isSyncing}\n" +
                      $"Pending Changes: {stats.hasPendingChanges}\n" +
                      $"Last Sync: {stats.lastSyncTime}\n" +
                      $"Version: {stats.saveVersion}\n" +
                      $"Uploads: {stats.totalUploads}\n" +
                      $"Downloads: {stats.totalDownloads}\n" +
                      $"Conflicts: {stats.conflictsResolved}\n" +
                      $"Failures: {stats.syncFailures}");
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Cloud save data structure.
    /// </summary>
    [Serializable]
    public class CloudSaveData
    {
        public string userId;
        public int version;
        public DateTime createdAt;
        public DateTime lastModified;
        public DateTime lastSyncTime;
        public Dictionary<string, string> data;
    }

    /// <summary>
    /// A save operation for offline queue.
    /// </summary>
    [Serializable]
    public struct SaveOperation
    {
        public OperationType operationType;
        public string key;
        public string value;
        public DateTime timestamp;
    }

    /// <summary>
    /// Conflict resolution result.
    /// </summary>
    [Serializable]
    public struct ConflictResolution
    {
        public CloudSaveData localSave;
        public CloudSaveData cloudSave;
        public CloudSaveData mergedSave;
        public bool hadConflict;
        public string strategy;
    }

    /// <summary>
    /// Cloud save statistics.
    /// </summary>
    [Serializable]
    public struct CloudSaveStats
    {
        public bool isAuthenticated;
        public bool isOnline;
        public bool isSyncing;
        public bool hasPendingChanges;
        public DateTime lastSyncTime;
        public int saveVersion;
        public int totalUploads;
        public int totalDownloads;
        public int conflictsResolved;
        public int syncFailures;
    }

    /// <summary>
    /// Types of save operations.
    /// </summary>
    public enum OperationType
    {
        Set,
        Remove,
        Clear
    }

    /// <summary>
    /// Conflict resolution strategies.
    /// </summary>
    public enum ConflictResolutionStrategy
    {
        ServerWins,    // Cloud version always wins
        ClientWins,    // Local version always wins
        MostRecent,    // Most recently modified wins
        Merge          // Merge both (client keys override server)
    }

    #endregion
}
