using UnityEngine;

namespace Laboratory.Core.Timing
{
    /// <summary>
    /// Interface for match timer functionality.
    /// Provides a clean abstraction for ECS systems to interact with match timers
    /// without requiring direct assembly dependencies.
    /// </summary>
    public interface IMatchTimer
    {
        /// <summary>
        /// Gets the remaining time in seconds.
        /// </summary>
        float RemainingTime { get; }

        /// <summary>
        /// Gets whether the timer is currently running.
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        /// Starts the match timer.
        /// </summary>
        void StartTimer();

        /// <summary>
        /// Stops the match timer.
        /// </summary>
        void StopTimer();

        /// <summary>
        /// Updates the timer with a specified delta time.
        /// </summary>
        /// <param name="deltaTime">The time elapsed since the last tick</param>
        void Tick(float deltaTime);

        /// <summary>
        /// Sets the match duration and reinitializes the timer.
        /// </summary>
        /// <param name="duration">New match duration in seconds</param>
        void SetMatchDuration(float duration);
    }
}
