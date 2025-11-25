using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// Provides visual animations for UI buttons including hover, press, and click effects
    /// Uses Unity coroutines for smooth, performant animations
    /// Includes scale, color, rotation, and punch animations
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Animation Settings")]
        [SerializeField] private bool enableHoverScale = true;
        [SerializeField] private float hoverScale = 1.1f;
        [SerializeField] private float hoverDuration = 0.2f;

        [SerializeField] private bool enablePressScale = true;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float pressDuration = 0.1f;

        [SerializeField] private bool enableClickPunch = true;
        [SerializeField] private float punchStrength = 0.1f;
        [SerializeField] private float punchDuration = 0.3f;

        [Header("Color Animation")]
        [SerializeField] private bool enableColorChange = false;
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private Color pressColor = Color.gray;
        [SerializeField] private float colorDuration = 0.2f;

        [Header("Rotation Animation")]
        [SerializeField] private bool enableRotation = false;
        [SerializeField] private float rotationAngle = 5f;
        [SerializeField] private float rotationDuration = 0.2f;

        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve hoverCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve pressCurve = AnimationCurve.Linear(0, 0, 1, 1);

        // Component references
        private Button _button;
        private RectTransform _rectTransform;
        private Image _image;
        private Vector3 _originalScale;
        private Color _originalColor;
        private Quaternion _originalRotation;

        // Animation state
        private bool _isHovering = false;
        private bool _isPressing = false;
        private Coroutine _currentAnimation;

        private void Awake()
        {
            // Get components
            _button = GetComponent<Button>();
            _rectTransform = GetComponent<RectTransform>();
            _image = GetComponent<Image>();

            // Store originals
            _originalScale = _rectTransform.localScale;
            _originalRotation = _rectTransform.localRotation;
            if (_image != null)
                _originalColor = _image.color;

            // Add click listener for punch animation
            if (_button != null && enableClickPunch)
            {
                _button.onClick.AddListener(PlayClickAnimation);
            }
        }

        private void OnDestroy()
        {
            // Clean up animations
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }

            // Remove listener
            if (_button != null)
            {
                _button.onClick.RemoveListener(PlayClickAnimation);
            }
        }

        #region IPointerHandler Implementations

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            _isHovering = true;
            AnimateHoverEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovering = false;
            AnimateHoverExit();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!_button.interactable) return;

            _isPressing = true;
            AnimatePressDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressing = false;
            AnimatePressUp();
        }

        #endregion

        #region Animation Methods

        private void AnimateHoverEnter()
        {
            StopCurrentAnimation();

            Vector3 targetScale = enableHoverScale ? _originalScale * hoverScale : _originalScale;
            Color targetColor = enableColorChange && _image != null ? hoverColor : _originalColor;
            Vector3 targetRotation = enableRotation ? new Vector3(0, 0, rotationAngle) : Vector3.zero;

            _currentAnimation = StartCoroutine(AnimateTransform(
                _rectTransform.localScale, targetScale,
                _image != null ? _image.color : _originalColor, targetColor,
                _rectTransform.localEulerAngles, targetRotation,
                hoverDuration, hoverCurve
            ));
        }

        private void AnimateHoverExit()
        {
            StopCurrentAnimation();

            _currentAnimation = StartCoroutine(AnimateTransform(
                _rectTransform.localScale, _originalScale,
                _image != null ? _image.color : _originalColor, _originalColor,
                _rectTransform.localEulerAngles, Vector3.zero,
                hoverDuration, hoverCurve
            ));
        }

        private void AnimatePressDown()
        {
            if (!enablePressScale) return;

            StopCurrentAnimation();

            float targetScale = _isHovering ? (hoverScale * pressScale) : pressScale;
            Color targetColor = enableColorChange && _image != null ? pressColor : (_image != null ? _image.color : _originalColor);

            _currentAnimation = StartCoroutine(AnimateTransform(
                _rectTransform.localScale, _originalScale * targetScale,
                _image != null ? _image.color : _originalColor, targetColor,
                _rectTransform.localEulerAngles, _rectTransform.localEulerAngles,
                pressDuration, pressCurve
            ));
        }

        private void AnimatePressUp()
        {
            StopCurrentAnimation();

            Vector3 targetScale = _isHovering && enableHoverScale ? _originalScale * hoverScale : _originalScale;
            Color targetColor = _isHovering && enableColorChange ? hoverColor : _originalColor;

            _currentAnimation = StartCoroutine(AnimateTransform(
                _rectTransform.localScale, targetScale,
                _image != null ? _image.color : _originalColor, targetColor,
                _rectTransform.localEulerAngles, _rectTransform.localEulerAngles,
                pressDuration, hoverCurve
            ));
        }

        private void PlayClickAnimation()
        {
            if (!enableClickPunch) return;

            StartCoroutine(PunchScaleAnimation());
        }

        private IEnumerator AnimateTransform(
            Vector3 startScale, Vector3 endScale,
            Color startColor, Color endColor,
            Vector3 startRotation, Vector3 endRotation,
            float duration, AnimationCurve curve)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = curve.Evaluate(elapsed / duration);

                _rectTransform.localScale = Vector3.Lerp(startScale, endScale, t);

                if (_image != null && enableColorChange)
                {
                    _image.color = Color.Lerp(startColor, endColor, t);
                }

                if (enableRotation)
                {
                    _rectTransform.localEulerAngles = Vector3.Lerp(startRotation, endRotation, t);
                }

                yield return null;
            }

            // Ensure final values
            _rectTransform.localScale = endScale;
            if (_image != null && enableColorChange)
                _image.color = endColor;
            if (enableRotation)
                _rectTransform.localEulerAngles = endRotation;
        }

        private IEnumerator PunchScaleAnimation()
        {
            float elapsed = 0f;
            Vector3 startScale = _rectTransform.localScale;

            while (elapsed < punchDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / punchDuration;

                // Sine wave for punch effect
                float punchAmount = Mathf.Sin(t * Mathf.PI * 4) * punchStrength * (1 - t);
                _rectTransform.localScale = startScale + Vector3.one * punchAmount;

                yield return null;
            }

            // Return to original scale
            _rectTransform.localScale = startScale;
        }

        private void StopCurrentAnimation()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
                _currentAnimation = null;
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Resets button to its original state
        /// </summary>
        public void ResetToOriginal()
        {
            StopCurrentAnimation();
            _rectTransform.localScale = _originalScale;
            _rectTransform.localRotation = _originalRotation;
            if (_image != null)
                _image.color = _originalColor;
        }

        /// <summary>
        /// Enables/disables all animations
        /// </summary>
        public void SetAnimationsEnabled(bool enabled)
        {
            enableHoverScale = enabled;
            enablePressScale = enabled;
            enableClickPunch = enabled;
            enableColorChange = enabled;
            enableRotation = enabled;
        }

        #endregion

        #region Editor Utilities

        [ContextMenu("Preview Hover Animation")]
        private void PreviewHoverAnimation()
        {
            if (!Application.isPlaying) return;
            _isHovering = true;
            AnimateHoverEnter();
        }

        [ContextMenu("Preview Click Animation")]
        private void PreviewClickAnimation()
        {
            if (!Application.isPlaying) return;
            PlayClickAnimation();
        }

        [ContextMenu("Reset to Original")]
        private void EditorResetToOriginal()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            _originalScale = Vector3.one;
            _originalRotation = Quaternion.identity;

            ResetToOriginal();
        }

        #endregion
    }
}
