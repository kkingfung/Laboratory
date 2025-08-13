using System.Collections;
using UnityEngine;
// FIXME: tidyup after 8/29
[RequireComponent(typeof(RectTransform))]
public class UIShakeEffect : MonoBehaviour
{
    [Tooltip("Maximum offset magnitude in pixels.")]
    [SerializeField] private float magnitude = 10f;

    [Tooltip("Duration of the shake in seconds.")]
    [SerializeField] private float duration = 0.5f;

    [Tooltip("If true, shake decays over time.")]
    [SerializeField] private bool decay = true;

    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Starts the shake effect.
    /// </summary>
    /// <param name="customMagnitude">Optional custom magnitude override.</param>
    /// <param name="customDuration">Optional custom duration override.</param>
    public void Shake(float? customMagnitude = null, float? customDuration = null)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            rectTransform.anchoredPosition = originalPosition;
        }

        float shakeMagnitude = customMagnitude ?? magnitude;
        float shakeDuration = customDuration ?? duration;

        shakeCoroutine = StartCoroutine(DoShake(shakeMagnitude, shakeDuration));
    }

    private IEnumerator DoShake(float shakeMagnitude, float shakeDuration)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float damper = decay ? 1f - (elapsed / shakeDuration) : 1f;

            float x = (Random.value * 2f - 1f) * shakeMagnitude * damper;
            float y = (Random.value * 2f - 1f) * shakeMagnitude * damper;

            rectTransform.anchoredPosition = originalPosition + new Vector3(x, y, 0);

            yield return null;
        }

        rectTransform.anchoredPosition = originalPosition;
        shakeCoroutine = null;
    }
}
