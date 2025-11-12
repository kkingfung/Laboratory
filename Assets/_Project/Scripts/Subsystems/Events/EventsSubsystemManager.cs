using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;

namespace Laboratory.Subsystems.Events
{
    /// <summary>
    /// Events & Celebrations Subsystem Manager for Project Chimera.
    /// Handles discovery celebrations, seasonal events, tournaments, world-first announcements,
    /// and community-wide celebrations that make genetic discoveries feel truly magical.
    /// </summary>
    public class EventsSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private EventsSubsystemConfig config;

        [Header("Services")]
        [SerializeField] private bool enableDiscoveryCelebrations = true;
        [SerializeField] private bool enableSeasonalEvents = true;
        [SerializeField] private bool enableTournaments = true;
        [SerializeField] private bool enableWorldFirstCelebrations = true;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Events";
        public float InitializationProgress { get; private set; }

        // Services
        public IDiscoveryCelebrationService DiscoveryCelebrationService { get; private set; }
        public ISeasonalEventService SeasonalEventService { get; private set; }
        public ITournamentService TournamentService { get; private set; }
        public ICommunityEventService CommunityEventService { get; private set; }

        // Events
        public static event Action<DiscoveryCelebrationEvent> OnDiscoveryCelebration;
        public static event Action<SeasonalEvent> OnSeasonalEvent;
        public static event Action<TournamentEvent> OnTournamentEvent;
        public static event Action<CommunityEvent> OnCommunityEvent;

        private readonly Dictionary<string, ActiveEvent> _activeEvents = new();
        private readonly Queue<EventTrigger> _eventQueue = new();
        private readonly Dictionary<string, PlayerEventHistory> _playerEventHistory = new();
        private readonly EventMetrics _eventMetrics = new();

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
            UpdateActiveEvents();
            CheckSeasonalTriggers();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void ValidateConfiguration()
        {
            if (config == null)
            {
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign an EventsSubsystemConfig.");
                return;
            }

            if (config.celebrationTemplates == null || config.celebrationTemplates.Count == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No celebration templates configured. Celebrations will be limited.");
            }

            if (config.seasonalEvents == null || config.seasonalEvents.Count == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No seasonal events configured. Seasonal content will be disabled.");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.2f;

            // Try to resolve services from service container
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                serviceContainer.TryResolve<IDiscoveryCelebrationService>(out var discoveryCelebrationService);
                serviceContainer.TryResolve<ISeasonalEventService>(out var seasonalEventService);
                serviceContainer.TryResolve<ITournamentService>(out var tournamentService);
                serviceContainer.TryResolve<ICommunityEventService>(out var communityEventService);

                DiscoveryCelebrationService = discoveryCelebrationService;
                SeasonalEventService = seasonalEventService;
                TournamentService = tournamentService;
                CommunityEventService = communityEventService;
            }

            if (config.enableDebugLogging)
            {
                Debug.Log("[EventsSubsystem] Event services resolved from service container");
                Debug.Log($"  DiscoveryCelebration: {(DiscoveryCelebrationService != null ? "Available" : "Not Available")}");
                Debug.Log($"  SeasonalEvent: {(SeasonalEventService != null ? "Available" : "Not Available")}");
                Debug.Log($"  Tournament: {(TournamentService != null ? "Available" : "Not Available")}");
                Debug.Log($"  CommunityEvent: {(CommunityEventService != null ? "Available" : "Not Available")}");
            }

            InitializationProgress = 0.4f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.5f;

                // Initialize discovery celebrations
                if (enableDiscoveryCelebrations)
                {
                    await DiscoveryCelebrationService.InitializeAsync();
                }
                InitializationProgress = 0.6f;

                // Initialize seasonal events
                if (enableSeasonalEvents)
                {
                    await SeasonalEventService.InitializeAsync();
                }
                InitializationProgress = 0.7f;

                // Initialize tournaments
                if (enableTournaments)
                {
                    await TournamentService.InitializeAsync();
                }
                InitializationProgress = 0.8f;

                // Initialize community events
                await CommunityEventService.InitializeAsync();
                InitializationProgress = 0.9f;

                // Subscribe to game events
                SubscribeToGameEvents();

                // Register services
                RegisterServices();

                // Start background event processing
                _ = StartEventProcessingLoop();

                // Initialize seasonal events for current date
                await InitializeCurrentSeasonalEvents();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Discovery Celebrations: {enableDiscoveryCelebrations}, " +
                         $"Seasonal Events: {enableSeasonalEvents}, " +
                         $"Tournaments: {enableTournaments}, " +
                         $"World First: {enableWorldFirstCelebrations}");

                // Notify system initialization
                EventBus.Publish(new SubsystemInitializedEvent(SubsystemName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{SubsystemName}] Initialization failed: {ex.Message}");
                InitializationProgress = 0f;
            }
        }

        private void SubscribeToGameEvents()
        {
            try
            {
                // Subscribe to genetics events for discovery celebrations
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete += HandleBreedingComplete;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred += HandleMutationOccurred;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered += HandleTraitDiscovered;

                // Subscribe to analytics events for discovery tracking
                Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnDiscoveryTracked += HandleDiscoveryTracked;

                // Subscribe to research events for publication celebrations
                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnPublicationCreated += HandlePublicationCreated;
                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnDiscoveryLogged += HandleResearchDiscovery;

                // Subscribe to ecosystem events for environmental celebrations
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnWeatherChanged += HandleWeatherChanged;
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent += HandleEnvironmentalEvent;

                Debug.Log($"[{SubsystemName}] Subscribed to game events successfully");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{SubsystemName}] Some event subscriptions failed: {ex.Message}");
            }
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.Register<IDiscoveryCelebrationService>(DiscoveryCelebrationService);
                ServiceContainer.Instance.Register<ISeasonalEventService>(SeasonalEventService);
                ServiceContainer.Instance.Register<ITournamentService>(TournamentService);
                ServiceContainer.Instance.Register<ICommunityEventService>(CommunityEventService);
                ServiceContainer.Instance.Register<EventsSubsystemManager>(this);
            }
        }

        private async Task InitializeCurrentSeasonalEvents()
        {
            if (enableSeasonalEvents)
            {
                await SeasonalEventService.CheckAndActivateSeasonalEvents(DateTime.Now);
            }
        }

        #endregion

        #region Core Event Operations

        /// <summary>
        /// Triggers a discovery celebration for a genetic discovery
        /// </summary>
        public async Task<bool> TriggerDiscoveryCelebrationAsync(DiscoveryData discoveryData, string playerId, bool isWorldFirst = false)
        {
            if (!IsInitialized || !enableDiscoveryCelebrations)
                return false;

            var celebration = await DiscoveryCelebrationService.CreateCelebrationAsync(discoveryData, playerId, isWorldFirst);

            if (celebration != null)
            {
                // Add to active events
                _activeEvents[celebration.eventId] = new ActiveEvent
                {
                    eventData = celebration,
                    startTime = DateTime.Now,
                    isActive = true
                };

                // Update player history
                UpdatePlayerEventHistory(playerId, celebration);

                // Fire celebration event
                var celebrationEvent = new DiscoveryCelebrationEvent
                {
                    celebration = celebration,
                    playerId = playerId,
                    isWorldFirst = isWorldFirst,
                    timestamp = DateTime.Now
                };

                OnDiscoveryCelebration?.Invoke(celebrationEvent);

                Debug.Log($"[{SubsystemName}] Discovery celebration triggered for {playerId}: {discoveryData.title}");
                return true;
            }

            return false;
        }

        /// <summary>
        /// Starts a seasonal event
        /// </summary>
        public async Task<bool> StartSeasonalEventAsync(string eventId, bool forceStart = false)
        {
            if (!enableSeasonalEvents)
                return false;

            return await SeasonalEventService.StartEventAsync(eventId, forceStart);
        }

        /// <summary>
        /// Creates a tournament event
        /// </summary>
        public async Task<Tournament> CreateTournamentAsync(TournamentRequest request, string organizerId)
        {
            if (!enableTournaments)
                return null;

            return await TournamentService.CreateTournamentAsync(request, organizerId);
        }

        /// <summary>
        /// Joins a player to a tournament
        /// </summary>
        public async Task<bool> JoinTournamentAsync(string tournamentId, string playerId, TournamentEntry entry)
        {
            if (!enableTournaments)
                return false;

            return await TournamentService.JoinTournamentAsync(tournamentId, playerId, entry);
        }

        /// <summary>
        /// Gets all active events
        /// </summary>
        public List<ActiveEvent> GetActiveEvents()
        {
            return new List<ActiveEvent>(_activeEvents.Values);
        }

        /// <summary>
        /// Gets active seasonal events
        /// </summary>
        public List<SeasonalEventData> GetActiveSeasonalEvents()
        {
            return SeasonalEventService.GetActiveEvents();
        }

        /// <summary>
        /// Gets active tournaments
        /// </summary>
        public List<Tournament> GetActiveTournaments()
        {
            return TournamentService.GetActiveTournaments();
        }

        /// <summary>
        /// Gets player event history
        /// </summary>
        public PlayerEventHistory GetPlayerEventHistory(string playerId)
        {
            return _playerEventHistory.GetValueOrDefault(playerId, new PlayerEventHistory { playerId = playerId });
        }

        /// <summary>
        /// Triggers a community-wide celebration
        /// </summary>
        public async Task<bool> TriggerCommunityEventAsync(CommunityEventType eventType, string description, Dictionary<string, object> eventData = null)
        {
            var communityEvent = new CommunityEventData
            {
                eventId = Guid.NewGuid().ToString(),
                eventType = eventType,
                title = GenerateCommunityEventTitle(eventType),
                description = description,
                startTime = DateTime.Now,
                duration = GetCommunityEventDuration(eventType),
                isActive = true,
                eventData = eventData ?? new Dictionary<string, object>()
            };

            return await CommunityEventService.TriggerEventAsync(communityEvent);
        }

        #endregion

        #region Event Processing

        private void ProcessEventQueue()
        {
            const int maxEventsPerFrame = 3;
            int processedCount = 0;

            while (_eventQueue.Count > 0 && processedCount < maxEventsPerFrame)
            {
                var eventTrigger = _eventQueue.Dequeue();
                ProcessEventTrigger(eventTrigger);
                processedCount++;
            }
        }

        private void ProcessEventTrigger(EventTrigger eventTrigger)
        {
            switch (eventTrigger.triggerType)
            {
                case EventTriggerType.Discovery:
                    ProcessDiscoveryTrigger(eventTrigger);
                    break;

                case EventTriggerType.Seasonal:
                    ProcessSeasonalTrigger(eventTrigger);
                    break;

                case EventTriggerType.Tournament:
                    ProcessTournamentTrigger(eventTrigger);
                    break;

                case EventTriggerType.Community:
                    ProcessCommunityTrigger(eventTrigger);
                    break;
            }
        }

        private void ProcessDiscoveryTrigger(EventTrigger eventTrigger)
        {
            if (eventTrigger.data.TryGetValue("discoveryData", out var discoveryObj) &&
                eventTrigger.data.TryGetValue("playerId", out var playerIdObj) &&
                discoveryObj is DiscoveryData discovery &&
                playerIdObj is string playerId)
            {
                var isWorldFirst = eventTrigger.data.GetValueOrDefault("isWorldFirst", false) is bool worldFirst && worldFirst;
                _ = TriggerDiscoveryCelebrationAsync(discovery, playerId, isWorldFirst);
            }
        }

        private void ProcessSeasonalTrigger(EventTrigger eventTrigger)
        {
            if (eventTrigger.data.TryGetValue("eventId", out var eventIdObj) && eventIdObj is string eventId)
            {
                _ = StartSeasonalEventAsync(eventId);
            }
        }

        private void ProcessTournamentTrigger(EventTrigger eventTrigger)
        {
            // Handle tournament-related triggers
            Debug.Log($"[{SubsystemName}] Processing tournament trigger: {eventTrigger.triggerType}");
        }

        private void ProcessCommunityTrigger(EventTrigger eventTrigger)
        {
            if (eventTrigger.data.TryGetValue("eventType", out var eventTypeObj) &&
                eventTrigger.data.TryGetValue("description", out var descriptionObj) &&
                eventTypeObj is CommunityEventType eventType &&
                descriptionObj is string description)
            {
                _ = TriggerCommunityEventAsync(eventType, description, eventTrigger.data);
            }
        }

        private void UpdateActiveEvents()
        {
            var now = DateTime.Now;
            var expiredEvents = new List<string>();

            foreach (var kvp in _activeEvents)
            {
                var activeEvent = kvp.Value;
                var eventAge = now - activeEvent.startTime;

                // Check if event has expired
                if (activeEvent.eventData != null && eventAge.TotalMinutes > GetEventDuration(activeEvent.eventData))
                {
                    expiredEvents.Add(kvp.Key);
                }
            }

            // Remove expired events
            foreach (var eventId in expiredEvents)
            {
                _activeEvents.Remove(eventId);
                Debug.Log($"[{SubsystemName}] Event expired: {eventId}");
            }
        }

        private void CheckSeasonalTriggers()
        {
            if (!enableSeasonalEvents)
                return;

            // Check for seasonal triggers every minute
            if (Time.unscaledTime % 60f < Time.unscaledDeltaTime)
            {
                SeasonalEventService.CheckSeasonalTriggers(DateTime.Now);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleBreedingComplete(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            if (result?.offspring != null && result.isSuccessful)
            {
                // Check if this breeding resulted in something celebration-worthy
                var celebrationWorthiness = CalculateBreedingCelebrationWorthiness(result);

                if (celebrationWorthiness >= config.minimumCelebrationThreshold)
                {
                    var discovery = new DiscoveryData
                    {
                        discoveryType = DiscoveryType.BreedingSuccess,
                        title = $"Exceptional Breeding Success",
                        description = $"Successfully bred {result.parent1Id} with {result.parent2Id}",
                        timestamp = DateTime.Now,
                        isSignificant = celebrationWorthiness >= config.significantDiscoveryThreshold
                    };

                    QueueDiscoveryEvent(discovery, "CurrentPlayer", false);
                }
            }
        }

        private void HandleMutationOccurred(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent)
        {
            if (mutationEvent.mutation.severity >= config.mutationCelebrationThreshold)
            {
                var discovery = new DiscoveryData
                {
                    discoveryType = DiscoveryType.Mutation,
                    title = $"Rare Mutation Discovered",
                    description = $"A significant {mutationEvent.mutation.mutationType} mutation occurred",
                    timestamp = DateTime.Now,
                    isSignificant = mutationEvent.mutation.severity >= config.significantDiscoveryThreshold
                };

                QueueDiscoveryEvent(discovery, "CurrentPlayer", false);
            }
        }

        private void HandleTraitDiscovered(Laboratory.Subsystems.Genetics.TraitDiscoveryEvent discoveryEvent)
        {
            var discovery = new DiscoveryData
            {
                discoveryType = DiscoveryType.NewTrait,
                title = $"New Trait Discovered: {discoveryEvent.traitName}",
                description = $"Discovered {discoveryEvent.traitName} in generation {discoveryEvent.generation}",
                timestamp = DateTime.Now,
                isSignificant = discoveryEvent.isWorldFirst
            };

            QueueDiscoveryEvent(discovery, "CurrentPlayer", discoveryEvent.isWorldFirst);
        }

        private void HandleDiscoveryTracked(Laboratory.Subsystems.Analytics.DiscoveryAnalyticsEvent analyticsEvent)
        {
            if (analyticsEvent.isWorldFirst)
            {
                var discovery = new DiscoveryData
                {
                    discoveryType = ParseDiscoveryType(analyticsEvent.discoveryType),
                    title = $"World First: {analyticsEvent.discoveredItem}",
                    description = $"First ever discovery of {analyticsEvent.discoveredItem}",
                    timestamp = analyticsEvent.timestamp,
                    isSignificant = true
                };

                QueueDiscoveryEvent(discovery, "CurrentPlayer", true);
            }
        }

        private void HandlePublicationCreated(Laboratory.Subsystems.Research.PublicationEvent publicationEvent)
        {
            // Celebrate significant research publications
            var discovery = new DiscoveryData
            {
                discoveryType = DiscoveryType.Research,
                title = $"Research Publication: {publicationEvent.publication.title}",
                description = "New research publication contributes to community knowledge",
                timestamp = DateTime.Now,
                isSignificant = publicationEvent.publication.coAuthors.Count > 0 // Collaborative research
            };

            QueueDiscoveryEvent(discovery, publicationEvent.publication.authorId, false);
        }

        private void HandleResearchDiscovery(Laboratory.Subsystems.Research.DiscoveryJournalEvent journalEvent)
        {
            if (journalEvent.Discovery.isSignificant)
            {
                var eventDiscovery = ConvertResearchDiscoveryToEventDiscovery(journalEvent.Discovery);
                QueueDiscoveryEvent(eventDiscovery, journalEvent.PlayerId, false);
            }
        }

        /// <summary>
        /// Converts a Research DiscoveryData to Events DiscoveryData
        /// </summary>
        private DiscoveryData ConvertResearchDiscoveryToEventDiscovery(Laboratory.Subsystems.Research.DiscoveryData researchDiscovery)
        {
            return new DiscoveryData
            {
                discoveryType = (DiscoveryType)(int)researchDiscovery.discoveryType,
                title = researchDiscovery.title,
                description = researchDiscovery.description,
                timestamp = researchDiscovery.timestamp,
                isSignificant = researchDiscovery.isSignificant,
                data = new Dictionary<string, object>(researchDiscovery.data)
            };
        }

        private void HandleWeatherChanged(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent)
        {
            // Trigger seasonal celebrations for dramatic weather changes
            if (IsWeatherChangeSignificant(weatherEvent))
            {
                var eventTrigger = new EventTrigger
                {
                    triggerType = EventTriggerType.Seasonal,
                    timestamp = DateTime.Now,
                    data = new Dictionary<string, object>
                    {
                        ["weatherEvent"] = weatherEvent,
                        ["eventType"] = "WeatherCelebration"
                    }
                };

                _eventQueue.Enqueue(eventTrigger);
            }
        }

        private void HandleEnvironmentalEvent(Laboratory.Subsystems.Ecosystem.EnvironmentalEvent environmentalEvent)
        {
            // Trigger community events for major environmental changes
            if (environmentalEvent.severity >= 0.7f)
            {
                var eventTrigger = new EventTrigger
                {
                    triggerType = EventTriggerType.Community,
                    timestamp = DateTime.Now,
                    data = new Dictionary<string, object>
                    {
                        ["eventType"] = CommunityEventType.EnvironmentalChallenge,
                        ["description"] = $"Major environmental event: {environmentalEvent.eventType}",
                        ["severity"] = environmentalEvent.severity
                    }
                };

                _eventQueue.Enqueue(eventTrigger);
            }
        }

        #endregion

        #region Helper Methods

        private void QueueDiscoveryEvent(DiscoveryData discovery, string playerId, bool isWorldFirst)
        {
            var eventTrigger = new EventTrigger
            {
                triggerType = EventTriggerType.Discovery,
                timestamp = DateTime.Now,
                data = new Dictionary<string, object>
                {
                    ["discoveryData"] = discovery,
                    ["playerId"] = playerId,
                    ["isWorldFirst"] = isWorldFirst
                }
            };

            _eventQueue.Enqueue(eventTrigger);
        }

        private float CalculateBreedingCelebrationWorthiness(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            var worthiness = 0f;

            // Base worthiness for successful breeding
            worthiness += 0.3f;

            // Bonus for mutations
            worthiness += result.mutations.Count * 0.2f;

            // Bonus for high trait count
            if (result.offspring?.Genes != null)
            {
                worthiness += result.offspring.Genes.Count * 0.1f;
            }

            // Bonus for generation count (older lineages are more valuable)
            // This would need to be extracted from the genetic profile
            worthiness += 0.05f;

            return Mathf.Clamp01(worthiness);
        }

        private DiscoveryType ParseDiscoveryType(string analyticsType)
        {
            return analyticsType.ToLower() switch
            {
                "trait" => DiscoveryType.NewTrait,
                "mutation" => DiscoveryType.Mutation,
                "species" => DiscoveryType.NewSpecies,
                "breeding" => DiscoveryType.BreedingSuccess,
                _ => DiscoveryType.Other
            };
        }

        private void UpdatePlayerEventHistory(string playerId, CelebrationData celebration)
        {
            if (!_playerEventHistory.TryGetValue(playerId, out var history))
            {
                history = new PlayerEventHistory { playerId = playerId };
                _playerEventHistory[playerId] = history;
            }

            history.totalCelebrations++;
            history.lastCelebration = DateTime.Now;

            if (celebration.isWorldFirst)
            {
                history.worldFirstDiscoveries++;
            }

            history.celebrationHistory.Add(new CelebrationHistoryEntry
            {
                celebrationId = celebration.eventId,
                celebrationType = celebration.celebrationType,
                timestamp = DateTime.Now,
                isWorldFirst = celebration.isWorldFirst
            });
        }

        private string GenerateCommunityEventTitle(CommunityEventType eventType)
        {
            return eventType switch
            {
                CommunityEventType.MassDiscovery => "Community Discovery Wave",
                CommunityEventType.ConservationCrisis => "Conservation Emergency",
                CommunityEventType.EnvironmentalChallenge => "Environmental Challenge",
                CommunityEventType.ResearchBreakthrough => "Research Breakthrough",
                CommunityEventType.SeasonalCelebration => "Seasonal Celebration",
                _ => "Community Event"
            };
        }

        private TimeSpan GetCommunityEventDuration(CommunityEventType eventType)
        {
            return eventType switch
            {
                CommunityEventType.MassDiscovery => TimeSpan.FromHours(2),
                CommunityEventType.ConservationCrisis => TimeSpan.FromDays(1),
                CommunityEventType.EnvironmentalChallenge => TimeSpan.FromHours(6),
                CommunityEventType.ResearchBreakthrough => TimeSpan.FromHours(4),
                CommunityEventType.SeasonalCelebration => TimeSpan.FromDays(7),
                _ => TimeSpan.FromHours(1)
            };
        }

        private float GetEventDuration(object eventData)
        {
            return eventData switch
            {
                CelebrationData celebration => (float)celebration.duration.TotalMinutes,
                SeasonalEventData seasonal => (float)seasonal.duration.TotalMinutes,
                Tournament tournament => (float)(tournament.endTime - tournament.startTime).TotalMinutes,
                _ => 60f // Default 1 hour
            };
        }

        private bool IsWeatherChangeSignificant(Laboratory.Subsystems.Ecosystem.WeatherEvent weatherEvent)
        {
            // Consider weather changes significant if they're dramatic
            return weatherEvent.newWeatherData.intensity >= 0.8f ||
                   Math.Abs(weatherEvent.newWeatherData.intensity - weatherEvent.previousWeatherData.intensity) >= 0.5f;
        }

        #endregion

        #region Background Processing

        private async Task StartEventProcessingLoop()
        {
            while (IsInitialized)
            {
                try
                {
                    await Task.Delay(config.backgroundProcessingIntervalMs);

                    // Update event metrics
                    UpdateEventMetrics();

                    // Process seasonal events
                    if (enableSeasonalEvents)
                    {
                        await SeasonalEventService.UpdateSeasonalEvents();
                    }

                    // Process tournaments
                    if (enableTournaments)
                    {
                        await TournamentService.UpdateTournaments();
                    }

                    // Clean up expired events
                    CleanupExpiredEvents();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{SubsystemName}] Background processing error: {ex.Message}");
                }
            }
        }

        private void UpdateEventMetrics()
        {
            _eventMetrics.totalCelebrations = _playerEventHistory.Values.Sum(h => h.totalCelebrations);
            _eventMetrics.totalWorldFirsts = _playerEventHistory.Values.Sum(h => h.worldFirstDiscoveries);
            _eventMetrics.activeEvents = _activeEvents.Count;
            _eventMetrics.activePlayers = _playerEventHistory.Count;
        }

        private void CleanupExpiredEvents()
        {
            var cutoffTime = DateTime.Now.AddDays(-config.eventHistoryRetentionDays);

            foreach (var history in _playerEventHistory.Values)
            {
                history.celebrationHistory.RemoveAll(c => c.timestamp < cutoffTime);
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            try
            {
                // Unsubscribe from events
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete -= HandleBreedingComplete;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred -= HandleMutationOccurred;
                Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered -= HandleTraitDiscovered;

                Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnDiscoveryTracked -= HandleDiscoveryTracked;

                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnPublicationCreated -= HandlePublicationCreated;
                Laboratory.Subsystems.Research.ResearchSubsystemManager.OnDiscoveryLogged -= HandleResearchDiscovery;

                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnWeatherChanged -= HandleWeatherChanged;
                Laboratory.Subsystems.Ecosystem.EcosystemSubsystemManager.OnEnvironmentalEvent -= HandleEnvironmentalEvent;

                Debug.Log($"[{SubsystemName}] Event unsubscription complete");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{SubsystemName}] Some event unsubscriptions failed: {ex.Message}");
            }

            // Clear collections
            _activeEvents.Clear();
            _eventQueue.Clear();
            _playerEventHistory.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Discovery Celebration")]
        private void TestDiscoveryCelebration()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Events subsystem not initialized");
                return;
            }

            var testDiscovery = new DiscoveryData
            {
                discoveryType = DiscoveryType.NewTrait,
                title = "Test Discovery",
                description = "A test discovery for celebration testing",
                timestamp = DateTime.Now,
                isSignificant = true
            };

            _ = TriggerDiscoveryCelebrationAsync(testDiscovery, "TestPlayer", false);
        }

        [ContextMenu("Test World First Celebration")]
        private void TestWorldFirstCelebration()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Events subsystem not initialized");
                return;
            }

            var testDiscovery = new DiscoveryData
            {
                discoveryType = DiscoveryType.NewSpecies,
                title = "World First Species",
                description = "First ever discovery of this species",
                timestamp = DateTime.Now,
                isSignificant = true
            };

            _ = TriggerDiscoveryCelebrationAsync(testDiscovery, "TestPlayer", true);
        }

        [ContextMenu("Test Community Event")]
        private void TestCommunityEvent()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Events subsystem not initialized");
                return;
            }

            _ = TriggerCommunityEventAsync(CommunityEventType.ResearchBreakthrough, "Test community breakthrough event");
        }

        [ContextMenu("Print Event Metrics")]
        private void PrintEventMetrics()
        {
            Debug.Log($"Event Metrics:\n" +
                     $"Total Celebrations: {_eventMetrics.totalCelebrations}\n" +
                     $"World Firsts: {_eventMetrics.totalWorldFirsts}\n" +
                     $"Active Events: {_eventMetrics.activeEvents}\n" +
                     $"Active Players: {_eventMetrics.activePlayers}");
        }

        #endregion
    }
}