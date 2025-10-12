using System;
using UnityEngine;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Configuration for the Save/Load Subsystem.
    /// Defines save slot limits, backup settings, cloud sync options, and autosave behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveLoadSubsystemConfig", menuName = "Chimera/Subsystems/SaveLoad/Config")]
    public class SaveLoadSubsystemConfig : ScriptableObject
    {
        [Header("Save Configuration")]
        public SaveConfig SaveConfig = new SaveConfig();

        [Header("Autosave Configuration")]
        public AutoSaveConfig AutoSaveConfig = new AutoSaveConfig();

        [Header("Cloud Sync Configuration")]
        public CloudSyncConfig CloudSyncConfig = new CloudSyncConfig();

        [Header("Data Integrity Configuration")]
        public DataIntegrityConfig DataIntegrityConfig = new DataIntegrityConfig();

        [Header("Backup Configuration")]
        public BackupConfig BackupConfig = new BackupConfig();

        [Header("Performance Configuration")]
        public PerformanceConfig PerformanceConfig = new PerformanceConfig();

        private void OnValidate()
        {
            SaveConfig.Validate();
            AutoSaveConfig.Validate();
            CloudSyncConfig.Validate();
            DataIntegrityConfig.Validate();
            BackupConfig.Validate();
            PerformanceConfig.Validate();
        }
    }

    [Serializable]
    public class SaveConfig
    {
        [Header("Save Slots")]
        [SerializeField] [Range(1, 20)] private int maxSaveSlots = 10;
        [SerializeField] [Range(0, 19)] private int autoSaveSlot = 0;

        [Header("Save Settings")]
        [SerializeField] private bool enableCompression = true;
        [SerializeField] private bool enableEncryption = false;
        [SerializeField] private CompressionLevel compressionLevel = CompressionLevel.Medium;

        [Header("File Settings")]
        [SerializeField] private string saveFileExtension = ".chimera";
        [SerializeField] private string saveDirectory = "ChimeraSaves";

        public int MaxSaveSlots => maxSaveSlots;
        public int AutoSaveSlot => autoSaveSlot;
        public bool EnableCompression => enableCompression;
        public bool EnableEncryption => enableEncryption;
        public CompressionLevel CompressionLevel => compressionLevel;
        public string SaveFileExtension => saveFileExtension;
        public string SaveDirectory => saveDirectory;

        public void Validate()
        {
            maxSaveSlots = Mathf.Clamp(maxSaveSlots, 1, 20);
            autoSaveSlot = Mathf.Clamp(autoSaveSlot, 0, maxSaveSlots - 1);
        }
    }

    [Serializable]
    public class AutoSaveConfig
    {
        [Header("Autosave Settings")]
        [SerializeField] private bool enableAutosave = true;
        [SerializeField] [Range(30f, 1800f)] private float autosaveInterval = 300f; // 5 minutes
        [SerializeField] private bool autosaveOnPause = true;
        [SerializeField] private bool autosaveOnFocusLost = true;
        [SerializeField] private bool autosaveOnSceneChange = true;

        [Header("Autosave Triggers")]
        [SerializeField] private bool autosaveOnCreatureBirth = true;
        [SerializeField] private bool autosaveOnBreedingSuccess = true;
        [SerializeField] private bool autosaveOnDiscovery = true;
        [SerializeField] private bool autosaveOnEcosystemChange = false;

        [Header("Autosave Limits")]
        [SerializeField] [Range(1, 10)] private int maxAutosaveFiles = 5;
        [SerializeField] private bool keepManualSaves = true;

        public bool EnableAutosave => enableAutosave;
        public float AutosaveInterval => autosaveInterval;
        public bool AutosaveOnPause => autosaveOnPause;
        public bool AutosaveOnFocusLost => autosaveOnFocusLost;
        public bool AutosaveOnSceneChange => autosaveOnSceneChange;
        public bool AutosaveOnCreatureBirth => autosaveOnCreatureBirth;
        public bool AutosaveOnBreedingSuccess => autosaveOnBreedingSuccess;
        public bool AutosaveOnDiscovery => autosaveOnDiscovery;
        public bool AutosaveOnEcosystemChange => autosaveOnEcosystemChange;
        public int MaxAutosaveFiles => maxAutosaveFiles;
        public bool KeepManualSaves => keepManualSaves;

        public void Validate()
        {
            autosaveInterval = Mathf.Clamp(autosaveInterval, 30f, 1800f);
            maxAutosaveFiles = Mathf.Clamp(maxAutosaveFiles, 1, 10);
        }
    }

    [Serializable]
    public class CloudSyncConfig
    {
        [Header("Cloud Sync Settings")]
        [SerializeField] private bool enableCloudSync = false;
        [SerializeField] private CloudProvider cloudProvider = CloudProvider.Unity;
        [SerializeField] private bool syncOnSave = true;
        [SerializeField] private bool syncOnLoad = true;
        [SerializeField] private bool autoSyncOnStartup = true;

        [Header("Sync Behavior")]
        [SerializeField] private ConflictResolution conflictResolution = ConflictResolution.MostRecent;
        [SerializeField] [Range(5f, 300f)] private float syncTimeout = 30f;
        [SerializeField] [Range(1, 5)] private int maxRetryAttempts = 3;

        [Header("Bandwidth Settings")]
        [SerializeField] private bool enableBandwidthLimit = false;
        [SerializeField] [Range(1f, 100f)] private float maxUploadSpeedMBps = 10f;
        [SerializeField] [Range(1f, 100f)] private float maxDownloadSpeedMBps = 10f;

        public bool EnableCloudSync => enableCloudSync;
        public CloudProvider CloudProvider => cloudProvider;
        public bool SyncOnSave => syncOnSave;
        public bool SyncOnLoad => syncOnLoad;
        public bool AutoSyncOnStartup => autoSyncOnStartup;
        public ConflictResolution ConflictResolution => conflictResolution;
        public float SyncTimeout => syncTimeout;
        public int MaxRetryAttempts => maxRetryAttempts;
        public bool EnableBandwidthLimit => enableBandwidthLimit;
        public float MaxUploadSpeedMBps => maxUploadSpeedMBps;
        public float MaxDownloadSpeedMBps => maxDownloadSpeedMBps;

        public void Validate()
        {
            syncTimeout = Mathf.Clamp(syncTimeout, 5f, 300f);
            maxRetryAttempts = Mathf.Clamp(maxRetryAttempts, 1, 5);
            maxUploadSpeedMBps = Mathf.Clamp(maxUploadSpeedMBps, 1f, 100f);
            maxDownloadSpeedMBps = Mathf.Clamp(maxDownloadSpeedMBps, 1f, 100f);
        }
    }

    [Serializable]
    public class DataIntegrityConfig
    {
        [Header("Validation Settings")]
        [SerializeField] private bool enableDataValidation = true;
        [SerializeField] private bool enableChecksums = true;
        [SerializeField] private bool enableSchemaValidation = true;
        [SerializeField] private ValidationLevel validationLevel = ValidationLevel.Standard;

        [Header("Repair Settings")]
        [SerializeField] private bool enableAutoRepair = true;
        [SerializeField] private bool backupBeforeRepair = true;
        [SerializeField] private RepairStrategy repairStrategy = RepairStrategy.Conservative;

        [Header("Error Handling")]
        [SerializeField] private bool enableCorruptionRecovery = true;
        [SerializeField] private bool reportCorruption = true;
        [SerializeField] private CorruptionAction corruptionAction = CorruptionAction.AttemptRepair;

        public bool EnableDataValidation => enableDataValidation;
        public bool EnableChecksums => enableChecksums;
        public bool EnableSchemaValidation => enableSchemaValidation;
        public ValidationLevel ValidationLevel => validationLevel;
        public bool EnableAutoRepair => enableAutoRepair;
        public bool BackupBeforeRepair => backupBeforeRepair;
        public RepairStrategy RepairStrategy => repairStrategy;
        public bool EnableCorruptionRecovery => enableCorruptionRecovery;
        public bool ReportCorruption => reportCorruption;
        public CorruptionAction CorruptionAction => corruptionAction;

        public void Validate()
        {
            // No clamping needed for enums and bools
        }
    }

    [Serializable]
    public class BackupConfig
    {
        [Header("Backup Settings")]
        [SerializeField] private bool enableBackups = true;
        [SerializeField] private bool backupOnSave = true;
        [SerializeField] private bool backupBeforeLoad = false;
        [SerializeField] [Range(1, 20)] private int maxBackups = 5;

        [Header("Backup Rotation")]
        [SerializeField] private BackupRotation backupRotation = BackupRotation.RollingWindow;
        [SerializeField] [Range(1, 30)] private int backupRetentionDays = 7;

        [Header("Backup Compression")]
        [SerializeField] private bool compressBackups = true;
        [SerializeField] private CompressionLevel backupCompressionLevel = CompressionLevel.High;

        public bool EnableBackups => enableBackups;
        public bool BackupOnSave => backupOnSave;
        public bool BackupBeforeLoad => backupBeforeLoad;
        public int MaxBackups => maxBackups;
        public BackupRotation BackupRotation => backupRotation;
        public int BackupRetentionDays => backupRetentionDays;
        public bool CompressBackups => compressBackups;
        public CompressionLevel BackupCompressionLevel => backupCompressionLevel;

        public void Validate()
        {
            maxBackups = Mathf.Clamp(maxBackups, 1, 20);
            backupRetentionDays = Mathf.Clamp(backupRetentionDays, 1, 30);
        }
    }

    [Serializable]
    public class PerformanceConfig
    {
        [Header("Async Settings")]
        [SerializeField] private bool useAsyncIO = true;
        [SerializeField] [Range(1, 10)] private int maxConcurrentOperations = 3;
        [SerializeField] [Range(1024, 1048576)] private int bufferSize = 65536; // 64KB

        [Header("Caching")]
        [SerializeField] private bool enableSaveCache = true;
        [SerializeField] [Range(1, 100)] private int maxCachedSaves = 10;
        [SerializeField] [Range(60f, 3600f)] private float cacheExpirationTime = 600f; // 10 minutes

        [Header("Memory Management")]
        [SerializeField] private bool enableMemoryPooling = true;
        [SerializeField] private bool enableGCOptimization = true;
        [SerializeField] [Range(1, 100)] private int largeObjectThresholdMB = 10;

        public bool UseAsyncIO => useAsyncIO;
        public int MaxConcurrentOperations => maxConcurrentOperations;
        public int BufferSize => bufferSize;
        public bool EnableSaveCache => enableSaveCache;
        public int MaxCachedSaves => maxCachedSaves;
        public float CacheExpirationTime => cacheExpirationTime;
        public bool EnableMemoryPooling => enableMemoryPooling;
        public bool EnableGCOptimization => enableGCOptimization;
        public int LargeObjectThresholdMB => largeObjectThresholdMB;

        public void Validate()
        {
            maxConcurrentOperations = Mathf.Clamp(maxConcurrentOperations, 1, 10);
            bufferSize = Mathf.Clamp(bufferSize, 1024, 1048576);
            maxCachedSaves = Mathf.Clamp(maxCachedSaves, 1, 100);
            cacheExpirationTime = Mathf.Clamp(cacheExpirationTime, 60f, 3600f);
            largeObjectThresholdMB = Mathf.Clamp(largeObjectThresholdMB, 1, 100);
        }
    }

    public enum CompressionLevel
    {
        None,
        Low,
        Medium,
        High
    }

    public enum CloudProvider
    {
        Unity,
        Steam,
        GooglePlay,
        Custom
    }

    public enum ConflictResolution
    {
        MostRecent,
        PromptUser,
        KeepBoth,
        KeepLocal,
        KeepRemote
    }

    public enum ValidationLevel
    {
        None,
        Basic,
        Standard,
        Strict
    }

    public enum RepairStrategy
    {
        Conservative,
        Aggressive,
        UserPrompt
    }

    public enum CorruptionAction
    {
        IgnoreAndContinue,
        AttemptRepair,
        PromptUser,
        RejectSave
    }

    public enum BackupRotation
    {
        RollingWindow,
        TimeBasedCleanup,
        Manual
    }
}