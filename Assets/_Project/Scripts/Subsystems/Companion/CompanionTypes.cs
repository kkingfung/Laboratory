using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Laboratory.Subsystems.Companion
{
    #region Core Companion Data

    [Serializable]
    public class CompanionDevice
    {
        public string deviceId;
        public string userId;
        public CompanionDeviceType deviceType;
        public string deviceName;
        public string deviceModel;
        public string osVersion;
        public string appVersion;
        public bool isConnected;
        public DateTime connectionTime;
        public DateTime disconnectionTime;
        public DateTime lastHeartbeat;
        public List<string> capabilities = new();
        public DeviceSettings settings = new();
        public Dictionary<string, object> metadata = new();
    }

    [Serializable]
    public class CompanionSession
    {
        public string sessionId;
        public string deviceId;
        public string userId;
        public DateTime startTime;
        public DateTime endTime;
        public bool isActive;
        public SessionType sessionType;
        public List<string> activitiesPerformed = new();
        public Dictionary<string, object> sessionData = new();
    }

    [Serializable]
    public class CrossPlatformMessage
    {
        public string messageId;
        public CrossPlatformMessageType messageType;
        public string sourceDeviceId;
        public string targetDeviceId;
        public string userId;
        public DateTime timestamp;
        public MessagePriority priority = MessagePriority.Normal;
        public Dictionary<string, object> data = new();
        public bool requiresAcknowledgment;
        public DateTime expirationTime;
    }

    [Serializable]
    public class CloudSyncState
    {
        public bool isConnected;
        public DateTime lastConnectionTime;
        public DateTime lastSyncTime;
        public int syncVersion;
        public List<string> pendingSyncItems = new();
        public Dictionary<string, DateTime> lastSyncByType = new();
        public SyncStatus status = SyncStatus.Idle;
        public string lastError;
    }

    public enum CompanionDeviceType
    {
        Mobile,
        Tablet,
        Desktop,
        Web,
        SmartWatch,
        AR,
        VR,
        TV
    }

    public enum SessionType
    {
        Monitoring,
        Interaction,
        SecondScreen,
        RemoteControl,
        DataSync,
        Companion
    }

    public enum CrossPlatformMessageType
    {
        DeviceConnection,
        DataSync,
        RemoteAction,
        Notification,
        SecondScreen,
        OfflineProgress,
        Heartbeat,
        StatusUpdate
    }

    public enum MessagePriority
    {
        Low,
        Normal,
        High,
        Critical,
        Immediate
    }

    public enum SyncStatus
    {
        Idle,
        Syncing,
        Success,
        Failed,
        Conflict,
        Pending
    }

    #endregion

    #region Cross-Platform Synchronization

    [Serializable]
    public class CrossPlatformSyncData
    {
        public string userId;
        public DateTime syncTime;
        public int syncVersion;
        public List<string> dataTypes = new();
        public Dictionary<string, object> gameState = new();
        public Dictionary<string, object> playerProgress = new();
        public Dictionary<string, object> creatures = new();
        public Dictionary<string, object> inventory = new();
        public Dictionary<string, object> achievements = new();
        public Dictionary<string, object> settings = new();
        public List<SyncConflict> conflicts = new();
        public SyncMetadata metadata = new();
    }

    [Serializable]
    public class SyncConflict
    {
        public string conflictId;
        public string dataType;
        public string fieldName;
        public object localValue;
        public object remoteValue;
        public DateTime localTimestamp;
        public DateTime remoteTimestamp;
        public ConflictResolution resolution = ConflictResolution.Manual;
        public bool isResolved;
    }

    [Serializable]
    public class SyncMetadata
    {
        public Dictionary<string, DateTime> lastModified = new();
        public Dictionary<string, string> checksums = new();
        public Dictionary<string, int> versions = new();
        public List<string> priorityData = new();
        public SyncStrategy strategy = SyncStrategy.Incremental;
    }

    public enum ConflictResolution
    {
        Manual,
        LocalWins,
        RemoteWins,
        Merge,
        Timestamp,
        Custom
    }

    public enum SyncStrategy
    {
        Full,
        Incremental,
        Priority,
        Compressed,
        Selective
    }

    #endregion

    #region Remote Actions

    [Serializable]
    public class RemoteAction
    {
        public string actionId;
        public RemoteActionType actionType;
        public string deviceId;
        public string userId;
        public DateTime timestamp;
        public Dictionary<string, object> parameters = new();
        public ActionStatus status = ActionStatus.Pending;
        public string result;
        public DateTime executionTime;
        public bool requiresConfirmation;
    }

    [Serializable]
    public class RemoteActionResult
    {
        public string actionId;
        public bool success;
        public string message;
        public Dictionary<string, object> resultData = new();
        public DateTime completionTime;
        public List<string> sideEffects = new();
    }

    [Serializable]
    public class ActionPermissions
    {
        public string userId;
        public List<RemoteActionType> allowedActions = new();
        public List<RemoteActionType> restrictedActions = new();
        public Dictionary<RemoteActionType, ActionRestrictions> actionRestrictions = new();
        public bool requiresAuthentication = true;
        public DateTime lastUpdated;
    }

    [Serializable]
    public class ActionRestrictions
    {
        public int maxUsagePerHour;
        public int maxUsagePerDay;
        public List<string> allowedParameters = new();
        public List<string> restrictedParameters = new();
        public bool requiresParentalConsent;
        public List<string> requiredPermissions = new();
    }

    public enum RemoteActionType
    {
        FeedCreature,
        ViewCreature,
        StartBreeding,
        CheckProgress,
        CollectRewards,
        SetNotificationPreferences,
        UpdateSettings,
        ViewInventory,
        UseItem,
        StartResearch,
        ViewAchievements,
        SocialInteraction
    }

    public enum ActionStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Cancelled,
        Expired
    }

    #endregion

    #region Notifications

    [Serializable]
    public class CompanionNotification
    {
        public string notificationId;
        public string userId;
        public string deviceId;
        public NotificationType notificationType;
        public string title;
        public string message;
        public DateTime timestamp;
        public DateTime scheduledTime;
        public NotificationPriority priority = NotificationPriority.Normal;
        public Dictionary<string, object> data = new();
        public List<NotificationAction> actions = new();
        public bool isRead;
        public bool isSent;
        public DateTime expirationTime;
    }

    [Serializable]
    public class NotificationAction
    {
        public string actionId;
        public string actionText;
        public string actionType;
        public Dictionary<string, object> actionData = new();
        public bool isDestructive;
    }

    [Serializable]
    public class NotificationPreferences
    {
        public string userId;
        public bool enableNotifications = true;
        public Dictionary<NotificationType, bool> typePreferences = new();
        public List<string> mutedDevices = new();
        public TimeRange quietHours = new();
        public NotificationFrequency frequency = NotificationFrequency.Normal;
        public bool enableSound = true;
        public bool enableVibration = true;
        public bool enablePush = true;
        public DateTime lastUpdated;
    }

    [Serializable]
    public class TimeRange
    {
        public TimeSpan startTime;
        public TimeSpan endTime;
        public List<DayOfWeek> activeDays = new();
        public bool isEnabled;
    }

    public enum NotificationType
    {
        General,
        CreatureNeeds,
        BreedingComplete,
        Discovery,
        Achievement,
        SocialActivity,
        ResearchComplete,
        OfflineProgress,
        SystemMessage,
        Emergency
    }

    public enum NotificationPriority
    {
        Low,
        Normal,
        High,
        Urgent
    }

    public enum NotificationFrequency
    {
        Minimal,
        Reduced,
        Normal,
        Frequent,
        Maximum
    }

    #endregion

    #region Offline Progress

    [Serializable]
    public class OfflineProgress
    {
        public string userId;
        public DateTime lastOnlineTime;
        public DateTime calculationTime;
        public TimeSpan timeOffline;
        public List<CreatureData> creatures = new();
        public Dictionary<string, float> resources = new();
        public List<string> completedActivities = new();
        public OfflineProgressSettings settings = new();
        public bool isProcessed;
    }

    [Serializable]
    public class OfflineRewards
    {
        public string userId;
        public TimeSpan timeOffline;
        public DateTime calculationTime;
        public Dictionary<string, float> creatureGrowth = new();
        public Dictionary<string, float> resourcesGenerated = new();
        public float researchProgress;
        public List<string> newDiscoveries = new();
        public List<Achievement> achievementsEarned = new();
        public int experienceGained;
        public Dictionary<string, object> customRewards = new();
    }

    [Serializable]
    public class OfflineProgressSettings
    {
        public bool enableOfflineProgress = true;
        public float maxOfflineHours = 168f; // 7 days
        public float progressRateMultiplier = 0.5f;
        public bool enableCreatureGrowth = true;
        public bool enableResourceGeneration = true;
        public bool enableResearchProgress = true;
        public List<string> excludedActivities = new();
        public Dictionary<string, object> customSettings = new();
    }

    [Serializable]
    public class CreatureData
    {
        public string creatureId;
        public string creatureName;
        public string speciesId;
        public int level;
        public float experience;
        public float happiness;
        public float health;
        public DateTime lastFed;
        public DateTime lastInteraction;
        public List<string> activeEffects = new();
        public Dictionary<string, object> stats = new();
    }

    #endregion

    #region Second Screen Features

    [Serializable]
    public class SecondScreenEvent
    {
        public string eventId;
        public SecondScreenEventType eventType;
        public string title;
        public string description;
        public DateTime timestamp;
        public Dictionary<string, object> eventData = new();
        public List<string> targetDeviceTypes = new();
        public TimeSpan duration;
        public bool isInteractive;
        public List<SecondScreenAction> availableActions = new();
    }

    [Serializable]
    public class SecondScreenAction
    {
        public string actionId;
        public string actionText;
        public string actionType;
        public Dictionary<string, object> actionParameters = new();
        public bool isEnabled = true;
        public string iconUrl;
    }

    [Serializable]
    public class SecondScreenContent
    {
        public string contentId;
        public SecondScreenContentType contentType;
        public string title;
        public string description;
        public Dictionary<string, object> content = new();
        public List<string> mediaUrls = new();
        public bool isInteractive;
        public DateTime lastUpdated;
    }

    public enum SecondScreenEventType
    {
        CreatureView,
        InventoryDisplay,
        MapOverlay,
        ResearchData,
        SocialFeed,
        Statistics,
        Tutorial,
        Notification
    }

    public enum SecondScreenContentType
    {
        Text,
        Image,
        Video,
        Interactive,
        AR,
        DataVisualization,
        Map,
        Game
    }

    #endregion

    #region AR and 3D Features

    [Serializable]
    public class ARExperience
    {
        public string experienceId;
        public ARExperienceType experienceType;
        public string title;
        public string description;
        public List<ARAsset> assets = new();
        public ARConfiguration configuration = new();
        public bool requiresTracking;
        public List<string> supportedDevices = new();
        public DateTime lastUpdated;
    }

    [Serializable]
    public class ARAsset
    {
        public string assetId;
        public ARAssetType assetType;
        public string assetUrl;
        public Vector3 position;
        public Vector3 rotation;
        public Vector3 scale = Vector3.one;
        public Dictionary<string, object> properties = new();
        public bool isAnimated;
        public List<string> animations = new();
    }

    [Serializable]
    public class ARConfiguration
    {
        public bool enablePlaneDetection;
        public bool enableImageTracking;
        public bool enableFaceTracking;
        public bool enableLightEstimation;
        public bool enableOcclusion;
        public float maxRenderDistance = 10f;
        public int maxTrackedObjects = 5;
        public ARRenderQuality renderQuality = ARRenderQuality.Medium;
    }

    public enum ARExperienceType
    {
        CreatureViewing,
        EnvironmentExploration,
        BreedingVisualization,
        EducationalOverlay,
        SocialSharing,
        Discovery,
        Tutorial
    }

    public enum ARAssetType
    {
        Model3D,
        Animation,
        Particle,
        UI,
        Audio,
        Video,
        Interactive
    }

    public enum ARRenderQuality
    {
        Low,
        Medium,
        High,
        Ultra
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Cross-platform data synchronization service
    /// </summary>
    public interface ICrossPlatformSyncService
    {
        Task<bool> InitializeAsync();
        Task<CrossPlatformSyncData> GatherSyncDataAsync();
        Task<CrossPlatformSyncData> GatherInitialSyncDataAsync(string userId);
        Task<bool> ApplySyncDataAsync(CrossPlatformSyncData syncData);
        Task<List<SyncConflict>> DetectConflictsAsync(CrossPlatformSyncData localData, CrossPlatformSyncData remoteData);
        Task<bool> ResolveConflictsAsync(List<SyncConflict> conflicts);
    }

    /// <summary>
    /// Companion app communication service
    /// </summary>
    public interface ICompanionAppService
    {
        Task<bool> InitializeAsync();
        void StartDeviceDiscovery();
        void StopDeviceDiscovery();
        Task<bool> SendMessageToDeviceAsync(CompanionDevice device, CrossPlatformMessage message);
        Task<CrossPlatformMessage> ReceiveMessageAsync();
        List<CompanionDevice> GetDiscoveredDevices();
        Task<bool> AuthenticateDeviceAsync(CompanionDevice device);
    }

    /// <summary>
    /// Cloud data storage and synchronization service
    /// </summary>
    public interface ICloudDataService
    {
        Task<bool> InitializeAsync();
        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();
        Task<bool> SyncDataAsync(CrossPlatformSyncData syncData);
        Task<CrossPlatformSyncData> PullDataAsync();
        Task<bool> SendHeartbeatAsync();
        Task<bool> BackupDataAsync();
        CloudConnectionStatus GetConnectionStatus();
    }

    /// <summary>
    /// Push notification delivery service
    /// </summary>
    public interface IPushNotificationService
    {
        Task<bool> InitializeAsync();
        Task<bool> SendNotificationAsync(CompanionNotification notification);
        void ScheduleNotification(CompanionNotification notification, DateTime deliveryTime);
        void CancelNotification(string notificationId);
        void ProcessPendingNotifications();
        void UpdateUserPreferences(string userId, NotificationPreferences preferences);
        NotificationPreferences GetUserPreferences(string userId);
    }

    /// <summary>
    /// Offline progress calculation and application service
    /// </summary>
    public interface IOfflineProgressService
    {
        Task<bool> InitializeAsync();
        OfflineProgress CalculateOfflineProgress(string userId, DateTime lastOnlineTime);
        OfflineRewards CalculateOfflineRewards(OfflineProgress progress);
        Task<bool> ApplyOfflineRewardsAsync(string userId, OfflineRewards rewards);
        void UpdateOfflineSettings(string userId, OfflineProgressSettings settings);
        OfflineProgressSettings GetOfflineSettings(string userId);
    }

    /// <summary>
    /// Second screen experience service
    /// </summary>
    public interface ISecondScreenService
    {
        Task<bool> InitializeAsync();
        Task<bool> SendSecondScreenContentAsync(string deviceId, SecondScreenContent content);
        List<SecondScreenContent> GetAvailableContent(CompanionDeviceType deviceType);
        Task<bool> HandleSecondScreenActionAsync(string deviceId, SecondScreenAction action);
        void RegisterSecondScreenDevice(CompanionDevice device);
        void UnregisterSecondScreenDevice(string deviceId);
    }

    /// <summary>
    /// Remote monitoring and control service
    /// </summary>
    public interface IRemoteMonitoringService
    {
        Task<bool> InitializeAsync();
        Task<bool> ProcessRemoteActionAsync(RemoteAction action);
        List<RemoteActionType> GetAvailableActions(string userId);
        ActionPermissions GetActionPermissions(string userId);
        void UpdateActionPermissions(string userId, ActionPermissions permissions);
        Task<RemoteActionResult> ExecuteActionAsync(RemoteAction action);
    }

    #endregion

    #region Configuration and Settings

    [Serializable]
    public class DeviceSettings
    {
        public bool enableNotifications = true;
        public bool enableAutoSync = true;
        public bool enableOfflineMode = true;
        public bool enableSecondScreen = true;
        public bool enableRemoteActions = true;
        public SyncStrategy preferredSyncStrategy = SyncStrategy.Incremental;
        public int syncIntervalMinutes = 5;
        public NotificationPreferences notificationPreferences = new();
        public Dictionary<string, object> customSettings = new();
    }

    [Serializable]
    public class CompanionAppInfo
    {
        public string appId;
        public string appName;
        public string version;
        public string platform;
        public List<string> supportedFeatures = new();
        public Dictionary<string, object> capabilities = new();
        public DateTime lastUpdated;
        public bool isVerified;
    }

    [Serializable]
    public class CloudConnectionInfo
    {
        public string connectionId;
        public string cloudProvider;
        public string endpoint;
        public DateTime connectionTime;
        public CloudConnectionStatus status;
        public string lastError;
        public int retryCount;
        public Dictionary<string, object> connectionData = new();
    }

    [Serializable]
    public class Achievement
    {
        public string achievementId;
        public string achievementName;
        public string description;
        public DateTime earnedDate;
        public string iconUrl;
        public int pointValue;
        public AchievementRarity rarity;
    }

    public enum CloudConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Syncing,
        Error,
        Retrying
    }

    public enum AchievementRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    #endregion

    #region Analytics and Metrics

    [Serializable]
    public class CompanionAnalytics
    {
        public string userId;
        public DateTime analysisDate;
        public CompanionUsageMetrics usageMetrics = new();
        public DeviceUsageStats deviceUsage = new();
        public FeatureUsageStats featureUsage = new();
        public EngagementMetrics engagement = new();
        public Dictionary<string, object> customMetrics = new();
    }

    [Serializable]
    public class CompanionUsageMetrics
    {
        public int totalSessions;
        public TimeSpan totalTime;
        public TimeSpan averageSessionLength;
        public int actionsPerformed;
        public int notificationsReceived;
        public int synchronizations;
        public DateTime firstUse;
        public DateTime lastUse;
    }

    [Serializable]
    public class DeviceUsageStats
    {
        public Dictionary<CompanionDeviceType, int> deviceTypeUsage = new();
        public Dictionary<string, TimeSpan> timeByDevice = new();
        public Dictionary<string, int> actionsByDevice = new();
        public string mostUsedDevice;
        public CompanionDeviceType preferredDeviceType;
    }

    [Serializable]
    public class FeatureUsageStats
    {
        public int remoteActionsUsed;
        public int secondScreenInteractions;
        public int offlineProgressClaims;
        public int notificationInteractions;
        public int arExperienceViews;
        public Dictionary<string, int> featureUsageCounts = new();
        public List<string> mostUsedFeatures = new();
    }

    [Serializable]
    public class EngagementMetrics
    {
        public float engagementScore;
        public int consecutiveDaysUsed;
        public float retentionRate;
        public int shareActionsPerformed;
        public float satisfactionScore;
        public List<string> engagementFactors = new();
    }

    #endregion
}