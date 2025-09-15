using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Spawns visual and audio effects when a ragdoll bone receives impact damage.
    /// Can be triggered via PartialRagdollController, Hitbox systems, or NetworkRagdollSync.
    /// Handles automatic cleanup of spawned effect objects and provides configurable impact responses.
    /// </summary>
    public class RagdollImpactEffect : MonoBehaviour
    {
        #region Fields

        [Header("Visual Effects")]
        [SerializeField]
        [Tooltip("Prefab to spawn on impact (particle system, decal, etc.)")]
        private GameObject impactPrefab;

        [SerializeField]
        [Tooltip("Scale multiplier for spawned impact prefab")]
        private float impactScale = 1f;

        [SerializeField]
        [Tooltip("Automatic cleanup time for effects without particle systems")]
        private float defaultLifetime = 2f;

        [Header("Audio Effects")]
        [SerializeField]
        [Tooltip("Audio clip to play on impact")]
        private AudioClip impactSound;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume level for impact sound")]
        private float soundVolume = 1f;

        [SerializeField]
        [Tooltip("AudioSource component for sound playback")]
        private AudioSource audioSource;

        [Header("Effect Behavior")]
        [SerializeField]
        [Tooltip("Whether to align effect rotation with impact normal")]
        private bool alignWithNormal = true;

        [SerializeField]
        [Tooltip("Whether to auto-destroy effects based on particle system duration")]
        private bool autoCleanupParticles = true;

        #endregion

        #region Properties

        /// <summary>
        /// The impact prefab that will be spawned
        /// </summary>
        public GameObject ImpactPrefab => impactPrefab;

        /// <summary>
        /// Current impact sound clip
        /// </summary>
        public AudioClip ImpactSound => impactSound;

        /// <summary>
        /// Current sound volume setting
        /// </summary>
        public float SoundVolume => soundVolume;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Validate required components on start
        /// </summary>
        private void Start()
        {
            ValidateComponents();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Trigger impact effect at the specified world position
        /// </summary>
        /// <param name="hitPosition">World position where impact occurred</param>
        public void TriggerImpact(Vector3 hitPosition)
        {
            TriggerImpact(hitPosition, Vector3.zero);
        }

        /// <summary>
        /// Trigger impact effect at the specified position with surface normal
        /// </summary>
        /// <param name="hitPosition">World position where impact occurred</param>
        /// <param name="hitNormal">Surface normal at impact point for proper rotation</param>
        public void TriggerImpact(Vector3 hitPosition, Vector3 hitNormal)
        {
            SpawnVisualEffect(hitPosition, hitNormal);
            PlayImpactSound();
        }

        /// <summary>
        /// Trigger impact effect with force information for enhanced effects
        /// </summary>
        /// <param name="hitPosition">World position where impact occurred</param>
        /// <param name="hitNormal">Surface normal at impact point</param>
        /// <param name="impactForce">Force magnitude of the impact</param>
        public void TriggerImpact(Vector3 hitPosition, Vector3 hitNormal, float impactForce)
        {
            SpawnVisualEffect(hitPosition, hitNormal, impactForce);
            PlayImpactSound();
        }

        /// <summary>
        /// Set new impact prefab at runtime
        /// </summary>
        /// <param name="newPrefab">New prefab to use for impacts</param>
        public void SetImpactPrefab(GameObject newPrefab)
        {
            impactPrefab = newPrefab;
        }

        /// <summary>
        /// Set new impact sound at runtime
        /// </summary>
        /// <param name="newSound">New audio clip to use for impacts</param>
        public void SetImpactSound(AudioClip newSound)
        {
            impactSound = newSound;
        }

        /// <summary>
        /// Update sound volume at runtime
        /// </summary>
        /// <param name="newVolume">New volume level (0-1)</param>
        public void SetSoundVolume(float newVolume)
        {
            soundVolume = Mathf.Clamp01(newVolume);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validate that required components are properly assigned
        /// </summary>
        private void ValidateComponents()
        {
            if (impactSound != null && audioSource == null)
            {
                Debug.LogWarning($"[{name}] Impact sound assigned but no AudioSource found. Sound will not play.", this);
            }
        }

        /// <summary>
        /// Spawn visual effect at impact location
        /// </summary>
        /// <param name="position">World position for effect</param>
        /// <param name="normal">Surface normal for rotation</param>
        /// <param name="forceScale">Optional force scale for effect intensity</param>
        private void SpawnVisualEffect(Vector3 position, Vector3 normal, float forceScale = 1f)
        {
            if (impactPrefab == null) 
                return;

            Quaternion rotation = CalculateEffectRotation(normal);
            GameObject effectInstance = Instantiate(impactPrefab, position, rotation);
            
            ApplyEffectScale(effectInstance, forceScale);
            SetupEffectCleanup(effectInstance);
        }

        /// <summary>
        /// Calculate appropriate rotation for the effect based on impact normal
        /// </summary>
        /// <param name="normal">Impact surface normal</param>
        /// <returns>Calculated rotation for effect</returns>
        private Quaternion CalculateEffectRotation(Vector3 normal)
        {
            if (!alignWithNormal || normal == Vector3.zero)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(normal);
        }

        /// <summary>
        /// Apply scale to effect instance based on impact force
        /// </summary>
        /// <param name="effectInstance">Effect GameObject to scale</param>
        /// <param name="forceScale">Force multiplier for scaling</param>
        private void ApplyEffectScale(GameObject effectInstance, float forceScale)
        {
            float totalScale = impactScale * Mathf.Clamp(forceScale, 0.5f, 2f);
            effectInstance.transform.localScale *= totalScale;
        }

        /// <summary>
        /// Setup automatic cleanup for the spawned effect
        /// </summary>
        /// <param name="effectInstance">Effect GameObject to setup cleanup for</param>
        private void SetupEffectCleanup(GameObject effectInstance)
        {
            if (!autoCleanupParticles)
                return;

            ParticleSystem particleSystem = effectInstance.GetComponent<ParticleSystem>();
            float lifetime = CalculateEffectLifetime(particleSystem);
            
            Destroy(effectInstance, lifetime);
        }

        /// <summary>
        /// Calculate appropriate lifetime for effect cleanup
        /// </summary>
        /// <param name="particleSystem">Particle system component if present</param>
        /// <returns>Lifetime in seconds</returns>
        private float CalculateEffectLifetime(ParticleSystem particleSystem)
        {
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                return main.duration + main.startLifetime.constantMax;
            }

            return defaultLifetime;
        }

        /// <summary>
        /// Play impact sound effect if available
        /// </summary>
        private void PlayImpactSound()
        {
            if (impactSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(impactSound, soundVolume);
            }
        }

        #endregion
    }
}
