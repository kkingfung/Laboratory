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
        [SerializeField, Min(0f)] private float fadeDuration = 0.3f;

        [Tooltip("CanvasGroup component to control UI visibility and interactions.")]
        [SerializeField] private CanvasGroup canvasGroup = null!; // Assign in Inspector

        #endregion

        #region Private Fields

        private Coroutine _fadeCoroutine;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the UI panel is currently open and visible.
        /// </summary>
        public bool IsOpen { get; private set; } = false;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            if (canvasGroup == null)
            {
                Debug.LogError($"{nameof(UIController)} requires a CanvasGroup assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            SetInitialState();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the UI panel with optional fade animation.
        /// </summary>
        public virtual void Show()
        {
            if (IsOpen) return;

            gameObject.SetActive(true);
            StopCurrentFade();

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
            if (!IsOpen) return;

            StopCurrentFade();

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

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            _fadeCoroutine = null;
        }

        protected virtual IEnumerator FadeOut()
        {
            IsOpen = false;
            float elapsed = 0f;

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
            _fadeCoroutine = null;
        }

        protected virtual void SetVisible(bool visible)
        {
            IsOpen = visible;
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
            gameObject.SetActive(visible);
        }

        #endregion

        #region Private Methods

        private void SetInitialState()
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        private void StopCurrentFade()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
        }

        #endregion
    }
}
