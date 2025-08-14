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

    private RectTransform _rectTransform;
    private Vector3 _originalPosition;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _originalPosition = _rectTransform.anchoredPosition;
    }

    /// <summary>
    /// Starts the shake effect.
    /// </summary>
    /// <param name="customMagnitude">Optional custom magnitude override.</param>
    /// <param name="customDuration">Optional custom duration override.</param>
    public void Shake(float? customMagnitude = null, float? customDuration = null)
    {
        if (_shakeCoroutine != null)
        {
            StopCoroutine(_shakeCoroutine);
            _rectTransform.anchoredPosition = _originalPosition;
        }

        float shakeMagnitude = customMagnitude ?? magnitude;
        float shakeDuration = customDuration ?? duration;

        _shakeCoroutine = StartCoroutine(DoShake(shakeMagnitude, shakeDuration));
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

            _rectTransform.anchoredPosition = _originalPosition + new Vector3(x, y, 0);

            yield return null;
        }

        _rectTransform.anchoredPosition = _originalPosition;
        _shakeCoroutine = null;
    }
}
