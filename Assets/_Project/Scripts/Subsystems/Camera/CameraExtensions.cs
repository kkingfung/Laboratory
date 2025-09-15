using UnityEngine;

namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// Camera component extensions for handling missing Follow/LookAt methods
    /// </summary>
    public static class CameraExtensions
    {
        public static void Follow(this MonoBehaviour camera, Transform target)
        {
            // Try to find Cinemachine components
            var virtualCamera = camera.GetComponent<MonoBehaviour>();
            if (virtualCamera != null)
            {
                // Use reflection to set Follow property if available
                var followProperty = virtualCamera.GetType().GetProperty("Follow");
                if (followProperty != null && followProperty.CanWrite)
                {
                    followProperty.SetValue(virtualCamera, target);
                    return;
                }
            }
            
            // Fallback: just move the camera to follow manually
            if (target != null)
            {
                camera.transform.position = target.position + Vector3.back * 5f + Vector3.up * 2f;
            }
        }
        
        public static void LookAt(this MonoBehaviour camera, Transform target)
        {
            // Try to find Cinemachine components
            var virtualCamera = camera.GetComponent<MonoBehaviour>();
            if (virtualCamera != null)
            {
                // Use reflection to set LookAt property if available
                var lookAtProperty = virtualCamera.GetType().GetProperty("LookAt");
                if (lookAtProperty != null && lookAtProperty.CanWrite)
                {
                    lookAtProperty.SetValue(virtualCamera, target);
                    return;
                }
            }
            
            // Fallback: manually point camera at target
            if (target != null)
            {
                camera.transform.LookAt(target);
            }
        }
    }
}
