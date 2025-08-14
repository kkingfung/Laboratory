using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Game.UI;
using Game.Abilities;

// FIXME: tidyup after 8/29
namespace Infrastructure.UI
{
    /// <summary>
    /// Central controller managing the HUD UI elements reactively.
    /// Integrates multiple UI components using MVVM pattern.
    /// </summary>
    public class HUDController : MonoBehaviour, IDisposable
    {
        [Header("UI Elements")]
        [SerializeField] private UnityEngine.UI.Text healthText = null!;
        [SerializeField] private UnityEngine.UI.Text scoreText = null!;
        [SerializeField] private UnityEngine.UI.Button[] abilityButtons = null!;

        [SerializeField] private MiniMapUI miniMapUI = null!;
        [SerializeField] private DamageIndicatorUI damageIndicatorUI = null!;
        [SerializeField] private CrosshairUI crosshairUI = null!;
        [SerializeField] private AbilityBarUI abilityBarUI = null!;
        [SerializeField] private ScoreboardUI scoreboardUI = null!;

        private CompositeDisposable _disposables = new();

        // ViewModel references to bind to (assign these from a DI container or setup method)
        private IPlayerStatusViewModel _playerStatusVM = null!;
        private IAbilityViewModel _abilityVM = null!;
        private IScoreViewModel _scoreVM = null!;
        private IMatchmakingViewModel _matchmakingVM = null!;
        private IGameStateViewModel _gameStateVM = null!;

        #region Initialization & Binding

        /// <summary>
        /// Call this to inject ViewModels after creation.
        /// </summary>
        public void Initialize(
            IPlayerStatusViewModel playerStatus,
            IAbilityViewModel ability,
            IScoreViewModel score,
            IMatchmakingViewModel matchmaking,
            IGameStateViewModel gameState)
        {
            _playerStatusVM = playerStatus;
            _abilityVM = ability;
            _scoreVM = score;
            _matchmakingVM = matchmaking;
            _gameStateVM = gameState;

            Bind();
        }

        private void Bind()
        {
            _disposables.Clear();

            // Player health
            _playerStatusVM.Health
                .Subscribe(h => healthText.text = $"Health: {h}")
                .AddTo(_disposables);

            // Score display
            _scoreVM.Score
                .Subscribe(s => scoreText.text = $"Score: {s}")
                .AddTo(_disposables);

            // Ability buttons and cooldowns
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                int index = i;
                var btn = abilityButtons[i];

                // Bind cooldown visual on abilityBarUI
                _abilityVM.GetCooldownRemaining(index)
                    .Subscribe(cd =>
                    {
                        btn.interactable = cd <= 0f;
                        abilityBarUI.UpdateCooldown(index, cd);
                    })
                    .AddTo(_disposables);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    _abilityVM.ActivateAbility(index);
                    crosshairUI.ExpandCrosshair(); // crosshair feedback on ability use
                });
            }

            // Example: subscribe to damage events (requires playerStatusVM to expose them)
            _playerStatusVM.DamageTaken
                .Subscribe(dmgSourcePos =>
                {
                    damageIndicatorUI.ShowDamageIndicator(dmgSourcePos, _playerStatusVM.PlayerTransform);
                })
                .AddTo(_disposables);

            // Example: Update minimap player reference (if player transform changes)
            _playerStatusVM.PlayerTransformObservable
                .Subscribe(t =>
                {
                    miniMapUI.SetPlayerTransform(t);
                })
                .AddTo(_disposables);

            // Scoreboard updates (assuming matchmakingVM provides player list)
            _matchmakingVM.PlayerList
                .Subscribe(players =>
                {
                    scoreboardUI.UpdateScoreboard(players);
                })
                .AddTo(_disposables);

            // Handle game state changes (example: hide/show HUD in menus)
            _gameStateVM.IsInGame
                .Subscribe(isInGame =>
                {
                    gameObject.SetActive(isInGame);
                })
                .AddTo(_disposables);
        }

        #endregion

        #region Damage & Hit Feedback

        /// <summary>
        /// Call externally to show crosshair hit feedback.
        /// </summary>
        public void ShowCrosshairHitFeedback()
        {
            crosshairUI.ShowHitFeedback();
        }

        #endregion

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

    #region ViewModel Interfaces (examples, expand to fit your game)

    public interface IPlayerStatusViewModel
    {
        IReadOnlyReactiveProperty<int> Health { get; }
        IObservable<Vector3> DamageTaken { get; } // position of damage source
        IObservable<Transform> PlayerTransformObservable { get; }
        Transform PlayerTransform { get; }
    }

    public interface IAbilityViewModel
    {
        IReadOnlyReactiveProperty<float> GetCooldownRemaining(int abilityIndex);
        void ActivateAbility(int abilityIndex);
    }

    public interface IScoreViewModel
    {
        IReadOnlyReactiveProperty<int> Score { get; }
    }

    public interface IMatchmakingViewModel
    {
        IReadOnlyReactiveProperty<System.Collections.Generic.List<PlayerData>> PlayerList { get; }
    }

    public interface IGameStateViewModel
    {
        IReadOnlyReactiveProperty<bool> IsInGame { get; }
    }

    #endregion

    #region Example PlayerData Class (replace with your data source)

    public class PlayerData
    {
        public string playerId = "";
        public string playerName = "";
        public int score = 0;
    }

    #endregion
}
