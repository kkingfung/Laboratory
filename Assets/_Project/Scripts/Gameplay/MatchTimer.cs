using System;
using MessagePipe;
using UniRx;
using UnityEngine;
using TMPro;

namespace Infrastructure
{
    /// <summary>
    /// Manages the countdown timer for a match.
    /// Publishes timer updates and match end events.
    /// </summary>
    public class MatchTimer : IDisposable
    {
        private readonly IMessageBroker _messageBroker;

        private readonly ReactiveProperty<float> _remainingTime = new(0f);

        /// <summary>
        /// Total match duration in seconds.
        /// </summary>
        public float MatchDuration { get; private set; }

        /// <summary>
        /// Current remaining time in seconds (reactive).
        /// </summary>
        public IReadOnlyReactiveProperty<float> RemainingTime => _remainingTime;

        /// <summary>
        /// Is the match timer currently running?
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Event published when match timer reaches zero.
        /// </summary>
        public struct MatchEndedEvent { }

        public MatchTimer(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        /// <summary>
        /// Starts the match timer with specified duration in seconds.
        /// </summary>
        public void Start(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                throw new ArgumentException("Duration must be positive", nameof(durationSeconds));

            MatchDuration = durationSeconds;
            _remainingTime.Value = durationSeconds;
            IsRunning = true;
        }

        /// <summary>
        /// Pauses the match timer.
        /// </summary>
        public void Pause()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Resumes the match timer.
        /// </summary>
        public void Resume()
        {
            if (_remainingTime.Value > 0f)
                IsRunning = true;
        }

        /// <summary>
        /// Resets the match timer to zero and stops.
        /// </summary>
        public void Reset()
        {
            IsRunning = false;
            _remainingTime.Value = 0f;
            MatchDuration = 0f;
        }

        /// <summary>
        /// Call this every frame (e.g. from a system or MonoBehaviour) to update timer.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        public void Tick(float deltaTime)
        {
            if (!IsRunning || _remainingTime.Value <= 0f)
                return;

            _remainingTime.Value -= deltaTime;
            if (_remainingTime.Value <= 0f)
            {
                _remainingTime.Value = 0f;
                IsRunning = false;
                _messageBroker.Publish(new MatchEndedEvent());
            }
        }

        public void Dispose()
        {
            _remainingTime?.Dispose();
        }
    }
}

namespace Laboratory.Gameplay.Lobby
{
    /// <summary>
    /// Tracks and displays the match timer.
    /// </summary>
    public class MatchTimer : MonoBehaviour
    {
        #region Fields

        [Header("UI Reference")]
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Timer Settings")]
        [SerializeField] private float matchDuration = 300f; // seconds

        private float _elapsedTime;
        private bool _isRunning;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the remaining time in seconds.
        /// </summary>
        public float RemainingTime => Mathf.Max(matchDuration - _elapsedTime, 0f);

        /// <summary>
        /// Gets whether the timer is running.
        /// </summary>
        public bool IsRunning => _isRunning;

        #endregion

        #region Unity Override Methods

        private void Update()
        {
            if (!_isRunning) return;

            _elapsedTime += Time.deltaTime;
            UpdateTimerUI();

            if (_elapsedTime >= matchDuration)
            {
                _isRunning = false;
                OnMatchEnded();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the match timer.
        /// </summary>
        public void StartTimer()
        {
            _elapsedTime = 0f;
            _isRunning = true;
            UpdateTimerUI();
        }

        /// <summary>
        /// Stops the match timer.
        /// </summary>
        public void StopTimer()
        {
            _isRunning = false;
        }

        #endregion

        #region Private Methods

        private void UpdateTimerUI()
        {
            if (timerText != null)
            {
                float timeLeft = RemainingTime;
                int minutes = Mathf.FloorToInt(timeLeft / 60f);
                int seconds = Mathf.FloorToInt(timeLeft % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void OnMatchEnded()
        {
            // Handle match end logic here (e.g., notify manager, show UI)
        }

        #endregion

        #region Inner Classes, Enums

        // No inner classes or enums
    }
}
