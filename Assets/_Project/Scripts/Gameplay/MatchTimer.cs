using System;
using MessagePipe;
using UniRx;
using UnityEngine;
using TMPro;

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
    }
}
