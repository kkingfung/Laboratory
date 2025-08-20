using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UniRx;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.State
{
    /// <summary>
    /// Unified game state service that replaces both GameStateMachine and GameStateManager.
    /// Provides async state transitions, event publishing, and proper state lifecycle management.
    /// </summary>
    public class GameStateService : IGameStateService, IDisposable
    {
        #region Fields

        private readonly IEventBus _eventBus;
        private readonly Dictionary<GameState, IGameState> _states = new();
        private readonly Dictionary<GameState, Func<IGameState>> _stateFactories = new();
        
        private readonly Subject<GameStateChangedEvent> _stateChangeSubject = new();
        private readonly CompositeDisposable _disposables = new();
        
        private GameState _currentState = GameState.None;
        private IGameState? _currentStateImplementation;
        private bool _isTransitioning = false;
        private bool _disposed = false;

        #endregion

        #region Constructor

        public GameStateService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            // Subscribe to state change requests from other systems
            _eventBus.Subscribe<GameStateChangeRequestedEvent>(OnStateChangeRequested)
                .AddTo(_disposables);
        }

        #endregion

        #region Properties

        public GameState Current => _currentState;
        
        public UniRx.IObservable<GameStateChangedEvent> StateChanges => _stateChangeSubject.AsObservable();

        #endregion

        #region Public Methods

        public async UniTask<bool> RequestTransitionAsync(GameState targetState, object? context = null)
        {
            ThrowIfDisposed();
            
            if (_isTransitioning)
            {
                Debug.LogWarning($"GameStateService: Cannot transition to {targetState} - already transitioning");
                return false;
            }

            if (_currentState == targetState)
            {
                Debug.LogWarning($"GameStateService: Already in state {targetState}");
                return true;
            }

            if (!CanTransitionTo(targetState))
            {
                Debug.LogWarning($"GameStateService: Invalid transition from {_currentState} to {targetState}");
                return false;
            }

            return await PerformTransitionAsync(targetState, context);
        }

        public void ApplyRemoteStateChange(GameState newState, bool suppressEvents = true)
        {
            ThrowIfDisposed();
            
            if (_currentState == newState) return;

            var previousState = _currentState;
            _currentState = newState;
            _currentStateImplementation = GetStateImplementation(newState);

            if (!suppressEvents)
            {
                var stateChangedEvent = new GameStateChangedEvent(previousState, newState);
                _stateChangeSubject.OnNext(stateChangedEvent);
                _eventBus.Publish(stateChangedEvent);
            }

            Debug.Log($"GameStateService: Applied remote state change from {previousState} to {newState}");
        }

        public void RegisterState<T>() where T : IGameState, new()
        {
            ThrowIfDisposed();
            RegisterState(() => new T());
        }

        public void RegisterState<T>(Func<T> factory) where T : IGameState
        {
            ThrowIfDisposed();
            
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            var tempInstance = factory();
            var stateType = tempInstance.StateType;
            
            _stateFactories[stateType] = () => factory();
            Debug.Log($"GameStateService: Registered state {stateType} with type {typeof(T).Name}");
        }

        public IGameState? GetCurrentStateImplementation()
        {
            ThrowIfDisposed();
            return _currentStateImplementation;
        }

        public bool CanTransitionTo(GameState targetState)
        {
            ThrowIfDisposed();
            
            // Allow transitions from None to any state (initialization)
            if (_currentState == GameState.None)
                return true;

            // Check if current state allows this transition
            if (_currentStateImplementation?.CanTransitionTo(targetState) == false)
                return false;

            // Add global transition rules here if needed
            return IsValidGlobalTransition(_currentState, targetState);
        }

        /// <summary>
        /// Updates the current state implementation (call from MonoBehaviour Update).
        /// </summary>
        public void Update()
        {
            ThrowIfDisposed();
            _currentStateImplementation?.OnUpdate();
        }

        #endregion

        #region Private Methods

        private async UniTask<bool> PerformTransitionAsync(GameState targetState, object? context)
        {
            _isTransitioning = true;
            var previousState = _currentState;

            try
            {
                // Publish transition request event
                var requestEvent = new GameStateChangeRequestedEvent(previousState, targetState, context);
                _eventBus.Publish(requestEvent);

                // Exit current state
                if (_currentStateImplementation != null)
                {
                    await _currentStateImplementation.OnExitAsync(targetState);
                }

                // Update current state
                _currentState = targetState;
                _currentStateImplementation = GetStateImplementation(targetState);

                // Enter new state
                if (_currentStateImplementation != null)
                {
                    await _currentStateImplementation.OnEnterAsync(previousState, context);
                }

                // Publish state changed event
                var changedEvent = new GameStateChangedEvent(previousState, targetState);
                _stateChangeSubject.OnNext(changedEvent);
                _eventBus.Publish(changedEvent);

                Debug.Log($"GameStateService: Transitioned from {previousState} to {targetState}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameStateService: Error during transition from {previousState} to {targetState}: {ex}");
                
                // Attempt to revert to previous state on error
                _currentState = previousState;
                _currentStateImplementation = GetStateImplementation(previousState);
                
                return false;
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private IGameState? GetStateImplementation(GameState state)
        {
            if (state == GameState.None)
                return null;

            // Check cached instances first
            if (_states.TryGetValue(state, out var cachedState))
                return cachedState;

            // Create new instance from factory
            if (_stateFactories.TryGetValue(state, out var factory))
            {
                var stateInstance = factory();
                _states[state] = stateInstance; // Cache for reuse
                return stateInstance;
            }

            Debug.LogError($"GameStateService: No implementation registered for state {state}");
            return null;
        }

        private void OnStateChangeRequested(GameStateChangeRequestedEvent requestEvent)
        {
            // Handle external state change requests asynchronously
            RequestTransitionAsync(requestEvent.ToState, requestEvent.Context).Forget();
        }

        private bool IsValidGlobalTransition(GameState from, GameState to)
        {
            // Define global transition rules that apply regardless of state implementation
            // Examples:
            
            // Can't go directly from Playing to MainMenu (must go through Paused or GameOver)
            if (from == GameState.Playing && to == GameState.MainMenu)
                return false;

            // Can't transition to None except during shutdown
            if (to == GameState.None)
                return false;

            // Loading state can transition to any state
            if (from == GameState.Loading)
                return true;

            // Disconnecting can only go to MainMenu or None
            if (from == GameState.Disconnecting)
                return to == GameState.MainMenu;

            return true;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            _disposables?.Dispose();
            _stateChangeSubject?.Dispose();

            // Dispose state implementations
            foreach (var state in _states.Values)
            {
                if (state is IDisposable disposableState)
                {
                    disposableState.Dispose();
                }
            }

            _states.Clear();
            _stateFactories.Clear();
            _disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(GameStateService));
        }

        #endregion
    }
}
