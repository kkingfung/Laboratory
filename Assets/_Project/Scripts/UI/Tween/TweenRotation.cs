using UnityEngine;

public class TweenRotation : TweenAction
{
    [SerializeField] private Transform targetTransform;
    [SerializeField] private Vector3 targetRotation;
    [SerializeField] private float duration;

    private Quaternion initialRotation;

    private void Start()
    {
        if (targetTransform != null)
        {
            initialRotation = targetTransform.rotation;
        }
    }

    protected override void PerformTween(float deltaTime)
    {
        if (targetTransform == null || duration <= 0) return;

        Quaternion targetQuat = Quaternion.Euler(targetRotation);
        float t = Mathf.Clamp01(elapsedTime / duration); // Accessing protected elapsedTime
        targetTransform.rotation = Quaternion.Lerp(initialRotation, targetQuat, t);

        if (Mathf.Approximately(t, 1.0f))
        {
            StopTween();
        }
    }
}
