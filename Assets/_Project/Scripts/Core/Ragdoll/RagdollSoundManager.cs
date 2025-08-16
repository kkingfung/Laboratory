using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Manages ragdoll-related audio effects including hits, collisions, and landings.
    /// Provides randomized sound selection and volume control for realistic ragdoll audio feedback.
    /// Can be integrated with PartialRagdollController, RagdollImpactEffect, or other ragdoll systems.
    /// </summary>
    public class RagdollSoundManager : MonoBehaviour
    {
        #region Fields

        [Header("Hit Sounds")]
        [SerializeField]
        [Tooltip("Audio clips played when ragdoll bones receive impact damage")]
        private List<AudioClip> hitClips = new List<AudioClip>();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume level for hit sound effects")]
        private float hitVolume = 1f;

        [Header("Collision Sounds")]
        [SerializeField]
        [Tooltip("Audio clips played when bones collide with environment objects")]
        private List<AudioClip> collisionClips = new List<AudioClip>();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume level for collision sound effects")]
        private float collisionVolume = 0.8f;

        [Header("Landing Sounds")]
        [SerializeField]
        [Tooltip("Audio clips played when character impacts ground or large surfaces")]
        private List<AudioClip> landingClips = new List<AudioClip>();

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Volume level for landing sound effects")]
        private float landingVolume = 0.9f;

        [Header("Audio Configuration")]
        [SerializeField]
        [Tooltip("AudioSource component used for all ragdoll sound playback")]
        private AudioSource audioSource;

        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Master volume multiplier for all ragdoll sounds")]
        private float masterVolume = 1f;

        [SerializeField]
        [Tooltip("Minimum time between sound effects to prevent audio spam")]
        private float soundCooldown = 0.1f;

        [SerializeField]
        [Tooltip("Whether to use 3D spatial audio for positional sound effects")]
        private bool use3DAudio = true;

        // Runtime state
        private float lastSoundTime = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// Master volume for all ragdoll sounds
        /// </summary>
        public float MasterVolume 
        { 
            get => masterVolume; 
            set => masterVolume = Mathf.Clamp01(value); 
        }

        /// <summary>
        /// Volume for hit sound effects
        /// </summary>
        public float HitVolume 
        { 
            get => hitVolume; 
            set => hitVolume = Mathf.Clamp01(value); 
        }

        /// <summary>
        /// Volume for collision sound effects
        /// </summary>
        public float CollisionVolume 
        { 
            get => collisionVolume; 
            set => collisionVolume = Mathf.Clamp01(value); 
        }

        /// <summary>
        /// Volume for landing sound effects
        /// </summary>
        public float LandingVolume 
        { 
            get => landingVolume; 
            set => landingVolume = Mathf.Clamp01(value); 
        }

        /// <summary>
        /// Whether the sound manager is ready to play sounds
        /// </summary>
        public bool IsReady => audioSource != null;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Validate configuration and setup audio source
        /// </summary>
        private void Start()
        {
            ValidateConfiguration();
            ConfigureAudioSource();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Play a random hit sound effect
        /// </summary>
        public void PlayHitSound()
        {
            PlayRandomClip(hitClips, hitVolume);
        }

        /// <summary>
        /// Play a random hit sound with custom volume multiplier
        /// </summary>
        /// <param name="volumeMultiplier">Additional volume scaling (0-1)</param>
        public void PlayHitSound(float volumeMultiplier)
        {
            PlayRandomClip(hitClips, hitVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play a random collision sound effect
        /// </summary>
        public void PlayCollisionSound()
        {
            PlayRandomClip(collisionClips, collisionVolume);
        }

        /// <summary>
        /// Play a random collision sound with custom volume multiplier
        /// </summary>
        /// <param name="volumeMultiplier">Additional volume scaling (0-1)</param>
        public void PlayCollisionSound(float volumeMultiplier)
        {
            PlayRandomClip(collisionClips, collisionVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play a random landing sound effect
        /// </summary>
        public void PlayLandingSound()
        {
            PlayRandomClip(landingClips, landingVolume);
        }

        /// <summary>
        /// Play a random landing sound with custom volume multiplier
        /// </summary>
        /// <param name="volumeMultiplier">Additional volume scaling (0-1)</param>
        public void PlayLandingSound(float volumeMultiplier)
        {
            PlayRandomClip(landingClips, landingVolume * volumeMultiplier);
        }

        /// <summary>
        /// Play sound effect based on impact force magnitude
        /// </summary>
        /// <param name="soundType">Type of sound to play</param>
        /// <param name="impactForce">Force magnitude for volume scaling</param>
        public void PlayImpactSound(SoundType soundType, float impactForce)
        {
            float forceVolume = Mathf.Clamp01(impactForce / 10f); // Normalize force to volume
            
            switch (soundType)
            {
                case SoundType.Hit:
                    PlayHitSound(forceVolume);
                    break;
                case SoundType.Collision:
                    PlayCollisionSound(forceVolume);
                    break;
                case SoundType.Landing:
                    PlayLandingSound(forceVolume);
                    break;
            }
        }

        /// <summary>
        /// Add new hit sound clip to the collection
        /// </summary>
        /// <param name="clip">Audio clip to add</param>
        public void AddHitClip(AudioClip clip)
        {
            if (clip != null && !hitClips.Contains(clip))
            {
                hitClips.Add(clip);
            }
        }

        /// <summary>
        /// Add new collision sound clip to the collection
        /// </summary>
        /// <param name="clip">Audio clip to add</param>
        public void AddCollisionClip(AudioClip clip)
        {
            if (clip != null && !collisionClips.Contains(clip))
            {
                collisionClips.Add(clip);
            }
        }

        /// <summary>
        /// Add new landing sound clip to the collection
        /// </summary>
        /// <param name="clip">Audio clip to add</param>
        public void AddLandingClip(AudioClip clip)
        {
            if (clip != null && !landingClips.Contains(clip))
            {
                landingClips.Add(clip);
            }
        }

        /// <summary>
        /// Clear all sound clips of a specific type
        /// </summary>
        /// <param name="soundType">Type of sounds to clear</param>
        public void ClearSounds(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.Hit:
                    hitClips.Clear();
                    break;
                case SoundType.Collision:
                    collisionClips.Clear();
                    break;
                case SoundType.Landing:
                    landingClips.Clear();
                    break;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Validate component configuration and log warnings for missing elements
        /// </summary>
        private void ValidateConfiguration()
        {
            if (audioSource == null)
            {
                Debug.LogWarning($"[{name}] No AudioSource assigned. Ragdoll sounds will not play.", this);
            }

            if (hitClips.Count == 0 && collisionClips.Count == 0 && landingClips.Count == 0)
            {
                Debug.LogWarning($"[{name}] No audio clips assigned. Sound manager has nothing to play.", this);
            }
        }

        /// <summary>
        /// Configure audio source settings for optimal ragdoll sound playback
        /// </summary>
        private void ConfigureAudioSource()
        {
            if (audioSource == null) return;

            if (use3DAudio)
            {
                audioSource.spatialBlend = 1f; // Full 3D
                audioSource.rolloffMode = AudioRolloffMode.Linear;
                audioSource.maxDistance = 50f;
            }
            else
            {
                audioSource.spatialBlend = 0f; // Full 2D
            }
        }

        /// <summary>
        /// Play a random clip from the specified collection with volume control
        /// </summary>
        /// <param name="clips">Collection of clips to choose from</param>
        /// <param name="volume">Volume level for playback</param>
        private void PlayRandomClip(List<AudioClip> clips, float volume)
        {
            if (!CanPlaySound() || clips == null || clips.Count == 0) 
                return;

            AudioClip clipToPlay = SelectRandomClip(clips);
            if (clipToPlay != null)
            {
                float finalVolume = volume * masterVolume;
                audioSource.PlayOneShot(clipToPlay, finalVolume);
                lastSoundTime = Time.time;
            }
        }

        /// <summary>
        /// Check if enough time has passed since the last sound to play another
        /// </summary>
        /// <returns>True if sound can be played</returns>
        private bool CanPlaySound()
        {
            return audioSource != null && (Time.time - lastSoundTime) >= soundCooldown;
        }

        /// <summary>
        /// Select a random audio clip from the provided collection
        /// </summary>
        /// <param name="clips">Collection to select from</param>
        /// <returns>Randomly selected audio clip</returns>
        private AudioClip SelectRandomClip(List<AudioClip> clips)
        {
            if (clips.Count == 0) return null;
            
            int randomIndex = Random.Range(0, clips.Count);
            return clips[randomIndex];
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Types of ragdoll sounds that can be played
        /// </summary>
        public enum SoundType
        {
            /// <summary>Sound played when ragdoll receives impact damage</summary>
            Hit,
            /// <summary>Sound played when ragdoll collides with environment</summary>
            Collision,
            /// <summary>Sound played when ragdoll lands on ground</summary>
            Landing
        }

        #endregion
    }
}
