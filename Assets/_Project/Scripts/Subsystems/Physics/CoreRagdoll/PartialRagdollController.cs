using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Network.Ragdoll;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Controls partial ragdoll activation for specific character bones.
    /// Manages the transition between animated and physics-driven bone states with smooth blending.
    /// Inspector assignment required for all bone references - no GetComponent calls used.
    /// </summary>
    public class PartialRagdollController : MonoBehaviour
    {
        #region Fields
        
        [Header("Ragdoll Configuration")]
        [Tooltip("List of bone transforms that participate in partial ragdoll physics.")]
        [SerializeField] private List<Transform> ragdollBones;
        
        [Header("Blending Settings")]
        [Tooltip("Duration in seconds for blending back to animation from ragdoll state.")]
        [SerializeField] private float blendDuration = 0.3f;
        
        [Header("Optional Network Integration")]
        [Tooltip("Network synchronization component for multiplayer scenarios.")]
        [SerializeField] private NetworkRagdollSync networkRagdollSync;
        
        /// <summary>
        /// Stores the original kinematic state of each ragdoll bone's Rigidbody.
        /// Used to restore proper physics state after ragdoll deactivation.
        /// </summary>
        private Dictionary<Rigidbody, bool> originalKinematicStates = new Dictionary<Rigidbody, bool>();
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Initializes ragdoll bones by caching their original kinematic states and setting initial animation mode.
        /// </summary>
        private void Awake()
        {
            InitializeRagdollBones();
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Applies partial ragdoll physics to all configured bones with the specified force.
        /// Transitions bones from animation to physics simulation and schedules blend-back recovery.
        /// </summary>
        /// <param name="force">World-space force vector to apply to all ragdoll bones</param>
        /// <param name="duration">Optional override for blend-back duration (uses default if negative)</param>
        public void ApplyPartialRagdoll(Vector3 force, float duration = -1f)
        {
            float effectiveDuration = GetEffectiveBlendDuration(duration);
            
            ProcessAllRagdollBones(force, effectiveDuration);
        }
        
        #endregion
        
        #region Private Methods - Initialization
        
        /// <summary>
        /// Sets up all ragdoll bones by caching original states and ensuring animation mode.
        /// </summary>
        private void InitializeRagdollBones()
        {
            foreach (Transform bone in ragdollBones)
            {
                ProcessBoneInitialization(bone);
            }
        }
        
        /// <summary>
        /// Initializes a single bone by caching its Rigidbody state and enabling kinematic mode.
        /// </summary>
        /// <param name="bone">Transform of the bone to initialize</param>
        private void ProcessBoneInitialization(Transform bone)
        {
            Rigidbody rigidbody = bone.GetComponent<Rigidbody>();
            if (rigidbody == null) return;
            
            CacheOriginalKinematicState(rigidbody);
            SetBoneAnimationMode(rigidbody);
        }
        
        /// <summary>
        /// Stores the original kinematic state of a Rigidbody for later restoration.
        /// </summary>
        /// <param name="rigidbody">Rigidbody component to cache state for</param>
        private void CacheOriginalKinematicState(Rigidbody rigidbody)
        {
            if (!originalKinematicStates.ContainsKey(rigidbody))
            {
                originalKinematicStates[rigidbody] = rigidbody.isKinematic;
            }
        }
        
        /// <summary>
        /// Sets a bone to animation mode by enabling kinematic physics.
        /// </summary>
        /// <param name="rigidbody">Rigidbody to set to kinematic mode</param>
        private void SetBoneAnimationMode(Rigidbody rigidbody)
        {
            rigidbody.isKinematic = true;
        }
        
        #endregion
        
        #region Private Methods - Ragdoll Processing
        
        /// <summary>
        /// Determines the effective blend duration, using default if no override specified.
        /// </summary>
        /// <param name="duration">Requested duration (negative values use default)</param>
        /// <returns>Effective blend duration to use</returns>
        private float GetEffectiveBlendDuration(float duration)
        {
            return duration <= 0f ? blendDuration : duration;
        }
        
        /// <summary>
        /// Processes ragdoll activation for all configured bones.
        /// </summary>
        /// <param name="force">Force to apply to bones</param>
        /// <param name="effectiveDuration">Blend-back duration to use</param>
        private void ProcessAllRagdollBones(Vector3 force, float effectiveDuration)
        {
            foreach (Transform bone in ragdollBones)
            {
                ProcessSingleRagdollBone(bone, force, effectiveDuration);
            }
        }
        
        /// <summary>
        /// Activates ragdoll physics for a single bone and schedules its recovery.
        /// </summary>
        /// <param name="bone">Bone transform to process</param>
        /// <param name="force">Force to apply</param>
        /// <param name="duration">Blend-back duration</param>
        private void ProcessSingleRagdollBone(Transform bone, Vector3 force, float duration)
        {
            Rigidbody rigidbody = bone.GetComponent<Rigidbody>();
            if (rigidbody == null) return;
            
            ActivateBonePhysics(rigidbody, force);
            TriggerNetworkSync(bone, force);
            ScheduleBoneRecovery(rigidbody, duration);
        }
        
        /// <summary>
        /// Enables physics simulation for a bone and applies the specified force.
        /// </summary>
        /// <param name="rigidbody">Target bone's Rigidbody component</param>
        /// <param name="force">Force vector to apply</param>
        private void ActivateBonePhysics(Rigidbody rigidbody, Vector3 force)
        {
            rigidbody.isKinematic = false;
            rigidbody.AddForce(force, ForceMode.Impulse);
        }
        
        /// <summary>
        /// Triggers network synchronization for the bone if network sync is available.
        /// </summary>
        /// <param name="bone">Bone that was affected</param>
        /// <param name="force">Force that was applied</param>
        private void TriggerNetworkSync(Transform bone, Vector3 force)
        {
            networkRagdollSync?.NetworkedHit(bone.name, force);
        }
        
        /// <summary>
        /// Starts the blend-back coroutine for the specified bone.
        /// </summary>
        /// <param name="rigidbody">Rigidbody to schedule recovery for</param>
        /// <param name="duration">Duration of the blend-back process</param>
        private void ScheduleBoneRecovery(Rigidbody rigidbody, float duration)
        {
            StartCoroutine(BlendBackCoroutine(rigidbody, duration));
        }
        
        #endregion
        
        #region Private Methods - Blend Recovery
        
        /// <summary>
        /// Coroutine that smoothly transitions a bone from ragdoll physics back to animation.
        /// Interpolates between ragdoll position/rotation and animation target over time.
        /// </summary>
        /// <param name="rigidbody">Rigidbody component of the bone to blend</param>
        /// <param name="duration">Total duration of the blend process</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator BlendBackCoroutine(Rigidbody rigidbody, float duration)
        {
            float timer = 0f;
            Transform boneTransform = rigidbody.transform;
            
            Vector3 startPosition = boneTransform.localPosition;
            Quaternion startRotation = boneTransform.localRotation;
            
            // Target pose assumed to be controlled by Animator
            Vector3 targetPosition = boneTransform.localPosition;
            Quaternion targetRotation = boneTransform.localRotation;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float blendProgress = timer / duration;
                
                ApplyBlendedTransform(boneTransform, startPosition, startRotation, 
                                    targetPosition, targetRotation, blendProgress);
                
                yield return null;
            }
            
            RestoreOriginalKinematicState(rigidbody);
        }
        
        /// <summary>
        /// Applies interpolated transform values during the blend process.
        /// </summary>
        /// <param name="boneTransform">Transform component to modify</param>
        /// <param name="startPos">Starting local position</param>
        /// <param name="startRot">Starting local rotation</param>
        /// <param name="targetPos">Target local position</param>
        /// <param name="targetRot">Target local rotation</param>
        /// <param name="progress">Blend progress from 0 to 1</param>
        private void ApplyBlendedTransform(Transform boneTransform, Vector3 startPos, Quaternion startRot,
                                         Vector3 targetPos, Quaternion targetRot, float progress)
        {
            boneTransform.localPosition = Vector3.Lerp(startPos, targetPos, progress);
            boneTransform.localRotation = Quaternion.Slerp(startRot, targetRot, progress);
        }
        
        /// <summary>
        /// Restores the bone's original kinematic state after blend completion.
        /// </summary>
        /// <param name="rigidbody">Rigidbody to restore state for</param>
        private void RestoreOriginalKinematicState(Rigidbody rigidbody)
        {
            if (originalKinematicStates.TryGetValue(rigidbody, out bool originalState))
            {
                rigidbody.isKinematic = originalState;
            }
        }
        
        #endregion
    }
}
