using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Laboratory.Chimera.Consciousness.Memory;

namespace Laboratory.Chimera.Consciousness.UI
{
    /// <summary>
    /// UI component for displaying individual memory items in the personality panel
    /// </summary>
    public class MemoryDisplayItem : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI memoryTitleText;
        [SerializeField] private TextMeshProUGUI memoryDescriptionText;
        [SerializeField] private TextMeshProUGUI memoryTimeText;
        [SerializeField] private Image memoryIcon;
        [SerializeField] private Image backgroundImage;

        [Header("Memory Type Colors")]
        [SerializeField] private Color positiveMemoryColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
        [SerializeField] private Color negativeMemoryColor = new Color(0.8f, 0.3f, 0.3f, 0.8f);
        [SerializeField] private Color neutralMemoryColor = new Color(0.7f, 0.7f, 0.7f, 0.8f);

        [Header("Memory Icons")]
        [SerializeField] private Sprite positiveIcon;
        [SerializeField] private Sprite negativeIcon;
        [SerializeField] private Sprite neutralIcon;

        /// <summary>
        /// Display a memory with details
        /// </summary>
        public void SetMemory(string title, string description, InteractionType interactionType, float timeAgo)
        {
            if (memoryTitleText != null)
                memoryTitleText.text = title;

            if (memoryDescriptionText != null)
                memoryDescriptionText.text = description;

            if (memoryTimeText != null)
                memoryTimeText.text = FormatTimeAgo(timeAgo);

            SetMemoryStyle(interactionType);
        }

        /// <summary>
        /// Set visual style based on memory type
        /// </summary>
        private void SetMemoryStyle(InteractionType interactionType)
        {
            Color backgroundColor;
            Sprite iconSprite;

            switch (interactionType)
            {
                case InteractionType.Positive:
                    backgroundColor = positiveMemoryColor;
                    iconSprite = positiveIcon;
                    break;
                case InteractionType.Negative:
                    backgroundColor = negativeMemoryColor;
                    iconSprite = negativeIcon;
                    break;
                default:
                    backgroundColor = neutralMemoryColor;
                    iconSprite = neutralIcon;
                    break;
            }

            if (backgroundImage != null)
                backgroundImage.color = backgroundColor;

            if (memoryIcon != null && iconSprite != null)
                memoryIcon.sprite = iconSprite;
        }

        /// <summary>
        /// Format time ago as human-readable string
        /// </summary>
        private string FormatTimeAgo(float timeAgo)
        {
            if (timeAgo < 60f)
                return "Just now";
            else if (timeAgo < 3600f)
                return $"{Mathf.FloorToInt(timeAgo / 60f)} min ago";
            else if (timeAgo < 86400f)
                return $"{Mathf.FloorToInt(timeAgo / 3600f)} hr ago";
            else
                return $"{Mathf.FloorToInt(timeAgo / 86400f)} days ago";
        }

        /// <summary>
        /// Highlight memory item with animation
        /// </summary>
        public void HighlightMemory()
        {
            StartCoroutine(HighlightAnimation());
        }

        private System.Collections.IEnumerator HighlightAnimation()
        {
            if (backgroundImage == null) yield break;

            Color originalColor = backgroundImage.color;
            Color highlightColor = new Color(originalColor.r, originalColor.g, originalColor.b, 1f);

            // Fade in highlight
            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                backgroundImage.color = Color.Lerp(originalColor, highlightColor, elapsed / duration);
                yield return null;
            }

            // Hold highlight
            yield return new WaitForSeconds(0.5f);

            // Fade back
            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                backgroundImage.color = Color.Lerp(highlightColor, originalColor, elapsed / duration);
                yield return null;
            }

            backgroundImage.color = originalColor;
        }

        /// <summary>
        /// Setup memory with data - wrapper for SetMemory with additional initialization
        /// </summary>
        public void SetupMemory(string title, string description, InteractionType interactionType, float timeAgo)
        {
            SetMemory(title, description, interactionType, timeAgo);

            // Initialize for animation
            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Animate the appearance of the memory item
        /// </summary>
        public void AnimateAppearance(float delay = 0f)
        {
            StartCoroutine(AnimateAppearanceCoroutine(delay));
        }

        /// <summary>
        /// Get the coroutine for animating appearance
        /// </summary>
        public Coroutine GetAnimateAppearanceCoroutine(float delay = 0f)
        {
            return StartCoroutine(AnimateAppearanceCoroutine(delay));
        }

        private IEnumerator AnimateAppearanceCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            var canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            float duration = 0.4f;
            float elapsed = 0f;
            Vector3 startPosition = transform.localPosition;
            Vector3 startScale = transform.localScale;

            // Start from right side with small scale
            transform.localPosition = startPosition + Vector3.right * 100f;
            transform.localScale = Vector3.one * 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Ease out animation
                t = 1f - (1f - t) * (1f - t);

                canvasGroup.alpha = t;
                transform.localPosition = Vector3.Lerp(startPosition + Vector3.right * 100f, startPosition, t);
                transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, startScale, t);

                yield return null;
            }

            canvasGroup.alpha = 1f;
            transform.localPosition = startPosition;
            transform.localScale = startScale;
        }
    }
}