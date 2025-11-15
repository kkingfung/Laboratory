using UnityEngine;
using Laboratory.Core.Enums;

namespace Laboratory.Core.Events.Messages
{
    /// <summary>
    /// Event fired when damage is dealt to an entity
    /// </summary>
    public class DamageEvent
    {
        /// <summary>The target that received damage</summary>
        public GameObject Target { get; }

        /// <summary>The source that caused the damage</summary>
        public object Source { get; }

        /// <summary>Amount of damage dealt</summary>
        public float Amount { get; }

        /// <summary>Type of damage dealt</summary>
        public DamageType DamageType { get; }

        /// <summary>Direction the damage came from</summary>
        public Vector3 Direction { get; }

        public DamageEvent(GameObject target, object source, float amount, DamageType damageType, Vector3 direction)
        {
            Target = target;
            Source = source;
            Amount = amount;
            DamageType = damageType;
            Direction = direction;
        }
    }

    /// <summary>
    /// Event fired when an entity dies
    /// </summary>
    public class DeathEvent
    {
        /// <summary>The entity that died</summary>
        public GameObject Target { get; }

        /// <summary>What caused the death</summary>
        public object Source { get; }

        public DeathEvent(GameObject target, object source)
        {
            Target = target;
            Source = source;
        }
    }
}