using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Companion
{
    /// <summary>
    /// Configuration ScriptableObject for the Cross-Platform Companion Subsystem.
    /// Controls companion app integration, cloud sync, and cross-platform features.
    /// </summary>
    [CreateAssetMenu(fileName = "CompanionSubsystemConfig", menuName = "Project Chimera/Subsystems/Companion Config")]
    public class CompanionSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Enable companion app support")]
        public bool enableCompanionAppSupport = true;

        [Tooltip("Enable debug logging for companion operations")]
        public bool enableDebugLogging = false;

        [Tooltip("Maximum messages processed per update cycle")]
        [Range(1, 100)]
        public int maxMessagesPerUpdate = 20;

        [Tooltip("Device discovery timeout in seconds")]
        [Range(5f, 60f)]
        public float deviceDiscoveryTimeoutSeconds = 30f;

        [Tooltip("Device connection timeout in seconds")]
        [Range(10f, 300f)]
        public float deviceTimeoutSeconds = 120f;

        [Header("Cloud Synchronization")]
        [Tooltip("Enable cloud data synchronization")]
        public bool enableCloudSync = true;

        [Tooltip("Cloud sync interval in seconds")]
        [Range(30f, 3600f)]
        public float syncIntervalSeconds = 300f;

        [Tooltip("Sync retry interval in seconds")]
        [Range(10f, 300f)]
        public float syncRetryIntervalSeconds = 60f;

        [Tooltip("Maximum sync retry attempts")]
        [Range(1, 10)]
        public int maxSyncRetryAttempts = 3;

        [Tooltip("Enable incremental sync")]
        public bool enableIncrementalSync = true;

        [Tooltip("Enable sync conflict resolution")]
        public bool enableSyncConflictResolution = true;

        [Tooltip("Default conflict resolution strategy")]
        public ConflictResolution defaultConflictResolution = ConflictResolution.Timestamp;

        [Header("Cross-Platform Communication")]
        [Tooltip("Enable device-to-device communication")]
        public bool enableDeviceToDeviceCommunication = true;

        [Tooltip("Message encryption enabled")]
        public bool enableMessageEncryption = true;

        [Tooltip("Message compression enabled")]
        public bool enableMessageCompression = true;

        [Tooltip("Message timeout in seconds")]
        [Range(5f, 120f)]
        public float messageTimeoutSeconds = 30f;

        [Tooltip("Heartbeat interval in seconds")]
        [Range(30f, 300f)]
        public float heartbeatIntervalSeconds = 60f;

        [Header("Remote Actions")]
        [Tooltip("Enable remote action execution")]
        public bool enableRemoteActions = true;

        [Tooltip("Require authentication for remote actions")]
        public bool requireActionAuthentication = true;

        [Tooltip("Enable action confirmation dialogs")]
        public bool enableActionConfirmations = true;

        [Tooltip("Maximum remote actions per hour")]
        [Range(1, 1000)]
        public int maxRemoteActionsPerHour = 100;

        [Tooltip("Remote action timeout in seconds")]
        [Range(5f, 300f)]
        public float remoteActionTimeoutSeconds = 60f;

        [Tooltip("Supported remote actions")]
        public List<RemoteActionType> supportedRemoteActions = new List<RemoteActionType>
        {
            RemoteActionType.ViewCreature,
            RemoteActionType.FeedCreature,
            RemoteActionType.CheckProgress,
            RemoteActionType.CollectRewards,
            RemoteActionType.SetNotificationPreferences
        };

        [Header("Push Notifications")]
        [Tooltip("Enable push notifications")]
        public bool enablePushNotifications = true;

        [Tooltip("Maximum notifications per day per user")]
        [Range(1, 100)]
        public int maxNotificationsPerDay = 20;

        [Tooltip("Notification retry attempts")]
        [Range(1, 5)]
        public int notificationRetryAttempts = 3;

        [Tooltip("Notification expiration hours")]
        [Range(1f, 168f)]
        public float notificationExpirationHours = 24f;

        [Tooltip("Enable notification batching")]
        public bool enableNotificationBatching = true;

        [Tooltip("Notification batch interval minutes")]
        [Range(1f, 60f)]
        public float notificationBatchIntervalMinutes = 5f;

        [Header("Offline Progress")]
        [Tooltip("Enable offline progress calculation")]
        public bool enableOfflineProgress = true;

        [Tooltip("Maximum offline progress hours")]
        [Range(1f, 168f)]
        public float maxOfflineProgressHours = 72f;

        [Tooltip("Offline progress rate multiplier")]
        [Range(0.1f, 2f)]
        public float offlineProgressMultiplier = 0.5f;

        [Tooltip("Enable offline creature care")]
        public bool enableOfflineCreatureCare = true;

        [Tooltip("Enable offline resource generation")]
        public bool enableOfflineResourceGeneration = true;

        [Tooltip("Enable offline research progress")]
        public bool enableOfflineResearchProgress = true;

        [Tooltip("Offline progress categories")]
        public List<OfflineProgressCategory> offlineProgressCategories = new List<OfflineProgressCategory>
        {
            new OfflineProgressCategory { categoryName = "Creature Growth", isEnabled = true, rateMultiplier = 0.3f },
            new OfflineProgressCategory { categoryName = "Resource Generation", isEnabled = true, rateMultiplier = 0.5f },
            new OfflineProgressCategory { categoryName = "Research Progress", isEnabled = true, rateMultiplier = 0.2f }
        };

        [Header("Second Screen Features")]
        [Tooltip("Enable second screen functionality")]
        public bool enableSecondScreen = true;

        [Tooltip("Enable dual-device gameplay")]
        public bool enableDualDeviceGameplay = true;

        [Tooltip("Enable real-time data display")]
        public bool enableRealTimeDataDisplay = true;

        [Tooltip("Second screen update interval seconds")]
        [Range(1f, 30f)]
        public float secondScreenUpdateIntervalSeconds = 5f;

        [Tooltip("Enable interactive second screen content")]
        public bool enableInteractiveSecondScreen = true;

        [Tooltip("Supported second screen content types")]
        public List<SecondScreenContentType> supportedSecondScreenContent = new List<SecondScreenContentType>
        {
            SecondScreenContentType.Text,
            SecondScreenContentType.Image,
            SecondScreenContentType.Interactive,
            SecondScreenContentType.DataVisualization
        };

        [Header("AR/VR Integration")]
        [Tooltip("Enable AR companion features")]
        public bool enableARFeatures = true;

        [Tooltip("Enable VR companion features")]
        public bool enableVRFeatures = false;

        [Tooltip("AR render quality")]
        public ARRenderQuality arRenderQuality = ARRenderQuality.Medium;

        [Tooltip("Maximum AR objects")]
        [Range(1, 20)]
        public int maxARObjects = 5;

        [Tooltip("AR tracking timeout seconds")]
        [Range(5f, 60f)]
        public float arTrackingTimeoutSeconds = 30f;

        [Tooltip("Enable AR creature viewing")]
        public bool enableARCreatureViewing = true;

        [Tooltip("Enable AR environment overlay")]
        public bool enableAREnvironmentOverlay = true;

        [Header("Platform-Specific Settings")]
        [Tooltip("Mobile platform settings")]
        public MobilePlatformSettings mobileSettings = new MobilePlatformSettings();

        [Tooltip("Desktop platform settings")]
        public DesktopPlatformSettings desktopSettings = new DesktopPlatformSettings();

        [Tooltip("Web platform settings")]
        public WebPlatformSettings webSettings = new WebPlatformSettings();

        [Header("Security and Privacy")]
        [Tooltip("Enable end-to-end encryption")]
        public bool enableEndToEndEncryption = true;

        [Tooltip("Enable data anonymization")]
        public bool enableDataAnonymization = true;

        [Tooltip("Data retention period days")]
        [Range(1, 2555)]
        public int dataRetentionDays = 365;

        [Tooltip("Enable privacy mode")]
        public bool enablePrivacyMode = false;

        [Tooltip("Require user consent for data sharing")]
        public bool requireUserConsentForDataSharing = true;

        [Header("Performance")]
        [Tooltip("Enable performance optimization")]
        public bool enablePerformanceOptimization = true;

        [Tooltip("Message queue size limit")]
        [Range(100, 10000)]
        public int messageQueueSizeLimit = 1000;

        [Tooltip("Sync data compression level")]
        [Range(0, 9)]
        public int syncDataCompressionLevel = 6;

        [Tooltip("Enable background sync")]
        public bool enableBackgroundSync = true;

        [Tooltip("Connection pool size")]
        [Range(1, 20)]
        public int connectionPoolSize = 5;

        [Header("Analytics")]
        [Tooltip("Enable companion analytics")]
        public bool enableCompanionAnalytics = true;

        [Tooltip("Analytics data collection interval minutes")]
        [Range(1f, 60f)]
        public float analyticsCollectionIntervalMinutes = 15f;

        [Tooltip("Enable usage metrics")]
        public bool enableUsageMetrics = true;

        [Tooltip("Enable engagement tracking")]
        public bool enableEngagementTracking = true;

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable timing values
            syncIntervalSeconds = Mathf.Max(30f, syncIntervalSeconds);
            syncRetryIntervalSeconds = Mathf.Max(10f, syncRetryIntervalSeconds);
            deviceTimeoutSeconds = Mathf.Max(10f, deviceTimeoutSeconds);
            messageTimeoutSeconds = Mathf.Max(5f, messageTimeoutSeconds);

            // Ensure reasonable limits
            maxMessagesPerUpdate = Mathf.Max(1, maxMessagesPerUpdate);
            maxSyncRetryAttempts = Mathf.Max(1, maxSyncRetryAttempts);
            maxRemoteActionsPerHour = Mathf.Max(1, maxRemoteActionsPerHour);
            maxNotificationsPerDay = Mathf.Max(1, maxNotificationsPerDay);

            // Ensure offline progress settings are reasonable
            maxOfflineProgressHours = Mathf.Max(1f, maxOfflineProgressHours);
            offlineProgressMultiplier = Mathf.Clamp(offlineProgressMultiplier, 0.1f, 2f);

            // Ensure supported actions have defaults
            if (supportedRemoteActions.Count == 0)
            {
                supportedRemoteActions.AddRange(GetDefaultSupportedActions());
            }

            // Ensure offline progress categories have defaults
            if (offlineProgressCategories.Count == 0)
            {
                offlineProgressCategories.AddRange(GetDefaultOfflineProgressCategories());
            }

            // Ensure second screen content types have defaults
            if (supportedSecondScreenContent.Count == 0)
            {
                supportedSecondScreenContent.AddRange(GetDefaultSecondScreenContent());
            }

            // Validate AR settings
            maxARObjects = Mathf.Max(1, maxARObjects);
            arTrackingTimeoutSeconds = Mathf.Max(5f, arTrackingTimeoutSeconds);

            // Validate security settings
            dataRetentionDays = Mathf.Max(1, dataRetentionDays);

            // Validate performance settings
            messageQueueSizeLimit = Mathf.Max(100, messageQueueSizeLimit);
            syncDataCompressionLevel = Mathf.Clamp(syncDataCompressionLevel, 0, 9);
            connectionPoolSize = Mathf.Max(1, connectionPoolSize);
        }

        private List<RemoteActionType> GetDefaultSupportedActions()
        {
            return new List<RemoteActionType>
            {
                RemoteActionType.ViewCreature,
                RemoteActionType.FeedCreature,
                RemoteActionType.CheckProgress,
                RemoteActionType.CollectRewards,
                RemoteActionType.SetNotificationPreferences,
                RemoteActionType.ViewInventory,
                RemoteActionType.ViewAchievements
            };
        }

        private List<OfflineProgressCategory> GetDefaultOfflineProgressCategories()
        {
            return new List<OfflineProgressCategory>
            {
                new OfflineProgressCategory { categoryName = "Creature Growth", isEnabled = true, rateMultiplier = 0.3f },
                new OfflineProgressCategory { categoryName = "Resource Generation", isEnabled = true, rateMultiplier = 0.5f },
                new OfflineProgressCategory { categoryName = "Research Progress", isEnabled = true, rateMultiplier = 0.2f },
                new OfflineProgressCategory { categoryName = "Discovery Progress", isEnabled = true, rateMultiplier = 0.1f },
                new OfflineProgressCategory { categoryName = "Experience Gain", isEnabled = true, rateMultiplier = 0.4f }
            };
        }

        private List<SecondScreenContentType> GetDefaultSecondScreenContent()
        {
            return new List<SecondScreenContentType>
            {
                SecondScreenContentType.Text,
                SecondScreenContentType.Image,
                SecondScreenContentType.Interactive,
                SecondScreenContentType.DataVisualization,
                SecondScreenContentType.Map
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets platform-specific settings
        /// </summary>
        public IPlatformCompanionSettings GetPlatformSettings()
        {
#if UNITY_ANDROID || UNITY_IOS
            return mobileSettings;
#elif UNITY_STANDALONE
            return desktopSettings;
#elif UNITY_WEBGL
            return webSettings;
#else
            return desktopSettings;
#endif
        }

        /// <summary>
        /// Checks if remote action is supported
        /// </summary>
        public bool IsRemoteActionSupported(RemoteActionType actionType)
        {
            return enableRemoteActions && supportedRemoteActions.Contains(actionType);
        }

        /// <summary>
        /// Gets offline progress category settings
        /// </summary>
        public OfflineProgressCategory GetOfflineProgressCategory(string categoryName)
        {
            return offlineProgressCategories.Find(c => c.categoryName == categoryName);
        }

        /// <summary>
        /// Checks if second screen content type is supported
        /// </summary>
        public bool IsSecondScreenContentSupported(SecondScreenContentType contentType)
        {
            return enableSecondScreen && supportedSecondScreenContent.Contains(contentType);
        }

        /// <summary>
        /// Calculates notification rate limit for user
        /// </summary>
        public bool IsWithinNotificationLimit(int notificationsSentToday)
        {
            return notificationsSentToday < maxNotificationsPerDay;
        }

        /// <summary>
        /// Calculates remote action rate limit for user
        /// </summary>
        public bool IsWithinRemoteActionLimit(int actionsThisHour)
        {
            return actionsThisHour < maxRemoteActionsPerHour;
        }

        /// <summary>
        /// Gets sync strategy based on configuration
        /// </summary>
        public SyncStrategy GetRecommendedSyncStrategy(int dataSize, bool isRealTime)
        {
            if (isRealTime)
                return SyncStrategy.Priority;
            else if (enableIncrementalSync && dataSize > 1024 * 1024) // 1MB
                return SyncStrategy.Incremental;
            else if (dataSize > 10 * 1024 * 1024) // 10MB
                return SyncStrategy.Compressed;
            else
                return SyncStrategy.Full;
        }

        /// <summary>
        /// Validates companion device capabilities
        /// </summary>
        public bool ValidateDeviceCapabilities(CompanionDevice device, List<string> requiredCapabilities)
        {
            if (!enableCompanionAppSupport)
                return false;

            foreach (var capability in requiredCapabilities)
            {
                if (!device.capabilities.Contains(capability))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets notification preferences for user
        /// </summary>
        public NotificationPreferences GetDefaultNotificationPreferences()
        {
            return new NotificationPreferences
            {
                enableNotifications = enablePushNotifications,
                typePreferences = new Dictionary<NotificationType, bool>
                {
                    { NotificationType.CreatureNeeds, true },
                    { NotificationType.BreedingComplete, true },
                    { NotificationType.Discovery, true },
                    { NotificationType.Achievement, true },
                    { NotificationType.OfflineProgress, true },
                    { NotificationType.SocialActivity, false },
                    { NotificationType.SystemMessage, true },
                    { NotificationType.Emergency, true }
                },
                frequency = NotificationFrequency.Normal,
                enableSound = true,
                enableVibration = true,
                enablePush = true
            };
        }

        #endregion
    }

    #region Platform Settings

    public interface IPlatformCompanionSettings
    {
        bool EnableBackgroundSync { get; }
        int MaxConcurrentConnections { get; }
        bool EnablePushNotifications { get; }
        bool EnableARFeatures { get; }
    }

    [System.Serializable]
    public class MobilePlatformSettings : IPlatformCompanionSettings
    {
        [Header("Mobile Optimization")]
        public bool enableBackgroundSync = true;
        public int maxConcurrentConnections = 3;
        public bool enablePushNotifications = true;
        public bool enableARFeatures = true;
        public bool enableBatteryOptimization = true;
        public bool enableDataSavingMode = true;
        public float backgroundSyncIntervalMinutes = 15f;

        public bool EnableBackgroundSync => enableBackgroundSync;
        public int MaxConcurrentConnections => maxConcurrentConnections;
        public bool EnablePushNotifications => enablePushNotifications;
        public bool EnableARFeatures => enableARFeatures;
    }

    [System.Serializable]
    public class DesktopPlatformSettings : IPlatformCompanionSettings
    {
        [Header("Desktop Features")]
        public bool enableBackgroundSync = true;
        public int maxConcurrentConnections = 10;
        public bool enablePushNotifications = false;
        public bool enableARFeatures = false;
        public bool enableMultiMonitorSupport = true;
        public bool enableSystemTrayIntegration = true;
        public bool enableKeyboardShortcuts = true;

        public bool EnableBackgroundSync => enableBackgroundSync;
        public int MaxConcurrentConnections => maxConcurrentConnections;
        public bool EnablePushNotifications => enablePushNotifications;
        public bool EnableARFeatures => enableARFeatures;
    }

    [System.Serializable]
    public class WebPlatformSettings : IPlatformCompanionSettings
    {
        [Header("Web Browser Features")]
        public bool enableBackgroundSync = false;
        public int maxConcurrentConnections = 5;
        public bool enablePushNotifications = true;
        public bool enableARFeatures = false;
        public bool enableWebAssembly = true;
        public bool enableServiceWorker = true;
        public bool enableOfflineStorage = true;

        public bool EnableBackgroundSync => enableBackgroundSync;
        public int MaxConcurrentConnections => maxConcurrentConnections;
        public bool EnablePushNotifications => enablePushNotifications;
        public bool EnableARFeatures => enableARFeatures;
    }

    #endregion

    #region Configuration Classes

    [System.Serializable]
    public class OfflineProgressCategory
    {
        public string categoryName;
        public bool isEnabled = true;
        public float rateMultiplier = 0.5f;
        public float maxAccumulationHours = 72f;
        public List<string> requiredConditions = new List<string>();
        public Dictionary<string, object> customSettings = new Dictionary<string, object>();
    }

    #endregion
}