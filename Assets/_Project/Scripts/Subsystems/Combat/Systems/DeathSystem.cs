using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Manages entity death state transitions and respawn timers.
    /// Only runs on the server to ensure authoritative death processing.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DeathSystem : SystemBase
    {
        #region Constants

        /// <summary>
        /// Default respawn delay in seconds
        /// </summary>
        private const float DefaultRespawnDelay = 5f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Processes entities that have died and sets up their death state and respawn timers.
        /// </summary>
        protected override void OnUpdate()
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            // Process entities that might have died
            foreach (var (health, entity) in SystemAPI.Query<RefRW<ECSHealthComponent>>().WithEntityAccess().WithNone<DeadTag>())
            {
                if (health.ValueRO.CurrentHealth <= 0 && health.ValueRO.IsAlive)
                {
                    ProcessEntityDeath(ecb, entity, currentTime);
                    health.ValueRW.IsAlive = false;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes a newly dead entity by adding death components and setting up respawn
        /// </summary>
        private void ProcessEntityDeath(EntityCommandBuffer ecb, Entity entity, float currentTime)
        {
            // Add dead tag with death information
            ecb.AddComponent(entity, new DeadTag
            {
                DeathTime = currentTime,
                Killer = Entity.Null,
                DeathPosition = float3.zero
            });

            // Add respawn timer
            ecb.AddComponent(entity, new RespawnTimer
            {
                TimeRemaining = DefaultRespawnDelay,
                TotalTime = DefaultRespawnDelay,
                IsActive = true
            });

            // Add death animation trigger
            ecb.AddComponent(entity, DeathAnimationTrigger.Create());

            Debug.Log($"Entity {entity} has died and will respawn in {DefaultRespawnDelay} seconds");
        }

        #endregion
    }
}
