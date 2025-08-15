using System.Collections;
using UnityEngine;

namespace Laboratory.Core.Combat
{
    /// <summary>
    /// Manages hit reactions for characters using ragdoll physics.
    /// Handles partial ragdoll activation, force application, and smooth recovery blending.
    /// All component references must be manually assigned in the Inspector.
    /// </summary>
    public class HitReactionManager : MonoBehaviour
    {
        #region Fields
        
        [Header("Required Component References")]
        [Tooltip("RagdollSetup component responsible for ragdoll bone management.")]
        [SerializeField] private RagdollSetup ragdollSetup;
        
        [Tooltip("RagdollBlend component responsible for smooth animation transitions.")]
        [SerializeField] private RagdollBlend ragdollBlend;
        
        [Header("Hit Reaction Settings")]
        [Tooltip("Duration in seconds before initiating blend back to animation.")]
        [SerializeField] private float blendDelay = 0.5f;
        
        [Tooltip("Multiplier applied to impact forces before applying to ragdoll bones.")]
        [SerializeField] private float forceMultiplier = 1.0f;
        
        /// <summary>
        /// Currently running blend coroutine reference for cancellation purposes.
        /// </summary>
        private Coroutine blendCoroutine;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Processes a hit event on a specific bone with the given force.
        /// Activates partial ragdoll physics, applies impact force, and schedules recovery blend.
        /// </summary>
        /// <param name="boneName">Name of the bone that was hit (must match RagdollSetup bone names)</param>
        /// <param name="hitForce">World-space force vector to apply to the bone</param>
        public void OnHit(string boneName, Vector3 hitForce)
        {
            if (!AreRequiredComponentsValid()) return;
            
            ActivatePartialRagdoll(boneName);
            ApplyImpactForce(boneName, hitForce);
            ScheduleBlendRecovery();
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Validates that all required component references are assigned.
        /// </summary>
        /// <returns>True if all required components are valid</returns>
        private bool AreRequiredComponentsValid()
        {
            return ragdollSetup != null && ragdollBlend != null;
        }
        
        /// <summary>
        /// Activates partial ragdoll physics for the specified bone.
        /// </summary>
        /// <param name="boneName">Name of the bone to activate ragdoll physics for</param>
        private void ActivatePartialRagdoll(string boneName)
        {
            ragdollSetup.SetPartialRagdoll(boneName);
        }
        
        /// <summary>
        /// Applies the calculated impact force to the specified bone.
        /// </summary>
        /// <param name="boneName">Name of the target bone</param>
        /// <param name="hitForce">Raw hit force vector</param>
        private void ApplyImpactForce(string boneName, Vector3 hitForce)
        {
            Vector3 scaledForce = hitForce * forceMultiplier;
            ragdollSetup.ApplyForceToBone(boneName, scaledForce);
        }
        
        /// <summary>
        /// Schedules the blend recovery process, cancelling any previous blend operation.
        /// </summary>
        private void ScheduleBlendRecovery()
        {
            CancelPreviousBlend();
            blendCoroutine = StartCoroutine(DelayedBlendCoroutine());
        }
        
        /// <summary>
        /// Cancels any currently running blend coroutine.
        /// </summary>
        private void CancelPreviousBlend()
        {
            if (blendCoroutine != null)
            {
                StopCoroutine(blendCoroutine);
                blendCoroutine = null;
            }
        }
        
        /// <summary>
        /// Coroutine that waits for the specified delay before initiating ragdoll blend back to animation.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator DelayedBlendCoroutine()
        {
            yield return new WaitForSeconds(blendDelay);
            
            if (ragdollBlend != null)
            {
                ragdollBlend.StartBlend();
            }
            
            blendCoroutine = null;
        }
        
        #endregion
    }
}
