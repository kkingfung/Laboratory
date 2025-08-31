using UnityEngine;

namespace Laboratory.Core.Abilities.Events
{
    /// <summary>
    /// Event triggered when an ability is activated.
    /// </summary>
    public record AbilityActivatedEvent(int AbilityIndex, GameObject Source = null)
    {
        /// <summary>
        /// Resets the event for object pooling.
        /// </summary>
        public void Reset()
        {
            // Events are immutable records, so we can't reset them
            // This is here for potential future pooling implementations
        }
    }

    /// <summary>
    /// Event triggered when an ability's state changes (e.g., cooldown).
    /// </summary>
    public record AbilityStateChangedEvent(
        int AbilityIndex, 
        bool IsOnCooldown, 
        float CooldownRemaining, 
        GameObject Source = null);

    /// <summary>
    /// Event triggered when an ability cooldown completes.
    /// </summary>
    public record AbilityCooldownCompleteEvent(int AbilityIndex, GameObject Source = null);

    /// <summary>
    /// Event triggered when an ability begins casting.
    /// </summary>
    public record AbilityCastStartEvent(int AbilityIndex, float CastTime, GameObject Source = null);

    /// <summary>
    /// Event triggered when an ability cast completes.
    /// </summary>
    public record AbilityCastCompleteEvent(int AbilityIndex, GameObject Source = null);

    /// <summary>
    /// Event triggered when an ability cast is interrupted.
    /// </summary>
    public record AbilityCastInterruptedEvent(int AbilityIndex, string Reason, GameObject Source = null);
}
