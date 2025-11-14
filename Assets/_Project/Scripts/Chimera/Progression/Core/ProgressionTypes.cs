using System;
using Unity.Collections;
using Laboratory.Chimera.Activities;

namespace Laboratory.Chimera.Progression
{
    /// <summary>
    /// Currency types in ChimeraOS
    /// </summary>
    public enum CurrencyType : byte
    {
        Coins = 0,          // Universal currency from activities
        Gems = 1,           // Premium currency for special items
        ActivityTokens = 2, // Unlock advanced activities/tournaments
        CraftingMaterials = 3 // Materials for equipment crafting
    }

    /// <summary>
    /// Monster development stat types
    /// </summary>
    public enum DevelopmentStat : byte
    {
        ExperiencePoints = 0,
        Level = 1,
        SkillPoints = 2,
        ActivityMastery = 3,
        OverallRating = 4
    }

    /// <summary>
    /// Skill tree categories
    /// </summary>
    public enum SkillCategory : byte
    {
        Combat = 0,
        Agility = 1,
        Intelligence = 2,
        Vitality = 3,
        Social = 4,
        Utility = 5
    }

    /// <summary>
    /// Achievement types
    /// </summary>
    public enum AchievementType : byte
    {
        ActivityCompletion = 0,
        PerfectScore = 1,
        MasteryLevel = 2,
        Breeding = 3,
        Collection = 4,
        Social = 5
    }

    /// <summary>
    /// Monster level and experience data
    /// </summary>
    [Serializable]
    public struct MonsterLevel
    {
        public int level;
        public int experiencePoints;
        public int experienceToNextLevel;
        public int totalExperienceGained;

        // Stat bonuses from leveling
        public float strengthBonus;
        public float agilityBonus;
        public float intelligenceBonus;
        public float vitalityBonus;
        public float socialBonus;
        public float adaptabilityBonus;
    }

    /// <summary>
    /// Skill tree node data
    /// </summary>
    [Serializable]
    public struct SkillNode
    {
        public int skillId;
        public FixedString64Bytes skillName;
        public SkillCategory category;
        public int requiredLevel;
        public int requiredSkillPoints;
        public int currentRank;
        public int maxRank;
        public bool isUnlocked;

        // Skill effects
        public StatModifierType affectedStats;
        public float bonusPerRank;
    }

    /// <summary>
    /// Achievement data
    /// </summary>
    [Serializable]
    public struct Achievement
    {
        public int achievementId;
        public FixedString64Bytes achievementName;
        public AchievementType type;
        public float progress; // 0.0 to 1.0
        public bool isCompleted;
        public int rewardCoins;
        public int rewardGems;
        public float completedAt;
    }

    /// <summary>
    /// Daily quest/challenge data
    /// </summary>
    [Serializable]
    public struct DailyChallenge
    {
        public int challengeId;
        public FixedString128Bytes description;
        public ActivityType targetActivity;
        public int targetCount;
        public int currentProgress;
        public bool isCompleted;

        // Rewards
        public int rewardCoins;
        public int rewardTokens;
        public int rewardExperience;

        public float expirationTime;
    }

    /// <summary>
    /// Reward bundle for activities/quests
    /// </summary>
    [Serializable]
    public struct RewardBundle
    {
        public int coins;
        public int gems;
        public int activityTokens;
        public int experience;
        public int skillPoints;

        // Equipment rewards
        public FixedList32Bytes<int> equipmentItemIds;

        // Material rewards
        public FixedList32Bytes<CraftingMaterialReward> materials;
    }

    /// <summary>
    /// Crafting material reward
    /// </summary>
    [Serializable]
    public struct CraftingMaterialReward
    {
        public int materialId;
        public int quantity;
    }

    /// <summary>
    /// Monster progression milestone (unlock features at levels)
    /// </summary>
    [Serializable]
    public struct ProgressionMilestone
    {
        public int level;
        public FixedString128Bytes milestoneDescription;
        public MilestoneRewardType rewardType;
        public int rewardValue;
    }

    /// <summary>
    /// Milestone reward types
    /// </summary>
    public enum MilestoneRewardType : byte
    {
        None = 0,
        SkillPoints = 1,
        EquipmentSlot = 2,
        ActivityUnlock = 3,
        StatBonus = 4
    }

    /// <summary>
    /// Leaderboard entry
    /// </summary>
    [Serializable]
    public struct LeaderboardEntry
    {
        public int monsterId;
        public FixedString64Bytes monsterName;
        public ActivityType activityType;
        public float bestScore;
        public int rank;
        public float lastUpdated;
    }

    /// <summary>
    /// Season pass progress
    /// </summary>
    [Serializable]
    public struct SeasonPassProgress
    {
        public int currentLevel;
        public int experiencePoints;
        public int experienceToNextLevel;
        public bool isPremiumActive;
        public float seasonEndTime;

        // Rewards claimed
        public int freeRewardsClaimed;
        public int premiumRewardsClaimed;
    }
}
