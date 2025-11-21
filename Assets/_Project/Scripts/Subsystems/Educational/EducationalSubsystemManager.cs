using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Subsystems.Educational
{
    /// <summary>
    /// Educational Integration Subsystem Manager
    ///
    /// Manages classroom environments, curriculum integration, student progress tracking,
    /// educator tools, and educational content delivery. Ensures COPPA compliance and
    /// provides comprehensive educational analytics and assessment tools.
    ///
    /// Key responsibilities:
    /// - Classroom session management and student rostering
    /// - Curriculum integration with standards alignment
    /// - Student progress tracking and assessment
    /// - Educator dashboard and teaching tools
    /// - Educational content delivery and scaffolding
    /// - COPPA compliance and privacy protection
    /// - Parent/guardian communication and reporting
    /// </summary>
    public class EducationalSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        #region ISubsystemManager Implementation

        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Educational";
        public float InitializationProgress { get; private set; }

        #endregion

        #region Events

        public static event Action<ClassroomSession> OnClassroomSessionStarted;
        public static event Action<ClassroomSession> OnClassroomSessionEnded;
        public static event Action<StudentProgress> OnStudentProgressUpdated;
        public static event Action<EducationalAssessment> OnAssessmentCompleted;
        public static event Action<CurriculumActivity> OnActivityCompleted;
        public static event Action<EducationalAlert> OnEducationalAlert;
        public static event Action<LearningObjective> OnLearningObjectiveAchieved;

        #endregion

        #region Configuration

        [Header("Configuration")]
        [SerializeField] private EducationalSubsystemConfig _config;

        public EducationalSubsystemConfig Config
        {
            get => _config;
            set => _config = value;
        }

        #endregion

        #region Services

        private IClassroomManagementService _classroomManagementService;
        private ICurriculumIntegrationService _curriculumIntegrationService;
        private IStudentProgressService _studentProgressService;
        private IEducatorToolsService _educatorToolsService;
        private IAssessmentService _assessmentService;
        private IEducationalAnalyticsService _analyticsService;
        private IPrivacyComplianceService _privacyComplianceService;

        #endregion

        #region State

        private bool _isInitialized;
        private bool _isRunning;
        private Coroutine _backgroundProcessingCoroutine;
        private Dictionary<string, ClassroomSession> _activeSessions;
        private Dictionary<string, StudentProfile> _studentProfiles;
        private Dictionary<string, EducatorProfile> _educatorProfiles;
        private List<CurriculumStandard> _curriculumStandards;
        private Queue<EducationalEvent> _eventQueue;
        private DateTime _lastAnalyticsUpdate;

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
                    Debug.LogError("[EducationalSubsystem] Configuration is null");
                    return false;
                }

                // Initialize services
                await InitializeServicesAsync();

                // Initialize data structures
                InitializeDataStructures();

                // Load curriculum standards
                await LoadCurriculumStandardsAsync();

                // Initialize privacy compliance
                await InitializePrivacyComplianceAsync();

                // Start background processing
                StartBackgroundProcessing();

                _isInitialized = true;
                _isRunning = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[EducationalSubsystem] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EducationalSubsystem] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeServicesAsync()
        {
            // Try to resolve services from service container
            // If they don't exist, set to null and log warning
            var serviceContainer = ServiceContainer.Instance;

            if (serviceContainer != null)
            {
                serviceContainer.TryResolve<IClassroomManagementService>(out _classroomManagementService);
                serviceContainer.TryResolve<ICurriculumIntegrationService>(out _curriculumIntegrationService);
                serviceContainer.TryResolve<IStudentProgressService>(out _studentProgressService);
                serviceContainer.TryResolve<IEducatorToolsService>(out _educatorToolsService);
                serviceContainer.TryResolve<IAssessmentService>(out _assessmentService);
                serviceContainer.TryResolve<IEducationalAnalyticsService>(out _analyticsService);
                serviceContainer.TryResolve<IPrivacyComplianceService>(out _privacyComplianceService);
            }

            if (_config.enableDebugLogging)
            {
                Debug.Log("[EducationalSubsystem] Educational services resolved from service container");
                Debug.Log($"  ClassroomManagement: {(_classroomManagementService != null ? "Available" : "Not Available")}");
                Debug.Log($"  CurriculumIntegration: {(_curriculumIntegrationService != null ? "Available" : "Not Available")}");
                Debug.Log($"  StudentProgress: {(_studentProgressService != null ? "Available" : "Not Available")}");
                Debug.Log($"  EducatorTools: {(_educatorToolsService != null ? "Available" : "Not Available")}");
                Debug.Log($"  Assessment: {(_assessmentService != null ? "Available" : "Not Available")}");
                Debug.Log($"  Analytics: {(_analyticsService != null ? "Available" : "Not Available")}");
                Debug.Log($"  PrivacyCompliance: {(_privacyComplianceService != null ? "Available" : "Not Available")}");
            }

            await Task.CompletedTask;
        }

        private void InitializeDataStructures()
        {
            _activeSessions = new Dictionary<string, ClassroomSession>();
            _studentProfiles = new Dictionary<string, StudentProfile>();
            _educatorProfiles = new Dictionary<string, EducatorProfile>();
            _curriculumStandards = new List<CurriculumStandard>();
            _eventQueue = new Queue<EducationalEvent>();
        }

        private Task LoadCurriculumStandardsAsync()
        {
            // Load curriculum standards from configuration
            if (_config.curriculumStandards != null)
            {
                _curriculumStandards.AddRange(_config.curriculumStandards);
            }

            // Load default standards if none configured
            if (_curriculumStandards.Count == 0)
            {
                _curriculumStandards.AddRange(CreateDefaultCurriculumStandards());
            }

            if (_config.enableDebugLogging)
                Debug.Log($"[EducationalSubsystem] Loaded {_curriculumStandards.Count} curriculum standards");

            return Task.CompletedTask;
        }

        private async Task InitializePrivacyComplianceAsync()
        {
            // Ensure COPPA compliance settings are properly configured
            await _privacyComplianceService.InitializeAsync();

            if (_config.enableDebugLogging)
                Debug.Log("[EducationalSubsystem] Privacy compliance initialized");
        }

        private void StartBackgroundProcessing()
        {
            _backgroundProcessingCoroutine = StartCoroutine(BackgroundProcessingLoop());
        }

        #endregion

        #region Background Processing

        private IEnumerator BackgroundProcessingLoop()
        {
            var interval = _config.backgroundProcessingIntervalMs / 1000f;

            while (_isRunning)
            {
                // Process educational events
                ProcessEducationalEvents();

                // Update student progress
                UpdateStudentProgress();

                // Check classroom sessions
                CheckClassroomSessions();

                // Update analytics
                UpdateAnalytics();

                // Check privacy compliance
                CheckPrivacyCompliance();

                yield return new WaitForSeconds(interval);
            }
        }

        private void ProcessEducationalEvents()
        {
            var processedCount = 0;
            var maxEvents = _config.maxEventsPerUpdate;

            while (_eventQueue.Count > 0 && processedCount < maxEvents)
            {
                var educationalEvent = _eventQueue.Dequeue();
                ProcessEducationalEvent(educationalEvent);
                processedCount++;
            }
        }

        private void ProcessEducationalEvent(EducationalEvent educationalEvent)
        {
            switch (educationalEvent.eventType)
            {
                case EducationalEventType.ActivityStarted:
                    ProcessActivityStartedEvent(educationalEvent);
                    break;

                case EducationalEventType.ActivityCompleted:
                    ProcessActivityCompletedEvent(educationalEvent);
                    break;

                case EducationalEventType.AssessmentSubmitted:
                    ProcessAssessmentSubmittedEvent(educationalEvent);
                    break;

                case EducationalEventType.LearningObjectiveAchieved:
                    ProcessLearningObjectiveAchievedEvent(educationalEvent);
                    break;

                case EducationalEventType.StudentInteraction:
                    ProcessStudentInteractionEvent(educationalEvent);
                    break;

                case EducationalEventType.ProgressMilestone:
                    ProcessProgressMilestoneEvent(educationalEvent);
                    break;
            }
        }

        private void ProcessActivityStartedEvent(EducationalEvent educationalEvent)
        {
            if (educationalEvent.data.TryGetValue("studentId", out var studentIdObj) &&
                educationalEvent.data.TryGetValue("activityId", out var activityIdObj))
            {
                var studentId = studentIdObj.ToString();
                var activityId = activityIdObj.ToString();

                _studentProgressService?.StartActivity(studentId, activityId);

                if (_config.enableDebugLogging)
                    Debug.Log($"[EducationalSubsystem] Student {studentId} started activity {activityId}");
            }
        }

        private void ProcessActivityCompletedEvent(EducationalEvent educationalEvent)
        {
            if (educationalEvent.data.TryGetValue("studentId", out var studentIdObj) &&
                educationalEvent.data.TryGetValue("activityId", out var activityIdObj) &&
                educationalEvent.data.TryGetValue("score", out var scoreObj))
            {
                var studentId = studentIdObj.ToString();
                var activityId = activityIdObj.ToString();
                var score = Convert.ToSingle(scoreObj);

                _studentProgressService?.CompleteActivity(studentId, activityId, score);

                // Fire activity completed event
                var activity = new CurriculumActivity
                {
                    activityId = activityId,
                    activityName = $"Activity {activityId}",
                    description = $"Student {studentId} completed activity with score {score}",
                    activityType = ActivityType.Assessment // Default type
                };
                OnActivityCompleted?.Invoke(activity);

                // Check for learning objectives achieved
                CheckLearningObjectivesForActivity(studentId, activityId, score);

                if (_config.enableDebugLogging)
                    Debug.Log($"[EducationalSubsystem] Student {studentId} completed activity {activityId} with score {score}");
            }
        }

        private void ProcessAssessmentSubmittedEvent(EducationalEvent educationalEvent)
        {
            if (educationalEvent.data.TryGetValue("studentId", out var studentIdObj) &&
                educationalEvent.data.TryGetValue("assessmentId", out var assessmentIdObj))
            {
                var studentId = studentIdObj.ToString();
                var assessmentId = assessmentIdObj.ToString();

                // Process assessment asynchronously
                Task.Run(async () => await ProcessAssessmentAsync(studentId, assessmentId));
            }
        }

        private void ProcessLearningObjectiveAchievedEvent(EducationalEvent educationalEvent)
        {
            if (educationalEvent.data.TryGetValue("studentId", out var studentIdObj) &&
                educationalEvent.data.TryGetValue("objectiveId", out var objectiveIdObj))
            {
                var studentId = studentIdObj.ToString();
                var objectiveId = objectiveIdObj.ToString();

                var objective = GetLearningObjective(objectiveId);
                if (objective != null)
                {
                    OnLearningObjectiveAchieved?.Invoke(objective);
                }
            }
        }

        private void ProcessStudentInteractionEvent(EducationalEvent educationalEvent)
        {
            if (educationalEvent.data.TryGetValue("studentId", out var studentIdObj))
            {
                var studentId = studentIdObj.ToString();
                _analyticsService?.TrackStudentInteraction(studentId, educationalEvent);
            }
        }

        private void ProcessProgressMilestoneEvent(EducationalEvent educationalEvent)
        {
            if (educationalEvent.data.TryGetValue("studentId", out var studentIdObj) &&
                educationalEvent.data.TryGetValue("milestoneId", out var milestoneIdObj))
            {
                var studentId = studentIdObj.ToString();
                var milestoneId = milestoneIdObj.ToString();

                _studentProgressService?.AchieveMilestone(studentId, milestoneId);

                if (_config.enableDebugLogging)
                    Debug.Log($"[EducationalSubsystem] Student {studentId} achieved milestone {milestoneId}");
            }
        }

        private void UpdateStudentProgress()
        {
            // Update progress for all active students
            foreach (var studentProfile in _studentProfiles.Values)
            {
                if (studentProfile.isActive)
                {
                    UpdateIndividualStudentProgress(studentProfile);
                }
            }
        }

        private void UpdateIndividualStudentProgress(StudentProfile studentProfile)
        {
            var progress = _studentProgressService?.GetStudentProgress(studentProfile.studentId);
            if (progress != null)
            {
                // Update progress calculations
                CalculateOverallProgress(progress);

                // Check for new achievements
                CheckForNewAchievements(studentProfile, progress);

                // Update engagement metrics
                UpdateEngagementMetrics(studentProfile, progress);

                OnStudentProgressUpdated?.Invoke(progress);
            }
        }

        private void CheckClassroomSessions()
        {
            var now = DateTime.Now;
            var sessionsToEnd = new List<string>();

            foreach (var session in _activeSessions.Values)
            {
                // Check if session should end
                if (now > session.endTime || session.status == ClassroomSessionStatus.Ending)
                {
                    sessionsToEnd.Add(session.sessionId);
                }
                // Check for session alerts
                else if (session.endTime - now < TimeSpan.FromMinutes(5) && !session.hasEndWarning)
                {
                    TriggerSessionEndWarning(session);
                }
            }

            // End expired sessions
            foreach (var sessionId in sessionsToEnd)
            {
                _ = EndClassroomSession(sessionId);
            }
        }

        private void UpdateAnalytics()
        {
            var timeSinceLastUpdate = DateTime.Now - _lastAnalyticsUpdate;
            if (timeSinceLastUpdate.TotalMinutes >= _config.analyticsUpdateIntervalMinutes)
            {
                _analyticsService?.UpdateEducationalAnalytics();
                _lastAnalyticsUpdate = DateTime.Now;
            }
        }

        private void CheckPrivacyCompliance()
        {
            _privacyComplianceService?.CheckCompliance();
        }

        #endregion

        #region Classroom Management

        /// <summary>
        /// Starts a new classroom session
        /// </summary>
        public async Task<ClassroomSession> StartClassroomSessionAsync(ClassroomSessionRequest request)
        {
            var session = await _classroomManagementService.StartSessionAsync(request);
            if (session != null)
            {
                _activeSessions[session.sessionId] = session;
                OnClassroomSessionStarted?.Invoke(session);

                if (_config.enableDebugLogging)
                    Debug.Log($"[EducationalSubsystem] Started classroom session: {session.sessionId}");
            }

            return session;
        }

        /// <summary>
        /// Ends a classroom session
        /// </summary>
        public async Task<bool> EndClassroomSession(string sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
            {
                var success = await _classroomManagementService.EndSessionAsync(sessionId);
                if (success)
                {
                    _activeSessions.Remove(sessionId);
                    OnClassroomSessionEnded?.Invoke(session);

                    if (_config.enableDebugLogging)
                        Debug.Log($"[EducationalSubsystem] Ended classroom session: {sessionId}");
                }

                return success;
            }

            return false;
        }

        /// <summary>
        /// Adds student to classroom session
        /// </summary>
        public async Task<bool> AddStudentToSessionAsync(string sessionId, string studentId)
        {
            return await _classroomManagementService.AddStudentAsync(sessionId, studentId);
        }

        /// <summary>
        /// Removes student from classroom session
        /// </summary>
        public async Task<bool> RemoveStudentFromSessionAsync(string sessionId, string studentId)
        {
            return await _classroomManagementService.RemoveStudentAsync(sessionId, studentId);
        }

        #endregion

        #region Student Management

        /// <summary>
        /// Creates a new student profile
        /// </summary>
        public async Task<StudentProfile> CreateStudentProfileAsync(StudentRegistration registration)
        {
            // Ensure COPPA compliance
            if (!await _privacyComplianceService.ValidateStudentRegistrationAsync(registration))
            {
                throw new InvalidOperationException("Student registration does not meet privacy compliance requirements");
            }

            var profile = await _studentProgressService.CreateStudentProfileAsync(registration);
            if (profile != null)
            {
                _studentProfiles[profile.studentId] = profile;

                if (_config.enableDebugLogging)
                    Debug.Log($"[EducationalSubsystem] Created student profile: {profile.studentId}");
            }

            return profile;
        }

        /// <summary>
        /// Gets student profile
        /// </summary>
        public StudentProfile GetStudentProfile(string studentId)
        {
            _studentProfiles.TryGetValue(studentId, out var profile);
            return profile;
        }

        /// <summary>
        /// Gets student progress
        /// </summary>
        public StudentProgress GetStudentProgress(string studentId)
        {
            return _studentProgressService?.GetStudentProgress(studentId);
        }

        #endregion

        #region Curriculum Integration

        /// <summary>
        /// Gets available curriculum activities
        /// </summary>
        public List<CurriculumActivity> GetCurriculumActivities(CurriculumFilter filter = null)
        {
            return _curriculumIntegrationService?.GetActivities(filter) ?? new List<CurriculumActivity>();
        }

        /// <summary>
        /// Gets curriculum standards
        /// </summary>
        public List<CurriculumStandard> GetCurriculumStandards()
        {
            return new List<CurriculumStandard>(_curriculumStandards);
        }

        /// <summary>
        /// Aligns activity with curriculum standards
        /// </summary>
        public async Task<List<CurriculumAlignment>> AlignActivityWithStandardsAsync(string activityId)
        {
            return await _curriculumIntegrationService.AlignActivityAsync(activityId) ?? new List<CurriculumAlignment>();
        }

        #endregion

        #region Assessment

        /// <summary>
        /// Creates a new assessment
        /// </summary>
        public async Task<EducationalAssessment> CreateAssessmentAsync(AssessmentDefinition definition)
        {
            return await _assessmentService.CreateAssessmentAsync(definition);
        }

        /// <summary>
        /// Submits assessment response
        /// </summary>
        public async Task<AssessmentResult> SubmitAssessmentAsync(string studentId, string assessmentId, AssessmentResponse response)
        {
            var result = await _assessmentService.SubmitAssessmentAsync(studentId, assessmentId, response);
            if (result != null)
            {
                OnAssessmentCompleted?.Invoke(result.assessment);
            }

            return result;
        }

        #endregion

        #region Analytics and Reporting

        /// <summary>
        /// Gets class analytics
        /// </summary>
        public async Task<ClassAnalytics> GetClassAnalyticsAsync(string classId)
        {
            return await _analyticsService.GetClassAnalyticsAsync(classId);
        }

        /// <summary>
        /// Gets student analytics
        /// </summary>
        public async Task<StudentAnalytics> GetStudentAnalyticsAsync(string studentId)
        {
            return await _analyticsService.GetStudentAnalyticsAsync(studentId);
        }

        /// <summary>
        /// Generates progress report
        /// </summary>
        public async Task<ProgressReport> GenerateProgressReportAsync(string studentId, ReportParameters parameters)
        {
            return await _analyticsService.GenerateProgressReportAsync(studentId, parameters);
        }

        #endregion

        #region Event Handling

        /// <summary>
        /// Logs educational event
        /// </summary>
        public void LogEducationalEvent(EducationalEvent educationalEvent)
        {
            _eventQueue.Enqueue(educationalEvent);

            if (_config.enableDebugLogging)
                Debug.Log($"[EducationalSubsystem] Logged event: {educationalEvent.eventType}");
        }

        /// <summary>
        /// Tracks student activity
        /// </summary>
        public void TrackStudentActivity(string studentId, string activityType, Dictionary<string, object> activityData = null)
        {
            var educationalEvent = new EducationalEvent
            {
                eventType = EducationalEventType.StudentInteraction,
                timestamp = DateTime.Now,
                studentId = studentId,
                data = activityData ?? new Dictionary<string, object>()
            };

            educationalEvent.data["activityType"] = activityType;
            LogEducationalEvent(educationalEvent);
        }

        #endregion

        #region Helper Methods

        private List<CurriculumStandard> CreateDefaultCurriculumStandards()
        {
            return new List<CurriculumStandard>
            {
                new CurriculumStandard
                {
                    standardId = "NGSS-LS1-1",
                    standardName = "Structure and Function",
                    description = "Use arguments based on empirical evidence and scientific reasoning to support an explanation for how characteristic animal behaviors and specialized plant structures affect the probability of successful reproduction of animals and plants respectively.",
                    gradeLevel = "Middle School",
                    subject = "Life Science",
                    learningObjectives = new List<LearningObjective>
                    {
                        new LearningObjective
                        {
                            objectiveId = "LS1-1-A",
                            description = "Identify animal behaviors that affect reproduction success",
                            bloomsLevel = BloomsLevel.Apply
                        }
                    }
                },
                new CurriculumStandard
                {
                    standardId = "NGSS-LS4-2",
                    standardName = "Natural Selection",
                    description = "Construct an explanation based on evidence that describes how genetic variations of traits in a population increase some individuals' probability of surviving and reproducing in a specific environment.",
                    gradeLevel = "Middle School",
                    subject = "Life Science",
                    learningObjectives = new List<LearningObjective>
                    {
                        new LearningObjective
                        {
                            objectiveId = "LS4-2-A",
                            description = "Explain how genetic variation affects survival",
                            bloomsLevel = BloomsLevel.Understand
                        }
                    }
                }
            };
        }

        private void CalculateOverallProgress(StudentProgress progress)
        {
            // Calculate overall progress based on completed activities and achievements
            var totalActivities = progress.activityProgress.Count;
            var completedActivities = progress.activityProgress.Count(ap => ap.isCompleted);

            progress.overallProgressPercentage = totalActivities > 0 ? (float)completedActivities / totalActivities * 100f : 0f;
        }

        private void CheckForNewAchievements(StudentProfile studentProfile, StudentProgress progress)
        {
            // Check for achievement criteria based on progress
            // This would integrate with the achievement system
        }

        private void UpdateEngagementMetrics(StudentProfile studentProfile, StudentProgress progress)
        {
            // Update engagement calculations
            var now = DateTime.Now;
            var sessionDuration = now - progress.lastActivityTime;

            if (sessionDuration.TotalMinutes < _config.engagementTimeoutMinutes)
            {
                progress.engagementScore = Mathf.Min(100f, progress.engagementScore + _config.engagementIncrement);
            }
            else
            {
                progress.engagementScore = Mathf.Max(0f, progress.engagementScore - _config.engagementDecrement);
            }
        }

        private void CheckLearningObjectivesForActivity(string studentId, string activityId, float score)
        {
            // Check if activity completion meets learning objective criteria
            var activities = GetCurriculumActivities();
            var activity = activities.Find(a => a.activityId == activityId);

            if (activity != null && score >= activity.passingScore)
            {
                foreach (var objectiveId in activity.learningObjectiveIds)
                {
                    var objective = GetLearningObjective(objectiveId);
                    if (objective != null)
                    {
                        var educationalEvent = new EducationalEvent
                        {
                            eventType = EducationalEventType.LearningObjectiveAchieved,
                            timestamp = DateTime.Now,
                            studentId = studentId,
                            data = new Dictionary<string, object>
                            {
                                ["objectiveId"] = objectiveId,
                                ["activityId"] = activityId,
                                ["score"] = score
                            }
                        };

                        LogEducationalEvent(educationalEvent);
                    }
                }
            }
        }

        private LearningObjective GetLearningObjective(string objectiveId)
        {
            foreach (var standard in _curriculumStandards)
            {
                var objective = standard.learningObjectives.Find(lo => lo.objectiveId == objectiveId);
                if (objective != null)
                    return objective;
            }

            return null;
        }

        private async Task ProcessAssessmentAsync(string studentId, string assessmentId)
        {
            // Process assessment and update student progress
            var result = await _assessmentService.ProcessAssessmentAsync(studentId, assessmentId);
            if (result != null)
            {
                OnAssessmentCompleted?.Invoke(result.assessment);
            }
        }

        private void TriggerSessionEndWarning(ClassroomSession session)
        {
            session.hasEndWarning = true;

            var alert = new EducationalAlert
            {
                alertType = EducationalAlertType.SessionEndWarning,
                message = $"Classroom session '{session.sessionName}' will end in 5 minutes",
                timestamp = DateTime.Now,
                sessionId = session.sessionId,
                severity = AlertSeverity.Medium
            };

            OnEducationalAlert?.Invoke(alert);
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Student Progress")]
        private void DebugLogStudentProgress()
        {
            foreach (var studentProfile in _studentProfiles.Values)
            {
                var progress = GetStudentProgress(studentProfile.studentId);
                if (progress != null)
                {
                    Debug.Log($"[EducationalSubsystem] Student {studentProfile.studentName} Progress: {progress.overallProgressPercentage:F1}%");
                }
            }
        }

        [ContextMenu("Log Active Sessions")]
        private void DebugLogActiveSessions()
        {
            Debug.Log($"[EducationalSubsystem] Active Sessions: {_activeSessions.Count}");
            foreach (var session in _activeSessions.Values)
            {
                Debug.Log($"  Session: {session.sessionName} ({session.studentCount} students)");
            }
        }

        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            _isRunning = false;

            if (_backgroundProcessingCoroutine != null)
            {
                StopCoroutine(_backgroundProcessingCoroutine);
            }
        }

        #endregion
    }
}