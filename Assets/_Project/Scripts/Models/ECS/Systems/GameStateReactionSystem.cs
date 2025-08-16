using System;
using Unity.Entities;
using UniRx;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Infrastructure.AsyncUtils;

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
        /// Reference to the game state manager for monitoring state changes
        /// </summary>
        private GameStateManager _gameStateManager = null!;
        
        /// <summary>
        /// Subscription to game state change events
        /// </summary>
        private IDisposable? _subscription;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the system is created. Initializes the game state manager
        /// and subscribes to state change events.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            InitializeGameStateManager();
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
        /// Initializes the game state manager from the service locator
        /// </summary>
        private void InitializeGameStateManager()
        {
            try
            {
                _gameStateManager = ServiceLocator.Instance.Resolve<GameStateManager>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resolve GameStateManager: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Subscribes to game state change events from the game state manager
        /// </summary>
        private void SubscribeToGameStateChanges()
        {
            if (_gameStateManager?.CurrentState != null)
            {
                _subscription = _gameStateManager.CurrentState.Subscribe(OnGameStateChanged);
            }
            else
            {
                Debug.LogError("GameStateManager or CurrentState is null, cannot subscribe to state changes");
            }
        }

        /// <summary>
        /// Handles game state change events and enables/disables systems accordingly
        /// </summary>
        /// <param name="state">The new game state</param>
        private void OnGameStateChanged(GameStateManager.GameState state)
        {
            switch (state)
            {
                case GameStateManager.GameState.Playing:
                    EnableGameplaySystems(true);
                    break;
                    
                case GameStateManager.GameState.Paused:
                case GameStateManager.GameState.MainMenu:
                case GameStateManager.GameState.Loading:
                case GameStateManager.GameState.GameOver:
                    EnableGameplaySystems(false);
                    break;
                    
                default:
                    Debug.LogWarning($"Unknown game state: {state}, disabling gameplay systems");
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

            // Enable/disable core gameplay systems
            SetSystemEnabled<PhysicsMovementSystem>(enabled);
            SetSystemEnabled<CombatSystem>(enabled);
            SetSystemEnabled<NetworkSyncSystem>(enabled);
            
            // Add more gameplay systems here as needed
            // SetSystemEnabled<Laboratory.ECS.Systems.YourGameplaySystem>(enabled);
        }

        /// <summary>
        /// Enables or disables a specific system type
        /// </summary>
        /// <typeparam name="T">The system type to enable/disable</typeparam>
        /// <param name="enabled">True to enable the system, false to disable it</param>
        private void SetSystemEnabled<T>(bool enabled) where T : SystemBase
        {
            try
            {
                var system = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<T>();
                if (system != null)
                {
                    system.Enabled = enabled;
                }
                else
                {
                    Debug.LogWarning($"System of type {typeof(T).Name} not found or not yet created");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set system {typeof(T).Name} enabled state to {enabled}: {ex.Message}");
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
