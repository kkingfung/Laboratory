using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Laboratory.Gameplay.Audio;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Provides audio feedback for UI button interactions including hover, click, and pointer down events.
    /// Integrates with the Laboratory Audio system for consistent UI sound management.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [RequireComponent(typeof(AudioSource))]
    public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
    {
        #region Fields

        [Header("Sound Configuration")]
        [Tooltip("Sound played when pointer enters the button area.")]
        [SerializeField] private AudioManager.UISound hoverSound = AudioManager.UISound.ButtonHover;

        [Tooltip("Sound played when button is clicked.")]
        [SerializeField] private AudioManager.UISound clickSound = AudioManager.UISound.ButtonClick;

        [Tooltip("Sound played when pointer is pressed down on button.")]
        [SerializeField] private AudioManager.UISound downSound = AudioManager.UISound.ButtonDown;

        [Header("Audio Settings")]
        [Tooltip("Volume level for button sounds.")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Tooltip("Pitch adjustment for button sounds.")]
        [Range(0.5f, 2f)]
        [SerializeField] private float pitch = 1f;

        private AudioSource _audioSource;
        private Button _button;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize components and setup button click listener.
        /// </summary>
        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _button = GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.AddListener(PlayClickSound);
            }
        }

        /// <summary>
        /// Clean up event listeners to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClickSound);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Handles pointer enter events to play hover sound.
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            PlaySound(GetAudioClip(hoverSound));
        }

        /// <summary>
        /// Handles pointer down events to play down sound.
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            PlaySound(GetAudioClip(downSound));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Plays the click sound when button is clicked.
        /// </summary>
        private void PlayClickSound()
        {
            PlaySound(GetAudioClip(clickSound));
        }

        /// <summary>
        /// Plays the specified audio clip with configured volume and pitch settings.
        /// </summary>
        /// <param name="clip">Audio clip to play</param>
        private void PlaySound(AudioClip clip)
        {
            if (clip == null || _audioSource == null) 
                return;

            _audioSource.pitch = pitch;
            _audioSource.volume = volume;
            _audioSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Gets the AudioClip for the specified UI sound from the AudioManager.
        /// </summary>
        /// <param name="uiSound">UI sound type to retrieve</param>
        /// <returns>AudioClip associated with the UI sound, or null if not found</returns>
        private AudioClip GetAudioClip(AudioManager.UISound uiSound)
        {
            // This method would need to be implemented based on your AudioManager structure
            // For now, returning null as a placeholder
            // TODO: Implement proper AudioManager integration
            return null;
        }

        #endregion
    }
}
