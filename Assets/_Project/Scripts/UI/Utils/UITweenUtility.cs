using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Laboratory.Infrastructure.AsyncUtils
{
    /// <summary>
    /// Utility class providing async tween functions for common UI animations using UniTask.
    /// Includes support for custom easing functions and cancellation tokens for responsive UI animations.
    /// </summary>
    public static class UITweenUtility
    {
        #region Public Methods - Alpha Tweening

        /// <summary>
        /// Tweens a CanvasGroup's alpha from startAlpha to endAlpha over duration seconds asynchronously.
        /// </summary>
        /// <param name="canvasGroup">CanvasGroup to animate</param>
        /// <param name="startAlpha">Starting alpha value (0-1)</param>
        /// <param name="endAlpha">Target alpha value (0-1)</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="easing">Optional easing function. Uses linear if null</param>
        /// <param name="cancellationToken">Cancellation token for early termination</param>
        /// <returns>UniTask that completes when animation finishes</returns>
        /// <exception cref="ArgumentNullException">Thrown when canvasGroup is null</exception>
        public static async UniTask TweenAlphaAsync(
            CanvasGroup canvasGroup,
            float startAlpha,
            float endAlpha,
            float duration,
            Func<float, float> easing = null,
            CancellationToken cancellationToken = default)
        {
            if (canvasGroup == null) 
                throw new ArgumentNullException(nameof(canvasGroup));

            if (duration <= 0f)
            {
                canvasGroup.alpha = endAlpha;
                return;
            }

            easing ??= EaseLinear;
            canvasGroup.alpha = startAlpha;

            await TweenValueAsync(
                value => canvasGroup.alpha = value,
                startAlpha,
                endAlpha,
                duration,
                easing,
                cancellationToken
            );
        }

        #endregion

        #region Public Methods - Position Tweening

        /// <summary>
        /// Tweens a RectTransform's anchoredPosition from start to end over duration seconds asynchronously.
        /// </summary>
        /// <param name="rectTransform">RectTransform to animate</param>
        /// <param name="start">Starting position</param>
        /// <param name="end">Target position</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="easing">Optional easing function. Uses linear if null</param>
        /// <param name="cancellationToken">Cancellation token for early termination</param>
        /// <returns>UniTask that completes when animation finishes</returns>
        /// <exception cref="ArgumentNullException">Thrown when rectTransform is null</exception>
        public static async UniTask TweenPositionAsync(
            RectTransform rectTransform,
            Vector2 start,
            Vector2 end,
            float duration,
            Func<float, float> easing = null,
            CancellationToken cancellationToken = default)
        {
            if (rectTransform == null) 
                throw new ArgumentNullException(nameof(rectTransform));

            if (duration <= 0f)
            {
                rectTransform.anchoredPosition = end;
                return;
            }

            easing ??= EaseLinear;
            rectTransform.anchoredPosition = start;

            await TweenVector2Async(
                value => rectTransform.anchoredPosition = value,
                start,
                end,
                duration,
                easing,
                cancellationToken
            );
        }

        #endregion

        #region Public Methods - Scale Tweening

        /// <summary>
        /// Tweens a Transform's localScale from start to end over duration seconds asynchronously.
        /// </summary>
        /// <param name="transform">Transform to animate</param>
        /// <param name="start">Starting scale</param>
        /// <param name="end">Target scale</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="easing">Optional easing function. Uses linear if null</param>
        /// <param name="cancellationToken">Cancellation token for early termination</param>
        /// <returns>UniTask that completes when animation finishes</returns>
        /// <exception cref="ArgumentNullException">Thrown when transform is null</exception>
        public static async UniTask TweenScaleAsync(
            Transform transform,
            Vector3 start,
            Vector3 end,
            float duration,
            Func<float, float> easing = null,
            CancellationToken cancellationToken = default)
        {
            if (transform == null) 
                throw new ArgumentNullException(nameof(transform));

            if (duration <= 0f)
            {
                transform.localScale = end;
                return;
            }

            easing ??= EaseLinear;
            transform.localScale = start;

            await TweenVector3Async(
                value => transform.localScale = value,
                start,
                end,
                duration,
                easing,
                cancellationToken
            );
        }

        #endregion

        #region Public Methods - Generic Tweening

        /// <summary>
        /// Generic tween for any float property with setter, with optional easing and cancellation.
        /// </summary>
        /// <param name="setter">Action to set the property value</param>
        /// <param name="start">Starting value</param>
        /// <param name="end">Target value</param>
        /// <param name="duration">Animation duration in seconds</param>
        /// <param name="easing">Optional easing function. Uses linear if null</param>
        /// <param name="cancellationToken">Cancellation token for early termination</param>
        /// <returns>UniTask that completes when animation finishes</returns>
        /// <exception cref="ArgumentNullException">Thrown when setter is null</exception>
        public static async UniTask TweenFloatAsync(
            Action<float> setter,
            float start,
            float end,
            float duration,
            Func<float, float> easing = null,
            CancellationToken cancellationToken = default)
        {
            if (setter == null) 
                throw new ArgumentNullException(nameof(setter));

            if (duration <= 0f)
            {
                setter(end);
                return;
            }

            easing ??= EaseLinear;
            setter(start);

            await TweenValueAsync(setter, start, end, duration, easing, cancellationToken);
        }

        #endregion

        #region Private Methods - Core Tween Implementation

        /// <summary>
        /// Core tween implementation for float values.
        /// </summary>
        /// <param name="setter">Action to set the value</param>
        /// <param name="start">Start value</param>
        /// <param name="end">End value</param>
        /// <param name="duration">Animation duration</param>
        /// <param name="easing">Easing function</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask for the animation</returns>
        private static async UniTask TweenValueAsync(
            Action<float> setter,
            float start,
            float end,
            float duration,
            Func<float, float> easing,
            CancellationToken cancellationToken)
        {
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

        /// <summary>
        /// Core tween implementation for Vector2 values.
        /// </summary>
        /// <param name="setter">Action to set the value</param>
        /// <param name="start">Start value</param>
        /// <param name="end">End value</param>
        /// <param name="duration">Animation duration</param>
        /// <param name="easing">Easing function</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask for the animation</returns>
        private static async UniTask TweenVector2Async(
            Action<Vector2> setter,
            Vector2 start,
            Vector2 end,
            float duration,
            Func<float, float> easing,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = easing(t);

                setter(Vector2.Lerp(start, end, easedT));
            }

            setter(end);
        }

        /// <summary>
        /// Core tween implementation for Vector3 values.
        /// </summary>
        /// <param name="setter">Action to set the value</param>
        /// <param name="start">Start value</param>
        /// <param name="end">End value</param>
        /// <param name="duration">Animation duration</param>
        /// <param name="easing">Easing function</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>UniTask for the animation</returns>
        private static async UniTask TweenVector3Async(
            Action<Vector3> setter,
            Vector3 start,
            Vector3 end,
            float duration,
            Func<float, float> easing,
            CancellationToken cancellationToken)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedT = easing(t);

                setter(Vector3.Lerp(start, end, easedT));
            }

            setter(end);
        }

        #endregion

        #region Easing Functions

        /// <summary>
        /// Linear easing function (no easing).
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseLinear(float t) => t;

        /// <summary>
        /// Quadratic ease-in function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseInQuad(float t) => t * t;

        /// <summary>
        /// Quadratic ease-out function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseOutQuad(float t) => t * (2f - t);

        /// <summary>
        /// Quadratic ease-in-out function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseInOutQuad(float t) =>
            t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;

        /// <summary>
        /// Cubic ease-in function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseInCubic(float t) => t * t * t;

        /// <summary>
        /// Cubic ease-out function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseOutCubic(float t)
        {
            float p = t - 1f;
            return p * p * p + 1f;
        }

        /// <summary>
        /// Cubic ease-in-out function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseInOutCubic(float t) =>
            t < 0.5f ? 4f * t * t * t : (t - 1f) * (2f * t - 2f) * (2f * t - 2f) + 1f;

        /// <summary>
        /// Sine ease-in function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseInSine(float t) => 1f - Mathf.Cos(t * Mathf.PI * 0.5f);

        /// <summary>
        /// Sine ease-out function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseOutSine(float t) => Mathf.Sin(t * Mathf.PI * 0.5f);

        /// <summary>
        /// Sine ease-in-out function.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseInOutSine(float t) => 0.5f * (1f - Mathf.Cos(t * Mathf.PI));

        /// <summary>
        /// Elastic ease-out function for bouncy animations.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseOutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            float p = 0.3f;
            float s = p / 4f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t - s) * (2f * Mathf.PI) / p) + 1f;
        }

        /// <summary>
        /// Back ease-out function for overshoot effects.
        /// </summary>
        /// <param name="t">Normalized time (0-1)</param>
        /// <returns>Eased value</returns>
        public static float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        #endregion
    }
}
