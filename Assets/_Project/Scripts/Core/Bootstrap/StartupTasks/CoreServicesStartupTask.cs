using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Services;
using Laboratory.Core.State;
using Laboratory.Core.State.Implementations;
using Laboratory.Core.Bootstrap;

#nullable enable

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
            
            ReportProgress(progress, 0.3f);
            
            // Initialize game state service and register default states
            var stateService = services.Resolve<IGameStateService>();
            RegisterGameStates(stateService);
            LogInfo("Game state service initialized");
            
            ReportProgress(progress, 0.7f);
            
            // Initialize other core services
            await InitializeAdditionalServicesAsync(services);
            
            ReportProgress(progress, 1.0f);
            LogInfo("Core services initialization complete");
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
            
            LogInfo("Registered 6 game states");
        }

        private async UniTask InitializeAdditionalServicesAsync(IServiceContainer services)
        {
            // Initialize additional core services that may need async setup
            if (services.TryResolve<IAssetService>(out var assetService))
            {
                // Asset service may need initialization
                LogInfo("Asset service ready");
            }

            if (services.TryResolve<IConfigService>(out var configService))
            {
                // Config service may need initialization
                LogInfo("Config service ready");
            }

            if (services.TryResolve<ISceneService>(out var sceneService))
            {
                // Scene service may need initialization
                LogInfo("Scene service ready");
            }

            await UniTask.Yield();
        }
    }
}
