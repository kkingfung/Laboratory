using Unity.Entities;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Component data for storing ragdoll sound configuration
    /// </summary>
    public struct RagdollSoundConfig : IComponentData
    {
        public float Volume;
        public Entity HitClipsEntity;
        public Entity CollisionClipsEntity; 
        public Entity LandingClipsEntity;
        public Entity AudioSourceEntity;
    }

    /// <summary>
    /// ECS/DOTS system for managing ragdoll-related sound effects.
    /// Handles audio playback for hit, collision, and landing events in the ECS architecture.
    /// </summary>
    /// <remarks>
    /// This system processes various ragdoll events and triggers appropriate sound effects.
    /// It uses WithoutBurst() to allow UnityEngine API calls for audio playback.
    /// Uses EntityCommandBuffer to handle structural changes safely.
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial struct RagdollSoundManagerByDots : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            // System initialization
            state.RequireForUpdate<RagdollSoundConfig>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            var config = SystemAPI.GetSingleton<RagdollSoundConfig>();

            ProcessHitEvents(ref state, ecb, config);
            ProcessCollisionEvents(ref state, ecb, config);
            ProcessLandingEvents(ref state, ecb, config);
        }

        public void OnDestroy(ref SystemState state)
        {
            // Cleanup if needed
        }

        /// <summary>
        /// Processes hit events and plays corresponding audio clips.
        /// </summary>
        private void ProcessHitEvents(ref SystemState state, EntityCommandBuffer ecb, RagdollSoundConfig config)
        {
            foreach (var (hitEvent, entity) in SystemAPI.Query<RefRW<HitEvent>>().WithEntityAccess())
            {
                // Play hit sound effect
                // Note: In a real implementation, you'd need to access audio clips through 
                // a managed component or singleton MonoBehaviour
                PlayRandomHitSound(config);
                
                // Remove the event component
                ecb.RemoveComponent<HitEvent>(entity);
            }
        }

        /// <summary>
        /// Processes collision events and plays corresponding audio clips.
        /// </summary>
        private void ProcessCollisionEvents(ref SystemState state, EntityCommandBuffer ecb, RagdollSoundConfig config)
        {
            foreach (var (collisionEvent, entity) in SystemAPI.Query<RefRW<CollisionEvent>>().WithEntityAccess())
            {
                // Play collision sound effect
                PlayRandomCollisionSound(config);
                
                // Remove the event component
                ecb.RemoveComponent<CollisionEvent>(entity);
            }
        }

        /// <summary>
        /// Processes landing events and plays corresponding audio clips.
        /// </summary>
        private void ProcessLandingEvents(ref SystemState state, EntityCommandBuffer ecb, RagdollSoundConfig config)
        {
            foreach (var (landingEvent, entity) in SystemAPI.Query<RefRW<LandingEvent>>().WithEntityAccess())
            {
                // Play landing sound effect
                PlayRandomLandingSound(config);
                
                // Remove the event component
                ecb.RemoveComponent<LandingEvent>(entity);
            }
        }

        /// <summary>
        /// Plays a random hit sound effect.
        /// </summary>
        private void PlayRandomHitSound(RagdollSoundConfig config)
        {
            RagdollSoundManager.Instance?.PlayHitSound();
        }

        /// <summary>
        /// Plays a random collision sound effect.
        /// </summary>
        private void PlayRandomCollisionSound(RagdollSoundConfig config)
        {
            RagdollSoundManager.Instance?.PlayCollisionSound();
        }

        /// <summary>
        /// Plays a random landing sound effect.
        /// </summary>
        private void PlayRandomLandingSound(RagdollSoundConfig config)
        {
            RagdollSoundManager.Instance?.PlayLandingSound();
        }
    }
}
