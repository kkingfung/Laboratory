using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Activities.Combat
{
    /// <summary>
    /// ⚔️ COMBAT ARENA SYSTEM - Complete tactical combat mini-game
    /// FEATURES: Tournament brackets, fighting styles, tactical elements, team battles
    /// PERFORMANCE: Real-time combat simulation with genetic combat preferences
    /// GENETICS: Aggression, Size, Intelligence affect combat performance and style
    /// </summary>

    #region Combat Components

    /// <summary>
    /// Combat arena configuration and tournament state
    /// </summary>
    public struct CombatArenaComponent : IComponentData
    {
        public ArenaType Type;
        public TournamentFormat Format;
        public int MaxFighters;
        public int CurrentFighters;
        public TournamentStatus Status;
        public int CurrentRound;
        public int TotalRounds;
        public float RoundTimer;
        public float RoundDuration;
        public Entity CurrentChampion;
        public int TournamentWins;
        public bool AllowTeamBattles;
        public float ArenaSize;
        public ArenaHazard ActiveHazards;
    }

    /// <summary>
    /// Individual fighter state in combat
    /// </summary>
    public struct CombatFighterComponent : IComponentData
    {
        public Entity Arena;
        public FighterStatus Status;
        public FightingStyle Style;
        public int CurrentHealth;
        public int MaxHealth;
        public int Attack;
        public int Defense;
        public int Speed;
        public float Stamina;
        public float MaxStamina;

        // Combat state
        public Entity CurrentTarget;
        public Entity Teammate;
        public float3 CombatPosition;
        public float AttackCooldown;
        public float BlockCooldown;
        public float DodgeCooldown;

        // Tournament progress
        public int Wins;
        public int Losses;
        public int TournamentRanking;
        public float TotalDamageDealt;
        public float TotalDamageTaken;
        public bool IsEliminated;
    }

    /// <summary>
    /// Combat performance and fighting capabilities
    /// </summary>
    public struct CombatPerformanceComponent : IComponentData
    {
        // Fighting abilities (from genetics)
        public float CombatPower;
        public float TacticalIntelligence;
        public float CombatSpeed;
        public float Endurance;
        public float Intimidation;
        public float Teamwork;

        // Style bonuses
        public float AggressiveBonus;
        public float DefensiveBonus;
        public float TechnicalBonus;
        public float BerserkerBonus;
        public float TacticalBonus;

        // Equipment bonuses
        public float WeaponProficiency;
        public float ArmorEffectiveness;
        public float SpecialAbilityPower;

        // Experience bonuses
        public int CombatsWon;
        public int TournamentsWon;
        public float ExperienceMultiplier;
        public bool HasSpecialMoves;
    }

    /// <summary>
    /// Combat action execution
    /// </summary>
    public struct CombatActionComponent : IComponentData
    {
        public CombatAction CurrentAction;
        public Entity ActionTarget;
        public float ActionProgress;
        public float ActionDuration;
        public float ActionPower;
        public bool ActionQueued;
        public CombatAction QueuedAction;
        public float ComboMeter;
        public int ComboCount;
    }

    /// <summary>
    /// Tournament bracket management
    /// </summary>
    public struct TournamentBracketComponent : IComponentData
    {
        public Entity Arena;
        public int BracketSize;
        public int CurrentMatches;
        public int CompletedMatches;
        public FixedList128Bytes<Entity> Participants;
        public FixedList64Bytes<Entity> Winners;
        public FixedList32Bytes<Entity> Finalists;
        public Entity Champion;
        public int PrizePool;
        public bool IsComplete;
    }

    #endregion

    #region Combat Enums

    public enum ArenaType : byte
    {
        Standard,
        Team_Battle,
        Survival,
        King_of_Hill,
        Capture_Flag,
        Last_Monster_Standing,
        Gauntlet,
        Championship
    }

    public enum TournamentFormat : byte
    {
        Single_Elimination,
        Double_Elimination,
        Round_Robin,
        Swiss_System,
        Ladder,
        Survival_Tournament
    }

    public enum TournamentStatus : byte
    {
        Registration,
        Bracket_Generation,
        In_Progress,
        Finals,
        Complete,
        Cancelled
    }

    public enum FighterStatus : byte
    {
        Waiting,
        Preparing,
        Fighting,
        Stunned,
        Defeated,
        Victorious,
        Eliminated
    }

    public enum FightingStyle : byte
    {
        Aggressive,
        Defensive,
        Technical,
        Berserker,
        Tactical,
        Balanced
    }

    public enum CombatAction : byte
    {
        None,
        Basic_Attack,
        Heavy_Attack,
        Quick_Attack,
        Block,
        Dodge,
        Charge,
        Special_Move,
        Combo_Attack,
        Retreat,
        Taunt
    }

    public enum ArenaHazard : uint
    {
        None = 0,
        Spikes = 1,
        Fire = 2,
        Electric = 4,
        Poison = 8,
        Moving_Platforms = 16,
        Falling_Objects = 32,
        Energy_Barriers = 64
    }

    #endregion

    #region Combat Systems

    /// <summary>
    /// Main combat arena management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class CombatArenaManagementSystem : SystemBase
    {
        private EntityQuery arenaQuery;
        private EntityQuery fighterQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            arenaQuery = GetEntityQuery(ComponentType.ReadWrite<CombatArenaComponent>());
            fighterQuery = GetEntityQuery(ComponentType.ReadWrite<CombatFighterComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update arena tournaments
            var arenaUpdateJob = new ArenaUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };
            Dependency = arenaUpdateJob.ScheduleParallel(arenaQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    public partial struct ArenaUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref CombatArenaComponent arena)
        {
            switch (arena.Status)
            {
                case TournamentStatus.Registration:
                    // Wait for enough fighters
                    if (arena.CurrentFighters >= GetMinimumFighters(arena.Format))
                    {
                        arena.Status = TournamentStatus.Bracket_Generation;
                    }
                    break;

                case TournamentStatus.Bracket_Generation:
                    // Generate tournament brackets
                    GenerateBrackets(ref arena);
                    arena.Status = TournamentStatus.In_Progress;
                    arena.RoundTimer = 0f;
                    break;

                case TournamentStatus.In_Progress:
                    UpdateTournamentRounds(ref arena, DeltaTime);
                    break;

                case TournamentStatus.Finals:
                    UpdateFinals(ref arena, DeltaTime);
                    break;

                case TournamentStatus.Complete:
                    // Award prizes and reset
                    if (arena.RoundTimer > 30f) // 30 second victory celebration
                    {
                        ResetTournament(ref arena);
                    }
                    else
                    {
                        arena.RoundTimer += DeltaTime;
                    }
                    break;
            }

            // Update arena hazards
            UpdateArenaHazards(ref arena, DeltaTime);
        }


        private int GetMinimumFighters(TournamentFormat format)
        {
            return format switch
            {
                TournamentFormat.Single_Elimination => 4,
                TournamentFormat.Double_Elimination => 4,
                TournamentFormat.Round_Robin => 3,
                TournamentFormat.Swiss_System => 6,
                TournamentFormat.Ladder => 8,
                TournamentFormat.Survival_Tournament => 10,
                _ => 4
            };
        }


        private void GenerateBrackets(ref CombatArenaComponent arena)
        {
            // Calculate number of rounds needed
            arena.TotalRounds = CalculateRounds(arena.CurrentFighters, arena.Format);
            arena.CurrentRound = 1;
        }


        private int CalculateRounds(int fighters, TournamentFormat format)
        {
            return format switch
            {
                TournamentFormat.Single_Elimination => (int)math.ceil(math.log2(fighters)),
                TournamentFormat.Double_Elimination => (int)math.ceil(math.log2(fighters)) + 1,
                TournamentFormat.Round_Robin => fighters - 1,
                TournamentFormat.Swiss_System => (int)math.ceil(math.log2(fighters)),
                _ => 3
            };
        }


        private void UpdateTournamentRounds(ref CombatArenaComponent arena, float deltaTime)
        {
            arena.RoundTimer += deltaTime;

            // Check if current round is complete
            if (arena.RoundTimer >= arena.RoundDuration)
            {
                arena.CurrentRound++;
                arena.RoundTimer = 0f;

                if (arena.CurrentRound > arena.TotalRounds)
                {
                    arena.Status = TournamentStatus.Finals;
                }
            }
        }


        private void UpdateFinals(ref CombatArenaComponent arena, float deltaTime)
        {
            arena.RoundTimer += deltaTime;

            // Finals last longer
            if (arena.RoundTimer >= arena.RoundDuration * 2f)
            {
                arena.Status = TournamentStatus.Complete;
                arena.RoundTimer = 0f;
                arena.TournamentWins++;
            }
        }


        private void ResetTournament(ref CombatArenaComponent arena)
        {
            arena.Status = TournamentStatus.Registration;
            arena.CurrentFighters = 0;
            arena.CurrentRound = 0;
            arena.TotalRounds = 0;
            arena.RoundTimer = 0f;
            arena.CurrentChampion = Entity.Null;
        }


        private void UpdateArenaHazards(ref CombatArenaComponent arena, float deltaTime)
        {
            // Cycle through different hazards during combat
            if (arena.Status == TournamentStatus.In_Progress)
            {
                if ((int)(arena.RoundTimer) % 30 == 0) // Change hazards every 30 seconds
                {
                    arena.ActiveHazards = (ArenaHazard)(((uint)arena.ActiveHazards << 1) % 128);
                    if (arena.ActiveHazards == ArenaHazard.None)
                        arena.ActiveHazards = ArenaHazard.Spikes;
                }
            }
        }
    }

    /// <summary>
    /// Combat mechanics and fighting system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CombatArenaManagementSystem))]
    public partial class CombatMechanicsSystem : SystemBase
    {
        private EntityQuery combatQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            combatQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<CombatFighterComponent>(),
                ComponentType.ReadWrite<CombatActionComponent>(),
                ComponentType.ReadOnly<CombatPerformanceComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var combatJob = new CombatMechanicsJob
            {
                DeltaTime = deltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime,
                random = Unity.Mathematics.Random.CreateFromIndex((uint)System.DateTime.Now.Ticks)
            };

            Dependency = combatJob.ScheduleParallel(combatQuery, Dependency);
        }
    }


    public partial struct CombatMechanicsJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;
        public Unity.Mathematics.Random random;

        public void Execute(ref CombatFighterComponent fighter,
            ref CombatActionComponent action,
            in CombatPerformanceComponent performance,
            RefRO<GeneticDataComponent> genetics)
        {
            if (fighter.Status != FighterStatus.Fighting)
                return;

            // Update cooldowns
            fighter.AttackCooldown = math.max(0f, fighter.AttackCooldown - DeltaTime);
            fighter.BlockCooldown = math.max(0f, fighter.BlockCooldown - DeltaTime);
            fighter.DodgeCooldown = math.max(0f, fighter.DodgeCooldown - DeltaTime);

            // Update stamina
            UpdateStamina(ref fighter, DeltaTime, performance);

            // Process current action
            ProcessCombatAction(ref fighter, ref action, performance, genetics.ValueRO);

            // AI decision making for next action
            if (action.CurrentAction == CombatAction.None && fighter.AttackCooldown <= 0f)
            {
                DecideNextAction(ref action, fighter, performance, genetics.ValueRO);
            }

            // Update combat position (simplified)
            UpdateCombatPosition(ref fighter, DeltaTime);
        }


        private void UpdateStamina(ref CombatFighterComponent fighter, float deltaTime, CombatPerformanceComponent performance)
        {
            float staminaRecovery = performance.Endurance * 2f * deltaTime;
            fighter.Stamina = math.min(fighter.MaxStamina, fighter.Stamina + staminaRecovery);

            // Stamina affects combat performance
            if (fighter.Stamina < fighter.MaxStamina * 0.3f)
            {
                // Low stamina penalties
                fighter.Speed = (int)(fighter.Speed * 0.7f);
                fighter.Attack = (int)(fighter.Attack * 0.8f);
            }
        }


        private void ProcessCombatAction(ref CombatFighterComponent fighter, ref CombatActionComponent action, CombatPerformanceComponent performance, GeneticDataComponent genetics)
        {
            if (action.CurrentAction == CombatAction.None)
                return;

            action.ActionProgress += DeltaTime;

            if (action.ActionProgress >= action.ActionDuration)
            {
                // Execute the action
                ExecuteCombatAction(ref fighter, ref action, performance, genetics);

                // Complete the action
                action.CurrentAction = CombatAction.None;
                action.ActionProgress = 0f;
                action.ActionTarget = Entity.Null;

                // Process queued action
                if (action.ActionQueued)
                {
                    action.CurrentAction = action.QueuedAction;
                    action.ActionQueued = false;
                    action.ActionDuration = GetActionDuration(action.CurrentAction, performance);
                }
            }
        }


        private void ExecuteCombatAction(ref CombatFighterComponent fighter, ref CombatActionComponent action, CombatPerformanceComponent performance, GeneticDataComponent genetics)
        {
            float staminaCost = GetStaminaCost(action.CurrentAction);
            fighter.Stamina = math.max(0f, fighter.Stamina - staminaCost);

            switch (action.CurrentAction)
            {
                case CombatAction.Basic_Attack:
                    ExecuteAttack(ref fighter, ref action, performance, 1.0f);
                    fighter.AttackCooldown = 1f;
                    break;

                case CombatAction.Heavy_Attack:
                    ExecuteAttack(ref fighter, ref action, performance, 2.0f);
                    fighter.AttackCooldown = 2f;
                    break;

                case CombatAction.Quick_Attack:
                    ExecuteAttack(ref fighter, ref action, performance, 0.7f);
                    fighter.AttackCooldown = 0.5f;
                    break;

                case CombatAction.Block:
                    fighter.Defense = (int)(fighter.Defense * 1.5f); // Temporary defense boost
                    fighter.BlockCooldown = 2f;
                    break;

                case CombatAction.Dodge:
                    // Dodge gives temporary invulnerability (handled elsewhere)
                    fighter.DodgeCooldown = 3f;
                    break;

                case CombatAction.Special_Move:
                    if (performance.HasSpecialMoves)
                    {
                        ExecuteSpecialMove(ref fighter, ref action, performance, genetics);
                    }
                    break;

                case CombatAction.Combo_Attack:
                    ExecuteComboAttack(ref fighter, ref action, performance);
                    break;
            }
        }


        private void ExecuteAttack(ref CombatFighterComponent fighter, ref CombatActionComponent action, CombatPerformanceComponent performance, float powerMultiplier)
        {
            float damage = fighter.Attack * powerMultiplier * performance.CombatPower;
            fighter.TotalDamageDealt += damage;

            // In a full system, this would apply damage to the target
            // For now, just track the damage dealt
        }


        private void ExecuteSpecialMove(ref CombatFighterComponent fighter, ref CombatActionComponent action, CombatPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Special moves based on fighting style and genetics
            float specialPower = performance.SpecialAbilityPower * genetics.Intelligence;
            float damage = fighter.Attack * specialPower * 3f; // Special moves are powerful

            fighter.TotalDamageDealt += damage;
            action.ComboMeter = 0f; // Reset combo meter after special
        }


        private void ExecuteComboAttack(ref CombatFighterComponent fighter, ref CombatActionComponent action, CombatPerformanceComponent performance)
        {
            action.ComboCount++;
            float comboMultiplier = 1f + (action.ComboCount * 0.2f);
            float damage = fighter.Attack * comboMultiplier * performance.CombatPower;

            fighter.TotalDamageDealt += damage;
            action.ComboMeter += 0.3f;

            if (action.ComboMeter >= 1f)
            {
                // Combo finisher
                damage *= 2f;
                action.ComboCount = 0;
                action.ComboMeter = 0f;
            }
        }


        private void DecideNextAction(ref CombatActionComponent action, CombatFighterComponent fighter, CombatPerformanceComponent performance, GeneticDataComponent genetics)
        {
            // AI decision making based on genetics and fighting style
            float aggressionFactor = genetics.Aggression;
            float intelligenceFactor = genetics.Intelligence;
            float staminaRatio = fighter.Stamina / fighter.MaxStamina;

            // Choose action based on fighting style and situation
            if (staminaRatio < 0.3f)
            {
                // Low stamina - be defensive
                action.CurrentAction = random.NextFloat() < 0.7f ? CombatAction.Block : CombatAction.Dodge;
            }
            else if (aggressionFactor > 0.7f)
            {
                // Aggressive fighters prefer attacks
                action.CurrentAction = random.NextFloat() < 0.6f ? CombatAction.Heavy_Attack : CombatAction.Basic_Attack;
            }
            else if (intelligenceFactor > 0.7f)
            {
                // Intelligent fighters use tactical moves
                action.CurrentAction = random.NextFloat() < 0.4f ? CombatAction.Special_Move : CombatAction.Combo_Attack;
            }
            else
            {
                // Balanced approach
                var actions = new CombatAction[] { CombatAction.Basic_Attack, CombatAction.Quick_Attack, CombatAction.Block };
                action.CurrentAction = actions[random.NextInt(0, actions.Length)];
            }

            action.ActionDuration = GetActionDuration(action.CurrentAction, performance);
            action.ActionProgress = 0f;
        }


        private float GetActionDuration(CombatAction combatAction, CombatPerformanceComponent performance)
        {
            float speedBonus = performance.CombatSpeed;

            return combatAction switch
            {
                CombatAction.Quick_Attack => 0.3f / speedBonus,
                CombatAction.Basic_Attack => 0.5f / speedBonus,
                CombatAction.Heavy_Attack => 1.0f / speedBonus,
                CombatAction.Block => 0.2f,
                CombatAction.Dodge => 0.4f / speedBonus,
                CombatAction.Special_Move => 1.5f / speedBonus,
                CombatAction.Combo_Attack => 0.7f / speedBonus,
                _ => 0.5f
            };
        }


        private float GetStaminaCost(CombatAction combatAction)
        {
            return combatAction switch
            {
                CombatAction.Quick_Attack => 5f,
                CombatAction.Basic_Attack => 10f,
                CombatAction.Heavy_Attack => 25f,
                CombatAction.Block => 5f,
                CombatAction.Dodge => 15f,
                CombatAction.Special_Move => 40f,
                CombatAction.Combo_Attack => 20f,
                _ => 10f
            };
        }


        private void UpdateCombatPosition(ref CombatFighterComponent fighter, float deltaTime)
        {
            // Simplified combat movement
            if (fighter.CurrentTarget != Entity.Null)
            {
                // Move toward target (simplified)
                float3 direction = math.normalize(new float3(1f, 0f, 0f)); // Placeholder
                fighter.CombatPosition += direction * fighter.Speed * 0.1f * deltaTime;
            }
        }
    }

    /// <summary>
    /// Tournament bracket and match management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CombatMechanicsSystem))]
    public partial class TournamentBracketSystem : SystemBase
    {
        private EntityQuery bracketQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            bracketQuery = GetEntityQuery(ComponentType.ReadWrite<TournamentBracketComponent>());
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            foreach (var bracket in SystemAPI.Query<RefRW<TournamentBracketComponent>>())
            {
                UpdateTournamentBracket(ref bracket.ValueRW);
            }
        }

        private void UpdateTournamentBracket(ref TournamentBracketComponent bracket)
        {
            if (bracket.IsComplete)
                return;

            // Check for completed matches and advance winners
            if (bracket.CompletedMatches >= bracket.CurrentMatches)
            {
                AdvanceTournament(ref bracket);
            }
        }

        private void AdvanceTournament(ref TournamentBracketComponent bracket)
        {
            // Move winners to next round
            // In a full implementation, this would manage the actual bracket progression

            if (bracket.Winners.Length <= 1)
            {
                // Tournament complete
                bracket.IsComplete = true;
                if (bracket.Winners.Length == 1)
                {
                    bracket.Champion = bracket.Winners[0];
                }
            }
        }
    }

    #endregion

    #region Combat Authoring

    /// <summary>
    /// MonoBehaviour authoring for combat arenas
    /// </summary>
    public class CombatArenaAuthoring : MonoBehaviour
    {
        [Header("Arena Configuration")]
        public ArenaType arenaType = ArenaType.Standard;
        public TournamentFormat tournamentFormat = TournamentFormat.Single_Elimination;
        [Range(4, 64)] public int maxFighters = 16;
        [Range(30f, 600f)] public float roundDuration = 180f;
        [Range(5f, 50f)] public float arenaSize = 20f;

        [Header("Arena Features")]
        public bool allowTeamBattles = false;
        public ArenaHazard availableHazards = ArenaHazard.None;
        public Transform[] spawnPoints;
        public Transform[] spectatorAreas;

        [Header("Tournament Settings")]
        [Range(100, 10000)] public int prizePool = 1000;
        public bool autoStartTournaments = true;

        [ContextMenu("Create Combat Arena Entity")]
        public void CreateCombatArenaEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add combat arena component
            entityManager.AddComponentData(entity, new CombatArenaComponent
            {
                Type = arenaType,
                Format = tournamentFormat,
                MaxFighters = maxFighters,
                CurrentFighters = 0,
                Status = TournamentStatus.Registration,
                CurrentRound = 0,
                TotalRounds = 0,
                RoundTimer = 0f,
                RoundDuration = roundDuration,
                CurrentChampion = Entity.Null,
                TournamentWins = 0,
                AllowTeamBattles = allowTeamBattles,
                ArenaSize = arenaSize,
                ActiveHazards = ArenaHazard.None
            });

            // Add activity center component
            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = ActivityType.Combat,
                MaxParticipants = maxFighters,
                CurrentParticipants = 0,
                ActivityDuration = roundDuration,
                DifficultyLevel = (float)arenaType,
                IsActive = true,
                QualityRating = 1.0f
            });

            // Add tournament bracket component
            entityManager.AddComponentData(entity, new TournamentBracketComponent
            {
                Arena = entity,
                BracketSize = maxFighters,
                CurrentMatches = 0,
                CompletedMatches = 0,
                Champion = Entity.Null,
                PrizePool = prizePool,
                IsComplete = false
            });

            // Link to transform
            entityManager.AddComponentData(entity, LocalTransform.FromPositionRotation(transform.position, transform.rotation));

            // Link to GameObject
            entityManager.AddComponentData(entity, new GameObjectLinkComponent
            {
                InstanceID = gameObject.GetInstanceID(),
                IsActive = gameObject.activeInHierarchy
            });

            Debug.Log($"✅ Created {arenaType} combat arena with {tournamentFormat} format");
        }

        private void OnDrawGizmos()
        {
            // Draw arena bounds
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * arenaSize);

            // Draw combat zones
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, arenaSize * 0.8f);

            // Draw spawn points
            if (spawnPoints != null)
            {
                Gizmos.color = Color.blue;
                foreach (var point in spawnPoints)
                {
                    if (point != null)
                    {
                        Gizmos.DrawWireSphere(point.position, 1f);
                        Gizmos.DrawLine(transform.position, point.position);
                    }
                }
            }

            // Draw spectator areas
            if (spectatorAreas != null)
            {
                Gizmos.color = Color.green;
                foreach (var area in spectatorAreas)
                {
                    if (area != null)
                    {
                        Gizmos.DrawWireCube(area.position, Vector3.one * 2f);
                    }
                }
            }

            // Draw hazard indicators
            if (availableHazards != ArenaHazard.None)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one);
            }
        }
    }

    #endregion
}