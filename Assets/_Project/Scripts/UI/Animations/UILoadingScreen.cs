using UnityEngine;
using UnityEngine.UI;
using TMPro;
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
        [SerializeField] private AnimationCurve progressCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // State
        private float _targetProgress = 0f;
        private float _currentProgress = 0f;
        private float _progressVelocity = 0f;
        private bool _isVisible = false;
        private Coroutine _messageRotationCoroutine;
        private Coroutine _fadeCoroutine;
        private Coroutine _progressCoroutine;

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

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
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
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, fadeInDuration, onComplete));

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
            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, fadeOutDuration, () =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            }));
        }

        /// <summary>
        /// Sets the loading progress (0-1)
        /// </summary>
        public void SetProgress(float progress)
        {
            _targetProgress = Mathf.Clamp01(progress);

            if (animateProgressBar && progressBar != null)
            {
                if (_progressCoroutine != null)
                    StopCoroutine(_progressCoroutine);
                _progressCoroutine = StartCoroutine(AnimateSlider(progressBar, _targetProgress, progressSmoothTime));
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
                    yield return StartCoroutine(FadeText(loadingText, 0f, 0.3f));

                    // Change message
                    loadingText.text = loadingMessages[messageIndex];
                    messageIndex = (messageIndex + 1) % loadingMessages.Length;

                    // Fade in
                    yield return StartCoroutine(FadeText(loadingText, 1f, 0.3f));
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
                    StartCoroutine(ScaleElement(rt, Vector3.one, 0.5f, i * 0.1f, true));
                }
                StartCoroutine(FadeCanvasGroup(cg, 1f, 0.5f, null, i * 0.1f));
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
                    StartCoroutine(FadeCanvasGroup(cg, 0f, 0.3f, null, i * 0.05f));
                }

                if (rt != null)
                {
                    StartCoroutine(ScaleElement(rt, Vector3.zero, 0.3f, i * 0.05f, false));
                }
            }
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup group, float targetAlpha, float duration, Action onComplete = null, float delay = 0f)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            float startAlpha = group.alpha;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }

            group.alpha = targetAlpha;
            onComplete?.Invoke();
        }

        private IEnumerator FadeText(TextMeshProUGUI text, float targetAlpha, float duration)
        {
            Color startColor = text.color;
            Color targetColor = new Color(startColor.r, startColor.g, startColor.b, targetAlpha);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                text.color = Color.Lerp(startColor, targetColor, t);
                yield return null;
            }

            text.color = targetColor;
        }

        private IEnumerator AnimateSlider(Slider slider, float targetValue, float duration)
        {
            float startValue = slider.value;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = progressCurve.Evaluate(elapsed / duration);
                slider.value = Mathf.Lerp(startValue, targetValue, t);
                yield return null;
            }

            slider.value = targetValue;
        }

        private IEnumerator ScaleElement(RectTransform rt, Vector3 targetScale, float duration, float delay, bool useBackEase)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            Vector3 startScale = rt.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Back ease approximation
                if (useBackEase)
                {
                    float overshoot = 1.70158f;
                    t = t * t * ((overshoot + 1) * t - overshoot);
                }
                else
                {
                    // InBack ease
                    float overshoot = 1.70158f;
                    t = 1 - (1 - t) * (1 - t) * ((overshoot + 1) * (1 - t) - overshoot);
                }

                rt.localScale = Vector3.Lerp(startScale, targetScale, Mathf.Clamp01(t));
                yield return null;
            }

            rt.localScale = targetScale;
        }

        #endregion

        #region Editor Utilities

        [ContextMenu("Test Show")]
        private void TestShow()
        {
            if (!Application.isPlaying) return;
            Show();
            StartCoroutine(SimulateLoading());
        }

        [ContextMenu("Test Hide")]
        private void TestHide()
        {
            if (!Application.isPlaying) return;
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
    }
}
