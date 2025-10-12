using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Chimera.Genetics;
using Laboratory.Subsystems.Ecosystem;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Save/Load Subsystem Manager for Project Chimera.
    /// Manages game state persistence, autosave, cloud sync, and data integrity.
    /// Handles genetic profiles, ecosystem data, player progress, and settings.
    /// </summary>
    public class SaveLoadSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private SaveLoadSubsystemConfig config;

        [Header("Systems")]
        [SerializeField] private SaveDataManager saveDataManager;
        [SerializeField] private AutoSaveManager autoSaveManager;
        [SerializeField] private CloudSyncManager cloudSyncManager;
        [SerializeField] private DataIntegrityManager integrityManager;

        [Header("Services")]
        [SerializeField] private bool enableAutosave = true;
        [SerializeField] private bool enableCloudSync = false;
        [SerializeField] private bool enableDataValidation = true;
        [SerializeField] private bool enableBackups = true;

        // Events
        public static event Action<SaveEvent> OnSaveStarted;
        public static event Action<SaveEvent> OnSaveCompleted;
        public static event Action<LoadEvent> OnLoadStarted;
        public static event Action<LoadEvent> OnLoadCompleted;
        public static event Action<SaveLoadError> OnSaveLoadError;
        public static event Action<CloudSyncEvent> OnCloudSyncEvent;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "SaveLoad";
        public float InitializationProgress { get; private set; }

        // Services
        public ISaveDataService SaveDataService => saveDataManager;
        public IAutoSaveService AutoSaveService => autoSaveManager;
        public ICloudSyncService CloudSyncService => cloudSyncManager;
        public IDataIntegrityService DataIntegrityService => integrityManager;

        // Current State
        private GameSaveData _currentGameState;
        private SaveLoadStatus _currentStatus = SaveLoadStatus.Idle;
        private readonly Dictionary<string, object> _pendingSaveData = new();

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && enableAutosave && _currentStatus == SaveLoadStatus.Idle)
            {
                _ = AutoSaveAsync("Application Pause");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && enableAutosave && _currentStatus == SaveLoadStatus.Idle)
            {
                _ = AutoSaveAsync("Application Focus Lost");
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign a SaveLoadSubsystemConfig.");
                return;
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.1f;

            // Initialize core components
            if (saveDataManager == null)
                saveDataManager = gameObject.AddComponent<SaveDataManager>();

            if (autoSaveManager == null)
                autoSaveManager = gameObject.AddComponent<AutoSaveManager>();

            if (cloudSyncManager == null)
                cloudSyncManager = gameObject.AddComponent<CloudSyncManager>();

            if (integrityManager == null)
                integrityManager = gameObject.AddComponent<DataIntegrityManager>();

            InitializationProgress = 0.3f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.4f;

                // Initialize save data manager
                await saveDataManager.InitializeAsync(config);
                InitializationProgress = 0.5f;

                // Initialize autosave manager
                await autoSaveManager.InitializeAsync(config);
                InitializationProgress = 0.6f;

                // Initialize cloud sync manager
                await cloudSyncManager.InitializeAsync(config);
                InitializationProgress = 0.7f;

                // Initialize data integrity manager
                await integrityManager.InitializeAsync(config);
                InitializationProgress = 0.8f;

                // Subscribe to events
                SubscribeToEvents();

                // Register services
                RegisterServices();

                // Initialize current game state
                await InitializeGameState();
                InitializationProgress = 0.95f;

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Save slots: {config.SaveConfig.MaxSaveSlots}, " +
                         $"Autosave: {enableAutosave}, Cloud: {enableCloudSync}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private async Task InitializeGameState()
        {
            // Try to load the most recent save
            var latestSave = await GetLatestSaveAsync();
            if (latestSave != null)
            {
                _currentGameState = latestSave;
                Debug.Log($"[{SubsystemName}] Loaded latest save: {latestSave.saveMetadata.saveName}");
            }
            else
            {
                // Create new game state
                _currentGameState = CreateNewGameState();
                Debug.Log($"[{SubsystemName}] Created new game state");
            }
        }

        private void SubscribeToEvents()
        {
            if (saveDataManager != null)
            {
                saveDataManager.OnSaveCompleted += HandleSaveCompleted;
                saveDataManager.OnLoadCompleted += HandleLoadCompleted;
                saveDataManager.OnSaveLoadError += HandleSaveLoadError;
            }

            if (autoSaveManager != null)
            {
                autoSaveManager.OnAutoSaveTriggered += HandleAutoSaveTriggered;
            }

            if (cloudSyncManager != null)
            {
                cloudSyncManager.OnCloudSyncEvent += HandleCloudSyncEvent;
            }
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<ISaveDataService>(saveDataManager);
                ServiceContainer.Instance.Register<IAutoSaveService>(autoSaveManager);
                ServiceContainer.Instance.Register<ICloudSyncService>(cloudSyncManager);
                ServiceContainer.Instance.Register<IDataIntegrityService>(integrityManager);
                ServiceContainer.Instance.Register<SaveLoadSubsystemManager>(this);
            }
        }

        #endregion

        #region Core Save/Load Operations

        /// <summary>
        /// Saves the current game state to a specific slot
        /// </summary>
        public async Task<bool> SaveGameAsync(int slotId, string saveName = null)
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[{SubsystemName}] Cannot save - subsystem not initialized");
                return false;
            }

            if (_currentStatus != SaveLoadStatus.Idle)
            {
                Debug.LogWarning($"[{SubsystemName}] Cannot save - operation in progress: {_currentStatus}");
                return false;
            }

            try
            {
                _currentStatus = SaveLoadStatus.Saving;

                var saveEvent = new SaveEvent
                {
                    slotId = slotId,
                    saveName = saveName ?? $"Save {slotId}",
                    saveType = SaveType.Manual,
                    timestamp = DateTime.UtcNow
                };

                OnSaveStarted?.Invoke(saveEvent);

                // Collect current game state
                await CollectCurrentGameState();

                // Update save metadata
                UpdateSaveMetadata(_currentGameState, saveEvent);

                // Validate data if enabled
                if (enableDataValidation)
                {
                    var validationResult = await integrityManager.ValidateGameDataAsync(_currentGameState);
                    if (!validationResult.isValid)
                    {
                        throw new InvalidOperationException($"Save data validation failed: {string.Join(", ", validationResult.errors)}");
                    }
                }

                // Create backup if enabled
                if (enableBackups)
                {
                    await CreateBackupAsync(slotId);
                }

                // Perform the save
                var saveResult = await saveDataManager.SaveGameDataAsync(slotId, _currentGameState);

                if (saveResult)
                {
                    saveEvent.isSuccessful = true;
                    OnSaveCompleted?.Invoke(saveEvent);

                    // Trigger cloud sync if enabled
                    if (enableCloudSync)
                    {
                        _ = cloudSyncManager.UploadSaveAsync(slotId, _currentGameState);
                    }

                    Debug.Log($"[{SubsystemName}] Game saved successfully to slot {slotId}");
                    return true;
                }
                else
                {
                    throw new InvalidOperationException("Save operation failed");
                }
            }
            catch (Exception ex)
            {
                var error = new SaveLoadError
                {
                    operation = SaveLoadOperation.Save,
                    slotId = slotId,
                    errorMessage = ex.Message,
                    exception = ex,
                    timestamp = DateTime.UtcNow
                };

                OnSaveLoadError?.Invoke(error);
                Debug.LogError($"[{SubsystemName}] Save failed: {ex.Message}");
                return false;
            }
            finally
            {
                _currentStatus = SaveLoadStatus.Idle;
            }
        }

        /// <summary>
        /// Loads a game state from a specific slot
        /// </summary>
        public async Task<bool> LoadGameAsync(int slotId)
        {
            if (!IsInitialized)
            {
                Debug.LogError($"[{SubsystemName}] Cannot load - subsystem not initialized");
                return false;
            }

            if (_currentStatus != SaveLoadStatus.Idle)
            {
                Debug.LogWarning($"[{SubsystemName}] Cannot load - operation in progress: {_currentStatus}");
                return false;
            }

            try
            {
                _currentStatus = SaveLoadStatus.Loading;

                var loadEvent = new LoadEvent
                {
                    slotId = slotId,
                    timestamp = DateTime.UtcNow
                };

                OnLoadStarted?.Invoke(loadEvent);

                // Load game data
                var gameData = await saveDataManager.LoadGameDataAsync(slotId);

                if (gameData == null)
                {
                    throw new InvalidOperationException($"No save data found in slot {slotId}");
                }

                // Validate data if enabled
                if (enableDataValidation)
                {
                    var validationResult = await integrityManager.ValidateGameDataAsync(gameData);
                    if (!validationResult.isValid)
                    {
                        Debug.LogWarning($"[{SubsystemName}] Save data validation failed, attempting repair...");
                        gameData = await integrityManager.RepairGameDataAsync(gameData);
                    }
                }

                // Apply the loaded state
                await ApplyLoadedGameState(gameData);

                _currentGameState = gameData;

                loadEvent.isSuccessful = true;
                loadEvent.saveName = gameData.saveMetadata.saveName;
                OnLoadCompleted?.Invoke(loadEvent);

                Debug.Log($"[{SubsystemName}] Game loaded successfully from slot {slotId}: {gameData.saveMetadata.saveName}");
                return true;
            }
            catch (Exception ex)
            {
                var error = new SaveLoadError
                {
                    operation = SaveLoadOperation.Load,
                    slotId = slotId,
                    errorMessage = ex.Message,
                    exception = ex,
                    timestamp = DateTime.UtcNow
                };

                OnSaveLoadError?.Invoke(error);
                Debug.LogError($"[{SubsystemName}] Load failed: {ex.Message}");
                return false;
            }
            finally
            {
                _currentStatus = SaveLoadStatus.Idle;
            }
        }

        /// <summary>
        /// Gets information about available save slots
        /// </summary>
        public async Task<SaveSlotInfo[]> GetSaveSlotInfoAsync()
        {
            if (!IsInitialized)
                return new SaveSlotInfo[0];

            return await saveDataManager.GetSaveSlotInfoAsync();
        }

        /// <summary>
        /// Deletes a save slot
        /// </summary>
        public async Task<bool> DeleteSaveAsync(int slotId)
        {
            if (!IsInitialized)
                return false;

            try
            {
                var result = await saveDataManager.DeleteSaveAsync(slotId);

                if (result && enableCloudSync)
                {
                    // Also delete from cloud
                    await cloudSyncManager.DeleteCloudSaveAsync(slotId);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Delete save failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Performs an autosave operation
        /// </summary>
        public async Task<bool> AutoSaveAsync(string trigger = "Manual")
        {
            if (!enableAutosave || !IsInitialized)
                return false;

            // Use dedicated autosave slot
            var autoSaveSlot = config.SaveConfig.AutoSaveSlot;

            var saveEvent = new SaveEvent
            {
                slotId = autoSaveSlot,
                saveName = $"AutoSave - {DateTime.Now:yyyy-MM-dd HH:mm}",
                saveType = SaveType.Auto,
                trigger = trigger,
                timestamp = DateTime.UtcNow
            };

            // Use the main save logic but with autosave metadata
            return await SaveGameAsync(autoSaveSlot, saveEvent.saveName);
        }

        #endregion

        #region Game State Collection

        private async Task CollectCurrentGameState()
        {
            if (_currentGameState == null)
            {
                _currentGameState = CreateNewGameState();
            }

            // Clear existing data
            _currentGameState.geneticsData?.Clear();
            _currentGameState.ecosystemData = new EcosystemSaveData();
            _currentGameState.playerData = new PlayerSaveData();

            // Collect genetics data
            await CollectGeneticsData();

            // Collect ecosystem data
            await CollectEcosystemData();

            // Collect player data
            await CollectPlayerData();

            // Update metadata
            _currentGameState.saveMetadata.lastSaved = DateTime.UtcNow;
            _currentGameState.saveMetadata.gameVersion = Application.version;
            _currentGameState.saveMetadata.playTime += Time.realtimeSinceStartup;

            Debug.Log($"[{SubsystemName}] Game state collected - " +
                     $"Genetics: {_currentGameState.geneticsData.Count}, " +
                     $"Populations: {_currentGameState.ecosystemData.populations?.Count ?? 0}");
        }

        private async Task CollectGeneticsData()
        {
            // Get genetics service
            var geneticsService = ServiceContainer.Instance?.Resolve<Laboratory.Subsystems.Genetics.GeneticsSubsystemManager>();
            if (geneticsService == null)
                return;

            // Collect genetic profiles from the genetics database
            var databaseService = ServiceContainer.Instance?.Resolve<Laboratory.Subsystems.Genetics.IGeneticDatabase>();
            if (databaseService != null)
            {
                // This would require extending the database service to get all profiles
                // For now, we'll store what we can access
                Debug.Log($"[{SubsystemName}] Collecting genetics data...");
            }

            await Task.CompletedTask;
        }

        private async Task CollectEcosystemData()
        {
            // Get ecosystem service
            var ecosystemService = ServiceContainer.Instance?.Resolve<EcosystemSubsystemManager>();
            if (ecosystemService == null)
                return;

            var ecosystemData = new EcosystemSaveData
            {
                biomes = new List<BiomeData>(),
                populations = new List<PopulationData>(),
                currentWeather = ConvertToSaveWeatherData(ecosystemService.GetCurrentWeather()),
                conservationStatus = ConvertToSaveConservationStatus(ecosystemService.GetConservationStatus()),
                lastUpdate = DateTime.UtcNow
            };

            // Note: In a complete implementation, we'd need methods to extract all biome and population data
            // For now, we're setting up the structure

            _currentGameState.ecosystemData = ecosystemData;

            Debug.Log($"[{SubsystemName}] Collecting ecosystem data...");
            await Task.CompletedTask;
        }

        private async Task CollectPlayerData()
        {
            var playerData = new PlayerSaveData
            {
                playerId = SystemInfo.deviceUniqueIdentifier,
                playerName = "Player", // Would get from player profile
                level = 1,
                experience = 0,
                currency = 0,
                unlockedAchievements = new List<string>(),
                gameSettings = new Dictionary<string, object>(),
                lastPlayed = DateTime.UtcNow
            };

            _currentGameState.playerData = playerData;

            Debug.Log($"[{SubsystemName}] Collecting player data...");
            await Task.CompletedTask;
        }

        #endregion

        #region Game State Application

        private async Task ApplyLoadedGameState(GameSaveData gameData)
        {
            // Apply genetics data
            await ApplyGeneticsData(gameData.geneticsData);

            // Apply ecosystem data
            await ApplyEcosystemData(gameData.ecosystemData);

            // Apply player data
            await ApplyPlayerData(gameData.playerData);

            Debug.Log($"[{SubsystemName}] Game state applied successfully");
        }

        private async Task ApplyGeneticsData(Dictionary<string, object> geneticsData)
        {
            if (geneticsData == null || geneticsData.Count == 0)
                return;

            // Apply genetic profiles to the genetics system
            // This would require extending the genetics system to accept loaded data

            Debug.Log($"[{SubsystemName}] Applied genetics data: {geneticsData.Count} entries");
            await Task.CompletedTask;
        }

        private async Task ApplyEcosystemData(EcosystemSaveData ecosystemData)
        {
            if (ecosystemData == null)
                return;

            var ecosystemService = ServiceContainer.Instance?.Resolve<EcosystemSubsystemManager>();
            if (ecosystemService == null)
                return;

            // Apply weather
            if (ecosystemData.currentWeather != null)
            {
                ecosystemService.SetWeather(
                    ConvertToEcosystemWeatherType(ecosystemData.currentWeather.weatherType),
                    ecosystemData.currentWeather.intensity,
                    ecosystemData.currentWeather.remainingDuration);
            }

            // Apply biomes and populations would require extending the ecosystem service

            Debug.Log($"[{SubsystemName}] Applied ecosystem data");
            await Task.CompletedTask;
        }

        private async Task ApplyPlayerData(PlayerSaveData playerData)
        {
            if (playerData == null)
                return;

            // Apply player settings, progress, etc.
            // This would integrate with player profile systems

            Debug.Log($"[{SubsystemName}] Applied player data for {playerData.playerName}");
            await Task.CompletedTask;
        }

        #endregion

        #region Utility Methods

        private GameSaveData CreateNewGameState()
        {
            return new GameSaveData
            {
                saveMetadata = new SaveMetadata
                {
                    saveId = Guid.NewGuid().ToString(),
                    saveName = "New Game",
                    created = DateTime.UtcNow,
                    lastSaved = DateTime.UtcNow,
                    gameVersion = Application.version,
                    playTime = 0f
                },
                geneticsData = new Dictionary<string, object>(),
                ecosystemData = new EcosystemSaveData(),
                playerData = new PlayerSaveData
                {
                    playerId = SystemInfo.deviceUniqueIdentifier,
                    playerName = "Player",
                    lastPlayed = DateTime.UtcNow
                }
            };
        }

        private void UpdateSaveMetadata(GameSaveData gameData, SaveEvent saveEvent)
        {
            gameData.saveMetadata.saveName = saveEvent.saveName;
            gameData.saveMetadata.lastSaved = saveEvent.timestamp;
            gameData.saveMetadata.saveType = saveEvent.saveType;
        }

        private async Task<GameSaveData> GetLatestSaveAsync()
        {
            var slots = await GetSaveSlotInfoAsync();
            if (slots.Length == 0)
                return null;

            // Find the most recent save
            SaveSlotInfo latestSlot = null;
            foreach (var slot in slots)
            {
                if (slot.isOccupied && (latestSlot == null || slot.lastSaved > latestSlot.lastSaved))
                {
                    latestSlot = slot;
                }
            }

            if (latestSlot != null)
            {
                return await saveDataManager.LoadGameDataAsync(latestSlot.slotId);
            }

            return null;
        }

        private async Task CreateBackupAsync(int slotId)
        {
            try
            {
                await saveDataManager.CreateBackupAsync(slotId);
                Debug.Log($"[{SubsystemName}] Backup created for slot {slotId}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{SubsystemName}] Backup creation failed: {ex.Message}");
            }
        }

        #endregion

        #region Event Handlers

        private void HandleSaveCompleted(SaveEvent saveEvent)
        {
            Debug.Log($"[{SubsystemName}] Save completed: {saveEvent.saveName}");
        }

        private void HandleLoadCompleted(LoadEvent loadEvent)
        {
            Debug.Log($"[{SubsystemName}] Load completed: {loadEvent.saveName}");
        }

        private void HandleSaveLoadError(SaveLoadError error)
        {
            Debug.LogError($"[{SubsystemName}] {error.operation} error: {error.errorMessage}");
        }

        private void HandleAutoSaveTriggered(string trigger)
        {
            _ = AutoSaveAsync(trigger);
        }

        private void HandleCloudSyncEvent(CloudSyncEvent syncEvent)
        {
            Debug.Log($"[{SubsystemName}] Cloud sync: {syncEvent.operation} - {syncEvent.status}");
        }

        #endregion

        #region Type Conversion Helpers

        private Laboratory.Subsystems.SaveLoad.WeatherData ConvertToSaveWeatherData(Laboratory.Subsystems.Ecosystem.WeatherData ecosystemWeather)
        {
            return new Laboratory.Subsystems.SaveLoad.WeatherData
            {
                weatherType = ConvertWeatherType(ecosystemWeather.weatherType),
                intensity = ecosystemWeather.intensity,
                remainingDuration = ecosystemWeather.remainingDuration,
                windDirection = ecosystemWeather.windDirection,
                temperature = ecosystemWeather.temperature,
                humidity = ecosystemWeather.humidity,
                startTime = ecosystemWeather.startTime
            };
        }

        private Laboratory.Subsystems.SaveLoad.WeatherType ConvertWeatherType(Laboratory.Subsystems.Ecosystem.WeatherType ecosystemWeatherType)
        {
            return ecosystemWeatherType switch
            {
                Laboratory.Subsystems.Ecosystem.WeatherType.Sunny => Laboratory.Subsystems.SaveLoad.WeatherType.Clear,
                Laboratory.Subsystems.Ecosystem.WeatherType.Cloudy => Laboratory.Subsystems.SaveLoad.WeatherType.Cloudy,
                Laboratory.Subsystems.Ecosystem.WeatherType.Rainy => Laboratory.Subsystems.SaveLoad.WeatherType.Rain,
                Laboratory.Subsystems.Ecosystem.WeatherType.Stormy => Laboratory.Subsystems.SaveLoad.WeatherType.Storm,
                Laboratory.Subsystems.Ecosystem.WeatherType.Snowy => Laboratory.Subsystems.SaveLoad.WeatherType.Snow,
                Laboratory.Subsystems.Ecosystem.WeatherType.Foggy => Laboratory.Subsystems.SaveLoad.WeatherType.Fog,
                _ => Laboratory.Subsystems.SaveLoad.WeatherType.Clear
            };
        }

        private Laboratory.Subsystems.SaveLoad.ConservationStatus ConvertToSaveConservationStatus(Dictionary<string, Laboratory.Subsystems.Ecosystem.ConservationStatus> ecosystemConservationStatus)
        {
            // For now, return a default conservation status since the save type expects a single value, not a dictionary
            // In a complete implementation, this would need to be restructured to handle multiple species conservation statuses
            return Laboratory.Subsystems.SaveLoad.ConservationStatus.LeastConcern;
        }

        private Laboratory.Subsystems.Ecosystem.WeatherType ConvertToEcosystemWeatherType(Laboratory.Subsystems.SaveLoad.WeatherType saveWeatherType)
        {
            return saveWeatherType switch
            {
                Laboratory.Subsystems.SaveLoad.WeatherType.Clear => Laboratory.Subsystems.Ecosystem.WeatherType.Sunny,
                Laboratory.Subsystems.SaveLoad.WeatherType.Cloudy => Laboratory.Subsystems.Ecosystem.WeatherType.Cloudy,
                Laboratory.Subsystems.SaveLoad.WeatherType.Rain => Laboratory.Subsystems.Ecosystem.WeatherType.Rainy,
                Laboratory.Subsystems.SaveLoad.WeatherType.Storm => Laboratory.Subsystems.Ecosystem.WeatherType.Stormy,
                Laboratory.Subsystems.SaveLoad.WeatherType.Snow => Laboratory.Subsystems.Ecosystem.WeatherType.Snowy,
                Laboratory.Subsystems.SaveLoad.WeatherType.Fog => Laboratory.Subsystems.Ecosystem.WeatherType.Foggy,
                _ => Laboratory.Subsystems.Ecosystem.WeatherType.Sunny
            };
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            if (saveDataManager != null)
            {
                saveDataManager.OnSaveCompleted -= HandleSaveCompleted;
                saveDataManager.OnLoadCompleted -= HandleLoadCompleted;
                saveDataManager.OnSaveLoadError -= HandleSaveLoadError;
            }

            if (autoSaveManager != null)
            {
                autoSaveManager.OnAutoSaveTriggered -= HandleAutoSaveTriggered;
            }

            if (cloudSyncManager != null)
            {
                cloudSyncManager.OnCloudSyncEvent -= HandleCloudSyncEvent;
            }

            // Clear state
            _pendingSaveData.Clear();
            _currentGameState = null;

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Save")]
        private void TestSave()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Save/Load system not initialized");
                return;
            }

            _ = SaveGameAsync(1, $"Test Save - {DateTime.Now:HH:mm:ss}");
        }

        [ContextMenu("Test Load")]
        private void TestLoad()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Save/Load system not initialized");
                return;
            }

            _ = LoadGameAsync(1);
        }

        [ContextMenu("Print Save Slots")]
        private async void PrintSaveSlots()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Save/Load system not initialized");
                return;
            }

            var slots = await GetSaveSlotInfoAsync();
            foreach (var slot in slots)
            {
                Debug.Log($"Slot {slot.slotId}: {(slot.isOccupied ? slot.saveName : "Empty")} " +
                         $"(Last saved: {slot.lastSaved:yyyy-MM-dd HH:mm})");
            }
        }

        #endregion
    }
}