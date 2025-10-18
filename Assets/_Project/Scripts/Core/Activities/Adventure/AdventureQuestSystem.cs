using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities;

namespace Laboratory.Core.Activities.Adventure
{
    /// <summary>
    /// 🗺️ ADVENTURE QUEST SYSTEM - Complete adventure and exploration mini-game
    /// FEATURES: Procedural quests, dungeon exploration, survival challenges, treasure hunting
    /// PERFORMANCE: Dynamic quest generation with efficient exploration mechanics
    /// GENETICS: Balanced stats + adventure gear = exploration success, cross-activity benefits
    /// </summary>

    #region Adventure Components

    /// <summary>
    /// Adventure guild configuration and state
    /// </summary>
    public struct AdventureGuildComponent : IComponentData
    {
        public GuildType Type;
        public GuildStatus Status;
        public int MaxAdventurers;
        public int CurrentAdventurers;
        public int AvailableQuests;
        public int CompletedQuests;
        public int ActiveExpeditions;
        public float GuildRenown;
        public int GuildLevel;
        public QuestDifficulty MaxDifficulty;
        public float QuestGenerationTimer;
        public float QuestGenerationRate;
        public int TreasureVaults;
        public float ExplorationRadius;

        // PvP Competition Features
        public bool PvPEnabled;
        public int ActiveCompetitions;
        public int CompetitionWins;
        public int CompetitionLosses;
        public float CompetitionTimer;
        public CompetitionType CurrentCompetition;
        public int MaxCompetitors;
        public float CompetitionRewardMultiplier;
    }

    /// <summary>
    /// Individual adventurer state
    /// </summary>
    public struct AdventurerComponent : IComponentData
    {
        public Entity Guild;
        public AdventurerStatus Status;
        public AdventurerClass Class;
        public Entity CurrentQuest;
        public float3 ExplorationPosition;
        public float ExplorationProgress;
        public int QuestsCompleted;
        public int TreasuresFound;
        public int DangersSurvived;
        public float SurvivalRating;
        public int ExperienceGained;
        public bool IsInDanger;
        public float DangerTimer;
        public AdventurerMood Mood;
        public float Stamina;
        public float MaxStamina;

        // PvP Competition Data
        public bool IsInCompetition;
        public Entity CompetitionOpponent;
        public CompetitionType CompetitionMode;
        public float CompetitionScore;
        public int CompetitionRank;
        public bool HasCompetitionAdvantage;
        public float CompetitionTimer;
        public int PvPWins;
        public int PvPLosses;
        public float PvPRating;
    }

    /// <summary>
    /// Adventure performance and exploration capabilities
    /// </summary>
    public struct AdventurePerformanceComponent : IComponentData
    {
        // Core adventure abilities (from genetics)
        public float ExplorationSkill;
        public float SurvivalInstinct;
        public float TreasureHunting;
        public float DangerSense;
        public float Adaptability;
        public float NavigationSkill;
        public float ResourceGathering;

        // Terrain specializations
        public float ForestExploration;
        public float MountainClimbing;
        public float CaveNavigation;
        public float WaterTraversal;
        public float DesertSurvival;
        public float UrbanExploration;

        // Adventure skills
        public float TrapDetection;
        public float PuzzleSolving;
        public float MonsterAvoidance;
        public float WeatherResistance;
        public float StaminaManagement;

        // Equipment bonuses
        public float ExplorationSpeedBonus;
        public float SurvivalGearBonus;
        public float NavigationAidBonus;

        // Experience bonuses
        public int AdventuresCompleted;
        public int DungeonsExplored;
        public float LegendaryDiscoveries;
        public bool HasAdvancedSurvival;
    }

    /// <summary>
    /// Procedural quest data
    /// </summary>
    public struct QuestComponent : IComponentData
    {
        public QuestType Type;
        public QuestDifficulty Difficulty;
        public QuestStatus Status;
        public Entity QuestGiver;
        public Entity Adventurer;
        public float3 QuestLocation;
        public float QuestRadius;
        public int ObjectivesTotal;
        public int ObjectivesCompleted;
        public float TimeLimit;
        public float TimeRemaining;
        public int RewardExperience;
        public int RewardTreasure;
        public bool IsTimeLimited;
        public bool HasDangers;
        public float DangerLevel;
        public uint QuestSeed;
    }

    /// <summary>
    /// Dungeon exploration state
    /// </summary>
    public struct DungeonComponent : IComponentData
    {
        public DungeonType Type;
        public int Depth;
        public int MaxDepth;
        public float ExplorationProgress;
        public int RoomsExplored;
        public int TotalRooms;
        public int TreasuresFound;
        public int TrapsEncountered;
        public int MonstersSeen;
        public float DifficultyProgression;
        public bool HasBoss;
        public bool BossDefeated;
        public Entity CurrentExplorer;
        public float DungeonSeed;
        public DungeonStatus Status;
    }

    /// <summary>
    /// Treasure and discovery system
    /// </summary>
    public struct TreasureComponent : IComponentData
    {
        public TreasureType Type;
        public TreasureRarity Rarity;
        public float3 Location;
        public bool IsHidden;
        public bool IsDiscovered;
        public bool IsCollected;
        public Entity Discoverer;
        public float DiscoveryDifficulty;
        public int Value;
        public bool HasSpecialProperties;
        public float MagicalPower;
        public TreasureEffect Effect;
    }

    /// <summary>
    /// Survival challenge mechanics
    /// </summary>
    public struct SurvivalChallengeComponent : IComponentData
    {
        public SurvivalType Type;
        public float Severity;
        public float Duration;
        public float TimeRemaining;
        public Entity Survivor;
        public bool IsActive;
        public float DamagePerSecond;
        public float ResourceDrain;
        public SurvivalRequirement Requirements;
        public bool IsSurvived;
        public float SurvivalScore;
    }

    /// <summary>
    /// PvP competition mechanics for adventure activities
    /// </summary>
    public struct AdventureCompetitionComponent : IComponentData
    {
        public CompetitionType Type;
        public CompetitionStatus Status;
        public Entity Competitor1;
        public Entity Competitor2;
        public float Competition1Score;
        public float Competition2Score;
        public float TimeLimit;
        public float TimeRemaining;
        public Entity Winner;
        public CompetitionRules Rules;
        public float3 CompetitionArea;
        public float CompetitionRadius;
        public int SpectatorCount;
        public float PrizePool;
        public bool IsTeamCompetition;
        public int TeamSize;
        public uint CompetitionSeed;
    }

    /// <summary>
    /// Territory control for competitive exploration
    /// </summary>
    public struct TerritoryControlComponent : IComponentData
    {
        public Entity Controller;
        public Entity Challenger;
        public float3 TerritoryCenter;
        public float TerritoryRadius;
        public float ControlProgress;
        public float ChallengeProgress;
        public int ResourceNodes;
        public int NodesControlled;
        public bool IsContested;
        public float ContestedTimer;
        public TerritoryStatus Status;
        public float ControlValue;
    }

    #endregion

    #region Adventure Enums

    public enum GuildType : byte
    {
        Explorers_Guild,
        Treasure_Hunters,
        Dungeon_Delvers,
        Survival_Specialists,
        Monster_Researchers,
        Cartographers,
        Relic_Seekers,
        Wilderness_Rangers
    }

    public enum GuildStatus : byte
    {
        Open,
        Quest_Assignment,
        Expedition_Active,
        Debriefing,
        Resupply,
        Closed,
        Emergency
    }

    public enum AdventurerStatus : byte
    {
        Idle,
        Preparing,
        Traveling,
        Exploring,
        In_Danger,
        Fighting,
        Resting,
        Returning,
        Completed,
        Lost
    }

    public enum AdventurerClass : byte
    {
        Scout,
        Treasure_Hunter,
        Survivalist,
        Navigator,
        Monster_Expert,
        Trap_Specialist,
        Archaeologist,
        All_Rounder
    }

    public enum AdventurerMood : byte
    {
        Excited,
        Confident,
        Cautious,
        Determined,
        Nervous,
        Exhausted,
        Triumphant,
        Discouraged
    }

    public enum QuestType : byte
    {
        Exploration,
        Treasure_Hunt,
        Dungeon_Delve,
        Survival_Challenge,
        Monster_Study,
        Artifact_Recovery,
        Mapping_Mission,
        Rescue_Operation,
        Resource_Gathering,
        Mystery_Investigation
    }

    public enum QuestDifficulty : byte
    {
        Novice = 1,
        Apprentice = 2,
        Journeyman = 3,
        Expert = 4,
        Master = 5,
        Legendary = 6
    }

    public enum QuestStatus : byte
    {
        Available,
        Assigned,
        In_Progress,
        Completed,
        Failed,
        Abandoned,
        Expired
    }

    public enum DungeonType : byte
    {
        Ancient_Ruins,
        Natural_Caves,
        Underground_Temple,
        Abandoned_Mine,
        Magical_Labyrinth,
        Monster_Lair,
        Forgotten_Vault,
        Crystal_Caverns
    }

    public enum DungeonStatus : byte
    {
        Unexplored,
        Partially_Explored,
        Fully_Mapped,
        Boss_Defeated,
        Collapsed,
        Sealed
    }

    public enum TreasureType : byte
    {
        Gold_Coins,
        Precious_Gems,
        Ancient_Artifact,
        Magical_Item,
        Rare_Material,
        Knowledge_Scroll,
        Equipment_Upgrade,
        Mysterious_Relic
    }

    public enum TreasureRarity : byte
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary,
        Mythical
    }

    public enum TreasureEffect : byte
    {
        None,
        Stat_Boost,
        Skill_Enhancement,
        Special_Ability,
        Permanent_Bonus,
        Unique_Power
    }

    public enum SurvivalType : byte
    {
        Extreme_Weather,
        Food_Shortage,
        Water_Scarcity,
        Monster_Territory,
        Toxic_Environment,
        Natural_Disaster,
        Getting_Lost,
        Equipment_Failure
    }

    public enum SurvivalRequirement : byte
    {
        High_Stamina,
        Good_Navigation,
        Survival_Gear,
        Team_Cooperation,
        Quick_Thinking,
        Patience
    }

    // PvP Competition Enums
    public enum CompetitionType : byte
    {
        Dungeon_Race,
        Treasure_Hunt_Duel,
        Territory_Control,
        Survival_Challenge,
        Speed_Exploration,
        Resource_Competition,
        Team_Expedition,
        Endurance_Trial
    }

    public enum CompetitionStatus : byte
    {
        Preparing,
        Active,
        Completed,
        Tie,
        Cancelled,
        Disputed
    }

    public enum CompetitionRules : byte
    {
        Standard,
        No_Equipment,
        Limited_Stamina,
        Hardcore_Mode,
        Team_Only,
        Solo_Only,
        Time_Attack,
        Sudden_Death
    }

    public enum TerritoryStatus : byte
    {
        Neutral,
        Controlled,
        Contested,
        Captured,
        Abandoned,
        Locked
    }

    #endregion

    #region Adventure Systems

    /// <summary>
    /// Main adventure guild management system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class AdventureGuildManagementSystem : SystemBase
    {
        private EntityQuery guildQuery;
        private EntityQuery adventurerQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            guildQuery = GetEntityQuery(ComponentType.ReadWrite<AdventureGuildComponent>());
            adventurerQuery = GetEntityQuery(ComponentType.ReadWrite<AdventurerComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update adventure guilds
            var guildUpdateJob = new GuildUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };
            Dependency = guildUpdateJob.ScheduleParallel(guildQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    public partial struct GuildUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref AdventureGuildComponent guild)
        {
            switch (guild.Status)
            {
                case GuildStatus.Open:
                    // Generate new quests periodically
                    guild.QuestGenerationTimer += DeltaTime;
                    if (guild.QuestGenerationTimer >= guild.QuestGenerationRate)
                    {
                        GenerateNewQuest(ref guild);
                        guild.QuestGenerationTimer = 0f;
                    }

                    // Accept adventurers
                    if (guild.CurrentAdventurers > 0)
                    {
                        guild.Status = GuildStatus.Quest_Assignment;
                    }
                    break;

                case GuildStatus.Quest_Assignment:
                    // Assign quests to adventurers
                    AssignQuestsToAdventurers(ref guild);
                    guild.Status = GuildStatus.Expedition_Active;
                    break;

                case GuildStatus.Expedition_Active:
                    // Monitor active expeditions
                    UpdateActiveExpeditions(ref guild, DeltaTime);
                    break;

                case GuildStatus.Debriefing:
                    // Process completed quests
                    ProcessQuestResults(ref guild);
                    guild.Status = GuildStatus.Open;
                    break;
            }

            // Update guild level and renown
            UpdateGuildProgression(ref guild);
        }


        private void GenerateNewQuest(ref AdventureGuildComponent guild)
        {
            if (guild.AvailableQuests < 10) // Maximum quest queue
            {
                guild.AvailableQuests++;
            }
        }


        private void AssignQuestsToAdventurers(ref AdventureGuildComponent guild)
        {
            // Simplified quest assignment
            int questsToAssign = math.min(guild.AvailableQuests, guild.CurrentAdventurers);
            guild.AvailableQuests -= questsToAssign;
            guild.ActiveExpeditions += questsToAssign;
        }


        private void UpdateActiveExpeditions(ref AdventureGuildComponent guild, float deltaTime)
        {
            // Check if expeditions are complete (simplified)
            if (guild.ActiveExpeditions > 0)
            {
                // Random completion chance
                if (math.random().NextFloat() < 0.01f * deltaTime) // 1% chance per second
                {
                    guild.ActiveExpeditions--;
                    guild.CompletedQuests++;

                    if (guild.ActiveExpeditions == 0)
                    {
                        guild.Status = GuildStatus.Debriefing;
                    }
                }
            }
        }


        private void ProcessQuestResults(ref AdventureGuildComponent guild)
        {
            // Award guild renown and experience
            guild.GuildRenown += guild.CompletedQuests * 10f;

            // Reset for next cycle
            guild.CurrentAdventurers = 0;
        }


        private void UpdateGuildProgression(ref AdventureGuildComponent guild)
        {
            // Calculate guild level from renown
            int newLevel = (int)(guild.GuildRenown / 1000f) + 1;

            if (newLevel > guild.GuildLevel)
            {
                guild.GuildLevel = newLevel;
                guild.MaxDifficulty = (QuestDifficulty)math.min((int)QuestDifficulty.Legendary, newLevel);
                guild.ExplorationRadius += 50f; // Unlock new areas
            }
        }
    }

    /// <summary>
    /// Adventure exploration and quest progression system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AdventureGuildManagementSystem))]
    public partial class AdventureExplorationSystem : SystemBase
    {
        private EntityQuery explorationQuery;

        protected override void OnCreate()
        {
            explorationQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<AdventurerComponent>(),
                ComponentType.ReadOnly<AdventurePerformanceComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var explorationJob = new AdventureExplorationJob
            {
                DeltaTime = deltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime
            };

            Dependency = explorationJob.ScheduleParallel(explorationQuery, Dependency);
        }
    }


    public partial struct AdventureExplorationJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;

        public void Execute(ref AdventurerComponent adventurer,
            in AdventurePerformanceComponent performance,
            in GeneticDataComponent genetics)
        {
            switch (adventurer.Status)
            {
                case AdventurerStatus.Exploring:
                    ProcessExploration(ref adventurer, performance, genetics);
                    break;

                case AdventurerStatus.In_Danger:
                    ProcessDangerSituation(ref adventurer, performance, genetics);
                    break;

                case AdventurerStatus.Resting:
                    ProcessResting(ref adventurer, DeltaTime);
                    break;

                case AdventurerStatus.Returning:
                    ProcessReturning(ref adventurer, performance);
                    break;
            }

            // Update stamina and mood
            UpdateAdventurerCondition(ref adventurer, performance, genetics, DeltaTime);
        }


        private void ProcessExploration(ref AdventurerComponent adventurer, AdventurePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Calculate exploration progress based on abilities
            float explorationRate = CalculateExplorationRate(performance, genetics);
            adventurer.ExplorationProgress += explorationRate * DeltaTime;

            // Check for discoveries
            if (CheckForDiscovery(performance, genetics))
            {
                adventurer.TreasuresFound++;
                adventurer.ExperienceGained += 50;
            }

            // Check for dangers
            if (CheckForDanger(performance, genetics))
            {
                adventurer.Status = AdventurerStatus.In_Danger;
                adventurer.IsInDanger = true;
                adventurer.DangerTimer = 0f;
            }

            // Check for quest completion
            if (adventurer.ExplorationProgress >= 1f)
            {
                CompleteQuest(ref adventurer);
            }
        }


        private float CalculateExplorationRate(AdventurePerformanceComponent performance, GeneticDataComponent genetics)
        {
            // Base exploration rate from genetics and skills
            float baseRate = (genetics.Speed + genetics.Stamina + genetics.Curiosity) / 3f;
            float skillBonus = performance.ExplorationSkill + performance.NavigationSkill;
            float equipmentBonus = performance.ExplorationSpeedBonus;

            return (baseRate + skillBonus + equipmentBonus) * 0.1f; // Normalized rate
        }


        private bool CheckForDiscovery(AdventurePerformanceComponent performance, GeneticDataComponent genetics)
        {
            float discoveryChance = genetics.Curiosity * performance.TreasureHunting * 0.01f; // 1% max per frame
            return math.random().NextFloat() < discoveryChance;
        }


        private bool CheckForDanger(AdventurePerformanceComponent performance, GeneticDataComponent genetics)
        {
            float dangerChance = 0.005f; // Base 0.5% chance per frame
            float dangerReduction = performance.DangerSense * genetics.Caution;
            float actualDangerChance = math.max(0.001f, dangerChance - dangerReduction * 0.001f);

            return math.random().NextFloat() < actualDangerChance;
        }


        private void ProcessDangerSituation(ref AdventurerComponent adventurer, AdventurePerformanceComponent performance, GeneticDataComponent genetics)
        {
            adventurer.DangerTimer += DeltaTime;

            // Calculate survival chance
            float survivalSkill = genetics.Adaptability * performance.SurvivalInstinct;
            float survivalChance = survivalSkill * 0.1f; // 10% max chance per second

            if (math.random().NextFloat() < survivalChance)
            {
                // Successfully escaped danger
                adventurer.Status = AdventurerStatus.Exploring;
                adventurer.IsInDanger = false;
                adventurer.DangersSurvived++;
                adventurer.ExperienceGained += 100;
                adventurer.SurvivalRating += 0.1f;
            }
            else if (adventurer.DangerTimer > 30f) // 30 seconds in danger = quest failure
            {
                adventurer.Status = AdventurerStatus.Lost;
            }
        }


        private void ProcessResting(ref AdventurerComponent adventurer, float deltaTime)
        {
            // Recover stamina while resting
            adventurer.Stamina = math.min(adventurer.MaxStamina, adventurer.Stamina + 20f * deltaTime);

            // Return to exploration when stamina is restored
            if (adventurer.Stamina >= adventurer.MaxStamina * 0.8f)
            {
                adventurer.Status = AdventurerStatus.Exploring;
            }
        }


        private void ProcessReturning(ref AdventurerComponent adventurer, AdventurePerformanceComponent performance)
        {
            // Return journey (simplified)
            adventurer.ExplorationProgress -= performance.NavigationSkill * DeltaTime * 0.5f;

            if (adventurer.ExplorationProgress <= 0f)
            {
                adventurer.Status = AdventurerStatus.Completed;
            }
        }


        private void CompleteQuest(ref AdventurerComponent adventurer)
        {
            adventurer.Status = AdventurerStatus.Returning;
            adventurer.QuestsCompleted++;
            adventurer.ExperienceGained += 200; // Base quest completion reward
        }


        private void UpdateAdventurerCondition(ref AdventurerComponent adventurer, AdventurePerformanceComponent performance, GeneticDataComponent genetics, float deltaTime)
        {
            // Update stamina drain during exploration
            if (adventurer.Status == AdventurerStatus.Exploring)
            {
                float staminaDrain = 5f * deltaTime;
                float staminaEfficiency = genetics.Stamina * performance.StaminaManagement;
                adventurer.Stamina = math.max(0f, adventurer.Stamina - staminaDrain / (1f + staminaEfficiency));

                // Force rest if stamina too low
                if (adventurer.Stamina < adventurer.MaxStamina * 0.2f)
                {
                    adventurer.Status = AdventurerStatus.Resting;
                }
            }

            // Update mood based on recent events
            UpdateAdventurerMood(ref adventurer);
        }


        private void UpdateAdventurerMood(ref AdventurerComponent adventurer)
        {
            if (adventurer.TreasuresFound > 3)
                adventurer.Mood = AdventurerMood.Triumphant;
            else if (adventurer.IsInDanger)
                adventurer.Mood = AdventurerMood.Nervous;
            else if (adventurer.Stamina < adventurer.MaxStamina * 0.3f)
                adventurer.Mood = AdventurerMood.Exhausted;
            else if (adventurer.QuestsCompleted > 0)
                adventurer.Mood = AdventurerMood.Confident;
            else
                adventurer.Mood = AdventurerMood.Determined;
        }
    }

    /// <summary>
    /// Procedural quest generation system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AdventureExplorationSystem))]
    public partial class ProceduralQuestSystem : SystemBase
    {
        private EntityQuery questQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            questQuery = GetEntityQuery(ComponentType.ReadWrite<QuestComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer();

            // Generate new quests as needed
            GenerateProceduralQuests(ecb);

            // Update existing quests
            foreach (var quest in SystemAPI.Query<RefRW<QuestComponent>>())
            {
                UpdateQuestProgress(ref quest.ValueRW);
            }
        }

        private void GenerateProceduralQuests(EntityCommandBuffer ecb)
        {
            // Generate a new quest (simplified example)
            if (math.random().NextFloat() < 0.01f) // 1% chance per frame
            {
                var questEntity = ecb.CreateEntity();
                var questSeed = (uint)UnityEngine.Random.Range(1, int.MaxValue);

                ecb.AddComponent(questEntity, GenerateRandomQuest(questSeed));
            }
        }

        private QuestComponent GenerateRandomQuest(uint seed)
        {
            var random = new Unity.Mathematics.Random(seed);

            return new QuestComponent
            {
                Type = (QuestType)random.NextInt(0, 10),
                Difficulty = (QuestDifficulty)random.NextInt(1, 7),
                Status = QuestStatus.Available,
                QuestGiver = Entity.Null,
                Adventurer = Entity.Null,
                QuestLocation = random.NextFloat3(-100f, 100f),
                QuestRadius = random.NextFloat(10f, 50f),
                ObjectivesTotal = random.NextInt(1, 5),
                ObjectivesCompleted = 0,
                TimeLimit = random.NextFloat(300f, 1800f), // 5-30 minutes
                TimeRemaining = 0f,
                RewardExperience = random.NextInt(100, 1000),
                RewardTreasure = random.NextInt(50, 500),
                IsTimeLimited = random.NextFloat() < 0.3f,
                HasDangers = random.NextFloat() < 0.6f,
                DangerLevel = random.NextFloat(0.1f, 1.0f),
                QuestSeed = seed
            };
        }

        private void UpdateQuestProgress(ref QuestComponent quest)
        {
            if (quest.Status != QuestStatus.In_Progress)
                return;

            // Update time limit
            if (quest.IsTimeLimited)
            {
                quest.TimeRemaining -= SystemAPI.Time.DeltaTime;
                if (quest.TimeRemaining <= 0f)
                {
                    quest.Status = QuestStatus.Expired;
                }
            }

            // Check completion
            if (quest.ObjectivesCompleted >= quest.ObjectivesTotal)
            {
                quest.Status = QuestStatus.Completed;
            }
        }
    }

    /// <summary>
    /// Dungeon exploration system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ProceduralQuestSystem))]
    public partial class DungeonExplorationSystem : SystemBase
    {
        private EntityQuery dungeonQuery;

        protected override void OnCreate()
        {
            dungeonQuery = GetEntityQuery(ComponentType.ReadWrite<DungeonComponent>());
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var dungeon in SystemAPI.Query<RefRW<DungeonComponent>>())
            {
                if (dungeon.ValueRO.Status == DungeonStatus.Partially_Explored)
                {
                    UpdateDungeonExploration(ref dungeon.ValueRW, deltaTime);
                }
            }
        }

        private void UpdateDungeonExploration(ref DungeonComponent dungeon, float deltaTime)
        {
            // Simulate dungeon exploration progress
            float explorationRate = 0.1f * deltaTime; // Base exploration rate
            dungeon.ExplorationProgress += explorationRate;

            // Update rooms explored based on progress
            int newRoomsExplored = (int)(dungeon.ExplorationProgress * dungeon.TotalRooms);
            if (newRoomsExplored > dungeon.RoomsExplored)
            {
                dungeon.RoomsExplored = newRoomsExplored;

                // Chance for encounters
                if (math.random().NextFloat() < 0.3f)
                {
                    if (math.random().NextFloat() < 0.5f)
                        dungeon.TreasuresFound++;
                    else
                        dungeon.TrapsEncountered++;
                }

                if (math.random().NextFloat() < 0.2f)
                    dungeon.MonstersSeen++;
            }

            // Increase difficulty as we go deeper
            dungeon.DifficultyProgression = dungeon.ExplorationProgress * 2f;

            // Check for completion
            if (dungeon.RoomsExplored >= dungeon.TotalRooms)
            {
                dungeon.Status = DungeonStatus.Fully_Mapped;

                // Boss fight for certain dungeon types
                if (dungeon.HasBoss && !dungeon.BossDefeated)
                {
                    // Simulate boss encounter
                    if (math.random().NextFloat() < 0.7f) // 70% boss defeat chance
                    {
                        dungeon.BossDefeated = true;
                        dungeon.Status = DungeonStatus.Boss_Defeated;
                    }
                }
            }
        }
    }

    /// <summary>
    /// PvP Competition System for Adventure Activities
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DungeonExplorationSystem))]
    public partial class AdventurePvPCompetitionSystem : SystemBase
    {
        private EntityQuery competitionQuery;
        private EntityQuery territoryQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            competitionQuery = GetEntityQuery(ComponentType.ReadWrite<AdventureCompetitionComponent>());
            territoryQuery = GetEntityQuery(ComponentType.ReadWrite<TerritoryControlComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update PvP competitions
            var competitionJob = new CompetitionUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };
            Dependency = competitionJob.ScheduleParallel(competitionQuery, Dependency);

            // Update territory control
            var territoryJob = new TerritoryControlJob
            {
                DeltaTime = deltaTime
            };
            Dependency = territoryJob.ScheduleParallel(territoryQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    public partial struct CompetitionUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref AdventureCompetitionComponent competition)
        {
            switch (competition.Status)
            {
                case CompetitionStatus.Preparing:
                    // Wait for competitors to be ready
                    if (competition.Competitor1 != Entity.Null && competition.Competitor2 != Entity.Null)
                    {
                        competition.Status = CompetitionStatus.Active;
                        competition.TimeRemaining = competition.TimeLimit;
                    }
                    break;

                case CompetitionStatus.Active:
                    UpdateActiveCompetition(ref competition);
                    break;

                case CompetitionStatus.Completed:
                    // Award prizes and cleanup
                    ProcessCompetitionResults(ref competition);
                    CommandBuffer.DestroyEntity(chunkIndex, entity);
                    break;
            }
        }


        private void UpdateActiveCompetition(ref AdventureCompetitionComponent competition)
        {
            competition.TimeRemaining -= DeltaTime;

            // Update scores based on competition type
            switch (competition.Type)
            {
                case CompetitionType.Dungeon_Race:
                    UpdateDungeonRace(ref competition);
                    break;

                case CompetitionType.Treasure_Hunt_Duel:
                    UpdateTreasureHuntDuel(ref competition);
                    break;

                case CompetitionType.Speed_Exploration:
                    UpdateSpeedExploration(ref competition);
                    break;

                case CompetitionType.Survival_Challenge:
                    UpdateSurvivalChallenge(ref competition);
                    break;

                case CompetitionType.Territory_Control:
                    UpdateTerritoryControl(ref competition);
                    break;
            }

            // Check for completion
            if (competition.TimeRemaining <= 0f || HasWinCondition(competition))
            {
                DetermineWinner(ref competition);
                competition.Status = CompetitionStatus.Completed;
            }
        }


        private void UpdateDungeonRace(ref AdventureCompetitionComponent competition)
        {
            // Simulate dungeon exploration progress
            float progress1 = math.random().NextFloat(0f, 1f) * DeltaTime;
            float progress2 = math.random().NextFloat(0f, 1f) * DeltaTime;

            competition.Competition1Score += progress1;
            competition.Competition2Score += progress2;
        }


        private void UpdateTreasureHuntDuel(ref AdventureCompetitionComponent competition)
        {
            // Treasure discovery simulation
            if (math.random().NextFloat() < 0.02f) // 2% chance per frame
            {
                if (math.random().NextFloat() < 0.5f)
                    competition.Competition1Score += 1f;
                else
                    competition.Competition2Score += 1f;
            }
        }


        private void UpdateSpeedExploration(ref AdventureCompetitionComponent competition)
        {
            // Speed-based exploration
            float speed1 = math.random().NextFloat(0.5f, 1.5f) * DeltaTime;
            float speed2 = math.random().NextFloat(0.5f, 1.5f) * DeltaTime;

            competition.Competition1Score += speed1;
            competition.Competition2Score += speed2;
        }


        private void UpdateSurvivalChallenge(ref AdventureCompetitionComponent competition)
        {
            // Survival endurance test
            float endurance1 = math.random().NextFloat(0.8f, 1.2f);
            float endurance2 = math.random().NextFloat(0.8f, 1.2f);

            // Higher score = better survival
            competition.Competition1Score += endurance1 * DeltaTime;
            competition.Competition2Score += endurance2 * DeltaTime;
        }


        private void UpdateTerritoryControl(ref AdventureCompetitionComponent competition)
        {
            // Territory capture mechanics
            float control1 = math.random().NextFloat(0f, 1f) * DeltaTime * 0.5f;
            float control2 = math.random().NextFloat(0f, 1f) * DeltaTime * 0.5f;

            competition.Competition1Score += control1;
            competition.Competition2Score += control2;
        }


        private bool HasWinCondition(AdventureCompetitionComponent competition)
        {
            switch (competition.Type)
            {
                case CompetitionType.Dungeon_Race:
                    return competition.Competition1Score >= 100f || competition.Competition2Score >= 100f;

                case CompetitionType.Treasure_Hunt_Duel:
                    return competition.Competition1Score >= 10f || competition.Competition2Score >= 10f;

                case CompetitionType.Speed_Exploration:
                    return competition.Competition1Score >= 50f || competition.Competition2Score >= 50f;

                case CompetitionType.Territory_Control:
                    return competition.Competition1Score >= 20f || competition.Competition2Score >= 20f;

                default:
                    return false;
            }
        }


        private void DetermineWinner(ref AdventureCompetitionComponent competition)
        {
            if (competition.Competition1Score > competition.Competition2Score)
            {
                competition.Winner = competition.Competitor1;
            }
            else if (competition.Competition2Score > competition.Competition1Score)
            {
                competition.Winner = competition.Competitor2;
            }
            else
            {
                competition.Status = CompetitionStatus.Tie;
                competition.Winner = Entity.Null;
            }
        }


        private void ProcessCompetitionResults(ref AdventureCompetitionComponent competition)
        {
            // Award experience and prizes to winner
            if (competition.Winner != Entity.Null)
            {
                // Winner gets prize pool
                competition.PrizePool *= 1.5f; // Bonus for winning
            }

            // Both participants get base experience
            // (This would update AdventurerComponent.ExperienceGained in a full implementation)
        }
    }


    public partial struct TerritoryControlJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref TerritoryControlComponent territory)
        {
            switch (territory.Status)
            {
                case TerritoryStatus.Contested:
                    UpdateContestedTerritory(ref territory);
                    break;

                case TerritoryStatus.Controlled:
                    // Generate resources for controller
                    territory.ControlValue += DeltaTime * 0.1f;
                    break;

                case TerritoryStatus.Neutral:
                    // Available for capture
                    if (territory.Challenger != Entity.Null)
                    {
                        territory.Status = TerritoryStatus.Contested;
                        territory.ContestedTimer = 0f;
                    }
                    break;
            }
        }


        private void UpdateContestedTerritory(ref TerritoryControlComponent territory)
        {
            territory.ContestedTimer += DeltaTime;

            // Simulate territorial struggle
            float controlChange = math.random().NextFloat(-1f, 1f) * DeltaTime * 0.1f;
            territory.ControlProgress += controlChange;

            float challengeChange = math.random().NextFloat(-1f, 1f) * DeltaTime * 0.1f;
            territory.ChallengeProgress += challengeChange;

            // Clamp values
            territory.ControlProgress = math.clamp(territory.ControlProgress, 0f, 1f);
            territory.ChallengeProgress = math.clamp(territory.ChallengeProgress, 0f, 1f);

            // Determine control after 60 seconds of contest
            if (territory.ContestedTimer >= 60f)
            {
                if (territory.ControlProgress > territory.ChallengeProgress)
                {
                    territory.Status = TerritoryStatus.Controlled;
                    // Controller maintains control
                }
                else
                {
                    territory.Status = TerritoryStatus.Captured;
                    territory.Controller = territory.Challenger;
                    territory.Challenger = Entity.Null;
                }

                territory.ContestedTimer = 0f;
                territory.ControlProgress = 0f;
                territory.ChallengeProgress = 0f;
            }
        }
    }

    #endregion

    #region Adventure Authoring

    /// <summary>
    /// MonoBehaviour authoring for adventure guilds
    /// </summary>
    public class AdventureGuildAuthoring : MonoBehaviour
    {
        [Header("Guild Configuration")]
        public GuildType guildType = GuildType.Explorers_Guild;
        [Range(1, 20)] public int maxAdventurers = 10;
        [Range(10f, 500f)] public float explorationRadius = 100f;
        [Range(30f, 300f)] public float questGenerationRate = 120f;

        [Header("Available Quest Types")]
        public QuestType[] supportedQuests = { QuestType.Exploration, QuestType.Treasure_Hunt, QuestType.Dungeon_Delve };
        public QuestDifficulty maxDifficulty = QuestDifficulty.Expert;
        public bool allowDangerousQuests = true;

        [Header("PvP Competition Settings")]
        public bool enablePvP = true;
        public CompetitionType[] supportedCompetitions = { CompetitionType.Dungeon_Race, CompetitionType.Treasure_Hunt_Duel, CompetitionType.Territory_Control };
        [Range(2, 8)] public int maxCompetitors = 4;
        [Range(1f, 5f)] public float competitionRewardMultiplier = 2f;
        public bool allowTeamCompetitions = true;

        [Header("Guild Facilities")]
        public Transform questBoard;
        public Transform[] equipmentShops;
        public Transform restArea;
        public Transform mapRoom;

        [Header("World Integration")]
        public Transform[] questLocations;
        public Transform[] dungeonEntrances;
        public Transform[] treasureSpots;

        [ContextMenu("Create Adventure Guild Entity")]
        public void CreateAdventureGuildEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            // Add adventure guild component
            entityManager.AddComponentData(entity, new AdventureGuildComponent
            {
                Type = guildType,
                Status = GuildStatus.Open,
                MaxAdventurers = maxAdventurers,
                CurrentAdventurers = 0,
                AvailableQuests = 5, // Start with some quests
                CompletedQuests = 0,
                ActiveExpeditions = 0,
                GuildRenown = 100f,
                GuildLevel = 1,
                MaxDifficulty = maxDifficulty,
                QuestGenerationTimer = 0f,
                QuestGenerationRate = questGenerationRate,
                TreasureVaults = 3,
                ExplorationRadius = explorationRadius,

                // PvP Competition Features
                PvPEnabled = enablePvP,
                ActiveCompetitions = 0,
                CompetitionWins = 0,
                CompetitionLosses = 0,
                CompetitionTimer = 0f,
                CurrentCompetition = supportedCompetitions.Length > 0 ? supportedCompetitions[0] : CompetitionType.Dungeon_Race,
                MaxCompetitors = maxCompetitors,
                CompetitionRewardMultiplier = competitionRewardMultiplier
            });

            // Add activity center component
            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = ActivityType.Adventure,
                MaxParticipants = maxAdventurers,
                CurrentParticipants = 0,
                ActivityDuration = 600f, // 10 minute adventures
                DifficultyLevel = (float)maxDifficulty,
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

            Debug.Log($"✅ Created {guildType} with {supportedQuests.Length} quest types");
        }

        private void OnDrawGizmos()
        {
            // Draw guild bounds
            var color = guildType switch
            {
                GuildType.Explorers_Guild => Color.green,
                GuildType.Treasure_Hunters => Color.yellow,
                GuildType.Dungeon_Delvers => Color.red,
                GuildType.Survival_Specialists => Color.blue,
                GuildType.Monster_Researchers => Color.magenta,
                GuildType.Cartographers => Color.cyan,
                _ => Color.white
            };

            Gizmos.color = color;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 12f);

            // Draw exploration radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireDisc(transform.position, Vector3.up, explorationRadius);

            // Draw quest locations
            if (questLocations != null)
            {
                Gizmos.color = Color.blue;
                foreach (var location in questLocations)
                {
                    if (location != null)
                    {
                        Gizmos.DrawWireSphere(location.position, 3f);
                        Gizmos.DrawLine(transform.position, location.position);
                    }
                }
            }

            // Draw dungeon entrances
            if (dungeonEntrances != null)
            {
                Gizmos.color = Color.red;
                foreach (var entrance in dungeonEntrances)
                {
                    if (entrance != null)
                    {
                        Gizmos.DrawWireCube(entrance.position, Vector3.one * 4f);
                    }
                }
            }

            // Draw treasure spots
            if (treasureSpots != null)
            {
                Gizmos.color = Color.yellow;
                foreach (var treasure in treasureSpots)
                {
                    if (treasure != null)
                    {
                        Gizmos.DrawIcon(treasure.position, "💰");
                    }
                }
            }

            // Draw guild facilities
            if (questBoard != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(questBoard.position, Vector3.one * 2f);
            }

            if (restArea != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(restArea.position, Vector3.one * 3f);
            }
        }
    }

    #endregion
}