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

        private CompositeDisposable disposables = new();

        // ViewModel references to bind to (assign these from a DI container or setup method)
        private IPlayerStatusViewModel playerStatusVM = null!;
        private IAbilityViewModel abilityVM = null!;
        private IScoreViewModel scoreVM = null!;
        private IMatchmakingViewModel matchmakingVM = null!; // hypothetical
        private IGameStateViewModel gameStateVM = null!; // hypothetical

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
            playerStatusVM = playerStatus;
            abilityVM = ability;
            scoreVM = score;
            matchmakingVM = matchmaking;
            gameStateVM = gameState;

            Bind();
        }

        private void Bind()
        {
            disposables.Clear();

            // Player health
            playerStatusVM.Health
                .Subscribe(h => healthText.text = $"Health: {h}")
                .AddTo(disposables);

            // Score display
            scoreVM.Score
                .Subscribe(s => scoreText.text = $"Score: {s}")
                .AddTo(disposables);

            // Ability buttons and cooldowns
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                int index = i;
                var btn = abilityButtons[i];

                // Bind cooldown visual on abilityBarUI
                abilityVM.GetCooldownRemaining(index)
                    .Subscribe(cd =>
                    {
                        btn.interactable = cd <= 0f;
                        abilityBarUI.UpdateCooldown(index, cd);
                    })
                    .AddTo(disposables);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    abilityVM.ActivateAbility(index);
                    crosshairUI.ExpandCrosshair(); // crosshair feedback on ability use
                });
            }

            // Example: subscribe to damage events (requires playerStatusVM to expose them)
            playerStatusVM.DamageTaken
                .Subscribe(dmgSourcePos =>
                {
                    damageIndicatorUI.ShowDamageIndicator(dmgSourcePos, playerStatusVM.PlayerTransform);
                })
                .AddTo(disposables);

            // Example: Update minimap player reference (if player transform changes)
            playerStatusVM.PlayerTransformObservable
                .Subscribe(t =>
                {
                    miniMapUI.SetPlayerTransform(t);
                })
                .AddTo(disposables);

            // Scoreboard updates (assuming matchmakingVM provides player list)
            matchmakingVM.PlayerList
                .Subscribe(players =>
                {
                    scoreboardUI.UpdateScoreboard(players);
                })
                .AddTo(disposables);

            // Handle game state changes (example: hide/show HUD in menus)
            gameStateVM.IsInGame
                .Subscribe(isInGame =>
                {
                    gameObject.SetActive(isInGame);
                })
                .AddTo(disposables);
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
            disposables.Dispose();
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
