using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using System.Linq;
using Laboratory.Core.Infrastructure;

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
        private float _globalDifficultyMultiplier = 1.0f;

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
                // Subscribe to genetics events for automatic player tracking
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete += HandleBreedingEvent;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred += HandleMutationEvent;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered += HandleTraitDiscoveryEvent;

                // Subscribe to research events for educational tracking
                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnDiscoveryLogged += HandleResearchDiscoveryEvent;
                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnPublicationCreated += HandlePublicationEvent;

                // Subscribe to ecosystem events for environmental awareness
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent += HandleEnvironmentalEvent;
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnPopulationChanged += HandlePopulationEvent;

                // Subscribe to analytics events for behavior patterns
                Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnPlayerActionTracked += HandlePlayerActionEvent;
                Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnEducationalProgressTracked += HandleEducationalProgressEvent;

                if (_config.enableDebugLogging)
                    Debug.Log("[AIDirector] Player tracking events initialized - fully integrated with Genetics, Research, Ecosystem, and Analytics subsystems");
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
                UpdatePlayerEngagement(playerId, directorEvent);
            }
        }

        private void ProcessDiscoveryEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var discoveryType = directorEvent.data.GetValueOrDefault("discoveryType", "unknown").ToString();

            // Update player discovery patterns
            _playerAnalysisService?.TrackDiscovery(playerId, discoveryType);

            // Check for narrative opportunities
            CheckDiscoveryNarrativeOpportunities(playerId, discoveryType);

            // Update global discovery state
            UpdateGlobalDiscoveryState(discoveryType);
        }

        private void ProcessAchievementEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var achievementType = directorEvent.data.GetValueOrDefault("achievementType", "unknown").ToString();

            // Celebrate achievement and check for story progression
            _narrativeGenerationService?.TriggerAchievementNarrative(playerId, achievementType);

            // Update player confidence and engagement
            UpdatePlayerConfidence(playerId, true);
        }

        private void ProcessCollaborationEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var collaborationType = directorEvent.data.GetValueOrDefault("collaborationType", "unknown").ToString();

            // Track collaboration patterns
            _playerAnalysisService?.TrackCollaboration(playerId, collaborationType);

            // Generate collaborative storylines
            GenerateCollaborativeStorylines(playerId, collaborationType);
        }

        private void ProcessStruggleEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var struggleType = directorEvent.data.GetValueOrDefault("struggleType", "unknown").ToString();

            // Provide educational scaffolding
            _educationalScaffoldingService?.ProvideSupport(playerId, struggleType);

            // Adjust difficulty if needed
            ConsiderDifficultyReduction(playerId);

            // Update player confidence
            UpdatePlayerConfidence(playerId, false);
        }

        private void ProcessMilestoneEvent(DirectorEvent directorEvent)
        {
            var playerId = directorEvent.playerId;
            var milestoneType = directorEvent.data.GetValueOrDefault("milestoneType", "unknown").ToString();

            // Generate milestone celebration
            _narrativeGenerationService?.GenerateMilestoneCelebration(playerId, milestoneType);

            // Update player progression state
            UpdatePlayerProgression(playerId, milestoneType);
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
                var profile = GetOrCreatePlayerProfile(playerId);
                UpdatePlayerProfileFromAnalysis(profile, analysis);

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
            var profile = _playerProfiles[playerId];
            var context = GetOrCreatePlayerContext(playerId);

            // Update context based on current player state
            context.engagement = CalculateEngagementScore(profile);
            context.skillLevel = CalculateSkillLevel(profile);
            context.progressRate = CalculateProgressRate(profile);
            context.socialActivity = CalculateSocialActivity(profile);
            context.timeInSession = CalculateSessionTime(profile);

            // Update context weights based on educational goals
            UpdateContextWeightsForPlayer(playerId, context);
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
            switch (decision.decisionType)
            {
                case DirectorDecisionType.AdjustDifficulty:
                    ExecuteDifficultyAdjustment(playerId, decision);
                    break;

                case DirectorDecisionType.TriggerNarrative:
                    ExecuteNarrativeTrigger(playerId, decision);
                    break;

                case DirectorDecisionType.SpawnContent:
                    ExecuteContentSpawn(playerId, decision);
                    break;

                case DirectorDecisionType.ProvideHint:
                    ExecuteHintProvision(playerId, decision);
                    break;

                case DirectorDecisionType.EncourageCollaboration:
                    ExecuteCollaborationEncouragement(playerId, decision);
                    break;

                case DirectorDecisionType.CelebrateAchievement:
                    ExecuteAchievementCelebration(playerId, decision);
                    break;
            }

            OnDirectorDecisionMade?.Invoke(decision);
        }

        private void UpdateActiveStorylines()
        {
            for (int i = _activeStorylines.Count - 1; i >= 0; i--)
            {
                var storyline = _activeStorylines[i];
                UpdateStoryline(storyline);

                if (storyline.isCompleted || storyline.hasExpired)
                {
                    CompleteStoryline(storyline);
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
            foreach (var playerId in _playerProfiles.Keys)
            {
                AdaptPlayerExperience(playerId);
            }
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
            var directorEvent = new DirectorEvent
            {
                eventType = DirectorEventType.PlayerAction,
                playerId = playerId,
                timestamp = DateTime.Now,
                data = actionData ?? new Dictionary<string, object>()
            };

            directorEvent.data["actionType"] = actionType;
            _eventQueue.Enqueue(directorEvent);
        }

        /// <summary>
        /// Reports a player discovery for narrative integration
        /// </summary>
        public void ReportDiscovery(string playerId, string discoveryType, object discoveryData = null)
        {
            var directorEvent = new DirectorEvent
            {
                eventType = DirectorEventType.Discovery,
                playerId = playerId,
                timestamp = DateTime.Now,
                data = new Dictionary<string, object>
                {
                    ["discoveryType"] = discoveryType,
                    ["discoveryData"] = discoveryData
                }
            };

            _eventQueue.Enqueue(directorEvent);
        }

        /// <summary>
        /// Gets current player profile
        /// </summary>
        public PlayerProfile GetPlayerProfile(string playerId)
        {
            _playerProfiles.TryGetValue(playerId, out var profile);
            return profile;
        }

        #endregion

        #region AI Decision Making

        private PlayerProfile GetOrCreatePlayerProfile(string playerId)
        {
            if (!_playerProfiles.ContainsKey(playerId))
            {
                _playerProfiles[playerId] = new PlayerProfile { playerId = playerId };
            }
            return _playerProfiles[playerId];
        }

        private DirectorContext GetOrCreatePlayerContext(string playerId)
        {
            if (!_playerContexts.ContainsKey(playerId))
            {
                _playerContexts[playerId] = new DirectorContext { playerId = playerId };
            }
            return _playerContexts[playerId];
        }

        private void UpdatePlayerProfileFromAnalysis(PlayerProfile profile, PlayerAnalysis analysis)
        {
            profile.skillLevel = analysis.estimatedSkillLevel;
            profile.engagementScore = analysis.engagementScore;
            profile.learningVelocity = analysis.learningVelocity;
            profile.preferredChallengeLevel = analysis.preferredChallengeLevel;
            profile.collaborationFrequency = analysis.collaborationFrequency;
            profile.lastAnalysisUpdate = DateTime.Now;
        }

        private float CalculateEngagementScore(PlayerProfile profile)
        {
            // Calculate based on recent activity, time spent, interactions
            var baseEngagement = profile.engagementScore;
            var timeFactor = CalculateTimeFactor(profile.lastActivity);
            return math.clamp(baseEngagement * timeFactor, 0f, 1f);
        }

        private float CalculateSkillLevel(PlayerProfile profile)
        {
            return (float)profile.skillLevel / (float)SkillLevel.Expert;
        }

        private float CalculateProgressRate(PlayerProfile profile)
        {
            return profile.learningVelocity;
        }

        private float CalculateSocialActivity(PlayerProfile profile)
        {
            return profile.collaborationFrequency;
        }

        private float CalculateSessionTime(PlayerProfile profile)
        {
            var sessionTime = DateTime.Now - profile.sessionStartTime;
            return (float)sessionTime.TotalMinutes;
        }

        private float CalculateTimeFactor(DateTime lastActivity)
        {
            var timeSinceActivity = DateTime.Now - lastActivity;
            var decayRate = _config.engagementDecayRate;
            return math.exp(-(float)timeSinceActivity.TotalMinutes * decayRate);
        }

        private void UpdateContextWeightsForPlayer(string playerId, DirectorContext context)
        {
            // Adjust context weights based on educational goals and player state
            if (context.engagement < 0.3f)
            {
                _contextWeights["engagement"] = 0.6f; // Prioritize engagement
            }
            else if (context.skillLevel > 0.8f)
            {
                _contextWeights["skill"] = 0.4f; // Focus on skill development
            }
        }

        #endregion

        #region Decision Execution

        private void ExecuteDifficultyAdjustment(string playerId, DirectorDecision decision)
        {
            var adjustment = new DifficultyAdjustment
            {
                playerId = playerId,
                adjustmentType = (DifficultyAdjustmentType)decision.parameters["adjustmentType"],
                magnitude = (float)decision.parameters["magnitude"],
                reason = decision.reasoning,
                timestamp = DateTime.Now
            };

            _difficultyAdaptationService?.ApplyDifficultyAdjustment(playerId, adjustment);
            OnDifficultyAdjusted?.Invoke(adjustment);
        }

        private void ExecuteNarrativeTrigger(string playerId, DirectorDecision decision)
        {
            var narrativeType = decision.parameters["narrativeType"].ToString();
            var narrativeEvent = _narrativeGenerationService?.GenerateNarrative(playerId, narrativeType);

            if (narrativeEvent != null)
            {
                OnNarrativeEventTriggered?.Invoke(narrativeEvent);
            }
        }

        private void ExecuteContentSpawn(string playerId, DirectorDecision decision)
        {
            var contentType = decision.parameters["contentType"].ToString();
            var spawnParameters = decision.parameters.GetValueOrDefault("spawnParameters", new Dictionary<string, object>());

            _contentOrchestrationService?.SpawnContent(playerId, contentType, spawnParameters);
        }

        private void ExecuteHintProvision(string playerId, DirectorDecision decision)
        {
            var hintType = decision.parameters["hintType"].ToString();
            var hintContent = decision.parameters["hintContent"].ToString();

            _educationalScaffoldingService?.ProvideHint(playerId, hintType, hintContent);
        }

        private void ExecuteCollaborationEncouragement(string playerId, DirectorDecision decision)
        {
            var collaborationType = decision.parameters["collaborationType"].ToString();
            var suggestedPartners = decision.parameters.GetValueOrDefault("suggestedPartners", new List<string>()) as List<string>;

            _contentOrchestrationService?.EncourageCollaboration(playerId, collaborationType, suggestedPartners);
        }

        private void ExecuteAchievementCelebration(string playerId, DirectorDecision decision)
        {
            var achievementType = decision.parameters["achievementType"].ToString();
            var celebrationType = decision.parameters["celebrationType"].ToString();

            _narrativeGenerationService?.CelebrateAchievement(playerId, achievementType, celebrationType);
        }

        #endregion

        #region Helper Methods

        private void UpdatePlayerEngagement(string playerId, DirectorEvent directorEvent)
        {
            var profile = GetOrCreatePlayerProfile(playerId);
            var engagementDelta = CalculateEngagementDelta(directorEvent);
            profile.engagementScore = math.clamp(profile.engagementScore + engagementDelta, 0f, 1f);
            profile.lastActivity = DateTime.Now;
        }

        private float CalculateEngagementDelta(DirectorEvent directorEvent)
        {
            var actionType = directorEvent.data.GetValueOrDefault("actionType", "unknown").ToString();

            return actionType switch
            {
                "discovery" => 0.1f,
                "collaboration" => 0.08f,
                "achievement" => 0.05f,
                "exploration" => 0.03f,
                _ => 0.01f
            };
        }

        private void CheckDiscoveryNarrativeOpportunities(string playerId, string discoveryType)
        {
            // Check if this discovery creates narrative opportunities
            if (_config.enableNarrativeGeneration)
            {
                var opportunities = _narrativeGenerationService?.AnalyzeDiscoveryOpportunities(playerId, discoveryType);
                if (opportunities != null && opportunities.Count > 0)
                {
                    foreach (var opportunity in opportunities)
                    {
                        CreateNarrativeStoryline(playerId, opportunity);
                    }
                }
            }
        }

        private void UpdateGlobalDiscoveryState(string discoveryType)
        {
            _currentState.globalDiscoveries[discoveryType] = _currentState.globalDiscoveries.GetValueOrDefault(discoveryType, 0) + 1;
        }

        private void UpdatePlayerConfidence(string playerId, bool positive)
        {
            var profile = GetOrCreatePlayerProfile(playerId);
            var confidenceDelta = positive ? 0.05f : -0.03f;
            profile.confidenceLevel = math.clamp(profile.confidenceLevel + confidenceDelta, 0f, 1f);
        }

        private void GenerateCollaborativeStorylines(string playerId, string collaborationType)
        {
            if (_config.enableCollaborativeStorylines)
            {
                var storylines = _emergentStorytellingService?.GenerateCollaborativeStorylines(playerId, collaborationType);
                if (storylines != null)
                {
                    _activeStorylines.AddRange(storylines);
                }
            }
        }

        private void ConsiderDifficultyReduction(string playerId)
        {
            var profile = GetOrCreatePlayerProfile(playerId);
            var context = GetOrCreatePlayerContext(playerId);

            if (context.engagement < 0.4f && profile.confidenceLevel < 0.3f)
            {
                var decision = new DirectorDecision
                {
                    decisionType = DirectorDecisionType.AdjustDifficulty,
                    playerId = playerId,
                    priority = DirectorPriority.High,
                    reasoning = "Player struggling, reducing difficulty to maintain engagement",
                    parameters = new Dictionary<string, object>
                    {
                        ["adjustmentType"] = DifficultyAdjustmentType.Reduce,
                        ["magnitude"] = 0.2f
                    }
                };

                ExecuteDirectorDecision(playerId, decision);
            }
        }

        private void UpdatePlayerProgression(string playerId, string milestoneType)
        {
            var profile = GetOrCreatePlayerProfile(playerId);
            profile.milestonesAchieved.Add(milestoneType);
            profile.progressionScore += CalculateMilestoneValue(milestoneType);
        }

        private float CalculateMilestoneValue(string milestoneType)
        {
            return milestoneType switch
            {
                "major_discovery" => 0.2f,
                "collaboration_success" => 0.15f,
                "skill_mastery" => 0.1f,
                "research_completion" => 0.08f,
                _ => 0.05f
            };
        }

        private void AdaptPlayerExperience(string playerId)
        {
            var profile = _playerProfiles[playerId];
            var context = _playerContexts[playerId];

            // Generate content adaptations based on player state
            var adaptations = _contentOrchestrationService?.GenerateAdaptations(profile, context);
            if (adaptations != null)
            {
                foreach (var adaptation in adaptations)
                {
                    OnContentAdapted?.Invoke(adaptation);
                }
            }
        }

        private void CreateNarrativeStoryline(string playerId, NarrativeOpportunity opportunity)
        {
            var storyline = new ActiveStoryline
            {
                storylineId = Guid.NewGuid().ToString(),
                playerId = playerId,
                narrativeType = opportunity.narrativeType,
                startTime = DateTime.Now,
                estimatedDuration = opportunity.estimatedDuration,
                isActive = true
            };

            _activeStorylines.Add(storyline);
        }

        private void UpdateStoryline(ActiveStoryline storyline)
        {
            var elapsed = DateTime.Now - storyline.startTime;
            storyline.progress = (float)(elapsed.TotalMinutes / storyline.estimatedDuration.TotalMinutes);

            if (storyline.progress >= 1.0f)
            {
                storyline.isCompleted = true;
            }
        }

        private void CompleteStoryline(ActiveStoryline storyline)
        {
            _emergentStorytellingService?.CompleteStoryline(storyline);

            if (_config.enableDebugLogging)
                Debug.Log($"[AIDirectorSubsystem] Completed storyline: {storyline.storylineId}");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Trigger Test Narrative")]
        private void DebugTriggerTestNarrative()
        {
            if (_playerProfiles.Count > 0)
            {
                var firstPlayer = _playerProfiles.Keys.First();
                ReportDiscovery(firstPlayer, "test_discovery", "Debug discovery");
            }
        }

        [ContextMenu("Log Player Profiles")]
        private void DebugLogPlayerProfiles()
        {
            foreach (var profile in _playerProfiles.Values)
            {
                Debug.Log($"[AIDirectorSubsystem] Player {profile.playerId}: Skill={profile.skillLevel}, Engagement={profile.engagementScore:F2}");
            }
        }

        [ContextMenu("Log Active Storylines")]
        private void DebugLogActiveStorylines()
        {
            Debug.Log($"[AIDirectorSubsystem] Active Storylines: {_activeStorylines.Count}");
            foreach (var storyline in _activeStorylines)
            {
                Debug.Log($"  {storyline.storylineId}: {storyline.narrativeType} ({storyline.progress:F1}%)");
            }
        }

        #endregion

        #region Cross-Subsystem Event Handlers

        private void HandleBreedingEvent(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            if (result?.isSuccessful == true)
            {
                TrackPlayerAction(result.playerId, "breeding_success", new Dictionary<string, object>
                {
                    ["parent1"] = result.parent1Id,
                    ["parent2"] = result.parent2Id,
                    ["offspring"] = result.offspringId,
                    ["mutationCount"] = result.mutations?.Count ?? 0,
                    ["difficulty"] = CalculateBreedingDifficulty(result)
                });

                // Provide positive reinforcement for successful breeding
                var profile = GetOrCreatePlayerProfile(result.playerId);
                UpdatePlayerConfidence(result.playerId, true);

                // Consider triggering celebration or educational content
                if (result.mutations?.Count > 0)
                {
                    ConsiderTriggeringMutationExplanation(result.playerId, result.mutations);
                }
            }
        }

        private void HandleMutationEvent(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent)
        {
            TrackPlayerAction(mutationEvent.playerId, "mutation_discovery", new Dictionary<string, object>
            {
                ["creatureId"] = mutationEvent.creatureId,
                ["mutationType"] = mutationEvent.mutation.mutationType.ToString(),
                ["severity"] = mutationEvent.mutation.severity,
                ["isWorldFirst"] = mutationEvent.isWorldFirst
            });

            // Generate educational content for rare mutations
            if (mutationEvent.mutation.severity > 0.7f || mutationEvent.isWorldFirst)
            {
                ConsiderTriggeringEducationalContent(mutationEvent.playerId, "rare_mutation", mutationEvent.mutation);
            }
        }

        private void HandleTraitDiscoveryEvent(Laboratory.Subsystems.Genetics.TraitDiscoveryEvent discoveryEvent)
        {
            TrackPlayerAction(discoveryEvent.playerId, "trait_discovery", new Dictionary<string, object>
            {
                ["traitName"] = discoveryEvent.traitName,
                ["generation"] = discoveryEvent.generation,
                ["isWorldFirst"] = discoveryEvent.isWorldFirst
            });

            // Celebrate world-first discoveries
            if (discoveryEvent.isWorldFirst)
            {
                TriggerCelebrationEvent(discoveryEvent.playerId, "world_first_trait", discoveryEvent.traitName);
            }
        }

        private void HandleResearchDiscoveryEvent(Laboratory.Subsystems.Research.DiscoveryJournalEvent discoveryEvent)
        {
            TrackPlayerAction(discoveryEvent.playerId, "research_discovery", new Dictionary<string, object>
            {
                ["discoveryType"] = discoveryEvent.discovery.discoveryType.ToString(),
                ["isSignificant"] = discoveryEvent.discovery.isSignificant
            });

            // Track research engagement patterns
            var profile = GetOrCreatePlayerProfile(discoveryEvent.playerId);
            profile.researchEngagement += discoveryEvent.discovery.isSignificant ? 0.1f : 0.05f;
        }

        private void HandlePublicationEvent(Laboratory.Subsystems.Research.PublicationEvent publicationEvent)
        {
            TrackPlayerAction(publicationEvent.publication.authorId, "publication_created", new Dictionary<string, object>
            {
                ["publicationType"] = publicationEvent.publication.publicationType.ToString(),
                ["collaborators"] = publicationEvent.publication.coAuthors?.Count ?? 0
            });

            // Encourage collaborative research
            if (publicationEvent.publication.coAuthors?.Count > 0)
            {
                EncourageCollaborativeResearch(publicationEvent.publication.authorId);
            }
        }

        private void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent envEvent)
        {
            // Track environmental awareness
            foreach (var playerId in GetActivePlayerIds())
            {
                if (IsPlayerInAffectedArea(playerId, envEvent.affectedArea))
                {
                    TrackPlayerAction(playerId, "environmental_exposure", new Dictionary<string, object>
                    {
                        ["eventType"] = envEvent.eventType.ToString(),
                        ["severity"] = envEvent.severity
                    });

                    // Provide environmental education if needed
                    ConsiderEnvironmentalEducation(playerId, envEvent);
                }
            }
        }

        private void HandlePopulationEvent(Laboratory.Subsystems.Ecosystem.PopulationEvent populationEvent)
        {
            // Track ecosystem management engagement
            TrackPlayerAction("EcosystemManager", "population_change", new Dictionary<string, object>
            {
                ["changeType"] = populationEvent.changeType.ToString(),
                ["populationId"] = populationEvent.populationId
            });

            // Update global ecosystem state for all players
            foreach (var playerId in GetActivePlayerIds())
            {
                var profile = GetOrCreatePlayerProfile(playerId);
                UpdatePlayerEcosystemAwareness(playerId, populationEvent);
            }
        }

        private void HandlePlayerActionEvent(Laboratory.Subsystems.Analytics.PlayerActionEvent actionEvent)
        {
            // Integrate analytics data into AI Director decisions
            var profile = GetOrCreatePlayerProfile(actionEvent.playerId);
            AnalyzePlayerActionPattern(profile, actionEvent);

            // Detect if player needs guidance
            if (DetectPlayerStruggle(profile, actionEvent))
            {
                ConsiderProvidingGuidance(actionEvent.playerId, actionEvent.actionType);
            }
        }

        private void HandleEducationalProgressEvent(Laboratory.Subsystems.Analytics.EducationalProgressEvent progressEvent)
        {
            TrackPlayerAction(progressEvent.playerId, "educational_progress", new Dictionary<string, object>
            {
                ["progressType"] = progressEvent.progressType,
                ["skillLevel"] = progressEvent.newSkillLevel
            });

            // Adapt content difficulty based on educational progress
            AdaptContentDifficulty(progressEvent.playerId, progressEvent.newSkillLevel);
        }

        #endregion

        #region AI Director Decision Helpers

        private float CalculateBreedingDifficulty(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            var difficulty = 0.5f; // Base difficulty

            // Increase difficulty based on mutation count
            if (result.mutations?.Count > 0)
                difficulty += result.mutations.Count * 0.1f;

            // Factor in genetic complexity
            if (result.offspring?.Genes?.Count > 10)
                difficulty += 0.2f;

            return Mathf.Clamp(difficulty, 0.1f, 1.0f);
        }

        private void ConsiderTriggeringMutationExplanation(string playerId, List<Laboratory.Subsystems.Genetics.GeneticMutation> mutations)
        {
            var profile = GetOrCreatePlayerProfile(playerId);

            // Only provide explanations for players who might benefit
            if (profile.skillLevel <= SkillLevel.Researcher)
            {
                foreach (var mutation in mutations)
                {
                    if (mutation.severity > 0.5f)
                    {
                        TriggerEducationalContent(playerId, "mutation_explanation", mutation);
                    }
                }
            }
        }

        private void ConsiderTriggeringEducationalContent(string playerId, string contentType, object contentData)
        {
            var profile = GetOrCreatePlayerProfile(playerId);

            // Check if player is in educational context
            if (profile.isEducationalContext && profile.engagementScore > 0.6f)
            {
                TriggerEducationalContent(playerId, contentType, contentData);
            }
        }

        private void TriggerCelebrationEvent(string playerId, string celebrationType, string achievementData)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.CelebrateAchievement,
                playerId = playerId,
                priority = DirectorPriority.High,
                reasoning = $"Celebrating {celebrationType}: {achievementData}",
                parameters = new Dictionary<string, object>
                {
                    ["achievementType"] = celebrationType,
                    ["celebrationType"] = "world_first",
                    ["achievementData"] = achievementData
                }
            };

            ExecuteDirectorDecision(playerId, decision);
        }

        private void EncourageCollaborativeResearch(string playerId)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.EncourageCollaboration,
                playerId = playerId,
                priority = DirectorPriority.Medium,
                reasoning = "Encouraging continued collaborative research",
                parameters = new Dictionary<string, object>
                {
                    ["collaborationType"] = "research",
                    ["suggestedPartners"] = GetPotentialCollaborators(playerId)
                }
            };

            ExecuteDirectorDecision(playerId, decision);
        }

        private bool IsPlayerInAffectedArea(string playerId, object affectedArea)
        {
            // Placeholder - would check player's current location against affected area
            return true; // Assume all players are affected for now
        }

        private void ConsiderEnvironmentalEducation(string playerId, Laboratory.Subsystems.Ecosystem.EnvironmentalEvent envEvent)
        {
            var profile = GetOrCreatePlayerProfile(playerId);

            if (profile.isEducationalContext && envEvent.severity > 0.7f)
            {
                TriggerEducationalContent(playerId, "environmental_impact", envEvent);
            }
        }

        private void AnalyzePlayerActionPattern(PlayerProfile profile, Laboratory.Subsystems.Analytics.PlayerActionEvent actionEvent)
        {
            // Track action frequency and patterns
            if (!profile.actionPatterns.ContainsKey(actionEvent.actionType))
            {
                profile.actionPatterns[actionEvent.actionType] = 0;
            }
            profile.actionPatterns[actionEvent.actionType]++;

            // Update engagement based on action type
            var engagementDelta = actionEvent.actionType switch
            {
                "breeding_attempt" => 0.05f,
                "discovery_made" => 0.1f,
                "collaboration_start" => 0.08f,
                "research_published" => 0.15f,
                _ => 0.01f
            };

            profile.engagementScore = Mathf.Clamp(profile.engagementScore + engagementDelta, 0f, 1f);
        }

        private bool DetectPlayerStruggle(PlayerProfile profile, Laboratory.Subsystems.Analytics.PlayerActionEvent actionEvent)
        {
            // Detect struggle patterns
            if (actionEvent.actionType == "breeding_failure" &&
                profile.actionPatterns.GetValueOrDefault("breeding_failure", 0) > 3)
            {
                return true;
            }

            if (profile.engagementScore < 0.3f && profile.confidenceLevel < 0.4f)
            {
                return true;
            }

            return false;
        }

        private void ConsiderProvidingGuidance(string playerId, string strugglingAction)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.ProvideHint,
                playerId = playerId,
                priority = DirectorPriority.High,
                reasoning = $"Player struggling with {strugglingAction}",
                parameters = new Dictionary<string, object>
                {
                    ["hintType"] = "guidance",
                    ["hintContent"] = GenerateGuidanceForAction(strugglingAction)
                }
            };

            ExecuteDirectorDecision(playerId, decision);
        }

        private void AdaptContentDifficulty(string playerId, float newSkillLevel)
        {
            var profile = GetOrCreatePlayerProfile(playerId);
            var oldSkillLevel = (float)profile.skillLevel / (float)SkillLevel.Expert;

            // If skill level increased significantly, increase content difficulty
            if (newSkillLevel > oldSkillLevel + 0.2f)
            {
                var decision = new DirectorDecision
                {
                    decisionType = DirectorDecisionType.AdjustDifficulty,
                    playerId = playerId,
                    priority = DirectorPriority.Medium,
                    reasoning = "Adapting to improved skill level",
                    parameters = new Dictionary<string, object>
                    {
                        ["adjustmentType"] = DifficultyAdjustmentType.Increase,
                        ["magnitude"] = 0.1f
                    }
                };

                ExecuteDirectorDecision(playerId, decision);
            }
        }

        private void TriggerEducationalContent(string playerId, string contentType, object contentData)
        {
            var decision = new DirectorDecision
            {
                decisionType = DirectorDecisionType.TriggerNarrative,
                playerId = playerId,
                priority = DirectorPriority.Medium,
                reasoning = $"Providing educational content: {contentType}",
                parameters = new Dictionary<string, object>
                {
                    ["narrativeType"] = "educational",
                    ["contentType"] = contentType,
                    ["contentData"] = contentData
                }
            };

            ExecuteDirectorDecision(playerId, decision);
        }

        private List<string> GetPotentialCollaborators(string playerId)
        {
            var collaborators = new List<string>();

            // Find players with complementary skills or research interests
            foreach (var otherProfile in _playerProfiles.Values)
            {
                if (otherProfile.playerId != playerId &&
                    otherProfile.isEducationalContext &&
                    Math.Abs((int)otherProfile.skillLevel - (int)_playerProfiles[playerId].skillLevel) <= 1)
                {
                    collaborators.Add(otherProfile.playerId);
                }
            }

            return collaborators.Take(3).ToList(); // Suggest up to 3 collaborators
        }

        private string GenerateGuidanceForAction(string strugglingAction)
        {
            return strugglingAction switch
            {
                "breeding_failure" => "Try selecting creatures with more compatible genetic traits, or experiment with different breeding combinations.",
                "research_difficulty" => "Consider collaborating with other researchers or focusing on smaller, more manageable research questions.",
                "ecosystem_management" => "Monitor population dynamics more closely and consider environmental factors affecting creature survival.",
                _ => "Take your time to explore different approaches. Don't hesitate to experiment and learn from each attempt."
            };
        }

        private List<string> GetActivePlayerIds()
        {
            return _playerProfiles.Keys.ToList();
        }

        private void UpdatePlayerEcosystemAwareness(string playerId, Laboratory.Subsystems.Ecosystem.PopulationEvent populationEvent)
        {
            var profile = GetOrCreatePlayerProfile(playerId);

            // Update player's ecosystem awareness based on population changes
            if (populationEvent.changeType.ToString().Contains("Decline") || populationEvent.changeType.ToString().Contains("Extinction"))
            {
                // Increase environmental concern for negative changes
                profile.environmentalConcern = math.clamp(profile.environmentalConcern + 0.1f, 0f, 1f);

                // Consider providing environmental education
                if (profile.isEducationalContext && profile.environmentalConcern > 0.7f)
                {
                    var decision = new DirectorDecision
                    {
                        decisionType = DirectorDecisionType.TriggerNarrative,
                        playerId = playerId,
                        priority = DirectorPriority.Medium,
                        reasoning = "Player needs environmental awareness due to population decline",
                        parameters = new Dictionary<string, object>
                        {
                            ["narrativeType"] = "environmental_education",
                            ["populationEvent"] = populationEvent
                        }
                    };
                    ExecuteDirectorDecision(playerId, decision);
                }
            }
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