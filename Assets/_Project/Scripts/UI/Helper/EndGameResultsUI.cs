using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameResultsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI deathsText;
    [SerializeField] private TextMeshProUGUI timeSurvivedText;
    [SerializeField] private Button closeButton;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;

    private Action? onCloseCallback;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        SetVisible(false);
    }

    /// <summary>
    /// Show the end game results UI.
    /// </summary>
    /// <param name="title">Title string, e.g. "Victory" or "Game Over".</param>
    /// <param name="score">Player score.</param>
    /// <param name="kills">Number of kills.</param>
    /// <param name="deaths">Number of deaths.</param>
    /// <param name="timeSurvived">Formatted time survived string.</param>
    /// <param name="onClose">Optional callback when results UI closes.</param>
    public void Show(string title, int score, int kills, int deaths, string timeSurvived, Action? onClose = null)
    {
        onCloseCallback = onClose;

        titleText.text = title;
        scoreText.text = $"Score: {score}";
        killsText.text = $"Kills: {kills}";
        deathsText.text = $"Deaths: {deaths}";
        timeSurvivedText.text = $"Time Survived: {timeSurvived}";

        SetVisible(true);
        StartCoroutine(FadeCanvasGroup(canvasGroup, 0f, 1f, fadeDuration));
    }

    private void Close()
    {
        StartCoroutine(FadeOutAndClose());
    }

    private IEnumerator FadeOutAndClose()
    {
        yield return FadeCanvasGroup(canvasGroup, 1f, 0f, fadeDuration);
        SetVisible(false);
        onCloseCallback?.Invoke();
    }

    private void SetVisible(bool visible)
    {
        resultsPanel.SetActive(visible);
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

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
}
