using Unity.Entities;

namespace Laboratory.Models.ECS.Components
{
    /// <summary>
    /// Temporary stub for DamageEventBus.
    /// TODO: Move this to the appropriate assembly or implement properly.
    /// </summary>
    public static class DamageEventBus
    {
        /// <summary>
        /// Creates a damage event bus entity.
        /// TODO: Implement proper damage event bus functionality.
        /// </summary>
        /// <param name="entityManager">Entity manager to create the entity</param>
        /// <returns>Entity representing the damage event bus</returns>
        public static Entity Create(EntityManager entityManager)
        {
            var entity = entityManager.CreateEntity();
            entityManager.SetName(entity, "DamageEventBus");
            
            // TODO: Add proper components for damage event handling
            // entityManager.AddBuffer<DamageTakenEventBufferElement>(entity);
            
            return entity;
        }
    }
}
