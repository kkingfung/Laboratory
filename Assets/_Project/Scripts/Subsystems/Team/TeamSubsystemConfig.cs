using UnityEngine;
using Laboratory.Subsystems.Team.Core;
using Laboratory.Core.GameModes;

namespace Laboratory.Subsystems.Team
{
    /// <summary>
    /// Team Subsystem Configuration - Designer-Friendly Team Settings
    /// PURPOSE: Centralized configuration for all team mechanics
    /// FEATURES: Matchmaking, onboarding, communication, genre-specific settings
    /// DESIGNER-FRIENDLY: No code required, visual inspector configuration
    /// VALIDATION: Built-in validation and defaults
    /// </summary>

    [CreateAssetMenu(fileName = "TeamSubsystemConfig", menuName = "Laboratory/Team/Team Subsystem Configuration")]
    public class TeamSubsystemConfig : ScriptableObject
    {
        [Header("General Team Settings")]
        [Tooltip("Enable team system globally")]
        public bool enableTeamSystem = true;

        [Tooltip("Default team size for most game modes")]
        [Range(2, 20)]
        public int defaultTeamSize = 4;

        [Tooltip("Allow solo players to use team features with AI teammates")]
        public bool allowSoloWithAI = true;

        [Tooltip("Enable cross-platform teams")]
        public bool enableCrossPlatform = true;

        [Header("Matchmaking Settings")]
        [SerializeField] private MatchmakingSettings matchmaking = new MatchmakingSettings();

        [Header("Tutorial & Onboarding")]
        [SerializeField] private TutorialSettings tutorial = new TutorialSettings();

        [Header("Communication Settings")]
        [SerializeField] private CommunicationSettings communication = new CommunicationSettings();

        [Header("Performance Settings")]
        [SerializeField] private PerformanceSettings performance = new PerformanceSettings();

        [Header("Genre-Specific Configurations")]
        [SerializeField] private GenreTeamSettings[] genreSettings = new GenreTeamSettings[0];

        // Public accessors
        public MatchmakingSettings Matchmaking => matchmaking;
        public TutorialSettings Tutorial => tutorial;
        public CommunicationSettings Communication => communication;
        public PerformanceSettings Performance => performance;

        /// <summary>
        /// Get genre-specific settings for a genre
        /// </summary>
        public GenreTeamSettings GetGenreSettings(GameGenre genre)
        {
            foreach (var settings in genreSettings)
            {
                if (settings.genre == genre)
                    return settings;
            }

            // Return default if not found
            return new GenreTeamSettings { genre = genre };
        }

        /// <summary>
        /// Validate configuration settings
        /// </summary>
        public bool ValidateConfiguration(out string[] errors)
        {
            var errorList = new System.Collections.Generic.List<string>();

            // Validate matchmaking
            if (matchmaking.maxSkillGap < matchmaking.strictSkillGap)
                errorList.Add("Max skill gap must be >= strict skill gap");

            if (matchmaking.maxQueueTime < 10f)
                errorList.Add("Max queue time should be at least 10 seconds");

            // Validate tutorial
            if (tutorial.maxTutorialDuration < 60f)
                errorList.Add("Tutorial duration should be at least 60 seconds");

            // Validate communication
            if (communication.pingCooldown < 0.5f)
                errorList.Add("Ping cooldown should be at least 0.5 seconds to prevent spam");

            if (communication.maxActivePings < 3)
                errorList.Add("Should allow at least 3 active pings");

            // Validate performance
            if (performance.maxConcurrentTeams < 10)
                errorList.Add("Should support at least 10 concurrent teams");

            errors = errorList.ToArray();
            return errorList.Count == 0;
        }

        /// <summary>
        /// Reset to sensible defaults
        /// </summary>
        [ContextMenu("Reset to Defaults")]
        public void ResetToDefaults()
        {
            enableTeamSystem = true;
            defaultTeamSize = 4;
            allowSoloWithAI = true;
            enableCrossPlatform = true;

            matchmaking = new MatchmakingSettings();
            tutorial = new TutorialSettings();
            communication = new CommunicationSettings();
            performance = new PerformanceSettings();

            Debug.Log("Team configuration reset to defaults");
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Ensure values stay within valid ranges
            defaultTeamSize = Mathf.Clamp(defaultTeamSize, 2, 20);
            matchmaking.OnValidate();
            tutorial.OnValidate();
            communication.OnValidate();
            performance.OnValidate();
        }
#endif
    }

    #region Configuration Classes

    [System.Serializable]
    public class MatchmakingSettings
    {
        [Header("Skill Matching")]
        [Tooltip("Enable skill-based matchmaking")]
        public bool enableSkillMatching = true;

        [Tooltip("Strict skill gap for competitive modes")]
        [Range(50f, 500f)]
        public float strictSkillGap = 100f;

        [Tooltip("Maximum skill gap allowed")]
        [Range(100f, 1000f)]
        public float maxSkillGap = 300f;

        [Tooltip("Skill range expansion per second of waiting")]
        [Range(10f, 100f)]
        public float skillRangeExpansionRate = 50f;

        [Header("Beginner Protection")]
        [Tooltip("Protect new players from experts")]
        public bool enableBeginnerProtection = true;

        [Tooltip("Skill rating threshold for beginner protection")]
        [Range(1000f, 1500f)]
        public float beginnerProtectionThreshold = 1200f;

        [Header("Queue Settings")]
        [Tooltip("Maximum time in queue before giving up (seconds)")]
        [Range(30f, 600f)]
        public float maxQueueTime = 300f;

        [Tooltip("Minimum match quality to accept (0-1)")]
        [Range(0.3f, 1f)]
        public float minMatchQuality = 0.6f;

        [Tooltip("Allow backfilling into existing teams")]
        public bool allowBackfill = true;

        [Header("Role Queue")]
        [Tooltip("Enable role-based matchmaking")]
        public bool enableRoleQueue = true;

        [Tooltip("Require balanced team composition")]
        public bool requireBalancedComposition = false;

        public void OnValidate()
        {
            strictSkillGap = Mathf.Clamp(strictSkillGap, 50f, 500f);
            maxSkillGap = Mathf.Max(strictSkillGap, maxSkillGap);
            skillRangeExpansionRate = Mathf.Clamp(skillRangeExpansionRate, 10f, 100f);
            beginnerProtectionThreshold = Mathf.Clamp(beginnerProtectionThreshold, 1000f, 1500f);
            maxQueueTime = Mathf.Clamp(maxQueueTime, 30f, 600f);
            minMatchQuality = Mathf.Clamp01(minMatchQuality);
        }
    }

    [System.Serializable]
    public class TutorialSettings
    {
        [Header("Tutorial Activation")]
        [Tooltip("Enable tutorials for new players")]
        public bool enableTutorials = true;

        [Tooltip("Automatically start tutorial for new players")]
        public bool autoStartTutorial = true;

        [Tooltip("Allow experienced players to skip tutorial")]
        public bool allowSkip = true;

        [Header("Tutorial Duration")]
        [Tooltip("Maximum tutorial duration (seconds)")]
        [Range(60f, 1800f)]
        public float maxTutorialDuration = 600f;

        [Tooltip("Time to complete each tutorial stage (seconds)")]
        [Range(30f, 300f)]
        public float averageStageTime = 60f;

        [Header("Adaptive Learning")]
        [Tooltip("Adapt tutorial difficulty to player performance")]
        public bool enableAdaptiveLearning = true;

        [Tooltip("Number of mistakes before reducing difficulty")]
        [Range(1, 10)]
        public int mistakesBeforeHelp = 3;

        [Tooltip("Show contextual hints during gameplay")]
        public bool showHints = true;

        [Tooltip("Maximum hints to show per session")]
        [Range(5, 50)]
        public int maxHintsPerSession = 20;

        [Header("Tutorial Rewards")]
        [Tooltip("Reward players for completing tutorial")]
        public bool giveCompletionReward = true;

        [Tooltip("Experience points for tutorial completion")]
        [Range(0, 1000)]
        public int tutorialCompletionXP = 500;

        public void OnValidate()
        {
            maxTutorialDuration = Mathf.Clamp(maxTutorialDuration, 60f, 1800f);
            averageStageTime = Mathf.Clamp(averageStageTime, 30f, 300f);
            mistakesBeforeHelp = Mathf.Clamp(mistakesBeforeHelp, 1, 10);
            maxHintsPerSession = Mathf.Clamp(maxHintsPerSession, 5, 50);
            tutorialCompletionXP = Mathf.Clamp(tutorialCompletionXP, 0, 1000);
        }
    }

    [System.Serializable]
    public class CommunicationSettings
    {
        [Header("Ping System")]
        [Tooltip("Enable ping system")]
        public bool enablePings = true;

        [Tooltip("Cooldown between pings (seconds)")]
        [Range(0.5f, 10f)]
        public float pingCooldown = 3f;

        [Tooltip("Maximum active pings per team")]
        [Range(3, 20)]
        public int maxActivePings = 10;

        [Tooltip("Ping display duration (seconds)")]
        [Range(3f, 30f)]
        public float pingDuration = 5f;

        [Tooltip("Maximum ping distance")]
        [Range(100f, 5000f)]
        public float maxPingDistance = 1000f;

        [Header("Quick Chat")]
        [Tooltip("Enable quick chat system")]
        public bool enableQuickChat = true;

        [Tooltip("Cooldown between chat messages (seconds)")]
        [Range(1f, 10f)]
        public float chatCooldown = 2f;

        [Tooltip("Maximum chat messages per minute")]
        [Range(5, 60)]
        public int maxChatPerMinute = 30;

        [Header("Tactical Commands")]
        [Tooltip("Enable tactical commands (leader only)")]
        public bool enableTacticalCommands = true;

        [Tooltip("Only team leader can issue tactical commands")]
        public bool leaderOnlyCommands = true;

        [Header("Communication Scoring")]
        [Tooltip("Track communication quality for matchmaking")]
        public bool trackCommunicationScore = true;

        [Tooltip("Bonus for good communication")]
        [Range(0f, 0.5f)]
        public float goodCommunicationBonus = 0.2f;

        public void OnValidate()
        {
            pingCooldown = Mathf.Clamp(pingCooldown, 0.5f, 10f);
            maxActivePings = Mathf.Clamp(maxActivePings, 3, 20);
            pingDuration = Mathf.Clamp(pingDuration, 3f, 30f);
            maxPingDistance = Mathf.Clamp(maxPingDistance, 100f, 5000f);
            chatCooldown = Mathf.Clamp(chatCooldown, 1f, 10f);
            maxChatPerMinute = Mathf.Clamp(maxChatPerMinute, 5, 60);
            goodCommunicationBonus = Mathf.Clamp01(goodCommunicationBonus);
        }
    }

    [System.Serializable]
    public class PerformanceSettings
    {
        [Header("Team Limits")]
        [Tooltip("Maximum concurrent teams")]
        [Range(10, 1000)]
        public int maxConcurrentTeams = 100;

        [Tooltip("Maximum players in matchmaking queue")]
        [Range(50, 5000)]
        public int maxQueueSize = 1000;

        [Header("Update Frequencies")]
        [Tooltip("Team system update frequency (Hz)")]
        [Range(10, 60)]
        public int teamUpdateRate = 30;

        [Tooltip("Matchmaking update frequency (Hz)")]
        [Range(1, 10)]
        public int matchmakingUpdateRate = 2;

        [Tooltip("Communication system update frequency (Hz)")]
        [Range(10, 60)]
        public int communicationUpdateRate = 20;

        [Header("Optimization")]
        [Tooltip("Enable Burst compilation for team systems")]
        public bool enableBurst = true;

        [Tooltip("Enable Job system parallelization")]
        public bool enableJobs = true;

        [Tooltip("Batch size for team processing")]
        [Range(10, 200)]
        public int teamBatchSize = 50;

        public void OnValidate()
        {
            maxConcurrentTeams = Mathf.Clamp(maxConcurrentTeams, 10, 1000);
            maxQueueSize = Mathf.Clamp(maxQueueSize, 50, 5000);
            teamUpdateRate = Mathf.Clamp(teamUpdateRate, 10, 60);
            matchmakingUpdateRate = Mathf.Clamp(matchmakingUpdateRate, 1, 10);
            communicationUpdateRate = Mathf.Clamp(communicationUpdateRate, 10, 60);
            teamBatchSize = Mathf.Clamp(teamBatchSize, 10, 200);
        }
    }

    [System.Serializable]
    public class GenreTeamSettings
    {
        [Tooltip("Game genre")]
        public GameGenre genre = GameGenre.Exploration;

        [Tooltip("Enable teams for this genre")]
        public bool enableTeams = true;

        [Tooltip("Default team size for this genre")]
        [Range(2, 20)]
        public int teamSize = 4;

        [Tooltip("Allow AI teammates")]
        public bool allowAI = true;

        [Tooltip("Enable competitive teams")]
        public bool allowCompetitive = true;

        [Tooltip("Enable cooperative teams")]
        public bool allowCooperative = true;

        [Tooltip("Genre-specific team bonus multiplier")]
        [Range(0.5f, 2f)]
        public float teamBonusMultiplier = 1f;

        [Tooltip("Custom configuration data (JSON)")]
        [TextArea(3, 10)]
        public string customConfig = "";
    }

    #endregion
}
