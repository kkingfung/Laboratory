using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MessagePipe;
using UnityEngine;

namespace Game.Abilities
{
    public class AbilityManager : MonoBehaviour
    {
        [SerializeField] private List<AbilityData> abilities;

        private IPublisher<AbilityStateChangedEvent> _statePublisher;
        private IPublisher<AbilityActivatedEvent> _activatedPublisher;
        private float[] _cooldownTimers;

        [Serializable]
        public class AbilityData
        {
            public string Name;
            public float Cooldown;
        }

        [Inject]
        public void Construct(
            IPublisher<AbilityStateChangedEvent> statePublisher,
            IPublisher<AbilityActivatedEvent> activatedPublisher)
        {
            _statePublisher = statePublisher;
            _activatedPublisher = activatedPublisher;
        }

        private void Awake()
        {
            _cooldownTimers = new float[abilities.Count];
        }

        private void Update()
        {
            for (int i = 0; i < _cooldownTimers.Length; i++)
            {
                if (_cooldownTimers[i] > 0f)
                {
                    _cooldownTimers[i] -= Time.deltaTime;
                    PublishAbilityState(i);
                }
            }
        }

        public void ActivateAbility(int index)
        {
            if (index < 0 || index >= abilities.Count) return;

            if (_cooldownTimers[index] <= 0f)
            {
                _cooldownTimers[index] = abilities[index].Cooldown;
                _activatedPublisher.Publish(new AbilityActivatedEvent(index));
                PublishAbilityState(index);

                // Simulate ability execution async (optional)
                UseAbility(index).Forget();
            }
        }

        private async UniTaskVoid UseAbility(int index)
        {
            Debug.Log($"Using ability: {abilities[index].Name}");
            await UniTask.Delay(500); // Example: wait for cast animation
        }

        private void PublishAbilityState(int index)
        {
            bool onCooldown = _cooldownTimers[index] > 0f;
            float cdRemaining = Mathf.Max(_cooldownTimers[index], 0f);
            _statePublisher.Publish(new AbilityStateChangedEvent(index, onCooldown, cdRemaining));
        }
    }
}
