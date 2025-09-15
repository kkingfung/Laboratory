using System;
using System.Collections.Generic;
using Laboratory.Core.Abilities.Interfaces;

namespace Laboratory.Core.Abilities.Systems
{
    /// <summary>
    /// Interface for core ability execution in the abilities layer  
    /// </summary>
    public interface ICoreAbilityExecutor
    {
        bool ExecuteAbility(string abilityId);
        bool CanExecuteAbility(string abilityId);
        void RegisterAbility(string abilityId, object ability);
        void UnregisterAbility(string abilityId);
    }

    /// <summary>
    /// Interface for ability system management across the game.
    /// Provides centralized ability operations for both MonoBehaviour and ECS systems.
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
