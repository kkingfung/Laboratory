using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Provides smooth blending functionality for transitioning ragdoll characters back to animated poses.
    /// Works in conjunction with RagdollSetup to create seamless transitions from physics to animation.
    /// All component references must be manually assigned via Inspector.
    /// </summary>
    public class RagdollBlend : MonoBehaviour
    {
        #region Fields
        
        [Header("Required Component References")]
        [Tooltip("RagdollSetup component containing the ragdoll bone configuration.")]
        [SerializeField] private RagdollSetup ragdollSetup;
        
        [Header("Blend Settings")]
        [Tooltip("Duration in seconds for blending from ragdoll pose back to animation.")]
        [SerializeField] private float blendDuration = 0.3f;
        
        /// <summary>
        /// Flag indicating whether a blend operation is currently in progress.
        /// </summary>
        private bool isBlending = false;
        
        /// <summary>
        /// Current elapsed time since the blend operation started.
        /// </summary>
        private float blendTimer = 0f;
        
        /// <summary>
        /// Cached ragdoll pose data captured at the start of the blend process.
        /// </summary>
        private Dictionary<Transform, TransformData> ragdollPose = new Dictionary<Transform, TransformData>();
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets a value indicating whether a blend operation is currently active.
        /// </summary>
        public bool IsBlending => isBlending;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Processes the blend interpolation during each frame when blending is active.
        /// Uses LateUpdate to ensure animation has been applied before blending.
        /// </summary>
        private void LateUpdate()
        {
            if (isBlending)
            {
                ProcessBlendUpdate();
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Initiates the blend process from current ragdoll pose back to animation.
        /// Captures the current ragdoll transforms and begins smooth interpolation to animated pose.
        /// </summary>
        public void StartBlend()
        {
            if (isBlending || !ValidateComponents()) return;
            
            InitializeBlendProcess();
        }
        
        /// <summary>
        /// Stops any active blend process and returns control to the animation system.
        /// </summary>
        public void StopBlend()
        {
            if (!isBlending) return;
            
            TerminateBlendProcess();
        }
        
        #endregion
        
        #region Private Methods - Validation
        
        /// <summary>
        /// Validates that all required components are properly assigned.
        /// </summary>
        /// <returns>True if all required components are valid</returns>
        private bool ValidateComponents()
        {
            return ragdollSetup != null;
        }
        
        #endregion
        
        #region Private Methods - Blend Initialization
        
        /// <summary>
        /// Initializes the blend process by capturing poses and configuring timing.
        /// </summary>
        private void InitializeBlendProcess()
        {
            CaptureCurrentRagdollPose();
            ConfigureRagdollForBlending();
            ResetBlendTiming();
            EnableAnimatorForBlending();
            
            isBlending = true;
        }
        
        /// <summary>
        /// Captures the current transform state of all ragdoll bones.
        /// </summary>
        private void CaptureCurrentRagdollPose()
        {
            ragdollPose.Clear();
            
            foreach (var bone in ragdollSetup.RagdollBones)
            {
                if (bone.BoneTransform != null)
                {
                    ragdollPose[bone.BoneTransform] = new TransformData(bone.BoneTransform);
                }
            }
        }
        
        /// <summary>
        /// Configures the ragdoll system for blending by disabling physics while preserving pose.
        /// </summary>
        private void ConfigureRagdollForBlending()
        {
            ragdollSetup.DisableRagdollKeepPose();
        }
        
        /// <summary>
        /// Resets the blend timer to start timing from zero.
        /// </summary>
        private void ResetBlendTiming()
        {
            blendTimer = 0f;
        }
        
        /// <summary>
        /// Enables the Animator component to provide target poses for blending.
        /// </summary>
        private void EnableAnimatorForBlending()
        {
            if (ragdollSetup.Animator != null)
            {
                ragdollSetup.Animator.enabled = true;
            }
        }
        
        #endregion
        
        #region Private Methods - Blend Processing
        
        /// <summary>
        /// Processes blend interpolation for the current frame.
        /// </summary>
        private void ProcessBlendUpdate()
        {
            UpdateBlendTiming();
            float blendProgress = CalculateBlendProgress();
            
            ApplyBlendInterpolation(blendProgress);
            
            if (IsBlendComplete(blendProgress))
            {
                CompleteBlendProcess();
            }
        }
        
        /// <summary>
        /// Updates the blend timer with the current frame's delta time.
        /// </summary>
        private void UpdateBlendTiming()
        {
            blendTimer += Time.deltaTime;
        }
        
        /// <summary>
        /// Calculates the current blend progress as a normalized value.
        /// </summary>
        /// <returns>Blend progress from 0.0 to 1.0</returns>
        private float CalculateBlendProgress()
        {
            return Mathf.Clamp01(blendTimer / blendDuration);
        }
        
        /// <summary>
        /// Applies interpolation between ragdoll and animation poses for all bones.
        /// </summary>
        /// <param name="blendProgress">Current blend progress (0.0 to 1.0)</param>
        private void ApplyBlendInterpolation(float blendProgress)
        {
            foreach (var bone in ragdollSetup.RagdollBones)
            {
                if (ShouldProcessBone(bone))
                {
                    InterpolateBoneTransform(bone.BoneTransform, blendProgress);
                }
            }
        }
        
        /// <summary>
        /// Determines if a bone should be processed during blending.
        /// </summary>
        /// <param name="bone">Ragdoll bone data to check</param>
        /// <returns>True if the bone should be processed</returns>
        private bool ShouldProcessBone(RagdollSetup.RagdollBone bone)
        {
            return bone.BoneTransform != null && ragdollPose.ContainsKey(bone.BoneTransform);
        }
        
        /// <summary>
        /// Interpolates a single bone's transform between ragdoll and animation poses.
        /// </summary>
        /// <param name="boneTransform">Transform to interpolate</param>
        /// <param name="blendProgress">Current blend progress</param>
        private void InterpolateBoneTransform(Transform boneTransform, float blendProgress)
        {
            TransformData startData = ragdollPose[boneTransform];
            TransformData targetData = new TransformData(boneTransform); // Current animation pose
            
            // Apply interpolated transform
            boneTransform.localPosition = Vector3.Lerp(startData.LocalPosition, targetData.LocalPosition, blendProgress);
            boneTransform.localRotation = Quaternion.Slerp(startData.LocalRotation, targetData.LocalRotation, blendProgress);
        }
        
        /// <summary>
        /// Determines if the blend process has completed.
        /// </summary>
        /// <param name="blendProgress">Current blend progress</param>
        /// <returns>True if blending is complete</returns>
        private bool IsBlendComplete(float blendProgress)
        {
            return blendProgress >= 1f;
        }
        
        /// <summary>
        /// Completes the blend process and cleans up resources.
        /// </summary>
        private void CompleteBlendProcess()
        {
            TerminateBlendProcess();
        }
        
        #endregion
        
        #region Private Methods - Cleanup
        
        /// <summary>
        /// Terminates the active blend process and cleans up cached data.
        /// </summary>
        private void TerminateBlendProcess()
        {
            isBlending = false;
            ragdollPose.Clear();
        }
        
        #endregion
        
        #region Helper Structures
        
        /// <summary>
        /// Helper structure for storing local transform data during the blend process.
        /// </summary>
        private struct TransformData
        {
            /// <summary>
            /// Local position of the transform.
            /// </summary>
            public Vector3 LocalPosition;
            
            /// <summary>
            /// Local rotation of the transform.
            /// </summary>
            public Quaternion LocalRotation;
            
            /// <summary>
            /// Initializes a new instance of the TransformData struct with data from the specified transform.
            /// </summary>
            /// <param name="transform">Transform to copy data from</param>
            public TransformData(Transform transform)
            {
                LocalPosition = transform.localPosition;
                LocalRotation = transform.localRotation;
            }
        }
        
        #endregion
    }
}
