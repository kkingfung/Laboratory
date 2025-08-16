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
    public partial struct RagdollImpactEffectSystem : ISystem
    {
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

        #region Fields

        private Queue<EffectData> pendingEffects;
        private RagdollImpactEffectSettings settings;

        #endregion

        #region ISystem Implementation

        /// <summary>
        /// Initialize system components and validate setup
        /// </summary>
        public void OnCreate(ref SystemState state)
        {
            pendingEffects = new Queue<EffectData>();
            
            // Get settings from a singleton entity or create default
            settings = GetOrCreateSettings(ref state);
            
            ValidateConfiguration();
        }

        /// <summary>
        /// Process hit events and trigger appropriate effects each frame
        /// </summary>
        public void OnUpdate(ref SystemState state)
        {
            ProcessHitEvents(ref state);
            ProcessCollisionEvents(ref state);
            ProcessLandingEvents(ref state);
            ProcessQueuedEffects();
        }

        public void OnDestroy(ref SystemState state)
        {
            // Cleanup if needed
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Get or create settings for this system
        /// </summary>
        private RagdollImpactEffectSettings GetOrCreateSettings(ref SystemState state)
        {
            // Try to find existing settings singleton
            var settingsQuery = SystemAPI.QueryBuilder().WithAll<RagdollImpactEffectSettings>().Build();
            
            if (settingsQuery.TryGetSingleton<RagdollImpactEffectSettings>(out var existingSettings))
            {
                return existingSettings;
            }

            // Create default settings if none found
            var defaultSettings = new RagdollImpactEffectSettings
            {
                ImpactScale = 1f,
                DefaultEffectLifetime = 2f,
                SoundVolume = 1f,
                MaxEffectsPerFrame = 10,
                UseObjectPooling = false
            };

            // Create a singleton entity for settings
            var settingsEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(settingsEntity, defaultSettings);
            state.EntityManager.AddComponent<Singleton>(settingsEntity);

            return defaultSettings;
        }

        /// <summary>
        /// Validate system configuration and log warnings for missing components
        /// </summary>
        private void ValidateConfiguration()
        {
            if (settings.ImpactPrefabEntity == Entity.Null)
            {
                Debug.LogWarning("[RagdollImpactEffectSystem] No impact prefab entity assigned. Visual effects will not spawn.");
            }
        }

        /// <summary>
        /// Process all entities with hit events
        /// </summary>
        private void ProcessHitEvents(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            foreach (var (hitEvent, entity) in SystemAPI.Query<RefRO<HitEvent>>().WithEntityAccess())
            {
                float3 hitPosition = GetEntityPosition(ref state, hitEvent.ValueRO.BoneEntity);
                float3 hitNormal = math.normalize(hitEvent.ValueRO.Force);
                float forceAmount = math.length(hitEvent.ValueRO.Force);

                QueueEffect(hitPosition, hitNormal, forceAmount, entity);
                
                // Schedule removal of the hit event to prevent reprocessing
                ecb.RemoveComponent<HitEvent>(entity);
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// Process all entities with collision events
        /// </summary>
        private void ProcessCollisionEvents(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            foreach (var (collisionEvent, entity) in SystemAPI.Query<RefRO<CollisionEvent>>().WithEntityAccess())
            {
                float3 position = GetEntityPosition(ref state, entity);
                float3 normal = collisionEvent.ValueRO.CollisionNormal;
                float force = math.length(collisionEvent.ValueRO.CollisionVelocity);

                QueueEffect(position, normal, force, entity);
                
                // Schedule removal of processed event
                ecb.RemoveComponent<CollisionEvent>(entity);
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// Process all entities with landing events
        /// </summary>
        private void ProcessLandingEvents(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            
            foreach (var (landingEvent, entity) in SystemAPI.Query<RefRO<LandingEvent>>().WithEntityAccess())
            {
                float3 position = GetEntityPosition(ref state, entity);
                float3 normal = new float3(0, 1, 0); // Assume upward normal for landing
                float force = landingEvent.ValueRO.ImpactVelocity;

                QueueEffect(position, normal, force, entity);
                
                // Schedule removal of processed event
                ecb.RemoveComponent<LandingEvent>(entity);
            }
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        /// <summary>
        /// Get world position of an entity
        /// </summary>
        /// <param name="state">System state reference</param>
        /// <param name="entity">Entity to get position for</param>
        /// <returns>World position as float3</returns>
        private float3 GetEntityPosition(ref SystemState state, Entity entity)
        {
            if (state.EntityManager.HasComponent<LocalTransform>(entity))
            {
                return state.EntityManager.GetComponentData<LocalTransform>(entity).Position;
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
            
            while (pendingEffects.Count > 0 && processedCount < settings.MaxEffectsPerFrame)
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
            if (settings.ImpactPrefabEntity == Entity.Null) 
                return;

            // Calculate rotation based on normal
            quaternion rotation = quaternion.identity;
            if (math.lengthsq(effectData.Normal) > 0f)
            {
                rotation = quaternion.LookRotationSafe(effectData.Normal, math.up());
            }

            // For now, we'll need to use a managed component bridge to spawn UnityEngine.GameObject
            // In a pure DOTS system, you'd typically work with entity prefabs
            // This is a simplified approach that bridges to GameObject spawning
            EffectSpawner.SpawnEffect(settings.ImpactPrefabEntity, effectData.Position, rotation, settings.ImpactScale, effectData.Force);
        }

        /// <summary>
        /// Play audio effect if configured
        /// </summary>
        private void PlayAudioEffect()
        {
            // In a pure DOTS system, audio would typically be handled through entity components
            // For now, we'll use a managed bridge
            if (settings.ImpactSoundEntity != Entity.Null)
            {
                AudioEffectPlayer.PlayImpactSound(settings.ImpactSoundEntity, settings.SoundVolume);
            }
        }

        #endregion
    }

    /// <summary>
    /// Component data for configuring the ragdoll impact effect system
    /// </summary>
    public struct RagdollImpactEffectSettings : IComponentData
    {
        public Entity ImpactPrefabEntity;
        public Entity ImpactSoundEntity;
        public Entity AudioSourceEntity;
        public float ImpactScale;
        public float DefaultEffectLifetime;
        public float SoundVolume;
        public int MaxEffectsPerFrame;
        public bool UseObjectPooling;
    }

    /// <summary>
    /// Static bridge class for managed GameObject spawning (temporary solution)
    /// In a full DOTS implementation, this would be replaced with pure entity operations
    /// </summary>
    public static class EffectSpawner
    {
        public static void SpawnEffect(Entity prefabEntity, float3 position, quaternion rotation, float scale, float force)
        {
            // This is a bridge method that would need to be implemented
            // based on your specific GameObject prefab spawning needs
            Debug.Log($"Spawning effect at {position} with force {force}");
        }
    }

    /// <summary>
    /// Static bridge class for managed audio playing (temporary solution)
    /// </summary>
    public static class AudioEffectPlayer
    {
        public static void PlayImpactSound(Entity soundEntity, float volume)
        {
            // This is a bridge method that would need to be implemented
            // based on your specific audio playing needs
            Debug.Log($"Playing impact sound at volume {volume}");
        }
    }
}
