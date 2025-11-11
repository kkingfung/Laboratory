using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Activities.Strategy
{
    /// <summary>
    /// 🏰 STRATEGY COMMAND SYSTEM - Complete turn-based strategy mini-game
    /// FEATURES: Battle simulations, resource management, diplomatic missions, tactical leadership
    /// PERFORMANCE: Turn-based processing with efficient AI decision trees
    /// GENETICS: Intelligence, Social, Dominance affect strategic performance
    /// </summary>

    #region Strategy Components

    /// <summary>
    /// Strategy command center configuration and state
    /// </summary>
    public struct StrategyCommandComponent : IComponentData
    {
        public StrategyType CurrentStrategy;
        public BattleStatus Status;
        public int MaxCommanders;
        public int CurrentCommanders;
        public int CurrentTurn;
        public int MaxTurns;
        public float TurnTimer;
        public float TurnDuration;
        public Entity CurrentCommander;
        public int VictoryCondition;
        public bool IsMultiplayer;
        public StrategicObjective Objective;
        public int DifficultyLevel;
    }

    /// <summary>
    /// Individual commander state in strategy games
    /// </summary>
    public struct StrategyCommanderComponent : IComponentData
    {
        public Entity CommandCenter;
        public CommanderStatus Status;
        public CommanderRole Role;
        public int Faction;
        public int ResourcesAvailable;
        public int UnitsCommanded;
        public int TerritoryControlled;
        public int VictoryPoints;
        public int ActionsRemaining;
        public bool HasInitiative;
        public Entity CurrentTarget;
        public StrategicPosition Position;
        public float Morale;
        public int ExperienceLevel;
    }

    /// <summary>
    /// Strategic performance and leadership capabilities
    /// </summary>
    public struct StrategyPerformanceComponent : IComponentData
    {
        // Core strategic abilities (from genetics)
        public float TacticalIntelligence;
        public float LeadershipSkill;
        public float ResourceManagement;
        public float DiplomaticSkill;
        public float LongTermPlanning;
        public float AdaptabilityInBattle;
        public float UnitCoordination;

        // Strategic specializations
        public float OffensiveStrategy;
        public float DefensiveStrategy;
        public float EconomicStrategy;
        public float DiplomaticStrategy;
        public float GuerrillaStrategy;
        public float SiegeStrategy;

        // Equipment bonuses
        public float CommandEfficiency;
        public float IntelligenceGathering;
        public float CommunicationRange;

        // Experience bonuses
        public int BattlesWon;
        public int CampaignsCompleted;
        public float StrategicExperience;
        public bool HasAdvancedTactics;
    }

    /// <summary>
    /// Strategic unit management
    /// </summary>
    public struct StrategicUnitComponent : IComponentData
    {
        public Entity Commander;
        public UnitType Type;
        public int Strength;
        public int MaxStrength;
        public int Movement;
        public int MovementRemaining;
        public float3 Position;
        public UnitStatus Status;
        public bool HasMoved;
        public bool HasAttacked;
        public Entity TargetEnemy;
        public float Morale;
        public int Experience;
        public bool IsElite;
    }

    /// <summary>
    /// Resource management component
    /// </summary>
    public struct StrategicResourceComponent : IComponentData
    {
        public Entity Commander;
        public int Gold;
        public int Food;
        public int Materials;
        public int Energy;
        public int Population;
        public int Research;

        // Resource generation
        public int GoldPerTurn;
        public int FoodPerTurn;
        public int MaterialsPerTurn;
        public int EnergyPerTurn;

        // Resource limits
        public int MaxGold;
        public int MaxFood;
        public int MaxMaterials;
        public int MaxEnergy;
        public int MaxPopulation;

        // Economic efficiency
        public float EconomicEfficiency;
        public float TradeBonus;
    }

    /// <summary>
    /// Diplomatic relations and negotiations
    /// </summary>
    public struct DiplomaticComponent : IComponentData
    {
        public Entity Commander;
        public FixedList64Bytes<Entity> KnownFactions;
        public FixedList64Bytes<float> RelationshipValues; // -1 to 1 for each faction
        public FixedList32Bytes<Entity> AlliedFactions;
        public FixedList32Bytes<Entity> EnemyFactions;
        public int DiplomaticActions;
        public bool CanNegotiate;
        public float DiplomaticInfluence;
        public int TrustLevel;
        public bool HasTradeAgreements;
    }

    /// <summary>
    /// Battle formation and tactical positioning
    /// </summary>
    public struct BattleFormationComponent : IComponentData
    {
        public FormationType Formation;
        public float3 FormationCenter;
        public float FormationSpread;
        public int UnitsInFormation;
        public float FormationBonus;
        public bool IsDefensive;
        public float CohesionLevel;
        public Entity FormationLeader;
        public FixedList64Bytes<Entity> FormationUnits;
    }

    #endregion

    #region Strategy Enums

    public enum StrategyType : byte
    {
        Battle_Simulation,
        Resource_Management,
        Diplomatic_Mission,
        Territory_Control,
        Economic_Warfare,
        Siege_Warfare,
        Naval_Strategy,
        Campaign_Management
    }

    public enum BattleStatus : byte
    {
        Planning,
        Setup,
        In_Progress,
        Resolution,
        Victory,
        Defeat,
        Stalemate
    }

    public enum CommanderStatus : byte
    {
        Idle,
        Planning,
        Commanding,
        Negotiating,
        Retreating,
        Victorious,
        Defeated,
        Reinforcing
    }

    public enum CommanderRole : byte
    {
        General,
        Admiral,
        Diplomat,
        Economist,
        Spy_Master,
        Engineer,
        Field_Marshal,
        Strategist
    }

    public enum UnitType : byte
    {
        Infantry,
        Cavalry,
        Archers,
        Artillery,
        Engineers,
        Scouts,
        Heavy_Infantry,
        Flying_Units,
        Naval_Units,
        Siege_Engines
    }

    public enum UnitStatus : byte
    {
        Ready,
        Moving,
        Attacking,
        Defending,
        Retreating,
        Reinforcing,
        Routed,
        Eliminated
    }

    public enum StrategicObjective : byte
    {
        Defeat_Enemy,
        Control_Territory,
        Protect_Resources,
        Economic_Victory,
        Diplomatic_Victory,
        Survival,
        Time_Limit,
        Capture_Objective
    }

    public enum StrategicPosition : byte
    {
        Frontline,
        Support,
        Reserve,
        Flanking,
        Reconnaissance,
        Command_Post,
        Supply_Line,
        Fortified
    }

    public enum FormationType : byte
    {
        Line,
        Column,
        Wedge,
        Square,
        Crescent,
        Scattered,
        Defensive_Circle,
        Pincer
    }

    #endregion

    #region Strategy Systems

    /// <summary>
    /// Main strategy command management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class StrategyCommandManagementSystem : SystemBase
    {
        private EntityQuery commandQuery;
        private EntityQuery commanderQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            commandQuery = GetEntityQuery(ComponentType.ReadWrite<StrategyCommandComponent>());
            commanderQuery = GetEntityQuery(ComponentType.ReadWrite<StrategyCommanderComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update strategy command centers
            var commandUpdateJob = new CommandCenterUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb,
                random = Unity.Mathematics.Random.CreateFromIndex((uint)(System.DateTime.Now.Ticks))
            };
            Dependency = commandUpdateJob.ScheduleParallel(commandQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }



    [BurstCompile]
    public partial struct CommandCenterUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public Unity.Mathematics.Random random;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref StrategyCommandComponent command)
        {
            switch (command.Status)
            {
                case BattleStatus.Planning:
                    // Wait for commanders to join
                    if (command.CurrentCommanders >= 2)
                    {
                        command.Status = BattleStatus.Setup;
                        command.TurnTimer = 0f;
                    }
                    break;

                case BattleStatus.Setup:
                    UpdateSetupPhase(ref command, DeltaTime);
                    break;

                case BattleStatus.In_Progress:
                    UpdateBattleProgress(ref command, DeltaTime);
                    break;

                case BattleStatus.Resolution:
                    UpdateBattleResolution(ref command, DeltaTime);
                    break;

                case BattleStatus.Victory:
                case BattleStatus.Defeat:
                case BattleStatus.Stalemate:
                    UpdateBattleConclusion(ref command, DeltaTime);
                    break;
            }

            // Update turn management
            UpdateTurnManagement(ref command, DeltaTime);
        }

        private void UpdateSetupPhase(ref StrategyCommandComponent command, float deltaTime)
        {
            command.TurnTimer += deltaTime;

            // Allow setup time for positioning and planning
            if (command.TurnTimer >= 60f) // 1 minute setup
            {
                command.Status = BattleStatus.In_Progress;
                command.CurrentTurn = 1;
                command.TurnTimer = 0f;
            }
        }

        private void UpdateBattleProgress(ref StrategyCommandComponent command, float deltaTime)
        {
            // Battle progresses turn by turn
            if (command.CurrentTurn >= command.MaxTurns)
            {
                command.Status = BattleStatus.Resolution;
                command.TurnTimer = 0f;
            }

            // Check victory conditions
            CheckVictoryConditions(ref command);
        }

        private void UpdateBattleResolution(ref StrategyCommandComponent command, float deltaTime)
        {
            command.TurnTimer += deltaTime;

            // Calculate final results
            if (command.TurnTimer >= 30f) // 30 second resolution
            {
                DetermineBattleOutcome(ref command);
                command.TurnTimer = 0f;
            }
        }

        private void UpdateBattleConclusion(ref StrategyCommandComponent command, float deltaTime)
        {
            command.TurnTimer += deltaTime;

            // Victory/defeat celebration/analysis
            if (command.TurnTimer >= 60f) // 1 minute conclusion
            {
                ResetForNextBattle(ref command);
            }
        }

        private void UpdateTurnManagement(ref StrategyCommandComponent command, float deltaTime)
        {
            if (command.Status != BattleStatus.In_Progress)
                return;

            command.TurnTimer += deltaTime;

            // Turn-based system
            if (command.TurnTimer >= command.TurnDuration)
            {
                command.CurrentTurn++;
                command.TurnTimer = 0f;
                // Switch active commander (would be more complex in full implementation)
            }
        }

        private void CheckVictoryConditions(ref StrategyCommandComponent command)
        {
            // Simplified victory checking
            switch (command.Objective)
            {
                case StrategicObjective.Defeat_Enemy:
                    // Check if any commander has eliminated all enemy units
                    break;

                case StrategicObjective.Control_Territory:
                    // Check territorial control percentages
                    break;

                case StrategicObjective.Economic_Victory:
                    // Check resource accumulation
                    break;

                case StrategicObjective.Time_Limit:
                    if (command.CurrentTurn >= command.MaxTurns)
                    {
                        command.Status = BattleStatus.Resolution;
                    }
                    break;
            }
        }


        private void DetermineBattleOutcome(ref StrategyCommandComponent command)
        {
            // Simplified outcome determination
            if (command.CurrentTurn >= command.MaxTurns)
            {
                command.Status = BattleStatus.Stalemate;
            }
            else
            {
                // Determine winner based on victory points, remaining units, etc.
                command.Status = random.NextFloat() > 0.5f ? BattleStatus.Victory : BattleStatus.Defeat;
            }
        }


        private void ResetForNextBattle(ref StrategyCommandComponent command)
        {
            command.Status = BattleStatus.Planning;
            command.CurrentCommanders = 0;
            command.CurrentTurn = 0;
            command.TurnTimer = 0f;
            command.CurrentCommander = Entity.Null;
        }
    }

    /// <summary>
    /// Strategic decision making and AI system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(StrategyCommandManagementSystem))]
    public partial class StrategicDecisionSystem : SystemBase
    {
        private EntityQuery strategicQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            strategicQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<StrategyCommanderComponent>(),
                ComponentType.ReadOnly<StrategyPerformanceComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var strategicJob = new StrategicDecisionJob
            {
                DeltaTime = deltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime,
                random = Unity.Mathematics.Random.CreateFromIndex((uint)System.DateTime.Now.Ticks)
            };

            Dependency = strategicJob.ScheduleParallel(strategicQuery, Dependency);
        }
    }



    [BurstCompile]
    public partial struct StrategicDecisionJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;
        public Unity.Mathematics.Random random;

        public void Execute(ref StrategyCommanderComponent commander,
            in StrategyPerformanceComponent performance,
            RefRO<GeneticDataComponent> genetics)
        {
            if (commander.Status != CommanderStatus.Commanding)
                return;

            // Update commander actions based on role and performance
            ProcessCommanderRole(ref commander, performance, genetics.ValueRO);

            // Make strategic decisions
            if (commander.ActionsRemaining > 0)
            {
                MakeStrategicDecision(ref commander, performance, genetics.ValueRO);
            }

            // Update morale and experience
            UpdateCommanderState(ref commander, performance, DeltaTime);
        }


        private void ProcessCommanderRole(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, GeneticDataComponent genetics)
        {
            switch (commander.Role)
            {
                case CommanderRole.General:
                    ProcessGeneralRole(ref commander, performance, genetics);
                    break;

                case CommanderRole.Diplomat:
                    ProcessDiplomatRole(ref commander, performance, genetics);
                    break;

                case CommanderRole.Economist:
                    ProcessEconomistRole(ref commander, performance, genetics);
                    break;

                case CommanderRole.Strategist:
                    ProcessStrategistRole(ref commander, performance, genetics);
                    break;

                default:
                    ProcessDefaultRole(ref commander, performance, genetics);
                    break;
            }
        }


        private void ProcessGeneralRole(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Generals focus on unit management and tactical decisions
            float commandEfficiency = genetics.Dominance * performance.LeadershipSkill;
            commander.UnitsCommanded = (int)(commander.UnitsCommanded * (1f + commandEfficiency * 0.1f));

            // Boost unit morale
            commander.Morale = math.min(1f, commander.Morale + performance.LeadershipSkill * 0.01f);
        }


        private void ProcessDiplomatRole(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Diplomats focus on negotiations and alliances
            float diplomaticPower = genetics.Sociability * performance.DiplomaticSkill;

            // Simplified diplomatic actions
            if (random.NextFloat() < diplomaticPower * 0.1f)
            {
                commander.VictoryPoints += 5; // Diplomatic victory points
            }
        }


        private void ProcessEconomistRole(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Economists focus on resource generation and management
            float economicSkill = genetics.Intelligence * performance.ResourceManagement;
            commander.ResourcesAvailable = (int)(commander.ResourcesAvailable * (1f + economicSkill * 0.05f));
        }


        private void ProcessStrategistRole(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Strategists provide long-term planning bonuses
            float strategicThinking = genetics.Intelligence * performance.LongTermPlanning;

            // Boost overall effectiveness of other commanders (simplified)
            commander.VictoryPoints += (int)(strategicThinking * 2f);
        }


        private void ProcessDefaultRole(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Default balanced approach
            float generalEffectiveness = (genetics.Intelligence + genetics.Dominance + genetics.Sociability) / 3f;
            commander.VictoryPoints += (int)(generalEffectiveness);
        }


        private void MakeStrategicDecision(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // AI decision making based on genetics and performance
            float aggressionFactor = genetics.Aggression;
            float intelligenceFactor = genetics.Intelligence;
            float socialFactor = genetics.Sociability;

            // Choose action based on commander personality
            if (aggressionFactor > 0.7f && commander.ActionsRemaining > 0)
            {
                // Aggressive action - attack
                ExecuteOffensiveAction(ref commander, performance);
            }
            else if (socialFactor > 0.7f && commander.ActionsRemaining > 0)
            {
                // Social action - diplomacy
                ExecuteDiplomaticAction(ref commander, performance);
            }
            else if (intelligenceFactor > 0.7f && commander.ActionsRemaining > 0)
            {
                // Intelligent action - strategic planning
                ExecuteStrategicAction(ref commander, performance);
            }
            else if (commander.ActionsRemaining > 0)
            {
                // Default action - resource management
                ExecuteEconomicAction(ref commander, performance);
            }
        }


        private void ExecuteOffensiveAction(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance)
        {
            commander.ActionsRemaining--;

            // Calculate attack success
            float attackPower = performance.OffensiveStrategy * performance.UnitCoordination;
            if (attackPower > 0.5f)
            {
                commander.VictoryPoints += 10;
                commander.TerritoryControlled++;
            }
        }


        private void ExecuteDiplomaticAction(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance)
        {
            commander.ActionsRemaining--;

            // Calculate diplomatic success
            float diplomaticPower = performance.DiplomaticStrategy * performance.LeadershipSkill;
            if (diplomaticPower > 0.5f)
            {
                commander.VictoryPoints += 8;
                commander.ResourcesAvailable += 100;
            }
        }

        private void ExecuteStrategicAction(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance)
        {
            commander.ActionsRemaining--;

            // Calculate strategic planning success
            float strategicPower = performance.LongTermPlanning * performance.TacticalIntelligence;
            if (strategicPower > 0.5f)
            {
                commander.VictoryPoints += 12;
                commander.ActionsRemaining += 1; // Strategic planning gives extra actions
            }
        }

        private void ExecuteEconomicAction(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance)
        {
            commander.ActionsRemaining--;

            // Calculate economic success
            float economicPower = performance.ResourceManagement * performance.EconomicStrategy;
            if (economicPower > 0.5f)
            {
                commander.ResourcesAvailable += (int)(200f * economicPower);
                commander.VictoryPoints += 5;
            }
        }

        private void UpdateCommanderState(ref StrategyCommanderComponent commander, StrategyPerformanceComponent performance, float deltaTime)
        {
            // Update morale based on recent performance
            if (commander.VictoryPoints > 50)
            {
                commander.Morale = math.min(1f, commander.Morale + 0.1f * deltaTime);
            }
            else if (commander.VictoryPoints < 10)
            {
                commander.Morale = math.max(0.1f, commander.Morale - 0.05f * deltaTime);
            }

            // Gain experience over time
            commander.ExperienceLevel += (int)(deltaTime * 10f);

            // Refresh actions for next turn (simplified)
            if (commander.ActionsRemaining <= 0)
            {
                commander.ActionsRemaining = CalculateActionsPerTurn(performance);
            }
        }


        private int CalculateActionsPerTurn(StrategyPerformanceComponent performance)
        {
            float actionCount = performance.CommandEfficiency * 3f;
            return math.max(1, (int)actionCount);
        }
    }

    /// <summary>
    /// Resource management system for strategic gameplay
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(StrategicDecisionSystem))]
    public partial class StrategicResourceSystem : SystemBase
    {
        private EntityQuery resourceQuery;

        protected override void OnCreate()
        {
            resourceQuery = GetEntityQuery(ComponentType.ReadWrite<StrategicResourceComponent>());
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var resource in SystemAPI.Query<RefRW<StrategicResourceComponent>>())
            {
                UpdateResourceGeneration(ref resource.ValueRW, deltaTime);
            }
        }

        private void UpdateResourceGeneration(ref StrategicResourceComponent resource, float deltaTime)
        {
            // Generate resources over time (per turn in actual implementation)
            float timeMultiplier = deltaTime / 60f; // Assume 1 minute = 1 turn

            resource.Gold = math.min(resource.MaxGold,
                resource.Gold + (int)(resource.GoldPerTurn * timeMultiplier * resource.EconomicEfficiency));

            resource.Food = math.min(resource.MaxFood,
                resource.Food + (int)(resource.FoodPerTurn * timeMultiplier));

            resource.Materials = math.min(resource.MaxMaterials,
                resource.Materials + (int)(resource.MaterialsPerTurn * timeMultiplier));

            resource.Energy = math.min(resource.MaxEnergy,
                resource.Energy + (int)(resource.EnergyPerTurn * timeMultiplier));

            // Apply trade bonuses
            if (resource.TradeBonus > 1f)
            {
                resource.Gold = (int)(resource.Gold * resource.TradeBonus);
                resource.Gold = math.min(resource.MaxGold, resource.Gold);
            }
        }
    }

    #endregion

    #region Strategy Authoring

    /// <summary>
    /// MonoBehaviour authoring for strategy command centers
    /// </summary>
    public class StrategyCommandAuthoring : MonoBehaviour
    {
        [Header("Command Configuration")]
        public StrategyType strategyType = StrategyType.Battle_Simulation;
        [Range(2, 8)] public int maxCommanders = 4;
        [Range(5, 50)] public int maxTurns = 20;
        [Range(30f, 300f)] public float turnDuration = 120f;
        public StrategicObjective objective = StrategicObjective.Defeat_Enemy;

        [Header("Battle Settings")]
        [Range(1, 10)] public int difficultyLevel = 5;
        public bool allowDiplomacy = true;
        public bool enableResourceManagement = true;
        public bool useAdvancedTactics = false;

        [Header("Map Configuration")]
        public Transform[] strategicPoints;
        public Transform[] resourceNodes;
        public Transform commandPost;

        [ContextMenu("Create Strategy Command Entity")]
        public void CreateStrategyCommandEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add strategy command component
            entityManager.AddComponentData(entity, new StrategyCommandComponent
            {
                CurrentStrategy = strategyType,
                Status = BattleStatus.Planning,
                MaxCommanders = maxCommanders,
                CurrentCommanders = 0,
                CurrentTurn = 0,
                MaxTurns = maxTurns,
                TurnTimer = 0f,
                TurnDuration = turnDuration,
                CurrentCommander = Entity.Null,
                VictoryCondition = 1000, // Points needed to win
                IsMultiplayer = maxCommanders > 2,
                Objective = objective,
                DifficultyLevel = difficultyLevel
            });

            // Add activity center component
            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = ActivityType.Strategy,
                MaxParticipants = maxCommanders,
                CurrentParticipants = 0,
                ActivityDuration = turnDuration * maxTurns,
                DifficultyLevel = difficultyLevel,
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

            Debug.Log($"✅ Created {strategyType} command center with {maxCommanders} max commanders");
        }

        private void OnDrawGizmos()
        {
            // Draw command center
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 12f);

            // Draw strategic points
            if (strategicPoints != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var point in strategicPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 2f);
                        Gizmos.DrawLine(transform.position, point.position);
                    }
                }
            }

            // Draw resource nodes
            if (resourceNodes != null)
            {
                Gizmos.color = Color.green;
                foreach (var node in resourceNodes)
                {
                    if (node != null)
                    {
                        Gizmos.DrawCube(node.position, Vector3.one * 1.5f);
                    }
                }
            }

            // Draw command post
            if (commandPost != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(commandPost.position, 3f);
            }

            // Draw difficulty indicator
            Gizmos.color = Color.white;
            for (int i = 0; i < difficultyLevel; i++)
            {
                Gizmos.DrawLine(
                    transform.position + Vector3.up * (5f + i * 0.5f),
                    transform.position + Vector3.up * (5f + i * 0.5f) + Vector3.right * 2f
                );
            }
        }
    }

    #endregion
}