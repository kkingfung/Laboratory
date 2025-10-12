using Unity.Entities;
using Unity.Collections;
using Unity.Burst;
using UnityEngine;
using Laboratory.Chimera.Discovery.Core;
using Laboratory.Chimera.Discovery.UI;

namespace Laboratory.Chimera.Discovery.Systems
{
    /// <summary>
    /// ECS system that bridges discovery detection to UI celebration system
    /// Processes celebration triggers and coordinates with MonoBehaviour celebration manager
    /// </summary>
    [BurstCompile]
    public partial struct CelebrationTriggerSystem : ISystem
    {
        private EntityQuery _celebrationQuery;
        private ComponentLookup<Laboratory.Chimera.Discovery.Core.DiscoveryEvent> _discoveryLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _celebrationQuery = SystemAPI.QueryBuilder()
                .WithAll<CelebrationTrigger>()
                .Build();

            _discoveryLookup = SystemAPI.GetComponentLookup<Laboratory.Chimera.Discovery.Core.DiscoveryEvent>(true);

            state.RequireForUpdate(_celebrationQuery);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Note: This system cannot be fully Burst compiled due to MonoBehaviour interaction
            _discoveryLookup.Update(ref state);

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            // Process celebration triggers
            foreach (var (trigger, entity) in SystemAPI.Query<RefRO<CelebrationTrigger>>().WithEntityAccess())
            {
                // Get the discovery data
                if (_discoveryLookup.TryGetComponent(trigger.ValueRO.DiscoveryEntity, out Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery))
                {
                    // Trigger celebration through MonoBehaviour manager
                    TriggerCelebrationEvent(discovery);

                    // Mark celebration as processed
                    ecb.AddComponent(entity, new CelebrationProcessed
                    {
                        ProcessedTime = (uint)SystemAPI.Time.ElapsedTime,
                        DiscoveryEntity = trigger.ValueRO.DiscoveryEntity
                    });
                }

                // Remove the trigger component
                ecb.RemoveComponent<CelebrationTrigger>(entity);
            }
        }

        /// <summary>
        /// Bridge to MonoBehaviour celebration system
        /// Cannot be Burst compiled due to MonoBehaviour interaction
        /// </summary>
        private void TriggerCelebrationEvent(Laboratory.Chimera.Discovery.Core.DiscoveryEvent discovery)
        {
            // Use the static API to trigger celebration
            DiscoveryCelebrationManager.TriggerCelebration(discovery);

            // Log the discovery for debugging
            UnityEngine.Debug.Log($"🎉 Discovery Celebration Triggered: {discovery.DiscoveryName} ({discovery.Rarity}) - Significance: {discovery.SignificanceScore:F1}");

            // Could also trigger other systems here:
            // - Achievement system
            // - Social sharing system
            // - Statistics tracking
            // - Leaderboard updates
        }
    }

    /// <summary>
    /// Component marking that a celebration has been processed
    /// Used for tracking and cleanup
    /// </summary>
    public struct CelebrationProcessed : IComponentData
    {
        public uint ProcessedTime;
        public Entity DiscoveryEntity;
    }

    /// <summary>
    /// System to clean up processed celebrations after a delay
    /// </summary>
    [BurstCompile]
    public partial struct CelebrationCleanupSystem : ISystem
    {
        private EntityQuery _processedQuery;
        private double _lastCleanupTime;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _processedQuery = SystemAPI.QueryBuilder()
                .WithAll<CelebrationProcessed>()
                .Build();

            _lastCleanupTime = SystemAPI.Time.ElapsedTime;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only run cleanup every 30 seconds
            if (SystemAPI.Time.ElapsedTime - _lastCleanupTime < 30.0)
                return;

            _lastCleanupTime = SystemAPI.Time.ElapsedTime;

            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            uint currentTime = (uint)SystemAPI.Time.ElapsedTime;

            // Remove processed celebrations older than 5 minutes
            foreach (var (processed, entity) in SystemAPI.Query<RefRO<CelebrationProcessed>>().WithEntityAccess())
            {
                if (currentTime - processed.ValueRO.ProcessedTime > 300) // 5 minutes
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}