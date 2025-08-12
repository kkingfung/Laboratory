using Unity.Entities;
using Unity.Netcode;
using UnityEngine;

[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class DeathSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!NetworkManager.Singleton.IsServer)
            return; // Only server decides death state

        float currentTime = (float)Time.ElapsedTime;

        Entities
            .WithNone<DeadTag>()
            .ForEach((Entity entity, NetworkLifeState netLife, ref HealthComponent health) =>
            {
                if (health.CurrentHealth <= 0 && netLife.IsAlive)
                {
                    // Mark network state
                    netLife.CurrentState.Value = LifeState.Dead;
                    netLife.RespawnTimeRemaining.Value = 5f; // e.g., 5s respawn delay

                    // Local ECS tagging
                    EntityManager.AddComponent<DeadTag>(entity);
                    EntityManager.AddComponentData(entity, new DeathTime { TimeOfDeath = currentTime });
                    EntityManager.AddComponentData(entity, new RespawnTimer { TimeRemaining = 5f });
                    EntityManager.AddComponentData(entity, new DeathAnimationTrigger { Triggered = false });

                    // Optional: disable input here on server
                }
            }).WithoutBurst().Run();
    }
}
