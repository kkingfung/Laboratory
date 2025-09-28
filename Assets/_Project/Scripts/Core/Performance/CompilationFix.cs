// This file exists purely to force Unity to recompile the Core assembly
// and recognize the new OptimizedMonoBehaviour class
// You can delete this file once compilation issues are resolved.

using UnityEngine;

namespace Laboratory.Core.Performance
{
    /// <summary>
    /// Temporary compilation fix to ensure Unity recognizes the Performance namespace
    /// </summary>
    public static class CompilationFix
    {
        /// <summary>
        /// This method exists to validate that OptimizedMonoBehaviour is accessible
        /// </summary>
        public static void ValidateOptimizedMonoBehaviour()
        {
            // This should compile without errors if the namespace is working correctly
            System.Type optimizedType = typeof(OptimizedMonoBehaviour);
            Debug.Log($"OptimizedMonoBehaviour type found: {optimizedType.Name}");
        }
    }
}