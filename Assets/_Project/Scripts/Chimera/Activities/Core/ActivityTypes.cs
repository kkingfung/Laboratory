using System;
using Unity.Collections;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Types of activity mini-games available in ChimeraOS
    /// </summary>
    public enum ActivityType : byte
    {
        None = 0,
        Racing = 1,
        Combat = 2,
        Puzzle = 3,
        Strategy = 4,
        Music = 5,
        Adventure = 6,
        Platforming = 7,
        Crafting = 8,
        Exploration = 9,
        Social = 10
    }

    /// <summary>
    /// Difficulty levels for activities
    /// </summary>
    public enum ActivityDifficulty : byte
    {
        Easy = 0,
        Normal = 1,
        Hard = 2,
        Expert = 3,
        Master = 4
    }

    /// <summary>
    /// Result status of an activity attempt
    /// </summary>
    public enum ActivityResultStatus : byte
    {
        Failed = 0,
        Bronze = 1,
        Silver = 2,
        Gold = 3,
        Platinum = 4
    }

    /// <summary>
    /// Performance metrics for activity completion
    /// Result of an activity attempt with performance data
    /// </summary>
    public struct ActivityResult
    {
        public int monsterId;
        public ActivityType activityType;
        public ActivityDifficulty difficulty;
        public ActivityResultStatus status;

        // Performance metrics
        public float completionTime;
        public float accuracyScore;
        public float performanceScore; // 0.0 to 1.0

        // Rewards
        public int coinsEarned;
        public int experienceGained;
        public int tokensEarned;

        // Genetic influence breakdown
        public float strengthContribution;
        public float agilityContribution;
        public float intelligenceContribution;
        public float vitalityContribution;
        public float socialContribution;
        public float adaptabilityContribution;

        public float completedAt;
    }

    /// <summary>
    /// Equipment slot types for activities
    /// </summary>
    public enum EquipmentSlot : byte
    {
        None = 0,
        Head = 1,
        Body = 2,
        Hands = 3,
        Feet = 4,
        Accessory1 = 5,
        Accessory2 = 6,
        Tool = 7
    }

    /// <summary>
    /// Equipment rarity tiers
    /// </summary>
    public enum EquipmentRarity : byte
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// Stats that can be modified by equipment
    /// </summary>
    [Flags]
    public enum StatModifierType : byte
    {
        None = 0,
        Strength = 1 << 0,
        Agility = 1 << 1,
        Intelligence = 1 << 2,
        Vitality = 1 << 3,
        Social = 1 << 4,
        Adaptability = 1 << 5,
        AllStats = Strength | Agility | Intelligence | Vitality | Social | Adaptability
    }

    /// <summary>
    /// Monster experience and mastery data for activities
    /// </summary>
    public struct MonsterActivityProgress
    {
        public int monsterId;
        public ActivityType activityType;

        // Experience system
        public int experiencePoints;
        public int level; // 1-100
        public int attemptsCount;
        public int successCount;

        // Best performances
        public float bestPerformanceScore;
        public float bestCompletionTime;
        public ActivityResultStatus highestRank;

        // Mastery bonuses (unlock at high proficiency)
        public bool hasMasteryBonus;
        public float masteryMultiplier; // 1.0 to 1.5

        public float lastAttemptTime;
    }
}
