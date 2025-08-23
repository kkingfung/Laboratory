using System;

namespace Laboratory.Core.Timing
{
    /// <summary>
    /// Common interface for timer implementations.
    /// Provides standardized timer functionality across the system.
    /// </summary>
    public interface ITimer : IDisposable
    {
        /// <summary>Timer duration in seconds.</summary>
        float Duration { get; }

        /// <summary>Remaining time in seconds.</summary>
        float Remaining { get; }

        /// <summary>Elapsed time since timer started.</summary>
        float Elapsed { get; }

        /// <summary>True if timer is currently active/running.</summary>
        bool IsActive { get; }

        /// <summary>Progress from 0 (just started) to 1 (completed).</summary>
        float Progress { get; }

        /// <summary>Event fired when timer completes.</summary>
        event Action OnCompleted;

        /// <summary>Event fired each tick with elapsed time.</summary>
        event Action<float> OnTick;

        /// <summary>Starts or restarts the timer.</summary>
        void Start();

        /// <summary>Attempts to start timer only if not active. Returns true if started.</summary>
        bool TryStart();

        /// <summary>Stops the timer without completing it.</summary>
        void Stop();

        /// <summary>Updates the timer by deltaTime seconds.</summary>
        void Tick(float deltaTime);

        /// <summary>Resets timer to ready state.</summary>
        void Reset();
    }
}
