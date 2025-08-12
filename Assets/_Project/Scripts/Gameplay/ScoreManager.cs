using System;
using MessagePipe;
using UniRx;

namespace Infrastructure
{
    /// <summary>
    /// Manages player or team scores, provides reactive updates and score change events.
    /// </summary>
    public class ScoreManager : IDisposable
    {
        private readonly IMessageBroker _messageBroker;

        private readonly ReactiveProperty<int> _score = new(0);

        /// <summary>
        /// Reactive read-only score property.
        /// </summary>
        public IReadOnlyReactiveProperty<int> Score => _score;

        public ScoreManager(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        /// <summary>
        /// Adds points to the score.
        /// </summary>
        /// <param name="points">Points to add, can be negative.</param>
        public void AddScore(int points)
        {
            if (points == 0) return;

            int previous = _score.Value;
            _score.Value += points;

            _messageBroker.Publish(new ScoreChangedEvent(previous, _score.Value));
        }

        /// <summary>
        /// Resets score to zero.
        /// </summary>
        public void Reset()
        {
            int previous = _score.Value;
            _score.Value = 0;
            _messageBroker.Publish(new ScoreChangedEvent(previous, 0));
        }

        /// <summary>
        /// Event published when score changes.
        /// </summary>
        public readonly struct ScoreChangedEvent
        {
            public int PreviousScore { get; }
            public int CurrentScore { get; }

            public ScoreChangedEvent(int previousScore, int currentScore)
            {
                PreviousScore = previousScore;
                CurrentScore = currentScore;
            }
        }

        public void Dispose()
        {
            _score?.Dispose();
        }
    }
}
