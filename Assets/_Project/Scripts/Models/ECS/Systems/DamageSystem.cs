using Unity.Entities;
using UnityEngine;
using Unity.Netcode;
using Unity.Transforms;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DamageSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Capture reference to GameObject lookup (assuming linked)
        var networkHealthLookup = GetComponentLookup<NetworkHealth>(true);

        Entities
            .WithAll<DamageRequest, HealthComponent>()
            .ForEach((Entity entity, int entityInQueryIndex, ref HealthComponent health, in DamageRequest damageRequest) =>
            {
                // Apply damage
                health.CurrentHealth -= damageRequest.Amount;
                if (health.CurrentHealth < 0) health.CurrentHealth = 0;

                // Sync health via NetworkHealth MonoBehaviour if exists
                if (entityManager.HasComponent<NetworkObject>(entity))
                {
                    var networkObject = entityManager.GetComponentObject<NetworkObject>(entity);
                    var go = networkObject.gameObject;
                    var networkHealth = go.GetComponent<NetworkHealth>();
                    if (networkHealth != null && networkHealth.IsServer)
                    {
                        networkHealth.ApplyDamage(damageRequest.Amount);
                    }
                }

                // TODO: Trigger damage taken events for UI, VFX, sounds here (e.g., via event system)

                // If dead, handle death logic here or via another system

                // Remove damage request component to mark processed
                EntityManager.RemoveComponent<DamageRequest>(entity);

            }).WithoutBurst().Run();
    }
}
