using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Animated progress bar component with smooth fill transitions and optional background management.
    /// Supports instant value setting and smooth animations with completion callbacks.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UIProgressBar : MonoBehaviour
    {
        #region Fields

        [Header("Progress Bar Configuration")]
        [Tooltip("Image component with Fill Method (e.g. radial, horizontal).")]
        [SerializeField] private Image fillImage = null!; // Assign in Inspector

        [Tooltip("Optional background object to enable/disable based on fill.")]
        [SerializeField] private GameObject background;

        [Header("Animation Settings")]
        [Tooltip("Default animation duration in seconds.")]
        [SerializeField, Min(0f)] private float animationDuration = 0.3f;

        #endregion

        #region Private Fields

        private Coroutine _animationCoroutine;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current fill amount of the progress bar (0 to 1).
        /// </summary>
        public float CurrentValue => fillImage != null ? fillImage.fillAmount : 0f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Validates component setup and logs errors for missing references.
        /// </summary>
        private void Awake()
        {
            if (fillImage == null)
            {
                Debug.LogError($"{nameof(UIProgressBar)} requires a Fill Image assigned in the Inspector.", this);
                enabled = false;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Instantly sets the fill amount without animation.
        /// </summary>
        /// <param name="fill">Fill amount between 0 and 1</param>
        public void SetValue(float fill)
        {
            if (fillImage == null) return;

            fill = Mathf.Clamp01(fill);
            StopCurrentAnimation();
            fillImage.fillAmount = fill;
            UpdateBackgroundVisibility(fill);
        }

        /// <summary>
        /// Animates the fill from current value to target value over specified duration.
        /// </summary>
        /// <param name="targetFill">Target fill amount between 0 and 1</param>
        /// <param name="duration">Animation duration in seconds. Uses default if negative</param>
        /// <param name="onComplete">Optional callback invoked when animation completes</param>
        public void AnimateTo(float targetFill, float duration = -1f, Action onComplete = null)
        {
            if (fillImage == null) return;

            targetFill = Mathf.Clamp01(targetFill);
            float actualDuration = duration < 0f ? animationDuration : duration;

            StopCurrentAnimation();
            _animationCoroutine = StartCoroutine(AnimateFillCoroutine(targetFill, actualDuration, onComplete));
        }

        /// <summary>Stops any ongoing fill animation.</summary>
        public void StopAnimation() => StopCurrentAnimation();

        #endregion

        #region Private Methods

        /// <summary>
        /// Stops the current animation coroutine if it's running.
        /// </summary>
        private void StopCurrentAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        /// <summary>
        /// Coroutine that handles smooth fill animation from current value to target.
        /// </summary>
        /// <param name="targetFill">Target fill amount</param>
        /// <param name="duration">Animation duration</param>
        /// <param name="onComplete">Completion callback</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator AnimateFillCoroutine(float targetFill, float duration, Action onComplete)
        {
            float startFill = fillImage.fillAmount;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentFill = Mathf.Lerp(startFill, targetFill, elapsed / duration);
                fillImage.fillAmount = currentFill;
                UpdateBackgroundVisibility(currentFill);
                yield return null;
            }

            // Ensure we end at exactly the target value
            fillImage.fillAmount = targetFill;
            UpdateBackgroundVisibility(targetFill);

            _animationCoroutine = null;
            onComplete?.Invoke();
        }

        /// <summary>
        /// Updates the background GameObject visibility based on fill amount.
        /// </summary>
        /// <param name="fill">Current fill amount</param>
        private void UpdateBackgroundVisibility(float fill)
        {
            if (background != null)
            {
                background.SetActive(fill > 0f);
            }
        }

        #endregion
    }
}
