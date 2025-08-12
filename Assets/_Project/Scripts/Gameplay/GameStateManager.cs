using System;
using MessagePipe;
using UniRx;
using UnityEngine;

#nullable enable

namespace Infrastructure
{
    public class GameStateManager : IDisposable
    {
        public enum GameState
        {
            None,
            Initializing,
            MainMenu,
            Loading,
            Playing,
            Paused,
            GameOver,
            Disconnecting,
        }

        private readonly IMessageBroker _messageBroker;
        private readonly ReactiveProperty<GameState> _currentState = new(GameState.None);

        public IReadOnlyReactiveProperty<GameState> CurrentState => _currentState;

        // Flag to ignore local sending on remote update to avoid loops
        private bool _suppressNetworkBroadcast;

        // Event fired when state changes locally and should be sent over network
        public event Action<GameState>? OnStateChangedForNetwork;

        public GameStateManager(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        /// <summary>
        /// Call this to locally change game state and notify subscribers.
        /// Will trigger network broadcast event.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (_currentState.Value == newState) return;

            var previous = _currentState.Value;
            _currentState.Value = newState;

            _messageBroker.Publish(new GameStateChangedEvent(previous, newState));

            if (!_suppressNetworkBroadcast)
            {
                OnStateChangedForNetwork?.Invoke(newState);
            }
        }

        /// <summary>
        /// Call this to apply state changes received from network.
        /// Will update state but suppress broadcasting back.
        /// </summary>
        public void ApplyRemoteState(GameState newState)
        {
            if (_currentState.Value == newState) return;

            _suppressNetworkBroadcast = true;
            SetState(newState);
            _suppressNetworkBroadcast = false;
        }

        public readonly struct GameStateChangedEvent
        {
            public GameState PreviousState { get; }
            public GameState CurrentState { get; }

            public GameStateChangedEvent(GameState previousState, GameState currentState)
            {
                PreviousState = previousState;
                CurrentState = currentState;
            }
        }

        public void Dispose()
        {
            _currentState?.Dispose();
        }
    }
}
