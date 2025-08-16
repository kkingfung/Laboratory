using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Handles creation, enabling, and disabling of ragdoll physics.
    /// Supports partial ragdoll activation for hit reactions and provides comprehensive bone management.
    /// All references must be assigned via Inspector to avoid runtime GetComponent calls.
    /// </summary>
    public class RagdollSetup : MonoBehaviour
    {
        #region Nested Types

        /// <summary>
        /// Configuration data for individual ragdoll bones
        /// </summary>
        [System.Serializable]
        public class RagdollBone
        {
            [Tooltip("Unique identifier for this bone (e.g., RightArm, Head, Spine)")]
            public string BoneName;

            [Tooltip("Transform component of this bone - assign manually")]
            public Transform BoneTransform;

            [Tooltip("Rigidbody component of this bone - assign manually")]
            public Rigidbody Rigidbody;

            [Tooltip("Collider component of this bone - assign manually")]
            public Collider Collider;

            [Tooltip("Joint component of this bone if present (optional)")]
            public Joint Joint;

            [Tooltip("Whether this bone should be affected by partial ragdoll activation")]
            public bool AffectedByPartialRagdoll = true;

            // Runtime state
            [HideInInspector] 
            public bool WasKinematic;
            
            [HideInInspector] 
            public bool WasColliderEnabled;

            /// <summary>
            /// Validate that required components are assigned
            /// </summary>
            public bool IsValid => BoneTransform != null && Rigidbody != null && Collider != null;
        }

        #endregion

        #region Fields

        [Header("Ragdoll Configuration")]
        [SerializeField]
        [Tooltip("List of all bones that participate in ragdoll physics - assign manually")]
        private List<RagdollBone> ragdollBones = new List<RagdollBone>();

        [Header("Animation Integration")]
        [SerializeField]
        [Tooltip("Character animator component - assign manually")]
        private Animator animator;

        [SerializeField]
        [Tooltip("Start in kinematic (animated) mode rather than ragdoll mode")]
        private bool startKinematic = true;

        [Header("Partial Ragdoll Settings")]
        [SerializeField]
        [Tooltip("Duration for partial ragdoll effects before returning to animation")]
        private float partialRagdollDuration = 2f;

        [SerializeField]
        [Tooltip("Whether to blend back to animation smoothly after partial ragdoll")]
        private bool blendBackToAnimation = true;

        // Runtime state
        private bool isRagdollActive = false;
        private bool isPartialRagdollActive = false;
        private float partialRagdollTimer = 0f;
        private Dictionary<string, RagdollBone> boneMap = new Dictionary<string, RagdollBone>();

        #endregion

        #region Properties

        /// <summary>
        /// Character animator component
        /// </summary>
        public Animator Animator => animator;

        /// <summary>
        /// Read-only access to all ragdoll bones
        /// </summary>
        public IReadOnlyList<RagdollBone> RagdollBones => ragdollBones.AsReadOnly();

        /// <summary>
        /// Whether full ragdoll physics is currently active
        /// </summary>
        public bool IsRagdollActive => isRagdollActive;

        /// <summary>
        /// Whether partial ragdoll is currently active
        /// </summary>
        public bool IsPartialRagdollActive => isPartialRagdollActive;

        /// <summary>
        /// Total number of configured ragdoll bones
        /// </summary>
        public int BoneCount => ragdollBones.Count;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize ragdoll system and cache initial states
        /// </summary>
        private void Awake()
        {
            InitializeSystem();
        }

        /// <summary>
        /// Handle partial ragdoll timer updates
        /// </summary>
        private void Update()
        {
            UpdatePartialRagdollTimer();
        }

        /// <summary>
        /// Validate configuration in editor
        /// </summary>
        private void OnValidate()
        {
            ValidateConfiguration();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Fully enable or disable ragdoll physics for all bones
        /// </summary>
        /// <param name="active">Whether ragdoll should be active</param>
        public void SetRagdollActive(bool active)
        {
            isRagdollActive = active;
            isPartialRagdollActive = false;
            partialRagdollTimer = 0f;

            SetAnimatorEnabled(!active);
            ApplyRagdollStateToAllBones(active);
        }

        /// <summary>
        /// Enable ragdoll physics only for a specific bone and its children
        /// </summary>
        /// <param name="targetBoneName">Name of the bone to activate ragdoll for</param>
        public void SetPartialRagdoll(string targetBoneName)
        {
            SetPartialRagdoll(targetBoneName, partialRagdollDuration);
        }

        /// <summary>
        /// Enable ragdoll physics for a specific bone with custom duration
        /// </summary>
        /// <param name="targetBoneName">Name of the bone to activate ragdoll for</param>
        /// <param name="duration">Duration to maintain partial ragdoll state</param>
        public void SetPartialRagdoll(string targetBoneName, float duration)
        {
            isPartialRagdollActive = true;
            partialRagdollTimer = duration;
            
            // Keep animator active for unaffected bones
            SetAnimatorEnabled(true);

            ApplyPartialRagdollState(targetBoneName);
        }

        /// <summary>
        /// Disable ragdoll while maintaining current pose for smooth transitions
        /// </summary>
        public void DisableRagdollKeepPose()
        {
            isRagdollActive = false;
            isPartialRagdollActive = false;
            partialRagdollTimer = 0f;

            foreach (var bone in ragdollBones)
            {
                if (!bone.IsValid) continue;
                
                bone.Rigidbody.isKinematic = true;
                bone.Collider.enabled = false;
            }
        }

        /// <summary>
        /// Apply force to a specific bone for impact reactions
        /// </summary>
        /// <param name="boneName">Name of the bone to apply force to</param>
        /// <param name="force">Force vector to apply</param>
        /// <param name="mode">Force mode for physics calculation</param>
        /// <returns>True if force was applied successfully</returns>
        public bool ApplyForceToBone(string boneName, Vector3 force, ForceMode mode = ForceMode.Impulse)
        {
            if (boneMap.TryGetValue(boneName, out RagdollBone bone) && bone.Rigidbody != null)
            {
                bone.Rigidbody.AddForce(force, mode);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Apply force at a specific position on a bone
        /// </summary>
        /// <param name="boneName">Name of the bone to apply force to</param>
        /// <param name="force">Force vector to apply</param>
        /// <param name="position">World position to apply force at</param>
        /// <returns>True if force was applied successfully</returns>
        public bool ApplyForceAtPosition(string boneName, Vector3 force, Vector3 position)
        {
            if (boneMap.TryGetValue(boneName, out RagdollBone bone) && bone.Rigidbody != null)
            {
                bone.Rigidbody.AddForceAtPosition(force, position);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Get bone configuration by name
        /// </summary>
        /// <param name="boneName">Name of the bone to retrieve</param>
        /// <returns>Bone configuration or null if not found</returns>
        public RagdollBone GetBone(string boneName)
        {
            boneMap.TryGetValue(boneName, out RagdollBone bone);
            return bone;
        }

        /// <summary>
        /// Check if a specific bone exists in the configuration
        /// </summary>
        /// <param name="boneName">Name of the bone to check</param>
        /// <returns>True if bone exists</returns>
        public bool HasBone(string boneName)
        {
            return boneMap.ContainsKey(boneName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialize the ragdoll system and prepare for runtime use
        /// </summary>
        private void InitializeSystem()
        {
            BuildBoneMap();
            CacheInitialStates();
            SetRagdollActive(!startKinematic);
        }

        /// <summary>
        /// Build dictionary for fast bone lookup by name
        /// </summary>
        private void BuildBoneMap()
        {
            boneMap.Clear();
            
            foreach (var bone in ragdollBones)
            {
                if (!string.IsNullOrEmpty(bone.BoneName))
                {
                    boneMap[bone.BoneName] = bone;
                }
            }
        }

        /// <summary>
        /// Save initial component states for restoration later
        /// </summary>
        private void CacheInitialStates()
        {
            foreach (var bone in ragdollBones)
            {
                if (!bone.IsValid) continue;
                
                bone.WasKinematic = bone.Rigidbody.isKinematic;
                bone.WasColliderEnabled = bone.Collider.enabled;
            }
        }

        /// <summary>
        /// Validate system configuration and log warnings
        /// </summary>
        private void ValidateConfiguration()
        {
            for (int i = 0; i < ragdollBones.Count; i++)
            {
                var bone = ragdollBones[i];
                if (!bone.IsValid)
                {
                    Debug.LogWarning($"[RagdollSetup] Bone at index {i} is missing required components.", this);
                }
            }
        }

        /// <summary>
        /// Enable or disable the animator component
        /// </summary>
        /// <param name="enabled">Whether animator should be enabled</param>
        private void SetAnimatorEnabled(bool enabled)
        {
            if (animator != null)
            {
                animator.enabled = enabled;
            }
        }

        /// <summary>
        /// Apply ragdoll state to all bones
        /// </summary>
        /// <param name="ragdollActive">Whether ragdoll physics should be active</param>
        private void ApplyRagdollStateToAllBones(bool ragdollActive)
        {
            foreach (var bone in ragdollBones)
            {
                if (!bone.IsValid) continue;
                
                bone.Rigidbody.isKinematic = !ragdollActive;
                bone.Collider.enabled = ragdollActive;
            }
        }

        /// <summary>
        /// Apply partial ragdoll state to specific bone and its children
        /// </summary>
        /// <param name="targetBoneName">Name of the target bone</param>
        private void ApplyPartialRagdollState(string targetBoneName)
        {
            foreach (var bone in ragdollBones)
            {
                if (!bone.IsValid || !bone.AffectedByPartialRagdoll) continue;

                bool shouldRagdoll = bone.BoneName == targetBoneName || IsChildOfBone(targetBoneName, bone.BoneTransform);
                
                bone.Rigidbody.isKinematic = !shouldRagdoll;
                bone.Collider.enabled = shouldRagdoll;
            }
        }

        /// <summary>
        /// Update partial ragdoll timer and disable when expired
        /// </summary>
        private void UpdatePartialRagdollTimer()
        {
            if (!isPartialRagdollActive) return;

            partialRagdollTimer -= Time.deltaTime;
            
            if (partialRagdollTimer <= 0f)
            {
                DisablePartialRagdoll();
            }
        }

        /// <summary>
        /// Disable partial ragdoll and return to full animation
        /// </summary>
        private void DisablePartialRagdoll()
        {
            isPartialRagdollActive = false;
            partialRagdollTimer = 0f;

            if (blendBackToAnimation)
            {
                // Smoothly return to kinematic state
                foreach (var bone in ragdollBones)
                {
                    if (!bone.IsValid) continue;
                    
                    bone.Rigidbody.isKinematic = true;
                    bone.Collider.enabled = false;
                }
            }
        }

        /// <summary>
        /// Check if a bone transform is a child of the specified target bone
        /// </summary>
        /// <param name="targetName">Name of the target parent bone</param>
        /// <param name="bone">Transform to check for parent relationship</param>
        /// <returns>True if bone is a child of target</returns>
        private bool IsChildOfBone(string targetName, Transform bone)
        {
            if (!boneMap.TryGetValue(targetName, out RagdollBone targetBone) || targetBone.BoneTransform == null)
                return false;

            Transform current = bone.parent;
            while (current != null)
            {
                if (current == targetBone.BoneTransform)
                    return true;
                current = current.parent;
            }
            
            return false;
        }

        #endregion
    }
}
