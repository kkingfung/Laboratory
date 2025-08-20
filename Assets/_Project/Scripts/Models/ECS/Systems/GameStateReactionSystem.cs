using System;
using Unity.Entities;
using UniRx;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Core.DI;
using Laboratory.Core.State;
using Laboratory.Core.Events.Messages;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Models.ECS.Components;

#nullable enable

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// System responsible for reacting to game state changes and enabling/disabling
    /// gameplay systems accordingly. This system listens to GameStateManager events
    /// and manages the lifecycle of various gameplay systems.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameStateReactionSystem : SystemBase
    {
        #region Fields
        
        /// <summary>
        /// Reference to the game state service for monitoring state changes
        /// </summary>
        private IGameStateService _gameStateService = null!;
        
        /// <summary>
        /// Reference to the service container for dependency injection
        /// </summary>
        private IServiceContainer _services = null!;
        
        /// <summary>
        /// Subscription to game state change events
        /// </summary>
        private IDisposable? _subscription;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the system is created. Initializes the game state service
        /// and subscribes to state change events.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            InitializeGameStateService();
            SubscribeToGameStateChanges();
        }

        /// <summary>
        /// Called every frame. This system doesn't require update logic as it
        /// reacts to events through subscriptions.
        /// </summary>
        protected override void OnUpdate()
        {
            // No update logic needed here, reacts via subscription
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
        /// Initializes the game state service from the service container
        /// </summary>
        private void InitializeGameStateService()
        {
            try
            {
                // Get the service container from GlobalServiceProvider
                _services = GlobalServiceProvider.Instance;
                _gameStateService = _services.Resolve<IGameStateService>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resolve IGameStateService: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to game state change events from the game state service
        /// </summary>
        private void SubscribeToGameStateChanges()
        {
            if (_gameStateService != null)
            {
                _subscription = _gameStateService.StateChanges.Subscribe(OnGameStateChanged);
            }
            else
            {
                Debug.LogError("IGameStateService is null, cannot subscribe to state changes");
            }
        }

        /// <summary>
        /// Handles game state change events and enables/disables systems accordingly
        /// </summary>
        /// <param name="evt">The game state change event</param>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            switch (evt.CurrentState)
            {
                case GameState.Playing:
                    EnableGameplaySystems(true);
                    break;
                    
                case GameState.Paused:
                case GameState.MainMenu:
                case GameState.Loading:
                case GameState.GameOver:
                    EnableGameplaySystems(false);
                    break;
                    
                default:
                    Debug.LogWarning($"Unknown game state: {evt.CurrentState}, disabling gameplay systems");
                    EnableGameplaySystems(false);
                    break;
            }
        }

        /// <summary>
        /// Enables or disables all gameplay-related systems based on the current game state
        /// </summary>
        /// <param name="enabled">True to enable systems, false to disable them</param>
        private void EnableGameplaySystems(bool enabled)
        {
            var systems = World.DefaultGameObjectInjectionWorld?.Systems;
            if (systems == null)
            {
                Debug.LogWarning("No systems found in DefaultGameObjectInjectionWorld");
                return;
            }

            // Enable/disable SystemBase systems (these can be enabled/disabled directly)
            SetSystemBaseEnabled<CombatSystem>(enabled);
            SetSystemBaseEnabled<NetworkSyncSystem>(enabled);
            
            // For ISystem systems, we'll create/update a singleton component that they can query
            // This is the recommended approach in Unity Entities 1.0+
            UpdateGameStateControlComponent(enabled);
            
            // Add more SystemBase systems here as needed
            // SetSystemBaseEnabled<Laboratory.ECS.Systems.YourSystemBaseSystem>(enabled);
        }

        /// <summary>
        /// Enables or disables a specific SystemBase system type
        /// </summary>
        /// <typeparam name="T">The SystemBase system type to enable/disable</typeparam>
        /// <param name="enabled">True to enable the system, false to disable it</param>
        private void SetSystemBaseEnabled<T>(bool enabled) where T : SystemBase
        {
            try
            {
                var system = World.DefaultGameObjectInjectionWorld?.GetExistingSystemManaged<T>();
                if (system != null)
                {
                    system.Enabled = enabled;
                }
                else
                {
                    Debug.LogWarning($"SystemBase of type {typeof(T).Name} not found or not yet created");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set SystemBase {typeof(T).Name} enabled state to {enabled}: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates or creates a singleton component that ISystem systems can query to determine if they should run
        /// This is the recommended approach for controlling ISystem behavior in Unity Entities 1.0+
        /// </summary>
        /// <param name="gameplayEnabled">True if gameplay systems should run, false otherwise</param>
        private void UpdateGameStateControlComponent(bool gameplayEnabled)
        {
            try
            {
                var world = World.DefaultGameObjectInjectionWorld;
                if (world?.EntityManager == null)
                {
                    Debug.LogWarning("EntityManager is null, cannot update game state control component");
                    return;
                }

                var entityManager = world.EntityManager;
                
                // Try to find existing singleton entity
                var query = entityManager.CreateEntityQuery(typeof(GameStateControlComponent));
                
                if (query.IsEmpty)
                {
                    // Create new singleton entity with the component
                    var entity = entityManager.CreateEntity(typeof(GameStateControlComponent));
                    entityManager.SetComponentData(entity, new GameStateControlComponent { GameplayEnabled = gameplayEnabled });
                }
                else
                {
                    // Update existing singleton
                    var entity = query.GetSingletonEntity();
                    entityManager.SetComponentData(entity, new GameStateControlComponent { GameplayEnabled = gameplayEnabled });
                }
                
                query.Dispose();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update game state control component: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up all subscriptions and disposable resources
        /// </summary>
        private void CleanupSubscriptions()
        {
            try
            {
                _subscription?.Dispose();
                _subscription = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during subscription cleanup: {ex.Message}");
            }
        }

        #endregion
    }
}
