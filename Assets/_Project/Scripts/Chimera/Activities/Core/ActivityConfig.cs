using UnityEngine;
using System;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// ScriptableObject configuration for activity mini-games
    /// Designer-friendly configuration for all activity types
    /// </summary>
    [CreateAssetMenu(fileName = "ActivityConfig", menuName = "Chimera/Activities/Activity Config")]
    public class ActivityConfig : ScriptableObject
    {
        [Header("Activity Settings")]
        [Tooltip("Type of activity this configuration represents")]
        public ActivityType activityType = ActivityType.Racing;

        [Tooltip("Display name shown to players")]
        public string activityName = "Racing Circuit";

        [TextArea(3, 5)]
        [Tooltip("Description of the activity")]
        public string description = "Test your monster's speed and agility on challenging tracks.";

        [Header("Performance Calculation")]
        [Tooltip("Primary stat weight (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float primaryStatWeight = 0.5f;

        [Tooltip("Secondary stat weight (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float secondaryStatWeight = 0.3f;

        [Tooltip("Tertiary stat weight (0.0 to 1.0)")]
        [Range(0f, 1f)]
        public float tertiaryStatWeight = 0.2f;

        [Header("Difficulty Settings")]
        [Tooltip("Base duration for each difficulty level in seconds")]
        public DifficultyDurations baseDurations = new DifficultyDurations
        {
            easy = 60f,
            normal = 90f,
            hard = 120f,
            expert = 150f,
            master = 180f
        };

        [Tooltip("Performance multipliers per difficulty")]
        public DifficultyMultipliers difficultyMultipliers = new DifficultyMultipliers
        {
            easy = 0.5f,
            normal = 1.0f,
            hard = 1.5f,
            expert = 2.0f,
            master = 3.0f
        };

        [Header("Reward Scaling")]
        [Tooltip("Base coins earned per rank")]
        public RankRewards coinRewards = new RankRewards
        {
            failed = 10,
            bronze = 50,
            silver = 100,
            gold = 200,
            platinum = 500
        };

        [Tooltip("Base experience earned per rank")]
        public RankRewards experienceRewards = new RankRewards
        {
            failed = 5,
            bronze = 25,
            silver = 50,
            gold = 100,
            platinum = 250
        };

        [Tooltip("Activity tokens earned per rank")]
        public RankRewards tokenRewards = new RankRewards
        {
            failed = 0,
            bronze = 1,
            silver = 2,
            gold = 5,
            platinum = 10
        };

        [Header("Rank Thresholds")]
        [Tooltip("Performance score thresholds for each rank (0.0 to 1.0)")]
        public RankThresholds rankThresholds = new RankThresholds
        {
            bronze = 0.4f,
            silver = 0.6f,
            gold = 0.8f,
            platinum = 0.95f
        };

        [Header("Mastery System")]
        [Tooltip("Experience points needed per level")]
        public int experiencePerLevel = 100;

        [Tooltip("Maximum mastery level")]
        public int maxMasteryLevel = 100;

        [Tooltip("Mastery bonus multiplier at max level")]
        [Range(1f, 2f)]
        public float maxMasteryMultiplier = 1.5f;

        [Header("Cross-Activity Benefits")]
        [Tooltip("Activities that provide bonus to this activity")]
        public CrossActivityBonus[] crossActivityBonuses = Array.Empty<CrossActivityBonus>();

        /// <summary>
        /// Gets base duration for specified difficulty
        /// </summary>
        public float GetBaseDuration(ActivityDifficulty difficulty)
        {
            return difficulty switch
            {
                ActivityDifficulty.Easy => baseDurations.easy,
                ActivityDifficulty.Normal => baseDurations.normal,
                ActivityDifficulty.Hard => baseDurations.hard,
                ActivityDifficulty.Expert => baseDurations.expert,
                ActivityDifficulty.Master => baseDurations.master,
                _ => baseDurations.normal
            };
        }

        /// <summary>
        /// Gets difficulty multiplier
        /// </summary>
        public float GetDifficultyMultiplier(ActivityDifficulty difficulty)
        {
            return difficulty switch
            {
                ActivityDifficulty.Easy => difficultyMultipliers.easy,
                ActivityDifficulty.Normal => difficultyMultipliers.normal,
                ActivityDifficulty.Hard => difficultyMultipliers.hard,
                ActivityDifficulty.Expert => difficultyMultipliers.expert,
                ActivityDifficulty.Master => difficultyMultipliers.master,
                _ => difficultyMultipliers.normal
            };
        }

        /// <summary>
        /// Gets coin reward for rank
        /// </summary>
        public int GetCoinReward(ActivityResultStatus rank)
        {
            return rank switch
            {
                ActivityResultStatus.Failed => coinRewards.failed,
                ActivityResultStatus.Bronze => coinRewards.bronze,
                ActivityResultStatus.Silver => coinRewards.silver,
                ActivityResultStatus.Gold => coinRewards.gold,
                ActivityResultStatus.Platinum => coinRewards.platinum,
                _ => coinRewards.failed
            };
        }

        /// <summary>
        /// Gets experience reward for rank
        /// </summary>
        public int GetExperienceReward(ActivityResultStatus rank)
        {
            return rank switch
            {
                ActivityResultStatus.Failed => experienceRewards.failed,
                ActivityResultStatus.Bronze => experienceRewards.bronze,
                ActivityResultStatus.Silver => experienceRewards.silver,
                ActivityResultStatus.Gold => experienceRewards.gold,
                ActivityResultStatus.Platinum => experienceRewards.platinum,
                _ => experienceRewards.failed
            };
        }

        /// <summary>
        /// Gets token reward for rank
        /// </summary>
        public int GetTokenReward(ActivityResultStatus rank)
        {
            return rank switch
            {
                ActivityResultStatus.Failed => tokenRewards.failed,
                ActivityResultStatus.Bronze => tokenRewards.bronze,
                ActivityResultStatus.Silver => tokenRewards.silver,
                ActivityResultStatus.Gold => tokenRewards.gold,
                ActivityResultStatus.Platinum => tokenRewards.platinum,
                _ => tokenRewards.failed
            };
        }

        /// <summary>
        /// Calculates mastery multiplier based on level
        /// </summary>
        public float GetMasteryMultiplier(int masteryLevel)
        {
            if (masteryLevel <= 0) return 1.0f;
            float normalizedLevel = Mathf.Clamp01((float)masteryLevel / maxMasteryLevel);
            return Mathf.Lerp(1.0f, maxMasteryMultiplier, normalizedLevel);
        }
    }

    [Serializable]
    public struct DifficultyDurations
    {
        public float easy;
        public float normal;
        public float hard;
        public float expert;
        public float master;
    }

    [Serializable]
    public struct DifficultyMultipliers
    {
        public float easy;
        public float normal;
        public float hard;
        public float expert;
        public float master;
    }

    [Serializable]
    public struct RankRewards
    {
        public int failed;
        public int bronze;
        public int silver;
        public int gold;
        public int platinum;
    }

    [Serializable]
    public struct RankThresholds
    {
        [Range(0f, 1f)] public float bronze;
        [Range(0f, 1f)] public float silver;
        [Range(0f, 1f)] public float gold;
        [Range(0f, 1f)] public float platinum;
    }

    [Serializable]
    public struct CrossActivityBonus
    {
        public ActivityType sourceActivity;
        [Range(0f, 0.5f)]
        public float bonusMultiplier;
        public string description;
    }
}
