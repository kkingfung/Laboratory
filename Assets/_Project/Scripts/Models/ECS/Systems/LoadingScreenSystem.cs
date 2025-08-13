using System;
using Unity.Entities;
using Infrastructure;
using Infrastructure.UI;
using UniRx;
using System.Threading.Tasks;
using UnityEngine;
// FIXME: tidyup after 8/29
namespace Models.ECS.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class LoadingScreenSystem : SystemBase
    {
        private GameStateManager _gameStateManager = null!;
        private LoadingScreen _loadingScreen = null!;
        private bool _isLoading = false;
        private IDisposable? _stateSubscription;

        protected override void OnCreate()
        {
            base.OnCreate();

            _gameStateManager = Infrastructure.ServiceLocator.Instance.Resolve<GameStateManager>();
            _loadingScreen = Infrastructure.ServiceLocator.Instance.Resolve<LoadingScreen>();

            _stateSubscription = _gameStateManager.CurrentState
                .Where(state => state == GameStateManager.GameState.Loading)
                .Subscribe(async _ => await StartLoadingAsync());
        }

        private async Task StartLoadingAsync()
        {
            if (_isLoading) return;

            _isLoading = true;

            // Example: load the "GameScene"
            await _loadingScreen.LoadSceneAsync("GameScene");

            // After loading, switch to Playing state
            _gameStateManager.SetState(GameStateManager.GameState.Playing);

            _isLoading = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _stateSubscription?.Dispose();
        }

        protected override void OnUpdate()
        {
            // No update logic needed; reacts via subscription
        }
    }
}
