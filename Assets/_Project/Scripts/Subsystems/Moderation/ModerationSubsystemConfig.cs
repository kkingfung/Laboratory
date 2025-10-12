using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Moderation
{
    /// <summary>
    /// Configuration ScriptableObject for the Moderation Subsystem.
    /// Controls content filtering, behavior monitoring, safety compliance, and educational mode settings.
    /// </summary>
    [CreateAssetMenu(fileName = "ModerationSubsystemConfig", menuName = "Project Chimera/Subsystems/Moderation Config")]
    public class ModerationSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Enable automatic moderation actions")]
        public bool enableAutomaticActions = false;

        [Tooltip("Background processing interval in milliseconds")]
        [Range(1000, 30000)]
        public int processingIntervalMs = 10000;

        [Tooltip("Auto-resolve safety alerts after this many hours")]
        [Range(1f, 72f)]
        public float autoResolveAlertHours = 24f;

        [Header("COPPA & Educational Compliance")]
        [Tooltip("Enable COPPA compliance features")]
        public bool coppaCompliant = true;

        [Tooltip("Require parental consent for users under 13")]
        public bool requireParentalConsent = true;

        [Tooltip("Enable educational safety mode")]
        public bool educationalSafetyMode = true;

        [Tooltip("Maximum session duration for educational mode (minutes)")]
        [Range(15, 180)]
        public int maxEducationalSessionMinutes = 60;

        [Header("Content Filtering")]
        [Tooltip("Enable real-time content filtering")]
        public bool enableContentFiltering = true;

        [Tooltip("Filter severity threshold")]
        public ViolationSeverity filterSeverityThreshold = ViolationSeverity.Minor;

        [Tooltip("Words to filter from content")]
        public List<string> profanityFilter = new List<string>
        {
            // Basic inappropriate words - in a real implementation, this would be much more comprehensive
            "inappropriate", "banned", "filtered"
        };

        [Tooltip("Whitelisted words that are allowed despite filtering")]
        public List<string> whitelist = new List<string>
        {
            "genetics", "evolution", "breeding", "mutation", "trait", "species", "organism"
        };

        [Tooltip("Educational exceptions - scientific terms that should always be allowed")]
        public List<string> educationalExceptions = new List<string>
        {
            "DNA", "RNA", "chromosome", "allele", "phenotype", "genotype", "heredity",
            "natural selection", "adaptation", "biodiversity", "ecosystem", "habitat"
        };

        [Header("Behavior Monitoring")]
        [Tooltip("Enable automated behavior analysis")]
        public bool enableBehaviorMonitoring = true;

        [Tooltip("Risk score threshold for automatic action")]
        [Range(0.1f, 1f)]
        public float riskScoreThreshold = 0.7f;

        [Tooltip("Maximum actions per minute before flagging as spam")]
        [Range(5, 100)]
        public int spamActionThreshold = 30;

        [Tooltip("Pattern detection window in hours")]
        [Range(1f, 48f)]
        public float behaviorAnalysisWindowHours = 12f;

        [Header("Moderation Actions")]
        [Tooltip("Enable warning system")]
        public bool enableWarnings = true;

        [Tooltip("Enable temporary restrictions")]
        public bool enableRestrictions = true;

        [Tooltip("Enable account suspensions")]
        public bool enableSuspensions = false; // Disabled by default for educational environments

        [Tooltip("Maximum warnings before automatic restriction")]
        [Range(1, 10)]
        public int maxWarningsBeforeRestriction = 3;

        [Tooltip("Default restriction duration in hours")]
        [Range(1f, 168f)]
        public float defaultRestrictionHours = 24f;

        [Header("Educational Mode")]
        [Tooltip("Allowed features in educational mode")]
        public List<string> educationalAllowedFeatures = new List<string>
        {
            "Breeding", "Research", "Discovery", "Genetics", "Ecosystem"
        };

        [Tooltip("Blocked features in educational mode")]
        public List<string> educationalBlockedFeatures = new List<string>
        {
            "DirectMessaging", "Trading", "ExternalLinks", "SocialMedia"
        };

        [Tooltip("Enable teacher dashboard features")]
        public bool enableTeacherDashboard = true;

        [Tooltip("Enable student progress monitoring")]
        public bool enableStudentMonitoring = true;

        [Header("Privacy Settings")]
        [Tooltip("Anonymize user data in logs")]
        public bool anonymizeUserData = true;

        [Tooltip("Data retention period in days")]
        [Range(7, 365)]
        public int dataRetentionDays = 30;

        [Tooltip("Enable data export for compliance")]
        public bool enableDataExport = true;

        [Header("Logging & Reporting")]
        [Tooltip("Enable moderation action logging")]
        public bool enableModerationLogging = true;

        [Tooltip("Save moderation logs to file")]
        public bool saveModerationLogs = true;

        [Tooltip("Enable safety alert notifications")]
        public bool enableSafetyAlerts = true;

        [Tooltip("Enable automatic reporting to external systems")]
        public bool enableExternalReporting = false;

        [Header("Filter Rules")]
        [Tooltip("Custom filter rules for advanced content moderation")]
        public List<FilterRule> customFilterRules = new List<FilterRule>();

        [Tooltip("Context-based filtering rules")]
        public List<ContextFilterRule> contextRules = new List<ContextFilterRule>();

        [Header("Notification Settings")]
        [Tooltip("Send notifications for safety alerts")]
        public bool sendSafetyAlertNotifications = true;

        [Tooltip("Send notifications for moderation actions")]
        public bool sendModerationActionNotifications = true;

        [Tooltip("Notification recipients (email addresses)")]
        public List<string> notificationRecipients = new List<string>();

        [Header("Integration Settings")]
        [Tooltip("External moderation service API endpoint")]
        public string externalModerationAPI = "";

        [Tooltip("External reporting service API endpoint")]
        public string externalReportingAPI = "";

        [Tooltip("API timeout in seconds")]
        [Range(5, 60)]
        public int apiTimeoutSeconds = 30;

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            processingIntervalMs = Mathf.Max(1000, processingIntervalMs);
            maxEducationalSessionMinutes = Mathf.Max(15, maxEducationalSessionMinutes);
            dataRetentionDays = Mathf.Max(7, dataRetentionDays);
            apiTimeoutSeconds = Mathf.Max(5, apiTimeoutSeconds);

            // Ensure educational exceptions include basic scientific terms
            if (educationalExceptions.Count == 0)
            {
                educationalExceptions.AddRange(new[]
                {
                    "DNA", "RNA", "chromosome", "allele", "phenotype", "genotype", "heredity",
                    "natural selection", "adaptation", "biodiversity", "ecosystem", "habitat"
                });
            }

            // Ensure educational features are properly configured
            if (educationalAllowedFeatures.Count == 0)
            {
                educationalAllowedFeatures.AddRange(new[]
                {
                    "Breeding", "Research", "Discovery", "Genetics", "Ecosystem"
                });
            }

            // Validate COPPA compliance
            if (coppaCompliant && !requireParentalConsent)
            {
                Debug.LogWarning("[ModerationConfig] COPPA compliance enabled but parental consent not required. This may not be compliant.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if a word is in the profanity filter
        /// </summary>
        public bool IsWordFiltered(string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;

            var lowerWord = word.ToLower();

            // Check whitelist first
            if (whitelist.Contains(lowerWord) || educationalExceptions.Contains(lowerWord))
                return false;

            // Check profanity filter
            return profanityFilter.Contains(lowerWord);
        }

        /// <summary>
        /// Checks if a feature is allowed in educational mode
        /// </summary>
        public bool IsFeatureAllowedInEducationalMode(string feature)
        {
            if (!educationalSafetyMode)
                return true;

            return educationalAllowedFeatures.Contains(feature) && !educationalBlockedFeatures.Contains(feature);
        }

        /// <summary>
        /// Gets the appropriate moderation action for a violation severity
        /// </summary>
        public ModerationActionType GetRecommendedAction(ViolationSeverity severity, int previousViolations)
        {
            return severity switch
            {
                ViolationSeverity.Minor when previousViolations < maxWarningsBeforeRestriction => ModerationActionType.Warning,
                ViolationSeverity.Minor => ModerationActionType.Restriction,
                ViolationSeverity.Major => enableRestrictions ? ModerationActionType.Restriction : ModerationActionType.Warning,
                ViolationSeverity.Severe => enableSuspensions ? ModerationActionType.Suspension : ModerationActionType.Restriction,
                _ => ModerationActionType.Warning
            };
        }

        /// <summary>
        /// Gets filter rules for a specific content type
        /// </summary>
        public List<FilterRule> GetFilterRulesForContentType(ContentType contentType)
        {
            var rules = new List<FilterRule>();

            // Add custom rules that apply to this content type
            foreach (var rule in customFilterRules)
            {
                if (rule.isActive && DoesRuleApplyToContentType(rule, contentType))
                {
                    rules.Add(rule);
                }
            }

            return rules;
        }

        /// <summary>
        /// Gets context rules for content analysis
        /// </summary>
        public List<ContextFilterRule> GetContextRules(ContentType contentType)
        {
            return contextRules.FindAll(rule => rule.isActive && DoesContextRuleApplyToContentType(rule, contentType));
        }

        /// <summary>
        /// Checks if user data should be retained based on retention policy
        /// </summary>
        public bool ShouldRetainUserData(System.DateTime lastActivity)
        {
            var dataAge = System.DateTime.Now - lastActivity;
            return dataAge.TotalDays <= dataRetentionDays;
        }

        /// <summary>
        /// Gets recommended restriction duration based on violation severity
        /// </summary>
        public System.TimeSpan GetRestrictionDuration(ViolationSeverity severity, int previousViolations)
        {
            var baseHours = defaultRestrictionHours;

            // Escalate based on severity
            var severityMultiplier = severity switch
            {
                ViolationSeverity.Minor => 1f,
                ViolationSeverity.Major => 2f,
                ViolationSeverity.Severe => 4f,
                _ => 1f
            };

            // Escalate based on previous violations
            var violationMultiplier = 1f + (previousViolations * 0.5f);

            var totalHours = baseHours * severityMultiplier * violationMultiplier;
            return System.TimeSpan.FromHours(Mathf.Min(totalHours, 168f)); // Max 1 week
        }

        #endregion

        #region Private Methods

        private bool DoesRuleApplyToContentType(FilterRule rule, ContentType contentType)
        {
            // For now, apply all rules to all content types
            // This could be extended to have content-type-specific rules
            return true;
        }

        private bool DoesContextRuleApplyToContentType(ContextFilterRule rule, ContentType contentType)
        {
            return rule.applicableContentTypes.Contains(contentType);
        }

        #endregion
    }

    [System.Serializable]
    public class ContextFilterRule
    {
        [Tooltip("Name of the context rule")]
        public string ruleName;

        [Tooltip("Context pattern to match")]
        public string contextPattern;

        [Tooltip("Action to take when context is matched")]
        public ContextAction action;

        [Tooltip("Content types this rule applies to")]
        public List<ContentType> applicableContentTypes = new List<ContentType>();

        [Tooltip("Whether this rule is currently active")]
        public bool isActive = true;
    }

    public enum ContextAction
    {
        Allow,
        Block,
        Flag,
        Replace
    }
}