using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Laboratory.Core.Debug;
using Laboratory.Core.Integration;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.AI.Personality;
using Laboratory.Systems.Ecosystem;
using Laboratory.Systems.Analytics;
using Laboratory.Systems.Quests;
using Laboratory.Systems.Breeding;
using Laboratory.Systems.Storytelling;

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
        [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
        [SerializeField] private int maxSaveSlots = 10;
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
        private List<SaveSlot> availableSaveSlots = new List<SaveSlot>();

        // Persistence tracking
        private float lastAutoSave;
        private bool saveInProgress;
        private bool loadInProgress;
        private PersistenceAnalytics analytics = new PersistenceAnalytics();

        // Connected systems
        private SystemIntegrationManager integrationManager;
        private GeneticEvolutionManager evolutionManager;
        private CreaturePersonalityManager personalityManager;
        private DynamicEcosystemSimulator ecosystemSimulator;
        private PlayerAnalyticsTracker analyticsTracker;
        private ProceduralQuestGenerator questGenerator;
        private AdvancedBreedingSimulator breedingSimulator;
        private AIStorytellerSystem storytellerSystem;

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
        public List<SaveSlot> AvailableSaves => availableSaveSlots;
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
            DebugManager.LogInfo("Initializing Game State Persistence System");

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

            DebugManager.LogInfo($"Persistence system initialized. Save path: {fullSavePath}");
        }

        private void ConnectToSystems()
        {
            integrationManager = SystemIntegrationManager.Instance;
            evolutionManager = GeneticEvolutionManager.Instance;
            personalityManager = CreaturePersonalityManager.Instance;
            ecosystemSimulator = DynamicEcosystemSimulator.Instance;
            analyticsTracker = PlayerAnalyticsTracker.Instance;
            questGenerator = ProceduralQuestGenerator.Instance;
            breedingSimulator = AdvancedBreedingSimulator.Instance;
            storytellerSystem = AIStorytellerSystem.Instance;

            DebugManager.LogInfo($"Connected to {CountConnectedSystems()} systems for persistence");
        }

        /// <summary>
        /// Saves the complete game state to a specific save slot
        /// </summary>
        public void SaveGameState(string saveSlotName, bool isAutoSave = false)
        {
            if (saveInProgress)
            {
                DebugManager.LogWarning("Save already in progress");
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
                DebugManager.LogWarning("Save/Load operation already in progress");
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
            DebugManager.LogInfo("Quick save completed");
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
                DebugManager.LogWarning("No quick save found");
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
                DebugManager.LogInfo($"Game state exported to: {exportPath}");
                analytics.totalExports++;
            }
            catch (System.Exception ex)
            {
                DebugManager.LogError($"Failed to export game state: {ex.Message}");
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
            DebugManager.LogInfo($"Starting save: {saveSlotName}");

            try
            {
                // Collect data from all systems
                OnSaveProgress?.Invoke(0.1f);
                var gameStateData = CollectAllGameStateData();
                yield return null;

                // Update metadata
                OnSaveProgress?.Invoke(0.3f);
                gameStateData.saveSlotName = saveSlotName;
                gameStateData.lastSaveTime = Time.time;
                gameStateData.isAutoSave = isAutoSave;
                gameStateData.dataVersion = currentDataVersion;
                yield return null;

                // Serialize data
                OnSaveProgress?.Invoke(0.5f);
                string serializedData = JsonUtility.ToJson(gameStateData, true);
                yield return null;

                // Compress if enabled
                if (compressSaveData)
                {
                    OnSaveProgress?.Invoke(0.7f);
                    serializedData = CompressString(serializedData);
                    yield return null;
                }

                // Write to file
                OnSaveProgress?.Invoke(0.9f);
                string filePath = GetSaveFilePath(saveSlotName);
                File.WriteAllText(filePath, serializedData);
                yield return null;

                // Update save slot tracking
                UpdateSaveSlot(saveSlotName, gameStateData);

                // Cleanup old auto-saves if needed
                if (isAutoSave && enableIncrementalSaves)
                {
                    CleanupOldAutoSaves();
                }

                OnSaveProgress?.Invoke(1.0f);

                float saveTime = Time.realtimeSinceStartup - startTime;
                analytics.totalSaves++;
                analytics.totalSaveTime += saveTime;
                analytics.averageSaveTime = analytics.totalSaveTime / analytics.totalSaves;
                analytics.lastSaveTime = Time.time;

                OnSaveCompleted?.Invoke(saveSlotName, true);
                DebugManager.LogInfo($"Save completed: {saveSlotName} ({saveTime:F2}s)");
            }
            catch (System.Exception ex)
            {
                DebugManager.LogError($"Save failed: {ex.Message}");
                OnSaveCompleted?.Invoke(saveSlotName, false);
                analytics.totalSaveFailures++;
            }
            finally
            {
                saveInProgress = false;
            }
        }

        private System.Collections.IEnumerator LoadGameStateCoroutine(string saveSlotName)
        {
            loadInProgress = true;
            float startTime = Time.realtimeSinceStartup;

            OnLoadStarted?.Invoke(saveSlotName);
            DebugManager.LogInfo($"Starting load: {saveSlotName}");

            try
            {
                // Read save file
                OnLoadProgress?.Invoke(0.1f);
                string filePath = GetSaveFilePath(saveSlotName);
                if (!File.Exists(filePath))
                {
                    throw new System.Exception($"Save file not found: {saveSlotName}");
                }

                string serializedData = File.ReadAllText(filePath);
                yield return null;

                // Decompress if needed
                OnLoadProgress?.Invoke(0.2f);
                if (compressSaveData)
                {
                    serializedData = DecompressString(serializedData);
                    yield return null;
                }

                // Deserialize data
                OnLoadProgress?.Invoke(0.3f);
                var gameStateData = JsonUtility.FromJson<GameStateData>(serializedData);
                yield return null;

                // Check data version and migrate if needed
                OnLoadProgress?.Invoke(0.4f);
                if (enableVersionMigration && gameStateData.dataVersion != currentDataVersion)
                {
                    gameStateData = MigrateGameStateData(gameStateData);
                    yield return null;
                }

                // Restore data to all systems
                OnLoadProgress?.Invoke(0.5f);
                yield return StartCoroutine(RestoreAllSystemsData(gameStateData));

                OnLoadProgress?.Invoke(1.0f);

                float loadTime = Time.realtimeSinceStartup - startTime;
                analytics.totalLoads++;
                analytics.totalLoadTime += loadTime;
                analytics.averageLoadTime = analytics.totalLoadTime / analytics.totalLoads;

                OnLoadCompleted?.Invoke(saveSlotName, true);
                DebugManager.LogInfo($"Load completed: {saveSlotName} ({loadTime:F2}s)");
            }
            catch (System.Exception ex)
            {
                DebugManager.LogError($"Load failed: {ex.Message}");
                OnLoadCompleted?.Invoke(saveSlotName, false);
                analytics.totalLoadFailures++;
            }
            finally
            {
                loadInProgress = false;
            }
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
            var report = evolutionManager.GenerateGlobalReport();

            return new SystemSaveData
            {
                systemName = "Evolution",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["populationReport"] = report,
                    ["totalPopulation"] = evolutionManager.TotalPopulationSize,
                    ["averageFitness"] = evolutionManager.AveragePopulationFitness
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
                    ["activePersonalities"] = personalityManager.ActivePersonalityCount,
                    ["systemEnabled"] = personalityManager.IsSystemEnabled
                }
            };
        }

        private SystemSaveData CollectEcosystemSystemData()
        {
            var stressAnalysis = ecosystemSimulator.AnalyzeEcosystemStress();

            return new SystemSaveData
            {
                systemName = "Ecosystem",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["overallHealth"] = ecosystemSimulator.OverallHealth.ToString(),
                    ["stressAnalysis"] = stressAnalysis,
                    ["analytics"] = ecosystemSimulator.Analytics
                }
            };
        }

        private SystemSaveData CollectAnalyticsSystemData()
        {
            var behaviorAnalysis = analyticsTracker.GetBehaviorAnalysis();
            var sessionAnalytics = analyticsTracker.GetSessionAnalytics();

            return new SystemSaveData
            {
                systemName = "Analytics",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["behaviorAnalysis"] = behaviorAnalysis,
                    ["sessionAnalytics"] = sessionAnalytics,
                    ["currentProfile"] = analyticsTracker.CurrentProfile
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
                    ["activeQuests"] = questGenerator.ActiveQuests.ToArray(),
                    ["completedQuests"] = questGenerator.CompletedQuests.ToArray(),
                    ["analytics"] = questGenerator.Analytics
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
                    ["activeSessions"] = breedingSimulator.ActiveSessions.ToArray(),
                    ["analytics"] = breedingSimulator.Analytics,
                    ["breedingInProgress"] = breedingSimulator.IsBreedingInProgress
                }
            };
        }

        private SystemSaveData CollectStorytellingSystemData()
        {
            var storyAnalysis = storytellerSystem.AnalyzeCurrentStories();

            return new SystemSaveData
            {
                systemName = "Storytelling",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["activeStories"] = storytellerSystem.ActiveStories.ToArray(),
                    ["storyHistory"] = storytellerSystem.StoryHistory.ToArray(),
                    ["storyAnalysis"] = storyAnalysis,
                    ["analytics"] = storytellerSystem.Analytics
                }
            };
        }

        private SystemSaveData CollectIntegrationSystemData()
        {
            var integrationReport = integrationManager.GenerateIntegrationReport();

            return new SystemSaveData
            {
                systemName = "Integration",
                dataVersion = currentDataVersion,
                saveTime = Time.time,
                systemData = new Dictionary<string, object>
                {
                    ["integrationReport"] = integrationReport,
                    ["overallHealth"] = integrationManager.OverallSystemHealth,
                    ["analytics"] = integrationManager.Analytics
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
            DebugManager.LogInfo($"Restoring {systemName} system data");

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
                        DebugManager.LogInfo("Emergency save completed");
                    }
                    catch (System.Exception ex)
                    {
                        DebugManager.LogError($"Emergency save failed: {ex.Message}");
                    }
                }

                instance = null;
            }
        }

        // Editor menu items
        [UnityEditor.MenuItem("Laboratory/Persistence/Quick Save", false, 800)]
        private static void MenuQuickSave()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.QuickSave();
                Debug.Log("Quick save initiated");
            }
        }

        [UnityEditor.MenuItem("Laboratory/Persistence/Quick Load", false, 801)]
        private static void MenuQuickLoad()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.QuickLoad();
                Debug.Log("Quick load initiated");
            }
        }

        [UnityEditor.MenuItem("Laboratory/Persistence/Show Save Statistics", false, 802)]
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