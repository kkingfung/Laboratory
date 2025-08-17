using UnityEngine;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Cysharp.Threading.Tasks;
using MessagePipe;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Core;
using UniRx;

namespace Laboratory.Core.Bootstrap
{
    /// <summary>
    /// Main game bootstrap responsible for initializing core systems, services, and DOTS world.
    /// This is the entry point for the entire game architecture.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        #region Constants

        private const string DEFAULT_INITIAL_SCENE_NAME = "LobbyScene";
        private const int DEFAULT_NETWORK_PORT = 7777;
        private const string DEFAULT_NETWORK_ADDRESS = "127.0.0.1";

        #endregion

        #region Fields

        [Header("Startup Scene Settings")]
        [SerializeField] private string _initialSceneName = DEFAULT_INITIAL_SCENE_NAME;
        [SerializeField] private bool _loadInitialSceneOnStart = true;

        private World _defaultWorld;
        private ServiceLocator _services;
        private IMessageBroker _messageBroker;
        private NetworkClient _networkClient;
        private GameStateManager _gameStateManager;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the service locator instance.
        /// </summary>
        public ServiceLocator Services => _services;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize all core systems and bootstrap the game.
        /// </summary>
        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);

            CreateDotsWorld();
            InitializeServiceLocator();
            InitializeNetworkClient();
            RegisterViewModels();
            await InitializeAsync();

            if (_loadInitialSceneOnStart)
            {
                await LoadInitialScene();
            }

            SubscribeToGameStateEvents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the service locator instance (legacy accessor).
        /// </summary>
        public ServiceLocator GetServiceLocator() => _services;

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates and configures the DOTS world with physics systems.
        /// </summary>
        private void CreateDotsWorld()
        {
            _defaultWorld = World.DefaultGameObjectInjectionWorld ?? new World("Default World");

            var systemGroup = _defaultWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();
            
            // Create unmanaged physics systems
            _defaultWorld.GetOrCreateSystem<BuildPhysicsWorld>();
            _defaultWorld.GetOrCreateSystem<StepPhysicsWorld>();
            _defaultWorld.GetOrCreateSystem<EndFramePhysicsSystem>();

            World.DefaultGameObjectInjectionWorld = _defaultWorld;
        }

        /// <summary>
        /// Initializes the service locator and core services.
        /// </summary>
        private void InitializeServiceLocator()
        {
            _services = new ServiceLocator();
            _messageBroker = new MessageBroker();
            _services.Register<IMessageBroker>(_messageBroker);
        }

        /// <summary>
        /// Initializes the network client and registers it with services.
        /// </summary>
        private void InitializeNetworkClient()
        {
            // TODO: Implement NetworkClient when it's created
            // _networkClient = new NetworkClient(_messageBroker);
            // _services.Register<NetworkClient>(_networkClient);
        }

        /// <summary>
        /// Registers ViewModels for MVVM pattern.
        /// </summary>
        private void RegisterViewModels()
        {
            // TODO: Implement ViewModels when they are created
            // _services.Register<GameStateViewModel>(new GameStateViewModel(_messageBroker));
            // _services.Register<PlayerViewModel>(new PlayerViewModel(_messageBroker));
            // _services.Register<ChatViewModel>(new ChatViewModel(_messageBroker));
        }

        /// <summary>
        /// Performs async initialization of services and connections.
        /// </summary>
        private async UniTask InitializeAsync()
        {
            Debug.Log("GameBootstrap: Initializing services...");

            try
            {
                // TODO: Implement NetworkClient connection when it's created
                // await _networkClient.ConnectAsync(DEFAULT_NETWORK_ADDRESS, DEFAULT_NETWORK_PORT);

                var assetPreloader = new AssetPreloader();
                _services.Register<AssetPreloader>(assetPreloader);
                await assetPreloader.PreloadCoreAssets();

                Debug.Log("GameBootstrap: Initialization complete.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameBootstrap: Initialization failed - {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Loads the initial scene asynchronously.
        /// </summary>
        private async UniTask LoadInitialScene()
        {
            Debug.Log($"Loading initial scene: {_initialSceneName}");
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_initialSceneName);
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Subscribes to game state change events for network synchronization.
        /// </summary>
        private void SubscribeToGameStateEvents()
        {
            // TODO: Implement event subscription when GameStateManager and NetworkClient are ready
            // if (_gameStateManager != null)
            // {
            //     _gameStateManager.OnStateChangedForNetwork += HandleGameStateChanged;
            // }
        }

        /// <summary>
        /// Handles game state changes and synchronizes over network.
        /// </summary>
        private void HandleGameStateChanged(GameStateManager.GameState newState)
        {
            // TODO: Implement state synchronization when GameState and RPCSerializer are ready
            // var bytes = RPCSerializer.SerializeGameState(newState);
            // _ = _networkClient.SendAsync(bytes);
        }

        #endregion
    }
}
