using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UniRx;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.State
{
    /// <summary>
    /// Game state enumeration defining all possible game states.
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
        NetworkLobby,
        MatchmakingQueue,
        SceneTransition
    }

    /// <summary>
    /// Interface for game state implementations.
    /// Each state handles its own enter/exit logic and updates.
    /// </summary>
    public interface IGameState
    {
        /// <summary>Gets the state type this implementation handles.</summary>
        GameState StateType { get; }
        
        /// <summary>Called when entering this state.</summary>
        UniTask OnEnterAsync(GameState fromState, object? context = null);
        
        /// <summary>Called every frame while this state is active.</summary>
        void OnUpdate();
        
        /// <summary>Called when exiting this state.</summary>
        UniTask OnExitAsync(GameState toState);
        
        /// <summary>Determines if transition to target state is allowed.</summary>
        bool CanTransitionTo(GameState targetState);
    }
}

namespace Laboratory.Core.State.Implementations
{
    /// <summary>
    /// Base class for game state implementations with common functionality.
    /// </summary>
    public abstract class GameStateBase : IGameState
    {
        protected IServiceContainer Services { get; private set; } = null!;
        protected IEventBus EventBus { get; private set; } = null!;
        
        public abstract GameState StateType { get; }

        public virtual async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            // Services are typically injected or resolved here
            Debug.Log($"Entering {StateType} from {fromState}");
            await UniTask.Yield();
        }

        public virtual void OnUpdate() 
        {
            // Override in derived classes for per-frame logic
        }

        public virtual async UniTask OnExitAsync(GameState toState)
        {
            Debug.Log($"Exiting {StateType} to {toState}");
            await UniTask.Yield();
        }

        public virtual bool CanTransitionTo(GameState targetState)
        {
            // Override in derived classes for custom transition rules
            return true;
        }

        protected void InitializeServices(IServiceContainer services)
        {
            Services = services;
            EventBus = services.Resolve<IEventBus>();
        }
    }
}