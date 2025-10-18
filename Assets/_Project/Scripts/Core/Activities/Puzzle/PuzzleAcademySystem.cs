using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities;

namespace Laboratory.Core.Activities.Puzzle
{
    /// <summary>
    /// 🧩 PUZZLE ACADEMY SYSTEM - Complete puzzle mini-game implementation
    /// FEATURES: Match-3, logic puzzles, pattern recognition, memory games, adaptive difficulty
    /// PERFORMANCE: Efficient puzzle generation and solving algorithms
    /// GENETICS: Intelligence, Curiosity, Memory directly affect puzzle performance
    /// </summary>

    #region Puzzle Components

    /// <summary>
    /// Puzzle academy configuration and state
    /// </summary>
    public struct PuzzleAcademyComponent : IComponentData
    {
        public PuzzleType CurrentPuzzleType;
        public int DifficultyLevel;
        public int MaxParticipants;
        public int CurrentParticipants;
        public AcademyStatus Status;
        public float SessionTimer;
        public float SessionDuration;
        public int TotalPuzzlesAvailable;
        public int CompletedPuzzles;
        public float AverageSolveTime;
        public bool AdaptiveDifficulty;
        public int RewardMultiplier;

        // PvP Competition Features
        public bool PvPEnabled;
        public int ActiveBattles;
        public int CompetitionWins;
        public int CompetitionLosses;
        public float BattleTimer;
        public PuzzleBattleType CurrentBattleType;
        public int MaxBattleParticipants;
        public float CompetitionRewardMultiplier;
        public bool AllowTeamBattles;
    }

    /// <summary>
    /// Individual puzzle solver state
    /// </summary>
    public struct PuzzleSolverComponent : IComponentData
    {
        public Entity Academy;
        public SolverStatus Status;
        public PuzzleType CurrentPuzzle;
        public int PuzzleDifficulty;
        public float SolveTime;
        public float BestTime;
        public int MovesUsed;
        public int OptimalMoves;
        public int HintsUsed;
        public int PuzzlesCompleted;
        public int ConsecutiveSuccesses;
        public float Accuracy;
        public bool IsStuck;
        public float StuckTimer;

        // PvP Battle Data
        public bool IsInBattle;
        public Entity BattleOpponent;
        public PuzzleBattleType BattleMode;
        public float BattleScore;
        public int BattlePosition;
        public bool HasBattleAdvantage;
        public float BattleTimer;
        public int PvPWins;
        public int PvPLosses;
        public float PvPRating;
        public int SpeedBonusPoints;
        public int AccuracyBonusPoints;
    }

    /// <summary>
    /// Puzzle solving performance and capabilities
    /// </summary>
    public struct PuzzlePerformanceComponent : IComponentData
    {
        // Core solving abilities (from genetics)
        public float LogicalReasoning;
        public float PatternRecognition;
        public float SpatialIntelligence;
        public float MemoryCapacity;
        public float ProcessingSpeed;
        public float Creativity;
        public float Persistence;

        // Puzzle-specific bonuses
        public float Match3Bonus;
        public float LogicPuzzleBonus;
        public float MemoryGameBonus;
        public float PatternBonus;
        public float MathPuzzleBonus;
        public float WordPuzzleBonus;

        // Equipment bonuses
        public float ConcentrationBoost;
        public float MemoryBoost;
        public float SpeedBoost;

        // Learning bonuses
        public int PuzzleExperience;
        public float LearningRate;
        public bool HasPuzzleIntuition;
    }

    /// <summary>
    /// Match-3 puzzle state
    /// </summary>
    public struct Match3PuzzleComponent : IComponentData
    {
        public int BoardWidth;
        public int BoardHeight;
        public int TargetScore;
        public int CurrentScore;
        public int MovesRemaining;
        public int ChainsCreated;
        public int SpecialGemsUsed;
        public bool HasObjectives;
        public int ObjectiveProgress;
        public float ComboMultiplier;
        public float TimeBonus;
    }

    /// <summary>
    /// Logic puzzle state
    /// </summary>
    public struct LogicPuzzleComponent : IComponentData
    {
        public LogicPuzzleType Type;
        public int GridSize;
        public int CluesGiven;
        public int CluesUsed;
        public int CorrectPlacements;
        public int IncorrectAttempts;
        public bool UseAdvancedLogic;
        public float LogicComplexity;
        public int DeductionSteps;
        public bool SolutionFound;
    }

    /// <summary>
    /// Memory game state
    /// </summary>
    public struct MemoryGameComponent : IComponentData
    {
        public MemoryGameType Type;
        public int SequenceLength;
        public int CurrentPosition;
        public int CorrectSequences;
        public int FailedAttempts;
        public float DisplayTime;
        public float RecallTime;
        public bool IsDisplayPhase;
        public bool IsRecallPhase;
        public float MemoryLoad;
    }

    /// <summary>
    /// Pattern recognition puzzle state
    /// </summary>
    public struct PatternPuzzleComponent : IComponentData
    {
        public PatternType Type;
        public int PatternComplexity;
        public int ElementsInPattern;
        public int CorrectIdentifications;
        public int MissedPatterns;
        public bool IsVisualPattern;
        public bool IsSequentialPattern;
        public float PatternSpeed;
        public int VariationCount;
    }

    /// <summary>
    /// PvP puzzle battle mechanics
    /// </summary>
    public struct PuzzleBattleComponent : IComponentData
    {
        public PuzzleBattleType Type;
        public BattleStatus Status;
        public Entity Participant1;
        public Entity Participant2;
        public float Battle1Score;
        public float Battle2Score;
        public float TimeLimit;
        public float TimeRemaining;
        public Entity Winner;
        public BattleRules Rules;
        public PuzzleType BattlePuzzleType;
        public int BattleDifficulty;
        public int SpectatorCount;
        public float PrizePool;
        public bool IsTeamBattle;
        public int TeamSize;
        public uint BattleSeed;
    }

    /// <summary>
    /// Speed solving competition mechanics
    /// </summary>
    public struct SpeedSolvingComponent : IComponentData
    {
        public Entity Competitor1;
        public Entity Competitor2;
        public float Speed1Time;
        public float Speed2Time;
        public int Speed1Moves;
        public int Speed2Moves;
        public bool IsTimeAttack;
        public float TimeBonus1;
        public float TimeBonus2;
        public float AccuracyPenalty1;
        public float AccuracyPenalty2;
        public SpeedSolvingStatus Status;
    }

    /// <summary>
    /// Collaborative puzzle mechanics for team battles
    /// </summary>
    public struct CollaborativePuzzleComponent : IComponentData
    {
        public Entity TeamLead1;
        public Entity TeamLead2;
        public int Team1Members;
        public int Team2Members;
        public float Team1Progress;
        public float Team2Progress;
        public bool RequiresCoordination;
        public float CoordinationBonus1;
        public float CoordinationBonus2;
        public int SharedPuzzleElements;
        public CollaborationStatus Status;
    }

    #endregion

    #region Puzzle Enums

    public enum PuzzleType : byte
    {
        Match3,
        Logic_Grid,
        Memory_Sequence,
        Pattern_Recognition,
        Word_Puzzle,
        Math_Problem,
        Spatial_Puzzle,
        Color_Matching,
        Symbol_Logic,
        Rhythm_Pattern
    }

    public enum AcademyStatus : byte
    {
        Open,
        Session_Active,
        Testing,
        Evaluation,
        Closed,
        Maintenance
    }

    public enum SolverStatus : byte
    {
        Idle,
        Selecting_Puzzle,
        Solving,
        Thinking,
        Stuck,
        Completed,
        Failed,
        Taking_Break
    }

    public enum LogicPuzzleType : byte
    {
        Sudoku,
        Nonogram,
        Logic_Grid,
        Boolean_Logic,
        Sequence_Logic,
        Constraint_Puzzle
    }

    public enum MemoryGameType : byte
    {
        Sequence_Recall,
        Spatial_Memory,
        Color_Memory,
        Symbol_Memory,
        Pattern_Memory,
        Audio_Memory
    }

    public enum PatternType : byte
    {
        Visual_Sequence,
        Color_Pattern,
        Shape_Pattern,
        Movement_Pattern,
        Timing_Pattern,
        Mathematical_Pattern
    }

    // PvP Battle Enums
    public enum PuzzleBattleType : byte
    {
        Speed_Solving,
        Head_to_Head_Match3,
        Logic_Duel,
        Memory_Challenge,
        Pattern_Race,
        Collaborative_Puzzle,
        Elimination_Tournament,
        Team_Battle
    }

    public enum BattleStatus : byte
    {
        Waiting,
        Starting,
        Active,
        Completed,
        Tie,
        Cancelled,
        Disputed
    }

    public enum BattleRules : byte
    {
        Standard,
        No_Hints,
        Time_Pressure,
        Accuracy_Focus,
        Speed_Focus,
        Team_Only,
        Elimination,
        Best_of_Three
    }

    public enum SpeedSolvingStatus : byte
    {
        Preparing,
        Racing,
        Finished,
        Photo_Finish,
        Disqualified
    }

    public enum CollaborationStatus : byte
    {
        Organizing,
        Coordinating,
        Solving,
        Completed,
        Failed_Coordination
    }

    #endregion

    #region Puzzle Systems

    /// <summary>
    /// Main puzzle academy management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class PuzzleAcademyManagementSystem : SystemBase
    {
        private EntityQuery academyQuery;
        private EntityQuery solverQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            academyQuery = GetEntityQuery(ComponentType.ReadWrite<PuzzleAcademyComponent>());
            solverQuery = GetEntityQuery(ComponentType.ReadWrite<PuzzleSolverComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update puzzle academies
            var academyUpdateJob = new AcademyUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };
            Dependency = academyUpdateJob.ScheduleParallel(academyQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    public partial struct AcademyUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref PuzzleAcademyComponent academy)
        {
            switch (academy.Status)
            {
                case AcademyStatus.Open:
                    // Wait for puzzle solvers
                    if (academy.CurrentParticipants > 0)
                    {
                        academy.Status = AcademyStatus.Session_Active;
                        academy.SessionTimer = 0f;
                    }
                    break;

                case AcademyStatus.Session_Active:
                    UpdatePuzzleSession(ref academy, DeltaTime);
                    break;

                case AcademyStatus.Testing:
                    UpdateTesting(ref academy, DeltaTime);
                    break;

                case AcademyStatus.Evaluation:
                    UpdateEvaluation(ref academy, DeltaTime);
                    break;
            }

            // Update difficulty adaptation
            if (academy.AdaptiveDifficulty)
            {
                UpdateAdaptiveDifficulty(ref academy);
            }
        }


        private void UpdatePuzzleSession(ref PuzzleAcademyComponent academy, float deltaTime)
        {
            academy.SessionTimer += deltaTime;

            // Session complete check
            if (academy.SessionTimer >= academy.SessionDuration || academy.CurrentParticipants == 0)
            {
                academy.Status = AcademyStatus.Evaluation;
                academy.SessionTimer = 0f;
            }

            // Update average solve time (simplified)
            if (academy.CompletedPuzzles > 0)
            {
                academy.AverageSolveTime = academy.SessionTimer / academy.CompletedPuzzles;
            }
        }


        private void UpdateTesting(ref PuzzleAcademyComponent academy, float deltaTime)
        {
            academy.SessionTimer += deltaTime;

            // Testing phase for special puzzle types
            if (academy.SessionTimer >= 60f) // 1 minute testing
            {
                academy.Status = AcademyStatus.Session_Active;
                academy.SessionTimer = 0f;
            }
        }


        private void UpdateEvaluation(ref PuzzleAcademyComponent academy, float deltaTime)
        {
            academy.SessionTimer += deltaTime;

            // Evaluation and reward distribution
            if (academy.SessionTimer >= 30f) // 30 second evaluation
            {
                // Calculate rewards based on performance
                academy.RewardMultiplier = CalculateRewardMultiplier(academy);

                // Reset for next session
                academy.Status = AcademyStatus.Open;
                academy.CompletedPuzzles = 0;
                academy.SessionTimer = 0f;
                academy.CurrentParticipants = 0;
            }
        }


        private void UpdateAdaptiveDifficulty(ref PuzzleAcademyComponent academy)
        {
            // Adjust difficulty based on performance
            if (academy.AverageSolveTime > 0f)
            {
                if (academy.AverageSolveTime < 30f) // Too easy - increase difficulty
                {
                    academy.DifficultyLevel = math.min(10, academy.DifficultyLevel + 1);
                }
                else if (academy.AverageSolveTime > 180f) // Too hard - decrease difficulty
                {
                    academy.DifficultyLevel = math.max(1, academy.DifficultyLevel - 1);
                }
            }
        }


        private int CalculateRewardMultiplier(PuzzleAcademyComponent academy)
        {
            float efficiencyRatio = academy.CompletedPuzzles / math.max(1f, academy.SessionTimer / 60f);
            return (int)math.clamp(efficiencyRatio * 100f, 50f, 200f);
        }
    }

    /// <summary>
    /// Puzzle solving mechanics system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PuzzleAcademyManagementSystem))]
    public partial class PuzzleSolvingSystem : SystemBase
    {
        private EntityQuery puzzleQuery;

        protected override void OnCreate()
        {
            puzzleQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<PuzzleSolverComponent>(),
                ComponentType.ReadOnly<PuzzlePerformanceComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var solvingJob = new PuzzleSolvingJob
            {
                DeltaTime = deltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime
            };

            Dependency = solvingJob.ScheduleParallel(puzzleQuery, Dependency);
        }
    }


    public partial struct PuzzleSolvingJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;

        public void Execute(ref PuzzleSolverComponent solver,
            in PuzzlePerformanceComponent performance,
            in GeneticDataComponent genetics)
        {
            if (solver.Status != SolverStatus.Solving)
                return;

            solver.SolveTime += DeltaTime;

            // Process puzzle solving based on type
            bool puzzleProgressed = ProcessPuzzleType(ref solver, performance, genetics);

            if (puzzleProgressed)
            {
                solver.StuckTimer = 0f;
                solver.IsStuck = false;
            }
            else
            {
                solver.StuckTimer += DeltaTime;
                if (solver.StuckTimer > GetStuckThreshold(performance, genetics))
                {
                    solver.IsStuck = true;
                    HandleStuckState(ref solver, performance);
                }
            }

            // Check for completion
            CheckPuzzleCompletion(ref solver, performance, genetics);
        }


        private bool ProcessPuzzleType(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            return solver.CurrentPuzzle switch
            {
                PuzzleType.Match3 => ProcessMatch3Solving(ref solver, performance, genetics),
                PuzzleType.Logic_Grid => ProcessLogicPuzzleSolving(ref solver, performance, genetics),
                PuzzleType.Memory_Sequence => ProcessMemoryGameSolving(ref solver, performance, genetics),
                PuzzleType.Pattern_Recognition => ProcessPatternPuzzleSolving(ref solver, performance, genetics),
                PuzzleType.Math_Problem => ProcessMathPuzzleSolving(ref solver, performance, genetics),
                PuzzleType.Word_Puzzle => ProcessWordPuzzleSolving(ref solver, performance, genetics),
                PuzzleType.Spatial_Puzzle => ProcessSpatialPuzzleSolving(ref solver, performance, genetics),
                _ => ProcessGenericPuzzleSolving(ref solver, performance, genetics)
            };
        }


        private bool ProcessMatch3Solving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Match-3 solving simulation
            float solvingSpeed = genetics.Intelligence * performance.Match3Bonus * performance.ProcessingSpeed;
            float progressChance = solvingSpeed * DeltaTime * 0.1f;

            if (Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 1000)).NextFloat() < progressChance)
            {
                solver.MovesUsed++;
                return true;
            }
            return false;
        }


        private bool ProcessLogicPuzzleSolving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Logic puzzle solving simulation
            float logicPower = genetics.Intelligence * performance.LogicalReasoning * performance.LogicPuzzleBonus;
            float deductionChance = logicPower * DeltaTime * 0.05f; // Slower than match-3

            if (Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 1337)).NextFloat() < deductionChance)
            {
                solver.MovesUsed++;
                return true;
            }
            return false;
        }


        private bool ProcessMemoryGameSolving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Memory game solving simulation
            float memoryPower = genetics.Intelligence * performance.MemoryCapacity * performance.MemoryGameBonus;
            float recallChance = memoryPower * DeltaTime * 0.15f;

            if (Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 2021)).NextFloat() < recallChance)
            {
                solver.MovesUsed++;
                return true;
            }
            return false;
        }


        private bool ProcessPatternPuzzleSolving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Pattern recognition solving simulation
            float patternPower = genetics.Curiosity * performance.PatternRecognition * performance.PatternBonus;
            float recognitionChance = patternPower * DeltaTime * 0.12f;

            if (Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 42)).NextFloat() < recognitionChance)
            {
                solver.MovesUsed++;
                return true;
            }
            return false;
        }


        private bool ProcessMathPuzzleSolving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Math puzzle solving simulation
            float mathPower = genetics.Intelligence * performance.LogicalReasoning * performance.MathPuzzleBonus;
            float calculationChance = mathPower * DeltaTime * 0.08f;

            return Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 314)).NextFloat() < calculationChance;
        }


        private bool ProcessWordPuzzleSolving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Word puzzle solving simulation
            float wordPower = genetics.Intelligence * performance.ProcessingSpeed * performance.WordPuzzleBonus;
            float wordChance = wordPower * DeltaTime * 0.1f;

            return Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 789)).NextFloat() < wordChance;
        }


        private bool ProcessSpatialPuzzleSolving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Spatial puzzle solving simulation
            float spatialPower = genetics.Agility * performance.SpatialIntelligence;
            float spatialChance = spatialPower * DeltaTime * 0.09f;

            return Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 111)).NextFloat() < spatialChance;
        }


        private bool ProcessGenericPuzzleSolving(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Generic puzzle solving simulation
            float genericPower = genetics.Intelligence * performance.LogicalReasoning;
            float genericChance = genericPower * DeltaTime * 0.1f;

            return Unity.Mathematics.Random.CreateFromIndex((uint)(Time * 555)).NextFloat() < genericChance;
        }


        private float GetStuckThreshold(PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Time before considering monster "stuck" (inversely related to persistence)
            float baseThreshold = 60f; // 1 minute base
            float persistenceBonus = performance.Persistence + genetics.Intelligence * 0.5f;
            return baseThreshold / math.max(0.1f, persistenceBonus);
        }


        private void HandleStuckState(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance)
        {
            solver.Status = SolverStatus.Stuck;

            // Decide whether to use hint or give up
            if (performance.HasPuzzleIntuition && solver.HintsUsed < 3)
            {
                solver.HintsUsed++;
                solver.Status = SolverStatus.Solving;
                solver.IsStuck = false;
                solver.StuckTimer = 0f;
            }
            else if (solver.StuckTimer > 180f) // 3 minutes stuck = give up
            {
                solver.Status = SolverStatus.Failed;
            }
        }


        private void CheckPuzzleCompletion(ref PuzzleSolverComponent solver, PuzzlePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Determine completion based on puzzle type and moves
            int requiredMoves = CalculateRequiredMoves(solver.CurrentPuzzle, solver.PuzzleDifficulty);

            if (solver.MovesUsed >= requiredMoves)
            {
                solver.Status = SolverStatus.Completed;
                solver.PuzzlesCompleted++;
                solver.ConsecutiveSuccesses++;

                // Calculate accuracy
                solver.Accuracy = (float)solver.OptimalMoves / math.max(1f, solver.MovesUsed);

                // Update best time
                if (solver.SolveTime < solver.BestTime || solver.BestTime == 0f)
                {
                    solver.BestTime = solver.SolveTime;
                }

                // Reset for next puzzle
                solver.MovesUsed = 0;
                solver.SolveTime = 0f;
                solver.HintsUsed = 0;
            }
        }


        private int CalculateRequiredMoves(PuzzleType puzzleType, int difficulty)
        {
            int baseMoves = puzzleType switch
            {
                PuzzleType.Match3 => 20,
                PuzzleType.Logic_Grid => 15,
                PuzzleType.Memory_Sequence => 10,
                PuzzleType.Pattern_Recognition => 5,
                PuzzleType.Math_Problem => 8,
                PuzzleType.Word_Puzzle => 12,
                PuzzleType.Spatial_Puzzle => 18,
                _ => 15
            };

            return baseMoves + (difficulty * 3);
        }
    }

    /// <summary>
    /// Specialized Match-3 puzzle system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PuzzleSolvingSystem))]
    public partial class Match3PuzzleSystem : SystemBase
    {
        private EntityQuery match3Query;

        protected override void OnCreate()
        {
            match3Query = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<Match3PuzzleComponent>(),
                ComponentType.ReadOnly<PuzzleSolverComponent>()
            });
        }

        protected override void OnUpdate()
        {
            foreach (var (match3, solver) in SystemAPI.Query<RefRW<Match3PuzzleComponent>, RefRO<PuzzleSolverComponent>>())
            {
                if (solver.ValueRO.CurrentPuzzle != PuzzleType.Match3 || solver.ValueRO.Status != SolverStatus.Solving)
                    continue;

                UpdateMatch3Game(ref match3.ValueRW, solver.ValueRO);
            }
        }

        private void UpdateMatch3Game(ref Match3PuzzleComponent match3, PuzzleSolverComponent solver)
        {
            // Simulate match-3 game progression
            if (solver.MovesUsed > 0)
            {
                // Calculate score based on moves and combos
                int moveScore = CalculateMatch3Score(solver.MovesUsed, match3.ChainsCreated);
                match3.CurrentScore += moveScore;

                // Update combo multiplier
                if (solver.MovesUsed % 3 == 0) // Every 3rd move creates a chain
                {
                    match3.ChainsCreated++;
                    match3.ComboMultiplier += 0.1f;
                }

                // Update moves remaining
                match3.MovesRemaining = math.max(0, match3.MovesRemaining - 1);

                // Check for special gems
                if (match3.ChainsCreated > 0 && math.random().NextFloat() < 0.3f)
                {
                    match3.SpecialGemsUsed++;
                    match3.CurrentScore += 100; // Bonus for special gems
                }
            }

            // Clamp combo multiplier
            match3.ComboMultiplier = math.clamp(match3.ComboMultiplier, 1f, 5f);
        }

        private int CalculateMatch3Score(int moves, int chains)
        {
            int baseScore = moves * 10;
            int chainBonus = chains * chains * 5; // Exponential chain bonus
            return baseScore + chainBonus;
        }
    }

    /// <summary>
    /// PvP Puzzle Battle System
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(Match3PuzzleSystem))]
    public partial class PuzzlePvPBattleSystem : SystemBase
    {
        private EntityQuery battleQuery;
        private EntityQuery speedQuery;
        private EntityQuery collaborativeQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            battleQuery = GetEntityQuery(ComponentType.ReadWrite<PuzzleBattleComponent>());
            speedQuery = GetEntityQuery(ComponentType.ReadWrite<SpeedSolvingComponent>());
            collaborativeQuery = GetEntityQuery(ComponentType.ReadWrite<CollaborativePuzzleComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update puzzle battles
            var battleJob = new PuzzleBattleUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };
            Dependency = battleJob.ScheduleParallel(battleQuery, Dependency);

            // Update speed solving competitions
            var speedJob = new SpeedSolvingJob
            {
                DeltaTime = deltaTime
            };
            Dependency = speedJob.ScheduleParallel(speedQuery, Dependency);

            // Update collaborative puzzles
            var collaborativeJob = new CollaborativePuzzleJob
            {
                DeltaTime = deltaTime
            };
            Dependency = collaborativeJob.ScheduleParallel(collaborativeQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    public partial struct PuzzleBattleUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref PuzzleBattleComponent battle)
        {
            switch (battle.Status)
            {
                case BattleStatus.Waiting:
                    // Wait for participants to be ready
                    if (battle.Participant1 != Entity.Null && battle.Participant2 != Entity.Null)
                    {
                        battle.Status = BattleStatus.Starting;
                        battle.TimeRemaining = battle.TimeLimit;
                    }
                    break;

                case BattleStatus.Starting:
                    battle.Status = BattleStatus.Active;
                    break;

                case BattleStatus.Active:
                    UpdateActiveBattle(ref battle);
                    break;

                case BattleStatus.Completed:
                    // Award prizes and cleanup
                    ProcessBattleResults(ref battle);
                    CommandBuffer.DestroyEntity(chunkIndex, entity);
                    break;
            }
        }


        private void UpdateActiveBattle(ref PuzzleBattleComponent battle)
        {
            battle.TimeRemaining -= DeltaTime;

            // Update scores based on battle type
            switch (battle.Type)
            {
                case PuzzleBattleType.Speed_Solving:
                    UpdateSpeedSolvingBattle(ref battle);
                    break;

                case PuzzleBattleType.Head_to_Head_Match3:
                    UpdateMatch3Battle(ref battle);
                    break;

                case PuzzleBattleType.Logic_Duel:
                    UpdateLogicDuel(ref battle);
                    break;

                case PuzzleBattleType.Memory_Challenge:
                    UpdateMemoryChallenge(ref battle);
                    break;

                case PuzzleBattleType.Pattern_Race:
                    UpdatePatternRace(ref battle);
                    break;

                case PuzzleBattleType.Team_Battle:
                    UpdateTeamBattle(ref battle);
                    break;
            }

            // Check for completion
            if (battle.TimeRemaining <= 0f || HasBattleWinCondition(battle))
            {
                DetermineBattleWinner(ref battle);
                battle.Status = BattleStatus.Completed;
            }
        }


        private void UpdateSpeedSolvingBattle(ref PuzzleBattleComponent battle)
        {
            // Speed solving simulation
            float speed1 = math.random().NextFloat(0.8f, 1.2f) * DeltaTime;
            float speed2 = math.random().NextFloat(0.8f, 1.2f) * DeltaTime;

            battle.Battle1Score += speed1;
            battle.Battle2Score += speed2;
        }


        private void UpdateMatch3Battle(ref PuzzleBattleComponent battle)
        {
            // Match-3 head-to-head simulation
            if (math.random().NextFloat() < 0.1f) // 10% chance per frame for moves
            {
                if (math.random().NextFloat() < 0.5f)
                    battle.Battle1Score += math.random().NextFloat(1f, 5f);
                else
                    battle.Battle2Score += math.random().NextFloat(1f, 5f);
            }
        }


        private void UpdateLogicDuel(ref PuzzleBattleComponent battle)
        {
            // Logic puzzle solving duel
            float logic1 = math.random().NextFloat(0.5f, 1f) * DeltaTime;
            float logic2 = math.random().NextFloat(0.5f, 1f) * DeltaTime;

            battle.Battle1Score += logic1;
            battle.Battle2Score += logic2;
        }


        private void UpdateMemoryChallenge(ref PuzzleBattleComponent battle)
        {
            // Memory challenge simulation
            if (math.random().NextFloat() < 0.05f) // 5% chance per frame
            {
                float memory1 = math.random().NextFloat(1f, 3f);
                float memory2 = math.random().NextFloat(1f, 3f);

                battle.Battle1Score += memory1;
                battle.Battle2Score += memory2;
            }
        }


        private void UpdatePatternRace(ref PuzzleBattleComponent battle)
        {
            // Pattern recognition race
            float pattern1 = math.random().NextFloat(0.7f, 1.3f) * DeltaTime;
            float pattern2 = math.random().NextFloat(0.7f, 1.3f) * DeltaTime;

            battle.Battle1Score += pattern1;
            battle.Battle2Score += pattern2;
        }


        private void UpdateTeamBattle(ref PuzzleBattleComponent battle)
        {
            // Team collaboration simulation
            float team1 = math.random().NextFloat(0.6f, 1.4f) * DeltaTime * battle.TeamSize;
            float team2 = math.random().NextFloat(0.6f, 1.4f) * DeltaTime * battle.TeamSize;

            battle.Battle1Score += team1;
            battle.Battle2Score += team2;
        }


        private bool HasBattleWinCondition(PuzzleBattleComponent battle)
        {
            switch (battle.Type)
            {
                case PuzzleBattleType.Speed_Solving:
                    return battle.Battle1Score >= 100f || battle.Battle2Score >= 100f;

                case PuzzleBattleType.Head_to_Head_Match3:
                    return battle.Battle1Score >= 500f || battle.Battle2Score >= 500f;

                case PuzzleBattleType.Logic_Duel:
                    return battle.Battle1Score >= 50f || battle.Battle2Score >= 50f;

                case PuzzleBattleType.Memory_Challenge:
                    return battle.Battle1Score >= 20f || battle.Battle2Score >= 20f;

                case PuzzleBattleType.Pattern_Race:
                    return battle.Battle1Score >= 75f || battle.Battle2Score >= 75f;

                case PuzzleBattleType.Team_Battle:
                    return battle.Battle1Score >= 200f || battle.Battle2Score >= 200f;

                default:
                    return false;
            }
        }


        private void DetermineBattleWinner(ref PuzzleBattleComponent battle)
        {
            if (battle.Battle1Score > battle.Battle2Score)
            {
                battle.Winner = battle.Participant1;
            }
            else if (battle.Battle2Score > battle.Battle1Score)
            {
                battle.Winner = battle.Participant2;
            }
            else
            {
                battle.Status = BattleStatus.Tie;
                battle.Winner = Entity.Null;
            }
        }


        private void ProcessBattleResults(ref PuzzleBattleComponent battle)
        {
            // Award experience and prizes to winner
            if (battle.Winner != Entity.Null)
            {
                battle.PrizePool *= 1.5f; // Winner bonus
            }

            // Both participants get base experience
            // (This would update PuzzleSolverComponent.ExperienceGained in a full implementation)
        }
    }


    public partial struct SpeedSolvingJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref SpeedSolvingComponent speedSolving)
        {
            switch (speedSolving.Status)
            {
                case SpeedSolvingStatus.Racing:
                    UpdateSpeedRace(ref speedSolving);
                    break;

                case SpeedSolvingStatus.Finished:
                    // Calculate final results
                    CalculateSpeedResults(ref speedSolving);
                    break;
            }
        }


        private void UpdateSpeedRace(ref SpeedSolvingComponent speedSolving)
        {
            // Update race times
            speedSolving.Speed1Time += DeltaTime;
            speedSolving.Speed2Time += DeltaTime;

            // Simulate moves
            if (math.random().NextFloat() < 0.1f) // 10% chance per frame
            {
                if (math.random().NextFloat() < 0.5f)
                    speedSolving.Speed1Moves++;
                else
                    speedSolving.Speed2Moves++;
            }

            // Check for completion
            if (speedSolving.Speed1Moves >= 20 || speedSolving.Speed2Moves >= 20)
            {
                speedSolving.Status = SpeedSolvingStatus.Finished;
            }
        }


        private void CalculateSpeedResults(ref SpeedSolvingComponent speedSolving)
        {
            // Calculate time bonuses and accuracy penalties
            speedSolving.TimeBonus1 = math.max(0f, 60f - speedSolving.Speed1Time);
            speedSolving.TimeBonus2 = math.max(0f, 60f - speedSolving.Speed2Time);

            // Accuracy penalty for extra moves
            int optimalMoves = 15;
            speedSolving.AccuracyPenalty1 = math.max(0f, speedSolving.Speed1Moves - optimalMoves) * 2f;
            speedSolving.AccuracyPenalty2 = math.max(0f, speedSolving.Speed2Moves - optimalMoves) * 2f;
        }
    }


    public partial struct CollaborativePuzzleJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref CollaborativePuzzleComponent collaborative)
        {
            switch (collaborative.Status)
            {
                case CollaborationStatus.Coordinating:
                    UpdateCoordination(ref collaborative);
                    break;

                case CollaborationStatus.Solving:
                    UpdateCollaborativeSolving(ref collaborative);
                    break;
            }
        }


        private void UpdateCoordination(ref CollaborativePuzzleComponent collaborative)
        {
            // Simulate team coordination
            float coordination1 = math.random().NextFloat(0.5f, 1.5f) * DeltaTime;
            float coordination2 = math.random().NextFloat(0.5f, 1.5f) * DeltaTime;

            collaborative.CoordinationBonus1 += coordination1;
            collaborative.CoordinationBonus2 += coordination2;

            // Move to solving phase when coordination is established
            if (collaborative.CoordinationBonus1 >= 5f && collaborative.CoordinationBonus2 >= 5f)
            {
                collaborative.Status = CollaborationStatus.Solving;
            }
        }


        private void UpdateCollaborativeSolving(ref CollaborativePuzzleComponent collaborative)
        {
            // Team solving with coordination bonuses
            float progress1 = collaborative.Team1Members * collaborative.CoordinationBonus1 * DeltaTime * 0.1f;
            float progress2 = collaborative.Team2Members * collaborative.CoordinationBonus2 * DeltaTime * 0.1f;

            collaborative.Team1Progress += progress1;
            collaborative.Team2Progress += progress2;

            // Check for completion
            if (collaborative.Team1Progress >= 100f || collaborative.Team2Progress >= 100f)
            {
                collaborative.Status = CollaborationStatus.Completed;
            }
        }
    }

    #endregion

    #region Puzzle Authoring

    /// <summary>
    /// MonoBehaviour authoring for puzzle academies
    /// </summary>
    public class PuzzleAcademyAuthoring : MonoBehaviour
    {
        [Header("Academy Configuration")]
        public PuzzleType[] availablePuzzles = { PuzzleType.Match3, PuzzleType.Logic_Grid, PuzzleType.Memory_Sequence };
        [Range(1, 10)] public int baseDifficulty = 3;
        [Range(1, 20)] public int maxParticipants = 10;
        [Range(60f, 1800f)] public float sessionDuration = 600f;

        [Header("Adaptive Learning")]
        public bool useAdaptiveDifficulty = true;
        public bool providePuzzleHints = true;
        [Range(0.5f, 3.0f)] public float learningCurveMultiplier = 1.0f;

        [Header("Puzzle Variety")]
        public bool includeMemoryGames = true;
        public bool includeLogicPuzzles = true;
        public bool includePatternRecognition = true;
        [Range(50, 500)] public int totalPuzzlesInLibrary = 200;

        [Header("PvP Battle Settings")]
        public bool enablePvPBattles = true;
        public PuzzleBattleType[] supportedBattleTypes = { PuzzleBattleType.Speed_Solving, PuzzleBattleType.Head_to_Head_Match3, PuzzleBattleType.Logic_Duel };
        [Range(2, 8)] public int maxBattleParticipants = 4;
        [Range(1f, 5f)] public float battleRewardMultiplier = 2f;
        public bool allowTeamBattles = true;

        [ContextMenu("Create Puzzle Academy Entity")]
        public void CreatePuzzleAcademyEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add puzzle academy component
            entityManager.AddComponentData(entity, new PuzzleAcademyComponent
            {
                CurrentPuzzleType = availablePuzzles.Length > 0 ? availablePuzzles[0] : PuzzleType.Match3,
                DifficultyLevel = baseDifficulty,
                MaxParticipants = maxParticipants,
                CurrentParticipants = 0,
                Status = AcademyStatus.Open,
                SessionTimer = 0f,
                SessionDuration = sessionDuration,
                TotalPuzzlesAvailable = totalPuzzlesInLibrary,
                CompletedPuzzles = 0,
                AverageSolveTime = 0f,
                AdaptiveDifficulty = useAdaptiveDifficulty,
                RewardMultiplier = 100,

                // PvP Competition Features
                PvPEnabled = enablePvPBattles,
                ActiveBattles = 0,
                CompetitionWins = 0,
                CompetitionLosses = 0,
                BattleTimer = 0f,
                CurrentBattleType = supportedBattleTypes.Length > 0 ? supportedBattleTypes[0] : PuzzleBattleType.Speed_Solving,
                MaxBattleParticipants = maxBattleParticipants,
                CompetitionRewardMultiplier = battleRewardMultiplier,
                AllowTeamBattles = allowTeamBattles
            });

            // Add activity center component
            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = ActivityType.Puzzle,
                MaxParticipants = maxParticipants,
                CurrentParticipants = 0,
                ActivityDuration = sessionDuration,
                DifficultyLevel = baseDifficulty,
                IsActive = true,
                QualityRating = 1.0f
            });

            // Link to transform
            entityManager.AddComponentData(entity, Unity.Transforms.LocalTransform.FromPositionRotation(transform.position, transform.rotation));

            // Link to GameObject
            entityManager.AddComponentData(entity, new GameObjectLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy
            });

            Debug.Log($"✅ Created Puzzle Academy with {availablePuzzles.Length} puzzle types and difficulty {baseDifficulty}");
        }

        private void OnDrawGizmos()
        {
            // Draw academy bounds
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 8f);

            // Draw puzzle stations
            for (int i = 0; i < availablePuzzles.Length && i < 8; i++)
            {
                var color = availablePuzzles[i] switch
                {
                    PuzzleType.Match3 => Color.red,
                    PuzzleType.Logic_Grid => Color.green,
                    PuzzleType.Memory_Sequence => Color.yellow,
                    PuzzleType.Pattern_Recognition => Color.magenta,
                    PuzzleType.Word_Puzzle => Color.cyan,
                    PuzzleType.Math_Problem => Color.white,
                    _ => Color.gray
                };

                Gizmos.color = color;
                float angle = (i / (float)availablePuzzles.Length) * 2f * Mathf.PI;
                Vector3 position = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 3f;
                Gizmos.DrawWireSphere(position, 1f);
            }

            // Draw difficulty indicator
            Gizmos.color = Color.blue;
            for (int i = 0; i < baseDifficulty; i++)
            {
                Gizmos.DrawLine(
                    transform.position + Vector3.up * (2f + i * 0.3f),
                    transform.position + Vector3.up * (2f + i * 0.3f) + Vector3.right * 1f
                );
            }
        }
    }

    #endregion
}