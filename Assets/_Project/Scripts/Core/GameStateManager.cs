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
        private readonly Subject<GameState> _stateChangeSubject = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current game state.
        /// </summary>
        public GameStateMachine.IGameState? CurrentState => _stateMachine?.CurrentState;

        /// <summary>
        /// Observable that emits when game state changes.
        /// </summary>
        public UniRx.IObservable<GameState> OnStateChanged => _stateChangeSubject.AsObservable();

        /// <summary>
        /// Gets the current game state as an enum.
        /// </summary>
        public GameState CurrentGameState { get; private set; } = GameState.None;

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
            if (CurrentGameState != gameState)
            {
                _stateMachine?.ChangeState(gameState);
                CurrentGameState = gameState;
                _stateChangeSubject.OnNext(gameState);
            }
        }

        /// <summary>
        /// Applies a remote game state change without broadcasting.
        /// Used for network synchronization to prevent message loops.
        /// </summary>
        /// <param name="gameState">The game state to apply from remote source.</param>
        public void ApplyRemoteState(GameState gameState)
        {
            // Apply the state change without any network broadcasting
            // This prevents infinite loops when receiving state sync messages
            if (CurrentGameState != gameState)
            {
                _stateMachine?.ChangeState(gameState);
                CurrentGameState = gameState;
                _stateChangeSubject.OnNext(gameState);
            }
            
            // Log for debugging network sync
            Debug.Log($"GameStateManager: Applied remote state change to {gameState}");
        }

        #endregion

        #region Unity Cleanup

        private void OnDestroy()
        {
            _stateChangeSubject?.Dispose();
        }

        #endregion
    }
}
