using Unity.Netcode;
using UnityEngine;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Manages damage requests and validation across the network.
    /// Provides server-authoritative damage processing with client feedback.
    /// </summary>
    public class NetworkDamageManager : NetworkBehaviour
    {
        #region Network RPC Methods

        /// <summary>
        /// Requests damage to be applied to a target player. Server validates and applies damage.
        /// </summary>
        /// <param name="targetPlayerClientId">Client ID of the target player to damage.</param>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="hitDirection">Direction vector of the hit for physics effects.</param>
        /// <param name="rpcParams">Server RPC parameters containing sender information.</param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestDamageServerRpc(ulong targetPlayerClientId, float damage, Vector3 hitDirection, ServerRpcParams rpcParams = default)
        {
            if (!IsServer) return;

            ulong attackerClientId = rpcParams.Receive.SenderClientId;

            var targetNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetPlayerClientId);
            if (targetNetObj == null)
            {
                Debug.LogWarning($"RequestDamageServerRpc: target player {targetPlayerClientId} not found.");
                return;
            }

            var playerHealth = targetNetObj.GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                Debug.LogWarning("RequestDamageServerRpc: target missing PlayerHealth component.");
                return;
            }

            // TODO: Add validation logic here (friendly fire, distance checks, cooldowns, etc.)
            // if (!IsValidHit(attackerClientId, targetPlayerClientId, damage, hitDirection)) return;

            // Apply damage server-side with authority
            playerHealth.ApplyDamageServer(damage, attackerClientId, hitDirection);

            // Notify all clients for visual/audio feedback
            BroadcastDamageClientRpc(targetPlayerClientId, attackerClientId, damage, hitDirection);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Notifies all clients about damage events for local effects and UI updates.
        /// </summary>
        /// <param name="targetClientId">Client ID of the damaged player.</param>
        /// <param name="attackerClientId">Client ID of the attacking player.</param>
        /// <param name="damage">Amount of damage dealt.</param>
        /// <param name="hitDirection">Direction vector of the hit.</param>
        /// <param name="clientRpcParams">Client RPC parameters.</param>
        [ClientRpc]
        private void BroadcastDamageClientRpc(ulong targetClientId, ulong attackerClientId, float damage, Vector3 hitDirection, ClientRpcParams clientRpcParams = default)
        {
            // Publish event to message bus for UI and effect systems to consume
            MessageBus.Publish(new DamageEvent(targetClientId, attackerClientId, damage, hitDirection));
        }

        #endregion
    }
}
