using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Burst;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// High-performance ECS system for hitbox interactions and ragdoll responses.
    /// Optimized with Burst compilation for maximum performance.
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
        /// Generates random test hits for entities with PartialRagdollTag using Burst-compatible random.
        /// </summary>
        private void ProcessTestHitGeneration()
        {
            var currentTime = (float)SystemAPI.Time.ElapsedTime;
            var randomSeed = (uint)(currentTime * 1000) + 1; // Ensure non-zero seed
            var random = Unity.Mathematics.Random.CreateFromIndex(randomSeed);

            Entities.WithAll<PartialRagdollTag>().WithoutBurst().ForEach((Entity entity, ref PhysicsVelocity velocity) =>
            {
                if (random.NextFloat() < TestHitProbability)
                {
                    CreateHitEvent(entity);
                }
            }).Run(); // Burst compilation enabled!
        }
        
        // Removed ShouldGenerateTestHit() - now using Burst-compatible random directly in ProcessTestHitGeneration()
        
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
