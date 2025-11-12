using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Laboratory.Core;
using Laboratory.Shared.Types;
using Laboratory.Core.Enums;
using Laboratory.Core.Events;
using Laboratory.Core.Debug;
using Laboratory.Systems.Quests;
using Laboratory.Systems.Breeding;
using Laboratory.Systems.Ecosystem;

namespace Laboratory.Systems.Analytics
{
    /// <summary>
    /// Advanced player analytics system that tracks gameplay patterns,
    /// emotional responses, and adaptation metrics to provide personalized
    /// gaming experiences and behavioral insights.
    /// </summary>
    public class PlayerAnalyticsTracker : MonoBehaviour
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableAdaptiveGameplay = true;
        [SerializeField] private bool enableBehaviorAnalysis = true;
        [SerializeField] private float adaptationResponseThreshold = 0.7f;

        [Header("Data Collection Settings")]
        [SerializeField] private float actionTrackingInterval = 0.1f;
        [SerializeField] private int maxSessionActions = 10000;
        [SerializeField] private bool trackDetailedInputMetrics = true;
        [SerializeField] private bool enableRealTimeAnalysis = true;

        [Header("Privacy Settings")]
        [SerializeField] private bool anonymizePlayerData = true;
        [SerializeField] private bool enableDataExport = false;

        [Header("Adaptation Settings")]
        [SerializeField] private float difficultyAdaptationSensitivity = 0.5f;
        [SerializeField] private bool enableContentRecommendations = true;

        [Header("Behavioral Analysis")]
        [SerializeField] private bool enablePersonalityProfiling = true;
        [SerializeField] private bool enableEmotionalAnalysis = true;
        [SerializeField] private float behaviorAnalysisInterval = 30f;

        // Events for external systems
        public System.Action<PlayerArchetype> OnPlayerArchetypeIdentified;
        public System.Action<BehaviorInsight> OnBehaviorInsightGenerated;

        // Core Analytics Data
        private PlayerProfile currentPlayerProfile;
        private GameplaySession currentSession;
        private List<PlayerAction> currentSessionActions = new List<PlayerAction>();
        private List<GameplaySession> sessionHistory = new List<GameplaySession>();

        // Real-time Analysis
        private Dictionary<MetricType, float> currentMetrics = new Dictionary<MetricType, float>();
        private Dictionary<PlayerBehaviorTrait, float> behaviorTraits = new Dictionary<PlayerBehaviorTrait, float>();
        private Queue<PlayerAction> recentActions = new Queue<PlayerAction>(100);

        // Adaptation System
        private GameAdaptationEngine adaptationEngine;
        private List<MilestoneType> sessionMilestones = new List<MilestoneType>();
        private List<string> currentInsights = new List<string>();

        // Performance Tracking
        private float sessionStartTime;
        private int totalActionCount;
        private Dictionary<ActionType, int> actionTypeCounters = new Dictionary<ActionType, int>();
        private Dictionary<SessionDataKey, Dictionary<string, object>> sessionData = new Dictionary<SessionDataKey, Dictionary<string, object>>();

        // Input Analytics
        private InputMetricsTracker inputTracker;
        private Dictionary<InputType, float> inputPatterns = new Dictionary<InputType, float>();

        // Emotional State Tracking
        private Dictionary<EmotionalState, float> emotionalHistory = new Dictionary<EmotionalState, float>();
        private float lastEmotionalUpdate;

        // Integration Hooks
        private PersonalityProfiler personalityProfiler;
        private List<string> playerPreferences = new List<string>();

        #region Initialization & Lifecycle

        void Awake()
        {
            InitializeAnalyticsSystem();
        }

        void Start()
        {
            if (enableAnalytics)
            {
                StartNewGameplaySession();
                Debug.Log("Initializing Player Analytics Tracker - Performance Optimized");
            }
        }

        void OnDestroy()
        {
            if (currentSession != null)
            {
                EndCurrentSession();
            }
            SaveAnalyticsData();
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && currentSession != null)
            {
                currentSession.pauseTime = Time.time;
            }
            else if (!pauseStatus && currentSession != null && currentSession.pauseTime > 0)
            {
                currentSession.totalPauseTime += Time.time - currentSession.pauseTime;
            }
        }

        #endregion

        #region Core Analytics System

        private void InitializeAnalyticsSystem()
        {
            // Load or create player profile
            LoadPlayerProfile();

            // Initialize sub-systems
            adaptationEngine = new GameAdaptationEngine();
            inputTracker = new InputMetricsTracker();
            personalityProfiler = new PersonalityProfiler();

            // Initialize behavior trait tracking
            InitializeBehaviorTraits();

            // Initialize emotional state tracking
            InitializeEmotionalTracking();

            if (currentPlayerProfile != null)
            {
                Debug.Log($"Player Analytics initialized for player: {currentPlayerProfile.playerId}");
            }
        }

        private void InitializeBehaviorTraits()
        {
            foreach (PlayerBehaviorTrait trait in System.Enum.GetValues(typeof(PlayerBehaviorTrait)))
            {
                behaviorTraits[trait] = 0.5f; // Start neutral
            }
        }

        private void InitializeEmotionalTracking()
        {
            foreach (EmotionalState state in System.Enum.GetValues(typeof(EmotionalState)))
            {
                emotionalHistory[state] = 0f;
            }
        }

        #endregion

        #region Session Data Helpers

        private void InitializeSessionDataCategory(SessionDataKey category)
        {
            if (!sessionData.ContainsKey(category))
            {
                sessionData[category] = new Dictionary<string, object>();
            }
        }

        private void AddToSessionDataList(SessionDataKey category, string key, object value)
        {
            InitializeSessionDataCategory(category);
            var categoryData = sessionData[category];

            if (!categoryData.ContainsKey(key))
            {
                categoryData[key] = new List<object>();
            }

            ((List<object>)categoryData[key]).Add(value);
        }

        private void IncrementSessionDataCounter(SessionDataKey category, string key)
        {
            InitializeSessionDataCategory(category);
            var categoryData = sessionData[category];

            if (!categoryData.ContainsKey(key))
            {
                categoryData[key] = 0;
            }

            categoryData[key] = (int)categoryData[key] + 1;
        }

        private int GetSessionDataCounter(SessionDataKey category, string key)
        {
            if (!sessionData.ContainsKey(category) || !sessionData[category].ContainsKey(key))
            {
                return 0;
            }

            return (int)sessionData[category][key];
        }

        private List<object> GetSessionDataList(SessionDataKey category, string key)
        {
            if (!sessionData.ContainsKey(category) || !sessionData[category].ContainsKey(key))
            {
                return new List<object>();
            }

            return (List<object>)sessionData[category][key];
        }

        #endregion

        #region Action Tracking

        private ActionType ParseActionType(string actionTypeString)
        {
            if (System.Enum.TryParse<ActionType>(actionTypeString, true, out ActionType actionType))
            {
                return actionType;
            }
            // Default fallback for unknown action types
            return ActionType.Exploration;
        }

        public void TrackPlayerAction(string actionType, string context, Dictionary<ParamKey, object> parameters = null)
        {
            if (!enableAnalytics) return;

            var action = new PlayerAction
            {
                actionType = ParseActionType(actionType),
                timestamp = Time.time,
                parameters = parameters ?? new Dictionary<ParamKey, object>()
            };

            // Add context information to parameters
            action.parameters[ParamKey.DecisionContext] = context;
            action.parameters[ParamKey.TimeSpent] = Time.time - sessionStartTime;

            currentSessionActions.Add(action);
            recentActions.Enqueue(action);

            if (recentActions.Count > 100)
                recentActions.Dequeue();

            // Update counters
            totalActionCount++;

            // Convert string actionType to enum
            if (System.Enum.TryParse<ActionType>(actionType, true, out ActionType actionEnum))
            {
                if (!actionTypeCounters.ContainsKey(actionEnum))
                    actionTypeCounters[actionEnum] = 0;
                actionTypeCounters[actionEnum]++;
            }

            Debug.Log($"Tracked action: {actionType} (Session actions: {currentSessionActions.Count})");

            // Real-time analysis
            if (enableRealTimeAnalysis)
            {
                AnalyzeActionInRealTime(action);
            }

            // Check for behavior pattern changes
            UpdateBehaviorTraits(action);

            // Check for adaptive responses
            if (enableAdaptiveGameplay)
            {
                CheckForAdaptationTriggers(action);
            }
        }

        public void TrackGameplayChoice(ChoiceCategory category, string choice, Dictionary<ParamKey, object> context = null)
        {
            var parameters = context ?? new Dictionary<ParamKey, object>();
            parameters[ParamKey.Category] = category;
            parameters[ParamKey.Choice] = choice;

            TrackPlayerAction($"Choice_{category}", "DecisionMaking", parameters);

            // Analyze choice pattern
            AnalyzeChoicePattern(category, choice, context);
        }

        public void TrackUIInteraction(string elementName, string interactionType, Dictionary<ParamKey, object> context = null)
        {
            var parameters = context ?? new Dictionary<ParamKey, object>();
            parameters[ParamKey.ElementName] = elementName;
            parameters[ParamKey.InteractionType] = interactionType;

            TrackPlayerAction("UI_Interaction", "UserInterface", parameters);

            // Update UI usage patterns
            UpdateUIPatterns(elementName, interactionType);
        }

        public void TrackEmotionalResponse(EmotionalState emotionalState, float intensity, string trigger = "")
        {
            emotionalHistory[emotionalState] = Mathf.Max(emotionalHistory[emotionalState], intensity);
            lastEmotionalUpdate = Time.time;

            var parameters = new Dictionary<ParamKey, object>
            {
                [ParamKey.EmotionalState] = emotionalState,
                [ParamKey.Intensity] = intensity,
                [ParamKey.Trigger] = trigger
            };

            TrackPlayerAction("Emotional_Response", "EmotionalSystem", parameters);

            // Update personality profile if available
            if (personalityProfiler != null)
            {
                personalityProfiler.RecordEmotionalResponse(emotionalState, intensity);
            }
        }

        #endregion

        #region Behavior Analysis

        private void AnalyzeActionInRealTime(PlayerAction action)
        {
            // Update real-time metrics
            UpdateRealTimeMetrics(action);

            // Detect behavior patterns
            DetectBehaviorPatterns(action);

            // Update player archetype
            UpdatePlayerArchetype(action);
        }

        private void UpdateRealTimeMetrics(PlayerAction action)
        {
            // Calculate session metrics
            float sessionDuration = Time.time - sessionStartTime;
            currentMetrics[MetricType.SessionDuration] = sessionDuration;
            currentMetrics[MetricType.ActionsPerMinute] = totalActionCount / Mathf.Max(sessionDuration / 60f, 0.1f);
            currentMetrics[MetricType.OverallEngagement] = recentActions.Count;

            // Calculate performance metrics
            var recentSuccesses = recentActions.Count(a =>
                a.parameters.ContainsKey(ParamKey.Success) &&
                a.parameters[ParamKey.Success].ToString().ToLower() == "true");
            currentMetrics[MetricType.RecentSuccessRate] = recentActions.Count > 0 ?
                (float)recentSuccesses / recentActions.Count : 0.5f;
        }

        private void DetectBehaviorPatterns(PlayerAction action)
        {
            // Detect repetitive behavior
            var recentSimilarActions = recentActions.Count(a => a.actionType == action.actionType);
            if (recentSimilarActions > 10)
            {
                behaviorTraits[PlayerBehaviorTrait.Repetitive] = Mathf.Min(1f, behaviorTraits[PlayerBehaviorTrait.Repetitive] + 0.1f);
            }

            // Detect exploration behavior
            var uniqueActionTypes = recentActions.Select(a => a.actionType).Distinct().Count();
            if (uniqueActionTypes > 5)
            {
                behaviorTraits[PlayerBehaviorTrait.Exploratory] = Mathf.Min(1f, behaviorTraits[PlayerBehaviorTrait.Exploratory] + 0.05f);
            }

            // Detect focused behavior
            var dominantActionType = recentActions.GroupBy(a => a.actionType)
                .OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
            if (dominantActionType != null)
            {
                var dominantCount = recentActions.Count(a => a.actionType == dominantActionType);
                if (dominantCount > recentActions.Count * 0.6f)
                {
                    behaviorTraits[PlayerBehaviorTrait.Focused] = Mathf.Min(1f, behaviorTraits[PlayerBehaviorTrait.Focused] + 0.05f);
                }
            }
        }

        private void UpdatePlayerArchetype(PlayerAction action)
        {
            ArchetypeType currentArchetype = DeterminePlayerArchetype();

            if (currentPlayerProfile.dominantArchetype != currentArchetype)
            {
                currentPlayerProfile.previousArchetypes.Add(currentPlayerProfile.dominantArchetype);
                currentPlayerProfile.dominantArchetype = currentArchetype;
                currentPlayerProfile.archetypeUpdateTime = Time.time;

                OnPlayerArchetypeChanged(currentArchetype);
            }
        }

        #endregion

        #region Player Preferences & Insights

        /// <summary>
        /// Gets player's preferred biomes based on analytics
        /// </summary>
        public Dictionary<BiomeType, float> GetPlayerBiomePreferences()
        {
            var biomeActions = currentSessionActions.Where(a => a.actionType == ActionType.Exploration);
            var biomePreferences = new Dictionary<BiomeType, float>();

            foreach (var action in biomeActions)
            {
                if (action.parameters.ContainsKey(ParamKey.Biome))
                {
                    if (action.parameters[ParamKey.Biome] is BiomeType biomeType)
                    {
                        float timeSpent = action.parameters.ContainsKey(ParamKey.TimeSpent) ?
                            float.Parse(action.parameters[ParamKey.TimeSpent].ToString()) : 1f;

                        if (!biomePreferences.ContainsKey(biomeType))
                            biomePreferences[biomeType] = 0f;

                        biomePreferences[biomeType] += timeSpent;
                    }
                }
            }

            // Normalize preferences
            float total = biomePreferences.Values.Sum();
            if (total > 0)
            {
                var normalizedPrefs = new Dictionary<BiomeType, float>();
                foreach (var pref in biomePreferences)
                {
                    normalizedPrefs[pref.Key] = pref.Value / total;
                }
                return normalizedPrefs;
            }

            return biomePreferences;
        }

        /// <summary>
        /// Gets player's preferred creature types based on interactions
        /// </summary>
        public Dictionary<string, float> GetPlayerCreaturePreferences()
        {
            var creatureActions = currentSessionActions.Where(a => a.actionType == ActionType.Social || a.actionType == ActionType.Breeding);
            var creaturePreferences = new Dictionary<string, float>();

            foreach (var action in creatureActions)
            {
                if (action.parameters.ContainsKey(ParamKey.Species))
                {
                    string species = action.parameters[ParamKey.Species].ToString();
                    creaturePreferences[species] = creaturePreferences.GetValueOrDefault(species, 0f) + 1f;
                }
                if (action.parameters.ContainsKey(ParamKey.ParentSpecies1))
                {
                    string species = action.parameters[ParamKey.ParentSpecies1].ToString();
                    creaturePreferences[species] = creaturePreferences.GetValueOrDefault(species, 0f) + 0.5f;
                }
                if (action.parameters.ContainsKey(ParamKey.ParentSpecies2))
                {
                    string species = action.parameters[ParamKey.ParentSpecies2].ToString();
                    creaturePreferences[species] = creaturePreferences.GetValueOrDefault(species, 0f) + 0.5f;
                }
            }

            return creaturePreferences;
        }

        #endregion

        #region Adaptive Gameplay

        private void CheckForAdaptationTriggers(PlayerAction action)
        {
            // Check for difficulty adaptation needs
            float successRate = currentMetrics.GetValueOrDefault(MetricType.RecentSuccessRate, 0.5f);

            if (successRate < 0.3f && Time.time - currentPlayerProfile.lastAdaptation > 60f)
            {
                TriggerGameAdaptation("DifficultyReduction", 0.3f);
            }
            else if (successRate > 0.8f && Time.time - currentPlayerProfile.lastAdaptation > 120f)
            {
                TriggerGameAdaptation("DifficultyIncrease", 0.2f);
            }

            // Check for content recommendation triggers
            CheckContentRecommendationTriggers();
        }

        private void CheckContentRecommendationTriggers()
        {
            float sessionDuration = Time.time - sessionStartTime;

            // Long session without exploration
            if (sessionDuration > 300f) // 5 minutes
            {
                var explorationActions = currentSessionActions.Count(a => a.actionType == ActionType.Exploration);
                if (explorationActions < 3)
                {
                    TriggerGameAdaptation("ExplorationRecommendation", 0.4f);
                }
            }
        }

        private void TriggerGameAdaptation(string adaptationType, float intensity)
        {
            currentPlayerProfile.lastAdaptation = Time.time;

            var adaptationData = new Dictionary<AdaptationKey, object>
            {
                [AdaptationKey.Type] = adaptationType,
                [AdaptationKey.Intensity] = intensity,
                [AdaptationKey.Trigger] = "AutomaticAnalysis",
                [AdaptationKey.PlayerArchetype] = currentPlayerProfile.dominantArchetype
            };

            Debug.Log($"Game adaptation triggered: {adaptationType} (Intensity: {intensity:F2})");

            // Notify adaptation engine
            adaptationEngine?.ProcessAdaptation(currentPlayerProfile);
        }

        #endregion

        #region Session Management

        private void StartNewGameplaySession()
        {
            currentSession = new GameplaySession
            {
                sessionId = (uint)UnityEngine.Random.Range(1000000, 9999999),
                startTime = Time.time,
                playerArchetype = currentPlayerProfile?.dominantArchetype ?? ArchetypeType.Unknown
            };

            sessionStartTime = Time.time;
            currentSessionActions.Clear();
            sessionMilestones.Clear();

            Debug.Log($"Started new gameplay session: {currentSession.sessionId}");
        }

        private void EndCurrentSession()
        {
            if (currentSession == null) return;

            currentSession.endTime = Time.time;
            currentSession.duration = currentSession.endTime - currentSession.startTime - currentSession.totalPauseTime;
            currentSession.totalActions = currentSessionActions.Count;
            currentSession.uniqueActionTypes = currentSessionActions.Select(a => a.actionType).Distinct().Count();
            currentSession.actions = new List<PlayerAction>(currentSessionActions);

            // Calculate session metrics
            currentSession.engagementMetrics = CalculateEngagementMetrics();
            currentSession.behaviorMetrics = CalculateBehaviorMetrics();
            currentSession.emotionalProfile = new Dictionary<EmotionalState, float>(emotionalHistory);

            // Add to history
            sessionHistory.Add(currentSession);

            // Update player profile
            UpdatePlayerProfileFromSession();

            currentSession = null;
        }

        private EngagementMetrics CalculateEngagementMetrics()
        {
            float sessionDuration = currentSession.duration;
            int actionCount = currentSessionActions.Count;

            return new EngagementMetrics
            {
                sessionDuration = sessionDuration,
                actionsPerMinute = actionCount / Mathf.Max(sessionDuration / 60f, 0.1f),
                overallEngagement = CalculateOverallEngagement(),
                focusLevel = CalculateFocusLevel(),
                explorationLevel = CalculateExplorationLevel()
            };
        }

        private float CalculateOverallEngagement()
        {
            // Combine multiple factors for engagement score
            float activityLevel = Mathf.Clamp01(currentSessionActions.Count / 100f);
            float varietyLevel = Mathf.Clamp01(currentSessionActions.Select(a => a.actionType).Distinct().Count() / 10f);
            float sessionLength = Mathf.Clamp01((Time.time - sessionStartTime) / 600f); // 10 minutes = full engagement

            return (activityLevel + varietyLevel + sessionLength) / 3f;
        }

        private float CalculateFocusLevel()
        {
            if (currentSessionActions.Count == 0) return 0.5f;

            var actionGroups = currentSessionActions.GroupBy(a => a.actionType);
            var largestGroup = actionGroups.OrderByDescending(g => g.Count()).FirstOrDefault();

            return largestGroup != null ? (float)largestGroup.Count() / currentSessionActions.Count : 0f;
        }

        private float CalculateExplorationLevel()
        {
            if (currentSessionActions.Count == 0) return 0f;

            return (float)currentSessionActions.Select(a => a.actionType).Distinct().Count() / currentSessionActions.Count;
        }

        #endregion

        #region Data Integration

        public void OnQuestCompleted(QuestData quest)
        {
            TrackPlayerAction("Quest_Completed", "Questing", new Dictionary<ParamKey, object>
            {
                [ParamKey.QuestId] = quest.questId,
                [ParamKey.QuestType] = quest.type,
                [ParamKey.QuestDifficulty] = quest.difficulty,
                [ParamKey.QuestGeneratedTime] = Time.time - quest.generatedTime
            });

            TrackEmotionalResponse(EmotionalState.Satisfaction, 0.7f, "QuestComplete");
        }

        public void OnQuestFailed(QuestData quest)
        {
            TrackPlayerAction("Quest_Failed", "Questing", new Dictionary<ParamKey, object>
            {
                [ParamKey.QuestId] = quest.questId,
                [ParamKey.QuestType] = quest.type,
                [ParamKey.QuestDifficulty] = quest.difficulty
            });

            TrackEmotionalResponse(EmotionalState.Frustration, 0.6f, "QuestFailure");
        }

        public void OnBreedingCompleted(BreedingSession session, CreatureInstance offspring)
        {
            TrackGameplayChoice(ChoiceCategory.BreedingChoice, "Completed", new Dictionary<ParamKey, object>
            {
                [ParamKey.ParentAFitness] = session.parentA.fitness,
                [ParamKey.ParentBFitness] = session.parentB.fitness,
                [ParamKey.OffspringFitness] = offspring.fitness,
                [ParamKey.BreedingTime] = session.completionTime - session.startTime
            });

            TrackEmotionalResponse(EmotionalState.Pride,
                Mathf.Lerp(0.4f, 0.9f, offspring.fitness),
                "BreedingResult"
            );
        }

        public void OnOffspringDecision(CreatureInstance offspring, bool accepted)
        {
            TrackGameplayChoice(ChoiceCategory.OffspringDecision, accepted ? "Accepted" : "Rejected", new Dictionary<ParamKey, object>
            {
                [ParamKey.OffspringFitness] = offspring.fitness,
                [ParamKey.Generation] = offspring.generation
            });
        }

        public void OnEnvironmentalEvent(EcosystemEvent envEvent)
        {
            TrackPlayerAction("Environmental_Event", "WorldEvents", new Dictionary<ParamKey, object>
            {
                [ParamKey.EventType] = envEvent.eventType
            });

            TrackEmotionalResponse(EmotionalState.Anticipation, 0.6f, "EnvironmentalEvent");
        }

        #endregion

        #region Debug and Editor Tools

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void OnGUI()
        {
            if (!Application.isPlaying || !enableAnalytics) return;

            // Simple debug display
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Session Actions: {currentSessionActions.Count}");
            GUILayout.Label($"Archetype: {currentPlayerProfile?.dominantArchetype.ToString() ?? "Unknown"}");
            GUILayout.Label($"Engagement: {CalculateOverallEngagement():F2}");
            GUILayout.EndArea();
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Analytics/Show Player Behavior Analysis", false, 500)]
        private static void ShowPlayerBehaviorAnalysis()
        {
            var tracker = FindObjectOfType<PlayerAnalyticsTracker>();
            if (tracker != null)
            {
                var analysis = tracker.GeneratePlayerBehaviorAnalysis();
                Debug.Log($"Player Behavior Analysis:\n" +
                         $"Dominant Archetype: {analysis.dominantArchetype}\n" +
                         $"Play Style: {analysis.playStyle}\n" +
                         $"Engagement Level: {analysis.engagementLevel:F2}\n" +
                         $"Active Insights: {analysis.insights.Count}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Analytics/Show Session Analytics", false, 501)]
        private static void ShowSessionAnalytics()
        {
            var tracker = FindObjectOfType<PlayerAnalyticsTracker>();
            if (tracker?.currentSession != null)
            {
                var analytics = tracker.CalculateEngagementMetrics();
                if (analytics != null)
                {
                    Debug.Log($"Session Analytics:\n" +
                             $"Duration: {analytics.sessionDuration:F1}s\n" +
                             $"Total Actions: {analytics.totalActions}\n" +
                             $"Actions/Min: {analytics.actionsPerMinute:F1}\n" +
                             $"Engagement: {analytics.overallEngagement:F2}");
                }
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Analytics/Trigger Test Adaptation", false, 502)]
        private static void TriggerTestAdaptation()
        {
            var tracker = FindObjectOfType<PlayerAnalyticsTracker>();
            if (tracker != null)
            {
                tracker.TriggerGameAdaptation("TestAdaptation", 0.5f);
                Debug.Log("Triggered test game adaptation");
            }
        }

        #endregion

        #region Advanced Analytics Methods

        /// <summary>
        /// Generates comprehensive behavior analysis
        /// </summary>
        public PlayerBehaviorAnalysis GeneratePlayerBehaviorAnalysis()
        {
            var analysis = new PlayerBehaviorAnalysis
            {
                playerId = currentPlayerProfile.playerId,
                dominantArchetype = currentPlayerProfile.dominantArchetype,
                playStyle = AnalyzePlayStyle(),
                engagementLevel = CalculateOverallEngagement(),
                behaviorTraits = new Dictionary<PlayerBehaviorTrait, float>(behaviorTraits),
                insights = GenerateInsights(),
                recommendations = GenerateRecommendations(),
                emotionalProfile = new Dictionary<EmotionalState, float>(emotionalHistory),
                sessionMetrics = currentMetrics
            };

            return analysis;
        }

        private PlayStyle AnalyzePlayStyle()
        {
            var recentActions = currentSessionActions.TakeLast(100).ToList();

            return new PlayStyle
            {
                explorationFocus = CalculateExplorationFocus(recentActions),
                breedingFocus = CalculateBreedingFocus(recentActions),
                questFocus = CalculateQuestFocus(recentActions),
                socialFocus = CalculateSocialFocus(recentActions),
                competitiveFocus = CalculateCompetitiveFocus(recentActions),
                creativeFocus = CalculateCreativeFocus(recentActions)
            };
        }

        private float CalculateExplorationFocus(List<PlayerAction> actions)
        {
            if (actions.Count == 0) return 0f;
            return (float)actions.Count(a => a.actionType == ActionType.Exploration) / actions.Count;
        }

        private float CalculateBreedingFocus(List<PlayerAction> actions)
        {
            if (actions.Count == 0) return 0f;
            return (float)actions.Count(a => a.actionType == ActionType.Breeding || a.actionType == ActionType.Research) / actions.Count;
        }

        private float CalculateQuestFocus(List<PlayerAction> actions)
        {
            if (actions.Count == 0) return 0f;
            return (float)actions.Count(a => a.actionType == ActionType.Quest) / actions.Count;
        }

        private float CalculateSocialFocus(List<PlayerAction> actions)
        {
            if (actions.Count == 0) return 0f;
            return (float)actions.Count(a => a.actionType == ActionType.Social) / actions.Count;
        }

        private float CalculateCompetitiveFocus(List<PlayerAction> actions)
        {
            if (actions.Count == 0) return 0f;
            return (float)actions.Count(a => a.actionType == ActionType.Combat) / actions.Count;
        }

        private float CalculateCreativeFocus(List<PlayerAction> actions)
        {
            if (actions.Count == 0) return 0f;
            return (float)actions.Count(a => a.actionType == ActionType.Research) / actions.Count;
        }

        private List<BehaviorInsight> GenerateInsights()
        {
            var insights = new List<BehaviorInsight>();

            // High trait insights
            foreach (var trait in behaviorTraits.Where(t => t.Value > 0.7f))
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = InsightType.HighTrait,
                    message = $"Player shows strong {trait.Key.ToString().ToLower()} tendencies (Score: {trait.Value:F2})",
                    confidence = trait.Value
                });
            }

            // Low trait insights
            foreach (var trait in behaviorTraits.Where(t => t.Value < 0.3f))
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = InsightType.LowTrait,
                    message = $"Player avoids {trait.Key.ToString().ToLower()}-based activities (Score: {trait.Value:F2})",
                    confidence = 1f - trait.Value
                });
            }

            // Session length insights
            if (currentSession != null && (Time.time - sessionStartTime) > 600f) // 10+ minutes
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = InsightType.LongSession,
                    message = "Player demonstrates high engagement with extended play sessions",
                    confidence = 0.8f
                });
            }

            // Variety insights
            var uniqueActionTypes = currentSessionActions.Select(a => a.actionType).Distinct().Count();
            if (uniqueActionTypes > 8)
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = InsightType.HighVariety,
                    message = $"Player explores diverse gameplay mechanics ({uniqueActionTypes} different action types)",
                    confidence = Mathf.Clamp01(uniqueActionTypes / 15f)
                });
            }
            else if (uniqueActionTypes <= 3 && currentSessionActions.Count > 20)
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = InsightType.Focused,
                    message = "Player shows focused behavior, concentrating on specific activities",
                    confidence = 0.7f
                });
            }

            // Success pattern insights
            var successfulActions = currentSessionActions.Count(a =>
                a.parameters.ContainsKey(ParamKey.Success) &&
                a.parameters[ParamKey.Success].ToString().ToLower() == "true");

            if (successfulActions > currentSessionActions.Count * 0.8f && currentSessionActions.Count > 10)
            {
                insights.Add(new BehaviorInsight
                {
                    insightType = InsightType.Achievement,
                    message = "Player thrives on challenges and achievement completion",
                    confidence = (float)successfulActions / currentSessionActions.Count
                });
            }

            return insights;
        }

        private List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();
            var preferences = GetAnalyzedPreferences();

            // Breeding recommendations
            if (preferences.ContainsKey(PreferenceType.Breeding) && preferences[PreferenceType.Breeding] > 0.6f)
            {
                recommendations.Add("Try breeding creatures with complementary traits for stronger offspring");
                recommendations.Add("Experiment with rare genetic combinations you haven't tried yet");

                if (behaviorTraits[PlayerBehaviorTrait.Exploratory] > 0.7f)
                {
                    recommendations.Add("Challenge yourself with complex multi-generational breeding projects");
                }
            }

            // Exploration recommendations
            if (preferences.ContainsKey(PreferenceType.Exploration) && preferences[PreferenceType.Exploration] > 0.6f)
            {
                recommendations.Add("Explore new biomes to discover unique creatures and resources");
                recommendations.Add("Search for rare environmental interactions and hidden areas");
            }

            // Quest recommendations
            if (preferences.ContainsKey(PreferenceType.Achievement) && preferences[PreferenceType.Achievement] > 0.5f)
            {
                recommendations.Add("Take on quests that match your favorite creature types");
                recommendations.Add("Try cooperative quests to experience different gameplay dynamics");
            }

            // Skill level based recommendations
            float overallSuccessRate = currentMetrics.GetValueOrDefault(MetricType.RecentSuccessRate, 0.5f);
            if (overallSuccessRate < 0.4f)
            {
                recommendations.Add("Focus on basic creature care to build foundational skills");
                recommendations.Add("Practice with easier breeding combinations first");
            }
            else if (overallSuccessRate > 0.8f)
            {
                recommendations.Add("Take on expert-level challenges for rare rewards");
                recommendations.Add("Mentor newer players to expand your knowledge");
            }

            // Play style specific recommendations
            ActionType dominantActivity = GetDominantActivity();
            switch (dominantActivity)
            {
                case ActionType.Breeding:
                    recommendations.Add("Document your successful breeding strategies for future reference");
                    break;
                case ActionType.Exploration:
                    recommendations.Add("Create a map of your discoveries to track progress");
                    break;
                case ActionType.Quest:
                    recommendations.Add("Try varying your quest approach for different outcomes");
                    break;
                case ActionType.Menu:
                    recommendations.Add("Consider setting a short-term goal to stay engaged");
                    break;
            }

            // Engagement based recommendations
            if (CalculateOverallEngagement() < 0.4f)
            {
                recommendations.Add("Take a break and try a different activity to refresh your interest");
                recommendations.Add("Set small, achievable goals to rebuild momentum");
            }

            return recommendations;
        }

        #endregion

        #region Performance Metrics

        private BehaviorMetrics CalculateBehaviorMetrics()
        {
            return new BehaviorMetrics
            {
                dominantBehaviorType = GetDominantActivity(),
                behaviorVariety = currentSessionActions.Select(a => a.actionType).Distinct().Count(),
                averageSessionLength = sessionHistory.Count > 0 ?
                    sessionHistory.Average(s => s.duration) : (Time.time - sessionStartTime),
                preferredGameplayStyle = DetermineGameplayStyle(),
                performanceConsistency = CalculatePerformanceConsistency(),
                learningCurveProgression = CalculateLearningProgression()
            };
        }

        private float CalculatePerformanceConsistency()
        {
            var recentPerformances = currentSessionActions
                .Where(a => a.parameters.ContainsKey(ParamKey.Success))
                .TakeLast(20)
                .Select(a => a.parameters[ParamKey.Success].ToString().ToLower() == "true" ? 1f : 0f)
                .ToList();

            if (recentPerformances.Count < 5) return 0.5f;

            float mean = recentPerformances.Average();
            float variance = recentPerformances.Sum(x => Mathf.Pow(x - mean, 2)) / recentPerformances.Count;

            return 1f - Mathf.Clamp01(variance); // Lower variance = higher consistency
        }

        private float CalculateLearningProgression()
        {
            if (sessionHistory.Count < 2) return 0.5f;

            var recentSessions = sessionHistory.TakeLast(5).ToList();
            var oldSessions = sessionHistory.Take(5).ToList();

            float recentSuccess = CalculateAverageSuccessRate(recentSessions);
            float oldSuccess = CalculateAverageSuccessRate(oldSessions);

            return Mathf.Clamp01(0.5f + (recentSuccess - oldSuccess));
        }

        private float CalculateAverageSuccessRate(List<GameplaySession> sessions)
        {
            var allActions = sessions.SelectMany(s => s.actions ?? new List<PlayerAction>());
            var successfulActions = allActions.Count(a =>
                a.parameters.ContainsKey(ParamKey.Success) &&
                a.parameters[ParamKey.Success].ToString().ToLower() == "true");

            var totalActionsWithSuccess = allActions.Count(a => a.parameters.ContainsKey(ParamKey.Success));

            return totalActionsWithSuccess > 0 ? (float)successfulActions / totalActionsWithSuccess : 0.5f;
        }

        #endregion

        #region Emotional Analysis

        private void UpdateBehaviorTraits(PlayerAction action)
        {
            // Update traits based on action patterns
            if (action.actionType == ActionType.Exploration)
            {
                behaviorTraits[PlayerBehaviorTrait.Exploratory] = Mathf.Min(1f, behaviorTraits[PlayerBehaviorTrait.Exploratory] + 0.02f);
            }

            if (action.actionType == ActionType.Social)
            {
                behaviorTraits[PlayerBehaviorTrait.Social] = Mathf.Min(1f, behaviorTraits[PlayerBehaviorTrait.Social] + 0.02f);
            }

            if (action.parameters.ContainsKey(ParamKey.Success))
            {
                bool success = action.parameters[ParamKey.Success].ToString().ToLower() == "true";
                if (success)
                {
                    behaviorTraits[PlayerBehaviorTrait.Persistent] = Mathf.Min(1f, behaviorTraits[PlayerBehaviorTrait.Persistent] + 0.01f);
                }
            }

            // Decay unused traits slightly
            foreach (var trait in behaviorTraits.Keys.ToList())
            {
                behaviorTraits[trait] = Mathf.Max(0f, behaviorTraits[trait] - 0.001f);
            }
        }

        private List<EmotionalMoment> DetectEmotionalMoments()
        {
            var moments = new List<EmotionalMoment>();
            var recentActions = currentSessionActions.TakeLast(50).ToList();

            // Success/failure patterns
            for (int i = 1; i < recentActions.Count; i++)
            {
                var current = recentActions[i];
                var previous = recentActions[i - 1];

                if (current.parameters.ContainsKey(ParamKey.Success) && previous.parameters.ContainsKey(ParamKey.Success))
                {
                    bool currentSuccess = current.parameters[ParamKey.Success].ToString().ToLower() == "true";
                    bool previousSuccess = previous.parameters[ParamKey.Success].ToString().ToLower() == "true";

                    if (currentSuccess && !previousSuccess)
                    {
                        moments.Add(new EmotionalMoment
                        {
                            timestamp = current.timestamp,
                            momentType = MomentType.Breakthrough,
                            description = $"Player achieved success in {current.actionType} after previous attempt",
                            intensity = 0.7f,
                            actionContext = current.actionType.ToString()
                        });
                    }
                }
            }

            // Discovery moments
            var discoveryActions = recentActions.Where(a =>
                a.parameters.ContainsKey(ParamKey.FirstDiscovery) &&
                a.parameters[ParamKey.FirstDiscovery].ToString().ToLower() == "true").ToList();

            foreach (var discovery in discoveryActions)
            {
                moments.Add(new EmotionalMoment
                {
                    timestamp = discovery.timestamp,
                    momentType = MomentType.Discovery,
                    description = $"Player discovered new content: {discovery.actionType}",
                    intensity = 0.8f,
                    actionContext = discovery.actionType.ToString()
                });
            }

            // Mastery moments (consistent success in complex actions)
            var complexActions = recentActions.Where(a =>
                a.actionType == ActionType.Breeding || a.actionType == ActionType.Research).ToList();

            if (complexActions.Count >= 5)
            {
                var successfulComplex = complexActions.Count(a =>
                    a.parameters.ContainsKey(ParamKey.Success) &&
                    a.parameters[ParamKey.Success].ToString().ToLower() == "true");

                if (successfulComplex >= complexActions.Count * 0.8f)
                {
                    moments.Add(new EmotionalMoment
                    {
                        timestamp = complexActions.Last().timestamp,
                        momentType = MomentType.Mastery,
                        description = "Player demonstrates mastery of complex mechanics",
                        intensity = 0.9f,
                        actionContext = "ComplexGameplay"
                    });
                }
            }

            return moments;
        }

        private void ProcessEmotionalMoments()
        {
            var moments = DetectEmotionalMoments();

            foreach (var moment in moments)
            {
                // Track the emotional moment
                TrackPlayerAction("Emotional_Moment", "EmotionalSystem", new Dictionary<ParamKey, object>
                {
                    [ParamKey.MomentType] = moment.momentType,
                    [ParamKey.Intensity] = moment.intensity,
                    [ParamKey.Context] = moment.actionContext
                });

                // Update emotional state
                switch (moment.momentType)
                {
                    case MomentType.Breakthrough:
                        TrackEmotionalResponse(EmotionalState.Pride, moment.intensity, "Breakthrough");
                        break;
                    case MomentType.Discovery:
                        TrackEmotionalResponse(EmotionalState.Excitement, moment.intensity, "Discovery");
                        break;
                    case MomentType.Mastery:
                        TrackEmotionalResponse(EmotionalState.Satisfaction, moment.intensity, "Mastery");
                        break;
                }
            }

            // Detect engagement spikes
            var actionWindow = recentActions.Where(a => a.timestamp > Time.time - 300f).ToList(); // 5 minute window
            if (actionWindow.Count > 20) // High activity
            {
                moments.Add(new EmotionalMoment
                {
                    timestamp = Time.time,
                    momentType = MomentType.EngagementSpike,
                    description = $"High engagement period with {actionWindow.Count} actions in 5 minutes",
                    intensity = 0.6f,
                    actionContext = "HighActivity"
                });
            }
        }

        #endregion

        #region Real-time Insights

        private string GenerateRealTimeInsight()
        {
            var recentActions = this.recentActions.ToList();
            if (recentActions.Count == 0) return "Insufficient recent activity";

            // Performance analysis
            var actionsWithResults = recentActions.Where(a => a.parameters.ContainsKey(ParamKey.Success)).ToList();
            if (actionsWithResults.Count >= 5)
            {
                var successCount = actionsWithResults.Count(a =>
                    a.parameters[ParamKey.Success].ToString().ToLower() == "true");
                var successRate = (float)successCount / actionsWithResults.Count;

                if (successRate < 0.3f)
                {
                    return "Low success rate detected - player may need assistance or reduced difficulty";
                }
                else if (successRate > 0.9f)
                {
                    return "High success rate detected - player may benefit from increased challenge";
                }
            }

            // Activity level analysis
            if (recentActions.Count < 5)
            {
                return "Low activity detected - player may be disengaged or need new content";
            }
            else if (recentActions.Count > 15)
            {
                return "High activity detected - player is highly engaged";
            }

            // Behavior pattern analysis
            ArchetypeType currentArchetype = DeterminePlayerArchetype();
            if (currentArchetype != currentPlayerProfile.dominantArchetype)
            {
                return $"Behavior shift detected - player transitioning from {currentPlayerProfile.dominantArchetype} to {currentArchetype}";
            }

            // Trait dominance analysis
            var dominantTrait = behaviorTraits.OrderByDescending(t => t.Value).FirstOrDefault();
            if (dominantTrait.Value > 0.8f)
            {
                return $"Strong {dominantTrait.Key.ToString().ToLower()} behavior pattern emerging";
            }

            return "Routine behavior analysis and optimization";
        }

        private void ProcessRealTimeAdaptations()
        {
            string insight = GenerateRealTimeInsight();
            currentInsights.Add(insight);

            // Keep only recent insights
            if (currentInsights.Count > 10)
            {
                currentInsights.RemoveAt(0);
            }

            // Trigger specific adaptations based on insights
            if (insight.Contains("Low success rate"))
            {
                TrackUIInteraction("DifficultyAdaptation", "SuggestEasier", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "DifficultyAdaptation",
                    [ParamKey.InteractionType] = "SuggestEasier",
                    [ParamKey.Trigger] = "LowPerformance"
                });
            }
            else if (insight.Contains("High success rate"))
            {
                TrackUIInteraction("DifficultyAdaptation", "SuggestHarder", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "DifficultyAdaptation",
                    [ParamKey.InteractionType] = "SuggestHarder",
                    [ParamKey.Trigger] = "HighPerformance"
                });
            }

            // Content recommendations
            if (behaviorTraits[PlayerBehaviorTrait.Exploratory] > 0.7f)
            {
                TrackUIInteraction("ContentRecommendation", "HighlightExploration", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "ContentRecommendation",
                    [ParamKey.InteractionType] = "HighlightExploration",
                    [ParamKey.Trigger] = "ExplorationPreference"
                });
            }

            if (behaviorTraits[PlayerBehaviorTrait.Social] > 0.6f)
            {
                TrackUIInteraction("ContentRecommendation", "SuggestSocial", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "ContentRecommendation",
                    [ParamKey.InteractionType] = "SuggestSocial",
                    [ParamKey.Trigger] = "SocialPreference"
                });
            }
        }

        #endregion

        #region Update and Monitoring

        void Update()
        {
            if (!enableAnalytics || currentSession == null) return;

            // Update real-time metrics
            UpdateRealTimeMetrics(null);

            // Check for session milestones
            CheckSessionMilestones();

            // Process real-time adaptations
            if (enableRealTimeAnalysis && Time.time - lastEmotionalUpdate > behaviorAnalysisInterval)
            {
                ProcessRealTimeAdaptations();
                ProcessEmotionalMoments();
                lastEmotionalUpdate = Time.time;
            }

            // Update player preferences
            UpdatePlayerPreferences();
        }

        private void CheckSessionMilestones()
        {
            float sessionDuration = Time.time - sessionStartTime;
            int totalActionCount = currentSessionActions.Count;

            // Long session milestone
            if (sessionDuration >= 30f && !sessionMilestones.Contains(MilestoneType.LongSession))
            {
                sessionMilestones.Add(MilestoneType.LongSession);
                TrackPlayerAction("Session_Milestone", "SessionManagement", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "SessionMilestone",
                    [ParamKey.InteractionType] = "LongSession",
                    [ParamKey.Intensity] = 0.6f
                });
            }

            // High activity milestone
            if (totalActionCount >= 100 && !sessionMilestones.Contains(MilestoneType.HighActivity))
            {
                sessionMilestones.Add(MilestoneType.HighActivity);
                TrackPlayerAction("Session_Milestone", "SessionManagement", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "SessionMilestone",
                    [ParamKey.InteractionType] = "HighActivity",
                    [ParamKey.Intensity] = 0.7f
                });
            }
        }

        private void UpdatePlayerPreferences()
        {
            // Update preferences based on recent behavior
            var playStyle = AnalyzePlayStyle();

            currentPlayerProfile.preferredGameplayStyle = DetermineGameplayStyle();

            // Track significant preference changes
            CheckForPreferenceChanges(playStyle);
        }

        private void CheckForPreferenceChanges(PlayStyle playStyle)
        {
            // Track archetype evolution
            ArchetypeType newArchetype = DeterminePlayerArchetype();
            if (newArchetype != currentPlayerProfile.dominantArchetype &&
                Time.time - currentPlayerProfile.archetypeUpdateTime > 300f) // 5 minute cooldown
            {
                TrackPlayerAction("Player_Evolution", "BehaviorAnalysis", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "PlayerArchetypeChange",
                    [ParamKey.InteractionType] = "ArchetypeEvolution",
                    [ParamKey.PreviousValue] = currentPlayerProfile.dominantArchetype,
                    [ParamKey.NewValue] = newArchetype
                });

                OnPlayerArchetypeChanged(newArchetype);
            }

            // Track skill progression
            float currentSkillLevel = CalculateOverallSkillLevel();
            if (Mathf.Abs(currentSkillLevel - currentPlayerProfile.skillLevel) > 0.1f)
            {
                TrackPlayerAction("Skill_Progression", "SkillAnalysis", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "SkillProgression",
                    [ParamKey.InteractionType] = "SkillLevelUp",
                    [ParamKey.PreviousValue] = currentPlayerProfile.skillLevel,
                    [ParamKey.NewValue] = currentSkillLevel
                });

                currentPlayerProfile.skillLevel = currentSkillLevel;
            }

            // Track biome preference changes
            var biomePreferences = GetPlayerBiomePreferences();
            var dominantBiome = biomePreferences.OrderByDescending(b => b.Value).FirstOrDefault();
            if (dominantBiome.Key != BiomeType.Unknown) // Check if we have a valid biome preference
            {
                TrackPlayerAction("Preference_Change", "PreferenceAnalysis", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "BiomePreference",
                    [ParamKey.InteractionType] = "PreferenceChange",
                    [ParamKey.NewValue] = dominantBiome.Key
                });
            }

            // Update social preference tracking
            if (playStyle.socialFocus > 0.6f && !playerPreferences.Contains("Social"))
            {
                playerPreferences.Add("Social");
            }
            else if (playStyle.socialFocus < 0.3f && playerPreferences.Contains("Social"))
            {
                playerPreferences.Remove("Social");
            }

            // Update exploration preference tracking
            if (playStyle.explorationFocus > 0.6f && !playerPreferences.Contains("Explorer"))
            {
                playerPreferences.Add("Explorer");
            }
            else if (playStyle.explorationFocus < 0.3f && playerPreferences.Contains("Explorer"))
            {
                playerPreferences.Remove("Explorer");
            }

            // Update competitive preference tracking
            if (playStyle.competitiveFocus > 0.6f && !playerPreferences.Contains("Competitive"))
            {
                playerPreferences.Add("Competitive");
            }
            else if (playStyle.competitiveFocus < 0.3f && playerPreferences.Contains("Competitive"))
            {
                playerPreferences.Remove("Competitive");
            }
        }

        private void OnPlayerArchetypeChanged(ArchetypeType newArchetype)
        {
            currentPlayerProfile.dominantArchetype = newArchetype;
            currentPlayerProfile.archetypeUpdateTime = Time.time;

            // Trigger adaptive responses based on new archetype
            var insights = GeneratePlayerBehaviorAnalysis();
            if (insights.riskFactors.Any(r => r == RiskFactorType.Frustration || r == RiskFactorType.Disengagement))
            {
                // Player may be struggling - offer easier content
                TrackPlayerAction("Adaptive_Response", "ArchetypeAdaptation", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "AdaptiveDifficulty",
                    [ParamKey.InteractionType] = "FrustrationResponse",
                    [ParamKey.Trigger] = "RealTimeAnalysis"
                });
            }

            if (insights.strengthAreas.Any(s => s == StrengthAreaType.Flow || s == StrengthAreaType.Performance))
            {
                // Player is doing well - suggest progression
                TrackPlayerAction("Adaptive_Response", "ArchetypeAdaptation", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "ProgressionSuggestion",
                    [ParamKey.InteractionType] = "SuccessRecognition",
                    [ParamKey.Trigger] = "HighPerformance"
                });
            }

            // Generate personalized recommendations
            var recommendations = GenerateRecommendations();
            foreach (var recommendation in recommendations.Take(3)) // Top 3 recommendations
            {
                TrackPlayerAction("Personalized_Recommendation", "RecommendationSystem", new Dictionary<ParamKey, object>
                {
                    [ParamKey.ElementName] = "PersonalizedRecommendation",
                    [ParamKey.InteractionType] = "RealTimeInsight",
                    [ParamKey.Content] = recommendation
                });
            }
        }

        #endregion

        #region Input Metrics

        private void InitializeInputMetrics()
        {
            inputPatterns = new Dictionary<InputType, float>
            {
                [InputType.Mouse] = 0,
                [InputType.Keyboard] = 0,
                [InputType.Gamepad] = 0,
                [InputType.Touch] = 0
            };

            var inputMetrics = new Dictionary<string, float>
            {
                ["Click"] = 0,
                ["Drag"] = 0,
                ["Scroll"] = 0,
                ["Hover"] = 0,
                ["KeyPress"] = 0,
                ["KeyCombo"] = 0
            };

            var timingMetrics = new Dictionary<string, float>
            {
                ["AverageClickTime"] = 0f,
                ["AverageDragDistance"] = 0f,
                ["AverageHoverTime"] = 0f,
                ["InputFrequency"] = 0f,
                ["ResponseTime"] = 0f
            };

            var accuracyMetrics = new Dictionary<string, float>
            {
                ["ClickAccuracy"] = 1.0f,
                ["TargetMissRate"] = 0f,
                ["IntentionalActions"] = 1.0f,
                ["AccidentalActions"] = 0f
            };

            var behaviorMetrics = new Dictionary<string, float>
            {
                ["Impulsiveness"] = 0.5f,
                ["Deliberateness"] = 0.5f,
                ["Exploration"] = 0.5f,
                ["Focus"] = 0.5f,
                ["Persistence"] = 0.5f
            };

            inputTracker = new InputMetricsTracker();
            inputTracker.Initialize();

            Debug.Log("Input metrics initialized for player analytics tracking");
        }

        #endregion

        #region Data Persistence

        private void SaveAnalyticsData()
        {
            if (currentSession != null)
            {
                EndCurrentSession();
            }

            SavePlayerProfile();
            SaveSessionHistory();
        }

        private void LoadPlayerProfile()
        {
            try
            {
                string profilePath = AnalyticsHelpers.GetAnalyticsDataPath("player_profile.json");
                if (System.IO.File.Exists(profilePath))
                {
                    string json = System.IO.File.ReadAllText(profilePath);
                    currentPlayerProfile = JsonUtility.FromJson<PlayerProfile>(json);
                    Debug.Log($"Player profile loaded for player: {currentPlayerProfile.playerId}");
                }
                else
                {
                    // Create new player profile
                    currentPlayerProfile = new PlayerProfile
                    {
                        playerId = System.Guid.NewGuid().ToString(),
                        dominantArchetype = ArchetypeType.Unknown,
                        preferredGameplayStyle = GameplayStyle.Casual
                    };
                    Debug.Log($"New player profile created: {currentPlayerProfile.playerId}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load player profile: {e.Message}");
                // Create fallback profile
                currentPlayerProfile = new PlayerProfile
                {
                    playerId = System.Guid.NewGuid().ToString(),
                    dominantArchetype = ArchetypeType.Unknown,
                    preferredGameplayStyle = GameplayStyle.Casual
                };
            }
        }

        private void SavePlayerProfile()
        {
            try
            {
                string json = JsonUtility.ToJson(currentPlayerProfile, true);
                System.IO.File.WriteAllText(AnalyticsHelpers.GetAnalyticsDataPath("player_profile.json"), json);

                Debug.Log($"Session saved - Duration: {currentSession.duration:F1}s, Actions: {currentSession.totalActions}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save player profile: {e.Message}");
            }
        }

        private void SaveSessionHistory()
        {
            try
            {
                var wrapper = new SessionHistoryWrapper { sessions = sessionHistory };
                string json = JsonUtility.ToJson(wrapper, true);
                System.IO.File.WriteAllText(AnalyticsHelpers.GetAnalyticsDataPath("session_history.json"), json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save session history: {e.Message}");
            }
        }

        private ArchetypeType DeterminePlayerArchetype()
        {
            return ArchetypeType.Explorer; // Default archetype
        }

        private string DeterminePreferredGameplayStyle()
        {
            return "Exploration"; // Default style
        }

        private float CalculateOverallSkillLevel()
        {
            return 1.0f; // Default skill level
        }

        private ActionType GetDominantActivity()
        {
            if (currentSessionActions.Count == 0) return ActionType.Menu;

            var activityCounts = currentSessionActions
                .GroupBy(a => a.actionType)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();

            return activityCounts?.Key ?? ActionType.Menu;
        }

        private Dictionary<PreferenceType, float> GetAnalyzedPreferences()
        {
            return new Dictionary<PreferenceType, float>
            {
                [PreferenceType.Exploration] = 0.7f,
                [PreferenceType.Social] = 0.5f,
                [PreferenceType.Competition] = 0.3f,
                [PreferenceType.Creative] = 0.6f
            };
        }

        private void UpdatePlayerProfileFromSession()
        {
            // Update player profile based on current session data
            if (currentPlayerProfile != null && currentSession != null)
            {
                currentPlayerProfile.playtime += currentSession.duration;
                // Additional profile updates can be added here
            }
        }

        /// <summary>
        /// Analyzes player choice patterns for behavioral insights
        /// </summary>
        private void AnalyzeChoicePattern(ChoiceCategory category, string choice, Dictionary<ParamKey, object> context)
        {
            // Track choice preferences by category
            var categoryKey = category.ToString();
            AddToSessionDataList(SessionDataKey.ChoicePatterns, categoryKey, choice);

            // Analyze patterns if we have enough data
            var choices = GetSessionDataList(SessionDataKey.ChoicePatterns, categoryKey);
            if (choices.Count >= 3)
            {
                var recentChoices = choices.TakeLast(3).ToList();

                // Simple pattern detection - could be expanded
                bool hasPattern = recentChoices.All(c => c.Equals(recentChoices[0]));
                if (hasPattern)
                {
                    Debug.Log($"Choice pattern detected for {category}: {choice}");
                }
            }
        }

        /// <summary>
        /// Updates UI interaction patterns for UX optimization
        /// </summary>
        private void UpdateUIPatterns(string elementName, string interactionType)
        {
            var patternKey = $"{elementName}_{interactionType}";
            IncrementSessionDataCounter(SessionDataKey.UIPatterns, patternKey);

            // Track frequently used UI elements
            var frequencyKey = elementName;
            IncrementSessionDataCounter(SessionDataKey.UIFrequency, frequencyKey);

            // Log high usage patterns
            var frequencyCount = GetSessionDataCounter(SessionDataKey.UIFrequency, frequencyKey);
            if (frequencyCount % 10 == 0)
            {
                Debug.Log($"High UI usage detected: {elementName} used {frequencyCount} times");
            }
        }

        /// <summary>
        /// Gets comprehensive behavior analysis data for external systems
        /// </summary>
        public BehaviorAnalysis GetBehaviorAnalysis()
        {
            return new BehaviorAnalysis
            {
                playTime = currentSession?.duration ?? 0f,
                actionCount = totalActionCount,
                dominantBehaviorTraits = behaviorTraits.Where(kvp => kvp.Value > 0.6f)
                                                      .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                preferredGameplayStyle = DetermineGameplayStyle(),
                engagementLevel = CalculateEngagementLevel(),
                difficultyPreference = adaptationEngine?.GetCurrentDifficulty() ?? 0.5f,
                sessionMetrics = currentMetrics,
                explorationFocus = behaviorTraits.GetValueOrDefault(PlayerBehaviorTrait.Exploratory, 0.5f),
                socialFocus = behaviorTraits.GetValueOrDefault(PlayerBehaviorTrait.Social, 0.5f),
                creativeFocus = behaviorTraits.GetValueOrDefault(PlayerBehaviorTrait.Creative, 0.5f)
            };
        }

        /// <summary>
        /// Determines the player's preferred gameplay style based on behavior patterns
        /// </summary>
        private GameplayStyle DetermineGameplayStyle()
        {
            // Simple heuristic - could be more sophisticated
            if (behaviorTraits.ContainsKey(PlayerBehaviorTrait.Exploratory) &&
                behaviorTraits[PlayerBehaviorTrait.Exploratory] > 0.7f)
                return GameplayStyle.Experimental;

            if (behaviorTraits.ContainsKey(PlayerBehaviorTrait.Competitive) &&
                behaviorTraits[PlayerBehaviorTrait.Competitive] > 0.7f)
                return GameplayStyle.Competitive;

            return GameplayStyle.Casual;
        }

        /// <summary>
        /// Calculates current engagement level
        /// </summary>
        private float CalculateEngagementLevel()
        {
            if (currentSession == null) return 0f;

            float sessionLength = currentSession.duration;
            float actionDensity = totalActionCount / Mathf.Max(sessionLength, 1f);

            // Normalize engagement based on action density and session length
            return Mathf.Clamp01(actionDensity * 0.1f + (sessionLength / 300f) * 0.3f);
        }

        #endregion

        #region Analyzer Classes

        public class PlayerBehaviorAnalyzer
        {
            public float CalculateCurrentEngagement() { return Random.Range(0.4f, 0.9f); }
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
    }

    public enum PlayerBehaviorTrait
    {
        Exploratory,
        Social,
        Competitive,
        Creative,
        Repetitive,
        Adaptive,
        Focused,
        Impulsive,
        Persistent
    }

    [System.Serializable]
    public class PlayerProfile
    {
        public string playerId;
        public float playtime;
        public Dictionary<PreferenceType, float> preferences = new Dictionary<PreferenceType, float>();
        public Dictionary<PlayerBehaviorTrait, float> traits = new Dictionary<PlayerBehaviorTrait, float>();
        public float skill_level;
        public List<string> achievements = new List<string>();
        public string favoriteSpecies;
        public ArchetypeType dominantArchetype;
        public float archetypeUpdateTime;
        public List<ArchetypeType> previousArchetypes = new List<ArchetypeType>();
        public GameplayStyle preferredGameplayStyle;
        public float skillLevel;
        public float lastAdaptation;
    }

    [System.Serializable]
    public class GameplaySession
    {
        public float startTime;
        public float endTime;
        public int actionCount;
        public List<string> achievements = new List<string>();
        public Dictionary<MetricType, float> sessionMetrics = new Dictionary<MetricType, float>();
        public float Duration => endTime - startTime;
        public float duration;
        public int totalActions;
        public List<PlayerAction> actions = new List<PlayerAction>();
        public int uniqueActionTypes;
        public EngagementMetrics engagementMetrics;
        public BehaviorMetrics behaviorMetrics;
        public Dictionary<EmotionalState, float> emotionalProfile = new Dictionary<EmotionalState, float>();
        public uint sessionId;
        public ArchetypeType playerArchetype;
        public float totalPauseTime;
        public float pauseTime;
    }

    [System.Serializable]
    public class PlayerAction
    {
        public ActionType actionType;
        public float timestamp;
        public Vector3 position;
        public Dictionary<ParamKey, object> parameters = new Dictionary<ParamKey, object>();
        public float intensity;
        public bool successful;
    }

    public class GameAdaptationEngine
    {
        public void ProcessPlayerData(PlayerProfile profile) { }
        public List<string> GetRecommendations() => new List<string>();
        public void UpdateDifficulty(float adjustment) { }
        public float GetCurrentDifficulty() => 1.0f;
        public void ProcessAdaptation(PlayerProfile profile) { }
    }

    public class InputMetricsTracker
    {
        public Dictionary<ActionType, int> inputCounts = new Dictionary<ActionType, int>();
        public float averageInputRate;
        public void TrackInput(ActionType type) { }
        public void Reset() { }
        public void Initialize() { }
    }

    public enum EmotionalState
    {
        Neutral,
        Excited,
        Frustrated,
        Relaxed,
        Focused,
        Overwhelmed,
        Satisfied,
        Curious,
        Pride,
        Excitement,
        Satisfaction,
        Anticipation,
        Frustration
    }

    public class PersonalityProfiler
    {
        public Dictionary<PlayerBehaviorTrait, float> GetPersonalityTraits() => new Dictionary<PlayerBehaviorTrait, float>();
        public void UpdateProfile(List<PlayerAction> actions) { }
        public PersonalityType GetPersonalityType() => PersonalityType.Balanced;

        /// <summary>
        /// Records emotional response data for personality profiling
        /// </summary>
        public void RecordEmotionalResponse(EmotionalState emotionalState, float intensity)
        {
            // Record emotional response for personality analysis
            // This could update internal personality metrics based on emotional patterns
            Debug.Log($"Recorded emotional response: {emotionalState} with intensity {intensity:F2}");
        }
    }

    public class InputMetrics
    {
        public int totalInputs;
        public float averageInputRate;
        public Dictionary<ActionType, int> inputTypes = new Dictionary<ActionType, int>();
    }

    [System.Serializable]
    public class EngagementMetrics
    {
        public float sessionDuration;
        public int interactionCount;
        public float focusScore;
        public float satisfactionLevel;
        public Dictionary<EngagementType, float> engagementFactors = new Dictionary<EngagementType, float>();
        public int totalActions;
        public float actionsPerMinute;
        public Dictionary<EngagementType, float> engagementMetrics = new Dictionary<EngagementType, float>();
        public float overallEngagement;
        public float focusLevel;
        public float explorationLevel;
    }

    [System.Serializable]
    public class QuestData
    {
        public string questId;
        public string questName;
        public float progress;
        public bool isCompleted;
        public Dictionary<QuestKey, object> questParameters = new Dictionary<QuestKey, object>();
        public float timeSpent;
        public Laboratory.Core.Enums.QuestType type;
        public float difficulty;
        public float generatedTime;
    }

    [System.Serializable]
    public class CreatureInstance
    {
        public uint creatureId;
        public string speciesName;
        public Vector3 position;
        public Dictionary<string, float> traits = new Dictionary<string, float>();
        public float age;
        public bool isAlive;
        public float fitness;
        public int generation;
    }

    [System.Serializable]
    public class BreedingSession
    {
        public CreatureInstance parentA;
        public CreatureInstance parentB;
        public float startTime;
        public float completionTime;
        public bool successful;
        public Dictionary<ParamKey, object> parameters = new Dictionary<ParamKey, object>();
    }

    [System.Serializable]
    public class EcosystemEvent
    {
        public Laboratory.Core.Enums.EventType eventType;
        public float timestamp;
        public Vector3 location;
        public Dictionary<EventKey, object> eventData = new Dictionary<EventKey, object>();
        public float impact;
    }

    [System.Serializable]
    public struct PlayerBehaviorAnalysis
    {
        public float explorationFocus;
        public float socialFocus;
        public float competitiveFocus;
        public float creativeFocus;
        public float repetitiveScore;
        public float adaptabilityScore;
        public List<RiskFactorType> riskFactors;
        public List<StrengthAreaType> strengthAreas;
        public float engagementLevel;
        public Dictionary<PlayerBehaviorTrait, float> behaviorTraits;
        public List<BehaviorInsight> insights;
        public List<string> recommendations;
        public Dictionary<EmotionalState, float> emotionalProfile;
        public Dictionary<MetricType, float> sessionMetrics;
        public string playerId;
        public ArchetypeType dominantArchetype;
        public PlayStyle playStyle;
    }

    [System.Serializable]
    public class PlayStyle
    {
        public float explorationFocus;
        public float socialFocus;
        public float competitiveFocus;
        public float creativeFocus;
        public float patienceLevel;
        public float riskTolerance;
        public Dictionary<string, float> preferences = new Dictionary<string, float>();
        public float questFocus;
        public float breedingFocus;
    }

    [System.Serializable]
    public class BehaviorInsight
    {
        public string insight;
        public float confidence;
        public string category;
        public Dictionary<ContextType, object> supportingData = new Dictionary<ContextType, object>();
        public float timestamp;
        public InsightType insightType;
        public string message;
    }

    [System.Serializable]
    public class BehaviorMetrics
    {
        public Dictionary<PlayerBehaviorTrait, float> traitScores = new Dictionary<PlayerBehaviorTrait, float>();
        public float consistency;
        public float adaptability;
        public float engagement;
        public List<PatternType> patterns = new List<PatternType>();
        public float behaviorVariety;
        public float averageSessionLength;
        public GameplayStyle preferredGameplayStyle;
        public float performanceConsistency;
        public float learningCurveProgression;
        public ActionType dominantBehaviorType;
    }

    [System.Serializable]
    public class EmotionalMoment
    {
        public EmotionalState state;
        public float intensity;
        public float timestamp;
        public string trigger;
        public Vector3 location;
        public Dictionary<ContextType, object> context = new Dictionary<ContextType, object>();
        public string description;
        public string actionContext;
        public MomentType momentType;
    }

    [System.Serializable]
    public class SessionHistoryWrapper
    {
        public List<GameplaySession> sessions = new List<GameplaySession>();
    }

    // Helper methods for PlayerAnalyticsTracker
    public static class AnalyticsHelpers
    {
        public static string GetAnalyticsDataPath(string filename)
        {
            return Application.persistentDataPath + "/" + filename;
        }
    }

    /// <summary>
    /// Comprehensive behavior analysis data structure
    /// </summary>
    [System.Serializable]
    public class BehaviorAnalysis
    {
        public float playTime;
        public int actionCount;
        public Dictionary<PlayerBehaviorTrait, float> dominantBehaviorTraits = new Dictionary<PlayerBehaviorTrait, float>();
        public GameplayStyle preferredGameplayStyle;
        public float engagementLevel;
        public float difficultyPreference;
        public Dictionary<MetricType, float> sessionMetrics = new Dictionary<MetricType, float>();
        public string analysisTimestamp;
        public float confidence;
        public float explorationFocus;
        public float socialFocus;
        public float creativeFocus;
        public float competitiveFocus;
    }

    /// <summary>
    /// Player archetype classification
    /// </summary>
    [System.Serializable]
    public enum PlayerArchetype
    {
        Explorer,      // Loves discovering new areas and content
        Achiever,      // Focuses on goals, completion, and optimization
        Socializer,    // Enjoys interaction with other players
        Killer,        // Prefers competitive gameplay and PvP
        Collector,     // Enjoys gathering and hoarding items/creatures
        Builder,       // Likes creating and customizing
        Experimenter,  // Tries different approaches and combinations
        Casual,        // Relaxed, intermittent play style
        Hardcore,      // Intensive, dedicated play style
        Balanced       // Shows traits from multiple archetypes
    }

    #endregion
}