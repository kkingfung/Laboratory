using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Laboratory.Chimera.Consciousness.UI
{
    /// <summary>
    /// UI component for displaying personality traits as visual bars
    /// </summary>
    public class PersonalityTraitBar : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI traitNameText;
        [SerializeField] private Slider traitSlider;
        [SerializeField] private TextMeshProUGUI traitValueText;
        [SerializeField] private Image fillImage;

        [Header("Visual Settings")]
        [SerializeField] private Color lowValueColor = Color.red;
        [SerializeField] private Color midValueColor = Color.yellow;
        [SerializeField] private Color highValueColor = Color.green;

        /// <summary>
        /// Set trait values and update display
        /// </summary>
        public void SetTrait(string traitName, float value, float maxValue = 100f)
        {
            if (traitNameText != null)
                traitNameText.text = traitName;

            if (traitSlider != null)
            {
                traitSlider.minValue = 0f;
                traitSlider.maxValue = maxValue;
                traitSlider.value = value;
            }

            if (traitValueText != null)
                traitValueText.text = $"{value:F0}/{maxValue:F0}";

            if (fillImage != null)
            {
                float normalizedValue = value / maxValue;
                fillImage.color = GetColorForValue(normalizedValue);
            }
        }

        /// <summary>
        /// Get color based on trait value
        /// </summary>
        private Color GetColorForValue(float normalizedValue)
        {
            if (normalizedValue <= 0.5f)
                return Color.Lerp(lowValueColor, midValueColor, normalizedValue * 2f);
            else
                return Color.Lerp(midValueColor, highValueColor, (normalizedValue - 0.5f) * 2f);
        }

        /// <summary>
        /// Animate trait value change
        /// </summary>
        public void AnimateToValue(float newValue, float maxValue = 100f)
        {
            if (traitSlider != null)
            {
                StartCoroutine(AnimateSlider(newValue, maxValue));
            }
        }

        private System.Collections.IEnumerator AnimateSlider(float targetValue, float maxValue)
        {
            float startValue = traitSlider.value;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float currentValue = Mathf.Lerp(startValue, targetValue, elapsed / duration);

                traitSlider.value = currentValue;

                if (traitValueText != null)
                    traitValueText.text = $"{currentValue:F0}/{maxValue:F0}";

                if (fillImage != null)
                {
                    float normalizedValue = currentValue / maxValue;
                    fillImage.color = GetColorForValue(normalizedValue);
                }

                yield return null;
            }

            // Ensure final values are set correctly
            SetTrait(traitNameText?.text ?? "", targetValue, maxValue);
        }

        /// <summary>
        /// Initialize the trait bar with basic setup
        /// </summary>
        public void Initialize(string traitName, float initialValue, float maxValue = 100f)
        {
            SetTrait(traitName, initialValue, maxValue);

            // Set initial transparency for animation
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Initialize the trait bar with color override
        /// </summary>
        public void Initialize(string traitName, float initialValue, Color traitColor)
        {
            SetTrait(traitName, initialValue, 100f);

            // Override the color
            if (fillImage != null)
                fillImage.color = traitColor;

            // Set initial transparency for animation
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Animate the appearance of the trait bar
        /// </summary>
        public void AnimateAppearance(float delay = 0f)
        {
            StartCoroutine(AnimateAppearanceCoroutine(delay));
        }

        /// <summary>
        /// Animate the appearance of the trait bar with animation curve
        /// </summary>
        public void AnimateAppearance(float delay, AnimationCurve curve)
        {
            StartCoroutine(AnimateAppearanceCoroutine(delay, curve));
        }

        /// <summary>
        /// Get the coroutine for animating appearance with animation curve
        /// </summary>
        public Coroutine GetAnimateAppearanceCoroutine(float delay, AnimationCurve curve)
        {
            return StartCoroutine(AnimateAppearanceCoroutine(delay, curve));
        }

        private IEnumerator AnimateAppearanceCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            transform.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease out animation
                t = 1f - (1f - t) * (1f - t);

                canvasGroup.alpha = t;
                transform.localScale = Vector3.Lerp(Vector3.zero, startScale, t);

                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = startScale;
        }

        private IEnumerator AnimateAppearanceCoroutine(float delay, AnimationCurve curve)
        {
            yield return new WaitForSeconds(delay);

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            float duration = 0.5f;
            float elapsed = 0f;
            Vector3 startScale = transform.localScale;
            transform.localScale = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Use animation curve if provided
                if (curve != null)
                    t = curve.Evaluate(t);
                else
                    t = 1f - (1f - t) * (1f - t); // Default ease out

                canvasGroup.alpha = t;
                transform.localScale = Vector3.Lerp(Vector3.zero, startScale, t);

                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localScale = startScale;
        }
    }
}