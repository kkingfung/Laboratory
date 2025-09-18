using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Subsystems.Combat.Abilities;
using Laboratory.UI.Helper;

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
        [SerializeField] private Text _healthText = null!;
        [SerializeField] private Text _scoreText = null!;
        [SerializeField] private Button[] _abilityButtons = null!;

        [Header("Specialized UI Components")]
        [SerializeField] private MiniMapUI _miniMapUI = null!;
        [SerializeField] private DamageIndicatorUI _damageIndicatorUI = null!;
        [SerializeField] private CrosshairUI _crosshairUI = null!;
        [SerializeField] private ScoreboardUI _scoreboardUI = null!;
        
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
            _crosshairUI.ShowHitFeedback();
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
                .Subscribe(health => _healthText.text = $"Health: {health}")
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds score system to score display UI.
        /// </summary>
        private void BindScoreSystem()
        {
            _scoreVM.Score
                .Subscribe(score => _scoreText.text = $"Score: {score}")
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds ability system to ability buttons and cooldown displays.
        /// Note: AbilityBarUI temporarily disabled due to compilation issues.
        /// </summary>
        private void BindAbilitySystem()
        {
            for (int i = 0; i < _abilityButtons.Length; i++)
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
            var button = _abilityButtons[abilityIndex];
            
            // Bind cooldown visualization
            _abilityVM.GetCooldownRemaining(abilityIndex)
                .Subscribe(cooldown =>
                {
                    button.interactable = cooldown <= 0f;
                    // _abilityBarUI.UpdateCooldown(abilityIndex, cooldown); // Temporarily disabled
                })
                .AddTo(_disposables);

            // Setup button click handler
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                _abilityVM.ActivateAbility(abilityIndex);
                _crosshairUI.ExpandCrosshair(); // Visual feedback on ability use
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
                    _damageIndicatorUI.ShowDamageIndicator(damageSourcePosition, _playerStatusVM.PlayerTransform);
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
                    _miniMapUI.SetPlayerTransform(playerTransform);
                })
                .AddTo(_disposables);
        }
        
        /// <summary>
        /// Binds scoreboard system to player list updates.
        /// Note: ScoreboardUI manages its own networked player list automatically,
        /// so we only need to ensure it's active when in game.
        /// </summary>
        private void BindScoreboardSystem()
        {
            // ScoreboardUI automatically handles networked players through Unity Netcode
            // No manual updates needed - it subscribes to network events internally
            
            // Enable/disable the scoreboard based on game state
            _gameStateVM.IsInGame
                .Subscribe(isInGame =>
                {
                    if (_scoreboardUI != null)
                    {
                        _scoreboardUI.gameObject.SetActive(isInGame);
                    }
                })
                .AddTo(_disposables);

            // Note: ScoreboardUI handles networked players automatically via Unity Netcode
            // Player list updates are managed internally through network events
            // No manual UpdatePlayerList calls needed
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
        UniRx.IObservable<Vector3> DamageTaken { get; }
        
        /// <summary>
        /// Observable stream of player transform changes.
        /// </summary>
        UniRx.IObservable<Transform> PlayerTransformObservable { get; }
        
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
