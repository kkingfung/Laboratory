using Unity.Collections;
using System.Collections.Generic;

namespace Laboratory.Chimera.Utilities
{
    /// <summary>
    /// Extension methods for collections used throughout Chimera systems
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Get value from NativeHashMap with default fallback
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key, TValue defaultValue = default)
            where TKey : unmanaged, System.IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// Get value from Dictionary with default fallback
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default)
        {
            return dictionary.TryGetValue(key, out TValue value) ? value : defaultValue;
        }

        /// <summary>
        /// Check if NativeHashMap contains key
        /// </summary>
        public static bool ContainsKey<TKey, TValue>(this NativeHashMap<TKey, TValue> hashMap, TKey key)
            where TKey : unmanaged, System.IEquatable<TKey>
            where TValue : unmanaged
        {
            return hashMap.TryGetValue(key, out _);
        }
    }
}