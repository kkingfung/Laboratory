using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using System.Linq;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Subsystems.Analytics;
using Laboratory.Subsystems.Genetics;
using Laboratory.Subsystems.Research;
using Laboratory.Subsystems.Ecosystem;
using Laboratory.Subsystems.AIDirector.Services;
using Laboratory.Chimera.Genetics;

namespace Laboratory.Subsystems.AIDirector
{
    /// <summary>
    /// AI Director Subsystem Manager
    ///
    /// Provides intelligent game direction, dynamic content adaptation, and emergent
    /// narrative experiences. Uses AI-driven systems to analyze player behavior,
    /// adapt difficulty, create meaningful discoveries, and orchestrate engaging
    /// educational experiences.
    ///
    /// Key responsibilities:
    /// - Dynamic difficulty and content adaptation
    /// - Emergent narrative generation and storytelling
    /// - Player behavior analysis and prediction
    /// - Intelligent creature spawn and ecosystem management
    /// - Educational pacing and scaffolding
    /// - Discovery orchestration and timing
    /// - Collaborative experience enhancement
    /// </summary>
    public class AIDirectorSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        #region ISubsystemManager Implementation

        public bool IsInitialized { get; private set; }
        public string SubsystemName => "AIDirector";
        public float InitializationProgress { get; private set; }

        #endregion

        #region Events

        public static event Action<DirectorDecision> OnDirectorDecisionMade;
        public static event Action<PlayerProfile> OnPlayerProfileUpdated;
        public static event Action<NarrativeEvent> OnNarrativeEventTriggered;
        public static event Action<DifficultyAdjustment> OnDifficultyAdjusted;
        public static event Action<ContentAdaptation> OnContentAdapted;
        public static event Action<EmergentStory> OnEmergentStoryGenerated;

        #endregion

        #region Configuration

        [Header("Configuration")]
        [SerializeField] private AIDirectorSubsystemConfig _config;

        public AIDirectorSubsystemConfig Config
        {
            get => _config;
            set => _config = value;
        }

        #endregion

        #region Services

        private IPlayerAnalysisService _playerAnalysisService;
        private IDifficultyAdaptationService _difficultyAdaptationService;
        private INarrativeGenerationService _narrativeGenerationService;
        private IContentOrchestrationService _contentOrchestrationService;
        private IEmergentStorytellingService _emergentStorytellingService;
        private IBehaviorPredictionService _behaviorPredictionService;
        private IEducationalScaffoldingService _educationalScaffoldingService;

        // Extracted service-based composition (Phase 1 refactoring)
        private PlayerProfileService _playerProfileService;
        private BehavioralAnalysisService _behavioralAnalysisService;
        private EducationalContentService _educationalContentService;
        private DecisionExecutionService _decisionExecutionService;
        private EventHandlerService _eventHandlerService;

        #endregion

        #region State

        private bool _isInitialized;
        private bool _isRunning;
        private Coroutine _directorLoopCoroutine;
        private Dictionary<string, PlayerProfile> _playerProfiles;
        private Dictionary<string, DirectorContext> _playerContexts;
        private Queue<DirectorEvent> _eventQueue;
        private List<ActiveStoryline> _activeStorylines;
        private DirectorState _currentState;
        private DateTime _lastAnalysisUpdate;

        // AI Decision Making
        private DecisionMatrix _decisionMatrix;
        private List<DirectorRule> _activeRules;
        private Dictionary<string, float> _contextWeights;

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
                    Debug.LogError("[AIDirectorSubsystem] Configuration is null");
                    return false;
                }

                // Initialize services
                await InitializeServicesAsync();

                // Initialize AI components
                InitializeAIComponents();

                // Load director rules and decision matrix
                LoadDirectorRules();

                // Initialize player tracking
                InitializePlayerTracking();

                // Start director processing loop
                StartDirectorLoop();

                _isInitialized = true;
                _isRunning = true;

                if (_config.enableDebugLogging)
                    Debug.Log("[AIDirectorSubsystem] Initialized successfully");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIDirectorSubsystem] Failed to initialize: {ex.Message}");
                return false;
            }
        }

        private async Task InitializeServicesAsync()
        {
            // Initialize player analysis service
            _playerAnalysisService = new PlayerAnalysisService(_config);
            await _playerAnalysisService.InitializeAsync();

            // Initialize difficulty adaptation service
            _difficultyAdaptationService = new DifficultyAdaptationService(_config);
            await _difficultyAdaptationService.InitializeAsync();

            // Initialize narrative generation service
            _narrativeGenerationService = new NarrativeGenerationService(_config);
            await _narrativeGenerationService.InitializeAsync();

            // Initialize content orchestration service
            _contentOrchestrationService = new ContentOrchestrationService(_config);
            await _contentOrchestrationService.InitializeAsync();

            // Initialize emergent storytelling service
            _emergentStorytellingService = new EmergentStorytellingService(_config);
            await _emergentStorytellingService.InitializeAsync();

            // Initialize behavior prediction service
            _behaviorPredictionService = new BehaviorPredictionService(_config);
            await _behaviorPredictionService.InitializeAsync();

            // Initialize educational scaffolding service
            _educationalScaffoldingService = new EducationalScaffoldingService(_config);
            await _educationalScaffoldingService.InitializeAsync();

            // Register with service container
            ServiceContainer.Instance?.RegisterService<IPlayerAnalysisService>(_playerAnalysisService);
            ServiceContainer.Instance?.RegisterService<IDifficultyAdaptationService>(_difficultyAdaptationService);
            ServiceContainer.Instance?.RegisterService<INarrativeGenerationService>(_narrativeGenerationService);
            ServiceContainer.Instance?.RegisterService<IContentOrchestrationService>(_contentOrchestrationService);
            ServiceContainer.Instance?.RegisterService<IEmergentStorytellingService>(_emergentStorytellingService);
            ServiceContainer.Instance?.RegisterService<IBehaviorPredictionService>(_behaviorPredictionService);
            ServiceContainer.Instance?.RegisterService<IEducationalScaffoldingService>(_educationalScaffoldingService);
        }

        private void InitializeAIComponents()
        {
            _playerProfiles = new Dictionary<string, PlayerProfile>();
            _playerContexts = new Dictionary<string, DirectorContext>();
            _eventQueue = new Queue<DirectorEvent>();
            _activeStorylines = new List<ActiveStoryline>();
            _currentState = new DirectorState();
            _decisionMatrix = new DecisionMatrix();
            _activeRules = new List<DirectorRule>();
            _contextWeights = new Dictionary<string, float>();

            // Initialize extracted services (Phase 1 refactoring)
            InitializeExtractedServices();
        }

        private void InitializeExtractedServices()
        {
            // Initialize PlayerProfileService
            _playerProfileService = new PlayerProfileService(_playerProfiles, _config, _eventQueue);

            // Initialize BehavioralAnalysisService
            _behavioralAnalysisService = new BehavioralAnalysisService(_playerProfiles);

            // Initialize DecisionExecutionService (needs to be initialized before EducationalContentService)
            _decisionExecutionService = new DecisionExecutionService(
                _playerProfiles,
                _config,
                _difficultyAdaptationService,
                _narrativeGenerationService,
                _contentOrchestrationService,
                _educationalScaffoldingService);

            // Wire up decision execution service events
            _decisionExecutionService.OnDirectorDecisionMade += (decision) => OnDirectorDecisionMade?.Invoke(decision);
            _decisionExecutionService.OnDifficultyAdjusted += (adjustment) => OnDifficultyAdjusted?.Invoke(adjustment);
            _decisionExecutionService.OnNarrativeEventTriggered += (narrativeEvent) => OnNarrativeEventTriggered?.Invoke(narrativeEvent);

            // Initialize EducationalContentService
            _educationalContentService = new EducationalContentService(
                _playerProfiles,
                _config,
                _decisionExecutionService.ExecuteDirectorDecision);

            // Initialize EventHandlerService
            _eventHandlerService = new EventHandlerService(
                _playerProfiles,
                _config,
                _playerProfileService,
                _behavioralAnalysisService,
                _educationalContentService,
                _decisionExecutionService);
        }

        private void LoadDirectorRules()
        {
            // Load default director rules from configuration
            if (_config.directorRules != null)
            {
                _activeRules.AddRange(_config.directorRules);
            }

            // Load default decision matrix
            InitializeDecisionMatrix();

            // Initialize context weights
            InitializeContextWeights();
        }

        private void InitializeDecisionMatrix()
        {
            _decisionMatrix = new DecisionMatrix
            {
                difficultyWeights = new Dictionary<DirectorContext.ContextType, float>
                {
                    { DirectorContext.ContextType.PlayerSkill, 0.4f },
                    { DirectorContext.ContextType.Engagement, 0.3f },
                    { DirectorContext.ContextType.Progress, 0.2f },
                    { DirectorContext.ContextType.Time, 0.1f }
                },
                narrativeWeights = new Dictionary<string, float>
                {
                    { "discovery", 0.3f },
                    { "collaboration", 0.25f },
                    { "exploration", 0.2f },
                    { "achievement", 0.15f },
                    { "challenge", 0.1f }
                }
            };
        }

        private void InitializeContextWeights()
        {
            _contextWeights["engagement"] = _config.engagementWeight;
            _contextWeights["skill"] = _config.skillWeight;
            _contextWeights["progress"] = _config.progressWeight;
            _contextWeights["social"] = _config.socialWeight;
            _contextWeights["educational"] = _config.educationalWeight;
        }

        private void InitializePlayerTracking()
        {
            try
            {
                // Subscribe to genetics events
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete += _eventHandlerService.HandleBreedingEvent;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred += _eventHandlerService.HandleMutationEvent;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered += _eventHandlerService.HandleTraitDiscoveryEvent;

                // Subscribe to research events
                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnDiscoveryLogged += _eventHandlerService.HandleResearchDiscoveryEvent;
                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnPublicationCreated += _eventHandlerService.HandlePublicationEvent;

                // Subscribe to ecosystem events
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent += _eventHandlerService.HandleEnvironmentalEvent;
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnPopulationChanged += _eventHandlerService.HandlePopulationEvent;

                // Subscribe to analytics events
                Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnPlayerActionTracked += _eventHandlerService.HandlePlayerActionEvent;
                Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnEducationalProgressTracked += _eventHandlerService.HandleEducationalProgressEvent;

                if (_config.enableDebugLogging)
                    Debug.Log("[AIDirector] Player tracking events initialized - Analytics integration active, other subsystems pending type availability");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AIDirector] Failed to initialize player tracking: {ex.Message}");
            }
        }

        private void StartDirectorLoop()
        {
            _directorLoopCoroutine = StartCoroutine(DirectorProcessingLoop());
        }

        #endregion

        #region Director Processing Loop

        private IEnumerator DirectorProcessingLoop()
        {
            var interval = _config.directorUpdateIntervalMs / 1000f;

            while (_isRunning)
            {
                // Process director events
                ProcessDirectorEvents();

                // Analyze player behavior
                AnalyzePlayerBehavior();

                // Update player contexts
                UpdatePlayerContexts();

                // Make director decisions
                MakeDirectorDecisions();

                // Update active storylines
                UpdateActiveStorylines();

                // Generate emergent content
                GenerateEmergentContent();

                // Adapt difficulty and content
                AdaptDifficultyAndContent();

                yield return new WaitForSeconds(interval);
            }
        }

        private void ProcessDirectorEvents()
        {
            var processedCount = 0;
            var maxEvents = _config.maxEventsPerUpdate;

            while (_eventQueue.Count > 0 && processedCount < maxEvents)
            {
                var directorEvent = _eventQueue.Dequeue();
                ProcessDirectorEvent(directorEvent);
                processedCount++;
            }
        }

        private void ProcessDirectorEvent(DirectorEvent directorEvent)
        {
            switch (directorEvent.eventType)
            {
                case DirectorEventType.PlayerAction:
                    ProcessPlayerActionEvent(directorEvent);
                    break;

                case DirectorEventType.Discovery:
                    ProcessDiscoveryEvent(directorEvent);
                    break;

                case DirectorEventType.Achievement:
                    ProcessAchievementEvent(directorEvent);
                    break;

                case DirectorEventType.Collaboration:
                    ProcessCollaborationEvent(directorEvent);
                    break;

                case DirectorEventType.Struggle:
                    ProcessStruggleEvent(directorEvent);
                    break;

                case DirectorEventType.Milestone:
                    ProcessMilestoneEvent(directorEvent);
                    break;
            }
        }

        private void ProcessPlayerActionEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            if (!string.IsNullOrEmpty(playerId))
            {
                _playerAnalysisService?.TrackPlayerAction(playerId, directorEvent);
                _playerProfileService.UpdatePlayerEngagement(playerId, directorEvent);
            }
        }

        private void ProcessDiscoveryEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var discoveryType = directorEvent.data.GetValueOrDefault("discoveryType", "unknown").ToString();

            // Update player discovery patterns
            _playerAnalysisService?.TrackDiscovery(playerId, discoveryType);

            // Check for narrative opportunities
            _narrativeGenerationService?.AnalyzeDiscoveryOpportunities(playerId, discoveryType);
        }

        private void ProcessAchievementEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var achievementType = directorEvent.data.GetValueOrDefault("achievementType", "unknown").ToString();

            // Celebrate achievement and check for story progression
            _narrativeGenerationService?.TriggerAchievementNarrative(playerId, achievementType);

            // Update player confidence and engagement
            _playerProfileService.UpdatePlayerConfidence(playerId, true);
        }

        private void ProcessCollaborationEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var collaborationType = directorEvent.data.GetValueOrDefault("collaborationType", "unknown").ToString();

            // Track collaboration patterns
            _playerAnalysisService?.TrackCollaboration(playerId, collaborationType);

            // Generate collaborative storylines
            _emergentStorytellingService?.GenerateCollaborativeStorylines(playerId, collaborationType);
        }

        private void ProcessStruggleEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var struggleType = directorEvent.data.GetValueOrDefault("struggleType", "unknown").ToString();

            // Provide educational scaffolding
            _educationalScaffoldingService?.ProvideSupport(playerId, struggleType);

            // Update player confidence
            _playerProfileService.UpdatePlayerConfidence(playerId, false);
        }

        private void ProcessMilestoneEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var milestoneType = directorEvent.data.GetValueOrDefault("milestoneType", "unknown").ToString();

            // Generate milestone celebration
            _narrativeGenerationService?.GenerateMilestoneCelebration(playerId, milestoneType);
        }

        private void AnalyzePlayerBehavior()
        {
            var timeSinceLastAnalysis = DateTime.Now - _lastAnalysisUpdate;
            if (timeSinceLastAnalysis.TotalMinutes >= _config.playerAnalysisIntervalMinutes)
            {
                foreach (var playerId in _playerProfiles.Keys)
                {
                    AnalyzeIndividualPlayer(playerId);
                }

                _lastAnalysisUpdate = DateTime.Now;
            }
        }

        private void AnalyzeIndividualPlayer(string playerId)
        {
            var analysis = _playerAnalysisService?.AnalyzePlayer(playerId);
            if (analysis != null)
            {
                var profile = _playerProfileService.GetOrCreatePlayerProfile(playerId);
                _playerProfileService.UpdatePlayerProfileFromAnalysis(profile, analysis);

                OnPlayerProfileUpdated?.Invoke(profile);
            }
        }

        private void UpdatePlayerContexts()
        {
            foreach (var playerId in _playerProfiles.Keys)
            {
                UpdatePlayerContext(playerId);
            }
        }

        private void UpdatePlayerContext(string playerId)
        {
            // Player context is now managed by PlayerProfileService
            // This method is kept for compatibility but functionality has been moved to services
        }

        private void MakeDirectorDecisions()
        {
            foreach (var playerId in _playerProfiles.Keys)
            {
                MakePlayerDecisions(playerId);
            }
        }

        private void MakePlayerDecisions(string playerId)
        {
            var profile = _playerProfiles[playerId];
            var context = _playerContexts[playerId];

            // Apply director rules to determine decisions
            var decisions = ApplyDirectorRules(profile, context);

            foreach (var decision in decisions)
            {
                ExecuteDirectorDecision(playerId, decision);
            }
        }

        private List<DirectorDecision> ApplyDirectorRules(PlayerProfile profile, DirectorContext context)
        {
            var decisions = new List<DirectorDecision>();

            foreach (var rule in _activeRules)
            {
                if (rule.IsApplicable(profile, context))
                {
                    var decision = rule.GenerateDecision(profile, context);
                    if (decision != null)
                    {
                        decisions.Add(decision);
                    }
                }
            }

            // Prioritize decisions based on importance
            decisions.Sort((a, b) => b.priority.CompareTo(a.priority));

            return decisions.Take(_config.maxDecisionsPerPlayer).ToList();
        }

        private void ExecuteDirectorDecision(string playerId, DirectorDecision decision)
        {
            _decisionExecutionService.ExecuteDirectorDecision(playerId, decision);
        }

        private void UpdateActiveStorylines()
        {
            for (int i = _activeStorylines.Count - 1; i >= 0; i--)
            {
                var storyline = _activeStorylines[i];

                if (storyline.isCompleted || storyline.hasExpired)
                {
                    _emergentStorytellingService?.CompleteStoryline(storyline);
                    _activeStorylines.RemoveAt(i);
                }
            }
        }

        private void GenerateEmergentContent()
        {
            if (_config.enableEmergentStoryGeneration)
            {
                var emergentStories = _emergentStorytellingService?.GenerateEmergentContent();
                if (emergentStories != null)
                {
                    foreach (var story in emergentStories)
                    {
                        OnEmergentStoryGenerated?.Invoke(story);
                    }
                }
            }
        }

        private void AdaptDifficultyAndContent()
        {
            // Player experience adaptation is now handled by individual services
            // (EducationalContentService, BehavioralAnalysisService, etc.)
        }

        #endregion

        #region Player Management

        /// <summary>
        /// Registers a new player with the AI Director
        /// </summary>
        public PlayerProfile RegisterPlayer(string playerId, PlayerRegistrationData registrationData)
        {
            var profile = new PlayerProfile
            {
                playerId = playerId,
                registrationDate = DateTime.Now,
                skillLevel = SkillLevel.Beginner,
                learningStyle = registrationData.learningStyle,
                interests = registrationData.interests,
                isEducationalContext = registrationData.isEducationalContext
            };

            _playerProfiles[playerId] = profile;
            _playerContexts[playerId] = new DirectorContext { playerId = playerId };

            // Initialize player analysis
            _playerAnalysisService?.InitializePlayer(playerId, registrationData);

            if (_config.enableDebugLogging)
                Debug.Log($"[AIDirectorSubsystem] Registered player: {playerId}");

            return profile;
        }

        /// <summary>
        /// Tracks a player action for AI analysis
        /// </summary>
        public void TrackPlayerAction(string playerId, string actionType, Dictionary<string, object> actionData = null)
        {
            _playerProfileService.TrackPlayerAction(playerId, actionType, actionData);
        }

        /// <summary>
        /// Reports a player discovery for narrative integration
        /// </summary>
        public void ReportDiscovery(string playerId, string discoveryType, object discoveryData = null)
        {
            _playerProfileService.ReportDiscovery(playerId, discoveryType, discoveryData);
        }

        /// <summary>
        /// Gets current player profile
        /// </summary>
        public PlayerProfile GetPlayerProfile(string playerId)
        {
            return _playerProfileService.GetPlayerProfile(playerId);
        }

        #endregion

        #region Lifecycle

        private void OnDestroy()
        {
            _isRunning = false;

            if (_directorLoopCoroutine != null)
            {
                StopCoroutine(_directorLoopCoroutine);
            }
        }

        #endregion
    }
}