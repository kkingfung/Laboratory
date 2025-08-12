using Unity.Netcode;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Handles networked damage requests and broadcasts authoritative results.
    /// </summary>
    public class NetworkDamageEventSystem : NetworkBehaviour
    {
        private DamageEventSystem _damageEventSystem;

        private void Awake()
        {
            _damageEventSystem = FindObjectOfType<DamageEventSystem>();
        }

        /// <summary>
        /// Called by a client to request damage application.
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestDamageServerRpc(ulong targetNetworkId, float damage, Vector3 hitDirection, ulong attackerId)
        {
            if (!IsServer) return;

            var targetObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetNetworkId);
            if (targetObject == null) return;

            // Validate (optional: check teams, cooldowns, etc.)
            var damageEvent = new DamageEvent
            {
                TargetId = targetNetworkId,
                AttackerId = attackerId,
                DamageAmount = damage,
                HitDirection = hitDirection
            };

            _damageEventSystem.ApplyDamage(damageEvent);

            // Broadcast result to all clients
            BroadcastDamageClientRpc(targetNetworkId, damage, hitDirection, attackerId);
        }

        [ClientRpc]
        private void BroadcastDamageClientRpc(ulong targetNetworkId, float damage, Vector3 hitDirection, ulong attackerId)
        {
            // Fire local UI effects only if we are the target
            if (NetworkManager.Singleton.LocalClientId == targetNetworkId)
            {
                MessageBus.Publish(new DamageEvent
                {
                    TargetId = targetNetworkId,
                    AttackerId = attackerId,
                    DamageAmount = damage,
                    HitDirection = hitDirection
                });
            }
        }
    }
}
