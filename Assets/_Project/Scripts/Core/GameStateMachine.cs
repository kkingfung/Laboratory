using System;
using System.Collections.Generic;

namespace Laboratory.Core
{
    /// <summary>
    /// Manages game states and transitions between them.
    /// </summary>
    public class GameStateMachine
    {
        #region Fields

        private readonly Dictionary<string, IGameState> _states = new();
        private IGameState? _currentState;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current active game state.
        /// </summary>
        public IGameState? CurrentState => _currentState;

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a new game state.
        /// </summary>
        public void RegisterState(string key, IGameState state)
        {
            if (!_states.ContainsKey(key))
                _states.Add(key, state);
        }

        /// <summary>
        /// Changes the current state to the specified key.
        /// </summary>
        public void ChangeState(string key)
        {
            if (_states.TryGetValue(key, out var newState))
            {
                _currentState?.OnExit();
                _currentState = newState;
                _currentState.OnEnter();
            }
            else
            {
                throw new ArgumentException($"GameState '{key}' not found.");
            }
        }

        /// <summary>
        /// Updates the current state.
        /// </summary>
        public void Update()
        {
            _currentState?.OnUpdate();
        }

        #endregion

        #region Inner Classes, Enums

        /// <summary>
        /// Interface for game state implementations.
        /// </summary>
        public interface IGameState
        {
            void OnEnter();
            void OnUpdate();
            void OnExit();
        }

        #endregion
    }
}
