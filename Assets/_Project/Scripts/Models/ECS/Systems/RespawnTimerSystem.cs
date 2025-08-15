using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Laboratory.Infrastructure.Networking;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Manages player respawn timers and handles the respawn process for dead players.
    /// Only runs on the server and synchronizes respawn state to clients.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class RespawnTimerSystem : SystemBase
    {
        #region Fields

        /// <summary>
        /// Default respawn position when no specific spawn point is configured.
        /// TODO: Replace with proper spawn point system.
        /// </summary>
        private static readonly Vector3 DefaultRespawnPosition = Vector3.zero;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Updates respawn timers for all dead players and handles respawn logic.
        /// Only executes on the server to maintain authoritative respawn control.
        /// </summary>
        protected override void OnUpdate()
        {
            // Only process respawns on the server
            if (!NetworkManager.Singleton.IsServer)
                return;

            float deltaTime = Time.DeltaTime;

            // Process all dead entities with respawn timers
            Entities
                .WithAll<DeadTag>()
                .ForEach((Entity entity, NetworkLifeState netLife, ref RespawnTimer timer, ref HealthComponent health) =>
                {
                    ProcessRespawnTimer(entity, netLife, ref timer, ref health, deltaTime);
                })
                .WithoutBurst() // Required for NetworkObject access
                .Run();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes an individual player's respawn timer and handles respawn when timer expires.
        /// </summary>
        /// <param name="entity">The entity being processed for respawn</param>
        /// <param name="netLife">Network life state component for client synchronization</param>
        /// <param name="timer">Respawn timer component to update</param>
        /// <param name="health">Health component to restore on respawn</param>
        /// <param name="deltaTime">Time elapsed since last frame</param>
        private void ProcessRespawnTimer(Entity entity, NetworkLifeState netLife, ref RespawnTimer timer, ref HealthComponent health, float deltaTime)
        {
            // Decrease respawn timer
            timer.TimeRemaining -= deltaTime;
            
            // Synchronize timer state to clients
            netLife.RespawnTimeRemaining.Value = timer.TimeRemaining;

            // Check if respawn time has elapsed
            if (timer.TimeRemaining <= 0)
            {
                ExecuteRespawn(entity, netLife, ref health);
            }
        }

        /// <summary>
        /// Executes the respawn process for a player entity, restoring health and removing death components.
        /// </summary>
        /// <param name="entity">The entity to respawn</param>
        /// <param name="netLife">Network life state component</param>
        /// <param name="health">Health component to restore</param>
        private void ExecuteRespawn(Entity entity, NetworkLifeState netLife, ref HealthComponent health)
        {
            // Restore player health to maximum
            RestorePlayerHealth(ref health);
            
            // Update network life state for client synchronization
            UpdateNetworkLifeState(netLife);
            
            // Remove death-related components
            RemoveDeathComponents(entity);
            
            // Set respawn position
            SetRespawnPosition(netLife);
        }

        /// <summary>
        /// Restores player health to maximum capacity.
        /// </summary>
        /// <param name="health">Health component to restore</param>
        private static void RestorePlayerHealth(ref HealthComponent health)
        {
            health.CurrentHealth = health.MaxHealth;
        }

        /// <summary>
        /// Updates network life state to indicate the player is alive and resets respawn timer.
        /// </summary>
        /// <param name="netLife">Network life state component to update</param>
        private static void UpdateNetworkLifeState(NetworkLifeState netLife)
        {
            netLife.CurrentState.Value = LifeState.Alive;
            netLife.RespawnTimeRemaining.Value = 0f;
        }

        /// <summary>
        /// Removes all death-related components from the respawned entity.
        /// </summary>
        /// <param name="entity">Entity to clean up death components from</param>
        private void RemoveDeathComponents(Entity entity)
        {
            EntityManager.RemoveComponent<DeadTag>(entity);
            EntityManager.RemoveComponent<DeathTime>(entity);
            EntityManager.RemoveComponent<RespawnTimer>(entity);
            EntityManager.RemoveComponent<DeathAnimationTrigger>(entity);
        }

        /// <summary>
        /// Sets the respawn position for the player entity.
        /// </summary>
        /// <param name="netLife">Network life state component to access NetworkObject</param>
        private static void SetRespawnPosition(NetworkLifeState netLife)
        {
            var networkObject = netLife.GetComponent<NetworkObject>();
            networkObject.transform.position = DefaultRespawnPosition;
            
            // TODO: Implement proper spawn point system
            // Consider factors like:
            // - Team-based spawn points
            // - Safe spawn locations away from enemies
            // - Spawn point rotation to prevent camping
            // - Map-specific spawn configurations
        }

        #endregion
    }
}
