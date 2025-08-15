using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// ECS/DOTS system for triggering impact effects on ragdoll hits.
/// </summary>
public class RagdollImpactEffectDots : SystemBase
{
    [Header("Impact Effects")]
    [SerializeField] private GameObject impactPrefab;

    [SerializeField] private AudioClip impactSound;
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private float impactScale = 1f;

    protected override void OnUpdate()
    {
        // Query all entities that have been hit (example: have HitEvent component)
        Entities.WithAll<EcsHitReactionSystem.HitEvent>().ForEach(
            (Entity hitEntity, int entityInQueryIndex, in EcsHitReactionSystem.HitEvent hitEvent) =>
            {
                // Get world position of hit bone
                float3 hitPosition = float3.zero;
                if (EntityManager.HasComponent<LocalTransform>(hitEvent.BoneEntity))
                {
                    hitPosition = EntityManager.GetComponentData<LocalTransform>(hitEvent.BoneEntity).Position;
                }

                // Spawn impact prefab
                if (impactPrefab != null)
                {
                    Quaternion rotation = Quaternion.identity;
                    if (math.lengthsq(hitEvent.Force) > 0f)
                        rotation = Quaternion.LookRotation(hitEvent.Force);

                    GameObject impactInstance = Object.Instantiate(impactPrefab, hitPosition, rotation);
                    impactInstance.transform.localScale *= impactScale;

                    // Destroy after particle lifetime
                    ParticleSystem ps = impactInstance.GetComponent<ParticleSystem>();
                    if (ps != null)
                        Object.Destroy(impactInstance, ps.main.duration + ps.main.startLifetime.constantMax);
                    else
                        Object.Destroy(impactInstance, 2f);
                }

                // Play sound
                if (impactSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(impactSound, soundVolume);
                }

                // Remove the HitEvent component so effect only triggers once
                EntityManager.RemoveComponent<EcsHitReactionSystem.HitEvent>(hitEntity);

            }).WithoutBurst().Run(); // WithoutBurst so we can call UnityEngine.Instantiate
    }
}
