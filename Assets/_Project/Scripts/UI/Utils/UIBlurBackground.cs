using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Creates a blur background effect for UI panels with smooth fade in/out animations.
    /// Manages a fullscreen blur image with material-based blur shader.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIBlurBackground : MonoBehaviour
    {
        #region Fields

        [Header("Blur Setup")]
        [Tooltip("Fullscreen UI Image that displays the blur effect.")]
        [SerializeField] private Image blurImage;

        [Tooltip("Material with a blur shader.")]
        [SerializeField] private Material blurMaterial;

        [Header("Fade Settings")]
        [Tooltip("Duration of fade-in/out in seconds.")]
        [SerializeField] private float fadeDuration = 0.3f;

        private CanvasGroup _canvasGroup;
        private Coroutine _fadeCoroutine;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize the blur background with proper setup and initial state.
        /// </summary>
        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();

            if (blurImage == null)
            {
                Debug.LogError("Blur Image is not assigned in UIBlurBackground.");
            }
            else
            {
                blurImage.material = blurMaterial;
                blurImage.gameObject.SetActive(false);
            }

            // Initialize as invisible and non-interactive
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the blur background with fade-in animation.
        /// </summary>
        public void ShowBlur()
        {
            if (blurImage == null)
                return;

            blurImage.gameObject.SetActive(true);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f, fadeDuration));

            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Hides the blur background with fade-out animation.
        /// </summary>
        public void HideBlur()
        {
            if (blurImage == null)
                return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeOutAndDisable(fadeDuration));

            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Fades the canvas group to the target alpha over the specified duration.
        /// </summary>
        /// <param name="targetAlpha">Target alpha value (0-1)</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
        {
            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        /// <summary>
        /// Fades out the blur and disables the image when complete.
        /// </summary>
        /// <param name="duration">Animation duration in seconds</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator FadeOutAndDisable(float duration)
        {
            yield return FadeCanvasGroup(0f, duration);
            if (blurImage != null)
                blurImage.gameObject.SetActive(false);
        }

        #endregion
    }
}
