using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Timing;

namespace Laboratory.Gameplay.Abilities
{
    /// <summary>
    /// Manages player abilities, cooldowns, and activation logic.
    /// </summary>
    public class AbilityManager : MonoBehaviour
    {
        #region Constants

        private const float DefaultCooldown = 5f;

        #endregion

        #region Fields

        [SerializeField] private int abilityCount = 3;
        private readonly List<CooldownTimer> _abilityCooldowns = new();
        private readonly List<AbilityData> _abilities = new();

        #endregion

        #region Data Structures

        [System.Serializable]
        private class AbilityData
        {
            public string name;
            public float cooldownDuration = DefaultCooldown;
            public int index;

            public AbilityData(int index, string name = null, float cooldown = DefaultCooldown)
            {
                this.index = index;
                this.name = string.IsNullOrEmpty(name) ? $"Ability {index + 1}" : name;
                this.cooldownDuration = cooldown;
            }
        }

        #endregion

        #region Properties

        public int AbilityCount => abilityCount;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            InitializeAbilities();
        }

        private void OnDestroy()
        {
            // Clean up timers
            foreach (var cooldown in _abilityCooldowns)
            {
                cooldown?.Dispose();
            }
            _abilityCooldowns.Clear();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to activate an ability by index.
        /// </summary>
        public void ActivateAbility(int index)
        {
            if (index < 0 || index >= abilityCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            var cooldownTimer = _abilityCooldowns[index];
            if (cooldownTimer.IsActive)
                return; // Ability is on cooldown

            // Start cooldown
            cooldownTimer.Start();

            // Raise ability activated event
            var evt = new AbilityActivatedEvent(index);
            // EventBus.Publish(evt); // Example, replace with your event system

            var stateEvt = new AbilityStateChangedEvent(index, true, cooldownTimer.Duration);
            // EventBus.Publish(stateEvt);
        }

        /// <summary>
        /// Gets the remaining cooldown time for an ability.
        /// </summary>
        /// <param name="index">Ability index</param>
        /// <returns>Remaining cooldown time in seconds</returns>
        public float GetAbilityCooldown(int index)
        {
            if (index < 0 || index >= abilityCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _abilityCooldowns[index].Remaining;
        }

        /// <summary>
        /// Checks if an ability is currently on cooldown.
        /// </summary>
        /// <param name="index">Ability index</param>
        /// <returns>True if ability is on cooldown</returns>
        public bool IsAbilityOnCooldown(int index)
        {
            if (index < 0 || index >= abilityCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _abilityCooldowns[index].IsActive;
        }

        #endregion

        #region Private Methods

        private void InitializeAbilities()
        {
            _abilities.Clear();
            _abilityCooldowns.Clear();
            
            for (int i = 0; i < abilityCount; i++)
            {
                // Create ability data
                var abilityData = new AbilityData(i);
                _abilities.Add(abilityData);
                
                // Create cooldown timer with event callbacks
                var cooldownTimer = new CooldownTimer(abilityData.cooldownDuration);
                cooldownTimer.OnCompleted += () => OnAbilityCooldownComplete(i);
                _abilityCooldowns.Add(cooldownTimer);
            }
        }
        
        private void OnAbilityCooldownComplete(int abilityIndex)
        {
            // Fire ability ready event
            var stateEvt = new AbilityStateChangedEvent(abilityIndex, false, 0f);
            // EventBus.Publish(stateEvt); // Example, replace with your event system
        }

        #endregion
    }
}
