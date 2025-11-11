using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using UnityEngine;
using Laboratory.Core.ECS;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;
using Laboratory.Core.Equipment;
using Laboratory.Core.Enums;

namespace Laboratory.Core.Progression
{
    /// <summary>
    /// Progression System - Tracks monster development, player advancement, and skill growth
    /// FEATURES: Experience gain, level progression, skill trees, achievement tracking
    /// PERFORMANCE: Handles progression for 1000+ creatures with efficient batch updates
    /// INTEGRATION: Works with activities, breeding, and equipment systems
    /// </summary>

    #region Progression Components

    /// <summary>
    /// Core progression tracking for creatures
    /// </summary>
    public struct CreatureProgressionComponent : IComponentData
    {
        // Level and Experience
        public int Level;
        public int Experience;
        public int ExperienceToNextLevel;
        public int TotalExperience;

        // Activity-specific experience
        public int RacingExperience;
        public int CombatExperience;
        public int PuzzleExperience;
        public int StrategyExperience;
        public int MusicExperience;
        public int AdventureExperience;
        public int PlatformingExperience;
        public int CraftingExperience;

        // Progression milestones
        public int TotalActivitiesCompleted;
        public int WinsAchieved;
        public int PersonalBests;
        public float HighestPerformanceScore;

        // Skill points for growth
        public int AvailableSkillPoints;
        public int SpentSkillPoints;
    }

    /// <summary>
    /// Skill specialization tracking
    /// </summary>
    public struct CreatureSkillsComponent : IComponentData
    {
        // Racing Skills
        public int SpeedMastery;
        public int AgilityMastery;
        public int EnduranceMastery;

        // Combat Skills
        public int AttackMastery;
        public int DefenseMastery;
        public int TacticalMastery;

        // Puzzle Skills
        public int LogicMastery;
        public int MemoryMastery;
        public int CreativityMastery;

        // Universal Skills
        public int LeadershipMastery;
        public int AdaptabilityMastery;
        public int SocialMastery;

        // Mastery bonuses (calculated)
        public float OverallMasteryBonus;
        public int MasteryLevel;
        public bool HasSpecialization;
    }

    /// <summary>
    /// Achievement tracking system
    /// </summary>
    public struct CreatureAchievementsComponent : IComponentData
    {
        // Activity achievements (bitflags for performance)
        public uint RacingAchievements;
        public uint CombatAchievements;
        public uint PuzzleAchievements;
        public uint StrategyAchievements;
        public uint MiscAchievements;

        // Special milestones
        public bool FirstWin;
        public bool Perfect100;
        public bool Champion;
        public bool Legendary;
        public bool GrandMaster;

        // Progression tracking
        public int TotalAchievements;
        public float AchievementScore;
    }

    /// <summary>
    /// Player progression (town-wide progress)
    /// </summary>
    public struct PlayerProgressionComponent : IComponentData
    {
        // Player level and town development
        public int PlayerLevel;
        public int PlayerExperience;
        public int TownRating;
        public int UnlockedActivities;
        public int UnlockedFeatures;

        // Research and development
        public int ResearchPoints;
        public int TechnologyLevel;
        public uint UnlockedTechnologies;

        // Economy and resources
        public int TotalCurrency;
        public int LifetimeEarnings;
        public int TradingReputation;

        // Population management
        public int MaxCreatures;
        public int BreedingLicense;
        public int FacilityUpgrades;

        // Prestige and rankings
        public int GlobalRanking;
        public int RegionalRanking;
        public int PrestigeLevel;
    }

    #endregion

    #region Progression Systems

    /// <summary>
    /// Core experience and level progression system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ActivityCenterSystem))]
    public partial class ExperienceProgressionSystem : SystemBase
    {
        private EntityQuery progressionQuery;
        private EndSimulationEntityCommandBufferSystem ecbSystem;

        protected override void OnCreate()
        {
            progressionQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<CreatureProgressionComponent>(),
                ComponentType.ReadWrite<CreatureSkillsComponent>(),
                ComponentType.ReadOnly<ActivityParticipantComponent>(),
                ComponentType.ReadOnly<GeneticDataComponent>()
            });

            ecbSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }

        protected override void OnUpdate()
        {
            var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();

            var progressionJob = new ExperienceGainJob
            {
                CommandBuffer = ecb,
                CurrentTime = (float)SystemAPI.Time.ElapsedTime
            };

            Dependency = progressionJob.ScheduleParallel(progressionQuery, Dependency);
            ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }


    public partial struct ExperienceGainJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        public float CurrentTime;

        public void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity,
            ref CreatureProgressionComponent progression,
            ref CreatureSkillsComponent skills,
            in ActivityParticipantComponent activity,
            in GeneticDataComponent genetics)
        {
            // Process experience gain from completed activities
            if (activity.Status == ActivityStatus.Completed && activity.ExperienceGained > 0)
            {
                // Add experience to total and activity-specific pools
                progression.Experience += activity.ExperienceGained;
                progression.TotalExperience += activity.ExperienceGained;

                // Add to activity-specific experience
                AddActivityExperience(ref progression, activity.CurrentActivity, activity.ExperienceGained);

                // Check for level up
                CheckLevelUp(ref progression, ref skills);

                // Update activity completion count
                progression.TotalActivitiesCompleted++;

                // Track performance records
                if (activity.PerformanceScore > progression.HighestPerformanceScore)
                {
                    progression.HighestPerformanceScore = activity.PerformanceScore;
                }

                // Award skill experience based on performance
                AwardSkillExperience(ref skills, activity.CurrentActivity, activity.PerformanceScore, genetics);
            }
        }


        private void AddActivityExperience(ref CreatureProgressionComponent progression, ActivityType activity, int experience)
        {
            switch (activity)
            {
                case ActivityType.Racing:
                    progression.RacingExperience += experience;
                    break;
                case ActivityType.Combat:
                    progression.CombatExperience += experience;
                    break;
                case ActivityType.Puzzle:
                    progression.PuzzleExperience += experience;
                    break;
                case ActivityType.Strategy:
                    progression.StrategyExperience += experience;
                    break;
                case ActivityType.Music:
                    progression.MusicExperience += experience;
                    break;
                case ActivityType.Adventure:
                    progression.AdventureExperience += experience;
                    break;
                case ActivityType.Platforming:
                    progression.PlatformingExperience += experience;
                    break;
                case ActivityType.Crafting:
                    progression.CraftingExperience += experience;
                    break;
            }
        }


        private void CheckLevelUp(ref CreatureProgressionComponent progression, ref CreatureSkillsComponent skills)
        {
            int requiredExperience = CalculateExperienceForLevel(progression.Level + 1);

            if (progression.Experience >= requiredExperience)
            {
                progression.Level++;
                progression.Experience -= requiredExperience;
                progression.ExperienceToNextLevel = CalculateExperienceForLevel(progression.Level + 1);

                // Award skill points on level up
                int skillPointsGained = CalculateSkillPointsForLevel(progression.Level);
                progression.AvailableSkillPoints += skillPointsGained;

                // Update mastery level
                UpdateMasteryLevel(ref skills);
            }
            else
            {
                progression.ExperienceToNextLevel = requiredExperience - progression.Experience;
            }
        }


        private int CalculateExperienceForLevel(int level)
        {
            // Exponential experience curve: 100 * 1.5^level
            return (int)(100f * math.pow(1.5f, level));
        }


        private int CalculateSkillPointsForLevel(int level)
        {
            // More skill points at higher levels
            return math.max(1, level / 5 + 1);
        }


        private void AwardSkillExperience(ref CreatureSkillsComponent skills, ActivityType activity, float performance, GeneticDataComponent genetics)
        {
            int skillGain = (int)(performance * 10f); // Base skill gain

            switch (activity)
            {
                case ActivityType.Racing:
                    skills.SpeedMastery += skillGain;
                    skills.AgilityMastery += skillGain / 2;
                    skills.EnduranceMastery += skillGain / 3;
                    break;

                case ActivityType.Combat:
                    skills.AttackMastery += skillGain;
                    skills.DefenseMastery += skillGain / 2;
                    skills.TacticalMastery += skillGain / 3;
                    break;

                case ActivityType.Puzzle:
                    skills.LogicMastery += skillGain;
                    skills.MemoryMastery += skillGain / 2;
                    skills.CreativityMastery += skillGain / 3;
                    break;

                case ActivityType.Strategy:
                    skills.TacticalMastery += skillGain;
                    skills.LeadershipMastery += skillGain / 2;
                    skills.LogicMastery += skillGain / 3;
                    break;

                default:
                    // Universal skills for other activities
                    skills.AdaptabilityMastery += skillGain / 2;
                    skills.SocialMastery += skillGain / 3;
                    break;
            }

            UpdateMasteryLevel(ref skills);
        }


        private void UpdateMasteryLevel(ref CreatureSkillsComponent skills)
        {
            // Calculate overall mastery from all skill categories
            int totalMastery = skills.SpeedMastery + skills.AgilityMastery + skills.EnduranceMastery +
                              skills.AttackMastery + skills.DefenseMastery + skills.TacticalMastery +
                              skills.LogicMastery + skills.MemoryMastery + skills.CreativityMastery +
                              skills.LeadershipMastery + skills.AdaptabilityMastery + skills.SocialMastery;

            skills.MasteryLevel = totalMastery / 1000; // Every 1000 points = 1 mastery level
            skills.OverallMasteryBonus = skills.MasteryLevel * 0.05f; // 5% bonus per mastery level

            // Check for specialization (having 500+ points in any skill)
            skills.HasSpecialization = skills.SpeedMastery >= 500 || skills.AttackMastery >= 500 ||
                                      skills.LogicMastery >= 500 || skills.TacticalMastery >= 500;
        }
    }

    /// <summary>
    /// Achievement tracking and reward system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(ExperienceProgressionSystem))]
    public partial class AchievementSystem : SystemBase
    {
        private EntityQuery achievementQuery;

        protected override void OnCreate()
        {
            achievementQuery = GetEntityQuery(new ComponentType[]
            {
                ComponentType.ReadWrite<CreatureAchievementsComponent>(),
                ComponentType.ReadOnly<CreatureProgressionComponent>(),
                ComponentType.ReadOnly<ActivityParticipantComponent>()
            });
        }

        protected override void OnUpdate()
        {
            var achievementJob = new AchievementCheckJob();
            Dependency = achievementJob.ScheduleParallel(achievementQuery, Dependency);
        }
    }


    public partial struct AchievementCheckJob : IJobEntity
    {
        public void Execute(ref CreatureAchievementsComponent achievements,
            in CreatureProgressionComponent progression,
            in ActivityParticipantComponent activity)
        {
            // Check for first win achievement
            if (!achievements.FirstWin && progression.WinsAchieved >= 1)
            {
                achievements.FirstWin = true;
                achievements.TotalAchievements++;
            }

            // Check for perfect performance achievement
            if (!achievements.Perfect100 && progression.HighestPerformanceScore >= 2.0f)
            {
                achievements.Perfect100 = true;
                achievements.TotalAchievements++;
            }

            // Check for champion status (100 activities completed)
            if (!achievements.Champion && progression.TotalActivitiesCompleted >= 100)
            {
                achievements.Champion = true;
                achievements.TotalAchievements++;
            }

            // Check for legendary status (level 50+)
            if (!achievements.Legendary && progression.Level >= 50)
            {
                achievements.Legendary = true;
                achievements.TotalAchievements++;
            }

            // Check for grand master status (1000 activities completed)
            if (!achievements.GrandMaster && progression.TotalActivitiesCompleted >= 1000)
            {
                achievements.GrandMaster = true;
                achievements.TotalAchievements++;
            }

            // Update achievement score
            achievements.AchievementScore = CalculateAchievementScore(achievements);
        }


        private float CalculateAchievementScore(CreatureAchievementsComponent achievements)
        {
            float score = achievements.TotalAchievements * 100f;

            // Bonus points for major achievements
            if (achievements.Champion) score += 500f;
            if (achievements.Legendary) score += 1000f;
            if (achievements.GrandMaster) score += 2500f;

            return score;
        }
    }

    /// <summary>
    /// Player-level progression system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class PlayerProgressionSystem : SystemBase
    {
        private EntityQuery playerQuery;
        private EntityQuery allCreaturesQuery;

        protected override void OnCreate()
        {
            playerQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerProgressionComponent>());
            allCreaturesQuery = GetEntityQuery(ComponentType.ReadOnly<CreatureProgressionComponent>());
        }

        protected override void OnUpdate()
        {
            // Update player progression based on creature achievements
            if (playerQuery.IsEmpty) return;

            var playerProgression = SystemAPI.GetSingletonRW<PlayerProgressionComponent>();

            // Calculate town rating from all creatures
            int totalCreatureLevel = 0;
            int creatureCount = 0;
            float totalAchievementScore = 0f;

            foreach (var (progression, achievements) in
                SystemAPI.Query<RefRO<CreatureProgressionComponent>, RefRO<CreatureAchievementsComponent>>())
            {
                totalCreatureLevel += progression.ValueRO.Level;
                totalAchievementScore += achievements.ValueRO.AchievementScore;
                creatureCount++;
            }

            if (creatureCount > 0)
            {
                int averageCreatureLevel = totalCreatureLevel / creatureCount;
                playerProgression.ValueRW.TownRating = (int)(averageCreatureLevel * 10 + totalAchievementScore / 100);

                // Player level based on town rating
                int newPlayerLevel = playerProgression.ValueRO.TownRating / 1000 + 1;
                if (newPlayerLevel > playerProgression.ValueRO.PlayerLevel)
                {
                    playerProgression.ValueRW.PlayerLevel = newPlayerLevel;
                    playerProgression.ValueRW.ResearchPoints += newPlayerLevel * 10;
                }
            }

            // Update maximum creatures based on player level
            playerProgression.ValueRW.MaxCreatures = 10 + playerProgression.ValueRO.PlayerLevel * 5;
        }
    }

    /// <summary>
    /// Skill point allocation system
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class SkillAllocationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // This system would handle automatic or player-directed skill point allocation
            // For now, it's a placeholder for future skill tree implementation

            foreach (var (progression, skills) in
                SystemAPI.Query<RefRW<CreatureProgressionComponent>, RefRW<CreatureSkillsComponent>>())
            {
                // Auto-allocate skill points based on creature genetics and activity preferences
                if (progression.ValueRO.AvailableSkillPoints > 0)
                {
                    // Simple auto-allocation logic (could be expanded)
                    var progressionData = progression.ValueRO;
                    var skillsData = skills.ValueRO;

                    if (progressionData.RacingExperience > progressionData.CombatExperience)
                    {
                        skillsData.SpeedMastery += 5;
                        skillsData.AgilityMastery += 3;
                    }
                    else if (progressionData.CombatExperience > progressionData.PuzzleExperience)
                    {
                        skillsData.AttackMastery += 5;
                        skillsData.DefenseMastery += 3;
                    }
                    else
                    {
                        skillsData.LogicMastery += 5;
                        skillsData.CreativityMastery += 3;
                    }

                    progressionData.AvailableSkillPoints--;
                    progressionData.SpentSkillPoints++;

                    progression.ValueRW = progressionData;
                    skills.ValueRW = skillsData;
                }
            }
        }
    }

    #endregion

    #region Authoring Components

    /// <summary>
    /// MonoBehaviour for creating progression-enabled creatures
    /// </summary>
    public class CreatureProgressionAuthoring : MonoBehaviour
    {
        [Header("Initial Progression")]
        [Range(1, 100)] public int startingLevel = 1;
        public int bonusExperience = 0;
        public bool unlockAllActivities = false;

        [Header("Skill Preferences")]
        public bool autoAllocateSkills = true;
        public SkillFocus primaryFocus = SkillFocus.Balanced;

        public enum SkillFocus
        {
            Balanced,
            Racing,
            Combat,
            Puzzle,
            Strategy,
            Social
        }

        [ContextMenu("Add Progression Components")]
        public void AddProgressionComponents()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world?.IsCreated != true) return;

            var entityManager = world.EntityManager;

            // Find creature entity (would need proper entity linking)
            var entities = entityManager.GetAllEntities(Allocator.Temp);

            foreach (var entity in entities)
            {
                if (entityManager.HasComponent<CreatureData>(entity))
                {
                    // Add progression components
                    entityManager.AddComponentData(entity, new CreatureProgressionComponent
                    {
                        Level = startingLevel,
                        Experience = 0,
                        ExperienceToNextLevel = CalculateExperienceForLevel(startingLevel + 1),
                        TotalExperience = bonusExperience,
                        AvailableSkillPoints = startingLevel,
                        HighestPerformanceScore = 0f
                    });

                    entityManager.AddComponentData(entity, new CreatureSkillsComponent
                    {
                        MasteryLevel = 0,
                        OverallMasteryBonus = 0f,
                        HasSpecialization = false
                    });

                    entityManager.AddComponentData(entity, new CreatureAchievementsComponent
                    {
                        TotalAchievements = 0,
                        AchievementScore = 0f
                    });

                    UnityEngine.Debug.Log($"✅ Added progression components to creature (Level {startingLevel})");
                    break;
                }
            }

            entities.Dispose();
        }

        private int CalculateExperienceForLevel(int level)
        {
            return (int)(100f * math.pow(1.5f, level));
        }

        private void OnDrawGizmos()
        {
            // Draw progression visualization
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.5f);

            // Draw level indicator
            for (int i = 0; i < startingLevel && i < 10; i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(
                    transform.position + Vector3.up * (2.5f + i * 0.2f),
                    transform.position + Vector3.up * (2.5f + i * 0.2f) + Vector3.right * 0.5f
                );
            }
        }
    }

    #endregion
}