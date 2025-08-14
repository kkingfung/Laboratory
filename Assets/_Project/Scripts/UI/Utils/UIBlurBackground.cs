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

        [Header("Canvas Group")]
        [Tooltip("CanvasGroup controlling the blur panel. Assign manually.")]
        [SerializeField] private CanvasGroup canvasGroup = null!;

        [Header("Blur Setup")]
        [Tooltip("Fullscreen UI Image that displays the blur effect.")]
        [SerializeField] private Image blurImage = null!;

        [Tooltip("Material with a blur shader.")]
        [SerializeField] private Material blurMaterial = null!;

        [Header("Fade Settings")]
        [Tooltip("Duration of fade-in/out in seconds.")]
        [SerializeField] private float fadeDuration = 0.3f;

        #endregion

        #region Private Fields

        private Coroutine _fadeCoroutine;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize the blur background with proper setup and initial state.
        /// </summary>
        private void Awake()
        {
            if (canvasGroup == null)
            {
                Debug.LogError($"{nameof(UIBlurBackground)} requires a CanvasGroup assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            if (blurImage == null)
            {
                Debug.LogError($"{nameof(UIBlurBackground)} requires a Blur Image assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            if (blurMaterial == null)
            {
                Debug.LogError($"{nameof(UIBlurBackground)} requires a Blur Material assigned in the Inspector.", this);
                enabled = false;
                return;
            }

            blurImage.material = blurMaterial;
            blurImage.gameObject.SetActive(false);

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Shows the blur background with fade-in animation.
        /// </summary>
        public void ShowBlur()
        {
            if (blurImage == null || canvasGroup == null) return;

            blurImage.gameObject.SetActive(true);

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f, fadeDuration));

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// Hides the blur background with fade-out animation.
        /// </summary>
        public void HideBlur()
        {
            if (blurImage == null || canvasGroup == null) return;

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeOutAndDisable(fadeDuration));

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
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
            if (canvasGroup == null) yield break;

            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = targetAlpha;
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
