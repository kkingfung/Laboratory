using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// ECS/DOTS implementation of ragdoll transform compression for networked ragdolls.
    /// Provides efficient compression and decompression of LocalTransform data for network transmission.
    /// </summary>
    /// <remarks>
    /// This static utility class handles compression of bone transform data in the ECS architecture,
    /// using quantization techniques to minimize network bandwidth while maintaining acceptable precision.
    /// </remarks>
    public static class RagdollTransformCompressorByDots
    {
        #region Data Structures

        /// <summary>
        /// Contains compressed transform data for a set of ragdoll bones.
        /// Uses native arrays for optimal performance in ECS systems.
        /// </summary>
        public struct CompressedData
        {
            /// <summary>
            /// Compressed bone positions using 16 bits per axis
            /// </summary>
            public NativeArray<uint3> CompressedPositions;

            /// <summary>
            /// Compressed bone rotations using 16 bits per component
            /// </summary>
            public NativeArray<uint4> CompressedRotations;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Compresses transform data from a collection of bone entities into a compact format.
        /// Uses delta compression against reference transforms to minimize data size.
        /// </summary>
        /// <param name="entityManager">EntityManager for accessing component data</param>
        /// <param name="boneEntities">Array of bone entities to compress</param>
        /// <param name="referencePosition">Reference position for delta compression</param>
        /// <param name="referenceRotation">Reference rotation for delta compression</param>
        /// <param name="allocator">Memory allocator for the compressed data arrays</param>
        /// <returns>Compressed transform data ready for network transmission</returns>
        public static CompressedData Compress(
            EntityManager entityManager, 
            NativeArray<Entity> boneEntities, 
            float3 referencePosition, 
            quaternion referenceRotation, 
            Allocator allocator)
        {
            int count = boneEntities.Length;
            CompressedData data = new CompressedData
            {
                CompressedPositions = new NativeArray<uint3>(count, allocator),
                CompressedRotations = new NativeArray<uint4>(count, allocator)
            };

            for (int i = 0; i < count; i++)
            {
                var transform = entityManager.GetComponentData<LocalTransform>(boneEntities[i]);

                // Calculate deltas from reference transforms
                float3 deltaPos = transform.Position - referencePosition;
                data.CompressedPositions[i] = CompressVector(deltaPos);

                quaternion deltaRot = math.mul(transform.Rotation, math.inverse(referenceRotation));
                data.CompressedRotations[i] = CompressQuaternion(deltaRot);
            }

            return data;
        }

        /// <summary>
        /// Decompresses transform data and applies it to the specified bone entities.
        /// Reconstructs original transforms by combining deltas with reference transforms.
        /// </summary>
        /// <param name="entityManager">EntityManager for setting component data</param>
        /// <param name="boneEntities">Array of bone entities to update</param>
        /// <param name="data">Compressed transform data to decompress</param>
        /// <param name="referencePosition">Reference position used during compression</param>
        /// <param name="referenceRotation">Reference rotation used during compression</param>
        public static void Decompress(
            EntityManager entityManager, 
            NativeArray<Entity> boneEntities, 
            CompressedData data, 
            float3 referencePosition, 
            quaternion referenceRotation)
        {
            int count = boneEntities.Length;
            for (int i = 0; i < count; i++)
            {
                float3 pos = DecompressVector(data.CompressedPositions[i]) + referencePosition;
                quaternion rot = math.mul(DecompressQuaternion(data.CompressedRotations[i]), referenceRotation);

                if (entityManager.HasComponent<LocalTransform>(boneEntities[i]))
                {
                    var transform = entityManager.GetComponentData<LocalTransform>(boneEntities[i]);
                    transform.Position = pos;
                    transform.Rotation = rot;
                    entityManager.SetComponentData(boneEntities[i], transform);
                }
            }
        }

        #endregion

        #region Private Methods - Compression Helpers

        /// <summary>
        /// Compresses a float3 vector using 16-bit quantization per axis.
        /// </summary>
        /// <param name="v">Vector to compress</param>
        /// <returns>Compressed vector as uint3</returns>
        private static uint3 CompressVector(float3 v)
        {
            const float max = 10f; // Adjust to expected delta range
            const uint scale = 65535; // 16 bits per axis
            
            return new uint3(
                (uint)math.clamp(math.round((v.x + max) / (2 * max) * scale), 0, scale),
                (uint)math.clamp(math.round((v.y + max) / (2 * max) * scale), 0, scale),
                (uint)math.clamp(math.round((v.z + max) / (2 * max) * scale), 0, scale)
            );
        }

        /// <summary>
        /// Decompresses a uint3 back to a float3 vector.
        /// </summary>
        /// <param name="c">Compressed vector data</param>
        /// <returns>Decompressed float3 vector</returns>
        private static float3 DecompressVector(uint3 c)
        {
            const float max = 10f;
            const float scale = 65535f;
            
            return new float3(
                (c.x / scale) * 2f * max - max,
                (c.y / scale) * 2f * max - max,
                (c.z / scale) * 2f * max - max
            );
        }

        /// <summary>
        /// Compresses a quaternion using 16-bit quantization per component.
        /// </summary>
        /// <param name="q">Quaternion to compress</param>
        /// <returns>Compressed quaternion as uint4</returns>
        private static uint4 CompressQuaternion(quaternion q)
        {
            const uint scale = 65535;
            
            return new uint4(
                (uint)math.clamp(math.round((q.value.x + 1f) / 2f * scale), 0, scale),
                (uint)math.clamp(math.round((q.value.y + 1f) / 2f * scale), 0, scale),
                (uint)math.clamp(math.round((q.value.z + 1f) / 2f * scale), 0, scale),
                (uint)math.clamp(math.round((q.value.w + 1f) / 2f * scale), 0, scale)
            );
        }

        /// <summary>
        /// Decompresses a uint4 back to a quaternion.
        /// </summary>
        /// <param name="c">Compressed quaternion data</param>
        /// <returns>Decompressed quaternion</returns>
        private static quaternion DecompressQuaternion(uint4 c)
        {
            const float scale = 65535f;
            
            return new quaternion(
                (c.x / scale) * 2f - 1f,
                (c.y / scale) * 2f - 1f,
                (c.z / scale) * 2f - 1f,
                (c.w / scale) * 2f - 1f
            );
        }

        #endregion
    }
}
