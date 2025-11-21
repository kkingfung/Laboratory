using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Laboratory.Chimera.Breeding.Core;
using Laboratory.Chimera.Core;
using Laboratory.Chimera.Breeding.Games;
using Unity.Entities;

namespace Laboratory.Chimera.Breeding.UI
{
    /// <summary>
    /// Main manager for interactive breeding mini-games
    /// Orchestrates different game types and provides smooth player experience
    /// </summary>
    public class BreedingGameManager : MonoBehaviour
    {
        [Header("Game Selection")]
        [SerializeField] private Canvas _gameSelectionCanvas;
        [SerializeField] private Button _geneMatchingButton;
        [SerializeField] private Button _dnaSequencingButton;
        [SerializeField] private Button _traitBalancingButton;
        [SerializeField] private Button _incubationButton;
        [SerializeField] private Button _randomGameButton;
        [SerializeField] private Button _skipGameButton;

        [Header("Game UI Containers")]
        [SerializeField] private GameObject _geneMatchingContainer;
        [SerializeField] private GameObject _dnaSequencingContainer;
        [SerializeField] private GameObject _traitBalancingContainer;
        [SerializeField] private GameObject _incubationContainer;

        [Header("Common UI")]
        [SerializeField] private Canvas _gameCanvas;
        [SerializeField] private TextMeshProUGUI _gameTitle;
        [SerializeField] private TextMeshProUGUI _difficultyText;
        [SerializeField] private TextMeshProUGUI _timeRemainingText;
        [SerializeField] private Slider _progressBar;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _exitButton;

        [Header("Parent Display")]
        [SerializeField] private Transform _parent1Display;
        [SerializeField] private Transform _parent2Display;
        [SerializeField] private GameObject _parentInfoPrefab;

        [Header("Results Screen")]
        [SerializeField] private GameObject _resultsPanel;
        [SerializeField] private TextMeshProUGUI _resultsTitle;
        [SerializeField] private TextMeshProUGUI _finalScore;
        [SerializeField] private TextMeshProUGUI _bonusesEarned;
        [SerializeField] private TextMeshProUGUI _breedingOutcome;
        [SerializeField] private Button _proceedButton;
        [SerializeField] private Button _retryButton;

        [Header("Audio & Effects")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _successSound;
        [SerializeField] private AudioClip _failureSound;
        [SerializeField] private AudioClip _countdownSound;
        [SerializeField] private ParticleSystem _successParticles;

        private BreedingGameData _currentGame;
        private IBreedingMiniGame _activeMiniGame;
        private bool _gameInProgress = false;
        private float _currentScore = 0f;

        // Game references
        private GeneMatchingGame _geneMatchingGame;
        private DNASequencingGame _dnaSequencingGame;
        private TraitBalancingGame _traitBalancingGame;
        private IncubationGame _incubationGame;

        private static BreedingGameManager _instance;
        public static BreedingGameManager Instance => _instance;

        // Mock data for demonstration
        private Entity _mockParent1;
        private Entity _mockParent2;
        private VisualGeneticData _mockGenetics1;
        private VisualGeneticData _mockGenetics2;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeComponents();
            SetupUI();
        }

        private void Start()
        {
            GenerateMockParents();
            ShowGameSelection();
        }

        /// <summary>
        /// Initialize mini-game components
        /// </summary>
        private void InitializeComponents()
        {
            _geneMatchingGame = GetComponentInChildren<GeneMatchingGame>();
            _dnaSequencingGame = GetComponentInChildren<DNASequencingGame>();
            _traitBalancingGame = GetComponentInChildren<TraitBalancingGame>();
            _incubationGame = GetComponentInChildren<IncubationGame>();
        }

        /// <summary>
        /// Setup UI event handlers
        /// </summary>
        private void SetupUI()
        {
            if (_geneMatchingButton != null)
                _geneMatchingButton.onClick.AddListener(() => StartGame(BreedingGameType.GeneMatching));

            if (_dnaSequencingButton != null)
                _dnaSequencingButton.onClick.AddListener(() => StartGame(BreedingGameType.DNASequencing));

            if (_traitBalancingButton != null)
                _traitBalancingButton.onClick.AddListener(() => StartGame(BreedingGameType.TraitBalancing));

            if (_incubationButton != null)
                _incubationButton.onClick.AddListener(() => StartGame(BreedingGameType.Incubation));

            if (_randomGameButton != null)
                _randomGameButton.onClick.AddListener(() => StartGame(BreedingGameType.RandomSelection));

            if (_skipGameButton != null)
                _skipGameButton.onClick.AddListener(SkipGames);

            if (_pauseButton != null)
                _pauseButton.onClick.AddListener(PauseGame);

            if (_exitButton != null)
                _exitButton.onClick.AddListener(ExitGame);

            if (_proceedButton != null)
                _proceedButton.onClick.AddListener(ProceedWithBreeding);

            if (_retryButton != null)
                _retryButton.onClick.AddListener(RetryGame);

            // Start with game canvas hidden
            if (_gameCanvas != null)
                _gameCanvas.gameObject.SetActive(false);

            if (_resultsPanel != null)
                _resultsPanel.SetActive(false);
        }

        /// <summary>
        /// Show game selection screen
        /// </summary>
        public void ShowGameSelection()
        {
            if (_gameSelectionCanvas != null)
                _gameSelectionCanvas.gameObject.SetActive(true);

            if (_gameCanvas != null)
                _gameCanvas.gameObject.SetActive(false);

            DisplayParentInfo();
        }

        /// <summary>
        /// Start a breeding mini-game
        /// </summary>
        public void StartGame(BreedingGameType gameType)
        {
            if (_gameInProgress) return;

            // Handle random selection
            if (gameType == BreedingGameType.RandomSelection)
            {
                var gameTypes = new BreedingGameType[] {
                    BreedingGameType.GeneMatching,
                    BreedingGameType.DNASequencing,
                    BreedingGameType.TraitBalancing,
                    BreedingGameType.Incubation
                };
                gameType = gameTypes[Random.Range(0, gameTypes.Length)];
            }

            // Create game session
            _currentGame = BreedingGameData.CreateSession(_mockParent1, _mockParent2, _mockGenetics1, _mockGenetics2, gameType);

            // Hide selection and show game
            if (_gameSelectionCanvas != null)
                _gameSelectionCanvas.gameObject.SetActive(false);

            if (_gameCanvas != null)
                _gameCanvas.gameObject.SetActive(true);

            // Setup game UI
            SetupGameUI();

            // Initialize specific mini-game
            _activeMiniGame = gameType switch
            {
                BreedingGameType.GeneMatching => InitializeGeneMatching(),
                BreedingGameType.DNASequencing => InitializeDNASequencing(),
                BreedingGameType.TraitBalancing => InitializeTraitBalancing(),
                BreedingGameType.Incubation => InitializeIncubation(),
                _ => null
            };

            if (_activeMiniGame != null)
            {
                _gameInProgress = true;
                _currentGame.State = BreedingGameState.Playing;
                StartCoroutine(GameUpdateLoop());
            }
        }

        /// <summary>
        /// Setup common game UI elements
        /// </summary>
        private void SetupGameUI()
        {
            if (_gameTitle != null)
                _gameTitle.text = GetGameTitle(_currentGame.GameType);

            if (_difficultyText != null)
                _difficultyText.text = $"Difficulty: {_currentGame.Difficulty}";

            if (_progressBar != null)
                _progressBar.value = 0f;

            if (_scoreText != null)
                _scoreText.text = "Score: 0";

            _currentScore = 0f;

            // Hide all game containers
            HideAllGameContainers();

            // Show appropriate container
            GameObject targetContainer = _currentGame.GameType switch
            {
                BreedingGameType.GeneMatching => _geneMatchingContainer,
                BreedingGameType.DNASequencing => _dnaSequencingContainer,
                BreedingGameType.TraitBalancing => _traitBalancingContainer,
                BreedingGameType.Incubation => _incubationContainer,
                _ => null
            };

            if (targetContainer != null)
                targetContainer.SetActive(true);
        }

        /// <summary>
        /// Hide all game containers
        /// </summary>
        private void HideAllGameContainers()
        {
            if (_geneMatchingContainer != null) _geneMatchingContainer.SetActive(false);
            if (_dnaSequencingContainer != null) _dnaSequencingContainer.SetActive(false);
            if (_traitBalancingContainer != null) _traitBalancingContainer.SetActive(false);
            if (_incubationContainer != null) _incubationContainer.SetActive(false);
        }

        /// <summary>
        /// Main game update loop
        /// </summary>
        private IEnumerator GameUpdateLoop()
        {
            while (_gameInProgress && _currentGame.State == BreedingGameState.Playing)
            {
                float deltaTime = Time.deltaTime;

                // Update game time
                _currentGame.ElapsedTime += deltaTime;

                // Update active mini-game
                if (_activeMiniGame != null)
                {
                    _activeMiniGame.UpdateGame(deltaTime);
                    _currentScore = _activeMiniGame.GetCurrentScore();

                    // Update game data with mini-game performance
                    float skillPerformance = _activeMiniGame.GetSkillPerformance();
                    _currentGame.UpdateGameProgress(deltaTime, skillPerformance);
                    _currentGame.PerfectMatchesFound = _activeMiniGame.GetPerfectMatches();
                }

                // Update UI
                UpdateGameUI();

                // Check for game completion
                if (_currentGame.ElapsedTime >= _currentGame.TimeLimit)
                {
                    CompleteGame(false); // Time out
                    break;
                }

                if (_activeMiniGame != null && _activeMiniGame.IsGameComplete())
                {
                    CompleteGame(true); // Successful completion
                    break;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Update game UI during gameplay
        /// </summary>
        private void UpdateGameUI()
        {
            // Update time remaining
            if (_timeRemainingText != null)
            {
                float timeLeft = _currentGame.TimeLimit - _currentGame.ElapsedTime;
                _timeRemainingText.text = $"Time: {timeLeft:F1}s";

                // Change color when time is running out
                if (timeLeft < 10f)
                    _timeRemainingText.color = Color.red;
                else if (timeLeft < 20f)
                    _timeRemainingText.color = Color.yellow;
                else
                    _timeRemainingText.color = Color.white;
            }

            // Update progress bar
            if (_progressBar != null && _currentGame.TotalPossibleMatches > 0)
            {
                float progress = (float)_currentGame.PerfectMatchesFound / _currentGame.TotalPossibleMatches;
                _progressBar.value = progress;
            }

            // Update score
            if (_scoreText != null)
                _scoreText.text = $"Score: {_currentScore:F0}";
        }

        /// <summary>
        /// Complete the current game
        /// </summary>
        private void CompleteGame(bool success)
        {
            _gameInProgress = false;
            _currentGame.State = success ? BreedingGameState.Completed : BreedingGameState.Failed;

            // Calculate final results
            var results = CalculateGameResults(success);

            // Show results
            StartCoroutine(ShowResults(results));
        }

        /// <summary>
        /// Calculate game results
        /// </summary>
        private BreedingGameResults CalculateGameResults(bool success)
        {
            var results = new BreedingGameResults
            {
                GameID = _currentGame.GameID,
                GameType = _currentGame.GameType,
                Success = success,
                FinalScore = _currentScore,
                SkillMultiplier = _currentGame.PlayerSkillBonus,
                TimeBonus = Mathf.Max(0.5f, 1.0f - (_currentGame.ElapsedTime / _currentGame.TimeLimit) * 0.5f),
                PerfectionBonus = _currentGame.PerfectMatchesFound == _currentGame.TotalPossibleMatches ? 1.5f : 1.0f,
                OffspringCount = success ? _currentGame.PotentialOffspringCount : 1,
                BonusTraitsEarned = _currentGame.BonusTraitsUnlocked,
                PerfectBreeding = success && _currentGame.BreedingSuccessChance > 0.9f,
                GeneticQualityBonus = _currentGame.SuccessMultiplier,
                ExperienceGained = Mathf.RoundToInt(_currentScore * 0.1f),
                LevelUp = false, // Would check player level system
                NewAbilityUnlocked = success && _currentGame.BreedingSuccessChance > 0.9f
            };

            return results;
        }

        /// <summary>
        /// Show game results
        /// </summary>
        private IEnumerator ShowResults(BreedingGameResults results)
        {
            // Hide game UI
            if (_gameCanvas != null)
                _gameCanvas.gameObject.SetActive(false);

            // Show results panel
            if (_resultsPanel != null)
                _resultsPanel.SetActive(true);

            // Play audio feedback
            if (_audioSource != null)
            {
                AudioClip clip = results.Success ? _successSound : _failureSound;
                if (clip != null) _audioSource.PlayOneShot(clip);
            }

            // Play particle effects for success
            if (results.Success && _successParticles != null)
                _successParticles.Play();

            // Setup results text
            if (_resultsTitle != null)
                _resultsTitle.text = results.Success ? "🎉 Breeding Success!" : "😔 Better Luck Next Time";

            if (_finalScore != null)
                _finalScore.text = $"Final Score: {results.FinalScore:F0}";

            if (_bonusesEarned != null)
            {
                var bonuses = new List<string>();
                if (results.TimeBonus > 1.2f) bonuses.Add("⚡ Speed Bonus");
                if (results.PerfectionBonus > 1.0f) bonuses.Add("💯 Perfect Game");
                if (results.BonusTraitsEarned) bonuses.Add("✨ Bonus Traits");
                if (results.PerfectBreeding) bonuses.Add("🏆 Perfect Breeding");

                _bonusesEarned.text = bonuses.Count > 0 ? string.Join("\n", bonuses) : "No special bonuses";
            }

            if (_breedingOutcome != null)
            {
                string outcome = results.Success
                    ? $"Expected {results.OffspringCount} offspring with {results.GeneticQualityBonus:P0} quality bonus!"
                    : "Breeding attempt failed. Try a different approach or improve your skills.";
                _breedingOutcome.text = outcome;
            }

            yield return new WaitForSeconds(1f);
        }

        /// <summary>
        /// Initialize specific mini-games
        /// </summary>
        private IBreedingMiniGame InitializeGeneMatching()
        {
            if (_geneMatchingGame != null)
            {
                _geneMatchingGame.Initialize(_currentGame);
                return _geneMatchingGame;
            }
            return null;
        }

        private IBreedingMiniGame InitializeDNASequencing()
        {
            if (_dnaSequencingGame != null)
            {
                _dnaSequencingGame.Initialize(_currentGame);
                return _dnaSequencingGame;
            }
            return null;
        }

        private IBreedingMiniGame InitializeTraitBalancing()
        {
            if (_traitBalancingGame != null)
            {
                _traitBalancingGame.Initialize(_currentGame);
                return _traitBalancingGame;
            }
            return null;
        }

        private IBreedingMiniGame InitializeIncubation()
        {
            if (_incubationGame != null)
            {
                _incubationGame.Initialize(_currentGame);
                return _incubationGame;
            }
            return null;
        }

        /// <summary>
        /// Display parent creature information
        /// </summary>
        private void DisplayParentInfo()
        {
            // This would display the genetic information of both parent creatures
            // For now, just show that we have mock data ready
            UnityEngine.Debug.Log($"Parent 1 Stats: {_mockGenetics1.Strength + _mockGenetics1.Vitality + _mockGenetics1.Agility + _mockGenetics1.Intelligence + _mockGenetics1.Adaptability + _mockGenetics1.Social}");
            UnityEngine.Debug.Log($"Parent 2 Stats: {_mockGenetics2.Strength + _mockGenetics2.Vitality + _mockGenetics2.Agility + _mockGenetics2.Intelligence + _mockGenetics2.Adaptability + _mockGenetics2.Social}");
        }

        /// <summary>
        /// Generate mock parent data for demonstration
        /// </summary>
        private void GenerateMockParents()
        {
            _mockGenetics1 = new VisualGeneticData
            {
                Strength = (byte)Random.Range(60, 95),
                Vitality = (byte)Random.Range(50, 90),
                Agility = (byte)Random.Range(70, 95),
                Intelligence = (byte)Random.Range(40, 80),
                Adaptability = (byte)Random.Range(55, 85),
                Social = (byte)Random.Range(45, 75),
                SpecialMarkers = (GeneticMarkerFlags)Random.Range(1, 8)
            };

            _mockGenetics2 = new VisualGeneticData
            {
                Strength = (byte)Random.Range(45, 85),
                Vitality = (byte)Random.Range(65, 95),
                Agility = (byte)Random.Range(50, 80),
                Intelligence = (byte)Random.Range(70, 95),
                Adaptability = (byte)Random.Range(60, 90),
                Social = (byte)Random.Range(55, 85),
                SpecialMarkers = (GeneticMarkerFlags)Random.Range(1, 8)
            };
        }

        /// <summary>
        /// UI Event Handlers
        /// </summary>
        private void SkipGames()
        {
            // Skip mini-games and proceed with basic breeding
            var results = new BreedingGameResults
            {
                Success = true,
                FinalScore = 50f,
                SkillMultiplier = 1.0f,
                OffspringCount = 1,
                GeneticQualityBonus = 1.0f
            };

            StartCoroutine(ShowResults(results));
        }

        private void PauseGame()
        {
            if (_gameInProgress)
            {
                _currentGame.State = BreedingGameState.Paused;
                Time.timeScale = 0f;
                // Show pause menu
            }
        }

        private void ExitGame()
        {
            _gameInProgress = false;
            Time.timeScale = 1f;
            ShowGameSelection();
        }

        private void ProceedWithBreeding()
        {
            // This would trigger the actual breeding process with the game results
            UnityEngine.Debug.Log("Proceeding with breeding based on mini-game results!");

            // Hide all UI and return to main game
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
            if (_gameSelectionCanvas != null) _gameSelectionCanvas.gameObject.SetActive(false);
            if (_gameCanvas != null) _gameCanvas.gameObject.SetActive(false);
        }

        private void RetryGame()
        {
            if (_resultsPanel != null) _resultsPanel.SetActive(false);
            StartGame(_currentGame.GameType);
        }

        /// <summary>
        /// Helper methods
        /// </summary>
        private string GetGameTitle(BreedingGameType gameType)
        {
            return gameType switch
            {
                BreedingGameType.GeneMatching => "🧬 Gene Matching",
                BreedingGameType.DNASequencing => "🔬 DNA Sequencing",
                BreedingGameType.TraitBalancing => "⚖️ Trait Balancing",
                BreedingGameType.Incubation => "🥚 Incubation Control",
                _ => "Breeding Mini-Game"
            };
        }

        /// <summary>
        /// Public API for external systems
        /// </summary>
        public static void StartBreedingSession(Entity parent1, Entity parent2, VisualGeneticData genetics1, VisualGeneticData genetics2)
        {
            if (Instance != null)
            {
                Instance._mockParent1 = parent1;
                Instance._mockParent2 = parent2;
                Instance._mockGenetics1 = genetics1;
                Instance._mockGenetics2 = genetics2;
                Instance.ShowGameSelection();
            }
        }

        public static bool IsGameInProgress()
        {
            return Instance != null && Instance._gameInProgress;
        }
    }

    /// <summary>
    /// Interface for breeding mini-games
    /// </summary>
    public interface IBreedingMiniGame
    {
        void Initialize(BreedingGameData gameData);
        void UpdateGame(float deltaTime);
        float GetCurrentScore();
        float GetSkillPerformance();
        int GetPerfectMatches();
        bool IsGameComplete();
    }

    // Stub classes for missing breeding game types
    public class TraitBalancingGame : MonoBehaviour, IBreedingMiniGame
    {
        public void Initialize(BreedingGameData gameData) { }
        public void UpdateGame(float deltaTime) { }
        public float GetCurrentScore() => 0f;
        public float GetSkillPerformance() => 0f;
        public int GetPerfectMatches() => 0;
        public bool IsGameComplete() => false;
    }

    public class IncubationGame : MonoBehaviour, IBreedingMiniGame
    {
        public void Initialize(BreedingGameData gameData) { }
        public void UpdateGame(float deltaTime) { }
        public float GetCurrentScore() => 0f;
        public float GetSkillPerformance() => 0f;
        public int GetPerfectMatches() => 0;
        public bool IsGameComplete() => false;
    }
}