using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace Laboratory.Audio
{
    /// <summary>
    /// SFX Manager for sound effects with categorization and advanced features
    /// Handles sound effect playback, limiting, and spatial positioning
    /// </summary>
    public class SFXManager
    {
        #region Fields

        private readonly AudioSystemManager _audioSystemManager;
        private readonly AudioPoolManager _audioPool;

        private readonly Dictionary<string, SFXCategory> _sfxCategories = new();
        private readonly Dictionary<string, float> _lastPlayTimes = new();
        private readonly List<SFXInstance> _activeSFX = new();

        private SFXManagerSettings _settings = new SFXManagerSettings
        {
            MaxConcurrentSFX = 32,
            MinTimeBetweenSameSFX = 0.1f,
            DefaultPriority = 128,
            DistanceFadeStart = 10f,
            DistanceFadeEnd = 50f,
            EnableSFXLimiting = true,
            EnableRandomization = true
        };

        #endregion

        #region Properties

        public int ActiveSFXCount => _activeSFX.Count;
        public SFXManagerSettings Settings => _settings;

        #endregion

        #region Constructor

        public SFXManager(AudioSystemManager audioSystemManager, AudioPoolManager audioPool)
        {
            _audioSystemManager = audioSystemManager;
            _audioPool = audioPool;

            InitializeDefaultCategories();
        }

        #endregion

        #region Public Methods - Basic SFX Playback

        /// <summary>
        /// Plays a sound effect with basic parameters
        /// </summary>
        public SFXInstance PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            return PlaySFX(clip, Vector3.zero, volume, pitch, false, "default");
        }

        /// <summary>
        /// Plays a sound effect with full parameters
        /// </summary>
        public SFXInstance PlaySFX(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, 
            bool is3D = false, string category = "default")
        {
            if (clip == null) return null;

            // Check if we can play this SFX (limiting and cooldown)
            if (!CanPlaySFX(clip, category))
            {
                return null;
            }

            // Get pooled audio source
            var audioSource = _audioPool.GetPooledAudioSource();
            if (audioSource == null) return null;

            // Apply randomization if enabled
            if (_settings.EnableRandomization && _sfxCategories.TryGetValue(category, out var sfxCategory))
            {
                volume *= Random.Range(sfxCategory.VolumeRandomization.x, sfxCategory.VolumeRandomization.y);
                pitch *= Random.Range(sfxCategory.PitchRandomization.x, sfxCategory.PitchRandomization.y);
            }

            // Configure audio source
            ConfigureAudioSource(audioSource, clip, position, volume, pitch, is3D, category);

            // Create SFX instance for tracking
            var sfxInstance = new SFXInstance
            {
                AudioSource = audioSource,
                Clip = clip,
                Category = category,
                StartTime = Time.time,
                Duration = clip.length,
                Is3D = is3D,
                Position = position,
                OriginalVolume = volume,
                OriginalPitch = pitch
            };

            // Start playback
            audioSource.Play();

            // Track this SFX
            _activeSFX.Add(sfxInstance);
            UpdateLastPlayTime(clip.name);

            // Start monitoring for completion
            _audioSystemManager.StartCoroutine(MonitorSFXCompletion(sfxInstance));

            return sfxInstance;
        }

        /// <summary>
        /// Plays a sound effect by name from preloaded clips
        /// </summary>
        public SFXInstance PlaySFX(string clipName, Vector3 position = default, float volume = 1f, 
            float pitch = 1f, bool is3D = false, string category = "default")
        {
            var clip = _audioSystemManager.GetAudioClip(clipName);
            return PlaySFX(clip, position, volume, pitch, is3D, category);
        }

        /// <summary>
        /// Plays a sound effect with random variation from a list
        /// </summary>
        public SFXInstance PlayRandomSFX(AudioClip[] clips, Vector3 position = default, 
            float volume = 1f, float pitch = 1f, bool is3D = false, string category = "default")
        {
            if (clips == null || clips.Length == 0) return null;

            var randomClip = clips[Random.Range(0, clips.Length)];
            return PlaySFX(randomClip, position, volume, pitch, is3D, category);
        }

        #endregion

        #region Public Methods - Advanced SFX Features

        /// <summary>
        /// Plays a sound effect that follows a transform
        /// </summary>
        public SFXInstance PlayFollowingSFX(AudioClip clip, Transform followTarget, float volume = 1f, 
            float pitch = 1f, string category = "default")
        {
            var sfxInstance = PlaySFX(clip, followTarget.position, volume, pitch, true, category);
            
            if (sfxInstance != null)
            {
                sfxInstance.FollowTarget = followTarget;
                _audioSystemManager.StartCoroutine(UpdateFollowingSFX(sfxInstance));
            }

            return sfxInstance;
        }

        /// <summary>
        /// Plays a sound effect with a delay
        /// </summary>
        public void PlaySFXDelayed(AudioClip clip, float delay, Vector3 position = default, 
            float volume = 1f, float pitch = 1f, bool is3D = false, string category = "default")
        {
            _audioSystemManager.StartCoroutine(PlaySFXDelayedCoroutine(clip, delay, position, volume, pitch, is3D, category));
        }

        /// <summary>
        /// Plays a looping sound effect (returns instance for manual control)
        /// </summary>
        public SFXInstance PlayLoopingSFX(AudioClip clip, Vector3 position = default, float volume = 1f, 
            float pitch = 1f, bool is3D = false, string category = "default")
        {
            var sfxInstance = PlaySFX(clip, position, volume, pitch, is3D, category);
            
            if (sfxInstance?.AudioSource != null)
            {
                sfxInstance.AudioSource.loop = true;
                sfxInstance.IsLooping = true;
            }

            return sfxInstance;
        }

        /// <summary>
        /// Stops a specific SFX instance
        /// </summary>
        public void StopSFX(SFXInstance sfxInstance, float fadeTime = 0f)
        {
            if (sfxInstance?.AudioSource == null) return;

            if (fadeTime > 0f)
            {
                _audioSystemManager.StartCoroutine(FadeOutSFX(sfxInstance, fadeTime));
            }
            else
            {
                sfxInstance.AudioSource.Stop();
                CompleteSFX(sfxInstance);
            }
        }

        /// <summary>
        /// Stops all SFX in a specific category
        /// </summary>
        public void StopSFXCategory(string category, float fadeTime = 0f)
        {
            var sfxToStop = new List<SFXInstance>();

            foreach (var sfx in _activeSFX)
            {
                if (sfx.Category == category)
                {
                    sfxToStop.Add(sfx);
                }
            }

            foreach (var sfx in sfxToStop)
            {
                StopSFX(sfx, fadeTime);
            }
        }

        /// <summary>
        /// Stops all currently playing SFX
        /// </summary>
        public void StopAllSFX(float fadeTime = 0f)
        {
            var allSFX = new List<SFXInstance>(_activeSFX);

            foreach (var sfx in allSFX)
            {
                StopSFX(sfx, fadeTime);
            }
        }

        #endregion

        #region Public Methods - Category Management

        /// <summary>
        /// Defines a new SFX category with specific settings
        /// </summary>
        public void DefineSFXCategory(string categoryName, SFXCategory categorySettings)
        {
            _sfxCategories[categoryName] = categorySettings;
        }

        /// <summary>
        /// Sets the volume for an entire SFX category
        /// </summary>
        public void SetCategoryVolume(string category, float volume)
        {
            if (_sfxCategories.TryGetValue(category, out var sfxCategory))
            {
                sfxCategory.Volume = Mathf.Clamp01(volume);

                // Update all active SFX in this category
                foreach (var sfx in _activeSFX)
                {
                    if (sfx.Category == category && sfx.AudioSource != null)
                    {
                        sfx.AudioSource.volume = sfx.OriginalVolume * volume;
                    }
                }
            }
        }

        /// <summary>
        /// Gets all active SFX instances in a category
        /// </summary>
        public List<SFXInstance> GetActiveSFXInCategory(string category)
        {
            var result = new List<SFXInstance>();

            foreach (var sfx in _activeSFX)
            {
                if (sfx.Category == category)
                {
                    result.Add(sfx);
                }
            }

            return result;
        }

        #endregion

        #region Public Methods - Settings and Management

        /// <summary>
        /// Updates SFX manager settings
        /// </summary>
        public void UpdateSettings(SFXManagerSettings newSettings)
        {
            _settings = newSettings;
        }

        /// <summary>
        /// Updates active SFX (call from main update loop)
        /// </summary>
        public void UpdateSFX()
        {
            // Update following SFX positions
            foreach (var sfx in _activeSFX)
            {
                if (sfx.FollowTarget != null && sfx.AudioSource != null)
                {
                    sfx.AudioSource.transform.position = sfx.FollowTarget.position;
                    sfx.Position = sfx.FollowTarget.position;
                }
            }

            // Clean up completed SFX
            var completedSFX = new List<SFXInstance>();
            foreach (var sfx in _activeSFX)
            {
                if (sfx.AudioSource == null || (!sfx.IsLooping && !sfx.AudioSource.isPlaying))
                {
                    completedSFX.Add(sfx);
                }
            }

            foreach (var sfx in completedSFX)
            {
                CompleteSFX(sfx);
            }
        }

        /// <summary>
        /// Gets SFX statistics for debugging
        /// </summary>
        public SFXStatistics GetStatistics()
        {
            var stats = new SFXStatistics
            {
                ActiveSFXCount = _activeSFX.Count,
                TotalSFXCategories = _sfxCategories.Count,
                SFXByCategory = new Dictionary<string, int>()
            };

            foreach (var sfx in _activeSFX)
            {
                if (stats.SFXByCategory.ContainsKey(sfx.Category))
                {
                    stats.SFXByCategory[sfx.Category]++;
                }
                else
                {
                    stats.SFXByCategory[sfx.Category] = 1;
                }
            }

            return stats;
        }

        #endregion

        #region Private Methods

        private void InitializeDefaultCategories()
        {
            // Default category
            DefineSFXCategory("default", new SFXCategory
            {
                Volume = 1f,
                MaxConcurrent = 8,
                Priority = 128,
                VolumeRandomization = new Vector2(0.9f, 1.1f),
                PitchRandomization = new Vector2(0.95f, 1.05f)
            });

            // Weapon sounds
            DefineSFXCategory("weapons", new SFXCategory
            {
                Volume = 1f,
                MaxConcurrent = 5,
                Priority = 64,
                VolumeRandomization = new Vector2(0.8f, 1.2f),
                PitchRandomization = new Vector2(0.9f, 1.1f)
            });

            // UI sounds
            DefineSFXCategory("ui", new SFXCategory
            {
                Volume = 0.7f,
                MaxConcurrent = 3,
                Priority = 32,
                VolumeRandomization = Vector2.one,
                PitchRandomization = Vector2.one
            });

            // Footsteps
            DefineSFXCategory("footsteps", new SFXCategory
            {
                Volume = 0.6f,
                MaxConcurrent = 4,
                Priority = 100,
                VolumeRandomization = new Vector2(0.7f, 1.3f),
                PitchRandomization = new Vector2(0.8f, 1.2f)
            });

            // Impact sounds
            DefineSFXCategory("impacts", new SFXCategory
            {
                Volume = 0.9f,
                MaxConcurrent = 10,
                Priority = 80,
                VolumeRandomization = new Vector2(0.6f, 1.4f),
                PitchRandomization = new Vector2(0.7f, 1.3f)
            });
        }

        private bool CanPlaySFX(AudioClip clip, string category)
        {
            // Check global SFX limit
            if (_settings.EnableSFXLimiting && _activeSFX.Count >= _settings.MaxConcurrentSFX)
            {
                return false;
            }

            // Check category limit
            if (_sfxCategories.TryGetValue(category, out var sfxCategory))
            {
                int categoryCount = GetActiveSFXInCategory(category).Count;
                if (categoryCount >= sfxCategory.MaxConcurrent)
                {
                    return false;
                }
            }

            // Check cooldown for same SFX
            if (_settings.MinTimeBetweenSameSFX > 0f)
            {
                if (_lastPlayTimes.TryGetValue(clip.name, out var lastPlayTime))
                {
                    if (Time.time - lastPlayTime < _settings.MinTimeBetweenSameSFX)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void ConfigureAudioSource(AudioSource audioSource, AudioClip clip, Vector3 position, 
            float volume, float pitch, bool is3D, string category)
        {
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.loop = false;
            audioSource.playOnAwake = false;

            // Configure spatial settings
            if (is3D)
            {
                audioSource.transform.position = position;
                audioSource.spatialBlend = 1f;
                audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
                audioSource.minDistance = _settings.DistanceFadeStart;
                audioSource.maxDistance = _settings.DistanceFadeEnd;
            }
            else
            {
                audioSource.spatialBlend = 0f;
            }

            // Apply category settings
            if (_sfxCategories.TryGetValue(category, out var sfxCategory))
            {
                audioSource.volume *= sfxCategory.Volume;
                audioSource.priority = sfxCategory.Priority;
            }
            else
            {
                audioSource.priority = _settings.DefaultPriority;
            }
        }

        private void UpdateLastPlayTime(string clipName)
        {
            _lastPlayTimes[clipName] = Time.time;
        }

        private void CompleteSFX(SFXInstance sfxInstance)
        {
            _activeSFX.Remove(sfxInstance);
            
            if (sfxInstance.AudioSource != null)
            {
                _audioPool.ReturnToPool(sfxInstance.AudioSource);
            }
        }

        private IEnumerator MonitorSFXCompletion(SFXInstance sfxInstance)
        {
            while (sfxInstance.AudioSource != null && sfxInstance.AudioSource.isPlaying && 
                   (sfxInstance.IsLooping || Time.time < sfxInstance.StartTime + sfxInstance.Duration))
            {
                yield return null;
            }

            if (!sfxInstance.IsLooping)
            {
                CompleteSFX(sfxInstance);
            }
        }

        private IEnumerator UpdateFollowingSFX(SFXInstance sfxInstance)
        {
            while (sfxInstance.AudioSource != null && sfxInstance.FollowTarget != null)
            {
                sfxInstance.AudioSource.transform.position = sfxInstance.FollowTarget.position;
                sfxInstance.Position = sfxInstance.FollowTarget.position;
                yield return null;
            }
        }

        private IEnumerator PlaySFXDelayedCoroutine(AudioClip clip, float delay, Vector3 position, 
            float volume, float pitch, bool is3D, string category)
        {
            yield return new WaitForSeconds(delay);
            PlaySFX(clip, position, volume, pitch, is3D, category);
        }

        private IEnumerator FadeOutSFX(SFXInstance sfxInstance, float fadeTime)
        {
            if (sfxInstance.AudioSource == null) yield break;

            float startVolume = sfxInstance.AudioSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime && sfxInstance.AudioSource != null && sfxInstance.AudioSource.isPlaying)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeTime;
                sfxInstance.AudioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (sfxInstance.AudioSource != null)
            {
                sfxInstance.AudioSource.Stop();
                CompleteSFX(sfxInstance);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents an active SFX instance
    /// </summary>
    public class SFXInstance
    {
        public AudioSource AudioSource;
        public AudioClip Clip;
        public string Category;
        public float StartTime;
        public float Duration;
        public bool Is3D;
        public Vector3 Position;
        public Transform FollowTarget;
        public float OriginalVolume;
        public float OriginalPitch;
        public bool IsLooping;

        public float Progress => Duration > 0 ? Mathf.Clamp01((Time.time - StartTime) / Duration) : 1f;
        public bool IsCompleted => !IsLooping && Time.time >= StartTime + Duration;
    }

    /// <summary>
    /// SFX category configuration
    /// </summary>
    [System.Serializable]
    public class SFXCategory
    {
        public float Volume = 1f;
        public int MaxConcurrent = 5;
        public int Priority = 128;
        public Vector2 VolumeRandomization = Vector2.one;
        public Vector2 PitchRandomization = Vector2.one;
        public float CooldownTime = 0f;
        public bool AllowInterruption = true;
    }

    /// <summary>
    /// SFX Manager settings
    /// </summary>
    [System.Serializable]
    public class SFXManagerSettings
    {
        public int MaxConcurrentSFX = 32;
        public float MinTimeBetweenSameSFX = 0.1f;
        public int DefaultPriority = 128;
        public float DistanceFadeStart = 10f;
        public float DistanceFadeEnd = 50f;
        public bool EnableSFXLimiting = true;
        public bool EnableRandomization = true;
        public bool EnableSpatialAudio = true;
        public bool EnableOcclusion = false;
    }

    /// <summary>
    /// SFX statistics for debugging
    /// </summary>
    public class SFXStatistics
    {
        public int ActiveSFXCount;
        public int TotalSFXCategories;
        public Dictionary<string, int> SFXByCategory;
        public float AveragePlaybackVolume;
        public int SFXPlayedThisFrame;
    }

    #endregion
}