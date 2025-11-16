using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Subsystems.Team.Core;
using Laboratory.Core.GameModes;
using Laboratory.Subsystems.Combat.Advanced;

namespace Laboratory.Subsystems.Team.GenreImplementations
{
    /// <summary>
    /// Genre-Specific Team Implementations - Team Mechanics for All 47 Genres
    /// PURPOSE: Adapt universal team framework to genre-specific gameplay
    /// COVERAGE: Combat, Racing, Puzzle, Exploration, Economics, Strategy, and 40+ more
    /// PLAYER-FRIENDLY: Intuitive team mechanics that fit each genre naturally
    /// ARCHITECTURE: Modular systems that activate/deactivate per genre
    /// </summary>

    #region Genre-Specific Components

    /// <summary>
    /// Combat Team Component - Arena/Fighting/FPS/TPS/BeatEmUp/HackAndSlash/SurvivalHorror
    /// </summary>
    public struct CombatTeamComponent : IComponentData
    {
        public FormationType Formation;
        public float TeamDPS;
        public float TeamHealing;
        public float TeamTankiness;
        public int EnemiesDefeated;
        public int TeamRevives;
        public float CombatEfficiency; // 0-1
    }

    /// <summary>
    /// Racing Team Component - Racing/EndlessRunner/VehicleSimulation/FlightSimulator
    /// </summary>
    public struct RacingTeamComponent : IComponentData
    {
        public RacingMode Mode;
        public float TeamBestLap;
        public float CombinedSpeed;
        public int CheckpointsCollected;
        public int BoostsShared;
        public float DraftingBonus; // Slipstream bonus
        public int RelayHandoffs; // For relay races
    }

    public enum RacingMode : byte
    {
        Traditional = 0,   // Fastest individual wins
        Relay = 1,         // Team members take turns
        Cooperative = 2,   // Combined team time
        Pursuit = 3        // Chase/evade team mechanics
    }

    /// <summary>
    /// Puzzle Team Component - Puzzle/Match3/TetrisLike/PhysicsPuzzle/HiddenObject/WordGame
    /// </summary>
    public struct PuzzleTeamComponent : IComponentData
    {
        public PuzzleCooperationMode Mode;
        public int PuzzlesSolved;
        public int HintsGiven;
        public int CollaborativeSolutions;
        public float SolutionSyncScore; // How coordinated team is
        public bool AllowHintSharing;
    }

    public enum PuzzleCooperationMode : byte
    {
        Split_Task = 0,      // Each player solves different parts
        Shared_Progress = 1, // All work on same puzzle
        Complementary = 2,   // Puzzles require different skills
        Sequential = 3       // Unlock next puzzles together
    }

    /// <summary>
    /// Exploration Team Component - Exploration/Metroidvania/WalkingSimulator/PointAndClick
    /// </summary>
    public struct ExplorationTeamComponent : IComponentData
    {
        public float MapCoveragePercent;
        public int DiscoveriesShared;
        public int ResourcesGathered;
        public int SecretsFound;
        public bool AllowMapSharing;
        public float ExplorationRadius; // How far apart team can spread
        public int FastTravelPoints; // Shared fast travel
    }

    /// <summary>
    /// Strategy Team Component - Strategy/RTS/TurnBased/4X/GrandStrategy/AutoBattler/ChessLike
    /// </summary>
    public struct StrategyTeamComponent : IComponentData
    {
        public StrategyTeamMode Mode;
        public int TerritoryControlled;
        public int ResourcePoolSize;
        public int TechnologiesResearched;
        public float StrategicCoordination; // How well strategies align
        public bool SharedEconomy;
        public bool SharedVision; // Fog of war sharing
    }

    public enum StrategyTeamMode : byte
    {
        Allied_Forces = 0,     // Separate armies, common goal
        Unified_Command = 1,   // Single combined force
        Specialized_Roles = 2, // Different strategic focuses
        Diplomatic_Team = 3    // Diplomacy-based cooperation
    }

    /// <summary>
    /// Economics Team Component - Economics/FarmingSimulator/ConstructionSimulator
    /// </summary>
    public struct EconomicsTeamComponent : IComponentData
    {
        public float TeamWealth;
        public int TradesCompleted;
        public int ProductionOutputs;
        public EconomicsMode Mode;
        public float MarketInfluence;
        public bool AllowResourceTrading;
        public float TaxRate; // Team contribution rate
    }

    public enum EconomicsMode : byte
    {
        Trading_Cooperative = 0,  // Share trading benefits
        Production_Chain = 1,     // Specialized production roles
        Investment_Pool = 2,      // Shared investment fund
        Guild_System = 3          // Guild-based cooperation
    }

    /// <summary>
    /// Sports Team Component - Sports/RhythmGame
    /// </summary>
    public struct SportsTeamComponent : IComponentData
    {
        public int TeamScore;
        public int Passes;
        public int Assists;
        public float Coordination; // Timing precision
        public SportsType Sport;
        public bool AllowPlayerSubstitution;
    }

    public enum SportsType : byte
    {
        Generic = 0,
        Ball_Based = 1,
        Rhythm_Based = 2,
        Timing_Based = 3
    }

    /// <summary>
    /// Stealth Team Component - Stealth genre
    /// </summary>
    public struct StealthTeamComponent : IComponentData
    {
        public float TeamVisibility; // Combined stealth rating
        public int SynchronizedTakedowns;
        public int DistractionsCreated;
        public int AlertsTriggered;
        public bool TeamDetected;
        public float NoiseLevel; // Combined noise
    }

    /// <summary>
    /// Tower Defense Team Component
    /// </summary>
    public struct TowerDefenseTeamComponent : IComponentData
    {
        public int TowersBuilt;
        public int WavesDefeated;
        public int LivesRemaining;
        public bool SharedResources;
        public bool SharedTowerControl;
        public float ZoneCoveragePercent;
    }

    /// <summary>
    /// Board/Card Game Team Component - BoardGame/CardGame
    /// </summary>
    public struct BoardCardTeamComponent : IComponentData
    {
        public BoardGameMode Mode;
        public int TurnsTaken;
        public int CardsPlayed;
        public int CombinedScore;
        public bool AllowCardTrading;
        public bool SharedHand; // Can see each other's cards
    }

    public enum BoardGameMode : byte
    {
        Cooperative = 0,
        Team_Vs_Team = 1,
        Shared_Deck = 2
    }

    /// <summary>
    /// Detective Team Component
    /// </summary>
    public struct DetectiveTeamComponent : IComponentData
    {
        public int CluesFound;
        public int CluesShared;
        public int TheoriesDeveloped;
        public float InvestigationProgress;
        public bool AllowClueSharing;
        public int InterrogationsCompleted;
    }

    /// <summary>
    /// City Builder Team Component
    /// </summary>
    public struct CityBuilderTeamComponent : IComponentData
    {
        public int BuildingsConstructed;
        public int PopulationTotal;
        public float CityHappiness;
        public bool SharedResources;
        public bool SharedZoning;
        public int DistrictCount;
    }

    /// <summary>
    /// Battle Royale Team Component
    /// </summary>
    public struct BattleRoyaleTeamComponent : IComponentData
    {
        public int TeamPlacement;
        public int EnemyTeamsEliminated;
        public int LootShared;
        public float SafeZoneDistance;
        public int RevivesPerformed;
        public bool LastTeamStanding;
    }

    /// <summary>
    /// Roguelike Team Component - Roguelike/Roguelite/BulletHell
    /// </summary>
    public struct RoguelikeTeamComponent : IComponentData
    {
        public int FloorDepth;
        public int ItemsShared;
        public int BossesDefeated;
        public bool PermadeathActive;
        public bool ShareProgression; // Meta-progression shared
        public int RunsCompleted;
    }

    #endregion

    #region Genre Team Systems

    /// <summary>
    /// Combat Genre Team System - Handles all combat-oriented genres
    /// Genres: FPS, ThirdPersonShooter, Fighting, BeatEmUp, HackAndSlash, SurvivalHorror
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class CombatGenreTeamSystem : SystemBase
    {
        private EntityQuery _combatTeamQuery;

        protected override void OnCreate()
        {
            _combatTeamQuery = GetEntityQuery(
                ComponentType.ReadWrite<CombatTeamComponent>(),
                ComponentType.ReadWrite<TeamComponent>(),
                ComponentType.ReadWrite<TeamPerformanceComponent>()
            );
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (combatTeam, team, performance) in
                SystemAPI.Query<RefRW<CombatTeamComponent>,
                               RefRW<TeamComponent>,
                               RefRW<TeamPerformanceComponent>>())
            {
                // Update combat metrics
                UpdateCombatMetrics(ref combatTeam.ValueRW, ref performance.ValueRW);

                // Formation bonuses
                ApplyFormationBonuses(ref combatTeam.ValueRW, ref team.ValueRW);

                // Combat efficiency affects team cohesion
                team.ValueRW.TeamCohesion += (combatTeam.ValueRO.CombatEfficiency - 0.5f) * deltaTime * 0.1f;
                team.ValueRW.TeamCohesion = math.clamp(team.ValueRW.TeamCohesion, 0f, 1f);

                // Revives boost morale
                if (combatTeam.ValueRO.TeamRevives > 0)
                {
                    team.ValueRW.TeamMorale = math.min(1f, team.ValueRW.TeamMorale + 0.1f);
                }
            }
        }

        private void UpdateCombatMetrics(
            ref CombatTeamComponent combat,
            ref TeamPerformanceComponent performance)
        {
            // Aggregate combat stats
            combat.TeamDPS = performance.CombinedDPS;
            combat.TeamHealing = performance.CombinedHealing;

            // Calculate efficiency based on performance
            float damageEfficiency = math.clamp(combat.TeamDPS / 1000f, 0f, 1f);
            float healingEfficiency = math.clamp(combat.TeamHealing / 500f, 0f, 1f);

            combat.CombatEfficiency = (damageEfficiency * 0.6f + healingEfficiency * 0.4f);
        }

        private void ApplyFormationBonuses(
            ref CombatTeamComponent combat,
            ref TeamComponent team)
        {
            // Formation bonuses based on type
            float formationBonus = combat.Formation switch
            {
                FormationType.Line => 0.15f,     // +15% defense
                FormationType.Wedge => 0.20f,    // +20% attack
                FormationType.Phalanx => 0.25f,  // +25% defense
                FormationType.Circle => 0.10f,   // +10% all-around
                _ => 0f
            };

            combat.CombatEfficiency *= (1f + formationBonus * team.TeamCohesion);
        }
    }

    /// <summary>
    /// Racing Genre Team System - Handles all racing genres
    /// Genres: Racing, EndlessRunner, VehicleSimulation, FlightSimulator
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RacingGenreTeamSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (racingTeam, team, performance) in
                SystemAPI.Query<RefRW<RacingTeamComponent>,
                               RefRW<TeamComponent>,
                               RefRW<TeamPerformanceComponent>>())
            {
                switch (racingTeam.ValueRO.Mode)
                {
                    case RacingMode.Traditional:
                        ProcessTraditionalRacing(ref racingTeam.ValueRW, ref performance.ValueRW);
                        break;

                    case RacingMode.Relay:
                        ProcessRelayRacing(ref racingTeam.ValueRW, ref team.ValueRW);
                        break;

                    case RacingMode.Cooperative:
                        ProcessCooperativeRacing(ref racingTeam.ValueRW, ref team.ValueRW);
                        break;

                    case RacingMode.Pursuit:
                        ProcessPursuitRacing(ref racingTeam.ValueRW);
                        break;
                }

                // Drafting bonuses boost team cohesion
                team.ValueRW.TeamCohesion += racingTeam.ValueRO.DraftingBonus * deltaTime * 0.05f;
                team.ValueRW.TeamCohesion = math.clamp(team.ValueRW.TeamCohesion, 0f, 1f);
            }
        }

        private void ProcessTraditionalRacing(
            ref RacingTeamComponent racing,
            ref TeamPerformanceComponent performance)
        {
            // Track combined speed
            racing.CombinedSpeed = performance.AverageSpeed;
        }

        private void ProcessRelayRacing(
            ref RacingTeamComponent racing,
            ref TeamComponent team)
        {
            // Relay handoffs affect team performance
            // Smooth handoffs = better cohesion
        }

        private void ProcessCooperativeRacing(
            ref RacingTeamComponent racing,
            ref TeamComponent team)
        {
            // Combined team time
            // Slower members can be helped by faster ones
            racing.DraftingBonus = team.TeamCohesion * 0.2f; // Up to 20% speed boost
        }

        private void ProcessPursuitRacing(ref RacingTeamComponent racing)
        {
            // Chase/evade mechanics
        }
    }

    /// <summary>
    /// Puzzle Genre Team System
    /// Genres: Puzzle, Match3, TetrisLike, PhysicsPuzzle, HiddenObject, WordGame
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PuzzleGenreTeamSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (puzzleTeam, team) in
                SystemAPI.Query<RefRW<PuzzleTeamComponent>, RefRW<TeamComponent>>())
            {
                // Collaborative puzzle solving increases cohesion
                if (puzzleTeam.ValueRO.CollaborativeSolutions > 0)
                {
                    team.ValueRW.TeamCohesion += 0.05f;
                    team.ValueRW.TeamCohesion = math.min(1f, team.ValueRW.TeamCohesion);
                }

                // Hint sharing improves team learning
                if (puzzleTeam.ValueRO.AllowHintSharing && puzzleTeam.ValueRO.HintsGiven > 0)
                {
                    team.ValueRW.TeamMorale += 0.02f;
                    team.ValueRW.TeamMorale = math.min(1f, team.ValueRW.TeamMorale);
                }

                // Solution synchronization bonus
                float syncBonus = puzzleTeam.ValueRO.SolutionSyncScore * 0.3f;
                team.ValueRW.TeamCohesion = math.lerp(team.ValueRW.TeamCohesion,
                    team.ValueRW.TeamCohesion + syncBonus, 0.1f);
            }
        }
    }

    /// <summary>
    /// Strategy Genre Team System
    /// Genres: Strategy, RTS, TurnBased, 4X, GrandStrategy, AutoBattler, ChessLike
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class StrategyGenreTeamSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (strategyTeam, team, performance) in
                SystemAPI.Query<RefRW<StrategyTeamComponent>,
                               RefRW<TeamComponent>,
                               RefRW<TeamPerformanceComponent>>())
            {
                // Shared economy improves resource efficiency
                if (strategyTeam.ValueRO.SharedEconomy)
                {
                    performance.ValueRW.ResourcesGathered *= 1.2f; // 20% bonus
                }

                // Shared vision improves coordination
                if (strategyTeam.ValueRO.SharedVision)
                {
                    strategyTeam.ValueRW.StrategicCoordination += 0.1f;
                }

                // Strategic coordination affects team cohesion
                team.ValueRW.TeamCohesion = math.lerp(
                    team.ValueRW.TeamCohesion,
                    strategyTeam.ValueRO.StrategicCoordination,
                    0.05f);
            }
        }
    }

    /// <summary>
    /// Exploration Genre Team System
    /// Genres: Exploration, Metroidvania, WalkingSimulator, PointAndClickAdventure
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class ExplorationGenreTeamSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (exploration, team, performance) in
                SystemAPI.Query<RefRW<ExplorationTeamComponent>,
                               RefRW<TeamComponent>,
                               RefRW<TeamPerformanceComponent>>())
            {
                // Shared discoveries boost morale
                if (exploration.ValueRO.DiscoveriesShared > 0)
                {
                    team.ValueRW.TeamMorale += 0.01f * exploration.ValueRO.DiscoveriesShared;
                    team.ValueRW.TeamMorale = math.min(1f, team.ValueRW.TeamMorale);
                }

                // Map sharing improves exploration efficiency
                if (exploration.ValueRO.AllowMapSharing)
                {
                    exploration.ValueRW.MapCoveragePercent += 0.5f; // Faster coverage
                }

                // Update performance metrics
                performance.ValueRW.ResourcesGathered = exploration.ValueRO.ResourcesGathered;
            }
        }
    }

    /// <summary>
    /// Economics Genre Team System
    /// Genres: Economics, FarmingSimulator, ConstructionSimulator
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class EconomicsGenreTeamSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var (economics, team, resourcePool) in
                SystemAPI.Query<RefRW<EconomicsTeamComponent>,
                               RefRW<TeamComponent>,
                               RefRW<TeamResourcePoolComponent>>())
            {
                // Update shared wealth
                if (economics.ValueRO.AllowResourceTrading)
                {
                    resourcePool.ValueRW.SharedCurrency = economics.ValueRO.TeamWealth;
                }

                // Production chains improve efficiency
                if (economics.ValueRO.Mode == EconomicsMode.Production_Chain)
                {
                    float efficiency = 1f + (team.ValueRO.CurrentMembers * 0.15f);
                    economics.ValueRW.ProductionOutputs = (int)(economics.ValueRO.ProductionOutputs * efficiency);
                }

                // Market influence boosts team cohesion
                team.ValueRW.TeamCohesion += economics.ValueRO.MarketInfluence * 0.01f;
                team.ValueRW.TeamCohesion = math.clamp(team.ValueRW.TeamCohesion, 0f, 1f);
            }
        }
    }

    /// <summary>
    /// Comprehensive Genre-Specific Team Manager
    /// Activates appropriate team components based on active genre
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GenreTeamActivationSystem : SystemBase
    {
        private GameGenre _currentGenre = GameGenre.Exploration;

        protected override void OnUpdate()
        {
            // This would integrate with GenreGameModeManager to detect genre changes
            // For now, this is a placeholder for the activation logic
        }

        /// <summary>
        /// Activate genre-specific team components for a team
        /// </summary>
        public void ActivateGenreTeamComponents(Entity teamEntity, GameGenre genre)
        {
            var em = EntityManager;

            // Remove previous genre components
            RemoveAllGenreComponents(teamEntity);

            // Add new genre-specific components
            switch (genre)
            {
                // Combat genres
                case GameGenre.FPS:
                case GameGenre.ThirdPersonShooter:
                case GameGenre.Fighting:
                case GameGenre.BeatEmUp:
                case GameGenre.HackAndSlash:
                case GameGenre.SurvivalHorror:
                    em.AddComponentData(teamEntity, new CombatTeamComponent
                    {
                        Formation = FormationType.Line
                    });
                    break;

                // Racing genres
                case GameGenre.Racing:
                case GameGenre.EndlessRunner:
                case GameGenre.VehicleSimulation:
                case GameGenre.FlightSimulator:
                    em.AddComponentData(teamEntity, new RacingTeamComponent
                    {
                        Mode = RacingMode.Traditional
                    });
                    break;

                // Puzzle genres
                case GameGenre.Puzzle:
                case GameGenre.Match3:
                case GameGenre.TetrisLike:
                case GameGenre.PhysicsPuzzle:
                case GameGenre.HiddenObject:
                case GameGenre.WordGame:
                    em.AddComponentData(teamEntity, new PuzzleTeamComponent
                    {
                        Mode = PuzzleCooperationMode.Shared_Progress,
                        AllowHintSharing = true
                    });
                    break;

                // Strategy genres
                case GameGenre.Strategy:
                case GameGenre.RealTimeStrategy:
                case GameGenre.TurnBasedStrategy:
                case GameGenre.FourXStrategy:
                case GameGenre.GrandStrategy:
                case GameGenre.AutoBattler:
                case GameGenre.ChessLike:
                    em.AddComponentData(teamEntity, new StrategyTeamComponent
                    {
                        Mode = StrategyTeamMode.Allied_Forces,
                        SharedEconomy = true,
                        SharedVision = true
                    });
                    break;

                // Exploration genres
                case GameGenre.Exploration:
                case GameGenre.Metroidvania:
                case GameGenre.WalkingSimulator:
                case GameGenre.PointAndClickAdventure:
                    em.AddComponentData(teamEntity, new ExplorationTeamComponent
                    {
                        AllowMapSharing = true,
                        ExplorationRadius = 100f
                    });
                    break;

                // Economics genres
                case GameGenre.Economics:
                case GameGenre.FarmingSimulator:
                case GameGenre.ConstructionSimulator:
                    em.AddComponentData(teamEntity, new EconomicsTeamComponent
                    {
                        Mode = EconomicsMode.Guild_System,
                        AllowResourceTrading = true
                    });
                    break;

                // Sports genres
                case GameGenre.Sports:
                case GameGenre.RhythmGame:
                    em.AddComponentData(teamEntity, new SportsTeamComponent
                    {
                        Sport = SportsType.Generic,
                        AllowPlayerSubstitution = true
                    });
                    break;

                // Stealth genres
                case GameGenre.Stealth:
                    em.AddComponentData(teamEntity, new StealthTeamComponent());
                    break;

                // Tower Defense
                case GameGenre.TowerDefense:
                    em.AddComponentData(teamEntity, new TowerDefenseTeamComponent
                    {
                        SharedResources = true,
                        SharedTowerControl = false
                    });
                    break;

                // Board/Card games
                case GameGenre.BoardGame:
                case GameGenre.CardGame:
                    em.AddComponentData(teamEntity, new BoardCardTeamComponent
                    {
                        Mode = BoardGameMode.Team_Vs_Team,
                        AllowCardTrading = true
                    });
                    break;

                // Detective
                case GameGenre.Detective:
                    em.AddComponentData(teamEntity, new DetectiveTeamComponent
                    {
                        AllowClueSharing = true
                    });
                    break;

                // City Builder
                case GameGenre.CityBuilder:
                    em.AddComponentData(teamEntity, new CityBuilderTeamComponent
                    {
                        SharedResources = true,
                        SharedZoning = true
                    });
                    break;

                // Battle Royale
                case GameGenre.BattleRoyale:
                    em.AddComponentData(teamEntity, new BattleRoyaleTeamComponent());
                    break;

                // Roguelike/Roguelite
                case GameGenre.Roguelike:
                case GameGenre.Roguelite:
                case GameGenre.BulletHell:
                    em.AddComponentData(teamEntity, new RoguelikeTeamComponent
                    {
                        ShareProgression = true,
                        PermadeathActive = false // Disabled for team play
                    });
                    break;
            }

            Debug.Log($"✅ Activated {genre} team components for team entity");
        }

        private void RemoveAllGenreComponents(Entity teamEntity)
        {
            var em = EntityManager;

            if (em.HasComponent<CombatTeamComponent>(teamEntity))
                em.RemoveComponent<CombatTeamComponent>(teamEntity);
            if (em.HasComponent<RacingTeamComponent>(teamEntity))
                em.RemoveComponent<RacingTeamComponent>(teamEntity);
            if (em.HasComponent<PuzzleTeamComponent>(teamEntity))
                em.RemoveComponent<PuzzleTeamComponent>(teamEntity);
            if (em.HasComponent<StrategyTeamComponent>(teamEntity))
                em.RemoveComponent<StrategyTeamComponent>(teamEntity);
            if (em.HasComponent<ExplorationTeamComponent>(teamEntity))
                em.RemoveComponent<ExplorationTeamComponent>(teamEntity);
            if (em.HasComponent<EconomicsTeamComponent>(teamEntity))
                em.RemoveComponent<EconomicsTeamComponent>(teamEntity);
            if (em.HasComponent<SportsTeamComponent>(teamEntity))
                em.RemoveComponent<SportsTeamComponent>(teamEntity);
            if (em.HasComponent<StealthTeamComponent>(teamEntity))
                em.RemoveComponent<StealthTeamComponent>(teamEntity);
            if (em.HasComponent<TowerDefenseTeamComponent>(teamEntity))
                em.RemoveComponent<TowerDefenseTeamComponent>(teamEntity);
            if (em.HasComponent<BoardCardTeamComponent>(teamEntity))
                em.RemoveComponent<BoardCardTeamComponent>(teamEntity);
            if (em.HasComponent<DetectiveTeamComponent>(teamEntity))
                em.RemoveComponent<DetectiveTeamComponent>(teamEntity);
            if (em.HasComponent<CityBuilderTeamComponent>(teamEntity))
                em.RemoveComponent<CityBuilderTeamComponent>(teamEntity);
            if (em.HasComponent<BattleRoyaleTeamComponent>(teamEntity))
                em.RemoveComponent<BattleRoyaleTeamComponent>(teamEntity);
            if (em.HasComponent<RoguelikeTeamComponent>(teamEntity))
                em.RemoveComponent<RoguelikeTeamComponent>(teamEntity);
        }
    }

    #endregion
}
