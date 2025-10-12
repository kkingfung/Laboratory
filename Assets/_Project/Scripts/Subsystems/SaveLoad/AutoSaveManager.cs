using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Events;

namespace Laboratory.Subsystems.SaveLoad
{
    /// <summary>
    /// Automatic save manager that handles timed autosaves and event-driven save triggers.
    /// Monitors gameplay events and system state to determine optimal save moments.
    /// </summary>
    public class AutoSaveManager : MonoBehaviour, IAutoSaveService
    {
        [Header("Configuration")]
        [SerializeField] private SaveLoadSubsystemConfig config;

        [Header("Auto Save State")]
        [SerializeField] private bool isAutoSaveEnabled = true;
        [SerializeField] private float autosaveInterval = 300f; // 5 minutes
        [SerializeField] private bool isPaused = false;

        [Header("Debug Info")]
        [SerializeField] private float timeSinceLastAutoSave;
        [SerializeField] private int totalAutoSaves;
        [SerializeField] private DateTime lastAutoSaveTime;

        // Properties
        public bool IsAutoSaveEnabled
        {
            get => isAutoSaveEnabled;
            set
            {
                isAutoSaveEnabled = value;
                if (value)
                {
                    StartAutoSave();
                }
                else
                {
                    StopAutoSave();
                }
            }
        }

        public float AutoSaveInterval
        {
            get => autosaveInterval;
            set
            {
                autosaveInterval = Mathf.Clamp(value, 30f, 1800f);
                if (config != null)
                {
                    // Update config if possible (in editor or runtime config changes)
                }
            }
        }

        // State
        private bool _isInitialized = false;
        private Coroutine _autoSaveCoroutine;
        private readonly Dictionary<string, Func<bool>> _autoSaveTriggers = new();
        private readonly Dictionary<string, DateTime> _triggerCooldowns = new();
        private ISaveDataService _saveDataService;

        // Events
        public event Action<string> OnAutoSaveTriggered;
        public event Action<SaveEvent> OnAutoSaveCompleted;

        #region Unity Lifecycle

        private void Update()
        {
            if (_isInitialized && isAutoSaveEnabled && !isPaused)
            {
                timeSinceLastAutoSave += Time.deltaTime;
                CheckEventBasedTriggers();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && config?.AutoSaveConfig.AutosaveOnPause == true)
            {
                _ = TriggerAutoSaveAsync("Application Pause");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && config?.AutoSaveConfig.AutosaveOnFocusLost == true)
            {
                _ = TriggerAutoSaveAsync("Application Focus Lost");
            }
        }

        #endregion

        #region Initialization

        public async Task InitializeAsync(SaveLoadSubsystemConfig configuration)
        {
            config = configuration;

            try
            {
                // Configure from settings
                ApplyConfiguration();

                // Get save data service
                _saveDataService = FindFirstObjectByType<SaveDataManager>();

                // Register for game events
                RegisterGameEventTriggers();

                // Register built-in triggers
                RegisterBuiltInTriggers();

                _isInitialized = true;
                Debug.Log($"[AutoSaveManager] Initialized - Interval: {autosaveInterval}s, Enabled: {isAutoSaveEnabled}");

                // Start autosave if enabled
                if (isAutoSaveEnabled)
                {
                    StartAutoSave();
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoSaveManager] Initialization failed: {ex.Message}");
                throw;
            }
        }

        private void ApplyConfiguration()
        {
            if (config?.AutoSaveConfig != null)
            {
                isAutoSaveEnabled = config.AutoSaveConfig.EnableAutosave;
                autosaveInterval = config.AutoSaveConfig.AutosaveInterval;
            }
        }

        private void RegisterGameEventTriggers()
        {
            // Subscribe to relevant game events
                // Subscribe to breeding events
                EventBus.Subscribe<CreatureBornEvent>(OnCreatureBorn);
                EventBus.Subscribe<BreedingSuccessEvent>(OnBreedingSuccess);

                // Subscribe to discovery events
                EventBus.Subscribe<DiscoveryMadeEvent>(OnDiscoveryMade);

                // Subscribe to ecosystem events
                EventBus.Subscribe<EcosystemChangedEvent>(OnEcosystemChanged);

                // Subscribe to scene change events
                EventBus.Subscribe<SceneChangeEvent>(OnSceneChange);
        }

        private void RegisterBuiltInTriggers()
        {
            // Register condition-based triggers
            RegisterAutoSaveTrigger("CreatureCountThreshold", () => CheckCreatureCountThreshold());
            RegisterAutoSaveTrigger("SignificantProgress", () => CheckSignificantProgress());
            RegisterAutoSaveTrigger("LowHealth", () => CheckLowHealthCondition());
        }

        #endregion

        #region Core AutoSave Operations

        public void StartAutoSave()
        {
            if (!_isInitialized || !isAutoSaveEnabled)
                return;

            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
            }

            _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
            isPaused = false;
            timeSinceLastAutoSave = 0f;

            Debug.Log($"[AutoSaveManager] Auto-save started with {autosaveInterval}s interval");
        }

        public void StopAutoSave()
        {
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
                _autoSaveCoroutine = null;
            }

            Debug.Log("[AutoSaveManager] Auto-save stopped");
        }

        public void PauseAutoSave()
        {
            isPaused = true;
            Debug.Log("[AutoSaveManager] Auto-save paused");
        }

        public void ResumeAutoSave()
        {
            isPaused = false;
            Debug.Log("[AutoSaveManager] Auto-save resumed");
        }

        public async Task<bool> TriggerAutoSaveAsync(string trigger = "Manual")
        {
            if (!_isInitialized || !isAutoSaveEnabled)
            {
                Debug.LogWarning($"[AutoSaveManager] Cannot auto-save: Initialized={_isInitialized}, Enabled={isAutoSaveEnabled}");
                return false;
            }

            // Check cooldown for this trigger
            if (IsOnCooldown(trigger))
            {
                Debug.Log($"[AutoSaveManager] Auto-save trigger '{trigger}' is on cooldown");
                return false;
            }

            try
            {
                OnAutoSaveTriggered?.Invoke(trigger);

                // Use the autosave slot from config
                var autoSaveSlot = config?.SaveConfig.AutoSaveSlot ?? 0;

                // Get the save/load manager to perform the save
                var saveLoadManager = FindFirstObjectByType<SaveLoadSubsystemManager>();
                if (saveLoadManager != null)
                {
                    var success = await saveLoadManager.AutoSaveAsync(trigger);

                    if (success)
                    {
                        // Reset timer and update statistics
                        timeSinceLastAutoSave = 0f;
                        totalAutoSaves++;
                        lastAutoSaveTime = DateTime.Now;

                        // Set cooldown for this trigger
                        SetTriggerCooldown(trigger);

                        var saveEvent = new SaveEvent
                        {
                            slotId = autoSaveSlot,
                            saveName = $"AutoSave - {DateTime.Now:HH:mm:ss}",
                            saveType = SaveType.Auto,
                            trigger = trigger,
                            isSuccessful = true,
                            timestamp = DateTime.UtcNow
                        };

                        OnAutoSaveCompleted?.Invoke(saveEvent);
                        Debug.Log($"[AutoSaveManager] Auto-save completed successfully (Trigger: {trigger})");
                    }

                    return success;
                }
                else
                {
                    Debug.LogError("[AutoSaveManager] SaveLoadSubsystemManager not found");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AutoSaveManager] Auto-save failed: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Trigger Management

        public void RegisterAutoSaveTrigger(string triggerName, Func<bool> condition)
        {
            if (string.IsNullOrEmpty(triggerName) || condition == null)
                return;

            _autoSaveTriggers[triggerName] = condition;
            Debug.Log($"[AutoSaveManager] Registered auto-save trigger: {triggerName}");
        }

        public void UnregisterAutoSaveTrigger(string triggerName)
        {
            if (_autoSaveTriggers.Remove(triggerName))
            {
                _triggerCooldowns.Remove(triggerName);
                Debug.Log($"[AutoSaveManager] Unregistered auto-save trigger: {triggerName}");
            }
        }

        private void CheckEventBasedTriggers()
        {
            foreach (var trigger in _autoSaveTriggers)
            {
                try
                {
                    if (!IsOnCooldown(trigger.Key) && trigger.Value())
                    {
                        _ = TriggerAutoSaveAsync(trigger.Key);
                        break; // Only trigger one auto-save per frame
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AutoSaveManager] Trigger '{trigger.Key}' evaluation failed: {ex.Message}");
                }
            }
        }

        private bool IsOnCooldown(string triggerName)
        {
            if (_triggerCooldowns.TryGetValue(triggerName, out var lastTriggered))
            {
                var cooldownDuration = GetTriggerCooldown(triggerName);
                return DateTime.Now - lastTriggered < cooldownDuration;
            }
            return false;
        }

        private void SetTriggerCooldown(string triggerName)
        {
            _triggerCooldowns[triggerName] = DateTime.Now;
        }

        private TimeSpan GetTriggerCooldown(string triggerName)
        {
            // Different cooldowns for different trigger types
            return triggerName switch
            {
                "Timer" => TimeSpan.FromSeconds(30),
                "CreatureBorn" => TimeSpan.FromMinutes(2),
                "BreedingSuccess" => TimeSpan.FromMinutes(3),
                "Discovery" => TimeSpan.FromMinutes(1),
                "EcosystemChange" => TimeSpan.FromMinutes(5),
                "SceneChange" => TimeSpan.FromSeconds(10),
                _ => TimeSpan.FromMinutes(1)
            };
        }

        #endregion

        #region Event Handlers

        private void OnCreatureBorn(CreatureBornEvent eventData)
        {
            if (config?.AutoSaveConfig.AutosaveOnCreatureBirth == true)
            {
                _ = TriggerAutoSaveAsync("CreatureBorn");
            }
        }

        private void OnBreedingSuccess(BreedingSuccessEvent eventData)
        {
            if (config?.AutoSaveConfig.AutosaveOnBreedingSuccess == true)
            {
                _ = TriggerAutoSaveAsync("BreedingSuccess");
            }
        }

        private void OnDiscoveryMade(DiscoveryMadeEvent eventData)
        {
            if (config?.AutoSaveConfig.AutosaveOnDiscovery == true)
            {
                _ = TriggerAutoSaveAsync("Discovery");
            }
        }

        private void OnEcosystemChanged(EcosystemChangedEvent eventData)
        {
            if (config?.AutoSaveConfig.AutosaveOnEcosystemChange == true)
            {
                _ = TriggerAutoSaveAsync("EcosystemChange");
            }
        }

        private void OnSceneChange(SceneChangeEvent eventData)
        {
            if (config?.AutoSaveConfig.AutosaveOnSceneChange == true)
            {
                _ = TriggerAutoSaveAsync("SceneChange");
            }
        }

        #endregion

        #region Condition Checks

        private bool CheckCreatureCountThreshold()
        {
            // Check if we have a significant number of creatures that warrant saving
            // This would integrate with the creature management system
            return false; // Placeholder
        }

        private bool CheckSignificantProgress()
        {
            // Check if player has made significant progress since last save
            return false; // Placeholder
        }

        private bool CheckLowHealthCondition()
        {
            // Check if any creatures are in critical condition
            return false; // Placeholder
        }

        #endregion

        #region Coroutine

        private IEnumerator AutoSaveCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);

                if (!isPaused && isAutoSaveEnabled && timeSinceLastAutoSave >= autosaveInterval)
                {
                    _ = TriggerAutoSaveAsync("Timer");
                }
            }
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            StopAutoSave();

            // Unsubscribe from events
            EventBus.Unsubscribe<CreatureBornEvent>(OnCreatureBorn);
            EventBus.Unsubscribe<BreedingSuccessEvent>(OnBreedingSuccess);
            EventBus.Unsubscribe<DiscoveryMadeEvent>(OnDiscoveryMade);
            EventBus.Unsubscribe<EcosystemChangedEvent>(OnEcosystemChanged);
            EventBus.Unsubscribe<SceneChangeEvent>(OnSceneChange);

            _autoSaveTriggers.Clear();
            _triggerCooldowns.Clear();
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Trigger Manual AutoSave")]
        private void TriggerManualAutoSave()
        {
            if (_isInitialized)
            {
                _ = TriggerAutoSaveAsync("Manual Debug");
            }
        }

        [ContextMenu("Reset AutoSave Timer")]
        private void ResetAutoSaveTimer()
        {
            timeSinceLastAutoSave = 0f;
        }

        #endregion
    }

    // Event types for auto-save triggers (these would be defined in the appropriate systems)

    public class CreatureBornEvent : BaseEvent
    {
        public string CreatureId { get; set; }
        public string SpeciesId { get; set; }
        public DateTime BirthTime { get; set; }
    }

    public class BreedingSuccessEvent : BaseEvent
    {
        public string Parent1Id { get; set; }
        public string Parent2Id { get; set; }
        public string OffspringId { get; set; }
        public DateTime BreedingTime { get; set; }
    }

    public class DiscoveryMadeEvent : BaseEvent
    {
        public string DiscoveryType { get; set; }
        public string DiscoveryId { get; set; }
        public DateTime DiscoveryTime { get; set; }
    }

    public class EcosystemChangedEvent : BaseEvent
    {
        public string BiomeId { get; set; }
        public string ChangeType { get; set; }
        public DateTime ChangeTime { get; set; }
    }

    public class SceneChangeEvent : BaseEvent
    {
        public string FromScene { get; set; }
        public string ToScene { get; set; }
        public DateTime ChangeTime { get; set; }
    }
}