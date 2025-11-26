using UnityEngine;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Progression;

namespace Laboratory.Subsystems.Gameplay
{
    /// <summary>
    /// ScriptableObject configuration for gameplay systems
    /// Designer-friendly settings for all 47 genres
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayConfig", menuName = "Chimera/Gameplay/Gameplay Config")]
    public class GameplayConfig : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField] private ActivityGenreCategory defaultGenre = ActivityGenreCategory.Action;
        [SerializeField] private float sessionTimeout = 1800f; // 30 minutes
        [SerializeField] private bool enableTutorials = true;
        [SerializeField] private bool enableDifficultyScaling = true;

        [Header("Session Management")]
        [SerializeField] private int maxConcurrentActivities = 1;
        [SerializeField] private float activityTransitionDelay = 2f;
        [SerializeField] private bool allowActivityInterruption = false;

        [Header("Progression")]
        [SerializeField] private bool trackActivityProgress = true;
        [SerializeField] private bool enableMilestones = true;
        [SerializeField] private float masteryGainRate = 1f;

        [Header("Performance")]
        [SerializeField] private bool enablePerformanceMonitoring = true;
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool optimizeForBatteryLife = false;

        [Header("Social Features")]
        [SerializeField] private bool enableMultiplayer = true;
        [SerializeField] private bool enableSpectatorMode = true;
        [SerializeField] private bool enableReplayRecording = true;

        [Header("Rewards")]
        [SerializeField] private float baseRewardMultiplier = 1f;
        [SerializeField] private bool enableBonusRewards = true;
        [SerializeField] private float perfectPlayBonusMultiplier = 1.5f;

        [Header("UI Feedback")]
        [SerializeField] private bool showPerformanceMetrics = true;
        [SerializeField] private bool showSkillProgression = true;
        [SerializeField] private bool enableHapticFeedback = true;

        // Properties
        public ActivityGenreCategory DefaultGenre => defaultGenre;
        public float SessionTimeout => sessionTimeout;
        public bool EnableTutorials => enableTutorials;
        public bool EnableDifficultyScaling => enableDifficultyScaling;

        public int MaxConcurrentActivities => maxConcurrentActivities;
        public float ActivityTransitionDelay => activityTransitionDelay;
        public bool AllowActivityInterruption => allowActivityInterruption;

        public bool TrackActivityProgress => trackActivityProgress;
        public bool EnableMilestones => enableMilestones;
        public float MasteryGainRate => masteryGainRate;

        public bool EnablePerformanceMonitoring => enablePerformanceMonitoring;
        public int TargetFrameRate => targetFrameRate;
        public bool OptimizeForBatteryLife => optimizeForBatteryLife;

        public bool EnableMultiplayer => enableMultiplayer;
        public bool EnableSpectatorMode => enableSpectatorMode;
        public bool EnableReplayRecording => enableReplayRecording;

        public float BaseRewardMultiplier => baseRewardMultiplier;
        public bool EnableBonusRewards => enableBonusRewards;
        public float PerfectPlayBonusMultiplier => perfectPlayBonusMultiplier;

        public bool ShowPerformanceMetrics => showPerformanceMetrics;
        public bool ShowSkillProgression => showSkillProgression;
        public bool EnableHapticFeedback => enableHapticFeedback;
    }
}
