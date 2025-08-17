using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Gameplay.Audio
{
    /// <summary>
    /// Manages playback of game audio clips and music.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Fields

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Audio Clips")]
        [SerializeField] private List<AudioClip> _musicClips;
        [SerializeField] private List<AudioClip> _sfxClips;

        private Dictionary<string, AudioClip> _musicClipDict;
        private Dictionary<string, AudioClip> _sfxClipDict;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            InitializeClipDictionaries();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Plays a music track by name.
        /// </summary>
        public void PlayMusic(string clipName, bool loop = true)
        {
            if (_musicClipDict.TryGetValue(clipName, out var clip))
            {
                _musicSource.clip = clip;
                _musicSource.loop = loop;
                _musicSource.Play();
            }
            else
            {
                Debug.LogWarning($"AudioManager: Music clip '{clipName}' not found.");
            }
        }

        /// <summary>
        /// Plays a sound effect by name.
        /// </summary>
        public void PlaySFX(string clipName)
        {
            if (_sfxClipDict.TryGetValue(clipName, out var clip))
            {
                _sfxSource.PlayOneShot(clip);
            }
            else
            {
                Debug.LogWarning($"AudioManager: SFX clip '{clipName}' not found.");
            }
        }

        /// <summary>
        /// Stops the currently playing music.
        /// </summary>
        public void StopMusic()
        {
            _musicSource.Stop();
        }

        #endregion

        #region Private Methods

        private void InitializeClipDictionaries()
        {
            _musicClipDict = new Dictionary<string, AudioClip>();
            foreach (var clip in _musicClips)
            {
                if (clip != null && !_musicClipDict.ContainsKey(clip.name))
                    _musicClipDict.Add(clip.name, clip);
            }

            _sfxClipDict = new Dictionary<string, AudioClip>();
            foreach (var clip in _sfxClips)
            {
                if (clip != null && !_sfxClipDict.ContainsKey(clip.name))
                    _sfxClipDict.Add(clip.name, clip);
            }
        }

        #endregion
    }
}
