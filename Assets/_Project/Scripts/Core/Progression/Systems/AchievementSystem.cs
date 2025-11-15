using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Laboratory.Core.Progression.Components;
using Laboratory.Core.Activities.Components;

namespace Laboratory.Core.Progression.Systems
{
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
                ComponentType.ReadOnly<CreatureProgressionComponent>()
            });
        }

        protected override void OnUpdate()
        {
            var achievementJob = new AchievementCheckJob();
            Dependency = achievementJob.ScheduleParallel(achievementQuery, Dependency);
        }
    }


    [BurstCompile]
    public partial struct AchievementCheckJob : IJobEntity
    {
        public void Execute(ref CreatureAchievementsComponent achievements,
            in CreatureProgressionComponent progression)
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
}