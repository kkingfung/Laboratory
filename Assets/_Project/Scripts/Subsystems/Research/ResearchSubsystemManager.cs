using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Events;
using Laboratory.Core.Player;

namespace Laboratory.Subsystems.Research
{
    /// <summary>
    /// Research & Documentation Subsystem Manager for Project Chimera.
    /// Handles player discovery journals, scientific publication generation, peer review mechanisms,
    /// and educational curriculum integration for collaborative research communities.
    /// </summary>
    public class ResearchSubsystemManager : MonoBehaviour, ISubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private ResearchSubsystemConfig config;

        [Header("Services")]
        [SerializeField] private bool enableDiscoveryJournals = true;
        [SerializeField] private bool enablePublications = true;
        [SerializeField] private bool enablePeerReview = true;
        [SerializeField] private bool enableCurriculumIntegration = true;

        // Public Properties
        public bool IsInitialized { get; private set; }
        public string SubsystemName => "Research";
        public float InitializationProgress { get; private set; }

        // Services
        public IDiscoveryJournalService DiscoveryJournalService { get; private set; }
        public IPublicationService PublicationService { get; private set; }
        public IPeerReviewService PeerReviewService { get; private set; }
        public ICurriculumIntegrationService CurriculumIntegrationService { get; private set; }
        private IPlayerSessionManager _playerSessionManager;

        // Events
        public static event Action<DiscoveryJournalEvent> OnDiscoveryLogged;
        public static event Action<PublicationEvent> OnPublicationCreated;
        public static event Action<PeerReviewEvent> OnReviewSubmitted;
        public static event Action<CurriculumProgressEvent> OnCurriculumProgress;

        private readonly Dictionary<string, PlayerResearchProfile> _playerProfiles = new();
        private readonly Dictionary<string, ResearchPublication> _publications = new();
        private readonly Dictionary<string, DiscoveryJournal> _journals = new();
        private readonly Queue<ResearchEvent> _researchEventQueue = new();
        private readonly ResearchMetrics _researchMetrics = new();

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
            ProcessResearchEventQueue();
            UpdateResearchMetrics();
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
                Debug.LogError($"[{SubsystemName}] Configuration is missing! Please assign a ResearchSubsystemConfig.");
                return;
            }

            if (config.publicationTemplates == null || config.publicationTemplates.Count == 0)
            {
                Debug.LogWarning($"[{SubsystemName}] No publication templates configured. Publication generation will be limited.");
            }

            if (enableCurriculumIntegration && config.curriculumMappings == null)
            {
                Debug.LogWarning($"[{SubsystemName}] Curriculum integration enabled but no mappings configured.");
            }
        }

        private void InitializeComponents()
        {
            InitializationProgress = 0.2f;

            // Initialize research services with callbacks to manager data
            DiscoveryJournalService = new DiscoveryJournalService(config, GetPlayerJournal, ProcessDiscoveryJournalEvent);
            PublicationService = new PublicationService(config, () => _publications, ProcessPublicationEvent);
            PeerReviewService = new PeerReviewService(config, () => _publications, ProcessPeerReviewEvent);
            CurriculumIntegrationService = new CurriculumIntegrationService(config, ProcessCurriculumProgressEvent);

            InitializationProgress = 0.4f;
        }

        private async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0.5f;

                // Initialize discovery journals
                if (enableDiscoveryJournals)
                {
                    await DiscoveryJournalService.InitializeAsync();
                }
                InitializationProgress = 0.6f;

                // Initialize publications
                if (enablePublications)
                {
                    await PublicationService.InitializeAsync();
                }
                InitializationProgress = 0.7f;

                // Initialize peer review
                if (enablePeerReview)
                {
                    await PeerReviewService.InitializeAsync();
                }
                InitializationProgress = 0.8f;

                // Initialize curriculum integration
                if (enableCurriculumIntegration)
                {
                    await CurriculumIntegrationService.InitializeAsync();
                }
                InitializationProgress = 0.9f;

                // Subscribe to game events
                SubscribeToGameEvents();

                // Register services
                RegisterServices();

                // Start background research processing
                _ = StartResearchProcessingLoop();

                // Load existing research data
                await LoadExistingResearchData();

                IsInitialized = true;
                InitializationProgress = 1.0f;

                Debug.Log($"[{SubsystemName}] Initialization complete. " +
                         $"Discovery Journals: {enableDiscoveryJournals}, " +
                         $"Publications: {enablePublications}, " +
                         $"Peer Review: {enablePeerReview}, " +
                         $"Curriculum Integration: {enableCurriculumIntegration}");

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
            // Subscribe to genetics events for automatic research documentation
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete += HandleBreedingComplete;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred += HandleMutationOccurred;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered += HandleTraitDiscovered;

            // Subscribe to analytics events for research pattern analysis
            Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnDiscoveryTracked += HandleDiscoveryTracked;
        }

        private void RegisterServices()
        {
            if (ServiceContainer.Instance != null)
            {
                ServiceContainer.Instance.RegisterService<IDiscoveryJournalService>(DiscoveryJournalService);
                ServiceContainer.Instance.RegisterService<IPublicationService>(PublicationService);
                ServiceContainer.Instance.RegisterService<IPeerReviewService>(PeerReviewService);
                ServiceContainer.Instance.RegisterService<ICurriculumIntegrationService>(CurriculumIntegrationService);
                ServiceContainer.Instance.RegisterService<ResearchSubsystemManager>(this);

                // Resolve player session manager
                _playerSessionManager = ServiceContainer.Instance.ResolveService<IPlayerSessionManager>();
            }
        }

        private async Task LoadExistingResearchData()
        {
            // In a real implementation, this would load from persistent storage
            await Task.CompletedTask;
        }

        #endregion

        #region Core Research Operations

        /// <summary>
        /// Logs a discovery to the player's research journal
        /// </summary>
        public async Task<bool> LogDiscoveryAsync(string playerId, DiscoveryData discovery)
        {
            if (!IsInitialized || !enableDiscoveryJournals)
                return false;

            return await DiscoveryJournalService.LogDiscoveryAsync(playerId, discovery);
        }

        /// <summary>
        /// Creates a research publication from discoveries
        /// </summary>
        public async Task<ResearchPublication> CreatePublicationAsync(string authorId, PublicationRequest request)
        {
            if (!IsInitialized || !enablePublications)
                return null;

            var publicationId = await PublicationService.CreatePublicationAsync(authorId, request.Title, request.Content);

            // Create and return the ResearchPublication object
            var publication = new ResearchPublication
            {
                id = publicationId,
                publicationId = publicationId,
                title = request.Title,
                content = request.Content,
                authorId = authorId,
                publicationType = request.Type,
                publicationDate = DateTime.Now,
                keywords = request.Keywords
            };

            return publication;
        }

        /// <summary>
        /// Submits a peer review for a publication
        /// </summary>
        public async Task<bool> SubmitPeerReviewAsync(string reviewerId, string publicationId, PeerReview review)
        {
            if (!IsInitialized || !enablePeerReview)
                return false;

            var reviewId = await PeerReviewService.SubmitReviewAsync(publicationId, reviewerId, review.Rating, review.Comments);
            return !string.IsNullOrEmpty(reviewId);
        }

        /// <summary>
        /// Gets the research profile for a player
        /// </summary>
        public PlayerResearchProfile GetPlayerResearchProfile(string playerId)
        {
            if (!_playerProfiles.TryGetValue(playerId, out var profile))
            {
                profile = new PlayerResearchProfile
                {
                    playerId = playerId,
                    totalDiscoveries = 0,
                    publications = new List<ResearchPublication>(),
                    reviewsSubmitted = 0,
                    researchLevel = ResearchLevel.Novice,
                    researchPoints = 0,
                    creationDate = DateTime.Now,
                    specializations = new List<ResearchSpecialization>()
                };
                _playerProfiles[playerId] = profile;
            }
            return profile;
        }

        /// <summary>
        /// Gets all publications by a player
        /// </summary>
        public List<ResearchPublication> GetPlayerPublications(string playerId)
        {
            var publications = new List<ResearchPublication>();
            foreach (var publication in _publications.Values)
            {
                if (publication.authorId == playerId)
                    publications.Add(publication);
            }
            return publications;
        }

        /// <summary>
        /// Gets the discovery journal for a player
        /// </summary>
        public DiscoveryJournal GetPlayerJournal(string playerId)
        {
            if (!_journals.TryGetValue(playerId, out var journal))
            {
                journal = new DiscoveryJournal
                {
                    ownerId = playerId,
                    creationDate = DateTime.Now,
                    entries = new List<DiscoveryEntry>()
                };
                _journals[playerId] = journal;
            }
            return journal;
        }

        /// <summary>
        /// Searches publications by criteria
        /// </summary>
        public List<ResearchPublication> SearchPublications(PublicationSearchCriteria criteria)
        {
            var results = new List<ResearchPublication>();

            foreach (var publication in _publications.Values)
            {
                if (MatchesSearchCriteria(publication, criteria))
                {
                    results.Add(publication);
                }
            }

            // Sort by relevance or date
            results.Sort((a, b) => b.publicationDate.CompareTo(a.publicationDate));

            return results;
        }

        /// <summary>
        /// Gets curriculum progress for educational environments
        /// </summary>
        public CurriculumProgress GetCurriculumProgress(string studentId, string curriculumId)
        {
            if (!enableCurriculumIntegration)
                return null;

            return CurriculumIntegrationService.GetStudentProgress(studentId, curriculumId);
        }

        /// <summary>
        /// Generates an automated research summary from discoveries
        /// </summary>
        public async Task<ResearchSummary> GenerateResearchSummaryAsync(string playerId, TimeSpan timeWindow)
        {
            await Task.CompletedTask;
            var profile = GetPlayerResearchProfile(playerId);
            var journal = GetPlayerJournal(playerId);

            var cutoffDate = DateTime.Now - timeWindow;
            var recentEntries = journal.entries.FindAll(e => e.CreatedDate >= cutoffDate);

            var summary = new ResearchSummary
            {
                playerId = playerId,
                timeWindow = timeWindow,
                totalDiscoveries = recentEntries.Count,
                uniqueSpecies = CountUniqueSpecies(recentEntries),
                newTraits = CountNewTraits(recentEntries),
                mutations = CountMutations(recentEntries),
                breedingExperiments = CountBreedingExperiments(recentEntries),
                researchScore = CalculateResearchScore(recentEntries),
                recommendations = GenerateResearchRecommendations(profile, recentEntries)
            };

            return summary;
        }

        #endregion

        #region Event Processing

        private void ProcessResearchEventQueue()
        {
            const int maxEventsPerFrame = 5;
            int processedCount = 0;

            while (_researchEventQueue.Count > 0 && processedCount < maxEventsPerFrame)
            {
                var researchEvent = _researchEventQueue.Dequeue();
                ProcessResearchEvent(researchEvent);
                processedCount++;
            }
        }

        private void ProcessResearchEvent(ResearchEvent researchEvent)
        {
            switch (researchEvent.EventType)
            {
                case "DiscoveryJournal":
                    // Parse data and create appropriate event - simplified for now
                    Debug.Log($"Processing DiscoveryJournal event: {researchEvent.Data}");
                    break;

                case "Publication":
                    Debug.Log($"Processing Publication event: {researchEvent.Data}");
                    break;

                case "PeerReview":
                    Debug.Log($"Processing PeerReview event: {researchEvent.Data}");
                    break;

                case "CurriculumProgress":
                    Debug.Log($"Processing CurriculumProgress event: {researchEvent.Data}");
                    break;
            }
        }

        private void ProcessDiscoveryJournalEvent(DiscoveryJournalEvent journalEvent)
        {
            OnDiscoveryLogged?.Invoke(journalEvent);

            // Update player research profile
            var profile = GetPlayerResearchProfile(journalEvent.PlayerId);
            profile.totalDiscoveries++;
            profile.researchPoints += CalculateDiscoveryPoints(journalEvent.Discovery);
            UpdateResearchLevel(profile);

            // Check for research achievements
            CheckResearchAchievements(profile, journalEvent.Discovery);
        }

        private void ProcessPublicationEvent(PublicationEvent publicationEvent)
        {
            OnPublicationCreated?.Invoke(publicationEvent);

            // Add to publications collection
            _publications[publicationEvent.publication.publicationId] = publicationEvent.publication;

            // Update author's research profile
            var profile = GetPlayerResearchProfile(publicationEvent.publication.authorId);
            profile.publications.Add(publicationEvent.publication);
            profile.researchPoints += config.publicationResearchPoints;
            UpdateResearchLevel(profile);

            // Update research metrics
            _researchMetrics.totalPublications++;
        }

        private void ProcessPeerReviewEvent(PeerReviewEvent reviewEvent)
        {
            OnReviewSubmitted?.Invoke(reviewEvent);

            // Update reviewer's research profile
            var profile = GetPlayerResearchProfile(reviewEvent.review.ReviewerId);
            profile.reviewsSubmitted++;
            profile.researchPoints += config.reviewResearchPoints;

            // Update research metrics
            _researchMetrics.totalReviews++;
        }

        private void ProcessCurriculumProgressEvent(CurriculumProgressEvent curriculumEvent)
        {
            OnCurriculumProgress?.Invoke(curriculumEvent);

            // Update curriculum integration metrics
            if (enableCurriculumIntegration)
            {
                CurriculumIntegrationService.UpdateStudentProgress(curriculumEvent);
            }
        }

        #endregion

        #region Research Analytics

        private void UpdateResearchMetrics()
        {
            // Update every 10 seconds
            if (Time.unscaledTime % 10f < Time.unscaledDeltaTime)
            {
                _researchMetrics.activeResearchers = _playerProfiles.Count;
                _researchMetrics.averageResearchLevel = CalculateAverageResearchLevel();
                _researchMetrics.discoveryRate = CalculateDiscoveryRate();
                _researchMetrics.collaborationIndex = CalculateCollaborationIndex();
            }
        }

        private float CalculateAverageResearchLevel()
        {
            if (_playerProfiles.Count == 0)
                return 0f;

            var totalLevel = 0f;
            foreach (var profile in _playerProfiles.Values)
            {
                totalLevel += (int)profile.researchLevel;
            }

            return totalLevel / _playerProfiles.Count;
        }

        private float CalculateDiscoveryRate()
        {
            var recentDiscoveries = 0;
            var cutoffTime = DateTime.Now.AddHours(-1);

            foreach (var journal in _journals.Values)
            {
                recentDiscoveries += journal.entries.Count(e => e.CreatedDate >= cutoffTime);
            }

            return recentDiscoveries;
        }

        private float CalculateCollaborationIndex()
        {
            // Calculate based on co-authored publications and peer reviews
            var collaborativePublications = _publications.Values.Count(p => p.coAuthors.Count > 0);
            var totalPublications = _publications.Count;

            if (totalPublications == 0)
                return 0f;

            return (float)collaborativePublications / totalPublications;
        }

        #endregion

        #region Event Handlers

        private void HandleBreedingComplete(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            if (result != null && result.isSuccessful)
            {
                // Automatically log breeding discoveries
                var discovery = new DiscoveryData
                {
                    discoveryType = DiscoveryType.BreedingSuccess,
                    title = $"Successful breeding: {result.parent1Id} × {result.parent2Id}",
                    description = GenerateBreedingDescription(result),
                    data = new Dictionary<string, object>
                    {
                        ["parent1Id"] = result.parent1Id,
                        ["parent2Id"] = result.parent2Id,
                        ["offspringId"] = result.offspringId,
                        ["traits"] = result.offspring?.Genes?.Count ?? 0,
                        ["mutations"] = result.mutations?.Count ?? 0
                    },
                    timestamp = DateTime.Now
                };

                // Log to current player's journal
                _ = LogDiscoveryAsync(GetCurrentPlayerId(), discovery);
            }
        }

        private void HandleMutationOccurred(Laboratory.Subsystems.Genetics.MutationEvent mutationEvent)
        {
            var discovery = new DiscoveryData
            {
                discoveryType = DiscoveryType.Mutation,
                title = $"Mutation discovered: {mutationEvent.mutation.affectedTrait}",
                description = $"A {mutationEvent.mutation.mutationType} mutation occurred affecting {mutationEvent.mutation.affectedTrait} with severity {mutationEvent.mutation.severity:F2}",
                data = new Dictionary<string, object>
                {
                    ["creatureId"] = mutationEvent.creatureId,
                    ["mutationType"] = mutationEvent.mutation.mutationType.ToString(),
                    ["affectedTrait"] = mutationEvent.mutation.affectedTrait,
                    ["severity"] = mutationEvent.mutation.severity
                },
                timestamp = DateTime.Now
            };

            _ = LogDiscoveryAsync(GetCurrentPlayerId(), discovery);
        }

        private void HandleTraitDiscovered(Laboratory.Subsystems.Genetics.TraitDiscoveryEvent discoveryEvent)
        {
            var discovery = new DiscoveryData
            {
                discoveryType = DiscoveryType.NewTrait,
                title = $"New trait discovered: {discoveryEvent.traitName}",
                description = $"Discovered {discoveryEvent.traitName} in generation {discoveryEvent.generation}",
                data = new Dictionary<string, object>
                {
                    ["traitName"] = discoveryEvent.traitName,
                    ["generation"] = discoveryEvent.generation,
                    ["isWorldFirst"] = discoveryEvent.isWorldFirst,
                    ["creatureId"] = discoveryEvent.creatureId
                },
                timestamp = DateTime.Now,
                isSignificant = discoveryEvent.isWorldFirst
            };

            _ = LogDiscoveryAsync(GetCurrentPlayerId(), discovery);
        }

        private void HandleDiscoveryTracked(Laboratory.Subsystems.Analytics.DiscoveryAnalyticsEvent analyticsEvent)
        {
            // Convert analytics discovery to research discovery
            var discovery = new DiscoveryData
            {
                discoveryType = ParseDiscoveryType(analyticsEvent.discoveryType),
                title = $"Discovery: {analyticsEvent.discoveredItem}",
                description = $"Analytics tracked discovery of {analyticsEvent.discoveredItem}",
                data = new Dictionary<string, object>
                {
                    ["discoveredItem"] = analyticsEvent.discoveredItem,
                    ["discoveryType"] = analyticsEvent.discoveryType,
                    ["isWorldFirst"] = analyticsEvent.isWorldFirst
                },
                timestamp = analyticsEvent.timestamp,
                isSignificant = analyticsEvent.isWorldFirst
            };

            _ = LogDiscoveryAsync(GetCurrentPlayerId(), discovery);
        }

        #endregion

        #region Player Context Resolution

        /// <summary>
        /// Gets the current active player ID, with fallback handling
        /// </summary>
        private string GetCurrentPlayerId()
        {
            // Try to get from player session manager first
            if (_playerSessionManager != null)
            {
                var currentPlayerId = _playerSessionManager.GetCurrentPlayerId();
                if (!string.IsNullOrEmpty(currentPlayerId))
                {
                    return currentPlayerId;
                }
            }

            // Fallback to legacy methods
            // Check if there's a player manager component in the scene
            var playerManager = FindFirstObjectByType<Laboratory.Core.Player.PlayerSessionManager>();
            if (playerManager != null)
            {
                return playerManager.GetCurrentPlayerId();
            }

            // Final fallback to default player
            return "DefaultPlayer";
        }

        /// <summary>
        /// Gets all active player IDs for multi-player scenarios
        /// </summary>
        private List<string> GetActivePlayerIds()
        {
            if (_playerSessionManager != null)
            {
                return _playerSessionManager.GetActivePlayerIds();
            }

            // Fallback: return current player as single-item list
            return new List<string> { GetCurrentPlayerId() };
        }

        /// <summary>
        /// Checks if a specific player is currently active
        /// </summary>
        private bool IsPlayerActive(string playerId)
        {
            if (_playerSessionManager != null)
            {
                return _playerSessionManager.IsPlayerActive(playerId);
            }

            // Fallback: assume player is active if it's the current player
            return playerId == GetCurrentPlayerId();
        }

        #endregion

        #region Helper Methods

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

        private string GenerateBreedingDescription(Laboratory.Subsystems.Genetics.GeneticBreedingResult result)
        {
            var description = $"Successfully bred {result.parent1Id} with {result.parent2Id}, resulting in offspring {result.offspringId}.";

            if (result.mutations.Count > 0)
            {
                description += $" {result.mutations.Count} mutation(s) occurred during breeding.";
            }

            if (result.offspring?.Genes != null && result.offspring.Genes.Count > 0)
            {
                description += $" Offspring has {result.offspring.Genes.Count} genetic traits.";
            }

            return description;
        }

        private int CalculateDiscoveryPoints(DiscoveryData discovery)
        {
            var basePoints = config.baseDiscoveryPoints;

            // Bonus for significant discoveries
            if (discovery.isSignificant)
                basePoints *= 2;

            // Bonus by discovery type
            basePoints += discovery.discoveryType switch
            {
                DiscoveryType.NewSpecies => 50,
                DiscoveryType.NewTrait => 25,
                DiscoveryType.Mutation => 15,
                DiscoveryType.BreedingSuccess => 10,
                _ => 5
            };

            return basePoints;
        }

        private void UpdateResearchLevel(PlayerResearchProfile profile)
        {
            var newLevel = profile.researchPoints switch
            {
                < 100 => ResearchLevel.Novice,
                < 500 => ResearchLevel.Intermediate,
                < 1500 => ResearchLevel.Advanced,
                < 3000 => ResearchLevel.Expert,
                _ => ResearchLevel.Master
            };

            if (newLevel != profile.researchLevel)
            {
                profile.researchLevel = newLevel;
                Debug.Log($"[{SubsystemName}] Player {profile.playerId} advanced to research level: {newLevel}");
            }
        }

        private void CheckResearchAchievements(PlayerResearchProfile profile, DiscoveryData discovery)
        {
            // Check for various research achievements
            if (discovery.isSignificant && discovery.discoveryType == DiscoveryType.NewSpecies)
            {
                // Award "Species Pioneer" achievement
                Debug.Log($"[{SubsystemName}] Player {profile.playerId} earned Species Pioneer achievement!");
            }

            if (profile.totalDiscoveries >= 100)
            {
                // Award "Prolific Researcher" achievement
                Debug.Log($"[{SubsystemName}] Player {profile.playerId} earned Prolific Researcher achievement!");
            }
        }

        private bool MatchesSearchCriteria(ResearchPublication publication, PublicationSearchCriteria criteria)
        {
            if (criteria.Keywords?.Count > 0)
            {
                var keyword = criteria.Keywords.FirstOrDefault()?.ToLower();
                if (!publication.title.ToLower().Contains(keyword) &&
                    !publication.abstractText.ToLower().Contains(keyword))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(criteria.AuthorId) && publication.authorId != criteria.AuthorId)
                return false;

            if (criteria.Type != PublicationType.Any && publication.publicationType != criteria.Type)
                return false;

            if (criteria.StartDate.HasValue && publication.publicationDate < criteria.StartDate.Value)
                return false;

            if (criteria.EndDate.HasValue && publication.publicationDate > criteria.EndDate.Value)
                return false;

            return true;
        }

        private int CountUniqueSpecies(List<DiscoveryEntry> entries)
        {
            var species = new HashSet<string>();
            foreach (var entry in entries)
            {
                if (entry.Data.discoveryType == DiscoveryType.NewSpecies)
                    species.Add(entry.Data.title);
            }
            return species.Count;
        }

        private int CountNewTraits(List<DiscoveryEntry> entries)
        {
            return entries.Count(e => e.Data.discoveryType == DiscoveryType.NewTrait);
        }

        private int CountMutations(List<DiscoveryEntry> entries)
        {
            return entries.Count(e => e.Data.discoveryType == DiscoveryType.Mutation);
        }

        private int CountBreedingExperiments(List<DiscoveryEntry> entries)
        {
            return entries.Count(e => e.Data.discoveryType == DiscoveryType.BreedingSuccess);
        }

        private float CalculateResearchScore(List<DiscoveryEntry> entries)
        {
            var score = 0f;
            foreach (var entry in entries)
            {
                score += CalculateDiscoveryPoints(entry.Data);
            }
            return score;
        }

        private List<string> GenerateResearchRecommendations(PlayerResearchProfile profile, List<DiscoveryEntry> recentEntries)
        {
            var recommendations = new List<string>();

            // Analyze recent research patterns and suggest next steps
            var traitDiscoveries = recentEntries.Count(e => e.Data.discoveryType == DiscoveryType.NewTrait);
            var breedingAttempts = recentEntries.Count(e => e.Data.discoveryType == DiscoveryType.BreedingSuccess);

            if (traitDiscoveries > breedingAttempts * 2)
            {
                recommendations.Add("Consider focusing more on breeding experiments to apply your trait discoveries.");
            }

            if (profile.publications.Count == 0 && profile.totalDiscoveries >= 10)
            {
                recommendations.Add("You have enough discoveries to create your first research publication!");
            }

            if (profile.reviewsSubmitted == 0 && profile.researchLevel >= ResearchLevel.Intermediate)
            {
                recommendations.Add("Consider reviewing other researchers' publications to contribute to the community.");
            }

            return recommendations;
        }

        #endregion

        #region Background Processing

        private async Task StartResearchProcessingLoop()
        {
            while (IsInitialized)
            {
                try
                {
                    await Task.Delay(config.backgroundProcessingIntervalMs);

                    // Update research analytics
                    UpdateResearchMetrics();

                    // Process pending publications
                    if (enablePublications)
                    {
                        PublicationService.ProcessPendingPublications();
                    }

                    // Update curriculum progress
                    if (enableCurriculumIntegration)
                    {
                        CurriculumIntegrationService.UpdateAllProgress();
                    }

                    // Cleanup old data
                    CleanupOldResearchData();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[{SubsystemName}] Background processing error: {ex.Message}");
                }
            }
        }

        private void CleanupOldResearchData()
        {
            var cutoffDate = DateTime.Now.AddDays(-config.dataRetentionDays);

            // Clean up old discovery entries if configured
            if (config.cleanupOldDiscoveries)
            {
                foreach (var journal in _journals.Values)
                {
                    journal.entries.RemoveAll(e => e.CreatedDate < cutoffDate);
                }
            }
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            // Unsubscribe from events
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnBreedingComplete -= HandleBreedingComplete;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnMutationOccurred -= HandleMutationOccurred;
            Laboratory.Subsystems.Genetics.GeneticsSubsystemManager.OnTraitDiscovered -= HandleTraitDiscovered;

            Laboratory.Subsystems.Analytics.AnalyticsSubsystemManager.OnDiscoveryTracked -= HandleDiscoveryTracked;

            // Clear collections
            _playerProfiles.Clear();
            _publications.Clear();
            _journals.Clear();
            _researchEventQueue.Clear();

            Debug.Log($"[{SubsystemName}] Cleanup complete");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Generate Test Research Data")]
        private void GenerateTestResearchData()
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Research subsystem not initialized");
                return;
            }

            var testDiscovery = new DiscoveryData
            {
                discoveryType = DiscoveryType.NewTrait,
                title = "Test Discovery",
                description = "A test discovery for debugging purposes",
                timestamp = DateTime.Now,
                isSignificant = true
            };

            _ = LogDiscoveryAsync("TestPlayer", testDiscovery);
        }

        [ContextMenu("Print Research Metrics")]
        private void PrintResearchMetrics()
        {
            Debug.Log($"Research Metrics:\n" +
                     $"Active Researchers: {_researchMetrics.activeResearchers}\n" +
                     $"Total Publications: {_researchMetrics.totalPublications}\n" +
                     $"Total Reviews: {_researchMetrics.totalReviews}\n" +
                     $"Discovery Rate: {_researchMetrics.discoveryRate:F1}/hour\n" +
                     $"Collaboration Index: {_researchMetrics.collaborationIndex:F2}");
        }

        #endregion
    }

    #region Supporting Types and Interfaces

    // Event Types
    public class DiscoveryJournalEvent
    {
        public string PlayerId { get; set; }
        public DiscoveryData Discovery { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class PublicationEvent
    {
        public string PublicationId { get; set; }
        public string Title { get; set; }
        public string AuthorId { get; set; }
        public ResearchPublication publication { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class PeerReviewEvent
    {
        public string ReviewId { get; set; }
        public string PublicationId { get; set; }
        public string ReviewerId { get; set; }
        public int Rating { get; set; }
        public PeerReview review { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class CurriculumProgressEvent
    {
        public string PlayerId { get; set; }
        public string ModuleId { get; set; }
        public float Progress { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    // Service Interfaces
    public interface IDiscoveryJournalService
    {
        Task InitializeAsync();
        Task<bool> LogDiscoveryAsync(string playerId, DiscoveryData discovery);
        Task<List<DiscoveryData>> GetPlayerDiscoveriesAsync(string playerId);
    }

    public interface IPublicationService
    {
        Task InitializeAsync();
        Task<string> CreatePublicationAsync(string authorId, string title, string content);
        Task<List<ResearchPublication>> GetPublicationsAsync();
        void ProcessPendingPublications();
    }

    public interface IPeerReviewService
    {
        Task InitializeAsync();
        Task<string> SubmitReviewAsync(string publicationId, string reviewerId, int rating, string comments);
        Task<List<PeerReview>> GetReviewsForPublicationAsync(string publicationId);
    }

    public interface ICurriculumIntegrationService
    {
        Task InitializeAsync();
        Task<bool> UpdateProgressAsync(string playerId, string moduleId, float progress);
        Task<CurriculumProgress> GetPlayerProgressAsync(string playerId);
        void UpdateAllProgress();
        void UpdateStudentProgress(CurriculumProgressEvent curriculumEvent);
        CurriculumProgress GetStudentProgress(string studentId, string curriculumId);
    }

    // Data Types


    public class DiscoveryJournal
    {
        public string PlayerId { get; set; }
        public string ownerId { get; set; }
        public DateTime creationDate { get; set; }
        public List<DiscoveryEntry> entries { get; set; } = new();
        public List<DiscoveryData> Discoveries { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class ResearchEvent
    {
        public string EventType { get; set; }
        public string Data { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ResearchMetrics
    {
        public int activeResearchers;
        public int totalPublications;
        public int totalReviews;
        public float discoveryRate;
        public float collaborationIndex;
        public float averageResearchLevel;
    }

    public class DiscoveryData
    {
        public DiscoveryType discoveryType;
        public string title;
        public string description;
        public DateTime timestamp;
        public bool isSignificant;
        public Dictionary<string, object> data = new();
    }

    public enum DiscoveryType
    {
        NewTrait,
        NewSpecies,
        NewBehavior,
        Mutation,
        BreedingSuccess,
        EcosystemChange,
        Other
    }

    public class PeerReview
    {
        public string Id { get; set; }
        public string PublicationId { get; set; }
        public string ReviewerId { get; set; }
        public int Rating { get; set; }
        public string Comments { get; set; }
        public DateTime SubmittedDate { get; set; }
    }

    public class CurriculumProgress
    {
        public string PlayerId { get; set; }
        public Dictionary<string, float> ModuleProgress { get; set; } = new();
        public int CompletedModules { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ResearchSubsystemConfig : ScriptableObject
    {
        [Header("Discovery Settings")]
        public bool enableJournaling = true;
        public int maxDiscoveriesPerPlayer = 1000;
        public int baseDiscoveryPoints = 10;
        public bool cleanupOldDiscoveries = true;
        public int dataRetentionDays = 365;

        [Header("Publication Settings")]
        public bool enablePeerReview = true;
        public int minReviewsRequired = 3;
        public int publicationResearchPoints = 50;
        public int reviewResearchPoints = 10;
        public List<PublicationTemplate> publicationTemplates = new();

        [Header("Curriculum Settings")]
        public bool enableProgressTracking = true;
        public float progressUpdateInterval = 1f;
        public List<CurriculumMapping> curriculumMappings = new();

        [Header("Background Processing")]
        public int backgroundProcessingIntervalMs = 5000;
    }

    public class PublicationRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public List<string> DiscoveryIds { get; set; } = new();
        public List<string> Keywords { get; set; } = new();
        public PublicationType Type { get; set; }
    }

    public class PublicationSearchCriteria
    {
        public string AuthorId { get; set; }
        public List<string> Keywords { get; set; } = new();
        public PublicationType? Type { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int MaxResults { get; set; } = 50;
    }

    public class ResearchSummary
    {
        public string playerId { get; set; }
        public TimeSpan timeWindow { get; set; }
        public int totalDiscoveries { get; set; }
        public int uniqueSpecies { get; set; }
        public int newTraits { get; set; }
        public int mutations { get; set; }
        public int breedingExperiments { get; set; }
        public float researchScore { get; set; }
        public List<string> recommendations { get; set; } = new();
        public int TotalDiscoveries { get; set; }
        public int TotalPublications { get; set; }
        public int TotalReviews { get; set; }
        public float AverageRating { get; set; }
        public List<string> TopKeywords { get; set; } = new();
        public DateTime LastActivity { get; set; }
    }

    public class DiscoveryEntry
    {
        public string Id { get; set; }
        public string PlayerId { get; set; }
        public DiscoveryData Data { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsVerified { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public enum PublicationType
    {
        Any,
        Research,
        Review,
        Tutorial,
        Hypothesis,
        Case_Study
    }

    public class PlayerResearchProfile
    {
        public string playerId;
        public int totalDiscoveries;
        public List<ResearchPublication> publications = new();
        public int reviewsSubmitted;
        public ResearchLevel researchLevel;
        public int researchPoints;
        public DateTime creationDate;
        public List<ResearchSpecialization> specializations = new();
    }

    public class ResearchPublication
    {
        public string id;
        public string publicationId;
        public string title;
        public string abstractText;
        public string authorId;
        public PublicationType publicationType;
        public DateTime publicationDate;
        public List<string> keywords = new();
        public List<string> coAuthors = new();
        public string content;
    }

    public enum ResearchLevel
    {
        Novice,
        Intermediate,
        Advanced,
        Expert,
        Master
    }

    public class ResearchSpecialization
    {
        public string name;
        public string description;
        public float proficiencyLevel;
        public List<string> relatedTopics = new();
    }

    public class PublicationTemplate
    {
        public string name;
        public string templateContent;
        public PublicationType type;
        public List<string> requiredSections = new();
    }

    public class CurriculumMapping
    {
        public string moduleId;
        public string moduleName;
        public List<string> requiredDiscoveryTypes = new();
        public float completionThreshold;
    }

    public class SubsystemInitializedEvent
    {
        public string SubsystemName { get; }
        public DateTime Timestamp { get; }

        public SubsystemInitializedEvent(string subsystemName)
        {
            SubsystemName = subsystemName;
            Timestamp = DateTime.Now;
        }
    }

    public static class EventBus
    {
        public static void Publish<T>(T eventData) where T : class
        {
            // Event bus implementation would go here
            Debug.Log($"Published event: {typeof(T).Name}");
        }
    }


    #endregion
}
