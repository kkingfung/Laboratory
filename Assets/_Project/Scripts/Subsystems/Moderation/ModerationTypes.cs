using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Laboratory.Subsystems.Moderation
{
    #region Core Moderation Events

    [Serializable]
    public abstract class ModerationEvent
    {
        public DateTime timestamp;
        public string eventId = Guid.NewGuid().ToString();
        public abstract string EventType { get; }
    }

    [Serializable]
    public class ContentModerationEvent : ModerationEvent
    {
        public string userId;
        public ContentType contentType;
        public ContentModerationResult result;
        public override string EventType => "ContentModeration";
    }

    [Serializable]
    public class BehaviorViolationEvent : ModerationEvent
    {
        public string userId;
        public BehaviorViolationType violationType;
        public string description;
        public float severity;
        public override string EventType => "BehaviorViolation";
    }

    [Serializable]
    public class ModerationActionEvent : ModerationEvent
    {
        public ModerationAction action;
        public override string EventType => "ModerationAction";
    }

    #endregion

    #region Content Moderation

    [Serializable]
    public class ContentModerationResult
    {
        public string originalContent;
        public string moderatedContent;
        public bool isClean;
        public string violationReason;
        public ViolationSeverity severity = ViolationSeverity.Minor;
        public List<string> detectedWords = new();
        public Dictionary<string, object> moderationDetails = new();
    }

    [Serializable]
    public class ContentViolation
    {
        public string userId;
        public string content;
        public string violationType;
        public ViolationSeverity severity;
        public DateTime timestamp;
        public bool wasAutomaticallyDetected = true;
    }

    public enum ContentType
    {
        CreatureName,
        Message,
        ResearchTitle,
        ResearchContent,
        UserProfile,
        Comment,
        Description
    }

    public enum ViolationSeverity
    {
        Minor,
        Major,
        Severe
    }

    #endregion

    #region Behavior Monitoring

    [Serializable]
    public class BehaviorReport
    {
        public string reportId;
        public string reportedUserId;
        public string reportingUserId;
        public BehaviorViolationType violationType;
        public string description;
        public DateTime timestamp;
        public BehaviorReportStatus status = BehaviorReportStatus.Pending;
        public Dictionary<string, object> evidence = new();
    }

    [Serializable]
    public class BehaviorPattern
    {
        public string userId;
        public string patternType;
        public float frequency;
        public float riskScore;
        public DateTime firstDetected;
        public DateTime lastOccurrence;
        public List<string> relatedActions = new();
    }

    public enum BehaviorViolationType
    {
        Harassment,
        Bullying,
        Spam,
        Cheating,
        ExploitingBugs,
        InappropriateNaming,
        Griefing,
        RealMoneyTrading,
        AccountSharing
    }

    public enum BehaviorReportStatus
    {
        Pending,
        UnderReview,
        Resolved,
        Dismissed,
        Escalated
    }

    #endregion

    #region Moderation Actions

    [Serializable]
    public class ModerationAction
    {
        public string actionId;
        public string userId;
        public ModerationActionType actionType;
        public string reason;
        public DateTime timestamp;
        public DateTime expirationTime;
        public string moderatorId;
        public RestrictionType restrictionType = RestrictionType.None;
        public bool isExpired = false;
        public Dictionary<string, object> actionDetails = new();
    }

    [Serializable]
    public class ModerationHistory
    {
        public string userId;
        public List<ModerationAction> moderationActions = new();
        public List<ContentViolation> contentViolations = new();
        public List<BehaviorReport> behaviorReports = new();
        public int totalWarnings;
        public int totalRestrictions;
        public DateTime firstViolation;
        public DateTime lastViolation;
        public float riskScore;
    }

    public enum ModerationActionType
    {
        Warning,
        Restriction,
        Suspension,
        Ban,
        ContentRemoval,
        AccountReview,
        EducationalNotice
    }

    public enum RestrictionType
    {
        None,
        Any,
        Communication,
        CreatureNaming,
        Research,
        Trading,
        Breeding,
        Social
    }

    #endregion

    #region Safety Alerts

    [Serializable]
    public class SafetyAlert
    {
        public string alertId;
        public SafetyAlertType alertType;
        public string description;
        public string userId;
        public SafetyAlertSeverity severity;
        public DateTime timestamp;
        public SafetyAlertStatus status;
        public Dictionary<string, object> alertData = new();
    }

    [Serializable]
    public class ActiveModerationAlert
    {
        public SafetyAlert alert;
        public DateTime lastUpdate;
    }

    public enum SafetyAlertType
    {
        InappropriateContent,
        SuspiciousBehavior,
        PrivacyViolation,
        UnderageUser,
        SystemVulnerability,
        CommunityGuidelines,
        TechnicalIssue
    }

    public enum SafetyAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum SafetyAlertStatus
    {
        Active,
        UnderReview,
        Resolved,
        AutoResolved,
        Escalated
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Content moderation service interface
    /// </summary>
    public interface IContentModerationService
    {
        Task<bool> InitializeAsync();
        Task<ContentModerationResult> ModerateContentAsync(string content, ContentType contentType, string userId = null);
        bool IsContentAppropriate(string content, ContentType contentType);
        List<string> GetFilteredWords();
        void AddToWhitelist(string word);
        void AddToBlacklist(string word);
    }

    /// <summary>
    /// Behavior monitoring service interface
    /// </summary>
    public interface IBehaviorMonitoringService
    {
        Task<bool> InitializeAsync();
        void AnalyzePlayerBehavior(Laboratory.Subsystems.Analytics.PlayerActionEvent actionEvent);
        void UpdateBehaviorAnalysis();
        List<BehaviorPattern> GetBehaviorPatterns(string userId);
        float GetUserRiskScore(string userId);
        List<string> GetSuspiciousUsers();
    }

    /// <summary>
    /// Safety compliance service interface
    /// </summary>
    public interface ISafetyComplianceService
    {
        Task<bool> InitializeAsync();
        bool ValidateCOPPACompliance(string userId, int age);
        bool ValidateDataCollection(string userId, string dataType);
        void UpdateComplianceMetrics();
        ComplianceReport GenerateComplianceReport();
        List<string> GetComplianceRecommendations();
    }

    /// <summary>
    /// Reporting service interface
    /// </summary>
    public interface IReportingService
    {
        Task<bool> InitializeAsync();
        Task<bool> SubmitReportAsync(BehaviorReport report);
        List<BehaviorReport> GetPendingReports();
        bool ResolveReport(string reportId, string resolution);
        ReportAnalytics GetReportAnalytics();
    }

    #endregion

    #region Compliance

    [Serializable]
    public class ComplianceReport
    {
        public DateTime generatedDate;
        public int totalUsers;
        public int underageUsers;
        public int dataCollectionViolations;
        public int contentViolations;
        public int behaviorViolations;
        public List<ComplianceIssue> issues = new();
        public float overallComplianceScore;
    }

    [Serializable]
    public class ComplianceIssue
    {
        public string issueType;
        public string description;
        public ComplianceSeverity severity;
        public DateTime detectedDate;
        public bool isResolved;
        public string resolution;
    }

    public enum ComplianceSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion

    #region Analytics

    [Serializable]
    public class ReportAnalytics
    {
        public int totalReports;
        public int pendingReports;
        public int resolvedReports;
        public int dismissedReports;
        public Dictionary<BehaviorViolationType, int> violationCounts = new();
        public Dictionary<string, int> reportsByUser = new();
        public float averageResolutionTime;
        public List<string> topReportReasons = new();
    }

    [Serializable]
    public class ModerationAnalytics
    {
        public int totalModerationActions;
        public int warningsIssued;
        public int restrictionsApplied;
        public int suspensionsApplied;
        public int bansApplied;
        public Dictionary<string, int> actionsByReason = new();
        public Dictionary<ContentType, int> contentViolationsByType = new();
        public float averageUserRiskScore;
        public List<string> topViolationReasons = new();
    }

    #endregion

    #region Word Filtering

    [Serializable]
    public class WordFilter
    {
        public List<string> blacklist = new();
        public List<string> whitelist = new();
        public List<string> educationalExceptions = new();
        public Dictionary<string, string> replacements = new();
        public bool enableWildcards = true;
        public bool caseSensitive = false;
    }

    [Serializable]
    public class FilterRule
    {
        public string pattern;
        public FilterRuleType ruleType;
        public ViolationSeverity severity;
        public string category;
        public bool isActive = true;
    }

    public enum FilterRuleType
    {
        ExactMatch,
        PartialMatch,
        Regex,
        Wildcard,
        Context
    }

    #endregion

    #region Educational Safety

    [Serializable]
    public class EducationalSafetySettings
    {
        public bool requireParentalConsent = true;
        public bool enableRestrictedMode = true;
        public bool allowDirectMessaging = false;
        public bool enableDataCollection = false;
        public bool enableExternalLinks = false;
        public int maxSessionDuration = 60; // minutes
        public List<string> allowedFeatures = new();
        public List<string> blockedFeatures = new();
    }

    [Serializable]
    public class ParentalControls
    {
        public string parentId;
        public string childId;
        public EducationalSafetySettings settings;
        public bool isActive = true;
        public DateTime lastUpdated;
        public List<string> notificationEvents = new();
    }

    #endregion
}