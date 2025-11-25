using UnityEngine;
using UnityEngine.Audio;

namespace Laboratory.Subsystems.Settings
{
    /// <summary>
    /// Comprehensive audio settings management
    /// Handles volume mixing, mute, and audio quality
    /// </summary>
    public class AudioSettings : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SettingsConfig config;

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer audioMixer;

        // Current volumes
        private float _masterVolume;
        private float _musicVolume;
        private float _sfxVolume;
        private float _voiceVolume;
        private float _uiVolume;

        // Mute states
        private bool _isMasterMuted = false;
        private bool _isMusicMuted = false;
        private bool _isSFXMuted = false;

        // Events
        public event System.Action OnAudioSettingsChanged;

        // PlayerPrefs keys
        private const string PREF_MASTER_VOL = "Audio_MasterVolume";
        private const string PREF_MUSIC_VOL = "Audio_MusicVolume";
        private const string PREF_SFX_VOL = "Audio_SFXVolume";
        private const string PREF_VOICE_VOL = "Audio_VoiceVolume";
        private const string PREF_UI_VOL = "Audio_UIVolume";

        // Audio mixer parameter names
        private const string MIXER_MASTER = "MasterVolume";
        private const string MIXER_MUSIC = "MusicVolume";
        private const string MIXER_SFX = "SFXVolume";
        private const string MIXER_VOICE = "VoiceVolume";
        private const string MIXER_UI = "UIVolume";

        private void Start()
        {
            LoadSettings();
            ApplySettings();
        }

        /// <summary>
        /// Load settings from PlayerPrefs or use defaults
        /// </summary>
        public void LoadSettings()
        {
            if (config == null)
            {
                Debug.LogWarning("[AudioSettings] No configuration assigned!");
                return;
            }

            _masterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOL, config.DefaultMasterVolume);
            _musicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, config.DefaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, config.DefaultSFXVolume);
            _voiceVolume = PlayerPrefs.GetFloat(PREF_VOICE_VOL, config.DefaultVoiceVolume);
            _uiVolume = PlayerPrefs.GetFloat(PREF_UI_VOL, config.DefaultUIVolume);

            Debug.Log($"[AudioSettings] Loaded settings: Master={_masterVolume:F2}, Music={_musicVolume:F2}, SFX={_sfxVolume:F2}");
        }

        /// <summary>
        /// Save settings to PlayerPrefs
        /// </summary>
        public void SaveSettings()
        {
            PlayerPrefs.SetFloat(PREF_MASTER_VOL, _masterVolume);
            PlayerPrefs.SetFloat(PREF_MUSIC_VOL, _musicVolume);
            PlayerPrefs.SetFloat(PREF_SFX_VOL, _sfxVolume);
            PlayerPrefs.SetFloat(PREF_VOICE_VOL, _voiceVolume);
            PlayerPrefs.SetFloat(PREF_UI_VOL, _uiVolume);
            PlayerPrefs.Save();

            Debug.Log("[AudioSettings] Settings saved");
        }

        /// <summary>
        /// Apply all audio settings to mixer
        /// </summary>
        public void ApplySettings()
        {
            if (audioMixer == null)
            {
                Debug.LogWarning("[AudioSettings] No audio mixer assigned! Using AudioListener volume instead.");
                AudioListener.volume = _masterVolume;
                return;
            }

            SetMixerVolume(MIXER_MASTER, _masterVolume);
            SetMixerVolume(MIXER_MUSIC, _musicVolume);
            SetMixerVolume(MIXER_SFX, _sfxVolume);
            SetMixerVolume(MIXER_VOICE, _voiceVolume);
            SetMixerVolume(MIXER_UI, _uiVolume);

            OnAudioSettingsChanged?.Invoke();

            Debug.Log("[AudioSettings] Applied all audio settings");
        }

        /// <summary>
        /// Set mixer volume (converts linear to decibel)
        /// </summary>
        private void SetMixerVolume(string parameterName, float linearVolume)
        {
            if (audioMixer == null) return;

            // Convert linear 0-1 to decibels (-80 to 0)
            float dB = linearVolume > 0 ? Mathf.Log10(linearVolume) * 20f : -80f;
            audioMixer.SetFloat(parameterName, dB);
        }

        /// <summary>
        /// Set master volume
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MIXER_MASTER, _masterVolume);
            SaveSettings();
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set music volume
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MIXER_MUSIC, _musicVolume);
            SaveSettings();
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set SFX volume
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            _sfxVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MIXER_SFX, _sfxVolume);
            SaveSettings();
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set voice volume
        /// </summary>
        public void SetVoiceVolume(float volume)
        {
            _voiceVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MIXER_VOICE, _voiceVolume);
            SaveSettings();
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Set UI volume
        /// </summary>
        public void SetUIVolume(float volume)
        {
            _uiVolume = Mathf.Clamp01(volume);
            SetMixerVolume(MIXER_UI, _uiVolume);
            SaveSettings();
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Toggle master mute
        /// </summary>
        public void ToggleMasterMute()
        {
            _isMasterMuted = !_isMasterMuted;
            SetMixerVolume(MIXER_MASTER, _isMasterMuted ? 0f : _masterVolume);
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Toggle music mute
        /// </summary>
        public void ToggleMusicMute()
        {
            _isMusicMuted = !_isMusicMuted;
            SetMixerVolume(MIXER_MUSIC, _isMusicMuted ? 0f : _musicVolume);
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Toggle SFX mute
        /// </summary>
        public void ToggleSFXMute()
        {
            _isSFXMuted = !_isSFXMuted;
            SetMixerVolume(MIXER_SFX, _isSFXMuted ? 0f : _sfxVolume);
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Mute all audio
        /// </summary>
        public void MuteAll(bool mute)
        {
            _isMasterMuted = mute;
            SetMixerVolume(MIXER_MASTER, mute ? 0f : _masterVolume);
            OnAudioSettingsChanged?.Invoke();
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            if (config == null) return;

            _masterVolume = config.DefaultMasterVolume;
            _musicVolume = config.DefaultMusicVolume;
            _sfxVolume = config.DefaultSFXVolume;
            _voiceVolume = config.DefaultVoiceVolume;
            _uiVolume = config.DefaultUIVolume;

            ApplySettings();
            SaveSettings();

            Debug.Log("[AudioSettings] Reset to defaults");
        }

        // Getters
        public float GetMasterVolume() => _masterVolume;
        public float GetMusicVolume() => _musicVolume;
        public float GetSFXVolume() => _sfxVolume;
        public float GetVoiceVolume() => _voiceVolume;
        public float GetUIVolume() => _uiVolume;
        public bool IsMasterMuted() => _isMasterMuted;
        public bool IsMusicMuted() => _isMusicMuted;
        public bool IsSFXMuted() => _isSFXMuted;
    }
}
