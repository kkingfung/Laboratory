using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Laboratory.Gameplay.Abilities;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI component for displaying ability buttons and cooldown timers.
    /// Handles ability activation through MessagePipe events and updates visual states.
    /// </summary>
    public class AbilityBarUI : MonoBehaviour, IDisposable
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private Button[] abilityButtons;
        [SerializeField] private TextMeshProUGUI[] cooldownTexts;

        private ISubscriber<AbilityStateChangedEvent> _stateSubscriber;
        private ISubscriber<AbilityActivatedEvent> _activatedSubscriber;
        private IDisposable _stateSub;
        private IDisposable _activatedSub;

        #endregion

        #region Dependency Injection

        /// <summary>
        /// Constructs the ability bar with required dependencies.
        /// </summary>
        /// <param name="stateSubscriber">Subscriber for ability state change events</param>
        /// <param name="activatedSubscriber">Subscriber for ability activation events</param>
        [Inject]
        public void Construct(
            ISubscriber<AbilityStateChangedEvent> stateSubscriber,
            ISubscriber<AbilityActivatedEvent> activatedSubscriber)
        {
            _stateSubscriber = stateSubscriber;
            _activatedSubscriber = activatedSubscriber;
        }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize button click handlers and ability activation events.
        /// </summary>
        private void Awake()
        {
            SetupButtonHandlers();
        }

        /// <summary>
        /// Subscribe to ability events when enabled.
        /// </summary>
        private void OnEnable()
        {
            _stateSub = _stateSubscriber?.Subscribe(OnAbilityStateChanged);
            _activatedSub = _activatedSubscriber?.Subscribe(OnAbilityActivated);
        }

        /// <summary>
        /// Unsubscribe from ability events when disabled.
        /// </summary>
        private void OnDisable()
        {
            _stateSub?.Dispose();
            _activatedSub?.Dispose();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets up button click handlers for ability activation.
        /// </summary>
        private void SetupButtonHandlers()
        {
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                int index = i; // Capture loop variable
                abilityButtons[i].onClick.AddListener(() =>
                {
                    var publisher = GlobalMessagePipe.GetPublisher<AbilityActivatedEvent>();
                    publisher.Publish(new AbilityActivatedEvent(index));
                });
            }
        }

        /// <summary>
        /// Handles ability state change events and updates UI accordingly.
        /// </summary>
        /// <param name="e">Ability state changed event data</param>
        private void OnAbilityStateChanged(AbilityStateChangedEvent e)
        {
            if (e.AbilityIndex >= abilityButtons.Length) return;

            abilityButtons[e.AbilityIndex].interactable = !e.IsOnCooldown;
            cooldownTexts[e.AbilityIndex].text = e.IsOnCooldown
                ? $"{Mathf.CeilToInt(e.CooldownRemaining)}s"
                : "";
        }

        /// <summary>
        /// Handles ability activation events.
        /// </summary>
        /// <param name="e">Ability activated event data</param>
        private void OnAbilityActivated(AbilityActivatedEvent e)
        {
            Debug.Log($"Ability {e.AbilityIndex} activated (UI received event)");
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Disposes of event subscriptions and cleanup resources.
        /// </summary>
        public void Dispose()
        {
            _stateSub?.Dispose();
            _activatedSub?.Dispose();
        }

        #endregion
    }
}
