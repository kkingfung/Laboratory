using UnityEngine;

namespace Laboratory.Core.Infrastructure
{
    /// <summary>
    /// Interface for publishing health-related events without circular dependencies
    /// This allows health components to publish events without directly depending on event system assemblies
    /// </summary>
    public interface IHealthEventPublisher
    {
        /// <summary>
        /// Publish a damage event
        /// </summary>
        void PublishDamageEvent(GameObject target, object source, float damage, int damageType, Vector3 direction);

        /// <summary>
        /// Publish a death event
        /// </summary>
        void PublishDeathEvent(GameObject target, object source);
    }
}