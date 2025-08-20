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
    /// Service interface for managing game state transitions and state machine logic.
    /// Combines the responsibilities of GameStateMachine and GameStateManager.
    /// </summary>
    public interface IGameStateService
    {
        /// <summary>Gets the current active game state.</summary>
        GameState Current { get; }
        
        /// <summary>Observable that emits when game state changes.</summary>
        UniRx.IObservable<GameStateChangedEvent> StateChanges { get; }
        
        /// <summary>Requests a state transition with optional context data.</summary>
        UniTask<bool> RequestTransitionAsync(GameState targetState, object? context = null);
        
        /// <summary>Applies a remote state change (for network synchronization).</summary>
        void ApplyRemoteStateChange(GameState newState, bool suppressEvents = true);
        
        /// <summary>Registers a state implementation.</summary>
        void RegisterState<T>() where T : IGameState, new();
        
        /// <summary>Registers a state implementation with a factory.</summary>
        void RegisterState<T>(Func<T> factory) where T : IGameState;
        
        /// <summary>Gets the current state implementation.</summary>
        IGameState? GetCurrentStateImplementation();
        
        /// <summary>Checks if a transition to the target state is valid.</summary>
        bool CanTransitionTo(GameState targetState);
        
        /// <summary>Updates the current state implementation (call from MonoBehaviour Update).</summary>
        void Update();
    }
}
