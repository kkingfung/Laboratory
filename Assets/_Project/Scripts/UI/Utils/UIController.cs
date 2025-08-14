using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
[RequireComponent(typeof(CanvasGroup))]
public class UIController : MonoBehaviour
{
    [Header("Fade Settings")]
    [SerializeField] private bool useFade = true;
    [SerializeField] private float fadeDuration = 0.3f;

    protected CanvasGroup _canvasGroup;
    private Coroutine _fadeCoroutine;

    public bool IsOpen { get; private set; } = false;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Show the UI panel.
    /// </summary>
    public virtual void Show()
    {
        if (IsOpen) return;

        gameObject.SetActive(true);

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        if (useFade)
            _fadeCoroutine = StartCoroutine(FadeIn());
        else
            SetVisible(true);
    }

    /// <summary>
    /// Hide the UI panel.
    /// </summary>
    public virtual void Hide()
    {
        if (!IsOpen) return;

        if (_fadeCoroutine != null)
            StopCoroutine(_fadeCoroutine);

        if (useFade)
            _fadeCoroutine = StartCoroutine(FadeOut());
        else
            SetVisible(false);
    }

    protected virtual IEnumerator FadeIn()
    {
        IsOpen = true;
        float elapsed = 0f;

        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 1f;
    }

    protected virtual IEnumerator FadeOut()
    {
        IsOpen = false;
        float elapsed = 0f;

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        _canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    protected virtual void SetVisible(bool visible)
    {
        IsOpen = visible;
        _canvasGroup.alpha = visible ? 1f : 0f;
        _canvasGroup.interactable = visible;
        _canvasGroup.blocksRaycasts = visible;
        gameObject.SetActive(visible);
    }
}
