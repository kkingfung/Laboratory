using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// ECS/DOTS system for playing ragdoll-related sounds.
/// Triggers sounds for hit, collision, and landing events.
/// </summary>
public class RagdollSoundManagerDots : SystemBase
{
    [Header("Audio Clips")]
    [SerializeField] private List<AudioClip> hitClips;
    [SerializeField] private List<AudioClip> collisionClips;
    [SerializeField] private List<AudioClip> landingClips;

    [Header("Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    protected override void OnUpdate()
    {
        // Handle HitEvents
        Entities.WithAll<EcsHitReactionSystem.HitEvent>().ForEach(
            (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.HitEvent hitEvent) =>
            {
                PlayRandomClip(hitClips);
                EntityManager.RemoveComponent<EcsHitReactionSystem.HitEvent>(entity);
            }).WithoutBurst().Run(); // WithoutBurst to allow UnityEngine calls

        // Handle CollisionEvents (optional)
        Entities.WithAll<EcsHitReactionSystem.CollisionEvent>().ForEach(
            (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.CollisionEvent collisionEvent) =>
            {
                PlayRandomClip(collisionClips);
                EntityManager.RemoveComponent<EcsHitReactionSystem.CollisionEvent>(entity);
            }).WithoutBurst().Run();

        // Handle LandingEvents (optional)
        Entities.WithAll<EcsHitReactionSystem.LandingEvent>().ForEach(
            (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.LandingEvent landingEvent) =>
            {
                PlayRandomClip(landingClips);
                EntityManager.RemoveComponent<EcsHitReactionSystem.LandingEvent>(entity);
            }).WithoutBurst().Run();
    }

    private void PlayRandomClip(List<AudioClip> clips)
    {
        if (clips == null || clips.Count == 0 || audioSource == null) return;
        int index = UnityEngine.Random.Range(0, clips.Count);
        audioSource.PlayOneShot(clips[index], volume);
    }
}
