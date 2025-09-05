using System;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Audio
{
    /// <summary>
    /// Audio Pool Manager for efficient AudioSource management
    /// Handles pooling and reuse of AudioSource components to avoid memory allocation
    /// </summary>
    public class AudioPoolManager : IDisposable
    {
        #region Fields

        private readonly AudioSystemManager _audioSystemManager;
        private readonly Queue<AudioSource> _availableSources = new();
        private readonly HashSet<AudioSource> _activeSources = new();
        private readonly int _maxPoolSize;
        private readonly GameObject _audioSourcePrefab;

        #endregion

        #region Properties

        public int AvailableSourceCount => _availableSources.Count;
        public int ActiveSourceCount => _activeSources.Count;
        public int TotalPoolSize => _availableSources.Count + _activeSources.Count;

        #endregion

        #region Constructor

        public AudioPoolManager(AudioSystemManager audioSystemManager, int initialSize, int maxSize, GameObject prefab)
        {
            _audioSystemManager = audioSystemManager;
            _maxPoolSize = maxSize;
            _audioSourcePrefab = prefab;

            InitializePool(initialSize);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets a pooled AudioSource or creates a new one if available
        /// </summary>
        public AudioSource GetPooledAudioSource()
        {
            AudioSource source = null;

            if (_availableSources.Count > 0)
            {
                source = _availableSources.Dequeue();
            }
            else if (TotalPoolSize < _maxPoolSize)
            {
                source = CreateNewAudioSource();
            }
            else
            {
                // Pool is full, find the oldest active source and reuse it
                source = FindOldestActiveSource();
                if (source != null)
                {
                    source.Stop();
                    _activeSources.Remove(source);
                }
            }

            if (source != null)
            {
                _activeSources.Add(source);
                PrepareAudioSource(source);
            }

            return source;
        }

        /// <summary>
        /// Returns an AudioSource to the pool for reuse
        /// </summary>
        public void ReturnToPool(AudioSource source)
        {
            if (source == null) return;

            if (_activeSources.Remove(source))
            {
                ResetAudioSource(source);
                _availableSources.Enqueue(source);
            }
        }

        /// <summary>
        /// Stops all active audio sources and returns them to pool
        /// </summary>
        public void StopAllAudio()
        {
            var activeSources = new List<AudioSource>(_activeSources);
            foreach (var source in activeSources)
            {
                if (source != null)
                {
                    source.Stop();
                    ReturnToPool(source);
                }
            }
        }

        /// <summary>
        /// Pauses all active audio sources
        /// </summary>
        public void PauseAllAudio()
        {
            foreach (var source in _activeSources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Pause();
                }
            }
        }

        /// <summary>
        /// Resumes all paused audio sources
        /// </summary>
        public void ResumeAllAudio()
        {
            foreach (var source in _activeSources)
            {
                if (source != null && !source.isPlaying)
                {
                    source.UnPause();
                }
            }
        }

        /// <summary>
        /// Gets pool status information for diagnostics
        /// </summary>
        public AudioPoolStatus GetPoolStatus()
        {
            return new AudioPoolStatus
            {
                AvailableSources = AvailableSourceCount,
                ActiveSources = ActiveSourceCount,
                TotalSources = TotalPoolSize,
                MaxPoolSize = _maxPoolSize,
                PoolUtilization = (float)ActiveSourceCount / _maxPoolSize
            };
        }

        /// <summary>
        /// Cleans up finished audio sources automatically
        /// Should be called regularly from Update loop
        /// </summary>
        public void UpdatePool()
        {
            var sourcesToReturn = new List<AudioSource>();

            foreach (var source in _activeSources)
            {
                if (source != null && !source.isPlaying)
                {
                    sourcesToReturn.Add(source);
                }
            }

            foreach (var source in sourcesToReturn)
            {
                ReturnToPool(source);
            }
        }

        #endregion

        #region Private Methods

        private void InitializePool(int initialSize)
        {
            for (int i = 0; i < initialSize; i++)
            {
                var source = CreateNewAudioSource();
                if (source != null)
                {
                    _availableSources.Enqueue(source);
                }
            }
        }

        private AudioSource CreateNewAudioSource()
        {
            GameObject audioObject;

            if (_audioSourcePrefab != null)
            {
                audioObject = UnityEngine.Object.Instantiate(_audioSourcePrefab);
            }
            else
            {
                // Create default audio source
                audioObject = new GameObject("PooledAudioSource");
                audioObject.AddComponent<AudioSource>();
            }

            if (_audioSystemManager != null)
            {
                audioObject.transform.SetParent(_audioSystemManager.transform);
            }

            var source = audioObject.GetComponent<AudioSource>();
            if (source != null)
            {
                // Setup default settings
                source.playOnAwake = false;
                source.spatialBlend = 0f; // 2D by default
                source.volume = 1f;
                source.pitch = 1f;

                return source;
            }

            UnityEngine.Object.DestroyImmediate(audioObject);
            return null;
        }

        private void PrepareAudioSource(AudioSource source)
        {
            if (source == null) return;

            source.gameObject.SetActive(true);
            ResetAudioSource(source);
        }

        private void ResetAudioSource(AudioSource source)
        {
            if (source == null) return;

            source.Stop();
            source.clip = null;
            source.volume = 1f;
            source.pitch = 1f;
            source.time = 0f;
            source.loop = false;
            source.spatialBlend = 0f;
            source.panStereo = 0f;
            source.priority = 128;
            source.mute = false;
            source.bypassEffects = false;
            source.bypassListenerEffects = false;
            source.bypassReverbZones = false;
        }

        private AudioSource FindOldestActiveSource()
        {
            AudioSource oldestSource = null;
            float earliestTime = float.MaxValue;

            foreach (var source in _activeSources)
            {
                if (source != null && source.time < earliestTime)
                {
                    earliestTime = source.time;
                    oldestSource = source;
                }
            }

            return oldestSource;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            StopAllAudio();

            // Cleanup all pooled sources
            var allSources = new List<AudioSource>(_availableSources);
            allSources.AddRange(_activeSources);

            foreach (var source in allSources)
            {
                if (source != null && source.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(source.gameObject);
                }
            }

            _availableSources.Clear();
            _activeSources.Clear();
        }

        #endregion
    }

    /// <summary>
    /// Audio pool status information for diagnostics
    /// </summary>
    [System.Serializable]
    public class AudioPoolStatus
    {
        public int AvailableSources;
        public int ActiveSources;
        public int TotalSources;
        public int MaxPoolSize;
        public float PoolUtilization;
    }
}