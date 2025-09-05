using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Laboratory.Audio
{
    /// <summary>
    /// Audio system configuration settings
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSystemSettings", menuName = "Laboratory/Audio/Audio System Settings")]
    [Serializable]
    public class AudioSystemSettings : ScriptableObject
    {
        [Header("Volume Settings")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        
        [Range(0f, 1f)]
        public float musicVolume = 0.8f;
        
        [Range(0f, 1f)]
        public float sfxVolume = 1f;
        
        [Range(0f, 1f)]
        public float ambientVolume = 0.6f;
        
        [Range(0f, 1f)]
        public float voiceVolume = 1f;
        
        [Range(0f, 1f)]
        public float uiVolume = 0.8f;

        [Header("3D Audio Settings")]
        public bool enable3DAudio = true;
        public float dopplerLevel = 1f;
        public float rolloffScale = 1f;
        public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
        
        [Header("Performance Settings")]
        public int maxConcurrentSounds = 32;
        public int audioPoolSize = 20;
        public bool enableAudioCompression = true;
        
        [Header("Quality Settings")]
        [Range(0, 3)]
        public int audioQuality = 2; // 0 = Low, 1 = Medium, 2 = High, 3 = Best
        
        public bool enableLowLatencyMode = false;
        public bool enableHardwareAcceleration = true;

        [Header("Mixer Settings")]
        public AudioMixer masterMixer;
        public string musicMixerGroup = "Music";
        public string sfxMixerGroup = "SFX";
        public string ambientMixerGroup = "Ambient";
        public string voiceMixerGroup = "Voice";
        public string uiMixerGroup = "UI";

        /// <summary>
        /// Validates the settings and ensures they are within acceptable ranges
        /// </summary>
        public void ValidateSettings()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume = Mathf.Clamp01(musicVolume);
            sfxVolume = Mathf.Clamp01(sfxVolume);
            ambientVolume = Mathf.Clamp01(ambientVolume);
            voiceVolume = Mathf.Clamp01(voiceVolume);
            uiVolume = Mathf.Clamp01(uiVolume);
            
            dopplerLevel = Mathf.Clamp(dopplerLevel, 0f, 5f);
            rolloffScale = Mathf.Clamp(rolloffScale, 0.1f, 5f);
            maxConcurrentSounds = Mathf.Clamp(maxConcurrentSounds, 1, 64);
            audioPoolSize = Mathf.Clamp(audioPoolSize, 5, 50);
            audioQuality = Mathf.Clamp(audioQuality, 0, 3);
        }

        // Properties for backward compatibility with uppercase names
        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SFXVolume => sfxVolume;
        public float AmbientVolume => ambientVolume;
        public float VoiceVolume => voiceVolume;
        public float UIVolume => uiVolume;

        /// <summary>
        /// Resets all settings to default values
        /// </summary>
        public void ResetToDefaults()
        {
            masterVolume = 1f;
            musicVolume = 0.8f;
            sfxVolume = 1f;
            ambientVolume = 0.6f;
            voiceVolume = 1f;
            uiVolume = 0.8f;
            
            enable3DAudio = true;
            dopplerLevel = 1f;
            rolloffScale = 1f;
            rolloffMode = AudioRolloffMode.Logarithmic;
            
            maxConcurrentSounds = 32;
            audioPoolSize = 20;
            enableAudioCompression = true;
            audioQuality = 2;
            enableLowLatencyMode = false;
            enableHardwareAcceleration = true;
        }
    }
}
