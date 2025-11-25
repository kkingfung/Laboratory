using UnityEngine;
using System;
using System.Collections.Generic;

namespace Laboratory.Subsystems.Tutorial
{
    /// <summary>
    /// 9-Stage Progressive Learning System
    /// Implements the adaptive onboarding flow from README
    /// </summary>
    public class NineStageOnboarding : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TutorialConfig config;

        // Current state
        private OnboardingStage _currentStage = OnboardingStage.None;
        private int _currentStageIndex = 0;
        private bool _isOnboardingActive = false;
        private float _stageStartTime;

        // Performance tracking
        private Queue<float> _performanceScores = new Queue<float>();
        private int _mistakesThisStage = 0;
        private int _hintsUsedThisStage = 0;

        // Completion tracking
        private HashSet<OnboardingStage> _completedStages = new HashSet<OnboardingStage>();
        private bool _hasCompletedOnboarding = false;

        // Events
        public event Action<OnboardingStage> OnStageStarted;
        public event Action<OnboardingStage, StagePerformance> OnStageCompleted;
        public event Action OnOnboardingCompleted;
        public event Action<string> OnHintRequested;
        public event Action<string> OnCelebration;

        private void Start()
        {
            if (config == null)
            {
                Debug.LogWarning("[NineStageOnboarding] No configuration assigned!");
            }
        }

        /// <summary>
        /// Start the 9-stage onboarding
        /// </summary>
        public void StartOnboarding()
        {
            if (config == null || !config.Enable9StageSystem)
            {
                Debug.LogWarning("[NineStageOnboarding] Cannot start - disabled or no config");
                return;
            }

            _isOnboardingActive = true;
            _currentStageIndex = 1;
            StartStage(OnboardingStage.Stage1_Welcome);

            Debug.Log("[NineStageOnboarding] Started 9-stage onboarding");
        }

        /// <summary>
        /// Start a specific stage
        /// </summary>
        private void StartStage(OnboardingStage stage)
        {
            _currentStage = stage;
            _stageStartTime = Time.time;
            _mistakesThisStage = 0;
            _hintsUsedThisStage = 0;

            OnStageStarted?.Invoke(stage);

            if (config != null && config.CelebrateSuccesses && _currentStageIndex > 1)
            {
                OnCelebration?.Invoke($"Great job completing {GetStageName(_currentStageIndex - 1)}!");
            }

            Debug.Log($"[NineStageOnboarding] Started stage {_currentStageIndex}: {GetStageName(_currentStageIndex)}");
        }

        /// <summary>
        /// Complete current stage and advance
        /// </summary>
        public void CompleteCurrentStage(float performanceScore = 1f)
        {
            if (!_isOnboardingActive) return;

            float stageDuration = Time.time - _stageStartTime;
            float minDuration = config != null ? config.GetStageMinDuration(_currentStageIndex) : 30f;

            // Can't complete too quickly (prevents rushing)
            if (stageDuration < minDuration)
            {
                Debug.LogWarning($"[NineStageOnboarding] Stage {_currentStageIndex} completed too quickly. Minimum: {minDuration}s, actual: {stageDuration:F1}s");
                return;
            }

            // Track performance
            _performanceScores.Enqueue(performanceScore);
            if (config != null && _performanceScores.Count > config.PerformanceSampleSize)
            {
                _performanceScores.Dequeue();
            }

            // Create performance report
            StagePerformance performance = new StagePerformance
            {
                Stage = _currentStage,
                Duration = stageDuration,
                Score = performanceScore,
                MistakeCount = _mistakesThisStage,
                HintsUsed = _hintsUsedThisStage,
                AverageRecentPerformance = GetAveragePerformance()
            };

            _completedStages.Add(_currentStage);
            OnStageCompleted?.Invoke(_currentStage, performance);

            // Celebrate if configured
            if (config != null && config.CelebrateSuccesses)
            {
                string celebration = GenerateCelebrationMessage(performance);
                OnCelebration?.Invoke(celebration);
            }

            // Advance to next stage
            _currentStageIndex++;

            if (_currentStageIndex > 9)
            {
                CompleteOnboarding();
            }
            else
            {
                OnboardingStage nextStage = (OnboardingStage)_currentStageIndex;
                StartStage(nextStage);
            }
        }

        /// <summary>
        /// Complete the entire onboarding
        /// </summary>
        private void CompleteOnboarding()
        {
            _isOnboardingActive = false;
            _hasCompletedOnboarding = true;

            OnOnboardingCompleted?.Invoke();
            OnCelebration?.Invoke("🎓 Congratulations! You've completed the full training program!");

            Debug.Log("[NineStageOnboarding] Onboarding completed!");
        }

        /// <summary>
        /// Record a mistake in the current stage
        /// </summary>
        public void RecordMistake()
        {
            _mistakesThisStage++;

            if (config != null && config.ForgiveMistakes)
            {
                Debug.Log($"[NineStageOnboarding] Mistake recorded ({_mistakesThisStage} this stage). Don't worry, everyone learns differently!");
            }

            // Offer hint after multiple mistakes
            if (_mistakesThisStage >= 3 && config != null && config.EnableContextualHints)
            {
                RequestHint();
            }
        }

        /// <summary>
        /// Request a contextual hint
        /// </summary>
        public void RequestHint()
        {
            if (config == null || !config.EnableContextualHints) return;

            if (_hintsUsedThisStage >= (config.MaxHintsPerStage))
            {
                Debug.Log("[NineStageOnboarding] Maximum hints reached for this stage");
                return;
            }

            _hintsUsedThisStage++;
            string hint = GetHintForCurrentStage();
            OnHintRequested?.Invoke(hint);

            Debug.Log($"[NineStageOnboarding] Hint requested: {hint}");
        }

        /// <summary>
        /// Get hint for current stage
        /// </summary>
        private string GetHintForCurrentStage()
        {
            switch (_currentStage)
            {
                case OnboardingStage.Stage1_Welcome:
                    return "Take your time to read through the welcome message. Press 'Next' when ready.";

                case OnboardingStage.Stage2_BasicControls:
                    return "Use WASD to move and mouse to look around. Try moving in all directions.";

                case OnboardingStage.Stage3_TeamJoining:
                    return "Look for the 'Join Team' button at the bottom of the screen.";

                case OnboardingStage.Stage4_RoleSelection:
                    return "Choose a role that matches your playstyle. Tank is great for beginners!";

                case OnboardingStage.Stage5_BasicTeamwork:
                    return "Stay near your teammates. The green indicator shows your team members.";

                case OnboardingStage.Stage6_Communication:
                    return "Use the ping system to communicate. Press T to open the ping wheel.";

                case OnboardingStage.Stage7_ObjectivesStrategy:
                    return "Check the top of the screen for current objectives. Follow the waypoint markers.";

                case OnboardingStage.Stage8_AdvancedTactics:
                    return "Coordinate with your team. Use formations and tactical commands.";

                case OnboardingStage.Stage9_Graduation:
                    return "You're almost done! Complete this final challenge to graduate.";

                default:
                    return "You're doing great! Keep going!";
            }
        }

        /// <summary>
        /// Get average recent performance
        /// </summary>
        private float GetAveragePerformance()
        {
            if (_performanceScores.Count == 0) return 1f;

            float sum = 0f;
            foreach (float score in _performanceScores)
            {
                sum += score;
            }

            return sum / _performanceScores.Count;
        }

        /// <summary>
        /// Generate celebration message based on performance
        /// </summary>
        private string GenerateCelebrationMessage(StagePerformance performance)
        {
            if (performance.Score >= 0.9f && performance.MistakeCount == 0)
            {
                return "⭐ Perfect! Outstanding performance!";
            }
            else if (performance.Score >= 0.7f)
            {
                return "✅ Great job! You're getting the hang of it!";
            }
            else if (performance.Score >= 0.5f)
            {
                return "👍 Good work! Keep practicing!";
            }
            else
            {
                return "💪 You completed it! Every step forward counts!";
            }
        }

        /// <summary>
        /// Get stage name
        /// </summary>
        private string GetStageName(int stageIndex)
        {
            OnboardingStage stage = (OnboardingStage)stageIndex;
            return stage.ToString().Replace("Stage" + stageIndex + "_", "").Replace("_", " ");
        }

        /// <summary>
        /// Skip to a specific stage (if allowed)
        /// </summary>
        public bool SkipToStage(int stageIndex)
        {
            if (config == null || !config.AllowStageSkipping) return false;

            if (stageIndex < config.MinimumStageForSkipping)
            {
                Debug.LogWarning($"[NineStageOnboarding] Cannot skip to stage {stageIndex}. Minimum: {config.MinimumStageForSkipping}");
                return false;
            }

            _currentStageIndex = stageIndex;
            StartStage((OnboardingStage)stageIndex);
            return true;
        }

        /// <summary>
        /// Get current stage
        /// </summary>
        public OnboardingStage GetCurrentStage()
        {
            return _currentStage;
        }

        /// <summary>
        /// Check if onboarding is active
        /// </summary>
        public bool IsOnboardingActive()
        {
            return _isOnboardingActive;
        }

        /// <summary>
        /// Check if onboarding is completed
        /// </summary>
        public bool HasCompletedOnboarding()
        {
            return _hasCompletedOnboarding;
        }

        /// <summary>
        /// Get progress percentage (0-100)
        /// </summary>
        public float GetProgressPercentage()
        {
            return (_currentStageIndex / 9f) * 100f;
        }
    }

    /// <summary>
    /// 9 onboarding stages as defined in README
    /// </summary>
    public enum OnboardingStage
    {
        None = 0,
        Stage1_Welcome = 1,
        Stage2_BasicControls = 2,
        Stage3_TeamJoining = 3,
        Stage4_RoleSelection = 4,
        Stage5_BasicTeamwork = 5,
        Stage6_Communication = 6,
        Stage7_ObjectivesStrategy = 7,
        Stage8_AdvancedTactics = 8,
        Stage9_Graduation = 9
    }

    /// <summary>
    /// Performance data for a completed stage
    /// </summary>
    public struct StagePerformance
    {
        public OnboardingStage Stage;
        public float Duration;
        public float Score;
        public int MistakeCount;
        public int HintsUsed;
        public float AverageRecentPerformance;
    }
}
