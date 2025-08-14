using System;
using MessagePipe;
using UniRx;
using UnityEngine;

#nullable enable

namespace Laboratory.Core
{
    /// <summary>
    /// Controls the GameStateMachine and handles state transitions for the game.
    /// </summary>
    public class GameStateManager : MonoBehaviour
    {
        #region Fields

        private GameStateMachine _stateMachine;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current game state.
        /// </summary>
        public GameStateMachine.IGameState? CurrentState => _stateMachine?.CurrentState;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            _stateMachine = new GameStateMachine();
            // Register initial states here if needed
        }

        private void Update()
        {
            _stateMachine?.Update();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Registers a new game state.
        /// </summary>
        public void RegisterState(string key, GameStateMachine.IGameState state)
        {
            _stateMachine?.RegisterState(key, state);
        }

        /// <summary>
        /// Changes the current game state.
        /// </summary>
        public void ChangeState(string key)
        {
            _stateMachine?.ChangeState(key);
        }

        #endregion
    }
}
