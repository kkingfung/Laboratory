using System;
using System.Collections.Generic;
using MessagingPipe;
using UniRx;

namespace Infrastructure
{
    /// <summary>
    /// Manages player abilities including cooldown timers and activation.
    /// Publishes ability events and exposes cooldown states reactively.
    /// </summary>
    public class AbilityManager : IDisposable
    {
        private readonly IMessageBroker _messageBroker;

        // Internal ability data
        private readonly Dictionary<string, CooldownTimer> _abilityCooldowns = new();

        // Exposes ability cooldown remaining time reactively per ability key
        private readonly Dictionary<string, ReactiveProperty<float>> _cooldownRemaining = new();

        /// <summary>
        /// Represents an ability activation event.
        /// </summary>
        public readonly struct AbilityActivatedEvent
        {
            public string AbilityKey { get; }

            public AbilityActivatedEvent(string abilityKey)
            {
                AbilityKey = abilityKey;
            }
        }

        public AbilityManager(IMessageBroker messageBroker)
        {
            _messageBroker = messageBroker ?? throw new ArgumentNullException(nameof(messageBroker));
        }

        /// <summary>
        /// Registers an ability with a cooldown duration in seconds.
        /// </summary>
        public void RegisterAbility(string abilityKey, float cooldownDuration)
        {
            if (string.IsNullOrEmpty(abilityKey))
                throw new ArgumentException("abilityKey cannot be null or empty", nameof(abilityKey));

            if (_abilityCooldowns.ContainsKey(abilityKey))
                throw new InvalidOperationException($"Ability '{abilityKey}' is already registered.");

            _abilityCooldowns[abilityKey] = new CooldownTimer(cooldownDuration);
            _cooldownRemaining[abilityKey] = new ReactiveProperty<float>(0f);
        }

        /// <summary>
        /// Tries to activate an ability. Returns false if on cooldown.
        /// </summary>
        public bool TryActivateAbility(string abilityKey)
        {
            if (!_abilityCooldowns.TryGetValue(abilityKey, out var cooldown))
                throw new InvalidOperationException($"Ability '{abilityKey}' is not registered.");

            if (cooldown.IsActive)
                return false;

            cooldown.Start();
            _messageBroker.Publish(new AbilityActivatedEvent(abilityKey));
            return true;
        }

        /// <summary>
        /// Update all cooldown timers. Call every frame or tick.
        /// </summary>
        public void Tick(float deltaTime)
        {
            foreach (var kvp in _abilityCooldowns)
            {
                var abilityKey = kvp.Key;
                var cooldown = kvp.Value;

                cooldown.Tick(deltaTime);
                _cooldownRemaining[abilityKey].Value = cooldown.Remaining;
            }
        }

        /// <summary>
        /// Returns a reactive property of the cooldown remaining time for an ability.
        /// </summary>
        public IReadOnlyReactiveProperty<float> GetCooldownRemaining(string abilityKey)
        {
            if (!_cooldownRemaining.TryGetValue(abilityKey, out var reactive))
                throw new InvalidOperationException($"Ability '{abilityKey}' is not registered.");

            return reactive;
        }

        /// <summary>
        /// Resets cooldown for the given ability.
        /// </summary>
        public void ResetCooldown(string abilityKey)
        {
            if (!_abilityCooldowns.TryGetValue(abilityKey, out var cooldown))
                throw new InvalidOperationException($"Ability '{abilityKey}' is not registered.");

            cooldown.Reset();
            _cooldownRemaining[abilityKey].Value = 0f;
        }

        public void Dispose()
        {
            foreach (var kvp in _cooldownRemaining)
            {
                kvp.Value.Dispose();
            }
        }
    }
}
