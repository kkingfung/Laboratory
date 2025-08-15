using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Syncs ragdoll events and optionally bone transforms over the network.
/// Works with RagdollSetup and HitReactionManager.
/// All references must be assigned via Inspector.
/// </summary>
public class NetworkRagdollSync : NetworkBehaviour
{
    [Header("Required References")]
    [SerializeField] private RagdollSetup ragdollSetup;
    [SerializeField] private HitReactionManager hitReactionManager;

    [Header("Settings")]
    [Tooltip("If true, sync bone transforms after ragdoll for accurate visual.")]
    [SerializeField] private bool syncBoneTransforms = true;

    [Tooltip("Time in seconds to send transform snapshot after ragdoll trigger.")]
    [SerializeField] private float transformSyncDelay = 0.1f;

    // Temporary storage for transform data
    private struct BoneSnapshot
    {
        public string boneName;
        public Vector3 position;
        public Quaternion rotation;
    }

    /// <summary>
    /// Call this instead of HitReactionManager.OnHit for networked hit.
    /// </summary>
    public void NetworkedHit(string boneName, Vector3 hitForce)
    {
        if (IsServer)
        {
            // Server triggers ragdoll locally
            hitReactionManager.OnHit(boneName, hitForce);

            // Notify clients
            TriggerRagdollClientRpc(boneName, hitForce);
        }
        else
        {
            // Ask server to process hit
            RequestRagdollServerRpc(boneName, hitForce);
        }
    }

    #region ServerRpc / ClientRpc

    [ServerRpc(RequireOwnership = false)]
    private void RequestRagdollServerRpc(string boneName, Vector3 hitForce)
    {
        NetworkedHit(boneName, hitForce);
    }

    [ClientRpc]
    private void TriggerRagdollClientRpc(string boneName, Vector3 hitForce)
    {
        if (IsOwner) return; // Owner already applied locally
        hitReactionManager.OnHit(boneName, hitForce);

        if (syncBoneTransforms)
            StartCoroutine(DelayedBoneSync());
    }

    #endregion

    #region Optional Bone Transform Sync

    private IEnumerator DelayedBoneSync()
    {
        yield return new WaitForSeconds(transformSyncDelay);

        foreach (var bone in ragdollSetup.RagdollBones)
        {
            SendBoneTransformClientRpc(
                bone.BoneName,
                bone.BoneTransform.position,
                bone.BoneTransform.rotation
            );
        }
    }

    [ClientRpc]
    private void SendBoneTransformClientRpc(string boneName, Vector3 pos, Quaternion rot)
    {
        if (IsOwner) return; // Skip owner
        foreach (var bone in ragdollSetup.RagdollBones)
        {
            if (bone.BoneName == boneName)
            {
                bone.BoneTransform.position = pos;
                bone.BoneTransform.rotation = rot;
                break;
            }
        }
    }

    #endregion
}
