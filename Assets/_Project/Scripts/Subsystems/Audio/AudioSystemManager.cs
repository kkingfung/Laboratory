using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Services;

namespace Laboratory.Audio
{
    /// <summary>
    /// Comprehensive Audio System Manager for Laboratory Unity Project
    /// Version 1.0 - Complete audio management with 3D audio, pooling, and dynamic mixing
    /// </summary>
    public class AudioSystemManager : MonoBehaviour, IAudioService
    {
        #region Fields

        [Header("Audio Configuration")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup voiceGroup;
        [SerializeField] private AudioMixerGroup uiGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource uiSource;

        [Header("Audio Settings")]
        [SerializeField] private AudioSystemSettings audioSettings;
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool enable3DAudio = true;
        [SerializeField] private bool enableAudioOcclusion = true;

        [Header("Pooling")]
        [SerializeField] private int initialPoolSize = 20;
        [SerializeField] private int maxPoolSize = 50;
        [SerializeField] private GameObject audioSourcePrefab;

        private AudioPoolManager _audioPool;
        private Audio3DManager _audio3DManager;
        private MusicManager _musicManager;
        private SFXManager _sfxManager;
        private AudioMixerController _mixerController;
        private AudioEventSystem _audioEventSystem;

        private IEventBus _eventBus;
        private Dictionary<string, AudioClip> _audioClipCache = new();
        private Dictionary<AudioCategory, float> _categoryVolumes = new();
        private List<ActiveAudioTrack> _activeAudioTracks = new();

        // Audio statistics
        private AudioStatistics _audioStats = new();

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }
        public AudioSystemSettings Settings => audioSettings;
        public AudioStatistics Statistics => _audioStats;

        #endregion

        #region Events

        public event Action<AudioClip, AudioCategory> OnAudioClipPlayed;
        public event Action<AudioClip> OnAudioClipStopped;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<AudioCategory, float> OnCategoryVolumeChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeAudioSystem();
        }

        private void Start()
        {
            RegisterWithServices();
            LoadAudioSettings();
            StartAudioStatisticsTracking();
        }

        private void Update()
        {
            UpdateActiveAudioTracks();
            UpdateAudioStatistics();
        }

        private void OnDestroy()
        {
            CleanupAudioSystem();
        }

        #endregion

        #region Initialization

        private void InitializeAudioSystem()
        {
            try
            {
                // Initialize core components
                _audioPool = new AudioPoolManager(this, initialPoolSize, maxPoolSize, audioSourcePrefab);
                _audio3DManager = new Audio3DManager(this);
                _musicManager = new MusicManager(this, musicSource);
                _sfxManager = new SFXManager(this, _audioPool);
                _mixerController = new AudioMixerController(masterMixer);
                _audioEventSystem = new AudioEventSystem();

                // Setup default volumes
                SetupDefaultVolumes();

                // Get event bus
                _eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();

                IsInitialized = true;

                if (enableDebugLogs)
                    Debug.Log("[AudioSystemManager] Successfully initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AudioSystemManager] Failed to initialize: {ex.Message}");
                IsInitialized = false;
            }
        }

        private void RegisterWithServices()
        {
            if (GlobalServiceProvider.IsInitialized)
            {
                var container = GlobalServiceProvider.GetContainer();
                container?.RegisterInstance<IAudioService>(this);
            }
        }

        private void SetupDefaultVolumes()
        {
            _categoryVolumes[AudioCategory.Master] = 1f;
            _categoryVolumes[AudioCategory.Music] = 0.8f;
            _categoryVolumes[AudioCategory.SFX] = 1f;
            _categoryVolumes[AudioCategory.Ambient] = 0.6f;
            _categoryVolumes[AudioCategory.Voice] = 1f;
            _categoryVolumes[AudioCategory.UI] = 0.9f;
        }

        #endregion

        #region IAudioService Implementation

        public void PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 1f)
        {
            if (!IsInitialized || clip == null) return;

            _musicManager.PlayMusic(clip, loop, fadeTime);
            TrackAudioPlayback(clip, AudioCategory.Music);

            OnAudioClipPlayed?.Invoke(clip, AudioCategory.Music);
        }

        public void StopMusic(float fadeTime = 1f)
        {
            if (!IsInitialized) return;

            var currentClip = musicSource?.clip;
            _musicManager.StopMusic(fadeTime);
            
            if (currentClip != null)
            {
                OnAudioClipStopped?.Invoke(currentClip);
            }
        }

        public void PlaySFX(AudioClip clip, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            if (!IsInitialized || clip == null) return;

            if (position.HasValue && enable3DAudio)
            {
                _audio3DManager.PlaySFXAt3DPosition(clip, position.Value, volume, pitch);
            }
            else
            {
                _sfxManager.PlaySFX(clip, volume, pitch);
            }

            TrackAudioPlayback(clip, AudioCategory.SFX);
            OnAudioClipPlayed?.Invoke(clip, AudioCategory.SFX);
        }

        public void PlaySFX(string clipName, Vector3? position = null, float volume = 1f, float pitch = 1f)
        {
            var clip = GetAudioClip(clipName);
            if (clip != null)
            {
                PlaySFX(clip, position, volume, pitch);
            }
            else
            {
                Debug.LogWarning($"[AudioSystemManager] Audio clip not found: {clipName}");
            }
        }

        public void PlayAmbient(AudioClip clip, bool loop = true, float volume = 1f)
        {
            if (!IsInitialized || clip == null) return;

            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.volume = volume * _categoryVolumes[AudioCategory.Ambient];
            ambientSource.Play();

            TrackAudioPlayback(clip, AudioCategory.Ambient);
            OnAudioClipPlayed?.Invoke(clip, AudioCategory.Ambient);
        }

        public void PlayUI(AudioClip clip, float volume = 1f)
        {
            if (!IsInitialized || clip == null) return;

            uiSource.clip = clip;
            uiSource.volume = volume * _categoryVolumes[AudioCategory.UI];
            uiSource.pitch = 1f;
            uiSource.Play();

            TrackAudioPlayback(clip, AudioCategory.UI);
            OnAudioClipPlayed?.Invoke(clip, AudioCategory.UI);
        }

        public void SetMasterVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            _categoryVolumes[AudioCategory.Master] = volume;
            _mixerController.SetMasterVolume(volume);

            OnMasterVolumeChanged?.Invoke(volume);
            _eventBus?.Publish(new AudioVolumeChangedEvent(AudioCategory.Master, volume));

            if (enableDebugLogs)
                Debug.Log($"[AudioSystemManager] Master volume set to: {volume:F2}");
        }

        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            volume = Mathf.Clamp01(volume);
            _categoryVolumes[category] = volume;
            _mixerController.SetCategoryVolume(category, volume);

            OnCategoryVolumeChanged?.Invoke(category, volume);
            _eventBus?.Publish(new AudioVolumeChangedEvent(category, volume));

            if (enableDebugLogs)
                Debug.Log($"[AudioSystemManager] {category} volume set to: {volume:F2}");
        }

        public float GetMasterVolume()
        {
            return _categoryVolumes.TryGetValue(AudioCategory.Master, out var volume) ? volume : 1f;
        }

        public float GetCategoryVolume(AudioCategory category)
        {
            return _categoryVolumes.TryGetValue(category, out var volume) ? volume : 1f;
        }

        public void StopAllAudio()
        {
            // Store clips before stopping to trigger events
            var musicClip = musicSource?.clip;
            var ambientClip = ambientSource?.clip;
            var uiClip = uiSource?.clip;

            _musicManager?.StopMusic(0f);
            ambientSource?.Stop();
            uiSource?.Stop();
            _audioPool?.StopAllAudio();

            // Trigger stop events for each stopped clip
            if (musicClip != null) OnAudioClipStopped?.Invoke(musicClip);
            if (ambientClip != null) OnAudioClipStopped?.Invoke(ambientClip);
            if (uiClip != null) OnAudioClipStopped?.Invoke(uiClip);

            _activeAudioTracks.Clear();

            if (enableDebugLogs)
                Debug.Log("[AudioSystemManager] Stopped all audio");
        }

        public void PauseAllAudio()
        {
            _musicManager?.PauseMusic();
            ambientSource?.Pause();
            uiSource?.Pause();
            _audioPool?.PauseAllAudio();

            if (enableDebugLogs)
                Debug.Log("[AudioSystemManager] Paused all audio");
        }

        public void ResumeAllAudio()
        {
            _musicManager?.ResumeMusic();
            ambientSource?.UnPause();
            uiSource?.UnPause();
            _audioPool?.ResumeAllAudio();

            if (enableDebugLogs)
                Debug.Log("[AudioSystemManager] Resumed all audio");
        }

        #endregion

        #region Audio Management

        public AudioSource GetPooledAudioSource()
        {
            return _audioPool?.GetPooledAudioSource();
        }

        public void ReturnAudioSourceToPool(AudioSource source)
        {
            _audioPool?.ReturnToPool(source);
        }

        public void PreloadAudioClip(string clipName, AudioClip clip)
        {
            if (clip != null && !_audioClipCache.ContainsKey(clipName))
            {
                _audioClipCache[clipName] = clip;
                
                if (enableDebugLogs)
                    Debug.Log($"[AudioSystemManager] Preloaded audio clip: {clipName}");
            }
        }

        public AudioClip GetAudioClip(string clipName)
        {
            if (_audioClipCache.TryGetValue(clipName, out var clip))
            {
                return clip;
            }

            // Try to load from Resources as fallback
            clip = Resources.Load<AudioClip>($"Audio/{clipName}");
            if (clip != null)
            {
                _audioClipCache[clipName] = clip;
                return clip;
            }

            return null;
        }

        public void SetListenerPosition(Transform listenerTransform)
        {
            _audio3DManager?.SetListenerPosition(listenerTransform);
        }

        public void EnableAudioOcclusion(bool enable)
        {
            enableAudioOcclusion = enable;
            _audio3DManager?.EnableOcclusion(enable);
        }

        #endregion

        #region Private Methods

        private void LoadAudioSettings()
        {
            if (audioSettings != null)
            {
                SetMasterVolume(audioSettings.MasterVolume);
                SetCategoryVolume(AudioCategory.Music, audioSettings.MusicVolume);
                SetCategoryVolume(AudioCategory.SFX, audioSettings.SFXVolume);
                SetCategoryVolume(AudioCategory.Ambient, audioSettings.AmbientVolume);
                SetCategoryVolume(AudioCategory.Voice, audioSettings.VoiceVolume);
                SetCategoryVolume(AudioCategory.UI, audioSettings.UIVolume);
            }
        }

        private void TrackAudioPlayback(AudioClip clip, AudioCategory category)
        {
            var track = new ActiveAudioTrack
            {
                Clip = clip,
                Category = category,
                StartTime = Time.time,
                Duration = clip.length
            };

            _activeAudioTracks.Add(track);
            _audioStats.TotalClipsPlayed++;
            _audioStats.ClipsPlayedByCategory[category]++;
        }

        private void UpdateActiveAudioTracks()
        {
            for (int i = _activeAudioTracks.Count - 1; i >= 0; i--)
            {
                var track = _activeAudioTracks[i];
                if (Time.time >= track.StartTime + track.Duration)
                {
                    _activeAudioTracks.RemoveAt(i);
                }
            }
        }

        private void UpdateAudioStatistics()
        {
            _audioStats.ActiveAudioSources = GetActiveAudioSourceCount();
            _audioStats.PooledAudioSources = _audioPool?.AvailableSourceCount ?? 0;
            _audioStats.CachedAudioClips = _audioClipCache.Count;
        }

        private int GetActiveAudioSourceCount()
        {
            int count = 0;
            if (musicSource != null && musicSource.isPlaying) count++;
            if (ambientSource != null && ambientSource.isPlaying) count++;
            if (uiSource != null && uiSource.isPlaying) count++;
            count += _audioPool?.ActiveSourceCount ?? 0;
            return count;
        }

        private void StartAudioStatisticsTracking()
        {
            _audioStats.SessionStartTime = DateTime.Now;
            
            // Initialize category counters
            foreach (AudioCategory category in Enum.GetValues(typeof(AudioCategory)))
            {
                _audioStats.ClipsPlayedByCategory[category] = 0;
            }
        }

        private void CleanupAudioSystem()
        {
            StopAllAudio();
            _audioPool?.Dispose();
            _audioClipCache.Clear();
            _activeAudioTracks.Clear();

            if (enableDebugLogs)
                Debug.Log("[AudioSystemManager] Audio system cleaned up");
        }

        #endregion

        #region Debug and Diagnostics

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogAudioStatistics()
        {
            Debug.Log($"[AudioSystemManager] Audio Statistics:\n" +
                     $"Total Clips Played: {_audioStats.TotalClipsPlayed}\n" +
                     $"Active Audio Sources: {_audioStats.ActiveAudioSources}\n" +
                     $"Pooled Audio Sources: {_audioStats.PooledAudioSources}\n" +
                     $"Cached Audio Clips: {_audioStats.CachedAudioClips}\n" +
                     $"Session Duration: {DateTime.Now - _audioStats.SessionStartTime}");
        }

        public AudioSystemDiagnostics GetDiagnostics()
        {
            return new AudioSystemDiagnostics
            {
                Statistics = _audioStats,
                ActiveTracks = new List<ActiveAudioTrack>(_activeAudioTracks),
                CategoryVolumes = new Dictionary<AudioCategory, float>(_categoryVolumes),
                IsInitialized = IsInitialized,
                PoolStatus = _audioPool?.GetPoolStatus(),
                MixerStatus = _mixerController?.GetMixerStatus()
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Audio categories for volume control and organization
    /// </summary>
    public enum AudioCategory
    {
        Master = 0,
        Music = 1,
        SFX = 2,
        Ambient = 3,
        Voice = 4,
        UI = 5
    }

    /// <summary>
    /// Represents an active audio track
    /// </summary>
    [System.Serializable]
    public class ActiveAudioTrack
    {
        public AudioClip Clip;
        public AudioCategory Category;
        public float StartTime;
        public float Duration;
        public Vector3? Position;

        public float Progress => Duration > 0 ? Mathf.Clamp01((Time.time - StartTime) / Duration) : 1f;
        public bool IsCompleted => Time.time >= StartTime + Duration;
    }

    /// <summary>
    /// Audio system statistics for monitoring and debugging
    /// </summary>
    [System.Serializable]
    public class AudioStatistics
    {
        public DateTime SessionStartTime;
        public int TotalClipsPlayed;
        public int ActiveAudioSources;
        public int PooledAudioSources;
        public int CachedAudioClips;
        public Dictionary<AudioCategory, int> ClipsPlayedByCategory = new();
        
        public TimeSpan SessionDuration => DateTime.Now - SessionStartTime;
    }

    /// <summary>
    /// Comprehensive diagnostics information
    /// </summary>
    public class AudioSystemDiagnostics
    {
        public AudioStatistics Statistics;
        public List<ActiveAudioTrack> ActiveTracks;
        public Dictionary<AudioCategory, float> CategoryVolumes;
        public bool IsInitialized;
        public object PoolStatus;
        public object MixerStatus;
    }

    #endregion

    #region Interface

    /// <summary>
    /// Audio service interface for dependency injection
    /// </summary>
    public interface IAudioService
    {
        bool IsInitialized { get; }
        AudioSystemSettings Settings { get; }
        AudioStatistics Statistics { get; }

        // Music
        void PlayMusic(AudioClip clip, bool loop = true, float fadeTime = 1f);
        void StopMusic(float fadeTime = 1f);

        // Sound Effects
        void PlaySFX(AudioClip clip, Vector3? position = null, float volume = 1f, float pitch = 1f);
        void PlaySFX(string clipName, Vector3? position = null, float volume = 1f, float pitch = 1f);

        // Ambient
        void PlayAmbient(AudioClip clip, bool loop = true, float volume = 1f);

        // UI
        void PlayUI(AudioClip clip, float volume = 1f);

        // Volume Control
        void SetMasterVolume(float volume);
        void SetCategoryVolume(AudioCategory category, float volume);
        float GetMasterVolume();
        float GetCategoryVolume(AudioCategory category);

        // Playback Control
        void StopAllAudio();
        void PauseAllAudio();
        void ResumeAllAudio();

        // Audio Management
        AudioClip GetAudioClip(string clipName);
        void PreloadAudioClip(string clipName, AudioClip clip);
        void SetListenerPosition(Transform listenerTransform);
        void EnableAudioOcclusion(bool enable);

        // Events
        event Action<AudioClip, AudioCategory> OnAudioClipPlayed;
        event Action<AudioClip> OnAudioClipStopped;
        event Action<float> OnMasterVolumeChanged;
        event Action<AudioCategory, float> OnCategoryVolumeChanged;
    }

    #endregion

    #region Events

    public class AudioVolumeChangedEvent
    {
        public AudioCategory Category { get; }
        public float Volume { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public AudioVolumeChangedEvent(AudioCategory category, float volume)
        {
            Category = category;
            Volume = volume;
        }
    }

    public class AudioClipPlayedEvent
    {
        public AudioClip Clip { get; }
        public AudioCategory Category { get; }
        public Vector3? Position { get; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public AudioClipPlayedEvent(AudioClip clip, AudioCategory category, Vector3? position = null)
        {
            Clip = clip;
            Category = category;
            Position = position;
        }
    }

    #endregion
}