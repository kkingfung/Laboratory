using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Services;
using Laboratory.Core.State;
using Laboratory.Core.Bootstrap;
using Laboratory.Infrastructure.AsyncUtils;
using System.Threading;

#nullable enable


#region Startup Tasks

namespace Laboratory.Core.Bootstrap.StartupTasks
{
   /// <summary>
    /// Sets up the game state system and transitions to the initial state.
    /// </summary>
    public class GameStateSetupStartupTask : StartupTaskBase
    {
        public override int Priority => 40;
        public override string Name => "Game State Setup";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            var stateService = services.Resolve<IGameStateService>();
            var eventBus = services.Resolve<IEventBus>();
            
            ReportProgress(progress, 0.5f);
            
            // Transition to initializing state
            await stateService.RequestTransitionAsync(GameState.Initializing);
            
            LogInfo("Game state system ready");
            ReportProgress(progress, 1.0f);
        }
    }
}

#endregion