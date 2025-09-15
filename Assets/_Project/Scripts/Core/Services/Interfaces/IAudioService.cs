using System;
using UnityEngine;

namespace Laboratory.Core.Services
{
    /// <summary>
    /// Audio categories for volume control and mixing
    /// </summary>
    public enum AudioCategory
    {
        SFX = 0,
        Music = 1,
        Ambient = 2,
        Voice = 3,
        UI = 4
    }
    
    /// <summary>
    /// Statistics for audio system performance monitoring
    /// </summary>
    public struct AudioStatistics
    {
        public int TotalClipsPlayed;
        public int ActiveAudioSources;
        public int CachedAudioClips;
        public float MemoryUsageMB;
        public int PooledSources;
        public int PooledSourcesInUse;
    }
    
    /// <summary>
    /// Audio system configuration settings (moved to Core to avoid circular dependency)
    /// </summary>
    [System.Serializable]
    public class AudioSystemSettings
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float MasterVolume = 1f;
        [Range(0f, 1f)] public float MusicVolume = 0.8f;
        [Range(0f, 1f)] public float SFXVolume = 1f;
        [Range(0f, 1f)] public float AmbientVolume = 0.6f;
        [Range(0f, 1f)] public float VoiceVolume = 1f;
        [Range(0f, 1f)] public float UIVolume = 0.9f;

        [Header("3D Audio Settings")]
        public bool Enable3DAudio = true;
        public bool EnableOcclusion = true;
        public float DopplerLevel = 1f;
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;

        [Header("Performance Settings")]
        public int MaxSFXSources = 32;
        public int PoolInitialSize = 20;
        public bool UseAudioPooling = true;
    }

    /// <summary>
    /// Interface for audio service providing audio management capabilities
    /// </summary>
    public interface IAudioService
    {
        #region Properties
        
        /// <summary>Gets whether the audio service is initialized.</summary>
        bool IsInitialized { get; }
        
        /// <summary>Gets the current audio statistics.</summary>
        AudioStatistics Statistics { get; }
        
        /// <summary>Gets the current audio settings.</summary>
        AudioSystemSettings Settings { get; }
        
        #endregion
        
        #region Volume Control
        
        /// <summary>Sets the master volume level (0.0 to 1.0).</summary>
        void SetMasterVolume(float volume);
        
        /// <summary>Gets the current master volume level.</summary>
        float GetMasterVolume();
        
        /// <summary>Sets volume for a specific audio category.</summary>
        void SetCategoryVolume(AudioCategory category, float volume);
        
        /// <summary>Gets volume for a specific audio category.</summary>
        float GetCategoryVolume(AudioCategory category);
        
        #endregion
        
        #region Audio Playback
        
        /// <summary>Plays a sound effect clip.</summary>
        void PlaySFX(AudioClip clip);
        
        /// <summary>Plays a sound effect clip at a specific position.</summary>
        void PlaySFX(AudioClip clip, Vector3 position);
        
        /// <summary>Plays music with optional fade-in.</summary>
        void PlayMusic(AudioClip clip, bool fadeIn = true);
        
        /// <summary>Plays ambient audio.</summary>
        void PlayAmbient(AudioClip clip);
        
        /// <summary>Plays UI sound effect.</summary>
        void PlayUI(AudioClip clip);
        
        /// <summary>Plays voice/dialogue audio.</summary>
        void PlayVoice(AudioClip clip);
        
        #endregion
        
        #region Audio Control
        
        /// <summary>Stops all currently playing audio.</summary>
        void StopAllAudio();
        
        /// <summary>Pauses all currently playing audio.</summary>
        void PauseAllAudio();
        
        /// <summary>Resumes all paused audio.</summary>
        void ResumeAllAudio();
        
        /// <summary>Stops all audio of a specific category.</summary>
        void StopCategory(AudioCategory category);
        
        #endregion
        
        #region Audio Caching
        
        /// <summary>Preloads an audio clip into cache.</summary>
        void PreloadAudioClip(string name, AudioClip clip);
        
        /// <summary>Gets a cached audio clip by name.</summary>
        AudioClip GetAudioClip(string name);
        
        /// <summary>Clears the audio cache.</summary>
        void ClearCache();
        
        #endregion
        
        #region Events
        
        /// <summary>Event fired when master volume changes.</summary>
        event Action<float> OnMasterVolumeChanged;
        
        /// <summary>Event fired when category volume changes.</summary>
        event Action<AudioCategory, float> OnCategoryVolumeChanged;
        
        /// <summary>Event fired when an audio clip starts playing.</summary>
        event Action<AudioClip, AudioCategory> OnAudioClipPlayed;
        
        /// <summary>Event fired when an audio clip stops playing.</summary>
        event Action<AudioClip, AudioCategory> OnAudioClipStopped;
        
        #endregion
    }
}