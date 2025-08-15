using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles creation, enabling, and disabling of ragdoll physics.
/// Supports partial ragdoll activation for hit reactions.
/// All references must be assigned via Inspector; no GetComponent calls.
/// </summary>
public class RagdollSetup : MonoBehaviour
{
    [System.Serializable]
    public class RagdollBone
    {
        [Tooltip("Unique name for this bone (e.g., RightArm)")]
        public string BoneName;

        [Tooltip("Assign the bone transform manually.")]
        public Transform BoneTransform;

        [Tooltip("Assign the Rigidbody manually.")]
        public Rigidbody Rigidbody;

        [Tooltip("Assign the Collider manually.")]
        public Collider Collider;

        [Tooltip("Assign the Joint manually if any (optional).")]
        public Joint Joint;

        [HideInInspector] public bool WasKinematic;
    }

    [Header("Ragdoll Bones")]
    [Tooltip("List of all bones that can be ragdolled. Assign manually.")]
    [SerializeField] private List<RagdollBone> ragdollBones = new List<RagdollBone>();

    [Header("References")]
    [Tooltip("Assign Animator manually.")]
    [SerializeField] private Animator animator;

    [Tooltip("Start in animation (kinematic) mode.")]
    [SerializeField] private bool startKinematic = true;

    private bool isRagdollActive = false;

    #region Properties
    public Animator Animator => animator;
    public List<RagdollBone> RagdollBones => ragdollBones;
    public bool IsRagdollActive => isRagdollActive;
    #endregion

    void Awake()
    {
        CacheInitialStates();
        SetRagdollActive(!startKinematic);
    }

    /// <summary>
    /// Saves initial Rigidbody states so we can restore them later.
    /// </summary>
    private void CacheInitialStates()
    {
        foreach (var bone in ragdollBones)
        {
            if (bone.Rigidbody != null)
                bone.WasKinematic = bone.Rigidbody.isKinematic;
        }
    }

    /// <summary>
    /// Fully enables or disables ragdoll physics.
    /// </summary>
    public void SetRagdollActive(bool active)
    {
        isRagdollActive = active;
        if (animator != null)
            animator.enabled = !active;

        foreach (var bone in ragdollBones)
        {
            if (bone.Rigidbody == null || bone.Collider == null) continue;
            bone.Rigidbody.isKinematic = !active;
            bone.Collider.enabled = active;
        }
    }

    /// <summary>
    /// Enables ragdoll only for a specific bone and its children.
    /// </summary>
    public void SetPartialRagdoll(string targetBoneName)
    {
        if (animator != null)
            animator.enabled = true; // Keep Animator active for unaffected bones

        isRagdollActive = true;

        foreach (var bone in ragdollBones)
        {
            bool shouldRagdoll = bone.BoneName == targetBoneName ||
                                 IsChildOf(targetBoneName, bone.BoneTransform);

            if (bone.Rigidbody != null) bone.Rigidbody.isKinematic = !shouldRagdoll;
            if (bone.Collider != null) bone.Collider.enabled = shouldRagdoll;
        }
    }

    /// <summary>
    /// Disables ragdoll but keeps current pose for blending.
    /// </summary>
    public void DisableRagdollKeepPose()
    {
        isRagdollActive = false;
        foreach (var bone in ragdollBones)
        {
            if (bone.Rigidbody != null) bone.Rigidbody.isKinematic = true;
            if (bone.Collider != null) bone.Collider.enabled = false;
        }
    }

    /// <summary>
    /// Apply force to a specific bone for impact reaction.
    /// </summary>
    public void ApplyForceToBone(string boneName, Vector3 force, ForceMode mode = ForceMode.Impulse)
    {
        foreach (var bone in ragdollBones)
        {
            if (bone.BoneName == boneName && bone.Rigidbody != null)
            {
                bone.Rigidbody.AddForce(force, mode);
                return;
            }
        }
    }

    /// <summary>
    /// Checks if a bone is a child of a given target bone.
    /// </summary>
    private bool IsChildOf(string targetName, Transform bone)
    {
        Transform t = bone;
        while (t != null)
        {
            if (t.name == targetName) return true;
            t = t.parent;
        }
        return false;
    }
}
