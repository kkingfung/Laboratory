using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Entities;
using R3;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.State;
using Laboratory.Core.Events.Messages;
using Laboratory.Infrastructure.AsyncUtils;
using GameState = Laboratory.Core.Events.Messages.GameState;

#nullable enable

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// System responsible for managing loading screen operations and scene transitions.
    /// This system listens to game state changes and handles asynchronous loading operations
    /// when the game enters the loading state.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class LoadingScreenSystem : SystemBase
    {
        #region Fields
        
        /// <summary>
        /// Reference to the game state service for monitoring and controlling game states
        /// </summary>
        private IGameStateService _gameStateService = null!;
        
        /// <summary>
        /// Reference to the service container for dependency injection
        /// </summary>
        private ServiceContainer _services = null!;
        
        /// <summary>
        /// Reference to the loading screen UI component for displaying loading progress
        /// </summary>
        private LoadingScreen _loadingScreen = null!;
        
        /// <summary>
        /// Flag indicating whether a loading operation is currently in progress
        /// </summary>
        private bool _isLoading = false;
        
        /// <summary>
        /// Subscription to game state change events
        /// </summary>
        private IDisposable? _stateSubscription;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the system is created. Initializes dependencies and subscribes
        /// to game state changes for loading operations.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            InitializeDependencies();
            SubscribeToLoadingStates();
        }

        /// <summary>
        /// Called every frame. This system doesn't require update logic as it
        /// reacts to events through subscriptions.
        /// </summary>
        protected override void OnUpdate()
        {
            // No update logic needed; reacts via subscription
        }

        /// <summary>
        /// Called when the system is destroyed. Cleans up subscriptions and resources.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            CleanupSubscriptions();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes all required dependencies from the service container
        /// </summary>
        private void InitializeDependencies()
        {
            try
            {
                // Get the service container from ServiceContainer
                _services = ServiceContainer.Instance;
                if (_services != null)
                {
                    _gameStateService = _services.ResolveService<IGameStateService>();
                    _loadingScreen = _services.ResolveService<LoadingScreen>();
                }
                else
                {
                    throw new InvalidOperationException("ServiceContainer.Instance is null");
                }
                
                if (_gameStateService == null)
                {
                    throw new InvalidOperationException("IGameStateService could not be resolved");
                }
                
                if (_loadingScreen == null)
                {
                    throw new InvalidOperationException("LoadingScreen could not be resolved");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize LoadingScreenSystem dependencies: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to game state changes, specifically listening for loading state transitions
        /// </summary>
        private void SubscribeToLoadingStates()
        {
            try
            {
                if (_gameStateService != null)
                {
                    var observable = _gameStateService.StateChanges as Observable<GameStateChangedEvent>;
                    _stateSubscription = observable?
                        .Where(evt => evt.CurrentState == GameState.Loading)
                        .Subscribe(async _ => await HandleLoadingStateAsync());
                }
                else
                {
                    Debug.LogError("IGameStateService is null, cannot subscribe to loading states");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to subscribe to loading states: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the loading state by initiating the asynchronous loading process
        /// </summary>
        private async Task HandleLoadingStateAsync()
        {
            try
            {
                await StartLoadingAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during loading state handling: {ex.Message}");
                await HandleLoadingError();
            }
        }

        /// <summary>
        /// Initiates the asynchronous loading process for scene transitions
        /// </summary>
        /// <returns>A task representing the asynchronous loading operation</returns>
        private async Task StartLoadingAsync()
        {
            if (_isLoading)
            {
                Debug.LogWarning("Loading operation already in progress, skipping duplicate request");
                return;
            }

            try
            {
                _isLoading = true;
                Debug.Log("Starting loading operation");

                // Perform the scene loading operation
                await LoadGameSceneAsync();

                // Transition to playing state after successful loading
                TransitionToPlayingState();
                
                Debug.Log("Loading operation completed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Loading operation failed: {ex.Message}");
                await HandleLoadingError();
            }
            finally
            {
                _isLoading = false;
            }
        }

        /// <summary>
        /// Loads the main game scene asynchronously
        /// </summary>
        /// <returns>A task representing the scene loading operation</returns>
        private async Task LoadGameSceneAsync()
        {
            const string gameSceneName = "GameScene";
            
            try
            {
                await _loadingScreen.LoadSceneAsync(gameSceneName);
                Debug.Log($"Successfully loaded scene: {gameSceneName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load scene '{gameSceneName}': {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Transitions the game state to playing after successful loading
        /// </summary>
        private async void TransitionToPlayingState()
        {
            try
            {
                await _gameStateService.RequestTransitionAsync(GameState.Playing);
                Debug.Log("Transitioned to Playing state");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to transition to Playing state: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Handles loading errors by transitioning to an appropriate error state
        /// </summary>
        /// <returns>A task representing the error handling operation</returns>
        private async Task HandleLoadingError()
        {
            try
            {
                // Give a brief delay to ensure any ongoing operations complete
                await Task.Delay(100);
                
                // Transition to main menu or error state
                await _gameStateService.RequestTransitionAsync(GameState.MainMenu);
                Debug.Log("Transitioned to MainMenu state due to loading error");
                
                // Optionally show error message to user through loading screen
                // await _loadingScreen.ShowErrorMessageAsync("Loading failed. Returning to main menu.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during loading error handling: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up all subscriptions and disposable resources
        /// </summary>
        private void CleanupSubscriptions()
        {
            try
            {
                _stateSubscription?.Dispose();
                _stateSubscription = null;
                Debug.Log("LoadingScreenSystem subscriptions cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during subscription cleanup: {ex.Message}");
            }
        }

        #endregion
    }
}
