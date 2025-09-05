using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Audio
{
    /// <summary>
    /// 3D Audio Manager for spatial audio and occlusion
    /// Handles 3D positioned audio with distance attenuation and occlusion effects
    /// </summary>
    public class Audio3DManager
    {
        #region Fields

        private readonly AudioSystemManager _audioSystemManager;
        private Transform _listenerTransform;
        private Camera _audioListenerCamera;
        
        [System.Serializable]
        public struct Audio3DSettings
        {
            public float MaxDistance;
            public float MinDistance;
            public AnimationCurve DistanceAttenuation;
            public LayerMask OcclusionLayers;
            public float OcclusionStrength;
            public float OcclusionSmoothTime;
        }

        private Audio3DSettings _settings = new Audio3DSettings
        {
            MaxDistance = 100f,
            MinDistance = 1f,
            OcclusionLayers = -1, // All layers
            OcclusionStrength = 0.8f,
            OcclusionSmoothTime = 0.1f
        };

        private readonly Dictionary<AudioSource, Audio3DTrackingData> _tracking3DAudio = new();
        private bool _occlusionEnabled = true;

        #endregion

        #region Constructor

        public Audio3DManager(AudioSystemManager audioSystemManager)
        {
            _audioSystemManager = audioSystemManager;
            InitializeAudioListener();
            SetupDefaultDistanceAttenuation();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays a sound effect at a specific 3D position
        /// </summary>
        public AudioSource PlaySFXAt3DPosition(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return null;

            var audioSource = _audioSystemManager.GetPooledAudioSource();
            if (audioSource == null) return null;

            // Configure for 3D audio
            audioSource.clip = clip;
            audioSource.transform.position = position;
            audioSource.spatialBlend = 1f; // Full 3D
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = false;

            // Apply 3D settings
            Apply3DSettings(audioSource);

            // Start tracking for occlusion and distance updates
            StartTracking3DAudio(audioSource, position);

            audioSource.Play();

            return audioSource;
        }

        /// <summary>
        /// Plays a looping ambient sound at a 3D position
        /// </summary>
        public AudioSource PlayAmbientAt3DPosition(AudioClip clip, Vector3 position, float volume = 1f, bool loop = true)
        {
            if (clip == null) return null;

            var audioSource = _audioSystemManager.GetPooledAudioSource();
            if (audioSource == null) return null;

            audioSource.clip = clip;
            audioSource.transform.position = position;
            audioSource.spatialBlend = 1f;
            audioSource.volume = volume;
            audioSource.loop = loop;
            audioSource.priority = 64; // Higher priority for ambient sounds

            Apply3DSettings(audioSource);
            StartTracking3DAudio(audioSource, position, true);

            audioSource.Play();

            return audioSource;
        }

        /// <summary>
        /// Sets the listener position for 3D audio calculations
        /// </summary>
        public void SetListenerPosition(Transform listenerTransform)
        {
            _listenerTransform = listenerTransform;

            // Update AudioListener position
            if (_audioListenerCamera != null && listenerTransform != null)
            {
                var audioListener = _audioListenerCamera.GetComponent<AudioListener>();
                if (audioListener != null)
                {
                    audioListener.transform.position = listenerTransform.position;
                    audioListener.transform.rotation = listenerTransform.rotation;
                }
            }
        }

        /// <summary>
        /// Enables or disables audio occlusion
        /// </summary>
        public void EnableOcclusion(bool enable)
        {
            _occlusionEnabled = enable;
        }

        /// <summary>
        /// Updates all 3D audio sources (call from Update loop)
        /// </summary>
        public void Update3DAudio()
        {
            if (_listenerTransform == null) return;

            var sourcesToRemove = new List<AudioSource>();

            foreach (var kvp in _tracking3DAudio)
            {
                var audioSource = kvp.Key;
                var trackingData = kvp.Value;

                if (audioSource == null || !audioSource.gameObject.activeInHierarchy)
                {
                    sourcesToRemove.Add(audioSource);
                    continue;
                }

                if (!audioSource.isPlaying && !trackingData.IsPersistent)
                {
                    sourcesToRemove.Add(audioSource);
                    _audioSystemManager.ReturnAudioSourceToPool(audioSource);
                    continue;
                }

                // Update distance-based volume
                UpdateDistanceAttenuation(audioSource, trackingData);

                // Update occlusion if enabled
                if (_occlusionEnabled)
                {
                    UpdateOcclusion(audioSource, trackingData);
                }
            }

            // Remove finished audio sources from tracking
            foreach (var source in sourcesToRemove)
            {
                _tracking3DAudio.Remove(source);
            }
        }

        /// <summary>
        /// Stops tracking a 3D audio source
        /// </summary>
        public void StopTracking3DAudio(AudioSource audioSource)
        {
            _tracking3DAudio.Remove(audioSource);
        }

        /// <summary>
        /// Updates 3D audio settings
        /// </summary>
        public void UpdateSettings(Audio3DSettings newSettings)
        {
            _settings = newSettings;

            // Reapply settings to all tracked audio sources
            foreach (var kvp in _tracking3DAudio)
            {
                Apply3DSettings(kvp.Key);
            }
        }

        /// <summary>
        /// Gets current 3D audio settings
        /// </summary>
        public Audio3DSettings GetSettings()
        {
            return _settings;
        }

        #endregion

        #region Private Methods

        private void InitializeAudioListener()
        {
            // Find or create AudioListener
            var audioListener = Object.FindFirstObjectByType<AudioListener>();
            
            if (audioListener == null)
            {
                // Create AudioListener on main camera
                _audioListenerCamera = Camera.main;
                if (_audioListenerCamera != null)
                {
                    audioListener = _audioListenerCamera.gameObject.AddComponent<AudioListener>();
                }
            }
            else
            {
                _audioListenerCamera = audioListener.GetComponent<Camera>();
            }

            if (_audioListenerCamera != null)
            {
                _listenerTransform = _audioListenerCamera.transform;
            }
        }

        private void SetupDefaultDistanceAttenuation()
        {
            // Create default distance attenuation curve
            var curve = new AnimationCurve();
            curve.AddKey(0f, 1f);      // Full volume at min distance
            curve.AddKey(0.1f, 0.8f);  // 80% at 10% of max distance
            curve.AddKey(0.5f, 0.3f);  // 30% at half distance
            curve.AddKey(1f, 0f);      // Silent at max distance

            _settings.DistanceAttenuation = curve;
        }

        private void Apply3DSettings(AudioSource audioSource)
        {
            if (audioSource == null) return;

            audioSource.minDistance = _settings.MinDistance;
            audioSource.maxDistance = _settings.MaxDistance;
            audioSource.rolloffMode = AudioRolloffMode.Custom;

            // Apply distance attenuation curve
            if (_settings.DistanceAttenuation != null)
            {
                audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, _settings.DistanceAttenuation);
            }
        }

        private void StartTracking3DAudio(AudioSource audioSource, Vector3 position, bool isPersistent = false)
        {
            var trackingData = new Audio3DTrackingData
            {
                OriginalVolume = audioSource.volume,
                Position = position,
                LastOcclusionCheck = 0f,
                CurrentOcclusionLevel = 0f,
                TargetOcclusionLevel = 0f,
                IsPersistent = isPersistent
            };

            _tracking3DAudio[audioSource] = trackingData;
        }

        private void UpdateDistanceAttenuation(AudioSource audioSource, Audio3DTrackingData trackingData)
        {
            if (_listenerTransform == null) return;

            float distance = Vector3.Distance(_listenerTransform.position, trackingData.Position);
            float normalizedDistance = Mathf.Clamp01(distance / _settings.MaxDistance);

            // Apply distance attenuation using curve
            if (_settings.DistanceAttenuation != null)
            {
                float attenuationFactor = _settings.DistanceAttenuation.Evaluate(normalizedDistance);
                audioSource.volume = trackingData.OriginalVolume * attenuationFactor * (1f - trackingData.CurrentOcclusionLevel);
            }
        }

        private void UpdateOcclusion(AudioSource audioSource, Audio3DTrackingData trackingData)
        {
            if (_listenerTransform == null || Time.time - trackingData.LastOcclusionCheck < 0.1f)
                return;

            trackingData.LastOcclusionCheck = Time.time;

            // Perform raycast to check for occlusion
            Vector3 listenerPosition = _listenerTransform.position;
            Vector3 sourcePosition = trackingData.Position;
            Vector3 direction = (sourcePosition - listenerPosition).normalized;
            float distance = Vector3.Distance(listenerPosition, sourcePosition);

            bool isOccluded = Physics.Raycast(listenerPosition, direction, distance, _settings.OcclusionLayers);

            // Update occlusion level
            trackingData.TargetOcclusionLevel = isOccluded ? _settings.OcclusionStrength : 0f;

            // Smooth transition to target occlusion level
            trackingData.CurrentOcclusionLevel = Mathf.MoveTowards(
                trackingData.CurrentOcclusionLevel, 
                trackingData.TargetOcclusionLevel, 
                Time.deltaTime / _settings.OcclusionSmoothTime
            );

            // Apply low-pass filter for occlusion effect
            var audioLowPassFilter = audioSource.GetComponent<AudioLowPassFilter>();
            if (trackingData.CurrentOcclusionLevel > 0.01f)
            {
                if (audioLowPassFilter == null)
                {
                    audioLowPassFilter = audioSource.gameObject.AddComponent<AudioLowPassFilter>();
                }

                // Adjust cutoff frequency based on occlusion level
                audioLowPassFilter.cutoffFrequency = Mathf.Lerp(22000f, 800f, trackingData.CurrentOcclusionLevel);
                audioLowPassFilter.lowpassResonanceQ = 1f;
            }
            else if (audioLowPassFilter != null)
            {
                Object.DestroyImmediate(audioLowPassFilter);
            }
        }

        #endregion

        #region Data Structures

        private class Audio3DTrackingData
        {
            public float OriginalVolume;
            public Vector3 Position;
            public float LastOcclusionCheck;
            public float CurrentOcclusionLevel;
            public float TargetOcclusionLevel;
            public bool IsPersistent;
        }

        #endregion
    }
}