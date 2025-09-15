using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS system responsible for processing hitbox interactions and triggering ragdoll responses.
    /// Demonstrates random hit generation for testing purposes - replace with actual hit detection logic.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class HitboxSystem : SystemBase
    {
        #region Fields
        
        /// <summary>
        /// Probability threshold for generating test hits (0.0 to 1.0).
        /// </summary>
        private const float TestHitProbability = 0.01f;
        
        /// <summary>
        /// Default test force vector applied during random hit generation.
        /// </summary>
        private static readonly float3 DefaultTestForce = new float3(0f, 5f, -3f);
        
        /// <summary>
        /// Default delay before starting blend-back animation.
        /// </summary>
        private const float DefaultBlendDelay = 0.3f;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Processes all entities with PartialRagdollTag and randomly generates test hits.
        /// This is demonstration code - replace with actual hit detection logic.
        /// </summary>
        protected override void OnUpdate()
        {
            ProcessTestHitGeneration();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Generates random test hits for entities with PartialRagdollTag.
        /// This method should be replaced with actual hit detection logic in production.
        /// </summary>
        private void ProcessTestHitGeneration()
        {
            Entities.WithAll<PartialRagdollTag>().ForEach((Entity entity, ref PhysicsVelocity velocity) =>
            {
                if (ShouldGenerateTestHit())
                {
                    CreateHitEvent(entity);
                }
            }).WithoutBurst().Run();
        }
        
        /// <summary>
        /// Determines if a test hit should be generated based on probability.
        /// </summary>
        /// <returns>True if a test hit should be generated</returns>
        private static bool ShouldGenerateTestHit()
        {
            return UnityEngine.Random.value < TestHitProbability;
        }
        
        /// <summary>
        /// Creates and adds a HitEvent component to the specified entity.
        /// </summary>
        /// <param name="entity">Target entity to receive the hit event</param>
        private void CreateHitEvent(Entity entity)
        {
            var hitEvent = new HitEvent
            {
                BoneEntity = entity,
                Force = DefaultTestForce,
                DelayBeforeBlend = DefaultBlendDelay
            };
            
            EntityManager.AddComponentData(entity, hitEvent);
        }
        
        #endregion
    }
}
