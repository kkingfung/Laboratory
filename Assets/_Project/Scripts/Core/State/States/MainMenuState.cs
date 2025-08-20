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
    public class MainMenuGameState : GameStateBase
    {
        public override GameState StateType => GameState.MainMenu;

        public override bool CanTransitionTo(GameState targetState)
        {
            // From main menu, can go to loading, network lobby, or quit
            return targetState == GameState.Loading || 
                   targetState == GameState.NetworkLobby ||
                   targetState == GameState.None;
        }
    }
}