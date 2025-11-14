using Unity.Entities;
using Unity.Collections;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Tag component indicating entity can participate in activities
    /// </summary>
    public struct ActivityParticipantTag : IComponentData
    {
    }

    /// <summary>
    /// Component tracking monster's current activity participation
    /// </summary>
    public struct ActiveActivityComponent : IComponentData
    {
        public ActivityType currentActivity;
        public ActivityDifficulty difficulty;
        public float startTime;
        public float duration;
        public bool isComplete;
        public float performanceScore;
    }

    /// <summary>
    /// Component storing monster's activity progress and mastery levels
    /// Buffer element for storing progress in multiple activities
    /// </summary>
    public struct ActivityProgressElement : IBufferElementData
    {
        public ActivityType activityType;
        public int experiencePoints;
        public int level;
        public int attemptsCount;
        public int successCount;
        public float bestPerformanceScore;
        public float bestCompletionTime;
        public ActivityResultStatus highestRank;
        public float masteryMultiplier;
        public float lastAttemptTime;
    }

    /// <summary>
    /// Component storing activity results for processing rewards
    /// </summary>
    public struct ActivityResultComponent : IComponentData
    {
        public int monsterId;
        public ActivityType activityType;
        public ActivityDifficulty difficulty;
        public ActivityResultStatus status;
        public float completionTime;
        public float performanceScore;
        public int coinsEarned;
        public int experienceGained;
        public int tokensEarned;
        public float completedAt;
    }

    /// <summary>
    /// Buffer element for equipped items (equipment system integration)
    /// </summary>
    public struct EquippedItemElement : IBufferElementData
    {
        public EquipmentSlot slot;
        public int itemId;
        public EquipmentRarity rarity;
        public StatModifierType modifierType;
        public float bonusValue;
        public ActivityType activityBonus; // Which activity this equipment helps
    }

    /// <summary>
    /// Component tracking monster's currency and tokens
    /// </summary>
    public struct CurrencyComponent : IComponentData
    {
        public int coins;
        public int gems;
        public int activityTokens;
    }

    /// <summary>
    /// Request to start an activity (used by game systems)
    /// </summary>
    public struct StartActivityRequest : IComponentData
    {
        public Entity monsterEntity;
        public ActivityType activityType;
        public ActivityDifficulty difficulty;
        public float requestTime;
    }

    /// <summary>
    /// Singleton component holding activity configurations
    /// </summary>
    public struct ActivitySystemData : IComponentData
    {
        public bool isInitialized;
        public float currentTime;
        public int totalActivitiesCompleted;
        public int totalRewardsDistributed;
    }
}
