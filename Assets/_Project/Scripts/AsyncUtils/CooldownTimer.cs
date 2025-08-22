using System;
using Laboratory.Core.Timing;

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Simple cooldown timer utility.
    /// Tracks cooldown time remaining and supports ticking and reset.
    /// Now uses the enhanced timer system internally while maintaining backward compatibility.
    /// </summary>
    [System.Obsolete("Use Laboratory.Core.Timing.CooldownTimer instead. This wrapper will be removed in a future version.")]
    public class CooldownTimer : IDisposable
    {
        #region Fields

        private readonly Laboratory.Core.Timing.CooldownTimer _enhancedTimer;

        #endregion

        #region Properties

        /// <summary>Cooldown duration in seconds.</summary>
        public float Duration => _enhancedTimer.Duration;

        /// <summary>Remaining time on cooldown in seconds.</summary>
        public float Remaining => _enhancedTimer.Remaining;

        /// <summary>True if cooldown is currently active.</summary>
        public bool IsActive => _enhancedTimer.IsActive;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes the cooldown timer with a duration.
        /// </summary>
        /// <param name="duration">Duration of cooldown in seconds.</param>
        public CooldownTimer(float duration)
        {
            _enhancedTimer = new Laboratory.Core.Timing.CooldownTimer(duration, autoRegister: false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts or restarts the cooldown timer.
        /// </summary>
        public void Start()
        {
            _enhancedTimer.Start();
        }

        /// <summary>
        /// Attempts to start cooldown only if it's not active.
        /// Returns true if cooldown started, false if already active.
        /// </summary>
        public bool TryStart()
        {
            return _enhancedTimer.TryStart();
        }

        /// <summary>
        /// Update cooldown timer by deltaTime seconds.
        /// Should be called every frame or tick.
        /// </summary>
        /// <param name="deltaTime">Time in seconds to reduce from Remaining.</param>
        public void Tick(float deltaTime)
        {
            _enhancedTimer.Tick(deltaTime);
        }

        /// <summary>
        /// Resets cooldown to zero (ready).
        /// </summary>
        public void Reset()
        {
            _enhancedTimer.Reset();
        }

        /// <summary>
        /// Disposes the enhanced timer resources.
        /// </summary>
        public void Dispose()
        {
            _enhancedTimer?.Dispose();
        }

        #endregion
    }
}
