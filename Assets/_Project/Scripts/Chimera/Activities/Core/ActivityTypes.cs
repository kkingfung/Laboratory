using System;
using Unity.Collections;

namespace Laboratory.Chimera.Activities
{
    /// <summary>
    /// Types of activity mini-games available in ChimeraOS
    /// Expanded to support 47 distinct game genres
    /// </summary>
    public enum ActivityType : byte
    {
        // ===== CORE SYSTEM =====
        None = 0,

        // ===== ACTION GENRES (7) =====
        FPS = 1,
        ThirdPersonShooter = 2,
        Fighting = 3,
        BeatEmUp = 4,
        HackAndSlash = 5,
        Stealth = 6,
        SurvivalHorror = 7,

        // ===== STRATEGY GENRES (5) =====
        RealTimeStrategy = 8,
        TurnBasedStrategy = 9,
        FourXStrategy = 10,
        GrandStrategy = 11,
        AutoBattler = 12,

        // ===== PUZZLE GENRES (5) =====
        Match3 = 13,
        TetrisLike = 14,
        PhysicsPuzzle = 15,
        HiddenObject = 16,
        WordGame = 17,

        // ===== ADVENTURE GENRES (4) =====
        PointAndClick = 18,
        VisualNovel = 19,
        WalkingSimulator = 20,
        Metroidvania = 21,

        // ===== PLATFORM GENRES (3) =====
        Platformer2D = 22,
        Platformer3D = 23,
        EndlessRunner = 24,

        // ===== SIMULATION GENRES (4) =====
        VehicleSimulation = 25,
        FlightSimulator = 26,
        FarmingSimulator = 27,
        ConstructionSimulator = 28,

        // ===== ARCADE GENRES (4) =====
        Roguelike = 29,
        Roguelite = 30,
        BulletHell = 31,
        ClassicArcade = 32,

        // ===== BOARD & CARD GENRES (3) =====
        BoardGame = 33,
        CardGame = 34,
        ChessLike = 35,

        // ===== CORE ACTIVITY GENRES (10) =====
        Exploration = 36,
        Racing = 37,
        TowerDefense = 38,
        BattleRoyale = 39,
        CityBuilder = 40,
        Detective = 41,
        Economics = 42,
        Sports = 43,

        // ===== MUSIC GENRES (2) =====
        RhythmGame = 44,
        MusicCreation = 45,

        // ===== LEGACY SUPPORT =====
        Combat = 46,
        Puzzle = 47,
        Strategy = 48,
        Music = 49,
        Adventure = 50,
        Platforming = 51,
        Crafting = 52,
        Social = 53,
        Training = 54,
        Breeding = 55,
        Foraging = 56,
        Resting = 57,

        Custom = 255
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
