using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Comprehensive save data validation and corruption recovery system
    /// Validates save integrity, detects corruption, and attempts automatic repairs
    /// </summary>
    public class SaveDataValidator
    {
        private readonly SaveLoadSubsystemConfig _config;
        private readonly List<IDataValidator> _validators = new List<IDataValidator>();

        public SaveDataValidator(SaveLoadSubsystemConfig config)
        {
            _config = config;
            RegisterDefaultValidators();
        }

        #region Validation

        public async Task<ValidationResult> ValidateGameDataAsync(GameSaveData gameData, ValidationLevel level)
        {
            var result = new ValidationResult
            {
                level = level,
                validatedAt = DateTime.UtcNow
            };

            if (gameData == null)
            {
                result.isValid = false;
                result.errors.Add("GameSaveData is null");
                return result;
            }

            try
            {
                // Basic validation
                await ValidateMetadata(gameData.saveMetadata, result);

                if (level >= ValidationLevel.Standard)
                {
                    await ValidatePlayerData(gameData.playerData, result);
                    await ValidateEcosystemData(gameData.ecosystemData, result);
                }

                if (level >= ValidationLevel.Deep)
                {
                    await ValidateGeneticsData(gameData.geneticsData, result);
                    await ValidateDataConsistency(gameData, result);
                }

                if (level >= ValidationLevel.Thorough)
                {
                    await ValidateDataIntegrity(gameData, result);
                    await RunCustomValidators(gameData, result);
                }

                // Determine overall validity
                result.isValid = result.errors.Count == 0;

                Debug.Log($"[SaveDataValidator] Validation complete: Valid={result.isValid}, Errors={result.errors.Count}, Warnings={result.warnings.Count}");

                return result;
            }
            catch (Exception ex)
            {
                result.isValid = false;
                result.errors.Add($"Validation exception: {ex.Message}");
                Debug.LogError($"[SaveDataValidator] Validation failed with exception: {ex}");
                return result;
            }
        }

        private async Task ValidateMetadata(SaveMetadata metadata, ValidationResult result)
        {
            if (metadata == null)
            {
                result.errors.Add("SaveMetadata is null");
                return;
            }

            if (string.IsNullOrEmpty(metadata.saveId))
                result.errors.Add("SaveMetadata.saveId is invalid");

            if (string.IsNullOrEmpty(metadata.saveName))
                result.warnings.Add("SaveMetadata.saveName is empty");

            if (metadata.created > DateTime.UtcNow)
                result.errors.Add("SaveMetadata.created is in the future");

            if (metadata.lastSaved > DateTime.UtcNow)
                result.errors.Add("SaveMetadata.lastSaved is in the future");

            if (metadata.lastSaved < metadata.created)
                result.errors.Add("SaveMetadata.lastSaved is before created time");

            if (metadata.playTime < 0)
                result.errors.Add("SaveMetadata.playTime is negative");

            // Version validation
            if (!IsVersionCompatible(metadata.gameVersion))
                result.warnings.Add($"Game version mismatch: Save={metadata.gameVersion}, Current={Application.version}");

            await Task.CompletedTask;
        }

        private async Task ValidatePlayerData(PlayerSaveData playerData, ValidationResult result)
        {
            if (playerData == null)
            {
                result.errors.Add("PlayerSaveData is null");
                return;
            }

            if (string.IsNullOrEmpty(playerData.playerId))
                result.errors.Add("PlayerData.playerId is invalid");

            if (playerData.level < 0)
                result.errors.Add("PlayerData.level is negative");

            if (playerData.experience < 0)
                result.errors.Add("PlayerData.experience is negative");

            if (playerData.currency < 0)
                result.warnings.Add("PlayerData.currency is negative");

            await Task.CompletedTask;
        }

        private async Task ValidateEcosystemData(EcosystemSaveData ecosystemData, ValidationResult result)
        {
            if (ecosystemData == null)
            {
                result.warnings.Add("EcosystemSaveData is null");
                return;
            }

            // Validate biomes
            if (ecosystemData.biomes != null)
            {
                foreach (var biome in ecosystemData.biomes)
                {
                    if (string.IsNullOrEmpty(biome.biomeId))
                        result.errors.Add($"Biome has invalid ID");

                    if (biome.radius < 0)
                        result.errors.Add($"Biome {biome.biomeId} has negative radius");
                }
            }

            // Validate populations
            if (ecosystemData.populations != null)
            {
                foreach (var population in ecosystemData.populations)
                {
                    if (population.population < 0)
                        result.errors.Add($"Population {population.speciesId} has negative count");

                    if (population.population > population.maxPopulation)
                        result.warnings.Add($"Population {population.speciesId} exceeds max population");

                    if (population.growthRate < -1f || population.growthRate > 10f)
                        result.warnings.Add($"Population {population.speciesId} has unusual growth rate: {population.growthRate}");
                }
            }

            await Task.CompletedTask;
        }

        private async Task ValidateGeneticsData(Dictionary<string, object> geneticsData, ValidationResult result)
        {
            if (geneticsData == null)
            {
                result.warnings.Add("GeneticsData is null");
                return;
            }

            if (geneticsData.Count == 0)
            {
                result.warnings.Add("GeneticsData is empty");
            }

            // Validate genetic data structure
            foreach (var kvp in geneticsData)
            {
                if (string.IsNullOrEmpty(kvp.Key))
                    result.errors.Add("GeneticsData contains entry with empty key");

                if (kvp.Value == null)
                    result.warnings.Add($"GeneticsData entry '{kvp.Key}' has null value");
            }

            await Task.CompletedTask;
        }

        private async Task ValidateDataConsistency(GameSaveData gameData, ValidationResult result)
        {
            // Check for data consistency across systems

            // Example: Player position should be in a valid scene
            if (!string.IsNullOrEmpty(gameData.playerData.currentScene))
            {
                // Would validate against list of valid scenes
            }

            // Ecosystem populations should reference valid biomes
            if (gameData.ecosystemData?.populations != null && gameData.ecosystemData?.biomes != null)
            {
                var biomeIds = new HashSet<string>(gameData.ecosystemData.biomes.Select(b => b.biomeId));

                foreach (var population in gameData.ecosystemData.populations)
                {
                    if (!string.IsNullOrEmpty(population.biomeId) && !biomeIds.Contains(population.biomeId))
                    {
                        result.errors.Add($"Population {population.speciesId} references non-existent biome {population.biomeId}");
                    }
                }
            }

            await Task.CompletedTask;
        }

        private async Task ValidateDataIntegrity(GameSaveData gameData, ValidationResult result)
        {
            // Deep data integrity checks

            // Check for duplicate IDs
            var allIds = new HashSet<string>();

            if (gameData.ecosystemData?.biomes != null)
            {
                foreach (var biome in gameData.ecosystemData.biomes)
                {
                    if (!allIds.Add(biome.biomeId))
                        result.errors.Add($"Duplicate biome ID: {biome.biomeId}");
                }
            }

            // Check for circular references (if applicable)
            // Check for orphaned data
            // Validate data ranges and bounds

            await Task.CompletedTask;
        }

        private async Task RunCustomValidators(GameSaveData gameData, ValidationResult result)
        {
            foreach (var validator in _validators)
            {
                try
                {
                    await validator.ValidateAsync(gameData, result);
                }
                catch (Exception ex)
                {
                    result.errors.Add($"Custom validator '{validator.GetType().Name}' failed: {ex.Message}");
                }
            }
        }

        #endregion

        #region Corruption Recovery

        public async Task<(GameSaveData repairedData, bool wasRepaired)> AttemptRepairAsync(GameSaveData corruptedData)
        {
            await Task.CompletedTask;
            bool wasRepaired = false;
            var data = corruptedData ?? new GameSaveData();

            Debug.Log("[SaveDataValidator] Attempting to repair corrupted save data...");

            try
            {
                // Repair metadata
                if (data.saveMetadata == null)
                {
                    data.saveMetadata = new SaveMetadata
                    {
                        saveName = "Recovered Save",
                        gameVersion = Application.version
                    };
                    wasRepaired = true;
                }
                else
                {
                    if (string.IsNullOrEmpty(data.saveMetadata.saveId))
                    {
                        data.saveMetadata.saveId = Guid.NewGuid().ToString();
                        wasRepaired = true;
                    }

                    if (data.saveMetadata.created > DateTime.UtcNow)
                    {
                        data.saveMetadata.created = DateTime.UtcNow;
                        wasRepaired = true;
                    }
                }

                // Repair player data
                if (data.playerData == null)
                {
                    data.playerData = new PlayerSaveData();
                    wasRepaired = true;
                }
                else
                {
                    if (data.playerData.level < 0)
                    {
                        data.playerData.level = 1;
                        wasRepaired = true;
                    }

                    if (data.playerData.experience < 0)
                    {
                        data.playerData.experience = 0;
                        wasRepaired = true;
                    }

                    if (data.playerData.currency < 0)
                    {
                        data.playerData.currency = 0;
                        wasRepaired = true;
                    }
                }

                // Repair ecosystem data
                if (data.ecosystemData == null)
                {
                    data.ecosystemData = new EcosystemSaveData();
                    wasRepaired = true;
                }
                else
                {
                    if (data.ecosystemData.biomes == null)
                    {
                        data.ecosystemData.biomes = new List<BiomeData>();
                        wasRepaired = true;
                    }

                    if (data.ecosystemData.populations == null)
                    {
                        data.ecosystemData.populations = new List<PopulationData>();
                        wasRepaired = true;
                    }

                    // Fix population counts
                    foreach (var population in data.ecosystemData.populations)
                    {
                        if (population.population < 0)
                        {
                            population.population = 0;
                            wasRepaired = true;
                        }

                        if (population.maxPopulation < population.population)
                        {
                            population.maxPopulation = population.population;
                            wasRepaired = true;
                        }
                    }
                }

                // Repair genetics data
                if (data.geneticsData == null)
                {
                    data.geneticsData = new Dictionary<string, object>();
                    wasRepaired = true;
                }

                if (wasRepaired)
                {
                    Debug.Log("[SaveDataValidator] Save data repaired successfully");
                }
                else
                {
                    Debug.Log("[SaveDataValidator] No repairs needed");
                }

                return (data, wasRepaired);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveDataValidator] Repair failed: {ex.Message}");
                return (data, wasRepaired);
            }
        }

        public async Task<bool> CanRecoverSaveAsync(GameSaveData corruptedData)
        {
            await Task.CompletedTask;
            if (corruptedData == null)
                return false;

            // Check if the save has critical data that can be recovered
            bool hasMetadata = corruptedData.saveMetadata != null;
            bool hasPlayerData = corruptedData.playerData != null;

            // A save is recoverable if it has at least metadata or player data
            return hasMetadata || hasPlayerData;
        }

        #endregion

        #region Custom Validators

        public void RegisterValidator(IDataValidator validator)
        {
            if (validator != null && !_validators.Contains(validator))
            {
                _validators.Add(validator);
                Debug.Log($"[SaveDataValidator] Registered custom validator: {validator.GetType().Name}");
            }
        }

        public void UnregisterValidator(IDataValidator validator)
        {
            if (_validators.Remove(validator))
            {
                Debug.Log($"[SaveDataValidator] Unregistered custom validator: {validator.GetType().Name}");
            }
        }

        private void RegisterDefaultValidators()
        {
            // Register any default validators here
        }

        #endregion

        #region Helper Methods

        private bool IsVersionCompatible(string saveVersion)
        {
            if (string.IsNullOrEmpty(saveVersion))
                return false;

            // Simple version compatibility check
            // In production, implement proper semantic versioning comparison
            return saveVersion == Application.version ||
                   saveVersion.StartsWith(GetMajorVersion(Application.version));
        }

        private string GetMajorVersion(string version)
        {
            var parts = version.Split('.');
            return parts.Length > 0 ? parts[0] : version;
        }

        #endregion
    }

    /// <summary>
    /// Interface for custom data validators
    /// </summary>
    public interface IDataValidator
    {
        Task ValidateAsync(GameSaveData gameData, ValidationResult result);
    }

    public enum ValidationLevel
    {
        Basic,      // Quick validation (metadata only)
        Standard,   // Standard validation (player + ecosystem)
        Deep,       // Deep validation (including genetics + consistency)
        Thorough    // Thorough validation (all checks + custom validators)
    }
}
