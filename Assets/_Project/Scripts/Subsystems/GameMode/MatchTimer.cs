using System;
using Laboratory.Core.Events;
using Laboratory.Infrastructure.Core;
using R3;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Core.Timing;

namespace Laboratory.Gameplay.Lobby
{
    /// <summary>
    /// Tracks and displays the match timer.
    /// </summary>
    public class MatchTimer : MonoBehaviour, IMatchTimer
    {
        #region Fields

        [Header("UI Reference")]
        [SerializeField] private Text timerText;

        [Header("Timer Settings")]
        [SerializeField] private float matchDuration = 300f; // seconds

        private CountdownTimer _matchTimer;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the remaining time in seconds.
        /// </summary>
        public float RemainingTime => _matchTimer?.Remaining ?? 0f;

        /// <summary>
        /// Gets whether the timer is running.
        /// </summary>
        public bool IsRunning => _matchTimer?.IsActive ?? false;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            InitializeTimer();
        }

        private void OnDestroy()
        {
            _matchTimer?.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the match timer.
        /// </summary>
        public void StartTimer()
        {
            _matchTimer?.Start();
        }

        /// <summary>
        /// Stops the match timer.
        /// </summary>
        public void StopTimer()
        {
            _matchTimer?.Stop();
        }

        /// <summary>
        /// Updates the timer with a specified delta time.
        /// This method allows external systems to control timer updates.
        /// Note: This is optional since the TimerService handles automatic updates.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last tick</param>
        public void Tick(float deltaTime)
        {
            _matchTimer?.Tick(deltaTime);
        }

        /// <summary>
        /// Sets the match duration and reinitializes the timer.
        /// </summary>
        /// <param name="duration">New match duration in seconds</param>
        public void SetMatchDuration(float duration)
        {
            matchDuration = duration;
            InitializeTimer();
        }

        #endregion

        #region Private Methods

        private void InitializeTimer()
        {
            // Dispose existing timer if any
            _matchTimer?.Dispose();
            
            // Create new countdown timer
            _matchTimer = new CountdownTimer(matchDuration);
            _matchTimer.OnCompleted += OnMatchEnded;
            _matchTimer.OnTick += (remainingTime) => UpdateTimerUI(remainingTime);
        }

        private void UpdateTimerUI(float remainingTime)
        {
            if (timerText != null)
            {
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                timerText.text = $"{minutes:00}:{seconds:00}";
            }
        }

        private void UpdateTimerUI()
        {
            UpdateTimerUI(RemainingTime);
        }

        private void OnMatchEnded()
        {
            // Handle match end logic here (e.g., notify manager, show UI)
            Debug.Log("Match timer ended!");
        }

        #endregion
    }
}
