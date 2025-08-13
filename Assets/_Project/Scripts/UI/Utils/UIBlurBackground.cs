using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
[RequireComponent(typeof(CanvasGroup))]
public class UIBlurBackground : MonoBehaviour
{
    [Header("Blur Setup")]
    [Tooltip("Fullscreen UI Image that displays the blur effect.")]
    [SerializeField] private Image blurImage;

    [Tooltip("Material with a blur shader.")]
    [SerializeField] private Material blurMaterial;

    [Header("Fade Settings")]
    [Tooltip("Duration of fade-in/out in seconds.")]
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (blurImage == null)
        {
            Debug.LogError("Blur Image is not assigned in UIBlurBackground.");
        }
        else
        {
            blurImage.material = blurMaterial;
            blurImage.gameObject.SetActive(false);
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// Show the blur background with fade-in.
    /// </summary>
    public void ShowBlur()
    {
        if (blurImage == null)
            return;

        blurImage.gameObject.SetActive(true);

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f, fadeDuration));

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// Hide the blur background with fade-out.
    /// </summary>
    public void HideBlur()
    {
        if (blurImage == null)
            return;

        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(FadeOutAndDisable(fadeDuration));

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private IEnumerator FadeOutAndDisable(float duration)
    {
        yield return FadeCanvasGroup(0f, duration);
        if (blurImage != null)
            blurImage.gameObject.SetActive(false);
    }
}
