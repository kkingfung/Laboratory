using System;
using UnityEngine;

namespace Laboratory.Core.Timing
{
    /// <summary>
    /// Enhanced cooldown timer with events and auto-registration support.
    /// Backward compatible with existing CooldownTimer usage.
    /// </summary>
    public class CooldownTimer : ITimer, IDisposable
    {
        #region Fields

        private float _duration;
        private float _remaining;
        private bool _autoRegister;

        #endregion

        #region Properties

        /// <summary>Cooldown duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>Remaining time on cooldown in seconds.</summary>
        public float Remaining => _remaining;

        /// <summary>Elapsed time since timer started.</summary>
        public float Elapsed => _duration - _remaining;

        /// <summary>True if cooldown is currently active.</summary>
        public bool IsActive => _remaining > 0f;

        /// <summary>Progress from 0 (just started) to 1 (completed).</summary>
        public float Progress => _duration > 0f ? Mathf.Clamp01(Elapsed / _duration) : 1f;

        #endregion

        #region Events

        /// <summary>Event fired when cooldown completes.</summary>
        public event Action OnCompleted;

        /// <summary>Event fired each tick with elapsed time.</summary>
        public event Action<float> OnTick;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the cooldown timer with a duration.
        /// </summary>
        /// <param name="duration">Duration of cooldown in seconds.</param>
        /// <param name="autoRegister">Whether to auto-register with TimerService if available.</param>
        public CooldownTimer(float duration, bool autoRegister = true)
        {
            if (duration < 0f) throw new ArgumentException("Duration must be >= 0");
            _duration = duration;
            _remaining = 0f;
            _autoRegister = autoRegister;

            if (_autoRegister && TimerService.Instance != null)
            {
                TimerService.Instance.RegisterTimer(this);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts or restarts the cooldown timer.
        /// </summary>
        public void Start()
        {
            _remaining = _duration;
        }

        /// <summary>
        /// Attempts to start cooldown only if it's not active.
        /// Returns true if cooldown started, false if already active.
        /// </summary>
        public bool TryStart()
        {
            if (IsActive) return false;
            Start();
            return true;
        }

        /// <summary>
        /// Stops the timer without completing it.
        /// </summary>
        public void Stop()
        {
            _remaining = 0f;
        }

        /// <summary>
        /// Update cooldown timer by deltaTime seconds.
        /// Should be called every frame or tick.
        /// </summary>
        /// <param name="deltaTime">Time in seconds to reduce from Remaining.</param>
        public void Tick(float deltaTime)
        {
            if (_remaining <= 0f) return;

            float oldElapsed = Elapsed;
            _remaining -= deltaTime;
            
            if (_remaining < 0f)
                _remaining = 0f;

            OnTick?.Invoke(Elapsed);

            // Fire completion event when timer finishes
            if (_remaining <= 0f && oldElapsed < _duration)
            {
                OnCompleted?.Invoke();
            }
        }

        /// <summary>
        /// Resets cooldown to zero (ready).
        /// </summary>
        public void Reset()
        {
            _remaining = 0f;
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