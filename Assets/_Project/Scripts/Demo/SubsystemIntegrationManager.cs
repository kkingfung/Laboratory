using UnityEngine;
using Laboratory.Subsystems.Camera;
using Laboratory.Subsystems.Gameplay;
using Laboratory.Subsystems.Tutorial;
using Laboratory.Subsystems.Settings;
using Laboratory.Subsystems.Spawning;
using Laboratory.Core.Enums;
using Laboratory.Chimera.Progression;

namespace Laboratory.Demo
{
    /// <summary>
    /// Coordinates communication and events between all subsystems
    /// Demonstrates proper subsystem integration patterns
    /// </summary>
    public class SubsystemIntegrationManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DemoSceneBootstrap bootstrap;

        // Subsystem references
        private CameraSubsystemManager _cameraSubsystem;
        private GameplaySubsystemManager _gameplaySubsystem;
        private TutorialSubsystemManager _tutorialSubsystem;
        private SettingsSubsystemManager _settingsSubsystem;
        private SpawningSubsystemManager _spawningSubsystem;

        // State tracking
        private bool _isSubscribed = false;

        private void Start()
        {
            if (bootstrap == null)
            {
                bootstrap = FindFirstObjectByType<DemoSceneBootstrap>();
            }

            if (bootstrap != null)
            {
                if (bootstrap.IsInitialized())
                {
                    Initialize();
                }
                else
                {
                    bootstrap.OnBootstrapComplete += Initialize;
                }
            }
        }

        /// <summary>
        /// Initialize integration manager
        /// </summary>
        private void Initialize()
        {
            GetSubsystemReferences();
            SubscribeToEvents();

            Debug.Log("[IntegrationManager] Initialized and subscribed to subsystem events");
        }

        /// <summary>
        /// Get references to all subsystems
        /// </summary>
        private void GetSubsystemReferences()
        {
            _cameraSubsystem = bootstrap.GetSubsystem<CameraSubsystemManager>();
            _gameplaySubsystem = bootstrap.GetSubsystem<GameplaySubsystemManager>();
            _tutorialSubsystem = bootstrap.GetSubsystem<TutorialSubsystemManager>();
            _settingsSubsystem = bootstrap.GetSubsystem<SettingsSubsystemManager>();
            _spawningSubsystem = bootstrap.GetSubsystem<SpawningSubsystemManager>();
        }

        /// <summary>
        /// Subscribe to all subsystem events
        /// </summary>
        private void SubscribeToEvents()
        {
            if (_isSubscribed) return;

            // Gameplay events
            if (_gameplaySubsystem != null)
            {
                GameplayOrchestrator orchestrator = _gameplaySubsystem.GetOrchestrator();
                if (orchestrator != null)
                {
                    orchestrator.OnSessionStarted += HandleGameplaySessionStarted;
                    orchestrator.OnSessionEnded += HandleGameplaySessionEnded;
                    orchestrator.OnActivityStarted += HandleActivityStarted;
                    orchestrator.OnActivityCompleted += HandleActivityCompleted;
                }

                GenreManager genreManager = _gameplaySubsystem.GetGenreManager();
                if (genreManager != null)
                {
                    genreManager.OnGenreActivated += HandleGenreActivated;
                    genreManager.OnGenreChanged += HandleGenreChanged;
                }
            }

            // Tutorial events
            if (_tutorialSubsystem != null)
            {
                var nineStage = _tutorialSubsystem.GetNineStageSystem();
                if (nineStage != null)
                {
                    nineStage.OnStageStarted += HandleTutorialStageStarted;
                    nineStage.OnStageCompleted += HandleTutorialStageCompleted;
                    nineStage.OnOnboardingCompleted += HandleTutorialCompleted;
                    nineStage.OnHintRequested += HandleTutorialHint;
                    nineStage.OnCelebration += HandleTutorialCelebration;
                }
            }

            // Settings events
            if (_settingsSubsystem != null)
            {
                var graphics = _settingsSubsystem.GetGraphicsSettings();
                if (graphics != null)
                {
                    graphics.OnGraphicsSettingsChanged += HandleGraphicsSettingsChanged;
                }

                var audio = _settingsSubsystem.GetAudioSettings();
                if (audio != null)
                {
                    audio.OnAudioSettingsChanged += HandleAudioSettingsChanged;
                }

                var input = _settingsSubsystem.GetInputSettings();
                if (input != null)
                {
                    input.OnInputSettingsChanged += HandleInputSettingsChanged;
                }
            }

            // Camera events
            if (_cameraSubsystem != null)
            {
                var camera = _cameraSubsystem.GetActiveCamera();
                if (camera != null)
                {
                    var stateMachine = camera.GetStateMachine();
                    if (stateMachine != null)
                    {
                        stateMachine.OnModeChanged += HandleCameraModeChanged;
                        stateMachine.OnTransitionStarted += HandleCameraTransitionStarted;
                    }
                }
            }

            _isSubscribed = true;
        }

        /// <summary>
        /// Unsubscribe from all events
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (!_isSubscribed) return;

            // Gameplay events
            if (_gameplaySubsystem != null)
            {
                GameplayOrchestrator orchestrator = _gameplaySubsystem.GetOrchestrator();
                if (orchestrator != null)
                {
                    orchestrator.OnSessionStarted -= HandleGameplaySessionStarted;
                    orchestrator.OnSessionEnded -= HandleGameplaySessionEnded;
                    orchestrator.OnActivityStarted -= HandleActivityStarted;
                    orchestrator.OnActivityCompleted -= HandleActivityCompleted;
                }

                GenreManager genreManager = _gameplaySubsystem.GetGenreManager();
                if (genreManager != null)
                {
                    genreManager.OnGenreActivated -= HandleGenreActivated;
                    genreManager.OnGenreChanged -= HandleGenreChanged;
                }
            }

            // Tutorial events
            if (_tutorialSubsystem != null)
            {
                var nineStage = _tutorialSubsystem.GetNineStageSystem();
                if (nineStage != null)
                {
                    nineStage.OnStageStarted -= HandleTutorialStageStarted;
                    nineStage.OnStageCompleted -= HandleTutorialStageCompleted;
                    nineStage.OnOnboardingCompleted -= HandleTutorialCompleted;
                    nineStage.OnHintRequested -= HandleTutorialHint;
                    nineStage.OnCelebration -= HandleTutorialCelebration;
                }
            }

            // Settings events
            if (_settingsSubsystem != null)
            {
                var graphics = _settingsSubsystem.GetGraphicsSettings();
                if (graphics != null)
                {
                    graphics.OnGraphicsSettingsChanged -= HandleGraphicsSettingsChanged;
                }

                var audio = _settingsSubsystem.GetAudioSettings();
                if (audio != null)
                {
                    audio.OnAudioSettingsChanged -= HandleAudioSettingsChanged;
                }

                var input = _settingsSubsystem.GetInputSettings();
                if (input != null)
                {
                    input.OnInputSettingsChanged -= HandleInputSettingsChanged;
                }
            }

            // Camera events
            if (_cameraSubsystem != null)
            {
                var camera = _cameraSubsystem.GetActiveCamera();
                if (camera != null)
                {
                    var stateMachine = camera.GetStateMachine();
                    if (stateMachine != null)
                    {
                        stateMachine.OnModeChanged -= HandleCameraModeChanged;
                        stateMachine.OnTransitionStarted -= HandleCameraTransitionStarted;
                    }
                }
            }

            _isSubscribed = false;
        }

        // ===== Event Handlers =====

        // Gameplay events
        private void HandleGameplaySessionStarted()
        {
            Debug.Log("[Integration] 🎮 Gameplay session started");
        }

        private void HandleGameplaySessionEnded()
        {
            Debug.Log("[Integration] 🎮 Gameplay session ended");
        }

        private void HandleActivityStarted(ActivityGenreCategory genre)
        {
            Debug.Log($"[Integration] 🎯 Activity started: {genre}");

            // Example: Trigger camera shake when combat activity starts
            if (genre == ActivityGenreCategory.Action && _cameraSubsystem != null)
            {
                var camera = _cameraSubsystem.GetActiveCamera();
                if (camera != null)
                {
                    camera.GetEffects()?.ImpulseShake(0.2f);
                }
            }
        }

        private void HandleActivityCompleted(ActivityGenreCategory genre, bool success)
        {
            Debug.Log($"[Integration] ✅ Activity completed: {genre} - Success: {success}");

            // Example: Record tutorial progress
            if (_tutorialSubsystem != null && success)
            {
                var nineStage = _tutorialSubsystem.GetNineStageSystem();
                if (nineStage != null && nineStage.IsOnboardingActive())
                {
                    // Could auto-advance tutorial based on successful activities
                }
            }
        }

        private void HandleGenreActivated(ActivityGenreCategory genre)
        {
            Debug.Log($"[Integration] 📂 Genre activated: {genre}");

            // Switch camera mode based on genre
            if (_cameraSubsystem != null)
            {
                var camera = _cameraSubsystem.GetActiveCamera();
                if (camera != null)
                {
                    CameraMode targetMode = GetCameraModeForGenre(genre);
                    camera.SetMode(targetMode);
                }
            }
        }

        private void HandleGenreChanged(ActivityGenreCategory oldGenre, ActivityGenreCategory newGenre)
        {
            Debug.Log($"[Integration] 🔄 Genre changed: {oldGenre} → {newGenre}");
        }

        // Tutorial events
        private void HandleTutorialStageStarted(OnboardingStage stage)
        {
            Debug.Log($"[Integration] 📚 Tutorial stage started: {stage}");
        }

        private void HandleTutorialStageCompleted(OnboardingStage stage, StagePerformance performance)
        {
            Debug.Log($"[Integration] ✨ Tutorial stage completed: {stage} (Score: {performance.Score:F2})");
        }

        private void HandleTutorialCompleted()
        {
            Debug.Log("[Integration] 🎓 Tutorial completed!");
        }

        private void HandleTutorialHint(string hint)
        {
            Debug.Log($"[Integration] 💡 Tutorial hint: {hint}");
        }

        private void HandleTutorialCelebration(string message)
        {
            Debug.Log($"[Integration] 🎉 {message}");
        }

        // Settings events
        private void HandleGraphicsSettingsChanged()
        {
            Debug.Log("[Integration] 🎨 Graphics settings changed");
        }

        private void HandleAudioSettingsChanged()
        {
            Debug.Log("[Integration] 🔊 Audio settings changed");
        }

        private void HandleInputSettingsChanged()
        {
            Debug.Log("[Integration] 🎮 Input settings changed");
        }

        // Camera events
        private void HandleCameraModeChanged(CameraMode mode)
        {
            Debug.Log($"[Integration] 📷 Camera mode changed: {mode}");
        }

        private void HandleCameraTransitionStarted(CameraMode from, CameraMode to)
        {
            Debug.Log($"[Integration] 🎬 Camera transition: {from} → {to}");
        }

        /// <summary>
        /// Get appropriate camera mode for a genre
        /// </summary>
        private CameraMode GetCameraModeForGenre(ActivityGenreCategory genre)
        {
            switch (genre)
            {
                case ActivityGenreCategory.Action:
                    return CameraMode.ThirdPerson;  // Includes Platformers, Fighting, FPS
                case ActivityGenreCategory.Racing:
                    return CameraMode.RacingThirdPerson;
                case ActivityGenreCategory.Strategy:
                    return CameraMode.StrategyRTS;
                case ActivityGenreCategory.Puzzle:
                    return CameraMode.TopDown;
                default:
                    return CameraMode.ThirdPerson;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}
