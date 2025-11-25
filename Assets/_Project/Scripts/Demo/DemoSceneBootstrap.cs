using UnityEngine;
using Laboratory.Subsystems.Camera;
using Laboratory.Subsystems.Gameplay;
using Laboratory.Subsystems.Tutorial;
using Laboratory.Subsystems.Settings;
using Laboratory.Subsystems.Spawning;
using Laboratory.Core.Enums;

namespace Laboratory.Demo
{
    /// <summary>
    /// Demo scene bootstrap that initializes all subsystems
    /// Validates integration of Camera, Gameplay, Tutorial, Settings, and Spawning
    /// </summary>
    public class DemoSceneBootstrap : MonoBehaviour
    {
        [Header("Subsystem Managers")]
        [SerializeField] private CameraSubsystemManager cameraSubsystem;
        [SerializeField] private GameplaySubsystemManager gameplaySubsystem;
        [SerializeField] private TutorialSubsystemManager tutorialSubsystem;
        [SerializeField] private SettingsSubsystemManager settingsSubsystem;
        [SerializeField] private SpawningSubsystemManager spawningSubsystem;

        [Header("Demo Configuration")]
        [SerializeField] private bool autoStartTutorial = true;
        [SerializeField] private bool autoStartGameplay = true;
        [SerializeField] private ActivityGenreCategory startingGenre = ActivityGenreCategory.Action;

        [Header("Demo Objects")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject[] creaturePrefabs;
        [SerializeField] private Transform spawnParent;

        // Initialization state
        private bool _isInitialized = false;
        private GameObject _playerInstance;

        // Events
        public event System.Action OnBootstrapComplete;

        private void Start()
        {
            InitializeDemo();
        }

        /// <summary>
        /// Initialize demo scene and all subsystems
        /// </summary>
        private void InitializeDemo()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[DemoBootstrap] Already initialized!");
                return;
            }

            Debug.Log("[DemoBootstrap] Starting demo initialization...");

            // Step 1: Find or create subsystem managers
            FindOrCreateSubsystems();

            // Step 2: Initialize spawning first (needed for spawning player)
            InitializeSpawningSubsystem();

            // Step 3: Spawn player
            SpawnPlayer();

            // Step 4: Initialize camera (needs player target)
            InitializeCameraSubsystem();

            // Step 5: Initialize settings
            InitializeSettingsSubsystem();

            // Step 6: Initialize gameplay
            InitializeGameplaySubsystem();

            // Step 7: Initialize tutorial (last, depends on others)
            InitializeTutorialSubsystem();

            _isInitialized = true;
            OnBootstrapComplete?.Invoke();

            Debug.Log("[DemoBootstrap] ✅ Demo initialization complete!");
        }

        /// <summary>
        /// Find or create subsystem managers
        /// </summary>
        private void FindOrCreateSubsystems()
        {
            // Camera
            if (cameraSubsystem == null)
            {
                cameraSubsystem = CameraSubsystemManager.Instance;
                if (cameraSubsystem == null)
                {
                    GameObject camObj = new GameObject("CameraSubsystem");
                    cameraSubsystem = camObj.AddComponent<CameraSubsystemManager>();
                }
            }

            // Gameplay
            if (gameplaySubsystem == null)
            {
                gameplaySubsystem = GameplaySubsystemManager.Instance;
                if (gameplaySubsystem == null)
                {
                    GameObject gameplayObj = new GameObject("GameplaySubsystem");
                    gameplaySubsystem = gameplayObj.AddComponent<GameplaySubsystemManager>();
                }
            }

            // Tutorial
            if (tutorialSubsystem == null)
            {
                tutorialSubsystem = TutorialSubsystemManager.Instance;
                if (tutorialSubsystem == null)
                {
                    GameObject tutorialObj = new GameObject("TutorialSubsystem");
                    tutorialSubsystem = tutorialObj.AddComponent<TutorialSubsystemManager>();
                }
            }

            // Settings
            if (settingsSubsystem == null)
            {
                settingsSubsystem = SettingsSubsystemManager.Instance;
                if (settingsSubsystem == null)
                {
                    GameObject settingsObj = new GameObject("SettingsSubsystem");
                    settingsSubsystem = settingsObj.AddComponent<SettingsSubsystemManager>();
                }
            }

            // Spawning
            if (spawningSubsystem == null)
            {
                spawningSubsystem = SpawningSubsystemManager.Instance;
                if (spawningSubsystem == null)
                {
                    GameObject spawningObj = new GameObject("SpawningSubsystem");
                    spawningSubsystem = spawningObj.AddComponent<SpawningSubsystemManager>();
                }
            }

            Debug.Log("[DemoBootstrap] All subsystem managers found/created");
        }

        /// <summary>
        /// Initialize camera subsystem
        /// </summary>
        private void InitializeCameraSubsystem()
        {
            if (cameraSubsystem == null) return;

            CameraController cam = cameraSubsystem.GetActiveCamera();

            if (cam == null)
            {
                // Find main camera and add controller
                UnityEngine.Camera mainCam = UnityEngine.Camera.main;
                if (mainCam != null)
                {
                    cam = mainCam.gameObject.GetComponent<CameraController>();
                    if (cam == null)
                    {
                        cam = mainCam.gameObject.AddComponent<CameraController>();
                    }

                    // Add required components
                    if (mainCam.gameObject.GetComponent<CameraStateMachine>() == null)
                    {
                        mainCam.gameObject.AddComponent<CameraStateMachine>();
                    }
                    if (mainCam.gameObject.GetComponent<CameraEffects>() == null)
                    {
                        mainCam.gameObject.AddComponent<CameraEffects>();
                    }

                    cameraSubsystem.SetActiveCamera(cam);
                }
            }

            // Set camera target to player
            if (cam != null && _playerInstance != null)
            {
                cam.SetTarget(_playerInstance.transform);
                cam.SetMode(CameraMode.ThirdPerson);
            }

            Debug.Log("[DemoBootstrap] Camera subsystem initialized");
        }

        /// <summary>
        /// Initialize gameplay subsystem
        /// </summary>
        private void InitializeGameplaySubsystem()
        {
            if (gameplaySubsystem == null) return;

            if (autoStartGameplay)
            {
                gameplaySubsystem.StartSession(startingGenre);
                Debug.Log($"[DemoBootstrap] Gameplay session started with genre: {startingGenre}");
            }

            Debug.Log("[DemoBootstrap] Gameplay subsystem initialized");
        }

        /// <summary>
        /// Initialize tutorial subsystem
        /// </summary>
        private void InitializeTutorialSubsystem()
        {
            if (tutorialSubsystem == null) return;

            if (autoStartTutorial)
            {
                tutorialSubsystem.StartOnboarding();
                Debug.Log("[DemoBootstrap] Tutorial onboarding started");
            }

            Debug.Log("[DemoBootstrap] Tutorial subsystem initialized");
        }

        /// <summary>
        /// Initialize settings subsystem
        /// </summary>
        private void InitializeSettingsSubsystem()
        {
            if (settingsSubsystem == null) return;

            // Load and apply settings
            settingsSubsystem.LoadAllSettings();
            settingsSubsystem.ApplyAllSettings();

            Debug.Log("[DemoBootstrap] Settings subsystem initialized");
        }

        /// <summary>
        /// Initialize spawning subsystem
        /// </summary>
        private void InitializeSpawningSubsystem()
        {
            if (spawningSubsystem == null) return;

            // Register creature prefabs
            if (creaturePrefabs != null && creaturePrefabs.Length > 0)
            {
                foreach (GameObject prefab in creaturePrefabs)
                {
                    if (prefab != null)
                    {
                        spawningSubsystem.RegisterPrefab(prefab.name, prefab);
                        spawningSubsystem.CreatePool(prefab, 10, 100);
                    }
                }
            }

            Debug.Log("[DemoBootstrap] Spawning subsystem initialized");
        }

        /// <summary>
        /// Spawn player character
        /// </summary>
        private void SpawnPlayer()
        {
            if (playerPrefab == null)
            {
                Debug.LogWarning("[DemoBootstrap] No player prefab assigned!");
                return;
            }

            Vector3 spawnPos = Vector3.zero;
            if (spawnParent != null)
            {
                spawnPos = spawnParent.position;
            }

            _playerInstance = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
            _playerInstance.name = "Player";

            Debug.Log("[DemoBootstrap] Player spawned at " + spawnPos);
        }

        /// <summary>
        /// Get player instance
        /// </summary>
        public GameObject GetPlayer()
        {
            return _playerInstance;
        }

        /// <summary>
        /// Check if demo is initialized
        /// </summary>
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        /// <summary>
        /// Get subsystem reference
        /// </summary>
        public T GetSubsystem<T>() where T : MonoBehaviour
        {
            if (typeof(T) == typeof(CameraSubsystemManager)) return cameraSubsystem as T;
            if (typeof(T) == typeof(GameplaySubsystemManager)) return gameplaySubsystem as T;
            if (typeof(T) == typeof(TutorialSubsystemManager)) return tutorialSubsystem as T;
            if (typeof(T) == typeof(SettingsSubsystemManager)) return settingsSubsystem as T;
            if (typeof(T) == typeof(SpawningSubsystemManager)) return spawningSubsystem as T;
            return null;
        }
    }
}
