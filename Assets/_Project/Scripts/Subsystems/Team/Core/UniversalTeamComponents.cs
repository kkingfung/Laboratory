using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace Laboratory.Subsystems.Team.Core
{
    /// <summary>
    /// Universal Team Framework - Cross-Genre Team System for Project Chimera
    /// PURPOSE: Unified team mechanics that work across all 47 game genres
    /// FEATURES: Skill-based matchmaking, role queue, team composition, communication
    /// ARCHITECTURE: ECS-based for 1000+ concurrent team players at 60 FPS
    /// PLAYER-FRIENDLY: Onboarding, tutorials, smart AI assistance, progressive difficulty
    /// </summary>

    #region Core Team Components

    /// <summary>
    /// Team entity - represents a group of players/creatures working together
    /// Works across all genres: Combat, Racing, Puzzle, Exploration, Economics, etc.
    /// </summary>
    public struct TeamComponent : IComponentData
    {
        public FixedString64Bytes TeamName;
        public Entity TeamLeader;
        public TeamType Type;
        public TeamStatus Status;
        public int MaxMembers;
        public int CurrentMembers;
        public float TeamCohesion; // 0-1, how well the team works together
        public float TeamMorale; // 0-1, affects performance
        public int TeamLevel; // Average level of all members
        public float TeamSkillRating; // ELO/MMR for matchmaking
        public uint TeamColorHash; // For visual identification
        public bool IsPublic; // Can others join?
        public bool AllowAutoFill; // Fill empty slots with matchmaking?
        public float FormationTimestamp; // When team was created
    }

    public enum TeamType : byte
    {
        Cooperative = 0,      // PvE teams working together
        Competitive = 1,      // PvP teams against each other
        Mixed = 2,            // Both PvE and PvP elements
        Guild = 3,            // Persistent social team
        Temporary = 4,        // One-off activity team
        Training = 5,         // Practice/tutorial team
        Ranked = 6,           // Competitive ranked play
        Casual = 7            // Casual unranked play
    }

    public enum TeamStatus : byte
    {
        Forming = 0,          // Recruiting members
        Ready = 1,            // All members ready, waiting to start
        Active = 2,           // Currently in activity
        Paused = 3,           // Activity paused
        Completed = 4,        // Activity finished successfully
        Failed = 5,           // Activity failed
        Disbanded = 6         // Team dissolved
    }

    /// <summary>
    /// Team member component - links individual entity to their team
    /// </summary>
    public struct TeamMemberComponent : IComponentData
    {
        public Entity TeamEntity;
        public TeamRole PrimaryRole;
        public TeamRole SecondaryRole;
        public int MemberSlot; // 0-based slot in team (0 = leader)
        public float RoleEfficiency; // 0-1, how well they perform their role
        public float ContributionScore; // Contribution to team objectives
        public bool IsReady; // Ready to start activity?
        public bool IsAI; // AI-controlled member?
        public PlayerSkillLevel SkillLevel;
        public float IndividualSkillRating; // Personal ELO/MMR
        public uint PreferredPlaystyle; // Bitflags for playstyle preferences
        public float JoinTimestamp; // When joined team
    }

    /// <summary>
    /// Universal role system that adapts to genre
    /// Combat: Tank/DPS/Healer | Racing: Driver/Navigator | Puzzle: Solver/Coordinator
    /// </summary>
    public enum TeamRole : byte
    {
        // Universal roles
        Leader = 0,           // Team coordinator
        Support = 1,          // Assists others
        Specialist = 2,       // Focused expertise
        Generalist = 3,       // Flexible all-rounder

        // Combat roles
        Tank = 10,            // Damage absorption
        DPS = 11,             // Damage dealer
        Healer = 12,          // Support/healing
        Crowd_Control = 13,   // Disable enemies

        // Racing roles
        Driver = 20,          // Main racer
        Navigator = 21,       // Route planning
        Mechanic = 22,        // Vehicle optimization

        // Puzzle roles
        Solver = 30,          // Problem solving
        Coordinator = 31,     // Team coordination
        Resource_Manager = 32,// Resource optimization

        // Exploration roles
        Scout = 40,           // Pathfinding
        Collector = 41,       // Resource gathering
        Cartographer = 42,    // Map creation

        // Economics roles
        Trader = 50,          // Trading specialist
        Crafter = 51,         // Production specialist
        Merchant = 52,        // Sales specialist

        // Strategy roles
        Commander = 60,       // Strategic planning
        Tactician = 61,       // Tactical execution
        Builder = 62          // Base/resource building
    }

    public enum PlayerSkillLevel : byte
    {
        Tutorial = 0,         // First-time player, needs guidance
        Beginner = 1,         // 0-10 hours played
        Novice = 2,           // 10-50 hours
        Intermediate = 3,     // 50-200 hours
        Advanced = 4,         // 200-500 hours
        Expert = 5,           // 500-1000 hours
        Master = 6,           // 1000+ hours
        Legend = 7            // Top 1% of players
    }

    /// <summary>
    /// Team objective - what the team is trying to accomplish
    /// Genre-agnostic objective system
    /// </summary>
    public struct TeamObjectiveComponent : IComponentData
    {
        public ObjectiveType Type;
        public ObjectiveStatus Status;
        public float Progress; // 0-1
        public float TimeRemaining; // -1 for unlimited
        public int SuccessThreshold; // Minimum score/completion for success
        public int CurrentScore;
        public bool RequiresAllMembers; // All members must participate?
        public Entity TargetEntity; // Objective target (if applicable)
        public float3 TargetLocation; // Objective location (if applicable)
    }

    public enum ObjectiveType : byte
    {
        // Combat objectives
        Defeat_Enemies = 0,
        Survive_Duration = 1,
        Protect_Target = 2,
        Capture_Point = 3,

        // Racing objectives
        Finish_First = 10,
        Beat_Time = 11,
        Collect_Items = 12,
        Complete_Laps = 13,

        // Puzzle objectives
        Solve_Puzzle = 20,
        Reach_Goal = 21,
        Collect_All = 22,
        Coordinate_Actions = 23,

        // Exploration objectives
        Discover_Location = 30,
        Map_Area = 31,
        Collect_Resources = 32,
        Escort_Target = 33,

        // Economics objectives
        Earn_Currency = 40,
        Trade_Items = 41,
        Craft_Items = 42,
        Build_Structure = 43,

        // Strategy objectives
        Conquer_Territory = 50,
        Defend_Base = 51,
        Resource_Control = 52,
        Tech_Advancement = 53
    }

    public enum ObjectiveStatus : byte
    {
        NotStarted = 0,
        InProgress = 1,
        Completed = 2,
        Failed = 3,
        Bonus_Completed = 4  // Extra objectives
    }

    /// <summary>
    /// Team communication component - pings, markers, quick chat
    /// </summary>
    public struct TeamCommunicationComponent : IBufferElementData
    {
        public CommunicationType Type;
        public Entity Sender;
        public float3 WorldPosition;
        public Entity TargetEntity;
        public FixedString64Bytes Message;
        public float Timestamp;
        public float Duration; // How long to display
        public float Urgency; // 0-1, affects visual presentation
    }

    public enum CommunicationType : byte
    {
        // Strategic markers
        Ping_Location = 0,
        Ping_Enemy = 1,
        Ping_Objective = 2,
        Ping_Danger = 3,
        Ping_Help = 4,
        Ping_Retreat = 5,
        Ping_Attack = 6,
        Ping_Defend = 7,

        // Quick chat
        Chat_Yes = 10,
        Chat_No = 11,
        Chat_Thanks = 12,
        Chat_Sorry = 13,
        Chat_Good_Job = 14,
        Chat_Need_Help = 15,

        // Tactical commands
        Command_Follow = 20,
        Command_Hold = 21,
        Command_Advance = 22,
        Command_Regroup = 23,
        Command_Formation = 24
    }

    /// <summary>
    /// Team composition tracker - ensures balanced teams
    /// </summary>
    public struct TeamCompositionComponent : IComponentData
    {
        public int TankCount;
        public int DPSCount;
        public int HealerCount;
        public int SupportCount;
        public int SpecialistCount;
        public float CompositionBalance; // 0-1, how balanced is team?
        public bool MeetsMinimumRequirements;
        public CompositionWarnings ActiveWarnings;
    }

    [System.Flags]
    public enum CompositionWarnings : uint
    {
        None = 0,
        No_Healer = 1 << 0,
        No_Tank = 1 << 1,
        No_DPS = 1 << 2,
        Too_Many_Same_Role = 1 << 3,
        Skill_Mismatch = 1 << 4,
        Level_Gap_Too_Large = 1 << 5,
        Insufficient_Members = 1 << 6
    }

    #endregion

    #region Matchmaking Components

    /// <summary>
    /// Matchmaking queue component - players waiting for teams
    /// </summary>
    public struct MatchmakingQueueComponent : IComponentData
    {
        public Entity PlayerEntity;
        public TeamRole DesiredRole;
        public TeamType DesiredTeamType;
        public float SkillRating;
        public PlayerSkillLevel SkillLevel;
        public float QueueStartTime;
        public float MaxWaitTime; // Give up after this
        public MatchmakingPreferences Preferences;
        public int PreferredTeamSize;
        public bool AcceptBackfill; // Fill into existing teams?
    }

    [System.Flags]
    public enum MatchmakingPreferences : uint
    {
        None = 0,
        Strict_Skill_Matching = 1 << 0,     // Only match similar skill
        Voice_Chat_Only = 1 << 1,            // Prefer voice-enabled teams
        Beginner_Friendly = 1 << 2,          // Patient with new players
        Competitive_Focus = 1 << 3,          // Win-oriented
        Casual_Focus = 1 << 4,               // Fun-oriented
        Same_Language = 1 << 5,              // Language preference
        Same_Region = 1 << 6,                // Region/ping preference
        Friends_Only = 1 << 7                // Only team with friends
    }

    /// <summary>
    /// Player skill rating component - ELO/MMR system
    /// </summary>
    public struct PlayerSkillRatingComponent : IComponentData
    {
        public float OverallRating; // General skill (1000-3000)
        public float CombatRating;
        public float RacingRating;
        public float PuzzleRating;
        public float StrategyRating;
        public float CooperationRating; // How well they work with teams
        public int TotalMatches;
        public int Wins;
        public int Losses;
        public float WinRate;
        public int WinStreak;
        public int LossStreak;
        public float PerformanceHistory; // Recent performance trend
        public bool IsCalibrating; // Still determining skill level?
        public int CalibrationMatchesRemaining;
    }

    /// <summary>
    /// Matchmaking result - successful team match
    /// </summary>
    public struct MatchmakingResultComponent : IComponentData
    {
        public Entity TeamEntity;
        public float MatchQuality; // 0-1, how well-balanced is match?
        public float AverageWaitTime;
        public float SkillVariance; // How spread out are skill levels?
        public bool BackfillMatch; // Was this backfilling an existing team?
    }

    #endregion

    #region Tutorial & Onboarding Components

    /// <summary>
    /// Tutorial progress tracking
    /// </summary>
    public struct TutorialProgressComponent : IComponentData
    {
        public TutorialStage CurrentStage;
        public float StageProgress; // 0-1
        public bool ShowHints;
        public bool ShowAdvancedTips;
        public TutorialCompletionFlags CompletedTutorials;
        public int TotalHintsShown;
        public int TotalMistakesMade;
        public float TutorialStartTime;
    }

    public enum TutorialStage : byte
    {
        Welcome = 0,
        Basic_Controls = 1,
        Team_Joining = 2,
        Role_Selection = 3,
        Basic_Teamwork = 4,
        Communication = 5,
        Objectives = 6,
        Advanced_Tactics = 7,
        Graduation = 8,
        Completed = 9
    }

    [System.Flags]
    public enum TutorialCompletionFlags : uint
    {
        None = 0,
        Joined_Team = 1 << 0,
        Selected_Role = 1 << 1,
        Used_Ping = 1 << 2,
        Used_Quick_Chat = 1 << 3,
        Completed_Objective = 1 << 4,
        Won_Match = 1 << 5,
        Used_Formation = 1 << 6,
        Helped_Teammate = 1 << 7,
        Combat_Basics = 1 << 8,
        Racing_Basics = 1 << 9,
        Puzzle_Basics = 1 << 10,
        Strategy_Basics = 1 << 11
    }

    /// <summary>
    /// Adaptive tutorial component - adjusts to player performance
    /// </summary>
    public struct AdaptiveTutorialComponent : IComponentData
    {
        public float LearningSpeed; // 0-1, how fast player picks things up
        public int RepetitionsNeeded; // How many times to show each concept
        public bool SkipBasics; // Experienced player, skip intro
        public DifficultyLevel CurrentDifficulty;
        public float SuccessRate; // 0-1, recent task success rate
        public int ConsecutiveFailures;
        public bool NeedsExtraHelp;
    }

    public enum DifficultyLevel : byte
    {
        Tutorial = 0,         // Hand-holding mode
        VeryEasy = 1,
        Easy = 2,
        Normal = 3,
        Hard = 4,
        VeryHard = 5,
        Expert = 6,
        Master = 7,
        Custom = 8            // Player-defined difficulty
    }

    /// <summary>
    /// Hint system component - contextual help for players
    /// </summary>
    public struct HintSystemComponent : IBufferElementData
    {
        public HintType Type;
        public FixedString128Bytes HintText;
        public float3 WorldPosition; // Where to show hint
        public Entity TargetEntity; // What the hint is about
        public float Priority; // 0-1, higher = more important
        public float DisplayDuration;
        public bool Dismissible;
        public bool OnlyShowOnce;
        public uint HintId; // Track if already shown
    }

    public enum HintType : byte
    {
        Control_Hint = 0,
        Role_Hint = 1,
        Objective_Hint = 2,
        Teamwork_Hint = 3,
        Strategy_Hint = 4,
        Warning = 5,
        Tip = 6,
        Achievement = 7
    }

    #endregion

    #region Team Performance & Feedback Components

    /// <summary>
    /// Team performance metrics - track team effectiveness
    /// </summary>
    public struct TeamPerformanceComponent : IComponentData
    {
        public float CombinedDPS; // Damage per second (combat)
        public float CombinedHealing; // Healing per second (combat)
        public float AverageSpeed; // Speed (racing)
        public float PuzzlesSolved; // Puzzles solved (puzzle)
        public float ResourcesGathered; // Resources (exploration/economics)
        public float ObjectivesCompleted;
        public float TeamKDA; // Kill/Death/Assist ratio (combat)
        public float SynergyBonus; // Bonus from good teamwork
        public float CommunicationScore; // How well team communicates
        public float FormationEfficiency; // Formation usage effectiveness
    }

    /// <summary>
    /// Post-match feedback component - learning and improvement
    /// </summary>
    public struct PostMatchFeedbackComponent : IComponentData
    {
        public float MatchDuration;
        public bool Victory;
        public float PersonalPerformanceScore; // 0-100
        public float TeamPerformanceScore; // 0-100
        public float MVPScore; // Most valuable player score
        public TeamRole PerformedBestAs; // Which role they excelled at
        public FixedString128Bytes StrengthFeedback; // "Great healing!"
        public FixedString128Bytes ImprovementFeedback; // "Try more pings"
        public int SkillRatingChange; // +/- rating change
        public bool EarnedNewRank;
        public RecommendedFocus NextFocus; // What to work on
    }

    public enum RecommendedFocus : byte
    {
        Communication = 0,
        Teamwork = 1,
        Mechanics = 2,
        Strategy = 3,
        Role_Mastery = 4,
        Versatility = 5
    }

    #endregion

    #region Team Resource Sharing Components

    /// <summary>
    /// Team resource pool - shared resources across genres
    /// </summary>
    public struct TeamResourcePoolComponent : IComponentData
    {
        public float SharedCurrency;
        public float SharedEnergy;
        public float SharedHealth;
        public float SharedExperience;
        public int SharedInventorySlots;
        public bool AllowResourceSharing;
        public ResourceSharingPolicy SharingPolicy;
    }

    public enum ResourceSharingPolicy : byte
    {
        Equal_Distribution = 0,      // Everyone gets equal share
        Need_Based = 1,               // Based on current need
        Merit_Based = 2,              // Based on contribution
        Leader_Controlled = 3,        // Leader decides
        Democratic_Vote = 4,          // Team votes on distribution
        Automatic_Smart = 5           // AI decides optimal distribution
    }

    /// <summary>
    /// Individual resource contribution tracking
    /// </summary>
    public struct ResourceContributionComponent : IComponentData
    {
        public float CurrencyContributed;
        public float EnergyContributed;
        public float ItemsContributed;
        public float AssistsProvided;
        public float ResourcesShared;
        public float ContributionRatio; // Personal contribution / team average
    }

    #endregion
}
