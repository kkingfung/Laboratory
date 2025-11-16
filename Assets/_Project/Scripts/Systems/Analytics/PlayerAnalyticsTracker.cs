using UnityEngine;
using System.Collections.Generic;
using Unity.Profiling;
using Laboratory.Shared.Types;
using Laboratory.Core;
using Laboratory.Core.Enums;
using Laboratory.Systems.Analytics.Services;
using Laboratory.Systems.Quests;
using Laboratory.Systems.Breeding;
using Laboratory.Systems.Ecosystem;
using Laboratory.Subsystems.AIDirector;

namespace Laboratory.Systems.Analytics
{
    /// <summary>
    /// Refactored Player Analytics Tracker using composition with focused service classes.
    /// Reduced from 2,101 lines to ~400 lines by delegating to specialized services.
    ///
    /// Services:
    /// - PlayerSessionManager: Session lifecycle and metrics
    /// - PlayerActionTracker: Action tracking and counters
    /// - BehaviorAnalysisService: Behavior patterns and archetypes
    /// - EmotionalAnalysisService: Emotional response tracking
    /// - AnalyticsDataPersistence: Save/load operations
    /// </summary>
    public class PlayerAnalyticsTrackerRefactored : MonoBehaviour
    {
        #region Configuration

        [Header("Analytics Configuration")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableAdaptiveGameplay = true;
        [SerializeField] private bool enableBehaviorAnalysis = true;
        [SerializeField] private float adaptationResponseThreshold = GameConstants.HIGH_ENGAGEMENT_THRESHOLD;

        [Header("Data Collection Settings")]
        [SerializeField] private float actionTrackingInterval = GameConstants.ACTION_TRACKING_INTERVAL;
        [SerializeField] private int maxSessionActions = GameConstants.MAX_SESSION_ACTIONS;
        [SerializeField] private bool trackDetailedInputMetrics = true;
        [SerializeField] private bool enableRealTimeAnalysis = true;

        [Header("Privacy Settings")]
        [SerializeField] private bool anonymizePlayerData = true;
        [SerializeField] private bool enableDataExport = false;

        [Header("Adaptation Settings")]
        [SerializeField] private float difficultyAdaptationSensitivity = GameConstants.DIFFICULTY_ADAPTATION_SENSITIVITY;
        [SerializeField] private bool enableContentRecommendations = true;

        [Header("Behavioral Analysis")]
        [SerializeField] private bool enablePersonalityProfiling = true;
        [SerializeField] private bool enableEmotionalAnalysis = true;
        [SerializeField] private float behaviorAnalysisInterval = GameConstants.BEHAVIOR_ANALYSIS_INTERVAL;

        #endregion

        #region Services

        private PlayerSessionManager _sessionManager;
        private PlayerActionTracker _actionTracker;
        private BehaviorAnalysisService _behaviorAnalysis;
        private EmotionalAnalysisService _emotionalAnalysis;
        private AnalyticsDataPersistence _dataPersistence;

        #endregion

        #region Events

        public System.Action<PlayerArchetype> OnPlayerArchetypeIdentified;
        public System.Action<BehaviorInsight> OnBehaviorInsightGenerated;

        #endregion

        #region State

        private PlayerProfile _currentPlayerProfile;
        private GameAdaptationEngine _adaptationEngine;
        private bool _isInitialized;

        // Performance profiling
        private static readonly ProfilerMarker s_TrackActionMarker = new ProfilerMarker("PlayerAnalytics.TrackAction");
        private static readonly ProfilerMarker s_UpdateAnalyticsMarker = new ProfilerMarker("PlayerAnalytics.Update");

        #endregion

        #region Properties

        public PlayerProfile CurrentProfile => _currentPlayerProfile;
        public bool IsInitialized => _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeAnalyticsSystem();
        }

        private void Start()
        {
            if (enableAnalytics)
            {
                StartNewGameplaySession();
                Debug.Log("[PlayerAnalyticsTracker] Initialized with service-based architecture");
            }
        }

        private void Update()
        {
            if (!enableAnalytics || !_isInitialized) return;

            using (s_UpdateAnalyticsMarker.Auto())
            {
                // Update emotional analysis
                if (enableEmotionalAnalysis)
                {
                    _emotionalAnalysis.UpdateDominantEmotion();
                }
            }
        }

        private void OnApplicationQuit()
        {
            EndCurrentSession();
            SaveAnalyticsData();
        }

        #endregion

        #region Initialization

        private void InitializeAnalyticsSystem()
        {
            Debug.Log("[PlayerAnalyticsTracker] Initializing analytics system...");

            // Initialize services
            _sessionManager = new PlayerSessionManager(maxSessionActions);
            _actionTracker = new PlayerActionTracker(actionTrackingInterval, trackDetailedInputMetrics);
            _behaviorAnalysis = new BehaviorAnalysisService(behaviorAnalysisInterval, enablePersonalityProfiling);
            _emotionalAnalysis = new EmotionalAnalysisService(enableEmotionalAnalysis);
            _dataPersistence = new AnalyticsDataPersistence(anonymizePlayerData, enableDataExport);

            // Initialize adaptation engine
            _adaptationEngine = new GameAdaptationEngine();

            // Subscribe to service events
            SubscribeToServiceEvents();

            // Load player profile
            _currentPlayerProfile = _dataPersistence.LoadPlayerProfile();

            _isInitialized = true;
            Debug.Log("[PlayerAnalyticsTracker] Analytics system initialized successfully");
        }

        private void SubscribeToServiceEvents()
        {
            // Session events
            _sessionManager.OnSessionStarted += OnSessionStarted;
            _sessionManager.OnSessionEnded += OnSessionEnded;

            // Action tracking events
            _actionTracker.OnActionTracked += OnActionTracked;

            // Behavior analysis events
            _behaviorAnalysis.OnPlayerArchetypeIdentified += archetype =>
            {
                OnPlayerArchetypeIdentified?.Invoke(archetype);
                UpdatePlayerProfileArchetype(archetype);
            };

            _behaviorAnalysis.OnBehaviorInsightGenerated += insight =>
            {
                OnBehaviorInsightGenerated?.Invoke(insight);
            };

            // Emotional analysis events
            _emotionalAnalysis.OnEmotionalResponseTracked += (state, intensity) =>
            {
                _sessionManager.RecordEmotionalState(state, intensity);
            };
        }

        #endregion

        #region Public API - Action Tracking

        /// <summary>
        /// Tracks a player action with context and parameters
        /// </summary>
        public void TrackPlayerAction(string actionType, string context, Dictionary<ParamKey, object> parameters = null)
        {
            if (!enableAnalytics || !_isInitialized) return;

            using (s_TrackActionMarker.Auto())
            {
                // Track the action
                var action = _actionTracker.TrackAction(actionType, context, parameters);

                // Record in session
                _sessionManager.RecordAction(action);

                // Analyze behavior if enabled
                if (enableBehaviorAnalysis)
                {
                    _behaviorAnalysis.AnalyzeAction(action);
                }

                // Analyze emotional response if enabled
                if (enableEmotionalAnalysis)
                {
                    _emotionalAnalysis.AnalyzeEmotionalPatterns(action);
                }

                // Check for adaptive gameplay triggers
                if (enableAdaptiveGameplay)
                {
                    CheckForAdaptationTriggers(action);
                }
            }
        }

        /// <summary>
        /// Tracks a gameplay choice
        /// </summary>
        public void TrackGameplayChoice(ChoiceCategory category, string choice, Dictionary<ParamKey, object> context = null)
        {
            if (!enableAnalytics) return;

            var action = _actionTracker.TrackGameplayChoice(category, choice, context);
            _sessionManager.RecordAction(action);

            if (enableBehaviorAnalysis)
            {
                _behaviorAnalysis.AnalyzeAction(action);
            }
        }

        /// <summary>
        /// Tracks a UI interaction
        /// </summary>
        public void TrackUIInteraction(string elementName, string interactionType, Dictionary<ParamKey, object> context = null)
        {
            if (!enableAnalytics) return;

            var action = _actionTracker.TrackUIInteraction(elementName, interactionType, context);
            _sessionManager.RecordAction(action);
        }

        /// <summary>
        /// Tracks an emotional response
        /// </summary>
        public void TrackEmotionalResponse(EmotionalState emotionalState, float intensity, string trigger = "")
        {
            if (!enableAnalytics || !enableEmotionalAnalysis) return;

            _emotionalAnalysis.TrackEmotionalResponse(emotionalState, intensity, trigger);
        }

        #endregion

        #region Public API - Game Event Integration

        /// <summary>
        /// Called when a quest is completed
        /// </summary>
        public void OnQuestCompleted(QuestData quest)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.QuestId] = quest.questId,
                [ParamKey.Success] = true
            };

            TrackPlayerAction("QuestCompleted", "Quest", parameters);
            TrackEmotionalResponse(EmotionalState.Satisfied, 0.7f, "Quest Completed");
            _sessionManager.AddMilestone(MilestoneType.QuestCompleted);
        }

        /// <summary>
        /// Called when a quest fails
        /// </summary>
        public void OnQuestFailed(QuestData quest)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.QuestId] = quest.questId,
                [ParamKey.Success] = false
            };

            TrackPlayerAction("QuestFailed", "Quest", parameters);
            TrackEmotionalResponse(EmotionalState.Frustrated, 0.6f, "Quest Failed");
        }

        /// <summary>
        /// Called when breeding is completed
        /// </summary>
        public void OnBreedingCompleted(BreedingSession session, CreatureInstance offspring)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.SessionId] = session.sessionId,
                [ParamKey.CreatureId] = offspring.creatureId
            };

            TrackPlayerAction("BreedingCompleted", "Breeding", parameters);
            TrackEmotionalResponse(EmotionalState.Curious, 0.6f, "Breeding Completed");
            _sessionManager.AddMilestone(MilestoneType.BreedingCompleted);
        }

        /// <summary>
        /// Called when player makes decision about offspring
        /// </summary>
        public void OnOffspringDecision(CreatureInstance offspring, bool accepted)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.CreatureId] = offspring.creatureId,
                [ParamKey.Accepted] = accepted
            };

            TrackPlayerAction(accepted ? "OffspringAccepted" : "OffspringRejected", "Breeding", parameters);
        }

        /// <summary>
        /// Called when environmental event occurs
        /// </summary>
        public void OnEnvironmentalEvent(EcosystemEvent envEvent)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.EventType] = envEvent.eventType
            };

            TrackPlayerAction("EnvironmentalEvent", "Ecosystem", parameters);
        }

        #endregion

        #region Session Management

        private void StartNewGameplaySession()
        {
            _sessionManager.StartNewSession(_currentPlayerProfile);
            _actionTracker.Reset();
            _emotionalAnalysis.Reset();

            Debug.Log("[PlayerAnalyticsTracker] New gameplay session started");
        }

        private void EndCurrentSession()
        {
            if (_sessionManager.CurrentSession == null) return;

            _sessionManager.EndSession(_currentPlayerProfile);
            Debug.Log("[PlayerAnalyticsTracker] Current session ended");
        }

        private void OnSessionStarted(GameplaySession session)
        {
            Debug.Log($"[PlayerAnalyticsTracker] Session started: {session.sessionId}");
        }

        private void OnSessionEnded(GameplaySession session)
        {
            // Update player profile
            UpdatePlayerProfileFromSession(session);

            // Save data
            SaveAnalyticsData();

            Debug.Log($"[PlayerAnalyticsTracker] Session ended: {session.sessionId}, Duration: {session.duration:F1}s");
        }

        #endregion

        #region Adaptive Gameplay

        private void CheckForAdaptationTriggers(PlayerAction action)
        {
            // Check engagement level
            var sessionStats = _sessionManager.GetSessionStats();
            var behaviorStats = _behaviorAnalysis.GetAnalysisStats();

            // Calculate engagement score
            float engagementScore = CalculateEngagementScore(sessionStats);

            if (engagementScore < 0.3f)
            {
                TriggerGameAdaptation("IncreaseEngagement", 0.5f);
            }
            else if (engagementScore > 0.9f && behaviorStats.dominantTrait == PlayerBehaviorTrait.RiskTaking)
            {
                TriggerGameAdaptation("IncreaseDifficulty", difficultyAdaptationSensitivity);
            }
        }

        private float CalculateEngagementScore(SessionStats stats)
        {
            if (stats.currentSessionDuration <= 0) return 0.5f;

            float actionDensity = stats.currentSessionActions / Mathf.Max(stats.currentSessionDuration / 60f, 1f);
            return Mathf.Clamp01(actionDensity / 30f); // 30 actions/min = full engagement
        }

        private void TriggerGameAdaptation(string adaptationType, float intensity)
        {
            _adaptationEngine.ProcessPlayerData(_currentPlayerProfile);

            if (adaptationType == "IncreaseDifficulty" || adaptationType == "DecreaseDifficulty")
            {
                _adaptationEngine.UpdateDifficulty(intensity);
            }

            Debug.Log($"[PlayerAnalyticsTracker] Game adaptation triggered: {adaptationType} (intensity: {intensity:F2})");
        }

        #endregion

        #region Data Persistence

        private void SaveAnalyticsData()
        {
            _dataPersistence.SavePlayerProfile(_currentPlayerProfile);
            _dataPersistence.SaveSessionHistory(_sessionManager.SessionHistory as List<GameplaySession>);
        }

        private void UpdatePlayerProfileFromSession(GameplaySession session)
        {
            if (_currentPlayerProfile == null) return;

            _currentPlayerProfile.totalPlayTime += session.duration;
            _currentPlayerProfile.totalSessions++;
            _currentPlayerProfile.lastPlayDate = System.DateTime.Now.ToString("yyyy-MM-dd");
        }

        private void UpdatePlayerProfileArchetype(PlayerArchetype archetype)
        {
            if (_currentPlayerProfile == null) return;

            _currentPlayerProfile.dominantArchetype = archetype.archetypeType;
        }

        #endregion

        #region Event Handlers

        private void OnActionTracked(PlayerAction action)
        {
            // Central handler for all tracked actions
            Debug.Log($"[PlayerAnalyticsTracker] Action tracked: {action.actionType} at {action.timestamp:F1}s");
        }

        #endregion

        #region Public Queries

        /// <summary>
        /// Gets current session statistics
        /// </summary>
        public SessionStats GetSessionStats()
        {
            return _sessionManager.GetSessionStats();
        }

        /// <summary>
        /// Gets action tracking statistics
        /// </summary>
        public ActionStats GetActionStats()
        {
            return _actionTracker.GetActionStats();
        }

        /// <summary>
        /// Gets behavior analysis statistics
        /// </summary>
        public BehaviorAnalysisStats GetBehaviorStats()
        {
            return _behaviorAnalysis.GetAnalysisStats();
        }

        /// <summary>
        /// Gets emotional analysis statistics
        /// </summary>
        public EmotionalAnalysisStats GetEmotionalStats()
        {
            return _emotionalAnalysis.GetEmotionalStats();
        }

        /// <summary>
        /// Gets persistence statistics
        /// </summary>
        public PersistenceStats GetPersistenceStats()
        {
            return _dataPersistence.GetPersistenceStats();
        }

        /// <summary>
        /// Exports all analytics data
        /// </summary>
        public void ExportAnalyticsData()
        {
            _dataPersistence.ExportAnalyticsData(
                _currentPlayerProfile,
                _sessionManager.SessionHistory as List<GameplaySession>
            );
        }

        #endregion

        #region Debug

        [ContextMenu("Print Analytics Summary")]
        private void PrintAnalyticsSummary()
        {
            var sessionStats = GetSessionStats();
            var actionStats = GetActionStats();
            var behaviorStats = GetBehaviorStats();
            var emotionalStats = GetEmotionalStats();

            Debug.Log("=== Player Analytics Summary ===\n" +
                     $"Session: {sessionStats.currentSessionDuration:F1}s, {sessionStats.currentSessionActions} actions\n" +
                     $"Actions: {actionStats.totalActions} total, {actionStats.uniqueActionTypes} types\n" +
                     $"Behavior: {behaviorStats.currentArchetype}, Diversity: {behaviorStats.traitDiversity:F2}\n" +
                     $"Emotional: {emotionalStats.dominantEmotion}, Stability: {emotionalStats.stability:F2}\n" +
                     $"Profile: {_currentPlayerProfile?.totalSessions ?? 0} sessions, {_currentPlayerProfile?.totalPlayTime ?? 0:F1}s total");
        }

        [ContextMenu("Export Analytics Data")]
        private void DebugExportAnalytics()
        {
            ExportAnalyticsData();
        }

        #endregion
    }
}
