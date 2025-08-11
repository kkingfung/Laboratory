using System;
using MessagingPipe;
using UniRx;
using UnityEngine;

namespace Infrastructure
{
    /// <summary>
    /// Manages the countdown timer for a match.
    /// Publishes timer updates and match end events.
    /// </summary>
    public class MatchTimer : IDisposable
    {
        private readonly IMessageBroker _messageBroker;

        private readonly ReactiveProperty<float> _remainingTime = new(0f);

        /// <summary>
        /// Total match duration in seconds.
        /// </summary>
        public float MatchDuration { get; private set; }

        /// <summary>
        /// Current remaining time in seconds (reactive).
        /// </summary>
        public IReadOnlyReactiveProperty<float> RemainingTime => _remainingTime;

        /// <summary>
        /// Is the match timer currently running?
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Event published when match timer reaches zero.
        /// </summary>
        public struct MatchEndedEvent { }

        public MatchTimer(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        /// <summary>
        /// Starts the match timer with specified duration in seconds.
        /// </summary>
        public void Start(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                throw new ArgumentException("Duration must be positive", nameof(durationSeconds));

            MatchDuration = durationSeconds;
            _remainingTime.Value = durationSeconds;
            IsRunning = true;
        }

        /// <summary>
        /// Pauses the match timer.
        /// </summary>
        public void Pause()
        {
            IsRunning = false;
        }

        /// <summary>
        /// Resumes the match timer.
        /// </summary>
        public void Resume()
        {
            if (_remainingTime.Value > 0f)
                IsRunning = true;
        }

        /// <summary>
        /// Resets the match timer to zero and stops.
        /// </summary>
        public void Reset()
        {
            IsRunning = false;
            _remainingTime.Value = 0f;
            MatchDuration = 0f;
        }

        /// <summary>
        /// Call this every frame (e.g. from a system or MonoBehaviour) to update timer.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since last update.</param>
        public void Tick(float deltaTime)
        {
            if (!IsRunning || _remainingTime.Value <= 0f)
                return;

            _remainingTime.Value -= deltaTime;
            if (_remainingTime.Value <= 0f)
            {
                _remainingTime.Value = 0f;
                IsRunning = false;
                _messageBroker.Publish(new MatchEndedEvent());
            }
        }

        public void Dispose()
        {
            _remainingTime?.Dispose();
        }
    }
}
