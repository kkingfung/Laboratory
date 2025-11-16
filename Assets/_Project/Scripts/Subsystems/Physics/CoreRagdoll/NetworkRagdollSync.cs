using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Laboratory.Core.Ragdoll;

namespace Laboratory.Network.Ragdoll
{
    /// <summary>
    /// Synchronizes ragdoll events and bone transforms across the network.
    /// Provides networked hit reactions and optional transform synchronization for multiplayer scenarios.
    /// All component references must be manually assigned via Inspector.
    /// </summary>
    public class NetworkRagdollSync : NetworkBehaviour
    {
        #region Fields
        
        [Header("Required Component References")]
        [Tooltip("RagdollSetup component for managing ragdoll bone states.")]
        [SerializeField] private RagdollSetup ragdollSetup;
        
        [Tooltip("HitReactionManager component for processing local hit reactions.")]
        [SerializeField] private HitReactionManager hitReactionManager;
        
        [Header("Network Synchronization Settings")]
        [Tooltip("Enable synchronization of bone transforms after ragdoll activation.")]
        [SerializeField] private bool syncBoneTransforms = true;
        
        [Tooltip("Delay in seconds before sending transform snapshots after ragdoll trigger.")]
        [SerializeField] private float transformSyncDelay = 0.1f;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Triggers a networked hit reaction with the specified parameters.
        /// Routes through server for authoritative processing in multiplayer scenarios.
        /// </summary>
        /// <param name="boneName">Name of the bone that received the hit</param>
        /// <param name="hitForce">Force vector to apply to the bone</param>
        public void NetworkedHit(string boneName, Vector3 hitForce)
        {
            if (IsServer)
            {
                ProcessServerHit(boneName, hitForce);
            }
            else
            {
                RequestServerHitProcessing(boneName, hitForce);
            }
        }
        
        #endregion
        
        #region Private Methods - Server Processing
        
        /// <summary>
        /// Processes hit on server and notifies all clients.
        /// </summary>
        /// <param name="boneName">Name of the affected bone</param>
        /// <param name="hitForce">Force vector to apply</param>
        private void ProcessServerHit(string boneName, Vector3 hitForce)
        {
            ExecuteLocalHitReaction(boneName, hitForce);
            NotifyClientsOfHit(boneName, hitForce);
        }
        
        /// <summary>
        /// Executes the hit reaction locally on the server.
        /// </summary>
        /// <param name="boneName">Name of the affected bone</param>
        /// <param name="hitForce">Force vector to apply</param>
        private void ExecuteLocalHitReaction(string boneName, Vector3 hitForce)
        {
            if (hitReactionManager != null)
            {
                hitReactionManager.OnHit(boneName, hitForce);
            }
        }
        
        /// <summary>
        /// Sends hit notification to all clients.
        /// </summary>
        /// <param name="boneName">Name of the affected bone</param>
        /// <param name="hitForce">Force vector to apply</param>
        private void NotifyClientsOfHit(string boneName, Vector3 hitForce)
        {
            TriggerRagdollClientRpc(boneName, hitForce);
        }
        
        #endregion
        
        #region Private Methods - Client Processing
        
        /// <summary>
        /// Requests server to process a hit event.
        /// </summary>
        /// <param name="boneName">Name of the affected bone</param>
        /// <param name="hitForce">Force vector to apply</param>
        private void RequestServerHitProcessing(string boneName, Vector3 hitForce)
        {
            RequestRagdollServerRpc(boneName, hitForce);
        }
        
        /// <summary>
        /// Processes hit reaction on client side.
        /// </summary>
        /// <param name="boneName">Name of the affected bone</param>
        /// <param name="hitForce">Force vector to apply</param>
        private void ProcessClientHitReaction(string boneName, Vector3 hitForce)
        {
            if (hitReactionManager != null)
            {
                hitReactionManager.OnHit(boneName, hitForce);
            }
        }
        
        /// <summary>
        /// Initiates bone transform synchronization after a delay.
        /// </summary>
        private void InitiateBoneTransformSync()
        {
            StartCoroutine(DelayedBoneSyncCoroutine());
        }
        
        #endregion
        
        #region Network RPCs
        
        /// <summary>
        /// Server RPC for requesting hit processing from clients.
        /// </summary>
        /// <param name="boneName">Name of the bone to hit</param>
        /// <param name="hitForce">Force vector to apply</param>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void RequestRagdollServerRpc(string boneName, Vector3 hitForce)
        {
            NetworkedHit(boneName, hitForce);
        }
        
        /// <summary>
        /// Client RPC for triggering ragdoll reactions on all clients.
        /// </summary>
        /// <param name="boneName">Name of the bone to hit</param>
        /// <param name="hitForce">Force vector to apply</param>
        [ClientRpc]
        private void TriggerRagdollClientRpc(string boneName, Vector3 hitForce)
        {
            if (IsOwner) return; // Owner already processed locally
            
            ProcessClientHitReaction(boneName, hitForce);
            
            if (syncBoneTransforms)
            {
                InitiateBoneTransformSync();
            }
        }
        
        /// <summary>
        /// Client RPC for synchronizing individual bone transforms.
        /// </summary>
        /// <param name="boneName">Name of the bone to update</param>
        /// <param name="position">New world position</param>
        /// <param name="rotation">New world rotation</param>
        [ClientRpc]
        private void SendBoneTransformClientRpc(string boneName, Vector3 position, Quaternion rotation)
        {
            if (IsOwner) return; // Skip owner to avoid overriding local transforms
            
            ApplyBoneTransform(boneName, position, rotation);
        }
        
        #endregion
        
        #region Private Methods - Transform Synchronization
        
        /// <summary>
        /// Coroutine that waits for the specified delay before synchronizing bone transforms.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator DelayedBoneSyncCoroutine()
        {
            yield return new WaitForSeconds(transformSyncDelay);
            SynchronizeAllBoneTransforms();
        }
        
        /// <summary>
        /// Sends transform data for all ragdoll bones to clients.
        /// </summary>
        private void SynchronizeAllBoneTransforms()
        {
            if (ragdollSetup?.RagdollBones == null) return;
            
            foreach (var bone in ragdollSetup.RagdollBones)
            {
                if (bone.BoneTransform != null)
                {
                    SendBoneTransformClientRpc(
                        bone.BoneName,
                        bone.BoneTransform.position,
                        bone.BoneTransform.rotation
                    );
                }
            }
        }
        
        /// <summary>
        /// Applies received transform data to the specified bone.
        /// </summary>
        /// <param name="boneName">Name of the bone to update</param>
        /// <param name="position">New world position</param>
        /// <param name="rotation">New world rotation</param>
        private void ApplyBoneTransform(string boneName, Vector3 position, Quaternion rotation)
        {
            if (ragdollSetup?.RagdollBones == null) return;
            
            foreach (var bone in ragdollSetup.RagdollBones)
            {
                if (bone.BoneName == boneName && bone.BoneTransform != null)
                {
                    bone.BoneTransform.position = position;
                    bone.BoneTransform.rotation = rotation;
                    break;
                }
            }
        }
        
        #endregion
    }
}
