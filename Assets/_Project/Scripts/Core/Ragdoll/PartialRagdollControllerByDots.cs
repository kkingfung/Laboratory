using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Static utility class providing DOTS/ECS-compatible partial ragdoll functionality.
    /// Applies physics forces to selected bone entities while maintaining animation control over others.
    /// Integrates with ECS blend-back systems for smooth recovery transitions.
    /// </summary>
    public static class PartialRagdollControllerByDots
    {
        #region Public Methods
        
        /// <summary>
        /// Applies partial ragdoll physics to specified bone entities and schedules blend-back recovery.
        /// Enables physics simulation, applies forces, and configures smooth transition back to animation.
        /// </summary>
        /// <param name="entityManager">EntityManager instance for component operations</param>
        /// <param name="boneEntities">Collection of bone entities to affect with ragdoll physics</param>
        /// <param name="force">World-space force vector to apply to all specified bones</param>
        /// <param name="blendDuration">Duration in seconds for the blend-back transition to animation</param>
        public static void ApplyPartialRagdoll(EntityManager entityManager, NativeArray<Entity> boneEntities, 
                                             float3 force, float blendDuration = 0.3f)
        {
            foreach (Entity boneEntity in boneEntities)
            {
                ProcessSingleBoneEntity(entityManager, boneEntity, force, blendDuration);
            }
        }
        
        #endregion
        
        #region Private Methods - Entity Processing
        
        /// <summary>
        /// Processes ragdoll application for a single bone entity.
        /// </summary>
        /// <param name="entityManager">EntityManager for component access</param>
        /// <param name="boneEntity">Target bone entity</param>
        /// <param name="force">Force to apply</param>
        /// <param name="blendDuration">Blend-back duration</param>
        private static void ProcessSingleBoneEntity(EntityManager entityManager, Entity boneEntity, 
                                                   float3 force, float blendDuration)
        {
            if (!ValidateBoneEntity(entityManager, boneEntity)) return;
            
            ApplyPhysicsForce(entityManager, boneEntity, force);
            ConfigureBlendBackComponents(entityManager, boneEntity, blendDuration);
        }
        
        /// <summary>
        /// Validates that the bone entity has required physics components.
        /// </summary>
        /// <param name="entityManager">EntityManager for component checking</param>
        /// <param name="boneEntity">Entity to validate</param>
        /// <returns>True if entity has required components</returns>
        private static bool ValidateBoneEntity(EntityManager entityManager, Entity boneEntity)
        {
            return entityManager.HasComponent<PhysicsVelocity>(boneEntity);
        }
        
        /// <summary>
        /// Applies the specified force to the bone entity by modifying its physics velocity.
        /// </summary>
        /// <param name="entityManager">EntityManager for component access</param>
        /// <param name="boneEntity">Target entity to apply force to</param>
        /// <param name="force">Force vector to add to current velocity</param>
        private static void ApplyPhysicsForce(EntityManager entityManager, Entity boneEntity, float3 force)
        {
            var velocity = entityManager.GetComponentData<PhysicsVelocity>(boneEntity);
            velocity.Linear += force;
            entityManager.SetComponentData(boneEntity, velocity);
        }
        
        /// <summary>
        /// Configures blend-back components for smooth transition back to animation.
        /// </summary>
        /// <param name="entityManager">EntityManager for component operations</param>
        /// <param name="boneEntity">Target bone entity</param>
        /// <param name="blendDuration">Duration of the blend-back process</param>
        private static void ConfigureBlendBackComponents(EntityManager entityManager, Entity boneEntity, 
                                                        float blendDuration)
        {
            AddBlendBackTag(entityManager, boneEntity);
            AddBlendBackData(entityManager, boneEntity, blendDuration);
        }
        
        /// <summary>
        /// Adds the BlendBackTag component to mark the entity for blend processing.
        /// </summary>
        /// <param name="entityManager">EntityManager for component operations</param>
        /// <param name="boneEntity">Target entity</param>
        private static void AddBlendBackTag(EntityManager entityManager, Entity boneEntity)
        {
            if (!entityManager.HasComponent<BlendBackTag>(boneEntity))
            {
                entityManager.AddComponent<BlendBackTag>(boneEntity);
            }
        }
        
        /// <summary>
        /// Adds or updates the BlendData component with current transform and timing information.
        /// </summary>
        /// <param name="entityManager">EntityManager for component operations</param>
        /// <param name="boneEntity">Target entity</param>
        /// <param name="blendDuration">Duration for the blend process</param>
        private static void AddBlendBackData(EntityManager entityManager, Entity boneEntity, float blendDuration)
        {
            if (entityManager.HasComponent<BlendData>(boneEntity)) return;
            
            var currentTransform = GetCurrentEntityTransform(entityManager, boneEntity);
            var blendData = CreateBlendData(currentTransform, blendDuration);
            
            entityManager.AddComponentData(boneEntity, blendData);
        }
        
        /// <summary>
        /// Retrieves the current LocalTransform of the specified entity.
        /// </summary>
        /// <param name="entityManager">EntityManager for component access</param>
        /// <param name="boneEntity">Entity to get transform from</param>
        /// <returns>Current LocalTransform of the entity</returns>
        private static LocalTransform GetCurrentEntityTransform(EntityManager entityManager, Entity boneEntity)
        {
            return entityManager.GetComponentData<LocalTransform>(boneEntity);
        }
        
        /// <summary>
        /// Creates a BlendData component with the specified parameters.
        /// </summary>
        /// <param name="currentTransform">Current transform to start blending from</param>
        /// <param name="blendDuration">Total duration of the blend process</param>
        /// <returns>Configured BlendData component</returns>
        private static BlendData CreateBlendData(LocalTransform currentTransform, float blendDuration)
        {
            return new BlendData
            {
                StartPosition = currentTransform.Position,
                StartRotation = currentTransform.Rotation,
                Timer = 0f,
                Duration = blendDuration
            };
        }
        
        #endregion
    }
}
