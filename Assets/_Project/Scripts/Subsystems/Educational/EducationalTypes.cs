using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;

namespace Laboratory.Subsystems.Educational
{
    #region Core Educational Data

    [Serializable]
    public class StudentProfile
    {
        public string studentId;
        public string studentName;
        public string username;
        public DateTime dateOfBirth;
        public string gradeLevel;
        public string schoolId;
        public string classId;
        public bool isActive = true;
        public PrivacySettings privacySettings = new();
        public LearningPreferences learningPreferences = new();
        public AccessibilitySettings accessibilitySettings = new();
        public DateTime registrationDate;
        public DateTime lastLoginDate;
        public ParentGuardianInfo parentGuardianInfo = new();
        public Dictionary<string, object> customAttributes = new();
    }

    [Serializable]
    public class EducatorProfile
    {
        public string educatorId;
        public string educatorName;
        public string username;
        public string email;
        public List<string> classIds = new();
        public List<string> subjectAreas = new();
        public string schoolId;
        public string district;
        public EducatorPermissions permissions = new();
        public EducatorPreferences preferences = new();
        public DateTime registrationDate;
        public DateTime lastLoginDate;
        public EducatorCertification certification = new();
    }

    [Serializable]
    public class ClassroomSession
    {
        public string sessionId;
        public string sessionName;
        public string educatorId;
        public string classId;
        public DateTime startTime;
        public DateTime endTime;
        public ClassroomSessionStatus status;
        public List<string> studentIds = new();
        public int studentCount;
        public List<string> plannedActivities = new();
        public SessionConfiguration configuration = new();
        public bool hasEndWarning = false;
        public Dictionary<string, object> sessionData = new();
    }

    [Serializable]
    public class StudentProgress
    {
        public string studentId;
        public DateTime lastUpdated;
        public float overallProgressPercentage;
        public List<ActivityProgress> activityProgress = new();
        public List<Achievement> achievements = new();
        public LearningAnalytics analytics = new();
        public float engagementScore;
        public DateTime lastActivityTime;
        public int totalTimeSpentMinutes;
        public List<LearningObjectiveProgress> objectiveProgress = new();
        public SkillAssessment skillAssessment = new();
    }

    [Serializable]
    public class ActivityProgress
    {
        public string activityId;
        public string activityName;
        public DateTime startTime;
        public DateTime completionTime;
        public bool isCompleted;
        public float score;
        public float timeSpentMinutes;
        public int attempts;
        public List<string> hints;
        public DifficultyLevel difficulty;
        public List<LearningObjective> objectivesMet = new();
        public Dictionary<string, object> progressData = new();
    }

    public enum ClassroomSessionStatus
    {
        Planned,
        Active,
        Paused,
        Ending,
        Completed,
        Cancelled
    }

    public enum DifficultyLevel
    {
        Beginner,
        Novice,
        Intermediate,
        Advanced,
        Expert
    }

    #endregion

    #region Curriculum Integration

    [Serializable]
    public class CurriculumStandard
    {
        public string standardId;
        public string standardName;
        public string description;
        public string gradeLevel;
        public string subject;
        public StandardsFramework framework;
        public List<LearningObjective> learningObjectives = new();
        public List<string> prerequisites = new();
        public List<string> relatedStandards = new();
        public DifficultyLevel difficultyLevel;
        public int estimatedTimeMinutes;
        public List<AssessmentCriteria> assessmentCriteria = new();
    }

    [Serializable]
    public class CurriculumActivity
    {
        public string activityId;
        public string activityName;
        public string description;
        public ActivityType activityType;
        public List<string> learningObjectiveIds = new();
        public List<string> curriculumStandardIds = new();
        public DifficultyLevel difficulty;
        public int estimatedDurationMinutes;
        public float passingScore = 70f;
        public bool requiresSupervision;
        public List<string> prerequisites = new();
        public ActivityConfiguration configuration = new();
        public Dictionary<string, object> customSettings = new();
    }

    [Serializable]
    public class LearningObjective
    {
        public string objectiveId;
        public string description;
        public BloomsLevel bloomsLevel;
        public List<string> skillsTargeted = new();
        public List<AssessmentMethod> assessmentMethods = new();
        public bool isRequired = true;
        public float weight = 1f;
        public List<string> relatedObjectives = new();
    }

    [Serializable]
    public class CurriculumAlignment
    {
        public string activityId;
        public string standardId;
        public float alignmentStrength; // 0-1
        public List<string> alignedObjectives = new();
        public string alignmentRationale;
        public DateTime alignmentDate;
        public string alignedBy;
        public bool isVerified;
    }

    public enum StandardsFramework
    {
        NGSS,           // Next Generation Science Standards
        CommonCore,     // Common Core State Standards
        StateStandards, // State-specific standards
        IB,             // International Baccalaureate
        Custom          // Custom or proprietary standards
    }

    public enum ActivityType
    {
        Exploration,
        Breeding,
        Research,
        Discovery,
        Assessment,
        Collaboration,
        Presentation,
        Simulation,
        Problem,
        Creative
    }

    public enum BloomsLevel
    {
        Remember,    // Level 1
        Understand,  // Level 2
        Apply,       // Level 3
        Analyze,     // Level 4
        Evaluate,    // Level 5
        Create       // Level 6
    }

    public enum AssessmentMethod
    {
        Quiz,
        Project,
        Observation,
        Portfolio,
        PeerReview,
        SelfAssessment,
        Performance,
        Rubric
    }

    #endregion

    #region Assessment System

    [Serializable]
    public class EducationalAssessment
    {
        public string assessmentId;
        public string assessmentName;
        public string description;
        public AssessmentType assessmentType;
        public List<string> learningObjectiveIds = new();
        public List<AssessmentQuestion> questions = new();
        public AssessmentConfiguration configuration = new();
        public ScoringMethod scoringMethod;
        public DateTime createdDate;
        public string createdBy;
        public bool isPublished;
        public Dictionary<string, object> metadata = new();
    }

    [Serializable]
    public class AssessmentQuestion
    {
        public string questionId;
        public string questionText;
        public QuestionType questionType;
        public List<AnswerOption> answerOptions = new();
        public List<string> correctAnswers = new();
        public float points;
        public DifficultyLevel difficulty;
        public List<string> hints = new();
        public string explanation;
        public Dictionary<string, object> questionData = new();
    }

    [Serializable]
    public class AnswerOption
    {
        public string optionId;
        public string optionText;
        public bool isCorrect;
        public string feedback;
        public float partialCredit = 0f;
    }

    [Serializable]
    public class AssessmentResponse
    {
        public string studentId;
        public string assessmentId;
        public List<QuestionResponse> questionResponses = new();
        public DateTime startTime;
        public DateTime submitTime;
        public bool isCompleted;
        public Dictionary<string, object> responseData = new();
    }

    [Serializable]
    public class QuestionResponse
    {
        public string questionId;
        public List<string> selectedAnswers = new();
        public string textResponse;
        public float timeSpentSeconds;
        public int attempts;
        public bool usedHints;
        public Dictionary<string, object> responseMetadata = new();
    }

    [Serializable]
    public class AssessmentResult
    {
        public string resultId;
        public string studentId;
        public EducationalAssessment assessment;
        public float score;
        public float maxScore;
        public float percentage;
        public bool passed;
        public DateTime completionTime;
        public TimeSpan duration;
        public List<QuestionResult> questionResults = new();
        public List<string> objectivesAchieved = new();
        public AssessmentFeedback feedback = new();
        public Dictionary<string, object> analytics = new();
    }

    [Serializable]
    public class QuestionResult
    {
        public string questionId;
        public float score;
        public float maxScore;
        public bool isCorrect;
        public string feedback;
        public List<string> missedConcepts = new();
    }

    [Serializable]
    public class AssessmentFeedback
    {
        public string overallFeedback;
        public List<string> strengths = new();
        public List<string> areasForImprovement = new();
        public List<string> recommendations = new();
        public List<string> nextSteps = new();
    }

    public enum AssessmentType
    {
        Formative,
        Summative,
        Diagnostic,
        Benchmark,
        SelfAssessment,
        PeerAssessment
    }

    public enum QuestionType
    {
        MultipleChoice,
        MultipleSelect,
        TrueFalse,
        ShortAnswer,
        Essay,
        Matching,
        Ordering,
        FillInBlank,
        Interactive
    }

    public enum ScoringMethod
    {
        Simple,
        Weighted,
        Rubric,
        Adaptive,
        PeerScored
    }

    #endregion

    #region Educational Events

    [Serializable]
    public class EducationalEvent
    {
        public EducationalEventType eventType;
        public DateTime timestamp;
        public string studentId;
        public string sessionId;
        public string activityId;
        public Dictionary<string, object> data = new();
    }

    public enum EducationalEventType
    {
        ActivityStarted,
        ActivityCompleted,
        AssessmentSubmitted,
        LearningObjectiveAchieved,
        StudentInteraction,
        ProgressMilestone,
        SessionJoined,
        SessionLeft,
        HintRequested,
        HelpRequested,
        CollaborationStarted,
        DiscoveryMade
    }

    [Serializable]
    public class EducationalAlert
    {
        public EducationalAlertType alertType;
        public string message;
        public DateTime timestamp;
        public string studentId;
        public string sessionId;
        public AlertSeverity severity;
        public bool isRead = false;
        public Dictionary<string, object> alertData = new();
    }

    public enum EducationalAlertType
    {
        SessionEndWarning,
        StudentStruggling,
        StudentExcelling,
        InactivityAlert,
        ComplianceViolation,
        TechnicalIssue,
        ProgressMilestone,
        AssessmentDue
    }

    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion

    #region Privacy and Compliance

    [Serializable]
    public class PrivacySettings
    {
        public bool shareProgressWithParents = true;
        public bool allowDataCollection = false;
        public bool allowThirdPartyIntegration = false;
        public bool allowPeerInteraction = true;
        public DataRetentionPolicy dataRetentionPolicy = DataRetentionPolicy.SchoolPolicy;
        public List<string> dataProcessingConsents = new();
        public DateTime lastConsentUpdate;
    }

    [Serializable]
    public class ParentGuardianInfo
    {
        public string parentName;
        public string parentEmail;
        public string parentPhone;
        public bool hasConsentForDataCollection;
        public bool hasConsentForCommunication;
        public DateTime consentDate;
        public List<ConsentRecord> consentHistory = new();
    }

    [Serializable]
    public class ConsentRecord
    {
        public string consentType;
        public bool isGranted;
        public DateTime consentDate;
        public string consentMethod;
        public string ipAddress;
    }

    public enum DataRetentionPolicy
    {
        SchoolPolicy,
        ParentRequest,
        LegalMinimum,
        Extended,
        Permanent
    }

    #endregion

    #region Analytics and Reporting

    [Serializable]
    public class LearningAnalytics
    {
        public float engagementScore;
        public TimeSpan totalTimeSpent;
        public int activitiesCompleted;
        public float averageScore;
        public List<SkillProgress> skillProgress = new();
        public LearningStyle preferredLearningStyle;
        public List<ConceptMastery> conceptMastery = new();
        public PerformanceTrend performanceTrend;
        public DateTime lastAnalyticsUpdate;
    }

    [Serializable]
    public class SkillProgress
    {
        public string skillName;
        public float currentLevel; // 0-100
        public float targetLevel;
        public ProgressStatus status;
        public DateTime lastUpdate;
        public List<SkillEvidence> evidence = new();
    }

    [Serializable]
    public class ConceptMastery
    {
        public string conceptId;
        public string conceptName;
        public float masteryLevel; // 0-100
        public int attemptsCount;
        public DateTime firstAttempt;
        public DateTime lastAttempt;
        public List<string> relatedConcepts = new();
    }

    [Serializable]
    public class ClassAnalytics
    {
        public string classId;
        public int totalStudents;
        public int activeStudents;
        public float averageEngagement;
        public float averageProgress;
        public List<ActivityPopularity> activityPopularity = new();
        public List<CommonStruggle> commonStruggles = new();
        public DateTime lastUpdate;
    }

    [Serializable]
    public class StudentAnalytics
    {
        public string studentId;
        public LearningAnalytics learningAnalytics;
        public EngagementMetrics engagement = new();
        public ProgressMetrics progress = new();
        public CollaborationMetrics collaboration = new();
        public DateTime analyticsDate;
    }

    [Serializable]
    public class ProgressReport
    {
        public string studentId;
        public string studentName;
        public DateTime reportDate;
        public DateTime reportPeriodStart;
        public DateTime reportPeriodEnd;
        public OverallSummary overallSummary = new();
        public List<SubjectProgress> subjectProgress = new();
        public List<SkillAssessment> skillAssessments = new();
        public List<string> achievements = new();
        public List<string> recommendations = new();
        public TeacherComments teacherComments = new();
    }

    public enum LearningStyle
    {
        Visual,
        Auditory,
        Kinesthetic,
        ReadingWriting,
        Mixed
    }

    public enum PerformanceTrend
    {
        Improving,
        Stable,
        Declining,
        Fluctuating,
        Insufficient
    }

    public enum ProgressStatus
    {
        BelowExpected,
        OnTrack,
        AboveExpected,
        Mastered
    }

    #endregion

    #region Configuration Classes

    [Serializable]
    public class SessionConfiguration
    {
        public bool allowLateJoin = true;
        public bool requireSupervision = false;
        public int maxStudents = 30;
        public List<string> allowedActivities = new();
        public Dictionary<string, object> customSettings = new();
    }

    [Serializable]
    public class ActivityConfiguration
    {
        public bool enableHints = true;
        public int maxHints = 3;
        public bool enableCollaboration = true;
        public int maxAttempts = 0; // 0 = unlimited
        public bool enableSaveProgress = true;
        public DifficultyAdaptation difficultyAdaptation = DifficultyAdaptation.None;
        public Dictionary<string, object> customSettings = new();
    }

    [Serializable]
    public class AssessmentConfiguration
    {
        public int timeLimit = 0; // 0 = no limit
        public bool shuffleQuestions = false;
        public bool shuffleAnswers = true;
        public bool showResults = true;
        public bool allowRetakes = false;
        public int maxAttempts = 1;
        public bool requireProctoring = false;
        public Dictionary<string, object> securitySettings = new();
    }

    [Serializable]
    public class LearningPreferences
    {
        public LearningStyle preferredStyle = LearningStyle.Mixed;
        public DifficultyLevel preferredDifficulty = DifficultyLevel.Intermediate;
        public bool enableAudioFeedback = true;
        public bool enableVisualEffects = true;
        public float feedbackSpeed = 1f;
        public List<string> interests = new();
    }

    [Serializable]
    public class AccessibilitySettings
    {
        public bool enableScreenReader = false;
        public bool enableHighContrast = false;
        public bool enableLargeText = false;
        public bool enableReducedMotion = false;
        public bool enableAudioDescriptions = false;
        public bool enableClosedCaptions = false;
        public float textSizeMultiplier = 1f;
        public float audioVolume = 1f;
    }

    [Serializable]
    public class EducatorPermissions
    {
        public bool canCreateAssessments = true;
        public bool canModifyActivities = false;
        public bool canViewStudentData = true;
        public bool canExportData = false;
        public bool canManageClass = true;
        public bool canContactParents = true;
        public List<string> restrictedFeatures = new();
    }

    [Serializable]
    public class EducatorPreferences
    {
        public bool enableRealTimeAlerts = true;
        public bool enableProgressNotifications = true;
        public bool enableWeeklyReports = true;
        public string preferredReportFormat = "PDF";
        public List<string> favoriteActivities = new();
        public Dictionary<string, object> dashboardSettings = new();
    }

    [Serializable]
    public class EducatorCertification
    {
        public List<string> certifications = new();
        public List<string> qualifications = new();
        public DateTime lastCertificationDate;
        public bool requiresContinuingEducation = false;
    }

    public enum DifficultyAdaptation
    {
        None,
        Basic,
        Advanced,
        AI
    }

    #endregion

    #region Service Interfaces

    /// <summary>
    /// Classroom session management service
    /// </summary>
    public interface IClassroomManagementService
    {
        Task<bool> InitializeAsync();
        Task<ClassroomSession> StartSessionAsync(ClassroomSessionRequest request);
        Task<bool> EndSessionAsync(string sessionId);
        Task<bool> AddStudentAsync(string sessionId, string studentId);
        Task<bool> RemoveStudentAsync(string sessionId, string studentId);
        List<ClassroomSession> GetActiveSessions();
        ClassroomSession GetSession(string sessionId);
    }

    /// <summary>
    /// Curriculum integration and alignment service
    /// </summary>
    public interface ICurriculumIntegrationService
    {
        Task<bool> InitializeAsync();
        List<CurriculumActivity> GetActivities(CurriculumFilter filter = null);
        List<CurriculumStandard> GetStandards(string gradeLevel = null, string subject = null);
        Task<List<CurriculumAlignment>> AlignActivityAsync(string activityId);
        Task<CurriculumActivity> CreateActivityAsync(CurriculumActivity activity);
        Task<bool> ValidateAlignmentAsync(string activityId, string standardId);
    }

    /// <summary>
    /// Student progress tracking service
    /// </summary>
    public interface IStudentProgressService
    {
        Task<bool> InitializeAsync();
        Task<StudentProfile> CreateStudentProfileAsync(StudentRegistration registration);
        StudentProgress GetStudentProgress(string studentId);
        void StartActivity(string studentId, string activityId);
        void CompleteActivity(string studentId, string activityId, float score);
        void AchieveMilestone(string studentId, string milestoneId);
        Task UpdateProgressAsync(string studentId);
    }

    /// <summary>
    /// Educator tools and dashboard service
    /// </summary>
    public interface IEducatorToolsService
    {
        Task<bool> InitializeAsync();
        Task<EducatorDashboard> GetDashboardAsync(string educatorId);
        Task<List<StudentSummary>> GetClassSummaryAsync(string classId);
        Task<bool> SendAlertAsync(string studentId, EducationalAlert alert);
        Task<List<ActivityRecommendation>> GetActivityRecommendationsAsync(string classId);
        Task<bool> CreateCustomActivityAsync(CurriculumActivity activity);
    }

    /// <summary>
    /// Assessment creation and grading service
    /// </summary>
    public interface IAssessmentService
    {
        Task<bool> InitializeAsync();
        Task<EducationalAssessment> CreateAssessmentAsync(AssessmentDefinition definition);
        Task<AssessmentResult> SubmitAssessmentAsync(string studentId, string assessmentId, AssessmentResponse response);
        Task<AssessmentResult> ProcessAssessmentAsync(string studentId, string assessmentId);
        List<EducationalAssessment> GetAvailableAssessments(string gradeLevel = null);
        Task<AssessmentAnalytics> GetAssessmentAnalyticsAsync(string assessmentId);
    }

    /// <summary>
    /// Educational analytics and reporting service
    /// </summary>
    public interface IEducationalAnalyticsService
    {
        Task<bool> InitializeAsync();
        void TrackStudentInteraction(string studentId, EducationalEvent educationalEvent);
        Task<ClassAnalytics> GetClassAnalyticsAsync(string classId);
        Task<StudentAnalytics> GetStudentAnalyticsAsync(string studentId);
        Task<ProgressReport> GenerateProgressReportAsync(string studentId, ReportParameters parameters);
        Task UpdateEducationalAnalytics();
    }

    /// <summary>
    /// Privacy compliance and COPPA service
    /// </summary>
    public interface IPrivacyComplianceService
    {
        Task<bool> InitializeAsync();
        Task<bool> ValidateStudentRegistrationAsync(StudentRegistration registration);
        void CheckCompliance();
        Task<bool> ProcessDataRequestAsync(DataRequest request);
        Task<bool> AnonymizeStudentDataAsync(string studentId);
        bool ValidateParentalConsent(string studentId);
    }

    #endregion

    #region Request/Response Classes

    [Serializable]
    public class ClassroomSessionRequest
    {
        public string sessionName;
        public string educatorId;
        public string classId;
        public DateTime startTime;
        public DateTime endTime;
        public List<string> plannedActivities = new();
        public SessionConfiguration configuration = new();
    }

    [Serializable]
    public class StudentRegistration
    {
        public string studentName;
        public string username;
        public DateTime dateOfBirth;
        public string gradeLevel;
        public string schoolId;
        public string classId;
        public ParentGuardianInfo parentGuardianInfo = new();
        public PrivacySettings privacySettings = new();
        public AccessibilitySettings accessibilitySettings = new();
    }

    [Serializable]
    public class CurriculumFilter
    {
        public string gradeLevel;
        public string subject;
        public List<string> standardIds = new();
        public DifficultyLevel? difficulty;
        public ActivityType? activityType;
        public int maxResults = 50;
    }

    [Serializable]
    public class AssessmentDefinition
    {
        public string assessmentName;
        public string description;
        public AssessmentType assessmentType;
        public List<string> learningObjectiveIds = new();
        public List<AssessmentQuestion> questions = new();
        public AssessmentConfiguration configuration = new();
    }

    [Serializable]
    public class ReportParameters
    {
        public DateTime startDate;
        public DateTime endDate;
        public List<string> includeSubjects = new();
        public bool includeSkillAssessments = true;
        public bool includeRecommendations = true;
        public string reportFormat = "PDF";
    }

    [Serializable]
    public class DataRequest
    {
        public string studentId;
        public DataRequestType requestType;
        public string reason;
        public DateTime requestDate;
        public string requestedBy;
        public bool requiresParentalApproval = true;
    }

    public enum DataRequestType
    {
        Export,
        Delete,
        Anonymize,
        Transfer,
        ViewAccess
    }

    #endregion

    #region Additional Supporting Classes

    [Serializable]
    public class ActivityPopularity
    {
        public string activityId;
        public string activityName;
        public int completionCount;
        public float averageScore;
        public float engagementRating;
    }

    [Serializable]
    public class CommonStruggle
    {
        public string conceptId;
        public string conceptName;
        public int strugglingStudents;
        public float averageScore;
        public List<string> recommendedActions = new();
    }

    [Serializable]
    public class EngagementMetrics
    {
        public float currentEngagement;
        public float averageEngagement;
        public TimeSpan totalActiveTime;
        public int sessionCount;
        public DateTime lastActivity;
    }

    [Serializable]
    public class ProgressMetrics
    {
        public float completionRate;
        public float averageScore;
        public int activitiesCompleted;
        public int objectivesAchieved;
        public ProgressStatus overallStatus;
    }

    [Serializable]
    public class CollaborationMetrics
    {
        public int collaborationCount;
        public float collaborationScore;
        public List<string> frequentPartners = new();
        public float leadershipScore;
    }

    [Serializable]
    public class OverallSummary
    {
        public float overallGrade;
        public string letterGrade;
        public int activitiesCompleted;
        public int objectivesAchieved;
        public TimeSpan totalTimeSpent;
        public string performanceSummary;
    }

    [Serializable]
    public class SubjectProgress
    {
        public string subjectName;
        public float subjectGrade;
        public int completedActivities;
        public int totalActivities;
        public List<string> strengthAreas = new();
        public List<string> growthAreas = new();
    }

    [Serializable]
    public class SkillAssessment
    {
        public string skillName;
        public float currentLevel;
        public float expectedLevel;
        public ProgressStatus status;
        public string recommendation;
    }

    [Serializable]
    public class TeacherComments
    {
        public string overallComment;
        public string strengthsComment;
        public string areasForGrowthComment;
        public string nextStepsComment;
        public DateTime commentDate;
        public string teacherName;
    }

    [Serializable]
    public class Achievement
    {
        public string achievementId;
        public string achievementName;
        public string description;
        public DateTime earnedDate;
        public AchievementType achievementType;
        public string iconUrl;
    }

    public enum AchievementType
    {
        Progress,
        Skill,
        Collaboration,
        Discovery,
        Consistency,
        Excellence
    }

    [Serializable]
    public class LearningObjectiveProgress
    {
        public string objectiveId;
        public string objectiveName;
        public float progress; // 0-100
        public bool isAchieved;
        public DateTime lastUpdate;
        public List<string> evidenceActivities = new();
    }

    [Serializable]
    public class SkillEvidence
    {
        public string activityId;
        public float score;
        public DateTime date;
        public string evidenceType;
    }

    [Serializable]
    public class AssessmentCriteria
    {
        public string criteriaId;
        public string description;
        public float weight;
        public List<string> indicators = new();
    }

    [Serializable]
    public class EducatorDashboard
    {
        public string educatorId;
        public List<ClassSummary> classes = new();
        public List<EducationalAlert> alerts = new();
        public List<StudentAlert> studentAlerts = new();
        public PerformanceOverview performance = new();
        public DateTime lastUpdate;
    }

    [Serializable]
    public class ClassSummary
    {
        public string classId;
        public string className;
        public int totalStudents;
        public int activeStudents;
        public float averageProgress;
        public int alertCount;
    }

    [Serializable]
    public class StudentSummary
    {
        public string studentId;
        public string studentName;
        public float progressPercentage;
        public float engagementScore;
        public DateTime lastActivity;
        public List<string> recentAchievements = new();
        public List<string> alerts = new();
    }

    [Serializable]
    public class StudentAlert
    {
        public string studentId;
        public string studentName;
        public EducationalAlertType alertType;
        public string message;
        public AlertSeverity severity;
        public DateTime timestamp;
    }

    [Serializable]
    public class PerformanceOverview
    {
        public float classAverageProgress;
        public float classAverageEngagement;
        public int totalActivitiesCompleted;
        public int totalAssessmentsCompleted;
        public List<string> topPerformingStudents = new();
        public List<string> strugglingStudents = new();
    }

    [Serializable]
    public class ActivityRecommendation
    {
        public string activityId;
        public string activityName;
        public string reason;
        public float relevanceScore;
        public List<string> targetStudents = new();
        public DifficultyLevel recommendedDifficulty;
    }

    [Serializable]
    public class AssessmentAnalytics
    {
        public string assessmentId;
        public int completionCount;
        public float averageScore;
        public float averageTimeMinutes;
        public List<QuestionAnalytics> questionAnalytics = new();
        public DateTime lastUpdate;
    }

    [Serializable]
    public class QuestionAnalytics
    {
        public string questionId;
        public float correctPercentage;
        public float averageTime;
        public List<string> commonWrongAnswers = new();
        public DifficultyLevel actualDifficulty;
    }

    #endregion
}