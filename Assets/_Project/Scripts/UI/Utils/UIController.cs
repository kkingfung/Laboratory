using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class UIController : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDuration = 0.3f;

    protected CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;

    public bool IsOpen { get; private set; } = false;

    protected virtual void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Show the UI panel.
    /// </summary>
    public virtual void Show()
    {
        if (IsOpen) return;

        gameObject.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (useFade)
            fadeCoroutine = StartCoroutine(FadeIn());
        else
            SetVisible(true);
    }

    /// <summary>
    /// Hide the UI panel.
    /// </summary>
    public virtual void Hide()
    {
        if (!IsOpen) return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        if (useFade)
            fadeCoroutine = StartCoroutine(FadeOut());
        else
            SetVisible(false);
    }

    protected virtual IEnumerator FadeIn()
    {
        IsOpen = true;
        float elapsed = 0f;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    protected virtual IEnumerator FadeOut()
    {
        IsOpen = false;
        float elapsed = 0f;

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    protected virtual void SetVisible(bool visible)
    {
        IsOpen = visible;
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
        gameObject.SetActive(visible);
    }
}
