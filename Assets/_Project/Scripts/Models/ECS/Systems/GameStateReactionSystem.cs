using System;
using Unity.Entities;
using Infrastructure;
using UniRx;
using UnityEngine;

namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class GameStateReactionSystem : SystemBase
    {
        private GameStateManager _gameStateManager = null!;
        private IDisposable? _subscription;

        protected override void OnCreate()
        {
            base.OnCreate();

            // Resolve GameStateManager from ServiceLocator or dependency container
            _gameStateManager = Infrastructure.ServiceLocator.Instance.Resolve<GameStateManager>();

            _subscription = _gameStateManager.CurrentState.Subscribe(OnGameStateChanged);
        }

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
                    EnableGameplaySystems(false);
                    break;
            }
        }

        private void EnableGameplaySystems(bool enabled)
        {
            // Example: Enable or disable certain systems
            var systems = World.DefaultGameObjectInjectionWorld?.Systems;
            if (systems == null) return;

            // Replace these names with your actual gameplay system types
            SetSystemEnabled<Models.ECS.Systems.PhysicsMovementSystem>(enabled);
            SetSystemEnabled<Models.ECS.Systems.CombatSystem>(enabled);
            SetSystemEnabled<Models.ECS.Systems.NetworkSyncSystem>(enabled);
            // Add more gameplay systems here as needed
        }

        private void SetSystemEnabled<T>(bool enabled) where T : SystemBase
        {
            var sys = World.DefaultGameObjectInjectionWorld?.GetExistingSystem<T>();
            if (sys != null)
                sys.Enabled = enabled;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _subscription?.Dispose();
        }

        protected override void OnUpdate()
        {
            // No update logic needed here, reacts via subscription
        }
    }
}
