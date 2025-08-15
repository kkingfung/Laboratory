using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;

namespace RagdollECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class HitReactionSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.WithAll<HitEvent>().ForEach((Entity entity, int entityInQueryIndex, in HitEvent hitEvent) =>
            {
                var boneArray = new NativeArray<Entity>(1, Allocator.Temp) { [0] = hitEvent.BoneEntity };
                PartialRagdollControllerDots.ApplyPartialRagdoll(EntityManager, boneArray, hitEvent.Force, hitEvent.DelayBeforeBlend);

                // Add impact event component for particles/sound
                EntityManager.AddComponentData(hitEvent.BoneEntity, hitEvent);

                // Remove hit event to prevent repeated application
                EntityManager.RemoveComponent<HitEvent>(entity);

            }).WithoutBurst().Run();
        }
    }
}
