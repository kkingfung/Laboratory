using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
[RequireComponent(typeof(Image))]
public class UIProgressBar : MonoBehaviour
{
    [Tooltip("Image component with Fill Method (e.g. radial, horizontal).")]
    [SerializeField] private Image fillImage;

    [Tooltip("Optional: Background image to enable/disable when empty/full.")]
    [SerializeField] private GameObject background;

    [Tooltip("Fill animation duration in seconds.")]
    [SerializeField] private float animationDuration = 0.3f;

    private Coroutine animationCoroutine;

    private void Reset()
    {
        fillImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (fillImage == null)
        {
            Debug.LogError("UIProgressBar: Fill Image is not assigned.");
        }
    }

    /// <summary>
    /// Instantly sets the fill amount (0 to 1).
    /// </summary>
    /// <param name="fill">Fill amount between 0 and 1.</param>
    public void SetValue(float fill)
    {
        fill = Mathf.Clamp01(fill);
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
        fillImage.fillAmount = fill;
        UpdateBackgroundVisibility(fill);
    }

    /// <summary>
    /// Animate the fill from current fill to target fill.
    /// </summary>
    /// <param name="targetFill">Fill amount between 0 and 1.</param>
    /// <param name="duration">Duration of animation in seconds.</param>
    /// <param name="onComplete">Optional callback when animation completes.</param>
    public void AnimateTo(float targetFill, float duration = -1f, Action? onComplete = null)
    {
        targetFill = Mathf.Clamp01(targetFill);
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateFill(targetFill, duration < 0 ? animationDuration : duration, onComplete));
    }

    private IEnumerator AnimateFill(float targetFill, float duration, Action? onComplete)
    {
        float startFill = fillImage.fillAmount;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fillImage.fillAmount = Mathf.Lerp(startFill, targetFill, elapsed / duration);
            UpdateBackgroundVisibility(fillImage.fillAmount);
            yield return null;
        }

        fillImage.fillAmount = targetFill;
        UpdateBackgroundVisibility(targetFill);
        animationCoroutine = null;
        onComplete?.Invoke();
    }

    private void UpdateBackgroundVisibility(float fill)
    {
        if (background != null)
        {
            background.SetActive(fill > 0f);
        }
    }
}
