using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Core save data manager responsible for reading and writing game save files.
    /// Handles file I/O, compression, encryption, and save slot management.
    /// </summary>
    public class SaveDataManager : MonoBehaviour, ISaveDataService
    {
        [Header("Dependencies")]
        [SerializeField] private SaveLoadSubsystemConfig config;

        // Services
        private IFileSystemService _fileSystemService;
        private ICompressionService _compressionService;
        private IEncryptionService _encryptionService;

        // State
        private bool _isInitialized = false;
        private readonly Dictionary<int, SaveSlotInfo> _saveSlotCache = new();
        private readonly object _lockObject = new object();

        // Events
        public event Action<SaveEvent> OnSaveCompleted;
        public event Action<LoadEvent> OnLoadCompleted;
        public event Action<SaveLoadError> OnSaveLoadError;

        #region Initialization

        public async Task InitializeAsync(SaveLoadSubsystemConfig configuration)
        {
            config = configuration;

            try
            {
                // Initialize services
                await InitializeServices();

                // Ensure save directories exist
                await CreateSaveDirectories();

                // Load save slot cache
                await RefreshSaveSlotCache();

                _isInitialized = true;
                Debug.Log($"[SaveDataManager] Initialized successfully with {_saveSlotCache.Count} save slots");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataManager] Initialization failed: {ex.Message}");
                throw;
            }
        }

        private async Task InitializeServices()
        {
            // Initialize file system service
            _fileSystemService = new UnityFileSystemService();

            // Initialize compression service if enabled
            if (config.SaveConfig.EnableCompression)
            {
                _compressionService = new GZipCompressionService();
            }

            // Initialize encryption service if enabled
            if (config.SaveConfig.EnableEncryption)
            {
                _encryptionService = new AESEncryptionService();
            }

            await Task.CompletedTask;
        }

        private async Task CreateSaveDirectories()
        {
            var saveDir = _fileSystemService.GetSaveDirectory();
            var backupDir = _fileSystemService.GetBackupDirectory();
            var tempDir = _fileSystemService.GetTempDirectory();

            _fileSystemService.CreateDirectory(saveDir);
            _fileSystemService.CreateDirectory(backupDir);
            _fileSystemService.CreateDirectory(tempDir);
        }

        private async Task RefreshSaveSlotCache()
        {
            lock (_lockObject)
            {
                _saveSlotCache.Clear();
            }

            for (int slotId = 0; slotId < config.SaveConfig.MaxSaveSlots; slotId++)
            {
                var slotInfo = await GetSaveSlotInfoInternal(slotId);
                lock (_lockObject)
                {
                    _saveSlotCache[slotId] = slotInfo;
                }
            }
        }

        #endregion

        #region Core Save/Load Operations

        public async Task<bool> SaveGameDataAsync(int slotId, GameSaveData gameData)
        {
            if (!_isInitialized)
            {
                OnSaveLoadError?.Invoke(new SaveLoadError
                {
                    operation = SaveLoadOperation.Save,
                    slotId = slotId,
                    errorMessage = "SaveDataManager not initialized"
                });
                return false;
            }

            if (slotId < 0 || slotId >= config.SaveConfig.MaxSaveSlots)
            {
                OnSaveLoadError?.Invoke(new SaveLoadError
                {
                    operation = SaveLoadOperation.Save,
                    slotId = slotId,
                    errorMessage = $"Invalid slot ID: {slotId}"
                });
                return false;
            }

            var saveEvent = new SaveEvent
            {
                slotId = slotId,
                saveName = gameData.saveMetadata.saveName,
                saveType = gameData.saveMetadata.saveType,
                timestamp = DateTime.UtcNow
            };

            var startTime = Time.realtimeSinceStartup;

            try
            {
                // Serialize game data
                var jsonData = JsonConvert.SerializeObject(gameData, Formatting.None);
                var rawData = System.Text.Encoding.UTF8.GetBytes(jsonData);

                // Apply compression if enabled
                if (config.SaveConfig.EnableCompression && _compressionService != null)
                {
                    rawData = await _compressionService.CompressAsync(rawData, config.SaveConfig.CompressionLevel);
                }

                // Apply encryption if enabled
                if (config.SaveConfig.EnableEncryption && _encryptionService != null)
                {
                    rawData = await _encryptionService.EncryptAsync(rawData);
                }

                // Write to file
                var filePath = GetSaveFilePath(slotId);
                var success = await _fileSystemService.WriteFileAsync(filePath, rawData);

                if (success)
                {
                    // Update cache
                    var slotInfo = await GetSaveSlotInfoInternal(slotId);
                    lock (_lockObject)
                    {
                        _saveSlotCache[slotId] = slotInfo;
                    }

                    saveEvent.isSuccessful = true;
                    saveEvent.duration = Time.realtimeSinceStartup - startTime;
                    saveEvent.fileSizeBytes = rawData.Length;

                    OnSaveCompleted?.Invoke(saveEvent);
                    Debug.Log($"[SaveDataManager] Save completed for slot {slotId}: {gameData.saveMetadata.saveName}");
                    return true;
                }
                else
                {
                    throw new IOException("Failed to write save file");
                }
            }
            catch (Exception ex)
            {
                saveEvent.isSuccessful = false;
                saveEvent.duration = Time.realtimeSinceStartup - startTime;

                var error = new SaveLoadError
                {
                    operation = SaveLoadOperation.Save,
                    slotId = slotId,
                    errorMessage = ex.Message,
                    exception = ex
                };

                OnSaveLoadError?.Invoke(error);
                Debug.LogError($"[SaveDataManager] Save failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        public async Task<GameSaveData> LoadGameDataAsync(int slotId)
        {
            if (!_isInitialized)
            {
                OnSaveLoadError?.Invoke(new SaveLoadError
                {
                    operation = SaveLoadOperation.Load,
                    slotId = slotId,
                    errorMessage = "SaveDataManager not initialized"
                });
                return null;
            }

            if (slotId < 0 || slotId >= config.SaveConfig.MaxSaveSlots)
            {
                OnSaveLoadError?.Invoke(new SaveLoadError
                {
                    operation = SaveLoadOperation.Load,
                    slotId = slotId,
                    errorMessage = $"Invalid slot ID: {slotId}"
                });
                return null;
            }

            var loadEvent = new LoadEvent
            {
                slotId = slotId,
                timestamp = DateTime.UtcNow
            };

            var startTime = Time.realtimeSinceStartup;

            try
            {
                var filePath = GetSaveFilePath(slotId);

                // Check if file exists
                if (!_fileSystemService.FileExists(filePath))
                {
                    Debug.LogWarning($"[SaveDataManager] No save file found for slot {slotId}");
                    return null;
                }

                // Read file data
                var rawData = await _fileSystemService.ReadFileAsync(filePath);

                // Apply decryption if enabled
                if (config.SaveConfig.EnableEncryption && _encryptionService != null)
                {
                    if (_encryptionService.IsDataEncrypted(rawData))
                    {
                        rawData = await _encryptionService.DecryptAsync(rawData);
                    }
                }

                // Apply decompression if enabled
                if (config.SaveConfig.EnableCompression && _compressionService != null)
                {
                    if (_compressionService.IsDataCompressed(rawData))
                    {
                        rawData = await _compressionService.DecompressAsync(rawData);
                    }
                }

                // Deserialize game data
                var jsonData = System.Text.Encoding.UTF8.GetString(rawData);
                var gameData = JsonConvert.DeserializeObject<GameSaveData>(jsonData);

                if (gameData != null)
                {
                    loadEvent.isSuccessful = true;
                    loadEvent.duration = Time.realtimeSinceStartup - startTime;
                    loadEvent.saveName = gameData.saveMetadata.saveName;

                    OnLoadCompleted?.Invoke(loadEvent);
                    Debug.Log($"[SaveDataManager] Load completed for slot {slotId}: {gameData.saveMetadata.saveName}");
                    return gameData;
                }
                else
                {
                    throw new InvalidDataException("Failed to deserialize save data");
                }
            }
            catch (Exception ex)
            {
                loadEvent.isSuccessful = false;
                loadEvent.duration = Time.realtimeSinceStartup - startTime;

                var error = new SaveLoadError
                {
                    operation = SaveLoadOperation.Load,
                    slotId = slotId,
                    errorMessage = ex.Message,
                    exception = ex
                };

                OnLoadCompleted?.Invoke(loadEvent);
                OnSaveLoadError?.Invoke(error);
                Debug.LogError($"[SaveDataManager] Load failed for slot {slotId}: {ex.Message}");
                return null;
            }
        }

        public async Task<SaveSlotInfo[]> GetSaveSlotInfoAsync()
        {
            if (!_isInitialized)
                return new SaveSlotInfo[0];

            var slotInfos = new SaveSlotInfo[config.SaveConfig.MaxSaveSlots];

            lock (_lockObject)
            {
                for (int i = 0; i < config.SaveConfig.MaxSaveSlots; i++)
                {
                    slotInfos[i] = _saveSlotCache.TryGetValue(i, out var info) ? info : new SaveSlotInfo(i);
                }
            }

            return slotInfos;
        }

        public async Task<bool> DeleteSaveAsync(int slotId)
        {
            if (!_isInitialized)
                return false;

            if (slotId < 0 || slotId >= config.SaveConfig.MaxSaveSlots)
                return false;

            try
            {
                var filePath = GetSaveFilePath(slotId);

                if (_fileSystemService.FileExists(filePath))
                {
                    var success = _fileSystemService.DeleteFile(filePath);

                    if (success)
                    {
                        // Update cache
                        lock (_lockObject)
                        {
                            _saveSlotCache[slotId] = new SaveSlotInfo(slotId);
                        }

                        Debug.Log($"[SaveDataManager] Save slot {slotId} deleted successfully");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                var error = new SaveLoadError
                {
                    operation = SaveLoadOperation.Delete,
                    slotId = slotId,
                    errorMessage = ex.Message,
                    exception = ex
                };

                OnSaveLoadError?.Invoke(error);
                Debug.LogError($"[SaveDataManager] Delete failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Backup Operations

        public async Task<bool> CreateBackupAsync(int slotId)
        {
            if (!_isInitialized)
                return false;

            try
            {
                var sourceFilePath = GetSaveFilePath(slotId);

                if (!_fileSystemService.FileExists(sourceFilePath))
                {
                    Debug.LogWarning($"[SaveDataManager] No save file to backup for slot {slotId}");
                    return false;
                }

                var backupFilePath = GetBackupFilePath(slotId, DateTime.UtcNow);
                var success = _fileSystemService.CopyFile(sourceFilePath, backupFilePath);

                if (success)
                {
                    Debug.Log($"[SaveDataManager] Backup created for slot {slotId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                var error = new SaveLoadError
                {
                    operation = SaveLoadOperation.Backup,
                    slotId = slotId,
                    errorMessage = ex.Message,
                    exception = ex
                };

                OnSaveLoadError?.Invoke(error);
                Debug.LogError($"[SaveDataManager] Backup failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RestoreBackupAsync(int slotId, string backupId)
        {
            if (!_isInitialized)
                return false;

            try
            {
                var backups = await GetBackupsAsync(slotId);
                var targetBackup = Array.Find(backups, b => b.backupId == backupId);

                if (targetBackup == null)
                {
                    Debug.LogWarning($"[SaveDataManager] Backup {backupId} not found for slot {slotId}");
                    return false;
                }

                var saveFilePath = GetSaveFilePath(slotId);
                var success = _fileSystemService.CopyFile(targetBackup.backupPath, saveFilePath);

                if (success)
                {
                    // Refresh cache
                    var slotInfo = await GetSaveSlotInfoInternal(slotId);
                    lock (_lockObject)
                    {
                        _saveSlotCache[slotId] = slotInfo;
                    }

                    Debug.Log($"[SaveDataManager] Backup {backupId} restored for slot {slotId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                var error = new SaveLoadError
                {
                    operation = SaveLoadOperation.Restore,
                    slotId = slotId,
                    errorMessage = ex.Message,
                    exception = ex
                };

                OnSaveLoadError?.Invoke(error);
                Debug.LogError($"[SaveDataManager] Restore failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        public async Task<BackupInfo[]> GetBackupsAsync(int slotId)
        {
            if (!_isInitialized)
                return new BackupInfo[0];

            try
            {
                var backupDir = _fileSystemService.GetBackupDirectory();
                var pattern = $"save_{slotId}_backup_*.{config.SaveConfig.SaveFileExtension.TrimStart('.')}";
                var backupFiles = await _fileSystemService.GetFilesInDirectoryAsync(backupDir, pattern);

                var backupInfos = new List<BackupInfo>();

                foreach (var filePath in backupFiles)
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var parts = fileName.Split('_');

                    if (parts.Length >= 4)
                    {
                        var backupInfo = new BackupInfo
                        {
                            backupId = parts[3],
                            originalSlotId = slotId,
                            backupPath = filePath,
                            backupCreated = await _fileSystemService.GetFileModificationTimeAsync(filePath),
                            fileSizeBytes = await _fileSystemService.GetFileSizeAsync(filePath),
                            reason = BackupReason.Manual
                        };

                        backupInfos.Add(backupInfo);
                    }
                }

                return backupInfos.ToArray();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataManager] Failed to get backups for slot {slotId}: {ex.Message}");
                return new BackupInfo[0];
            }
        }

        #endregion

        #region Helper Methods

        private async Task<SaveSlotInfo> GetSaveSlotInfoInternal(int slotId)
        {
            var slotInfo = new SaveSlotInfo(slotId);

            try
            {
                var filePath = GetSaveFilePath(slotId);

                if (_fileSystemService.FileExists(filePath))
                {
                    slotInfo.isOccupied = true;
                    slotInfo.fileSizeBytes = await _fileSystemService.GetFileSizeAsync(filePath);
                    slotInfo.lastSaved = await _fileSystemService.GetFileModificationTimeAsync(filePath);

                    // Try to read metadata without loading full save
                    try
                    {
                        var gameData = await LoadGameDataAsync(slotId);
                        if (gameData != null)
                        {
                            slotInfo.saveName = gameData.saveMetadata.saveName;
                            slotInfo.gameVersion = gameData.saveMetadata.gameVersion;
                            slotInfo.saveType = gameData.saveMetadata.saveType;
                            slotInfo.playTime = gameData.saveMetadata.playTime;
                        }
                    }
                    catch
                    {
                        slotInfo.isCorrupted = true;
                        slotInfo.saveName = "Corrupted Save";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveDataManager] Failed to get slot info for {slotId}: {ex.Message}");
                slotInfo.isCorrupted = true;
            }

            return slotInfo;
        }

        private string GetSaveFilePath(int slotId)
        {
            var saveDir = _fileSystemService.GetSaveDirectory();
            var fileName = $"save_{slotId}{config.SaveConfig.SaveFileExtension}";
            return Path.Combine(saveDir, fileName);
        }

        private string GetBackupFilePath(int slotId, DateTime timestamp)
        {
            var backupDir = _fileSystemService.GetBackupDirectory();
            var backupId = timestamp.ToString("yyyyMMdd_HHmmss");
            var fileName = $"save_{slotId}_backup_{backupId}{config.SaveConfig.SaveFileExtension}";
            return Path.Combine(backupDir, fileName);
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Refresh Save Slot Cache")]
        private void RefreshCacheDebug()
        {
            if (_isInitialized)
            {
                _ = RefreshSaveSlotCache();
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic file system service implementation for Unity.
    /// </summary>
    public class UnityFileSystemService : IFileSystemService
    {
        private readonly string _saveDirectory;
        private readonly string _backupDirectory;
        private readonly string _tempDirectory;

        public UnityFileSystemService()
        {
            _saveDirectory = Path.Combine(Application.persistentDataPath, "ChimeraSaves");
            _backupDirectory = Path.Combine(Application.persistentDataPath, "ChimeraBackups");
            _tempDirectory = Path.Combine(Application.persistentDataPath, "ChimeraTemp");
        }

        public async Task<bool> WriteFileAsync(string filePath, byte[] data)
        {
            try
            {
                await File.WriteAllBytesAsync(filePath, data);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<byte[]> ReadFileAsync(string filePath)
        {
            return await File.ReadAllBytesAsync(filePath);
        }

        public bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public async Task<long> GetFileSizeAsync(string filePath)
        {
            return new FileInfo(filePath).Length;
        }

        public async Task<DateTime> GetFileModificationTimeAsync(string filePath)
        {
            return File.GetLastWriteTime(filePath);
        }

        public bool CreateDirectory(string directoryPath)
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string[]> GetFilesInDirectoryAsync(string directoryPath, string pattern = "*")
        {
            try
            {
                return Directory.GetFiles(directoryPath, pattern);
            }
            catch
            {
                return new string[0];
            }
        }

        public bool CopyFile(string sourcePath, string destinationPath)
        {
            try
            {
                File.Copy(sourcePath, destinationPath, true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool MoveFile(string sourcePath, string destinationPath)
        {
            try
            {
                File.Move(sourcePath, destinationPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public string GetSaveDirectory() => _saveDirectory;
        public string GetBackupDirectory() => _backupDirectory;
        public string GetTempDirectory() => _tempDirectory;
    }

    /// <summary>
    /// GZip compression service implementation.
    /// </summary>
    public class GZipCompressionService : ICompressionService
    {
        public async Task<byte[]> CompressAsync(byte[] data, CompressionLevel level = CompressionLevel.Medium)
        {
            using (var memory = new MemoryStream())
            {
                using (var gzip = new System.IO.Compression.GZipStream(memory, GetCompressionLevel(level)))
                {
                    await gzip.WriteAsync(data, 0, data.Length);
                }
                return memory.ToArray();
            }
        }

        public async Task<byte[]> DecompressAsync(byte[] compressedData)
        {
            using (var memory = new MemoryStream(compressedData))
            using (var gzip = new System.IO.Compression.GZipStream(memory, System.IO.Compression.CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                await gzip.CopyToAsync(output);
                return output.ToArray();
            }
        }

        public async Task<CompressionInfo> GetCompressionInfoAsync(byte[] data)
        {
            var compressed = await CompressAsync(data);
            return new CompressionInfo
            {
                originalSize = data.Length,
                compressedSize = compressed.Length,
                compressionRatio = (float)compressed.Length / data.Length,
                algorithm = "GZip"
            };
        }

        public bool IsDataCompressed(byte[] data)
        {
            return data.Length >= 2 && data[0] == 0x1f && data[1] == 0x8b;
        }

        private System.IO.Compression.CompressionLevel GetCompressionLevel(CompressionLevel level)
        {
            return level switch
            {
                CompressionLevel.None => System.IO.Compression.CompressionLevel.NoCompression,
                CompressionLevel.Low => System.IO.Compression.CompressionLevel.Fastest,
                CompressionLevel.Medium => System.IO.Compression.CompressionLevel.Optimal,
                CompressionLevel.High => System.IO.Compression.CompressionLevel.Optimal,
                _ => System.IO.Compression.CompressionLevel.Optimal
            };
        }
    }

    /// <summary>
    /// AES encryption service implementation.
    /// </summary>
    public class AESEncryptionService : IEncryptionService
    {
        private const string DefaultKey = "ChimeraGameSaveKey2024";

        public async Task<byte[]> EncryptAsync(byte[] data, string key = null)
        {
            key ??= DefaultKey;
            // Simplified AES implementation - in production, use proper cryptographic libraries
            return data; // Placeholder
        }

        public async Task<byte[]> DecryptAsync(byte[] encryptedData, string key = null)
        {
            key ??= DefaultKey;
            // Simplified AES implementation - in production, use proper cryptographic libraries
            return encryptedData; // Placeholder
        }

        public async Task<string> GenerateKeyAsync()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task<bool> ValidateKeyAsync(string key)
        {
            return !string.IsNullOrEmpty(key);
        }

        public bool IsDataEncrypted(byte[] data)
        {
            // Simplified check - in production, use proper header detection
            return false;
        }
    }
}