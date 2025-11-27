namespace Laboratory.Core.Activities.Types
{
    /// <summary>
    /// Types of activities that creatures can participate in
    /// Supports 47 distinct game genres for comprehensive gameplay variety
    /// </summary>
    public enum ActivityType : byte
    {
        // ===== CORE SYSTEM =====
        /// <summary>No activity</summary>
        None = 0,

        // ===== ACTION GENRES (7) =====
        /// <summary>First-Person Shooter - Precision shooting from first-person perspective</summary>
        FPS = 1,
        /// <summary>Third-Person Shooter - Cover-based shooting with tactical positioning</summary>
        ThirdPersonShooter = 2,
        /// <summary>Fighting - Close-combat fighting with combos and special moves</summary>
        Fighting = 3,
        /// <summary>Beat 'Em Up - Side-scrolling combat against waves of enemies</summary>
        BeatEmUp = 4,
        /// <summary>Hack and Slash - Fast-paced melee combat with large enemy groups</summary>
        HackAndSlash = 5,
        /// <summary>Stealth - Sneaking and strategic takedowns</summary>
        Stealth = 6,
        /// <summary>Survival Horror - Resource management with atmospheric tension</summary>
        SurvivalHorror = 7,

        // ===== STRATEGY GENRES (5) =====
        /// <summary>Real-Time Strategy - Real-time resource management and unit control</summary>
        RealTimeStrategy = 8,
        /// <summary>Turn-Based Strategy - Tactical turn-based combat</summary>
        TurnBasedStrategy = 9,
        /// <summary>4X Strategy - Explore, Expand, Exploit, Exterminate</summary>
        FourXStrategy = 10,
        /// <summary>Grand Strategy - Large-scale empire management</summary>
        GrandStrategy = 11,
        /// <summary>Auto Battler - Automated combat with pre-battle team composition</summary>
        AutoBattler = 12,

        // ===== PUZZLE GENRES (5) =====
        /// <summary>Match-3 - Match three or more identical items</summary>
        Match3 = 13,
        /// <summary>Tetris-Like - Falling blocks spatial puzzle</summary>
        TetrisLike = 14,
        /// <summary>Physics Puzzle - Physics-based problem solving</summary>
        PhysicsPuzzle = 15,
        /// <summary>Hidden Object - Find hidden items in detailed scenes</summary>
        HiddenObject = 16,
        /// <summary>Word Game - Word formation and vocabulary challenges</summary>
        WordGame = 17,

        // ===== ADVENTURE GENRES (4) =====
        /// <summary>Point-and-Click - Narrative-driven exploration with inventory puzzles</summary>
        PointAndClick = 18,
        /// <summary>Visual Novel - Story-driven with branching narrative choices</summary>
        VisualNovel = 19,
        /// <summary>Walking Simulator - Exploration-focused narrative experience</summary>
        WalkingSimulator = 20,
        /// <summary>Metroidvania - Exploration with ability-gated progression</summary>
        Metroidvania = 21,

        // ===== PLATFORM GENRES (3) =====
        /// <summary>2D Platformer - Side-scrolling jump-based navigation</summary>
        Platformer2D = 22,
        /// <summary>3D Platformer - 3D environment jump-based navigation</summary>
        Platformer3D = 23,
        /// <summary>Endless Runner - Infinite procedural obstacle avoidance</summary>
        EndlessRunner = 24,

        // ===== SIMULATION GENRES (4) =====
        /// <summary>Vehicle Simulation - Realistic vehicle operation</summary>
        VehicleSimulation = 25,
        /// <summary>Flight Simulator - Realistic aircraft flight mechanics</summary>
        FlightSimulator = 26,
        /// <summary>Farming Simulator - Agricultural management and growth</summary>
        FarmingSimulator = 27,
        /// <summary>Construction Simulator - Building and construction management</summary>
        ConstructionSimulator = 28,

        // ===== ARCADE GENRES (4) =====
        /// <summary>Roguelike - Procedural levels with permanent death</summary>
        Roguelike = 29,
        /// <summary>Roguelite - Procedural with meta-progression</summary>
        Roguelite = 30,
        /// <summary>Bullet Hell - Dodge complex bullet patterns</summary>
        BulletHell = 31,
        /// <summary>Classic Arcade - Retro arcade game mechanics</summary>
        ClassicArcade = 32,

        // ===== BOARD & CARD GENRES (3) =====
        /// <summary>Board Game - Digital board game mechanics</summary>
        BoardGame = 33,
        /// <summary>Card Game - Card-based strategy and collection</summary>
        CardGame = 34,
        /// <summary>Chess-Like - Chess-inspired tactical positioning</summary>
        ChessLike = 35,

        // ===== CORE ACTIVITY GENRES (10) =====
        /// <summary>Exploration - Open-world discovery and navigation</summary>
        Exploration = 36,
        /// <summary>Racing - Speed-based track competition</summary>
        Racing = 37,
        /// <summary>Tower Defense - Strategic tower placement and enemy waves</summary>
        TowerDefense = 38,
        /// <summary>Battle Royale - Last-player-standing survival combat</summary>
        BattleRoyale = 39,
        /// <summary>City Builder - Urban planning and resource management</summary>
        CityBuilder = 40,
        /// <summary>Detective - Investigation and deduction puzzles</summary>
        Detective = 41,
        /// <summary>Economics - Market trading and financial strategy</summary>
        Economics = 42,
        /// <summary>Sports - Athletic competition and team coordination</summary>
        Sports = 43,

        // ===== MUSIC GENRES (2) =====
        /// <summary>Rhythm Game - Musical timing and beat matching</summary>
        RhythmGame = 44,
        /// <summary>Music Creation - Procedural music composition</summary>
        MusicCreation = 45,

        // ===== LEGACY SUPPORT =====
        /// <summary>Generic Combat - Legacy combat activity</summary>
        Combat = 46,
        /// <summary>Generic Puzzle - Legacy puzzle activity</summary>
        Puzzle = 47,
        /// <summary>Generic Strategy - Legacy strategy activity</summary>
        Strategy = 48,
        /// <summary>Generic Music - Legacy music activity</summary>
        Music = 49,
        /// <summary>Generic Adventure - Legacy adventure activity</summary>
        Adventure = 50,
        /// <summary>Platforming - Legacy platforming activity</summary>
        Platforming = 51,
        /// <summary>Crafting - Resource gathering and item creation</summary>
        Crafting = 52,
        /// <summary>Social - Social interaction and bonding</summary>
        Social = 53,
        /// <summary>Training - Skill improvement and practice</summary>
        Training = 54,
        /// <summary>Breeding - Creature breeding mechanics</summary>
        Breeding = 55,
        /// <summary>Foraging - Resource collection from environment</summary>
        Foraging = 56,
        /// <summary>Resting - Recovery and passive bonding</summary>
        Resting = 57,

        /// <summary>Custom activity - User-defined genre</summary>
        Custom = 255
    }

    public enum ActivityStatus : byte
    {
        NotParticipating,
        Queued,
        Warming_Up,
        Active,
        Completed,
        Failed,
        Rewarded
    }
}