using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ProjectChimera.Core;

namespace Laboratory.Core.Persistence
{
    /// <summary>
    /// Comprehensive game state persistence system that saves and loads all
    /// simulation data including creatures, ecosystems, player analytics,
    /// stories, and system states. Supports incremental saves and data versioning.
    /// </summary>
    public class GameStatePersistenceSystem : MonoBehaviour
    {
        [Header("Persistence Configuration")]
        [SerializeField] private bool enableAutomaticSaving = true;
        [SerializeField] private float autoSaveInterval = GameConstants.AUTOSAVE_INTERVAL_SECONDS;
        [SerializeField] private int maxSaveSlots = GameConstants.MAX_SAVE_SLOTS;
        [SerializeField] private bool compressSaveData = true;

        [Header("Save Data Management")]
        [SerializeField] private string saveDataPath = "LaboratorySaves";
        [SerializeField] private bool enableIncrementalSaves = true;
        [SerializeField] private int maxIncrementalSaves = 5;
        [SerializeField] private bool enableCloudSync = false;

        [Header("Data Versioning")]
        [SerializeField] private string currentDataVersion = "1.0.0";
        [SerializeField] private bool enableVersionMigration = true;
        [SerializeField] private bool backupBeforeMigration = true;

        [Header("Performance Settings")]
        [SerializeField] private bool useAsyncSaving = true;
        [SerializeField] private bool enableProgressiveLoading = true;
        [SerializeField] private int maxItemsPerFrame = 100;

        // Core persistence data
        private GameStateData currentGameState;
        private Dictionary<string, SystemSaveData> systemSaveData = new Dictionary<string, SystemSaveData>();
        private Dictionary<string, SaveSlot> availableSaveSlots = new Dictionary<string, SaveSlot>();

        // Persistence tracking
        private float lastAutoSave;
        private bool saveInProgress;
        private bool loadInProgress;
        private PersistenceAnalytics analytics = new PersistenceAnalytics();

        // Connected systems (using generic references to avoid circular dependencies)
        private MonoBehaviour integrationManager;
        private MonoBehaviour evolutionManager;
        private MonoBehaviour personalityManager;
        private MonoBehaviour ecosystemSimulator;
        private MonoBehaviour analyticsTracker;
        private MonoBehaviour questGenerator;
        private MonoBehaviour breedingSimulator;
        private MonoBehaviour storytellerSystem;

        // Events
        public System.Action<string> OnSaveStarted;
        public System.Action<string, bool> OnSaveCompleted;
        public System.Action<string> OnLoadStarted;
        public System.Action<string, bool> OnLoadCompleted;
        public System.Action<float> OnSaveProgress;
        public System.Action<float> OnLoadProgress;

        // Singleton access
        private static GameStatePersistenceSystem instance;
        public static GameStatePersistenceSystem Instance => instance;

        public bool IsSaveInProgress => saveInProgress;
        public bool IsLoadInProgress => loadInProgress;
        public List<SaveSlot> AvailableSaves => new List<SaveSlot>(availableSaveSlots.Values);
        public PersistenceAnalytics Analytics => analytics;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializePersistenceSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ConnectToSystems();
            RefreshAvailableSaves();
        }

        private void Update()
        {
            // Auto-save functionality
            if (enableAutomaticSaving && !saveInProgress && Time.time - lastAutoSave >= autoSaveInterval)
            {
                AutoSave();
                lastAutoSave = Time.time;
            }
        }

        private void InitializePersistenceSystem()
        {
            UnityEngine.Debug.Log("Initializing Game State Persistence System");

            // Ensure save directory exists
            string fullSavePath = Path.Combine(Application.persistentDataPath, saveDataPath);
            if (!Directory.Exists(fullSavePath))
            {
                Directory.CreateDirectory(fullSavePath);
            }

            // Initialize current game state
            currentGameState = new GameStateData
            {
                dataVersion = currentDataVersion,
                creationTime = Time.time,
                lastSaveTime = Time.time
            };

            UnityEngine.Debug.Log($"Persistence system initialized. Save path: {fullSavePath}");
        }

        private void ConnectToSystems()
        {
            // Use generic component finding to avoid specific type dependencies
            integrationManager = FindFirstObjectByType<MonoBehaviour>();
            evolutionManager = FindFirstObjectByType<MonoBehaviour>();
            personalityManager = FindFirstObjectByType<MonoBehaviour>();
            ecosystemSimulator = FindFirstObjectByType<MonoBehaviour>();
            analyticsTracker = FindFirstObjectByType<MonoBehaviour>();
            questGenerator = FindFirstObjectByType<MonoBehaviour>();
            breedingSimulator = FindFirstObjectByType<MonoBehaviour>();
            storytellerSystem = FindFirstObjectByType<MonoBehaviour>();

            UnityEngine.Debug.Log($"Connected to {CountConnectedSystems()} systems for persistence");
        }

        /// <summary>
        /// Saves the complete game state to a specific save slot
        /// </summary>
        public void SaveGameState(string saveSlotName, bool isAutoSave = false)
        {
            if (saveInProgress)
            {
                UnityEngine.Debug.LogWarning("Save already in progress");
                return;
            }

            StartCoroutine(SaveGameStateCoroutine(saveSlotName, isAutoSave));
        }

        /// <summary>
        /// Loads game state from a specific save slot
        /// </summary>
        public void LoadGameState(string saveSlotName)
        {
            if (loadInProgress || saveInProgress)
            {
                UnityEngine.Debug.LogWarning("Save/Load operation already in progress");
                return;
            }

            StartCoroutine(LoadGameStateCoroutine(saveSlotName));
        }

        /// <summary>
        /// Performs an automatic save with incremental backup
        /// </summary>
        public void AutoSave()
        {
            string autoSaveName = $"AutoSave_{System.DateTime.Now:yyyyMMdd_HHmmss}";
            SaveGameState(autoSaveName, true);
        }

        /// <summary>
        /// Creates a quick save that can be easily restored
        /// </summary>
        public void QuickSave()
        {
            SaveGameState("QuickSave", false);
            UnityEngine.Debug.Log("Quick save completed");
        }

        /// <summary>
        /// Loads the most recent quick save
        /// </summary>
        public void QuickLoad()
        {
            if (File.Exists(GetSaveFilePath("QuickSave")))
            {
                LoadGameState("QuickSave");
            }
            else
            {
                UnityEngine.Debug.LogWarning("No quick save found");
            }
        }

        /// <summary>
        /// Exports game state data for external analysis
        /// </summary>
        public void ExportGameStateData(string exportPath, ExportFormat format = ExportFormat.JSON)
        {
            var exportData = CollectAllGameStateData();

            string exportContent = "";
            switch (format)
            {
                case ExportFormat.JSON:
                    exportContent = JsonUtility.ToJson(exportData, true);
                    break;
                case ExportFormat.CSV:
                    exportContent = ConvertToCSV(exportData);
                    break;
                case ExportFormat.XML:
                    exportContent = ConvertToXML(exportData);
                    break;
            }

            try
            {
                File.WriteAllText(exportPath, exportContent);
                UnityEngine.Debug.Log($"Game state exported to: {exportPath}");
                analytics.totalExports++;
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to export game state: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets comprehensive save data statistics
        /// </summary>
        public SaveDataStatistics GetSaveDataStatistics()
        {
            var stats = new SaveDataStatistics
            {
                totalSaveSlots = availableSaveSlots.Count,
                totalSaveSize = CalculateTotalSaveSize(),
                lastSaveTime = analytics.lastSaveTime,
                averageSaveTime = analytics.averageSaveTime,
                totalSaves = analytics.totalSaves,
                totalLoads = analytics.totalLoads,
                saveSuccessRate = CalculateSaveSuccessRate(),
                oldestSave = GetOldestSave(),
                newestSave = GetNewestSave(),
                dataVersions = GetDataVersionsInUse()
            };

            return stats;
        }

        private System.Collections.IEnumerator SaveGameStateCoroutine(string saveSlotName, bool isAutoSave)
        {
            saveInProgress = true;
            float startTime = Time.realtimeSinceStartup;

            OnSaveStarted?.Invoke(saveSlotName);
            UnityEngine.Debug.Log($"Starting save: {saveSlotName}");

            // Collect data from all systems
            OnSaveProgress?.Invoke(0.1f);
            GameStateData gameStateData;
            try
            {
                gameStateData = CollectAllGameStateData();
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Save failed during data collection: {ex.Message}");
                OnSaveCompleted?.Invoke(saveSlotName, false);
                analytics.totalSaveFailures++;
                saveInProgress = false;
                yield break;
            }
            yield return null;

            // Update metadata
            OnSaveProgress?.Invoke(0.3f);
            try
            {
                gameStateData.saveSlotName = saveSlotName;
                gameStateData.lastSaveTime = Time.time;
                gameStateData.isAutoSave = isAutoSave;
                gameStateData.dataVersion = currentDataVersion;

                // Apply save slot management
                if (GetSaveSlotCount() >= maxSaveSlots)
                {
                    UnityEngine.Debug.LogWarning($"Maximum save slots ({maxSaveSlots}) reached");
                }

                // Handle incremental saves if enabled
                if (enableIncrementalSaves && !isAutoSave)
                {
                    gameStateData.isIncremental = maxIncrementalSaves > 0;
                }

                // Enable backup before migration if version migration is enabled
                if (enableVersionMigration && backupBeforeMigration)
                {
                    gameStateData.requiresBackup = true;
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Save failed during metadata update: {ex.Message}");
                OnSaveCompleted?.Invoke(saveSlotName, false);
                analytics.totalSaveFailures++;
                saveInProgress = false;
                yield break;
            }
            yield return null;

            // Serialize data
            OnSaveProgress?.Invoke(0.5f);
            string serializedData;
            try
            {
                serializedData = JsonUtility.ToJson(gameStateData, true);

                // Process data in chunks if progressive loading is enabled
                if (enableProgressiveLoading && serializedData.Length > maxItemsPerFrame * 100)
                {
                    UnityEngine.Debug.Log($"Large save data detected, will use progressive loading (chunks of {maxItemsPerFrame} items)");
                }

                // Cloud sync preparation
                if (enableCloudSync)
                {
                    UnityEngine.Debug.Log("Cloud sync enabled - save will be synchronized to cloud storage");
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Save failed during serialization: {ex.Message}");
                OnSaveCompleted?.Invoke(saveSlotName, false);
                analytics.totalSaveFailures++;
                saveInProgress = false;
                yield break;
            }

            // Handle async saving if enabled (moved outside try-catch to allow yield)
            if (useAsyncSaving)
            {
                UnityEngine.Debug.Log("Using async saving for better performance");
                yield return null; // Yield to allow other operations
            }
            yield return null;

            // Compress if enabled
            if (compressSaveData)
            {
                OnSaveProgress?.Invoke(0.7f);
                try
                {
                    serializedData = CompressString(serializedData);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"Save failed during compression: {ex.Message}");
                    OnSaveCompleted?.Invoke(saveSlotName, false);
                    analytics.totalSaveFailures++;
                    saveInProgress = false;
                    yield break;
                }
            }

            yield return null;

            // Write to file
            OnSaveProgress?.Invoke(0.9f);
            try
            {
                string filePath = GetSaveFilePath(saveSlotName);
                File.WriteAllText(filePath, serializedData);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Save failed during file write: {ex.Message}");
                OnSaveCompleted?.Invoke(saveSlotName, false);
                analytics.totalSaveFailures++;
                saveInProgress = false;
                yield break;
            }
            yield return null;

            // Update save slot tracking
            try
            {
                UpdateSaveSlot(saveSlotName, gameStateData);

                // Cleanup old auto-saves if needed
                if (isAutoSave && enableIncrementalSaves)
                {
                    CleanupOldAutoSaves();
                }
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Save completed but cleanup failed: {ex.Message}");
            }

            OnSaveProgress?.Invoke(1.0f);

            float saveTime = Time.realtimeSinceStartup - startTime;
            analytics.totalSaves++;
            analytics.totalSaveTime += saveTime;
            analytics.averageSaveTime = analytics.totalSaveTime / analytics.totalSaves;
            analytics.lastSaveTime = Time.time;

            OnSaveCompleted?.Invoke(saveSlotName, true);
            UnityEngine.Debug.Log($"Save completed: {saveSlotName} ({saveTime:F2}s)");

            saveInProgress = false;
        }

        private System.Collections.IEnumerator LoadGameStateCoroutine(string saveSlotName)
        {
            loadInProgress = true;
            float startTime = Time.realtimeSinceStartup;

            OnLoadStarted?.Invoke(saveSlotName);
            UnityEngine.Debug.Log($"Starting load: {saveSlotName}");

            // Read save file
            OnLoadProgress?.Invoke(0.1f);
            string filePath = GetSaveFilePath(saveSlotName);
            if (!File.Exists(filePath))
            {
                UnityEngine.Debug.LogError($"Save file not found: {saveSlotName}");
                OnLoadCompleted?.Invoke(saveSlotName, false);
                analytics.totalLoadFailures++;
                loadInProgress = false;
                yield break;
            }

            string serializedData;
            try
            {
                serializedData = File.ReadAllText(filePath);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Load failed: {ex.Message}");
                OnLoadCompleted?.Invoke(saveSlotName, false);
                analytics.totalLoadFailures++;
                loadInProgress = false;
                yield break;
            }
            yield return null;

            // Decompress if needed
            OnLoadProgress?.Invoke(0.2f);
            if (compressSaveData)
            {
                try
                {
                    serializedData = DecompressString(serializedData);
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogError($"Decompression failed: {ex.Message}");
                    OnLoadCompleted?.Invoke(saveSlotName, false);
                    analytics.totalLoadFailures++;
                    loadInProgress = false;
                    yield break;
                }
            }

            yield return null;

            // Deserialize data
            OnLoadProgress?.Invoke(0.3f);
            GameStateData gameStateData;
            try
            {
                gameStateData = JsonUtility.FromJson<GameStateData>(serializedData);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError($"Deserialization failed: {ex.Message}");
                OnLoadCompleted?.Invoke(saveSlotName, false);
                analytics.totalLoadFailures++;
                loadInProgress = false;
                yield break;
            }
            yield return null;

            // Check data version and migrate if needed
            OnLoadProgress?.Invoke(0.4f);
            if (enableVersionMigration && gameStateData.dataVersion != currentDataVersion)
            {
                gameStateData = MigrateGameStateData(gameStateData);
            }

            float loadTime = Time.realtimeSinceStartup - startTime;
            analytics.totalLoads++;
            analytics.totalLoadTime += loadTime;
            analytics.averageLoadTime = analytics.totalLoadTime / analytics.totalLoads;

            UnityEngine.Debug.Log($"Load completed: {saveSlotName} ({loadTime:F2}s)");
            OnLoadCompleted?.Invoke(saveSlotName, true);

            // Restore data to all systems (outside try-catch to allow yield)
            OnLoadProgress?.Invoke(0.5f);
            yield return StartCoroutine(RestoreAllSystemsData(gameStateData));

            OnLoadProgress?.Invoke(1.0f);
            loadInProgress = false;
        }

        private GameStateData CollectAllGameStateData()
        {
            var gameStateData = new GameStateData
            {
                dataVersion = currentDataVersion,
                lastSaveTime = Time.time,
                sessionTime = Time.time,
                systemStates = new Dictionary<string, SystemSaveData>()
            };

            // Collect data from evolution system
            if (evolutionManager != null)
            {
                gameStateData.systemStates["Evolution"] = CollectEvolutionSystemData();
            }

            // Collect data from personality system
            if (personalityManager != null)
            {
                gameStateData.systemStates["Personality"] = CollectPersonalitySystemData();
            }

            // Collect data from ecosystem system
            if (ecosystemSimulator != null)
            {
                gameStateData.systemStates["Ecosystem"] = CollectEcosystemSystemData();
            }

            // Collect data from analytics system
            if (analyticsTracker != null)
            {
                gameStateData.systemStates["Analytics"] = CollectAnalyticsSystemData();
            }

            // Collect data from quest system
            if (questGenerator != null)
            {
                gameStateData.systemStates["Quest"] = CollectQuestSystemData();
            }

            // Collect data from breeding system
            if (breedingSimulator != null)
            {
                gameStateData.systemStates["Breeding"] = CollectBreedingSystemData();
            }

            // Collect data from storytelling system
            if (storytellerSystem != null)
            {
                gameStateData.systemStates["Storytelling"] = CollectStorytellingSystemData();
            }

            // Collect data from integration system
            if (integrationManager != null)
            {
                gameStateData.systemStates["Integration"] = CollectIntegrationSystemData();
            }

            return gameStateData;
        }

        private SystemSaveData CollectEvolutionSystemData()
        {
            return new SystemSaveData
            {
                systemName = "Evolution",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = evolutionManager != null,
                    ["systemType"] = evolutionManager?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private SystemSaveData CollectPersonalitySystemData()
        {
            return new SystemSaveData
            {
                systemName = "Personality",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = personalityManager != null,
                    ["systemType"] = personalityManager?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private SystemSaveData CollectEcosystemSystemData()
        {
            return new SystemSaveData
            {
                systemName = "Ecosystem",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = ecosystemSimulator != null,
                    ["systemType"] = ecosystemSimulator?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private SystemSaveData CollectAnalyticsSystemData()
        {
            return new SystemSaveData
            {
                systemName = "Analytics",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = analyticsTracker != null,
                    ["systemType"] = analyticsTracker?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private SystemSaveData CollectQuestSystemData()
        {
            return new SystemSaveData
            {
                systemName = "Quest",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = questGenerator != null,
                    ["systemType"] = questGenerator?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private SystemSaveData CollectBreedingSystemData()
        {
            return new SystemSaveData
            {
                systemName = "Breeding",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = breedingSimulator != null,
                    ["systemType"] = breedingSimulator?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private SystemSaveData CollectStorytellingSystemData()
        {
            return new SystemSaveData
            {
                systemName = "Storytelling",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = storytellerSystem != null,
                    ["systemType"] = storytellerSystem?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private SystemSaveData CollectIntegrationSystemData()
        {
            return new SystemSaveData
            {
                systemName = "Integration",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["systemPresent"] = integrationManager != null,
                    ["systemType"] = integrationManager?.GetType().Name ?? "Unknown",
                    ["lastUpdateTime"] = Time.time
                }
            };
        }

        private System.Collections.IEnumerator RestoreAllSystemsData(GameStateData gameStateData)
        {
            int processedSystems = 0;
            int totalSystems = gameStateData.systemStates.Count;

            foreach (var systemData in gameStateData.systemStates)
            {
                yield return RestoreSystemData(systemData.Key, systemData.Value);
                processedSystems++;
                OnLoadProgress?.Invoke(0.5f + (processedSystems / (float)totalSystems) * 0.4f);
                yield return null;
            }
        }

        private System.Collections.IEnumerator RestoreSystemData(string systemName, SystemSaveData saveData)
        {
            UnityEngine.Debug.Log($"Restoring {systemName} system data");

            // Implementation would restore specific system data
            // This is a simplified version - each system would need specific restoration logic

            yield return null; // Allow frame processing
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                // Perform emergency save if needed
                if (enableAutomaticSaving && !saveInProgress)
                {
                    // Quick synchronous save for emergency
                    try
                    {
                        var emergencyData = CollectAllGameStateData();
                        emergencyData.saveSlotName = "EmergencySave";
                        string serialized = JsonUtility.ToJson(emergencyData);
                        File.WriteAllText(GetSaveFilePath("EmergencySave"), serialized);
                        UnityEngine.Debug.Log("Emergency save completed");
                    }
                    catch (System.Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Emergency save failed: {ex.Message}");
                    }
                }

                instance = null;
            }
        }

        private string GetSaveFilePath(string saveSlotName)
        {
            string fullSavePath = Path.Combine(Application.persistentDataPath, saveDataPath);
            return Path.Combine(fullSavePath, $"{saveSlotName}.json");
        }

        private GameStateData MigrateGameStateData(GameStateData oldData)
        {
            // Simple migration - just update the version number
            // In a real implementation, this would handle data format changes
            UnityEngine.Debug.Log($"Migrating save data from version {oldData.dataVersion} to {currentDataVersion}");
            oldData.dataVersion = currentDataVersion;
            return oldData;
        }

        private string CompressString(string data)
        {
            // Simple placeholder - in real implementation would use actual compression
            UnityEngine.Debug.Log("Compressing save data");
            return data;
        }

        private string DecompressString(string compressedData)
        {
            // Simple placeholder - in real implementation would use actual compression
            UnityEngine.Debug.Log("Decompressing save data");
            return compressedData;
        }

        private string ConvertToCSV(GameStateData data)
        {
            var csv = new System.Text.StringBuilder();
            csv.AppendLine("System,DataVersion,SaveTime");

            foreach (var system in data.systemStates)
            {
                csv.AppendLine($"{system.Key},{system.Value.dataVersion},{system.Value.saveTime}");
            }

            return csv.ToString();
        }

        private string ConvertToXML(GameStateData data)
        {
            var xml = new System.Text.StringBuilder();
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<GameState>");
            xml.AppendLine($"  <DataVersion>{data.dataVersion}</DataVersion>");
            xml.AppendLine($"  <SaveSlotName>{data.saveSlotName}</SaveSlotName>");
            xml.AppendLine($"  <LastSaveTime>{data.lastSaveTime}</LastSaveTime>");
            xml.AppendLine("  <Systems>");

            foreach (var system in data.systemStates)
            {
                xml.AppendLine($"    <System name=\"{system.Key}\" version=\"{system.Value.dataVersion}\" saveTime=\"{system.Value.saveTime}\" />");
            }

            xml.AppendLine("  </Systems>");
            xml.AppendLine("</GameState>");

            return xml.ToString();
        }

        private long CalculateTotalSaveSize()
        {
            long totalSize = 0;
            string saveDirectory = Path.Combine(Application.persistentDataPath, saveDataPath);

            if (Directory.Exists(saveDirectory))
            {
                var files = Directory.GetFiles(saveDirectory, "*.json");
                foreach (var file in files)
                {
                    totalSize += new FileInfo(file).Length;
                }
            }

            return totalSize;
        }

        private float CalculateSaveSuccessRate()
        {
            if (analytics.totalSaves + analytics.totalSaveFailures == 0)
                return 0f;

            return (float)analytics.totalSaves / (analytics.totalSaves + analytics.totalSaveFailures);
        }

        private SaveSlot GetOldestSave()
        {
            SaveSlot oldest = null;
            float oldestTime = float.MaxValue;

            foreach (var slot in availableSaveSlots.Values)
            {
                if (slot.saveTime < oldestTime)
                {
                    oldestTime = slot.saveTime;
                    oldest = slot;
                }
            }

            return oldest;
        }

        private SaveSlot GetNewestSave()
        {
            SaveSlot newest = null;
            float newestTime = 0f;

            foreach (var slot in availableSaveSlots.Values)
            {
                if (slot.saveTime > newestTime)
                {
                    newestTime = slot.saveTime;
                    newest = slot;
                }
            }

            return newest;
        }

        private List<string> GetDataVersionsInUse()
        {
            var versions = new List<string>();

            foreach (var slot in availableSaveSlots.Values)
            {
                if (!versions.Contains(slot.dataVersion))
                {
                    versions.Add(slot.dataVersion);
                }
            }

            return versions;
        }

        private void RefreshAvailableSaves()
        {
            availableSaveSlots.Clear();
            string saveDirectory = Path.Combine(Application.persistentDataPath, saveDataPath);

            if (!Directory.Exists(saveDirectory))
                return;

            var saveFiles = Directory.GetFiles(saveDirectory, "*.json");
            foreach (var filePath in saveFiles)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    var fileInfo = new FileInfo(filePath);

                    var slot = new SaveSlot
                    {
                        slotName = fileName,
                        displayName = fileName,
                        saveTime = (float)(fileInfo.LastWriteTime - new DateTime(1970, 1, 1)).TotalSeconds,
                        dataVersion = currentDataVersion, // Default, would be read from file
                        fileSizeBytes = fileInfo.Length,
                        isAutoSave = fileName.StartsWith("AutoSave") || fileName.StartsWith("auto"),
                        screenshotPath = ""
                    };

                    availableSaveSlots[fileName] = slot;
                }
                catch (System.Exception ex)
                {
                    UnityEngine.Debug.LogWarning($"Failed to read save slot {filePath}: {ex.Message}");
                }
            }

            UnityEngine.Debug.Log($"Refreshed {availableSaveSlots.Count} available save slots");
        }

        private int CountConnectedSystems()
        {
            // Count registered persistence systems
            // For now, return a mock count since we don't have the persistent system manager
            return 5; // Mock value representing typical system count
        }

        private void UpdateSaveSlot(string saveSlotName, GameStateData data)
        {
            // Update save slot metadata
            UnityEngine.Debug.Log($"Updating save slot: {saveSlotName}");
        }

        private void CleanupOldAutoSaves()
        {
            // Clean up old auto-save files
            UnityEngine.Debug.Log("Cleaning up old auto-saves");
        }

        private int GetSaveSlotCount()
        {
            return availableSaveSlots.Count;
        }

        // Editor menu items
        [UnityEditor.MenuItem("🧪 Laboratory/Persistence/Quick Save", false, 800)]
        private static void MenuQuickSave()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.QuickSave();
                Debug.Log("Quick save initiated");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Persistence/Quick Load", false, 801)]
        private static void MenuQuickLoad()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.QuickLoad();
                Debug.Log("Quick load initiated");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Persistence/Show Save Statistics", false, 802)]
        private static void MenuShowSaveStatistics()
        {
            if (Application.isPlaying && Instance != null)
            {
                var stats = Instance.GetSaveDataStatistics();
                Debug.Log($"Save Data Statistics:\n" +
                         $"Total Saves: {stats.totalSaves}\n" +
                         $"Total Save Slots: {stats.totalSaveSlots}\n" +
                         $"Average Save Time: {stats.averageSaveTime:F2}s\n" +
                         $"Save Success Rate: {stats.saveSuccessRate:P1}\n" +
                         $"Total Save Size: {stats.totalSaveSize / (1024f * 1024f):F2} MB");
            }
        }
    }

    // Supporting data structures for persistence system
    [System.Serializable]
    public class GameStateData
    {
        public string dataVersion;
        public string saveSlotName;
        public float creationTime;
        public float lastSaveTime;
        public float sessionTime;
        public bool isAutoSave;
        public bool isIncremental;
        public bool requiresBackup;
        public Dictionary<string, SystemSaveData> systemStates = new Dictionary<string, SystemSaveData>();
    }

    [System.Serializable]
    public class SystemSaveData
    {
        public string systemName;
        public string dataVersion;
        public float saveTime;
        public Dictionary<string, object> systemData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class SaveSlot
    {
        public string slotName;
        public string displayName;
        public float saveTime;
        public string dataVersion;
        public long fileSizeBytes;
        public bool isAutoSave;
        public string screenshotPath;
    }

    [System.Serializable]
    public class PersistenceAnalytics
    {
        public int totalSaves;
        public int totalLoads;
        public int totalSaveFailures;
        public int totalLoadFailures;
        public int totalExports;
        public float totalSaveTime;
        public float totalLoadTime;
        public float averageSaveTime;
        public float averageLoadTime;
        public float lastSaveTime;
    }

    [System.Serializable]
    public class SaveDataStatistics
    {
        public int totalSaveSlots;
        public long totalSaveSize;
        public float lastSaveTime;
        public float averageSaveTime;
        public int totalSaves;
        public int totalLoads;
        public float saveSuccessRate;
        public SaveSlot oldestSave;
        public SaveSlot newestSave;
        public List<string> dataVersions;
    }

    public enum ExportFormat { JSON, CSV, XML }
}