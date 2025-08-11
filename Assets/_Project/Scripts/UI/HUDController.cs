using System;
using UniRx;
using UnityEngine;

namespace Infrastructure.UI
{
    /// <summary>
    /// Controller class that binds HUD UI elements to ViewModel data and handles UI interactions.
    /// Designed to be used with MVVM pattern.
    /// </summary>
    public class HUDController : IDisposable
    {
        // Example ViewModel interfaces or classes you would inject or assign
        private readonly IPlayerStatusViewModel _playerStatusVM;
        private readonly IAbilityViewModel _abilityVM;
        private readonly IScoreViewModel _scoreVM;

        private readonly CompositeDisposable _disposables = new();

        // References to actual UI elements (assign via inspector or dynamically)
        private readonly UnityEngine.UI.Text _healthText;
        private readonly UnityEngine.UI.Text _scoreText;
        private readonly UnityEngine.UI.Button[] _abilityButtons;

        public HUDController(
            IPlayerStatusViewModel playerStatusVM,
            IAbilityViewModel abilityVM,
            IScoreViewModel scoreVM,
            UnityEngine.UI.Text healthText,
            UnityEngine.UI.Text scoreText,
            UnityEngine.UI.Button[] abilityButtons)
        {
            _playerStatusVM = playerStatusVM;
            _abilityVM = abilityVM;
            _scoreVM = scoreVM;

            _healthText = healthText;
            _scoreText = scoreText;
            _abilityButtons = abilityButtons;

            Bind();
        }

        private void Bind()
        {
            // Bind player health text reactively
            _playerStatusVM.Health
                .Subscribe(health => _healthText.text = $"Health: {health}")
                .AddTo(_disposables);

            // Bind score text reactively
            _scoreVM.Score
                .Subscribe(score => _scoreText.text = $"Score: {score}")
                .AddTo(_disposables);

            // Bind ability buttons cooldown and click handlers
            for (int i = 0; i < _abilityButtons.Length; i++)
            {
                int index = i; // capture for closure
                var button = _abilityButtons[index];

                // Example: subscribe to cooldown time and disable button if cooldown active
                _abilityVM.GetCooldownRemaining(index)
                    .Subscribe(cd =>
                    {
                        button.interactable = cd <= 0f;
                        // Optionally update UI visuals like cooldown overlays
                    })
                    .AddTo(_disposables);

                // Add click listener to trigger ability activation
                button.onClick.AddListener(() =>
                {
                    _abilityVM.ActivateAbility(index);
                });
            }
        }

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }

    // Example interfaces for ViewModels, adjust as per your actual ViewModel implementations
    public interface IPlayerStatusViewModel
    {
        IReadOnlyReactiveProperty<int> Health { get; }
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
}
