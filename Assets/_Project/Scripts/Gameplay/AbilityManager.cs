using System;
using System.Collections.Generic;
using UnityEngine;

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
        private readonly List<float> _cooldowns = new();
        private readonly List<bool> _isOnCooldown = new();

        #endregion

        #region Properties

        public int AbilityCount => abilityCount;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            InitializeAbilities();
        }

        private void Update()
        {
            UpdateCooldowns();
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

            if (_isOnCooldown[index])
                return;

            _isOnCooldown[index] = true;
            _cooldowns[index] = DefaultCooldown;

            // Raise event (implementation depends on your event system)
            var evt = new AbilityActivatedEvent(index);
            // EventBus.Publish(evt); // Example, replace with your event system

            var stateEvt = new AbilityStateChangedEvent(index, true, DefaultCooldown);
            // EventBus.Publish(stateEvt);
        }

        #endregion

        #region Private Methods

        private void InitializeAbilities()
        {
            _cooldowns.Clear();
            _isOnCooldown.Clear();
            for (int i = 0; i < abilityCount; i++)
            {
                _cooldowns.Add(0f);
                _isOnCooldown.Add(false);
            }
        }

        private void UpdateCooldowns()
        {
            for (int i = 0; i < abilityCount; i++)
            {
                if (_isOnCooldown[i])
                {
                    _cooldowns[i] -= Time.deltaTime;
                    if (_cooldowns[i] <= 0f)
                    {
                        _cooldowns[i] = 0f;
                        _isOnCooldown[i] = false;

                        var stateEvt = new AbilityStateChangedEvent(i, false, 0f);
                        // EventBus.Publish(stateEvt); // Example, replace with your event system
                    }
                }
            }
        }

        #endregion
    }
}
