using System;
using System.Threading.Tasks;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Laboratory.Core.GameModes;
using Laboratory.Subsystems.Team.Core;
using Laboratory.Subsystems.Team.Systems;
using Laboratory.Subsystems.Team.GenreImplementations;

namespace Laboratory.Subsystems.Team
{
    /// <summary>
    /// Team Subsystem Manager - Central Coordinator for Team Systems
    /// PURPOSE: Initialize, configure, and manage all team-related systems
    /// FEATURES: Scene bootstrap, configuration management, genre integration
    /// ARCHITECTURE: Integrates with ChimeraGameConfig and GenreGameModeManager
    /// PLAYER-FRIENDLY: Drop-and-play scene setup, automatic configuration
    /// </summary>
    public class TeamSubsystemManager : MonoBehaviour, IGenreSubsystemManager
    {
        [Header("Configuration")]
        [SerializeField] private TeamSubsystemConfig teamConfig;
        [SerializeField] private bool autoInitialize = true;
        [SerializeField] private bool debugLogging = true;

        [Header("System Activation")]
        [SerializeField] private bool enableMatchmaking = true;
        [SerializeField] private bool enableTutorials = true;
        [SerializeField] private bool enableCommunication = true;
        [SerializeField] private bool enableGenreSpecificSystems = true;

        // ISubsystemManager implementation
        public string SubsystemName => "Team Subsystem";
        public bool IsInitialized { get; private set; }
        public float InitializationProgress { get; private set; }

        // IGenreSubsystemManager implementation
        public GameGenre SupportedGenre => GameGenre.Exploration; // Supports all genres
        public GameGenre CurrentActiveGenre { get; private set; } = GameGenre.Exploration;
        public event Action<GameGenre, bool> GenreModeChanged;

        private World _ecsWorld;
        private MatchmakingSystem _matchmakingSystem;
        private TutorialOnboardingSystem _tutorialSystem;
        private TeamCommunicationSystem _communicationSystem;
        private GenreTeamActivationSystem _genreActivationSystem;

        #region Unity Lifecycle

        private void Awake()
        {
            if (autoInitialize)
            {
                _ = InitializeAsync();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        public async Task InitializeAsync()
        {
            try
            {
                InitializationProgress = 0f;

                // Validate configuration
                if (teamConfig == null)
                {
                    Debug.LogError("Team configuration is null! Please assign a TeamSubsystemConfig asset.");
                    return;
                }

                if (!teamConfig.ValidateConfiguration(out string[] errors))
                {
                    Debug.LogError($"Team configuration validation failed:\n{string.Join("\n", errors)}");
                    return;
                }

                InitializationProgress = 0.1f;

                // Get ECS World
                _ecsWorld = World.DefaultGameObjectInjectionWorld;
                if (_ecsWorld == null || !_ecsWorld.IsCreated)
                {
                    Debug.LogError("No ECS World found! Ensure Entities package is properly configured.");
                    return;
                }

                InitializationProgress = 0.2f;

                // Initialize systems
                await InitializeTeamSystems();

                InitializationProgress = 1f;
                IsInitialized = true;

                if (debugLogging)
                {
                    Debug.Log($"✅ {SubsystemName} initialized successfully!");
                    LogSystemStatus();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize Team Subsystem: {ex.Message}\n{ex.StackTrace}");
                InitializationProgress = 0f;
            }
        }

        private async Task InitializeTeamSystems()
        {
            // Initialize matchmaking system
            if (enableMatchmaking && teamConfig.Matchmaking.enableSkillMatching)
            {
                _matchmakingSystem = _ecsWorld.GetOrCreateSystemManaged<MatchmakingSystem>();
                if (debugLogging)
                    Debug.Log("✓ Matchmaking System initialized");
            }

            InitializationProgress = 0.4f;

            // Initialize tutorial system
            if (enableTutorials && teamConfig.Tutorial.enableTutorials)
            {
                _tutorialSystem = _ecsWorld.GetOrCreateSystemManaged<TutorialOnboardingSystem>();
                if (debugLogging)
                    Debug.Log("✓ Tutorial & Onboarding System initialized");
            }

            InitializationProgress = 0.6f;

            // Initialize communication system
            if (enableCommunication && teamConfig.Communication.enablePings)
            {
                _communicationSystem = _ecsWorld.GetOrCreateSystemManaged<TeamCommunicationSystem>();
                _ecsWorld.GetOrCreateSystemManaged<SmartPingAssistSystem>();
                if (debugLogging)
                    Debug.Log("✓ Communication System initialized");
            }

            InitializationProgress = 0.8f;

            // Initialize genre-specific systems
            if (enableGenreSpecificSystems)
            {
                InitializeGenreSystems();
            }

            await Task.CompletedTask;
        }

        private void InitializeGenreSystems()
        {
            _genreActivationSystem = _ecsWorld.GetOrCreateSystemManaged<GenreTeamActivationSystem>();

            // Initialize all genre-specific systems
            _ecsWorld.GetOrCreateSystemManaged<CombatGenreTeamSystem>();
            _ecsWorld.GetOrCreateSystemManaged<RacingGenreTeamSystem>();
            _ecsWorld.GetOrCreateSystemManaged<PuzzleGenreTeamSystem>();
            _ecsWorld.GetOrCreateSystemManaged<StrategyGenreTeamSystem>();
            _ecsWorld.GetOrCreateSystemManaged<ExplorationGenreTeamSystem>();
            _ecsWorld.GetOrCreateSystemManaged<EconomicsGenreTeamSystem>();

            if (debugLogging)
                Debug.Log("✓ Genre-Specific Team Systems initialized (all 47 genres supported)");
        }

        #endregion

        #region Genre Mode Management

        public bool CanActivateForGenre(GameGenre genre)
        {
            // Team system supports all genres
            return true;
        }

        public async Task ActivateGenreMode(GameGenre genre, GenreConfig config)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning("Team Subsystem not initialized, cannot activate genre mode");
                return;
            }

            if (debugLogging)
                Debug.Log($"🎮 Activating team systems for genre: {genre}");

            // Get genre-specific settings
            var genreSettings = teamConfig.GetGenreSettings(genre);

            if (!genreSettings.enableTeams)
            {
                if (debugLogging)
                    Debug.Log($"⏸️ Teams disabled for genre: {genre}");
                return;
            }

            // Activate appropriate genre systems
            // (Systems will automatically process only entities with matching components)

            CurrentActiveGenre = genre;
            GenreModeChanged?.Invoke(genre, true);

            await Task.CompletedTask;
        }

        public async Task DeactivateGenreMode()
        {
            if (debugLogging)
                Debug.Log($"Deactivating team systems for genre: {CurrentActiveGenre}");

            CurrentActiveGenre = GameGenre.Exploration;
            GenreModeChanged?.Invoke(CurrentActiveGenre, false);

            await Task.CompletedTask;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Create a new team entity
        /// </summary>
        public Entity CreateTeam(
            string teamName,
            TeamType teamType,
            int maxMembers,
            GameGenre genre = GameGenre.Exploration)
        {
            if (!IsInitialized || _ecsWorld == null)
            {
                Debug.LogError("Team Subsystem not initialized");
                return Entity.Null;
            }

            var em = _ecsWorld.EntityManager;
            var teamEntity = em.CreateEntity();

            // Add core team component
            em.AddComponentData(teamEntity, new TeamComponent
            {
                TeamName = new FixedString64Bytes(teamName),
                TeamLeader = Entity.Null,
                Type = teamType,
                Status = TeamStatus.Forming,
                MaxMembers = maxMembers,
                CurrentMembers = 0,
                TeamCohesion = 0.5f,
                TeamMorale = 1f,
                TeamLevel = 1,
                TeamSkillRating = 1500f,
                TeamColorHash = (uint)UnityEngine.Random.Range(0, 16777216),
                IsPublic = teamType != TeamType.Guild,
                AllowAutoFill = teamType == TeamType.Casual || teamType == TeamType.Training,
                FormationTimestamp = (float)Time.timeAsDouble
            });

            // Add composition tracking
            em.AddComponentData(teamEntity, new TeamCompositionComponent
            {
                CompositionBalance = 0.5f,
                MeetsMinimumRequirements = false
            });

            // Add communication buffer
            em.AddBuffer<TeamCommunicationComponent>(teamEntity);

            // Add performance tracking
            em.AddComponentData(teamEntity, new TeamPerformanceComponent());

            // Add resource pool
            em.AddComponentData(teamEntity, new TeamResourcePoolComponent
            {
                AllowResourceSharing = true,
                SharingPolicy = ResourceSharingPolicy.Need_Based
            });

            // Add genre-specific components
            if (_genreActivationSystem != null && genre != GameGenre.Exploration)
            {
                _genreActivationSystem.ActivateGenreTeamComponents(teamEntity, genre);
            }

            if (debugLogging)
                Debug.Log($"✅ Created team '{teamName}' (Type: {teamType}, Genre: {genre})");

            return teamEntity;
        }

        /// <summary>
        /// Add a player to the matchmaking queue
        /// </summary>
        public void QueuePlayerForMatchmaking(
            Entity playerEntity,
            TeamRole desiredRole,
            TeamType desiredTeamType,
            float skillRating,
            PlayerSkillLevel skillLevel)
        {
            if (!IsInitialized || _ecsWorld == null)
            {
                Debug.LogError("Team Subsystem not initialized");
                return;
            }

            var em = _ecsWorld.EntityManager;

            em.AddComponentData(playerEntity, new MatchmakingQueueComponent
            {
                PlayerEntity = playerEntity,
                DesiredRole = desiredRole,
                DesiredTeamType = desiredTeamType,
                SkillRating = skillRating,
                SkillLevel = skillLevel,
                QueueStartTime = (float)Time.timeAsDouble,
                MaxWaitTime = teamConfig.Matchmaking.maxQueueTime,
                Preferences = MatchmakingPreferences.None,
                PreferredTeamSize = teamConfig.defaultTeamSize,
                AcceptBackfill = teamConfig.Matchmaking.allowBackfill
            });

            // Add skill rating component if not exists
            if (!em.HasComponent<PlayerSkillRatingComponent>(playerEntity))
            {
                em.AddComponentData(playerEntity, new PlayerSkillRatingComponent
                {
                    OverallRating = skillRating,
                    CombatRating = skillRating,
                    RacingRating = skillRating,
                    PuzzleRating = skillRating,
                    StrategyRating = skillRating,
                    CooperationRating = skillRating,
                    TotalMatches = 0,
                    Wins = 0,
                    Losses = 0,
                    WinRate = 0f,
                    IsCalibrating = true,
                    CalibrationMatchesRemaining = 10
                });
            }

            if (debugLogging)
                Debug.Log($"📋 Player queued for matchmaking (Role: {desiredRole}, Skill: {skillRating:F0})");
        }

        /// <summary>
        /// Send a ping from a player
        /// </summary>
        public bool SendPing(
            Entity playerEntity,
            CommunicationType pingType,
            Vector3 worldPosition,
            Entity targetEntity = default)
        {
            if (!IsInitialized || _ecsWorld == null)
                return false;

            return TeamCommunicationSystem.SendPing(
                _ecsWorld.EntityManager,
                playerEntity,
                pingType,
                worldPosition,
                targetEntity,
                0.8f);
        }

        /// <summary>
        /// Send quick chat message
        /// </summary>
        public bool SendQuickChat(Entity playerEntity, CommunicationType chatType)
        {
            if (!IsInitialized || _ecsWorld == null)
                return false;

            return TeamCommunicationSystem.SendQuickChat(
                _ecsWorld.EntityManager,
                playerEntity,
                chatType);
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            if (debugLogging)
                Debug.Log($"Cleaning up {SubsystemName}");

            IsInitialized = false;
            InitializationProgress = 0f;
        }

        #endregion

        #region Debug & Diagnostics

        private void LogSystemStatus()
        {
            Debug.Log($"=== Team Subsystem Status ===");
            Debug.Log($"Configuration: {teamConfig.name}");
            Debug.Log($"Matchmaking: {(enableMatchmaking ? "Enabled" : "Disabled")}");
            Debug.Log($"Tutorials: {(enableTutorials ? "Enabled" : "Disabled")}");
            Debug.Log($"Communication: {(enableCommunication ? "Enabled" : "Disabled")}");
            Debug.Log($"Genre Systems: {(enableGenreSpecificSystems ? "Enabled" : "Disabled")}");
            Debug.Log($"Default Team Size: {teamConfig.defaultTeamSize}");
            Debug.Log($"Max Concurrent Teams: {teamConfig.Performance.maxConcurrentTeams}");
            Debug.Log($"============================");
        }

        [ContextMenu("Print System Status")]
        public void PrintSystemStatus()
        {
            if (!IsInitialized)
            {
                Debug.Log("Team Subsystem not initialized");
                return;
            }

            LogSystemStatus();

            if (_ecsWorld != null && _ecsWorld.IsCreated)
            {
                var em = _ecsWorld.EntityManager;

                // Count active teams
                var teamQuery = em.CreateEntityQuery(typeof(TeamComponent));
                int teamCount = teamQuery.CalculateEntityCount();
                teamQuery.Dispose();

                // Count players in queue
                var queueQuery = em.CreateEntityQuery(typeof(MatchmakingQueueComponent));
                int queueCount = queueQuery.CalculateEntityCount();
                queueQuery.Dispose();

                Debug.Log($"Active Teams: {teamCount}");
                Debug.Log($"Players in Queue: {queueCount}");
            }
        }

        [ContextMenu("Create Test Team")]
        public void CreateTestTeam()
        {
            var testTeam = CreateTeam(
                "Test Team Alpha",
                TeamType.Cooperative,
                4,
                CurrentActiveGenre);

            Debug.Log($"Created test team: {testTeam}");
        }

        #endregion
    }
}
