using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Manages the transition from ragdoll state back to animated state.
    /// Provides smooth blending between physics simulation and animation playback.
    /// </summary>
    public class BlendBackSystem : MonoBehaviour
    {
        #region Fields

        [Header("Blend Settings")]
        [SerializeField] private float _blendDuration = 0.5f;
        [SerializeField] private AnimationCurve _blendCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool _useCustomBlendCurve = false;
        [SerializeField] private float _blendStartDelay = 0.1f;

        [Header("Animation Control")]
        [SerializeField] private bool _resumeAnimationOnBlend = true;
        [SerializeField] private float _animationResumeDelay = 0.2f;
        [SerializeField] private bool _syncAnimationTime = true;
        [SerializeField] private float _animationSyncThreshold = 0.1f;

        [Header("Physics Control")]
        [SerializeField] private bool _graduallyDisablePhysics = true;
        [SerializeField] private float _physicsDisableDelay = 0.3f;
        [SerializeField] private bool _maintainMomentum = true;
        [SerializeField] private float _momentumPreservation = 0.7f;

        [Header("Bone Alignment")]
        [SerializeField] private bool _alignBonesOnBlend = true;
        [SerializeField] private float _boneAlignmentSpeed = 5f;
        [SerializeField] private bool _smoothBoneAlignment = true;
        [SerializeField] private float _alignmentThreshold = 0.01f;

        // Runtime state
        private bool _isBlending = false;
        private float _blendProgress = 0f;
        private float _blendStartTime = 0f;
        private Coroutine _blendCoroutine;
        private List<Rigidbody> _ragdollBones = new List<Rigidbody>();
        private List<Transform> _boneTransforms = new List<Transform>();
        private Animator _targetAnimator;
        private bool _wasAnimationEnabled = false;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the system is currently blending back to animation
        /// </summary>
        public bool IsBlending => _isBlending;

        /// <summary>
        /// Current blend progress (0-1)
        /// </summary>
        public float BlendProgress => _blendProgress;

        /// <summary>
        /// Duration of the blend transition
        /// </summary>
        public float BlendDuration => _blendDuration;

        /// <summary>
        /// Whether animation will be resumed after blending
        /// </summary>
        public bool ResumeAnimationOnBlend => _resumeAnimationOnBlend;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeBlendSystem();
        }

        private void Start()
        {
            ValidateConfiguration();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the blend back process from ragdoll to animation
        /// </summary>
        /// <param name="immediate">Whether to start blending immediately or after delay</param>
        public void StartBlendBack(bool immediate = false)
        {
            if (_isBlending) return;

            if (immediate)
            {
                BeginBlendProcess();
            }
            else
            {
                StartCoroutine(DelayedBlendStart());
            }
        }

        /// <summary>
        /// Stops the current blend process
        /// </summary>
        public void StopBlendBack()
        {
            if (!_isBlending) return;

            if (_blendCoroutine != null)
            {
                StopCoroutine(_blendCoroutine);
                _blendCoroutine = null;
            }

            _isBlending = false;
            _blendProgress = 0f;
        }

        /// <summary>
        /// Sets the blend duration
        /// </summary>
        /// <param name="duration">Blend duration in seconds</param>
        public void SetBlendDuration(float duration)
        {
            _blendDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        /// Sets the blend start delay
        /// </summary>
        /// <param name="delay">Delay before blending starts</param>
        public void SetBlendStartDelay(float delay)
        {
            _blendStartDelay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// Sets the animation resume delay
        /// </summary>
        /// <param name="delay">Delay before animation resumes</param>
        public void SetAnimationResumeDelay(float delay)
        {
            _animationResumeDelay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// Sets the bone alignment speed
        /// </summary>
        /// <param name="speed">Bone alignment speed multiplier</param>
        public void SetBoneAlignmentSpeed(float speed)
        {
            _boneAlignmentSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Sets the alignment threshold
        /// </summary>
        /// <param name="threshold">Threshold for considering bones aligned</param>
        public void SetAlignmentThreshold(float threshold)
        {
            _alignmentThreshold = Mathf.Max(0.001f, threshold);
        }

        /// <summary>
        /// Enables or disables momentum preservation
        /// </summary>
        /// <param name="enabled">Whether momentum should be preserved</param>
        public void SetMomentumPreservation(bool enabled)
        {
            _maintainMomentum = enabled;
        }

        /// <summary>
        /// Sets the momentum preservation amount
        /// </summary>
        /// <param name="amount">Momentum preservation factor (0-1)</param>
        public void SetMomentumPreservationAmount(float amount)
        {
            _momentumPreservation = Mathf.Clamp01(amount);
        }

        #endregion

        #region Private Methods

        private void InitializeBlendSystem()
        {
            _targetAnimator = GetComponent<Animator>();
            
            // Cache ragdoll bones
            var rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                if (rb != GetComponent<Rigidbody>()) // Skip root rigidbody
                {
                    _ragdollBones.Add(rb);
                    _boneTransforms.Add(rb.transform);
                }
            }

            Debug.Log($"BlendBackSystem: Initialized with {_ragdollBones.Count} ragdoll bones");
        }

        private void ValidateConfiguration()
        {
            if (_blendDuration <= 0f)
            {
                Debug.LogWarning("BlendBackSystem: Blend duration should be greater than 0");
                _blendDuration = 0.5f;
            }

            if (_blendStartDelay < 0f)
            {
                Debug.LogWarning("BlendBackSystem: Blend start delay should not be negative");
                _blendStartDelay = 0.1f;
            }

            if (_animationResumeDelay < 0f)
            {
                Debug.LogWarning("BlendBackSystem: Animation resume delay should not be negative");
                _animationResumeDelay = 0.2f;
            }

            if (_boneAlignmentSpeed <= 0f)
            {
                Debug.LogWarning("BlendBackSystem: Bone alignment speed should be greater than 0");
                _boneAlignmentSpeed = 5f;
            }

            if (_alignmentThreshold <= 0f)
            {
                Debug.LogWarning("BlendBackSystem: Alignment threshold should be greater than 0");
                _alignmentThreshold = 0.01f;
            }
        }

        private System.Collections.IEnumerator DelayedBlendStart()
        {
            yield return new WaitForSeconds(_blendStartDelay);
            BeginBlendProcess();
        }

        private void BeginBlendProcess()
        {
            if (_isBlending) return;

            _isBlending = true;
            _blendProgress = 0f;
            _blendStartTime = Time.time;

            // Store animation state
            if (_targetAnimator != null)
            {
                _wasAnimationEnabled = _targetAnimator.enabled;
                _targetAnimator.enabled = false;
            }

            // Start blend coroutine
            _blendCoroutine = StartCoroutine(BlendProcessCoroutine());
        }

        private System.Collections.IEnumerator BlendProcessCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < _blendDuration)
            {
                elapsed = Time.time - _blendStartTime;
                _blendProgress = elapsed / _blendDuration;

                // Apply blend curve if custom curve is enabled
                float curveValue = _useCustomBlendCurve ? _blendCurve.Evaluate(_blendProgress) : _blendProgress;

                // Update physics and animation blending
                UpdatePhysicsBlending(curveValue);
                UpdateAnimationBlending(curveValue);

                // Update bone alignment
                if (_alignBonesOnBlend)
                {
                    UpdateBoneAlignment(curveValue);
                }

                yield return null;
            }

            // Complete the blend
            CompleteBlend();
        }

        private void UpdatePhysicsBlending(float blendValue)
        {
            if (!_graduallyDisablePhysics) return;

            foreach (var bone in _ragdollBones)
            {
                if (bone != null)
                {
                    // Gradually reduce physics influence
                    bone.linearDamping = Mathf.Lerp(0f, 10f, blendValue);
                    bone.angularDamping = Mathf.Lerp(0f, 10f, blendValue);

                    // Preserve momentum if enabled
                    if (_maintainMomentum && blendValue < 0.5f)
                    {
                        bone.linearVelocity *= _momentumPreservation;
                        bone.angularVelocity *= _momentumPreservation;
                    }
                }
            }
        }

        private void UpdateAnimationBlending(float blendValue)
        {
            if (_targetAnimator == null) return;

            // Gradually enable animation
            if (blendValue > 0.5f)
            {
                _targetAnimator.enabled = true;
                
                // Sync animation time if enabled
                if (_syncAnimationTime)
                {
                    SyncAnimationTime();
                }
            }
        }

        private void UpdateBoneAlignment(float blendValue)
        {
            if (_boneTransforms.Count == 0) return;

            foreach (var boneTransform in _boneTransforms)
            {
                if (boneTransform == null) continue;

                // Get the corresponding animated bone
                var animatedBone = GetAnimatedBone(boneTransform.name);
                if (animatedBone == null) continue;

                if (_smoothBoneAlignment)
                {
                    // Smooth alignment
                    boneTransform.position = Vector3.Lerp(
                        boneTransform.position, 
                        animatedBone.position, 
                        Time.deltaTime * _boneAlignmentSpeed * blendValue
                    );

                    boneTransform.rotation = Quaternion.Slerp(
                        boneTransform.rotation, 
                        animatedBone.rotation, 
                        Time.deltaTime * _boneAlignmentSpeed * blendValue
                    );
                }
                else
                {
                    // Direct alignment
                    boneTransform.position = Vector3.Lerp(
                        boneTransform.position, 
                        animatedBone.position, 
                        blendValue
                    );

                    boneTransform.rotation = Quaternion.Slerp(
                        boneTransform.rotation, 
                        animatedBone.rotation, 
                        blendValue
                    );
                }
            }
        }

        private Transform GetAnimatedBone(string boneName)
        {
            // Find the corresponding bone in the animated skeleton
            // This is a simplified approach - you might need to implement a more sophisticated bone mapping
            var animatedBone = _targetAnimator.GetBoneTransform(HumanBodyBones.Hips);
            
            // Search through child transforms for matching bone names
            if (animatedBone != null)
            {
                var childBones = animatedBone.GetComponentsInChildren<Transform>();
                foreach (var child in childBones)
                {
                    if (child.name == boneName)
                    {
                        return child;
                    }
                }
            }

            return null;
        }

        private void SyncAnimationTime()
        {
            if (_targetAnimator == null) return;

            // Get current animation state info
            var currentState = _targetAnimator.GetCurrentAnimatorStateInfo(0);
            
            // Calculate appropriate animation time based on current state
            float normalizedTime = currentState.normalizedTime;
            
            // Apply animation time with threshold checking
            if (Mathf.Abs(normalizedTime - _targetAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime) > _animationSyncThreshold)
            {
                _targetAnimator.Play(currentState.fullPathHash, 0, normalizedTime);
            }
        }

        private void CompleteBlend()
        {
            _isBlending = false;
            _blendProgress = 1f;

            // Enable animation if it was enabled before
            if (_resumeAnimationOnBlend && _targetAnimator != null)
            {
                StartCoroutine(DelayedAnimationResume());
            }

            // Disable physics on all ragdoll bones
            if (_graduallyDisablePhysics)
            {
                StartCoroutine(DelayedPhysicsDisable());
            }

            // Final bone alignment
            if (_alignBonesOnBlend)
            {
                FinalizeBoneAlignment();
            }

            Debug.Log("BlendBackSystem: Blend back process completed");
        }

        private System.Collections.IEnumerator DelayedAnimationResume()
        {
            yield return new WaitForSeconds(_animationResumeDelay);
            
            if (_targetAnimator != null)
            {
                _targetAnimator.enabled = _wasAnimationEnabled;
            }
        }

        private System.Collections.IEnumerator DelayedPhysicsDisable()
        {
            yield return new WaitForSeconds(_physicsDisableDelay);

            foreach (var bone in _ragdollBones)
            {
                if (bone != null)
                {
                    bone.isKinematic = true;
                    bone.detectCollisions = false;
                }
            }
        }

        private void FinalizeBoneAlignment()
        {
            foreach (var boneTransform in _boneTransforms)
            {
                if (boneTransform == null) continue;

                var animatedBone = GetAnimatedBone(boneTransform.name);
                if (animatedBone == null) continue;

                // Final alignment
                boneTransform.position = animatedBone.position;
                boneTransform.rotation = animatedBone.rotation;
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_isBlending)
            {
                // Draw blend progress indicator
                Gizmos.color = Color.Lerp(Color.red, Color.green, _blendProgress);
                Gizmos.DrawWireSphere(transform.position, 1f + _blendProgress);

                // Draw bone alignment paths
                if (_alignBonesOnBlend)
                {
                    Gizmos.color = Color.blue;
                    foreach (var boneTransform in _boneTransforms)
                    {
                        if (boneTransform != null)
                        {
                            var animatedBone = GetAnimatedBone(boneTransform.name);
                            if (animatedBone != null)
                            {
                                Gizmos.DrawLine(boneTransform.position, animatedBone.position);
                            }
                        }
                    }
                }
            }
        }

        #endregion
    }
}
