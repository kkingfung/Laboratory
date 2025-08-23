using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;
using UniRx;
using UnityEngine;

#nullable enable

namespace Laboratory.Core.State.Implementations
{
    /// <summary>
    /// Main menu game state - handles menu navigation and game startup.
    /// </summary>
    public class MainMenuGameState : GameStateBase
    {
        public override GameState StateType => GameState.MainMenu;

        public override async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await base.OnEnterAsync(fromState, context);
            
            // Initialize main menu systems
            Debug.Log("MainMenuGameState: Loading main menu UI and systems");
        }

        public override bool CanTransitionTo(GameState targetState)
        {
            // From main menu, can go to most states except None
            return targetState switch
            {
                GameState.None => false,
                GameState.Initializing => false, // Can't go back to initializing
                _ => true
            };
        }
    }

    /// <summary>
    /// Loading game state - handles asset loading and scene transitions.
    /// </summary>
    public class LoadingGameState : GameStateBase
    {
        public override GameState StateType => GameState.Loading;

        public override async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await base.OnEnterAsync(fromState, context);
            
            // Show loading screen and start loading operations
            Debug.Log("LoadingGameState: Starting loading operations");
        }

        public override bool CanTransitionTo(GameState targetState)
        {
            // Loading can transition to any state (loading complete)
            return targetState != GameState.None;
        }
    }

    /// <summary>
    /// Playing game state - handles active gameplay.
    /// </summary>
    public class PlayingGameState : GameStateBase
    {
        public override GameState StateType => GameState.Playing;

        public override async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await base.OnEnterAsync(fromState, context);
            
            // Initialize gameplay systems
            Debug.Log("PlayingGameState: Starting gameplay systems");
        }

        public override bool CanTransitionTo(GameState targetState)
        {
            return targetState switch
            {
                GameState.None => false,
                GameState.Initializing => false,
                GameState.MainMenu => false, // Must go through pause or game over
                _ => true
            };
        }
    }

    /// <summary>
    /// Paused game state - handles game pause and resume.
    /// </summary>
    public class PausedGameState : GameStateBase
    {
        public override GameState StateType => GameState.Paused;

        public override async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await base.OnEnterAsync(fromState, context);
            
            // Pause game systems
            Time.timeScale = 0f;
            Debug.Log("PausedGameState: Game paused");
        }

        public override async UniTask OnExitAsync(GameState toState)
        {
            // Resume game systems
            Time.timeScale = 1f;
            Debug.Log("PausedGameState: Game resumed");
            
            await base.OnExitAsync(toState);
        }

        public override bool CanTransitionTo(GameState targetState)
        {
            return targetState switch
            {
                GameState.None => false,
                GameState.Initializing => false,
                _ => true
            };
        }
    }

    /// <summary>
    /// Game over state - handles end game logic and transitions.
    /// </summary>
    public class GameOverGameState : GameStateBase
    {
        public override GameState StateType => GameState.GameOver;

        public override async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await base.OnEnterAsync(fromState, context);
            
            // Show game over UI and handle end game logic
            Debug.Log("GameOverGameState: Game ended");
        }

        public override bool CanTransitionTo(GameState targetState)
        {
            return targetState switch
            {
                GameState.None => false,
                GameState.Initializing => false,
                GameState.Playing => false, // Can't go back to playing directly
                GameState.Paused => false,
                _ => true
            };
        }
    }
}
