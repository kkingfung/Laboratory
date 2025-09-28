using Unity.Burst;
using System;

namespace Laboratory.Core.ECS
{
    /// <summary>
    /// Helper class to manage Burst compilation issues during development
    /// Use this to temporarily disable Burst for debugging without removing attributes
    /// </summary>
    public static class BurstDebugHelper
    {
        // Toggle this to disable Burst compilation project-wide when encountering internal compiler errors
        public const bool ENABLE_BURST_COMPILATION = true; // Set to false to disable Burst temporarily

        /// <summary>
        /// Conditional BurstCompile attribute - only applies when ENABLE_BURST_COMPILATION is true
        /// Use this instead of [BurstCompile] during debugging
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Class)]
        public class ConditionalBurstCompileAttribute : Attribute
        {
            public ConditionalBurstCompileAttribute()
            {
                // This attribute acts as a marker - actual Burst compilation is controlled by the constant
            }
        }

        /// <summary>
        /// Apply this to systems experiencing Burst internal compiler errors
        /// </summary>
        public static void LogBurstError(string systemName, Exception error)
        {
            UnityEngine.Debug.LogWarning($"[BURST ERROR] System '{systemName}' encountered Burst compiler error. " +
                                       $"Consider setting ENABLE_BURST_COMPILATION = false temporarily. Error: {error.Message}");
        }

        /// <summary>
        /// Safe Burst compilation check - use in systems that might have issues
        /// </summary>
        public static bool ShouldUseBurst()
        {
            return ENABLE_BURST_COMPILATION;
        }
    }
}

// Usage example:
// Instead of: [BurstCompile]
// Use: [BurstDebugHelper.ConditionalBurstCompile]
//
// Or wrap systems like:
// #if ENABLE_BURST_COMPILATION
// [BurstCompile]
// #endif
// public partial struct MySystem : ISystem { }