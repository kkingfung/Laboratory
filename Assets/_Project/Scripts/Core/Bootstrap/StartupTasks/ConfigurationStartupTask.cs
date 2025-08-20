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
    /// Loads essential configuration files needed for the application.
    /// </summary>
    public class ConfigurationStartupTask : StartupTaskBase
    {
        public override int Priority => 20;
        public override string Name => "Configuration";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            var configService = services.Resolve<IConfigService>();
            
            ReportProgress(progress, 0.2f);
            
            // Preload essential configurations
            var configProgress = new Progress<float>(p => ReportProgress(progress, 0.2f + (p * 0.8f)));
            await configService.PreloadEssentialConfigsAsync(configProgress, cancellation);
            
            LogInfo("Essential configurations loaded");
            ReportProgress(progress, 1.0f);
        }
    }
}

#endregion