using UnityEngine;
using Laboratory.Tutorial;

namespace Laboratory.Subsystems.Tutorial
{
    /// <summary>
    /// Subsystem manager for Tutorial system
    /// Coordinates both basic tutorials and 9-stage onboarding
    /// Follows Project Chimera architecture pattern
    /// </summary>
    public class TutorialSubsystemManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TutorialConfig config;

        [Header("Components")]
        [SerializeField] private TutorialSystem basicTutorialSystem;
        [SerializeField] private NineStageOnboarding nineStageSystem;

        // Singleton
        private static TutorialSubsystemManager _instance;
        public static TutorialSubsystemManager Instance => _instance;

        // State
        private bool _isInitialized = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeComponents();
        }

        private void Start()
        {
            InitializeSubsystem();
        }

        /// <summary>
        /// Initialize required components
        /// </summary>
        private void InitializeComponents()
        {
            // Find or create basic tutorial system
            if (basicTutorialSystem == null)
            {
                basicTutorialSystem = GetComponentInChildren<TutorialSystem>();
                if (basicTutorialSystem == null)
                {
                    basicTutorialSystem = FindFirstObjectByType<TutorialSystem>();
                }
            }

            // Find or create 9-stage system
            if (nineStageSystem == null)
            {
                nineStageSystem = GetComponentInChildren<NineStageOnboarding>();
                if (nineStageSystem == null)
                {
                    GameObject nineStageObj = new GameObject("NineStageOnboarding");
                    nineStageObj.transform.SetParent(transform);
                    nineStageSystem = nineStageObj.AddComponent<NineStageOnboarding>();
                }
            }
        }

        /// <summary>
        /// Initialize tutorial subsystem
        /// </summary>
        private void InitializeSubsystem()
        {
            if (_isInitialized) return;

            if (config == null)
            {
                Debug.LogWarning("[TutorialSubsystem] No configuration assigned!");
            }

            // Subscribe to 9-stage events
            if (nineStageSystem != null)
            {
                nineStageSystem.OnStageStarted += HandleStageStarted;
                nineStageSystem.OnStageCompleted += HandleStageCompleted;
                nineStageSystem.OnOnboardingCompleted += HandleOnboardingCompleted;
            }

            _isInitialized = true;
            Debug.Log("[TutorialSubsystem] Initialized");
        }

        /// <summary>
        /// Start 9-stage onboarding for new players
        /// </summary>
        public void StartOnboarding()
        {
            if (nineStageSystem != null)
            {
                nineStageSystem.StartOnboarding();
            }
            else
            {
                Debug.LogError("[TutorialSubsystem] Nine-stage system not found!");
            }
        }

        /// <summary>
        /// Complete current onboarding stage
        /// </summary>
        public void CompleteCurrentStage(float performanceScore = 1f)
        {
            if (nineStageSystem != null)
            {
                nineStageSystem.CompleteCurrentStage(performanceScore);
            }
        }

        /// <summary>
        /// Record a mistake in current stage
        /// </summary>
        public void RecordMistake()
        {
            if (nineStageSystem != null)
            {
                nineStageSystem.RecordMistake();
            }
        }

        /// <summary>
        /// Request a hint
        /// </summary>
        public void RequestHint()
        {
            if (nineStageSystem != null)
            {
                nineStageSystem.RequestHint();
            }
        }

        /// <summary>
        /// Start a basic tutorial by ID
        /// </summary>
        public void StartTutorial(string tutorialId)
        {
            if (basicTutorialSystem != null)
            {
                basicTutorialSystem.StartTutorial(tutorialId);
            }
            else
            {
                Debug.LogError("[TutorialSubsystem] Basic tutorial system not found!");
            }
        }

        /// <summary>
        /// Register a tutorial definition
        /// </summary>
        public void RegisterTutorial(Laboratory.Tutorial.Tutorial tutorial)
        {
            if (basicTutorialSystem != null)
            {
                basicTutorialSystem.RegisterTutorial(tutorial);
            }
        }

        /// <summary>
        /// Get nine-stage system
        /// </summary>
        public NineStageOnboarding GetNineStageSystem()
        {
            return nineStageSystem;
        }

        /// <summary>
        /// Get basic tutorial system
        /// </summary>
        public TutorialSystem GetBasicTutorialSystem()
        {
            return basicTutorialSystem;
        }

        /// <summary>
        /// Get configuration
        /// </summary>
        public TutorialConfig GetConfig()
        {
            return config;
        }

        /// <summary>
        /// Check if onboarding is completed
        /// </summary>
        public bool HasCompletedOnboarding()
        {
            return nineStageSystem != null && nineStageSystem.HasCompletedOnboarding();
        }

        /// <summary>
        /// Get onboarding progress percentage
        /// </summary>
        public float GetOnboardingProgress()
        {
            if (nineStageSystem != null)
            {
                return nineStageSystem.GetProgressPercentage();
            }
            return 0f;
        }

        // Event handlers
        private void HandleStageStarted(OnboardingStage stage)
        {
            Debug.Log($"[TutorialSubsystem] Stage started: {stage}");
        }

        private void HandleStageCompleted(OnboardingStage stage, StagePerformance performance)
        {
            Debug.Log($"[TutorialSubsystem] Stage completed: {stage}. Score: {performance.Score:F2}");
        }

        private void HandleOnboardingCompleted()
        {
            Debug.Log("[TutorialSubsystem] Onboarding completed!");
            // Could save completion to player profile here
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (nineStageSystem != null)
            {
                nineStageSystem.OnStageStarted -= HandleStageStarted;
                nineStageSystem.OnStageCompleted -= HandleStageCompleted;
                nineStageSystem.OnOnboardingCompleted -= HandleOnboardingCompleted;
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
