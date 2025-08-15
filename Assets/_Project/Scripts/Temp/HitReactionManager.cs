using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles hit reactions for a character using RagdollSetup and RagdollBlend.
/// Supports partial ragdoll, force application, and smooth recovery.
/// All references must be assigned in the Inspector; no GetComponent calls.
/// </summary>
public class HitReactionManager : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Assign the RagdollSetup component manually.")]
    [SerializeField] private RagdollSetup ragdollSetup;

    [Tooltip("Assign the RagdollBlend component manually.")]
    [SerializeField] private RagdollBlend ragdollBlend;

    [Header("Settings")]
    [Tooltip("Time in seconds before starting blend back to animation.")]
    [SerializeField] private float blendDelay = 0.5f;

    [Tooltip("Multiplier for impact force applied to ragdoll bone.")]
    [SerializeField] private float forceMultiplier = 1.0f;

    private Coroutine blendCoroutine;

    /// <summary>
    /// Call this when a hit occurs.
    /// </summary>
    /// <param name="boneName">The bone that was hit (should match RagdollSetup bone names)</param>
    /// <param name="hitForce">World-space force vector applied to bone</param>
    public void OnHit(string boneName, Vector3 hitForce)
    {
        if (ragdollSetup == null || ragdollBlend == null) return;

        // Activate partial ragdoll for hit bone
        ragdollSetup.SetPartialRagdoll(boneName);

        // Apply impact force
        ragdollSetup.ApplyForceToBone(boneName, hitForce * forceMultiplier);

        // Start blend coroutine (cancel previous if exists)
        if (blendCoroutine != null)
            StopCoroutine(blendCoroutine);

        blendCoroutine = StartCoroutine(DelayedBlend());
    }

    /// <summary>
    /// Waits for blendDelay seconds before starting ragdoll blend back to animation.
    /// </summary>
    private IEnumerator DelayedBlend()
    {
        yield return new WaitForSeconds(blendDelay);

        if (ragdollBlend != null)
            ragdollBlend.StartBlend();

        blendCoroutine = null;
    }
}
