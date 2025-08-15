using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls partial ragdoll for a character.
/// Assign specific bones in the inspector to become ragdolled on hit.
/// </summary>
public class PartialRagdollController : MonoBehaviour
{
    [Header("Ragdoll Bones")]
    [Tooltip("Bones that will enter ragdoll when hit.")]
    [SerializeField] private List<Transform> ragdollBones;

    [Header("Settings")]
    [Tooltip("Blend duration back to animation in seconds.")]
    [SerializeField] private float blendDuration = 0.3f;

    [Tooltip("Optional network sync component.")]
    [SerializeField] private NetworkRagdollSync networkRagdollSync;

    // Store original kinematic state
    private Dictionary<Rigidbody, bool> originalKinematicStates = new Dictionary<Rigidbody, bool>();

    private void Awake()
    {
        foreach (var bone in ragdollBones)
        {
            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb != null && !originalKinematicStates.ContainsKey(rb))
            {
                originalKinematicStates[rb] = rb.isKinematic;
                rb.isKinematic = true; // Keep bones animated initially
            }
        }
    }

    /// <summary>
    /// Apply ragdoll force to specific bones.
    /// </summary>
    public void ApplyPartialRagdoll(Vector3 force, float duration = -1f)
    {
        if (duration <= 0f) duration = blendDuration;

        foreach (var bone in ragdollBones)
        {
            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb == null) continue;

            // Enable physics
            rb.isKinematic = false;
            rb.AddForce(force, ForceMode.Impulse);

            // Optional network sync
            networkRagdollSync?.NetworkedHit(bone.name, force);

            // Schedule blend back
            StartCoroutine(BlendBackCoroutine(rb, duration));
        }
    }

    private System.Collections.IEnumerator BlendBackCoroutine(Rigidbody rb, float duration)
    {
        float timer = 0f;
        Vector3 startPos = rb.transform.localPosition;
        Quaternion startRot = rb.transform.localRotation;

        Vector3 endPos = rb.transform.localPosition; // Assuming Animator will control final pose
        Quaternion endRot = rb.transform.localRotation;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            rb.transform.localPosition = Vector3.Lerp(startPos, endPos, t);
            rb.transform.localRotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        // Restore kinematic state
        if (originalKinematicStates.ContainsKey(rb))
        {
            rb.isKinematic = originalKinematicStates[rb];
        }
    }
}
