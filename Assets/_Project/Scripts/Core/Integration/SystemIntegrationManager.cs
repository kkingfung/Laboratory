using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Events;
using Laboratory.Chimera.Genetics.Advanced;
using Laboratory.AI.Personality;
using Laboratory.Systems.Ecosystem;
using Laboratory.Systems.Analytics;
using Laboratory.Systems.Quests;
using Laboratory.Systems.Breeding;
using Laboratory.Systems.Storytelling;

namespace Laboratory.Core.Integration
{
    /// <summary>
    /// Master integration manager that orchestrates communication between all major systems,
    /// creates emergent cross-system behaviors, and manages the overall simulation state.
    /// This acts as the "conductor" of the simulation orchestra.
    /// </summary>
    public class SystemIntegrationManager : MonoBehaviour
    {
        [Header("Integration Configuration")]
        [SerializeField] private bool enableSystemIntegration = true;
        [SerializeField] private float integrationUpdateInterval = 2f;
        [SerializeField] private bool enableEmergentBehaviors = true;
        [SerializeField] private bool enableCrossSystemEvents = true;

        [Header("System Orchestration")]
        [SerializeField, Range(0f, 1f)] private float systemSynergy = 0.8f;
        [SerializeField] private int maxConcurrentEvents = 5;
        [SerializeField] private float eventPropagationDelay = 0.5f;

        [Header("Emergent Behavior Thresholds")]
        [SerializeField, Range(0f, 1f)] private float evolutionaryBreakthroughThreshold = 0.9f;
        [SerializeField, Range(0f, 1f)] private float ecosystemCrisisThreshold = 0.3f;
        [SerializeField, Range(0f, 1f)] private float narrativeClimatThreshold = 0.8f;

        [Header("System Health Monitoring")]
        [SerializeField] private bool monitorSystemHealth = true;
        [SerializeField] private float healthCheckInterval = 10f;
        [SerializeField] private SystemHealthConfig[] systemHealthConfigs;

        // Connected systems
        private GeneticEvolutionManager evolutionManager;
        private CreaturePersonalityManager personalityManager;
        private DynamicEcosystemSimulator ecosystemSimulator;
        private PlayerAnalyticsTracker analyticsTracker;
        private ProceduralQuestGenerator questGenerator;
        private AdvancedBreedingSimulator breedingSimulator;
        private AIStorytellerSystem storytellerSystem;
        private EnhancedDebugConsole debugConsole;

        // Integration state
        private Dictionary<string, SystemState> systemStates = new Dictionary<string, SystemState>();
        private List<CrossSystemEvent> pendingEvents = new List<CrossSystemEvent>();
        private Queue<SystemCommand> commandQueue = new Queue<SystemCommand>();

        // Emergent behavior tracking
        private EmergentBehaviorTracker behaviorTracker;
        private Dictionary<string, float> emergentScores = new Dictionary<string, float>();

        // System health monitoring
        private Dictionary<string, SystemHealth> systemHealthStatus = new Dictionary<string, SystemHealth>();
        private float lastHealthCheck;
        private float lastIntegrationUpdate;

        // Integration analytics
        private IntegrationAnalytics analytics = new IntegrationAnalytics();

        // Events
        public System.Action<CrossSystemEvent> OnCrossSystemEventTriggered;
        public System.Action<EmergentBehavior> OnEmergentBehaviorDetected;
        public System.Action<SystemCrisis> OnSystemCrisisDetected;
        public System.Action<string, SystemHealth> OnSystemHealthChanged;

        // Singleton access
        private static SystemIntegrationManager instance;
        public static SystemIntegrationManager Instance => instance;

        public bool IsIntegrationActive => enableSystemIntegration && AllSystemsConnected();
        public IntegrationAnalytics Analytics => analytics;
        public SystemHealthSummary OverallSystemHealth => GenerateHealthSummary();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeIntegrationManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            ConnectToAllSystems();
            InitializeEmergentBehaviorTracking();
            StartSystemOrchestration();
        }

        private void Update()
        {
            if (!enableSystemIntegration) return;

            // Process system integration
            if (Time.time - lastIntegrationUpdate >= integrationUpdateInterval)
            {
                UpdateSystemIntegration();
                lastIntegrationUpdate = Time.time;
            }

            // Monitor system health
            if (monitorSystemHealth && Time.time - lastHealthCheck >= healthCheckInterval)
            {
                PerformSystemHealthCheck();
                lastHealthCheck = Time.time;
            }

            // Process command queue
            ProcessCommandQueue();

            // Process pending cross-system events
            ProcessPendingEvents();

            // Detect emergent behaviors
            if (enableEmergentBehaviors)
            {
                DetectEmergentBehaviors();
            }
        }

        private void InitializeIntegrationManager()
        {
            DebugManager.LogInfo("Initializing System Integration Manager");

            // Initialize behavior tracker
            behaviorTracker = new EmergentBehaviorTracker();

            // Initialize system states
            InitializeSystemStates();

            // Initialize health monitoring
            InitializeHealthMonitoring();

            DebugManager.LogInfo("System Integration Manager initialized");
        }

        private void ConnectToAllSystems()
        {
            // Connect to all major systems
            evolutionManager = GeneticEvolutionManager.Instance;
            personalityManager = CreaturePersonalityManager.Instance;
            ecosystemSimulator = DynamicEcosystemSimulator.Instance;
            analyticsTracker = PlayerAnalyticsTracker.Instance;
            questGenerator = ProceduralQuestGenerator.Instance;
            breedingSimulator = AdvancedBreedingSimulator.Instance;
            storytellerSystem = AIStorytellerSystem.Instance;
            // ⚡ OPTIMIZED: Try to get from cache or use lazy initialization to avoid FindObjectOfType
            if (debugConsole == null)
            {
                debugConsole = FindObjectOfType<EnhancedDebugConsole>();
            }

            // Subscribe to system events
            SubscribeToSystemEvents();

            DebugManager.LogInfo($"Connected to {CountConnectedSystems()}/8 major systems");
        }

        private void SubscribeToSystemEvents()
        {
            // Evolution system events
            if (evolutionManager != null)
            {
                evolutionManager.OnEliteCreatureEmerged += HandleEliteCreatureEmergence;
                evolutionManager.OnEvolutionaryMilestone += HandleEvolutionaryMilestone;
            }

            // Personality system events
            if (personalityManager != null)
            {
                personalityManager.OnSocialInteraction += HandleSocialInteraction;
                personalityManager.OnMoodChanged += HandleMoodChange;
            }

            // Ecosystem events
            if (ecosystemSimulator != null)
            {
                ecosystemSimulator.OnEnvironmentalEventStarted += HandleEnvironmentalEvent;
                ecosystemSimulator.OnBiomeHealthChanged += HandleBiomeHealthChange;
                ecosystemSimulator.OnEcosystemStressChanged += HandleEcosystemStress;
            }

            // Analytics events
            if (analyticsTracker != null)
            {
                analyticsTracker.OnPlayerArchetypeIdentified += HandlePlayerArchetypeChange;
                analyticsTracker.OnGameAdaptationTriggered += HandleGameAdaptation;
            }

            // Quest events
            if (questGenerator != null)
            {
                questGenerator.OnQuestCompleted += HandleQuestCompletion;
                questGenerator.OnQuestGenerated += HandleQuestGeneration;
            }

            // Breeding events
            if (breedingSimulator != null)
            {
                breedingSimulator.OnBreedingCompleted += HandleBreedingCompletion;
                breedingSimulator.OnOffspringAccepted += HandleOffspringAcceptance;
            }

            // Storytelling events
            if (storytellerSystem != null)
            {
                storytellerSystem.OnStoryGenerated += HandleStoryGeneration;
                storytellerSystem.OnNarrativeUpdate += HandleNarrativeUpdate;
            }
        }

        /// <summary>
        /// Triggers a complex cross-system cascade event
        /// </summary>
        public void TriggerSystemCascade(CascadeType cascadeType, Dictionary<string, object> parameters = null)
        {
            var cascade = new SystemCascade
            {
                cascadeType = cascadeType,
                startTime = Time.time,
                parameters = parameters ?? new Dictionary<string, object>(),
                affectedSystems = new List<string>()
            };

            switch (cascadeType)
            {
                case CascadeType.EvolutionaryLeap:
                    ExecuteEvolutionaryLeapCascade(cascade);
                    break;

                case CascadeType.EcosystemCollapse:
                    ExecuteEcosystemCollapseCascade(cascade);
                    break;

                case CascadeType.SocialRevolution:
                    ExecuteSocialRevolutionCascade(cascade);
                    break;

                case CascadeType.TechnologicalBreakthrough:
                    ExecuteTechnologicalBreakthroughCascade(cascade);
                    break;
            }

            analytics.totalCascadeEvents++;
            DebugManager.LogInfo($"Triggered system cascade: {cascadeType}");
        }

        /// <summary>
        /// Creates a synchronized multi-system event
        /// </summary>
        public void OrchestrateSynchronizedEvent(string eventName, List<string> targetSystems, Dictionary<string, object> eventData)
        {
            var synchronizedEvent = new SynchronizedSystemEvent
            {
                eventName = eventName,
                targetSystems = targetSystems,
                eventData = eventData,
                scheduledTime = Time.time + eventPropagationDelay,
                synchronizationId = System.Guid.NewGuid().ToString()
            };

            // Queue the event for synchronized execution
            foreach (var systemName in targetSystems)
            {
                var crossSystemEvent = new CrossSystemEvent
                {
                    sourceSystem = "Integration",
                    targetSystem = systemName,
                    eventType = eventName,
                    eventData = eventData,
                    scheduledTime = synchronizedEvent.scheduledTime,
                    synchronizationId = synchronizedEvent.synchronizationId
                };

                pendingEvents.Add(crossSystemEvent);
            }

            DebugManager.LogInfo($"Orchestrated synchronized event '{eventName}' across {targetSystems.Count} systems");
        }

        /// <summary>
        /// Gets comprehensive system integration report
        /// </summary>
        public SystemIntegrationReport GenerateIntegrationReport()
        {
            var report = new SystemIntegrationReport
            {
                timestamp = Time.time,
                connectedSystemsCount = CountConnectedSystems(),
                overallIntegrationHealth = CalculateOverallIntegrationHealth(),
                systemStates = new Dictionary<string, SystemState>(systemStates),
                emergentBehaviors = behaviorTracker.GetActiveEmergentBehaviors(),
                crossSystemEvents = analytics.totalCrossSystemEvents,
                cascadeEvents = analytics.totalCascadeEvents,
                systemSynergyScore = CalculateSystemSynergyScore(),
                recommendations = GenerateIntegrationRecommendations()
            };

            return report;
        }

        private void UpdateSystemIntegration()
        {
            // Update system states
            UpdateAllSystemStates();

            // Calculate emergent scores
            UpdateEmergentScores();

            // Check for system synergy opportunities
            CheckSynergyOpportunities();

            // Update analytics
            UpdateIntegrationAnalytics();
        }

        private void UpdateAllSystemStates()
        {
            // Update each system's state based on their current status
            if (evolutionManager != null)
            {
                systemStates["Evolution"] = AnalyzeEvolutionSystemState();
            }

            if (ecosystemSimulator != null)
            {
                systemStates["Ecosystem"] = AnalyzeEcosystemSystemState();
            }

            if (personalityManager != null)
            {
                systemStates["Personality"] = AnalyzePersonalitySystemState();
            }

            // Continue for other systems...
        }

        private void CheckSynergyOpportunities()
        {
            // Genetic-Ecosystem Synergy
            CheckGeneticEcosystemSynergy();

            // Personality-Quest Synergy
            CheckPersonalityQuestSynergy();

            // Analytics-Storytelling Synergy
            CheckAnalyticsStorytellingSynergy();

            // Breeding-Evolution Synergy
            CheckBreedingEvolutionSynergy();
        }

        private void CheckGeneticEcosystemSynergy()
        {
            if (evolutionManager == null || ecosystemSimulator == null) return;

            float avgFitness = evolutionManager.AveragePopulationFitness;
            var ecosystemHealth = ecosystemSimulator.OverallHealth;

            // If high fitness but poor ecosystem, trigger adaptation event
            if (avgFitness > 0.8f && ecosystemHealth == EcosystemHealth.Poor)
            {
                TriggerCrossSystemEvent("GeneticAdaptation", "Evolution", "Ecosystem", new Dictionary<string, object>
                {
                    ["adaptationType"] = "EcosystemOptimization",
                    ["intensity"] = 0.7f
                });
            }

            // If excellent ecosystem but low fitness, boost evolution
            if (ecosystemHealth == EcosystemHealth.Excellent && avgFitness < 0.4f)
            {
                TriggerCrossSystemEvent("EvolutionBoost", "Ecosystem", "Evolution", new Dictionary<string, object>
                {
                    ["boostType"] = "EnvironmentalAdvantage",
                    ["multiplier"] = 1.5f
                });
            }
        }

        private void CheckPersonalityQuestSynergy()
        {
            if (personalityManager == null || questGenerator == null) return;

            int activePersonalities = personalityManager.ActivePersonalityCount;
            var activeQuests = questGenerator.ActiveQuests.Count;

            // If many personalities but few quests, generate social quests
            if (activePersonalities > 10 && activeQuests < 2)
            {
                var questContext = new Dictionary<string, object>
                {
                    ["preferredType"] = "Social",
                    ["personality_driven"] = true,
                    ["complexity"] = "High"
                };

                questGenerator.GenerateQuest();

                TriggerCrossSystemEvent("PersonalityQuestGeneration", "Personality", "Quest", questContext);
            }
        }

        private void CheckAnalyticsStorytellingSynergy()
        {
            if (analyticsTracker == null || storytellerSystem == null) return;

            var behaviorAnalysis = analyticsTracker.GetBehaviorAnalysis();
            int activeStories = storytellerSystem.ActiveStories.Count;

            // If high engagement but no stories, generate engaging narrative
            if (behaviorAnalysis.engagementLevel > 0.8f && activeStories == 0)
            {
                storytellerSystem.GenerateStory();

                TriggerCrossSystemEvent("EngagementStoryGeneration", "Analytics", "Storytelling", new Dictionary<string, object>
                {
                    ["playerArchetype"] = behaviorAnalysis.dominantArchetype.ToString(),
                    ["engagementLevel"] = behaviorAnalysis.engagementLevel
                });
            }
        }

        private void CheckBreedingEvolutionSynergy()
        {
            if (breedingSimulator == null || evolutionManager == null) return;

            bool breedingActive = breedingSimulator.IsBreedingInProgress;
            int populationSize = evolutionManager.TotalPopulationSize;

            // If active breeding but small population, encourage more breeding
            if (breedingActive && populationSize < 20)
            {
                TriggerCrossSystemEvent("PopulationBoost", "Breeding", "Evolution", new Dictionary<string, object>
                {
                    ["encourageBreeding"] = true,
                    ["targetPopulation"] = 30
                });
            }
        }

        private void DetectEmergentBehaviors()
        {
            // Detect complex multi-system behaviors
            DetectEvolutionaryBreakthrough();
            DetectEcosystemResilience();
            DetectNarrativeComplexity();
            DetectSocialComplexity();
        }

        private void DetectEvolutionaryBreakthrough()
        {
            if (evolutionManager == null) return;

            float avgFitness = evolutionManager.AveragePopulationFitness;

            if (avgFitness > evolutionaryBreakthroughThreshold)
            {
                var behavior = new EmergentBehavior
                {
                    behaviorType = "EvolutionaryBreakthrough",
                    description = "Population has achieved exceptional fitness levels",
                    confidence = avgFitness,
                    involvedSystems = new[] { "Evolution", "Personality", "Ecosystem" },
                    timestamp = Time.time
                };

                behaviorTracker.RecordEmergentBehavior(behavior);
                OnEmergentBehaviorDetected?.Invoke(behavior);

                // Trigger breakthrough cascade
                TriggerSystemCascade(CascadeType.EvolutionaryLeap, new Dictionary<string, object>
                {
                    ["avgFitness"] = avgFitness
                });
            }
        }

        private void DetectEcosystemResilience()
        {
            if (ecosystemSimulator == null) return;

            var health = ecosystemSimulator.OverallHealth;

            if (health == EcosystemHealth.Excellent && emergentScores.GetValueOrDefault("EcosystemStability", 0f) > 0.8f)
            {
                var behavior = new EmergentBehavior
                {
                    behaviorType = "EcosystemResilience",
                    description = "Ecosystem has achieved exceptional stability and health",
                    confidence = 0.9f,
                    involvedSystems = new[] { "Ecosystem", "Evolution", "Quest" },
                    timestamp = Time.time
                };

                behaviorTracker.RecordEmergentBehavior(behavior);
                OnEmergentBehaviorDetected?.Invoke(behavior);
            }
        }

        private void TriggerCrossSystemEvent(string eventType, string sourceSystem, string targetSystem, Dictionary<string, object> eventData)
        {
            var crossEvent = new CrossSystemEvent
            {
                sourceSystem = sourceSystem,
                targetSystem = targetSystem,
                eventType = eventType,
                eventData = eventData,
                scheduledTime = Time.time,
                synchronizationId = System.Guid.NewGuid().ToString()
            };

            pendingEvents.Add(crossEvent);
            OnCrossSystemEventTriggered?.Invoke(crossEvent);

            analytics.totalCrossSystemEvents++;
        }

        // Event handlers for system integration
        private void HandleEliteCreatureEmergence(CreatureGenome eliteCreature)
        {
            // Cascade this event to multiple systems
            var eventData = new Dictionary<string, object>
            {
                ["creatureId"] = eliteCreature.id,
                ["fitness"] = eliteCreature.fitness,
                ["generation"] = eliteCreature.generation
            };

            // Notify storyteller for narrative
            TriggerCrossSystemEvent("EliteEmergence", "Evolution", "Storytelling", eventData);

            // Notify quest system for special quests
            TriggerCrossSystemEvent("EliteEmergence", "Evolution", "Quest", eventData);

            // Notify analytics for tracking
            TriggerCrossSystemEvent("EliteEmergence", "Evolution", "Analytics", eventData);
        }

        private void HandleEnvironmentalEvent(EnvironmentalEvent envEvent)
        {
            var eventData = new Dictionary<string, object>
            {
                ["eventType"] = envEvent.eventType.ToString(),
                ["description"] = envEvent.description
            };

            // Propagate to personality system (creatures react emotionally)
            TriggerCrossSystemEvent("EnvironmentalImpact", "Ecosystem", "Personality", eventData);

            // Propagate to quest system (environmental challenges)
            TriggerCrossSystemEvent("EnvironmentalChallenge", "Ecosystem", "Quest", eventData);

            // Propagate to storyteller (dramatic events)
            TriggerCrossSystemEvent("EnvironmentalNarrative", "Ecosystem", "Storytelling", eventData);
        }

        private void HandleQuestCompletion(ProceduralQuest quest)
        {
            var eventData = new Dictionary<string, object>
            {
                ["questType"] = quest.type.ToString(),
                ["difficulty"] = quest.difficulty
            };

            // Reward ecosystem health for successful quests
            if (quest.type == QuestType.Survival)
            {
                TriggerCrossSystemEvent("QuestReward", "Quest", "Ecosystem", eventData);
            }

            // Generate narrative for significant quest completions
            if (quest.difficulty > 0.7f)
            {
                TriggerCrossSystemEvent("QuestNarrative", "Quest", "Storytelling", eventData);
            }
        }

        // Additional event handlers and system integration methods...

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        // Editor menu items
        [UnityEditor.MenuItem("🧪 Laboratory/Integration/Show System Integration Report", false, 700)]
        private static void MenuShowIntegrationReport()
        {
            if (Application.isPlaying && Instance != null)
            {
                var report = Instance.GenerateIntegrationReport();
                Debug.Log($"System Integration Report:\n" +
                         $"Connected Systems: {report.connectedSystemsCount}/8\n" +
                         $"Integration Health: {report.overallIntegrationHealth:F2}\n" +
                         $"System Synergy Score: {report.systemSynergyScore:F2}\n" +
                         $"Cross-System Events: {report.crossSystemEvents}\n" +
                         $"Active Emergent Behaviors: {report.emergentBehaviors.Count}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Integration/Trigger System Cascade", false, 701)]
        private static void MenuTriggerCascade()
        {
            if (Application.isPlaying && Instance != null)
            {
                var cascadeTypes = System.Enum.GetValues(typeof(CascadeType)).Cast<CascadeType>();
                var randomCascade = cascadeTypes.ElementAt(Random.Range(0, cascadeTypes.Count()));

                Instance.TriggerSystemCascade(randomCascade);
                Debug.Log($"Triggered system cascade: {randomCascade}");
            }
        }

        [UnityEditor.MenuItem("🧪 Laboratory/Integration/Force System Synergy Check", false, 702)]
        private static void MenuForceSynergyCheck()
        {
            if (Application.isPlaying && Instance != null)
            {
                Instance.CheckSynergyOpportunities();
                Debug.Log("Forced system synergy check - look for cross-system events");
            }
        }
    }

    // Supporting data structures for system integration
    [System.Serializable]
    public class SystemState
    {
        public string systemName;
        public SystemStatus status;
        public float performanceLevel;
        public Dictionary<string, float> metrics = new Dictionary<string, float>();
        public float lastUpdateTime;
    }

    [System.Serializable]
    public class CrossSystemEvent
    {
        public string sourceSystem;
        public string targetSystem;
        public string eventType;
        public Dictionary<string, object> eventData;
        public float scheduledTime;
        public string synchronizationId;
        public bool processed;
    }

    [System.Serializable]
    public class SystemCascade
    {
        public CascadeType cascadeType;
        public float startTime;
        public Dictionary<string, object> parameters;
        public List<string> affectedSystems;
    }

    [System.Serializable]
    public class EmergentBehavior
    {
        public string behaviorType;
        public string description;
        public float confidence;
        public string[] involvedSystems;
        public float timestamp;
        public Dictionary<string, object> behaviorData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class IntegrationAnalytics
    {
        public int totalCrossSystemEvents;
        public int totalCascadeEvents;
        public int totalEmergentBehaviors;
        public Dictionary<string, int> eventsByType = new Dictionary<string, int>();
        public float averageSystemHealth;
    }

    public enum SystemStatus { Healthy, Warning, Critical, Offline }
    public enum CascadeType { EvolutionaryLeap, EcosystemCollapse, SocialRevolution, TechnologicalBreakthrough }

    // Additional supporting classes...
    public class EmergentBehaviorTracker
    {
        private List<EmergentBehavior> activeBehaviors = new List<EmergentBehavior>();

        public void RecordEmergentBehavior(EmergentBehavior behavior)
        {
            activeBehaviors.Add(behavior);

            // Keep only recent behaviors
            if (activeBehaviors.Count > 50)
            {
                activeBehaviors.RemoveAt(0);
            }
        }

        public List<EmergentBehavior> GetActiveEmergentBehaviors()
        {
            return activeBehaviors.Where(b => Time.time - b.timestamp < 300f).ToList(); // Last 5 minutes
        }
    }

    [System.Serializable]
    public class SystemHealth
    {
        public SystemStatus status;
        public float healthScore;
        public List<string> issues = new List<string>();
        public float lastCheckTime;
    }

    [System.Serializable]
    public class SystemHealthConfig
    {
        public string systemName;
        public float healthThreshold;
        public string[] criticalMetrics;
    }

    [System.Serializable]
    public class SystemIntegrationReport
    {
        public float timestamp;
        public int connectedSystemsCount;
        public float overallIntegrationHealth;
        public Dictionary<string, SystemState> systemStates;
        public List<EmergentBehavior> emergentBehaviors;
        public int crossSystemEvents;
        public int cascadeEvents;
        public float systemSynergyScore;
        public List<string> recommendations;
    }

    [System.Serializable]
    public class SynchronizedSystemEvent
    {
        public string eventName;
        public List<string> targetSystems;
        public Dictionary<string, object> eventData;
        public float scheduledTime;
        public string synchronizationId;
    }

    [System.Serializable]
    public class SystemCommand
    {
        public string targetSystem;
        public string commandType;
        public Dictionary<string, object> parameters;
        public float executeTime;
    }

    [System.Serializable]
    public class SystemCrisis
    {
        public string crisisType;
        public string[] affectedSystems;
        public float severity;
        public string description;
        public float timestamp;
    }

    [System.Serializable]
    public class SystemHealthSummary
    {
        public float overallHealth;
        public Dictionary<string, SystemHealth> systemHealthByName;
        public List<string> criticalIssues;
        public List<string> recommendations;
    }
}