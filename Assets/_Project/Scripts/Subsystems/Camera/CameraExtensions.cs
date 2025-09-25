using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// PERFORMANCE-OPTIMIZED Camera extensions with component caching and reflection optimization
    /// Eliminates expensive GetComponent and reflection calls through intelligent caching
    /// </summary>
    public static class CameraExtensions
    {
        // Cache for component references to eliminate repeated GetComponent calls
        private static Dictionary<MonoBehaviour, Component> _cachedCinemachineComponents = new Dictionary<MonoBehaviour, Component>();

        // Cache for reflection PropertyInfo to eliminate repeated reflection calls
        private static Dictionary<System.Type, PropertyInfo> _cachedFollowProperties = new Dictionary<System.Type, PropertyInfo>();
        private static Dictionary<System.Type, PropertyInfo> _cachedLookAtProperties = new Dictionary<System.Type, PropertyInfo>();

        public static void Follow(this MonoBehaviour camera, Transform target)
        {
            var virtualCamera = GetCachedCinemachineComponent(camera);
            if (virtualCamera != null)
            {
                var followProperty = GetCachedFollowProperty(virtualCamera.GetType());
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
            var virtualCamera = GetCachedCinemachineComponent(camera);
            if (virtualCamera != null)
            {
                var lookAtProperty = GetCachedLookAtProperty(virtualCamera.GetType());
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

        /// <summary>
        /// Get cached Cinemachine component - eliminates repeated GetComponent calls
        /// PERFORMANCE: O(1) lookup after first call per camera
        /// </summary>
        private static Component GetCachedCinemachineComponent(MonoBehaviour camera)
        {
            if (_cachedCinemachineComponents.TryGetValue(camera, out Component cached))
            {
                return cached; // Return cached reference - O(1) performance
            }

            // Find Cinemachine virtual camera component (only done once per camera)
            Component virtualCamera = null;

            // Try common Cinemachine component types
            var components = camera.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                var typeName = comp.GetType().Name;
                if (typeName.Contains("CinemachineVirtualCamera") ||
                    typeName.Contains("CinemachineFreeLook") ||
                    typeName.Contains("CinemachineStateDrivenCamera"))
                {
                    virtualCamera = comp;
                    break;
                }
            }

            // Cache the result (even if null) to avoid future lookups
            _cachedCinemachineComponents[camera] = virtualCamera;
            return virtualCamera;
        }

        /// <summary>
        /// Get cached Follow property reflection info - eliminates repeated reflection calls
        /// PERFORMANCE: O(1) lookup after first call per component type
        /// </summary>
        private static PropertyInfo GetCachedFollowProperty(System.Type type)
        {
            if (_cachedFollowProperties.TryGetValue(type, out PropertyInfo cached))
            {
                return cached; // Return cached reflection info - O(1) performance
            }

            var followProperty = type.GetProperty("Follow");
            _cachedFollowProperties[type] = followProperty; // Cache even if null
            return followProperty;
        }

        /// <summary>
        /// Get cached LookAt property reflection info - eliminates repeated reflection calls
        /// PERFORMANCE: O(1) lookup after first call per component type
        /// </summary>
        private static PropertyInfo GetCachedLookAtProperty(System.Type type)
        {
            if (_cachedLookAtProperties.TryGetValue(type, out PropertyInfo cached))
            {
                return cached; // Return cached reflection info - O(1) performance
            }

            var lookAtProperty = type.GetProperty("LookAt");
            _cachedLookAtProperties[type] = lookAtProperty; // Cache even if null
            return lookAtProperty;
        }

        /// <summary>
        /// Clear caches when cameras are destroyed - call this in OnDestroy
        /// </summary>
        public static void ClearCacheForCamera(MonoBehaviour camera)
        {
            _cachedCinemachineComponents.Remove(camera);
            // Note: We keep reflection caches as they're shared across all cameras of the same type
        }

        /// <summary>
        /// Clear all caches - call this when changing scenes or for memory cleanup
        /// </summary>
        public static void ClearAllCaches()
        {
            _cachedCinemachineComponents.Clear();
            _cachedFollowProperties.Clear();
            _cachedLookAtProperties.Clear();
        }
    }
}