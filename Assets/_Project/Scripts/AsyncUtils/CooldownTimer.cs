using System;
// FIXME: tidyup after 8/29
namespace Infrastructure
{
    /// <summary>
    /// Simple cooldown timer utility.
    /// Tracks cooldown time remaining and supports ticking and reset.
    /// </summary>
    public class CooldownTimer
    {
        /// <summary>Cooldown duration in seconds.</summary>
        public float Duration { get; private set; }

        /// <summary>Remaining time on cooldown in seconds.</summary>
        public float Remaining { get; private set; }

        /// <summary>True if cooldown is currently active.</summary>
        public bool IsActive => Remaining > 0f;

        /// <summary>
        /// Initializes the cooldown timer with a duration.
        /// </summary>
        /// <param name="duration">Duration of cooldown in seconds.</param>
        public CooldownTimer(float duration)
        {
            if (duration < 0f) throw new ArgumentException("Duration must be >= 0");
            Duration = duration;
            Remaining = 0f;
        }

        /// <summary>
        /// Starts or restarts the cooldown timer.
        /// </summary>
        public void Start()
        {
            Remaining = Duration;
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
            if (Remaining <= 0f) return;

            Remaining -= deltaTime;
            if (Remaining < 0f)
                Remaining = 0f;
        }

        /// <summary>
        /// Resets cooldown to zero (ready).
        /// </summary>
        public void Reset()
        {
            Remaining = 0f;
        }
    }
}
