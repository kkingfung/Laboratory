using Unity.Entities;
using Unity.Netcode;
using UnityEngine;
using Laboratory.Models.ECS.Components;
using Laboratory.Infrastructure.Networking;

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
        /// Only executes on the server for authoritative control.
        /// </summary>
        protected override void OnUpdate()
        {
            if (!NetworkManager.Singleton.IsServer)
                return;

            float currentTime = (float)SystemAPI.Time.ElapsedTime;

            Entities
                .WithNone<DeadTag>()
                .ForEach((Entity entity, NetworkLifeState netLife, ref ECSHealthComponent health) =>
                {
                    ProcessPotentialDeath(entity, netLife, ref health, currentTime);
                }).WithoutBurst().Run();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes an entity to check if it should die and handles the death transition.
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <param name="netLife">The network life state component</param>
        /// <param name="health">The health component</param>
        /// <param name="currentTime">Current elapsed time</param>
        private void ProcessPotentialDeath(Entity entity, NetworkLifeState netLife, ref ECSHealthComponent health, float currentTime)
        {
            if (health.CurrentHealth <= 0 && netLife.IsAlive)
            {
                ProcessEntityDeath(entity, netLife, currentTime);
            }
        }

        /// <summary>
        /// Processes the death of an entity, updating network state and adding ECS components.
        /// </summary>
        /// <param name="entity">The entity that died</param>
        /// <param name="netLife">The network life state to update</param>
        /// <param name="currentTime">Current elapsed time</param>
        private void ProcessEntityDeath(Entity entity, NetworkLifeState netLife, float currentTime)
        {
            UpdateNetworkLifeState(netLife);
            AddDeathComponents(entity, currentTime);
        }

        /// <summary>
        /// Updates the network life state to reflect death.
        /// </summary>
        /// <param name="netLife">The network life state to update</param>
        private void UpdateNetworkLifeState(NetworkLifeState netLife)
        {
            netLife.CurrentState.Value = LifeState.Dead;
            netLife.RespawnTimeRemaining.Value = DefaultRespawnDelay;
        }

        /// <summary>
        /// Adds necessary ECS components for death processing.
        /// </summary>
        /// <param name="entity">The entity to add components to</param>
        /// <param name="currentTime">Current elapsed time</param>
        private void AddDeathComponents(Entity entity, float currentTime)
        {
            EntityManager.AddComponent<DeadTag>(entity);
            EntityManager.AddComponentData(entity, new DeathTime { TimeOfDeath = currentTime });
            EntityManager.AddComponentData(entity, new RespawnTimer { TimeRemaining = DefaultRespawnDelay });
            EntityManager.AddComponentData(entity, new DeathAnimationTrigger { Triggered = false });
        }

        #endregion
    }
}
