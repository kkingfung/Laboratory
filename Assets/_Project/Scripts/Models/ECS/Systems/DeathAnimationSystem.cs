using Unity.Entities;
using UnityEngine;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Triggers death animations for entities marked as dead.
    /// Handles animation state changes and ensures animations are only triggered once per death.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class DeathAnimationSystem : SystemBase
    {
        #region Unity Override Methods

        /// <summary>
        /// Processes all dead entities and triggers their death animations if not already triggered.
        /// </summary>
        protected override void OnUpdate()
        {
            Entities
                .WithAll<DeadTag>()
                .ForEach((Entity entity, ref DeathAnimationTrigger deathAnimTrigger) =>
                {
                    ProcessDeathAnimation(entity, ref deathAnimTrigger);
                }).WithoutBurst().Run();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes death animation for a single entity.
        /// </summary>
        /// <param name="entity">The dead entity</param>
        /// <param name="deathAnimTrigger">The death animation trigger component</param>
        private void ProcessDeathAnimation(Entity entity, ref DeathAnimationTrigger deathAnimTrigger)
        {
            if (deathAnimTrigger.Triggered)
                return;

            TriggerDeathAnimation(entity);
            MarkAnimationAsTriggered(ref deathAnimTrigger);
        }

        /// <summary>
        /// Triggers the death animation on the entity's animator component.
        /// </summary>
        /// <param name="entity">The entity to animate</param>
        private void TriggerDeathAnimation(Entity entity)
        {
            // Try to get game object reference through various means
            GameObject gameObject = null;
            
            // Check if entity has a GameObject component or similar
            if (EntityManager.HasComponent<Transform>(entity))
            {
                var transform = EntityManager.GetComponentObject<Transform>(entity);
                gameObject = transform?.gameObject;
            }
            
            if (gameObject != null)
            {
                var animator = gameObject.GetComponent<Animator>();
                animator?.SetTrigger("Die");
            }
            else
            {
                Debug.LogWarning($"Could not find GameObject for entity {entity} to trigger death animation");
            }
        }

        /// <summary>
        /// Marks the death animation as triggered to prevent duplicate animations.
        /// </summary>
        /// <param name="deathAnimTrigger">The trigger component to update</param>
        private void MarkAnimationAsTriggered(ref DeathAnimationTrigger deathAnimTrigger)
        {
            deathAnimTrigger.Triggered = true;
        }

        #endregion
    }
}
