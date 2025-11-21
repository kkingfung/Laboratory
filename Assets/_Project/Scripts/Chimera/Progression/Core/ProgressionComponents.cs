using Unity.Entities;
using Unity.Collections;
using UnityEngine;

namespace Laboratory.Chimera.Progression
{
    /// <summary>
    /// PARTNERSHIP SKILL COMPONENT - Tracks player+chimera mastery in 47 genres
    /// NO LEVELS! Skill improves through actual gameplay practice.
    /// Victory comes from player skill + chimera cooperation, not stats.
    /// </summary>
    public struct PartnershipSkillComponent : IComponentData
    {
        // Genre Mastery (0.0 to 1.0) - Improves through practice, not XP
        public float actionMastery;      // FPS, Fighting, Platformers, etc.
        public float strategyMastery;    // RTS, TBS, 4X, etc.
        public float puzzleMastery;      // Match-3, Physics puzzles, etc.
        public float racingMastery;      // Racing, flight sims, etc.
        public float rhythmMastery;      // Rhythm games, music creation
        public float explorationMastery; // Adventure, Metroidvania, etc.
        public float economicsMastery;   // Trading, crafting, city building

        // Partnership Quality (0.0 to 1.0) - How well player & chimera work together
        public float cooperationLevel;   // Cooperation in activities
        public float trustLevel;         // Built through consistent care
        public float understandingLevel; // Learning chimera's personality

        // Activity Participation (counts, not levels)
        public int totalActivitiesCompleted;
        public int genresExplored; // How many of 47 genres tried

        // Recent Performance (moving average)
        public float recentSuccessRate; // Last 10 activities
        public float improvementTrend;  // Getting better or worse?
    }

    /// <summary>
    /// DEPRECATED - Kept for migration compatibility only
    /// Use PartnershipSkillComponent instead
    /// </summary>
    [System.Obsolete("Use PartnershipSkillComponent instead - no more levels!")]
    public struct MonsterLevelComponent : IComponentData
    {
        public int level;
        public int experiencePoints;
        public int experienceToNextLevel;
        public int totalExperienceGained;
        public int skillPointsAvailable;
        public int skillPointsSpent;
        public int prestigeLevel;
        public float prestigeMultiplier;
    }

    /// <summary>
    /// DEPRECATED - No stat bonuses from levels!
    /// Equipment affects personality > stats. Chimera personality affects cooperation.
    /// </summary>
    [System.Obsolete("No level-based stat bonuses in new system")]
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
    /// DEPRECATED - No XP in new system!
    /// Use RecordSkillImprovement instead
    /// </summary>
    [System.Obsolete("Use RecordSkillImprovement instead - no XP system")]
    public struct AwardExperienceRequest : IComponentData
    {
        public Entity targetEntity;
        public int experienceAmount;
        public float requestTime;
    }

    /// <summary>
    /// Request to record skill improvement from activity completion
    /// Replaces XP system - skill improves through DOING, not arbitrary points
    /// </summary>
    public struct RecordSkillImprovementRequest : IComponentData
    {
        public Entity partnershipEntity;    // Player-Chimera partnership
        public ActivityGenreCategory genre; // Which genre was practiced
        public float performanceQuality;    // How well did they do? (0.0-1.0)
        public bool cooperatedWell;         // Did chimera cooperate with player?
        public float activityDuration;      // Time spent (more practice = more improvement)
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
    /// DEPRECATED - No level-ups in new system!
    /// </summary>
    [System.Obsolete("No level system - use SkillMilestoneReachedEvent")]
    public struct LevelUpEvent : IComponentData
    {
        public Entity monsterEntity;
        public int oldLevel;
        public int newLevel;
        public int skillPointsAwarded;
        public float eventTime;
    }

    /// <summary>
    /// Skill milestone reached event (replaces level-ups)
    /// Example: First time reaching 50% mastery in a genre
    /// </summary>
    public struct SkillMilestoneReachedEvent : IComponentData
    {
        public Entity partnershipEntity;
        public ActivityGenreCategory genre;
        public SkillMilestoneType milestoneType; // FirstActivity, Competent, Proficient, Expert, Master
        public float masteryLevel; // Current mastery (0.0-1.0)
        public float eventTime;
        // Cosmetic rewards only!
        public FixedString64Bytes cosmeticRewardDescription; // "Unlocked racing outfit"
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
        public int totalLevelsGained; // Legacy - deprecated
        public int totalExperienceDistributed; // Legacy - deprecated
        public int totalCurrencyDistributed;

        // New partnership metrics
        public int totalSkillImprovementsRecorded;
        public int totalMilestonesReached;
        public int totalPartnershipsFormed;
    }

    /// <summary>
    /// Activity genre categories for skill tracking
    /// </summary>
    public enum ActivityGenreCategory
    {
        Action = 0,      // FPS, Fighting, Platformers, etc.
        Strategy = 1,    // RTS, TBS, 4X, etc.
        Puzzle = 2,      // Match-3, Physics puzzles, etc.
        Racing = 3,      // Racing, flight sims, etc.
        Rhythm = 4,      // Rhythm games, music creation
        Exploration = 5, // Adventure, Metroidvania, etc.
        Economics = 6    // Trading, crafting, city building
    }

    /// <summary>
    /// Skill milestone types (replaces arbitrary levels)
    /// </summary>
    public enum SkillMilestoneType
    {
        FirstActivity = 0,    // First time trying this genre
        Beginner = 1,         // 10% mastery
        Competent = 2,        // 25% mastery
        Proficient = 3,       // 50% mastery
        Expert = 4,           // 75% mastery
        Master = 5            // 90%+ mastery
    }
}
