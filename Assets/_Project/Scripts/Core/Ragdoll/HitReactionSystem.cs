using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS system responsible for processing hit events and applying ragdoll reactions.
    /// Handles hit event processing, force application, and impact effect triggering.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class HitReactionSystem : SystemBase
    {
        #region Unity Override Methods
        
        /// <summary>
        /// Processes all entities with HitEvent components and applies appropriate ragdoll reactions.
        /// Creates partial ragdoll effects, applies forces, and triggers impact events.
        /// </summary>
        protected override void OnUpdate()
        {
            ProcessHitEvents();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Iterates through all hit events and processes them individually.
        /// </summary>
        private void ProcessHitEvents()
        {
            Entities.WithAll<HitEvent>().ForEach((Entity entity, int entityInQueryIndex, in HitEvent hitEvent) =>
            {
                ProcessSingleHitEvent(hitEvent);
                AddImpactEventComponent(hitEvent);
                RemoveProcessedHitEvent(entity);
            }).WithoutBurst().Run();
        }
        
        /// <summary>
        /// Processes a single hit event by applying partial ragdoll physics.
        /// </summary>
        /// <param name="hitEvent">Hit event data containing bone entity and force information</param>
        private void ProcessSingleHitEvent(HitEvent hitEvent)
        {
            var boneArray = CreateBoneEntityArray(hitEvent.BoneEntity);
            ApplyPartialRagdollToEntity(boneArray, hitEvent);
            boneArray.Dispose();
        }
        
        /// <summary>
        /// Creates a temporary native array containing the target bone entity.
        /// </summary>
        /// <param name="boneEntity">Entity representing the bone that was hit</param>
        /// <returns>NativeArray containing the single bone entity</returns>
        private NativeArray<Entity> CreateBoneEntityArray(Entity boneEntity)
        {
            return new NativeArray<Entity>(1, Allocator.Temp) { [0] = boneEntity };
        }
        
        /// <summary>
        /// Applies partial ragdoll physics to the specified bone entities.
        /// </summary>
        /// <param name="boneArray">Array of bone entities to affect</param>
        /// <param name="hitEvent">Hit event containing force and timing data</param>
        private void ApplyPartialRagdollToEntity(NativeArray<Entity> boneArray, HitEvent hitEvent)
        {
            PartialRagdollControllerDots.ApplyPartialRagdoll(
                EntityManager,
                boneArray,
                hitEvent.Force,
                hitEvent.DelayBeforeBlend
            );
        }
        
        /// <summary>
        /// Adds impact event component to the hit bone for visual and audio effects.
        /// </summary>
        /// <param name="hitEvent">Hit event data to propagate to impact systems</param>
        private void AddImpactEventComponent(HitEvent hitEvent)
        {
            // Add the hit event to the bone entity for sound processing
            EntityManager.AddComponentData(hitEvent.BoneEntity, new HitEvent
            {
                BoneEntity = hitEvent.BoneEntity,
                Force = hitEvent.Force,
                DelayBeforeBlend = hitEvent.DelayBeforeBlend,
                ImpactPoint = hitEvent.ImpactPoint,
                ImpactMagnitude = hitEvent.ImpactMagnitude
            });
        }
        
        /// <summary>
        /// Removes the processed hit event component to prevent repeated application.
        /// </summary>
        /// <param name="entity">Entity containing the processed hit event</param>
        private void RemoveProcessedHitEvent(Entity entity)
        {
            EntityManager.RemoveComponent<HitEvent>(entity);
        }
        
        #endregion
    }
}
