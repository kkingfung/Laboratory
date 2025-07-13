
// 2025/7/13 AI-Tag
// This was rewritten with the help of Assistant, a Unity Artificial Intelligence product.

using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;

public struct BreakableItem : IComponentData
{
    public int MaxHealth;
    public int CurrentHealth;
    public Entity BrokenVersionPrefab;
    public Entity BreakSoundEntity;
    public bool IsBroken;
}

public class BreakableItemSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var commandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        Entities
            .WithStructuralChanges()
            .ForEach((Entity entity, ref BreakableItem breakableItem, in LocalTransform localTransform) =>
            {
                if (breakableItem.IsBroken)
                    return;

                if (breakableItem.CurrentHealth <= 0)
                {
                    breakableItem.IsBroken = true;

                    // Play break sound
                    if (breakableItem.BreakSoundEntity != Entity.Null)
                    {
                        var soundEntity = commandBuffer.Instantiate(breakableItem.BreakSoundEntity);
                        commandBuffer.SetComponent(soundEntity, new Translation { Value = localTransform.Translation.Value });
                    }

                    // Instantiate broken version
                    if (breakableItem.BrokenVersionPrefab != Entity.Null)
                    {
                        var brokenEntity = commandBuffer.Instantiate(breakableItem.BrokenVersionPrefab);
                        commandBuffer.SetComponent(brokenEntity, new Translation { Value = localTransform.Translation.Value });
                        commandBuffer.SetComponent(brokenEntity, new Rotation { Value = localTransform.Rotation.Value });
                    }

                    // Destroy the current entity
                    commandBuffer.DestroyEntity(entity);
                }
            }).Run();

        commandBuffer.Playback(EntityManager);
        commandBuffer.Dispose();
    }
}
