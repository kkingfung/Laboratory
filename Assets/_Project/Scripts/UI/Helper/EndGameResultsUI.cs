using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.UI.Helper
{
    /// <summary>
    /// UI component for displaying end game results including score, kills, deaths, and time survived.
    /// Provides smooth fade in/out animations and callback support.
    /// </summary>
    public class EndGameResultsUI : MonoBehaviour
    {
        #region Fields

        [Header("UI References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject resultsPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI deathsText;
        [SerializeField] private TextMeshProUGUI timeSurvivedText;
        [SerializeField] private Button closeButton;

        [Header("Animation Settings")]
        [SerializeField] private float fadeDuration = 0.5f;

        private Action _onCloseCallback;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize UI components and setup event handlers.
        /// </summary>
        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            SetVisible(false);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Show the end game results UI with specified statistics.
        /// </summary>
        /// <param name="title">Title string, e.g. "Victory" or "Game Over"</param>
        /// <param name="score">Player score</param>
        /// <param name="kills">Number of kills</param>
        /// <param name="deaths">Number of deaths</param>
        /// <param name="timeSurvived">Formatted time survived string</param>
        /// <param name="onClose">Optional callback when results UI closes</param>
        public void Show(string title, int score, int kills, int deaths, string timeSurvived, Action onClose = null)
        {
            _onCloseCallback = onClose;

            UpdateDisplayTexts(title, score, kills, deaths, timeSurvived);
            
            SetVisible(true);
            StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Update all display texts with game results.
        /// </summary>
        /// <param name="title">Game result title</param>
        /// <param name="score">Final score</param>
        /// <param name="kills">Total kills</param>
        /// <param name="deaths">Total deaths</param>
        /// <param name="timeSurvived">Time survived string</param>
        private void UpdateDisplayTexts(string title, int score, int kills, int deaths, string timeSurvived)
        {
            titleText.text = title;
            scoreText.text = $"Score: {score}";
            killsText.text = $"Kills: {kills}";
            deathsText.text = $"Deaths: {deaths}";
            timeSurvivedText.text = $"Time Survived: {timeSurvived}";
        }

        /// <summary>
        /// Close the results UI with fade out animation.
        /// </summary>
        private void Close()
        {
            StartCoroutine(FadeOutAndClose());
        }

        /// <summary>
        /// Coroutine for fade out animation and cleanup.
        /// </summary>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator FadeOutAndClose()
        {
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration));
            SetVisible(false);
            _onCloseCallback?.Invoke();
        }

        /// <summary>
        /// Set UI visibility and interaction state.
        /// </summary>
        /// <param name="visible">Whether the UI should be visible</param>
        private void SetVisible(bool visible)
        {
            resultsPanel.SetActive(visible);
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// Fade canvas group between alpha values over specified duration.
        /// </summary>
        /// <param name="cg">Canvas group to fade</param>
        /// <param name="from">Starting alpha value</param>
        /// <param name="to">Target alpha value</param>
        /// <param name="duration">Fade duration in seconds</param>
        /// <returns>IEnumerator for coroutine</returns>
        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            cg.alpha = to;
        }

        #endregion
    }
}
