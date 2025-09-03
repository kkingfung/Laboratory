using UnityEngine;

namespace Laboratory.Core.Abilities.Events
{
    /// <summary>
    /// Event triggered when an ability is activated.
    /// </summary>
    public class AbilityActivatedEvent
    {
        public int AbilityIndex { get; }
        public GameObject Source { get; }

        public AbilityActivatedEvent(int abilityIndex, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            Source = source;
        }

        /// <summary>
        /// Resets the event for object pooling.
        /// </summary>
        public void Reset()
        {
            // This method is kept for potential future pooling implementations
            // Since we're using immutable properties, actual reset would require object recreation
        }
    }

    /// <summary>
    /// Event triggered when an ability's state changes (e.g., cooldown).
    /// </summary>
    public class AbilityStateChangedEvent
    {
        public int AbilityIndex { get; }
        public bool IsOnCooldown { get; }
        public float CooldownRemaining { get; }
        public GameObject Source { get; }

        public AbilityStateChangedEvent(int abilityIndex, bool isOnCooldown, float cooldownRemaining, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            IsOnCooldown = isOnCooldown;
            CooldownRemaining = cooldownRemaining;
            Source = source;
        }
    }

    /// <summary>
    /// Event triggered when an ability cooldown completes.
    /// </summary>
    public class AbilityCooldownCompleteEvent
    {
        public int AbilityIndex { get; }
        public GameObject Source { get; }

        public AbilityCooldownCompleteEvent(int abilityIndex, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            Source = source;
        }
    }

    /// <summary>
    /// Event triggered when an ability begins casting.
    /// </summary>
    public class AbilityCastStartEvent
    {
        public int AbilityIndex { get; }
        public float CastTime { get; }
        public GameObject Source { get; }

        public AbilityCastStartEvent(int abilityIndex, float castTime, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            CastTime = castTime;
            Source = source;
        }
    }

    /// <summary>
    /// Event triggered when an ability cast completes.
    /// </summary>
    public class AbilityCastCompleteEvent
    {
        public int AbilityIndex { get; }
        public GameObject Source { get; }

        public AbilityCastCompleteEvent(int abilityIndex, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            Source = source;
        }
    }

    /// <summary>
    /// Event triggered when an ability cast is interrupted.
    /// </summary>
    public class AbilityCastInterruptedEvent
    {
        public int AbilityIndex { get; }
        public string Reason { get; }
        public GameObject Source { get; }

        public AbilityCastInterruptedEvent(int abilityIndex, string reason, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            Reason = reason;
            Source = source;
        }
    }
}
