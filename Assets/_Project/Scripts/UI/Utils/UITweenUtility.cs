using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
// FIXME: tidyup after 8/29
public static class UITweenUtility
{
    /// <summary>
    /// Tweens a CanvasGroup's alpha from startAlpha to endAlpha over duration seconds asynchronously,
    /// with optional easing and cancellation.
    /// </summary>
    public static async UniTask TweenAlphaAsync(
        CanvasGroup canvasGroup,
        float startAlpha,
        float endAlpha,
        float duration,
        Func<float, float>? easing = null,
        CancellationToken cancellationToken = default)
    {
        if (canvasGroup == null) throw new ArgumentNullException(nameof(canvasGroup));
        if (duration <= 0f)
        {
            canvasGroup.alpha = endAlpha;
            return;
        }

        easing ??= EaseLinear;

        canvasGroup.alpha = startAlpha;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = easing(t);

            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedT);
        }

        canvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// Tweens a RectTransform's anchoredPosition from start to end over duration seconds asynchronously,
    /// with optional easing and cancellation.
    /// </summary>
    public static async UniTask TweenPositionAsync(
        RectTransform rectTransform,
        Vector2 start,
        Vector2 end,
        float duration,
        Func<float, float>? easing = null,
        CancellationToken cancellationToken = default)
    {
        if (rectTransform == null) throw new ArgumentNullException(nameof(rectTransform));
        if (duration <= 0f)
        {
            rectTransform.anchoredPosition = end;
            return;
        }

        easing ??= EaseLinear;

        rectTransform.anchoredPosition = start;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = easing(t);

            rectTransform.anchoredPosition = Vector2.Lerp(start, end, easedT);
        }

        rectTransform.anchoredPosition = end;
    }

    /// <summary>
    /// Tweens a Transform's localScale from start to end over duration seconds asynchronously,
    /// with optional easing and cancellation.
    /// </summary>
    public static async UniTask TweenScaleAsync(
        Transform transform,
        Vector3 start,
        Vector3 end,
        float duration,
        Func<float, float>? easing = null,
        CancellationToken cancellationToken = default)
    {
        if (transform == null) throw new ArgumentNullException(nameof(transform));
        if (duration <= 0f)
        {
            transform.localScale = end;
            return;
        }

        easing ??= EaseLinear;

        transform.localScale = start;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = easing(t);

            transform.localScale = Vector3.Lerp(start, end, easedT);
        }

        transform.localScale = end;
    }

    /// <summary>
    /// Generic tween for any float property with setter,
    /// with optional easing and cancellation.
    /// </summary>
    public static async UniTask TweenFloatAsync(
        Action<float> setter,
        float start,
        float end,
        float duration,
        Func<float, float>? easing = null,
        CancellationToken cancellationToken = default)
    {
        if (setter == null) throw new ArgumentNullException(nameof(setter));
        if (duration <= 0f)
        {
            setter(end);
            return;
        }

        easing ??= EaseLinear;

        setter(start);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = easing(t);

            setter(Mathf.Lerp(start, end, easedT));
        }

        setter(end);
    }

    #region Easing Functions

    public static float EaseLinear(float t) => t;

    public static float EaseInQuad(float t) => t * t;

    public static float EaseOutQuad(float t) => t * (2f - t);

    public static float EaseInOutQuad(float t) =>
        t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

    public static float EaseInCubic(float t) => t * t * t;

    public static float EaseOutCubic(float t)
    {
        float p = t - 1f;
        return p * p * p + 1f;
    }

    public static float EaseInOutCubic(float t) =>
        t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;

    // Add more easing functions as needed

    #endregion
}
