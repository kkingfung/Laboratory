using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Laboratory.Core.Utilities
{
    /// <summary>
    /// Core utility methods for Project Chimera
    /// Provides common functionality used across multiple systems
    /// </summary>
    public static class CoreUtilities
    {
        /// <summary>
        /// Safe distance calculation with overflow protection
        /// </summary>
        public static float DistanceSafe(Vector3 a, Vector3 b)
        {
            return Mathf.Clamp(Vector3.Distance(a, b), 0f, float.MaxValue);
        }

        /// <summary>
        /// Normalized direction vector with zero vector protection
        /// </summary>
        public static Vector3 DirectionSafe(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            if (direction.magnitude < 0.001f)
                return Vector3.forward;
            return direction.normalized;
        }

        /// <summary>
        /// Fast approximate distance calculation (avoids square root)
        /// </summary>
        public static float DistanceSquaredSafe(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude;
        }

        /// <summary>
        /// Clamp vector magnitude to specified range
        /// </summary>
        public static Vector3 ClampMagnitude(Vector3 vector, float minMagnitude, float maxMagnitude)
        {
            float magnitude = vector.magnitude;
            if (magnitude < 0.001f)
                return Vector3.zero;

            magnitude = Mathf.Clamp(magnitude, minMagnitude, maxMagnitude);
            return vector.normalized * magnitude;
        }

        /// <summary>
        /// Linear interpolation with time delta clamping
        /// </summary>
        public static float LerpSafe(float a, float b, float t)
        {
            return Mathf.Lerp(a, b, Mathf.Clamp01(t));
        }

        /// <summary>
        /// Vector3 linear interpolation with time delta clamping
        /// </summary>
        public static Vector3 LerpSafe(Vector3 a, Vector3 b, float t)
        {
            return Vector3.Lerp(a, b, Mathf.Clamp01(t));
        }

        /// <summary>
        /// Checks if a value is approximately zero with specified tolerance
        /// </summary>
        public static bool IsApproximatelyZero(float value, float tolerance = 0.001f)
        {
            return Mathf.Abs(value) < tolerance;
        }

        /// <summary>
        /// Checks if two vectors are approximately equal
        /// </summary>
        public static bool IsApproximatelyEqual(Vector3 a, Vector3 b, float tolerance = 0.001f)
        {
            return Vector3.Distance(a, b) < tolerance;
        }

        /// <summary>
        /// Safe array access with bounds checking
        /// </summary>
        public static T GetSafe<T>(T[] array, int index, T defaultValue = default(T))
        {
            if (array == null || index < 0 || index >= array.Length)
                return defaultValue;
            return array[index];
        }

        /// <summary>
        /// Safe list access with bounds checking
        /// </summary>
        public static T GetSafe<T>(List<T> list, int index, T defaultValue = default(T))
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;
            return list[index];
        }

        /// <summary>
        /// Remove null entries from a list
        /// </summary>
        public static void RemoveNulls<T>(List<T> list) where T : class
        {
            if (list == null) return;
            list.RemoveAll(item => item == null);
        }

        /// <summary>
        /// Hash string to integer for performance-critical lookups
        /// </summary>
        public static int HashString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return 0;

            int hash = 17;
            foreach (char c in input)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }

        /// <summary>
        /// Weighted random selection from a list
        /// </summary>
        public static T WeightedRandomSelect<T>(List<T> items, List<float> weights)
        {
            if (items == null || weights == null || items.Count != weights.Count || items.Count == 0)
                return default(T);

            float totalWeight = weights.Sum();
            if (totalWeight <= 0f)
                return items[Random.Range(0, items.Count)];

            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;

            for (int i = 0; i < items.Count; i++)
            {
                currentWeight += weights[i];
                if (randomValue <= currentWeight)
                    return items[i];
            }

            return items[items.Count - 1];
        }
    }
}