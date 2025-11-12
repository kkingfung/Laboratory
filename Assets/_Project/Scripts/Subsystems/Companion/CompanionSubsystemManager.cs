using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using System.Linq;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Subsystems.Companion
{
    /// <summary>
    /// Cross-Platform Companion Subsystem Manager
    ///
    /// Provides seamless cross-platform integration between the main Project Chimera
    /// game and companion applications (mobile apps, web portals, AR experiences).
    /// Enables continuous engagement, remote monitoring, and extended gameplay
    /// beyond the primary platform.
    ///
    /// Key responsibilities:
    /// - Cross-platform data synchronization and cloud integration
    /// - Companion app communication and state management
    /// - Remote creature monitoring and care
    /// - Offline progress tracking and catch-up mechanics
    /// - Push notifications and engagement alerts
    /// - Second-screen experiences and dual-platform gameplay
    /// - Mobile AR creature viewing and interaction
    /// </summary>
    public class CompanionSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        #region ISubsystemManager Implementation

        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Companion";
        public float InitializationProgress { get; private set; }

        #endregion

        #region Events

        public static event Action<CompanionDevice> OnCompanionDeviceConnected;
        public static event Action<CompanionDevice> OnCompanionDeviceDisconnected;
        public static event Action<CrossPlatformSyncData> OnDataSynchronized;
        public static event Action<CompanionNotification> OnNotificationSent;
        public static event Action<RemoteAction> OnRemoteActionReceived;
        public static event Action<OfflineProgress> OnOfflineProgressProcessed;
        public static event Action<SecondScreenEvent> OnSecondScreenEventTriggered;

        #endregion

        #region Configuration

        [Header("Configuration")]
        [SerializeField] private CompanionSubsystemConfig _config;

        public CompanionSubsystemConfig Config
        {
            get => _config;
            set => _config = value;
        }

        #endregion

        #region Services

        private ICrossPlatformSyncService _crossPlatformSyncService;
        private ICompanionAppService _companionAppService;
        private ICloudDataService _cloudDataService;
        private IPushNotificationService _pushNotificationService;
        private IOfflineProgressService _offlineProgressService;
        private ISecondScreenService _secondScreenService;
        private IRemoteMonitoringService _remoteMonitoringService;

        #endregion

        #region State

        private bool _isInitialized;
        private bool _isRunning;
        private Coroutine _syncCoroutine;
        private Dictionary<string, CompanionDevice> _connectedDevices;
        private Dictionary<string, CompanionSession> _activeSessions;
        private Queue<CrossPlatformMessage> _messageQueue;
        private CloudSyncState _cloudSyncState;
        private DateTime _lastSyncAttempt;
        private Dictionary<string, OfflineProgress> _pendingOfflineProgress;

        // Companion app state
        private bool _isCloudConnected = false;
        private DateTime _lastHeartbeat;
        private int _syncRetryCount = 0;

        #endregion

        #region Initialization

        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized)
                return true;

            try
            {
                if (_config == null)
                {
                    Debug.LogError("[CompanionSubsystem] Configuration is null");
                    return false;
                }

                // Initialize services
                await InitializeServicesAsync();

                // Initialize data structures
                InitializeDataStructures();

                // Initialize cloud connection
                await InitializeCloudConnectionAsync();

                // Start synchronization loop
                StartSynchronizationLoop();

                // Initialize companion device discovery
                InitializeDeviceDiscovery();

                _isInitialized = true;
                _isRunning = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[CompanionSubsystem] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CompanionSubsystem] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeServicesAsync()
        {
            // Initialize cross-platform sync service
            _crossPlatformSyncService = new CrossPlatformSyncService(_config);
            await _crossPlatformSyncService.InitializeAsync();

            // Initialize companion app service
            _companionAppService = new CompanionAppService(_config);
            await _companionAppService.InitializeAsync();

            // Initialize cloud data service
            _cloudDataService = new CloudDataService(_config);
            await _cloudDataService.InitializeAsync();

            // Initialize push notification service
            _pushNotificationService = new PushNotificationService(_config);
            await _pushNotificationService.InitializeAsync();

            // Initialize offline progress service
            _offlineProgressService = new OfflineProgressService(_config);
            await _offlineProgressService.InitializeAsync();

            // Initialize second screen service
            _secondScreenService = new SecondScreenService(_config);
            await _secondScreenService.InitializeAsync();

            // Initialize remote monitoring service
            _remoteMonitoringService = new RemoteMonitoringService(_config);
            await _remoteMonitoringService.InitializeAsync();

            // Register with service container
            ServiceContainer.Instance?.RegisterService<ICrossPlatformSyncService>(_crossPlatformSyncService);
            ServiceContainer.Instance?.RegisterService<ICompanionAppService>(_companionAppService);
            ServiceContainer.Instance?.RegisterService<ICloudDataService>(_cloudDataService);
            ServiceContainer.Instance?.RegisterService<IPushNotificationService>(_pushNotificationService);
            ServiceContainer.Instance?.RegisterService<IOfflineProgressService>(_offlineProgressService);
            ServiceContainer.Instance?.RegisterService<ISecondScreenService>(_secondScreenService);
            ServiceContainer.Instance?.RegisterService<IRemoteMonitoringService>(_remoteMonitoringService);
        }

        private void InitializeDataStructures()
        {
            _connectedDevices = new Dictionary<string, CompanionDevice>();
            _activeSessions = new Dictionary<string, CompanionSession>();
            _messageQueue = new Queue<CrossPlatformMessage>();
            _cloudSyncState = new CloudSyncState();
            _pendingOfflineProgress = new Dictionary<string, OfflineProgress>();
        }

        private async Task InitializeCloudConnectionAsync()
        {
            try
            {
                _isCloudConnected = await _cloudDataService.ConnectAsync();
                if (_isCloudConnected)
                {
                    _cloudSyncState.isConnected = true;
                    _cloudSyncState.lastConnectionTime = DateTime.Now;
                    _lastHeartbeat = DateTime.Now;

                    if (_config.enableDebugLogging)
                        Debug.Log("[CompanionSubsystem] Cloud connection established");
                }
                else
                {
                    if (_config.enableDebugLogging)
                        Debug.LogWarning("[CompanionSubsystem] Failed to establish cloud connection");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CompanionSubsystem] Cloud connection failed: {ex.Message}");
                _isCloudConnected = false;
            }
        }

        private void StartSynchronizationLoop()
        {
            _syncCoroutine = StartCoroutine(SynchronizationLoop());
        }

        private void InitializeDeviceDiscovery()
        {
            if (_config.enableCompanionAppSupport)
            {
                _companionAppService?.StartDeviceDiscovery();
            }
        }

        #endregion

        #region Synchronization Loop

        private IEnumerator SynchronizationLoop()
        {
            var interval = _config.syncIntervalSeconds;

            while (_isRunning)
            {
                // Process message queue
                ProcessMessageQueue();

                // Perform cloud synchronization
                PerformCloudSynchronization();

                // Update companion device connections
                UpdateCompanionDevices();

                // Process offline progress
                ProcessOfflineProgress();

                // Send pending notifications
                ProcessPendingNotifications();

                // Update heartbeat
                UpdateHeartbeat();

                yield return new WaitForSeconds(interval);
            }
        }

        private void ProcessMessageQueue()
        {
            var processedCount = 0;
            var maxMessages = _config.maxMessagesPerUpdate;

            while (_messageQueue.Count > 0 && processedCount < maxMessages)
            {
                var message = _messageQueue.Dequeue();
                ProcessCrossPlatformMessage(message);
                processedCount++;
            }
        }

        private void ProcessCrossPlatformMessage(CrossPlatformMessage message)
        {
            switch (message.messageType)
            {
                case CrossPlatformMessageType.DeviceConnection:
                    ProcessDeviceConnectionMessage(message);
                    break;

                case CrossPlatformMessageType.DataSync:
                    ProcessDataSyncMessage(message);
                    break;

                case CrossPlatformMessageType.RemoteAction:
                    ProcessRemoteActionMessage(message);
                    break;

                case CrossPlatformMessageType.Notification:
                    ProcessNotificationMessage(message);
                    break;

                case CrossPlatformMessageType.SecondScreen:
                    ProcessSecondScreenMessage(message);
                    break;

                case CrossPlatformMessageType.OfflineProgress:
                    ProcessOfflineProgressMessage(message);
                    break;
            }
        }

        private void ProcessDeviceConnectionMessage(CrossPlatformMessage message)
        {
            var deviceData = message.data.GetValueOrDefault("device") as CompanionDevice;
            if (deviceData != null)
            {
                var isConnecting = (bool)message.data.GetValueOrDefault("connecting", true);

                if (isConnecting)
                {
                    ConnectCompanionDevice(deviceData);
                }
                else
                {
                    DisconnectCompanionDevice(deviceData.deviceId);
                }
            }
        }

        private void ProcessDataSyncMessage(CrossPlatformMessage message)
        {
            var syncData = message.data.GetValueOrDefault("syncData") as CrossPlatformSyncData;
            if (syncData != null)
            {
                ApplySyncData(syncData);
            }
        }

        private void ProcessRemoteActionMessage(CrossPlatformMessage message)
        {
            var remoteAction = message.data.GetValueOrDefault("action") as RemoteAction;
            if (remoteAction != null)
            {
                ExecuteRemoteAction(remoteAction);
            }
        }

        private void ProcessNotificationMessage(CrossPlatformMessage message)
        {
            var notification = message.data.GetValueOrDefault("notification") as CompanionNotification;
            if (notification != null)
            {
                SendNotificationToDevices(notification);
            }
        }

        private void ProcessSecondScreenMessage(CrossPlatformMessage message)
        {
            var secondScreenEvent = message.data.GetValueOrDefault("event") as SecondScreenEvent;
            if (secondScreenEvent != null)
            {
                TriggerSecondScreenEvent(secondScreenEvent);
            }
        }

        private void ProcessOfflineProgressMessage(CrossPlatformMessage message)
        {
            var offlineProgress = message.data.GetValueOrDefault("progress") as OfflineProgress;
            if (offlineProgress != null)
            {
                ProcessOfflineProgressData(offlineProgress);
            }
        }

        private void PerformCloudSynchronization()
        {
            if (!_isCloudConnected || !ShouldSyncNow())
                return;

            Task.Run(async () =>
            {
                try
                {
                    var syncData = await _crossPlatformSyncService.GatherSyncDataAsync();
                    var success = await _cloudDataService.SyncDataAsync(syncData);

                    if (success)
                    {
                        _cloudSyncState.lastSyncTime = DateTime.Now;
                        _syncRetryCount = 0;
                        OnDataSynchronized?.Invoke(syncData);

                        if (_config.enableDebugLogging)
                            Debug.Log("[CompanionSubsystem] Cloud sync completed successfully");
                    }
                    else
                    {
                        _syncRetryCount++;
                        if (_config.enableDebugLogging)
                            Debug.LogWarning($"[CompanionSubsystem] Cloud sync failed (attempt {_syncRetryCount})");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CompanionSubsystem] Cloud sync error: {ex.Message}");
                    _syncRetryCount++;
                }

                _lastSyncAttempt = DateTime.Now;
            });
        }

        private bool ShouldSyncNow()
        {
            var timeSinceLastSync = DateTime.Now - _cloudSyncState.lastSyncTime;
            var timeSinceLastAttempt = DateTime.Now - _lastSyncAttempt;

            // Sync if enough time has passed or if retry is needed
            return timeSinceLastSync.TotalSeconds >= _config.syncIntervalSeconds ||
                   (_syncRetryCount > 0 && timeSinceLastAttempt.TotalSeconds >= _config.syncRetryIntervalSeconds);
        }

        private void UpdateCompanionDevices()
        {
            var devicesToRemove = new List<string>();

            foreach (var device in _connectedDevices.Values)
            {
                // Check device heartbeat
                var timeSinceLastHeartbeat = DateTime.Now - device.lastHeartbeat;
                if (timeSinceLastHeartbeat.TotalSeconds > _config.deviceTimeoutSeconds)
                {
                    devicesToRemove.Add(device.deviceId);
                }
                else
                {
                    // Update device status
                    UpdateDeviceStatus(device);
                }
            }

            // Remove timed-out devices
            foreach (var deviceId in devicesToRemove)
            {
                DisconnectCompanionDevice(deviceId);
            }
        }

        private void ProcessOfflineProgress()
        {
            var progressToProcess = new List<OfflineProgress>(_pendingOfflineProgress.Values);
            _pendingOfflineProgress.Clear();

            foreach (var progress in progressToProcess)
            {
                ApplyOfflineProgress(progress);
            }
        }

        private void ProcessPendingNotifications()
        {
            _pushNotificationService?.ProcessPendingNotifications();
        }

        private void UpdateHeartbeat()
        {
            var timeSinceLastHeartbeat = DateTime.Now - _lastHeartbeat;
            if (timeSinceLastHeartbeat.TotalSeconds >= _config.heartbeatIntervalSeconds)
            {
                SendHeartbeat();
                _lastHeartbeat = DateTime.Now;
            }
        }

        #endregion

        #region Companion Device Management

        /// <summary>
        /// Connects a companion device
        /// </summary>
        public void ConnectCompanionDevice(CompanionDevice device)
        {
            device.connectionTime = DateTime.Now;
            device.lastHeartbeat = DateTime.Now;
            device.isConnected = true;

            _connectedDevices[device.deviceId] = device;

            // Create companion session
            var session = new CompanionSession
            {
                sessionId = Guid.NewGuid().ToString(),
                deviceId = device.deviceId,
                userId = device.userId,
                startTime = DateTime.Now,
                isActive = true
            };

            _activeSessions[session.sessionId] = session;

            OnCompanionDeviceConnected?.Invoke(device);

            // Send initial sync data to device
            Task.Run(async () => await SendInitialSyncToDevice(device));

            if (_config.enableDebugLogging)
                Debug.Log($"[CompanionSubsystem] Connected device: {device.deviceId} ({device.deviceType})");
        }

        /// <summary>
        /// Disconnects a companion device
        /// </summary>
        public void DisconnectCompanionDevice(string deviceId)
        {
            if (_connectedDevices.TryGetValue(deviceId, out var device))
            {
                device.isConnected = false;
                device.disconnectionTime = DateTime.Now;

                _connectedDevices.Remove(deviceId);

                // End companion sessions for this device
                var sessionsToEnd = _activeSessions.Values.Where(s => s.deviceId == deviceId).ToList();
                foreach (var session in sessionsToEnd)
                {
                    EndCompanionSession(session.sessionId);
                }

                OnCompanionDeviceDisconnected?.Invoke(device);

                if (_config.enableDebugLogging)
                    Debug.Log($"[CompanionSubsystem] Disconnected device: {deviceId}");
            }
        }

        /// <summary>
        /// Gets connected devices
        /// </summary>
        public List<CompanionDevice> GetConnectedDevices()
        {
            return new List<CompanionDevice>(_connectedDevices.Values);
        }

        /// <summary>
        /// Checks if a specific device type is connected
        /// </summary>
        public bool IsDeviceTypeConnected(CompanionDeviceType deviceType)
        {
            return _connectedDevices.Values.Any(d => d.deviceType == deviceType);
        }

        #endregion

        #region Cross-Platform Communication

        /// <summary>
        /// Sends a message to companion devices
        /// </summary>
        public async Task<bool> SendMessageToDevicesAsync(CrossPlatformMessage message, List<string> targetDeviceIds = null)
        {
            var targetDevices = targetDeviceIds?.Select(id => _connectedDevices.GetValueOrDefault(id))
                                              .Where(d => d != null)
                                              .ToList() ?? _connectedDevices.Values.ToList();

            var success = true;

            foreach (var device in targetDevices)
            {
                var deviceSuccess = await _companionAppService.SendMessageToDeviceAsync(device, message);
                success = success && deviceSuccess;
            }

            return success;
        }

        /// <summary>
        /// Sends data to cloud for synchronization
        /// </summary>
        public async Task<bool> SyncToCloudAsync(CrossPlatformSyncData syncData)
        {
            if (!_isCloudConnected)
            {
                await InitializeCloudConnectionAsync();
                if (!_isCloudConnected)
                    return false;
            }

            var success = await _cloudDataService.SyncDataAsync(syncData);
            if (success)
            {
                OnDataSynchronized?.Invoke(syncData);
            }

            return success;
        }

        /// <summary>
        /// Pulls latest data from cloud
        /// </summary>
        public async Task<CrossPlatformSyncData> PullFromCloudAsync()
        {
            if (!_isCloudConnected)
                return null;

            return await _cloudDataService.PullDataAsync();
        }

        #endregion

        #region Remote Actions

        /// <summary>
        /// Executes a remote action received from companion device
        /// </summary>
        public void ExecuteRemoteAction(RemoteAction action)
        {
            switch (action.actionType)
            {
                case RemoteActionType.FeedCreature:
                    ExecuteFeedCreatureAction(action);
                    break;

                case RemoteActionType.ViewCreature:
                    ExecuteViewCreatureAction(action);
                    break;

                case RemoteActionType.StartBreeding:
                    ExecuteStartBreedingAction(action);
                    break;

                case RemoteActionType.CheckProgress:
                    ExecuteCheckProgressAction(action);
                    break;

                case RemoteActionType.SetNotificationPreferences:
                    ExecuteSetNotificationPreferencesAction(action);
                    break;

                case RemoteActionType.CollectRewards:
                    ExecuteCollectRewardsAction(action);
                    break;
            }

            OnRemoteActionReceived?.Invoke(action);

            if (_config.enableDebugLogging)
                Debug.Log($"[CompanionSubsystem] Executed remote action: {action.actionType}");
        }

        private void ExecuteFeedCreatureAction(RemoteAction action)
        {
            var creatureId = action.parameters.GetValueOrDefault("creatureId")?.ToString();
            var foodType = action.parameters.GetValueOrDefault("foodType")?.ToString();

            if (!string.IsNullOrEmpty(creatureId) && !string.IsNullOrEmpty(foodType))
            {
                // This would integrate with the creature management system
                // For now, just log the action
                if (_config.enableDebugLogging)
                    Debug.Log($"[CompanionSubsystem] Remote feed creature: {creatureId} with {foodType}");
            }
        }

        private void ExecuteViewCreatureAction(RemoteAction action)
        {
            var creatureId = action.parameters.GetValueOrDefault("creatureId")?.ToString();
            var deviceId = action.deviceId;

            if (!string.IsNullOrEmpty(creatureId) && !string.IsNullOrEmpty(deviceId))
            {
                // Send creature data to requesting device
                Task.Run(async () => await SendCreatureDataToDevice(deviceId, creatureId));
            }
        }

        private void ExecuteStartBreedingAction(RemoteAction action)
        {
            var parent1Id = action.parameters.GetValueOrDefault("parent1Id")?.ToString();
            var parent2Id = action.parameters.GetValueOrDefault("parent2Id")?.ToString();

            if (!string.IsNullOrEmpty(parent1Id) && !string.IsNullOrEmpty(parent2Id))
            {
                // This would integrate with the breeding system
                if (_config.enableDebugLogging)
                    Debug.Log($"[CompanionSubsystem] Remote start breeding: {parent1Id} x {parent2Id}");
            }
        }

        private void ExecuteCheckProgressAction(RemoteAction action)
        {
            var userId = action.userId;
            var deviceId = action.deviceId;

            if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(deviceId))
            {
                // Send progress data to requesting device
                Task.Run(async () => await SendProgressDataToDevice(deviceId, userId));
            }
        }

        private void ExecuteSetNotificationPreferencesAction(RemoteAction action)
        {
            var userId = action.userId;
            var preferences = action.parameters.GetValueOrDefault("preferences") as NotificationPreferences;

            if (!string.IsNullOrEmpty(userId) && preferences != null)
            {
                _pushNotificationService?.UpdateUserPreferences(userId, preferences);
            }
        }

        private void ExecuteCollectRewardsAction(RemoteAction action)
        {
            var userId = action.userId;
            var rewardIds = action.parameters.GetValueOrDefault("rewardIds") as List<string>;

            if (!string.IsNullOrEmpty(userId) && rewardIds != null)
            {
                // This would integrate with the rewards system
                foreach (var rewardId in rewardIds)
                {
                    if (_config.enableDebugLogging)
                        Debug.Log($"[CompanionSubsystem] Remote collect reward: {rewardId} for {userId}");
                }
            }
        }

        #endregion

        #region Notifications

        /// <summary>
        /// Sends a notification to companion devices
        /// </summary>
        public async Task<bool> SendNotificationAsync(CompanionNotification notification)
        {
            var success = await _pushNotificationService.SendNotificationAsync(notification);
            if (success)
            {
                OnNotificationSent?.Invoke(notification);
            }

            return success;
        }

        /// <summary>
        /// Schedules a notification for future delivery
        /// </summary>
        public void ScheduleNotification(CompanionNotification notification, DateTime deliveryTime)
        {
            _pushNotificationService?.ScheduleNotification(notification, deliveryTime);
        }

        /// <summary>
        /// Cancels a scheduled notification
        /// </summary>
        public void CancelNotification(string notificationId)
        {
            _pushNotificationService?.CancelNotification(notificationId);
        }

        #endregion

        #region Offline Progress

        /// <summary>
        /// Processes offline progress for a user
        /// </summary>
        public void ProcessOfflineProgressData(OfflineProgress offlineProgress)
        {
            _pendingOfflineProgress[offlineProgress.userId] = offlineProgress;

            if (_config.enableDebugLogging)
                Debug.Log($"[CompanionSubsystem] Queued offline progress for {offlineProgress.userId}");
        }

        /// <summary>
        /// Applies offline progress to game state
        /// </summary>
        private void ApplyOfflineProgress(OfflineProgress offlineProgress)
        {
            // Calculate offline rewards and progress
            var offlineRewards = CalculateOfflineRewards(offlineProgress);

            // Apply the rewards to the game state
            ApplyOfflineRewards(offlineProgress.userId, offlineRewards);

            // Update player about offline progress
            NotifyPlayerOfOfflineProgress(offlineProgress.userId, offlineRewards);

            OnOfflineProgressProcessed?.Invoke(offlineProgress);

            if (_config.enableDebugLogging)
                Debug.Log($"[CompanionSubsystem] Applied offline progress for {offlineProgress.userId}");
        }

        private OfflineRewards CalculateOfflineRewards(OfflineProgress offlineProgress)
        {
            var rewards = new OfflineRewards
            {
                userId = offlineProgress.userId,
                timeOffline = offlineProgress.timeOffline,
                calculationTime = DateTime.Now
            };

            // Calculate creature growth
            foreach (var creature in offlineProgress.creatures)
            {
                var growthAmount = CalculateCreatureGrowth(creature, offlineProgress.timeOffline);
                rewards.creatureGrowth[creature.creatureId] = growthAmount;
            }

            // Calculate resource generation
            var resourceGeneration = CalculateResourceGeneration(offlineProgress.timeOffline);
            rewards.resourcesGenerated = resourceGeneration;

            // Calculate research progress
            var researchProgress = CalculateResearchProgress(offlineProgress.timeOffline);
            rewards.researchProgress = researchProgress;

            return rewards;
        }

        private float CalculateCreatureGrowth(CreatureData creature, TimeSpan timeOffline)
        {
            var baseGrowthRate = 0.1f; // Growth per hour
            var hoursOffline = (float)timeOffline.TotalHours;
            var maxOfflineHours = _config.maxOfflineProgressHours;

            var effectiveHours = Math.Min(hoursOffline, maxOfflineHours);
            return effectiveHours * baseGrowthRate;
        }

        private Dictionary<string, float> CalculateResourceGeneration(TimeSpan timeOffline)
        {
            var resources = new Dictionary<string, float>();
            var hoursOffline = (float)timeOffline.TotalHours;
            var maxOfflineHours = _config.maxOfflineProgressHours;

            var effectiveHours = Math.Min(hoursOffline, maxOfflineHours);

            resources["food"] = effectiveHours * 10f;
            resources["research_points"] = effectiveHours * 5f;
            resources["materials"] = effectiveHours * 3f;

            return resources;
        }

        private float CalculateResearchProgress(TimeSpan timeOffline)
        {
            var baseResearchRate = 0.05f; // Research progress per hour
            var hoursOffline = (float)timeOffline.TotalHours;
            var maxOfflineHours = _config.maxOfflineProgressHours;

            var effectiveHours = Math.Min(hoursOffline, maxOfflineHours);
            return effectiveHours * baseResearchRate;
        }

        private void ApplyOfflineRewards(string userId, OfflineRewards rewards)
        {
            // This would integrate with the game's progression systems
            // For now, just log the rewards
            if (_config.enableDebugLogging)
            {
                Debug.Log($"[CompanionSubsystem] Applied offline rewards for {userId}:");
                Debug.Log($"  Creature Growth: {rewards.creatureGrowth.Count} creatures");
                Debug.Log($"  Resources: {rewards.resourcesGenerated.Count} types");
                Debug.Log($"  Research Progress: {rewards.researchProgress:F2}");
            }
        }

        private void NotifyPlayerOfOfflineProgress(string userId, OfflineRewards rewards)
        {
            var notification = new CompanionNotification
            {
                notificationId = Guid.NewGuid().ToString(),
                userId = userId,
                title = "Welcome Back!",
                message = GenerateOfflineProgressMessage(rewards),
                notificationType = NotificationType.OfflineProgress,
                timestamp = DateTime.Now,
                data = new Dictionary<string, object> { ["rewards"] = rewards }
            };

            Task.Run(async () => await SendNotificationAsync(notification));
        }

        private string GenerateOfflineProgressMessage(OfflineRewards rewards)
        {
            var messageBuilder = new System.Text.StringBuilder();
            messageBuilder.Append("While you were away: ");

            if (rewards.creatureGrowth.Count > 0)
            {
                messageBuilder.Append($"{rewards.creatureGrowth.Count} creatures grew, ");
            }

            if (rewards.resourcesGenerated.Count > 0)
            {
                messageBuilder.Append("resources were generated, ");
            }

            if (rewards.researchProgress > 0)
            {
                messageBuilder.Append("research progressed. ");
            }

            return messageBuilder.ToString().TrimEnd(',', ' ') + "!";
        }

        #endregion

        #region Second Screen Features

        /// <summary>
        /// Triggers a second screen event
        /// </summary>
        public void TriggerSecondScreenEvent(SecondScreenEvent secondScreenEvent)
        {
            var compatibleDevices = _connectedDevices.Values
                .Where(d => d.capabilities.Contains("second_screen"))
                .ToList();

            foreach (var device in compatibleDevices)
            {
                var message = new CrossPlatformMessage
                {
                    messageType = CrossPlatformMessageType.SecondScreen,
                    targetDeviceId = device.deviceId,
                    data = new Dictionary<string, object> { ["event"] = secondScreenEvent }
                };

                Task.Run(async () => await SendMessageToDevicesAsync(message, new List<string> { device.deviceId }));
            }

            OnSecondScreenEventTriggered?.Invoke(secondScreenEvent);

            if (_config.enableDebugLogging)
                Debug.Log($"[CompanionSubsystem] Triggered second screen event: {secondScreenEvent.eventType}");
        }

        #endregion

        #region Helper Methods

        private async Task SendInitialSyncToDevice(CompanionDevice device)
        {
            var syncData = await _crossPlatformSyncService.GatherInitialSyncDataAsync(device.userId);
            var message = new CrossPlatformMessage
            {
                messageType = CrossPlatformMessageType.DataSync,
                targetDeviceId = device.deviceId,
                data = new Dictionary<string, object> { ["syncData"] = syncData }
            };

            await SendMessageToDevicesAsync(message, new List<string> { device.deviceId });
        }

        private async Task SendCreatureDataToDevice(string deviceId, string creatureId)
        {
            // This would gather creature data from the game systems
            var creatureData = new Dictionary<string, object>
            {
                ["creatureId"] = creatureId,
                ["name"] = $"Creature_{creatureId}",
                ["status"] = "healthy",
                ["lastFed"] = DateTime.Now.AddHours(-2),
                ["happiness"] = 0.8f
            };

            var message = new CrossPlatformMessage
            {
                messageType = CrossPlatformMessageType.DataSync,
                targetDeviceId = deviceId,
                data = new Dictionary<string, object> { ["creatureData"] = creatureData }
            };

            await SendMessageToDevicesAsync(message, new List<string> { deviceId });
        }

        private async Task SendProgressDataToDevice(string deviceId, string userId)
        {
            // This would gather progress data from the game systems
            var progressData = new Dictionary<string, object>
            {
                ["userId"] = userId,
                ["level"] = 15,
                ["experience"] = 1250,
                ["creaturesOwned"] = 8,
                ["discoveriesMade"] = 23,
                ["lastPlayTime"] = DateTime.Now.AddDays(-1)
            };

            var message = new CrossPlatformMessage
            {
                messageType = CrossPlatformMessageType.DataSync,
                targetDeviceId = deviceId,
                data = new Dictionary<string, object> { ["progressData"] = progressData }
            };

            await SendMessageToDevicesAsync(message, new List<string> { deviceId });
        }

        private void UpdateDeviceStatus(CompanionDevice device)
        {
            // Update device-specific status information
            device.lastHeartbeat = DateTime.Now;

            // This could include battery level, connection quality, etc.
        }

        private void SendHeartbeat()
        {
            if (_isCloudConnected)
            {
                Task.Run(async () => await _cloudDataService.SendHeartbeatAsync());
            }
        }

        private void ApplySyncData(CrossPlatformSyncData syncData)
        {
            // Apply synchronized data to the game state
            // This would integrate with various game systems
            if (_config.enableDebugLogging)
                Debug.Log($"[CompanionSubsystem] Applied sync data: {syncData.dataTypes.Count} types");
        }

        private void SendNotificationToDevices(CompanionNotification notification)
        {
            Task.Run(async () => await SendNotificationAsync(notification));
        }

        private void EndCompanionSession(string sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                session.endTime = DateTime.Now;
                session.isActive = false;
                _activeSessions.Remove(sessionId);

                if (_config.enableDebugLogging)
                    Debug.Log($"[CompanionSubsystem] Ended companion session: {sessionId}");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Send Test Notification")]
        private async void DebugSendTestNotification()
        {
            var notification = new CompanionNotification
            {
                notificationId = Guid.NewGuid().ToString(),
                title = "Test Notification",
                message = "This is a test notification from Project Chimera!",
                notificationType = NotificationType.General,
                timestamp = DateTime.Now
            };

            await SendNotificationAsync(notification);
        }

        [ContextMenu("Simulate Device Connection")]
        private void DebugSimulateDeviceConnection()
        {
            var testDevice = new CompanionDevice
            {
                deviceId = Guid.NewGuid().ToString(),
                deviceType = CompanionDeviceType.Mobile,
                userId = "test_user",
                deviceName = "Test Mobile Device",
                capabilities = new List<string> { "notifications", "remote_actions", "second_screen" }
            };

            ConnectCompanionDevice(testDevice);
        }

        [ContextMenu("Force Cloud Sync")]
        private async void DebugForceCloudSync()
        {
            var syncData = await _crossPlatformSyncService.GatherSyncDataAsync();
            await SyncToCloudAsync(syncData);
        }

        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            _isRunning = false;

            if (_syncCoroutine != null)
            {
                StopCoroutine(_syncCoroutine);
            }

            // Disconnect all devices
            var connectedDeviceIds = new List<string>(_connectedDevices.Keys);
            foreach (var deviceId in connectedDeviceIds)
            {
                DisconnectCompanionDevice(deviceId);
            }

            // Disconnect from cloud
            if (_isCloudConnected)
            {
                Task.Run(async () => await _cloudDataService.DisconnectAsync());
            }
        }

        #endregion
    }

    #region Service Implementations

    /// <summary>
    /// Basic implementation of cross-platform sync service
    /// </summary>
    public class CrossPlatformSyncService : ICrossPlatformSyncService
    {
        private readonly CompanionSubsystemConfig _config;
        private bool _isInitialized;

        public CrossPlatformSyncService(CompanionSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                await Task.Delay(100); // Simulate initialization
                _isInitialized = true;
                UnityEngine.Debug.Log("[CrossPlatformSyncService] Initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CrossPlatformSyncService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public async Task<CrossPlatformSyncData> GatherSyncDataAsync()
        {
            if (!_isInitialized) return new CrossPlatformSyncData();

            await Task.Delay(50); // Simulate gathering data

            return new CrossPlatformSyncData
            {
                userId = "player_001",
                timestamp = DateTime.Now,
                version = "1.0.0",
                gameProgressData = new Dictionary<string, object>
                {
                    ["level"] = 5,
                    ["experience"] = 1250,
                    ["creatures_owned"] = 3
                },
                creatureData = new List<CreatureSyncData>(),
                userPreferences = new Dictionary<string, object>
                {
                    ["notifications_enabled"] = true,
                    ["companion_sync"] = true
                },
                metadata = new Dictionary<string, object>
                {
                    ["last_platform"] = Application.platform.ToString(),
                    ["session_id"] = Guid.NewGuid().ToString()
                }
            };
        }

        public async Task<CrossPlatformSyncData> GatherInitialSyncDataAsync(string userId)
        {
            var syncData = await GatherSyncDataAsync();
            syncData.userId = userId;
            syncData.metadata["initial_sync"] = true;
            return syncData;
        }

        public async Task<bool> ApplySyncDataAsync(CrossPlatformSyncData syncData)
        {
            if (!_isInitialized || syncData == null) return false;

            try
            {
                await Task.Delay(100); // Simulate applying data
                UnityEngine.Debug.Log($"[CrossPlatformSyncService] Applied sync data for user {syncData.userId}");
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CrossPlatformSyncService] Failed to apply sync data: {ex.Message}");
                return false;
            }
        }

        public async Task<List<SyncConflict>> DetectConflictsAsync(CrossPlatformSyncData localData, CrossPlatformSyncData remoteData)
        {
            await Task.Delay(50);
            var conflicts = new List<SyncConflict>();

            // Basic conflict detection logic
            if (localData.timestamp > remoteData.timestamp.AddMinutes(5))
            {
                conflicts.Add(new SyncConflict
                {
                    conflictType = SyncConflictType.TimestampMismatch,
                    localValue = localData.timestamp,
                    remoteValue = remoteData.timestamp,
                    fieldName = "timestamp"
                });
            }

            return conflicts;
        }

        public async Task<bool> ResolveConflictsAsync(List<SyncConflict> conflicts)
        {
            if (conflicts == null || conflicts.Count == 0) return true;

            try
            {
                await Task.Delay(100); // Simulate conflict resolution
                UnityEngine.Debug.Log($"[CrossPlatformSyncService] Resolved {conflicts.Count} conflicts");
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CrossPlatformSyncService] Failed to resolve conflicts: {ex.Message}");
                return false;
            }
        }
    }

    /// <summary>
    /// Basic implementation of companion app service
    /// </summary>
    public class CompanionAppService : ICompanionAppService
    {
        private readonly CompanionSubsystemConfig _config;
        private bool _isInitialized;
        private bool _isDiscovering;
        private List<CompanionDevice> _discoveredDevices;

        public CompanionAppService(CompanionSubsystemConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _discoveredDevices = new List<CompanionDevice>();
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                await Task.Delay(100); // Simulate initialization
                _isInitialized = true;
                UnityEngine.Debug.Log("[CompanionAppService] Initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CompanionAppService] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        public void StartDeviceDiscovery()
        {
            if (!_isInitialized) return;

            _isDiscovering = true;
            UnityEngine.Debug.Log("[CompanionAppService] Started device discovery");

            // Simulate finding devices
            _discoveredDevices.Add(new CompanionDevice
            {
                deviceId = "mobile_001",
                deviceName = "Player's Phone",
                deviceType = CompanionDeviceType.MobileApp,
                isConnected = false,
                connectionStatus = CompanionConnectionStatus.Discovered,
                lastSeenTime = DateTime.Now,
                userId = "player_001",
                capabilities = new List<CompanionCapability>
                {
                    CompanionCapability.RemoteMonitoring,
                    CompanionCapability.PushNotifications
                }
            });
        }

        public void StopDeviceDiscovery()
        {
            _isDiscovering = false;
            UnityEngine.Debug.Log("[CompanionAppService] Stopped device discovery");
        }

        public async Task<bool> SendMessageToDeviceAsync(CompanionDevice device, CrossPlatformMessage message)
        {
            if (!_isInitialized || device == null || message == null) return false;

            try
            {
                await Task.Delay(50); // Simulate sending message
                UnityEngine.Debug.Log($"[CompanionAppService] Sent message to device {device.deviceName}");
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CompanionAppService] Failed to send message: {ex.Message}");
                return false;
            }
        }

        public async Task<CrossPlatformMessage> ReceiveMessageAsync()
        {
            if (!_isInitialized) return null;

            await Task.Delay(100); // Simulate checking for messages

            // Return null for now (no messages)
            return null;
        }

        public List<CompanionDevice> GetDiscoveredDevices()
        {
            return new List<CompanionDevice>(_discoveredDevices);
        }

        public async Task<bool> AuthenticateDeviceAsync(CompanionDevice device)
        {
            if (!_isInitialized || device == null) return false;

            try
            {
                await Task.Delay(200); // Simulate authentication
                device.isConnected = true;
                device.connectionStatus = CompanionConnectionStatus.Connected;
                UnityEngine.Debug.Log($"[CompanionAppService] Authenticated device {device.deviceName}");
                return true;
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[CompanionAppService] Failed to authenticate device: {ex.Message}");
                return false;
            }
        }
    }

    #endregion
}