using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS-based system for handling ragdoll impact effects using Unity DOTS.
    /// Provides efficient physics-based impact responses for multiple entities.
    /// </summary>
    public class RagdollImpactEffectByDots : MonoBehaviour
    {
        #region Fields

        [Header("Impact Settings")]
        [SerializeField] private float _impactForceThreshold = 5f;
        [SerializeField] private float _impactVelocityThreshold = 3f;
        [SerializeField] private float _impactRadius = 2f;
        [SerializeField] private LayerMask _impactLayers = -1;

        [Header("Effect Configuration")]
        [SerializeField] private bool _enableParticleEffects = true;
        [SerializeField] private bool _enableSoundEffects = true;
        [SerializeField] private bool _enableScreenShake = true;
        [SerializeField] private float _screenShakeIntensity = 0.5f;

        [Header("Physics Response")]
        [SerializeField] private float _forceMultiplier = 1f;
        [SerializeField] private float _torqueMultiplier = 0.5f;
        [SerializeField] private bool _applyRadialForce = true;
        [SerializeField] private float _radialForceFalloff = 0.8f;

        [Header("Performance")]
        [SerializeField] private int _maxSimultaneousImpacts = 10;
        [SerializeField] private float _impactCooldown = 0.1f;
        #pragma warning disable 0414 // Field assigned but never used - planned for future object pooling system
        [SerializeField] private bool _useObjectPooling = true;
        #pragma warning restore 0414

        // Runtime state
        private EntityManager _entityManager;
        private World _defaultWorld;
        private bool _isInitialized = false;
        private float _lastImpactTime = 0f;
        private int _activeImpactCount = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the system has been initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Current number of active impacts
        /// </summary>
        public int ActiveImpactCount => _activeImpactCount;

        /// <summary>
        /// Impact force threshold for triggering effects
        /// </summary>
        public float ImpactForceThreshold => _impactForceThreshold;

        /// <summary>
        /// Impact velocity threshold for triggering effects
        /// </summary>
        public float ImpactVelocityThreshold => _impactVelocityThreshold;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDotsSystem();
        }

        private void Start()
        {
            ValidateConfiguration();
        }

        private void OnDestroy()
        {
            CleanupDotsSystem();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies an impact effect at a specific position
        /// </summary>
        /// <param name="position">World position of the impact</param>
        /// <param name="force">Impact force vector</param>
        /// <param name="source">Source of the impact</param>
        public void ApplyImpact(Vector3 position, Vector3 force, GameObject source = null)
        {
            if (!_isInitialized || !CanApplyImpact()) return;

            _lastImpactTime = Time.time;
            _activeImpactCount++;

            // Apply physics impact
            ApplyPhysicsImpact(position, force);

            // Apply visual effects
            if (_enableParticleEffects)
                ApplyParticleEffects(position, force);

            // Apply audio effects
            if (_enableSoundEffects)
                ApplySoundEffects(position, force);

            // Apply screen effects
            if (_enableScreenShake)
                ApplyScreenShake(force.magnitude);

            // Notify ECS system
            NotifyEcsSystem(position, force, source);
        }

        /// <summary>
        /// Applies an impact effect with additional parameters
        /// </summary>
        /// <param name="position">World position of the impact</param>
        /// <param name="force">Impact force vector</param>
        /// <param name="radius">Impact radius</param>
        /// <param name="source">Source of the impact</param>
        public void ApplyImpactWithRadius(Vector3 position, Vector3 force, float radius, GameObject source = null)
        {
            if (!_isInitialized || !CanApplyImpact()) return;

            float originalRadius = _impactRadius;
            _impactRadius = radius;

            ApplyImpact(position, force, source);

            _impactRadius = originalRadius;
        }

        /// <summary>
        /// Sets the impact force threshold
        /// </summary>
        /// <param name="threshold">New force threshold</param>
        public void SetImpactForceThreshold(float threshold)
        {
            _impactForceThreshold = Mathf.Max(0f, threshold);
        }

        /// <summary>
        /// Sets the impact velocity threshold
        /// </summary>
        /// <param name="threshold">New velocity threshold</param>
        public void SetImpactVelocityThreshold(float threshold)
        {
            _impactVelocityThreshold = Mathf.Max(0f, threshold);
        }

        /// <summary>
        /// Sets the impact radius
        /// </summary>
        /// <param name="radius">New impact radius</param>
        public void SetImpactRadius(float radius)
        {
            _impactRadius = Mathf.Max(0.1f, radius);
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
        /// Enables or disables particle effects
        /// </summary>
        /// <param name="enabled">Whether particle effects should be enabled</param>
        public void SetParticleEffectsEnabled(bool enabled)
        {
            _enableParticleEffects = enabled;
        }

        /// <summary>
        /// Enables or disables sound effects
        /// </summary>
        /// <param name="enabled">Whether sound effects should be enabled</param>
        public void SetSoundEffectsEnabled(bool enabled)
        {
            _enableSoundEffects = enabled;
        }

        /// <summary>
        /// Enables or disables screen shake
        /// </summary>
        /// <param name="enabled">Whether screen shake should be enabled</param>
        public void SetScreenShakeEnabled(bool enabled)
        {
            _enableScreenShake = enabled;
        }

        #endregion

        #region Private Methods

        private void InitializeDotsSystem()
        {
            try
            {
                _defaultWorld = World.DefaultGameObjectInjectionWorld;
                if (_defaultWorld != null && _defaultWorld.IsCreated)
                {
                    _entityManager = _defaultWorld.EntityManager;
                    _isInitialized = true;
                    Debug.Log("RagdollImpactEffectByDots: DOTS system initialized successfully");
                }
                else
                {
                    Debug.LogWarning("RagdollImpactEffectByDots: Default world not available");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RagdollImpactEffectByDots: Failed to initialize DOTS system: {e.Message}");
                _isInitialized = false;
            }
        }

        private void ValidateConfiguration()
        {
            if (_impactForceThreshold <= 0f)
            {
                Debug.LogWarning("RagdollImpactEffectByDots: Impact force threshold should be greater than 0");
                _impactForceThreshold = 5f;
            }

            if (_impactVelocityThreshold <= 0f)
            {
                Debug.LogWarning("RagdollImpactEffectByDots: Impact velocity threshold should be greater than 0");
                _impactVelocityThreshold = 3f;
            }

            if (_impactRadius <= 0f)
            {
                Debug.LogWarning("RagdollImpactEffectByDots: Impact radius should be greater than 0");
                _impactRadius = 2f;
            }

            if (_maxSimultaneousImpacts <= 0)
            {
                Debug.LogWarning("RagdollImpactEffectByDots: Max simultaneous impacts should be greater than 0");
                _maxSimultaneousImpacts = 10;
            }
        }

        private bool CanApplyImpact()
        {
            if (Time.time - _lastImpactTime < _impactCooldown)
                return false;

            if (_activeImpactCount >= _maxSimultaneousImpacts)
                return false;

            return true;
        }

        private void ApplyPhysicsImpact(Vector3 position, Vector3 force)
        {
            if (!_applyRadialForce) return;

            Collider[] colliders = Physics.OverlapSphere(position, _impactRadius, _impactLayers);
            
            foreach (var collider in colliders)
            {
                if (collider.attachedRigidbody != null)
                {
                    float distance = Vector3.Distance(position, collider.transform.position);
                    float falloff = 1f - (distance / _impactRadius) * _radialForceFalloff;
                    
                    if (falloff > 0f)
                    {
                        Vector3 appliedForce = force * _forceMultiplier * falloff;
                        collider.attachedRigidbody.AddForce(appliedForce, ForceMode.Impulse);

                        if (_torqueMultiplier > 0f)
                        {
                            Vector3 torque = Vector3.Cross(force, Vector3.up) * _torqueMultiplier * falloff;
                            collider.attachedRigidbody.AddTorque(torque, ForceMode.Impulse);
                        }
                    }
                }
            }
        }

        private void ApplyParticleEffects(Vector3 position, Vector3 force)
        {
            // This would typically instantiate particle systems
            // For now, just log the effect
            Debug.Log($"RagdollImpactEffectByDots: Particle effect at {position} with force {force.magnitude}");
        }

        private void ApplySoundEffects(Vector3 position, Vector3 force)
        {
            // This would typically play impact sounds
            // For now, just log the effect
            Debug.Log($"RagdollImpactEffectByDots: Sound effect at {position} with force {force.magnitude}");
        }

        private void ApplyScreenShake(float forceMagnitude)
        {
            // This would typically apply camera shake
            // For now, just log the effect
            float intensity = Mathf.Clamp01(forceMagnitude / _impactForceThreshold) * _screenShakeIntensity;
            Debug.Log($"RagdollImpactEffectByDots: Screen shake with intensity {intensity}");
        }

        private void NotifyEcsSystem(Vector3 position, Vector3 force, GameObject source)
        {
            if (!_isInitialized || _entityManager == null) return;

            try
            {
                // Create impact event entity
                var impactEvent = _entityManager.CreateEntity();
                
                // Add components for the impact event
                _entityManager.AddComponentData(impactEvent, new LocalTransform
                {
                    Position = new float3(position.x, position.y, position.z),
                    Rotation = quaternion.identity,
                    Scale = 1f
                });

                // Add custom impact data component if needed
                // _entityManager.AddComponentData(impactEvent, new ImpactData { Force = force, Source = source });

                Debug.Log($"RagdollImpactEffectByDots: ECS impact event created at {position}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RagdollImpactEffectByDots: Failed to create ECS impact event: {e.Message}");
            }
        }

        private void CleanupDotsSystem()
        {
            if (_isInitialized)
            {
                _isInitialized = false;
                _defaultWorld = null;
                Debug.Log("RagdollImpactEffectByDots: DOTS system cleaned up");
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _impactRadius);

            // Draw impact thresholds
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, Vector3.one * _impactForceThreshold * 0.1f);
        }

        #endregion
    }
}
