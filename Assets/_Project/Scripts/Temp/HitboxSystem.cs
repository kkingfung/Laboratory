using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;

namespace RagdollECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class HitboxSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            // Example: for demonstration purposes only
            Entities.WithAll<PartialRagdollTag>().ForEach((Entity entity, ref PhysicsVelocity velocity) =>
            {
                // Randomly trigger a test hit
                if (UnityEngine.Random.value < 0.01f)
                {
                    EntityManager.AddComponentData(entity, new HitEvent
                    {
                        BoneEntity = entity,
                        Force = new float3(0f, 5f, -3f),
                        DelayBeforeBlend = 0.3f
                    });
                }

            }).WithoutBurst().Run();
        }
    }
}
