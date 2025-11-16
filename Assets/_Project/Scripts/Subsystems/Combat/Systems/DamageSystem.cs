using Unity.Entities;
using Unity.Netcode;
using Unity.Transforms;
using UnityEngine;
using Laboratory.Models.ECS.Components;
using Laboratory.Infrastructure.Networking;
using Laboratory.Core.Health.Components;
using DamageType = Laboratory.Core.Enums.DamageType;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// Processes damage requests and applies them to entity health components.
    /// Handles network synchronization and cleanup of processed damage requests.
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class DamageSystem : SystemBase
    {
        #region Fields
        // No component lookups needed for MonoBehaviour components
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// System initialization.
        /// </summary>
        protected override void OnCreate()
        {
            base.OnCreate();
            // NetworkHealth is a MonoBehaviour, accessed through GameObject
        }

        /// <summary>
        /// Processes all pending damage requests and applies them to health components.
        /// </summary>
        protected override void OnUpdate()
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (var (health, damageRequest, entity) in SystemAPI.Query<RefRW<ECSHealthComponent>, RefRO<Laboratory.Models.ECS.Components.DamageRequest>>()
                .WithEntityAccess())
            {
                ProcessDamageRequest(entity, ref health.ValueRW, in damageRequest.ValueRO, entityManager);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Processes a single damage request and updates the entity's health.
        /// </summary>
        /// <param name="entity">The entity receiving damage</param>
        /// <param name="health">The health component to modify</param>
        /// <param name="damageRequest">The damage request to process</param>
        /// <param name="entityManager">Entity manager for component operations</param>
        private void ProcessDamageRequest(Entity entity, ref ECSHealthComponent health, in Laboratory.Models.ECS.Components.DamageRequest damageRequest, EntityManager entityManager)
        {
            ApplyDamageToHealth(ref health, damageRequest.Amount);
            SyncNetworkHealth(entity, damageRequest.Amount, entityManager);
            CleanupDamageRequest(entity);
        }

        /// <summary>
        /// Applies damage to the health component.
        /// </summary>
        /// <param name="health">The health component to modify</param>
        /// <param name="damageAmount">Amount of damage to apply</param>
        private void ApplyDamageToHealth(ref ECSHealthComponent health, float damageAmount)
        {
            health.CurrentHealth -= (int)damageAmount;
            if (health.CurrentHealth < 0) 
                health.CurrentHealth = 0;
        }

        /// <summary>
        /// Synchronizes health changes with the network health component if present.
        /// </summary>
        /// <param name="entity">The entity to sync</param>
        /// <param name="damageAmount">Amount of damage applied</param>
        /// <param name="entityManager">Entity manager for component access</param>
        private void SyncNetworkHealth(Entity entity, float damageAmount, EntityManager entityManager)
        {
            if (!entityManager.HasComponent<Unity.Netcode.NetworkObject>(entity))
                return;

            var networkObject = entityManager.GetComponentObject<Unity.Netcode.NetworkObject>(entity);
            var gameObject = networkObject.gameObject;
            var networkHealth = gameObject.GetComponent<NetworkHealthComponent>();

            if (networkHealth != null && networkHealth.IsServer)
            {
                var damageRequest = new Laboratory.Core.Health.DamageRequest
                {
                    Amount = damageAmount,
                    Type = DamageType.Normal,
                    Source = null,
                    Direction = Vector3.zero
                };
                networkHealth.TakeDamage(damageRequest);
            }
        }

        /// <summary>
        /// Removes the processed damage request component from the entity.
        /// </summary>
        /// <param name="entity">The entity to clean up</param>
        private void CleanupDamageRequest(Entity entity)
        {
            EntityManager.RemoveComponent<Laboratory.Models.ECS.Components.DamageRequest>(entity);
        }

        #endregion
    }
}
