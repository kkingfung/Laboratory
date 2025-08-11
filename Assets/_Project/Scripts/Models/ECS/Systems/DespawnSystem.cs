using Unity.Entities;

namespace Models.ECS.Systems
{
    /// <summary>
    /// Despawns (destroys) entities marked as dead.
    /// Can be extended to add delays, animations, or pooling logic.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DespawnSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Create an ECB to safely destroy entities outside of iteration
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            Entities
                .WithAll<PlayerStateComponent>()
                .ForEach((Entity entity, in PlayerStateComponent state) =>
                {
                    if (!state.IsAlive)
                    {
                        // Destroy the entity immediately
                        ecb.DestroyEntity(entity);
                    }
                }).Run();

            // Execute all destroy commands
            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
