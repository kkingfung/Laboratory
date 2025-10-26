using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Subsystems.Ecosystem;
using Laboratory.Shared.Types;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Comprehensive data structure for saved game state.
    /// Contains all persistent data including genetics, ecosystem, and player progress.
    /// </summary>
    [Serializable]
    public class GameSaveData
    {
        public SaveMetadata saveMetadata;
        public Dictionary<string, object> geneticsData;
        public EcosystemSaveData ecosystemData;
        public PlayerSaveData playerData;

        public GameSaveData()
        {
            saveMetadata = new SaveMetadata();
            geneticsData = new Dictionary<string, object>();
            ecosystemData = new EcosystemSaveData();
            playerData = new PlayerSaveData();
        }
    }

    /// <summary>
    /// Metadata about a save file including creation time, version, and player info.
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public string saveId;
        public string saveName;
        public DateTime created;
        public DateTime lastSaved;
        public string gameVersion;
        public float playTime;
        public SaveType saveType;
        public string playerName;
        public int gameLevel;
        public Dictionary<string, object> customMetadata;

        public SaveMetadata()
        {
            saveId = Guid.NewGuid().ToString();
            created = DateTime.UtcNow;
            lastSaved = DateTime.UtcNow;
            gameVersion = Application.version;
            saveType = SaveType.Manual;
            customMetadata = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Ecosystem data for persistence including biomes, populations, and environmental state.
    /// </summary>
    [Serializable]
    public class EcosystemSaveData
    {
        public List<BiomeData> biomes;
        public List<PopulationData> populations;
        public WeatherData currentWeather;
        public ConservationStatus conservationStatus;
        public DateTime lastUpdate;
        public Dictionary<string, object> environmentalFactors;

        public EcosystemSaveData()
        {
            biomes = new List<BiomeData>();
            populations = new List<PopulationData>();
            environmentalFactors = new Dictionary<string, object>();
            lastUpdate = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Player progress and settings data.
    /// </summary>
    [Serializable]
    public class PlayerSaveData
    {
        public string playerId;
        public string playerName;
        public int level;
        public int experience;
        public int currency;
        public List<string> unlockedAchievements;
        public Dictionary<string, object> gameSettings;
        public DateTime lastPlayed;
        public Vector3 lastPosition;
        public string currentScene;
        public Dictionary<string, int> statistics;

        public PlayerSaveData()
        {
            playerId = SystemInfo.deviceUniqueIdentifier;
            playerName = "Player";
            unlockedAchievements = new List<string>();
            gameSettings = new Dictionary<string, object>();
            statistics = new Dictionary<string, int>();
            lastPlayed = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Information about a save slot.
    /// </summary>
    [Serializable]
    public class SaveSlotInfo
    {
        public int slotId;
        public bool isOccupied;
        public string saveName;
        public DateTime lastSaved;
        public float playTime;
        public string gameVersion;
        public SaveType saveType;
        public long fileSizeBytes;
        public bool isCorrupted;
        public string thumbnailPath;

        public SaveSlotInfo()
        {
            isOccupied = false;
            isCorrupted = false;
        }

        public SaveSlotInfo(int slot) : this()
        {
            slotId = slot;
        }
    }

    /// <summary>
    /// Event data for save operations.
    /// </summary>
    [Serializable]
    public class SaveEvent
    {
        public int slotId;
        public string saveName;
        public SaveType saveType;
        public string trigger;
        public DateTime timestamp;
        public bool isSuccessful;
        public float duration;
        public long fileSizeBytes;

        public SaveEvent()
        {
            timestamp = DateTime.UtcNow;
            saveType = SaveType.Manual;
        }
    }

    /// <summary>
    /// Event data for load operations.
    /// </summary>
    [Serializable]
    public class LoadEvent
    {
        public int slotId;
        public string saveName;
        public DateTime timestamp;
        public bool isSuccessful;
        public float duration;
        public bool wasRepaired;
        public List<string> repairActions;

        public LoadEvent()
        {
            timestamp = DateTime.UtcNow;
            repairActions = new List<string>();
        }
    }

    /// <summary>
    /// Error information for save/load operations.
    /// </summary>
    [Serializable]
    public class SaveLoadError
    {
        public SaveLoadOperation operation;
        public int slotId;
        public string errorMessage;
        public Exception exception;
        public DateTime timestamp;
        public ErrorSeverity severity;
        public bool isRecoverable;

        public SaveLoadError()
        {
            timestamp = DateTime.UtcNow;
            severity = ErrorSeverity.Error;
        }
    }

    /// <summary>
    /// Cloud synchronization event data.
    /// </summary>
    [Serializable]
    public class CloudSyncEvent
    {
        public CloudSyncOperation operation;
        public CloudSyncStatus status;
        public int slotId;
        public string message;
        public DateTime timestamp;
        public float progress;
        public long bytesTransferred;
        public long totalBytes;

        public CloudSyncEvent()
        {
            timestamp = DateTime.UtcNow;
            status = CloudSyncStatus.Idle;
        }
    }

    /// <summary>
    /// Data validation result with error details.
    /// </summary>
    [Serializable]
    public class ValidationResult
    {
        public bool isValid;
        public List<string> errors;
        public List<string> warnings;
        public DateTime validatedAt;
        public ValidationLevel level;

        public ValidationResult()
        {
            isValid = true;
            errors = new List<string>();
            warnings = new List<string>();
            validatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Backup information for save files.
    /// </summary>
    [Serializable]
    public class BackupInfo
    {
        public string backupId;
        public int originalSlotId;
        public string originalSaveName;
        public DateTime backupCreated;
        public long fileSizeBytes;
        public string backupPath;
        public BackupReason reason;

        public BackupInfo()
        {
            backupId = Guid.NewGuid().ToString();
            backupCreated = DateTime.UtcNow;
        }
    }

    // Enums for type safety and clarity

    public enum SaveType
    {
        Manual,
        Auto,
        Quick,
        Checkpoint,
        Emergency
    }

    public enum SaveLoadOperation
    {
        Save,
        Load,
        Delete,
        Backup,
        Restore,
        Validate,
        Repair
    }

    public enum SaveLoadStatus
    {
        Idle,
        Saving,
        Loading,
        Validating,
        Repairing,
        BackingUp,
        Syncing,
        Error
    }

    public enum CloudSyncOperation
    {
        Upload,
        Download,
        Delete,
        Sync,
        ConflictResolution
    }

    public enum CloudSyncStatus
    {
        Idle,
        Connecting,
        Uploading,
        Downloading,
        Syncing,
        ConflictDetected,
        Success,
        Failed,
        Cancelled
    }

    public enum ErrorSeverity
    {
        Warning,
        Error,
        Critical,
        Fatal
    }

    public enum BackupReason
    {
        BeforeSave,
        BeforeLoad,
        BeforeRepair,
        Manual,
        Automatic,
        Emergency
    }

    // Additional data structures for ecosystem integration

    [Serializable]
    public class BiomeData
    {
        public string biomeId;
        public string biomeName;
        public BiomeType biomeType;
        public Vector3 position;
        public float radius;
        public Dictionary<string, float> environmentalFactors;
        public List<string> habitatCreatures;
        public BiomeHealth health;

        public BiomeData()
        {
            environmentalFactors = new Dictionary<string, float>();
            habitatCreatures = new List<string>();
        }
    }

    [Serializable]
    public class PopulationData
    {
        public string speciesId;
        public string biomeId;
        public int population;
        public int maxPopulation;
        public float growthRate;
        public PopulationStatus status;
        public List<string> threats;
        public Dictionary<string, object> geneticDiversity;

        public PopulationData()
        {
            threats = new List<string>();
            geneticDiversity = new Dictionary<string, object>();
        }
    }

    [Serializable]
    public class WeatherData
    {
        public WeatherType weatherType;
        public float intensity;
        public float remainingDuration;
        public Vector3 windDirection;
        public float temperature;
        public float humidity;
        public DateTime startTime;

        public WeatherData()
        {
            weatherType = WeatherType.Clear;
            intensity = 0.5f;
            startTime = DateTime.UtcNow;
        }
    }

    // Weather and environment enums

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Rain,
        Storm,
        Snow,
        Fog,
        Extreme
    }


    public enum BiomeHealth
    {
        Thriving,
        Healthy,
        Stable,
        Declining,
        Critical,
        Extinct
    }

    public enum PopulationStatus
    {
        Growing,
        Stable,
        Declining,
        Endangered,
        Critical,
        Extinct
    }

    public enum ConservationStatus
    {
        LeastConcern,
        NearThreatened,
        Vulnerable,
        Endangered,
        CriticallyEndangered,
        ExtinctInWild,
        Extinct
    }
}