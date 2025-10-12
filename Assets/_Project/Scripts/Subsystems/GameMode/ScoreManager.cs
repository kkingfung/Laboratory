using System;
using Laboratory.Core.Events;
using Laboratory.Core.Infrastructure;
using R3;
using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Gameplay.Scoring
{
    /// <summary>
    /// Manages player or team scores, provides reactive updates and score change events.
    /// Uses UnifiedEventBus for event publishing and R3 for reactive properties.
    /// </summary>
    public class ScoreManager : MonoBehaviour, IDisposable
    {
        #region Fields

        private IEventBus _eventBus;
        private readonly ReactiveProperty<int> _score = new(0);
        private readonly Dictionary<int, int> _playerScores = new();
        private readonly CompositeDisposable _disposables = new();

        [Header("Score Settings")]
        [SerializeField] private int maxScore = 10000;
        [SerializeField] private bool enableDebugLogs = false;

        #endregion

        #region Properties

        /// <summary>
        /// Current total score as a reactive property.
        /// </summary>
        public ReadOnlyReactiveProperty<int> Score { get; private set; }

        /// <summary>
        /// Current score value.
        /// </summary>
        public int CurrentScore => _score.Value;

        /// <summary>
        /// Maximum allowed score.
        /// </summary>
        public int MaxScore => maxScore;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeServices();
            InitializeReactiveProperties();
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Initialization

        private void InitializeServices()
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
                if (enableDebugLogs)
                    Debug.Log("[ScoreManager] EventBus service initialized");
            }
        }

        private void InitializeReactiveProperties()
        {
            Score = _score.ToReadOnlyReactiveProperty().AddTo(_disposables);
            
            // Subscribe to score changes for logging and events
            Score.Subscribe(newScore => {
                if (enableDebugLogs)
                    Debug.Log($"[ScoreManager] Score changed to: {newScore}");
            }).AddTo(_disposables);
        }

        private void SubscribeToEvents()
        {
            // Subscribe to score-related events if needed
            // Example: Reset score when game restarts
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds points to the total score.
        /// </summary>
        /// <param name="points">Points to add, can be negative.</param>
        public void AddScore(int points)
        {
            if (points == 0) return;

            int previous = _score.Value;
            int newScore = Mathf.Clamp(previous + points, 0, maxScore);
            
            if (newScore != previous)
            {
                _score.Value = newScore;
                PublishScoreChangedEvent(previous, newScore, points);
            }
        }

        /// <summary>
        /// Sets the score to a specific value.
        /// </summary>
        /// <param name="newScore">New score value.</param>
        public void SetScore(int newScore)
        {
            newScore = Mathf.Clamp(newScore, 0, maxScore);
            int previous = _score.Value;
            
            if (newScore != previous)
            {
                _score.Value = newScore;
                PublishScoreChangedEvent(previous, newScore, newScore - previous);
            }
        }

        /// <summary>
        /// Resets score to zero.
        /// </summary>
        public void ResetScore()
        {
            int previous = _score.Value;
            if (previous != 0)
            {
                _score.Value = 0;
                PublishScoreChangedEvent(previous, 0, -previous);
            }
        }

        /// <summary>
        /// Gets the score for a given player ID.
        /// </summary>
        public int GetPlayerScore(int playerId)
        {
            return _playerScores.TryGetValue(playerId, out var score) ? score : 0;
        }

        /// <summary>
        /// Adds score to a specific player.
        /// </summary>
        public void AddPlayerScore(int playerId, int amount)
        {
            if (amount == 0) return;

            int previousScore = GetPlayerScore(playerId);
            int newScore = Mathf.Max(0, previousScore + amount);
            
            _playerScores[playerId] = newScore;
            
            // Publish player score changed event
            _eventBus?.Publish(new PlayerScoreChangedEvent(playerId, previousScore, newScore, amount));
            
            if (enableDebugLogs)
                Debug.Log($"[ScoreManager] Player {playerId} score: {previousScore} -> {newScore}");
        }

        /// <summary>
        /// Sets a player's score to a specific value.
        /// </summary>
        public void SetPlayerScore(int playerId, int score)
        {
            score = Mathf.Max(0, score);
            int previousScore = GetPlayerScore(playerId);
            
            if (score != previousScore)
            {
                _playerScores[playerId] = score;
                
                // Publish player score changed event
                _eventBus?.Publish(new PlayerScoreChangedEvent(playerId, previousScore, score, score - previousScore));
                
                if (enableDebugLogs)
                    Debug.Log($"[ScoreManager] Player {playerId} score set to: {score}");
            }
        }

        /// <summary>
        /// Resets all player scores.
        /// </summary>
        public void ResetAllPlayerScores()
        {
            var previousScores = new Dictionary<int, int>(_playerScores);
            _playerScores.Clear();
            
            // Publish reset event
            _eventBus?.Publish(new AllPlayerScoresResetEvent(previousScores));
            
            if (enableDebugLogs)
                Debug.Log("[ScoreManager] All player scores reset");
        }

        /// <summary>
        /// Gets all player scores as a read-only dictionary.
        /// </summary>
        public IReadOnlyDictionary<int, int> GetAllPlayerScores()
        {
            return new Dictionary<int, int>(_playerScores);
        }

        /// <summary>
        /// Gets the highest scoring player ID.
        /// </summary>
        public int GetTopPlayer()
        {
            int topPlayer = -1;
            int topScore = -1;
            
            foreach (var kvp in _playerScores)
            {
                if (kvp.Value > topScore)
                {
                    topScore = kvp.Value;
                    topPlayer = kvp.Key;
                }
            }
            
            return topPlayer;
        }

        #endregion

        #region Private Methods

        private void PublishScoreChangedEvent(int previousScore, int newScore, int delta)
        {
            var scoreEvent = new ScoreChangedEvent(previousScore, newScore, delta);
            _eventBus?.Publish(scoreEvent);
            
            if (enableDebugLogs)
                Debug.Log($"[ScoreManager] Score changed: {previousScore} -> {newScore} (Δ{delta:+#;-#;0})");
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _disposables?.Dispose();
            _score?.Dispose();
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Add 100 Points")]
        private void DebugAdd100Points()
        {
            AddScore(100);
        }

        [ContextMenu("Reset Score")]
        private void DebugResetScore()
        {
            ResetScore();
        }

        [ContextMenu("Add Random Player Score")]
        private void DebugAddRandomPlayerScore()
        {
            int playerId = UnityEngine.Random.Range(1, 5);
            int score = UnityEngine.Random.Range(10, 100);
            AddPlayerScore(playerId, score);
        }

        #endregion
    }

    #region Event Classes

    /// <summary>
    /// Event published when the total score changes.
    /// </summary>
    public class ScoreChangedEvent
    {
        public int PreviousScore { get; }
        public int CurrentScore { get; }
        public int Delta { get; }
        public DateTime Timestamp { get; }

        public ScoreChangedEvent(int previousScore, int currentScore, int delta)
        {
            PreviousScore = previousScore;
            CurrentScore = currentScore;
            Delta = delta;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when a player's score changes.
    /// </summary>
    public class PlayerScoreChangedEvent
    {
        public int PlayerId { get; }
        public int PreviousScore { get; }
        public int CurrentScore { get; }
        public int Delta { get; }
        public DateTime Timestamp { get; }

        public PlayerScoreChangedEvent(int playerId, int previousScore, int currentScore, int delta)
        {
            PlayerId = playerId;
            PreviousScore = previousScore;
            CurrentScore = currentScore;
            Delta = delta;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Event published when all player scores are reset.
    /// </summary>
    public class AllPlayerScoresResetEvent
    {
        public IReadOnlyDictionary<int, int> PreviousScores { get; }
        public DateTime Timestamp { get; }

        public AllPlayerScoresResetEvent(Dictionary<int, int> previousScores)
        {
            PreviousScores = previousScores;
            Timestamp = DateTime.Now;
        }
    }

    #endregion
}
