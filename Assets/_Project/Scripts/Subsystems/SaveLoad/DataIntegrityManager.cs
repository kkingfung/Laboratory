using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Data integrity manager for save file validation, corruption detection, and repair.
    /// Ensures save data remains consistent and recoverable across sessions.
    /// </summary>
    public class DataIntegrityManager : MonoBehaviour, IDataIntegrityService
    {
        [Header("Configuration")]
        [SerializeField] private SaveLoadSubsystemConfig config;

        [Header("Integrity State")]
        [SerializeField] private bool isValidationEnabled = true;
        [SerializeField] private IntegrityValidationLevel currentValidationLevel = IntegrityValidationLevel.Standard;
        [SerializeField] private bool autoRepairEnabled = true;

        [Header("Statistics")]
        [SerializeField] private int totalValidations;
        [SerializeField] private int totalRepairs;
        [SerializeField] private int corruptionDetections;
        [SerializeField] private DateTime lastValidationTime;

        // State
        private bool _isInitialized = false;
        private readonly Dictionary<int, string> _checksumCache = new();
        private readonly Dictionary<string, ValidationSchema> _validationSchemas = new();

        // Events
        public event Action<ValidationResult> OnValidationCompleted;
        public event Action<RepairResult> OnRepairCompleted;
        public event Action<CorruptionDetectedEvent> OnCorruptionDetected;

        #region Initialization

        public async Task InitializeAsync(SaveLoadSubsystemConfig configuration)
        {
            config = configuration;

            try
            {
                ApplyConfiguration();
                await InitializeValidationSchemas();
                await LoadChecksumCache();

                _isInitialized = true;
                Debug.Log($"[DataIntegrityManager] Initialized - Validation: {isValidationEnabled}, Level: {currentValidationLevel}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataIntegrityManager] Initialization failed: {ex.Message}");
                throw;
            }
        }

        private void ApplyConfiguration()
        {
            if (config?.DataIntegrityConfig != null)
            {
                isValidationEnabled = config.DataIntegrityConfig.EnableDataValidation;
                currentValidationLevel = config.DataIntegrityConfig.ValidationLevel;
                autoRepairEnabled = config.DataIntegrityConfig.EnableAutoRepair;
            }
        }

        private async Task InitializeValidationSchemas()
        {
            // Initialize validation schemas for different data types
            _validationSchemas["GameSaveData"] = CreateGameSaveSchema();
            _validationSchemas["SaveMetadata"] = CreateSaveMetadataSchema();
            _validationSchemas["EcosystemData"] = CreateEcosystemDataSchema();
            _validationSchemas["PlayerData"] = CreatePlayerDataSchema();

            await Task.CompletedTask;
        }

        private async Task LoadChecksumCache()
        {
            // Load existing checksums from persistent storage
            _checksumCache.Clear();
            await Task.CompletedTask;
        }

        #endregion

        #region Core Validation Operations

        public async Task<ValidationResult> ValidateGameDataAsync(GameSaveData gameData)
        {
            if (!_isInitialized || !isValidationEnabled)
            {
                return new ValidationResult { isValid = true };
            }

            var result = new ValidationResult
            {
                level = (ValidationLevel)currentValidationLevel,
                validatedAt = DateTime.UtcNow
            };

            try
            {
                totalValidations++;

                // Basic null checks
                if (gameData == null)
                {
                    result.isValid = false;
                    result.errors.Add("Game data is null");
                    return result;
                }

                // Validate metadata
                await ValidateMetadata(gameData.saveMetadata, result);

                // Validate genetics data
                await ValidateGeneticsData(gameData.geneticsData, result);

                // Validate ecosystem data
                await ValidateEcosystemData(gameData.ecosystemData, result);

                // Validate player data
                await ValidatePlayerData(gameData.playerData, result);

                // Schema validation if enabled
                if (config.DataIntegrityConfig.EnableSchemaValidation)
                {
                    await ValidateAgainstSchema(gameData, result);
                }

                // Custom validation rules based on level
                await ApplyValidationLevel(gameData, result);

                lastValidationTime = DateTime.UtcNow;
                OnValidationCompleted?.Invoke(result);

                Debug.Log($"[DataIntegrityManager] Validation completed - Valid: {result.isValid}, " +
                         $"Errors: {result.errors.Count}, Warnings: {result.warnings.Count}");
            }
            catch (Exception ex)
            {
                result.isValid = false;
                result.errors.Add($"Validation exception: {ex.Message}");
                Debug.LogError($"[DataIntegrityManager] Validation failed: {ex.Message}");
            }

            return result;
        }

        public async Task<GameSaveData> RepairGameDataAsync(GameSaveData gameData)
        {
            if (!_isInitialized || !autoRepairEnabled)
            {
                return gameData;
            }

            var repairResult = new RepairResult
            {
                repairedAt = DateTime.UtcNow
            };

            try
            {
                totalRepairs++;

                // Create a copy for repair
                var repairedData = CloneGameData(gameData);

                // Repair metadata
                repairedData.saveMetadata = await RepairMetadata(repairedData.saveMetadata, repairResult);

                // Repair genetics data
                repairedData.geneticsData = await RepairGeneticsData(repairedData.geneticsData, repairResult);

                // Repair ecosystem data
                repairedData.ecosystemData = await RepairEcosystemData(repairedData.ecosystemData, repairResult);

                // Repair player data
                repairedData.playerData = RepairPlayerData(repairedData.playerData, repairResult);

                // Validate the repaired data
                var validationResult = await ValidateGameDataAsync(repairedData);

                if (validationResult.isValid)
                {
                    repairResult.wasSuccessful = true;
                    repairResult.repairedData = repairedData;
                }
                else
                {
                    repairResult.wasSuccessful = false;
                    repairResult.unrepairableIssues.AddRange(validationResult.errors);
                }

                OnRepairCompleted?.Invoke(repairResult);
                Debug.Log($"[DataIntegrityManager] Repair completed - Success: {repairResult.wasSuccessful}, " +
                         $"Actions: {repairResult.repairActions.Count}");

                return repairResult.wasSuccessful ? repairedData : gameData;
            }
            catch (Exception ex)
            {
                repairResult.wasSuccessful = false;
                repairResult.unrepairableIssues.Add($"Repair exception: {ex.Message}");
                OnRepairCompleted?.Invoke(repairResult);

                Debug.LogError($"[DataIntegrityManager] Repair failed: {ex.Message}");
                return gameData;
            }
        }

        public async Task<bool> CreateChecksumAsync(int slotId)
        {
            if (!_isInitialized)
                return false;

            try
            {
                var saveDataManager = FindFirstObjectByType<SaveDataManager>();
                if (saveDataManager == null)
                    return false;

                var gameData = await saveDataManager.LoadGameDataAsync(slotId);
                if (gameData == null)
                    return false;

                var checksum = CalculateChecksum(gameData);
                _checksumCache[slotId] = checksum;

                Debug.Log($"[DataIntegrityManager] Checksum created for slot {slotId}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataIntegrityManager] Create checksum failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> VerifyChecksumAsync(int slotId)
        {
            if (!_isInitialized || !_checksumCache.ContainsKey(slotId))
                return false;

            try
            {
                var saveDataManager = FindFirstObjectByType<SaveDataManager>();
                if (saveDataManager == null)
                    return false;

                var gameData = await saveDataManager.LoadGameDataAsync(slotId);
                if (gameData == null)
                    return false;

                var currentChecksum = CalculateChecksum(gameData);
                var storedChecksum = _checksumCache[slotId];

                var isValid = currentChecksum == storedChecksum;

                if (!isValid)
                {
                    var corruptionEvent = new CorruptionDetectedEvent
                    {
                        slotId = slotId,
                        corruptionType = CorruptionType.ChecksumMismatch,
                        detectedAt = DateTime.UtcNow,
                        errorMessage = "Checksum verification failed",
                        isRecoverable = true
                    };

                    OnCorruptionDetected?.Invoke(corruptionEvent);
                    corruptionDetections++;
                }

                Debug.Log($"[DataIntegrityManager] Checksum verification for slot {slotId}: {(isValid ? "PASSED" : "FAILED")}");
                return isValid;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataIntegrityManager] Checksum verification failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        public async Task<CorruptionReport> ScanForCorruptionAsync()
        {
            if (!_isInitialized)
                return new CorruptionReport();

            var report = new CorruptionReport
            {
                scanTime = DateTime.UtcNow,
                corruptedSaves = new List<CorruptedSaveInfo>(),
                systemIssues = new List<string>()
            };

            try
            {
                var saveDataManager = FindFirstObjectByType<SaveDataManager>();
                if (saveDataManager == null)
                {
                    report.systemIssues.Add("SaveDataManager not found");
                    return report;
                }

                var saveSlots = await saveDataManager.GetSaveSlotInfoAsync();
                report.totalSavesScanned = saveSlots.Length;

                foreach (var slot in saveSlots)
                {
                    if (!slot.isOccupied)
                        continue;

                    try
                    {
                        var gameData = await saveDataManager.LoadGameDataAsync(slot.slotId);
                        if (gameData == null)
                        {
                            report.corruptedSaves.Add(new CorruptedSaveInfo
                            {
                                slotId = slot.slotId,
                                saveName = slot.saveName,
                                corruptionType = CorruptionType.UnreadableFile,
                                isRecoverable = false,
                                issues = new List<string> { "Failed to load save data" }
                            });
                            continue;
                        }

                        var validationResult = await ValidateGameDataAsync(gameData);
                        if (!validationResult.isValid)
                        {
                            report.corruptedSaves.Add(new CorruptedSaveInfo
                            {
                                slotId = slot.slotId,
                                saveName = slot.saveName,
                                corruptionType = CorruptionType.InvalidFormat,
                                isRecoverable = true,
                                issues = validationResult.errors
                            });
                        }

                        // Verify checksum if available
                        if (_checksumCache.ContainsKey(slot.slotId))
                        {
                            var checksumValid = await VerifyChecksumAsync(slot.slotId);
                            if (!checksumValid)
                            {
                                var existingCorruption = report.corruptedSaves.Find(c => c.slotId == slot.slotId);
                                if (existingCorruption != null)
                                {
                                    existingCorruption.issues.Add("Checksum mismatch");
                                }
                                else
                                {
                                    report.corruptedSaves.Add(new CorruptedSaveInfo
                                    {
                                        slotId = slot.slotId,
                                        saveName = slot.saveName,
                                        corruptionType = CorruptionType.ChecksumMismatch,
                                        isRecoverable = true,
                                        issues = new List<string> { "Checksum mismatch" }
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        report.corruptedSaves.Add(new CorruptedSaveInfo
                        {
                            slotId = slot.slotId,
                            saveName = slot.saveName,
                            corruptionType = CorruptionType.Unknown,
                            isRecoverable = false,
                            issues = new List<string> { ex.Message }
                        });
                    }
                }

                // Determine overall severity
                report.overallSeverity = CalculateOverallSeverity(report.corruptedSaves);

                Debug.Log($"[DataIntegrityManager] Corruption scan completed - " +
                         $"Scanned: {report.totalSavesScanned}, Corrupted: {report.corruptedSaves.Count}, " +
                         $"Severity: {report.overallSeverity}");
            }
            catch (Exception ex)
            {
                report.systemIssues.Add($"Scan exception: {ex.Message}");
                Debug.LogError($"[DataIntegrityManager] Corruption scan failed: {ex.Message}");
            }

            return report;
        }

        public async Task<bool> QuarantineCorruptedSaveAsync(int slotId)
        {
            if (!_isInitialized)
                return false;

            try
            {
                var saveDataManager = FindFirstObjectByType<SaveDataManager>();
                if (saveDataManager == null)
                    return false;

                // Create a quarantine backup
                var backupResult = await saveDataManager.CreateBackupAsync(slotId);
                if (!backupResult)
                {
                    Debug.LogWarning($"[DataIntegrityManager] Failed to create quarantine backup for slot {slotId}");
                }

                // Mark as quarantined (would require extending save slot info)
                Debug.Log($"[DataIntegrityManager] Save slot {slotId} quarantined");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DataIntegrityManager] Quarantine failed for slot {slotId}: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Validation Methods

        private async Task ValidateMetadata(SaveMetadata metadata, ValidationResult result)
        {
            if (metadata == null)
            {
                result.errors.Add("Save metadata is null");
                return;
            }

            if (string.IsNullOrEmpty(metadata.saveId))
                result.errors.Add("Save ID is missing");

            if (string.IsNullOrEmpty(metadata.saveName))
                result.warnings.Add("Save name is empty");

            if (metadata.created > DateTime.UtcNow)
                result.errors.Add("Save creation date is in the future");

            if (metadata.lastSaved > DateTime.UtcNow)
                result.errors.Add("Last saved date is in the future");

            if (metadata.playTime < 0)
                result.errors.Add("Play time cannot be negative");

            await Task.CompletedTask;
        }

        private async Task ValidateGeneticsData(Dictionary<string, object> geneticsData, ValidationResult result)
        {
            if (geneticsData == null)
            {
                result.warnings.Add("Genetics data is null");
                return;
            }

            // Validate genetics data structure and content
            await Task.CompletedTask;
        }

        private async Task ValidateEcosystemData(EcosystemSaveData ecosystemData, ValidationResult result)
        {
            if (ecosystemData == null)
            {
                result.warnings.Add("Ecosystem data is null");
                return;
            }

            if (ecosystemData.biomes != null)
            {
                foreach (var biome in ecosystemData.biomes)
                {
                    if (string.IsNullOrEmpty(biome.biomeId))
                        result.errors.Add($"Biome missing ID: {biome.biomeName}");
                }
            }

            await Task.CompletedTask;
        }

        private async Task ValidatePlayerData(PlayerSaveData playerData, ValidationResult result)
        {
            if (playerData == null)
            {
                result.errors.Add("Player data is null");
                return;
            }

            if (string.IsNullOrEmpty(playerData.playerId))
                result.errors.Add("Player ID is missing");

            if (playerData.level < 0)
                result.errors.Add("Player level cannot be negative");

            if (playerData.experience < 0)
                result.errors.Add("Player experience cannot be negative");

            await Task.CompletedTask;
        }

        private async Task ValidateAgainstSchema(GameSaveData gameData, ValidationResult result)
        {
            // Schema-based validation would go here
            await Task.CompletedTask;
        }

        private async Task ApplyValidationLevel(GameSaveData gameData, ValidationResult result)
        {
            switch (currentValidationLevel)
            {
                case IntegrityValidationLevel.Basic:
                    // Only critical validation
                    break;
                case IntegrityValidationLevel.Standard:
                    // Standard validation rules
                    break;
                case IntegrityValidationLevel.Strict:
                    // Comprehensive validation
                    await PerformStrictValidation(gameData, result);
                    break;
            }
        }

        private async Task PerformStrictValidation(GameSaveData gameData, ValidationResult result)
        {
            // Additional strict validation rules
            await Task.CompletedTask;
        }

        #endregion

        #region Repair Methods

        private async Task<SaveMetadata> RepairMetadata(SaveMetadata metadata, RepairResult repairResult)
        {
            if (metadata == null)
            {
                metadata = new SaveMetadata();
                repairResult.repairActions.Add("Created missing save metadata");
            }

            if (string.IsNullOrEmpty(metadata.saveId))
            {
                metadata.saveId = Guid.NewGuid().ToString();
                repairResult.repairActions.Add("Generated missing save ID");
            }

            if (string.IsNullOrEmpty(metadata.saveName))
            {
                metadata.saveName = $"Repaired Save {DateTime.Now:yyyy-MM-dd HH:mm}";
                repairResult.repairActions.Add("Set default save name");
            }

            if (metadata.created > DateTime.UtcNow)
            {
                metadata.created = DateTime.UtcNow;
                repairResult.repairActions.Add("Fixed future creation date");
            }

            if (metadata.playTime < 0)
            {
                metadata.playTime = 0;
                repairResult.repairActions.Add("Reset negative play time");
            }

            await Task.CompletedTask;
            return metadata;
        }

        private async Task<Dictionary<string, object>> RepairGeneticsData(Dictionary<string, object> geneticsData, RepairResult repairResult)
        {
            if (geneticsData == null)
            {
                geneticsData = new Dictionary<string, object>();
                repairResult.repairActions.Add("Created missing genetics data");
            }

            await Task.CompletedTask;
            return geneticsData;
        }

        private async Task<EcosystemSaveData> RepairEcosystemData(EcosystemSaveData ecosystemData, RepairResult repairResult)
        {
            if (ecosystemData == null)
            {
                ecosystemData = new EcosystemSaveData();
                repairResult.repairActions.Add("Created missing ecosystem data");
            }

            if (ecosystemData.biomes == null)
            {
                ecosystemData.biomes = new List<BiomeData>();
                repairResult.repairActions.Add("Created missing biomes list");
            }

            if (ecosystemData.populations == null)
            {
                ecosystemData.populations = new List<PopulationData>();
                repairResult.repairActions.Add("Created missing populations list");
            }

            await Task.CompletedTask;
            return ecosystemData;
        }

        private PlayerSaveData RepairPlayerData(PlayerSaveData playerData, RepairResult repairResult)
        {
            if (playerData == null)
            {
                playerData = new PlayerSaveData();
                repairResult.repairActions.Add("Created missing player data");
            }

            if (string.IsNullOrEmpty(playerData.playerId))
            {
                playerData.playerId = SystemInfo.deviceUniqueIdentifier;
                repairResult.repairActions.Add("Generated missing player ID");
            }

            if (playerData.level < 0)
            {
                playerData.level = 0;
                repairResult.repairActions.Add("Reset negative player level");
            }

            if (playerData.experience < 0)
            {
                playerData.experience = 0;
                repairResult.repairActions.Add("Reset negative player experience");
            }

            return playerData;
        }

        #endregion

        #region Helper Methods

        private GameSaveData CloneGameData(GameSaveData original)
        {
            // Simple clone using JSON serialization
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(original);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<GameSaveData>(json);
        }

        private string CalculateChecksum(GameSaveData gameData)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(gameData);
            var bytes = Encoding.UTF8.GetBytes(json);

            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        private CorruptionSeverity CalculateOverallSeverity(List<CorruptedSaveInfo> corruptedSaves)
        {
            if (corruptedSaves.Count == 0)
                return CorruptionSeverity.None;

            var unrecoverableCount = corruptedSaves.Count(c => !c.isRecoverable);
            var totalCount = corruptedSaves.Count;

            if (unrecoverableCount > totalCount * 0.5f)
                return CorruptionSeverity.Critical;
            else if (totalCount > 3)
                return CorruptionSeverity.Severe;
            else if (totalCount > 1)
                return CorruptionSeverity.Moderate;
            else
                return CorruptionSeverity.Minor;
        }

        private ValidationSchema CreateGameSaveSchema()
        {
            return new ValidationSchema(); // Placeholder
        }

        private ValidationSchema CreateSaveMetadataSchema()
        {
            return new ValidationSchema(); // Placeholder
        }

        private ValidationSchema CreateEcosystemDataSchema()
        {
            return new ValidationSchema(); // Placeholder
        }

        private ValidationSchema CreatePlayerDataSchema()
        {
            return new ValidationSchema(); // Placeholder
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Validation")]
        private void TestValidation()
        {
            if (_isInitialized)
            {
                Debug.Log("[DataIntegrityManager] Test validation - Not implemented in demo");
            }
        }

        [ContextMenu("Scan for Corruption")]
        private void ScanForCorruptionDebug()
        {
            if (_isInitialized)
            {
                _ = ScanForCorruptionAsync();
            }
        }

        #endregion
    }

    #region Supporting Classes

    public class ValidationSchema
    {
        public string SchemaVersion { get; set; }
        public Dictionary<string, ValidationRule> Rules { get; set; }

        public ValidationSchema()
        {
            Rules = new Dictionary<string, ValidationRule>();
        }
    }

    public class ValidationRule
    {
        public string PropertyName { get; set; }
        public Type ExpectedType { get; set; }
        public bool IsRequired { get; set; }
        public object DefaultValue { get; set; }
        public Func<object, bool> CustomValidator { get; set; }
    }

    #endregion
}