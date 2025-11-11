using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Laboratory.Core.Progression.Components;
using Laboratory.Core.ECS.Components;
using Laboratory.Core.Activities.Components;
using Laboratory.Core.Activities.Types;

namespace Laboratory.Core.Progression.Jobs
{

    [BurstCompile]
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
}