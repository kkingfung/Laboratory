using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.Genres.Platforming
{
    /// <summary>
    /// Platforming UI displaying lives, coins, and level time
    /// </summary>
    public class PlatformingUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Text livesText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text timeText;
        [SerializeField] private GameObject checkpointIndicator;
        [SerializeField] private GameObject completionPanel;
        [SerializeField] private Text completionTimeText;
        [SerializeField] private Text completionCoinsText;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverText;

        [Header("Hearts Display")]
        [SerializeField] private GameObject heartsContainer;
        [SerializeField] private GameObject heartPrefab;

        [Header("References")]
        [SerializeField] private PlatformingGameMode gameMode;

        // Hearts
        private GameObject[] _heartObjects;

        private void Start()
        {
            // Find game mode if not assigned
            if (gameMode == null)
            {
                gameMode = FindFirstObjectByType<PlatformingGameMode>();
            }

            // Subscribe to events
            if (gameMode != null)
            {
                gameMode.OnLivesChanged += HandleLivesChanged;
                gameMode.OnCoinsChanged += HandleCoinsChanged;
                gameMode.OnCheckpointReached += HandleCheckpointReached;
                gameMode.OnLevelCompleted += HandleLevelCompleted;
                gameMode.OnGameOver += HandleGameOver;
            }

            // Hide panels
            if (completionPanel != null)
            {
                completionPanel.SetActive(false);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }

            if (checkpointIndicator != null)
            {
                checkpointIndicator.SetActive(false);
            }

            // Initialize hearts
            InitializeHearts();

            // Initial display
            UpdateDisplay();
        }

        private void Update()
        {
            UpdateTimeDisplay();
        }

        /// <summary>
        /// Initialize heart display
        /// </summary>
        private void InitializeHearts()
        {
            if (heartsContainer == null || heartPrefab == null || gameMode == null) return;

            int maxLives = gameMode.GetLives();
            _heartObjects = new GameObject[maxLives];

            for (int i = 0; i < maxLives; i++)
            {
                GameObject heart = Instantiate(heartPrefab, heartsContainer.transform);
                _heartObjects[i] = heart;
            }

            UpdateHeartsDisplay();
        }

        /// <summary>
        /// Update hearts visual display
        /// </summary>
        private void UpdateHeartsDisplay()
        {
            if (_heartObjects == null || gameMode == null) return;

            int currentLives = gameMode.GetLives();

            for (int i = 0; i < _heartObjects.Length; i++)
            {
                if (_heartObjects[i] != null)
                {
                    _heartObjects[i].SetActive(i < currentLives);
                }
            }
        }

        /// <summary>
        /// Update all display elements
        /// </summary>
        private void UpdateDisplay()
        {
            UpdateLivesDisplay();
            UpdateCoinsDisplay();
            UpdateTimeDisplay();
        }

        /// <summary>
        /// Update lives text
        /// </summary>
        private void UpdateLivesDisplay()
        {
            if (livesText == null || gameMode == null) return;

            int lives = gameMode.GetLives();
            livesText.text = $"Lives: {lives}";

            // Color coding
            if (lives <= 1)
            {
                livesText.color = Color.red;
            }
            else if (lives <= 3)
            {
                livesText.color = Color.yellow;
            }
            else
            {
                livesText.color = Color.white;
            }

            UpdateHeartsDisplay();
        }

        /// <summary>
        /// Update coins text
        /// </summary>
        private void UpdateCoinsDisplay()
        {
            if (coinsText == null || gameMode == null) return;

            int coins = gameMode.GetCoinsCollected();
            coinsText.text = $"Coins: {coins}";
        }

        /// <summary>
        /// Update time text
        /// </summary>
        private void UpdateTimeDisplay()
        {
            if (timeText == null || gameMode == null) return;

            float time = gameMode.GetLevelTime();
            timeText.text = FormatTime(time);
        }

        /// <summary>
        /// Format time as MM:SS
        /// </summary>
        private string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            return $"Time: {minutes:00}:{seconds:00}";
        }

        /// <summary>
        /// Show checkpoint indicator briefly
        /// </summary>
        private void ShowCheckpointIndicator()
        {
            if (checkpointIndicator != null)
            {
                checkpointIndicator.SetActive(true);
                Invoke(nameof(HideCheckpointIndicator), 2f);
            }
        }

        /// <summary>
        /// Hide checkpoint indicator
        /// </summary>
        private void HideCheckpointIndicator()
        {
            if (checkpointIndicator != null)
            {
                checkpointIndicator.SetActive(false);
            }
        }

        // Event handlers
        private void HandleLivesChanged(int lives)
        {
            UpdateLivesDisplay();
            Debug.Log($"[PlatformingUI] Lives: {lives}");
        }

        private void HandleCoinsChanged(int coins)
        {
            UpdateCoinsDisplay();
            Debug.Log($"[PlatformingUI] Coins: {coins}");
        }

        private void HandleCheckpointReached(Transform checkpoint)
        {
            ShowCheckpointIndicator();
            Debug.Log($"[PlatformingUI] Checkpoint reached: {checkpoint.name}");
        }

        private void HandleLevelCompleted(float completionTime)
        {
            if (completionPanel != null)
            {
                completionPanel.SetActive(true);

                if (completionTimeText != null)
                {
                    int minutes = Mathf.FloorToInt(completionTime / 60f);
                    int seconds = Mathf.FloorToInt(completionTime % 60f);
                    completionTimeText.text = $"Time: {minutes:00}:{seconds:00}";
                }

                if (completionCoinsText != null && gameMode != null)
                {
                    int coins = gameMode.GetCoinsCollected();
                    completionCoinsText.text = $"Coins: {coins}";
                }
            }

            Debug.Log($"[PlatformingUI] Level completed in {completionTime:F2}s");
        }

        private void HandleGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);

                if (gameOverText != null && gameMode != null)
                {
                    int coins = gameMode.GetCoinsCollected();
                    float time = gameMode.GetLevelTime();
                    gameOverText.text = $"Game Over!\nCoins: {coins}\n{FormatTime(time)}";
                }
            }

            Debug.Log("[PlatformingUI] Game Over!");
        }

        /// <summary>
        /// Restart button handler
        /// </summary>
        public void OnRestartClicked()
        {
            if (gameMode != null)
            {
                gameMode.RestartLevel();

                // Hide panels
                if (completionPanel != null)
                {
                    completionPanel.SetActive(false);
                }

                if (gameOverPanel != null)
                {
                    gameOverPanel.SetActive(false);
                }

                // Reinitialize display
                UpdateDisplay();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gameMode != null)
            {
                gameMode.OnLivesChanged -= HandleLivesChanged;
                gameMode.OnCoinsChanged -= HandleCoinsChanged;
                gameMode.OnCheckpointReached -= HandleCheckpointReached;
                gameMode.OnLevelCompleted -= HandleLevelCompleted;
                gameMode.OnGameOver -= HandleGameOver;
            }
        }
    }
}
