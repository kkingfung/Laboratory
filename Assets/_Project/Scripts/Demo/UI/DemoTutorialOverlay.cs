using UnityEngine;
using UnityEngine.UI;
using Laboratory.Subsystems.Tutorial;

namespace Laboratory.Demo.UI
{
    /// <summary>
    /// Tutorial overlay UI for 9-stage onboarding
    /// Displays tutorial steps, hints, and progress
    /// </summary>
    public class DemoTutorialOverlay : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject overlayPanel;
        [SerializeField] private Text stageTitle;
        [SerializeField] private Text stageDescription;
        [SerializeField] private Text hintText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Text progressText;
        [SerializeField] private GameObject celebrationPanel;
        [SerializeField] private Text celebrationText;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button hintButton;
        [SerializeField] private Button skipButton;

        // References
        private TutorialSubsystemManager _tutorialSubsystem;
        private NineStageOnboarding _nineStage;

        // Animation state
        private float _celebrationTimer = 0f;
        private const float CELEBRATION_DURATION = 3f;

        private void Start()
        {
            _tutorialSubsystem = TutorialSubsystemManager.Instance;

            if (_tutorialSubsystem == null)
            {
                Debug.LogWarning("[DemoTutorialOverlay] Tutorial subsystem not found!");
                return;
            }

            _nineStage = _tutorialSubsystem.GetNineStageSystem();

            if (_nineStage == null)
            {
                Debug.LogWarning("[DemoTutorialOverlay] Nine-stage system not found!");
                return;
            }

            InitializeUI();
            SubscribeToEvents();

            // Hide overlay if tutorial not active
            UpdateOverlayVisibility();
        }

        private void Update()
        {
            // Update celebration timer
            if (_celebrationTimer > 0f)
            {
                _celebrationTimer -= Time.deltaTime;
                if (_celebrationTimer <= 0f && celebrationPanel != null)
                {
                    celebrationPanel.SetActive(false);
                }
            }

            // Update progress
            UpdateProgress();
        }

        /// <summary>
        /// Initialize UI controls
        /// </summary>
        private void InitializeUI()
        {
            // Setup buttons
            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextClicked);
            }

            if (hintButton != null)
            {
                hintButton.onClick.AddListener(OnHintClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipClicked);
            }

            // Hide hint and celebration by default
            if (hintText != null)
            {
                hintText.gameObject.SetActive(false);
            }

            if (celebrationPanel != null)
            {
                celebrationPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Subscribe to tutorial events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_nineStage != null)
            {
                _nineStage.OnStageStarted += OnStageStarted;
                _nineStage.OnStageCompleted += OnStageCompleted;
                _nineStage.OnOnboardingCompleted += OnOnboardingCompleted;
                _nineStage.OnHintRequested += OnHintRequested;
                _nineStage.OnCelebration += OnCelebration;
            }
        }

        /// <summary>
        /// Unsubscribe from tutorial events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (_nineStage != null)
            {
                _nineStage.OnStageStarted -= OnStageStarted;
                _nineStage.OnStageCompleted -= OnStageCompleted;
                _nineStage.OnOnboardingCompleted -= OnOnboardingCompleted;
                _nineStage.OnHintRequested -= OnHintRequested;
                _nineStage.OnCelebration -= OnCelebration;
            }
        }

        /// <summary>
        /// Update overlay visibility based on tutorial state
        /// </summary>
        private void UpdateOverlayVisibility()
        {
            if (overlayPanel != null && _nineStage != null)
            {
                overlayPanel.SetActive(_nineStage.IsOnboardingActive());
            }
        }

        /// <summary>
        /// Update progress bar and text
        /// </summary>
        private void UpdateProgress()
        {
            if (_nineStage == null) return;

            float progress = _nineStage.GetProgressPercentage();

            if (progressSlider != null)
            {
                progressSlider.value = progress / 100f;
            }

            if (progressText != null)
            {
                progressText.text = $"{progress:F0}% Complete";
            }
        }

        /// <summary>
        /// Update stage display
        /// </summary>
        private void UpdateStageDisplay(OnboardingStage stage)
        {
            if (stageTitle != null)
            {
                stageTitle.text = GetStageTitle(stage);
            }

            if (stageDescription != null)
            {
                stageDescription.text = GetStageDescription(stage);
            }
        }

        /// <summary>
        /// Get user-friendly stage title
        /// </summary>
        private string GetStageTitle(OnboardingStage stage)
        {
            switch (stage)
            {
                case OnboardingStage.Stage1_Welcome:
                    return "Welcome to Project Chimera!";
                case OnboardingStage.Stage2_BasicControls:
                    return "Basic Controls & Movement";
                case OnboardingStage.Stage3_TeamJoining:
                    return "Team Joining & Formation";
                case OnboardingStage.Stage4_RoleSelection:
                    return "Role Selection";
                case OnboardingStage.Stage5_BasicTeamwork:
                    return "Basic Teamwork";
                case OnboardingStage.Stage6_Communication:
                    return "Communication Systems";
                case OnboardingStage.Stage7_ObjectivesStrategy:
                    return "Objectives & Strategy";
                case OnboardingStage.Stage8_AdvancedTactics:
                    return "Advanced Tactics";
                case OnboardingStage.Stage9_Graduation:
                    return "Graduation";
                default:
                    return "Tutorial";
            }
        }

        /// <summary>
        /// Get stage description
        /// </summary>
        private string GetStageDescription(OnboardingStage stage)
        {
            switch (stage)
            {
                case OnboardingStage.Stage1_Welcome:
                    return "Welcome! This tutorial will teach you everything you need to know about playing with your chimera partner.";
                case OnboardingStage.Stage2_BasicControls:
                    return "Learn to move and control your character using WASD keys and mouse.";
                case OnboardingStage.Stage3_TeamJoining:
                    return "Join a team and learn about team formations.";
                case OnboardingStage.Stage4_RoleSelection:
                    return "Choose your role: Tank, DPS, Healer, or Support.";
                case OnboardingStage.Stage5_BasicTeamwork:
                    return "Work together with your team to achieve objectives.";
                case OnboardingStage.Stage6_Communication:
                    return "Use pings and quick chat to communicate with teammates.";
                case OnboardingStage.Stage7_ObjectivesStrategy:
                    return "Learn about objectives and develop strategic thinking.";
                case OnboardingStage.Stage8_AdvancedTactics:
                    return "Master advanced tactics and formations.";
                case OnboardingStage.Stage9_Graduation:
                    return "Complete your final challenge and graduate!";
                default:
                    return "";
            }
        }

        // Event handlers
        private void OnStageStarted(OnboardingStage stage)
        {
            UpdateOverlayVisibility();
            UpdateStageDisplay(stage);

            // Hide hint from previous stage
            if (hintText != null)
            {
                hintText.gameObject.SetActive(false);
            }
        }

        private void OnStageCompleted(OnboardingStage stage, StagePerformance performance)
        {
            Debug.Log($"[DemoTutorialOverlay] Stage {stage} completed with score {performance.Score:F2}");
        }

        private void OnOnboardingCompleted()
        {
            UpdateOverlayVisibility();
            ShowCelebration("🎓 Tutorial Complete! You're ready to play!");
        }

        private void OnHintRequested(string hint)
        {
            if (hintText != null)
            {
                hintText.text = $"💡 Hint: {hint}";
                hintText.gameObject.SetActive(true);
            }
        }

        private void OnCelebration(string message)
        {
            ShowCelebration(message);
        }

        /// <summary>
        /// Show celebration message
        /// </summary>
        private void ShowCelebration(string message)
        {
            if (celebrationPanel != null && celebrationText != null)
            {
                celebrationText.text = message;
                celebrationPanel.SetActive(true);
                _celebrationTimer = CELEBRATION_DURATION;
            }
        }

        // Button handlers
        private void OnNextClicked()
        {
            if (_tutorialSubsystem != null)
            {
                // Complete current stage with perfect score
                _tutorialSubsystem.CompleteCurrentStage(1.0f);
            }
        }

        private void OnHintClicked()
        {
            if (_tutorialSubsystem != null)
            {
                _tutorialSubsystem.RequestHint();
            }
        }

        private void OnSkipClicked()
        {
            if (_nineStage != null && _tutorialSubsystem != null)
            {
                // Skip to stage 9 (graduation)
                bool skipped = _nineStage.SkipToStage(9);
                if (skipped)
                {
                    Debug.Log("[DemoTutorialOverlay] Skipped to graduation stage");
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}
