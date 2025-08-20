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
    public class LoadingGameState : GameStateBase
    {
        public override GameState StateType => GameState.Loading;

        public override bool CanTransitionTo(GameState targetState)
        {
            // Loading can transition to any state
            return true;
        }
    }

    public class PlayingGameState : GameStateBase
    {
        public override GameState StateType => GameState.Playing;

        public override bool CanTransitionTo(GameState targetState)
        {
            // From playing, can pause, end game, or disconnect
            return targetState == GameState.Paused ||
                   targetState == GameState.GameOver ||
                   targetState == GameState.Disconnecting ||
                   targetState == GameState.Loading; // For scene transitions
        }
    }

    public class PausedGameState : GameStateBase
    {
        public override GameState StateType => GameState.Paused;

        public override async UniTask OnEnterAsync(GameState fromState, object? context = null)
        {
            await base.OnEnterAsync(fromState, context);
            Time.timeScale = 0f; // Pause the game
        }

        public override async UniTask OnExitAsync(GameState toState)
        {
            Time.timeScale = 1f; // Resume the game
            await base.OnExitAsync(toState);
        }

        public override bool CanTransitionTo(GameState targetState)
        {
            // From paused, can resume, quit to menu, or end game
            return targetState == GameState.Playing ||
                   targetState == GameState.MainMenu ||
                   targetState == GameState.GameOver;
        }
    }

    public class GameOverGameState : GameStateBase
    {
        public override GameState StateType => GameState.GameOver;

        public override bool CanTransitionTo(GameState targetState)
        {
            // From game over, can restart (loading) or return to menu
            return targetState == GameState.Loading ||
                   targetState == GameState.MainMenu;
        }
    }
}