using UnityEngine;
using Cinemachine;

public class ReplayCameraFollow : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField] private Transform target;

    void Start()
    {
        if (virtualCamera != null && target != null)
        {
            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;
        }
    }

    // Optional: switch to different targets during playback
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (virtualCamera != null)
        {
            virtualCamera.Follow = target;
            virtualCamera.LookAt = target;
        }
    }
}
