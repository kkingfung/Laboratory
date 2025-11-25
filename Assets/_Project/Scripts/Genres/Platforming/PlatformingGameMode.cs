using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Genres.Platforming
{
    /// <summary>
    /// Platforming game mode manager
    /// Handles lives, checkpoints, collectibles, and level completion
    /// </summary>
    public class PlatformingGameMode : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private PlatformingConfig config;

        [Header("Player")]
        [SerializeField] private PlatformerController player;
        [SerializeField] private Transform startPosition;

        [Header("Level")]
        [SerializeField] private Transform levelGoal;
        [SerializeField] private List<Transform> checkpoints = new List<Transform>();

        // State
        private int _lives;
        private int _coinsCollected;
        private Transform _currentCheckpoint;
        private float _levelTime;
        private bool _isLevelComplete;
        private bool _isGameOver;

        // Events
        public event System.Action<int> OnLivesChanged;
        public event System.Action<int> OnCoinsChanged;
        public event System.Action<Transform> OnCheckpointReached;
        public event System.Action<float> OnLevelCompleted; // completion time
        public event System.Action OnGameOver;

        private void Start()
        {
            InitializeLevel();
        }

        private void Update()
        {
            if (!_isLevelComplete && !_isGameOver)
            {
                _levelTime += Time.deltaTime;
            }
        }

        /// <summary>
        /// Initialize level
        /// </summary>
        private void InitializeLevel()
        {
            if (config == null)
            {
                Debug.LogError("[PlatformingGameMode] No config assigned!");
                return;
            }

            _lives = config.LivesCount;
            _coinsCollected = 0;
            _currentCheckpoint = startPosition;
            _levelTime = 0f;
            _isLevelComplete = false;
            _isGameOver = false;

            // Spawn player at start
            if (player != null && startPosition != null)
            {
                player.ResetToPosition(startPosition.position);
            }

            Debug.Log($"[PlatformingGameMode] Level initialized with {_lives} lives");
        }

        /// <summary>
        /// Player died
        /// </summary>
        public void PlayerDied()
        {
            _lives--;
            OnLivesChanged?.Invoke(_lives);

            Debug.Log($"[PlatformingGameMode] Player died! Lives remaining: {_lives}");

            if (_lives <= 0)
            {
                GameOver();
            }
            else
            {
                Invoke(nameof(RespawnPlayer), config != null ? config.RespawnDelay : 1f);
            }
        }

        /// <summary>
        /// Respawn player at checkpoint
        /// </summary>
        private void RespawnPlayer()
        {
            if (player != null && _currentCheckpoint != null)
            {
                player.ResetToPosition(_currentCheckpoint.position);
                Debug.Log("[PlatformingGameMode] Player respawned at checkpoint");
            }
        }

        /// <summary>
        /// Reach checkpoint
        /// </summary>
        public void ReachCheckpoint(Transform checkpoint)
        {
            if (config == null || !config.EnableCheckpoints) return;

            _currentCheckpoint = checkpoint;
            OnCheckpointReached?.Invoke(checkpoint);

            Debug.Log($"[PlatformingGameMode] Checkpoint reached: {checkpoint.name}");
        }

        /// <summary>
        /// Collect coin
        /// </summary>
        public void CollectCoin()
        {
            _coinsCollected++;
            OnCoinsChanged?.Invoke(_coinsCollected);

            // Extra life at threshold
            if (config != null && _coinsCollected % config.CoinsToExtraLife == 0)
            {
                GainExtraLife();
            }

            Debug.Log($"[PlatformingGameMode] Coin collected! Total: {_coinsCollected}");
        }

        /// <summary>
        /// Gain extra life
        /// </summary>
        private void GainExtraLife()
        {
            _lives++;
            OnLivesChanged?.Invoke(_lives);

            Debug.Log($"[PlatformingGameMode] Extra life! Lives: {_lives}");
        }

        /// <summary>
        /// Reach level goal
        /// </summary>
        public void ReachGoal()
        {
            if (_isLevelComplete) return;

            _isLevelComplete = true;
            OnLevelCompleted?.Invoke(_levelTime);

            // Check if beat target time
            if (config != null && config.EnableTimeTrial)
            {
                bool beatTime = _levelTime <= config.TargetTime;
                Debug.Log($"[PlatformingGameMode] Level complete! Time: {_levelTime:F2}s {(beatTime ? "(Beat target!)" : "")}");
            }
            else
            {
                Debug.Log($"[PlatformingGameMode] Level complete! Time: {_levelTime:F2}s");
            }
        }

        /// <summary>
        /// Game over
        /// </summary>
        private void GameOver()
        {
            _isGameOver = true;
            OnGameOver?.Invoke();

            Debug.Log("[PlatformingGameMode] Game Over!");
        }

        /// <summary>
        /// Restart level
        /// </summary>
        public void RestartLevel()
        {
            InitializeLevel();
        }

        // Getters
        public int GetLives() => _lives;
        public int GetCoinsCollected() => _coinsCollected;
        public float GetLevelTime() => _levelTime;
        public bool IsLevelComplete() => _isLevelComplete;
        public bool IsGameOver() => _isGameOver;
    }
}
