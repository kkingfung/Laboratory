using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Gameplay.Inventory;
using Laboratory.Subsystems.Inventory;
using Newtonsoft.Json;

namespace Laboratory.Subsystems.Inventory
{
    /// <summary>
    /// Inventory Save/Load System for Laboratory Unity Project
    /// Version 1.0 - Complete persistence system with versioning and validation
    /// </summary>
    public class InventorySaveSystem : MonoBehaviour
    {
        #region Fields

        [Header("Save Configuration")]
        [SerializeField] private string saveFileName = "inventory_save.json";
        [SerializeField] private bool enableAutoSave = true;
        [SerializeField] private float autoSaveInterval = 30f; // seconds
        [SerializeField] private bool enableBackups = true;
        [SerializeField] private int maxBackupFiles = 5;
        [SerializeField] private bool enableDebugLogs = true;

        [Header("Encryption (Optional)")]
        [SerializeField] private bool enableEncryption = false;
        [SerializeField] private string encryptionKey = "YourEncryptionKey";

        private EnhancedInventorySystem _inventorySystem;
        private IEventBus _eventBus;
        private string _saveDirectory;
        private float _lastAutoSaveTime;

        private const int SAVE_VERSION = 1;

        #endregion

        #region Events

        public event Action<InventorySaveData> OnInventorySaved;
        public event Action<InventorySaveData> OnInventoryLoaded;
        public event Action<string> OnSaveError;
        public event Action<string> OnLoadError;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSaveSystem();
        }

        private void Start()
        {
            AutoLoadInventory();
        }

        private void Update()
        {
            HandleAutoSave();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
                SaveInventory();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
                SaveInventory();
        }

        private void OnDestroy()
        {
            SaveInventory();
        }

        #endregion

        #region Initialization

        private void InitializeSaveSystem()
        {
            try
            {
                // Set up save directory
                _saveDirectory = Path.Combine(Application.persistentDataPath, "Saves", "Inventory");
                if (!Directory.Exists(_saveDirectory))
                {
                    Directory.CreateDirectory(_saveDirectory);
                }

                // Get dependencies
                _inventorySystem = FindFirstObjectByType<EnhancedInventorySystem>();
                _eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();

                if (enableDebugLogs)
                    Debug.Log($"[InventorySaveSystem] Initialized with save directory: {_saveDirectory}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveSystem] Failed to initialize: {ex.Message}");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Saves the current inventory state
        /// </summary>
        public bool SaveInventory(string customFileName = null)
        {
            if (_inventorySystem == null)
            {
                Debug.LogError("[InventorySaveSystem] No inventory system found");
                return false;
            }

            try
            {
                var saveData = CreateSaveData();
                var fileName = customFileName ?? saveFileName;
                var filePath = Path.Combine(_saveDirectory, fileName);

                // Create backup if enabled
                if (enableBackups && File.Exists(filePath))
                {
                    CreateBackup(filePath);
                }

                // Convert to JSON
                var json = JsonConvert.SerializeObject(saveData, Formatting.Indented);

                // Apply encryption if enabled
                if (enableEncryption)
                {
                    json = EncryptData(json);
                }

                // Write to file
                File.WriteAllText(filePath, json);

                // Fire events
                OnInventorySaved?.Invoke(saveData);
                _eventBus?.Publish(new InventorySavedEvent(saveData, filePath));

                if (enableDebugLogs)
                    Debug.Log($"[InventorySaveSystem] Inventory saved successfully to: {filePath}");

                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to save inventory: {ex.Message}";
                Debug.LogError($"[InventorySaveSystem] {errorMessage}");
                
                OnSaveError?.Invoke(errorMessage);
                _eventBus?.Publish(new InventorySaveErrorEvent(errorMessage));
                
                return false;
            }
        }

        /// <summary>
        /// Loads inventory state from file
        /// </summary>
        public bool LoadInventory(string customFileName = null)
        {
            if (_inventorySystem == null)
            {
                Debug.LogError("[InventorySaveSystem] No inventory system found");
                return false;
            }

            try
            {
                var fileName = customFileName ?? saveFileName;
                var filePath = Path.Combine(_saveDirectory, fileName);

                if (!File.Exists(filePath))
                {
                    if (enableDebugLogs)
                        Debug.Log($"[InventorySaveSystem] No save file found at: {filePath}");
                    return false;
                }

                // Read file content
                var json = File.ReadAllText(filePath);

                // Decrypt if needed
                if (enableEncryption)
                {
                    json = DecryptData(json);
                }

                // Deserialize
                var saveData = JsonConvert.DeserializeObject<InventorySaveData>(json);

                if (!ValidateSaveData(saveData))
                {
                    Debug.LogError("[InventorySaveSystem] Invalid save data");
                    return false;
                }

                // Apply save data to inventory system
                ApplySaveData(saveData);

                // Fire events
                OnInventoryLoaded?.Invoke(saveData);
                _eventBus?.Publish(new InventoryLoadedEvent(saveData, filePath));

                if (enableDebugLogs)
                    Debug.Log($"[InventorySaveSystem] Inventory loaded successfully from: {filePath}");

                return true;
            }
            catch (Exception ex)
            {
                var errorMessage = $"Failed to load inventory: {ex.Message}";
                Debug.LogError($"[InventorySaveSystem] {errorMessage}");
                
                OnLoadError?.Invoke(errorMessage);
                _eventBus?.Publish(new InventoryLoadErrorEvent(errorMessage));
                
                return false;
            }
        }

        /// <summary>
        /// Checks if a save file exists
        /// </summary>
        public bool HasSaveFile(string customFileName = null)
        {
            var fileName = customFileName ?? saveFileName;
            var filePath = Path.Combine(_saveDirectory, fileName);
            return File.Exists(filePath);
        }

        /// <summary>
        /// Deletes a save file
        /// </summary>
        public bool DeleteSaveFile(string customFileName = null)
        {
            try
            {
                var fileName = customFileName ?? saveFileName;
                var filePath = Path.Combine(_saveDirectory, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    
                    if (enableDebugLogs)
                        Debug.Log($"[InventorySaveSystem] Deleted save file: {filePath}");
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveSystem] Failed to delete save file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets available save files
        /// </summary>
        public List<InventorySaveInfo> GetAvailableSaves()
        {
            var saves = new List<InventorySaveInfo>();

            try
            {
                if (!Directory.Exists(_saveDirectory))
                    return saves;

                var files = Directory.GetFiles(_saveDirectory, "*.json");

                foreach (var file in files)
                {
                    try
                    {
                        var info = new FileInfo(file);
                        var saveInfo = new InventorySaveInfo
                        {
                            FileName = Path.GetFileName(file),
                            FilePath = file,
                            CreationTime = info.CreationTime,
                            LastModified = info.LastWriteTime,
                            FileSize = info.Length
                        };

                        // Try to get additional info from file content
                        var json = File.ReadAllText(file);
                        if (enableEncryption)
                            json = DecryptData(json);

                        var saveData = JsonConvert.DeserializeObject<InventorySaveData>(json);
                        if (saveData != null)
                        {
                            saveInfo.SaveVersion = saveData.SaveVersion;
                            saveInfo.ItemCount = saveData.Items?.Count ?? 0;
                            saveInfo.IsValid = ValidateSaveData(saveData);
                        }

                        saves.Add(saveInfo);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[InventorySaveSystem] Failed to read save info for {file}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveSystem] Failed to get available saves: {ex.Message}");
            }

            return saves.OrderByDescending(s => s.LastModified).ToList();
        }

        #endregion

        #region Private Methods

        private void HandleAutoSave()
        {
            if (!enableAutoSave)
                return;

            if (Time.unscaledTime - _lastAutoSaveTime >= autoSaveInterval)
            {
                SaveInventory();
                _lastAutoSaveTime = Time.unscaledTime;
            }
        }

        private void AutoLoadInventory()
        {
            if (HasSaveFile())
            {
                LoadInventory();
            }
        }

        private InventorySaveData CreateSaveData()
        {
            var saveData = new InventorySaveData
            {
                SaveVersion = SAVE_VERSION,
                SaveTimestamp = DateTime.Now,
                Items = new List<InventoryItemSaveData>()
            };

            // Convert inventory items to save data
            var items = _inventorySystem.GetAllItems();
            foreach (var item in items)
            {
                var itemSaveData = new InventoryItemSaveData
                {
                    ItemID = item.ItemData.ItemID,
                    Quantity = item.Quantity,
                    SlotIndex = item.SlotIndex,
                    AcquiredTime = item.AcquiredTime,
                    LastUsedTime = item.LastUsedTime
                };

                saveData.Items.Add(itemSaveData);
            }

            return saveData;
        }

        private bool ValidateSaveData(InventorySaveData saveData)
        {
            if (saveData == null)
                return false;

            if (saveData.SaveVersion > SAVE_VERSION)
            {
                Debug.LogWarning($"[InventorySaveSystem] Save file version {saveData.SaveVersion} is newer than supported version {SAVE_VERSION}");
                return false;
            }

            if (saveData.Items == null)
            {
                Debug.LogWarning("[InventorySaveSystem] Save data has null items list");
                return false;
            }

            return true;
        }

        private void ApplySaveData(InventorySaveData saveData)
        {
            // Clear current inventory
            var currentItems = _inventorySystem.GetAllItems().ToArray();
            foreach (var item in currentItems)
            {
                _inventorySystem.TryRemoveItem(item.ItemData.ItemID, item.Quantity);
            }

            // Load items from save data
            foreach (var itemSaveData in saveData.Items)
            {
                // Find item data by ID (would need ItemDatabase service)
                var itemData = FindItemDataByID(itemSaveData.ItemID);
                if (itemData != null)
                {
                    _inventorySystem.TryAddItem(itemData, itemSaveData.Quantity);
                }
                else
                {
                    Debug.LogWarning($"[InventorySaveSystem] Could not find ItemData for ID: {itemSaveData.ItemID}");
                }
            }
        }

        private ItemData FindItemDataByID(string itemID)
        {
            // This would typically be resolved through an ItemDatabase service
            // For now, we'll try to find it in Resources
            var allItemData = Resources.LoadAll<ItemData>("Items");
            return allItemData.FirstOrDefault(item => item.ItemID == itemID);
        }

        private void CreateBackup(string originalFilePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(originalFilePath);
                var fileName = Path.GetFileNameWithoutExtension(originalFilePath);
                var extension = Path.GetExtension(originalFilePath);
                
                var backupFileName = $"{fileName}_backup_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var backupPath = Path.Combine(directory, backupFileName);
                
                File.Copy(originalFilePath, backupPath);
                
                // Clean up old backups
                CleanupOldBackups(directory, fileName, extension);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InventorySaveSystem] Failed to create backup: {ex.Message}");
            }
        }

        private void CleanupOldBackups(string directory, string fileName, string extension)
        {
            try
            {
                var pattern = $"{fileName}_backup_*{extension}";
                var backupFiles = Directory.GetFiles(directory, pattern)
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(maxBackupFiles)
                    .ToArray();

                foreach (var file in backupFiles)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InventorySaveSystem] Failed to cleanup old backups: {ex.Message}");
            }
        }

        private string EncryptData(string data)
        {
            // Simple encryption - in production, use proper encryption
            try
            {
                var dataBytes = System.Text.Encoding.UTF8.GetBytes(data);
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(encryptionKey);
                
                for (int i = 0; i < dataBytes.Length; i++)
                {
                    dataBytes[i] ^= keyBytes[i % keyBytes.Length];
                }
                
                return Convert.ToBase64String(dataBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveSystem] Encryption failed: {ex.Message}");
                return data; // Return original data if encryption fails
            }
        }

        private string DecryptData(string encryptedData)
        {
            // Simple decryption - in production, use proper decryption
            try
            {
                var dataBytes = Convert.FromBase64String(encryptedData);
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(encryptionKey);
                
                for (int i = 0; i < dataBytes.Length; i++)
                {
                    dataBytes[i] ^= keyBytes[i % keyBytes.Length];
                }
                
                return System.Text.Encoding.UTF8.GetString(dataBytes);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventorySaveSystem] Decryption failed: {ex.Message}");
                return encryptedData; // Return original data if decryption fails
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Complete inventory save data
    /// </summary>
    [System.Serializable]
    public class InventorySaveData
    {
        public int SaveVersion;
        public DateTime SaveTimestamp;
        public List<InventoryItemSaveData> Items;
        
        // Additional metadata
        public string PlayerID;
        public int PlayerLevel;
        public Dictionary<string, object> CustomData;
    }

    /// <summary>
    /// Individual inventory item save data
    /// </summary>
    [System.Serializable]
    public class InventoryItemSaveData
    {
        public string ItemID;
        public int Quantity;
        public int SlotIndex;
        public DateTime AcquiredTime;
        public DateTime LastUsedTime;
        
        // Additional item-specific data
        public Dictionary<string, object> CustomData;
    }

    /// <summary>
    /// Information about a save file
    /// </summary>
    [System.Serializable]
    public class InventorySaveInfo
    {
        public string FileName;
        public string FilePath;
        public DateTime CreationTime;
        public DateTime LastModified;
        public long FileSize;
        public int SaveVersion;
        public int ItemCount;
        public bool IsValid;
        
        public string FormattedFileSize => FormatBytes(FileSize);
        public string TimeSinceLastModified => GetTimeSinceString(LastModified);

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private string GetTimeSinceString(DateTime dateTime)
        {
            var timeSince = DateTime.Now - dateTime;
            if (timeSince.TotalDays >= 1)
                return $"{(int)timeSince.TotalDays} days ago";
            if (timeSince.TotalHours >= 1)
                return $"{(int)timeSince.TotalHours} hours ago";
            if (timeSince.TotalMinutes >= 1)
                return $"{(int)timeSince.TotalMinutes} minutes ago";
            return "Just now";
        }
    }

    #endregion

    #region Events

    public class InventorySavedEvent
    {
        public InventorySaveData SaveData { get; }
        public string FilePath { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public InventorySavedEvent(InventorySaveData saveData, string filePath)
        {
            SaveData = saveData;
            FilePath = filePath;
        }
    }

    public class InventoryLoadedEvent
    {
        public InventorySaveData SaveData { get; }
        public string FilePath { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public InventoryLoadedEvent(InventorySaveData saveData, string filePath)
        {
            SaveData = saveData;
            FilePath = filePath;
        }
    }

    public class InventorySaveErrorEvent
    {
        public string ErrorMessage { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public InventorySaveErrorEvent(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }

    public class InventoryLoadErrorEvent
    {
        public string ErrorMessage { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public InventoryLoadErrorEvent(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }

    #endregion
}