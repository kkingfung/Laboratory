using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Centralized constants for Project Chimera
    /// Eliminates magic numbers and provides single source of truth for game values
    /// </summary>
    public static class GameConstants
    {
        #region Equipment System

        /// <summary>Maximum number of equipment slots per creature</summary>
        public const int MAX_EQUIPMENT_SLOTS = 8;

        /// <summary>Default equipment durability (0-1)</summary>
        public const float DEFAULT_EQUIPMENT_DURABILITY = 1.0f;

        /// <summary>Critical equipment durability threshold</summary>
        public const float CRITICAL_DURABILITY_THRESHOLD = 0.2f;

        #endregion

        #region Weather System

        /// <summary>Hours in a game day</summary>
        public const float DAY_LENGTH_HOURS = 24f;

        /// <summary>Hour when dawn begins</summary>
        public const float DAWN_HOUR = 6f;

        /// <summary>Hour when dusk begins</summary>
        public const float DUSK_HOUR = 20f;

        /// <summary>Default weather transition duration (seconds)</summary>
        public const float WEATHER_TRANSITION_DURATION = 30f;

        /// <summary>Maximum sun light intensity</summary>
        public const float MAX_SUN_INTENSITY = 1.5f;

        /// <summary>Night light intensity</summary>
        public const float NIGHT_LIGHT_INTENSITY = 0.1f;

        /// <summary>Rain movement speed penalty (multiplier)</summary>
        public const float RAIN_MOVEMENT_PENALTY = 0.85f;

        /// <summary>Snow movement speed penalty (multiplier)</summary>
        public const float SNOW_MOVEMENT_PENALTY = 0.7f;

        #endregion

        #region Discovery System

        /// <summary>Minimum genetic novelty for minor discovery</summary>
        public const float MINOR_DISCOVERY_NOVELTY_THRESHOLD = 0.3f;

        /// <summary>Minimum genetic novelty for major discovery</summary>
        public const float MAJOR_DISCOVERY_NOVELTY_THRESHOLD = 0.4f;

        /// <summary>Minimum genetic novelty for legendary discovery</summary>
        public const float LEGENDARY_DISCOVERY_NOVELTY_THRESHOLD = 0.7f;

        /// <summary>Discovery points for common discovery</summary>
        public const int COMMON_DISCOVERY_POINTS = 10;

        /// <summary>Discovery points for rare discovery</summary>
        public const int RARE_DISCOVERY_POINTS = 50;

        /// <summary>Discovery points for legendary discovery</summary>
        public const int LEGENDARY_DISCOVERY_POINTS = 200;

        #endregion

        #region Analytics & Behavior

        /// <summary>High engagement threshold for analytics</summary>
        public const float HIGH_ENGAGEMENT_THRESHOLD = 0.7f;

        /// <summary>Difficulty adaptation sensitivity</summary>
        public const float DIFFICULTY_ADAPTATION_SENSITIVITY = 0.5f;

        /// <summary>Behavior analysis interval (seconds)</summary>
        public const float BEHAVIOR_ANALYSIS_INTERVAL = 30f;

        /// <summary>Action tracking interval (seconds)</summary>
        public const float ACTION_TRACKING_INTERVAL = 0.1f;

        /// <summary>Maximum session actions to track</summary>
        public const int MAX_SESSION_ACTIONS = 10000;

        #endregion

        #region Creature System

        /// <summary>Default creature health</summary>
        public const float DEFAULT_CREATURE_HEALTH = 100f;

        /// <summary>Critical health threshold (percentage)</summary>
        public const float CRITICAL_HEALTH_THRESHOLD = 0.2f;

        /// <summary>Low health threshold (percentage)</summary>
        public const float LOW_HEALTH_THRESHOLD = 0.3f;

        /// <summary>Default creature speed (units per second)</summary>
        public const float DEFAULT_CREATURE_SPEED = 5f;

        /// <summary>Maximum creatures in scene for optimal performance</summary>
        public const int TARGET_MAX_CREATURES = 1000;

        #endregion

        #region Breeding System

        /// <summary>Minimum breeding compatibility</summary>
        public const float MIN_BREEDING_COMPATIBILITY = 0.4f;

        /// <summary>Optimal breeding compatibility</summary>
        public const float OPTIMAL_BREEDING_COMPATIBILITY = 0.8f;

        /// <summary>Mutation rate baseline</summary>
        public const float BASE_MUTATION_RATE = 0.05f;

        /// <summary>Maximum trait inheritance variance</summary>
        public const float MAX_TRAIT_VARIANCE = 0.15f;

        /// <summary>Breeding cooldown (seconds)</summary>
        public const float BREEDING_COOLDOWN_SECONDS = 60f;

        #endregion

        #region AI System

        /// <summary>AI decision update interval (seconds)</summary>
        public const float AI_DECISION_INTERVAL = 0.5f;

        /// <summary>Pathfinding grid cell size (units)</summary>
        public const float PATHFINDING_CELL_SIZE = 1f;

        /// <summary>Maximum pathfinding iterations per frame</summary>
        public const int MAX_PATHFINDING_ITERATIONS = 1000;

        /// <summary>AI perception radius (units)</summary>
        public const float AI_PERCEPTION_RADIUS = 20f;

        /// <summary>Threat detection radius (units)</summary>
        public const float THREAT_DETECTION_RADIUS = 15f;

        /// <summary>Max AI updates per frame (timeslicing)</summary>
        public const int MAX_AI_UPDATES_PER_FRAME = 50;

        #endregion

        #region Ecosystem System

        /// <summary>Resource regeneration rate per second</summary>
        public const float RESOURCE_REGENERATION_RATE = 0.1f;

        /// <summary>Population density threshold for crowding</summary>
        public const float CROWDING_DENSITY_THRESHOLD = 0.8f;

        /// <summary>Minimum viable population</summary>
        public const int MIN_VIABLE_POPULATION = 5;

        /// <summary>Ecosystem balance check interval (seconds)</summary>
        public const float ECOSYSTEM_CHECK_INTERVAL = 5f;

        #endregion

        #region Procedural Generation

        /// <summary>Default terrain scale</summary>
        public const float DEFAULT_TERRAIN_SCALE = 50f;

        /// <summary>Terrain generation octaves</summary>
        public const int TERRAIN_OCTAVES = 4;

        /// <summary>Terrain persistence</summary>
        public const float TERRAIN_PERSISTENCE = 0.5f;

        /// <summary>Terrain lacunarity</summary>
        public const float TERRAIN_LACUNARITY = 2f;

        /// <summary>Default chunk size (units)</summary>
        public const int DEFAULT_CHUNK_SIZE = 100;

        /// <summary>View distance (chunks)</summary>
        public const int DEFAULT_VIEW_DISTANCE = 3;

        #endregion

        #region Performance & Optimization

        /// <summary>Target frame rate</summary>
        public const int TARGET_FPS = 60;

        /// <summary>Frame budget in milliseconds (16.67ms = 60 FPS)</summary>
        public const float FRAME_BUDGET_MS = 16.67f;

        /// <summary>Performance warning threshold (milliseconds)</summary>
        public const float PERFORMANCE_WARNING_THRESHOLD_MS = 5f;

        /// <summary>Performance critical threshold (milliseconds)</summary>
        public const float PERFORMANCE_CRITICAL_THRESHOLD_MS = 10f;

        /// <summary>Object pool default size</summary>
        public const int DEFAULT_POOL_SIZE = 100;

        /// <summary>Target pool hit rate</summary>
        public const float TARGET_POOL_HIT_RATE = 0.95f;

        /// <summary>Memory leak detection threshold (seconds)</summary>
        public const float LEAK_DETECTION_THRESHOLD_SECONDS = 30f;

        #endregion

        #region Multiplayer

        /// <summary>Maximum players per session</summary>
        public const int MAX_PLAYERS_PER_SESSION = 16;

        /// <summary>Network tick rate (Hz)</summary>
        public const int NETWORK_TICK_RATE = 20;

        /// <summary>Connection timeout (seconds)</summary>
        public const float CONNECTION_TIMEOUT_SECONDS = 30f;

        /// <summary>Maximum reconnection attempts</summary>
        public const int MAX_RECONNECTION_ATTEMPTS = 3;

        /// <summary>Reconnection delay (seconds)</summary>
        public const float RECONNECTION_DELAY_SECONDS = 2f;

        #endregion

        #region UI & User Experience

        /// <summary>UI animation duration (seconds)</summary>
        public const float UI_ANIMATION_DURATION = 0.3f;

        /// <summary>Tooltip display delay (seconds)</summary>
        public const float TOOLTIP_DELAY = 0.5f;

        /// <summary>Screen fade duration (seconds)</summary>
        public const float SCREEN_FADE_DURATION = 1f;

        /// <summary>Double-click time window (seconds)</summary>
        public const float DOUBLE_CLICK_TIME_WINDOW = 0.3f;

        #endregion

        #region Save System

        /// <summary>Autosave interval (seconds)</summary>
        public const float AUTOSAVE_INTERVAL_SECONDS = 300f; // 5 minutes

        /// <summary>Maximum save slots</summary>
        public const int MAX_SAVE_SLOTS = 10;

        /// <summary>Save data version</summary>
        public const int SAVE_DATA_VERSION = 1;

        #endregion

        #region Audio

        /// <summary>Default master volume</summary>
        public const float DEFAULT_MASTER_VOLUME = 0.8f;

        /// <summary>Audio fade duration (seconds)</summary>
        public const float AUDIO_FADE_DURATION = 2f;

        /// <summary>3D audio max distance</summary>
        public const float AUDIO_MAX_DISTANCE = 50f;

        #endregion
    }
}
