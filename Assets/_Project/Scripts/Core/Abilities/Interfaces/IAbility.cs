using System;

namespace Laboratory.Core.Abilities.Interfaces
{
    /// <summary>
    /// Core ability interface that defines the basic structure and functionality of an ability.
    /// All abilities in the system should implement this interface.
    /// </summary>
    public interface IAbility
    {
        /// <summary>
        /// Unique identifier for this ability.
        /// </summary>
        string AbilityId { get; }
        
        /// <summary>
        /// Display name of the ability.
        /// </summary>
        string DisplayName { get; }
        
        /// <summary>
        /// Description of what the ability does.
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Cooldown time in seconds before this ability can be used again.
        /// </summary>
        float CooldownTime { get; }
        
        /// <summary>
        /// Cast time in seconds (0 for instant abilities).
        /// </summary>
        float CastTime { get; }
        
        /// <summary>
        /// Resource cost to use this ability (mana, energy, etc.).
        /// </summary>
        int ResourceCost { get; }
        
        /// <summary>
        /// Range of the ability in world units. 0 for self-targeted abilities.
        /// </summary>
        float Range { get; }
        
        /// <summary>
        /// Whether this ability is currently usable.
        /// </summary>
        bool IsUsable { get; }
        
        /// <summary>
        /// Whether this ability is currently on cooldown.
        /// </summary>
        bool IsOnCooldown { get; }
        
        /// <summary>
        /// Remaining cooldown time in seconds.
        /// </summary>
        float CooldownRemaining { get; }
        
        /// <summary>
        /// Attempts to activate this ability.
        /// </summary>
        /// <returns>True if the ability was successfully activated.</returns>
        bool TryActivate();
        
        /// <summary>
        /// Forces the ability to activate regardless of cooldown or resource costs.
        /// Should only be used for debugging or special scenarios.
        /// </summary>
        void ForceActivate();
        
        /// <summary>
        /// Resets the ability's cooldown to zero.
        /// </summary>
        void ResetCooldown();
        
        /// <summary>
        /// Updates the ability's state. Called each frame by the ability manager.
        /// </summary>
        /// <param name="deltaTime">Time since last update.</param>
        void UpdateAbility(float deltaTime);
    }
    
    /// <summary>
    /// Interface for abilities that can be cast with a cast time.
    /// </summary>
    public interface ICastableAbility : IAbility
    {
        /// <summary>
        /// Whether the ability is currently being cast.
        /// </summary>
        bool IsCasting { get; }
        
        /// <summary>
        /// Remaining cast time in seconds.
        /// </summary>
        float CastTimeRemaining { get; }
        
        /// <summary>
        /// Whether the cast can be interrupted.
        /// </summary>
        bool CanInterruptCast { get; }
        
        /// <summary>
        /// Starts casting the ability.
        /// </summary>
        /// <returns>True if casting started successfully.</returns>
        bool StartCast();
        
        /// <summary>
        /// Interrupts the current cast.
        /// </summary>
        /// <param name="reason">Reason for the interruption.</param>
        void InterruptCast(string reason = "");
        
        /// <summary>
        /// Event triggered when casting starts.
        /// </summary>
        event Action<ICastableAbility> OnCastStart;
        
        /// <summary>
        /// Event triggered when casting completes.
        /// </summary>
        event Action<ICastableAbility> OnCastComplete;
        
        /// <summary>
        /// Event triggered when casting is interrupted.
        /// </summary>
        event Action<ICastableAbility, string> OnCastInterrupted;
    }
    
    /// <summary>
    /// Interface for abilities that target specific locations or entities.
    /// </summary>
    public interface ITargetedAbility : IAbility
    {
        /// <summary>
        /// Type of targeting this ability uses.
        /// </summary>
        AbilityTargetType TargetType { get; }
        
        /// <summary>
        /// Whether this ability requires a valid target to activate.
        /// </summary>
        bool RequiresTarget { get; }
        
        /// <summary>
        /// Sets the target for this ability.
        /// </summary>
        /// <param name="target">Target object or position.</param>
        /// <returns>True if the target is valid for this ability.</returns>
        bool SetTarget(object target);
        
        /// <summary>
        /// Clears the current target.
        /// </summary>
        void ClearTarget();
        
        /// <summary>
        /// Checks if the given target is valid for this ability.
        /// </summary>
        /// <param name="target">Target to validate.</param>
        /// <returns>True if the target is valid.</returns>
        bool IsValidTarget(object target);
    }
    
    /// <summary>
    /// Types of targeting that abilities can use.
    /// </summary>
    public enum AbilityTargetType
    {
        None,           // No targeting required
        Self,           // Targets the caster
        SingleTarget,   // Targets a single entity
        AreaOfEffect,   // Targets an area
        Directional,    // Targets in a direction
        GroundTarget    // Targets a position on the ground
    }
}
