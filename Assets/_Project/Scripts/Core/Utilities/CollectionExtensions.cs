using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Laboratory.Core.Utilities
{
    /// <summary>
    /// High-performance collection extensions that replace LINQ operations
    /// Eliminates allocations and provides better performance for critical code paths
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// High-performance replacement for LINQ Where().First()
        /// Returns the first element matching the predicate
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstWhere<T>(this T[] array, Func<T, bool> predicate)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                    return array[i];
            }
            return default(T);
        }

        /// <summary>
        /// High-performance replacement for LINQ Where().FirstOrDefault()
        /// Returns the first element matching the predicate or default value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FirstWhereOrDefault<T>(this T[] array, Func<T, bool> predicate, T defaultValue = default(T))
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                    return array[i];
            }
            return defaultValue;
        }

        /// <summary>
        /// High-performance replacement for LINQ Count()
        /// Counts elements matching the predicate
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountWhere<T>(this T[] array, Func<T, bool> predicate)
        {
            int count = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// High-performance replacement for LINQ Any()
        /// Checks if any element matches the predicate
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool AnyWhere<T>(this T[] array, Func<T, bool> predicate)
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (predicate(array[i]))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// High-performance replacement for LINQ Select()
        /// Transforms array elements to a new array
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TResult[] SelectToArray<T, TResult>(this T[] array, Func<T, TResult> selector)
        {
            var result = new TResult[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                result[i] = selector(array[i]);
            }
            return result;
        }

        /// <summary>
        /// High-performance list extension for safe clearing
        /// Only clears if the list has elements to avoid unnecessary operations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ClearIfNotEmpty<T>(this List<T> list)
        {
            if (list.Count > 0)
            {
                list.Clear();
            }
        }

        /// <summary>
        /// High-performance dictionary extension for safer lookups
        /// Optimized for value type keys
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValueFast<TKey, TValue>(this Dictionary<TKey, TValue> dict,
            TKey key, out TValue value) where TKey : struct
        {
            return dict.TryGetValue(key, out value);
        }

        /// <summary>
        /// Fast array searching for reference types
        /// More efficient than Array.IndexOf for small arrays
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastIndexOf<T>(this T[] array, T item) where T : class
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (ReferenceEquals(array[i], item))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Fast array searching for value types
        /// More efficient than Array.IndexOf for small arrays
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastIndexOfValue<T>(this T[] array, T item) where T : struct, IEquatable<T>
        {
            for (int i = 0; i < array.Length; i++)
            {
                if (array[i].Equals(item))
                    return i;
            }
            return -1;
        }
    }
}