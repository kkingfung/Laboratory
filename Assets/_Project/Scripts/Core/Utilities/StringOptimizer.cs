using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Laboratory.Core.Utilities
{
    /// <summary>
    /// String optimization utilities to eliminate allocations in performance-critical code
    /// Features: Cached number strings, StringBuilder pooling, zero-allocation formatting
    /// </summary>
    public static class StringOptimizer
    {
        // Pre-allocated string caches for common values
        private static readonly string[] _cachedNumbers = new string[1000];
        private static readonly string[] _cachedPercentages = new string[101];
        private static readonly ObjectPool<StringBuilder> _stringBuilderPool =
            new ObjectPool<StringBuilder>(() => new StringBuilder(256), sb => sb.Clear());

        static StringOptimizer()
        {
            InitializeCachedStrings();
        }

        /// <summary>
        /// Zero-allocation number to string conversion for common values (0-999)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToStringCached(this int value)
        {
            if (value >= 0 && value < _cachedNumbers.Length)
                return _cachedNumbers[value];

            return value.ToString();
        }

        /// <summary>
        /// Zero-allocation percentage formatting for values 0-100%
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToPercentString(this float value)
        {
            int percentage = Mathf.RoundToInt(value * 100f);
            if (percentage >= 0 && percentage <= 100)
                return _cachedPercentages[percentage];

            return $"{percentage}%";
        }

        /// <summary>
        /// Optimized string formatting using pooled StringBuilder
        /// Use this instead of string.Format or string interpolation in hot paths
        /// </summary>
        public static string FormatOptimized(string format, params object[] args)
        {
            var sb = _stringBuilderPool.Get();
            try
            {
                sb.AppendFormat(format, args);
                return sb.ToString();
            }
            finally
            {
                _stringBuilderPool.Return(sb);
            }
        }

        /// <summary>
        /// Optimized debug logging that compiles out in release builds
        /// Use this instead of Debug.Log in performance-critical sections
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogOptimized(string message)
        {
            Debug.Log(message);
        }

        /// <summary>
        /// Optimized debug logging with formatting
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogOptimizedFormat(string format, params object[] args)
        {
            Debug.Log(FormatOptimized(format, args));
        }

        private static void InitializeCachedStrings()
        {
            // Cache common numbers (0-999)
            for (int i = 0; i < _cachedNumbers.Length; i++)
            {
                _cachedNumbers[i] = i.ToString();
            }

            // Cache percentage strings (0%-100%)
            for (int i = 0; i <= 100; i++)
            {
                _cachedPercentages[i] = $"{i}%";
            }
        }
    }
}