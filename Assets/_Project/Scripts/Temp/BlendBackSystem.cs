using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace RagdollECS
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class BlendBackSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float dt = Time.DeltaTime;

            Entities.WithAll<BlendBackTag, BlendData>().ForEach((Entity entity, ref LocalTransform transform, ref BlendData blend) =>
            {
                blend.Timer += dt;
                float t = math.min(blend.Timer / blend.Duration, 1f);

                transform.Position = math.lerp(blend.StartPosition, transform.Position, t);
                transform.Rotation = math.slerp(blend.StartRotation, transform.Rotation, t);

                if (t >= 1f)
                    EntityManager.RemoveComponent<BlendBackTag>(entity);

            }).WithoutBurst().Run();
        }
    }
}
