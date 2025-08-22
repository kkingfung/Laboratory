using System;

namespace Laboratory.Core.Timing
{
    /// <summary>
    /// Common interface for all timer implementations.
    /// Provides unified API for cooldowns, countdowns, and progress timers.
    /// </summary>
    public interface ITimer
    {
        /// <summary>Timer duration in seconds.</summary>
        float Duration { get; }
        
        /// <summary>Remaining time in seconds.</summary>
        float Remaining { get; }
        
        /// <summary>Elapsed time in seconds.</summary>
        float Elapsed { get; }
        
        /// <summary>True if timer is currently active/running.</summary>
        bool IsActive { get; }
        
        /// <summary>Normalized progress from 0 to 1 (0 = start, 1 = complete).</summary>
        float Progress { get; }
        
        /// <summary>Event fired when timer completes.</summary>
        event Action OnCompleted;
        
        /// <summary>Event fired each tick with elapsed time.</summary>
        event Action<float> OnTick;
        
        /// <summary>Starts or restarts the timer.</summary>
        void Start();
        
        /// <summary>Stops the timer without completing it.</summary>
        void Stop();
        
        /// <summary>Resets timer to initial state.</summary>
        void Reset();
        
        /// <summary>Updates timer by deltaTime seconds.</summary>
        /// <param name="deltaTime">Time elapsed since last tick</param>
        void Tick(float deltaTime);
    }
}