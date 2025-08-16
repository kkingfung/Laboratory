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
        #region Enums

        /// <summary>
        /// Default game states
        /// </summary>
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

        #endregion

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
        public void RegisterState(GameState gameState, GameStateMachine.IGameState state)
        {
            _stateMachine?.RegisterState(gameState, state);
        }

        /// <summary>
        /// Changes the current game state.
        /// </summary>
        public void ChangeState(GameState gameState)
        {
            _stateMachine?.ChangeState(gameState);
        }

        #endregion
    }
}
