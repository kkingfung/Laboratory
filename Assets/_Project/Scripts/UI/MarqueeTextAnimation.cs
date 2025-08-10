// 2025/7/21 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.UI;

public class MarqueeTextAnimation : MonoBehaviour
{
    public enum MarqueeDirection
    {
        Horizontal,
        Vertical
    }

    [SerializeField] private Text targetText; // The Text component to animate
    [SerializeField] private MarqueeDirection direction = MarqueeDirection.Horizontal;
    [SerializeField] private float speed = 50.0f; // Speed of the marquee animation
    [SerializeField] private RectTransform viewport; // The viewport RectTransform to constrain the text

    private RectTransform textRectTransform;
    private Vector2 startPosition;

    private void Start()
    {
        if (targetText == null || viewport == null)
        {
            Debug.LogError("MarqueeTextAnimation: Target Text or Viewport is not assigned.");
            enabled = false;
            return;
        }

        textRectTransform = targetText.GetComponent<RectTransform>();
        startPosition = textRectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (textRectTransform == null || viewport == null) return;

        Vector2 position = textRectTransform.anchoredPosition;

        // Update position based on the direction
        if (direction == MarqueeDirection.Horizontal)
        {
            position.x -= speed * Time.deltaTime;

            // Reset position when the text moves out of the viewport
            if (position.x + textRectTransform.rect.width < -viewport.rect.width / 2)
            {
                position.x = viewport.rect.width / 2;
            }
        }
        else if (direction == MarqueeDirection.Vertical)
        {
            position.y -= speed * Time.deltaTime;

            // Reset position when the text moves out of the viewport
            if (position.y + textRectTransform.rect.height < -viewport.rect.height / 2)
            {
                position.y = viewport.rect.height / 2;
            }
        }

        textRectTransform.anchoredPosition = position;
    }
}