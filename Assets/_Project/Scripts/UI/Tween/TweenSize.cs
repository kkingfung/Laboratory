using UnityEngine;

public class TweenSize : TweenAction
{
    [SerializeField] private RectTransform targetRectTransform;
    [SerializeField] private Vector2 targetSize;
    [SerializeField] private float duration;

    private Vector2 initialSize;

    private void Start()
    {
        if (targetRectTransform != null)
        {
            initialSize = targetRectTransform.sizeDelta;
        }
    }

    protected override void PerformTween(float deltaTime)
    {
        if (targetRectTransform == null || duration <= 0) return;

        float t = Mathf.Clamp01(elapsedTime / duration); // Accessing protected elapsedTime
        targetRectTransform.sizeDelta = Vector2.Lerp(initialSize, targetSize, t);

        if (Mathf.Approximately(t, 1.0f))
        {
            StopTween();
        }
    }
}
