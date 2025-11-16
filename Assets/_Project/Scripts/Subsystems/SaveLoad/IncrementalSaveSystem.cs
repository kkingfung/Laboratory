using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Incremental save system that only saves changed data since last save
    /// Dramatically improves save performance for large datasets (1000+ creatures)
    /// </summary>
    public class IncrementalSaveSystem
    {
        private GameSaveData _lastSaveSnapshot;
        private readonly Dictionary<string, object> _changeTracker = new Dictionary<string, object>();
        private readonly SaveLoadSubsystemConfig _config;
        private DateTime _lastFullSaveTime = DateTime.MinValue;

        public IncrementalSaveSystem(SaveLoadSubsystemConfig config)
        {
            _config = config;
        }

        #region Change Tracking

        public void BeginChangeTracking(GameSaveData baselineData)
        {
            _lastSaveSnapshot = DeepClone(baselineData);
            _changeTracker.Clear();

            Debug.Log("[IncrementalSaveSystem] Change tracking started");
        }

        public void TrackChange(string path, object value)
        {
            _changeTracker[path] = value;
        }

        public bool HasChanges()
        {
            return _changeTracker.Count > 0;
        }

        public int GetChangeCount()
        {
            return _changeTracker.Count;
        }

        #endregion

        #region Incremental Save

        public async Task<IncrementalSaveData> CreateIncrementalSaveAsync(GameSaveData currentData)
        {
            if (_lastSaveSnapshot == null)
            {
                Debug.LogWarning("[IncrementalSaveSystem] No baseline snapshot. Creating full save.");
                return null; // Caller should perform full save
            }

            var incrementalData = new IncrementalSaveData
            {
                baselineTimestamp = _lastSaveSnapshot.saveMetadata.lastSaved,
                incrementTimestamp = DateTime.UtcNow,
                changes = new Dictionary<string, object>()
            };

            try
            {
                // Detect changes between current data and last snapshot
                await DetectChangesAsync(currentData, _lastSaveSnapshot, incrementalData);

                // Add tracked changes
                foreach (var change in _changeTracker)
                {
                    incrementalData.changes[change.Key] = change.Value;
                }

                Debug.Log($"[IncrementalSaveSystem] Incremental save created: {incrementalData.changes.Count} changes detected");

                // Clear change tracker
                _changeTracker.Clear();

                return incrementalData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IncrementalSaveSystem] Failed to create incremental save: {ex.Message}");
                return null;
            }
        }

        public async Task<GameSaveData> ApplyIncrementalSaveAsync(GameSaveData baselineData, IncrementalSaveData incrementalData)
        {
            if (baselineData == null || incrementalData == null)
            {
                Debug.LogError("[IncrementalSaveSystem] Cannot apply incremental save: null data");
                return baselineData;
            }

            try
            {
                var updatedData = DeepClone(baselineData);

                foreach (var change in incrementalData.changes)
                {
                    ApplyChange(updatedData, change.Key, change.Value);
                }

                // Update metadata
                updatedData.saveMetadata.lastSaved = incrementalData.incrementTimestamp;

                Debug.Log($"[IncrementalSaveSystem] Applied {incrementalData.changes.Count} incremental changes");

                return updatedData;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[IncrementalSaveSystem] Failed to apply incremental save: {ex.Message}");
                return baselineData;
            }
        }

        #endregion

        #region Change Detection

        private async Task DetectChangesAsync(GameSaveData current, GameSaveData baseline, IncrementalSaveData incrementalData)
        {
            // Detect metadata changes
            DetectMetadataChanges(current.saveMetadata, baseline.saveMetadata, incrementalData);

            // Detect player data changes
            DetectPlayerDataChanges(current.playerData, baseline.playerData, incrementalData);

            // Detect ecosystem changes
            await DetectEcosystemChangesAsync(current.ecosystemData, baseline.ecosystemData, incrementalData);

            // Detect genetics changes
            DetectGeneticsChanges(current.geneticsData, baseline.geneticsData, incrementalData);

            await Task.CompletedTask;
        }

        private void DetectMetadataChanges(SaveMetadata current, SaveMetadata baseline, IncrementalSaveData incrementalData)
        {
            if (current == null || baseline == null) return;

            if (current.playTime != baseline.playTime)
                incrementalData.changes["metadata.playTime"] = current.playTime;

            if (current.gameLevel != baseline.gameLevel)
                incrementalData.changes["metadata.gameLevel"] = current.gameLevel;
        }

        private void DetectPlayerDataChanges(PlayerSaveData current, PlayerSaveData baseline, IncrementalSaveData incrementalData)
        {
            if (current == null || baseline == null) return;

            if (current.level != baseline.level)
                incrementalData.changes["player.level"] = current.level;

            if (current.experience != baseline.experience)
                incrementalData.changes["player.experience"] = current.experience;

            if (current.currency != baseline.currency)
                incrementalData.changes["player.currency"] = current.currency;

            if (current.lastPosition != baseline.lastPosition)
                incrementalData.changes["player.lastPosition"] = current.lastPosition;

            if (current.currentScene != baseline.currentScene)
                incrementalData.changes["player.currentScene"] = current.currentScene;

            // Detect achievement changes
            if (current.unlockedAchievements != null && baseline.unlockedAchievements != null)
            {
                var newAchievements = current.unlockedAchievements.Except(baseline.unlockedAchievements).ToList();
                if (newAchievements.Count > 0)
                {
                    incrementalData.changes["player.newAchievements"] = newAchievements;
                }
            }

            // Detect statistics changes
            if (current.statistics != null && baseline.statistics != null)
            {
                foreach (var stat in current.statistics)
                {
                    if (!baseline.statistics.TryGetValue(stat.Key, out int baselineValue) ||
                        stat.Value != baselineValue)
                    {
                        incrementalData.changes[$"player.statistics.{stat.Key}"] = stat.Value;
                    }
                }
            }
        }

        private async Task DetectEcosystemChangesAsync(EcosystemSaveData current, EcosystemSaveData baseline, IncrementalSaveData incrementalData)
        {
            if (current == null || baseline == null) return;

            // Detect biome changes
            if (current.biomes != null && baseline.biomes != null)
            {
                var baselineBiomes = baseline.biomes.ToDictionary(b => b.biomeId, b => b);

                foreach (var biome in current.biomes)
                {
                    if (!baselineBiomes.TryGetValue(biome.biomeId, out var baselineBiome))
                    {
                        // New biome
                        incrementalData.changes[$"ecosystem.biomes.{biome.biomeId}"] = biome;
                    }
                    else
                    {
                        // Check for biome property changes
                        if (!AreBiomesEqual(biome, baselineBiome))
                        {
                            incrementalData.changes[$"ecosystem.biomes.{biome.biomeId}"] = biome;
                        }
                    }
                }
            }

            // Detect population changes
            if (current.populations != null && baseline.populations != null)
            {
                var baselinePopulations = baseline.populations.ToDictionary(p => p.speciesId, p => p);

                foreach (var population in current.populations)
                {
                    if (!baselinePopulations.TryGetValue(population.speciesId, out var baselinePop))
                    {
                        // New population
                        incrementalData.changes[$"ecosystem.populations.{population.speciesId}"] = population;
                    }
                    else
                    {
                        // Check for population changes
                        if (!ArePopulationsEqual(population, baselinePop))
                        {
                            incrementalData.changes[$"ecosystem.populations.{population.speciesId}"] = population;
                        }
                    }
                }
            }

            // Detect weather changes
            if (current.currentWeather != null && baseline.currentWeather != null)
            {
                if (!AreWeathersEqual(current.currentWeather, baseline.currentWeather))
                {
                    incrementalData.changes["ecosystem.currentWeather"] = current.currentWeather;
                }
            }

            await Task.CompletedTask;
        }

        private void DetectGeneticsChanges(Dictionary<string, object> current, Dictionary<string, object> baseline, IncrementalSaveData incrementalData)
        {
            if (current == null || baseline == null) return;

            foreach (var entry in current)
            {
                if (!baseline.TryGetValue(entry.Key, out var baselineValue) ||
                    !AreValuesEqual(entry.Value, baselineValue))
                {
                    incrementalData.changes[$"genetics.{entry.Key}"] = entry.Value;
                }
            }
        }

        #endregion

        #region Change Application

        private void ApplyChange(GameSaveData data, string path, object value)
        {
            var parts = path.Split('.');

            if (parts.Length < 2)
                return;

            switch (parts[0])
            {
                case "metadata":
                    ApplyMetadataChange(data.saveMetadata, parts, value);
                    break;

                case "player":
                    ApplyPlayerChange(data.playerData, parts, value);
                    break;

                case "ecosystem":
                    ApplyEcosystemChange(data.ecosystemData, parts, value);
                    break;

                case "genetics":
                    ApplyGeneticsChange(data.geneticsData, parts, value);
                    break;
            }
        }

        private void ApplyMetadataChange(SaveMetadata metadata, string[] path, object value)
        {
            switch (path[1])
            {
                case "playTime":
                    metadata.playTime = Convert.ToSingle(value);
                    break;
                case "gameLevel":
                    metadata.gameLevel = Convert.ToInt32(value);
                    break;
            }
        }

        private void ApplyPlayerChange(PlayerSaveData playerData, string[] path, object value)
        {
            switch (path[1])
            {
                case "level":
                    playerData.level = Convert.ToInt32(value);
                    break;
                case "experience":
                    playerData.experience = Convert.ToInt32(value);
                    break;
                case "currency":
                    playerData.currency = Convert.ToInt32(value);
                    break;
                case "lastPosition":
                    playerData.lastPosition = (Vector3)value;
                    break;
                case "currentScene":
                    playerData.currentScene = value.ToString();
                    break;
                case "newAchievements":
                    if (value is List<string> newAchievements)
                    {
                        playerData.unlockedAchievements.AddRange(newAchievements);
                    }
                    break;
                case "statistics":
                    if (path.Length >= 3)
                    {
                        playerData.statistics[path[2]] = Convert.ToInt32(value);
                    }
                    break;
            }
        }

        private void ApplyEcosystemChange(EcosystemSaveData ecosystemData, string[] path, object value)
        {
            switch (path[1])
            {
                case "biomes":
                    if (path.Length >= 3 && value is BiomeData biome)
                    {
                        var existingIndex = ecosystemData.biomes.FindIndex(b => b.biomeId == path[2]);
                        if (existingIndex >= 0)
                            ecosystemData.biomes[existingIndex] = biome;
                        else
                            ecosystemData.biomes.Add(biome);
                    }
                    break;

                case "populations":
                    if (path.Length >= 3 && value is PopulationData population)
                    {
                        var existingIndex = ecosystemData.populations.FindIndex(p => p.speciesId == path[2]);
                        if (existingIndex >= 0)
                            ecosystemData.populations[existingIndex] = population;
                        else
                            ecosystemData.populations.Add(population);
                    }
                    break;

                case "currentWeather":
                    ecosystemData.currentWeather = (WeatherData)value;
                    break;
            }
        }

        private void ApplyGeneticsChange(Dictionary<string, object> geneticsData, string[] path, object value)
        {
            if (path.Length >= 2)
            {
                geneticsData[path[1]] = value;
            }
        }

        #endregion

        #region Full Save Scheduling

        public bool ShouldPerformFullSave()
        {
            // Perform full save periodically (default: 30 minutes)
            var timeSinceLastFullSave = DateTime.UtcNow - _lastFullSaveTime;
            var fullSaveInterval = TimeSpan.FromMinutes(30);

            if (timeSinceLastFullSave >= fullSaveInterval)
                return true;

            // Perform full save if too many incremental changes
            if (_changeTracker.Count > 1000)
                return true;

            return false;
        }

        public void MarkFullSaveCompleted()
        {
            _lastFullSaveTime = DateTime.UtcNow;
        }

        #endregion

        #region Helper Methods

        private GameSaveData DeepClone(GameSaveData data)
        {
            if (data == null) return null;

            // Use JSON serialization for deep cloning
            var json = JsonConvert.SerializeObject(data);
            return JsonConvert.DeserializeObject<GameSaveData>(json);
        }

        private bool AreBiomesEqual(BiomeData a, BiomeData b)
        {
            return a.biomeType == b.biomeType &&
                   a.position == b.position &&
                   a.radius == b.radius &&
                   a.health == b.health;
        }

        private bool ArePopulationsEqual(PopulationData a, PopulationData b)
        {
            return a.population == b.population &&
                   a.maxPopulation == b.maxPopulation &&
                   a.growthRate == b.growthRate &&
                   a.status == b.status;
        }

        private bool AreWeathersEqual(WeatherData a, WeatherData b)
        {
            return a.weatherType == b.weatherType &&
                   a.intensity == b.intensity &&
                   a.temperature == b.temperature &&
                   a.humidity == b.humidity;
        }

        private bool AreValuesEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            return a.Equals(b);
        }

        #endregion
    }

    /// <summary>
    /// Data structure for incremental save containing only changed data
    /// </summary>
    [Serializable]
    public class IncrementalSaveData
    {
        public DateTime baselineTimestamp;
        public DateTime incrementTimestamp;
        public Dictionary<string, object> changes;
        public int changeCount => changes?.Count ?? 0;

        public IncrementalSaveData()
        {
            changes = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Statistics about incremental saving performance
    /// </summary>
    [Serializable]
    public class IncrementalSaveStats
    {
        public int totalIncrementalSaves;
        public int totalFullSaves;
        public float averageIncrementalSaveTime;
        public float averageFullSaveTime;
        public long totalDataSaved;
        public long dataSavedByIncrementalSaves;
        public float compressionRatio => totalDataSaved > 0 ? (float)dataSavedByIncrementalSaves / totalDataSaved : 0f;
    }
}
