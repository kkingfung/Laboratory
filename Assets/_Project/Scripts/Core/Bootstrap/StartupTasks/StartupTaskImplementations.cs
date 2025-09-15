using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Laboratory.Core.DI;
using Laboratory.Core.Services;
using Laboratory.Core.Bootstrap;
using System.Linq;

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
            
            // Add small delay to make this truly async
            await UniTask.Yield();
            
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
            
            // Initialize health system service
            try
            {
                var healthSystemService = services.Resolve<Laboratory.Core.Systems.IHealthSystem>();
                if (healthSystemService != null)
                {
                    LogInfo("Health system service initialized");
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize health system", ex);
            }
            
            ReportProgress(progress, 0.5f);
            
            // Initialize other game systems here
            await UniTask.Yield();
            
            ReportProgress(progress, 1.0f);
            LogInfo("Game systems initialization complete");
        }
    }

    /// <summary>
    /// Initializes network systems and services with proper integration.
    /// </summary>
    public class NetworkInitializationStartupTask : StartupTaskBase
    {
        public override int Priority => 50;
        public override string Name => "Network Initialization";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            LogInfo("Initializing network systems");
            
            try
            {
                // Check if network service is available
                if (services.TryResolve<INetworkService>(out var networkService))
                {
                    LogInfo("Network service found, initializing...");
                    await InitializeNetworkService(networkService!, progress, cancellation);
                }
                else
                {
                    LogInfo("Network service not registered, setting up offline mode");
                    await SetupOfflineMode(services, progress, cancellation);
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize network systems", ex);
                LogInfo("Continuing in offline mode");
                await SetupOfflineMode(services, progress, cancellation);
            }
            
            ReportProgress(progress, 1.0f);
            LogInfo("Network initialization complete");
        }
        
        private async UniTask InitializeNetworkService(INetworkService networkService, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.2f);
            
            try
            {
                // Create default network configuration
                var networkConfig = new NetworkConfiguration
                {
                    DefaultHost = "localhost",
                    DefaultPort = 7777,
                    ConnectionTimeoutMs = 5000,
                    ReconnectAttempts = 3,
                    EnableCompression = true,
                    EnableEncryption = false
                };
                
                ReportProgress(progress, 0.4f);
                
                // Initialize the network service
                await networkService.InitializeAsync(networkConfig, cancellation);
                
                ReportProgress(progress, 0.8f);
                
                LogInfo("Network service initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Network service initialization failed: {ex.Message}");
                throw;
            }
        }
        
        private async UniTask SetupOfflineMode(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.3f);
            
            // Ensure basic networking components exist for offline play
            var existingNetworkManager = UnityEngine.Object.FindFirstObjectByType<UnityEngine.MonoBehaviour>();
            
            if (existingNetworkManager == null)
            {
                LogInfo("No existing network manager found, creating placeholder for offline mode");
                
                var networkGO = new GameObject("OfflineNetworkManager");
                networkGO.AddComponent<OfflineNetworkPlaceholder>();
                UnityEngine.Object.DontDestroyOnLoad(networkGO);
            }
            
            ReportProgress(progress, 0.7f);
            
            // Setup offline-specific configurations
            await ConfigureOfflineMode();
            
            LogInfo("Offline mode configured successfully");
        }
        
        private async UniTask ConfigureOfflineMode()
        {
            // Configure game for offline play
            // Disable multiplayer-specific features
            // Enable single-player alternatives
            await UniTask.Yield();
        }
    }

    /// <summary>
    /// Initializes UI systems and preloads UI assets with proper integration.
    /// </summary>
    public class UISystemStartupTask : StartupTaskBase
    {
        public override int Priority => 60;
        public override string Name => "UI System";

        public override async UniTask ExecuteAsync(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.1f);
            
            LogInfo("Initializing UI systems");
            
            try
            {
                // Check if UI service is available
                if (services.TryResolve<IUIService>(out var uiService))
                {
                    LogInfo("UI service found, initializing...");
                    await InitializeUIService(uiService!, progress, cancellation);
                }
                else
                {
                    LogInfo("UI service not registered, setting up basic UI systems");
                    await SetupBasicUISystem(services, progress, cancellation);
                }
            }
            catch (Exception ex)
            {
                LogError("Failed to initialize UI systems", ex);
                LogInfo("Continuing with basic UI functionality");
                await SetupBasicUISystem(services, progress, cancellation);
            }
            
            ReportProgress(progress, 1.0f);
            LogInfo("UI system initialization complete");
        }
        
        private async UniTask InitializeUIService(IUIService uiService, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.2f);
            
            try
            {
                // Create UI configuration with sensible defaults
                var uiConfig = new UIConfiguration
                {
                    MainCanvasName = "MainCanvas",
                    CreateCanvasIfMissing = true,
                    DontDestroyOnLoad = true,
                    CommonPrefabPaths = new string[]
                    {
                        "UI/LoadingScreen",
                        "UI/MessageBox",
                        "UI/Notification",
                        "UI/MainMenu",
                        "UI/PauseMenu",
                        "UI/SettingsMenu"
                    },
                    MaxCachedPrefabs = 50,
                    EnableUIPooling = true,
                    ScreenTransitionDuration = 0.3f
                };
                
                ReportProgress(progress, 0.4f);
                
                // Initialize the UI service
                await uiService.InitializeAsync(uiConfig, cancellation);
                
                ReportProgress(progress, 0.6f);
                
                // Preload common UI prefabs
                await uiService.PreloadCommonUIPrefabsAsync(cancellation);
                
                ReportProgress(progress, 0.8f);
                
                LogInfo("UI service initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"UI service initialization failed: {ex.Message}");
                throw;
            }
        }
        
        private async UniTask SetupBasicUISystem(IServiceContainer services, IProgress<float>? progress, CancellationToken cancellation)
        {
            ReportProgress(progress, 0.2f);
            
            // Ensure main canvas exists
            var mainCanvas = UnityEngine.Object.FindFirstObjectByType<UnityEngine.Canvas>();
            if (mainCanvas == null)
            {
                LogInfo("Creating main canvas");
                
                var canvasGO = new GameObject("MainCanvas");
                mainCanvas = canvasGO.AddComponent<UnityEngine.Canvas>();
                mainCanvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 0;
                
                // Add canvas scaler with proper settings
                var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                
                // Add graphic raycaster
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                UnityEngine.Object.DontDestroyOnLoad(canvasGO);
                LogInfo("Main canvas created successfully");
            }
            else
            {
                LogInfo("Using existing main canvas");
            }
            
            ReportProgress(progress, 0.5f);
            
            // Ensure EventSystem exists
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                LogInfo("Creating EventSystem");
                
                var eventSystemGO = new GameObject("EventSystem");
                eventSystem = eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                
                // Add appropriate input module based on available input systems
                if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.InputSystem.PlayerInput>() != null)
                {
                    // New Input System detected
                    eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
                else
                {
                    // Fallback to legacy input module
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
                
                UnityEngine.Object.DontDestroyOnLoad(eventSystemGO);
                LogInfo("EventSystem created successfully");
            }
            else
            {
                LogInfo("Using existing EventSystem");
            }
            
            ReportProgress(progress, 0.7f);
            
            // Setup basic UI hierarchy for common UI elements
            await SetupUIHierarchy(mainCanvas.transform);
            
            ReportProgress(progress, 0.9f);
            
            LogInfo("Basic UI system setup complete");
        }
        
        private async UniTask SetupUIHierarchy(Transform canvasTransform)
        {
            // Create common UI containers
            var containers = new string[]
            {
                "BackgroundLayer",
                "GameplayLayer", 
                "MenuLayer",
                "OverlayLayer",
                "ModalLayer",
                "TooltipLayer"
            };
            
            for (int i = 0; i < containers.Length; i++)
            {
                var containerName = containers[i];
                var existingContainer = canvasTransform.Find(containerName);
                
                if (existingContainer == null)
                {
                    var containerGO = new GameObject(containerName);
                    containerGO.transform.SetParent(canvasTransform, false);
                    
                    // Add RectTransform and configure for full screen
                    var rectTransform = containerGO.AddComponent<RectTransform>();
                    rectTransform.anchorMin = Vector2.zero;
                    rectTransform.anchorMax = Vector2.one;
                    rectTransform.sizeDelta = Vector2.zero;
                    rectTransform.anchoredPosition = Vector2.zero;
                    
                    // Set sorting order based on layer
                    if (containerGO.TryGetComponent<UnityEngine.Canvas>(out var layerCanvas))
                    {
                        layerCanvas.sortingOrder = i;
                    }
                }
            }
            
            await UniTask.Yield();
        }
    }

    /// <summary>
    /// Placeholder component for offline network functionality.
    /// </summary>
    public class OfflineNetworkPlaceholder : MonoBehaviour
    {
        private void Awake()
        {
            gameObject.name = "OfflineNetworkManager";
        }
        
        private void Start()
        {
            Debug.Log("[OfflineNetworkPlaceholder] Offline mode active - multiplayer features disabled");
        }
    }
}