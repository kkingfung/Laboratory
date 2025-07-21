// 2025/7/21 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public class TweenColor : TweenAction
{
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color targetColor;
    [SerializeField] private float duration;

    private Color initialColor;

    private void Start()
    {
        if (targetRenderer != null)
        {
            initialColor = targetRenderer.material.color;
        }
    }

    protected override void PerformTween(float deltaTime)
    {
        if (targetRenderer == null || duration <= 0) return;

        float t = Mathf.Clamp01(elapsedTime / duration);
        targetRenderer.material.color = Color.Lerp(initialColor, targetColor, t);

        if (Mathf.Approximately(t, 1.0f))
        {
            StopTween();
        }
    }
}