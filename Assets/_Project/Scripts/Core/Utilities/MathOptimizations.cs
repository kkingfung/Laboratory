using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Laboratory.Core.Utilities
{
    /// <summary>
    /// Optimized math utilities for performance-critical calculations
    /// Features: Vectorized operations, fast approximations, aggressive inlining
    /// </summary>
    public static class MathOptimizations
    {
        // Pre-computed constants for maximum performance
        public const float EPSILON = 1e-6f;
        public const float VERY_SMALL_NUMBER = 1e-8f;
        public const float PI = math.PI;
        public const float TWO_PI = math.PI * 2f;
        public const float HALF_PI = math.PI * 0.5f;
        public const float INV_PI = 1f / math.PI;
        public const float DEG_TO_RAD = math.PI / 180f;
        public const float RAD_TO_DEG = 180f / math.PI;

        /// <summary>
        /// Fast distance comparison without expensive square root
        /// Use this when you only need to compare distances
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsWithinDistanceSquared(float3 a, float3 b, float distanceSquared)
        {
            return math.distancesq(a, b) <= distanceSquared;
        }

        /// <summary>
        /// Safe vector normalization with fallback
        /// Prevents division by zero and returns fallback for zero-length vectors
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 SafeNormalize(float3 vector, float3 fallback = default)
        {
            float lengthSq = math.lengthsq(vector);
            return lengthSq > EPSILON ? vector * math.rsqrt(lengthSq) : fallback;
        }

        /// <summary>
        /// Fast approximate equality check for floating point values
        /// More efficient than Mathf.Approximately for Burst-compiled code
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool FastApproximatelyEqual(float a, float b, float epsilon = EPSILON)
        {
            return math.abs(a - b) < epsilon;
        }

        /// <summary>
        /// Fast floor operation using Unity.Mathematics
        /// Optimized for Burst compilation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastFloor(float value)
        {
            return (int)math.floor(value);
        }

        /// <summary>
        /// Fast ceiling operation using Unity.Mathematics
        /// Optimized for Burst compilation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastCeil(float value)
        {
            return (int)math.ceil(value);
        }

        /// <summary>
        /// Fast clamping to 0-1 range
        /// More efficient than Mathf.Clamp01 for repeated operations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastClamp01(float value)
        {
            return math.clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Fast modulo operation for positive integers
        /// More efficient than % operator for certain use cases
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastMod(int value, int divisor)
        {
            return value - (value / divisor) * divisor;
        }

        /// <summary>
        /// Fast linear interpolation using Unity.Mathematics
        /// Optimized for Burst compilation and vectorization
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastLerp(float a, float b, float t)
        {
            return math.lerp(a, b, t);
        }

        /// <summary>
        /// Fast squared distance calculation
        /// Use when you don't need the actual distance value
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastDistanceSquared(float3 a, float3 b)
        {
            return math.distancesq(a, b);
        }

        /// <summary>
        /// Fast 2D distance squared calculation
        /// Optimized for XZ plane calculations (ignoring Y)
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FastDistance2DSquared(float3 a, float3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// Fast power of 2 check for integers
        /// Useful for optimizing array sizes and bit operations
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Fast next power of 2 calculation
        /// Useful for optimizing buffer sizes
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOfTwo(int value)
        {
            value--;
            value |= value >> 1;
            value |= value >> 2;
            value |= value >> 4;
            value |= value >> 8;
            value |= value >> 16;
            return value + 1;
        }

        /// <summary>
        /// Fast hash function for 3D coordinates
        /// Useful for spatial hashing and procedural generation
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FastHash3D(int3 position)
        {
            uint hash = (uint)(position.x * 73856093) ^ (uint)(position.y * 19349663) ^ (uint)(position.z * 83492791);
            return hash;
        }

        /// <summary>
        /// Fast 2D hash function for grid coordinates
        /// Optimized for spatial partitioning systems
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FastHash2D(int2 position)
        {
            uint hash = (uint)(position.x * 73856093) ^ (uint)(position.y * 19349663);
            return hash;
        }
    }
}