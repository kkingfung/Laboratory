using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// MonoBehaviour companion to RagdollSoundManagerByDots for handling audio playback.
    /// This class bridges the gap between ECS and Unity's audio system.
    /// </summary>
    public class RagdollSoundManager : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Audio Clips")]
        [Tooltip("Audio clips to play when a ragdoll is hit")]
        [SerializeField] private List<AudioClip> hitClips = new List<AudioClip>();
        
        [Tooltip("Audio clips to play when a ragdoll collides with objects")]
        [SerializeField] private List<AudioClip> collisionClips = new List<AudioClip>();
        
        [Tooltip("Audio clips to play when a ragdoll lands")]
        [SerializeField] private List<AudioClip> landingClips = new List<AudioClip>();

        [Header("Audio Settings")]
        [Tooltip("AudioSource component for playing sound effects")]
        [SerializeField] private AudioSource audioSource;
        
        [Tooltip("Volume level for ragdoll sound effects")]
        [SerializeField] [Range(0f, 1f)] private float volume = 1f;

        #endregion

        #region Static Instance

        /// <summary>
        /// Static instance for easy access from ECS systems
        /// </summary>
        public static RagdollSoundManager Instance { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Set up singleton
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Validate components
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            // Create ECS config entity
            CreateConfigEntity();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays a random clip from the hit clips collection
        /// </summary>
        public void PlayHitSound()
        {
            PlayRandomClip(hitClips);
        }

        /// <summary>
        /// Plays a random clip from the collision clips collection
        /// </summary>
        public void PlayCollisionSound()
        {
            PlayRandomClip(collisionClips);
        }

        /// <summary>
        /// Plays a random clip from the landing clips collection
        /// </summary>
        public void PlayLandingSound()
        {
            PlayRandomClip(landingClips);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Creates the ECS configuration entity
        /// </summary>
        private void CreateConfigEntity()
        {
            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null) return;

            var entityManager = world.EntityManager;
            var configEntity = entityManager.CreateEntity(typeof(RagdollSoundConfig));
            
            entityManager.SetComponentData(configEntity, new RagdollSoundConfig
            {
                Volume = volume
            });
        }

        /// <summary>
        /// Plays a random audio clip from the provided list
        /// </summary>
        /// <param name="clips">List of audio clips to choose from</param>
        private void PlayRandomClip(List<AudioClip> clips)
        {
            if (clips == null || clips.Count == 0 || audioSource == null) 
                return;

            int index = Random.Range(0, clips.Count);
            if (clips[index] != null)
            {
                audioSource.PlayOneShot(clips[index], volume);
            }
        }

        #endregion
    }
}
