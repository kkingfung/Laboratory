using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using Laboratory.Core.Events;
using Laboratory.Core.Enums;
using Laboratory.Systems.Ecosystem;

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

        // Connected systems (using MonoBehaviour for loose coupling)
        private MonoBehaviour evolutionManager;
        private MonoBehaviour personalityManager;
        private MonoBehaviour ecosystemSimulator;
        private MonoBehaviour analyticsTracker;
        private MonoBehaviour questGenerator;
        private MonoBehaviour breedingSimulator;
        private MonoBehaviour storytellerSystem;
        private MonoBehaviour debugConsole;

        // Integration state - PERFORMANCE OPTIMIZED
        private Dictionary<Laboratory.Core.Enums.SystemType, SystemState> systemStates = new Dictionary<Laboratory.Core.Enums.SystemType, SystemState>();
        private List<CrossSystemEvent> pendingEvents = new List<CrossSystemEvent>();
        private Queue<SystemCommand> commandQueue = new Queue<SystemCommand>();

        // Emergent behavior tracking - PERFORMANCE OPTIMIZED
        private EmergentBehaviorTracker behaviorTracker;
        private Dictionary<TraitType, float> emergentScores = new Dictionary<TraitType, float>(); // Reuse trait system for emergent behavior scoring

        // System health monitoring - PERFORMANCE OPTIMIZED
        private Dictionary<Laboratory.Core.Enums.SystemType, SystemHealth> systemHealthStatus = new Dictionary<Laboratory.Core.Enums.SystemType, SystemHealth>();
        private bool integrationDataInitialized = false;
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
            InitializeIntegrationData();
            ConnectToAllSystems();
            InitializeEmergentBehaviorTracking();
            StartSystemOrchestration();
        }

        private void OnDestroy()
        {
            DisposeIntegrationData();
            if (instance == this)
            {
                instance = null;
            }
        }

        private void InitializeIntegrationData()
        {
            if (!integrationDataInitialized)
            {
                systemStates = new SystemStateArray(Allocator.Persistent);
                systemHealthStatus = new SystemHealthArray(Allocator.Persistent);

                // Initialize emergent scores with enum-based dictionary
                foreach (TraitType traitType in System.Enum.GetValues(typeof(TraitType)))
                {
                    emergentScores[traitType] = 0.5f;
                }

                integrationDataInitialized = true;

                Debug.Log("SystemIntegrationManager: Performance-optimized data structures initialized");
            }
        }

        private void DisposeIntegrationData()
        {
            if (integrationDataInitialized)
            {
                systemStates.Dispose();
                systemHealthStatus.Dispose();
                integrationDataInitialized = false;
            }
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
                debugConsole = FindFirstObjectByType<EnhancedDebugConsole>();
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
                systemStates = ConvertSystemStatesToDictionary(),
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

        /// <summary>
        /// Convert optimized SystemStateArray back to Dictionary for backward compatibility
        /// </summary>
        private Dictionary<SystemType, SystemState> ConvertSystemStatesToDictionary()
        {
            var dictionary = new Dictionary<SystemType, SystemState>();

            if (!integrationDataInitialized) return dictionary;

            foreach (SystemType systemId in System.Enum.GetValues(typeof(SystemType)))
            {
                dictionary[systemId] = systemStates[systemId];
            }

            return dictionary;
        }

        private void UpdateAllSystemStates()
        {
            if (!integrationDataInitialized) return;

            // Update each system's state based on their current status - PERFORMANCE OPTIMIZED
            if (evolutionManager != null)
            {
                systemStates[SystemType.Evolution] = AnalyzeEvolutionSystemState();
            }

            if (ecosystemSimulator != null)
            {
                systemStates[SystemType.Ecosystem] = AnalyzeEcosystemSystemState();
            }

            if (personalityManager != null)
            {
                systemStates[SystemType.AI] = AnalyzePersonalitySystemState();
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

            if (health == EcosystemHealth.Excellent && emergentScores.GetValueOrDefault(TraitType.Adaptability, 0f) > 0.8f)
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
        private void HandleEliteCreatureEmergence(object eliteCreature)
        {
            // Cascade this event to multiple systems
            var eventData = new Dictionary<string, object>
            {
                ["creature"] = eliteCreature,
                ["timestamp"] = System.DateTime.UtcNow
            };

            // Notify storyteller for narrative
            TriggerCrossSystemEvent("EliteEmergence", "Evolution", "Storytelling", eventData);

            // Notify quest system for special quests
            TriggerCrossSystemEvent("EliteEmergence", "Evolution", "Quest", eventData);

            // Notify analytics for tracking
            TriggerCrossSystemEvent("EliteEmergence", "Evolution", "Analytics", eventData);
        }

        private void HandleEnvironmentalEvent(object envEvent)
        {
            var eventData = new Dictionary<string, object>
            {
                ["event"] = envEvent,
                ["timestamp"] = System.DateTime.UtcNow
            };

            // Propagate to personality system (creatures react emotionally)
            TriggerCrossSystemEvent("EnvironmentalImpact", "Ecosystem", "Personality", eventData);

            // Propagate to quest system (environmental challenges)
            TriggerCrossSystemEvent("EnvironmentalChallenge", "Ecosystem", "Quest", eventData);

            // Propagate to storyteller (dramatic events)
            TriggerCrossSystemEvent("EnvironmentalNarrative", "Ecosystem", "Storytelling", eventData);
        }

        private void HandleQuestCompletion(object quest)
        {
            // Use reflection to safely access quest properties
            var questType = quest.GetType();
            var typeProperty = questType.GetProperty("type");
            var difficultyProperty = questType.GetProperty("difficulty");

            var questTypeValue = typeProperty?.GetValue(quest);
            var difficultyValue = difficultyProperty?.GetValue(quest);

            var eventData = new Dictionary<string, object>
            {
                ["quest"] = quest,
                ["questType"] = questTypeValue?.ToString() ?? "Unknown",
                ["difficulty"] = difficultyValue ?? 0f,
                ["timestamp"] = System.DateTime.UtcNow
            };

            // Reward ecosystem health for survival quests
            if (questTypeValue?.ToString() == "Survival")
            {
                TriggerCrossSystemEvent("QuestReward", "Quest", "Ecosystem", eventData);
            }

            // Generate narrative for high difficulty quest completions
            var difficulty = Convert.ToSingle(difficultyValue ?? 0f);
            if (difficulty > 0.7f)
            {
                TriggerCrossSystemEvent("QuestNarrative", "Quest", "Storytelling", eventData);
            }

            // Always trigger general quest completion event
            TriggerCrossSystemEvent("QuestCompletion", "Quest", "Analytics", eventData);
        }

        private void UpdateEmergentScores()
        {
            // Calculate emergent scores based on current system state using enum-based dictionary
            if (evolutionManager != null && systemStates.IsCreated)
            {
                emergentScores[TraitType.Adaptability] =
                    Mathf.Clamp01(evolutionManager.AveragePopulationFitness * 0.8f + systemStates[SystemType.Evolution].performanceLevel * 0.2f);
            }

            if (ecosystemSimulator != null && systemStates.IsCreated)
            {
                float ecosystemScore = ConvertEcosystemHealthToFloat(ecosystemSimulator.OverallHealth);
                emergentScores[TraitType.ColdTolerance] =
                    Mathf.Clamp01(ecosystemScore * 0.9f + systemStates[SystemType.Ecosystem].performanceLevel * 0.1f);
            }

            if (personalityManager != null && systemStates.IsCreated)
            {
                emergentScores[TraitType.Sociability] =
                    Mathf.Clamp01(personalityManager.ActivePersonalityCount / 20f + systemStates[SystemType.AI].performanceLevel * 0.3f);
            }
        }

        private float ConvertEcosystemHealthToFloat(EcosystemHealth health)
        {
            return health switch
            {
                EcosystemHealth.Excellent => 1.0f,
                EcosystemHealth.Good => 0.8f,
                EcosystemHealth.Average => 0.6f,
                EcosystemHealth.Poor => 0.3f,
                EcosystemHealth.Critical => 0.1f,
                _ => 0.5f
            };
        }

        private void InitializeSystemStates()
        {
            if (!integrationDataInitialized) return;

            // Initialize all system states to default values
            foreach (SystemType systemId in System.Enum.GetValues(typeof(SystemType)))
            {
                systemStates[systemId] = new SystemState
                {
                    systemName = systemId.ToString(),
                    status = SystemStatus.Healthy,
                    performanceLevel = 0.8f,
                    lastUpdateTime = Time.time
                };
            }
        }

        private void InitializeHealthMonitoring()
        {
            if (!integrationDataInitialized) return;

            foreach (SystemType systemId in System.Enum.GetValues(typeof(SystemType)))
            {
                systemHealthStatus[systemId] = new SystemHealth
                {
                    status = SystemStatus.Healthy,
                    healthScore = 1.0f,
                    lastCheckTime = Time.time
                };
            }
        }

        private bool AllSystemsConnected()
        {
            return CountConnectedSystems() >= 6; // Minimum viable system count
        }

        private int CountConnectedSystems()
        {
            int count = 0;
            if (evolutionManager != null) count++;
            if (personalityManager != null) count++;
            if (ecosystemSimulator != null) count++;
            if (analyticsTracker != null) count++;
            if (questGenerator != null) count++;
            if (breedingSimulator != null) count++;
            if (storytellerSystem != null) count++;
            if (debugConsole != null) count++;
            return count;
        }

        private float CalculateOverallIntegrationHealth()
        {
            if (!integrationDataInitialized) return 0.5f;

            float totalHealth = 0f;
            int systemCount = 0;

            foreach (SystemType systemId in System.Enum.GetValues(typeof(SystemType)))
            {
                totalHealth += systemHealthStatus[systemId].healthScore;
                systemCount++;
            }

            return systemCount > 0 ? totalHealth / systemCount : 0.5f;
        }

        private float CalculateSystemSynergyScore()
        {
            float synergyScore = systemSynergy;

            // Adjust based on active cross-system events
            if (pendingEvents.Count > 0)
            {
                synergyScore += Mathf.Min(pendingEvents.Count * 0.1f, 0.2f);
            }

            // Adjust based on emergent behaviors
            if (behaviorTracker != null)
            {
                var activeBehaviors = behaviorTracker.GetActiveEmergentBehaviors();
                synergyScore += Mathf.Min(activeBehaviors.Count * 0.05f, 0.15f);
            }

            return Mathf.Clamp01(synergyScore);
        }

        private List<string> GenerateIntegrationRecommendations()
        {
            var recommendations = new List<string>();

            int connectedSystems = CountConnectedSystems();
            if (connectedSystems < 8)
            {
                recommendations.Add($"Connect remaining {8 - connectedSystems} systems for full integration");
            }

            float overallHealth = CalculateOverallIntegrationHealth();
            if (overallHealth < 0.7f)
            {
                recommendations.Add("Investigate system health issues - overall health below threshold");
            }

            if (pendingEvents.Count > maxConcurrentEvents)
            {
                recommendations.Add("High event load detected - consider optimizing cross-system communication");
            }

            if (emergentScores.Values.All(score => score < 0.6f))
            {
                recommendations.Add("Low emergent behavior scores - systems may need more interaction");
            }

            return recommendations;
        }

        private SystemHealthSummary GenerateHealthSummary()
        {
            var summary = new SystemHealthSummary
            {
                overallHealth = CalculateOverallIntegrationHealth(),
                systemHealthByName = new Dictionary<string, SystemHealth>(),
                criticalIssues = new List<string>(),
                recommendations = new List<string>()
            };

            if (!integrationDataInitialized) return summary;

            foreach (SystemType systemId in System.Enum.GetValues(typeof(SystemType)))
            {
                var health = systemHealthStatus[systemId];
                summary.systemHealthByName[systemId.ToString()] = health;

                if (health.status == SystemStatus.Critical)
                {
                    summary.criticalIssues.Add($"{systemId} system is in critical state");
                }
            }

            return summary;
        }

        private void UpdateIntegrationAnalytics()
        {
            analytics.averageSystemHealth = CalculateOverallIntegrationHealth();
            analytics.totalEmergentBehaviors = behaviorTracker?.GetActiveEmergentBehaviors().Count ?? 0;
        }

        private void ProcessCommandQueue()
        {
            while (commandQueue.Count > 0 && commandQueue.Peek().executeTime <= Time.time)
            {
                var command = commandQueue.Dequeue();
                ExecuteSystemCommand(command);
            }
        }

        private void ProcessPendingEvents()
        {
            for (int i = pendingEvents.Count - 1; i >= 0; i--)
            {
                var evt = pendingEvents[i];
                if (evt.scheduledTime <= Time.time && !evt.processed)
                {
                    ExecuteCrossSystemEvent(evt);
                    evt.processed = true;
                    pendingEvents.RemoveAt(i);
                }
            }
        }

        private void ExecuteSystemCommand(SystemCommand command)
        {
            Debug.Log($"Executing system command: {command.commandType} on {command.targetSystem}");
        }

        private void ExecuteCrossSystemEvent(CrossSystemEvent evt)
        {
            Debug.Log($"Executing cross-system event: {evt.eventType} from {evt.sourceSystem} to {evt.targetSystem}");
        }

        private void PerformSystemHealthCheck()
        {
            if (!integrationDataInitialized) return;

            foreach (SystemType systemId in System.Enum.GetValues(typeof(SystemType)))
            {
                var currentHealth = AnalyzeSystemHealth(systemId);
                var previousHealth = systemHealthStatus[systemId];

                if (currentHealth.status != previousHealth.status)
                {
                    OnSystemHealthChanged?.Invoke(systemId.ToString(), currentHealth);
                }

                systemHealthStatus[systemId] = currentHealth;
            }
        }

        private SystemHealth AnalyzeSystemHealth(SystemType systemId)
        {
            var health = new SystemHealth
            {
                status = SystemStatus.Healthy,
                healthScore = 1.0f,
                lastCheckTime = Time.time
            };

            MonoBehaviour targetSystem = systemId switch
            {
                SystemType.Evolution => evolutionManager,
                SystemType.AI => personalityManager,
                SystemType.Ecosystem => ecosystemSimulator,
                SystemType.Analytics => analyticsTracker,
                SystemType.Quest => questGenerator,
                SystemType.Breeding => breedingSimulator,
                SystemType.Storytelling => storytellerSystem,
                _ => null
            };

            if (targetSystem == null)
            {
                health.status = SystemStatus.Offline;
                health.healthScore = 0f;
                health.issues.Add("System not connected");
            }
            else if (!targetSystem.gameObject.activeInHierarchy)
            {
                health.status = SystemStatus.Critical;
                health.healthScore = 0.2f;
                health.issues.Add("System inactive");
            }

            return health;
        }

        private SystemState AnalyzeEvolutionSystemState()
        {
            return new SystemState
            {
                systemName = "Evolution",
                status = evolutionManager?.gameObject.activeInHierarchy == true ? SystemStatus.Healthy : SystemStatus.Offline,
                performanceLevel = evolutionManager?.AveragePopulationFitness ?? 0.5f,
                lastUpdateTime = Time.time
            };
        }

        private SystemState AnalyzeEcosystemSystemState()
        {
            return new SystemState
            {
                systemName = "Ecosystem",
                status = ecosystemSimulator?.gameObject.activeInHierarchy == true ? SystemStatus.Healthy : SystemStatus.Offline,
                performanceLevel = ConvertEcosystemHealthToFloat(ecosystemSimulator?.OverallHealth ?? EcosystemHealth.Average),
                lastUpdateTime = Time.time
            };
        }

        private SystemState AnalyzePersonalitySystemState()
        {
            return new SystemState
            {
                systemName = "Personality",
                status = personalityManager?.gameObject.activeInHierarchy == true ? SystemStatus.Healthy : SystemStatus.Offline,
                performanceLevel = personalityManager != null ? Mathf.Clamp01(personalityManager.ActivePersonalityCount / 20f) : 0.5f,
                lastUpdateTime = Time.time
            };
        }

        private void ExecuteEvolutionaryLeapCascade(SystemCascade cascade)
        {
            cascade.affectedSystems.AddRange(new[] { "Evolution", "Ecosystem", "Storytelling", "Quest" });

            TriggerCrossSystemEvent("EvolutionaryLeap", "Evolution", "Ecosystem", cascade.parameters);
            TriggerCrossSystemEvent("EvolutionaryLeap", "Evolution", "Storytelling", cascade.parameters);
            TriggerCrossSystemEvent("EvolutionaryLeap", "Evolution", "Quest", cascade.parameters);

            Debug.Log("Executed Evolutionary Leap cascade");
        }

        private void ExecuteEcosystemCollapseCascade(SystemCascade cascade)
        {
            cascade.affectedSystems.AddRange(new[] { "Ecosystem", "Evolution", "Quest", "Analytics" });

            TriggerCrossSystemEvent("EcosystemCollapse", "Ecosystem", "Evolution", cascade.parameters);
            TriggerCrossSystemEvent("EcosystemCollapse", "Ecosystem", "Quest", cascade.parameters);
            TriggerCrossSystemEvent("EcosystemCollapse", "Ecosystem", "Analytics", cascade.parameters);

            Debug.Log("Executed Ecosystem Collapse cascade");
        }

        private void ExecuteSocialRevolutionCascade(SystemCascade cascade)
        {
            cascade.affectedSystems.AddRange(new[] { "Personality", "Quest", "Storytelling", "Analytics" });

            TriggerCrossSystemEvent("SocialRevolution", "Personality", "Quest", cascade.parameters);
            TriggerCrossSystemEvent("SocialRevolution", "Personality", "Storytelling", cascade.parameters);
            TriggerCrossSystemEvent("SocialRevolution", "Personality", "Analytics", cascade.parameters);

            Debug.Log("Executed Social Revolution cascade");
        }

        private void ExecuteTechnologicalBreakthroughCascade(SystemCascade cascade)
        {
            cascade.affectedSystems.AddRange(new[] { "Analytics", "Evolution", "Quest", "Breeding" });

            TriggerCrossSystemEvent("TechnologicalBreakthrough", "Analytics", "Evolution", cascade.parameters);
            TriggerCrossSystemEvent("TechnologicalBreakthrough", "Analytics", "Quest", cascade.parameters);
            TriggerCrossSystemEvent("TechnologicalBreakthrough", "Analytics", "Breeding", cascade.parameters);

            Debug.Log("Executed Technological Breakthrough cascade");
        }

        private void DetectNarrativeComplexity()
        {
            if (storytellerSystem == null) return;

            int activeStories = storytellerSystem.ActiveStories?.Count ?? 0;

            if (activeStories > 3 && emergentScores.GetValueOrDefault(TraitType.Intelligence, 0f) > 0.7f)
            {
                var behavior = new EmergentBehavior
                {
                    behaviorType = "NarrativeComplexity",
                    description = "Multiple interconnected stories creating narrative complexity",
                    confidence = 0.8f,
                    involvedSystems = new[] { "Storytelling", "Quest", "Analytics" },
                    timestamp = Time.time
                };

                behaviorTracker.RecordEmergentBehavior(behavior);
                OnEmergentBehaviorDetected?.Invoke(behavior);
            }
        }

        private void DetectSocialComplexity()
        {
            if (personalityManager == null) return;

            int activePersonalities = personalityManager.ActivePersonalityCount;

            if (activePersonalities > 15 && emergentScores.GetValueOrDefault(TraitType.Sociability, 0f) > 0.8f)
            {
                var behavior = new EmergentBehavior
                {
                    behaviorType = "SocialComplexity",
                    description = "High density of personality interactions creating emergent social behaviors",
                    confidence = 0.85f,
                    involvedSystems = new[] { "Personality", "Evolution", "Quest" },
                    timestamp = Time.time
                };

                behaviorTracker.RecordEmergentBehavior(behavior);
                OnEmergentBehaviorDetected?.Invoke(behavior);
            }
        }

        // Event handler stubs for missing system events
        private void HandleEvolutionaryMilestone(object milestone) { }
        private void HandleSocialInteraction(object interaction) { }
        private void HandleMoodChange(object moodChange) { }
        private void HandleBiomeHealthChange(object biomeHealth) { }
        private void HandleEcosystemStress(object stressLevel) { }
        private void HandlePlayerArchetypeChange(object archetype) { }
        private void HandleGameAdaptation(object adaptation) { }
        private void HandleQuestGeneration(object quest) { }
        private void HandleBreedingCompletion(object breedingSession, object offspring) { }
        private void HandleOffspringAcceptance(object offspring) { }
        private void HandleStoryGeneration(object story) { }
        private void HandleNarrativeUpdate(object narrative) { }


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
        public Dictionary<SystemType, float> metrics = new Dictionary<SystemType, float>();
        public float lastUpdateTime;
    }

    [System.Serializable]
    public class CrossSystemEvent
    {
        public string sourceSystem;
        public string targetSystem;
        public string eventType;
        public Dictionary<SystemType, object> eventData;
        public float scheduledTime;
        public string synchronizationId;
        public bool processed;
    }

    [System.Serializable]
    public class SystemCascade
    {
        public CascadeType cascadeType;
        public float startTime;
        public Dictionary<SystemType, object> parameters;
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
        public Dictionary<SystemType, object> behaviorData = new Dictionary<SystemType, object>();
    }

    [System.Serializable]
    public class IntegrationAnalytics
    {
        public int totalCrossSystemEvents;
        public int totalCascadeEvents;
        public int totalEmergentBehaviors;
        public Dictionary<SystemType, int> eventsByType = new Dictionary<SystemType, int>();
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
        public Dictionary<SystemType, SystemState> systemStates;
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
        public Dictionary<SystemType, object> eventData;
        public float scheduledTime;
        public string synchronizationId;
    }

    [System.Serializable]
    public class SystemCommand
    {
        public string targetSystem;
        public string commandType;
        public Dictionary<SystemType, object> parameters;
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
        public Dictionary<SystemType, SystemHealth> systemHealthByName;
        public List<string> criticalIssues;
        public List<string> recommendations;
    }
}