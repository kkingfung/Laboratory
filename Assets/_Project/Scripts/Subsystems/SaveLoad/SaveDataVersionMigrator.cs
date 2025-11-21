using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Handles save data versioning and migration across game updates
    /// Automatically migrates old save formats to current version
    /// </summary>
    public class SaveDataVersionMigrator
    {
        private readonly List<ISaveMigration> _migrations = new List<ISaveMigration>();
        private readonly Dictionary<string, int> _versionRegistry = new Dictionary<string, int>();

        private const int CurrentSaveVersion = 1;

        public SaveDataVersionMigrator()
        {
            RegisterDefaultMigrations();
        }

        #region Version Migration

        public async Task<(GameSaveData migratedData, bool wasMigrated)> MigrateToCurrentVersionAsync(GameSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogError("[SaveDataVersionMigrator] Cannot migrate null save data");
                return (null, false);
            }

            bool wasMigrated = false;
            var currentData = saveData;

            try
            {
                // Determine save version
                int saveVersion = DetermineSaveVersion(saveData);
                int targetVersion = CurrentSaveVersion;

                Debug.Log($"[SaveDataVersionMigrator] Migrating save from version {saveVersion} to {targetVersion}");

                if (saveVersion < targetVersion)
                {
                    // Apply migrations in sequence
                    var applicableMigrations = _migrations
                        .Where(m => m.FromVersion >= saveVersion && m.ToVersion <= targetVersion)
                        .OrderBy(m => m.FromVersion)
                        .ToList();

                    foreach (var migration in applicableMigrations)
                    {
                        Debug.Log($"[SaveDataVersionMigrator] Applying migration: {migration.GetType().Name} (v{migration.FromVersion} → v{migration.ToVersion})");

                        try
                        {
                            currentData = await migration.MigrateAsync(currentData);
                            wasMigrated = true;
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[SaveDataVersionMigrator] Migration '{migration.GetType().Name}' failed: {ex.Message}");
                            throw new SaveMigrationException($"Migration failed at version {migration.FromVersion}", ex);
                        }
                    }

                    // Update save version
                    if (currentData.saveMetadata.customMetadata == null)
                    {
                        currentData.saveMetadata.customMetadata = new Dictionary<string, object>();
                    }

                    currentData.saveMetadata.customMetadata["SaveFormatVersion"] = targetVersion;

                    Debug.Log($"[SaveDataVersionMigrator] Migration complete: {applicableMigrations.Count} migrations applied");
                }
                else if (saveVersion > targetVersion)
                {
                    Debug.LogWarning($"[SaveDataVersionMigrator] Save version ({saveVersion}) is newer than current version ({targetVersion}). Data may be incompatible.");
                }
                else
                {
                    Debug.Log("[SaveDataVersionMigrator] Save is already at current version");
                }

                return (currentData, wasMigrated);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataVersionMigrator] Migration failed: {ex}");
                return (saveData, false);
            }
        }

        public bool IsMigrationNeeded(GameSaveData saveData)
        {
            if (saveData == null)
                return false;

            int saveVersion = DetermineSaveVersion(saveData);
            return saveVersion < CurrentSaveVersion;
        }

        public Task<bool> CanMigrateAsync(GameSaveData saveData)
        {
            if (saveData == null)
                return Task.FromResult(false);

            try
            {
                int saveVersion = DetermineSaveVersion(saveData);

                // Check if we have migration path from save version to current
                var hasMigrationPath = _migrations.Any(m => m.FromVersion == saveVersion) ||
                                      saveVersion == CurrentSaveVersion;

                return Task.FromResult(hasMigrationPath);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        private int DetermineSaveVersion(GameSaveData saveData)
        {
            // Try to get version from metadata
            if (saveData.saveMetadata?.customMetadata != null &&
                saveData.saveMetadata.customMetadata.TryGetValue("SaveFormatVersion", out var versionObj))
            {
                if (versionObj is int version)
                    return version;

                if (int.TryParse(versionObj.ToString(), out int parsedVersion))
                    return parsedVersion;
            }

            // Try to infer version from game version
            if (!string.IsNullOrEmpty(saveData.saveMetadata?.gameVersion))
            {
                if (_versionRegistry.TryGetValue(saveData.saveMetadata.gameVersion, out int registeredVersion))
                    return registeredVersion;
            }

            // Default to version 0 (oldest format)
            return 0;
        }

        #endregion

        #region Migration Registration

        public void RegisterMigration(ISaveMigration migration)
        {
            if (migration == null)
                return;

            if (_migrations.Any(m => m.FromVersion == migration.FromVersion && m.ToVersion == migration.ToVersion))
            {
                Debug.LogWarning($"[SaveDataVersionMigrator] Migration for v{migration.FromVersion}→v{migration.ToVersion} already registered");
                return;
            }

            _migrations.Add(migration);
            Debug.Log($"[SaveDataVersionMigrator] Registered migration: {migration.GetType().Name} (v{migration.FromVersion} → v{migration.ToVersion})");
        }

        public void RegisterGameVersion(string gameVersion, int saveFormatVersion)
        {
            _versionRegistry[gameVersion] = saveFormatVersion;
            Debug.Log($"[SaveDataVersionMigrator] Registered game version {gameVersion} → save format v{saveFormatVersion}");
        }

        private void RegisterDefaultMigrations()
        {
            // Register game version mappings
            RegisterGameVersion("0.1.0", 0);
            RegisterGameVersion("0.2.0", 0);
            RegisterGameVersion("1.0.0", 1);
            RegisterGameVersion(Application.version, CurrentSaveVersion);

            // Register migrations
            RegisterMigration(new MigrationV0ToV1());

            // Future migrations would be registered here
            // RegisterMigration(new MigrationV1ToV2());
            // RegisterMigration(new MigrationV2ToV3());
        }

        #endregion

        #region Backup & Rollback

        public async Task<string> CreateMigrationBackupAsync(GameSaveData saveData, int slotId)
        {
            try
            {
                var backupId = $"premigration_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
                var backupPath = System.IO.Path.Combine(
                    Application.persistentDataPath,
                    "ChimeraBackups",
                    $"save_{slotId}_migration_{backupId}.json"
                );

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(saveData, Newtonsoft.Json.Formatting.Indented);
                await System.IO.File.WriteAllTextAsync(backupPath, json);

                Debug.Log($"[SaveDataVersionMigrator] Created migration backup: {backupPath}");

                return backupPath;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataVersionMigrator] Failed to create migration backup: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region Migration Report

        public MigrationReport GenerateMigrationReport(GameSaveData saveData)
        {
            var report = new MigrationReport
            {
                currentVersion = DetermineSaveVersion(saveData),
                targetVersion = CurrentSaveVersion,
                isMigrationNeeded = IsMigrationNeeded(saveData),
                timestamp = DateTime.UtcNow
            };

            if (report.isMigrationNeeded)
            {
                var applicableMigrations = _migrations
                    .Where(m => m.FromVersion >= report.currentVersion && m.ToVersion <= report.targetVersion)
                    .OrderBy(m => m.FromVersion)
                    .ToList();

                report.requiredMigrations = applicableMigrations.Select(m => m.GetType().Name).ToList();
                report.estimatedDuration = applicableMigrations.Count * 0.5f; // 0.5s per migration estimate
            }

            return report;
        }

        #endregion
    }

    /// <summary>
    /// Interface for save data migrations
    /// </summary>
    public interface ISaveMigration
    {
        int FromVersion { get; }
        int ToVersion { get; }
        Task<GameSaveData> MigrateAsync(GameSaveData oldData);
    }

    /// <summary>
    /// Example migration from version 0 to version 1
    /// </summary>
    public class MigrationV0ToV1 : ISaveMigration
    {
        public int FromVersion => 0;
        public int ToVersion => 1;

        public async Task<GameSaveData> MigrateAsync(GameSaveData oldData)
        {
            Debug.Log("[MigrationV0ToV1] Migrating save data...");

            // Ensure all collections are initialized
            if (oldData.geneticsData == null)
                oldData.geneticsData = new Dictionary<string, object>();

            if (oldData.ecosystemData == null)
                oldData.ecosystemData = new EcosystemSaveData();

            if (oldData.playerData == null)
                oldData.playerData = new PlayerSaveData();

            // Initialize new fields added in v1
            if (oldData.playerData.statistics == null)
                oldData.playerData.statistics = new Dictionary<string, int>();

            if (oldData.ecosystemData.environmentalFactors == null)
                oldData.ecosystemData.environmentalFactors = new Dictionary<string, object>();

            // Migrate old data structures to new format (example)
            // In a real migration, you would convert old field names/types to new ones

            Debug.Log("[MigrationV0ToV1] Migration complete");

            await Task.CompletedTask;
            return oldData;
        }
    }

    /// <summary>
    /// Migration report with details about required migrations
    /// </summary>
    [Serializable]
    public class MigrationReport
    {
        public int currentVersion;
        public int targetVersion;
        public bool isMigrationNeeded;
        public List<string> requiredMigrations = new List<string>();
        public float estimatedDuration;
        public DateTime timestamp;
    }

    /// <summary>
    /// Exception thrown when save migration fails
    /// </summary>
    public class SaveMigrationException : Exception
    {
        public SaveMigrationException(string message) : base(message) { }
        public SaveMigrationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
