using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Subsystems.Moderation
{
    /// <summary>
    /// Moderation & Safety Subsystem Manager for Project Chimera.
    /// Handles content moderation, safety filtering, COPPA compliance, and automated behavior detection.
    /// Essential for educational environments and community safety.
    /// </summary>
    public class ModerationSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private ModerationSubsystemConfig config;

        [Header("Services")]
        [SerializeField] private bool enableContentFiltering = true;
        [SerializeField] private bool enableBehaviorMonitoring = true;
        [SerializeField] private bool enableEducationalMode = true;
        [SerializeField] private bool enableAutomaticActions = false;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Moderation";
        public float InitializationProgress { get; private set; }

        // Services
        public IContentModerationService ContentModerationService { get; private set; }
        public IBehaviorMonitoringService BehaviorMonitoringService { get; private set; }
        public ISafetyComplianceService SafetyComplianceService { get; private set; }
        public IReportingService ReportingService { get; private set; }

        // Events
        public static event Action<ContentModerationEvent> OnContentModerated;
        public static event Action<BehaviorViolationEvent> OnBehaviorViolation;
        public static event Action<SafetyAlert> OnSafetyAlert;
        public static event Action<ModerationActionEvent> OnModerationAction;

        private readonly Queue<ModerationEvent> _moderationQueue = new();
        private readonly Dictionary<string, ModerationHistory> _userModerationHistory = new();
        private readonly List<ActiveModerationAlert> _activeAlerts = new();

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

        private void Update()
        {
            ProcessModerationQueue();
            UpdateActiveAlerts();
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
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign a ModerationSubsystemConfig.");
                return;
            }

            // Validate COPPA compliance settings
            if (enableEducationalMode && !config.coppaCompliant)
            {
                Debug.LogWarning($"[{SubsystemName}] Educational mode enabled but COPPA compliance is disabled. " +
                                "This may not be suitable for educational environments.");
            }

            // Validate filter lists
            if (config.profanityFilter == null || config.profanityFilter.Count == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No profanity filter configured. Content filtering may be ineffective.");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.2f;

            // Initialize moderation services
            ContentModerationService = new ContentModerationService(config);
            BehaviorMonitoringService = new BehaviorMonitoringService(config);
            SafetyComplianceService = new SafetyComplianceService(config);
            ReportingService = new ReportingService(config);

            InitializationProgress = 0.4f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.5f;

                // Initialize content filtering
                await ContentModerationService.InitializeAsync();
                InitializationProgress = 0.6f;

                // Initialize behavior monitoring
                await BehaviorMonitoringService.InitializeAsync();
                InitializationProgress = 0.7f;

                // Initialize safety compliance
                await SafetyComplianceService.InitializeAsync();
                InitializationProgress = 0.8f;

                // Initialize reporting service
                await ReportingService.InitializeAsync();
                InitializationProgress = 0.9f;

                // Subscribe to game events
                SubscribeToGameEvents();

                // Register services
                RegisterServices();

                // Start background moderation processing
                _ = StartModerationProcessingLoop();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Content Filtering: {enableContentFiltering}, " +
                         $"Behavior Monitoring: {enableBehaviorMonitoring}, " +
                         $"Educational Mode: {enableEducationalMode}, " +
                         $"Auto Actions: {enableAutomaticActions}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private void SubscribeToGameEvents()
        {
            // Subscribe to player action events for behavior monitoring
            if (BehaviorMonitoringService != null)
            {
                Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnPlayerActionTracked += HandlePlayerAction;
            }

            // Subscribe to social events for content moderation
            // These would be connected when social systems are implemented
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<IContentModerationService>(ContentModerationService);
                ServiceContainer.Instance.Register<IBehaviorMonitoringService>(BehaviorMonitoringService);
                ServiceContainer.Instance.Register<ISafetyComplianceService>(SafetyComplianceService);
                ServiceContainer.Instance.Register<IReportingService>(ReportingService);
                ServiceContainer.Instance.Register<ModerationSubsystemManager>(this);
            }
        }

        #endregion

        #region Core Moderation Operations

        /// <summary>
        /// Moderates user-generated content (creature names, messages, etc.)
        /// </summary>
        public async Task<ContentModerationResult> ModerateContentAsync(string content, ContentType contentType, string userId = null)
        {
            if (!IsInitialized || !enableContentFiltering)
                return new ContentModerationResult { originalContent = content, moderatedContent = content, isClean = true };

            return await ContentModerationService.ModerateContentAsync(content, contentType, userId);
        }

        /// <summary>
        /// Reports suspicious or inappropriate behavior
        /// </summary>
        public async Task<bool> ReportBehaviorAsync(string reportedUserId, string reportingUserId, BehaviorViolationType violationType, string description = null)
        {
            if (!IsInitialized)
                return false;

            var report = new BehaviorReport
            {
                reportedUserId = reportedUserId,
                reportingUserId = reportingUserId,
                violationType = violationType,
                description = description,
                timestamp = DateTime.Now,
                reportId = Guid.NewGuid().ToString()
            };

            return await ReportingService.SubmitReportAsync(report);
        }

        /// <summary>
        /// Checks if a user is currently restricted
        /// </summary>
        public bool IsUserRestricted(string userId, RestrictionType restrictionType = RestrictionType.Any)
        {
            if (!_userModerationHistory.TryGetValue(userId, out var history))
                return false;

            var now = DateTime.Now;
            foreach (var action in history.moderationActions)
            {
                if (action.actionType == ModerationActionType.Restriction &&
                    action.restrictionType == restrictionType &&
                    action.expirationTime > now)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets moderation history for a user
        /// </summary>
        public ModerationHistory GetUserModerationHistory(string userId)
        {
            return _userModerationHistory.GetValueOrDefault(userId, new ModerationHistory { userId = userId });
        }

        /// <summary>
        /// Applies a moderation action to a user
        /// </summary>
        public async Task<bool> ApplyModerationActionAsync(string userId, ModerationActionType actionType, TimeSpan? duration = null, string reason = null)
        {
            if (!IsInitialized)
                return false;

            var action = new ModerationAction
            {
                actionId = Guid.NewGuid().ToString(),
                userId = userId,
                actionType = actionType,
                reason = reason ?? "Automated moderation action",
                timestamp = DateTime.Now,
                expirationTime = duration.HasValue ? DateTime.Now.Add(duration.Value) : DateTime.MaxValue,
                moderatorId = "System"
            };

            // Add to user history
            if (!_userModerationHistory.ContainsKey(userId))
                _userModerationHistory[userId] = new ModerationHistory { userId = userId };

            _userModerationHistory[userId].moderationActions.Add(action);

            // Queue moderation event
            var moderationEvent = new ModerationActionEvent
            {
                action = action,
                timestamp = DateTime.Now
            };

            _moderationQueue.Enqueue(moderationEvent);

            Debug.Log($"[{SubsystemName}] Applied {actionType} to user {userId}: {reason}");

            OnModerationAction?.Invoke(moderationEvent);

            return true;
        }

        /// <summary>
        /// Creates a safety alert for review
        /// </summary>
        public void CreateSafetyAlert(SafetyAlertType alertType, string description, string userId = null, SafetyAlertSeverity severity = SafetyAlertSeverity.Medium)
        {
            var alert = new SafetyAlert
            {
                alertId = Guid.NewGuid().ToString(),
                alertType = alertType,
                description = description,
                userId = userId,
                severity = severity,
                timestamp = DateTime.Now,
                status = SafetyAlertStatus.Active
            };

            _activeAlerts.Add(new ActiveModerationAlert { alert = alert, lastUpdate = DateTime.Now });

            OnSafetyAlert?.Invoke(alert);

            Debug.Log($"[{SubsystemName}] Safety alert created: {alertType} - {description}");
        }

        #endregion

        #region Content Validation

        /// <summary>
        /// Validates creature names for appropriateness
        /// </summary>
        public async Task<bool> ValidateCreatureNameAsync(string creatureName, string ownerId)
        {
            if (!enableContentFiltering)
                return true;

            var result = await ModerateContentAsync(creatureName, ContentType.CreatureName, ownerId);
            return result.isClean;
        }

        /// <summary>
        /// Validates user messages and descriptions
        /// </summary>
        public async Task<string> ValidateAndFilterMessageAsync(string message, string senderId)
        {
            if (!enableContentFiltering)
                return message;

            var result = await ModerateContentAsync(message, ContentType.Message, senderId);

            if (!result.isClean && enableAutomaticActions)
            {
                // Record violation
                RecordContentViolation(senderId, result);
            }

            return result.moderatedContent;
        }

        /// <summary>
        /// Validates research publication content
        /// </summary>
        public async Task<ContentModerationResult> ValidateResearchContentAsync(string title, string content, string authorId)
        {
            if (!enableContentFiltering)
                return new ContentModerationResult { originalContent = content, moderatedContent = content, isClean = true };

            // Validate title
            var titleResult = await ModerateContentAsync(title, ContentType.ResearchTitle, authorId);

            // Validate content
            var contentResult = await ModerateContentAsync(content, ContentType.ResearchContent, authorId);

            return new ContentModerationResult
            {
                originalContent = content,
                moderatedContent = contentResult.moderatedContent,
                isClean = titleResult.isClean && contentResult.isClean,
                violationReason = titleResult.violationReason ?? contentResult.violationReason,
                moderationDetails = new Dictionary<string, object>
                {
                    ["titleClean"] = titleResult.isClean,
                    ["contentClean"] = contentResult.isClean
                }
            };
        }

        #endregion

        #region Event Processing

        private void ProcessModerationQueue()
        {
            const int maxEventsPerFrame = 5;
            int processedCount = 0;

            while (_moderationQueue.Count > 0 && processedCount < maxEventsPerFrame)
            {
                var moderationEvent = _moderationQueue.Dequeue();
                ProcessModerationEvent(moderationEvent);
                processedCount++;
            }
        }

        private void ProcessModerationEvent(ModerationEvent moderationEvent)
        {
            switch (moderationEvent)
            {
                case ContentModerationEvent contentEvent:
                    ProcessContentModerationEvent(contentEvent);
                    break;

                case BehaviorViolationEvent behaviorEvent:
                    ProcessBehaviorViolationEvent(behaviorEvent);
                    break;

                case ModerationActionEvent actionEvent:
                    ProcessModerationActionEvent(actionEvent);
                    break;
            }
        }

        private void ProcessContentModerationEvent(ContentModerationEvent contentEvent)
        {
            OnContentModerated?.Invoke(contentEvent);

            if (!contentEvent.result.isClean && enableAutomaticActions)
            {
                // Escalate based on severity
                switch (contentEvent.result.severity)
                {
                    case ViolationSeverity.Minor:
                        CreateSafetyAlert(SafetyAlertType.InappropriateContent, $"Minor content violation: {contentEvent.result.violationReason}", contentEvent.userId, SafetyAlertSeverity.Low);
                        break;

                    case ViolationSeverity.Major:
                        CreateSafetyAlert(SafetyAlertType.InappropriateContent, $"Major content violation: {contentEvent.result.violationReason}", contentEvent.userId, SafetyAlertSeverity.High);
                        _ = ApplyModerationActionAsync(contentEvent.userId, ModerationActionType.Warning, reason: contentEvent.result.violationReason);
                        break;

                    case ViolationSeverity.Severe:
                        CreateSafetyAlert(SafetyAlertType.InappropriateContent, $"Severe content violation: {contentEvent.result.violationReason}", contentEvent.userId, SafetyAlertSeverity.Critical);
                        _ = ApplyModerationActionAsync(contentEvent.userId, ModerationActionType.Restriction, TimeSpan.FromHours(24), contentEvent.result.violationReason);
                        break;
                }
            }
        }

        private void ProcessBehaviorViolationEvent(BehaviorViolationEvent behaviorEvent)
        {
            OnBehaviorViolation?.Invoke(behaviorEvent);

            if (enableAutomaticActions)
            {
                // Apply automatic actions based on violation type
                switch (behaviorEvent.violationType)
                {
                    case BehaviorViolationType.Harassment:
                    case BehaviorViolationType.Bullying:
                        _ = ApplyModerationActionAsync(behaviorEvent.userId, ModerationActionType.Restriction, TimeSpan.FromHours(12), behaviorEvent.description);
                        break;

                    case BehaviorViolationType.Spam:
                        _ = ApplyModerationActionAsync(behaviorEvent.userId, ModerationActionType.Warning, reason: "Spam behavior detected");
                        break;

                    case BehaviorViolationType.Cheating:
                        _ = ApplyModerationActionAsync(behaviorEvent.userId, ModerationActionType.Suspension, TimeSpan.FromDays(7), "Cheating behavior detected");
                        break;
                }
            }
        }

        private void ProcessModerationActionEvent(ModerationActionEvent actionEvent)
        {
            // Log moderation action
            if (config.enableModerationLogging)
            {
                LogModerationAction(actionEvent.action);
            }
        }

        private void UpdateActiveAlerts()
        {
            var now = DateTime.Now;

            // Auto-resolve old alerts
            for (int i = _activeAlerts.Count - 1; i >= 0; i--)
            {
                var alert = _activeAlerts[i];
                var alertAge = now - alert.alert.timestamp;

                if (alertAge.TotalHours > config.autoResolveAlertHours)
                {
                    alert.alert.status = SafetyAlertStatus.AutoResolved;
                    _activeAlerts.RemoveAt(i);
                    Debug.Log($"[{SubsystemName}] Auto-resolved alert: {alert.alert.alertId}");
                }
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerAction(Laboratory.Subsystems.Analytics.PlayerActionEvent actionEvent)
        {
            if (!enableBehaviorMonitoring)
                return;

            // Monitor for suspicious behavior patterns
            BehaviorMonitoringService.AnalyzePlayerBehavior(actionEvent);
        }

        private void RecordContentViolation(string userId, ContentModerationResult result)
        {
            var violation = new ContentViolation
            {
                userId = userId,
                content = result.originalContent,
                violationType = result.violationReason,
                severity = result.severity,
                timestamp = DateTime.Now
            };

            if (!_userModerationHistory.ContainsKey(userId))
                _userModerationHistory[userId] = new ModerationHistory { userId = userId };

            _userModerationHistory[userId].contentViolations.Add(violation);
        }

        #endregion

        #region Background Processing

        private async Task StartModerationProcessingLoop()
        {
            while (IsInitialized)
            {
                try
                {
                    await Task.Delay(config.processingIntervalMs);

                    // Update behavior analysis
                    if (enableBehaviorMonitoring)
                    {
                        BehaviorMonitoringService.UpdateBehaviorAnalysis();
                    }

                    // Clean up expired restrictions
                    CleanupExpiredRestrictions();

                    // Update safety compliance metrics
                    SafetyComplianceService.UpdateComplianceMetrics();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{SubsystemName}] Background processing error: {ex.Message}");
                }
            }
        }

        private void CleanupExpiredRestrictions()
        {
            var now = DateTime.Now;

            foreach (var history in _userModerationHistory.Values)
            {
                for (int i = history.moderationActions.Count - 1; i >= 0; i--)
                {
                    var action = history.moderationActions[i];
                    if (action.expirationTime <= now && action.actionType == ModerationActionType.Restriction)
                    {
                        Debug.Log($"[{SubsystemName}] Restriction expired for user {history.userId}");
                        // Keep in history but mark as expired
                        action.isExpired = true;
                    }
                }
            }
        }

        #endregion

        #region Logging

        private void LogModerationAction(ModerationAction action)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Moderation Action - " +
                          $"User: {action.userId}, " +
                          $"Action: {action.actionType}, " +
                          $"Reason: {action.reason}, " +
                          $"Moderator: {action.moderatorId}";

            Debug.Log($"[{SubsystemName}] {logEntry}");

            if (config.saveModerationLogs)
            {
                SaveModerationLog(logEntry);
            }
        }

        private void SaveModerationLog(string logEntry)
        {
            try
            {
                var logPath = System.IO.Path.Combine(Application.persistentDataPath, "Moderation", "moderation.log");
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(logPath));
                System.IO.File.AppendAllText(logPath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Failed to save moderation log: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnPlayerActionTracked -= HandlePlayerAction;

            // Clear collections
            _moderationQueue.Clear();
            _userModerationHistory.Clear();
            _activeAlerts.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Content Moderation")]
        private void TestContentModeration()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Moderation subsystem not initialized");
                return;
            }

            _ = ModerateContentAsync("This is a test creature name", ContentType.CreatureName, "TestUser");
        }

        [ContextMenu("Create Test Safety Alert")]
        private void CreateTestSafetyAlert()
        {
            CreateSafetyAlert(SafetyAlertType.InappropriateContent, "Test safety alert for debugging", "TestUser", SafetyAlertSeverity.Medium);
        }

        [ContextMenu("Apply Test Moderation Action")]
        private void ApplyTestModerationAction()
        {
            _ = ApplyModerationActionAsync("TestUser", ModerationActionType.Warning, reason: "Test moderation action");
        }

        #endregion
    }
}