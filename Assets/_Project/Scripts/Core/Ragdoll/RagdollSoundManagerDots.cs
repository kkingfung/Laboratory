using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS/DOTS system for managing ragdoll-related sound effects.
    /// Handles audio playback for hit, collision, and landing events in the ECS architecture.
    /// </summary>
    /// <remarks>
    /// This system processes various ragdoll events and triggers appropriate sound effects.
    /// It uses WithoutBurst() to allow UnityEngine API calls for audio playback.
    /// </remarks>
    public class RagdollSoundManagerDots : SystemBase
    {
        #region Fields

        [Header("Audio Clips")]
        [Tooltip("Audio clips to play when a ragdoll is hit")]
        [SerializeField] private List<AudioClip> hitClips;
        
        [Tooltip("Audio clips to play when a ragdoll collides with objects")]
        [SerializeField] private List<AudioClip> collisionClips;
        
        [Tooltip("Audio clips to play when a ragdoll lands")]
        [SerializeField] private List<AudioClip> landingClips;

        [Header("Audio Settings")]
        [Tooltip("AudioSource component for playing sound effects")]
        [SerializeField] private AudioSource audioSource;
        
        [Tooltip("Volume level for ragdoll sound effects")]
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called every frame to process ragdoll sound events.
        /// Handles hit, collision, and landing events by playing appropriate audio clips.
        /// </summary>
        protected override void OnUpdate()
        {
            ProcessHitEvents();
            ProcessCollisionEvents();
            ProcessLandingEvents();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes hit events and plays corresponding audio clips.
        /// </summary>
        private void ProcessHitEvents()
        {
            Entities.WithAll<EcsHitReactionSystem.HitEvent>().ForEach(
                (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.HitEvent hitEvent) =>
                {
                    PlayRandomClip(hitClips);
                    EntityManager.RemoveComponent<EcsHitReactionSystem.HitEvent>(entity);
                }).WithoutBurst().Run(); // WithoutBurst to allow UnityEngine calls
        }

        /// <summary>
        /// Processes collision events and plays corresponding audio clips.
        /// </summary>
        private void ProcessCollisionEvents()
        {
            Entities.WithAll<EcsHitReactionSystem.CollisionEvent>().ForEach(
                (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.CollisionEvent collisionEvent) =>
                {
                    PlayRandomClip(collisionClips);
                    EntityManager.RemoveComponent<EcsHitReactionSystem.CollisionEvent>(entity);
                }).WithoutBurst().Run();
        }

        /// <summary>
        /// Processes landing events and plays corresponding audio clips.
        /// </summary>
        private void ProcessLandingEvents()
        {
            Entities.WithAll<EcsHitReactionSystem.LandingEvent>().ForEach(
                (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.LandingEvent landingEvent) =>
                {
                    PlayRandomClip(landingClips);
                    EntityManager.RemoveComponent<EcsHitReactionSystem.LandingEvent>(entity);
                }).WithoutBurst().Run();
        }

        /// <summary>
        /// Plays a random audio clip from the provided list.
        /// </summary>
        /// <param name="clips">List of audio clips to choose from</param>
        private void PlayRandomClip(List<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0 || audioSource == null) 
                return;

            int index = UnityEngine.Random.Range(0, clips.Count);
            audioSource.PlayOneShot(clips[index], volume);
        }

        #endregion
    }
}
