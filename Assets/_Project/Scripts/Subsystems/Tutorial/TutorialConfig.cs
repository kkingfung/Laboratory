using UnityEngine;

namespace Laboratory.Subsystems.Tutorial
{
    /// <summary>
    /// ScriptableObject configuration for tutorial subsystem
    /// Designer-friendly settings for 9-stage onboarding
    /// </summary>
    [CreateAssetMenu(fileName = "TutorialConfig", menuName = "Chimera/Tutorial/Tutorial Config")]
    public class TutorialConfig : ScriptableObject
    {
        [Header("General Settings")]
        [SerializeField] private bool enableTutorials = true;
        [SerializeField] private bool allowSkipping = true;
        [SerializeField] private bool forceCompletionForNewPlayers = false;

        [Header("9-Stage Onboarding")]
        [SerializeField] private bool enable9StageSystem = true;
        [SerializeField] private bool allowStageSkipping = false;
        [SerializeField] private int minimumStageForSkipping = 3;

        [Header("Adaptive Settings")]
        [SerializeField] private bool enableAdaptiveDifficulty = true;
        [SerializeField] private float difficultyAdjustmentSpeed = 0.1f;
        [SerializeField] private int performanceSampleSize = 5;

        [Header("Hints")]
        [SerializeField] private bool enableContextualHints = true;
        [SerializeField] private float hintDelaySeconds = 10f;
        [SerializeField] private int maxHintsPerStage = 3;

        [Header("Celebration")]
        [SerializeField] private bool celebrateSuccesses = true;
        [SerializeField] private bool forgiveMistakes = true;
        [SerializeField] private float celebrationDuration = 2f;

        [Header("Stage Timing")]
        [SerializeField] private float stage1MinDuration = 30f; // Welcome
        [SerializeField] private float stage2MinDuration = 60f; // Basic Controls
        [SerializeField] private float stage3MinDuration = 90f; // Team Joining
        [SerializeField] private float stage4MinDuration = 120f; // Role Selection
        [SerializeField] private float stage5MinDuration = 120f; // Basic Teamwork
        [SerializeField] private float stage6MinDuration = 90f; // Communication
        [SerializeField] private float stage7MinDuration = 150f; // Objectives
        [SerializeField] private float stage8MinDuration = 180f; // Advanced Tactics
        [SerializeField] private float stage9MinDuration = 60f; // Graduation

        [Header("Progression")]
        [SerializeField] private bool saveProgress = true;
        [SerializeField] private bool allowReplay = true;
        [SerializeField] private bool trackPerformanceMetrics = true;

        // Properties
        public bool EnableTutorials => enableTutorials;
        public bool AllowSkipping => allowSkipping;
        public bool ForceCompletionForNewPlayers => forceCompletionForNewPlayers;

        public bool Enable9StageSystem => enable9StageSystem;
        public bool AllowStageSkipping => allowStageSkipping;
        public int MinimumStageForSkipping => minimumStageForSkipping;

        public bool EnableAdaptiveDifficulty => enableAdaptiveDifficulty;
        public float DifficultyAdjustmentSpeed => difficultyAdjustmentSpeed;
        public int PerformanceSampleSize => performanceSampleSize;

        public bool EnableContextualHints => enableContextualHints;
        public float HintDelaySeconds => hintDelaySeconds;
        public int MaxHintsPerStage => maxHintsPerStage;

        public bool CelebrateSuccesses => celebrateSuccesses;
        public bool ForgiveMistakes => forgiveMistakes;
        public float CelebrationDuration => celebrationDuration;

        public float GetStageMinDuration(int stage)
        {
            switch (stage)
            {
                case 1: return stage1MinDuration;
                case 2: return stage2MinDuration;
                case 3: return stage3MinDuration;
                case 4: return stage4MinDuration;
                case 5: return stage5MinDuration;
                case 6: return stage6MinDuration;
                case 7: return stage7MinDuration;
                case 8: return stage8MinDuration;
                case 9: return stage9MinDuration;
                default: return 60f;
            }
        }

        public bool SaveProgress => saveProgress;
        public bool AllowReplay => allowReplay;
        public bool TrackPerformanceMetrics => trackPerformanceMetrics;
    }
}
