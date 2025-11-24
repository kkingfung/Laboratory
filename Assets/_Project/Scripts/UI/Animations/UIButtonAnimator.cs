using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// Provides visual animations for UI buttons including hover, press, and click effects
    /// Uses DOTween for smooth, performant animations
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
        [SerializeField] private int punchVibrato = 10;
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

        [Header("Ease Settings")]
        [SerializeField] private Ease hoverEase = Ease.OutQuad;
        [SerializeField] private Ease pressEase = Ease.InQuad;

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
        private Tweener _currentTween;

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
            // Clean up tweens
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
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
            KillCurrentTween();

            Sequence sequence = DOTween.Sequence();

            // Scale animation
            if (enableHoverScale)
            {
                sequence.Join(_rectTransform.DOScale(_originalScale * hoverScale, hoverDuration).SetEase(hoverEase));
            }

            // Color animation
            if (enableColorChange && _image != null)
            {
                sequence.Join(_image.DOColor(hoverColor, colorDuration));
            }

            // Rotation animation
            if (enableRotation)
            {
                sequence.Join(_rectTransform.DOLocalRotate(new Vector3(0, 0, rotationAngle), rotationDuration).SetEase(hoverEase));
            }

            _currentTween = sequence;
        }

        private void AnimateHoverExit()
        {
            KillCurrentTween();

            Sequence sequence = DOTween.Sequence();

            // Return to original scale
            if (enableHoverScale)
            {
                sequence.Join(_rectTransform.DOScale(_originalScale, hoverDuration).SetEase(hoverEase));
            }

            // Return to original color
            if (enableColorChange && _image != null)
            {
                sequence.Join(_image.DOColor(_originalColor, colorDuration));
            }

            // Return to original rotation
            if (enableRotation)
            {
                sequence.Join(_rectTransform.DOLocalRotate(Vector3.zero, rotationDuration).SetEase(hoverEase));
            }

            _currentTween = sequence;
        }

        private void AnimatePressDown()
        {
            if (!enablePressScale) return;

            KillCurrentTween();

            Sequence sequence = DOTween.Sequence();

            // Press scale
            float targetScale = _isHovering ? (hoverScale * pressScale) : pressScale;
            sequence.Append(_rectTransform.DOScale(_originalScale * targetScale, pressDuration).SetEase(pressEase));

            // Press color
            if (enableColorChange && _image != null)
            {
                sequence.Join(_image.DOColor(pressColor, colorDuration));
            }

            _currentTween = sequence;
        }

        private void AnimatePressUp()
        {
            Sequence sequence = DOTween.Sequence();

            // Return to hover or normal state
            if (_isHovering)
            {
                if (enableHoverScale)
                {
                    sequence.Append(_rectTransform.DOScale(_originalScale * hoverScale, pressDuration).SetEase(hoverEase));
                }
                if (enableColorChange && _image != null)
                {
                    sequence.Join(_image.DOColor(hoverColor, colorDuration));
                }
            }
            else
            {
                if (enableHoverScale || enablePressScale)
                {
                    sequence.Append(_rectTransform.DOScale(_originalScale, pressDuration).SetEase(hoverEase));
                }
                if (enableColorChange && _image != null)
                {
                    sequence.Join(_image.DOColor(_originalColor, colorDuration));
                }
            }

            _currentTween = sequence;
        }

        private void PlayClickAnimation()
        {
            if (!enableClickPunch) return;

            // Punch scale animation
            _rectTransform.DOPunchScale(Vector3.one * punchStrength, punchDuration, punchVibrato);
        }

        private void KillCurrentTween()
        {
            if (_currentTween != null && _currentTween.IsActive())
            {
                _currentTween.Kill();
            }
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Resets button to its original state
        /// </summary>
        public void ResetToOriginal()
        {
            KillCurrentTween();
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
            _isHovering = true;
            AnimateHoverEnter();
        }

        [ContextMenu("Preview Click Animation")]
        private void PreviewClickAnimation()
        {
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
