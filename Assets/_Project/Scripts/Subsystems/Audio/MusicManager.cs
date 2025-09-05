using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Audio
{
    /// <summary>
    /// Music Manager for background music with crossfading and transitions
    /// Handles smooth music transitions, playlists, and dynamic music systems
    /// </summary>
    public class MusicManager
    {
        #region Fields

        private readonly AudioSystemManager _audioSystemManager;
        private readonly AudioSource _primaryMusicSource;
        private AudioSource _secondaryMusicSource;

        private MusicTrack _currentTrack;
        private MusicTrack _queuedTrack;
        private readonly Queue<MusicTrack> _musicQueue = new();
        private readonly List<MusicTrack> _playlist = new();

        private Coroutine _fadeCoroutine;
        private Coroutine _playlistCoroutine;

        private bool _isPlaying = false;
        private bool _isPaused = false;
        private bool _isLooping = true;
        private bool _playlistMode = false;
        private bool _shuffleMode = false;
        private int _currentPlaylistIndex = 0;

        private MusicManagerSettings _settings = new MusicManagerSettings
        {
            DefaultFadeTime = 2f,
            DefaultVolume = 0.8f,
            CrossfadeThreshold = 0.5f,
            PlaylistShuffleOnRepeat = true
        };

        #endregion

        #region Properties

        public bool IsPlaying => _isPlaying;
        public bool IsPaused => _isPaused;
        public bool IsLooping { get => _isLooping; set => _isLooping = value; }
        public bool PlaylistMode { get => _playlistMode; set => SetPlaylistMode(value); }
        public bool ShuffleMode { get => _shuffleMode; set => _shuffleMode = value; }
        
        public MusicTrack CurrentTrack => _currentTrack;
        public float CurrentTime => _primaryMusicSource?.time ?? 0f;
        public float CurrentLength => _currentTrack?.Clip?.length ?? 0f;
        public float CurrentProgress => CurrentLength > 0 ? CurrentTime / CurrentLength : 0f;

        public int PlaylistCount => _playlist.Count;
        public int CurrentPlaylistIndex => _currentPlaylistIndex;

        #endregion

        #region Constructor

        public MusicManager(AudioSystemManager audioSystemManager, AudioSource primaryMusicSource)
        {
            _audioSystemManager = audioSystemManager;
            _primaryMusicSource = primaryMusicSource;

            CreateSecondaryMusicSource();
            ConfigureMusicSources();
        }

        #endregion

        #region Public Methods - Playback Control

        /// <summary>
        /// Plays a music track with optional fade in
        /// </summary>
        public void PlayMusic(AudioClip clip, bool loop = true, float fadeTime = -1f)
        {
            if (clip == null) return;

            var musicTrack = new MusicTrack
            {
                Clip = clip,
                Name = clip.name,
                Loop = loop,
                Volume = _settings.DefaultVolume,
                FadeTime = fadeTime >= 0 ? fadeTime : _settings.DefaultFadeTime
            };

            PlayMusic(musicTrack);
        }

        /// <summary>
        /// Plays a music track with full configuration
        /// </summary>
        public void PlayMusic(MusicTrack musicTrack)
        {
            if (musicTrack?.Clip == null) return;

            _queuedTrack = musicTrack;

            if (_currentTrack != null && _isPlaying)
            {
                // Crossfade to new track
                StartCrossfade(_currentTrack, musicTrack);
            }
            else
            {
                // Start new track immediately
                StartMusicTrack(musicTrack, _primaryMusicSource);
            }
        }

        /// <summary>
        /// Stops currently playing music with optional fade out
        /// </summary>
        public void StopMusic(float fadeTime = -1f)
        {
            fadeTime = fadeTime >= 0 ? fadeTime : _settings.DefaultFadeTime;

            if (_fadeCoroutine != null)
            {
                _audioSystemManager.StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = _audioSystemManager.StartCoroutine(FadeOutAndStop(fadeTime));
        }

        /// <summary>
        /// Pauses the currently playing music
        /// </summary>
        public void PauseMusic()
        {
            if (_isPlaying && !_isPaused)
            {
                _primaryMusicSource?.Pause();
                _secondaryMusicSource?.Pause();
                _isPaused = true;
            }
        }

        /// <summary>
        /// Resumes paused music
        /// </summary>
        public void ResumeMusic()
        {
            if (_isPlaying && _isPaused)
            {
                _primaryMusicSource?.UnPause();
                _secondaryMusicSource?.UnPause();
                _isPaused = false;
            }
        }

        /// <summary>
        /// Sets the music volume
        /// </summary>
        public void SetVolume(float volume)
        {
            volume = Mathf.Clamp01(volume);
            _settings.DefaultVolume = volume;

            if (_primaryMusicSource != null)
                _primaryMusicSource.volume = volume;
        }

        /// <summary>
        /// Seeks to a specific time in the current track
        /// </summary>
        public void SeekTo(float time)
        {
            if (_primaryMusicSource != null && _currentTrack != null)
            {
                time = Mathf.Clamp(time, 0f, _currentTrack.Clip.length);
                _primaryMusicSource.time = time;
            }
        }

        #endregion

        #region Public Methods - Playlist Management

        /// <summary>
        /// Adds a track to the playlist
        /// </summary>
        public void AddToPlaylist(MusicTrack track)
        {
            if (track != null)
            {
                _playlist.Add(track);
            }
        }

        /// <summary>
        /// Adds a track to the playlist by AudioClip
        /// </summary>
        public void AddToPlaylist(AudioClip clip, string trackName = null)
        {
            if (clip != null)
            {
                var track = new MusicTrack
                {
                    Clip = clip,
                    Name = trackName ?? clip.name,
                    Volume = _settings.DefaultVolume,
                    Loop = false // Playlist tracks typically don't loop individually
                };
                AddToPlaylist(track);
            }
        }

        /// <summary>
        /// Removes a track from the playlist
        /// </summary>
        public void RemoveFromPlaylist(int index)
        {
            if (index >= 0 && index < _playlist.Count)
            {
                _playlist.RemoveAt(index);
                
                // Adjust current index if needed
                if (_currentPlaylistIndex >= index && _currentPlaylistIndex > 0)
                {
                    _currentPlaylistIndex--;
                }
            }
        }

        /// <summary>
        /// Clears the entire playlist
        /// </summary>
        public void ClearPlaylist()
        {
            _playlist.Clear();
            _currentPlaylistIndex = 0;
        }

        /// <summary>
        /// Plays the next track in the playlist
        /// </summary>
        public void PlayNext()
        {
            if (!_playlistMode || _playlist.Count == 0) return;

            if (_shuffleMode)
            {
                _currentPlaylistIndex = Random.Range(0, _playlist.Count);
            }
            else
            {
                _currentPlaylistIndex = (_currentPlaylistIndex + 1) % _playlist.Count;
            }

            PlayPlaylistTrack(_currentPlaylistIndex);
        }

        /// <summary>
        /// Plays the previous track in the playlist
        /// </summary>
        public void PlayPrevious()
        {
            if (!_playlistMode || _playlist.Count == 0) return;

            if (_shuffleMode)
            {
                _currentPlaylistIndex = Random.Range(0, _playlist.Count);
            }
            else
            {
                _currentPlaylistIndex = _currentPlaylistIndex > 0 ? _currentPlaylistIndex - 1 : _playlist.Count - 1;
            }

            PlayPlaylistTrack(_currentPlaylistIndex);
        }

        /// <summary>
        /// Plays a specific track from the playlist
        /// </summary>
        public void PlayPlaylistTrack(int index)
        {
            if (index >= 0 && index < _playlist.Count)
            {
                _currentPlaylistIndex = index;
                var track = _playlist[index];
                PlayMusic(track);
            }
        }

        /// <summary>
        /// Gets a copy of the current playlist
        /// </summary>
        public List<MusicTrack> GetPlaylist()
        {
            return new List<MusicTrack>(_playlist);
        }

        #endregion

        #region Public Methods - Queue Management

        /// <summary>
        /// Adds a track to the music queue
        /// </summary>
        public void QueueTrack(MusicTrack track)
        {
            if (track != null)
            {
                _musicQueue.Enqueue(track);
            }
        }

        /// <summary>
        /// Plays the next queued track
        /// </summary>
        public void PlayNextQueued()
        {
            if (_musicQueue.Count > 0)
            {
                var nextTrack = _musicQueue.Dequeue();
                PlayMusic(nextTrack);
            }
        }

        /// <summary>
        /// Clears the music queue
        /// </summary>
        public void ClearQueue()
        {
            _musicQueue.Clear();
        }

        /// <summary>
        /// Gets the number of queued tracks
        /// </summary>
        public int GetQueueCount()
        {
            return _musicQueue.Count;
        }

        #endregion

        #region Public Methods - Settings

        /// <summary>
        /// Updates music manager settings
        /// </summary>
        public void UpdateSettings(MusicManagerSettings newSettings)
        {
            _settings = newSettings;
        }

        /// <summary>
        /// Gets current music manager settings
        /// </summary>
        public MusicManagerSettings GetSettings()
        {
            return _settings;
        }

        #endregion

        #region Private Methods

        private void CreateSecondaryMusicSource()
        {
            if (_primaryMusicSource != null)
            {
                var secondaryObject = new GameObject("SecondaryMusicSource");
                secondaryObject.transform.SetParent(_primaryMusicSource.transform.parent);
                _secondaryMusicSource = secondaryObject.AddComponent<AudioSource>();
            }
        }

        private void ConfigureMusicSources()
        {
            ConfigureMusicSource(_primaryMusicSource);
            ConfigureMusicSource(_secondaryMusicSource);
        }

        private void ConfigureMusicSource(AudioSource source)
        {
            if (source == null) return;

            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D audio for music
            source.priority = 0; // Highest priority
            source.volume = _settings.DefaultVolume;
        }

        private void StartMusicTrack(MusicTrack track, AudioSource audioSource)
        {
            if (track?.Clip == null || audioSource == null) return;

            audioSource.clip = track.Clip;
            audioSource.loop = track.Loop;
            audioSource.volume = track.Volume;
            audioSource.pitch = track.Pitch;
            audioSource.Play();

            _currentTrack = track;
            _isPlaying = true;
            _isPaused = false;

            // Start monitoring for track end if not looping
            if (!track.Loop)
            {
                _audioSystemManager.StartCoroutine(MonitorTrackEnd(track, audioSource));
            }
        }

        private void StartCrossfade(MusicTrack fromTrack, MusicTrack toTrack)
        {
            if (_fadeCoroutine != null)
            {
                _audioSystemManager.StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = _audioSystemManager.StartCoroutine(CrossfadeCoroutine(fromTrack, toTrack));
        }

        private IEnumerator CrossfadeCoroutine(MusicTrack fromTrack, MusicTrack toTrack)
        {
            float fadeTime = Mathf.Max(fromTrack.FadeTime, toTrack.FadeTime);
            float elapsedTime = 0f;

            // Start the new track on secondary source
            StartMusicTrack(toTrack, _secondaryMusicSource);
            _secondaryMusicSource.volume = 0f; // Start silent

            float startVolumeFrom = _primaryMusicSource.volume;
            float targetVolumeTo = toTrack.Volume;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeTime;

                // Fade out current track
                _primaryMusicSource.volume = Mathf.Lerp(startVolumeFrom, 0f, t);

                // Fade in new track
                _secondaryMusicSource.volume = Mathf.Lerp(0f, targetVolumeTo, t);

                yield return null;
            }

            // Complete the fade
            _primaryMusicSource.volume = 0f;
            _secondaryMusicSource.volume = targetVolumeTo;

            // Stop the old track and swap sources
            _primaryMusicSource.Stop();
            SwapMusicSources();

            _currentTrack = toTrack;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutAndStop(float fadeTime)
        {
            float startVolume = _primaryMusicSource.volume;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime && _primaryMusicSource.isPlaying)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / fadeTime;
                
                _primaryMusicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                
                yield return null;
            }

            _primaryMusicSource.Stop();
            _primaryMusicSource.volume = _settings.DefaultVolume;

            _isPlaying = false;
            _isPaused = false;
            _currentTrack = null;
            _fadeCoroutine = null;
        }

        private IEnumerator MonitorTrackEnd(MusicTrack track, AudioSource audioSource)
        {
            while (audioSource.isPlaying)
            {
                yield return null;
            }

            // Track ended, handle next action
            HandleTrackEnd(track);
        }

        private void HandleTrackEnd(MusicTrack track)
        {
            // Check for queued tracks first
            if (_musicQueue.Count > 0)
            {
                PlayNextQueued();
                return;
            }

            // Handle playlist mode
            if (_playlistMode && _playlist.Count > 0)
            {
                PlayNext();
                return;
            }

            // No next track, stop playing
            _isPlaying = false;
            _currentTrack = null;
        }

        private void SwapMusicSources()
        {
            var temp = _primaryMusicSource;
            // Note: This is conceptual - in practice, you'd swap the references
            // The actual implementation would depend on your specific setup
        }

        private void SetPlaylistMode(bool enabled)
        {
            _playlistMode = enabled;

            if (enabled && _playlist.Count > 0)
            {
                if (_playlistCoroutine != null)
                {
                    _audioSystemManager.StopCoroutine(_playlistCoroutine);
                }
                _playlistCoroutine = _audioSystemManager.StartCoroutine(PlaylistCoroutine());
            }
            else if (_playlistCoroutine != null)
            {
                _audioSystemManager.StopCoroutine(_playlistCoroutine);
                _playlistCoroutine = null;
            }
        }

        private IEnumerator PlaylistCoroutine()
        {
            while (_playlistMode && _playlist.Count > 0)
            {
                if (!_isPlaying)
                {
                    PlayPlaylistTrack(_currentPlaylistIndex);
                }

                yield return new WaitForSeconds(1f); // Check every second
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Represents a music track with metadata
    /// </summary>
    [System.Serializable]
    public class MusicTrack
    {
        public AudioClip Clip;
        public string Name;
        public string Artist;
        public string Album;
        public float Volume = 1f;
        public float Pitch = 1f;
        public bool Loop = true;
        public float FadeTime = 2f;
        public MusicGenre Genre = MusicGenre.Unknown;
        public MusicMood Mood = MusicMood.Neutral;

        public float Duration => Clip?.length ?? 0f;
    }

    /// <summary>
    /// Music manager settings
    /// </summary>
    [System.Serializable]
    public class MusicManagerSettings
    {
        public float DefaultFadeTime = 2f;
        public float DefaultVolume = 0.8f;
        public float CrossfadeThreshold = 0.5f;
        public bool PlaylistShuffleOnRepeat = true;
        public bool AutoAdvancePlaylist = true;
        public bool FadeOnPause = false;
        public float PauseFadeTime = 0.5f;
    }

    /// <summary>
    /// Music genres for categorization
    /// </summary>
    public enum MusicGenre
    {
        Unknown = 0,
        Ambient = 1,
        Electronic = 2,
        Orchestral = 3,
        Rock = 4,
        Jazz = 5,
        Classical = 6,
        Cinematic = 7
    }

    /// <summary>
    /// Music moods for dynamic music selection
    /// </summary>
    public enum MusicMood
    {
        Neutral = 0,
        Calm = 1,
        Tense = 2,
        Exciting = 3,
        Melancholy = 4,
        Triumphant = 5,
        Mysterious = 6,
        Peaceful = 7
    }

    #endregion
}