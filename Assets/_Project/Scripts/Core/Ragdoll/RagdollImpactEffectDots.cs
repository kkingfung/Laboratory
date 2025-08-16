using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS/DOTS system for triggering impact effects on ragdoll hits.
    /// Processes hit events and spawns appropriate visual and audio effects.
    /// Operates efficiently on large numbers of ragdoll entities simultaneously.
    /// </summary>
    public class RagdollImpactEffectDots : SystemBase
    {
        #region Fields

        [Header("Visual Effects")]
        [SerializeField]
        [Tooltip("Prefab to instantiate on impact")]
        private GameObject impactPrefab;

        [SerializeField]
        [Tooltip("Scale multiplier for spawned impact effects")]
        private float impactScale = 1f;

        [SerializeField]
        [Tooltip("Default lifetime for effects without particle systems")]
        private float defaultEffectLifetime = 2f;

        [Header("Audio Effects")]
        [SerializeField]
        [Tooltip("Audio clip to play on impact")]
        private AudioClip impactSound;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume level for impact sounds")]
        private float soundVolume = 1f;

        [SerializeField]
        [Tooltip("AudioSource for playing impact sounds")]
        private AudioSource audioSource;

        [Header("Performance Settings")]
        [SerializeField]
        [Tooltip("Maximum number of effects to process per frame")]
        private int maxEffectsPerFrame = 10;

        [SerializeField]
        [Tooltip("Whether to use object pooling for effects (future implementation)")]
        private bool useObjectPooling = false;

        // Runtime collections
        private Queue<EffectData> pendingEffects = new Queue<EffectData>();

        #endregion

        #region Nested Types

        /// <summary>
        /// Data structure for queued effect processing
        /// </summary>
        private struct EffectData
        {
            public float3 Position;
            public float3 Normal;
            public float Force;
            public Entity SourceEntity;
        }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize system components and validate setup
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            ValidateConfiguration();
        }

        /// <summary>
        /// Process hit events and trigger appropriate effects each frame
        /// </summary>
        protected override void OnUpdate()
        {
            ProcessHitEvents();
            ProcessCollisionEvents();
            ProcessLandingEvents();
            ProcessQueuedEffects();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Update impact prefab at runtime
        /// </summary>
        /// <param name="newPrefab">New prefab to use for impacts</param>
        public void SetImpactPrefab(GameObject newPrefab)
        {
            impactPrefab = newPrefab;
        }

        /// <summary>
        /// Update impact sound at runtime
        /// </summary>
        /// <param name="newSound">New audio clip for impacts</param>
        public void SetImpactSound(AudioClip newSound)
        {
            impactSound = newSound;
        }

        /// <summary>
        /// Update sound volume at runtime
        /// </summary>
        /// <param name="newVolume">New volume level (0-1)</param>
        public void SetSoundVolume(float newVolume)
        {
            soundVolume = math.clamp(newVolume, 0f, 1f);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validate system configuration and log warnings for missing components
        /// </summary>
        private void ValidateConfiguration()
        {
            if (impactPrefab == null)
            {
                Debug.LogWarning("[RagdollImpactEffectDots] No impact prefab assigned. Visual effects will not spawn.");
            }

            if (impactSound != null && audioSource == null)
            {
                Debug.LogWarning("[RagdollImpactEffectDots] Impact sound assigned but no AudioSource found. Audio will not play.");
            }
        }

        /// <summary>
        /// Process all entities with hit events
        /// </summary>
        private void ProcessHitEvents()
        {
            Entities.WithAll<EcsHitReactionSystem.HitEvent>().ForEach(
                (Entity hitEntity, int entityInQueryIndex, in EcsHitReactionSystem.HitEvent hitEvent) =>
                {
                    float3 hitPosition = GetEntityPosition(hitEvent.BoneEntity);
                    float3 hitNormal = math.normalize(hitEvent.Force);
                    float forceAmount = math.length(hitEvent.Force);

                    QueueEffect(hitPosition, hitNormal, forceAmount, hitEntity);
                    
                    // Remove the hit event to prevent reprocessing
                    EntityManager.RemoveComponent<EcsHitReactionSystem.HitEvent>(hitEntity);

                }).WithoutBurst().Run(); // WithoutBurst required for UnityEngine calls
        }

        /// <summary>
        /// Process all entities with collision events
        /// </summary>
        private void ProcessCollisionEvents()
        {
            Entities.WithAll<EcsHitReactionSystem.CollisionEvent>().ForEach(
                (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.CollisionEvent collisionEvent) =>
                {
                    float3 position = GetEntityPosition(entity);
                    float3 normal = collisionEvent.ContactNormal;
                    float force = math.length(collisionEvent.ImpactVelocity);

                    QueueEffect(position, normal, force, entity);
                    
                    // Remove processed event
                    EntityManager.RemoveComponent<EcsHitReactionSystem.CollisionEvent>(entity);

                }).WithoutBurst().Run();
        }

        /// <summary>
        /// Process all entities with landing events
        /// </summary>
        private void ProcessLandingEvents()
        {
            Entities.WithAll<EcsHitReactionSystem.LandingEvent>().ForEach(
                (Entity entity, int entityInQueryIndex, in EcsHitReactionSystem.LandingEvent landingEvent) =>
                {
                    float3 position = GetEntityPosition(entity);
                    float3 normal = new float3(0, 1, 0); // Assume upward normal for landing
                    float force = landingEvent.ImpactForce;

                    QueueEffect(position, normal, force, entity);
                    
                    // Remove processed event
                    EntityManager.RemoveComponent<EcsHitReactionSystem.LandingEvent>(entity);

                }).WithoutBurst().Run();
        }

        /// <summary>
        /// Get world position of an entity
        /// </summary>
        /// <param name="entity">Entity to get position for</param>
        /// <returns>World position as float3</returns>
        private float3 GetEntityPosition(Entity entity)
        {
            if (EntityManager.HasComponent<LocalTransform>(entity))
            {
                return EntityManager.GetComponentData<LocalTransform>(entity).Position;
            }
            
            return float3.zero;
        }

        /// <summary>
        /// Queue an effect for processing
        /// </summary>
        /// <param name="position">Effect position</param>
        /// <param name="normal">Surface normal</param>
        /// <param name="force">Impact force</param>
        /// <param name="sourceEntity">Source entity</param>
        private void QueueEffect(float3 position, float3 normal, float force, Entity sourceEntity)
        {
            pendingEffects.Enqueue(new EffectData
            {
                Position = position,
                Normal = normal,
                Force = force,
                SourceEntity = sourceEntity
            });
        }

        /// <summary>
        /// Process queued effects with frame rate limiting
        /// </summary>
        private void ProcessQueuedEffects()
        {
            int processedCount = 0;
            
            while (pendingEffects.Count > 0 && processedCount < maxEffectsPerFrame)
            {
                EffectData effectData = pendingEffects.Dequeue();
                SpawnEffect(effectData);
                processedCount++;
            }
        }

        /// <summary>
        /// Spawn visual and audio effect for the given effect data
        /// </summary>
        /// <param name="effectData">Data describing the effect to spawn</param>
        private void SpawnEffect(EffectData effectData)
        {
            SpawnVisualEffect(effectData);
            PlayAudioEffect();
        }

        /// <summary>
        /// Spawn visual effect at the specified location
        /// </summary>
        /// <param name="effectData">Effect data containing position and force information</param>
        private void SpawnVisualEffect(EffectData effectData)
        {
            if (impactPrefab == null) 
                return;

            // Calculate rotation based on normal
            Quaternion rotation = Quaternion.identity;
            if (math.lengthsq(effectData.Normal) > 0f)
            {
                rotation = Quaternion.LookRotation(effectData.Normal);
            }

            // Spawn effect instance
            GameObject impactInstance = Object.Instantiate(impactPrefab, effectData.Position, rotation);
            
            // Apply scaling based on force
            float forceScale = math.clamp(effectData.Force / 10f, 0.5f, 2f); // Normalize force to reasonable scale
            impactInstance.transform.localScale *= impactScale * forceScale;

            // Setup automatic cleanup
            SetupEffectCleanup(impactInstance);
        }

        /// <summary>
        /// Setup automatic cleanup for spawned effects
        /// </summary>
        /// <param name="effectInstance">Effect GameObject to cleanup</param>
        private void SetupEffectCleanup(GameObject effectInstance)
        {
            ParticleSystem particleSystem = effectInstance.GetComponent<ParticleSystem>();
            float lifetime = CalculateLifetime(particleSystem);
            
            Object.Destroy(effectInstance, lifetime);
        }

        /// <summary>
        /// Calculate appropriate lifetime for effect cleanup
        /// </summary>
        /// <param name="particleSystem">Particle system if present</param>
        /// <returns>Lifetime in seconds</returns>
        private float CalculateLifetime(ParticleSystem particleSystem)
        {
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                return main.duration + main.startLifetime.constantMax;
            }
            
            return defaultEffectLifetime;
        }

        /// <summary>
        /// Play audio effect if configured
        /// </summary>
        private void PlayAudioEffect()
        {
            if (impactSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(impactSound, soundVolume);
            }
        }

        #endregion
    }
}
