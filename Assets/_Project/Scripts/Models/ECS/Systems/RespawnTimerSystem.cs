// FIXME: tidyup after 8/29
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class RespawnTimerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        float deltaTime = Time.DeltaTime;

        Entities
            .WithAll<DeadTag>()
            .ForEach((Entity entity, NetworkLifeState netLife, ref RespawnTimer timer, ref HealthComponent health) =>
            {
                timer.TimeRemaining -= deltaTime;
                netLife.RespawnTimeRemaining.Value = timer.TimeRemaining; // sync to clients

                if (timer.TimeRemaining <= 0)
                {
                    // Restore state
                    health.CurrentHealth = health.MaxHealth;
                    netLife.CurrentState.Value = LifeState.Alive;
                    netLife.RespawnTimeRemaining.Value = 0f;

                    // Remove ECS tags
                    EntityManager.RemoveComponent<DeadTag>(entity);
                    EntityManager.RemoveComponent<DeathTime>(entity);
                    EntityManager.RemoveComponent<RespawnTimer>(entity);
                    EntityManager.RemoveComponent<DeathAnimationTrigger>(entity);

                    // Example respawn position
                    var netObj = netLife.GetComponent<NetworkObject>();
                    netObj.transform.position = Vector3.zero; // TODO: set real spawn point
                }
            }).WithoutBurst().Run();
    }
}
