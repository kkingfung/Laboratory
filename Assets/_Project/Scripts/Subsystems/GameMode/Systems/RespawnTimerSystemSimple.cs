using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Models.ECS.Components;
using Laboratory.Core.Events;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Component for respawn invulnerability period.
    /// </summary>
    public struct RespawnInvulnerability : IComponentData
    {
        /// <summary>Time remaining for invulnerability in seconds.</summary>
        public float TimeRemaining;
    }
    
    /// <summary>
    /// Simplified respawn timer system that compiles successfully
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RespawnTimerSystemSimple : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<DeadTag>();
        }

        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            float deltaTime = SystemAPI.Time.DeltaTime;

            // Process all dead entities with respawn timers
            foreach (var (timer, health, entity) in SystemAPI.Query<RefRW<RespawnTimer>, RefRW<ECSHealthComponent>>()
                .WithEntityAccess().WithAll<DeadTag>())
            {
                ProcessRespawnTimer(ecb, entity, ref timer.ValueRW, ref health.ValueRW, deltaTime);
            }
        }

        private void ProcessRespawnTimer(EntityCommandBuffer ecb, Entity entity, ref RespawnTimer timer, ref ECSHealthComponent health, float deltaTime)
        {
            // Decrease respawn timer
            timer.TimeRemaining -= deltaTime;

            // Check if respawn time has elapsed
            if (timer.TimeRemaining <= 0)
            {
                ExecuteRespawn(ecb, entity, ref health);
            }
        }

        private void ExecuteRespawn(EntityCommandBuffer ecb, Entity entity, ref ECSHealthComponent health)
        {
            // Restore player health to maximum
            health.CurrentHealth = health.MaxHealth;
            
            // Remove death components
            ecb.RemoveComponent<DeadTag>(entity);
            ecb.RemoveComponent<RespawnTimer>(entity);
            
            Debug.Log($"RespawnTimerSystemSimple: Successfully respawned entity {entity}");
        }
    }
}
