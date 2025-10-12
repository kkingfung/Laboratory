using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Subsystems.Analytics
{
    /// <summary>
    /// Analytics & Telemetry Subsystem Manager for Project Chimera.
    /// Tracks player behavior, breeding patterns, discovery metrics, and performance data.
    /// Essential for balancing genetic rarity and understanding player engagement patterns.
    /// </summary>
    public class AnalyticsSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private AnalyticsSubsystemConfig config;

        [Header("Services")]
        [SerializeField] private bool enablePlayerTracking = true;
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private bool enableEducationalAnalytics = true;
        [SerializeField] private bool enablePrivacyMode = false;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Analytics";
        public float InitializationProgress { get; private set; }

        // Services
        public IPlayerBehaviorTracker PlayerBehaviorTracker { get; private set; }
        public IPerformanceMonitor PerformanceMonitor { get; private set; }
        public IEducationalAnalytics EducationalAnalytics { get; private set; }
        public IDiscoveryMetrics DiscoveryMetrics { get; private set; }

        // Events
        public static event Action<PlayerActionEvent> OnPlayerActionTracked;
        public static event Action<DiscoveryAnalyticsEvent> OnDiscoveryTracked;
        public static event Action<PerformanceMetrics> OnPerformanceMetricsUpdated;
        public static event Action<EducationalProgressEvent> OnEducationalProgressTracked;

        private readonly Queue<AnalyticsEvent> _eventQueue = new();
        private readonly Dictionary<string, float> _sessionMetrics = new();
        private readonly AnalyticsSessionData _currentSession = new();

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            _ = InitializeAsync();
        }

        private void Update()
        {
            ProcessEventQueue();
            UpdateSessionMetrics();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrackEvent(new PlayerActionEvent
                {
                    actionType = "SessionPaused",
                    timestamp = DateTime.Now
                });
            }
            else
            {
                TrackEvent(new PlayerActionEvent
                {
                    actionType = "SessionResumed",
                    timestamp = DateTime.Now
                });
            }
        }

        #endregion

        #region Initialization

        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign an AnalyticsSubsystemConfig.");
                return;
            }

            // Check privacy compliance
            if (enableEducationalAnalytics && config.requiresParentalConsent)
            {
                Debug.Log($"[{SubsystemName}] Educational analytics enabled with parental consent requirement");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.2f;

            // Initialize tracking services
            PlayerBehaviorTracker = new PlayerBehaviorTracker(config);
            PerformanceMonitor = new PerformanceMonitor(config);
            EducationalAnalytics = new EducationalAnalytics(config);
            DiscoveryMetrics = new DiscoveryMetrics(config);

            InitializationProgress = 0.4f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.5f;

                // Start session tracking
                StartNewSession();

                InitializationProgress = 0.6f;

                // Subscribe to game events
                SubscribeToGameEvents();

                InitializationProgress = 0.8f;

                // Register services
                RegisterServices();

                InitializationProgress = 0.9f;

                // Start background analytics processing
                _ = StartAnalyticsProcessingLoop();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Player Tracking: {enablePlayerTracking}, " +
                         $"Performance: {enablePerformanceMonitoring}, " +
                         $"Educational: {enableEducationalAnalytics}, " +
                         $"Privacy Mode: {enablePrivacyMode}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private void StartNewSession()
        {
            _currentSession.sessionId = Guid.NewGuid().ToString();
            _currentSession.startTime = DateTime.Now;
            _currentSession.playerActions = new List<PlayerActionEvent>();
            _currentSession.discoveries = new List<DiscoveryAnalyticsEvent>();
            _currentSession.performanceSnapshots = new List<PerformanceMetrics>();

            Debug.Log($"[{SubsystemName}] Started new analytics session: {_currentSession.sessionId}");
        }

        private void SubscribeToGameEvents()
        {
            // Subscribe to genetics events
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete += HandleBreedingComplete;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred += HandleMutationOccurred;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered += HandleTraitDiscovered;

            // Subscribe to ecosystem events
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnWeatherChanged += HandleWeatherChanged;
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnBiomeChanged += HandleBiomeChanged;
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent += HandleEnvironmentalEvent;
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.RegisterService<IPlayerBehaviorTracker>(PlayerBehaviorTracker);
                ServiceContainer.Instance.RegisterService<IPerformanceMonitor>(PerformanceMonitor);
                ServiceContainer.Instance.RegisterService<IEducationalAnalytics>(EducationalAnalytics);
                ServiceContainer.Instance.RegisterService<IDiscoveryMetrics>(DiscoveryMetrics);
                ServiceContainer.Instance.RegisterService<AnalyticsSubsystemManager>(this);
            }
        }

        #endregion

        #region Core Analytics Operations

        /// <summary>
        /// Tracks a general analytics event
        /// </summary>
        public void TrackEvent(AnalyticsEvent analyticsEvent)
        {
            if (!IsInitialized || enablePrivacyMode)
                return;

            analyticsEvent.timestamp = DateTime.Now;
            analyticsEvent.sessionId = _currentSession.sessionId;

            _eventQueue.Enqueue(analyticsEvent);
        }

        /// <summary>
        /// Tracks player behavior specifically
        /// </summary>
        public void TrackPlayerAction(string actionType, Dictionary<string, object> parameters = null)
        {
            if (!enablePlayerTracking)
                return;

            var actionEvent = new PlayerActionEvent
            {
                actionType = actionType,
                parameters = parameters ?? new Dictionary<string, object>(),
                timestamp = DateTime.Now,
                sessionId = _currentSession.sessionId
            };

            TrackEvent(actionEvent);
            OnPlayerActionTracked?.Invoke(actionEvent);
        }

        /// <summary>
        /// Tracks breeding pattern analytics
        /// </summary>
        public void TrackBreedingPattern(string parent1Species, string parent2Species, bool wasSuccessful,
            List<string> resultingTraits = null)
        {
            var parameters = new Dictionary<string, object>
            {
                ["parent1Species"] = parent1Species,
                ["parent2Species"] = parent2Species,
                ["successful"] = wasSuccessful,
                ["resultingTraitCount"] = resultingTraits?.Count ?? 0
            };

            if (resultingTraits != null)
            {
                parameters["resultingTraits"] = string.Join(",", resultingTraits);
            }

            TrackPlayerAction("BreedingAttempt", parameters);
        }

        /// <summary>
        /// Tracks discovery events for balancing
        /// </summary>
        public void TrackDiscovery(string discoveryType, string discoveredItem, bool isWorldFirst = false)
        {
            var discoveryEvent = new DiscoveryAnalyticsEvent
            {
                discoveryType = discoveryType,
                discoveredItem = discoveredItem,
                isWorldFirst = isWorldFirst,
                timestamp = DateTime.Now,
                sessionId = _currentSession.sessionId
            };

            TrackEvent(discoveryEvent);
            OnDiscoveryTracked?.Invoke(discoveryEvent);

            // Update discovery metrics
            DiscoveryMetrics.RecordDiscovery(discoveryEvent);
        }

        /// <summary>
        /// Tracks educational progress for school environments
        /// </summary>
        public void TrackEducationalProgress(string lessonId, string conceptMastered, float confidenceLevel)
        {
            if (!enableEducationalAnalytics)
                return;

            var progressEvent = new EducationalProgressEvent
            {
                lessonId = lessonId,
                conceptMastered = conceptMastered,
                confidenceLevel = confidenceLevel,
                timestamp = DateTime.Now,
                sessionId = _currentSession.sessionId
            };

            TrackEvent(progressEvent);
            OnEducationalProgressTracked?.Invoke(progressEvent);

            // Update educational analytics
            EducationalAnalytics.RecordProgress(progressEvent);
        }

        /// <summary>
        /// Gets current session analytics summary
        /// </summary>
        public AnalyticsSessionSummary GetSessionSummary()
        {
            return new AnalyticsSessionSummary
            {
                sessionId = _currentSession.sessionId,
                sessionDuration = DateTime.Now - _currentSession.startTime,
                totalActions = _currentSession.playerActions.Count,
                totalDiscoveries = _currentSession.discoveries.Count,
                breedingAttempts = _currentSession.playerActions.FindAll(a => a.actionType == "BreedingAttempt").Count,
                averagePerformance = PerformanceMonitor.GetAveragePerformance(),
                educationalProgress = EducationalAnalytics.GetOverallProgress()
            };
        }

        #endregion

        #region Event Processing

        private void ProcessEventQueue()
        {
            const int maxEventsPerFrame = 10;
            int processedCount = 0;

            while (_eventQueue.Count > 0 && processedCount < maxEventsPerFrame)
            {
                var analyticsEvent = _eventQueue.Dequeue();
                ProcessAnalyticsEvent(analyticsEvent);
                processedCount++;
            }
        }

        private void ProcessAnalyticsEvent(AnalyticsEvent analyticsEvent)
        {
            // Add to session data
            switch (analyticsEvent)
            {
                case PlayerActionEvent actionEvent:
                    _currentSession.playerActions.Add(actionEvent);
                    PlayerBehaviorTracker.ProcessPlayerAction(actionEvent);
                    break;

                case DiscoveryAnalyticsEvent discoveryEvent:
                    _currentSession.discoveries.Add(discoveryEvent);
                    break;

                case PerformanceMetrics performanceMetrics:
                    _currentSession.performanceSnapshots.Add(performanceMetrics);
                    break;

                case EducationalProgressEvent progressEvent:
                    EducationalAnalytics.RecordProgress(progressEvent);
                    break;
            }

            // Process real-time analytics
            if (config.enableRealTimeAnalytics)
            {
                SendToAnalyticsService(analyticsEvent);
            }
        }

        private void SendToAnalyticsService(AnalyticsEvent analyticsEvent)
        {
            // Implementation would send to external analytics service
            // For now, just log for debugging
            Debug.Log($"[{SubsystemName}] Analytics Event: {analyticsEvent.GetType().Name} at {analyticsEvent.timestamp}");
        }

        #endregion

        #region Performance Monitoring

        private void UpdateSessionMetrics()
        {
            if (!enablePerformanceMonitoring)
                return;

            // Update every second
            if (Time.unscaledTime % 1f < Time.unscaledDeltaTime)
            {
                var performanceMetrics = PerformanceMonitor.CollectCurrentMetrics();
                TrackEvent(performanceMetrics);
                OnPerformanceMetricsUpdated?.Invoke(performanceMetrics);
            }
        }

        /// <summary>
        /// Gets current performance analytics
        /// </summary>
        public PerformanceAnalyticsSummary GetPerformanceAnalytics()
        {
            return PerformanceMonitor.GetAnalyticsSummary();
        }

        #endregion

        #region Game Event Handlers

        private void HandleBreedingComplete(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            if (result != null)
            {
                var resultingTraits = new List<string>();
                if (result.offspring?.Genes != null)
                {
                    foreach (var gene in result.offspring.Genes)
                    {
                        resultingTraits.Add(gene.traitName);
                    }
                }

                TrackBreedingPattern("Unknown", "Unknown", result.isSuccessful, resultingTraits);

                // Track if this created a rare combination
                if (result.mutations.Count > 0)
                {
                    TrackDiscovery("Mutation", result.mutations[0].affectedTrait);
                }
            }
        }

        private void HandleMutationOccurred(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent)
        {
            TrackDiscovery("Mutation", mutationEvent.mutation.affectedTrait);
        }

        private void HandleTraitDiscovered(Laboratory.Subsystems.Genetics.TraitDiscoveryEvent discoveryEvent)
        {
            TrackDiscovery("Trait", discoveryEvent.traitName, discoveryEvent.isWorldFirst);
        }

        private void HandleWeatherChanged(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent)
        {
            TrackPlayerAction("WeatherExperienced", new Dictionary<string, object>
            {
                ["weatherType"] = weatherEvent.newWeatherData.weatherType.ToString(),
                ["intensity"] = weatherEvent.newWeatherData.intensity
            });
        }

        private void HandleBiomeChanged(Laboratory.Subsystems.Ecosystem.BiomeChangedEvent biomeEvent)
        {
            TrackPlayerAction("BiomeExplored", new Dictionary<string, object>
            {
                ["biomeId"] = biomeEvent.biomeId,
                ["changeType"] = biomeEvent.changeType.ToString()
            });
        }

        private void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent environmentalEvent)
        {
            TrackPlayerAction("EnvironmentalEventWitnessed", new Dictionary<string, object>
            {
                ["eventType"] = environmentalEvent.eventType.ToString(),
                ["severity"] = environmentalEvent.severity
            });
        }

        #endregion

        #region Background Processing

        private async Task StartAnalyticsProcessingLoop()
        {
            while (IsInitialized)
            {
                try
                {
                    // Process analytics in background
                    await Task.Delay(config.processingIntervalMs);

                    // Update behavioral patterns
                    PlayerBehaviorTracker.UpdateBehaviorPatterns();

                    // Check for performance anomalies
                    PerformanceMonitor.CheckForAnomalies();

                    // Update discovery rarity metrics
                    DiscoveryMetrics.UpdateRarityMetrics();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{SubsystemName}] Background processing error: {ex.Message}");
                }
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // End current session
            if (_currentSession != null)
            {
                _currentSession.endTime = DateTime.Now;

                // Save session data if configured
                if (config.saveSessionData)
                {
                    SaveSessionData();
                }
            }

            // Unsubscribe from events
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete -= HandleBreedingComplete;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred -= HandleMutationOccurred;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered -= HandleTraitDiscovered;

            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnWeatherChanged -= HandleWeatherChanged;
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnBiomeChanged -= HandleBiomeChanged;
            Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent -= HandleEnvironmentalEvent;

            // Clear collections
            _eventQueue.Clear();
            _sessionMetrics.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        private void SaveSessionData()
        {
            try
            {
                var sessionJson = JsonUtility.ToJson(_currentSession, true);
                var fileName = $"analytics_session_{_currentSession.sessionId}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = System.IO.Path.Combine(Application.persistentDataPath, "Analytics", fileName);

                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(filePath));
                System.IO.File.WriteAllText(filePath, sessionJson);

                Debug.Log($"[{SubsystemName}] Session data saved: {fileName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Failed to save session data: {ex.Message}");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Generate Test Analytics")]
        private void GenerateTestAnalytics()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Analytics subsystem not initialized");
                return;
            }

            // Generate some test data
            TrackPlayerAction("TestAction", new Dictionary<string, object> { ["testParam"] = "testValue" });
            TrackDiscovery("TestDiscovery", "TestTrait", false);
            TrackEducationalProgress("TestLesson", "TestConcept", 0.85f);
        }

        [ContextMenu("Print Session Summary")]
        private void PrintSessionSummary()
        {
            var summary = GetSessionSummary();
            Debug.Log($"Session Summary:\n" +
                     $"ID: {summary.sessionId}\n" +
                     $"Duration: {summary.sessionDuration}\n" +
                     $"Actions: {summary.totalActions}\n" +
                     $"Discoveries: {summary.totalDiscoveries}\n" +
                     $"Breeding Attempts: {summary.breedingAttempts}");
        }

        #endregion
    }
}