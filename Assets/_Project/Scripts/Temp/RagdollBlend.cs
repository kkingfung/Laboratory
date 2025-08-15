using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Smoothly blends a ragdoll character back to Animator pose.
/// Works with RagdollSetup.cs.
/// All references must be assigned in Inspector; no GetComponent calls.
/// </summary>
public class RagdollBlend : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Assign the RagdollSetup component manually.")]
    [SerializeField] private RagdollSetup ragdollSetup;

    [Tooltip("Blend duration from ragdoll to animation in seconds.")]
    [SerializeField] private float blendDuration = 0.3f;

    private bool isBlending = false;
    private float blendTimer = 0f;

    // Stores bone transforms at ragdoll pose
    private Dictionary<Transform, TransformData> ragdollPose = new Dictionary<Transform, TransformData>();

    /// <summary>
    /// Starts blending the ragdoll back to the Animator pose.
    /// </summary>
    public void StartBlend()
    {
        if (isBlending || ragdollSetup == null) return;

        // Capture current ragdoll pose
        ragdollPose.Clear();
        foreach (var bone in ragdollSetup.RagdollBones)
        {
            ragdollPose[bone.BoneTransform] = new TransformData(bone.BoneTransform);
        }

        // Disable ragdoll physics but keep current pose
        ragdollSetup.DisableRagdollKeepPose();

        blendTimer = 0f;
        isBlending = true;

        // Enable Animator for blending
        if (ragdollSetup.Animator != null)
            ragdollSetup.Animator.enabled = true;
    }

    private void LateUpdate()
    {
        if (!isBlending) return;

        blendTimer += Time.deltaTime;
        float t = Mathf.Clamp01(blendTimer / blendDuration);

        foreach (var bone in ragdollSetup.RagdollBones)
        {
            Transform tr = bone.BoneTransform;
            if (!ragdollPose.ContainsKey(tr)) continue;

            TransformData startData = ragdollPose[tr];
            TransformData targetData = new TransformData(tr); // Animator pose

            tr.localPosition = Vector3.Lerp(startData.LocalPosition, targetData.LocalPosition, t);
            tr.localRotation = Quaternion.Slerp(startData.LocalRotation, targetData.LocalRotation, t);
        }

        if (t >= 1f)
        {
            isBlending = false;
            ragdollPose.Clear();
        }
    }

    /// <summary>
    /// Helper struct to store local transform data.
    /// </summary>
    private struct TransformData
    {
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;

        public TransformData(Transform tr)
        {
            LocalPosition = tr.localPosition;
            LocalRotation = tr.localRotation;
        }
    }
}
