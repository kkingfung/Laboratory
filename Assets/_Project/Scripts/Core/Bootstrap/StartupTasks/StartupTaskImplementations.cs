using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Core.DI;
using Laboratory.Core.Services;
using Laboratory.Core.Bootstrap;

#nullable enable

namespace Laboratory.Core.Bootstrap.StartupTasks
{
    /// <summary>
    /// Core services startup task - initializes dependency injection and core infrastructure.
    /// </summary>
    public class CoreServicesStartupTask : StartupTaskBase
    {
        public override int Priority => 10;
        public override string Name => "Core Services";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            LogInfo("Initializing core services");
            
            ReportProgress(progress, 0.3f);
            
            // Core services are already registered in GameBootstrap
            // This task validates they are working correctly
            
            ReportProgress(progress, 0.7f);
            
            // Test service resolution
            var eventBus = services.Resolve<Laboratory.Core.Events.IEventBus>();
            var stateService = services.Resolve<Laboratory.Core.State.IGameStateService>();
            
            LogInfo("Core services validation complete");
            ReportProgress(progress, 1.0f);
        }
    }

    /// <summary>
    /// Loads and validates configuration files during startup.
    /// </summary>
    public class ConfigurationStartupTask : StartupTaskBase
    {
        public override int Priority => 20;
        public override string Name => "Configuration";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            var configService = services.Resolve<IConfigService>();
            LogInfo("Starting configuration loading");
            
            ReportProgress(progress, 0.3f);
            
            // Load essential configurations
            await configService.PreloadEssentialConfigsAsync(
                new Progress<float>(p => ReportProgress(progress, 0.3f + p * 0.6f)), 
                cancellation);
            
            ReportProgress(progress, 0.9f);
            
            LogInfo("Configuration loading complete");
            ReportProgress(progress, 1.0f);
        }
    }

    /// <summary>
    /// Preloads essential assets during startup.
    /// </summary>
    public class AssetPreloadStartupTask : StartupTaskBase
    {
        public override int Priority => 30;
        public override string Name => "Asset Preload";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            var assetService = services.Resolve<IAssetService>();
            LogInfo("Starting asset preloading");
            
            ReportProgress(progress, 0.2f);
            
            // Preload core game assets
            await assetService.PreloadCoreAssetsAsync(
                new Progress<float>(p => ReportProgress(progress, 0.2f + p * 0.7f)), 
                cancellation);
            
            ReportProgress(progress, 0.9f);
            
            LogInfo("Asset preloading complete");
            ReportProgress(progress, 1.0f);
        }
    }

    /// <summary>
    /// Sets up the initial game state.
    /// </summary>
    public class GameStateSetupStartupTask : StartupTaskBase
    {
        public override int Priority => 40;
        public override string Name => "Game State Setup";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            var stateService = services.Resolve<Laboratory.Core.State.IGameStateService>();
            LogInfo("Setting up initial game state");
            
            ReportProgress(progress, 0.3f);
            
            // Register all game state implementations
            stateService.RegisterState<Laboratory.Core.State.Implementations.InitializingGameState>();
            stateService.RegisterState<Laboratory.Core.State.Implementations.MainMenuGameState>();
            stateService.RegisterState<Laboratory.Core.State.Implementations.LoadingGameState>();
            stateService.RegisterState<Laboratory.Core.State.Implementations.PlayingGameState>();
            stateService.RegisterState<Laboratory.Core.State.Implementations.PausedGameState>();
            stateService.RegisterState<Laboratory.Core.State.Implementations.GameOverGameState>();
            
            ReportProgress(progress, 0.7f);
            
            // Transition to initializing state
            await stateService.RequestTransitionAsync(Laboratory.Core.State.GameState.Initializing);
            
            ReportProgress(progress, 1.0f);
            LogInfo("Game state setup complete");
        }
    }

    /// <summary>
    /// Initializes game systems like damage manager, ability system, etc.
    /// </summary>
    public class GameSystemStartupTask : StartupTaskBase
    {
        public override int Priority => 45;
        public override string Name => "Game Systems";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            LogInfo("Initializing game systems");
            
            // Initialize damage manager
            try
            {
                var damageManager = Laboratory.Core.Health.Managers.DamageManager.Instance;
                if (damageManager != null)
                {
                    LogInfo("Damage manager initialized");
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize damage manager", ex);
            }
            
            ReportProgress(progress, 0.5f);
            
            // Initialize other game systems here
            await UniTask.Yield();
            
            ReportProgress(progress, 1.0f);
            LogInfo("Game systems initialization complete");
        }
    }

    /// <summary>
    /// Initializes network systems and services.
    /// </summary>
    public class NetworkInitializationStartupTask : StartupTaskBase
    {
        public override int Priority => 50;
        public override string Name => "Network Initialization";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            LogInfo("Initializing network systems");
            
            // TODO: Initialize network services when they're implemented
            // var networkService = services.Resolve<INetworkService>();
            
            ReportProgress(progress, 0.5f);
            
            await UniTask.Yield();
            
            ReportProgress(progress, 1.0f);
            LogInfo("Network initialization complete");
        }
    }

    /// <summary>
    /// Initializes UI systems and preloads UI assets.
    /// </summary>
    public class UISystemStartupTask : StartupTaskBase
    {
        public override int Priority => 60;
        public override string Name => "UI System";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            LogInfo("Initializing UI systems");
            
            ReportProgress(progress, 0.5f);
            
            // TODO: Initialize UI system when it's implemented
            // var uiService = services.Resolve<IUIService>();
            
            await UniTask.Yield();
            
            ReportProgress(progress, 1.0f);
            LogInfo("UI system initialization complete");
        }
    }
}
