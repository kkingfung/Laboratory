using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Manages hit reactions and impact responses for ragdoll characters.
    /// Provides configurable hit reactions with physics-based responses and animation blending.
    /// </summary>
    public class HitReactionSystem : MonoBehaviour
    {
        #region Fields

        [Header("Hit Detection")]
        [SerializeField] private float _hitForceThreshold = 5f;
        [SerializeField] private float _hitVelocityThreshold = 3f;
        [SerializeField] private LayerMask _hitLayers = -1;
        [SerializeField] private bool _useRaycastDetection = true;
        [SerializeField] private float _detectionRadius = 2f;

        [Header("Reaction Settings")]
        [SerializeField] private float _reactionDuration = 0.5f;
        [SerializeField] private AnimationCurve _reactionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private bool _useCustomReactionCurve = false;
        [SerializeField] private float _reactionDelay = 0.1f;

        [Header("Physics Response")]
        [SerializeField] private bool _applyForceToBones = true;
        [SerializeField] private float _forceMultiplier = 1f;
        [SerializeField] private bool _applyTorque = true;
        [SerializeField] private float _torqueMultiplier = 0.5f;
        [SerializeField] private bool _maintainMomentum = true;

        [Header("Animation Response")]
        [SerializeField] private bool _blendWithAnimation = true;
        [SerializeField] private float _animationBlendWeight = 0.7f;
        [SerializeField] private bool _playHitAnimation = true;
        [SerializeField] private string _hitAnimationTrigger = "Hit";
        [SerializeField] private float _animationBlendDuration = 0.3f;

        [Header("Visual Effects")]
        [SerializeField] private bool _enableHitEffects = true;
        [SerializeField] private GameObject _hitEffectPrefab;
        [SerializeField] private bool _enableScreenShake = true;
        [SerializeField] private float _screenShakeIntensity = 0.5f;
        [SerializeField] private float _screenShakeDuration = 0.2f;

        [Header("Audio")]
        [SerializeField] private bool _enableHitSounds = true;
        [SerializeField] private AudioClip[] _hitSoundClips;
        [SerializeField] private float _hitSoundVolume = 0.8f;
        [SerializeField] private float _hitSoundPitch = 1f;

        // Runtime state
        private bool _isReacting = false;
        private float _reactionProgress = 0f;
        private float _reactionStartTime = 0f;
        private Coroutine _reactionCoroutine;
        private List<Rigidbody> _ragdollBones = new List<Rigidbody>();
        private List<Collider> _hitColliders = new List<Collider>();
        private Animator _targetAnimator;
        private AudioSource _audioSource;
        private Vector3 _lastHitPoint;
        private Vector3 _lastHitForce;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the system is currently processing a hit reaction
        /// </summary>
        public bool IsReacting => _isReacting;

        /// <summary>
        /// Current reaction progress (0-1)
        /// </summary>
        public float ReactionProgress => _reactionProgress;

        /// <summary>
        /// Duration of the hit reaction
        /// </summary>
        public float ReactionDuration => _reactionDuration;

        /// <summary>
        /// Whether hit effects are enabled
        /// </summary>
        public bool HitEffectsEnabled => _enableHitEffects;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeHitSystem();
        }

        private void Start()
        {
            ValidateConfiguration();
        }

        private void Update()
        {
            if (_useRaycastDetection)
            {
                UpdateHitDetection();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Triggers a hit reaction at the specified point with the given force
        /// </summary>
        /// <param name="hitPoint">World position of the hit</param>
        /// <param name="hitForce">Force vector of the hit</param>
        /// <param name="source">Source of the hit</param>
        public void TriggerHitReaction(Vector3 hitPoint, Vector3 hitForce, GameObject source = null)
        {
            if (_isReacting) return;

            _lastHitPoint = hitPoint;
            _lastHitForce = hitForce;

            // Check if hit meets threshold requirements
            if (hitForce.magnitude < _hitForceThreshold)
                return;

            // Start hit reaction
            StartCoroutine(DelayedHitReaction());
        }

        /// <summary>
        /// Stops the current hit reaction
        /// </summary>
        public void StopHitReaction()
        {
            if (!_isReacting) return;

            if (_reactionCoroutine != null)
            {
                StopCoroutine(_reactionCoroutine);
                _reactionCoroutine = null;
            }

            _isReacting = false;
            _reactionProgress = 0f;
        }

        /// <summary>
        /// Sets the hit force threshold
        /// </summary>
        /// <param name="threshold">New force threshold</param>
        public void SetHitForceThreshold(float threshold)
        {
            _hitForceThreshold = Mathf.Max(0f, threshold);
        }

        /// <summary>
        /// Sets the hit velocity threshold
        /// </summary>
        /// <param name="threshold">New velocity threshold</param>
        public void SetHitVelocityThreshold(float threshold)
        {
            _hitVelocityThreshold = Mathf.Max(0f, threshold);
        }

        /// <summary>
        /// Sets the reaction duration
        /// </summary>
        /// <param name="duration">Reaction duration in seconds</param>
        public void SetReactionDuration(float duration)
        {
            _reactionDuration = Mathf.Max(0.1f, duration);
        }

        /// <summary>
        /// Sets the reaction delay
        /// </summary>
        /// <param name="delay">Delay before reaction starts</param>
        public void SetReactionDelay(float delay)
        {
            _reactionDelay = Mathf.Max(0f, delay);
        }

        /// <summary>
        /// Sets the force multiplier
        /// </summary>
        /// <param name="multiplier">New force multiplier</param>
        public void SetForceMultiplier(float multiplier)
        {
            _forceMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Sets the torque multiplier
        /// </summary>
        /// <param name="multiplier">New torque multiplier</param>
        public void SetTorqueMultiplier(float multiplier)
        {
            _torqueMultiplier = Mathf.Max(0f, multiplier);
        }

        /// <summary>
        /// Enables or disables hit effects
        /// </summary>
        /// <param name="enabled">Whether hit effects should be enabled</param>
        public void SetHitEffectsEnabled(bool enabled)
        {
            _enableHitEffects = enabled;
        }

        /// <summary>
        /// Enables or disables screen shake
        /// </summary>
        /// <param name="enabled">Whether screen shake should be enabled</param>
        public void SetScreenShakeEnabled(bool enabled)
        {
            _enableScreenShake = enabled;
        }

        /// <summary>
        /// Enables or disables hit sounds
        /// </summary>
        /// <param name="enabled">Whether hit sounds should be enabled</param>
        public void SetHitSoundsEnabled(bool enabled)
        {
            _enableHitSounds = enabled;
        }

        #endregion

        #region Private Methods

        private void InitializeHitSystem()
        {
            _targetAnimator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();

            // Cache ragdoll bones
            var rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (var rb in rigidbodies)
            {
                if (rb != GetComponent<Rigidbody>()) // Skip root rigidbody
                {
                    _ragdollBones.Add(rb);
                }
            }

            // Cache hit colliders
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col != GetComponent<Collider>()) // Skip root collider
                {
                    _hitColliders.Add(col);
                }
            }

            Debug.Log($"HitReactionSystem: Initialized with {_ragdollBones.Count} ragdoll bones and {_hitColliders.Count} colliders");
        }

        private void ValidateConfiguration()
        {
            if (_hitForceThreshold <= 0f)
            {
                Debug.LogWarning("HitReactionSystem: Hit force threshold should be greater than 0");
                _hitForceThreshold = 5f;
            }

            if (_hitVelocityThreshold <= 0f)
            {
                Debug.LogWarning("HitReactionSystem: Hit velocity threshold should be greater than 0");
                _hitVelocityThreshold = 3f;
            }

            if (_reactionDuration <= 0f)
            {
                Debug.LogWarning("HitReactionSystem: Reaction duration should be greater than 0");
                _reactionDuration = 0.5f;
            }

            if (_reactionDelay < 0f)
            {
                Debug.LogWarning("HitReactionSystem: Reaction delay should not be negative");
                _reactionDelay = 0.1f;
            }

            if (_forceMultiplier < 0f)
            {
                Debug.LogWarning("HitReactionSystem: Force multiplier should not be negative");
                _forceMultiplier = 1f;
            }

            if (_torqueMultiplier < 0f)
            {
                Debug.LogWarning("HitReactionSystem: Torque multiplier should not be negative");
                _torqueMultiplier = 0.5f;
            }
        }

        private void UpdateHitDetection()
        {
            // Simple sphere cast detection for hits
            Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _hitLayers);
            
            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody != null && hit.attachedRigidbody.velocity.magnitude > _hitVelocityThreshold)
                {
                    Vector3 hitPoint = hit.ClosestPoint(transform.position);
                    Vector3 hitForce = hit.attachedRigidbody.velocity * hit.attachedRigidbody.mass;
                    
                    TriggerHitReaction(hitPoint, hitForce, hit.gameObject);
                }
            }
        }

        private System.Collections.IEnumerator DelayedHitReaction()
        {
            yield return new WaitForSeconds(_reactionDelay);
            BeginHitReaction();
        }

        private void BeginHitReaction()
        {
            if (_isReacting) return;

            _isReacting = true;
            _reactionProgress = 0f;
            _reactionStartTime = Time.time;

            // Apply physics response
            if (_applyForceToBones)
            {
                ApplyForceToBones();
            }

            // Play hit animation
            if (_playHitAnimation && _targetAnimator != null)
            {
                _targetAnimator.SetTrigger(_hitAnimationTrigger);
            }

            // Start reaction coroutine
            _reactionCoroutine = StartCoroutine(HitReactionCoroutine());
        }

        private System.Collections.IEnumerator HitReactionCoroutine()
        {
            float elapsed = 0f;

            while (elapsed < _reactionDuration)
            {
                elapsed = Time.time - _reactionStartTime;
                _reactionProgress = elapsed / _reactionDuration;

                // Apply reaction curve if custom curve is enabled
                float curveValue = _useCustomReactionCurve ? _reactionCurve.Evaluate(_reactionProgress) : _reactionProgress;

                // Update animation blending
                if (_blendWithAnimation)
                {
                    UpdateAnimationBlending(curveValue);
                }

                // Update visual effects
                if (_enableHitEffects)
                {
                    UpdateHitEffects(curveValue);
                }

                yield return null;
            }

            // Complete the reaction
            CompleteHitReaction();
        }

        private void ApplyForceToBones()
        {
            foreach (var bone in _ragdollBones)
            {
                if (bone != null)
                {
                    // Calculate force direction from hit point to bone
                    Vector3 forceDirection = (bone.position - _lastHitPoint).normalized;
                    float distance = Vector3.Distance(bone.position, _lastHitPoint);
                    float forceFalloff = 1f / (1f + distance);

                    // Apply force
                    Vector3 appliedForce = forceDirection * _lastHitForce.magnitude * _forceMultiplier * forceFalloff;
                    bone.AddForce(appliedForce, ForceMode.Impulse);

                    // Apply torque if enabled
                    if (_applyTorque)
                    {
                        Vector3 torque = Vector3.Cross(forceDirection, Vector3.up) * _lastHitForce.magnitude * _torqueMultiplier * forceFalloff;
                        bone.AddTorque(torque, ForceMode.Impulse);
                    }

                    // Maintain momentum if enabled
                    if (_maintainMomentum)
                    {
                        bone.velocity *= 0.8f; // Reduce velocity slightly
                    }
                }
            }
        }

        private void UpdateAnimationBlending(float reactionValue)
        {
            if (_targetAnimator == null) return;

            // Blend between ragdoll and animation based on reaction progress
            float blendWeight = Mathf.Lerp(0f, _animationBlendWeight, reactionValue);
            
            // This would typically involve setting animator parameters or blend weights
            // For now, we'll just log the blending
            Debug.Log($"HitReactionSystem: Animation blend weight: {blendWeight}");
        }

        private void UpdateHitEffects(float reactionValue)
        {
            // Update hit effects based on reaction progress
            // This could involve particle system intensity, color changes, etc.
            
            // For now, just log the effect update
            Debug.Log($"HitReactionSystem: Hit effects updated with progress: {reactionValue}");
        }

        private void CompleteHitReaction()
        {
            _isReacting = false;
            _reactionProgress = 1f;

            // Spawn hit effects
            if (_enableHitEffects && _hitEffectPrefab != null)
            {
                SpawnHitEffect();
            }

            // Play hit sound
            if (_enableHitSounds && _audioSource != null)
            {
                PlayHitSound();
            }

            // Apply screen shake
            if (_enableScreenShake)
            {
                ApplyScreenShake();
            }

            Debug.Log("HitReactionSystem: Hit reaction completed");
        }

        private void SpawnHitEffect()
        {
            if (_hitEffectPrefab == null) return;

            GameObject effect = Instantiate(_hitEffectPrefab, _lastHitPoint, Quaternion.identity);
            
            // Orient effect towards hit direction
            if (_lastHitForce != Vector3.zero)
            {
                effect.transform.rotation = Quaternion.LookRotation(_lastHitForce.normalized);
            }

            // Destroy effect after a delay
            Destroy(effect, 2f);
        }

        private void PlayHitSound()
        {
            if (_audioSource == null || _hitSoundClips == null || _hitSoundClips.Length == 0) return;

            // Select random hit sound
            AudioClip selectedClip = _hitSoundClips[Random.Range(0, _hitSoundClips.Length)];
            
            if (selectedClip != null)
            {
                _audioSource.pitch = _hitSoundPitch;
                _audioSource.volume = _hitSoundVolume;
                _audioSource.PlayOneShot(selectedClip);
            }
        }

        private void ApplyScreenShake()
        {
            // This would typically involve camera shake
            // For now, just log the screen shake
            float intensity = Mathf.Clamp01(_lastHitForce.magnitude / _hitForceThreshold) * _screenShakeIntensity;
            Debug.Log($"HitReactionSystem: Screen shake with intensity {intensity} for {_screenShakeDuration}s");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Draw hit force threshold
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * _hitForceThreshold * 0.1f);

            // Draw last hit point if available
            if (_lastHitPoint != Vector3.zero)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(_lastHitPoint, 0.2f);
                Gizmos.DrawLine(_lastHitPoint, _lastHitPoint + _lastHitForce.normalized);
            }
        }

        #endregion
    }
}
