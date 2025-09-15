using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Abilities
{
    /// <summary>
    /// Core interface for ability managers in the system.
    /// Provides essential functionality for ability management and system integration.
    /// </summary>
    public interface IAbilityManagerCore
    {
        /// <summary>
        /// The GameObject this manager is attached to
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Number of abilities managed by this manager
        /// </summary>
        int AbilityCount { get; }

        /// <summary>
        /// Activates an ability at the specified index
        /// </summary>
        /// <param name="abilityIndex">Index of the ability to activate</param>
        /// <returns>True if the ability was successfully activated</returns>
        bool ActivateAbility(int abilityIndex);

        /// <summary>
        /// Gets the remaining cooldown time for an ability
        /// </summary>
        /// <param name="abilityIndex">Index of the ability</param>
        /// <returns>Remaining cooldown time in seconds</returns>
        float GetAbilityCooldown(int abilityIndex);

        /// <summary>
        /// Checks if an ability is currently on cooldown
        /// </summary>
        /// <param name="abilityIndex">Index of the ability</param>
        /// <returns>True if the ability is on cooldown</returns>
        bool IsAbilityOnCooldown(int abilityIndex);

        /// <summary>
        /// Resets all ability cooldowns
        /// </summary>
        void ResetAllCooldowns();

        /// <summary>
        /// Gets the ability at the specified index
        /// </summary>
        /// <param name="abilityIndex">Index of the ability</param>
        /// <returns>The ability instance or null if not found</returns>
        AbilityBase GetAbility(int abilityIndex);

        /// <summary>
        /// Gets all abilities managed by this manager
        /// </summary>
        /// <returns>Read-only list of abilities</returns>
        IReadOnlyList<AbilityBase> GetAllAbilities();

        /// <summary>
        /// Event fired when an ability is activated
        /// </summary>
        event Action<int> OnAbilityActivated;

        /// <summary>
        /// Event fired when an ability's cooldown completes
        /// </summary>
        event Action<int> OnAbilityCooldownComplete;

        /// <summary>
        /// Event fired when an ability's state changes
        /// </summary>
        event Action<int, bool, float> OnAbilityStateChanged;
    }
}
