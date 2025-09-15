using System;

namespace Laboratory.Core.Systems
{
    /// <summary>
    /// Interface for ability system management across the game.
    /// Provides centralized ability operations for both MonoBehaviour and ECS systems.
    /// </summary>
    public interface IGameplayAbilitySystem
    {
        /// <summary>
        /// Register an ability manager with the system.
        /// </summary>
        void RegisterAbilityManager(IAbilityManager abilityManager);

        /// <summary>
        /// Unregister an ability manager from the system.
        /// </summary>
        void UnregisterAbilityManager(IAbilityManager abilityManager);

        /// <summary>
        /// Try to activate an ability on a specific manager.
        /// </summary>
        bool TryActivateAbility(IAbilityManager manager, int abilityIndex);

        /// <summary>
        /// Get the cooldown remaining for a specific ability.
        /// </summary>
        float GetAbilityCooldown(IAbilityManager manager, int abilityIndex);

        /// <summary>
        /// Check if an ability is on cooldown.
        /// </summary>
        bool IsAbilityOnCooldown(IAbilityManager manager, int abilityIndex);

        /// <summary>
        /// Get all registered ability managers.
        /// </summary>
        System.Collections.Generic.IReadOnlyList<IAbilityManager> GetAllAbilityManagers();

        /// <summary>
        /// Event fired when any ability is activated.
        /// </summary>
        event Action<IAbilityManager, int> OnAbilityActivated;

        /// <summary>
        /// Event fired when any ability cooldown completes.
        /// </summary>
        event Action<IAbilityManager, int> OnAbilityCooldownComplete;

        /// <summary>
        /// Event fired when any ability state changes.
        /// </summary>
        event Action<IAbilityManager, int, bool, float> OnAbilityStateChanged;
    }
}
