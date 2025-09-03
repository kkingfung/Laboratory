using System;
using Laboratory.Gameplay.Abilities;

namespace Laboratory.Core.Systems
{
    /// <summary>
    /// Interface for ability system management across the game.
    /// Provides centralized ability operations for both MonoBehaviour and ECS systems.
    /// </summary>
    public interface IAbilitySystem
    {
        /// <summary>
        /// Register an ability manager with the system.
        /// </summary>
        void RegisterAbilityManager(AbilityManager abilityManager);

        /// <summary>
        /// Unregister an ability manager from the system.
        /// </summary>
        void UnregisterAbilityManager(AbilityManager abilityManager);

        /// <summary>
        /// Try to activate an ability on a specific manager.
        /// </summary>
        bool TryActivateAbility(AbilityManager manager, int abilityIndex);

        /// <summary>
        /// Get the cooldown remaining for a specific ability.
        /// </summary>
        float GetAbilityCooldown(AbilityManager manager, int abilityIndex);

        /// <summary>
        /// Check if an ability is on cooldown.
        /// </summary>
        bool IsAbilityOnCooldown(AbilityManager manager, int abilityIndex);

        /// <summary>
        /// Get all registered ability managers.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<AbilityManager> GetAllAbilityManagers();

        /// <summary>
        /// Event fired when any ability is activated.
        /// </summary>
        event Action<AbilityManager, int> OnAbilityActivated;

        /// <summary>
        /// Event fired when any ability cooldown completes.
        /// </summary>
        event Action<AbilityManager, int> OnAbilityCooldownComplete;

        /// <summary>
        /// Event fired when any ability state changes.
        /// </summary>
        event Action<AbilityManager, int, bool, float> OnAbilityStateChanged;
    }
}
