using UnityEngine;
using DG.Tweening;
using System;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// Animates UI panel/screen transitions with fade, slide, scale, and custom effects
    /// Supports show/hide animations with callbacks
    /// Uses DOTween for smooth, performant transitions
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

        [Header("Easing")]
        [SerializeField] private Ease showEase = Ease.OutCubic;
        [SerializeField] private Ease hideEase = Ease.InCubic;

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
        private Sequence _currentSequence;
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
            if (playHideOnDisable && _currentSequence != null)
            {
                _currentSequence.Kill();
            }
        }

        private void OnDestroy()
        {
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
            }
        }

        #region Public Methods

        /// <summary>
        /// Plays the show animation
        /// </summary>
        public void Show(Action onComplete = null)
        {
            if (_isShowing) return;

            KillCurrentSequence();
            _isShowing = true;

            // Prepare initial state
            PrepareShowState();

            // Create animation sequence
            _currentSequence = DOTween.Sequence();
            _currentSequence.SetDelay(showDelay);

            BuildShowAnimation(_currentSequence);

            _currentSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Plays the hide animation
        /// </summary>
        public void Hide(Action onComplete = null)
        {
            if (!_isShowing) return;

            KillCurrentSequence();
            _isShowing = false;

            // Create animation sequence
            _currentSequence = DOTween.Sequence();
            _currentSequence.SetDelay(hideDelay);

            BuildHideAnimation(_currentSequence);

            _currentSequence.OnComplete(() =>
            {
                onComplete?.Invoke();
                gameObject.SetActive(false);
            });
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
            KillCurrentSequence();
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
            KillCurrentSequence();
            _canvasGroup.alpha = 0f;
            _isShowing = false;
            gameObject.SetActive(false);
        }

        #endregion

        #region Private Animation Methods

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

        private void BuildShowAnimation(Sequence sequence)
        {
            switch (showTransition)
            {
                case TransitionType.Fade:
                    sequence.Append(_canvasGroup.DOFade(1f, showDuration).SetEase(showEase));
                    break;

                case TransitionType.Slide:
                    sequence.Append(_rectTransform.DOAnchorPos(_originalPosition, showDuration).SetEase(showEase));
                    break;

                case TransitionType.Scale:
                    sequence.Append(_rectTransform.DOScale(_originalScale, showDuration).SetEase(showEase));
                    break;

                case TransitionType.FadeSlideIn:
                    sequence.Append(_canvasGroup.DOFade(1f, showDuration).SetEase(showEase));
                    sequence.Join(_rectTransform.DOAnchorPos(_originalPosition, showDuration).SetEase(showEase));
                    break;

                case TransitionType.FadeSlideOut:
                    sequence.Append(_canvasGroup.DOFade(1f, showDuration).SetEase(showEase));
                    sequence.Join(_rectTransform.DOAnchorPos(_originalPosition, showDuration).SetEase(showEase));
                    break;

                case TransitionType.FadeScale:
                    sequence.Append(_canvasGroup.DOFade(1f, showDuration).SetEase(showEase));
                    sequence.Join(_rectTransform.DOScale(_originalScale, showDuration).SetEase(showEase));
                    break;

                case TransitionType.SlideScale:
                    sequence.Append(_rectTransform.DOAnchorPos(_originalPosition, showDuration).SetEase(showEase));
                    sequence.Join(_rectTransform.DOScale(_originalScale, showDuration).SetEase(showEase));
                    break;

                case TransitionType.FullTransition:
                    sequence.Append(_canvasGroup.DOFade(1f, showDuration).SetEase(showEase));
                    sequence.Join(_rectTransform.DOAnchorPos(_originalPosition, showDuration).SetEase(showEase));
                    sequence.Join(_rectTransform.DOScale(_originalScale, showDuration).SetEase(showEase));
                    break;
            }
        }

        private void BuildHideAnimation(Sequence sequence)
        {
            switch (hideTransition)
            {
                case TransitionType.Fade:
                    sequence.Append(_canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase));
                    break;

                case TransitionType.Slide:
                case TransitionType.FadeSlideOut:
                    sequence.Append(_rectTransform.DOAnchorPos(GetSlideEndPosition(), hideDuration).SetEase(hideEase));
                    if (hideTransition == TransitionType.FadeSlideOut)
                        sequence.Join(_canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase));
                    break;

                case TransitionType.Scale:
                case TransitionType.FadeScale:
                    sequence.Append(_rectTransform.DOScale(_originalScale * startScale, hideDuration).SetEase(hideEase));
                    if (hideTransition == TransitionType.FadeScale)
                        sequence.Join(_canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase));
                    break;

                case TransitionType.FadeSlideIn:
                    sequence.Append(_canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase));
                    sequence.Join(_rectTransform.DOAnchorPos(GetSlideEndPosition(), hideDuration).SetEase(hideEase));
                    break;

                case TransitionType.SlideScale:
                case TransitionType.FullTransition:
                    sequence.Append(_rectTransform.DOAnchorPos(GetSlideEndPosition(), hideDuration).SetEase(hideEase));
                    sequence.Join(_rectTransform.DOScale(_originalScale * startScale, hideDuration).SetEase(hideEase));
                    if (hideTransition == TransitionType.FullTransition)
                        sequence.Join(_canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase));
                    break;
            }
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

        private void KillCurrentSequence()
        {
            if (_currentSequence != null && _currentSequence.IsActive())
            {
                _currentSequence.Kill();
            }
        }

        #endregion

        #region Editor Utilities

        [ContextMenu("Preview Show")]
        private void PreviewShow()
        {
            Show();
        }

        [ContextMenu("Preview Hide")]
        private void PreviewHide()
        {
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
