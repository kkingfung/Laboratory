using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;

namespace Laboratory.UI.Animations
{
    /// <summary>
    /// Enhanced loading screen with smooth progress bar, animated text, and visual feedback
    /// Supports asynchronous operations, custom loading messages, and completion callbacks
    /// </summary>
    public class UILoadingScreen : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI loadingText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image spinnerImage;
        [SerializeField] private GameObject[] animatedElements;

        [Header("Loading Messages")]
        [SerializeField] private string[] loadingMessages = new string[]
        {
            "Loading...",
            "Preparing assets...",
            "Initializing systems...",
            "Almost ready..."
        };
        [SerializeField] private float messageChangeInterval = 2f;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float progressSmoothTime = 0.3f;
        [SerializeField] private float spinnerRotationSpeed = 180f;

        [Header("Progress Bar Animation")]
        [SerializeField] private bool animateProgressBar = true;
        [SerializeField] private Ease progressEase = Ease.OutCubic;

        // State
        private float _targetProgress = 0f;
        private float _currentProgress = 0f;
        private float _progressVelocity = 0f;
        private bool _isVisible = false;
        private Coroutine _messageRotationCoroutine;

        // Singleton instance (optional)
        private static UILoadingScreen _instance;
        public static UILoadingScreen Instance => _instance;

        private void Awake()
        {
            // Optional singleton pattern
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            // Initialize
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            HideImmediate();
        }

        private void Update()
        {
            // Smooth progress bar animation
            if (_isVisible && Mathf.Abs(_targetProgress - _currentProgress) > 0.001f)
            {
                _currentProgress = Mathf.SmoothDamp(_currentProgress, _targetProgress, ref _progressVelocity, progressSmoothTime);
                UpdateProgressDisplay(_currentProgress);
            }

            // Spinner rotation
            if (_isVisible && spinnerImage != null)
            {
                spinnerImage.transform.Rotate(Vector3.forward, -spinnerRotationSpeed * Time.deltaTime);
            }
        }

        #region Public Methods

        /// <summary>
        /// Shows the loading screen with fade-in animation
        /// </summary>
        public void Show(Action onComplete = null)
        {
            if (_isVisible) return;

            gameObject.SetActive(true);
            _isVisible = true;
            _targetProgress = 0f;
            _currentProgress = 0f;

            // Fade in
            canvasGroup.DOFade(1f, fadeInDuration).OnComplete(() =>
            {
                onComplete?.Invoke();
            });

            // Start message rotation
            if (loadingMessages.Length > 0 && _messageRotationCoroutine == null)
            {
                _messageRotationCoroutine = StartCoroutine(RotateLoadingMessages());
            }

            // Animate elements in
            AnimateElementsIn();
        }

        /// <summary>
        /// Hides the loading screen with fade-out animation
        /// </summary>
        public void Hide(Action onComplete = null)
        {
            if (!_isVisible) return;

            _isVisible = false;

            // Stop message rotation
            if (_messageRotationCoroutine != null)
            {
                StopCoroutine(_messageRotationCoroutine);
                _messageRotationCoroutine = null;
            }

            // Animate elements out
            AnimateElementsOut();

            // Fade out
            canvasGroup.DOFade(0f, fadeOutDuration).OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        /// <summary>
        /// Sets the loading progress (0-1)
        /// </summary>
        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);

            if (animateProgressBar && progressBar != null)
            {
                progressBar.DOValue(_targetProgress, progressSmoothTime).SetEase(progressEase);
            }
        }

        /// <summary>
        /// Sets the loading message
        /// </summary>
        public void SetMessage(string message)
        {
            if (loadingText != null)
            {
                loadingText.text = message;
            }
        }

        /// <summary>
        /// Shows loading screen and waits for operation to complete
        /// </summary>
        public IEnumerator LoadAsync(Func<IEnumerator> operation, string message = "Loading...")
        {
            Show();
            SetMessage(message);
            SetProgress(0f);

            yield return operation();

            SetProgress(1f);
            yield return new WaitForSeconds(0.5f); // Brief pause to show completion

            Hide();
        }

        /// <summary>
        /// Immediately shows without animation
        /// </summary>
        public void ShowImmediate()
        {
            gameObject.SetActive(true);
            canvasGroup.alpha = 1f;
            _isVisible = true;
            _targetProgress = 0f;
            _currentProgress = 0f;
        }

        /// <summary>
        /// Immediately hides without animation
        /// </summary>
        public void HideImmediate()
        {
            gameObject.SetActive(false);
            canvasGroup.alpha = 0f;
            _isVisible = false;

            if (_messageRotationCoroutine != null)
            {
                StopCoroutine(_messageRotationCoroutine);
                _messageRotationCoroutine = null;
            }
        }

        #endregion

        #region Private Methods

        private void UpdateProgressDisplay(float progress)
        {
            if (progressBar != null && !animateProgressBar)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
        }

        private IEnumerator RotateLoadingMessages()
        {
            int messageIndex = 0;

            while (_isVisible)
            {
                if (loadingText != null && loadingMessages.Length > 0)
                {
                    // Fade out
                    loadingText.DOFade(0f, 0.3f);
                    yield return new WaitForSeconds(0.3f);

                    // Change message
                    loadingText.text = loadingMessages[messageIndex];
                    messageIndex = (messageIndex + 1) % loadingMessages.Length;

                    // Fade in
                    loadingText.DOFade(1f, 0.3f);
                }

                yield return new WaitForSeconds(messageChangeInterval);
            }
        }

        private void AnimateElementsIn()
        {
            if (animatedElements == null || animatedElements.Length == 0) return;

            for (int i = 0; i < animatedElements.Length; i++)
            {
                if (animatedElements[i] == null) continue;

                RectTransform rt = animatedElements[i].GetComponent<RectTransform>();
                CanvasGroup cg = animatedElements[i].GetComponent<CanvasGroup>();

                if (cg == null)
                {
                    cg = animatedElements[i].AddComponent<CanvasGroup>();
                }

                // Fade and scale in
                cg.alpha = 0f;
                if (rt != null)
                {
                    rt.localScale = Vector3.zero;
                    rt.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).SetDelay(i * 0.1f);
                }
                cg.DOFade(1f, 0.5f).SetDelay(i * 0.1f);
            }
        }

        private void AnimateElementsOut()
        {
            if (animatedElements == null || animatedElements.Length == 0) return;

            for (int i = 0; i < animatedElements.Length; i++)
            {
                if (animatedElements[i] == null) continue;

                RectTransform rt = animatedElements[i].GetComponent<RectTransform>();
                CanvasGroup cg = animatedElements[i].GetComponent<CanvasGroup>();

                if (cg != null)
                {
                    cg.DOFade(0f, 0.3f).SetDelay(i * 0.05f);
                }

                if (rt != null)
                {
                    rt.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).SetDelay(i * 0.05f);
                }
            }
        }

        #endregion

        #region Editor Utilities

        [ContextMenu("Test Show")]
        private void TestShow()
        {
            Show();
            StartCoroutine(SimulateLoading());
        }

        [ContextMenu("Test Hide")]
        private void TestHide()
        {
            Hide();
        }

        private IEnumerator SimulateLoading()
        {
            float progress = 0f;
            while (progress < 1f)
            {
                progress += Time.deltaTime * 0.3f;
                SetProgress(progress);
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
            Hide();
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up singleton reference
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
