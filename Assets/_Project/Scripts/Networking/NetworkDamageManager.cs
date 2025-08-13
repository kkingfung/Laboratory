// NetworkDamageManager.cs
using Unity.Netcode;
using UnityEngine;
// FIXME: tidyup after 8/29
public class NetworkDamageManager : NetworkBehaviour
{
    // Called by clients to request damaging a player (or self). Server validates and applies.
    // targetPlayerClientId: the clientId that owns the target player NetworkObject
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
            Debug.LogWarning("RequestDamageServerRpc: target missing PlayerHealth.");
            return;
        }

        // Optional: validation (friendly fire, distance, cheat checks, cooldowns)
        // if (!IsValidHit(...)) return;

        // Apply damage server-side
        playerHealth.ApplyDamageServer(damage, attackerClientId, hitDirection);

        // Broadcast for clients to play local hit effects if needed (clients may subscribe to MessageBus after this broadcast)
        BroadcastDamageClientRpc(targetPlayerClientId, attackerClientId, damage, hitDirection);
    }

    // Optional: notify clients to show local visual/sound feedback
    [ClientRpc]
    private void BroadcastDamageClientRpc(ulong targetClientId, ulong attackerClientId, float damage, Vector3 hitDirection, ClientRpcParams clientRpcParams = default)
    {
        // Fire a local MessageBus event so UIs/EFFECTS can respond
        MessageBus.Publish(new DamageEvent(targetClientId, attackerClientId, damage, hitDirection));
    }
}
