using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Core save data service interface for managing game state persistence.
    /// </summary>
    public interface ISaveDataService
    {
        Task<bool> SaveGameDataAsync(int slotId, GameSaveData gameData);
        Task<GameSaveData> LoadGameDataAsync(int slotId);
        Task<SaveSlotInfo[]> GetSaveSlotInfoAsync();
        Task<bool> DeleteSaveAsync(int slotId);
        Task<bool> CreateBackupAsync(int slotId);
        Task<bool> RestoreBackupAsync(int slotId, string backupId);
        Task<BackupInfo[]> GetBackupsAsync(int slotId);

        event Action<SaveEvent> OnSaveCompleted;
        event Action<LoadEvent> OnLoadCompleted;
        event Action<SaveLoadError> OnSaveLoadError;
    }

    /// <summary>
    /// Automatic save service interface for timed and event-driven saves.
    /// </summary>
    public interface IAutoSaveService
    {
        bool IsAutoSaveEnabled { get; set; }
        float AutoSaveInterval { get; set; }

        void StartAutoSave();
        void StopAutoSave();
        void PauseAutoSave();
        void ResumeAutoSave();
        Task<bool> TriggerAutoSaveAsync(string trigger = "Manual");
        void RegisterAutoSaveTrigger(string triggerName, Func<bool> condition);
        void UnregisterAutoSaveTrigger(string triggerName);

        event Action<string> OnAutoSaveTriggered;
        event Action<SaveEvent> OnAutoSaveCompleted;
    }

    /// <summary>
    /// Cloud synchronization service interface for cross-device save persistence.
    /// </summary>
    public interface ICloudSyncService
    {
        bool IsCloudSyncEnabled { get; set; }
        CloudSyncStatus SyncStatus { get; }

        Task<bool> UploadSaveAsync(int slotId, GameSaveData gameData);
        Task<GameSaveData> DownloadSaveAsync(int slotId);
        Task<bool> DeleteCloudSaveAsync(int slotId);
        Task<bool> SyncAllSavesAsync();
        Task<CloudSaveInfo[]> GetCloudSaveInfoAsync();
        Task<bool> ResolveConflictAsync(int slotId, ConflictResolution resolution);

        event Action<CloudSyncEvent> OnCloudSyncEvent;
        event Action<ConflictDetectedEvent> OnConflictDetected;
    }

    /// <summary>
    /// Data integrity service interface for validation and repair of save data.
    /// </summary>
    public interface IDataIntegrityService
    {
        Task<ValidationResult> ValidateGameDataAsync(GameSaveData gameData);
        Task<GameSaveData> RepairGameDataAsync(GameSaveData gameData);
        Task<bool> CreateChecksumAsync(int slotId);
        Task<bool> VerifyChecksumAsync(int slotId);
        Task<CorruptionReport> ScanForCorruptionAsync();
        Task<bool> QuarantineCorruptedSaveAsync(int slotId);

        event Action<ValidationResult> OnValidationCompleted;
        event Action<RepairResult> OnRepairCompleted;
        event Action<CorruptionDetectedEvent> OnCorruptionDetected;
    }

    /// <summary>
    /// File system operations interface for platform-specific save file management.
    /// </summary>
    public interface IFileSystemService
    {
        Task<bool> WriteFileAsync(string filePath, byte[] data);
        Task<byte[]> ReadFileAsync(string filePath);
        bool DeleteFile(string filePath);
        bool FileExists(string filePath);
        Task<long> GetFileSizeAsync(string filePath);
        Task<DateTime> GetFileModificationTimeAsync(string filePath);
        bool CreateDirectory(string directoryPath);
        Task<string[]> GetFilesInDirectoryAsync(string directoryPath, string pattern = "*");
        bool CopyFile(string sourcePath, string destinationPath);
        bool MoveFile(string sourcePath, string destinationPath);

        string GetSaveDirectory();
        string GetBackupDirectory();
        string GetTempDirectory();
    }

    /// <summary>
    /// Compression service interface for reducing save file sizes.
    /// </summary>
    public interface ICompressionService
    {
        Task<byte[]> CompressAsync(byte[] data, CompressionLevel level = CompressionLevel.Medium);
        Task<byte[]> DecompressAsync(byte[] compressedData);
        Task<CompressionInfo> GetCompressionInfoAsync(byte[] data);
        bool IsDataCompressed(byte[] data);
    }

    /// <summary>
    /// Encryption service interface for securing save data.
    /// </summary>
    public interface IEncryptionService
    {
        Task<byte[]> EncryptAsync(byte[] data, string key = null);
        Task<byte[]> DecryptAsync(byte[] encryptedData, string key = null);
        Task<string> GenerateKeyAsync();
        Task<bool> ValidateKeyAsync(string key);
        bool IsDataEncrypted(byte[] data);
    }

    // Additional data structures for service interfaces

    [Serializable]
    public class CloudSaveInfo
    {
        public int slotId;
        public string saveName;
        public DateTime lastModified;
        public long fileSizeBytes;
        public string cloudProvider;
        public CloudSyncStatus syncStatus;
        public bool hasConflict;
    }

    [Serializable]
    public class ConflictDetectedEvent
    {
        public int slotId;
        public GameSaveData localSave;
        public GameSaveData cloudSave;
        public DateTime localTimestamp;
        public DateTime cloudTimestamp;
        public ConflictReason reason;
    }

    [Serializable]
    public class RepairResult
    {
        public bool wasSuccessful;
        public List<string> repairActions;
        public GameSaveData repairedData;
        public List<string> unrepairableIssues;
        public DateTime repairedAt;
    }

    [Serializable]
    public class CorruptionDetectedEvent
    {
        public int slotId;
        public CorruptionType corruptionType;
        public string errorMessage;
        public DateTime detectedAt;
        public bool isRecoverable;
    }

    [Serializable]
    public class CorruptionReport
    {
        public List<CorruptedSaveInfo> corruptedSaves;
        public List<string> systemIssues;
        public DateTime scanTime;
        public int totalSavesScanned;
        public CorruptionSeverity overallSeverity;
    }

    [Serializable]
    public class CorruptedSaveInfo
    {
        public int slotId;
        public string saveName;
        public CorruptionType corruptionType;
        public bool isRecoverable;
        public List<string> issues;
    }

    [Serializable]
    public class CompressionInfo
    {
        public long originalSize;
        public long compressedSize;
        public float compressionRatio;
        public CompressionLevel level;
        public string algorithm;
    }

    // Additional enums

    public enum ConflictReason
    {
        TimestampMismatch,
        ContentDifference,
        VersionMismatch,
        ChecksumMismatch,
        Unknown
    }

    public enum CorruptionType
    {
        ChecksumMismatch,
        InvalidFormat,
        MissingData,
        UnreadableFile,
        VersionIncompatible,
        Unknown
    }

    public enum CorruptionSeverity
    {
        None,
        Minor,
        Moderate,
        Severe,
        Critical
    }
}