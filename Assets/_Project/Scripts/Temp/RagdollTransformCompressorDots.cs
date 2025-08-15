using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

/// <summary>
/// ECS/DOTS version of RagdollTransformCompressor for networked ragdolls.
/// Compresses LocalTransform data of bones for efficient network transmission.
/// </summary>
public static class RagdollTransformCompressorDots
{
    public struct CompressedData
    {
        public NativeArray<uint3> CompressedPositions; // 16 bits per axis
        public NativeArray<uint4> CompressedRotations; // 16 bits per component
    }

    /// <summary>
    /// Compress all bone transforms into a compact struct array.
    /// </summary>
    public static CompressedData Compress(EntityManager entityManager, NativeArray<Entity> boneEntities, float3 referencePosition, quaternion referenceRotation, Allocator allocator)
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

            float3 deltaPos = transform.Position - referencePosition;
            data.CompressedPositions[i] = CompressVector(deltaPos);

            quaternion deltaRot = transform.Rotation * math.inverse(referenceRotation);
            data.CompressedRotations[i] = CompressQuaternion(deltaRot);
        }

        return data;
    }

    /// <summary>
    /// Decompress compressed data and apply back to bone entities.
    /// </summary>
    public static void Decompress(EntityManager entityManager, NativeArray<Entity> boneEntities, CompressedData data, float3 referencePosition, quaternion referenceRotation)
    {
        int count = boneEntities.Length;
        for (int i = 0; i < count; i++)
        {
            float3 pos = DecompressVector(data.CompressedPositions[i]) + referencePosition;
            quaternion rot = DecompressQuaternion(data.CompressedRotations[i]) * referenceRotation;

            if (entityManager.HasComponent<LocalTransform>(boneEntities[i]))
            {
                var transform = entityManager.GetComponentData<LocalTransform>(boneEntities[i]);
                transform.Position = pos;
                transform.Rotation = rot;
                entityManager.SetComponentData(boneEntities[i], transform);
            }
        }
    }

    #region Compression Helpers

    private static uint3 CompressVector(float3 v)
    {
        float max = 10f; // adjust to expected delta range
        uint scale = 65535; // 16 bits per axis
        return new uint3(
            (uint)math.clamp(math.round((v.x + max) / (2 * max) * scale), 0, scale),
            (uint)math.clamp(math.round((v.y + max) / (2 * max) * scale), 0, scale),
            (uint)math.clamp(math.round((v.z + max) / (2 * max) * scale), 0, scale)
        );
    }

    private static float3 DecompressVector(uint3 c)
    {
        float max = 10f;
        float scale = 65535f;
        return new float3(
            (c.x / scale) * 2f * max - max,
            (c.y / scale) * 2f * max - max,
            (c.z / scale) * 2f * max - max
        );
    }

    private static uint4 CompressQuaternion(quaternion q)
    {
        uint scale = 65535;
        return new uint4(
            (uint)math.clamp(math.round((q.value.x + 1f) / 2f * scale), 0, scale),
            (uint)math.clamp(math.round((q.value.y + 1f) / 2f * scale), 0, scale),
            (uint)math.clamp(math.round((q.value.z + 1f) / 2f * scale), 0, scale),
            (uint)math.clamp(math.round((q.value.w + 1f) / 2f * scale), 0, scale)
        );
    }

    private static quaternion DecompressQuaternion(uint4 c)
    {
        float scale = 65535f;
        return new quaternion(
            (c.x / scale) * 2f - 1f,
            (c.y / scale) * 2f - 1f,
            (c.z / scale) * 2f - 1f,
            (c.w / scale) * 2f - 1f
        );
    }

    #endregion
}
