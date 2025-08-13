using System;
using MessagePipe;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
namespace Game.Abilities
{
    public class AbilityBarUI : MonoBehaviour, IDisposable
    {
        [SerializeField] private Button[] abilityButtons;
        [SerializeField] private TextMeshProUGUI[] cooldownTexts;

        private ISubscriber<AbilityStateChangedEvent> _stateSubscriber;
        private ISubscriber<AbilityActivatedEvent> _activatedSubscriber;
        private IDisposable _stateSub;
        private IDisposable _activatedSub;

        [Inject]
        public void Construct(
            ISubscriber<AbilityStateChangedEvent> stateSubscriber,
            ISubscriber<AbilityActivatedEvent> activatedSubscriber)
        {
            _stateSubscriber = stateSubscriber;
            _activatedSubscriber = activatedSubscriber;
        }

        private void Awake()
        {
            // Hook button clicks to AbilityManager through publisher
            for (int i = 0; i < abilityButtons.Length; i++)
            {
                int index = i;
                abilityButtons[i].onClick.AddListener(() =>
                {
                    // Instead of calling AbilityManager directly, publish event
                    var publisher = GlobalMessagePipe.GetPublisher<AbilityActivatedEvent>();
                    publisher.Publish(new AbilityActivatedEvent(index));
                });
            }
        }

        private void OnEnable()
        {
            _stateSub = _stateSubscriber.Subscribe(OnAbilityStateChanged);
            _activatedSub = _activatedSubscriber.Subscribe(OnAbilityActivated);
        }

        private void OnDisable()
        {
            _stateSub?.Dispose();
            _activatedSub?.Dispose();
        }

        private void OnAbilityStateChanged(AbilityStateChangedEvent e)
        {
            if (e.AbilityIndex >= abilityButtons.Length) return;

            abilityButtons[e.AbilityIndex].interactable = !e.IsOnCooldown;
            cooldownTexts[e.AbilityIndex].text = e.IsOnCooldown
                ? $"{Mathf.CeilToInt(e.CooldownRemaining)}s"
                : "";
        }

        private void OnAbilityActivated(AbilityActivatedEvent e)
        {
            Debug.Log($"Ability {e.AbilityIndex} activated (UI received event)");
        }

        public void Dispose()
        {
            _stateSub?.Dispose();
            _activatedSub?.Dispose();
        }
    }
}
