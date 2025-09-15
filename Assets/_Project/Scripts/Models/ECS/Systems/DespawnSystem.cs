using Unity.Entities;
using Unity.Collections;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Handles entity despawning and cleanup for dead entities.
    /// Safely destroys entities marked as dead using Entity Command Buffer.
    /// Can be extended to add delays, animations, or pooling logic.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
    public partial class DespawnSystem : SystemBase
    {
        #region Unity Override Methods

        /// <summary>
        /// Processes all dead entities and destroys them safely.
        /// Uses EntityCommandBuffer for safe entity destruction during iteration.
        /// </summary>
        protected override void OnUpdate()
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(World.Unmanaged);

            // Query for entities with PlayerStateComponent and check if they're dead
            Entities
                .WithAll<PlayerStateComponent>()
                .ForEach((Entity entity, in PlayerStateComponent state) =>
                {
                    // Check if player is dead and schedule for destruction
                    if (!state.IsAlive)
                    {
                        ecb.DestroyEntity(entity);
                    }
                }).Schedule();
        }

        #endregion


    }
}
