using UnityEngine;

public class TweenPosition : TweenAction
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float duration;

    private Vector3 initialPosition;

    private void Start()
    {
        if (targetTransform != null)
        {
            initialPosition = targetTransform.position;
        }
    }

    protected override void PerformTween(float deltaTime)
    {
        if (targetTransform == null || duration <= 0) return;

        float t = Mathf.Clamp01(elapsedTime / duration); // Accessing protected elapsedTime
        targetTransform.position = Vector3.Lerp(initialPosition, targetPosition, t);

        if (Mathf.Approximately(t, 1.0f))
        {
            StopTween();
        }
    }
}
