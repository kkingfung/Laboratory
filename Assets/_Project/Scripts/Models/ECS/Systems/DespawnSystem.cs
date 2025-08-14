using Unity.Entities;
using Unity.Collections;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Handles entity despawning and cleanup for dead entities.
    /// Safely destroys entities marked as dead using Entity Command Buffer.
    /// Can be extended to add delays, animations, or pooling logic.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DespawnSystem : SystemBase
    {
        #region Unity Override Methods

        /// <summary>
        /// Processes all dead entities and destroys them safely.
        /// Uses EntityCommandBuffer for safe entity destruction during iteration.
        /// </summary>
        protected override void OnUpdate()
        {
            using var ecb = new EntityCommandBuffer(Allocator.Temp);

            Entities
                .WithAll<PlayerStateComponent>()
                .ForEach((Entity entity, in PlayerStateComponent state) =>
                {
                    ProcessEntityDespawn(entity, in state, ecb);
                }).Run();

            ExecuteDespawnCommands(ecb);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes a single entity for potential despawning.
        /// </summary>
        /// <param name="entity">The entity to check for despawn</param>
        /// <param name="state">The player state component</param>
        /// <param name="ecb">Entity command buffer for safe operations</param>
        private void ProcessEntityDespawn(Entity entity, in PlayerStateComponent state, EntityCommandBuffer ecb)
        {
            if (!state.IsAlive)
            {
                ScheduleEntityDestruction(entity, ecb);
            }
        }

        /// <summary>
        /// Schedules an entity for destruction in the command buffer.
        /// </summary>
        /// <param name="entity">The entity to destroy</param>
        /// <param name="ecb">Entity command buffer to add the destruction command to</param>
        private void ScheduleEntityDestruction(Entity entity, EntityCommandBuffer ecb)
        {
            ecb.DestroyEntity(entity);
        }

        /// <summary>
        /// Executes all queued despawn commands safely.
        /// </summary>
        /// <param name="ecb">Entity command buffer containing destruction commands</param>
        private void ExecuteDespawnCommands(EntityCommandBuffer ecb)
        {
            ecb.Playback(EntityManager);
        }

        #endregion
    }
}
