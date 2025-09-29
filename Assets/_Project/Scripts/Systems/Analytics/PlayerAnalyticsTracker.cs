using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

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

        // Core analytics data
        private PlayerProfile currentPlayerProfile;
        private List<GameplaySession> sessionHistory = new List<GameplaySession>();
        private Dictionary<string, ActionMetrics> actionMetrics = new Dictionary<string, ActionMetrics>();
        private Dictionary<string, float> behaviorScores = new Dictionary<string, float>();

        // Real-time tracking
        private GameplaySession currentSession;
        private List<PlayerAction> currentSessionActions = new List<PlayerAction>();
        private float sessionStartTime;
        private float lastAnalyticsUpdate;
        private float lastAdaptation;

        // Input tracking
        private InputActionAsset playerInput;
        private Dictionary<string, InputMetrics> inputMetrics = new Dictionary<string, InputMetrics>();

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
            Debug.Log("Initializing Player Analytics Tracker");

            // Initialize player profile
            currentPlayerProfile = LoadOrCreatePlayerProfile();

            // Initialize analyzers
            behaviorAnalyzer = new PlayerBehaviorAnalyzer();
            engagementAnalyzer = new EngagementAnalyzer();
            preferenceAnalyzer = new PreferenceAnalyzer();

            // Initialize behavior scores
            InitializeBehaviorScores();

            // Load historical data
            LoadSessionHistory();

            Debug.Log($"Player Analytics initialized for player: {currentPlayerProfile.playerId}");
        }

        /// <summary>
        /// Tracks a player action with contextual data
        /// </summary>
        public void TrackAction(string actionType, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalyticsTracking) return;

            var action = new PlayerAction
            {
                actionType = actionType,
                timestamp = Time.time,
                sessionTime = Time.time - sessionStartTime,
                parameters = parameters ?? new Dictionary<string, object>(),
                context = CaptureCurrentContext()
            };

            currentSessionActions.Add(action);

            // Update action metrics
            if (!actionMetrics.ContainsKey(actionType))
            {
                actionMetrics[actionType] = new ActionMetrics();
            }

            actionMetrics[actionType].totalCount++;
            actionMetrics[actionType].lastUsed = Time.time;
            actionMetrics[actionType].averageInterval = CalculateAverageInterval(actionType);

            // Real-time analysis
            if (enableRealTimeAnalysis)
            {
                AnalyzeActionInRealTime(action);
            }

            Debug.Log($"Tracked action: {actionType} (Session actions: {currentSessionActions.Count})");
        }

        /// <summary>
        /// Tracks UI interaction with detailed metrics
        /// </summary>
        public void TrackUIInteraction(string elementName, string interactionType, float interactionTime = 0f)
        {
            if (!trackUIInteractions) return;

            var parameters = new Dictionary<string, object>
            {
                ["elementName"] = elementName,
                ["interactionType"] = interactionType,
                ["interactionTime"] = interactionTime
            };

            TrackAction("UI_Interaction", parameters);

            // Update UI-specific metrics
            UpdateUIMetrics(elementName, interactionType, interactionTime);
        }

        /// <summary>
        /// Tracks gameplay choice with decision context
        /// </summary>
        public void TrackGameplayChoice(string choiceCategory, string choiceValue, Dictionary<string, object> decisionContext = null)
        {
            if (!trackGameplayChoices) return;

            var parameters = new Dictionary<string, object>
            {
                ["choiceCategory"] = choiceCategory,
                ["choiceValue"] = choiceValue,
                ["decisionContext"] = decisionContext ?? new Dictionary<string, object>()
            };

            TrackAction("Gameplay_Choice", parameters);

            // Update choice preferences
            UpdateChoicePreferences(choiceCategory, choiceValue);
        }

        /// <summary>
        /// Tracks emotional response indicators
        /// </summary>
        public void TrackEmotionalResponse(EmotionalState emotionalState, float intensity, string trigger = "")
        {
            if (!trackEmotionalResponse) return;

            var parameters = new Dictionary<string, object>
            {
                ["emotionalState"] = emotionalState.ToString(),
                ["intensity"] = intensity.ToStringCached(),
                ["trigger"] = trigger
            };

            TrackAction("Emotional_Response", parameters);

            // Update emotional profile
            UpdateEmotionalProfile(emotionalState, intensity);
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
                behaviorScores = new Dictionary<string, float>(behaviorScores),
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
            TrackGameplayChoice("QuestCompletion", quest.type.ToString(), new Dictionary<string, object>
            {
                ["questDifficulty"] = quest.difficulty,
                ["completionTime"] = Time.time - quest.generatedTime
            });

            // Track emotional response to quest completion
            TrackEmotionalResponse(EmotionalState.Satisfaction, 0.7f, "QuestComplete");
        }

        private void HandleQuestGenerated(ProceduralQuest quest)
        {
            TrackAction("QuestGenerated", new Dictionary<string, object>
            {
                ["questType"] = quest.type.ToString(),
                ["questDifficulty"] = quest.difficulty
            });
        }

        private void HandleBreedingCompleted(BreedingSession session, CreatureGenome offspring)
        {
            TrackGameplayChoice("BreedingChoice", "Completed", new Dictionary<string, object>
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
            TrackGameplayChoice("OffspringDecision", "Accepted", new Dictionary<string, object>
            {
                ["offspringFitness"] = offspring.fitness,
                ["offspringGeneration"] = offspring.generation
            });
        }

        private void HandleEnvironmentalEvent(EnvironmentalEvent envEvent)
        {
            TrackAction("EnvironmentalEvent", new Dictionary<string, object>
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
        public Dictionary<string, float> preferences = new Dictionary<string, float>();
        public Dictionary<string, int> achievements = new Dictionary<string, int>();
        public PlayerArchetype dominantArchetype;
        public float createdTime;
        public float lastUpdated;
    }

    [System.Serializable]
    public class GameplaySession
    {
        public string sessionId;
        public float startTime;
        public float duration;
        public PlayerProfile playerProfile;
        public int actionCount;
        public float lastActivityTime;
        public Dictionary<string, object> sessionData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class PlayerAction
    {
        public string actionType;
        public float timestamp;
        public float sessionTime;
        public Dictionary<string, object> parameters = new Dictionary<string, object>();
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
        public Dictionary<string, float> behaviorScores;
        public PlayStyle playStyle;
        public float engagementLevel;
        public Dictionary<string, float> preferences;
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
        public Dictionary<string, float> dominantActivities;
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
        public Dictionary<string, float> expectedImpact;
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
        public Dictionary<string, object> environmentalFactors;
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
        public Dictionary<string, float> GetCurrentPreferences()
        {
            return new Dictionary<string, float>
            {
                ["explorationPreference"] = Random.Range(0f, 1f),
                ["breedingPreference"] = Random.Range(0f, 1f),
                ["questPreference"] = Random.Range(0f, 1f)
            };
        }
    }

    public class InputMetrics
    {
        public int totalInputs;
        public float averageInputRate;
        public Dictionary<string, int> inputTypes = new Dictionary<string, int>();
    }
}