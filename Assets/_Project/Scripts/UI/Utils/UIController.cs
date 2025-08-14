using System.Collections;
using UnityEngine;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Base controller class for UI panels providing show/hide functionality with smooth fade animations.
    /// Manages CanvasGroup properties for proper UI interaction states.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIController : MonoBehaviour
    {
        #region Fields

        [Header("Fade Settings")]
        [Tooltip("Enable smooth fade in/out animations.")]
        [SerializeField] private bool useFade = true;

        [Tooltip("Duration of fade animations in seconds.")]
        [SerializeField] private float fadeDuration = 0.3f;

        protected CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the UI panel is currently open and visible.
        /// </summary>
        public bool IsOpen { get; private set; } = false;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize the UI controller with default invisible state.
        /// </summary>
        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            SetInitialState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the UI panel with optional fade animation.
        /// </summary>
        public virtual void Show()
        {
            if (IsOpen) 
                return;

            gameObject.SetActive(true);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            if (useFade)
                _fadeCoroutine = StartCoroutine(FadeIn());
            else
                SetVisible(true);
        }

        /// <summary>
        /// Hides the UI panel with optional fade animation.
        /// </summary>
        public virtual void Hide()
        {
            if (!IsOpen) 
                return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            if (useFade)
                _fadeCoroutine = StartCoroutine(FadeOut());
            else
                SetVisible(false);
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Coroutine that handles fade-in animation.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        protected virtual IEnumerator FadeIn()
        {
            IsOpen = true;
            float elapsed = 0f;

            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _fadeCoroutine = null;
        }

        /// <summary>
        /// Coroutine that handles fade-out animation.
        /// </summary>
        /// <returns>Coroutine enumerator</returns>
        protected virtual IEnumerator FadeOut()
        {
            IsOpen = false;
            float elapsed = 0f;

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            _fadeCoroutine = null;
        }

        /// <summary>
        /// Instantly sets the visibility state without animation.
        /// </summary>
        /// <param name="visible">Whether the UI should be visible</param>
        protected virtual void SetVisible(bool visible)
        {
            IsOpen = visible;
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
            gameObject.SetActive(visible);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the initial state of the UI controller to invisible and inactive.
        /// </summary>
        private void SetInitialState()
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        #endregion
    }
}
