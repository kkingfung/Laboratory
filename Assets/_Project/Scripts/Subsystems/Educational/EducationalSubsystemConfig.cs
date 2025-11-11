using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Educational
{
    /// <summary>
    /// Configuration ScriptableObject for the Educational Integration Subsystem.
    /// Controls classroom management, curriculum integration, student tracking, and COPPA compliance.
    /// </summary>
    [CreateAssetMenu(fileName = "EducationalSubsystemConfig", menuName = "Project Chimera/Subsystems/Educational Config")]
    public class EducationalSubsystemConfig : ScriptableObject
    {
        [Header("Core Settings")]
        [Tooltip("Background processing interval in milliseconds")]
        [Range(1000, 30000)]
        public int backgroundProcessingIntervalMs = 5000;

        [Tooltip("Enable debug logging for educational operations")]
        public bool enableDebugLogging = false;

        [Tooltip("Maximum events processed per update cycle")]
        [Range(1, 100)]
        public int maxEventsPerUpdate = 20;

        [Tooltip("Analytics update interval in minutes")]
        [Range(1, 60)]
        public int analyticsUpdateIntervalMinutes = 10;

        [Header("Classroom Management")]
        [Tooltip("Maximum students per classroom session")]
        [Range(1, 50)]
        public int maxStudentsPerSession = 30;

        [Tooltip("Default session duration in minutes")]
        [Range(15, 480)]
        public int defaultSessionDurationMinutes = 90;

        [Tooltip("Session warning time before end in minutes")]
        [Range(1, 30)]
        public int sessionEndWarningMinutes = 5;

        [Tooltip("Enable late joining to sessions")]
        public bool allowLateJoinToSessions = true;

        [Tooltip("Require educator supervision for all activities")]
        public bool requireEducatorSupervision = true;

        [Tooltip("Auto-save session data interval in minutes")]
        [Range(1, 15)]
        public int sessionAutoSaveIntervalMinutes = 5;

        [Header("Student Progress Tracking")]
        [Tooltip("Enable real-time progress tracking")]
        public bool enableRealTimeProgressTracking = true;

        [Tooltip("Progress auto-save interval in minutes")]
        [Range(1, 30)]
        public int progressAutoSaveIntervalMinutes = 2;

        [Tooltip("Engagement timeout in minutes (time before considering student inactive)")]
        [Range(1, 60)]
        public int engagementTimeoutMinutes = 10;

        [Tooltip("Engagement score increment per activity")]
        [Range(0.1f, 10f)]
        public float engagementIncrement = 1f;

        [Tooltip("Engagement score decrement for inactivity")]
        [Range(0.1f, 5f)]
        public float engagementDecrement = 0.5f;

        [Tooltip("Minimum activities required for progress calculation")]
        [Range(1, 20)]
        public int minimumActivitiesForProgress = 3;

        [Header("Assessment Configuration")]
        [Tooltip("Enable automated assessment grading")]
        public bool enableAutomatedGrading = true;

        [Tooltip("Default passing score percentage")]
        [Range(50f, 100f)]
        public float defaultPassingScore = 70f;

        [Tooltip("Maximum assessment attempts allowed")]
        [Range(1, 10)]
        public int maxAssessmentAttempts = 3;

        [Tooltip("Assessment time limit in minutes (0 = no limit)")]
        [Range(0, 300)]
        public int defaultAssessmentTimeLimit = 60;

        [Tooltip("Enable immediate feedback after assessment")]
        public bool enableImmediateFeedback = true;

        [Tooltip("Allow assessment retakes")]
        public bool allowAssessmentRetakes = true;

        [Header("Curriculum Integration")]
        [Tooltip("Available curriculum standards")]
        public List<CurriculumStandard> curriculumStandards = new List<CurriculumStandard>();

        [Tooltip("Default difficulty progression")]
        public DifficultyProgression defaultDifficultyProgression = DifficultyProgression.Adaptive;

        [Tooltip("Enable automatic curriculum alignment")]
        public bool enableAutomaticAlignment = true;

        [Tooltip("Minimum alignment strength for suggestions")]
        [Range(0.1f, 1f)]
        public float minimumAlignmentStrength = 0.6f;

        [Tooltip("Enable adaptive difficulty based on performance")]
        public bool enableAdaptiveDifficulty = true;

        [Header("Privacy and Compliance")]
        [Tooltip("Enable COPPA compliance mode")]
        public bool enableCOPPACompliance = true;

        [Tooltip("Require parental consent for students under 13")]
        public bool requireParentalConsentUnder13 = true;

        [Tooltip("Data retention period in days")]
        [Range(30, 2555)] // Up to 7 years
        public int dataRetentionDays = 2555;

        [Tooltip("Enable anonymous data collection")]
        public bool enableAnonymousDataCollection = false;

        [Tooltip("Allow third-party integrations")]
        public bool allowThirdPartyIntegrations = false;

        [Tooltip("Enable audit logging for compliance")]
        public bool enableAuditLogging = true;

        [Header("Educator Tools")]
        [Tooltip("Enable real-time educator dashboard")]
        public bool enableEducatorDashboard = true;

        [Tooltip("Dashboard update interval in seconds")]
        [Range(5, 300)]
        public int dashboardUpdateIntervalSeconds = 30;

        [Tooltip("Enable automated student alerts")]
        public bool enableAutomatedStudentAlerts = true;

        [Tooltip("Student struggle detection threshold (number of failed attempts)")]
        [Range(2, 10)]
        public int studentStruggleThreshold = 3;

        [Tooltip("Inactivity alert threshold in minutes")]
        [Range(5, 120)]
        public int inactivityAlertThresholdMinutes = 15;

        [Tooltip("Enable parent/guardian communication")]
        public bool enableParentCommunication = true;

        [Header("Accessibility")]
        [Tooltip("Enable accessibility features")]
        public bool enableAccessibilityFeatures = true;

        [Tooltip("Default text size multiplier")]
        [Range(0.5f, 3f)]
        public float defaultTextSizeMultiplier = 1f;

        [Tooltip("Enable screen reader support")]
        public bool enableScreenReaderSupport = true;

        [Tooltip("Enable high contrast mode")]
        public bool enableHighContrastMode = true;

        [Tooltip("Enable reduced motion options")]
        public bool enableReducedMotionOptions = true;

        [Header("Content Delivery")]
        [Tooltip("Enable adaptive content delivery")]
        public bool enableAdaptiveContentDelivery = true;

        [Tooltip("Content adaptation algorithm")]
        public ContentAdaptationAlgorithm contentAdaptationAlgorithm = ContentAdaptationAlgorithm.LearningStyle;

        [Tooltip("Enable multimedia content")]
        public bool enableMultimediaContent = true;

        [Tooltip("Enable interactive simulations")]
        public bool enableInteractiveSimulations = true;

        [Tooltip("Content quality threshold")]
        [Range(0.1f, 1f)]
        public float contentQualityThreshold = 0.8f;

        [Header("Collaboration Features")]
        [Tooltip("Enable student collaboration")]
        public bool enableStudentCollaboration = true;

        [Tooltip("Maximum collaboration group size")]
        [Range(2, 10)]
        public int maxCollaborationGroupSize = 4;

        [Tooltip("Enable peer assessment")]
        public bool enablePeerAssessment = false;

        [Tooltip("Enable discussion forums")]
        public bool enableDiscussionForums = true;

        [Tooltip("Moderate all student communications")]
        public bool moderateStudentCommunications = true;

        [Header("Reporting and Analytics")]
        [Tooltip("Enable detailed analytics")]
        public bool enableDetailedAnalytics = true;

        [Tooltip("Generate weekly progress reports")]
        public bool generateWeeklyReports = true;

        [Tooltip("Generate monthly summary reports")]
        public bool generateMonthlyReports = true;

        [Tooltip("Enable real-time performance metrics")]
        public bool enableRealTimeMetrics = true;

        [Tooltip("Analytics data aggregation interval in hours")]
        [Range(1, 24)]
        public int analyticsAggregationIntervalHours = 6;

        [Header("Performance")]
        [Tooltip("Maximum concurrent sessions")]
        [Range(1, 100)]
        public int maxConcurrentSessions = 20;

        [Tooltip("Student data cache size")]
        [Range(100, 10000)]
        public int studentDataCacheSize = 1000;

        [Tooltip("Progress calculation batch size")]
        [Range(10, 500)]
        public int progressCalculationBatchSize = 50;

        [Tooltip("Enable data compression")]
        public bool enableDataCompression = true;

        [Header("Platform-Specific Settings")]
        [Tooltip("Platform-specific configurations")]
        public List<PlatformEducationalSettings> platformSettings = new List<PlatformEducationalSettings>();

        #region Validation

        private void OnValidate()
        {
            // Ensure reasonable values
            backgroundProcessingIntervalMs = Mathf.Max(1000, backgroundProcessingIntervalMs);
            maxStudentsPerSession = Mathf.Max(1, maxStudentsPerSession);
            defaultSessionDurationMinutes = Mathf.Max(15, defaultSessionDurationMinutes);
            progressAutoSaveIntervalMinutes = Mathf.Max(1, progressAutoSaveIntervalMinutes);

            // Ensure assessment settings are reasonable
            defaultPassingScore = Mathf.Clamp(defaultPassingScore, 50f, 100f);
            maxAssessmentAttempts = Mathf.Max(1, maxAssessmentAttempts);

            // Ensure privacy compliance settings
            if (enableCOPPACompliance)
            {
                requireParentalConsentUnder13 = true;
                enableAuditLogging = true;
            }

            // Ensure curriculum standards have defaults
            if (curriculumStandards.Count == 0)
            {
                curriculumStandards.AddRange(CreateDefaultCurriculumStandards());
            }

            // Ensure platform settings have defaults
            if (platformSettings.Count == 0)
            {
                platformSettings.AddRange(CreateDefaultPlatformSettings());
            }

            // Validate accessibility settings
            defaultTextSizeMultiplier = Mathf.Clamp(defaultTextSizeMultiplier, 0.5f, 3f);

            // Validate performance settings
            maxConcurrentSessions = Mathf.Max(1, maxConcurrentSessions);
            studentDataCacheSize = Mathf.Max(100, studentDataCacheSize);
        }

        private List<CurriculumStandard> CreateDefaultCurriculumStandards()
        {
            return new List<CurriculumStandard>
            {
                new CurriculumStandard
                {
                    standardId = "NGSS-MS-LS1-5",
                    standardName = "Environmental Factors on Organisms",
                    description = "Construct a scientific explanation based on evidence for how environmental and genetic factors influence the growth of organisms.",
                    gradeLevel = "6-8",
                    subject = "Life Science",
                    framework = StandardsFramework.NGSS,
                    difficultyLevel = DifficultyLevel.Intermediate,
                    estimatedTimeMinutes = 120
                },
                new CurriculumStandard
                {
                    standardId = "NGSS-MS-LS3-1",
                    standardName = "Inheritance of Traits",
                    description = "Develop and use a model to describe why structural changes to genes (mutations) located on chromosomes may affect proteins and may result in harmful, beneficial, or neutral effects to the structure and function of the organism.",
                    gradeLevel = "6-8",
                    subject = "Life Science",
                    framework = StandardsFramework.NGSS,
                    difficultyLevel = DifficultyLevel.Advanced,
                    estimatedTimeMinutes = 150
                },
                new CurriculumStandard
                {
                    standardId = "NGSS-MS-LS4-4",
                    standardName = "Natural Selection",
                    description = "Construct an explanation based on evidence that describes how genetic variations of traits in a population increase some individuals' probability of surviving and reproducing in a specific environment.",
                    gradeLevel = "6-8",
                    subject = "Life Science",
                    framework = StandardsFramework.NGSS,
                    difficultyLevel = DifficultyLevel.Advanced,
                    estimatedTimeMinutes = 180
                }
            };
        }

        private List<PlatformEducationalSettings> CreateDefaultPlatformSettings()
        {
            return new List<PlatformEducationalSettings>
            {
                new PlatformEducationalSettings
                {
                    platformType = EducationalPlatform.Classroom,
                    maxConcurrentUsers = 30,
                    enableRealTimeSync = true,
                    dataStorageLocation = DataStorageLocation.Local,
                    requiresInternetConnection = false
                },
                new PlatformEducationalSettings
                {
                    platformType = EducationalPlatform.Online,
                    maxConcurrentUsers = 100,
                    enableRealTimeSync = true,
                    dataStorageLocation = DataStorageLocation.Cloud,
                    requiresInternetConnection = true
                },
                new PlatformEducationalSettings
                {
                    platformType = EducationalPlatform.Hybrid,
                    maxConcurrentUsers = 50,
                    enableRealTimeSync = true,
                    dataStorageLocation = DataStorageLocation.Hybrid,
                    requiresInternetConnection = true
                }
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets curriculum standard by ID
        /// </summary>
        public CurriculumStandard GetCurriculumStandard(string standardId)
        {
            return curriculumStandards.Find(cs => cs.standardId == standardId);
        }

        /// <summary>
        /// Gets curriculum standards by grade level
        /// </summary>
        public List<CurriculumStandard> GetCurriculumStandardsByGrade(string gradeLevel)
        {
            return curriculumStandards.FindAll(cs => cs.gradeLevel == gradeLevel);
        }

        /// <summary>
        /// Gets curriculum standards by subject
        /// </summary>
        public List<CurriculumStandard> GetCurriculumStandardsBySubject(string subject)
        {
            return curriculumStandards.FindAll(cs => cs.subject == subject);
        }

        /// <summary>
        /// Checks if COPPA compliance is required for student age
        /// </summary>
        public bool RequiresCOPPACompliance(int studentAge)
        {
            return enableCOPPACompliance && studentAge < 13;
        }

        /// <summary>
        /// Gets session configuration for classroom type
        /// </summary>
        public SessionConfiguration GetDefaultSessionConfiguration()
        {
            return new SessionConfiguration
            {
                allowLateJoin = allowLateJoinToSessions,
                requireSupervision = requireEducatorSupervision,
                maxStudents = maxStudentsPerSession
            };
        }

        /// <summary>
        /// Gets assessment configuration with defaults
        /// </summary>
        public AssessmentConfiguration GetDefaultAssessmentConfiguration()
        {
            return new AssessmentConfiguration
            {
                timeLimit = defaultAssessmentTimeLimit,
                shuffleQuestions = false,
                shuffleAnswers = true,
                showResults = enableImmediateFeedback,
                allowRetakes = allowAssessmentRetakes,
                maxAttempts = maxAssessmentAttempts
            };
        }

        /// <summary>
        /// Gets privacy settings for student age
        /// </summary>
        public PrivacySettings GetPrivacySettingsForAge(int studentAge)
        {
            var settings = new PrivacySettings();

            if (RequiresCOPPACompliance(studentAge))
            {
                settings.allowDataCollection = false;
                settings.allowThirdPartyIntegration = false;
                settings.shareProgressWithParents = true;
                settings.dataRetentionPolicy = DataRetentionPolicy.LegalMinimum;
            }
            else
            {
                settings.allowDataCollection = enableAnonymousDataCollection;
                settings.allowThirdPartyIntegration = allowThirdPartyIntegrations;
                settings.shareProgressWithParents = true;
                settings.dataRetentionPolicy = DataRetentionPolicy.SchoolPolicy;
            }

            return settings;
        }

        /// <summary>
        /// Gets platform-specific settings
        /// </summary>
        public PlatformEducationalSettings GetPlatformSettings(EducationalPlatform platform)
        {
            return platformSettings.Find(ps => ps.platformType == platform) ??
                   platformSettings.FirstOrDefault();
        }

        /// <summary>
        /// Validates student registration requirements
        /// </summary>
        public bool ValidateStudentRegistration(StudentRegistration registration)
        {
            // Check required fields
            if (string.IsNullOrEmpty(registration.studentName) ||
                string.IsNullOrEmpty(registration.username))
                return false;

            // Check COPPA compliance
            var age = CalculateAge(registration.dateOfBirth);
            if (RequiresCOPPACompliance(age))
            {
                if (registration.parentGuardianInfo == null ||
                    string.IsNullOrEmpty(registration.parentGuardianInfo.parentEmail))
                    return false;

                if (!registration.parentGuardianInfo.hasConsentForDataCollection)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Calculates recommended activity difficulty for student
        /// </summary>
        public DifficultyLevel GetRecommendedDifficulty(StudentProgress progress)
        {
            if (progress == null || progress.activityProgress.Count < minimumActivitiesForProgress)
                return DifficultyLevel.Beginner;

            var averageScore = progress.activityProgress.Average(ap => ap.score);

            if (averageScore >= 90f)
                return DifficultyLevel.Advanced;
            else if (averageScore >= 80f)
                return DifficultyLevel.Intermediate;
            else if (averageScore >= 70f)
                return DifficultyLevel.Novice;
            else
                return DifficultyLevel.Beginner;
        }

        /// <summary>
        /// Checks if student needs intervention based on performance
        /// </summary>
        public bool RequiresIntervention(StudentProgress progress)
        {
            if (progress == null)
                return false;

            // Check for consecutive failures
            var recentActivities = progress.activityProgress
                .OrderByDescending(ap => ap.startTime)
                .Take(studentStruggleThreshold);

            if (recentActivities.Count() >= studentStruggleThreshold)
            {
                var failedCount = recentActivities.Count(ap => ap.score < defaultPassingScore);
                if (failedCount >= studentStruggleThreshold)
                    return true;
            }

            // Check engagement score
            if (progress.engagementScore < 30f)
                return true;

            // Check inactivity
            var timeSinceLastActivity = DateTime.Now - progress.lastActivityTime;
            if (timeSinceLastActivity.TotalMinutes > inactivityAlertThresholdMinutes * 2)
                return true;

            return false;
        }

        #endregion

        #region Helper Methods

        private int CalculateAge(DateTime dateOfBirth)
        {
            var today = DateTime.Today;
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > today.AddYears(-age))
                age--;
            return age;
        }

        #endregion
    }

    #region Configuration Enums

    public enum DifficultyProgression
    {
        Fixed,
        Linear,
        Adaptive,
        UserChoice
    }

    public enum ContentAdaptationAlgorithm
    {
        None,
        Performance,
        LearningStyle,
        Engagement,
        AI
    }

    public enum EducationalPlatform
    {
        Classroom,
        Online,
        Hybrid,
        Mobile
    }

    public enum DataStorageLocation
    {
        Local,
        Cloud,
        Hybrid
    }

    #endregion

    #region Platform Settings

    [System.Serializable]
    public class PlatformEducationalSettings
    {
        [Header("Platform Configuration")]
        public EducationalPlatform platformType;
        public int maxConcurrentUsers = 30;
        public bool enableRealTimeSync = true;
        public DataStorageLocation dataStorageLocation = DataStorageLocation.Local;
        public bool requiresInternetConnection = false;

        [Header("Feature Support")]
        public bool supportsMultimedia = true;
        public bool supportsCollaboration = true;
        public bool supportsAssessments = true;
        public bool supportsAnalytics = true;

        [Header("Performance")]
        public int dataSyncIntervalSeconds = 30;
        public int maxFileUploadSizeMB = 10;
        public bool enableDataCompression = true;
        public bool enableOfflineMode = false;
    }

    #endregion
}