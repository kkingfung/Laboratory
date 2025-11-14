using UnityEngine;

namespace Laboratory.Chimera.Progression
{
    /// <summary>
    /// ScriptableObject configuration for monster progression system
    /// Designer-friendly settings for leveling, rewards, and development
    /// </summary>
    [CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Chimera/Progression/Progression Config")]
    public class ProgressionConfig : ScriptableObject
    {
        [Header("Leveling System")]
        [Tooltip("Maximum monster level")]
        public int maxLevel = 100;

        [Tooltip("Base experience required for level 2")]
        public int baseExperiencePerLevel = 100;

        [Tooltip("Experience scaling per level (exponential)")]
        [Range(1f, 2f)]
        public float experienceScaling = 1.15f;

        [Tooltip("Skill points awarded per level")]
        public int skillPointsPerLevel = 1;

        [Tooltip("Bonus skill points at milestone levels (every X levels)")]
        public int milestoneInterval = 10;

        [Tooltip("Bonus skill points at milestones")]
        public int bonusSkillPointsAtMilestone = 3;

        [Header("Stat Bonuses Per Level")]
        [Tooltip("Stat increase percentage per level")]
        [Range(0f, 0.1f)]
        public float statBonusPerLevel = 0.02f; // 2% per level

        [Tooltip("Maximum stat bonus from leveling")]
        [Range(0f, 2f)]
        public float maxStatBonus = 1.0f; // 100% at max level

        [Header("Currency Scaling")]
        [Tooltip("Base coin multiplier for rewards")]
        [Range(0.5f, 2f)]
        public float coinRewardMultiplier = 1.0f;

        [Tooltip("Base gem conversion rate (gems per 100 coins)")]
        public int gemConversionRate = 10;

        [Tooltip("Activity tokens earned per Gold rank")]
        public int tokensPerGoldRank = 5;

        [Tooltip("Activity tokens earned per Platinum rank")]
        public int tokensPerPlatinumRank = 10;

        [Header("Activity Mastery")]
        [Tooltip("Experience per mastery level")]
        public int masteryExperiencePerLevel = 100;

        [Tooltip("Maximum mastery bonus multiplier")]
        [Range(1f, 2f)]
        public float maxMasteryBonus = 1.5f;

        [Header("Daily Challenges")]
        [Tooltip("Number of daily challenges")]
        public int dailyChallengesCount = 3;

        [Tooltip("Daily challenge coin reward")]
        public int dailyChallengeCoins = 500;

        [Tooltip("Daily challenge token reward")]
        public int dailyChallengeTokens = 5;

        [Tooltip("Daily challenge experience reward")]
        public int dailyChallengeExperience = 200;

        [Header("Achievements")]
        [Tooltip("Bonus coins for completing achievements")]
        public int achievementBonusCoins = 1000;

        [Tooltip("Bonus gems for completing achievements")]
        public int achievementBonusGems = 10;

        [Header("Prestige System")]
        [Tooltip("Enable prestige/rebirth system")]
        public bool enablePrestige = true;

        [Tooltip("Prestige level requirement")]
        public int prestigeRequiredLevel = 100;

        [Tooltip("Prestige bonus multiplier per prestige level")]
        [Range(0.05f, 0.5f)]
        public float prestigeBonusPerLevel = 0.1f; // 10% per prestige

        /// <summary>
        /// Calculates experience required for a specific level
        /// </summary>
        public int GetExperienceForLevel(int level)
        {
            if (level <= 1) return 0;

            float totalExp = 0f;
            for (int i = 2; i <= level; i++)
            {
                totalExp += baseExperiencePerLevel * Mathf.Pow(experienceScaling, i - 2);
            }

            return Mathf.RoundToInt(totalExp);
        }

        /// <summary>
        /// Calculates experience needed to reach next level from current level
        /// </summary>
        public int GetExperienceToNextLevel(int currentLevel)
        {
            if (currentLevel >= maxLevel) return 0;

            return Mathf.RoundToInt(baseExperiencePerLevel *
                Mathf.Pow(experienceScaling, currentLevel - 1));
        }

        /// <summary>
        /// Calculates total skill points available at a level
        /// </summary>
        public int GetTotalSkillPoints(int level)
        {
            int basePoints = (level - 1) * skillPointsPerLevel;
            int milestones = (level - 1) / milestoneInterval;
            int bonusPoints = milestones * bonusSkillPointsAtMilestone;

            return basePoints + bonusPoints;
        }

        /// <summary>
        /// Calculates stat bonus at a specific level
        /// </summary>
        public float GetStatBonusAtLevel(int level)
        {
            float bonus = (level - 1) * statBonusPerLevel;
            return Mathf.Min(bonus, maxStatBonus);
        }

        /// <summary>
        /// Gets activity tokens for rank
        /// </summary>
        public int GetActivityTokensForRank(Activities.ActivityResultStatus rank)
        {
            return rank switch
            {
                Activities.ActivityResultStatus.Platinum => tokensPerPlatinumRank,
                Activities.ActivityResultStatus.Gold => tokensPerGoldRank,
                Activities.ActivityResultStatus.Silver => tokensPerGoldRank / 2,
                Activities.ActivityResultStatus.Bronze => tokensPerGoldRank / 4,
                _ => 0
            };
        }

        /// <summary>
        /// Calculates mastery bonus multiplier based on mastery level
        /// </summary>
        public float GetMasteryMultiplier(int masteryLevel)
        {
            if (masteryLevel <= 0) return 1.0f;

            float progress = Mathf.Clamp01((float)masteryLevel / 100f);
            return Mathf.Lerp(1.0f, maxMasteryBonus, progress);
        }

        private void OnValidate()
        {
            maxLevel = Mathf.Max(1, maxLevel);
            baseExperiencePerLevel = Mathf.Max(1, baseExperiencePerLevel);
            masteryExperiencePerLevel = Mathf.Max(1, masteryExperiencePerLevel);
            skillPointsPerLevel = Mathf.Max(0, skillPointsPerLevel);
            milestoneInterval = Mathf.Max(1, milestoneInterval);
            dailyChallengesCount = Mathf.Max(1, dailyChallengesCount);
        }
    }
}
