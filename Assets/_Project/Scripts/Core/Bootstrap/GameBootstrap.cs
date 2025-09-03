using UnityEngine;
using Unity.Entities;
using Cysharp.Threading.Tasks;
// MessagePipe removed - using pure UniRx implementation
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Services;
using Laboratory.Core.State;
using Laboratory.Core.Bootstrap;
using Laboratory.Core.Bootstrap.StartupTasks;
// using Laboratory.Infrastructure.AsyncUtils; // Removed due to cyclic dependency
using System.Threading;
using System;

#nullable enable

namespace Laboratory.Core.Bootstrap
{
    /// <summary>
    /// Improved game bootstrap that uses the new architecture with dependency injection,
    /// unified event system, and startup orchestration. Replaces the original scattered
    /// initialization with a clean, testable, and extensible system.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        #region Fields

        [Header("Bootstrap Settings")]
        [SerializeField] private bool _enableDebugLogging = true;
        [SerializeField] private string _initialSceneName = "MainMenu";
        [SerializeField] private bool _loadInitialSceneOnStart = true;

        private IServiceContainer _services = null!;
        private StartupOrchestrator _orchestrator = null!;
        private CancellationTokenSource _shutdownCts = new();

        #endregion

        #region Unity Lifecycle

        private async void Awake()
        {
            // Ensure this bootstrap persists across scenes
            DontDestroyOnLoad(gameObject);
            
            if (_enableDebugLogging)
            {
                Debug.Log("GameBootstrap: Starting initialization...");
            }

            try
            {
                await InitializeAsync(_shutdownCts.Token);
                
                if (_loadInitialSceneOnStart && !string.IsNullOrEmpty(_initialSceneName))
                {
                    await LoadInitialSceneAsync();
                }
                
                Debug.Log("GameBootstrap: Initialization completed successfully!");
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("GameBootstrap: Initialization was cancelled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameBootstrap: Initialization failed: {ex}");
                
                // Optionally show error UI or quit application
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
            }
        }

        private void Update()
        {
            // Update the game state service (if it needs per-frame updates)
            if (_services?.TryResolve<IGameStateService>(out var stateService) == true)
            {
                stateService?.Update();
            }
        }

        private void OnApplicationQuit()
        {
            _shutdownCts?.Cancel();
        }

        private void OnDestroy()
        {
            GlobalServiceProvider.Shutdown();
            _services?.Dispose();
            _shutdownCts?.Dispose();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the global service container instance.
        /// </summary>
        public IServiceContainer Services => _services;

        /// <summary>
        /// Manually triggers the initialization process (useful for testing).
        /// </summary>
        public async UniTask InitializeManuallyAsync(CancellationToken cancellation = default)
        {
            await InitializeAsync(cancellation);
        }

        #endregion

        #region Private Methods

        private async UniTask InitializeAsync(CancellationToken cancellation)
        {
            // Phase 1: Create core infrastructure
            CreateServiceContainer();
            RegisterCoreServices();
            
            // Phase 2: Create and configure startup orchestrator
            _orchestrator = new StartupOrchestrator();
            ConfigureStartupTasks();
            
            // Phase 3: Execute all startup tasks
            var progress = new System.Progress<float>(OnInitializationProgress);
            await _orchestrator.InitializeAsync(_services, progress, cancellation);
            
            // Phase 4: Final setup
            await PostInitializationSetupAsync();
        }

        private void CreateServiceContainer()
        {
            _services = new ServiceContainer();
            
            // Register the container itself so services can access it
            _services.RegisterInstance<IServiceContainer>(_services);
            
            // Initialize the global service provider for ECS systems
            GlobalServiceProvider.Initialize(_services);
        }

        private void RegisterCoreServices()
        {
            // Core infrastructure services using pure UniRx implementation
            _services.Register<IEventBus, UnifiedEventBus>();
            _services.Register<IGameStateService, GameStateService>();
            
            // Application services
            _services.Register<IAssetService, AssetService>();
            _services.Register<ISceneService, SceneService>();
            _services.Register<IConfigService, ConfigService>();
            
            // Network services (register interface, implement later)
            // _services.Register<INetworkService, NetworkService>();
            
            Debug.Log("GameBootstrap: Core services registered");
        }

        private void ConfigureStartupTasks()
        {
            // Add startup tasks in dependency order
            // Lower priority numbers execute first
            
            _orchestrator.AddTask<CoreServicesStartupTask>();        // Priority: 10
            _orchestrator.AddTask<ConfigurationStartupTask>();       // Priority: 20
            _orchestrator.AddTask<AssetPreloadStartupTask>();        // Priority: 30
            _orchestrator.AddTask<GameStateSetupStartupTask>();      // Priority: 40
            _orchestrator.AddTask<GameSystemStartupTask>();          // Priority: 45
            _orchestrator.AddTask<NetworkInitializationStartupTask>();// Priority: 50
            _orchestrator.AddTask<UISystemStartupTask>();            // Priority: 60
            
            Debug.Log($"GameBootstrap: Configured {7} startup tasks");
        }

        private void OnInitializationProgress(float progress)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"GameBootstrap: Initialization progress: {progress:P1}");
            }
            
            // Optionally update a loading screen here
        }

        private async UniTask PostInitializationSetupAsync()
        {
            // Transition to initial game state
            var stateService = _services.Resolve<IGameStateService>();
            await stateService.RequestTransitionAsync(GameState.MainMenu);
            
            // Log final statistics
            if (_enableDebugLogging)
            {
                LogInitializationStatistics();
            }
        }

        private async UniTask LoadInitialSceneAsync()
        {
            var sceneService = _services.Resolve<ISceneService>();
            var progress = new System.Progress<float>(p => 
                Debug.Log($"Loading initial scene: {p:P1}"));
            
            await sceneService.LoadSceneAsync(_initialSceneName, 
                UnityEngine.SceneManagement.LoadSceneMode.Single, 
                progress, _shutdownCts.Token);
        }

        private void LogInitializationStatistics()
        {
            var executionInfo = _orchestrator.GetExecutionInfo();
            var totalTasks = executionInfo.Count;
            var completedTasks = 0;
            var totalTime = 0.0;

            foreach (var kvp in executionInfo)
            {
                var info = kvp.Value;
                if (info.Status == TaskExecutionStatus.Completed)
                {
                    completedTasks++;
                    totalTime += info.Duration.TotalMilliseconds;
                }
            }

            Debug.Log($"GameBootstrap Statistics:\n" +
                     $"- Total Tasks: {totalTasks}\n" +
                     $"- Completed: {completedTasks}\n" +
                     $"- Total Time: {totalTime:F2}ms\n" +
                     $"- Average per Task: {(completedTasks > 0 ? totalTime / completedTasks : 0):F2}ms");
        }

        #endregion
    }
}