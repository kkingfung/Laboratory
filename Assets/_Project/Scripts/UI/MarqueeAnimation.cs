// 2025/7/21 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.UI;

public class MarqueeAnimation : MonoBehaviour
{
    public enum MarqueeDirection
    {
        Horizontal,
        Vertical
    }

    [SerializeField] private RawImage targetImage; // The image to animate
    [SerializeField] private MarqueeDirection direction = MarqueeDirection.Horizontal;
    [SerializeField] private float speed = 1.0f; // Speed of the animation

    private Vector2 uvOffset = Vector2.zero;

    private void Update()
    {
        if (targetImage == null) return;

        // Update the UV offset based on the selected direction and speed
        if (direction == MarqueeDirection.Horizontal)
        {
            uvOffset.x += speed * Time.deltaTime;
        }
        else if (direction == MarqueeDirection.Vertical)
        {
            uvOffset.y += speed * Time.deltaTime;
        }

        // Apply the UV offset to the material of the RawImage
        targetImage.uvRect = new Rect(uvOffset.x, uvOffset.y, targetImage.uvRect.width, targetImage.uvRect.height);
    }
}