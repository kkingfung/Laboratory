using System;

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Simple cooldown timer utility.
    /// Tracks cooldown time remaining and supports ticking and reset.
    /// </summary>
    public class CooldownTimer
    {
        #region Fields

        private float _duration;
        private float _remaining;

        #endregion

        #region Properties

        /// <summary>Cooldown duration in seconds.</summary>
        public float Duration => _duration;

        /// <summary>Remaining time on cooldown in seconds.</summary>
        public float Remaining => _remaining;

        /// <summary>True if cooldown is currently active.</summary>
        public bool IsActive => _remaining > 0f;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the cooldown timer with a duration.
        /// </summary>
        /// <param name="duration">Duration of cooldown in seconds.</param>
        public CooldownTimer(float duration)
        {
            if (duration < 0f) throw new ArgumentException("Duration must be >= 0");
            _duration = duration;
            _remaining = 0f;
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
        /// Update cooldown timer by deltaTime seconds.
        /// Should be called every frame or tick.
        /// </summary>
        /// <param name="deltaTime">Time in seconds to reduce from Remaining.</param>
        public void Tick(float deltaTime)
        {
            if (_remaining <= 0f) return;

            _remaining -= deltaTime;
            if (_remaining < 0f)
                _remaining = 0f;
        }

        /// <summary>
        /// Resets cooldown to zero (ready).
        /// </summary>
        public void Reset()
        {
            _remaining = 0f;
        }

        #endregion
    }
}
