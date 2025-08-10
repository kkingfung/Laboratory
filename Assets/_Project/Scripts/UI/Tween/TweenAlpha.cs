// 2025/7/21 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;

public class TweenAlpha : TweenAction
{
    [SerializeField] private CanvasGroup targetCanvasGroup;
    [SerializeField] private float targetAlpha;
    [SerializeField] private float duration;

    private float initialAlpha;

    private void Start()
    {
        if (targetCanvasGroup != null)
        {
            initialAlpha = targetCanvasGroup.alpha;
        }
    }

    protected override void PerformTween(float deltaTime)
    {
        if (targetCanvasGroup == null || duration <= 0) return;

        float alphaChange = (targetAlpha - initialAlpha) / duration * deltaTime;
        targetCanvasGroup.alpha = Mathf.Clamp(targetCanvasGroup.alpha + alphaChange, Mathf.Min(initialAlpha, targetAlpha), Mathf.Max(initialAlpha, targetAlpha));

        if (Mathf.Approximately(targetCanvasGroup.alpha, targetAlpha))
        {
            StopTween();
        }
    }
}
