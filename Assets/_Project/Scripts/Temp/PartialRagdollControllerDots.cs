using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// ECS/DOTS version of partial ragdoll controller.
/// Apply force to selected bones/entities while leaving others animated.
/// </summary>
public static class PartialRagdollControllerDots
{
    /// <summary>
    /// Apply ragdoll force to selected bone entities and schedule blend-back.
    /// </summary>
    /// <param name="entityManager">EntityManager reference</param>
    /// <param name="boneEntities">Bones to ragdoll</param>
    /// <param name="force">World-space force to apply</param>
    /// <param name="blendDuration">Time in seconds to blend back</param>
    public static void ApplyPartialRagdoll(EntityManager entityManager, NativeArray<Entity> boneEntities, float3 force, float blendDuration = 0.3f)
    {
        foreach (var boneEntity in boneEntities)
        {
            // Enable physics by ensuring PhysicsVelocity exists
            if (!entityManager.HasComponent<PhysicsVelocity>(boneEntity)) continue;

            var velocity = entityManager.GetComponentData<PhysicsVelocity>(boneEntity);
            velocity.Linear += force;
            entityManager.SetComponentData(boneEntity, velocity);

            // Add BlendBackTag and BlendData for smooth interpolation
            if (!entityManager.HasComponent<EcsHitReactionSystem.BlendBackTag>(boneEntity))
                entityManager.AddComponent<EcsHitReactionSystem.BlendBackTag>(boneEntity);

            if (!entityManager.HasComponent<EcsBlendBackSystem.BlendData>(boneEntity))
            {
                var transform = entityManager.GetComponentData<LocalTransform>(boneEntity);
                entityManager.AddComponentData(boneEntity, new EcsBlendBackSystem.BlendData
                {
                    StartPosition = transform.Position,
                    StartRotation = transform.Rotation,
                    Timer = 0f,
                    Duration = blendDuration
                });
            }

            // Optional: Network sync (trigger RPC for NetworkRagdollSyncDots)
            // NetworkRagdollSyncDots.TriggerBoneHit(boneEntity, force);
        }
    }
}
