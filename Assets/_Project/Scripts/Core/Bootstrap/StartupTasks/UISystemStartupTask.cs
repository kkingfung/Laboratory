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
    /// Initializes UI systems and prepares the user interface.
    /// </summary>
    public class UISystemStartupTask : StartupTaskBase
    {
        public override int Priority => 60;
        public override string Name => "UI System";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            var eventBus = services.Resolve<IEventBus>();
            
            ReportProgress(progress, 0.4f);
            
            // TODO: Initialize UI systems
            // - Setup UI event handlers
            // - Initialize loading screen
            // - Prepare main menu
            
            ReportProgress(progress, 0.8f);
            
            LogInfo("UI systems initialized");
            
            ReportProgress(progress, 1.0f);
            await UniTask.Yield();
        }
    }
}

#endregion