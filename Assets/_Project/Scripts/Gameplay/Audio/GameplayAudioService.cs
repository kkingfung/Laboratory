using UnityEngine;
using UnityEngine.Audio;

namespace Laboratory.Gameplay.Audio
{
    /// <summary>
    /// Service for handling gameplay-specific audio operations.
    /// Manages audio events, sound effects, and music for gameplay scenarios.
    /// </summary>
    public class GameplayAudioService : MonoBehaviour
    {
        [Header("Audio Configuration")]
        [SerializeField] private AudioMixerGroup sfxMixerGroup;
        [SerializeField] private AudioMixerGroup musicMixerGroup;
        [SerializeField] private bool enableAudioLogging = false;
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private AudioSource musicAudioSource;
        
        /// <summary>
        /// Initializes the gameplay audio service.
        /// </summary>
        public void Initialize()
        {
            if (enableAudioLogging)
            {
                Debug.Log("GameplayAudioService initialized");
            }
            
            // Setup audio sources if not assigned
            if (sfxAudioSource == null)
            {
                sfxAudioSource = gameObject.AddComponent<AudioSource>();
                sfxAudioSource.outputAudioMixerGroup = sfxMixerGroup;
            }
            
            if (musicAudioSource == null)
            {
                musicAudioSource = gameObject.AddComponent<AudioSource>();
                musicAudioSource.outputAudioMixerGroup = musicMixerGroup;
                musicAudioSource.loop = true;
            }
        }
        
        /// <summary>
        /// Plays a sound effect.
        /// </summary>
        /// <param name="clip">The audio clip to play</param>
        /// <param name="volume">The volume to play at</param>
        public void PlaySFX(AudioClip clip, float volume = 1.0f)
        {
            if (clip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip, volume);
            }
        }
        
        /// <summary>
        /// Plays background music.
        /// </summary>
        /// <param name="clip">The music clip to play</param>
        /// <param name="volume">The volume to play at</param>
        public void PlayMusic(AudioClip clip, float volume = 1.0f)
        {
            if (clip != null && musicAudioSource != null)
            {
                musicAudioSource.clip = clip;
                musicAudioSource.volume = volume;
                musicAudioSource.Play();
            }
        }
        
        /// <summary>
        /// Stops the currently playing music.
        /// </summary>
        public void StopMusic()
        {
            if (musicAudioSource != null)
            {
                musicAudioSource.Stop();
            }
        }
        
        /// <summary>
        /// Cleanup method for the audio service.
        /// </summary>
        public void Cleanup()
        {
            StopMusic();
        }
    }
}
