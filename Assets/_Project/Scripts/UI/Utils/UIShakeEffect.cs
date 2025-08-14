using System.Collections;
using UnityEngine;

namespace Laboratory.UI.Utils
{
    /// <summary>
    /// Provides shake animation effects for UI RectTransform elements with customizable parameters.
    /// Supports decay over time and can be triggered with custom magnitude and duration values.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIShakeEffect : MonoBehaviour
    {
        #region Fields

        [Header("Shake Configuration")]
        [SerializeField] private RectTransform rectTransform = null!; // Assign in Inspector
        [Tooltip("Maximum offset magnitude in pixels.")]
        [SerializeField] private float magnitude = 10f;

        [Tooltip("Duration of the shake in seconds.")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("If true, shake intensity decays over time.")]
        [SerializeField] private bool decay = true;

        private Vector3 _originalPosition;
        private Coroutine _shakeCoroutine;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether a shake animation is currently playing.
        /// </summary>
        public bool IsShaking => _shakeCoroutine != null;
        public float Magnitude { get => magnitude; set => magnitude = Mathf.Max(0f, value); }
        public float Duration { get => duration; set => duration = Mathf.Max(0f, value); }
        public bool Decay { get => decay; set => decay = value; }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize the shake effect by caching components and original position.
        /// </summary>
        private void Awake()
        {
            if (rectTransform == null)
            {
                Debug.LogError($"{nameof(UIShakeEffect)} requires a RectTransform reference assigned in the Inspector.");
                enabled = false;
                return;
            }

            _originalPosition = rectTransform.anchoredPosition;
        }

        /// <summary>
        /// Ensure shake is stopped when the component is disabled.
        /// </summary>
        private void OnDisable()
        {
            StopShake();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts the shake effect with default parameters.
        /// </summary>
        public void Shake() => Shake(null, null);

        /// <summary>
        /// Starts the shake effect with optional custom parameters.
        /// </summary>
        /// <param name="customMagnitude">Optional custom magnitude override</param>
        /// <param name="customDuration">Optional custom duration override</param>
        public void Shake(float? customMagnitude = null, float? customDuration = null)
        {
            StopCurrentShake();

            float shakeMagnitude = customMagnitude ?? magnitude;
            float shakeDuration = customDuration ?? duration;

            if (shakeMagnitude <= 0f || shakeDuration <= 0f) return;

            _shakeCoroutine = StartCoroutine(DoShakeCoroutine(shakeMagnitude, shakeDuration));
        }

        /// <summary>
        /// Immediately stops the current shake animation and resets position.
        /// </summary>
        public void StopShake()
        {
            StopCurrentShake();
            rectTransform.anchoredPosition = _originalPosition;
        }

        public void UpdateOriginalPosition() => _originalPosition = rectTransform.anchoredPosition;

        #endregion

        #region Private Methods

        private void StopCurrentShake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }
        }

        private IEnumerator DoShakeCoroutine(float shakeMagnitude, float shakeDuration)
        {
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damper = decay ? 1f - (elapsed / shakeDuration) : 1f;

                Vector2 offset = new Vector2(
                    (Random.value * 2f - 1f) * shakeMagnitude * damper,
                    (Random.value * 2f - 1f) * shakeMagnitude * damper
                );

                rectTransform.anchoredPosition = _originalPosition + (Vector3)offset;
                yield return null;
            }

            rectTransform.anchoredPosition = _originalPosition;
            _shakeCoroutine = null;
        }

        #endregion
    }
}
