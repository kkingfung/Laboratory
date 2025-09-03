using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Systems
{
    /// <summary>
    /// Interface for ability managers that can be managed by the ability system.
    /// This abstraction prevents circular dependencies between Core and Gameplay assemblies.
    /// </summary>
    public interface IAbilityManagerCore
    {
        /// <summary>
        /// Gets the GameObject this ability manager is attached to.
        /// </summary>
        GameObject GameObject { get; }

        /// <summary>
        /// Number of abilities managed by this manager.
        /// </summary>
        int AbilityCount { get; }

        /// <summary>
        /// Attempts to activate an ability by index.
        /// </summary>
        bool ActivateAbility(int index);

        /// <summary>
        /// Gets the remaining cooldown time for an ability.
        /// </summary>
        float GetAbilityCooldown(int index);

        /// <summary>
        /// Checks if an ability is currently on cooldown.
        /// </summary>
        bool IsAbilityOnCooldown(int index);

        /// <summary>
        /// Gets the progress of an ability's cooldown.
        /// </summary>
        float GetAbilityCooldownProgress(int index);

        /// <summary>
        /// Resets all ability cooldowns.
        /// </summary>
        void ResetAllCooldowns();
    }

    /// <summary>
    /// Interface for ability system management across the game.
    /// Uses IAbilityManager interface to avoid circular dependencies.
    /// </summary>
    public interface IAbilitySystem
    {
        /// <summary>
        /// Register an ability manager with the system.
        /// </summary>
        void RegisterAbilityManager(IAbilityManagerCore abilityManager);

        /// <summary>
        /// Unregister an ability manager from the system.
        /// </summary>
        void UnregisterAbilityManager(IAbilityManagerCore abilityManager);

        /// <summary>
        /// Try to activate an ability on a specific manager.
        /// </summary>
        bool TryActivateAbility(IAbilityManagerCore manager, int abilityIndex);

        /// <summary>
        /// Get the cooldown remaining for a specific ability.
        /// </summary>
        float GetAbilityCooldown(IAbilityManagerCore manager, int abilityIndex);

        /// <summary>
        /// Check if an ability is on cooldown.
        /// </summary>
        bool IsAbilityOnCooldown(IAbilityManagerCore manager, int abilityIndex);

        /// <summary>
        /// Get all registered ability managers.
        /// </summary>
        IReadOnlyList<IAbilityManagerCore> GetAllAbilityManagers();

        /// <summary>
        /// Event fired when any ability is activated.
        /// </summary>
        event Action<IAbilityManagerCore, int> OnAbilityActivated;

        /// <summary>
        /// Event fired when any ability cooldown completes.
        /// </summary>
        event Action<IAbilityManagerCore, int> OnAbilityCooldownComplete;

        /// <summary>
        /// Event fired when any ability state changes.
        /// </summary>
        event Action<IAbilityManagerCore, int, bool, float> OnAbilityStateChanged;
    }
}
