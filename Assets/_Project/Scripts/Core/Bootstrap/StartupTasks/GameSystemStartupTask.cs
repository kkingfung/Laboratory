using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Health.Managers;
using Laboratory.Core.Bootstrap;

#nullable enable

namespace Laboratory.Core.Bootstrap.StartupTasks
{
    /// <summary>
    /// Initializes game-specific services like damage management, combat systems, etc.
    /// Runs after core services but before UI systems.
    /// </summary>
    public class GameSystemStartupTask : StartupTaskBase
    {
        public override int Priority => 45;
        public override string Name => "Game Systems";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            LogInfo("Initializing game systems...");
            
            // Initialize damage manager
            await InitializeDamageManager(services);
            ReportProgress(progress, 0.4f);
            
            // Initialize other game systems
            await InitializeAdditionalSystems(services);
            ReportProgress(progress, 0.8f);
            
            LogInfo("Game systems initialized successfully");
            ReportProgress(progress, 1.0f);
        }

        private async UniTask InitializeDamageManager(IServiceContainer services)
        {
            try
            {
                // Create DamageManager GameObject if it doesn't exist
                var existingDamageManager = GameObject.FindObjectOfType<DamageManager>();
                if (existingDamageManager == null)
                {
                    var damageManagerGO = new GameObject("DamageManager");
                    var damageManager = damageManagerGO.AddComponent<DamageManager>();
                    GameObject.DontDestroyOnLoad(damageManagerGO);
                    
                    LogInfo("DamageManager created and configured");
                }
                else
                {
                    LogInfo("DamageManager already exists");
                }
                
                // Register damage manager as a service (singleton reference)
                services.RegisterInstance<DamageManager>(DamageManager.Instance!);
                
                await UniTask.Yield();
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize DamageManager", ex);
                throw;
            }
        }

        private async UniTask InitializeAdditionalSystems(IServiceContainer services)
        {
            // Initialize other game-specific managers
            
            // Timer Service (if not already initialized)
            if (!services.IsRegistered<Laboratory.Core.Timing.TimerService>())
            {
                var timerServiceGO = new GameObject("TimerService");
                var timerService = timerServiceGO.AddComponent<Laboratory.Core.Timing.TimerService>();
                GameObject.DontDestroyOnLoad(timerServiceGO);
                services.RegisterInstance(timerService);
                LogInfo("TimerService initialized");
            }
            
            // Audio Manager (placeholder for future implementation)
            // Combat System Manager (placeholder for future implementation)
            // Effect System Manager (placeholder for future implementation)
            
            await UniTask.Yield();
        }
    }
}
