using Unity.Entities;
using Unity.Collections;

namespace Laboratory.Chimera.Progression
{
    /// <summary>
    /// Component tracking monster level and experience
    /// </summary>
    public struct MonsterLevelComponent : IComponentData
    {
        public int level;
        public int experiencePoints;
        public int experienceToNextLevel;
        public int totalExperienceGained;
        public int skillPointsAvailable;
        public int skillPointsSpent;

        // Prestige system
        public int prestigeLevel;
        public float prestigeMultiplier;
    }

    /// <summary>
    /// Component tracking stat bonuses from leveling
    /// </summary>
    public struct LevelStatBonusComponent : IComponentData
    {
        public float strengthBonus;
        public float agilityBonus;
        public float intelligenceBonus;
        public float vitalityBonus;
        public float socialBonus;
        public float adaptabilityBonus;
    }

    /// <summary>
    /// Buffer storing unlocked skills
    /// </summary>
    public struct UnlockedSkillElement : IBufferElementData
    {
        public SkillNode skill;
    }

    /// <summary>
    /// Buffer storing completed achievements
    /// </summary>
    public struct AchievementElement : IBufferElementData
    {
        public Achievement achievement;
    }

    /// <summary>
    /// Buffer storing active daily challenges
    /// </summary>
    public struct DailyChallengeElement : IBufferElementData
    {
        public DailyChallenge challenge;
    }

    /// <summary>
    /// Component tracking leaderboard ranking
    /// </summary>
    public struct LeaderboardRankComponent : IComponentData
    {
        public int globalRank;
        public int racingRank;
        public int combatRank;
        public int puzzleRank;
        public float lastUpdated;
    }

    /// <summary>
    /// Component for season pass progress
    /// </summary>
    public struct SeasonPassComponent : IComponentData
    {
        public SeasonPassProgress progress;
    }

    /// <summary>
    /// Request to award experience
    /// </summary>
    public struct AwardExperienceRequest : IComponentData
    {
        public Entity targetEntity;
        public int experienceAmount;
        public float requestTime;
    }

    /// <summary>
    /// Request to award currency
    /// </summary>
    public struct AwardCurrencyRequest : IComponentData
    {
        public Entity targetEntity;
        public CurrencyType currencyType;
        public int amount;
        public float requestTime;
    }

    /// <summary>
    /// Request to unlock skill
    /// </summary>
    public struct UnlockSkillRequest : IComponentData
    {
        public Entity targetEntity;
        public int skillId;
        public float requestTime;
    }

    /// <summary>
    /// Level up event (for notifications)
    /// </summary>
    public struct LevelUpEvent : IComponentData
    {
        public Entity monsterEntity;
        public int oldLevel;
        public int newLevel;
        public int skillPointsAwarded;
        public float eventTime;
    }

    /// <summary>
    /// Achievement completion event
    /// </summary>
    public struct AchievementCompletedEvent : IComponentData
    {
        public Entity monsterEntity;
        public int achievementId;
        public int coinsAwarded;
        public int gemsAwarded;
        public float eventTime;
    }

    /// <summary>
    /// Milestone reached event
    /// </summary>
    public struct MilestoneReachedEvent : IComponentData
    {
        public Entity monsterEntity;
        public int milestoneLevel;
        public MilestoneRewardType rewardType;
        public int rewardValue;
        public float eventTime;
    }

    /// <summary>
    /// Singleton component holding progression system data
    /// </summary>
    public struct ProgressionSystemData : IComponentData
    {
        public bool isInitialized;
        public float currentTime;
        public int totalLevelsGained;
        public int totalExperienceDistributed;
        public int totalCurrencyDistributed;
    }
}
