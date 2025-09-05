using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Laboratory.Audio
{
    /// <summary>
    /// Audio Mixer Controller for dynamic mixing and advanced audio processing
    /// Manages audio mixer parameters, snapshots, and real-time audio adjustments
    /// </summary>
    public class AudioMixerController
    {
        #region Fields

        private readonly AudioMixer _masterMixer;
        private readonly Dictionary<string, float> _parameterCache = new();
        private readonly Dictionary<string, AudioMixerSnapshot> _snapshots = new();
        
        private AudioMixerSnapshot _currentSnapshot;
        private string _currentSnapshotName;

        // Parameter names (should match your AudioMixer exposed parameters)
        private const string MASTER_VOLUME_PARAM = "MasterVolume";
        private const string MUSIC_VOLUME_PARAM = "MusicVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";
        private const string AMBIENT_VOLUME_PARAM = "AmbientVolume";
        private const string VOICE_VOLUME_PARAM = "VoiceVolume";
        private const string UI_VOLUME_PARAM = "UIVolume";

        private const string MASTER_LOWPASS_PARAM = "MasterLowPass";
        private const string MASTER_HIGHPASS_PARAM = "MasterHighPass";
        private const string REVERB_LEVEL_PARAM = "ReverbLevel";
        private const string ECHO_LEVEL_PARAM = "EchoLevel";

        #endregion

        #region Properties

        public AudioMixer MasterMixer => _masterMixer;
        public string CurrentSnapshotName => _currentSnapshotName;

        #endregion

        #region Constructor

        public AudioMixerController(AudioMixer masterMixer)
        {
            _masterMixer = masterMixer;
            InitializeMixer();
        }

        #endregion

        #region Public Methods - Volume Control

        /// <summary>
        /// Sets the master volume (0.0 to 1.0)
        /// </summary>
        public void SetMasterVolume(float normalizedVolume)
        {
            float dbValue = NormalizedToDecibel(normalizedVolume);
            SetMixerParameter(MASTER_VOLUME_PARAM, dbValue);
        }

        /// <summary>
        /// Sets volume for a specific audio category
        /// </summary>
        public void SetCategoryVolume(AudioCategory category, float normalizedVolume)
        {
            string paramName = GetVolumeParameterName(category);
            if (!string.IsNullOrEmpty(paramName))
            {
                float dbValue = NormalizedToDecibel(normalizedVolume);
                SetMixerParameter(paramName, dbValue);
            }
        }

        /// <summary>
        /// Gets the current master volume (0.0 to 1.0)
        /// </summary>
        public float GetMasterVolume()
        {
            float dbValue = GetMixerParameter(MASTER_VOLUME_PARAM);
            return DecibelToNormalized(dbValue);
        }

        /// <summary>
        /// Gets volume for a specific audio category
        /// </summary>
        public float GetCategoryVolume(AudioCategory category)
        {
            string paramName = GetVolumeParameterName(category);
            if (!string.IsNullOrEmpty(paramName))
            {
                float dbValue = GetMixerParameter(paramName);
                return DecibelToNormalized(dbValue);
            }
            return 1f;
        }

        #endregion

        #region Public Methods - Audio Effects

        /// <summary>
        /// Sets low-pass filter cutoff frequency
        /// </summary>
        public void SetLowPassFilter(float cutoffFrequency)
        {
            cutoffFrequency = Mathf.Clamp(cutoffFrequency, 20f, 22000f);
            SetMixerParameter(MASTER_LOWPASS_PARAM, cutoffFrequency);
        }

        /// <summary>
        /// Sets high-pass filter cutoff frequency
        /// </summary>
        public void SetHighPassFilter(float cutoffFrequency)
        {
            cutoffFrequency = Mathf.Clamp(cutoffFrequency, 20f, 22000f);
            SetMixerParameter(MASTER_HIGHPASS_PARAM, cutoffFrequency);
        }

        /// <summary>
        /// Sets reverb level (0.0 to 1.0)
        /// </summary>
        public void SetReverbLevel(float level)
        {
            level = Mathf.Clamp01(level);
            float dbValue = NormalizedToDecibel(level);
            SetMixerParameter(REVERB_LEVEL_PARAM, dbValue);
        }

        /// <summary>
        /// Sets echo level (0.0 to 1.0)
        /// </summary>
        public void SetEchoLevel(float level)
        {
            level = Mathf.Clamp01(level);
            float dbValue = NormalizedToDecibel(level);
            SetMixerParameter(ECHO_LEVEL_PARAM, dbValue);
        }

        /// <summary>
        /// Applies underwater effect
        /// </summary>
        public void ApplyUnderwaterEffect(bool enable, float intensity = 1f)
        {
            if (enable)
            {
                SetLowPassFilter(Mathf.Lerp(22000f, 800f, intensity));
                SetReverbLevel(0.6f * intensity);
                SetEchoLevel(0.3f * intensity);
            }
            else
            {
                ClearAudioEffects();
            }
        }

        /// <summary>
        /// Applies muffled effect (for being in a confined space)
        /// </summary>
        public void ApplyMuffledEffect(bool enable, float intensity = 1f)
        {
            if (enable)
            {
                SetLowPassFilter(Mathf.Lerp(22000f, 1500f, intensity));
                SetHighPassFilter(Mathf.Lerp(20f, 200f, intensity));
            }
            else
            {
                ClearAudioEffects();
            }
        }

        /// <summary>
        /// Clears all audio effects to default values
        /// </summary>
        public void ClearAudioEffects()
        {
            SetLowPassFilter(22000f);  // No low-pass filtering
            SetHighPassFilter(20f);    // No high-pass filtering
            SetReverbLevel(0f);        // No reverb
            SetEchoLevel(0f);          // No echo
        }

        #endregion

        #region Public Methods - Snapshots

        /// <summary>
        /// Transitions to a mixer snapshot
        /// </summary>
        public void TransitionToSnapshot(string snapshotName, float transitionTime = 1f)
        {
            if (_snapshots.TryGetValue(snapshotName, out var snapshot))
            {
                snapshot.TransitionTo(transitionTime);
                _currentSnapshot = snapshot;
                _currentSnapshotName = snapshotName;
            }
            else
            {
                Debug.LogWarning($"[AudioMixerController] Snapshot not found: {snapshotName}");
            }
        }

        /// <summary>
        /// Registers a snapshot for use
        /// </summary>
        public void RegisterSnapshot(string name, AudioMixerSnapshot snapshot)
        {
            if (snapshot != null)
            {
                _snapshots[name] = snapshot;
            }
        }

        /// <summary>
        /// Creates common audio scenarios as snapshots
        /// </summary>
        public void SetupCommonSnapshots()
        {
            // These would typically be set up in the Unity Editor
            // This method provides a way to register them programmatically
            
            var snapshots = _masterMixer.FindSnapshot("Normal");
            if (snapshots != null) RegisterSnapshot("Normal", snapshots);

            snapshots = _masterMixer.FindSnapshot("Combat");
            if (snapshots != null) RegisterSnapshot("Combat", snapshots);

            snapshots = _masterMixer.FindSnapshot("Menu");
            if (snapshots != null) RegisterSnapshot("Menu", snapshots);

            snapshots = _masterMixer.FindSnapshot("Underwater");
            if (snapshots != null) RegisterSnapshot("Underwater", snapshots);

            snapshots = _masterMixer.FindSnapshot("Muffled");
            if (snapshots != null) RegisterSnapshot("Muffled", snapshots);

            snapshots = _masterMixer.FindSnapshot("Paused");
            if (snapshots != null) RegisterSnapshot("Paused", snapshots);
        }

        /// <summary>
        /// Gets all registered snapshot names
        /// </summary>
        public List<string> GetAvailableSnapshots()
        {
            return new List<string>(_snapshots.Keys);
        }

        #endregion

        #region Public Methods - Advanced Features

        /// <summary>
        /// Applies dynamic range compression based on game state
        /// </summary>
        public void SetDynamicRangeCompression(float ratio, float threshold, float attack, float release)
        {
            // This would require custom parameters in your AudioMixer
            SetMixerParameter("CompressorRatio", ratio);
            SetMixerParameter("CompressorThreshold", threshold);
            SetMixerParameter("CompressorAttack", attack);
            SetMixerParameter("CompressorRelease", release);
        }

        /// <summary>
        /// Sets up ducking (automatically lower other audio when voice/music plays)
        /// </summary>
        public void SetupDucking(AudioCategory dominantCategory, float duckAmount, float duckTime)
        {
            // Implementation would depend on your specific mixer setup
            // This is a placeholder for ducking functionality
            string duckParameterName = $"Duck{dominantCategory}Amount";
            SetMixerParameter(duckParameterName, NormalizedToDecibel(1f - duckAmount));
        }

        /// <summary>
        /// Enables/disables audio spatialization
        /// </summary>
        public void SetSpatializationEnabled(bool enabled)
        {
            SetMixerParameter("SpatializationEnabled", enabled ? 1f : 0f);
        }

        /// <summary>
        /// Sets master pitch (useful for slow-motion effects)
        /// </summary>
        public void SetMasterPitch(float pitch)
        {
            pitch = Mathf.Clamp(pitch, 0.1f, 3f);
            SetMixerParameter("MasterPitch", pitch);
        }

        #endregion

        #region Public Methods - Diagnostics

        /// <summary>
        /// Gets current mixer status for debugging
        /// </summary>
        public AudioMixerStatus GetMixerStatus()
        {
            var status = new AudioMixerStatus
            {
                MasterVolume = GetMasterVolume(),
                CurrentSnapshot = _currentSnapshotName,
                ActiveParameters = new Dictionary<string, float>(_parameterCache),
                AvailableSnapshots = GetAvailableSnapshots()
            };

            // Add category volumes
            foreach (AudioCategory category in System.Enum.GetValues(typeof(AudioCategory)))
            {
                if (category != AudioCategory.Master)
                {
                    status.CategoryVolumes[category] = GetCategoryVolume(category);
                }
            }

            return status;
        }

        /// <summary>
        /// Logs all current mixer parameter values
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void LogMixerStatus()
        {
            Debug.Log($"[AudioMixerController] Current Mixer Status:\n" +
                     $"Master Volume: {GetMasterVolume():F2}\n" +
                     $"Current Snapshot: {_currentSnapshotName}\n" +
                     $"Active Parameters: {_parameterCache.Count}");

            foreach (var param in _parameterCache)
            {
                Debug.Log($"  {param.Key}: {param.Value:F2}");
            }
        }

        #endregion

        #region Private Methods

        private void InitializeMixer()
        {
            if (_masterMixer == null)
            {
                Debug.LogError("[AudioMixerController] No AudioMixer assigned!");
                return;
            }

            // Cache initial parameter values
            CacheAllParameters();

            // Setup common snapshots
            SetupCommonSnapshots();

            // Set initial snapshot if available
            if (_snapshots.TryGetValue("Normal", out var normalSnapshot))
            {
                _currentSnapshot = normalSnapshot;
                _currentSnapshotName = "Normal";
            }
        }

        private void CacheAllParameters()
        {
            // Cache volume parameters
            CacheParameter(MASTER_VOLUME_PARAM);
            CacheParameter(MUSIC_VOLUME_PARAM);
            CacheParameter(SFX_VOLUME_PARAM);
            CacheParameter(AMBIENT_VOLUME_PARAM);
            CacheParameter(VOICE_VOLUME_PARAM);
            CacheParameter(UI_VOLUME_PARAM);

            // Cache effect parameters
            CacheParameter(MASTER_LOWPASS_PARAM);
            CacheParameter(MASTER_HIGHPASS_PARAM);
            CacheParameter(REVERB_LEVEL_PARAM);
            CacheParameter(ECHO_LEVEL_PARAM);
        }

        private void CacheParameter(string parameterName)
        {
            if (_masterMixer.GetFloat(parameterName, out float value))
            {
                _parameterCache[parameterName] = value;
            }
        }

        private void SetMixerParameter(string parameterName, float value)
        {
            if (_masterMixer == null) return;

            bool success = _masterMixer.SetFloat(parameterName, value);
            if (success)
            {
                _parameterCache[parameterName] = value;
            }
            else
            {
                Debug.LogWarning($"[AudioMixerController] Failed to set parameter: {parameterName}");
            }
        }

        private float GetMixerParameter(string parameterName)
        {
            if (_parameterCache.TryGetValue(parameterName, out float cachedValue))
            {
                return cachedValue;
            }

            if (_masterMixer != null && _masterMixer.GetFloat(parameterName, out float value))
            {
                _parameterCache[parameterName] = value;
                return value;
            }

            return 0f;
        }

        private string GetVolumeParameterName(AudioCategory category)
        {
            return category switch
            {
                AudioCategory.Master => MASTER_VOLUME_PARAM,
                AudioCategory.Music => MUSIC_VOLUME_PARAM,
                AudioCategory.SFX => SFX_VOLUME_PARAM,
                AudioCategory.Ambient => AMBIENT_VOLUME_PARAM,
                AudioCategory.Voice => VOICE_VOLUME_PARAM,
                AudioCategory.UI => UI_VOLUME_PARAM,
                _ => string.Empty
            };
        }

        private float NormalizedToDecibel(float normalizedValue)
        {
            if (normalizedValue <= 0f)
                return -80f; // Minimum dB value

            return Mathf.Log10(normalizedValue) * 20f;
        }

        private float DecibelToNormalized(float decibelValue)
        {
            if (decibelValue <= -80f)
                return 0f;

            return Mathf.Pow(10f, decibelValue / 20f);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Audio mixer status for diagnostics
    /// </summary>
    public class AudioMixerStatus
    {
        public float MasterVolume;
        public string CurrentSnapshot;
        public Dictionary<string, float> ActiveParameters = new();
        public Dictionary<AudioCategory, float> CategoryVolumes = new();
        public List<string> AvailableSnapshots = new();
    }

    #endregion
}