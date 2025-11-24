using UnityEngine;
using UnityEngine.Events;
using Laboratory.Audio;

namespace Laboratory.Subsystems.Audio
{
    /// <summary>
    /// Authoring component for triggering audio events in gameplay.
    /// Drop this on any GameObject to add audio functionality.
    ///
    /// USAGE EXAMPLES:
    /// - UI Buttons: Play click sounds
    /// - Creatures: Play birth/death sounds
    /// - Environmental: Play ambient loops in zones
    /// - Breeding: Play breeding success sounds
    /// </summary>
    public class AudioTriggerAuthoring : MonoBehaviour
    {
        [Header("Audio Configuration")]
        [Tooltip("Reference to the audio clip database")]
        [SerializeField] private AudioClipDatabase audioDatabase;

        [Header("Trigger Settings")]
        [Tooltip("What triggers the audio?")]
        [SerializeField] private AudioTriggerType triggerType = AudioTriggerType.OnEnable;

        [Tooltip("Delay before playing (seconds)")]
        [SerializeField] private float playDelay = 0f;

        [Header("Music Settings")]
        [Tooltip("Music track ID to play")]
        [SerializeField] private string musicTrackID = "";

        [Tooltip("Stop currently playing music?")]
        [SerializeField] private bool stopCurrentMusic = false;

        [Header("SFX Settings")]
        [Tooltip("Sound effect ID to play")]
        [SerializeField] private string sfxID = "";

        [Tooltip("Play at position (3D audio)")]
        [SerializeField] private bool playAt3DPosition = false;

        [Tooltip("3D audio position (leave null for this GameObject)")]
        [SerializeField] private Transform audioPosition;

        [Header("Ambient Settings")]
        [Tooltip("Ambient track ID to play")]
        [SerializeField] private string ambientID = "";

        [Header("Events")]
        [Tooltip("Additional actions when audio plays")]
        [SerializeField] private UnityEvent onAudioPlayed;

        private AudioSystemManager _audioSystem;
        private bool _hasPlayed = false;

        #region Unity Lifecycle

        private void Awake()
        {
            _audioSystem = FindFirstObjectByType<AudioSystemManager>();
            if (_audioSystem == null)
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] AudioSystemManager not found! Audio will not play on '{gameObject.name}'");
            }

            if (audioDatabase == null)
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] AudioClipDatabase not assigned on '{gameObject.name}'");
            }
        }

        private void OnEnable()
        {
            if (triggerType == AudioTriggerType.OnEnable)
            {
                PlayAudio();
            }
        }

        private void Start()
        {
            if (triggerType == AudioTriggerType.OnStart)
            {
                PlayAudio();
            }
        }

        private void OnDisable()
        {
            if (triggerType == AudioTriggerType.OnDisable)
            {
                PlayAudio();
            }
        }

        private void OnDestroy()
        {
            if (triggerType == AudioTriggerType.OnDestroy)
            {
                PlayAudio();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerType == AudioTriggerType.OnTriggerEnter)
            {
                PlayAudio();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (triggerType == AudioTriggerType.OnTriggerExit)
            {
                PlayAudio();
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (triggerType == AudioTriggerType.OnCollision)
            {
                PlayAudio();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually trigger audio playback.
        /// Call this from code, UI buttons, or animation events.
        /// </summary>
        public void PlayAudio()
        {
            if (_audioSystem == null || audioDatabase == null) return;

            // Apply delay if configured
            if (playDelay > 0f)
            {
                Invoke(nameof(PlayAudioInternal), playDelay);
            }
            else
            {
                PlayAudioInternal();
            }
        }

        /// <summary>
        /// Play a specific music track by ID
        /// </summary>
        public void PlayMusic(string trackID)
        {
            musicTrackID = trackID;
            PlayMusicInternal();
        }

        /// <summary>
        /// Play a specific SFX by ID
        /// </summary>
        public void PlaySFX(string soundID)
        {
            sfxID = soundID;
            PlaySFXInternal();
        }

        /// <summary>
        /// Stop currently playing audio
        /// </summary>
        public void StopAudio()
        {
            // Implementation depends on AudioSystemManager API
            Debug.Log($"[AudioTriggerAuthoring] Stop audio requested on '{gameObject.name}'");
        }

        #endregion

        #region Private Methods

        private void PlayAudioInternal()
        {
            // Check if already played (for one-shot triggers)
            if (triggerType == AudioTriggerType.OnceOnly && _hasPlayed)
                return;

            // Play music if specified
            if (!string.IsNullOrEmpty(musicTrackID))
            {
                PlayMusicInternal();
            }

            // Play SFX if specified
            if (!string.IsNullOrEmpty(sfxID))
            {
                PlaySFXInternal();
            }

            // Play ambient if specified
            if (!string.IsNullOrEmpty(ambientID))
            {
                PlayAmbientInternal();
            }

            // Invoke events
            onAudioPlayed?.Invoke();

            _hasPlayed = true;
        }

        private void PlayMusicInternal()
        {
            var musicTrack = audioDatabase.GetMusicTrack(musicTrackID);
            if (musicTrack == null)
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] Music track '{musicTrackID}' not found in database");
                return;
            }

            if (stopCurrentMusic)
            {
                // Stop current music first
                // _audioSystem.StopMusic();
            }

            // Play music
            // _audioSystem.PlayMusic(musicTrack);

            Debug.Log($"[AudioTriggerAuthoring] Playing music '{musicTrackID}' on '{gameObject.name}'");
        }

        private void PlaySFXInternal()
        {
            var sfxData = audioDatabase.GetSFX(sfxID);
            if (sfxData == null)
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] SFX '{sfxID}' not found in database");
                return;
            }

            if (sfxData.Clips == null || sfxData.Clips.Count == 0)
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] SFX '{sfxID}' has no clips assigned");
                return;
            }

            // Get random clip for variation
            var clip = sfxData.Clips[Random.Range(0, sfxData.Clips.Count)];

            // Play 2D or 3D
            if (playAt3DPosition)
            {
                Vector3 position = audioPosition != null ? audioPosition.position : transform.position;
                // _audioSystem.PlaySFX3D(clip, position, sfxData.Volume);
                Debug.Log($"[AudioTriggerAuthoring] Playing 3D SFX '{sfxID}' at {position}");
            }
            else
            {
                // _audioSystem.PlaySFX(clip, sfxData.Volume);
                Debug.Log($"[AudioTriggerAuthoring] Playing 2D SFX '{sfxID}' on '{gameObject.name}'");
            }
        }

        private void PlayAmbientInternal()
        {
            var ambientTrack = audioDatabase.GetAmbientTrack(ambientID);
            if (ambientTrack == null)
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] Ambient track '{ambientID}' not found in database");
                return;
            }

            // _audioSystem.PlayAmbient(ambientTrack);
            Debug.Log($"[AudioTriggerAuthoring] Playing ambient '{ambientID}' on '{gameObject.name}'");
        }

        #endregion

        #region Editor Utilities

        [ContextMenu("Test Play Audio")]
        private void TestPlayAudio()
        {
            if (Application.isPlaying)
            {
                PlayAudio();
            }
            else
            {
                Debug.LogWarning("[AudioTriggerAuthoring] Enter Play Mode to test audio");
            }
        }

        [ContextMenu("Validate Configuration")]
        private void ValidateConfiguration()
        {
            int issues = 0;

            if (audioDatabase == null)
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] AudioClipDatabase not assigned on '{gameObject.name}'");
                issues++;
            }

            if (string.IsNullOrEmpty(musicTrackID) && string.IsNullOrEmpty(sfxID) && string.IsNullOrEmpty(ambientID))
            {
                Debug.LogWarning($"[AudioTriggerAuthoring] No audio IDs configured on '{gameObject.name}'");
                issues++;
            }

            if (playAt3DPosition && audioPosition == null && !Application.isPlaying)
            {
                Debug.Log($"[AudioTriggerAuthoring] 3D audio will use GameObject position on '{gameObject.name}'");
            }

            if (issues == 0)
                Debug.Log($"[AudioTriggerAuthoring] Configuration valid on '{gameObject.name}'");
        }

        #endregion
    }

    /// <summary>
    /// Defines when audio should trigger
    /// </summary>
    public enum AudioTriggerType
    {
        [Tooltip("Never trigger automatically (manual only)")]
        Manual,

        [Tooltip("Play when this GameObject is enabled")]
        OnEnable,

        [Tooltip("Play on Start()")]
        OnStart,

        [Tooltip("Play when this GameObject is disabled")]
        OnDisable,

        [Tooltip("Play when this GameObject is destroyed")]
        OnDestroy,

        [Tooltip("Play when something enters the trigger collider")]
        OnTriggerEnter,

        [Tooltip("Play when something exits the trigger collider")]
        OnTriggerExit,

        [Tooltip("Play on collision")]
        OnCollision,

        [Tooltip("Play once only (never repeat)")]
        OnceOnly
    }
}
