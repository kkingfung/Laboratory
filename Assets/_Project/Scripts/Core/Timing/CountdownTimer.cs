using System;
using UnityEngine;

namespace Laboratory.Core.Timing
{
    /// <summary>
    /// Countdown timer that counts down from duration to zero.
    /// Perfect for match timers, loading timeouts, and UI countdowns.
    /// </summary>
    public class CountdownTimer : ITimer, IDisposable
    {
        #region Fields

        private float _duration;
        private float _remaining;
        private bool _isActive;
        private bool _autoRegister;

        #endregion

        #region Properties

        /// <summary>Timer duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>Remaining time in seconds.</summary>
        public float Remaining => _remaining;

        /// <summary>Elapsed time since timer started.</summary>
        public float Elapsed => _duration - _remaining;

        /// <summary>True if timer is currently running.</summary>
        public bool IsActive => _isActive && _remaining > 0f;

        /// <summary>Progress from 0 (just started) to 1 (completed).</summary>
        public float Progress => _duration > 0f ? Mathf.Clamp01(Elapsed / _duration) : 1f;

        #endregion

        #region Events

        /// <summary>Event fired when countdown reaches zero.</summary>
        public event Action OnCompleted;

        /// <summary>Event fired each tick with remaining time.</summary>
        public event Action<float> OnTick;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the countdown timer with a duration.
        /// </summary>
        /// <param name="duration">Duration to count down from in seconds.</param>
        /// <param name="autoRegister">Whether to auto-register with TimerService if available.</param>
        public CountdownTimer(float duration, bool autoRegister = true)
        {
            if (duration < 0f) throw new ArgumentException("Duration must be >= 0");
            _duration = duration;
            _remaining = duration;
            _isActive = false;
            _autoRegister = autoRegister;

            if (_autoRegister && TimerService.Instance != null)
            {
                TimerService.Instance.RegisterTimer(this);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts or restarts the countdown timer.
        /// </summary>
        public void Start()
        {
            _remaining = _duration;
            _isActive = true;
        }

        /// <summary>
        /// Attempts to start timer only if not active. Returns true if started.
        /// </summary>
        public bool TryStart()
        {
            if (_isActive) return false;
            Start();
            return true;
        }

        /// <summary>
        /// Stops the timer.
        /// </summary>
        public void Stop()
        {
            _isActive = false;
        }

        /// <summary>
        /// Resets timer to initial state (full duration, not running).
        /// </summary>
        public void Reset()
        {
            _remaining = _duration;
            _isActive = false;
        }

        /// <summary>
        /// Updates countdown timer by deltaTime seconds.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last tick.</param>
        public void Tick(float deltaTime)
        {
            if (!_isActive || _remaining <= 0f) return;

            float oldRemaining = _remaining;
            _remaining -= deltaTime;
            
            if (_remaining < 0f)
                _remaining = 0f;

            OnTick?.Invoke(_remaining);

            // Fire completion event when timer reaches zero
            if (_remaining <= 0f && oldRemaining > 0f)
            {
                _isActive = false;
                OnCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Sets remaining time directly (useful for syncing across network).
        /// </summary>
        /// <param name="remainingTime">New remaining time in seconds.</param>
        public void SetRemainingTime(float remainingTime)
        {
            _remaining = Mathf.Clamp(remainingTime, 0f, _duration);
        }

        /// <summary>
        /// Releases resources and unregisters from TimerService.
        /// </summary>
        public void Dispose()
        {
            if (_autoRegister && TimerService.Instance != null)
            {
                TimerService.Instance.UnregisterTimer(this);
            }
            
            OnCompleted = null;
            OnTick = null;
        }

        #endregion
    }
}