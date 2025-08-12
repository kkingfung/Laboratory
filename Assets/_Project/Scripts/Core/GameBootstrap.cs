using UnityEngine;
using Unity.Entities;
using Unity.Physics.Systems;
using Cysharp.Threading.Tasks;
using R3;
using MessagePipe;
using UniRx;

namespace Infrastructure
{
    public class GameBootstrap : MonoBehaviour
    {
        #region Fields

        [Header("Startup Scene Settings")]
        [SerializeField] private string initialSceneName = "LobbyScene";
        [SerializeField] private bool loadInitialSceneOnStart = true;

        private World _defaultWorld;
        private ServiceLocator _services;
        private IMessageBroker _messageBroker;
        private NetworkClient _networkClient;

        #endregion

        #region Unity Lifecycle

        private async void Awake()
        {
            DontDestroyOnLoad(gameObject);

            // 1. Create DOTS World
            CreateDotsWorld();

            // 2. Setup Service Locator
            _services = new ServiceLocator();
            _messageBroker = new MessageBroker();
            _services.Register<IMessageBroker>(_messageBroker);

            // 3. Setup Network Client
            _networkClient = new NetworkClient();
            _services.Register<NetworkClient>(_networkClient);

            // 4. Setup ViewModels
            RegisterViewModels();

            // 5. Initialize async systems
            await InitializeAsync();

            // 6. Load Initial Scene
            if (loadInitialSceneOnStart)
            {
                await LoadInitialScene();
            }

            _gameStateManager.OnStateChangedForNetwork += (newState) =>
            {
                var bytes = RPCSerializer.SerializeGameState(newState);
                _networkClient.Send(bytes); // Your send method
            };
        }

        #endregion

        #region DOTS Setup

        private void CreateDotsWorld()
        {
            _defaultWorld = World.DefaultGameObjectInjectionWorld ?? new World("Default World");

            // Unity Physics default setup
            var systemGroup = _defaultWorld.GetOrCreateSystemManaged<SimulationSystemGroup>();
            _defaultWorld.GetOrCreateSystemManaged<BuildPhysicsWorld>();
            _defaultWorld.GetOrCreateSystemManaged<StepPhysicsWorld>();
            _defaultWorld.GetOrCreateSystemManaged<EndFramePhysicsSystem>();

            // Register default world
            World.DefaultGameObjectInjectionWorld = _defaultWorld;
        }

        #endregion

        #region MVVM Setup

        private void RegisterViewModels()
        {
            // Example MVVM setup
            _services.Register<GameStateViewModel>(new GameStateViewModel(_messageBroker));
            _services.Register<PlayerViewModel>(new PlayerViewModel(_messageBroker));
            _services.Register<ChatViewModel>(new ChatViewModel(_messageBroker));
        }

        #endregion

        #region Initialization

        private async UniTask InitializeAsync()
        {
            Debug.Log("GameBootstrap: Initializing services...");

            // Connect to server
            await _networkClient.ConnectAsync("127.0.0.1", 7777);

            // Preload assets
            var assetPreloader = new AssetPreloader();
            _services.Register<AssetPreloader>(assetPreloader);
            await assetPreloader.PreloadCoreAssets();

            Debug.Log("GameBootstrap: Initialization complete.");
        }

        #endregion

        #region Scene Management

        private async UniTask LoadInitialScene()
        {
            Debug.Log($"Loading initial scene: {initialSceneName}");
            await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(initialSceneName);
        }

        #endregion
    }
}
