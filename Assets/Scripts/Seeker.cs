// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;

public struct Seeker : IComponentData
{
    public float MeleeAttackRange;
    public int MeleeDamage;
    public Entity ProjectilePrefab;
    public float ProjectileSpeed;
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SeekerAttackSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

        foreach (var (seeker, localTransform) in SystemAPI.Query<Seeker, LocalTransform>())
        {
            if (UnityEngine.Input.GetButtonDown("Fire1")) // Melee Attack
            {
                var colliders = new NativeList<int>(Allocator.Temp);
                var sphere = new Unity.Physics.SphereGeometry
                {
                    Center = localTransform.Position,
                    Radius = seeker.MeleeAttackRange
                };

                var filter = CollisionFilter.Default;
                physicsWorld.OverlapSphere(sphere, ref colliders);
                foreach (var collider in colliders)
                {
                    var hiderEntity = collider.gameObject.GetComponent<GameObjectEntity>()?.Entity;
                    if (hiderEntity.HasValue && state.EntityManager.HasComponent<Hider>(hiderEntity.Value))
                    {
                        var hider = state.EntityManager.GetComponentData<Hider>(hiderEntity.Value);
                        hider.Health -= seeker.MeleeDamage;
                        state.EntityManager.SetComponentData(hiderEntity.Value, hider);
                    }
                }
            }

            if (UnityEngine.Input.GetButtonDown("Fire2")) // Ranged Attack
            {
                if (seeker.ProjectilePrefab != Entity.Null)
                {
                    var projectile = state.EntityManager.Instantiate(seeker.ProjectilePrefab);
                    var projectileTransform = new LocalTransform
                    {
                        Position = localTransform.Position,
                        Rotation = localTransform.Rotation,
                        Scale = 1.0f
                    };
                    state.EntityManager.SetComponentData(projectile, projectileTransform);

                    var projectileVelocity = new PhysicsVelocity
                    {
                        Linear = math.forward(localTransform.Rotation) * seeker.ProjectileSpeed
                    };
                    state.EntityManager.SetComponentData(projectile, projectileVelocity);
                }
            }
        }
    }
}
