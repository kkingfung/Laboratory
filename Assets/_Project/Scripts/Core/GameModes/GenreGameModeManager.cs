using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.State;
using Laboratory.Core.Events.Messages;
using R3;

namespace Laboratory.Core.GameModes
{
    /// <summary>
    /// Central manager for coordinating genre-based gameplay modes.
    /// Orchestrates subsystem reconfiguration when switching between genres.
    /// </summary>
    public class GenreGameModeManager : MonoBehaviour
    {
        [Header("Genre Configuration")]
        [SerializeField] private GenreConfig[] genreConfigs;
        [SerializeField] private bool debugLogging = true;

        private readonly Dictionary<GameGenre, List<IGenreSubsystemManager>> _genreSubsystems = new();
        private readonly Dictionary<GameGenre, GenreConfig> _genreConfigLookup = new();

        private GameGenre _currentGenre = GameGenre.Exploration;
        private ServiceContainer _services;
        private IGameStateService _gameStateService;
        private bool _isInitialized = false;

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeGenreConfigs();
            RegisterAsService();
        }

        private void Start()
        {
            InitializeAsync().ContinueWith(OnInitializationComplete);
        }

        private void OnDestroy()
        {
            CleanupGenreManagers();
        }

        #endregion

        #region Initialization

        private void InitializeGenreConfigs()
        {
            foreach (var config in genreConfigs)
            {
                _genreConfigLookup[config.genre] = config;
            }
        }

        private void RegisterAsService()
        {
            _services = ServiceContainer.Instance;
            _services.RegisterService<GenreGameModeManager>(this);
        }

        private async Task InitializeAsync()
        {
            try
            {
                _gameStateService = _services.ResolveService<IGameStateService>();
                await DiscoverGenreSubsystems();
                SubscribeToGameStateChanges();
                _isInitialized = true;

                if (debugLogging)
                    Debug.Log($"GenreGameModeManager initialized with {_genreSubsystems.Count} genre support");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize GenreGameModeManager: {ex.Message}");
            }
        }

        private async Task DiscoverGenreSubsystems()
        {
            // Find all IGenreSubsystemManager implementations in the scene
            var genreManagers = FindObjectsOfType<MonoBehaviour>()
                .OfType<IGenreSubsystemManager>()
                .ToList();

            foreach (var manager in genreManagers)
            {
                var supportedGenre = manager.SupportedGenre;

                if (!_genreSubsystems.ContainsKey(supportedGenre))
                {
                    _genreSubsystems[supportedGenre] = new List<IGenreSubsystemManager>();
                }

                _genreSubsystems[supportedGenre].Add(manager);

                if (debugLogging)
                    Debug.Log($"Registered {manager.GetType().Name} for genre {supportedGenre}");
            }
        }

        private void OnInitializationComplete(Task task)
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"GenreGameModeManager initialization failed: {task.Exception?.GetBaseException().Message}");
            }
        }

        #endregion

        #region Genre Switching

        public async Task<bool> SwitchToGenre(GameGenre targetGenre)
        {
            if (!_isInitialized)
            {
                Debug.LogWarning("GenreGameModeManager not initialized yet");
                return false;
            }

            if (_currentGenre == targetGenre)
            {
                if (debugLogging)
                    Debug.Log($"Already in {targetGenre} mode");
                return true;
            }

            try
            {
                if (debugLogging)
                    Debug.Log($"Switching from {_currentGenre} to {targetGenre}");

                // Deactivate current genre
                await DeactivateCurrentGenre();

                // Activate new genre
                await ActivateGenre(targetGenre);

                _currentGenre = targetGenre;

                // Notify game state service of the genre change
                await NotifyGameStateOfGenreChange(targetGenre);

                if (debugLogging)
                    Debug.Log($"Successfully switched to {targetGenre} mode");

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to switch to {targetGenre}: {ex.Message}");
                return false;
            }
        }

        private async Task DeactivateCurrentGenre()
        {
            if (_currentGenre == GameGenre.Exploration) return;

            if (_genreSubsystems.TryGetValue(_currentGenre, out var managers))
            {
                var deactivationTasks = managers.Select(m => m.DeactivateGenreMode());
                await Task.WhenAll(deactivationTasks);
            }
        }

        private async Task ActivateGenre(GameGenre genre)
        {
            if (!_genreSubsystems.TryGetValue(genre, out var managers))
            {
                if (debugLogging)
                    Debug.LogWarning($"No subsystem managers found for genre {genre}");
                return;
            }

            var config = GetGenreConfig(genre);
            var activationTasks = managers
                .Where(m => m.CanActivateForGenre(genre))
                .Select(m => m.ActivateGenreMode(genre, config));

            await Task.WhenAll(activationTasks);
        }

        private async Task NotifyGameStateOfGenreChange(GameGenre genre)
        {
            var targetState = MapGenreToGameState(genre);
            if (_gameStateService != null && targetState != GameState.None)
            {
                await _gameStateService.RequestTransitionAsync(targetState);
            }
        }

        #endregion

        #region Configuration & Utility

        public GenreConfig GetGenreConfig(GameGenre genre)
        {
            return _genreConfigLookup.TryGetValue(genre, out var config)
                ? config
                : CreateDefaultConfig(genre);
        }

        private GenreConfig CreateDefaultConfig(GameGenre genre)
        {
            return new GenreConfig
            {
                genre = genre,
                configName = $"Default {genre} Config",
                difficultyModifier = 1.0f,
                allowGeneticModification = true,
                maxCreatures = 10,
                timeScale = 1.0f
            };
        }

        private GameState MapGenreToGameState(GameGenre genre)
        {
            return genre switch
            {
                GameGenre.Strategy => GameState.Playing, // Could be GameState.PlayingStrategy if extended
                GameGenre.Racing => GameState.Playing,   // Could be GameState.PlayingRacing if extended
                GameGenre.Puzzle => GameState.Playing,   // Could be GameState.PlayingPuzzle if extended
                GameGenre.Exploration => GameState.Playing,
                _ => GameState.Playing
            };
        }

        public bool IsGenreSupported(GameGenre genre)
        {
            return _genreSubsystems.ContainsKey(genre);
        }

        public List<GameGenre> GetSupportedGenres()
        {
            return _genreSubsystems.Keys.ToList();
        }

        public GameGenre GetCurrentGenre()
        {
            return _currentGenre;
        }

        #endregion

        #region Game State Integration

        private void SubscribeToGameStateChanges()
        {
            // This would integrate with your existing game state system
            // if (_gameStateService?.StateChanges != null)
            // {
            //     _gameStateService.StateChanges.Subscribe(OnGameStateChanged);
            // }
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            // Handle transitions back to main menu, pause, etc.
            if (evt.CurrentState == GameState.MainMenu || evt.CurrentState == GameState.Paused)
            {
                // Optionally deactivate current genre mode
            }
        }

        #endregion

        #region Cleanup

        private void CleanupGenreManagers()
        {
            foreach (var managers in _genreSubsystems.Values)
            {
                foreach (var manager in managers)
                {
                    try
                    {
                        manager.DeactivateGenreMode().Wait(1000); // Quick cleanup
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Error during cleanup of {manager.GetType().Name}: {ex.Message}");
                    }
                }
            }

            _genreSubsystems.Clear();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Quick switch to strategy mode using your existing AI and combat systems
        /// </summary>
        public async Task<bool> SwitchToStrategyMode()
        {
            return await SwitchToGenre(GameGenre.Strategy);
        }

        /// <summary>
        /// Quick switch to racing mode using your existing player and physics systems
        /// </summary>
        public async Task<bool> SwitchToRacingMode()
        {
            return await SwitchToGenre(GameGenre.Racing);
        }

        /// <summary>
        /// Return to default exploration/breeding mode
        /// </summary>
        public async Task<bool> SwitchToExplorationMode()
        {
            return await SwitchToGenre(GameGenre.Exploration);
        }

        #endregion
    }
}