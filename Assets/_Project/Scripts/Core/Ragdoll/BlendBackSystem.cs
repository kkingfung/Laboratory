using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS system responsible for smoothly blending ragdoll bones back to their animated positions.
    /// Processes entities with BlendBackTag and BlendData components to create smooth transitions.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BlendBackSystem : SystemBase
    {
        private EndSimulationEntityCommandBufferSystem _commandBufferSystem;
        #region Unity Override Methods
        
        protected override void OnCreate()
        {
            _commandBufferSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
        }
        
        /// <summary>
        /// Updates the blend back interpolation for all entities with blend data.
        /// Uses linear interpolation for positions and spherical interpolation for rotations.
        /// </summary>
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;
            var commandBuffer = _commandBufferSystem.CreateCommandBuffer().AsParallelWriter();

            Entities.WithAll<BlendBackTag, BlendData>().ForEach((Entity entity, int entityInQueryIndex, ref LocalTransform transform, ref BlendData blend) =>
            {
                // Update blend timer
                blend.Timer += deltaTime;
                float blendProgress = math.min(blend.Timer / blend.Duration, 1f);

                // Interpolate transform components
                transform.Position = math.lerp(blend.StartPosition, transform.Position, blendProgress);
                transform.Rotation = math.slerp(blend.StartRotation, transform.Rotation, blendProgress);

                // Remove blend component when complete
                if (blendProgress >= 1f)
                {
                    commandBuffer.RemoveComponent<BlendBackTag>(entityInQueryIndex, entity);
                }
            }).ScheduleParallel();
            
            _commandBufferSystem.AddJobHandleForProducer(Dependency);
        }
        
        #endregion
    }
}
