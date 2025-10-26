using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Systems.Quests;
using Laboratory.Systems.Breeding;
using Laboratory.Systems.Ecosystem;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.Core.Enums;

namespace Laboratory.Systems.Analytics
{
    /// <summary>
    /// Comprehensive player analytics and behavior tracking system that monitors
    /// player actions, preferences, and patterns to provide insights and adapt
    /// the game experience dynamically based on individual play styles.
    /// </summary>
    public class PlayerAnalyticsTracker : MonoBehaviour
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool enableAnalyticsTracking = true;
        [SerializeField] private bool enableRealTimeAnalysis = true;
        [SerializeField] private float analyticsUpdateInterval = 5f;
        [SerializeField] private int maxSessionHistoryLength = 100;

        [Header("Data Collection Settings")]
        [SerializeField] private bool trackInputPatterns = true;
        [SerializeField] private bool trackUIInteractions = true;
        [SerializeField] private bool trackGameplayChoices = true;
        [SerializeField] private bool trackPerformanceMetrics = true;
        [SerializeField] private bool trackEmotionalResponse = true;

        [Header("Privacy Settings")]
        [SerializeField] private bool anonymizeData = true;
        [SerializeField] private bool localStorageOnly = true;
        [SerializeField] private int dataRetentionDays = 30;

        [Header("Adaptation Settings")]
        [SerializeField] private bool enableDynamicAdaptation = true;
        [SerializeField, Range(0f, 1f)] private float adaptationSensitivity = 0.5f;
        [SerializeField] private float adaptationCooldown = 60f;

        [Header("Behavioral Analysis")]
        [SerializeField] private PlayerArchetype[] playerArchetypes;
        [SerializeField] private BehaviorPattern[] behaviorPatterns;
        [SerializeField] private EngagementMetric[] engagementMetrics;

        // Core analytics data - PERFORMANCE OPTIMIZED WITH ENUMS
        private PlayerProfile currentPlayerProfile;
        private List<GameplaySession> sessionHistory = new List<GameplaySession>();
        private Dictionary<ActionType, ActionMetrics> actionMetrics = new Dictionary<ActionType, ActionMetrics>();
        private Dictionary<TraitType, float> behaviorScores = new Dictionary<TraitType, float>();

        // Real-time tracking
        private GameplaySession currentSession;
        private List<PlayerAction> currentSessionActions = new List<PlayerAction>();
        private float sessionStartTime;
        private float lastAnalyticsUpdate;
        private float lastAdaptation;
        private bool analyticsInitialized = false;
        private float currentEngagement = 0.5f;

        // Input tracking
        private InputActionAsset playerInput;
        private Dictionary<ActionType, InputMetrics> inputMetrics = new Dictionary<ActionType, InputMetrics>();

        // Behavioral analysis
        private PlayerBehaviorAnalyzer behaviorAnalyzer;
        private EngagementAnalyzer engagementAnalyzer;
        private PreferenceAnalyzer preferenceAnalyzer;

        // Events
        public System.Action<PlayerProfile> OnPlayerProfileUpdated;
        public System.Action<PlayerArchetype> OnPlayerArchetypeIdentified;
        public System.Action<GameplaySession> OnSessionCompleted;
        public System.Action<BehaviorInsight> OnBehaviorInsightGenerated;
        public System.Action<PlayerAdaptation> OnGameAdaptationTriggered;

        // Singleton access
        private static PlayerAnalyticsTracker instance;
        public static PlayerAnalyticsTracker Instance => instance;

        public PlayerProfile CurrentProfile => currentPlayerProfile;
        public GameplaySession CurrentSession => currentSession;
        public bool IsTrackingActive => enableAnalyticsTracking && currentSession != null;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAnalytics();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartNewSession();
            SetupInputTracking();
            ConnectToGameSystems();
        }

        private void OnDestroy()
        {
            DisposeAnalytics();
        }

        private void DisposeAnalytics()
        {
            analyticsInitialized = false;
        }

        private void Update()
        {
            if (!enableAnalyticsTracking) return;

            UpdateCurrentSession();

            // Periodic analytics processing
            if (Time.time - lastAnalyticsUpdate >= analyticsUpdateInterval)
            {
                ProcessAnalyticsData();
                lastAnalyticsUpdate = Time.time;
            }

            // Dynamic adaptation check
            if (enableDynamicAdaptation && Time.time - lastAdaptation >= adaptationCooldown)
            {
                CheckForAdaptationOpportunities();
            }
        }

        private void InitializeAnalytics()
        {
            Debug.Log("Initializing Player Analytics Tracker - Performance Optimized");

            // Initialize performance-optimized data structures
            if (!analyticsInitialized)
            {
                // Initialize enum-based dictionaries for performance
                foreach (ActionType actionType in System.Enum.GetValues(typeof(ActionType)))
                {
                    actionMetrics[actionType] = new ActionMetrics();
                }

                foreach (TraitType traitType in System.Enum.GetValues(typeof(TraitType)))
                {
                    behaviorScores[traitType] = 0.5f;
                }

                analyticsInitialized = true;
            }

            // Initialize player profile
            currentPlayerProfile = LoadOrCreatePlayerProfile();

            // Initialize analyzers
            behaviorAnalyzer = new PlayerBehaviorAnalyzer();
            engagementAnalyzer = new EngagementAnalyzer();
            preferenceAnalyzer = new PreferenceAnalyzer();

            // Load historical data
            LoadSessionHistory();

            Debug.Log($"Player Analytics initialized for player: {currentPlayerProfile.playerId}");
        }

        /// <summary>
        /// Tracks a player action with contextual data - PERFORMANCE OPTIMIZED with enum
        /// </summary>
        public void TrackAction(ActionType actionType, Dictionary<ParamKey, object> parameters = null)
        {
            if (!enableAnalyticsTracking) return;

            var action = new PlayerAction
            {
                actionType = actionType.ToString(),
                timestamp = Time.time,
                sessionTime = Time.time - sessionStartTime,
                parameters = parameters ?? new Dictionary<ParamKey, object>(),
                context = CaptureCurrentContext()
            };

            currentSessionActions.Add(action);

            // Update optimized action metrics
            UpdateActionMetrics(actionType);

            // Real-time analysis
            if (enableRealTimeAnalysis)
            {
                AnalyzeActionInRealTime(action);
            }

            Debug.Log($"Tracked action: {actionType} (Session actions: {currentSessionActions.Count})");
        }

        /// <summary>
        /// Update action metrics using optimized enum-based dictionary
        /// </summary>
        private void UpdateActionMetrics(ActionType actionID)
        {
            if (!analyticsInitialized) return;

            if (actionMetrics.ContainsKey(actionID))
            {
                var metrics = actionMetrics[actionID];
                metrics.totalCount++;
                metrics.lastUsed = Time.time;
                actionMetrics[actionID] = metrics;
            }
            else
            {
                actionMetrics[actionID] = new ActionMetrics { totalCount = 1, lastUsed = Time.time };
            }
        }

        /// <summary>
        /// Tracks UI interaction with detailed metrics
        /// </summary>
        public void TrackUIInteraction(string elementName, string interactionType, float interactionTime = 0f)
        {
            if (!trackUIInteractions) return;

            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.ElementName] = elementName,
                [ParamKey.InteractionType] = interactionType,
                [ParamKey.InteractionTime] = interactionTime
            };

            TrackAction(ActionType.UI, parameters);

            // Update UI-specific metrics
            UpdateUIMetrics(elementName, interactionType, interactionTime);
        }

        /// <summary>
        /// Tracks gameplay choice with decision context
        /// </summary>
        public void TrackGameplayChoice(ChoiceCategory choiceCategory, string choiceValue, Dictionary<ParamKey, object> decisionContext = null)
        {
            if (!trackGameplayChoices) return;

            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.ChoiceCategory] = choiceCategory,
                [ParamKey.ChoiceValue] = choiceValue,
                [ParamKey.DecisionContext] = decisionContext ?? new Dictionary<ParamKey, object>()
            };

            TrackAction(ActionType.Social, parameters);

            // Update choice preferences
            UpdateChoicePreferences(choiceCategory, choiceValue);
        }

        /// <summary>
        /// Tracks emotional response indicators
        /// </summary>
        public void TrackEmotionalResponse(EmotionalState emotionalState, float intensity, string trigger = "")
        {
            if (!trackEmotionalResponse) return;

            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.EmotionalState] = emotionalState.ToString(),
                [ParamKey.Intensity] = intensity.ToString(),
                [ParamKey.Trigger] = trigger
            };

            TrackAction(ActionType.Social, parameters);

            // Update emotional profile
            UpdateEmotionalProfile(emotionalState, intensity);
        }

        // ===== CHIMERA SYSTEM INTEGRATION METHODS =====

        /// <summary>
        /// Tracks creature breeding activities and outcomes
        /// </summary>
        public void TrackBreedingAction(BreedingType breedingType, string parentSpecies1, string parentSpecies2, bool success, Dictionary<ParamKey, object> additionalData = null)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.BreedingType] = breedingType,
                [ParamKey.ParentSpecies1] = parentSpecies1,
                [ParamKey.ParentSpecies2] = parentSpecies2,
                [ParamKey.Success] = success
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    parameters[kvp.Key] = kvp.Value;
                }
            }

            TrackAction(ActionType.Breeding, parameters);
        }

        /// <summary>
        /// Tracks creature exploration and discovery
        /// </summary>
        public void TrackCreatureInteraction(string creatureSpecies, string interactionType, string biome, bool firstDiscovery = false)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.Species] = creatureSpecies,
                [ParamKey.InteractionType] = interactionType,
                [ParamKey.Biome] = biome,
                [ParamKey.FirstDiscovery] = firstDiscovery
            };

            TrackAction(ActionType.Social, parameters);
        }

        /// <summary>
        /// Tracks ecosystem exploration and biome preferences
        /// </summary>
        public void TrackBiomeExploration(string biomeType, float timeSpent, int creaturesDiscovered, int resourcesGathered)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.Biome] = biomeType,
                [ParamKey.TimeSpent] = timeSpent,
                [ParamKey.CreaturesDiscovered] = creaturesDiscovered,
                [ParamKey.ResourcesGathered] = resourcesGathered
            };

            TrackAction(ActionType.Exploration, parameters);
        }

        /// <summary>
        /// Tracks genetic research and experimentation
        /// </summary>
        public void TrackGeneticResearch(string researchType, string targetTrait, bool breakthrough, float researchTime)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.ResearchType] = researchType,
                [ParamKey.TargetTrait] = targetTrait,
                [ParamKey.Breakthrough] = breakthrough,
                [ParamKey.ResearchTime] = researchTime
            };

            TrackAction(ActionType.Research, parameters);
        }

        /// <summary>
        /// Tracks quest completion and preferences
        /// </summary>
        public void TrackQuestProgress(string questType, string questId, float progress, bool completed)
        {
            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.QuestType] = questType,
                [ParamKey.QuestId] = questId,
                [ParamKey.Progress] = progress,
                [ParamKey.Completed] = completed
            };

            TrackAction(ActionType.Quest, parameters);
        }

        /// <summary>
        /// Gets personalized recommendations for the player
        /// </summary>
        public List<string> GetPersonalizedRecommendations()
        {
            return GenerateGameplayRecommendations();
        }

        /// <summary>
        /// Gets the current player engagement level (0-1)
        /// </summary>
        public float GetCurrentEngagementLevel()
        {
            return engagementAnalyzer.CalculateCurrentEngagement();
        }

        /// <summary>
        /// Gets player's preferred biomes based on analytics
        /// </summary>
        public List<string> GetPreferredBiomes()
        {
            var biomeActions = currentSessionActions.Where(a => a.actionType == "Chimera_Exploration");
            var biomePreferences = new Dictionary<BiomeType, float>();

            foreach (var action in biomeActions)
            {
                if (action.parameters.ContainsKey("biomeType"))
                {
                    string biome = action.parameters["biomeType"].ToString();
                    float timeSpent = action.parameters.ContainsKey("timeSpent") ?
                        float.Parse(action.parameters["timeSpent"].ToString()) : 1f;

                    if (!biomePreferences.ContainsKey(biome))
                        biomePreferences[biome] = 0f;

                    biomePreferences[biome] += timeSpent;
                }
            }

            return biomePreferences.OrderByDescending(kvp => kvp.Value)
                                  .Take(3)
                                  .Select(kvp => kvp.Key)
                                  .ToList();
        }

        /// <summary>
        /// Gets player's preferred creature types based on interactions
        /// </summary>
        public List<string> GetPreferredCreatureTypes()
        {
            var creatureActions = currentSessionActions.Where(a => a.actionType == "Chimera_Interaction" || a.actionType == "Chimera_Breeding");
            var speciesPreferences = new Dictionary<SystemType, int>();

            foreach (var action in creatureActions)
            {
                if (action.parameters.ContainsKey("species"))
                {
                    string species = action.parameters["species"].ToString();
                    speciesPreferences[species] = speciesPreferences.GetValueOrDefault(species, 0) + 1;
                }
                if (action.parameters.ContainsKey("parentSpecies1"))
                {
                    string species = action.parameters["parentSpecies1"].ToString();
                    speciesPreferences[species] = speciesPreferences.GetValueOrDefault(species, 0) + 1;
                }
                if (action.parameters.ContainsKey("parentSpecies2"))
                {
                    string species = action.parameters["parentSpecies2"].ToString();
                    speciesPreferences[species] = speciesPreferences.GetValueOrDefault(species, 0) + 1;
                }
            }

            return speciesPreferences.OrderByDescending(kvp => kvp.Value)
                                    .Take(5)
                                    .Select(kvp => kvp.Key)
                                    .ToList();
        }

        /// <summary>
        /// Gets player behavior analysis with insights
        /// </summary>
        public PlayerBehaviorAnalysis GetBehaviorAnalysis()
        {
            var analysis = new PlayerBehaviorAnalysis
            {
                playerId = currentPlayerProfile.playerId,
                analysisTime = Time.time,
                dominantArchetype = DetermineDominantArchetype(),
                behaviorScores = ConvertBehaviorScoresToEnumDict(),
                playStyle = AnalyzePlayStyle(),
                engagementLevel = engagementAnalyzer.CalculateCurrentEngagement(),
                preferences = preferenceAnalyzer.GetCurrentPreferences(),
                insights = GenerateBehaviorInsights(),
                recommendations = GenerateGameplayRecommendations()
            };

            return analysis;
        }

        /// <summary>
        /// Gets detailed session analytics
        /// </summary>
        public SessionAnalytics GetSessionAnalytics()
        {
            if (currentSession == null) return null;

            var analytics = new SessionAnalytics
            {
                sessionId = currentSession.sessionId,
                sessionDuration = Time.time - sessionStartTime,
                totalActions = currentSessionActions.Count,
                actionsPerMinute = CalculateActionsPerMinute(),
                dominantActivities = GetDominantActivities(),
                engagementMetrics = CalculateEngagementMetrics(),
                performanceMetrics = CalculatePerformanceMetrics(),
                emotionalJourney = AnalyzeEmotionalJourney(),
                keyMoments = IdentifyKeyMoments()
            };

            return analytics;
        }

        /// <summary>
        /// Triggers game adaptation based on player behavior
        /// </summary>
        public PlayerAdaptation TriggerGameAdaptation(AdaptationType adaptationType, float intensity = 1f)
        {
            if (!enableDynamicAdaptation) return null;

            var adaptation = new PlayerAdaptation
            {
                adaptationType = adaptationType,
                intensity = intensity,
                timestamp = Time.time,
                reason = DetermineAdaptationReason(),
                playerArchetype = DetermineDominantArchetype(),
                expectedImpact = CalculateExpectedImpact(adaptationType, intensity)
            };

            ApplyGameAdaptation(adaptation);
            lastAdaptation = Time.time;

            OnGameAdaptationTriggered?.Invoke(adaptation);

            Debug.Log($"Game adaptation triggered: {adaptationType} (Intensity: {intensity:F2})");

            return adaptation;
        }

        private void StartNewSession()
        {
            currentSession = new GameplaySession
            {
                sessionId = System.Guid.NewGuid().ToString(),
                startTime = Time.time,
                playerProfile = currentPlayerProfile
            };

            currentSessionActions.Clear();
            sessionStartTime = Time.time;

            Debug.Log($"Started new gameplay session: {currentSession.sessionId}");
        }

        private void UpdateCurrentSession()
        {
            if (currentSession == null) return;

            currentSession.duration = Time.time - sessionStartTime;
            currentSession.actionCount = currentSessionActions.Count;
            currentSession.lastActivityTime = Time.time;

            // Update session metrics
            UpdateSessionMetrics();
        }

        private void ProcessAnalyticsData()
        {
            if (currentSessionActions.Count == 0) return;

            // Analyze recent actions
            var recentActions = GetRecentActions(analyticsUpdateInterval);

            // Update behavior scores
            UpdateBehaviorScoresFromActions(recentActions);

            // Update player profile
            UpdatePlayerProfile();

            // Generate insights
            if (enableRealTimeAnalysis)
            {
                GenerateRealTimeInsights();
            }
        }

        private void CheckForAdaptationOpportunities()
        {
            var analysis = GetBehaviorAnalysis();

            // Check for engagement drops
            if (analysis.engagementLevel < 0.3f)
            {
                TriggerGameAdaptation(AdaptationType.EngagementBoost, 0.7f);
                return;
            }

            // Check for difficulty mismatches
            var performanceMetrics = CalculatePerformanceMetrics();
            if (performanceMetrics.successRate > 0.9f)
            {
                TriggerGameAdaptation(AdaptationType.DifficultyIncrease, 0.5f);
            }
            else if (performanceMetrics.successRate < 0.3f)
            {
                TriggerGameAdaptation(AdaptationType.DifficultyDecrease, 0.5f);
            }

            // Check for content preferences
            var preferences = analysis.preferences;
            if (preferences.ContainsKey("explorationPreference") && preferences["explorationPreference"] > 0.8f)
            {
                TriggerGameAdaptation(AdaptationType.ContentFocus, 0.6f);
            }
        }

        private void SetupInputTracking()
        {
            if (!trackInputPatterns) return;

            // This would integrate with Unity's Input System
            // For now, we'll set up basic input metric tracking
            InitializeInputMetrics();
        }

        private void ConnectToGameSystems()
        {
            // Connect to quest system
            if (ProceduralQuestGenerator.Instance != null)
            {
                ProceduralQuestGenerator.Instance.OnQuestCompleted += HandleQuestCompleted;
                ProceduralQuestGenerator.Instance.OnQuestGenerated += HandleQuestGenerated;
            }

            // Connect to breeding system
            if (AdvancedBreedingSimulator.Instance != null)
            {
                AdvancedBreedingSimulator.Instance.OnBreedingCompleted += HandleBreedingCompleted;
                AdvancedBreedingSimulator.Instance.OnOffspringAccepted += HandleOffspringAccepted;
            }

            // Connect to ecosystem system
            if (DynamicEcosystemSimulator.Instance != null)
            {
                DynamicEcosystemSimulator.Instance.OnEnvironmentalEventStarted += HandleEnvironmentalEvent;
            }
        }

        private void HandleQuestCompleted(ProceduralQuest quest)
        {
            TrackGameplayChoice(ChoiceCategory.QuestCompletion, quest.type.ToString(), new Dictionary<ParamKey, object>
            {
                ["questDifficulty"] = quest.difficulty,
                ["completionTime"] = Time.time - quest.generatedTime
            });

            // Track emotional response to quest completion
            TrackEmotionalResponse(EmotionalState.Satisfaction, 0.7f, "QuestComplete");
        }

        private void HandleQuestGenerated(ProceduralQuest quest)
        {
            TrackAction(ActionType.Quest, new Dictionary<ParamKey, object>
            {
                ["questType"] = quest.type.ToString(),
                ["questDifficulty"] = quest.difficulty
            });
        }

        private void HandleBreedingCompleted(BreedingSession session, CreatureGenome offspring)
        {
            TrackGameplayChoice(ChoiceCategory.BreedingChoice, "Completed", new Dictionary<ParamKey, object>
            {
                ["parentAFitness"] = session.parentA.fitness,
                ["parentBFitness"] = session.parentB.fitness,
                ["offspringFitness"] = offspring.fitness,
                ["breedingTime"] = session.completionTime - session.startTime
            });

            // Determine emotional response based on breeding success
            bool exceeded = offspring.fitness > Mathf.Max(session.parentA.fitness, session.parentB.fitness);
            TrackEmotionalResponse(
                exceeded ? EmotionalState.Excitement : EmotionalState.Curiosity,
                exceeded ? 0.8f : 0.5f,
                "BreedingResult"
            );
        }

        private void HandleOffspringAccepted(CreatureGenome offspring)
        {
            TrackGameplayChoice(ChoiceCategory.OffspringDecision, "Accepted", new Dictionary<ParamKey, object>
            {
                ["offspringFitness"] = offspring.fitness,
                ["offspringGeneration"] = offspring.generation
            });
        }

        private void HandleEnvironmentalEvent(EnvironmentalEvent envEvent)
        {
            TrackAction(ActionType.Exploration, new Dictionary<ParamKey, object>
            {
                ["eventType"] = envEvent.eventType.ToString()
            });

            // Environmental events might cause stress or excitement
            TrackEmotionalResponse(EmotionalState.Anticipation, 0.6f, "EnvironmentalEvent");
        }

        // Additional helper methods for analytics processing...
        // (Implementation continues with detailed analysis algorithms)

        private void OnDestroy()
        {
            if (instance == this)
            {
                // Save session data before destroying
                SaveCurrentSession();
                instance = null;
            }
        }

        // Editor menu items
#if UNITY_EDITOR
        [UnityEditor.MenuItem("🧪 Laboratory/Analytics/Show Player Behavior Analysis", false, 500)]
        private static void MenuShowBehaviorAnalysis()
        {
            if (Application.isPlaying && Instance != null)
            {
                var analysis = Instance.GetBehaviorAnalysis();
                Debug.Log($"Player Behavior Analysis:\n" +
                         $"Dominant Archetype: {analysis.dominantArchetype}\n" +
                         $"Play Style: {analysis.playStyle}\n" +
                         $"Engagement Level: {analysis.engagementLevel:F2}\n" +
                         $"Active Insights: {analysis.insights.Count}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Analytics/Show Session Analytics", false, 501)]
        private static void MenuShowSessionAnalytics()
        {
            if (Application.isPlaying && Instance != null)
            {
                var analytics = Instance.GetSessionAnalytics();
                if (analytics != null)
                {
                    Debug.Log($"Session Analytics:\n" +
                             $"Duration: {analytics.sessionDuration:F1}s\n" +
                             $"Total Actions: {analytics.totalActions}\n" +
                             $"Actions/Min: {analytics.actionsPerMinute:F1}\n" +
                             $"Engagement: {analytics.engagementMetrics.overallEngagement:F2}");
                }
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Analytics/Trigger Test Adaptation", false, 502)]
        private static void MenuTriggerTestAdaptation()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.TriggerGameAdaptation(AdaptationType.EngagementBoost, 0.5f);
                Debug.Log("Triggered test game adaptation");
            }
        }
#endif
    }

    // Supporting data structures for analytics
    [System.Serializable]
    public class PlayerProfile
    {
        public string playerId;
        public string playerName;
        public float totalPlayTime;
        public int totalSessions;
        public int sessionsCompleted;
        public PlayStyle preferredPlayStyle;
        public float skillLevel;
        public Dictionary<ConfigKey, float> preferences = new Dictionary<ConfigKey, float>();
        public Dictionary<ActionType, int> achievements = new Dictionary<ActionType, int>();
        public PlayerArchetype dominantArchetype;
        public float createdTime;
        public float lastUpdated;
    }

    [System.Serializable]
    public class GameplaySession
    {
        public string sessionId;
        public float startTime;
        public float endTime;
        public float duration;
        public PlayerProfile playerProfile;
        public int actionCount;
        public int totalActions;
        public float lastActivityTime;
        public SessionAnalytics analytics;
        public Dictionary<ParamKey, object> sessionData = new Dictionary<ParamKey, object>();
    }

    [System.Serializable]
    public class PlayerAction
    {
        public string actionType;
        public float timestamp;
        public float sessionTime;
        public Dictionary<ParamKey, object> parameters = new Dictionary<ParamKey, object>();
        public GameplayContext context;
    }

    [System.Serializable]
    public class ActionMetrics
    {
        public int totalCount;
        public float lastUsed;
        public float averageInterval;
        public float peakUsagePeriod;
    }

    [System.Serializable]
    public class PlayerBehaviorAnalysis
    {
        public string playerId;
        public float analysisTime;
        public PlayerArchetype dominantArchetype;
        public Dictionary<TraitType, float> behaviorScores;
        public PlayStyle playStyle;
        public float engagementLevel;
        public Dictionary<ConfigKey, float> preferences;
        public List<BehaviorInsight> insights;
        public List<string> recommendations;
    }

    [System.Serializable]
    public class SessionAnalytics
    {
        public string sessionId;
        public float sessionDuration;
        public int totalActions;
        public float actionsPerMinute;
        public Dictionary<ActionType, float> dominantActivities;
        public EngagementMetrics engagementMetrics;
        public PerformanceMetrics performanceMetrics;
        public List<EmotionalDataPoint> emotionalJourney;
        public List<KeyMoment> keyMoments;
    }

    [System.Serializable]
    public class PlayerAdaptation
    {
        public AdaptationType adaptationType;
        public float intensity;
        public float timestamp;
        public string reason;
        public PlayerArchetype playerArchetype;
        public Dictionary<SystemType, float> expectedImpact;
    }

    // Enums and supporting types for analytics
    public enum PlayerArchetype { Explorer, Achiever, Socializer, Experimenter, Completionist }
    public enum PlayStyle { Casual, Focused, Intensive, Strategic, Creative }
    public enum EmotionalState { Excitement, Satisfaction, Frustration, Curiosity, Anticipation, Boredom }
    public enum AdaptationType { DifficultyIncrease, DifficultyDecrease, ContentFocus, EngagementBoost, PersonalizationUpdate }

    [System.Serializable]
    public class BehaviorPattern
    {
        public string patternName;
        public string[] requiredActions;
        public float minimumFrequency;
        public float patternScore;
    }

    [System.Serializable]
    public class EngagementMetric
    {
        public string metricName;
        public float weight;
        public float currentValue;
    }

    [System.Serializable]
    public class BehaviorInsight
    {
        public string insightType;
        public string message;
        public float confidence;
        public float timestamp;
    }

    [System.Serializable]
    public class GameplayContext
    {
        public string currentScene;
        public string currentActivity;
        public Dictionary<ParamKey, object> environmentalFactors;
    }

    [System.Serializable]
    public class EngagementMetrics
    {
        public float overallEngagement;
        public float sessionEngagement;
        public float activityEngagement;
        public List<float> engagementOverTime;
    }

    [System.Serializable]
    public class PerformanceMetrics
    {
        public float successRate;
        public float averageCompletionTime;
        public float difficultyRating;
        public int retryCount;
    }

    [System.Serializable]
    public class EmotionalDataPoint
    {
        public float timestamp;
        public EmotionalState state;
        public float intensity;
        public string trigger;
    }

    [System.Serializable]
    public class KeyMoment
    {
        public float timestamp;
        public string momentType;
        public string description;
        public float significance;
    }

        private PlayerProfile LoadOrCreatePlayerProfile()
        {
            string profilePath = GetAnalyticsDataPath("player_profile.json");

            if (System.IO.File.Exists(profilePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(profilePath);
                    var profile = JsonUtility.FromJson<PlayerProfile>(json);
                    profile.lastUpdated = Time.time;
                    return profile;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load player profile: {e.Message}");
                }
            }

            // Create new profile
            var newProfile = new PlayerProfile
            {
                playerId = GeneratePlayerId(),
                createdTime = Time.time,
                lastUpdated = Time.time,
                totalPlayTime = 0f,
                sessionsCompleted = 0,
                preferredPlayStyle = PlayStyle.Casual,
                skillLevel = 0.5f
            };

            SavePlayerProfile(newProfile);
            return newProfile;
        }

        private string GetAnalyticsDataPath(string filename)
        {
            string dataPath = System.IO.Path.Combine(Application.persistentDataPath, "Analytics");
            if (!System.IO.Directory.Exists(dataPath))
            {
                System.IO.Directory.CreateDirectory(dataPath);
            }
            return System.IO.Path.Combine(dataPath, filename);
        }

        private string GeneratePlayerId()
        {
            // Generate anonymous but consistent player ID
            if (anonymizeData)
            {
                return System.Guid.NewGuid().ToString().Substring(0, 8);
            }
            else
            {
                return $"player_{System.DateTime.Now.Ticks}";
            }
        }

        private void SavePlayerProfile(PlayerProfile profile)
        {
            try
            {
                string json = JsonUtility.ToJson(profile, true);
                System.IO.File.WriteAllText(GetAnalyticsDataPath("player_profile.json"), json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save player profile: {e.Message}");
            }
        }

        private Dictionary<TraitType, float> ConvertBehaviorScoresToEnumDict()
        {
            return new Dictionary<TraitType, float>(behaviorScores);
        }

        private void LoadSessionHistory()
        {
            string historyPath = GetAnalyticsDataPath("session_history.json");
            sessionHistory.Clear();

            if (System.IO.File.Exists(historyPath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(historyPath);
                    var wrapper = JsonUtility.FromJson<SessionHistoryWrapper>(json);
                    sessionHistory = wrapper.sessions ?? new List<GameplaySession>();

                    // Clean up old sessions based on retention settings
                    CleanupOldSessions();

                    Debug.Log($"Loaded {sessionHistory.Count} sessions from history");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Failed to load session history: {e.Message}");
                    sessionHistory = new List<GameplaySession>();
                }
            }
        }

        private void CleanupOldSessions()
        {
            if (dataRetentionDays <= 0) return;

            float cutoffTime = Time.time - (dataRetentionDays * 24 * 3600);
            sessionHistory.RemoveAll(session => session.startTime < cutoffTime);
        }

        private GameplayContext CaptureCurrentContext()
        {
            var context = new GameplayContext
            {
                currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                currentActivity = DetermineCurrentActivity(),
                environmentalFactors = new Dictionary<ParamKey, object>()
            };

            // Capture environmental factors
            context.environmentalFactors[ParamKey.SessionTime] = Time.time - sessionStartTime;
            context.environmentalFactors[ParamKey.TotalActions] = currentSessionActions.Count;
            context.environmentalFactors[ParamKey.FrameRate] = Application.targetFrameRate;

            // Capture Chimera-specific context
            var chimeraManager = FindFirstObjectByType<Laboratory.Chimera.AI.ChimeraAIManager>();
            if (chimeraManager != null)
            {
                context.environmentalFactors[ParamKey.ActiveCreatures] = GetActiveCreatureCount();
                context.environmentalFactors[ParamKey.CurrentBiome] = GetCurrentBiome();
            }

            // Capture quest context
            var questSystem = FindFirstObjectByType<Laboratory.Systems.Quests.QuestManager>();
            if (questSystem != null)
            {
                context.environmentalFactors[ParamKey.ActiveQuests] = GetActiveQuestCount();
                context.environmentalFactors[ParamKey.QuestProgress] = GetCurrentQuestProgress();
            }

            return context;
        }

        private string DetermineCurrentActivity()
        {
            // Determine activity based on recent actions
            var recentActions = GetRecentActions(30f); // Last 30 seconds
            if (recentActions.Count == 0) return "Idle";

            var actionCounts = recentActions.GroupBy(a => a.actionType)
                .ToDictionary(g => g.Key, g => g.Count());

            var dominantAction = actionCounts.OrderByDescending(kvp => kvp.Value).First().Key;

            // Map actions to activities
            if (dominantAction.Contains("breed") || dominantAction.Contains("genetics")) return "Breeding";
            if (dominantAction.Contains("quest") || dominantAction.Contains("mission")) return "Questing";
            if (dominantAction.Contains("explore") || dominantAction.Contains("move")) return "Exploration";
            if (dominantAction.Contains("menu") || dominantAction.Contains("ui")) return "MenuNavigation";
            if (dominantAction.Contains("battle") || dominantAction.Contains("combat")) return "Combat";

            return "General";
        }

        private int GetActiveCreatureCount()
        {
            var creatures = FindObjectsByType<Laboratory.Chimera.AI.ChimeraMonsterAI>(FindObjectsSortMode.None);
            return creatures?.Length ?? 0;
        }

        private string GetCurrentBiome()
        {
            // Try to determine current biome from environment
            var biomeManager = FindFirstObjectByType<Laboratory.Subsystems.Ecosystem.Services.BiomeManager>();
            if (biomeManager != null)
            {
                // Try to get current biome from recent exploration actions
                var recentExploration = actionHistory
                    .Where(a => a.actionType == ActionType.Exploration &&
                               a.parameters.ContainsKey(ParamKey.Biome) &&
                               (DateTime.UtcNow - a.timestamp).TotalMinutes < 30)
                    .OrderByDescending(a => a.timestamp)
                    .FirstOrDefault();

                if (recentExploration != null && recentExploration.parameters.TryGetValue(ParamKey.Biome, out var biomeValue))
                {
                    return biomeValue.ToString();
                }

                // Fallback to most commonly visited biome
                var biomeVisits = actionHistory
                    .Where(a => a.actionType == ActionType.Exploration && a.parameters.ContainsKey(ParamKey.Biome))
                    .GroupBy(a => a.parameters[ParamKey.Biome].ToString())
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (biomeVisits != null)
                {
                    return biomeVisits.Key;
                }
            }

            // Default to Forest biome if no data available
            return BiomeType.Forest.ToString();
        }

        private int GetActiveQuestCount()
        {
            var questManager = FindFirstObjectByType<Laboratory.Systems.Quests.QuestManager>();
            if (questManager != null)
            {
                // Count active quests from recent quest actions
                var activeQuestIds = actionHistory
                    .Where(a => a.actionType == ActionType.Quest &&
                               a.parameters.ContainsKey(ParamKey.QuestId) &&
                               a.parameters.ContainsKey(ParamKey.Completed) &&
                               !(bool)a.parameters[ParamKey.Completed] &&
                               (DateTime.UtcNow - a.timestamp).TotalHours < 24)
                    .Select(a => a.parameters[ParamKey.QuestId].ToString())
                    .Distinct()
                    .Count();

                return Math.Max(activeQuestIds, 1); // Assume at least 1 quest active for new players
            }

            // Default quest count for new players
            return 1;
        }

        private float GetCurrentQuestProgress()
        {
            var questManager = FindFirstObjectByType<Laboratory.Systems.Quests.QuestManager>();
            if (questManager != null)
            {
                // Calculate quest completion from recent quest actions
                var questActions = actionHistory
                    .Where(a => a.actionType == ActionType.Quest &&
                               a.parameters.ContainsKey(ParamKey.Progress))
                    .ToList();

                if (questActions.Count > 0)
                {
                    // Get the most recent progress for each quest
                    var questProgress = questActions
                        .GroupBy(a => a.parameters.GetValueOrDefault(ParamKey.QuestId, "default").ToString())
                        .Select(g => g.OrderByDescending(a => a.timestamp).First())
                        .Select(a => Convert.ToSingle(a.parameters[ParamKey.Progress]))
                        .ToList();

                    if (questProgress.Count > 0)
                    {
                        return questProgress.Average() / 100f; // Convert percentage to 0-1 range
                    }
                }

                // Check for completed quests
                var completedQuests = actionHistory
                    .Where(a => a.actionType == ActionType.Quest &&
                               a.parameters.ContainsKey(ParamKey.Completed) &&
                               (bool)a.parameters[ParamKey.Completed])
                    .Count();

                var totalQuests = Math.Max(actionHistory
                    .Where(a => a.actionType == ActionType.Quest && a.parameters.ContainsKey(ParamKey.QuestId))
                    .Select(a => a.parameters[ParamKey.QuestId].ToString())
                    .Distinct()
                    .Count(), 1);

                return (float)completedQuests / totalQuests;
            }

            // Default progress for new players
            return 0.1f;
        }

        private float CalculateAverageInterval(string actionType)
        {
            // Calculate average time between actions of this type from recent session data
            var typeActions = actionHistory
                .Where(a => a.actionType == actionType)
                .OrderBy(a => a.timestamp)
                .ToList();

            if (typeActions.Count < 2) return 5f; // Default 5 second interval for new action types

            var intervals = new List<float>();
            for (int i = 1; i < typeActions.Count; i++)
            {
                var interval = (float)(typeActions[i].timestamp - typeActions[i-1].timestamp).TotalSeconds;
                intervals.Add(interval);
            }

            return intervals.Count > 0 ? intervals.Average() : 5f;
            var actionsOfType = currentSessionActions.Where(a => a.actionType == actionType).ToList();
            return actionsOfType.Count > 1 ? 1.0f : 0.0f;
        }

        private void AnalyzeActionInRealTime(PlayerAction action)
        {
            // Update behavior scores based on action type
            UpdateBehaviorScoresFromAction(action);

            // Check for interesting patterns
            DetectActionPatterns(action);

            // Update engagement metrics
            UpdateEngagementFromAction(action);

            if (enableRealTimeAnalysis)
            {
                Debug.Log($"Analyzed action: {action.actionType} in {action.context.currentActivity}");
            }
        }

        private SessionAnalytics AnalyzeCurrentSession()
        {
            if (currentSession == null) return new SessionAnalytics();

            var analytics = new SessionAnalytics
            {
                startTime = currentSession.startTime,
                duration = Time.time - currentSession.startTime,
                totalActions = currentSessionActions.Count,
                actionsPerMinute = CalculateActionsPerMinute(),
                dominantActivities = GetDominantActivities(),
                engagementScore = engagementAnalyzer.CalculateCurrentEngagement(),
                successRate = CalculateSessionSuccessRate(),
                insights = GenerateBehaviorInsights()
            };

            return analytics;
        }

        private void UpdateBehaviorScoresFromAction(PlayerAction action)
        {
            if (!analyticsInitialized) return;

            string actionLower = action.actionType.ToLower();

            if (actionLower.Contains("explore") || actionLower.Contains("move"))
            {
                if (behaviorScores.ContainsKey(TraitType.Curiosity))
                    behaviorScores[TraitType.Curiosity] = Mathf.Clamp01(behaviorScores[TraitType.Curiosity] + 0.01f);
            }
            else if (actionLower.Contains("breed") || actionLower.Contains("genetics"))
            {
                if (behaviorScores.ContainsKey(TraitType.Intelligence))
                    behaviorScores[TraitType.Intelligence] = Mathf.Clamp01(behaviorScores[TraitType.Intelligence] + 0.01f);
            }
            else if (actionLower.Contains("social") || actionLower.Contains("interact"))
            {
                if (behaviorScores.ContainsKey(TraitType.Sociability))
                    behaviorScores[TraitType.Sociability] = Mathf.Clamp01(behaviorScores[TraitType.Sociability] + 0.01f);
            }
            else if (actionLower.Contains("quest") || actionLower.Contains("achievement"))
            {
                if (behaviorScores.ContainsKey(TraitType.Dominance))
                    behaviorScores[TraitType.Dominance] = Mathf.Clamp01(behaviorScores[TraitType.Dominance] + 0.01f);
            }
        }

        private void DetectActionPatterns(PlayerAction action)
        {
            // Look for rapid consecutive actions (might indicate frustration or high engagement)
            var recentActions = GetRecentActions(10f);
            if (recentActions.Count > 20)
            {
                Debug.Log("High activity detected - player is highly engaged");
            }

            // Look for repetitive actions (might indicate grinding or confusion)
            var sameTypeActions = recentActions.Where(a => a.actionType == action.actionType).Count();
            if (sameTypeActions > 10)
            {
                Debug.Log($"Repetitive {action.actionType} actions detected - possible grinding behavior");
            }
        }

        private void UpdateEngagementFromAction(PlayerAction action)
        {
            // Simple engagement tracking based on action frequency and variety
            // Action type is already enum-based, parse from string for compatibility
            System.Enum.TryParse<ActionType>(action.actionType, out var actionID);
            if (!actionMetrics.ContainsKey(actionID))
            {
                // New action type increases engagement
                currentEngagement = Mathf.Clamp01(currentEngagement + 0.05f);
            }
        }

        private float CalculateSessionSuccessRate()
        {
            // Calculate success based on completed vs attempted actions
            var completedActions = currentSessionActions.Where(a =>
                a.parameters.ContainsKey("success") &&
                a.parameters["success"].ToString().ToLower() == "true").Count();

            if (currentSessionActions.Count == 0) return 0.5f;
            return (float)completedActions / currentSessionActions.Count;
        }

        private void UpdateUIMetrics(string elementName, string interactionType, float interactionTime)
        {
            if (!trackUIInteractions) return;

            // Track UI element usage frequency
            var uiKey = $"{elementName}_{interactionType}";
            if (!inputMetrics.ContainsKey(ActionType.UI))
            {
                inputMetrics[ActionType.UI] = new InputMetrics();
            }

            var metrics = inputMetrics[ActionType.UI];
            metrics.totalInputs++;
            metrics.averageInputRate = CalculateInputRate(ActionType.UI);

            if (!metrics.inputTypes.ContainsKey(ActionType.UI))
                metrics.inputTypes[ActionType.UI] = 0;
            metrics.inputTypes[ActionType.UI]++;

            // Update behavior scores based on UI interaction patterns
            if (interactionTime > 5f) // Long interaction suggests careful consideration
            {
                behaviorScores[TraitType.Caution] = Mathf.Clamp01(behaviorScores[TraitType.Caution] + 0.002f);
            }
            else if (interactionTime < 1f) // Quick interaction suggests efficiency
            {
                behaviorScores[TraitType.Speed] = Mathf.Clamp01(behaviorScores[TraitType.Speed] + 0.002f);
            }
        }

        private void UpdateChoicePreferences(ChoiceCategory choiceCategory, string choiceValue)
        {
            if (!trackGameplayChoices) return;

            // Update player profile preferences based on choices
            if (!currentPlayerProfile.preferences.ContainsKey(ConfigKey.Difficulty))
                currentPlayerProfile.preferences[ConfigKey.Difficulty] = 0.5f;

            switch (choiceCategory)
            {
                case ChoiceCategory.QuestCompletion:
                    // Successful quest completion increases confidence in current difficulty
                    if (choiceValue.Contains("Success") || choiceValue.Contains("Complete"))
                    {
                        behaviorScores[TraitType.Dominance] = Mathf.Clamp01(behaviorScores[TraitType.Dominance] + 0.01f);
                        currentPlayerProfile.preferences[ConfigKey.Difficulty] =
                            Mathf.Clamp01(currentPlayerProfile.preferences[ConfigKey.Difficulty] + 0.02f);
                    }
                    break;

                case ChoiceCategory.BreedingChoice:
                    // Breeding choices indicate experimentation vs optimization preference
                    if (choiceValue.Contains("Experimental"))
                    {
                        behaviorScores[TraitType.Curiosity] = Mathf.Clamp01(behaviorScores[TraitType.Curiosity] + 0.015f);
                    }
                    else if (choiceValue.Contains("Optimal") || choiceValue.Contains("Safe"))
                    {
                        behaviorScores[TraitType.Caution] = Mathf.Clamp01(behaviorScores[TraitType.Caution] + 0.015f);
                    }
                    break;

                case ChoiceCategory.SocialInteraction:
                    // Social choices affect sociability scores
                    behaviorScores[TraitType.Sociability] = Mathf.Clamp01(behaviorScores[TraitType.Sociability] + 0.01f);
                    break;
            }
        }

        private void UpdateEmotionalProfile(EmotionalState emotionalState, float intensity)
        {
            if (!trackEmotionalResponse) return;

            // Update behavior scores based on emotional responses
            switch (emotionalState)
            {
                case EmotionalState.Excitement:
                    behaviorScores[TraitType.Curiosity] = Mathf.Clamp01(behaviorScores[TraitType.Curiosity] + intensity * 0.01f);
                    behaviorScores[TraitType.Adaptability] = Mathf.Clamp01(behaviorScores[TraitType.Adaptability] + intensity * 0.005f);
                    break;

                case EmotionalState.Satisfaction:
                    behaviorScores[TraitType.Dominance] = Mathf.Clamp01(behaviorScores[TraitType.Dominance] + intensity * 0.008f);
                    break;

                case EmotionalState.Frustration:
                    behaviorScores[TraitType.Caution] = Mathf.Clamp01(behaviorScores[TraitType.Caution] + intensity * 0.01f);
                    // Reduce difficulty preference if frustrated
                    if (currentPlayerProfile.preferences.ContainsKey(ConfigKey.Difficulty))
                    {
                        currentPlayerProfile.preferences[ConfigKey.Difficulty] =
                            Mathf.Clamp01(currentPlayerProfile.preferences[ConfigKey.Difficulty] - intensity * 0.05f);
                    }
                    break;

                case EmotionalState.Curiosity:
                    behaviorScores[TraitType.Curiosity] = Mathf.Clamp01(behaviorScores[TraitType.Curiosity] + intensity * 0.012f);
                    behaviorScores[TraitType.Intelligence] = Mathf.Clamp01(behaviorScores[TraitType.Intelligence] + intensity * 0.005f);
                    break;

                case EmotionalState.Anticipation:
                    behaviorScores[TraitType.Speed] = Mathf.Clamp01(behaviorScores[TraitType.Speed] + intensity * 0.007f);
                    break;

                case EmotionalState.Boredom:
                    // Boredom suggests need for more variety or challenge
                    if (currentPlayerProfile.preferences.ContainsKey(ConfigKey.Difficulty))
                    {
                        currentPlayerProfile.preferences[ConfigKey.Difficulty] =
                            Mathf.Clamp01(currentPlayerProfile.preferences[ConfigKey.Difficulty] + intensity * 0.03f);
                    }
                    break;
            }

            // Track emotional pattern for archetype determination
            if (intensity > 0.7f) // Strong emotional response
            {
                behaviorScores[TraitType.Sociability] = Mathf.Clamp01(behaviorScores[TraitType.Sociability] + 0.005f);
            }
        }

        private PlayerArchetype DetermineDominantArchetype()
        {
            if (behaviorScores.Count == 0) return PlayerArchetype.Explorer;

            // Analyze behavior patterns to determine archetype
            float explorationScore = behaviorScores.GetValueOrDefault(TraitType.Curiosity, 0f) +
                                    behaviorScores.GetValueOrDefault(TraitType.Adaptability, 0f);

            float achieverScore = behaviorScores.GetValueOrDefault(TraitType.Dominance, 0f) +
                                behaviorScores.GetValueOrDefault(TraitType.Intelligence, 0f);

            float socializerScore = behaviorScores.GetValueOrDefault(TraitType.Sociability, 0f) * 2f;

            float experimenterScore = behaviorScores.GetValueOrDefault(TraitType.Curiosity, 0f) +
                                     behaviorScores.GetValueOrDefault(TraitType.Intelligence, 0f);

            float completionistScore = behaviorScores.GetValueOrDefault(TraitType.Caution, 0f) +
                                      behaviorScores.GetValueOrDefault(TraitType.Dominance, 0f);

            // Count quest/achievement oriented actions
            var questActions = currentSessionActions.Count(a => a.actionType.Contains("Quest") || a.actionType.Contains("Achievement"));
            if (questActions > currentSessionActions.Count * 0.4f)
            {
                completionistScore += 0.3f;
            }

            // Count exploration actions
            var explorationActions = currentSessionActions.Count(a => a.actionType.Contains("Exploration") || a.actionType.Contains("Discovery"));
            if (explorationActions > currentSessionActions.Count * 0.3f)
            {
                explorationScore += 0.2f;
            }

            // Count social actions
            var socialActions = currentSessionActions.Count(a => a.actionType.Contains("Social") || a.actionType.Contains("Interaction"));
            if (socialActions > currentSessionActions.Count * 0.2f)
            {
                socializerScore += 0.2f;
            }

            // Return highest scoring archetype
            var scores = new Dictionary<PlayerArchetype, float>
            {
                [PlayerArchetype.Explorer] = explorationScore,
                [PlayerArchetype.Achiever] = achieverScore,
                [PlayerArchetype.Socializer] = socializerScore,
                [PlayerArchetype.Experimenter] = experimenterScore,
                [PlayerArchetype.Completionist] = completionistScore
            };

            return scores.OrderByDescending(kvp => kvp.Value).First().Key;
        }

        private PlayStyle AnalyzePlayStyle()
        {
            if (currentSessionActions.Count == 0) return PlayStyle.Casual;

            float sessionDuration = Time.time - sessionStartTime;
            float actionsPerMinute = sessionDuration > 0 ? (currentSessionActions.Count / sessionDuration) * 60f : 0f;

            // Analyze action frequency and session patterns
            var recentSessions = sessionHistory.TakeLast(5).ToList();
            float avgSessionLength = recentSessions.Count > 0 ? recentSessions.Average(s => s.duration) : sessionDuration;

            // Calculate focus metrics
            var actionTypes = currentSessionActions.GroupBy(a => a.actionType).Count();
            float focusScore = actionTypes > 0 ? (float)currentSessionActions.Count / actionTypes : 1f;

            // Determine play style based on metrics
            if (actionsPerMinute > 15f && avgSessionLength > 3600f) // High APM, long sessions
            {
                return focusScore > 10f ? PlayStyle.Intensive : PlayStyle.Focused;
            }
            else if (actionsPerMinute > 10f)
            {
                return focusScore > 8f ? PlayStyle.Focused : PlayStyle.Strategic;
            }
            else if (avgSessionLength > 1800f && focusScore > 12f) // Long focused sessions
            {
                return PlayStyle.Strategic;
            }
            else if (actionTypes > 6) // High variety of actions
            {
                return PlayStyle.Creative;
            }

            return PlayStyle.Casual;
        }

        private List<BehaviorInsight> GenerateBehaviorInsights()
        {
            var insights = new List<BehaviorInsight>();

            if (behaviorScores.Count == 0) return insights;

            // Analyze high trait scores for insights
            foreach (var trait in behaviorScores)
            {
                if (trait.Value > 0.8f)
                {
                    insights.Add(new BehaviorInsight
                    {
                        insightType = "HighTrait",
                        message = $"Player shows strong {trait.Key.ToString().ToLower()} tendencies (Score: {trait.Value:F2})",
                        confidence = trait.Value,
                        timestamp = Time.time
                    });
                }
                else if (trait.Value < 0.2f)
                {
                    insights.Add(new BehaviorInsight
                    {
                        insightType = "LowTrait",
                        message = $"Player avoids {trait.Key.ToString().ToLower()}-based activities (Score: {trait.Value:F2})",
                        confidence = 1f - trait.Value,
                        timestamp = Time.time
                    });
                }
            }

            // Analyze session patterns
            float sessionDuration = Time.time - sessionStartTime;
            if (sessionDuration > 7200f) // 2+ hour session
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = "LongSession",
                    message = "Player demonstrates high engagement with extended play sessions",
                    confidence = Mathf.Clamp01(sessionDuration / 10800f), // Confidence increases up to 3 hours
                    timestamp = Time.time
                });
            }

            // Analyze action variety
            var actionTypes = currentSessionActions.GroupBy(a => a.actionType).Count();
            if (actionTypes > 8)
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = "HighVariety",
                    message = $"Player explores diverse gameplay mechanics ({actionTypes} different action types)",
                    confidence = Mathf.Clamp01(actionTypes / 12f),
                    timestamp = Time.time
                });
            }
            else if (actionTypes < 3 && currentSessionActions.Count > 20)
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = "Focused",
                    message = "Player shows focused behavior, concentrating on specific activities",
                    confidence = 0.8f,
                    timestamp = Time.time
                });
            }

            // Analyze difficulty preference patterns
            var archetype = DetermineDominantArchetype();
            if (archetype == PlayerArchetype.Achiever && behaviorScores.GetValueOrDefault(TraitType.Dominance, 0f) > 0.7f)
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = "Achievement",
                    message = "Player thrives on challenges and achievement completion",
                    confidence = behaviorScores[TraitType.Dominance],
                    timestamp = Time.time
                });
            }

            return insights.OrderByDescending(i => i.confidence).Take(5).ToList();
        }

        private List<string> GenerateGameplayRecommendations()
        {
            var recommendations = new List<string>();

            // Analyze player preferences
            var preferences = preferenceAnalyzer.GetCurrentPreferences();
            var recentActivity = DetermineCurrentActivity();
            var skillLevel = CalculatePlayerSkillLevel();

            // Generate breeding recommendations
            if (preferences.ContainsKey("breedingPreference") && preferences["breedingPreference"] > 0.6f)
            {
                recommendations.Add("Try breeding creatures with complementary traits for stronger offspring");
                recommendations.Add("Experiment with rare genetic combinations you haven't tried yet");

                if (skillLevel > 0.7f)
                {
                    recommendations.Add("Challenge yourself with complex multi-generational breeding projects");
                }
            }

            // Generate exploration recommendations
            if (preferences.ContainsKey("explorationPreference") && preferences["explorationPreference"] > 0.6f)
            {
                recommendations.Add("Explore new biomes to discover unique creatures and resources");
                recommendations.Add("Search for rare environmental interactions and hidden areas");
            }

            // Generate quest recommendations
            if (preferences.ContainsKey("questPreference") && preferences["questPreference"] > 0.5f)
            {
                recommendations.Add("Take on quests that match your favorite creature types");
                recommendations.Add("Try cooperative quests to experience different gameplay dynamics");
            }

            // Generate skill-based recommendations
            if (skillLevel < 0.4f)
            {
                recommendations.Add("Focus on basic creature care to build foundational skills");
                recommendations.Add("Practice with easier breeding combinations first");
            }
            else if (skillLevel > 0.8f)
            {
                recommendations.Add("Take on expert-level challenges for rare rewards");
                recommendations.Add("Mentor newer players to expand your knowledge");
            }

            // Activity-specific recommendations
            switch (recentActivity)
            {
                case "Breeding":
                    recommendations.Add("Document your successful breeding strategies for future reference");
                    break;
                case "Exploration":
                    recommendations.Add("Create a map of your discoveries to track progress");
                    break;
                case "Questing":
                    recommendations.Add("Try varying your quest approach for different outcomes");
                    break;
                case "Idle":
                    recommendations.Add("Consider setting a short-term goal to stay engaged");
                    break;
            }

            // Engagement-based recommendations
            var currentEngagement = engagementAnalyzer.CalculateCurrentEngagement();
            if (currentEngagement < 0.5f)
            {
                recommendations.Add("Take a break and try a different activity to refresh your interest");
                recommendations.Add("Set small, achievable goals to rebuild momentum");
            }

            return recommendations.Take(5).ToList(); // Limit to top 5 recommendations
        }

        private float CalculatePlayerSkillLevel()
        {
            if (sessionHistory.Count == 0) return 0.5f;

            // Calculate based on performance metrics from recent sessions
            var recentSessions = sessionHistory.TakeLast(10);
            float avgSuccessRate = 0f;
            float avgActionsPerMinute = 0f;
            int sessionCount = 0;

            foreach (var session in recentSessions)
            {
                if (session.analytics != null)
                {
                    avgSuccessRate += session.analytics.successRate;
                    avgActionsPerMinute += session.analytics.actionsPerMinute;
                    sessionCount++;
                }
            }

            if (sessionCount > 0)
            {
                avgSuccessRate /= sessionCount;
                avgActionsPerMinute /= sessionCount;

                // Combine metrics to determine skill level
                float skillLevel = (avgSuccessRate * 0.7f) + (Mathf.Clamp01(avgActionsPerMinute / 10f) * 0.3f);
                return Mathf.Clamp01(skillLevel);
            }

            return currentPlayerProfile.skillLevel;
        }

        private float CalculateActionsPerMinute()
        {
            if (sessionStartTime == null) return 0f;

            var sessionDuration = (float)(DateTime.UtcNow - sessionStartTime.Value).TotalMinutes;
            return sessionDuration > 0 ? totalActionCount / sessionDuration : 0f;
        }

        private Dictionary<ActionType, float> GetDominantActivities()
        {
            // Calculate dominant activities based on action metrics
            var activities = new Dictionary<ActionType, float>();
            foreach (var kvp in actionMetrics)
            {
                activities[kvp.Key] = kvp.Value.totalCount;
            }
            return activities;
        }

        private EngagementMetrics CalculateEngagementMetrics()
        {
            float sessionDuration = Time.time - sessionStartTime;

            // Calculate session engagement based on action frequency
            float sessionEngagement = 0.5f;
            if (sessionDuration > 0)
            {
                float actionsPerMinute = (currentSessionActions.Count / sessionDuration) * 60f;
                sessionEngagement = Mathf.Clamp01(actionsPerMinute / 10f); // Target: 10 actions per minute for full engagement
            }

            // Calculate activity engagement based on variety and completion
            float activityEngagement = 0.5f;
            if (currentSessionActions.Count > 0)
            {
                var actionTypes = currentSessionActions.GroupBy(a => a.actionType).Count();
                var completedActions = currentSessionActions.Count(a =>
                    a.parameters != null &&
                    a.parameters.ContainsKey(ParamKey.Success) &&
                    a.parameters[ParamKey.Success].ToString().ToLower() == "true");

                float varietyScore = Mathf.Clamp01(actionTypes / 8f); // Target: 8+ action types
                float completionRate = currentSessionActions.Count > 0 ?
                    (float)completedActions / currentSessionActions.Count : 0f;

                activityEngagement = (varietyScore * 0.6f) + (completionRate * 0.4f);
            }

            // Calculate overall engagement from recent sessions
            float overallEngagement = 0.5f;
            var recentSessions = sessionHistory.TakeLast(10).ToList();
            if (recentSessions.Count > 0)
            {
                float avgSessionDuration = recentSessions.Average(s => s.duration);
                float avgActionsPerSession = recentSessions.Average(s => s.actionCount);

                float durationScore = Mathf.Clamp01(avgSessionDuration / 3600f); // Target: 1 hour sessions
                float activityScore = Mathf.Clamp01(avgActionsPerSession / 100f); // Target: 100+ actions per session

                overallEngagement = (durationScore * 0.5f) + (activityScore * 0.3f) + (sessionEngagement * 0.2f);
            }

            // Track engagement over time (last 10 data points)
            var engagementOverTime = new List<float>();
            if (recentSessions.Count > 0)
            {
                foreach (var session in recentSessions.TakeLast(10))
                {
                    float sessionScore = session.duration > 0 ?
                        Mathf.Clamp01((session.actionCount / session.duration) * 60f / 10f) : 0f;
                    engagementOverTime.Add(sessionScore);
                }
            }
            else
            {
                engagementOverTime.Add(sessionEngagement);
            }

            return new EngagementMetrics
            {
                overallEngagement = Mathf.Clamp01(overallEngagement),
                sessionEngagement = Mathf.Clamp01(sessionEngagement),
                activityEngagement = Mathf.Clamp01(activityEngagement),
                engagementOverTime = engagementOverTime
            };
        }

        private PerformanceMetrics CalculatePerformanceMetrics()
        {
            float successRate = 0.5f;
            float averageCompletionTime = 60f;
            float difficultyRating = 0.5f;
            int retryCount = 0;

            if (currentSessionActions.Count > 0)
            {
                // Calculate success rate from completed actions
                var actionsWithResults = currentSessionActions.Where(a =>
                    a.parameters != null && a.parameters.ContainsKey(ParamKey.Success)).ToList();

                if (actionsWithResults.Count > 0)
                {
                    var successfulActions = actionsWithResults.Count(a =>
                        a.parameters[ParamKey.Success].ToString().ToLower() == "true");
                    successRate = (float)successfulActions / actionsWithResults.Count;
                }

                // Calculate average completion time for breeding/quest actions
                var timedActions = currentSessionActions.Where(a =>
                    a.parameters != null && a.parameters.ContainsKey(ParamKey.ResearchTime)).ToList();

                if (timedActions.Count > 0)
                {
                    var completionTimes = timedActions.Select(a =>
                        float.Parse(a.parameters[ParamKey.ResearchTime].ToString())).ToList();
                    averageCompletionTime = completionTimes.Average();
                }

                // Estimate difficulty rating based on player behavior
                float cautionScore = behaviorScores.GetValueOrDefault(TraitType.Caution, 0.5f);
                float dominanceScore = behaviorScores.GetValueOrDefault(TraitType.Dominance, 0.5f);

                // High caution + low dominance = prefers easier content
                // Low caution + high dominance = prefers harder content
                difficultyRating = (dominanceScore + (1f - cautionScore)) / 2f;

                // Count retry patterns (same action type within short time)
                var groupedActions = currentSessionActions
                    .GroupBy(a => a.actionType)
                    .Where(g => g.Count() > 1);

                foreach (var group in groupedActions)
                {
                    var sortedActions = group.OrderBy(a => a.timestamp).ToList();
                    for (int i = 1; i < sortedActions.Count; i++)
                    {
                        if (sortedActions[i].timestamp - sortedActions[i - 1].timestamp < 300f) // 5 minutes
                        {
                            retryCount++;
                        }
                    }
                }
            }

            // Factor in historical performance from recent sessions
            var recentSessions = sessionHistory.TakeLast(5).Where(s => s.analytics != null).ToList();
            if (recentSessions.Count > 0)
            {
                float historicalSuccessRate = recentSessions.Average(s => s.analytics.performanceMetrics.successRate);
                successRate = (successRate * 0.7f) + (historicalSuccessRate * 0.3f); // Weight current session more
            }

            return new PerformanceMetrics
            {
                successRate = Mathf.Clamp01(successRate),
                averageCompletionTime = Mathf.Max(1f, averageCompletionTime),
                difficultyRating = Mathf.Clamp01(difficultyRating),
                retryCount = retryCount
            };
        }

        private List<EmotionalDataPoint> AnalyzeEmotionalJourney()
        {
            var emotionalJourney = new List<EmotionalDataPoint>();

            if (!trackEmotionalResponse) return emotionalJourney;

            // Extract emotional responses from session actions
            var emotionalActions = currentSessionActions.Where(a =>
                a.parameters != null && a.parameters.ContainsKey(ParamKey.EmotionalState)).ToList();

            foreach (var action in emotionalActions)
            {
                if (System.Enum.TryParse<EmotionalState>(action.parameters[ParamKey.EmotionalState].ToString(), out var state))
                {
                    float intensity = 0.5f;
                    if (action.parameters.ContainsKey(ParamKey.Intensity))
                    {
                        float.TryParse(action.parameters[ParamKey.Intensity].ToString(), out intensity);
                    }

                    string trigger = "Unknown";
                    if (action.parameters.ContainsKey(ParamKey.Trigger))
                    {
                        trigger = action.parameters[ParamKey.Trigger].ToString();
                    }

                    emotionalJourney.Add(new EmotionalDataPoint
                    {
                        timestamp = action.timestamp,
                        state = state,
                        intensity = intensity,
                        trigger = trigger
                    });
                }
            }

            // Infer emotional states from performance patterns
            if (currentSessionActions.Count > 10)
            {
                var recentActions = currentSessionActions.TakeLast(5).ToList();
                var successfulActions = recentActions.Count(a =>
                    a.parameters != null &&
                    a.parameters.ContainsKey(ParamKey.Success) &&
                    a.parameters[ParamKey.Success].ToString().ToLower() == "true");

                float recentSuccessRate = recentActions.Count > 0 ? (float)successfulActions / recentActions.Count : 0.5f;

                // Infer satisfaction from success streaks
                if (recentSuccessRate > 0.8f)
                {
                    emotionalJourney.Add(new EmotionalDataPoint
                    {
                        timestamp = Time.time,
                        state = EmotionalState.Satisfaction,
                        intensity = recentSuccessRate,
                        trigger = "SuccessStreak"
                    });
                }
                // Infer frustration from failure patterns
                else if (recentSuccessRate < 0.3f)
                {
                    emotionalJourney.Add(new EmotionalDataPoint
                    {
                        timestamp = Time.time,
                        state = EmotionalState.Frustration,
                        intensity = 1f - recentSuccessRate,
                        trigger = "FailurePattern"
                    });
                }
            }

            return emotionalJourney.OrderBy(e => e.timestamp).ToList();
        }

        private List<KeyMoment> IdentifyKeyMoments()
        {
            var keyMoments = new List<KeyMoment>();

            if (currentSessionActions.Count < 5) return keyMoments;

            // Identify breakthrough moments (first success after failures)
            var actionsWithResults = currentSessionActions.Where(a =>
                a.parameters != null && a.parameters.ContainsKey(ParamKey.Success)).ToList();

            for (int i = 1; i < actionsWithResults.Count; i++)
            {
                var current = actionsWithResults[i];
                var previous = actionsWithResults[i - 1];

                bool currentSuccess = current.parameters[ParamKey.Success].ToString().ToLower() == "true";
                bool previousSuccess = previous.parameters[ParamKey.Success].ToString().ToLower() == "true";

                if (currentSuccess && !previousSuccess)
                {
                    keyMoments.Add(new KeyMoment
                    {
                        timestamp = current.timestamp,
                        momentType = "Breakthrough",
                        description = $"Player achieved success in {current.actionType} after previous attempt",
                        significance = 0.7f
                    });
                }
            }

            // Identify discovery moments (first time actions)
            var firstTimeActions = currentSessionActions
                .GroupBy(a => a.actionType)
                .Where(g => g.Count() == 1)
                .Select(g => g.First())
                .ToList();

            foreach (var action in firstTimeActions)
            {
                if (action.parameters != null && action.parameters.ContainsKey(ParamKey.FirstDiscovery) &&
                    action.parameters[ParamKey.FirstDiscovery].ToString().ToLower() == "true")
                {
                    keyMoments.Add(new KeyMoment
                    {
                        timestamp = action.timestamp,
                        momentType = "Discovery",
                        description = $"Player discovered new content: {action.actionType}",
                        significance = 0.8f
                    });
                }
            }

            // Identify mastery moments (consistent success in complex actions)
            var complexActions = currentSessionActions.Where(a =>
                a.actionType.Contains("Breeding") || a.actionType.Contains("Research")).ToList();

            if (complexActions.Count >= 3)
            {
                var recentComplex = complexActions.TakeLast(3).ToList();
                var allSuccessful = recentComplex.All(a =>
                    a.parameters != null &&
                    a.parameters.ContainsKey(ParamKey.Success) &&
                    a.parameters[ParamKey.Success].ToString().ToLower() == "true");

                if (allSuccessful)
                {
                    keyMoments.Add(new KeyMoment
                    {
                        timestamp = recentComplex.Last().timestamp,
                        momentType = "Mastery",
                        description = "Player demonstrates mastery of complex mechanics",
                        significance = 0.9f
                    });
                }
            }

            // Identify engagement spikes (high activity periods)
            var timeWindows = currentSessionActions
                .GroupBy(a => Mathf.FloorToInt(a.sessionTime / 300f)) // 5-minute windows
                .Where(g => g.Count() > 15) // High activity threshold
                .ToList();

            foreach (var window in timeWindows)
            {
                var windowActions = window.ToList();
                keyMoments.Add(new KeyMoment
                {
                    timestamp = windowActions.First().timestamp,
                    momentType = "EngagementSpike",
                    description = $"High engagement period with {windowActions.Count} actions in 5 minutes",
                    significance = Mathf.Clamp01(windowActions.Count / 25f)
                });
            }

            return keyMoments.OrderByDescending(k => k.significance).Take(5).ToList();
        }

        private string DetermineAdaptationReason()
        {
            var recentActions = GetRecentActions(600f); // Last 10 minutes
            if (recentActions.Count == 0) return "Insufficient recent activity";

            // Analyze recent performance
            var actionsWithResults = recentActions.Where(a =>
                a.parameters != null && a.parameters.ContainsKey(ParamKey.Success)).ToList();

            if (actionsWithResults.Count > 0)
            {
                var successfulActions = actionsWithResults.Count(a =>
                    a.parameters[ParamKey.Success].ToString().ToLower() == "true");
                float recentSuccessRate = (float)successfulActions / actionsWithResults.Count;

                if (recentSuccessRate < 0.3f)
                {
                    return "Low success rate detected - player may need assistance or reduced difficulty";
                }
                else if (recentSuccessRate > 0.9f)
                {
                    return "High success rate detected - player may benefit from increased challenge";
                }
            }

            // Analyze engagement patterns
            float actionsPerMinute = recentActions.Count / 10f;
            if (actionsPerMinute < 0.5f)
            {
                return "Low activity detected - player may be disengaged or need new content";
            }
            else if (actionsPerMinute > 3f)
            {
                return "High activity detected - player is highly engaged";
            }

            // Analyze behavior pattern changes
            var currentArchetype = DetermineDominantArchetype();
            if (currentPlayerProfile.dominantArchetype != currentArchetype)
            {
                return $"Behavior shift detected - player transitioning from {currentPlayerProfile.dominantArchetype} to {currentArchetype}";
            }

            // Analyze trait score changes
            var significantTraitChanges = behaviorScores.Where(kvp =>
                Mathf.Abs(kvp.Value - 0.5f) > 0.3f).ToList();

            if (significantTraitChanges.Count > 0)
            {
                var dominantTrait = significantTraitChanges.OrderByDescending(kvp => kvp.Value).First();
                return $"Strong {dominantTrait.Key.ToString().ToLower()} behavior pattern emerging";
            }

            return "Routine behavior analysis and optimization";
        }

        private Dictionary<SystemType, float> CalculateExpectedImpact(AdaptationType adaptationType, float intensity)
        {
            // Calculate expected impact on different systems
            var impact = new Dictionary<SystemType, float>();
            impact[SystemType.Analytics] = intensity * 0.8f;
            impact[SystemType.AI] = intensity * 0.6f;
            impact[SystemType.UI] = intensity * 0.4f;
            return impact;
        }

        private void ApplyGameAdaptation(PlayerAdaptation adaptation)
        {
            var insights = GenerateBehaviorInsights();
            var engagement = CalculateEngagementMetrics();
            var performance = CalculatePerformanceMetrics();

            // Adapt difficulty based on performance
            if (performance.successRate < 0.3f && engagement.frustrationLevel > 0.7f)
            {
                // Suggest easier content
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "DifficultyAdaptation",
                    [ParamKey.InteractionType] = "SuggestEasier",
                    [ParamKey.Trigger] = "LowPerformance"
                });
            }
            else if (performance.successRate > 0.8f && engagement.flowState > 0.8f)
            {
                // Suggest more challenging content
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "DifficultyAdaptation",
                    [ParamKey.InteractionType] = "SuggestHarder",
                    [ParamKey.Trigger] = "HighPerformance"
                });
            }

            // Adapt content based on play style
            var playStyle = AnalyzePlayStyle();
            if (playStyle.explorationFocus > 0.7f)
            {
                // Highlight exploration opportunities
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "ContentRecommendation",
                    [ParamKey.InteractionType] = "HighlightExploration",
                    [ParamKey.Trigger] = "ExplorationPreference"
                });
            }
            else if (playStyle.socialFocus > 0.7f)
            {
                // Suggest social interactions
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "ContentRecommendation",
                    [ParamKey.InteractionType] = "SuggestSocial",
                    [ParamKey.Trigger] = "SocialPreference"
                });
            }
        }

        private void UpdateSessionMetrics()
        {
            if (sessionStartTime == null) return;

            var currentTime = DateTime.UtcNow;
            var sessionDuration = (float)(currentTime - sessionStartTime.Value).TotalMinutes;

            // Update session-level metrics
            sessionMetrics[ParamKey.SessionTime] = sessionDuration;
            sessionMetrics[ParamKey.TotalActions] = totalActionCount;

            // Calculate actions per minute
            var actionsPerMinute = sessionDuration > 0 ? totalActionCount / sessionDuration : 0f;
            sessionMetrics[ParamKey.FrameRate] = actionsPerMinute; // Repurpose for action rate

            // Update peak concurrent activity
            var currentActivity = actionHistory.Count(a => (currentTime - a.timestamp).TotalMinutes < 5);
            if (currentActivity > peakConcurrentActivity)
            {
                peakConcurrentActivity = currentActivity;
            }

            // Update behavior scores based on recent activity
            UpdateBehaviorScoresFromActions();

            // Track significant session milestones
            if (sessionDuration >= 30f && !sessionMilestones.Contains("LongSession"))
            {
                sessionMilestones.Add("LongSession");
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "SessionMilestone",
                    [ParamKey.InteractionType] = "LongSession",
                    [ParamKey.TimeSpent] = sessionDuration
                });
            }

            if (totalActionCount >= 100 && !sessionMilestones.Contains("HighActivity"))
            {
                sessionMilestones.Add("HighActivity");
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "SessionMilestone",
                    [ParamKey.InteractionType] = "HighActivity",
                    [ParamKey.TotalActions] = totalActionCount
                });
            }
        }

        private List<PlayerAction> GetRecentActions(float timeWindow)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-timeWindow);
            return actionHistory.Where(a => a.timestamp >= cutoffTime).ToList();
        }

        private void UpdateBehaviorScoresFromActions(List<PlayerAction> actions)
        {
            if (actions == null || actions.Count == 0) return;

            // Analyze action patterns to update behavior scores
            var actionCounts = actions.GroupBy(a => a.actionType)
                                     .ToDictionary(g => g.Key, g => g.Count());

            var totalActions = actions.Count;

            // Update behavior scores based on action distribution
            if (actionCounts.ContainsKey(ActionType.Exploration))
            {
                var explorationRatio = (float)actionCounts[ActionType.Exploration] / totalActions;
                behaviorScores[TraitType.Curiosity] = Mathf.Lerp(
                    behaviorScores.GetValueOrDefault(TraitType.Curiosity, 0.5f),
                    explorationRatio,
                    0.1f
                );
            }

            if (actionCounts.ContainsKey(ActionType.Social))
            {
                var socialRatio = (float)actionCounts[ActionType.Social] / totalActions;
                behaviorScores[TraitType.Sociability] = Mathf.Lerp(
                    behaviorScores.GetValueOrDefault(TraitType.Sociability, 0.5f),
                    socialRatio,
                    0.1f
                );
            }

            if (actionCounts.ContainsKey(ActionType.Combat))
            {
                var combatRatio = (float)actionCounts[ActionType.Combat] / totalActions;
                behaviorScores[TraitType.Aggression] = Mathf.Lerp(
                    behaviorScores.GetValueOrDefault(TraitType.Aggression, 0.5f),
                    combatRatio,
                    0.1f
                );
            }

            if (actionCounts.ContainsKey(ActionType.Research))
            {
                var researchRatio = (float)actionCounts[ActionType.Research] / totalActions;
                behaviorScores[TraitType.Intelligence] = Mathf.Lerp(
                    behaviorScores.GetValueOrDefault(TraitType.Intelligence, 0.5f),
                    researchRatio,
                    0.1f
                );
            }

            // Analyze action timing patterns for caution/adaptability
            var actionIntervals = new List<float>();
            for (int i = 1; i < actions.Count; i++)
            {
                var interval = actions[i].timestamp - actions[i-1].timestamp;
                actionIntervals.Add(interval);
            }

            if (actionIntervals.Count > 0)
            {
                var avgInterval = actionIntervals.Average();
                var cautionScore = Mathf.Clamp01(avgInterval / 10f); // Longer intervals = more cautious
                behaviorScores[TraitType.Caution] = Mathf.Lerp(
                    behaviorScores.GetValueOrDefault(TraitType.Caution, 0.5f),
                    cautionScore,
                    0.05f
                );

                var intervalVariance = actionIntervals.Select(i => Mathf.Pow(i - avgInterval, 2)).Average();
                var adaptabilityScore = Mathf.Clamp01(intervalVariance / 25f); // Higher variance = more adaptable
                behaviorScores[TraitType.Adaptability] = Mathf.Lerp(
                    behaviorScores.GetValueOrDefault(TraitType.Adaptability, 0.5f),
                    adaptabilityScore,
                    0.05f
                );
            }
        }

        private void UpdatePlayerProfile()
        {
            var currentArchetype = DetermineDominantArchetype();
            var playStyle = AnalyzePlayStyle();
            var engagement = CalculateEngagementMetrics();
            var performance = CalculatePerformanceMetrics();

            // Update player archetype if it has changed significantly
            if (currentPlayerArchetype != currentArchetype)
            {
                var previousArchetype = currentPlayerArchetype;
                currentPlayerArchetype = currentArchetype;

                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "PlayerArchetypeChange",
                    [ParamKey.InteractionType] = "ArchetypeEvolution",
                    [ParamKey.ChoiceCategory] = ChoiceCategory.SocialInteraction,
                    [ParamKey.ChoiceValue] = currentArchetype.ToString()
                });
            }

            // Update skill progression
            var currentSkillLevel = DetermineSkillLevel();
            if (currentSkillLevel > playerSkillLevel)
            {
                var skillGain = currentSkillLevel - playerSkillLevel;
                playerSkillLevel = currentSkillLevel;

                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "SkillProgression",
                    [ParamKey.InteractionType] = "SkillLevelUp",
                    [ParamKey.Progress] = playerSkillLevel,
                    [ParamKey.Intensity] = skillGain
                });
            }

            // Update preferences based on behavior patterns
            var preferredBiome = DeterminePreferredBiome();
            if (preferredBiome != mostPreferredBiome)
            {
                mostPreferredBiome = preferredBiome;
                TrackAction(ActionType.Exploration, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "BiomePreference",
                    [ParamKey.InteractionType] = "PreferenceChange",
                    [ParamKey.Biome] = preferredBiome.ToString(),
                    [ParamKey.FirstDiscovery] = false
                });
            }

            // Update social interaction preferences
            if (playStyle.socialFocus > 0.6f && !playerPreferences.Contains("Social"))
            {
                playerPreferences.Add("Social");
            }
            else if (playStyle.socialFocus < 0.3f && playerPreferences.Contains("Social"))
            {
                playerPreferences.Remove("Social");
            }

            // Update exploration preferences
            if (playStyle.explorationFocus > 0.6f && !playerPreferences.Contains("Explorer"))
            {
                playerPreferences.Add("Explorer");
            }
            else if (playStyle.explorationFocus < 0.3f && playerPreferences.Contains("Explorer"))
            {
                playerPreferences.Remove("Explorer");
            }

            // Update competitive preferences
            if (playStyle.competitiveFocus > 0.6f && !playerPreferences.Contains("Competitive"))
            {
                playerPreferences.Add("Competitive");
            }
            else if (playStyle.competitiveFocus < 0.3f && playerPreferences.Contains("Competitive"))
            {
                playerPreferences.Remove("Competitive");
            }

            currentPlayerProfile.lastUpdated = Time.time;
        }

        private void GenerateRealTimeInsights()
        {
            var insights = GenerateRealTimeInsights();

            // Apply insights to game systems
            if (insights.riskFactors.Any(r => r.Contains("frustration") || r.Contains("disengagement")))
            {
                // Trigger adaptive difficulty adjustment
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "AdaptiveDifficulty",
                    [ParamKey.InteractionType] = "FrustrationResponse",
                    [ParamKey.Trigger] = "RealTimeAnalysis"
                });
            }

            if (insights.strengthAreas.Any(s => s.Contains("flow") || s.Contains("performance")))
            {
                // Celebrate player success and suggest progression
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "ProgressionSuggestion",
                    [ParamKey.InteractionType] = "SuccessRecognition",
                    [ParamKey.Trigger] = "HighPerformance"
                });
            }

            // Update recommendations based on insights
            foreach (var recommendation in insights.recommendedActions)
            {
                TrackAction(ActionType.UI, new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "PersonalizedRecommendation",
                    [ParamKey.InteractionType] = "RealTimeInsight",
                    [ParamKey.ChoiceValue] = recommendation
                });
            }
        }

        private void InitializeInputMetrics()
        {
            inputMetrics = new Dictionary<ParamKey, object>();

            // Initialize input tracking metrics
            inputMetrics[ParamKey.TotalActions] = 0;
            inputMetrics[ParamKey.SessionTime] = 0f;
            inputMetrics[ParamKey.InteractionTime] = 0f;

            // Initialize input pattern tracking
            lastInputTime = null;
            inputPatterns = new List<InputPattern>();

            // Initialize device-specific metrics
            var inputDevices = new Dictionary<string, int>
            {
                ["Mouse"] = 0,
                ["Keyboard"] = 0,
                ["Gamepad"] = 0,
                ["Touch"] = 0
            };

            // Initialize gesture and interaction type tracking
            var gestureTypes = new Dictionary<string, int>
            {
                ["Click"] = 0,
                ["Drag"] = 0,
                ["Scroll"] = 0,
                ["Hover"] = 0,
                ["KeyPress"] = 0,
                ["KeyCombo"] = 0
            };

            // Initialize timing metrics
            var timingMetrics = new Dictionary<string, float>
            {
                ["AverageClickTime"] = 0f,
                ["AverageDragDistance"] = 0f,
                ["AverageHoverTime"] = 0f,
                ["InputFrequency"] = 0f,
                ["ResponseTime"] = 0f
            };

            // Initialize accuracy metrics
            var accuracyMetrics = new Dictionary<string, float>
            {
                ["ClickAccuracy"] = 1.0f,
                ["TargetMissRate"] = 0f,
                ["IntentionalActions"] = 1.0f,
                ["AccidentalActions"] = 0f
            };

            // Initialize behavioral pattern metrics
            var behaviorMetrics = new Dictionary<string, float>
            {
                ["Impulsiveness"] = 0.5f,
                ["Deliberateness"] = 0.5f,
                ["Exploration"] = 0.5f,
                ["Focus"] = 0.5f,
                ["Persistence"] = 0.5f
            };

            // Store all metrics in the main dictionary
            inputMetrics[ParamKey.ElementName] = inputDevices;
            inputMetrics[ParamKey.InteractionType] = gestureTypes;
            inputMetrics[ParamKey.Intensity] = timingMetrics;
            inputMetrics[ParamKey.Success] = accuracyMetrics;
            inputMetrics[ParamKey.EmotionalState] = behaviorMetrics;

            Debug.Log("Input metrics initialized for player analytics tracking");
        }

        private void SaveCurrentSession()
        {
            if (currentSession != null)
            {
                // Finalize current session
                currentSession.endTime = Time.time;
                currentSession.duration = currentSession.endTime - currentSession.startTime;
                currentSession.totalActions = currentSessionActions.Count;
                currentSession.analytics = AnalyzeCurrentSession();

                // Add to history
                sessionHistory.Add(currentSession);

                // Limit history size
                if (sessionHistory.Count > maxSessionHistoryLength)
                {
                    sessionHistory.RemoveAt(0);
                }

                // Save to file
                SaveSessionHistoryToFile();

                // Update player profile
                currentPlayerProfile.totalPlayTime += currentSession.duration;
                currentPlayerProfile.sessionsCompleted++;
                currentPlayerProfile.lastUpdated = Time.time;
                SavePlayerProfile(currentPlayerProfile);

                Debug.Log($"Session saved - Duration: {currentSession.duration:F1}s, Actions: {currentSession.totalActions}");
            }
        }

        private void SaveSessionHistoryToFile()
        {
            try
            {
                var wrapper = new SessionHistoryWrapper { sessions = sessionHistory };
                string json = JsonUtility.ToJson(wrapper, true);
                System.IO.File.WriteAllText(GetAnalyticsDataPath("session_history.json"), json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save session history: {e.Message}");
            }
        }
    }

    [System.Serializable]
    public class SessionHistoryWrapper
    {
        public List<GameplaySession> sessions;
    }

    // Analyzer classes (simplified for brevity)
    public class PlayerBehaviorAnalyzer
    {
        // Behavior analysis implementation
    }

    public class EngagementAnalyzer
    {
        public float CalculateCurrentEngagement() { return Random.Range(0.4f, 0.9f); }
    }

    public class PreferenceAnalyzer
    {
        public Dictionary<ActionType, float> GetCurrentPreferences()
        {
            return new Dictionary<ActionType, float>
            {
                [ActionType.Exploration] = Random.Range(0f, 1f),
                [ActionType.Breeding] = Random.Range(0f, 1f),
                [ActionType.Quest] = Random.Range(0f, 1f)
            };
        }
    }

    public class InputMetrics
    {
        public int totalInputs;
        public float averageInputRate;
        public Dictionary<ActionType, int> inputTypes = new Dictionary<ActionType, int>();
    }
}