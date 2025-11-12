using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Laboratory.Tutorial
{
    /// <summary>
    /// Tutorial system for step-by-step player onboarding.
    /// Supports sequential steps, conditions, triggers, and UI highlighting.
    /// Tracks completion state and allows skipping/replaying tutorials.
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Tutorial Settings")]
        [SerializeField] private bool enableTutorials = true;
        [SerializeField] private bool allowSkipping = true;
        [SerializeField] private bool logTutorialEvents = true;
        [SerializeField] private KeyCode skipKey = KeyCode.Escape;

        [Header("UI")]
        [SerializeField] private Canvas tutorialCanvas;
        [SerializeField] private GameObject tutorialPanel;
        [SerializeField] private UnityEngine.UI.Text titleText;
        [SerializeField] private UnityEngine.UI.Text descriptionText;
        [SerializeField] private UnityEngine.UI.Button nextButton;
        [SerializeField] private UnityEngine.UI.Button skipButton;

        #endregion

        #region Private Fields

        private static TutorialSystem _instance;

        // Active tutorial
        private Tutorial _currentTutorial;
        private int _currentStepIndex;
        private bool _isPlayingTutorial;

        // Tutorial registry
        private readonly Dictionary<string, Tutorial> _tutorials = new Dictionary<string, Tutorial>();
        private readonly HashSet<string> _completedTutorials = new HashSet<string>();

        // Step tracking
        private TutorialStep _currentStep;
        private bool _waitingForCondition;

        // Events
        public event Action<Tutorial> OnTutorialStarted;
        public event Action<Tutorial> OnTutorialCompleted;
        public event Action<Tutorial> OnTutorialSkipped;
        public event Action<TutorialStep> OnStepStarted;
        public event Action<TutorialStep> OnStepCompleted;

        // Statistics
        private int _totalTutorialsCompleted;
        private int _totalTutorialsSkipped;

        #endregion

        #region Properties

        public static TutorialSystem Instance => _instance;
        public bool IsEnabled => enableTutorials;
        public bool IsPlayingTutorial => _isPlayingTutorial;
        public Tutorial CurrentTutorial => _currentTutorial;
        public TutorialStep CurrentStep => _currentStep;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!enableTutorials || !_isPlayingTutorial) return;

            // Check skip key
            if (allowSkipping && Input.GetKeyDown(skipKey))
            {
                SkipCurrentTutorial();
            }

            // Check step conditions
            if (_waitingForCondition && _currentStep != null)
            {
                CheckStepCondition();
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[TutorialSystem] Initializing...");

            // Setup UI
            if (tutorialCanvas != null)
            {
                tutorialCanvas.gameObject.SetActive(false);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipCurrentTutorial);
                skipButton.gameObject.SetActive(allowSkipping);
            }

            LoadCompletionState();

            Debug.Log("[TutorialSystem] Initialized");
        }

        #endregion

        #region Tutorial Management

        /// <summary>
        /// Register a tutorial.
        /// </summary>
        public void RegisterTutorial(Tutorial tutorial)
        {
            if (_tutorials.ContainsKey(tutorial.tutorialId))
            {
                Debug.LogWarning($"[TutorialSystem] Tutorial already registered: {tutorial.tutorialId}");
                return;
            }

            _tutorials[tutorial.tutorialId] = tutorial;

            if (logTutorialEvents)
            {
                Debug.Log($"[TutorialSystem] Registered tutorial: {tutorial.tutorialName} ({tutorial.steps.Count} steps)");
            }
        }

        /// <summary>
        /// Start a tutorial by ID.
        /// </summary>
        public void StartTutorial(string tutorialId)
        {
            if (!enableTutorials)
            {
                Debug.LogWarning("[TutorialSystem] Tutorials are disabled");
                return;
            }

            if (_isPlayingTutorial)
            {
                Debug.LogWarning("[TutorialSystem] Another tutorial is already playing");
                return;
            }

            if (!_tutorials.TryGetValue(tutorialId, out var tutorial))
            {
                Debug.LogError($"[TutorialSystem] Tutorial not found: {tutorialId}");
                return;
            }

            // Check if already completed
            if (_completedTutorials.Contains(tutorialId) && !tutorial.allowReplay)
            {
                Debug.Log($"[TutorialSystem] Tutorial already completed: {tutorialId}");
                return;
            }

            _currentTutorial = tutorial;
            _currentStepIndex = 0;
            _isPlayingTutorial = true;

            OnTutorialStarted?.Invoke(tutorial);

            if (logTutorialEvents)
            {
                Debug.Log($"[TutorialSystem] Started tutorial: {tutorial.tutorialName}");
            }

            StartNextStep();
        }

        /// <summary>
        /// Complete the current tutorial.
        /// </summary>
        private void CompleteTutorial()
        {
            if (_currentTutorial == null) return;

            _completedTutorials.Add(_currentTutorial.tutorialId);
            _totalTutorialsCompleted++;

            OnTutorialCompleted?.Invoke(_currentTutorial);

            if (logTutorialEvents)
            {
                Debug.Log($"[TutorialSystem] Completed tutorial: {_currentTutorial.tutorialName}");
            }

            SaveCompletionState();
            CleanupTutorial();
        }

        /// <summary>
        /// Skip the current tutorial.
        /// </summary>
        public void SkipCurrentTutorial()
        {
            if (_currentTutorial == null) return;

            _totalTutorialsSkipped++;

            OnTutorialSkipped?.Invoke(_currentTutorial);

            if (logTutorialEvents)
            {
                Debug.Log($"[TutorialSystem] Skipped tutorial: {_currentTutorial.tutorialName}");
            }

            CleanupTutorial();
        }

        private void CleanupTutorial()
        {
            HideTutorialUI();

            _currentTutorial = null;
            _currentStep = null;
            _currentStepIndex = 0;
            _isPlayingTutorial = false;
            _waitingForCondition = false;
        }

        /// <summary>
        /// Check if a tutorial has been completed.
        /// </summary>
        public bool IsTutorialCompleted(string tutorialId)
        {
            return _completedTutorials.Contains(tutorialId);
        }

        /// <summary>
        /// Reset tutorial completion (for testing).
        /// </summary>
        public void ResetTutorial(string tutorialId)
        {
            _completedTutorials.Remove(tutorialId);
            SaveCompletionState();

            if (logTutorialEvents)
            {
                Debug.Log($"[TutorialSystem] Reset tutorial: {tutorialId}");
            }
        }

        #endregion

        #region Step Management

        private void StartNextStep()
        {
            if (_currentTutorial == null) return;

            // Check if tutorial complete
            if (_currentStepIndex >= _currentTutorial.steps.Count)
            {
                CompleteTutorial();
                return;
            }

            _currentStep = _currentTutorial.steps[_currentStepIndex];

            OnStepStarted?.Invoke(_currentStep);

            if (logTutorialEvents)
            {
                Debug.Log($"[TutorialSystem] Step {_currentStepIndex + 1}/{_currentTutorial.steps.Count}: {_currentStep.title}");
            }

            // Show UI
            ShowTutorialUI();

            // Execute step actions
            _currentStep.onStepStart?.Invoke();

            // Check completion type
            if (_currentStep.requiresCondition)
            {
                _waitingForCondition = true;
                if (nextButton != null)
                    nextButton.gameObject.SetActive(false);
            }
            else
            {
                _waitingForCondition = false;
                if (nextButton != null)
                    nextButton.gameObject.SetActive(true);
            }

            // Auto-advance after delay
            if (_currentStep.autoAdvanceAfterDelay > 0 && !_currentStep.requiresCondition)
            {
                StartCoroutine(AutoAdvanceCoroutine(_currentStep.autoAdvanceAfterDelay));
            }
        }

        private void CompleteCurrentStep()
        {
            if (_currentStep == null) return;

            OnStepCompleted?.Invoke(_currentStep);

            _currentStep.onStepComplete?.Invoke();

            _currentStepIndex++;
            StartNextStep();
        }

        private void OnNextButtonClicked()
        {
            if (!_waitingForCondition)
            {
                CompleteCurrentStep();
            }
        }

        private void CheckStepCondition()
        {
            if (_currentStep == null || !_currentStep.requiresCondition) return;

            if (_currentStep.completionCondition != null && _currentStep.completionCondition.Invoke())
            {
                _waitingForCondition = false;
                CompleteCurrentStep();
            }
        }

        private IEnumerator AutoAdvanceCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_currentStep != null && !_waitingForCondition)
            {
                CompleteCurrentStep();
            }
        }

        #endregion

        #region UI Management

        private void ShowTutorialUI()
        {
            if (tutorialCanvas != null)
            {
                tutorialCanvas.gameObject.SetActive(true);
            }

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }

            if (titleText != null && _currentStep != null)
            {
                titleText.text = _currentStep.title;
            }

            if (descriptionText != null && _currentStep != null)
            {
                descriptionText.text = _currentStep.description;
            }
        }

        private void HideTutorialUI()
        {
            if (tutorialCanvas != null)
            {
                tutorialCanvas.gameObject.SetActive(false);
            }

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
        }

        #endregion

        #region Persistence

        private void SaveCompletionState()
        {
            var completedList = string.Join(",", _completedTutorials);
            PlayerPrefs.SetString("TutorialCompletions", completedList);
            PlayerPrefs.Save();
        }

        private void LoadCompletionState()
        {
            string completedList = PlayerPrefs.GetString("TutorialCompletions", "");
            if (!string.IsNullOrEmpty(completedList))
            {
                foreach (var tutorialId in completedList.Split(','))
                {
                    _completedTutorials.Add(tutorialId);
                }

                if (logTutorialEvents)
                {
                    Debug.Log($"[TutorialSystem] Loaded {_completedTutorials.Count} completed tutorials");
                }
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get tutorial statistics.
        /// </summary>
        public TutorialStats GetStats()
        {
            return new TutorialStats
            {
                registeredTutorials = _tutorials.Count,
                completedTutorials = _completedTutorials.Count,
                totalTutorialsCompleted = _totalTutorialsCompleted,
                totalTutorialsSkipped = _totalTutorialsSkipped,
                isPlayingTutorial = _isPlayingTutorial,
                currentTutorialId = _currentTutorial?.tutorialId,
                currentStepIndex = _currentStepIndex,
                isEnabled = enableTutorials
            };
        }

        /// <summary>
        /// Get all registered tutorials.
        /// </summary>
        public List<Tutorial> GetAllTutorials()
        {
            return new List<Tutorial>(_tutorials.Values);
        }

        /// <summary>
        /// Reset all tutorial completions.
        /// </summary>
        public void ResetAllTutorials()
        {
            _completedTutorials.Clear();
            SaveCompletionState();

            Debug.Log("[TutorialSystem] Reset all tutorials");
        }

        #endregion

        #region Context Menu

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Tutorial System Statistics ===\n" +
                      $"Registered Tutorials: {stats.registeredTutorials}\n" +
                      $"Completed Tutorials: {stats.completedTutorials}\n" +
                      $"Total Completed: {stats.totalTutorialsCompleted}\n" +
                      $"Total Skipped: {stats.totalTutorialsSkipped}\n" +
                      $"Currently Playing: {stats.isPlayingTutorial}\n" +
                      $"Enabled: {stats.isEnabled}");
        }

        [ContextMenu("Reset All Tutorials")]
        private void ResetAllTutorialsMenu()
        {
            ResetAllTutorials();
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// A complete tutorial with multiple steps.
    /// </summary>
    [Serializable]
    public class Tutorial
    {
        public string tutorialId;
        public string tutorialName;
        public string description;
        public List<TutorialStep> steps = new List<TutorialStep>();
        public bool allowReplay = true;
    }

    /// <summary>
    /// A single step in a tutorial.
    /// </summary>
    [Serializable]
    public class TutorialStep
    {
        public string title;
        [TextArea(3, 6)]
        public string description;

        // Completion
        public bool requiresCondition;
        public Func<bool> completionCondition;
        public float autoAdvanceAfterDelay;

        // Highlighting
        public GameObject highlightTarget;
        public bool blockInteraction;

        // Events
        public UnityEvent onStepStart;
        public UnityEvent onStepComplete;
    }

    /// <summary>
    /// Tutorial system statistics.
    /// </summary>
    [Serializable]
    public struct TutorialStats
    {
        public int registeredTutorials;
        public int completedTutorials;
        public int totalTutorialsCompleted;
        public int totalTutorialsSkipped;
        public bool isPlayingTutorial;
        public string currentTutorialId;
        public int currentStepIndex;
        public bool isEnabled;
    }

    #endregion
}
