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
  public class InitializingGameState : GameStateBase
    {
        public override GameState StateType => GameState.Initializing;

        public override bool CanTransitionTo(GameState targetState)
        {
            // Can transition to any state from initializing
            return targetState != GameState.None;
        }
    }
}