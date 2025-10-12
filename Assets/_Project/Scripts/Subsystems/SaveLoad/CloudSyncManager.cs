using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Cloud synchronization manager for cross-platform save persistence.
    /// Handles uploading, downloading, and conflict resolution for cloud saves.
    /// </summary>
    public class CloudSyncManager : MonoBehaviour, ICloudSyncService
    {
        [Header("Configuration")]
        [SerializeField] private SaveLoadSubsystemConfig config;

        [Header("Cloud Sync State")]
        [SerializeField] private bool isCloudSyncEnabled = false;
        [SerializeField] private CloudSyncStatus syncStatus = CloudSyncStatus.Idle;
        [SerializeField] private CloudProvider currentProvider = CloudProvider.Unity;

        [Header("Debug Info")]
        [SerializeField] private int totalUploads;
        [SerializeField] private int totalDownloads;
        [SerializeField] private DateTime lastSyncTime;
        [SerializeField] private List<string> activeOperations = new();

        // Properties
        public bool IsCloudSyncEnabled
        {
            get => isCloudSyncEnabled;
            set
            {
                isCloudSyncEnabled = value;
                if (value && !_isInitialized)
                {
                    _ = InitializeCloudProvider();
                }
            }
        }

        public CloudSyncStatus SyncStatus => syncStatus;

        // State
        private bool _isInitialized = false;
        private ICloudProvider _cloudProvider;
        private readonly Dictionary<int, CloudSaveInfo> _cloudSaveCache = new();
        private readonly Dictionary<string, ConflictDetectedEvent> _pendingConflicts = new();

        // Events
        public event Action<CloudSyncEvent> OnCloudSyncEvent;
        public event Action<ConflictDetectedEvent> OnConflictDetected;

        #region Initialization

        public async Task InitializeAsync(SaveLoadSubsystemConfig configuration)
        {
            config = configuration;

            try
            {
                ApplyConfiguration();

                if (isCloudSyncEnabled)
                {
                    await InitializeCloudProvider();
                }

                _isInitialized = true;
                Debug.Log($"[CloudSyncManager] Initialized - Provider: {currentProvider}, Enabled: {isCloudSyncEnabled}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSyncManager] Initialization failed: {ex.Message}");
                throw;
            }
        }

        private void ApplyConfiguration()
        {
            if (config?.CloudSyncConfig != null)
            {
                isCloudSyncEnabled = config.CloudSyncConfig.EnableCloudSync;
                currentProvider = config.CloudSyncConfig.CloudProvider;
            }
        }

        private async Task InitializeCloudProvider()
        {
            try
            {
                UpdateSyncStatus(CloudSyncStatus.Connecting);

                _cloudProvider = CreateCloudProvider(currentProvider);
                await _cloudProvider.InitializeAsync();

                // Load cloud save info cache
                await RefreshCloudSaveCache();

                UpdateSyncStatus(CloudSyncStatus.Idle);
                Debug.Log($"[CloudSyncManager] Cloud provider '{currentProvider}' initialized successfully");
            }
            catch (Exception ex)
            {
                UpdateSyncStatus(CloudSyncStatus.Failed);
                Debug.LogError($"[CloudSyncManager] Cloud provider initialization failed: {ex.Message}");
                throw;
            }
        }

        private ICloudProvider CreateCloudProvider(CloudProvider provider)
        {
            return provider switch
            {
                CloudProvider.Unity => new UnityCloudProvider(),
                CloudProvider.Steam => new SteamCloudProvider(),
                CloudProvider.GooglePlay => new GooglePlayCloudProvider(),
                CloudProvider.Custom => new CustomCloudProvider(),
                _ => new UnityCloudProvider()
            };
        }

        #endregion

        #region Core Cloud Operations

        public async Task<bool> UploadSaveAsync(int slotId, GameSaveData gameData)
        {
            if (!CanPerformCloudOperation("Upload"))
                return false;

            var operationId = $"Upload_{slotId}_{DateTime.UtcNow.Ticks}";
            var cloudEvent = new CloudSyncEvent
            {
                operation = CloudSyncOperation.Upload,
                slotId = slotId,
                timestamp = DateTime.UtcNow
            };

            try
            {
                activeOperations.Add(operationId);
                UpdateSyncStatus(CloudSyncStatus.Uploading);
                cloudEvent.status = CloudSyncStatus.Uploading;
                OnCloudSyncEvent?.Invoke(cloudEvent);

                // Serialize the save data
                var saveData = SerializeSaveData(gameData);

                // Upload to cloud
                var cloudSaveId = await _cloudProvider.UploadSaveAsync(slotId, saveData);

                if (!string.IsNullOrEmpty(cloudSaveId))
                {
                    // Update cloud save cache
                    var cloudSaveInfo = new CloudSaveInfo
                    {
                        slotId = slotId,
                        saveName = gameData.saveMetadata.saveName,
                        lastModified = DateTime.UtcNow,
                        fileSizeBytes = saveData.Length,
                        cloudProvider = currentProvider.ToString(),
                        syncStatus = CloudSyncStatus.Success
                    };

                    _cloudSaveCache[slotId] = cloudSaveInfo;

                    // Update statistics
                    totalUploads++;
                    lastSyncTime = DateTime.UtcNow;

                    cloudEvent.status = CloudSyncStatus.Success;
                    cloudEvent.bytesTransferred = saveData.Length;
                    cloudEvent.totalBytes = saveData.Length;
                    cloudEvent.progress = 1.0f;

                    OnCloudSyncEvent?.Invoke(cloudEvent);
                    Debug.Log($"[CloudSyncManager] Upload completed for slot {slotId}: {gameData.saveMetadata.saveName}");
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Cloud upload failed - no save ID returned");
                }
            }
            catch (Exception ex)
            {
                cloudEvent.status = CloudSyncStatus.Failed;
                cloudEvent.message = ex.Message;
                OnCloudSyncEvent?.Invoke(cloudEvent);

                Debug.LogError($"[CloudSyncManager] Upload failed for slot {slotId}: {ex.Message}");
                return false;
            }
            finally
            {
                activeOperations.Remove(operationId);
                if (activeOperations.Count == 0)
                {
                    UpdateSyncStatus(CloudSyncStatus.Idle);
                }
            }
        }

        public async Task<GameSaveData> DownloadSaveAsync(int slotId)
        {
            if (!CanPerformCloudOperation("Download"))
                return null;

            var operationId = $"Download_{slotId}_{DateTime.UtcNow.Ticks}";
            var cloudEvent = new CloudSyncEvent
            {
                operation = CloudSyncOperation.Download,
                slotId = slotId,
                timestamp = DateTime.UtcNow
            };

            try
            {
                activeOperations.Add(operationId);
                UpdateSyncStatus(CloudSyncStatus.Downloading);
                cloudEvent.status = CloudSyncStatus.Downloading;
                OnCloudSyncEvent?.Invoke(cloudEvent);

                // Download from cloud
                var saveData = await _cloudProvider.DownloadSaveAsync(slotId);

                if (saveData != null && saveData.Length > 0)
                {
                    // Deserialize the save data
                    var gameData = DeserializeSaveData(saveData);

                    if (gameData != null)
                    {
                        // Update statistics
                        totalDownloads++;
                        lastSyncTime = DateTime.UtcNow;

                        cloudEvent.status = CloudSyncStatus.Success;
                        cloudEvent.bytesTransferred = saveData.Length;
                        cloudEvent.totalBytes = saveData.Length;
                        cloudEvent.progress = 1.0f;

                        OnCloudSyncEvent?.Invoke(cloudEvent);
                        Debug.Log($"[CloudSyncManager] Download completed for slot {slotId}: {gameData.saveMetadata.saveName}");
                        return gameData;
                    }
                    else
                    {
                        throw new InvalidDataException("Failed to deserialize downloaded save data");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CloudSyncManager] No cloud save found for slot {slotId}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                cloudEvent.status = CloudSyncStatus.Failed;
                cloudEvent.message = ex.Message;
                OnCloudSyncEvent?.Invoke(cloudEvent);

                Debug.LogError($"[CloudSyncManager] Download failed for slot {slotId}: {ex.Message}");
                return null;
            }
            finally
            {
                activeOperations.Remove(operationId);
                if (activeOperations.Count == 0)
                {
                    UpdateSyncStatus(CloudSyncStatus.Idle);
                }
            }
        }

        public async Task<bool> DeleteCloudSaveAsync(int slotId)
        {
            if (!CanPerformCloudOperation("Delete"))
                return false;

            try
            {
                var result = await _cloudProvider.DeleteSaveAsync(slotId);

                if (result)
                {
                    // Remove from cache
                    _cloudSaveCache.Remove(slotId);

                    var cloudEvent = new CloudSyncEvent
                    {
                        operation = CloudSyncOperation.Delete,
                        slotId = slotId,
                        status = CloudSyncStatus.Success,
                        timestamp = DateTime.UtcNow
                    };

                    OnCloudSyncEvent?.Invoke(cloudEvent);
                    Debug.Log($"[CloudSyncManager] Cloud save deleted for slot {slotId}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSyncManager] Delete cloud save failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SyncAllSavesAsync()
        {
            if (!CanPerformCloudOperation("SyncAll"))
                return false;

            try
            {
                UpdateSyncStatus(CloudSyncStatus.Syncing);

                var cloudEvent = new CloudSyncEvent
                {
                    operation = CloudSyncOperation.Sync,
                    status = CloudSyncStatus.Syncing,
                    timestamp = DateTime.UtcNow
                };

                OnCloudSyncEvent?.Invoke(cloudEvent);

                // Get local and cloud save info
                var saveDataManager = FindFirstObjectByType<SaveDataManager>();
                if (saveDataManager == null)
                {
                    throw new InvalidOperationException("SaveDataManager not found");
                }

                var localSlots = await saveDataManager.GetSaveSlotInfoAsync();
                var cloudSaves = await GetCloudSaveInfoAsync();

                int syncedCount = 0;
                int conflictCount = 0;

                // Process each slot
                for (int slotId = 0; slotId < localSlots.Length; slotId++)
                {
                    var localSlot = localSlots[slotId];
                    var cloudSave = Array.Find(cloudSaves, c => c.slotId == slotId);

                    if (localSlot.isOccupied && cloudSave != null)
                    {
                        // Both exist - check for conflicts
                        if (await DetectConflict(localSlot, cloudSave))
                        {
                            await HandleConflict(slotId, localSlot, cloudSave);
                            conflictCount++;
                        }
                        else
                        {
                            // Sync the newer one
                            if (localSlot.lastSaved > cloudSave.lastModified)
                            {
                                var gameData = await saveDataManager.LoadGameDataAsync(slotId);
                                await UploadSaveAsync(slotId, gameData);
                            }
                            else if (cloudSave.lastModified > localSlot.lastSaved)
                            {
                                var cloudData = await DownloadSaveAsync(slotId);
                                if (cloudData != null)
                                {
                                    await saveDataManager.SaveGameDataAsync(slotId, cloudData);
                                }
                            }
                            syncedCount++;
                        }
                    }
                    else if (localSlot.isOccupied && cloudSave == null)
                    {
                        // Local only - upload to cloud
                        var gameData = await saveDataManager.LoadGameDataAsync(slotId);
                        await UploadSaveAsync(slotId, gameData);
                        syncedCount++;
                    }
                    else if (!localSlot.isOccupied && cloudSave != null)
                    {
                        // Cloud only - download to local
                        var cloudData = await DownloadSaveAsync(slotId);
                        if (cloudData != null)
                        {
                            await saveDataManager.SaveGameDataAsync(slotId, cloudData);
                            syncedCount++;
                        }
                    }
                }

                cloudEvent.status = CloudSyncStatus.Success;
                cloudEvent.message = $"Synced {syncedCount} saves, {conflictCount} conflicts detected";
                OnCloudSyncEvent?.Invoke(cloudEvent);

                Debug.Log($"[CloudSyncManager] Sync completed - {syncedCount} saves synced, {conflictCount} conflicts");
                return true;
            }
            catch (Exception ex)
            {
                var cloudEvent = new CloudSyncEvent
                {
                    operation = CloudSyncOperation.Sync,
                    status = CloudSyncStatus.Failed,
                    message = ex.Message,
                    timestamp = DateTime.UtcNow
                };

                OnCloudSyncEvent?.Invoke(cloudEvent);
                Debug.LogError($"[CloudSyncManager] Sync all failed: {ex.Message}");
                return false;
            }
            finally
            {
                UpdateSyncStatus(CloudSyncStatus.Idle);
            }
        }

        public async Task<CloudSaveInfo[]> GetCloudSaveInfoAsync()
        {
            if (!_isInitialized || !isCloudSyncEnabled)
                return new CloudSaveInfo[0];

            try
            {
                await RefreshCloudSaveCache();
                return _cloudSaveCache.Values.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSyncManager] Failed to get cloud save info: {ex.Message}");
                return new CloudSaveInfo[0];
            }
        }

        public async Task<bool> ResolveConflictAsync(int slotId, ConflictResolution resolution)
        {
            if (!_pendingConflicts.TryGetValue($"conflict_{slotId}", out var conflict))
            {
                Debug.LogWarning($"[CloudSyncManager] No pending conflict found for slot {slotId}");
                return false;
            }

            try
            {
                var saveDataManager = FindFirstObjectByType<SaveDataManager>();
                if (saveDataManager == null)
                {
                    throw new InvalidOperationException("SaveDataManager not found");
                }

                switch (resolution)
                {
                    case ConflictResolution.KeepLocal:
                        await UploadSaveAsync(slotId, conflict.localSave);
                        break;

                    case ConflictResolution.KeepRemote:
                        await saveDataManager.SaveGameDataAsync(slotId, conflict.cloudSave);
                        break;

                    case ConflictResolution.KeepBoth:
                        // Keep local in current slot, put cloud in next available slot
                        var nextSlot = await FindNextAvailableSlot(saveDataManager);
                        if (nextSlot >= 0)
                        {
                            await saveDataManager.SaveGameDataAsync(nextSlot, conflict.cloudSave);
                        }
                        break;

                    case ConflictResolution.MostRecent:
                        if (conflict.localTimestamp > conflict.cloudTimestamp)
                        {
                            await UploadSaveAsync(slotId, conflict.localSave);
                        }
                        else
                        {
                            await saveDataManager.SaveGameDataAsync(slotId, conflict.cloudSave);
                        }
                        break;
                }

                _pendingConflicts.Remove($"conflict_{slotId}");
                Debug.Log($"[CloudSyncManager] Conflict resolved for slot {slotId} using {resolution}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloudSyncManager] Conflict resolution failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helper Methods

        private bool CanPerformCloudOperation(string operation)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"[CloudSyncManager] Cannot {operation} - not initialized");
                return false;
            }

            if (!isCloudSyncEnabled)
            {
                Debug.LogWarning($"[CloudSyncManager] Cannot {operation} - cloud sync disabled");
                return false;
            }

            if (_cloudProvider == null)
            {
                Debug.LogWarning($"[CloudSyncManager] Cannot {operation} - no cloud provider");
                return false;
            }

            return true;
        }

        private void UpdateSyncStatus(CloudSyncStatus newStatus)
        {
            if (syncStatus != newStatus)
            {
                syncStatus = newStatus;
                Debug.Log($"[CloudSyncManager] Status changed to: {newStatus}");
            }
        }

        private async Task RefreshCloudSaveCache()
        {
            if (_cloudProvider == null)
                return;

            try
            {
                var cloudSaves = await _cloudProvider.GetCloudSaveListAsync();
                _cloudSaveCache.Clear();

                foreach (var save in cloudSaves)
                {
                    _cloudSaveCache[save.slotId] = save;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CloudSyncManager] Failed to refresh cloud save cache: {ex.Message}");
            }
        }

        private async Task<bool> DetectConflict(SaveSlotInfo localSlot, CloudSaveInfo cloudSave)
        {
            // Simple timestamp-based conflict detection
            var timeDifference = Math.Abs((localSlot.lastSaved - cloudSave.lastModified).TotalSeconds);
            return timeDifference > 60; // More than 1 minute difference
        }

        private async Task HandleConflict(int slotId, SaveSlotInfo localSlot, CloudSaveInfo cloudSave)
        {
            var saveDataManager = FindFirstObjectByType<SaveDataManager>();
            var localData = await saveDataManager.LoadGameDataAsync(slotId);
            var cloudData = await DownloadSaveAsync(slotId);

            var conflict = new ConflictDetectedEvent
            {
                slotId = slotId,
                localSave = localData,
                cloudSave = cloudData,
                localTimestamp = localSlot.lastSaved,
                cloudTimestamp = cloudSave.lastModified,
                reason = ConflictReason.TimestampMismatch
            };

            _pendingConflicts[$"conflict_{slotId}"] = conflict;
            OnConflictDetected?.Invoke(conflict);

            // Auto-resolve based on config
            if (config?.CloudSyncConfig.ConflictResolution != ConflictResolution.PromptUser)
            {
                await ResolveConflictAsync(slotId, config.CloudSyncConfig.ConflictResolution);
            }
        }

        private async Task<int> FindNextAvailableSlot(SaveDataManager saveDataManager)
        {
            var slots = await saveDataManager.GetSaveSlotInfoAsync();
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].isOccupied)
                {
                    return i;
                }
            }
            return -1;
        }

        private byte[] SerializeSaveData(GameSaveData gameData)
        {
            var jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(gameData, Newtonsoft.Json.Formatting.None);
            return System.Text.Encoding.UTF8.GetBytes(jsonData);
        }

        private GameSaveData DeserializeSaveData(byte[] data)
        {
            var jsonData = System.Text.Encoding.UTF8.GetString(data);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<GameSaveData>(jsonData);
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Cloud Upload")]
        private void TestCloudUpload()
        {
            if (_isInitialized && isCloudSyncEnabled)
            {
                Debug.Log("[CloudSyncManager] Test cloud upload - Not implemented in demo");
            }
        }

        [ContextMenu("Refresh Cloud Cache")]
        private void RefreshCloudCacheDebug()
        {
            if (_isInitialized)
            {
                _ = RefreshCloudSaveCache();
            }
        }

        #endregion
    }

    #region Cloud Provider Interfaces and Implementations

    public interface ICloudProvider
    {
        Task InitializeAsync();
        Task<string> UploadSaveAsync(int slotId, byte[] saveData);
        Task<byte[]> DownloadSaveAsync(int slotId);
        Task<bool> DeleteSaveAsync(int slotId);
        Task<CloudSaveInfo[]> GetCloudSaveListAsync();
        bool IsAuthenticated { get; }
    }

    public class UnityCloudProvider : ICloudProvider
    {
        public bool IsAuthenticated { get; private set; }

        public async Task InitializeAsync()
        {
            // Unity Cloud Save initialization
            IsAuthenticated = true;
            await Task.CompletedTask;
        }

        public async Task<string> UploadSaveAsync(int slotId, byte[] saveData)
        {
            // Unity Cloud Save upload implementation
            await Task.Delay(100); // Simulate network delay
            return $"unity_save_{slotId}_{DateTime.UtcNow.Ticks}";
        }

        public async Task<byte[]> DownloadSaveAsync(int slotId)
        {
            // Unity Cloud Save download implementation
            await Task.Delay(100); // Simulate network delay
            return null; // No save found
        }

        public async Task<bool> DeleteSaveAsync(int slotId)
        {
            // Unity Cloud Save delete implementation
            await Task.Delay(50);
            return true;
        }

        public async Task<CloudSaveInfo[]> GetCloudSaveListAsync()
        {
            // Unity Cloud Save list implementation
            await Task.Delay(100);
            return new CloudSaveInfo[0];
        }
    }

    public class SteamCloudProvider : ICloudProvider
    {
        public bool IsAuthenticated { get; private set; }

        public async Task InitializeAsync()
        {
            // Steam Cloud initialization
            IsAuthenticated = false; // Steam not available in this context
            await Task.CompletedTask;
        }

        public async Task<string> UploadSaveAsync(int slotId, byte[] saveData)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Steam Cloud integration not implemented");
        }

        public async Task<byte[]> DownloadSaveAsync(int slotId)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Steam Cloud integration not implemented");
        }

        public async Task<bool> DeleteSaveAsync(int slotId)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Steam Cloud integration not implemented");
        }

        public async Task<CloudSaveInfo[]> GetCloudSaveListAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Steam Cloud integration not implemented");
        }
    }

    public class GooglePlayCloudProvider : ICloudProvider
    {
        public bool IsAuthenticated { get; private set; }

        public async Task InitializeAsync()
        {
            // Google Play Games Services initialization
            IsAuthenticated = false; // Not available in this context
            await Task.CompletedTask;
        }

        public async Task<string> UploadSaveAsync(int slotId, byte[] saveData)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Google Play Cloud integration not implemented");
        }

        public async Task<byte[]> DownloadSaveAsync(int slotId)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Google Play Cloud integration not implemented");
        }

        public async Task<bool> DeleteSaveAsync(int slotId)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Google Play Cloud integration not implemented");
        }

        public async Task<CloudSaveInfo[]> GetCloudSaveListAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Google Play Cloud integration not implemented");
        }
    }

    public class CustomCloudProvider : ICloudProvider
    {
        public bool IsAuthenticated { get; private set; }

        public async Task InitializeAsync()
        {
            // Custom cloud service initialization
            IsAuthenticated = false;
            await Task.CompletedTask;
        }

        public async Task<string> UploadSaveAsync(int slotId, byte[] saveData)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Custom cloud integration not implemented");
        }

        public async Task<byte[]> DownloadSaveAsync(int slotId)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Custom cloud integration not implemented");
        }

        public async Task<bool> DeleteSaveAsync(int slotId)
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Custom cloud integration not implemented");
        }

        public async Task<CloudSaveInfo[]> GetCloudSaveListAsync()
        {
            await Task.CompletedTask;
            throw new NotImplementedException("Custom cloud integration not implemented");
        }
    }

    #endregion
}