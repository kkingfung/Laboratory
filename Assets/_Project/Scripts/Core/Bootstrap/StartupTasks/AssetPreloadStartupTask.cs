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
using Laboratory.Core.Bootstrap;
using Laboratory.Infrastructure.AsyncUtils;

#nullable enable


#region Startup Tasks

namespace Laboratory.Core.Bootstrap.StartupTasks
{
    /// <summary>
    /// Preloads critical assets needed for the game to function.
    /// </summary>
    public class AssetPreloadStartupTask : StartupTaskBase
    {
        public override int Priority => 30;
        public override string Name => "Asset Preload";
        public override System.TimeSpan EstimatedDuration => System.TimeSpan.FromSeconds(3);

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            var assetService = services.Resolve<IAssetService>();
            
            // Preload core assets with progress reporting
            var assetProgress = new Progress<float>(p => ReportProgress(progress, p));
            await assetService.PreloadCoreAssetsAsync(assetProgress, cancellation);
            
            var stats = assetService.GetCacheStats();
            LogInfo($"Preloaded {stats.TotalAssets} assets ({stats.TotalMemoryUsage / (1024 * 1024):F2} MB)");
        }
    }
}

#endregion