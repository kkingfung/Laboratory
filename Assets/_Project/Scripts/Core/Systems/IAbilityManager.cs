using System;
using UnityEngine;

namespace Laboratory.Core.Systems
{
    /// <summary>
    /// Interface for ability management systems.
    /// Provides core ability management functionality including activation, cooldown tracking, and state queries.
    /// </summary>
    public interface IAbilityManager
    {
        /// <summary>
        /// Event triggered when an ability is activated.
        /// </summary>
        event Action<int> OnAbilityActivated;
        
        /// <summary>
        /// Event triggered when an ability's state changes.
        /// </summary>
        event Action<int, bool> OnAbilityStateChanged;
        
        /// <summary>
        /// Gets the total number of abilities managed by this manager.
        /// </summary>
        int AbilityCount { get; }
        
        /// <summary>
        /// Activates the specified ability if it's not on cooldown.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to activate.</param>
        /// <returns>True if the ability was successfully activated, false otherwise.</returns>
        bool ActivateAbility(int abilityIndex);
        
        /// <summary>
        /// Attempts to activate an ability, returning false if it's on cooldown.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to activate.</param>
        /// <returns>True if the ability was activated, false if on cooldown or invalid.</returns>
        bool TryActivateAbility(int abilityIndex);
        
        /// <summary>
        /// Checks if the specified ability is currently on cooldown.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to check.</param>
        /// <returns>True if the ability is on cooldown, false otherwise.</returns>
        bool IsAbilityOnCooldown(int abilityIndex);
        
        /// <summary>
        /// Gets the remaining cooldown time for the specified ability.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to check.</param>
        /// <returns>Remaining cooldown time in seconds, 0 if not on cooldown.</returns>
        float GetAbilityCooldown(int abilityIndex);
        
        /// <summary>
        /// Resets all ability cooldowns to zero.
        /// </summary>
        void ResetAllCooldowns();
        
        /// <summary>
        /// Gets ability data for the specified ability.
        /// </summary>
        /// <param name="abilityIndex">Index of the ability.</param>
        /// <returns>Ability data structure containing ability information.</returns>
        object GetAbilityData(int abilityIndex);
    }
}