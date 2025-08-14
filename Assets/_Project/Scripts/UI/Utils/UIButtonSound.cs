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
        [SerializeField] private AudioClip hoverSound;

        [Tooltip("Sound played when button is clicked.")]
        [SerializeField] private AudioClip clickSound;

        [Tooltip("Sound played when pointer is pressed down on button.")]
        [SerializeField] private AudioClip downSound;

        [Header("Audio Settings")]
        [Tooltip("Volume level for button sounds.")]
        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Tooltip("Pitch adjustment for button sounds.")]
        [Range(0.5f, 2f)]
        [SerializeField] private float pitch = 1f;

        [Tooltip("AudioSource used to play the sounds.")]
        [SerializeField] private AudioSource audioSource = null!;

        [Tooltip("Button component to hook click events.")]
        [SerializeField] private Button button = null!;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize components and setup button click listener.
        /// </summary>
        private void Awake()
        {
            if (audioSource == null)
            {
                Debug.LogError($"{nameof(UIButtonSound)} requires an AudioSource assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            if (button == null)
            {
                Debug.LogError($"{nameof(UIButtonSound)} requires a Button assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            button.onClick.AddListener(PlayClickSound);
        }

        /// <summary>
        /// Clean up event listeners to prevent memory leaks.
        /// </summary>
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(PlayClickSound);
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
            PlaySound(hoverSound);
        }

        /// <summary>
        /// Handles pointer down events to play down sound.
        /// </summary>
        /// <param name="eventData">Pointer event data</param>
        public void OnPointerDown(PointerEventData eventData)
        {
            PlaySound(downSound);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Plays the click sound when button is clicked.
        /// </summary>
        private void PlayClickSound()
        {
            PlaySound(clickSound);
        }

        /// <summary>
        /// Plays the specified audio clip with configured volume and pitch settings.
        /// </summary>
        /// <param name="clip">Audio clip to play</param>
        private void PlaySound(AudioClip clip)
        {
            if (clip == null || audioSource == null)
                return;

            audioSource.pitch = pitch;
            audioSource.volume = volume;
            audioSource.PlayOneShot(clip);
        }

        #endregion
    }
}
