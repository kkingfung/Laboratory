using UnityEngine;
using System;
using System.Collections;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// Animates UI panel/screen transitions with fade, slide, scale, and custom effects
    /// Supports show/hide animations with callbacks
    /// Uses Unity coroutines for smooth, performant transitions
    /// </summary>
    public class UITransitionAnimator : MonoBehaviour
    {
        [Header("Animation Type")]
        [SerializeField] private TransitionType showTransition = TransitionType.FadeSlideIn;
        [SerializeField] private TransitionType hideTransition = TransitionType.FadeSlideOut;

        [Header("Timing")]
        [SerializeField] private float showDuration = 0.4f;
        [SerializeField] private float hideDuration = 0.3f;
        [SerializeField] private float showDelay = 0f;
        [SerializeField] private float hideDelay = 0f;

        [Header("Animation Curves")]
        [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private AnimationCurve hideCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Slide Settings")]
        [SerializeField] private SlideDirection slideDirection = SlideDirection.Bottom;
        [SerializeField] private float slideDistance = 100f;

        [Header("Scale Settings")]
        [SerializeField] private float startScale = 0.8f;
        [SerializeField] private float endScale = 1f;

        [Header("Auto-Play")]
        [SerializeField] private bool playShowOnEnable = false;
        [SerializeField] private bool playHideOnDisable = false;

        // Component references
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;

        // Original values
        private Vector2 _originalPosition;
        private Vector3 _originalScale;

        // State
        private Coroutine _currentAnimation;
        private bool _isShowing = false;

        public enum TransitionType
        {
            Fade,
            Slide,
            Scale,
            FadeSlideIn,
            FadeSlideOut,
            FadeScale,
            SlideScale,
            FullTransition
        }

        public enum SlideDirection
        {
            Left,
            Right,
            Top,
            Bottom
        }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();

            // Auto-add CanvasGroup if missing
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Store original values
            _originalPosition = _rectTransform.anchoredPosition;
            _originalScale = _rectTransform.localScale;
        }

        private void OnEnable()
        {
            if (playShowOnEnable)
            {
                Show();
            }
        }

        private void OnDisable()
        {
            if (playHideOnDisable && _currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
        }

        private void OnDestroy()
        {
            if (_currentAnimation != null)
            {
                StopCoroutine(_currentAnimation);
            }
        }

        #region Public Methods

        /// <summary>
        /// Plays the show animation
        /// </summary>
        public void Show(Action onComplete = null)
        {
            if (_isShowing) return;

            StopCurrentAnimation();
            _isShowing = true;

            _currentAnimation = StartCoroutine(ShowCoroutine(onComplete));
        }

        /// <summary>
        /// Plays the hide animation
        /// </summary>
        public void Hide(Action onComplete = null)
        {
            if (!_isShowing) return;

            StopCurrentAnimation();
            _isShowing = false;

            _currentAnimation = StartCoroutine(HideCoroutine(onComplete));
        }

        /// <summary>
        /// Toggles visibility with animation
        /// </summary>
        public void Toggle(Action onComplete = null)
        {
            if (_isShowing)
                Hide(onComplete);
            else
                Show(onComplete);
        }

        /// <summary>
        /// Immediately shows without animation
        /// </summary>
        public void ShowImmediate()
        {
            StopCurrentAnimation();
            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;
            _isShowing = true;
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Immediately hides without animation
        /// </summary>
        public void HideImmediate()
        {
            StopCurrentAnimation();
            _canvasGroup.alpha = 0f;
            _isShowing = false;
            gameObject.SetActive(false);
        }

        #endregion

        #region Coroutine Animation Methods

        private IEnumerator ShowCoroutine(Action onComplete)
        {
            // Prepare initial state
            PrepareShowState();

            // Wait for delay
            if (showDelay > 0)
                yield return new WaitForSeconds(showDelay);

            // Animate based on transition type
            yield return StartCoroutine(AnimateShow());

            onComplete?.Invoke();
        }

        private IEnumerator HideCoroutine(Action onComplete)
        {
            // Wait for delay
            if (hideDelay > 0)
                yield return new WaitForSeconds(hideDelay);

            // Animate based on transition type
            yield return StartCoroutine(AnimateHide());

            gameObject.SetActive(false);
            onComplete?.Invoke();
        }

        private void PrepareShowState()
        {
            gameObject.SetActive(true);

            switch (showTransition)
            {
                case TransitionType.Fade:
                case TransitionType.FadeSlideIn:
                case TransitionType.FadeSlideOut:
                case TransitionType.FadeScale:
                case TransitionType.FullTransition:
                    _canvasGroup.alpha = 0f;
                    break;
            }

            switch (showTransition)
            {
                case TransitionType.Slide:
                case TransitionType.FadeSlideIn:
                case TransitionType.SlideScale:
                case TransitionType.FullTransition:
                    _rectTransform.anchoredPosition = GetSlideStartPosition();
                    break;
            }

            switch (showTransition)
            {
                case TransitionType.Scale:
                case TransitionType.FadeScale:
                case TransitionType.SlideScale:
                case TransitionType.FullTransition:
                    _rectTransform.localScale = _originalScale * startScale;
                    break;
            }
        }

        private IEnumerator AnimateShow()
        {
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;
            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector3 startScaleValue = _rectTransform.localScale;

            while (elapsed < showDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = showCurve.Evaluate(elapsed / showDuration);

                switch (showTransition)
                {
                    case TransitionType.Fade:
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                        break;

                    case TransitionType.Slide:
                        _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalPosition, t);
                        break;

                    case TransitionType.Scale:
                        _rectTransform.localScale = Vector3.Lerp(startScaleValue, _originalScale, t);
                        break;

                    case TransitionType.FadeSlideIn:
                    case TransitionType.FadeSlideOut:
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                        _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalPosition, t);
                        break;

                    case TransitionType.FadeScale:
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                        _rectTransform.localScale = Vector3.Lerp(startScaleValue, _originalScale, t);
                        break;

                    case TransitionType.SlideScale:
                        _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalPosition, t);
                        _rectTransform.localScale = Vector3.Lerp(startScaleValue, _originalScale, t);
                        break;

                    case TransitionType.FullTransition:
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                        _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalPosition, t);
                        _rectTransform.localScale = Vector3.Lerp(startScaleValue, _originalScale, t);
                        break;
                }

                yield return null;
            }

            // Ensure final values
            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;
        }

        private IEnumerator AnimateHide()
        {
            float elapsed = 0f;
            float startAlpha = _canvasGroup.alpha;
            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector3 startScaleValue = _rectTransform.localScale;
            Vector2 endPos = GetSlideEndPosition();
            Vector3 endScaleValue = _originalScale * startScale;

            while (elapsed < hideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = hideCurve.Evaluate(elapsed / hideDuration);

                switch (hideTransition)
                {
                    case TransitionType.Fade:
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                        break;

                    case TransitionType.Slide:
                    case TransitionType.FadeSlideOut:
                        _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                        if (hideTransition == TransitionType.FadeSlideOut)
                            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                        break;

                    case TransitionType.Scale:
                    case TransitionType.FadeScale:
                        _rectTransform.localScale = Vector3.Lerp(startScaleValue, endScaleValue, t);
                        if (hideTransition == TransitionType.FadeScale)
                            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                        break;

                    case TransitionType.FadeSlideIn:
                        _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                        _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                        break;

                    case TransitionType.SlideScale:
                    case TransitionType.FullTransition:
                        _rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                        _rectTransform.localScale = Vector3.Lerp(startScaleValue, endScaleValue, t);
                        if (hideTransition == TransitionType.FullTransition)
                            _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                        break;
                }

                yield return null;
            }

            // Ensure final values
            if (hideTransition != TransitionType.Slide && hideTransition != TransitionType.Scale && hideTransition != TransitionType.SlideScale)
                _canvasGroup.alpha = 0f;
        }

        private Vector2 GetSlideStartPosition()
        {
            return slideDirection switch
            {
                SlideDirection.Left => _originalPosition + Vector2.left * slideDistance,
                SlideDirection.Right => _originalPosition + Vector2.right * slideDistance,
                SlideDirection.Top => _originalPosition + Vector2.up * slideDistance,
                SlideDirection.Bottom => _originalPosition + Vector2.down * slideDistance,
                _ => _originalPosition
            };
        }

        private Vector2 GetSlideEndPosition()
        {
            return GetSlideStartPosition();
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

        #region Editor Utilities

        [ContextMenu("Preview Show")]
        private void PreviewShow()
        {
            if (!Application.isPlaying) return;
            Show();
        }

        [ContextMenu("Preview Hide")]
        private void PreviewHide()
        {
            if (!Application.isPlaying) return;
            Hide();
        }

        [ContextMenu("Reset to Original")]
        private void ResetToOriginal()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _originalPosition;
            _rectTransform.localScale = _originalScale;
        }

        #endregion
    }
}
