using Unity.Entities;
using Unity.Collections;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// ECS-based damage event bus for handling damage events between entities.
    /// </summary>
    public static class DamageEventBus
    {
        /// <summary>
        /// Creates a damage event bus entity in the specified EntityManager.
        /// </summary>
        public static Entity Create(EntityManager entityManager)
        {
            var entity = entityManager.CreateEntity();
            entityManager.AddBuffer<DamageTakenEventBufferElement>(entity);
            return entity;
        }

        /// <summary>
        /// Publishes a damage event to the bus.
        /// </summary>
        public static void PublishDamageEvent(EntityManager entityManager, Entity busEntity, DamageTakenEvent damageEvent)
        {
            if (!entityManager.Exists(busEntity)) return;
            
            var buffer = entityManager.GetBuffer<DamageTakenEventBufferElement>(busEntity);
            buffer.Add(new DamageTakenEventBufferElement { Value = damageEvent });
        }
    }
}
