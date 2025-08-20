using System;
using System.Threading;
using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Services;
using Laboratory.Core.State;
using Laboratory.Core.State.Implementations;
using Laboratory.Core.Bootstrap;
using Laboratory.Infrastructure.AsyncUtils;

#nullable enable


#region Startup Tasks

namespace Laboratory.Core.Bootstrap.StartupTasks
{
    /// <summary>
    /// Initializes core service instances and ensures they're properly configured.
    /// </summary>
    public class CoreServicesStartupTask : StartupTaskBase
    {
        public override int Priority => 10;
        public override string Name => "Core Services";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            // Initialize event bus
            var eventBus = services.Resolve<IEventBus>();
            LogInfo("Event bus initialized");
            
            ReportProgress(progress, 0.5f);
            
            // Initialize game state service and register default states
            var stateService = services.Resolve<IGameStateService>();
            RegisterGameStates(stateService);
            LogInfo("Game state service initialized");
            
            ReportProgress(progress, 1.0f);
            await UniTask.Yield();
        }

        private void RegisterGameStates(IGameStateService stateService)
        {
            // Register built-in game states
            stateService.RegisterState(() => new InitializingGameState());
            stateService.RegisterState(() => new MainMenuGameState());
            stateService.RegisterState(() => new LoadingGameState());
            stateService.RegisterState(() => new PlayingGameState());
            stateService.RegisterState(() => new PausedGameState());
            stateService.RegisterState(() => new GameOverGameState());
        }
    }
}

#endregion