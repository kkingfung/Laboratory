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
        [Tooltip("Maximum offset magnitude in pixels.")]
        [SerializeField] private float magnitude = 10f;

        [Tooltip("Duration of the shake in seconds.")]
        [SerializeField] private float duration = 0.5f;

        [Tooltip("If true, shake intensity decays over time.")]
        [SerializeField] private bool decay = true;

        private RectTransform _rectTransform;
        private Vector3 _originalPosition;
        private Coroutine _shakeCoroutine;

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether a shake animation is currently playing.
        /// </summary>
        public bool IsShaking => _shakeCoroutine != null;

        /// <summary>
        /// Gets or sets the default shake magnitude in pixels.
        /// </summary>
        public float Magnitude
        {
            get => magnitude;
            set => magnitude = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets the default shake duration in seconds.
        /// </summary>
        public float Duration
        {
            get => duration;
            set => duration = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Gets or sets whether shake intensity should decay over time.
        /// </summary>
        public bool Decay
        {
            get => decay;
            set => decay = value;
        }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize the shake effect by caching components and original position.
        /// </summary>
        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            StoreOriginalPosition();
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
        public void Shake()
        {
            Shake(null, null);
        }

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

            if (shakeDuration <= 0f || shakeMagnitude <= 0f)
            {
                return;
            }

            _shakeCoroutine = StartCoroutine(DoShakeCoroutine(shakeMagnitude, shakeDuration));
        }

        /// <summary>
        /// Immediately stops the current shake animation and resets position.
        /// </summary>
        public void StopShake()
        {
            StopCurrentShake();
            ResetPosition();
        }

        /// <summary>
        /// Updates the stored original position to the current position.
        /// Useful when the UI element has moved and you want to shake from the new position.
        /// </summary>
        public void UpdateOriginalPosition()
        {
            StoreOriginalPosition();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Stores the current anchored position as the original position.
        /// </summary>
        private void StoreOriginalPosition()
        {
            if (_rectTransform != null)
            {
                _originalPosition = _rectTransform.anchoredPosition;
            }
        }

        /// <summary>
        /// Stops the current shake coroutine if it's running.
        /// </summary>
        private void StopCurrentShake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }
        }

        /// <summary>
        /// Resets the RectTransform position to the original stored position.
        /// </summary>
        private void ResetPosition()
        {
            if (_rectTransform != null)
            {
                _rectTransform.anchoredPosition = _originalPosition;
            }
        }

        /// <summary>
        /// Coroutine that performs the shake animation over the specified duration.
        /// </summary>
        /// <param name="shakeMagnitude">Shake intensity in pixels</param>
        /// <param name="shakeDuration">Total shake duration in seconds</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator DoShakeCoroutine(float shakeMagnitude, float shakeDuration)
        {
            float elapsed = 0f;

            while (elapsed < shakeDuration)
            {
                elapsed += Time.unscaledDeltaTime;

                // Calculate shake damping factor
                float damper = CalculateShakeDamper(elapsed, shakeDuration);

                // Generate random offset
                Vector2 shakeOffset = GenerateShakeOffset(shakeMagnitude * damper);

                // Apply shake offset to position
                _rectTransform.anchoredPosition = _originalPosition + (Vector3)shakeOffset;

                yield return null;
            }

            // Reset to original position and clear coroutine reference
            ResetPosition();
            _shakeCoroutine = null;
        }

        /// <summary>
        /// Calculates the shake damping factor based on decay settings and elapsed time.
        /// </summary>
        /// <param name="elapsed">Time elapsed since shake started</param>
        /// <param name="totalDuration">Total shake duration</param>
        /// <returns>Damping factor between 0 and 1</returns>
        private float CalculateShakeDamper(float elapsed, float totalDuration)
        {
            return decay ? 1f - (elapsed / totalDuration) : 1f;
        }

        /// <summary>
        /// Generates a random shake offset within the specified magnitude.
        /// </summary>
        /// <param name="currentMagnitude">Current shake magnitude considering damping</param>
        /// <returns>Random offset vector</returns>
        private Vector2 GenerateShakeOffset(float currentMagnitude)
        {
            float x = (Random.value * 2f - 1f) * currentMagnitude;
            float y = (Random.value * 2f - 1f) * currentMagnitude;
            return new Vector2(x, y);
        }

        #endregion
    }
}
