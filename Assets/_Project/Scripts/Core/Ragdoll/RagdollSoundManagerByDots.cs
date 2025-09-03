using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS-based system for managing ragdoll sound effects using Unity DOTS.
    /// Provides efficient audio management for multiple ragdoll entities with spatial audio support.
    /// </summary>
    public class RagdollSoundManagerByDots : MonoBehaviour
    {
        #region Fields

        [Header("Audio Settings")]
        [SerializeField] private float _masterVolume = 1f;
        [SerializeField] private float _spatialBlend = 1f;
        [SerializeField] private float _minDistance = 1f;
        [SerializeField] private float _maxDistance = 50f;
        [SerializeField] private bool _use3DSound = true;

        [Header("Impact Sounds")]
        [SerializeField] private AudioClip[] _impactSounds;
        [SerializeField] private float _impactVolume = 0.8f;
        [SerializeField] private float _impactPitch = 1f;
        [SerializeField] private float _impactCooldown = 0.1f;
        [SerializeField] private bool _randomizePitch = true;
        [SerializeField] private float _pitchVariation = 0.2f;

        [Header("Movement Sounds")]
        [SerializeField] private AudioClip[] _movementSounds;
        [SerializeField] private float _movementVolume = 0.6f;
        [SerializeField] private float _movementThreshold = 0.5f;
        [SerializeField] private float _movementCooldown = 0.2f;
        [SerializeField] private bool _enableMovementSounds = true;

        [Header("Collision Sounds")]
        [SerializeField] private AudioClip[] _collisionSounds;
        [SerializeField] private float _collisionVolume = 0.7f;
        [SerializeField] private float _collisionThreshold = 2f;
        [SerializeField] private float _collisionCooldown = 0.15f;
        [SerializeField] private bool _enableCollisionSounds = true;

        [Header("Performance")]
        [SerializeField] private int _maxSimultaneousSounds = 8;
        [SerializeField] private bool _useObjectPooling = true;
        [SerializeField] private int _audioSourcePoolSize = 12;
        [SerializeField] private bool _enableDistanceCulling = true;
        [SerializeField] private float _cullingDistance = 100f;

        // Runtime state
        private EntityManager _entityManager;
        private World _defaultWorld;
        private bool _isInitialized = false;
        private List<AudioSource> _audioSourcePool = new List<AudioSource>();
        private Dictionary<int, float> _lastSoundTimes = new Dictionary<int, float>();
        private int _activeSoundCount = 0;
        private Transform _playerTransform;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the system has been initialized
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Current number of active sounds
        /// </summary>
        public int ActiveSoundCount => _activeSoundCount;

        /// <summary>
        /// Master volume for all ragdoll sounds
        /// </summary>
        public float MasterVolume => _masterVolume;

        /// <summary>
        /// Whether 3D spatial audio is enabled
        /// </summary>
        public bool Use3DSound => _use3DSound;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeDotsSystem();
        }

        private void Start()
        {
            ValidateConfiguration();
            InitializeAudioPool();
            FindPlayerTransform();
        }

        private void OnDestroy()
        {
            CleanupDotsSystem();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays an impact sound at the specified position
        /// </summary>
        /// <param name="position">World position for the sound</param>
        /// <param name="force">Impact force for volume scaling</param>
        /// <param name="source">Source of the impact</param>
        public void PlayImpactSound(Vector3 position, float force, GameObject source = null)
        {
            if (!_isInitialized || !CanPlaySound(SoundType.Impact)) return;

            AudioClip clip = GetRandomClip(_impactSounds);
            if (clip == null) return;

            float volume = CalculateImpactVolume(force);
            float pitch = CalculateImpactPitch();

            PlaySoundAtPosition(clip, position, volume, pitch, SoundType.Impact);
        }

        /// <summary>
        /// Plays a movement sound at the specified position
        /// </summary>
        /// <param name="position">World position for the sound</param>
        /// <param name="velocity">Movement velocity for volume scaling</param>
        /// <param name="source">Source of the movement</param>
        public void PlayMovementSound(Vector3 position, Vector3 velocity, GameObject source = null)
        {
            if (!_isInitialized || !_enableMovementSounds || !CanPlaySound(SoundType.Movement)) return;

            AudioClip clip = GetRandomClip(_movementSounds);
            if (clip == null) return;

            float volume = CalculateMovementVolume(velocity.magnitude);
            float pitch = 1f;

            PlaySoundAtPosition(clip, position, volume, pitch, SoundType.Movement);
        }

        /// <summary>
        /// Plays a collision sound at the specified position
        /// </summary>
        /// <param name="position">World position for the sound</param>
        /// <param name="collisionForce">Collision force for volume scaling</param>
        /// <param name="source">Source of the collision</param>
        public void PlayCollisionSound(Vector3 position, float collisionForce, GameObject source = null)
        {
            if (!_isInitialized || !_enableCollisionSounds || !CanPlaySound(SoundType.Collision)) return;

            AudioClip clip = GetRandomClip(_collisionSounds);
            if (clip == null) return;

            float volume = CalculateCollisionVolume(collisionForce);
            float pitch = 1f;

            PlaySoundAtPosition(clip, position, volume, pitch, SoundType.Collision);
        }

        /// <summary>
        /// Sets the master volume
        /// </summary>
        /// <param name="volume">New master volume (0-1)</param>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Sets the spatial blend
        /// </summary>
        /// <param name="blend">New spatial blend (0-1)</param>
        public void SetSpatialBlend(float blend)
        {
            _spatialBlend = Mathf.Clamp01(blend);
        }

        /// <summary>
        /// Sets the minimum distance for 3D audio
        /// </summary>
        /// <param name="distance">New minimum distance</param>
        public void SetMinDistance(float distance)
        {
            _minDistance = Mathf.Max(0.1f, distance);
        }

        /// <summary>
        /// Sets the maximum distance for 3D audio
        /// </summary>
        /// <param name="distance">New maximum distance</param>
        public void SetMaxDistance(float distance)
        {
            _maxDistance = Mathf.Max(_minDistance, distance);
        }

        /// <summary>
        /// Enables or disables 3D sound
        /// </summary>
        /// <param name="enabled">Whether 3D sound should be enabled</param>
        public void Set3DSoundEnabled(bool enabled)
        {
            _use3DSound = enabled;
        }

        /// <summary>
        /// Sets the impact volume
        /// </summary>
        /// <param name="volume">New impact volume (0-1)</param>
        public void SetImpactVolume(float volume)
        {
            _impactVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Sets the movement volume
        /// </summary>
        /// <param name="volume">New movement volume (0-1)</param>
        public void SetMovementVolume(float volume)
        {
            _movementVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Sets the collision volume
        /// </summary>
        /// <param name="volume">New collision volume (0-1)</param>
        public void SetCollisionVolume(float volume)
        {
            _collisionVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// Stops all currently playing sounds
        /// </summary>
        public void StopAllSounds()
        {
            if (_useObjectPooling)
            {
                foreach (var audioSource in _audioSourcePool)
                {
                    if (audioSource != null && audioSource.isPlaying)
                    {
                        audioSource.Stop();
                    }
                }
            }
            else
            {
                // Find and stop all AudioSource components when not using pooling
                var audioSources = GetComponents<AudioSource>();
                foreach (var audioSource in audioSources)
                {
                    if (audioSource != null && audioSource.isPlaying)
                    {
                        audioSource.Stop();
                    }
                }
            }
            _activeSoundCount = 0;
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
                    Debug.Log("RagdollSoundManagerByDots: DOTS system initialized successfully");
                }
                else
                {
                    Debug.LogWarning("RagdollSoundManagerByDots: Default world not available");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RagdollSoundManagerByDots: Failed to initialize DOTS system: {e.Message}");
                _isInitialized = false;
            }
        }

        private void ValidateConfiguration()
        {
            if (_masterVolume < 0f || _masterVolume > 1f)
            {
                Debug.LogWarning("RagdollSoundManagerByDots: Master volume should be between 0 and 1");
                _masterVolume = Mathf.Clamp01(_masterVolume);
            }

            if (_spatialBlend < 0f || _spatialBlend > 1f)
            {
                Debug.LogWarning("RagdollSoundManagerByDots: Spatial blend should be between 0 and 1");
                _spatialBlend = Mathf.Clamp01(_spatialBlend);
            }

            if (_minDistance <= 0f)
            {
                Debug.LogWarning("RagdollSoundManagerByDots: Min distance should be greater than 0");
                _minDistance = 1f;
            }

            if (_maxDistance <= _minDistance)
            {
                Debug.LogWarning("RagdollSoundManagerByDots: Max distance should be greater than min distance");
                _maxDistance = _minDistance + 1f;
            }

            if (_impactVolume < 0f || _impactVolume > 1f)
            {
                Debug.LogWarning("RagdollSoundManagerByDots: Impact volume should be between 0 and 1");
                _impactVolume = Mathf.Clamp01(_impactVolume);
            }

            if (_movementVolume < 0f || _movementVolume > 1f)
            {
                Debug.LogWarning("RagdollSoundManagerByDots: Movement volume should be between 0 and 1");
                _movementVolume = Mathf.Clamp01(_movementVolume);
            }

            if (_collisionVolume < 0f || _collisionVolume > 1f)
            {
                Debug.LogWarning("RagdollSoundManagerByDots: Collision volume should be between 0 and 1");
                _collisionVolume = Mathf.Clamp01(_collisionVolume);
            }
        }

        private void InitializeAudioPool()
        {
            if (!_useObjectPooling) return;

            for (int i = 0; i < _audioSourcePoolSize; i++)
            {
                var audioSource = gameObject.AddComponent<AudioSource>();
                ConfigureAudioSource(audioSource);
                _audioSourcePool.Add(audioSource);
            }

            Debug.Log($"RagdollSoundManagerByDots: Audio pool initialized with {_audioSourcePoolSize} sources");
        }

        private void ConfigureAudioSource(AudioSource audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = _spatialBlend;
            audioSource.minDistance = _minDistance;
            audioSource.maxDistance = _maxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.dopplerLevel = 0f;
        }

        private void FindPlayerTransform()
        {
            // Find player transform for distance calculations
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTransform = player.transform;
            }
            else
            {
                // Fallback to camera transform
                var camera = UnityEngine.Camera.main;
                if (camera != null)
                {
                    _playerTransform = camera.transform;
                }
            }
        }

        private bool CanPlaySound(SoundType soundType)
        {
            if (_activeSoundCount >= _maxSimultaneousSounds)
                return false;

            int soundId = (int)soundType;
            if (_lastSoundTimes.TryGetValue(soundId, out float lastTime))
            {
                float cooldown = GetCooldownForSoundType(soundType);
                if (Time.time - lastTime < cooldown)
                    return false;
            }

            return true;
        }

        private float GetCooldownForSoundType(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.Impact: return _impactCooldown;
                case SoundType.Movement: return _movementCooldown;
                case SoundType.Collision: return _collisionCooldown;
                default: return 0.1f;
            }
        }

        private AudioClip GetRandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        private float CalculateImpactVolume(float force)
        {
            float normalizedForce = Mathf.Clamp01(force / 10f); // Normalize to 0-1 range
            return _impactVolume * _masterVolume * normalizedForce;
        }

        private float CalculateImpactPitch()
        {
            if (!_randomizePitch) return _impactPitch;
            
            float variation = UnityEngine.Random.Range(-_pitchVariation, _pitchVariation);
            return _impactPitch + variation;
        }

        private float CalculateMovementVolume(float velocity)
        {
            if (velocity < _movementThreshold) return 0f;
            
            float normalizedVelocity = Mathf.Clamp01((velocity - _movementThreshold) / 5f);
            return _movementVolume * _masterVolume * normalizedVelocity;
        }

        private float CalculateCollisionVolume(float collisionForce)
        {
            if (collisionForce < _collisionThreshold) return 0f;
            
            float normalizedForce = Mathf.Clamp01((collisionForce - _collisionThreshold) / 10f);
            return _collisionVolume * _masterVolume * normalizedForce;
        }

        private void PlaySoundAtPosition(AudioClip clip, Vector3 position, float volume, float pitch, SoundType soundType)
        {
            if (clip == null) return;

            // Check distance culling
            if (_enableDistanceCulling && _playerTransform != null)
            {
                float distance = Vector3.Distance(position, _playerTransform.position);
                if (distance > _cullingDistance) return;
            }

            // Get available audio source from pool
            AudioSource audioSource = GetAvailableAudioSource();
            if (audioSource == null) return;

            // Configure and play sound
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.transform.position = position;
            audioSource.Play();

            // Update tracking
            _activeSoundCount++;
            _lastSoundTimes[(int)soundType] = Time.time;

            // Schedule return to pool
            StartCoroutine(ReturnAudioSourceToPool(audioSource, clip.length));
        }

        private AudioSource GetAvailableAudioSource()
        {
            if (_useObjectPooling)
            {
                foreach (var audioSource in _audioSourcePool)
                {
                    if (audioSource != null && !audioSource.isPlaying)
                    {
                        return audioSource;
                    }
                }
                return null;
            }
            else
            {
                // Create a new AudioSource for one-time use
                var audioSource = gameObject.AddComponent<AudioSource>();
                ConfigureAudioSource(audioSource);
                return audioSource;
            }
        }

        private System.Collections.IEnumerator ReturnAudioSourceToPool(AudioSource audioSource, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
                _activeSoundCount--;

                // If not using pooling, destroy the AudioSource component
                if (!_useObjectPooling)
                {
                    DestroyImmediate(audioSource);
                }
            }
        }

        private void CleanupDotsSystem()
        {
            if (_isInitialized)
            {
                _isInitialized = false;
                _defaultWorld = null;
                Debug.Log("RagdollSoundManagerByDots: DOTS system cleaned up");
            }
        }

        #endregion

        #region Supporting Types

        /// <summary>
        /// Types of sounds that can be played
        /// </summary>
        public enum SoundType
        {
            Impact,
            Movement,
            Collision
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_use3DSound)
            {
                // Draw 3D audio range
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, _minDistance);
                Gizmos.DrawWireSphere(transform.position, _maxDistance);

                // Draw culling distance if enabled
                if (_enableDistanceCulling)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(transform.position, _cullingDistance);
                }
            }
        }

        #endregion
    }
}
