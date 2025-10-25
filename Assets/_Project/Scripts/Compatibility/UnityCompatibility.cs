using UnityEngine;

namespace Laboratory.Compatibility
{
    /// <summary>
    /// Unity API compatibility utilities for different Unity versions
    /// </summary>
    public static class UnityCompatibility
    {
        /// <summary>
        /// FindFirstObjectByType compatibility method for Unity versions before 2023.1
        /// </summary>
        public static T FindFirstObjectByType<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        /// <summary>
        /// FindFirstObjectByType compatibility method for non-generic usage
        /// </summary>
        public static Object FindFirstObjectByType(System.Type type)
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType(type);
#else
            return Object.FindObjectOfType(type);
#endif
        }
    }
}