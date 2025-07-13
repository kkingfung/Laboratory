// 2025/7/13 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct Effect : IComponentData
{
    public float Duration; // Duration of the effect
    public float ElapsedTime; // Time elapsed since the effect started
}

public struct EffectSpawnRequest : IComponentData
{
    public Entity EffectPrefab; // Prefab to spawn for the effect
    public float3 Position; // Position to spawn the effect
}

public class EffectManagerSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = Time.DeltaTime;

        // Handle active effects
        Entities.ForEach((Entity entity, ref Effect effect) =>
        {
            // Update elapsed time
            effect.ElapsedTime += deltaTime;

            // Check if the effect duration is over
            if (effect.ElapsedTime >= effect.Duration)
            {
                // Destroy the effect entity
                EntityManager.DestroyEntity(entity);
            }
        }).ScheduleParallel();

        // Handle effect spawn requests
        Entities.WithStructuralChanges().ForEach((Entity entity, in EffectSpawnRequest spawnRequest) =>
        {
            // Instantiate the effect prefab
            if (spawnRequest.EffectPrefab != Entity.Null)
            {
                var effectEntity = EntityManager.Instantiate(spawnRequest.EffectPrefab);

                // Set the position of the effect
                EntityManager.SetComponentData(effectEntity, new LocalTransform { Position = spawnRequest.Position });

                // Optionally, add an Effect component to track its duration
                if (!EntityManager.HasComponent<Effect>(effectEntity))
                {
                    EntityManager.AddComponentData(effectEntity, new Effect { Duration = 5f, ElapsedTime = 0f }); // Example duration of 5 seconds
                }
            }

            // Remove the spawn request entity
            EntityManager.DestroyEntity(entity);
        }).Run();
    }
}