using UnityEngine;

namespace Laboratory.Gameplay.Abilities
{
    #region Event Classes

    /// <summary>
    /// Event triggered when an ability is activated.
    /// </summary>
    public class AbilityActivatedEvent
    {
        public int AbilityIndex { get; set; }
        public GameObject Source { get; set; }

        public AbilityActivatedEvent(int abilityIndex, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            Source = source;
        }
    }

    /// <summary>
    /// Event triggered when an ability's state changes (e.g., cooldown).
    /// </summary>
    public class AbilityStateChangedEvent
    {
        public int AbilityIndex { get; set; }
        public bool IsOnCooldown { get; set; }
        public float CooldownRemaining { get; set; }
        public GameObject Source { get; set; }

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
        public int AbilityIndex { get; set; }
        public GameObject Source { get; set; }

        public AbilityCooldownCompleteEvent(int abilityIndex, GameObject source = null)
        {
            AbilityIndex = abilityIndex;
            Source = source;
        }
    }

    #endregion
}
