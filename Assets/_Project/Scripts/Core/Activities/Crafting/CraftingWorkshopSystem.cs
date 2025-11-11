using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.Equipment;

namespace Laboratory.Core.Activities.Crafting
{
    /// <summary>
    /// 🔨 CRAFTING WORKSHOP SYSTEM - Complete item creation and resource processing
    /// FEATURES: Recipe crafting, resource processing, quality control, innovation
    /// PERFORMANCE: Batch processing for multiple simultaneous crafting operations
    /// GENETICS: Intelligence, Adaptability affect crafting success and innovation
    /// </summary>

    #region Crafting Components

    public struct CraftingWorkshopComponent : IComponentData
    {
        public WorkshopType Type;
        public WorkshopStatus Status;
        public int MaxCrafters;
        public int CurrentCrafters;
        public int RecipesKnown;
        public int ItemsCrafted;
        public float WorkshopEfficiency;
        public int QualityLevel;
        public bool CanInnovate;
        public float InnovationRate;
        public int MaterialStorage;
        public int MaxMaterialStorage;
        public float SessionTimer;

        // PvP Competition Features
        public bool PvPEnabled;
        public int ActiveCompetitions;
        public int CompetitionWins;
        public int CompetitionLosses;
        public float CompetitionTimer;
        public CraftingCompetitionType CurrentCompetition;
        public int MaxCompetitors;
        public float CompetitionRewardMultiplier;
        public bool AllowTeamCrafting;
    }

    public struct CrafterComponent : IComponentData
    {
        public Entity Workshop;
        public CrafterStatus Status;
        public CraftingSpecialty Specialty;
        public Entity CurrentRecipe;
        public float CraftingProgress;
        public float CraftingTime;
        public int ItemsCrafted;
        public int QualityItemsProduced;
        public int RecipesLearned;
        public float SkillLevel;
        public bool IsInnovating;
        public float InnovationProgress;
        public CraftingMood Mood;
        public float Stamina;

        // PvP Competition Data
        public bool IsInCompetition;
        public Entity CompetitionOpponent;
        public CraftingCompetitionType CompetitionMode;
        public float CompetitionScore;
        public int CompetitionRank;
        public bool HasCompetitionAdvantage;
        public float CompetitionTimer;
        public int PvPWins;
        public int PvPLosses;
        public float PvPRating;
        public int SpeedCraftingBonus;
        public int QualityCompetitionBonus;
    }

    public struct CraftingPerformanceComponent : IComponentData
    {
        public float CraftingSpeed;
        public float QualityControl;
        public float ResourceEfficiency;
        public float InnovationAbility;
        public float RecipeLearning;
        public float ToolProficiency;
        public float MaterialKnowledge;
        public float PrecisionWork;
        public float CreativeThinking;
        public int CraftingExperience;
        public bool HasMasterSkills;
    }

    public struct CraftingRecipeComponent : IComponentData
    {
        public RecipeType Type;
        public RecipeDifficulty Difficulty;
        public FixedList64Bytes<MaterialType> RequiredMaterials;
        public FixedList64Bytes<int> MaterialQuantities;
        public float CraftingTime;
        public float SuccessRate;
        public ItemQuality MinQuality;
        public ItemQuality MaxQuality;
        public bool RequiresSpecialty;
        public CraftingSpecialty RequiredSpecialty;
        public uint RecipeHash;
        public bool IsDiscovered;
    }

    public struct CraftingMaterialComponent : IComponentData
    {
        public MaterialType Type;
        public MaterialGrade Grade;
        public int Quantity;
        public float QualityModifier;
        public bool IsRare;
        public float ProcessingTime;
        public MaterialProperty Properties;
        public float Value;
    }

    public struct CraftedItemComponent : IComponentData
    {
        public ItemType Type;
        public ItemQuality Quality;
        public Entity Crafter;
        public float QualityScore;
        public float DurabilityBonus;
        public bool HasSpecialProperties;
        public float ItemValue;
        public uint CraftingSignature;
        public bool IsInnovation;
    }

    /// <summary>
    /// PvP crafting competition mechanics
    /// </summary>
    public struct CraftingCompetitionComponent : IComponentData
    {
        public CraftingCompetitionType Type;
        public CompetitionStatus Status;
        public Entity Competitor1;
        public Entity Competitor2;
        public float Competition1Score;
        public float Competition2Score;
        public float TimeLimit;
        public float TimeRemaining;
        public Entity Winner;
        public CompetitionRules Rules;
        public ItemType TargetItem;
        public int TargetQuantity;
        public ItemQuality MinQuality;
        public int SpectatorCount;
        public float PrizePool;
        public bool IsTeamCompetition;
        public int TeamSize;
        public uint CompetitionSeed;
    }

    /// <summary>
    /// Speed crafting competition mechanics
    /// </summary>
    public struct SpeedCraftingComponent : IComponentData
    {
        public Entity Competitor1;
        public Entity Competitor2;
        public int Items1Crafted;
        public int Items2Crafted;
        public float Speed1Time;
        public float Speed2Time;
        public bool IsTimeAttack;
        public float TimeBonus1;
        public float TimeBonus2;
        public float QualityPenalty1;
        public float QualityPenalty2;
        public SpeedCraftingStatus Status;
    }

    /// <summary>
    /// Innovation competition for creating new recipes
    /// </summary>
    public struct InnovationCompetitionComponent : IComponentData
    {
        public Entity Innovator1;
        public Entity Innovator2;
        public int Innovations1Created;
        public int Innovations2Created;
        public float Innovation1Quality;
        public float Innovation2Quality;
        public bool RequiresOriginality;
        public float OriginalityBonus1;
        public float OriginalityBonus2;
        public int SharedMaterials;
        public InnovationCompetitionStatus Status;
    }

    /// <summary>
    /// Resource trading and stealing mechanics for competition
    /// </summary>
    public struct ResourceCompetitionComponent : IComponentData
    {
        public Entity Trader1;
        public Entity Trader2;
        public int Resources1Acquired;
        public int Resources2Acquired;
        public float Trading1Efficiency;
        public float Trading2Efficiency;
        public bool AllowResourceStealing;
        public int StolenResources1;
        public int StolenResources2;
        public float MarketValue1;
        public float MarketValue2;
        public ResourceCompetitionStatus Status;
    }

    #endregion

    #region Crafting Enums

    public enum WorkshopType : byte
    {
        General_Workshop,
        Equipment_Forge,
        Potion_Laboratory,
        Enchanting_Chamber,
        Tech_Lab,
        Art_Studio,
        Food_Kitchen,
        Innovation_Center
    }

    public enum WorkshopStatus : byte
    {
        Open,
        Crafting_Session,
        Material_Processing,
        Quality_Control,
        Innovation_Mode,
        Maintenance,
        Closed
    }

    public enum CrafterStatus : byte
    {
        Idle,
        Selecting_Recipe,
        Gathering_Materials,
        Crafting,
        Quality_Check,
        Innovating,
        Learning,
        Resting
    }

    public enum CraftingSpecialty : byte
    {
        None,
        Weaponsmith,
        Armorcrafter,
        Alchemist,
        Enchanter,
        Engineer,
        Artist,
        Chef,
        Innovator
    }

    public enum CraftingMood : byte
    {
        Focused,
        Creative,
        Methodical,
        Inspired,
        Frustrated,
        Determined,
        Experimental,
        Perfectionist
    }

    public enum RecipeType : byte
    {
        Equipment,
        Consumable,
        Material,
        Tool,
        Enhancement,
        Decoration,
        Component,
        Innovation
    }

    public enum RecipeDifficulty : byte
    {
        Novice,
        Apprentice,
        Journeyman,
        Expert,
        Master,
        Legendary
    }

    public enum MaterialType : byte
    {
        Metal,
        Wood,
        Gem,
        Fabric,
        Chemical,
        Crystal,
        Organic,
        Synthetic,
        Magical,
        Rare_Element
    }

    public enum MaterialGrade : byte
    {
        Common,
        Quality,
        Superior,
        Exceptional,
        Legendary,
        Mythical
    }

    public enum ItemType : byte
    {
        Weapon,
        Armor,
        Accessory,
        Tool,
        Potion,
        Enhancement,
        Component,
        Art_Piece
    }

    public enum ItemQuality : byte
    {
        Poor,
        Common,
        Good,
        Superior,
        Exceptional,
        Perfect,
        Legendary
    }

    public enum MaterialProperty : uint
    {
        None = 0,
        Durable = 1,
        Lightweight = 2,
        Conductive = 4,
        Magical = 8,
        Flexible = 16,
        Rare = 32,
        Volatile = 64
    }

    // PvP Competition Enums
    public enum CraftingCompetitionType : byte
    {
        Speed_Crafting,
        Quality_Contest,
        Innovation_Challenge,
        Resource_Trading,
        Efficiency_Battle,
        Team_Workshop,
        Master_Craftsman_Duel,
        Recipe_Creation_Race
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
        Limited_Materials,
        Time_Pressure,
        Quality_Focus,
        Innovation_Only,
        Team_Collaboration,
        No_Equipment_Bonus,
        Sudden_Death
    }

    public enum SpeedCraftingStatus : byte
    {
        Preparing,
        Racing,
        Finished,
        Quality_Check,
        Disqualified
    }

    public enum InnovationCompetitionStatus : byte
    {
        Brainstorming,
        Experimenting,
        Creating,
        Testing,
        Completed,
        Failed_Innovation
    }

    public enum ResourceCompetitionStatus : byte
    {
        Market_Opening,
        Trading_Active,
        Bidding_War,
        Final_Trades,
        Market_Closed
    }

    #endregion

    #region Crafting Systems

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class CraftingWorkshopManagementSystem : SystemBase
    {
        private EntityQuery workshopQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            workshopQuery = GetEntityQuery(ComponentType.ReadWrite<CraftingWorkshopComponent>());
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var workshop in SystemAPI.Query<RefRW<CraftingWorkshopComponent>>())
            {
                UpdateWorkshop(ref workshop.ValueRW, deltaTime);
            }
        }

        private void UpdateWorkshop(ref CraftingWorkshopComponent workshop, float deltaTime)
        {
            workshop.SessionTimer += deltaTime;

            switch (workshop.Status)
            {
                case WorkshopStatus.Open:
                    if (workshop.CurrentCrafters > 0)
                        workshop.Status = WorkshopStatus.Crafting_Session;
                    break;

                case WorkshopStatus.Crafting_Session:
                    UpdateCraftingSession(ref workshop, deltaTime);
                    break;

                case WorkshopStatus.Innovation_Mode:
                    UpdateInnovationMode(ref workshop, deltaTime);
                    break;
            }

            // Update workshop efficiency based on usage
            if (workshop.CurrentCrafters > 0)
            {
                workshop.WorkshopEfficiency = math.min(2f, workshop.WorkshopEfficiency + 0.01f * deltaTime);
            }
        }

        private void UpdateCraftingSession(ref CraftingWorkshopComponent workshop, float deltaTime)
        {
            // Increase items crafted (simplified)
            if (workshop.CurrentCrafters > 0 && random.NextFloat() < 0.01f)
            {
                workshop.ItemsCrafted++;
            }
        }

        private void UpdateInnovationMode(ref CraftingWorkshopComponent workshop, float deltaTime)
        {
            if (workshop.CanInnovate)
            {
                // Innovation attempts
                if (random.NextFloat() < workshop.InnovationRate * deltaTime)
                {
                    workshop.RecipesKnown++;
                }
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CraftingWorkshopManagementSystem))]
    public partial class CraftingProcessSystem : SystemBase
    {
        private EntityQuery craftingQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            craftingQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<CrafterComponent>(),
                ComponentType.ReadOnly<CraftingPerformanceComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            var craftingJob = new CraftingProcessJob
            {
                DeltaTime = deltaTime,
                Time = (float)SystemAPI.Time.ElapsedTime,
                random = random
            };

            Dependency = craftingJob.ScheduleParallel(craftingQuery, Dependency);
        }
    }


    [BurstCompile]
    public partial struct CraftingProcessJob : IJobEntity
    {
        public float DeltaTime;
        public float Time;
        public Unity.Mathematics.Random random;

        public void Execute(ref CrafterComponent crafter,
            in CraftingPerformanceComponent performance,
            RefRO<GeneticDataComponent> genetics)
        {
            if (crafter.Status != CrafterStatus.Crafting)
                return;

            // Update crafting progress
            float craftingSpeed = genetics.ValueRO.Intelligence * performance.CraftingSpeed;
            crafter.CraftingProgress += craftingSpeed * DeltaTime * 0.1f;

            // Update stamina
            crafter.Stamina = math.max(0f, crafter.Stamina - 2f * DeltaTime);

            // Check for completion
            if (crafter.CraftingProgress >= 1f)
            {
                CompleteCraftingItem(ref crafter, performance, genetics.ValueRO);
            }

            // Check for innovation opportunities
            if (genetics.ValueRO.Curiosity > 0.7f && performance.InnovationAbility > 0.6f)
            {
                if (random.NextFloat() < 0.001f) // 0.1% chance per frame
                {
                    crafter.IsInnovating = true;
                    crafter.Status = CrafterStatus.Innovating;
                }
            }

            // Update mood
            UpdateCrafterMood(ref crafter, performance, genetics.ValueRO);
        }


        private void CompleteCraftingItem(ref CrafterComponent crafter, CraftingPerformanceComponent performance, GeneticDataComponent genetics)
        {
            crafter.ItemsCrafted++;
            crafter.CraftingProgress = 0f;

            // Calculate quality based on performance
            float qualityRoll = genetics.Intelligence * performance.QualityControl * random.NextFloat();
            if (qualityRoll > 0.8f)
            {
                crafter.QualityItemsProduced++;
            }

            // Gain skill experience
            crafter.SkillLevel += 0.1f;

            // Return to idle state
            crafter.Status = CrafterStatus.Idle;
        }


        private void UpdateCrafterMood(ref CrafterComponent crafter, CraftingPerformanceComponent performance, GeneticDataComponent genetics)
        {
            if (crafter.QualityItemsProduced > crafter.ItemsCrafted * 0.8f)
                crafter.Mood = CraftingMood.Inspired;
            else if (crafter.IsInnovating)
                crafter.Mood = CraftingMood.Experimental;
            else if (crafter.Stamina < 0.3f)
                crafter.Mood = CraftingMood.Frustrated;
            else if (genetics.Intelligence > 0.7f)
                crafter.Mood = CraftingMood.Methodical;
            else
                crafter.Mood = CraftingMood.Focused;
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CraftingProcessSystem))]
    public partial class RecipeDiscoverySystem : SystemBase
    {
        private EntityQuery recipeQuery;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            recipeQuery = GetEntityQuery(ComponentType.ReadWrite<CraftingRecipeComponent>());
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            // Handle recipe discovery and learning
            foreach (var recipe in SystemAPI.Query<RefRW<CraftingRecipeComponent>>())
            {
                if (!recipe.ValueRO.IsDiscovered)
                {
                    // Simple discovery mechanics
                    if (random.NextFloat() < 0.001f)
                    {
                        recipe.ValueRW.IsDiscovered = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// PvP Crafting Competition System
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(RecipeDiscoverySystem))]
    public partial class CraftingPvPCompetitionSystem : SystemBase
    {
        private EntityQuery competitionQuery;
        private EntityQuery speedCraftingQuery;
        private EntityQuery innovationQuery;
        private EntityQuery resourceQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;
        private Unity.Mathematics.Random random;

        protected override void OnCreate()
        {
            competitionQuery = GetEntityQuery(ComponentType.ReadWrite<CraftingCompetitionComponent>());
            speedCraftingQuery = GetEntityQuery(ComponentType.ReadWrite<SpeedCraftingComponent>());
            innovationQuery = GetEntityQuery(ComponentType.ReadWrite<InnovationCompetitionComponent>());
            resourceQuery = GetEntityQuery(ComponentType.ReadWrite<ResourceCompetitionComponent>());
            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
            random = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks);
        }

        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            // Update crafting competitions
            var competitionJob = new CraftingCompetitionUpdateJob
            {
                DeltaTime = deltaTime,
                CommandBuffer = ecb
            };
            Dependency = competitionJob.ScheduleParallel(competitionQuery, Dependency);

            // Update speed crafting competitions
            var speedJob = new SpeedCraftingJob
            {
                DeltaTime = deltaTime
            };
            Dependency = speedJob.ScheduleParallel(speedCraftingQuery, Dependency);

            // Update innovation competitions
            var innovationJob = new InnovationCompetitionJob
            {
                DeltaTime = deltaTime,
                FrameCount = (uint)(SystemAPI.Time.ElapsedTime * 60f) // Approximate frame count at 60 FPS
            };
            Dependency = innovationJob.ScheduleParallel(innovationQuery, Dependency);

            // Update resource competitions
            var resourceJob = new ResourceCompetitionJob
            {
                DeltaTime = deltaTime
            };
            Dependency = resourceJob.ScheduleParallel(resourceQuery, Dependency);

            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    [BurstCompile]
    public partial struct CraftingCompetitionUpdateJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref CraftingCompetitionComponent competition)
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


        private void UpdateActiveCompetition(ref CraftingCompetitionComponent competition)
        {
            competition.TimeRemaining -= DeltaTime;

            // Update scores based on competition type
            switch (competition.Type)
            {
                case CraftingCompetitionType.Speed_Crafting:
                    UpdateSpeedCraftingCompetition(ref competition);
                    break;

                case CraftingCompetitionType.Quality_Contest:
                    UpdateQualityContest(ref competition);
                    break;

                case CraftingCompetitionType.Innovation_Challenge:
                    UpdateInnovationChallenge(ref competition);
                    break;

                case CraftingCompetitionType.Resource_Trading:
                    UpdateResourceTrading(ref competition);
                    break;

                case CraftingCompetitionType.Efficiency_Battle:
                    UpdateEfficiencyBattle(ref competition);
                    break;

                case CraftingCompetitionType.Team_Workshop:
                    UpdateTeamWorkshop(ref competition);
                    break;

                case CraftingCompetitionType.Master_Craftsman_Duel:
                    UpdateMasterCraftsmanDuel(ref competition);
                    break;
            }

            // Check for completion
            if (competition.TimeRemaining <= 0f || HasCompetitionWinCondition(competition))
            {
                DetermineCompetitionWinner(ref competition);
                competition.Status = CompetitionStatus.Completed;
            }
        }


        private void UpdateSpeedCraftingCompetition(ref CraftingCompetitionComponent competition)
        {
            // Speed crafting simulation
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            float speed1 = random.NextFloat(0.8f, 1.2f) * DeltaTime;
            float speed2 = random.NextFloat(0.8f, 1.2f) * DeltaTime;

            competition.Competition1Score += speed1;
            competition.Competition2Score += speed2;
        }


        private void UpdateQualityContest(ref CraftingCompetitionComponent competition)
        {
            // Quality-focused crafting simulation
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            if (random.NextFloat() < 0.08f) // 8% chance per frame
            {
                float quality1 = random.NextFloat(1f, 5f);
                float quality2 = random.NextFloat(1f, 5f);

                competition.Competition1Score += quality1;
                competition.Competition2Score += quality2;
            }
        }


        private void UpdateInnovationChallenge(ref CraftingCompetitionComponent competition)
        {
            // Innovation competition simulation
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            if (random.NextFloat() < 0.02f) // 2% chance per frame for innovation
            {
                float innovation1 = random.NextFloat(5f, 15f);
                float innovation2 = random.NextFloat(5f, 15f);

                competition.Competition1Score += innovation1;
                competition.Competition2Score += innovation2;
            }
        }


        private void UpdateResourceTrading(ref CraftingCompetitionComponent competition)
        {
            // Resource trading competition
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            float trading1 = random.NextFloat(0.5f, 1.5f) * DeltaTime;
            float trading2 = random.NextFloat(0.5f, 1.5f) * DeltaTime;

            competition.Competition1Score += trading1;
            competition.Competition2Score += trading2;
        }


        private void UpdateEfficiencyBattle(ref CraftingCompetitionComponent competition)
        {
            // Efficiency competition (materials vs output)
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            float efficiency1 = random.NextFloat(0.7f, 1.3f) * DeltaTime;
            float efficiency2 = random.NextFloat(0.7f, 1.3f) * DeltaTime;

            competition.Competition1Score += efficiency1;
            competition.Competition2Score += efficiency2;
        }


        private void UpdateTeamWorkshop(ref CraftingCompetitionComponent competition)
        {
            // Team collaboration simulation
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            float team1 = random.NextFloat(0.6f, 1.4f) * DeltaTime * competition.TeamSize;
            float team2 = random.NextFloat(0.6f, 1.4f) * DeltaTime * competition.TeamSize;

            competition.Competition1Score += team1;
            competition.Competition2Score += team2;
        }


        private void UpdateMasterCraftsmanDuel(ref CraftingCompetitionComponent competition)
        {
            // High-stakes master duel
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            if (random.NextFloat() < 0.05f) // 5% chance per frame
            {
                float mastery1 = random.NextFloat(2f, 8f);
                float mastery2 = random.NextFloat(2f, 8f);

                competition.Competition1Score += mastery1;
                competition.Competition2Score += mastery2;
            }
        }


        private bool HasCompetitionWinCondition(CraftingCompetitionComponent competition)
        {
            switch (competition.Type)
            {
                case CraftingCompetitionType.Speed_Crafting:
                    return competition.Competition1Score >= 100f || competition.Competition2Score >= 100f;

                case CraftingCompetitionType.Quality_Contest:
                    return competition.Competition1Score >= 50f || competition.Competition2Score >= 50f;

                case CraftingCompetitionType.Innovation_Challenge:
                    return competition.Competition1Score >= 75f || competition.Competition2Score >= 75f;

                case CraftingCompetitionType.Resource_Trading:
                    return competition.Competition1Score >= 80f || competition.Competition2Score >= 80f;

                case CraftingCompetitionType.Efficiency_Battle:
                    return competition.Competition1Score >= 60f || competition.Competition2Score >= 60f;

                case CraftingCompetitionType.Team_Workshop:
                    return competition.Competition1Score >= 150f || competition.Competition2Score >= 150f;

                case CraftingCompetitionType.Master_Craftsman_Duel:
                    return competition.Competition1Score >= 100f || competition.Competition2Score >= 100f;

                default:
                    return false;
            }
        }


        private void DetermineCompetitionWinner(ref CraftingCompetitionComponent competition)
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


        private void ProcessCompetitionResults(ref CraftingCompetitionComponent competition)
        {
            // Award experience and prizes to winner
            if (competition.Winner != Entity.Null)
            {
                competition.PrizePool *= 1.5f; // Winner bonus
            }

            // Both participants get base experience
            // (This would update CrafterComponent.ExperienceGained in a full implementation)
        }
    }


    [BurstCompile]
    public partial struct SpeedCraftingJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref SpeedCraftingComponent speedCrafting)
        {
            switch (speedCrafting.Status)
            {
                case SpeedCraftingStatus.Racing:
                    UpdateSpeedCraftingRace(ref speedCrafting);
                    break;

                case SpeedCraftingStatus.Quality_Check:
                    CalculateSpeedCraftingResults(ref speedCrafting);
                    break;
            }
        }


        private void UpdateSpeedCraftingRace(ref SpeedCraftingComponent speedCrafting)
        {
            // Update crafting times
            speedCrafting.Speed1Time += DeltaTime;
            speedCrafting.Speed2Time += DeltaTime;

            // Simulate item completion
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            if (random.NextFloat() < 0.15f) // 15% chance per frame
            {
                if (random.NextFloat() < 0.5f)
                    speedCrafting.Items1Crafted++;
                else
                    speedCrafting.Items2Crafted++;
            }

            // Check for completion
            if (speedCrafting.Items1Crafted >= 10 || speedCrafting.Items2Crafted >= 10)
            {
                speedCrafting.Status = SpeedCraftingStatus.Quality_Check;
            }
        }


        private void CalculateSpeedCraftingResults(ref SpeedCraftingComponent speedCrafting)
        {
            // Calculate time bonuses and quality penalties
            speedCrafting.TimeBonus1 = math.max(0f, 120f - speedCrafting.Speed1Time); // 2 minute target
            speedCrafting.TimeBonus2 = math.max(0f, 120f - speedCrafting.Speed2Time);

            // Quality penalty for rushing
            if (speedCrafting.Speed1Time < 60f) // Rushed in under 1 minute
                speedCrafting.QualityPenalty1 = 10f;
            if (speedCrafting.Speed2Time < 60f)
                speedCrafting.QualityPenalty2 = 10f;

            speedCrafting.Status = SpeedCraftingStatus.Finished;
        }
    }


    [BurstCompile]
    public partial struct InnovationCompetitionJob : IJobEntity
    {
        public float DeltaTime;
        public uint FrameCount;

        public void Execute(ref InnovationCompetitionComponent innovation)
        {
            switch (innovation.Status)
            {
                case InnovationCompetitionStatus.Experimenting:
                    UpdateInnovationExperiments(ref innovation);
                    break;

                case InnovationCompetitionStatus.Creating:
                    UpdateInnovationCreation(ref innovation);
                    break;
            }
        }


        private void UpdateInnovationExperiments(ref InnovationCompetitionComponent innovation)
        {
            // Simulation innovation experimentation
            var random = Unity.Mathematics.Random.CreateFromIndex(FrameCount);
            if (random.NextFloat() < 0.03f) // 3% chance per frame
            {
                if (random.NextFloat() < 0.5f)
                {
                    innovation.Innovations1Created++;
                    innovation.Innovation1Quality += random.NextFloat(1f, 5f);
                }
                else
                {
                    innovation.Innovations2Created++;
                    innovation.Innovation2Quality += random.NextFloat(1f, 5f);
                }
            }

            // Move to creation phase after some innovations
            if (innovation.Innovations1Created >= 3 || innovation.Innovations2Created >= 3)
            {
                innovation.Status = InnovationCompetitionStatus.Creating;
            }
        }


        private void UpdateInnovationCreation(ref InnovationCompetitionComponent innovation)
        {
            // Finalize innovations with originality bonuses
            if (innovation.RequiresOriginality)
            {
                innovation.OriginalityBonus1 = innovation.Innovation1Quality * 0.2f;
                innovation.OriginalityBonus2 = innovation.Innovation2Quality * 0.2f;
            }

            // Complete when enough innovations are created
            if (innovation.Innovations1Created >= 5 || innovation.Innovations2Created >= 5)
            {
                innovation.Status = InnovationCompetitionStatus.Completed;
            }
        }
    }


    [BurstCompile]
    public partial struct ResourceCompetitionJob : IJobEntity
    {
        public float DeltaTime;

        public void Execute(ref ResourceCompetitionComponent resource)
        {
            switch (resource.Status)
            {
                case ResourceCompetitionStatus.Trading_Active:
                    UpdateResourceTrading(ref resource);
                    break;

                case ResourceCompetitionStatus.Bidding_War:
                    UpdateBiddingWar(ref resource);
                    break;
            }
        }


        private void UpdateResourceTrading(ref ResourceCompetitionComponent resource)
        {
            // Simulate resource acquisition and trading
            float acquisition1 = resource.Trading1Efficiency * DeltaTime * 0.1f;
            float acquisition2 = resource.Trading2Efficiency * DeltaTime * 0.1f;

            resource.Resources1Acquired += (int)acquisition1;
            resource.Resources2Acquired += (int)acquisition2;

            // Calculate market values
            resource.MarketValue1 += resource.Resources1Acquired * 1.5f;
            resource.MarketValue2 += resource.Resources2Acquired * 1.5f;

            // Rare chance for bidding war
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            if (random.NextFloat() < 0.01f) // 1% chance per frame
            {
                resource.Status = ResourceCompetitionStatus.Bidding_War;
            }
        }


        private void UpdateBiddingWar(ref ResourceCompetitionComponent resource)
        {
            // Intense bidding simulation
            var random = Unity.Mathematics.Random.CreateFromIndex((uint)UnityEngine.Time.frameCount);
            if (random.NextFloat() < 0.2f) // 20% chance per frame
            {
                int bidWinner = random.NextInt(0, 2);
                if (bidWinner == 0)
                {
                    resource.Resources1Acquired += 5;
                    resource.MarketValue1 += 20f;
                }
                else
                {
                    resource.Resources2Acquired += 5;
                    resource.MarketValue2 += 20f;
                }
            }

            // Return to normal trading after bidding war
            if (random.NextFloat() < 0.05f) // 5% chance to end
            {
                resource.Status = ResourceCompetitionStatus.Trading_Active;
            }
        }
    }

    #endregion

    public class CraftingWorkshopAuthoring : MonoBehaviour
    {
        [Header("Workshop Configuration")]
        public WorkshopType workshopType = WorkshopType.General_Workshop;
        [Range(1, 15)] public int maxCrafters = 8;
        [Range(1, 10)] public int qualityLevel = 5;
        public bool allowInnovation = true;
        [Range(100, 10000)] public int materialStorage = 1000;

        [Header("Specializations")]
        public CraftingSpecialty[] supportedSpecialties;
        public RecipeType[] craftableTypes;

        [Header("PvP Competition Settings")]
        public bool enablePvPCompetitions = true;
        public CraftingCompetitionType[] supportedCompetitions = { CraftingCompetitionType.Speed_Crafting, CraftingCompetitionType.Quality_Contest, CraftingCompetitionType.Innovation_Challenge };
        [Range(2, 8)] public int maxCompetitors = 4;
        [Range(1f, 5f)] public float competitionRewardMultiplier = 2f;
        public bool allowTeamCrafting = true;

        [ContextMenu("Create Crafting Workshop Entity")]
        public void CreateCraftingWorkshopEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity();

            entityManager.AddComponentData(entity, new CraftingWorkshopComponent
            {
                Type = workshopType,
                Status = WorkshopStatus.Open,
                MaxCrafters = maxCrafters,
                QualityLevel = qualityLevel,
                CanInnovate = allowInnovation,
                InnovationRate = allowInnovation ? 0.1f : 0f,
                MaterialStorage = materialStorage / 2,
                MaxMaterialStorage = materialStorage,
                WorkshopEfficiency = 1f,

                // PvP Competition Features
                PvPEnabled = enablePvPCompetitions,
                ActiveCompetitions = 0,
                CompetitionWins = 0,
                CompetitionLosses = 0,
                CompetitionTimer = 0f,
                CurrentCompetition = supportedCompetitions.Length > 0 ? supportedCompetitions[0] : CraftingCompetitionType.Speed_Crafting,
                MaxCompetitors = maxCompetitors,
                CompetitionRewardMultiplier = competitionRewardMultiplier,
                AllowTeamCrafting = allowTeamCrafting
            });

            entityManager.AddComponentData(entity, new ActivityCenterComponent
            {
                ActivityType = ActivityType.Crafting,
                MaxParticipants = maxCrafters,
                ActivityDuration = 600f,
                DifficultyLevel = qualityLevel,
                IsActive = true,
                QualityRating = qualityLevel / 10f
            });

            Debug.Log($"✅ Created {workshopType} with quality level {qualityLevel}");
        }
    }
}