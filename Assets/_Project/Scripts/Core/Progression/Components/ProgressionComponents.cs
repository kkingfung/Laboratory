using Unity.Entities;

namespace Laboratory.Core.Progression.Components
{
    /// <summary>
    /// Core progression tracking for creatures
    /// </summary>
    public struct CreatureProgressionComponent : IComponentData
    {
        // Level and Experience
        public int Level;
        public int Experience;
        public int ExperienceToNextLevel;
        public int TotalExperience;

        // Activity-specific experience
        public int RacingExperience;
        public int CombatExperience;
        public int PuzzleExperience;
        public int StrategyExperience;
        public int MusicExperience;
        public int AdventureExperience;
        public int PlatformingExperience;
        public int CraftingExperience;

        // Progression milestones
        public int TotalActivitiesCompleted;
        public int WinsAchieved;
        public int PersonalBests;
        public float HighestPerformanceScore;

        // Skill points for growth
        public int AvailableSkillPoints;
        public int SpentSkillPoints;
    }

    /// <summary>
    /// Skill specialization tracking
    /// </summary>
    public struct CreatureSkillsComponent : IComponentData
    {
        // Racing Skills
        public int SpeedMastery;
        public int AgilityMastery;
        public int EnduranceMastery;

        // Combat Skills
        public int AttackMastery;
        public int DefenseMastery;
        public int TacticalMastery;

        // Puzzle Skills
        public int LogicMastery;
        public int MemoryMastery;
        public int CreativityMastery;

        // Universal Skills
        public int LeadershipMastery;
        public int AdaptabilityMastery;
        public int SocialMastery;

        // Mastery bonuses (calculated)
        public float OverallMasteryBonus;
        public int MasteryLevel;
        public bool HasSpecialization;
    }

    /// <summary>
    /// Achievement tracking system
    /// </summary>
    public struct CreatureAchievementsComponent : IComponentData
    {
        // Activity achievements (bitflags for performance)
        public uint RacingAchievements;
        public uint CombatAchievements;
        public uint PuzzleAchievements;
        public uint StrategyAchievements;
        public uint MiscAchievements;

        // Special milestones
        public bool FirstWin;
        public bool Perfect100;
        public bool Champion;
        public bool Legendary;
        public bool GrandMaster;

        // Progression tracking
        public int TotalAchievements;
        public float AchievementScore;
    }

    /// <summary>
    /// Player progression (town-wide progress)
    /// </summary>
    public struct PlayerProgressionComponent : IComponentData
    {
        // Player level and town development
        public int PlayerLevel;
        public int PlayerExperience;
        public int TownRating;
        public int UnlockedActivities;
        public int UnlockedFeatures;

        // Research and development
        public int ResearchPoints;
        public int TechnologyLevel;
        public uint UnlockedTechnologies;

        // Economy and resources
        public int TotalCurrency;
        public int LifetimeEarnings;
        public int TradingReputation;

        // Population management
        public int MaxCreatures;
        public int BreedingLicense;
        public int FacilityUpgrades;

        // Prestige and rankings
        public int GlobalRanking;
        public int RegionalRanking;
        public int PrestigeLevel;
    }
}