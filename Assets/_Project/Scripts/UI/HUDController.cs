using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Gameplay.Abilities;
using Laboratory.UI.Helper;
using Laboratory.Infrastructure.UI;

namespace Laboratory.UI
{
    /// <summary>
    /// Central HUD controller that manages all in-game UI elements using reactive programming.
    /// Integrates multiple UI components through MVVM pattern and provides centralized HUD state management.
    /// Handles player status display, ability management, scoring, and various UI feedback systems.
    /// </summary>
    public class HUDController : MonoBehaviour, IDisposable
    {
        #region Serialized Fields
        
        [Header("Core UI Elements")]
        [SerializeField] private Text healthText = null!;
        [SerializeField] private Text scoreText = null!;
        [SerializeField] private Button[] abilityButtons = null!;

        [Header("Specialized UI Components")]
        [SerializeField] private MiniMapUI miniMapUI = null!;
        [SerializeField] private DamageIndicatorUI damageIndicatorUI = null!;
        [SerializeField] private CrosshairUI crosshairUI = null!;
        [SerializeField] private AbilityBarUI abilityBarUI = null!;
        [SerializeField] private ScoreboardUI scoreboardUI = null!;
        
        #endregion
        
        #region Fields
        
        /// <summary>
        /// Disposable container for reactive subscriptions
        /// </summary>
        private readonly CompositeDisposable _disposables = new();
        
        /// <summary>
        /// ViewModel for player status information
        /// </summary>
        private IPlayerStatusViewModel _playerStatusVM = null!;
        
        /// <summary>
        /// ViewModel for ability system management
        /// </summary>
        private IAbilityViewModel _abilityVM = null!;
        
        /// <summary>
        /// ViewModel for score tracking
        /// </summary>
        private IScoreViewModel _scoreVM = null!;
        
        /// <summary>
        /// ViewModel for matchmaking information
        /// </summary>
        private IMatchmakingViewModel _matchmakingVM = null!;
        
        /// <summary>
        /// ViewModel for overall game state
        /// </summary>
        private IGameStateViewModel _gameStateVM = null!;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initializes the HUD controller with required ViewModels through dependency injection.
        /// </summary>
        /// <param name="playerStatus">ViewModel for player status information</param>
        /// <param name="ability">ViewModel for ability system</param>
        /// <param name="score">ViewModel for score tracking</param>
        /// <param name="matchmaking">ViewModel for matchmaking data</param>
        /// <param name="gameState">ViewModel for game state management</param>
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

            BindViewModels();
        }
        
        /// <summary>
        /// Displays crosshair hit feedback for successful hits.
        /// </summary>
        public void ShowCrosshairHitFeedback()
        {
            crosshairUI.ShowHitFeedback();
        }
        
        /// <summary>
        /// Releases all resources used by the HUD controller.
        /// </summary>
        public void Dispose()
        {
            _disposables?.Dispose();
        }
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Unity lifecycle method called when the GameObject is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            Dispose();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Binds all ViewModels to their respective UI elements using reactive subscriptions.
        /// </summary>
        private void BindViewModels()
        {
            _disposables.Clear();

            BindPlayerStatus();
            BindScoreSystem();
            BindAbilitySystem();
            BindDamageSystem();
            BindMiniMapSystem();
            BindScoreboardSystem();
            BindGameStateSystem();
        }
        
        /// <summary>
        /// Binds player status information to UI elements.
        /// </summary>
        private void BindPlayerStatus()
        {
            _playerStatusVM.Health
                .Subscribe(health => healthText.text = $"Health: {health}")
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds score system to score display UI.
        /// </summary>
        private void BindScoreSystem()
        {
            _scoreVM.Score
                .Subscribe(score => scoreText.text = $"Score: {score}")
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds ability system to ability buttons and cooldown displays.
        /// </summary>
        private void BindAbilitySystem()
        {
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                BindAbilityButton(i);
            }
        }
        
        /// <summary>
        /// Binds a specific ability button to its corresponding ability system.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to bind</param>
        private void BindAbilityButton(int abilityIndex)
        {
            var button = abilityButtons[abilityIndex];
            
            // Bind cooldown visualization
            _abilityVM.GetCooldownRemaining(abilityIndex)
                .Subscribe(cooldown =>
                {
                    button.interactable = cooldown <= 0f;
                    abilityBarUI.UpdateCooldown(abilityIndex, cooldown);
                })
                .AddTo(_disposables);

            // Setup button click handler
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                _abilityVM.ActivateAbility(abilityIndex);
                crosshairUI.ExpandCrosshair(); // Visual feedback on ability use
            });
        }
        
        /// <summary>
        /// Binds damage system to damage indicator UI.
        /// </summary>
        private void BindDamageSystem()
        {
            _playerStatusVM.DamageTaken
                .Subscribe(damageSourcePosition =>
                {
                    damageIndicatorUI.ShowDamageIndicator(damageSourcePosition, _playerStatusVM.PlayerTransform);
                })
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds minimap system to player transform updates.
        /// </summary>
        private void BindMiniMapSystem()
        {
            _playerStatusVM.PlayerTransformObservable
                .Subscribe(playerTransform =>
                {
                    miniMapUI.SetPlayerTransform(playerTransform);
                })
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds scoreboard system to player list updates.
        /// </summary>
        private void BindScoreboardSystem()
        {
            _matchmakingVM.PlayerList
                .Subscribe(playerList =>
                {
                    scoreboardUI.UpdateScoreboard(playerList);
                })
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds game state system to HUD visibility controls.
        /// </summary>
        private void BindGameStateSystem()
        {
            _gameStateVM.IsInGame
                .Subscribe(isInGame =>
                {
                    gameObject.SetActive(isInGame);
                })
                .AddTo(_disposables);
        }
        
        #endregion
    }
    
    #region ViewModel Interfaces
    
    /// <summary>
    /// Interface for player status information management.
    /// </summary>
    public interface IPlayerStatusViewModel
    {
        /// <summary>
        /// Reactive property for player health value.
        /// </summary>
        IReadOnlyReactiveProperty<int> Health { get; }
        
        /// <summary>
        /// Observable stream of damage events with source positions.
        /// </summary>
        IObservable<Vector3> DamageTaken { get; }
        
        /// <summary>
        /// Observable stream of player transform changes.
        /// </summary>
        IObservable<Transform> PlayerTransformObservable { get; }
        
        /// <summary>
        /// Current player transform reference.
        /// </summary>
        Transform PlayerTransform { get; }
    }
    
    /// <summary>
    /// Interface for ability system management.
    /// </summary>
    public interface IAbilityViewModel
    {
        /// <summary>
        /// Gets the remaining cooldown time for a specific ability.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to check</param>
        /// <returns>Reactive property containing cooldown remaining in seconds</returns>
        IReadOnlyReactiveProperty<float> GetCooldownRemaining(int abilityIndex);
        
        /// <summary>
        /// Activates the specified ability if available.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to activate</param>
        void ActivateAbility(int abilityIndex);
    }
    
    /// <summary>
    /// Interface for score system management.
    /// </summary>
    public interface IScoreViewModel
    {
        /// <summary>
        /// Reactive property for current score value.
        /// </summary>
        IReadOnlyReactiveProperty<int> Score { get; }
    }
    
    /// <summary>
    /// Interface for matchmaking and player list management.
    /// </summary>
    public interface IMatchmakingViewModel
    {
        /// <summary>
        /// Reactive property for current player list in the match.
        /// </summary>
        IReadOnlyReactiveProperty<List<PlayerData>> PlayerList { get; }
    }
    
    /// <summary>
    /// Interface for overall game state management.
    /// </summary>
    public interface IGameStateViewModel
    {
        /// <summary>
        /// Reactive property indicating whether the player is currently in an active game.
        /// </summary>
        IReadOnlyReactiveProperty<bool> IsInGame { get; }
    }
    
    #endregion
    
    #region Inner Classes, Enums
    
    /// <summary>
    /// Data structure representing player information in the match.
    /// </summary>
    [Serializable]
    public class PlayerData
    {
        /// <summary>
        /// Unique identifier for the player.
        /// </summary>
        public string playerId = "";
        
        /// <summary>
        /// Display name of the player.
        /// </summary>
        public string playerName = "";
        
        /// <summary>
        /// Current score of the player.
        /// </summary>
        public int score = 0;
    }
    
    #endregion
}
