using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Compresses and decompresses ragdoll bone transforms for efficient network transmission.
/// Use with NetworkRagdollSync to send transforms in compact form.
/// </summary>
public class RagdollTransformCompressor : MonoBehaviour
{
    [Header("Ragdoll Bones")]
    [Tooltip("Assign all ragdoll bones manually.")]
    [SerializeField] private List<Transform> ragdollBones;

    [Header("Compression Settings")]
    [Tooltip("Number of bits per position axis.")]
    [SerializeField] private int positionBits = 16;

    [Tooltip("Number of bits per rotation axis.")]
    [SerializeField] private int rotationBits = 16;

    [Tooltip("Reference position used for delta compression.")]
    [SerializeField] private Vector3 referencePosition = Vector3.zero;

    [Tooltip("Reference rotation used for delta compression.")]
    [SerializeField] private Quaternion referenceRotation = Quaternion.identity;

    /// <summary>
    /// Compress all bone transforms into compact struct arrays.
    /// </summary>
    public RagdollCompressedData Compress()
    {
        RagdollCompressedData data = new RagdollCompressedData(ragdollBones.Count);

        for (int i = 0; i < ragdollBones.Count; i++)
        {
            Transform bone = ragdollBones[i];

            // Delta position
            Vector3 deltaPos = bone.localPosition - referencePosition;
            data.CompressedPositions[i] = CompressVector(deltaPos, positionBits);

            // Delta rotation
            Quaternion deltaRot = bone.localRotation * Quaternion.Inverse(referenceRotation);
            data.CompressedRotations[i] = CompressQuaternion(deltaRot, rotationBits);
        }

        return data;
    }

    /// <summary>
    /// Decompress bone transforms and apply to the bones.
    /// </summary>
    public void Decompress(RagdollCompressedData data)
    {
        for (int i = 0; i < ragdollBones.Count; i++)
        {
            Vector3 deltaPos = DecompressVector(data.CompressedPositions[i], positionBits);
            ragdollBones[i].localPosition = deltaPos + referencePosition;

            Quaternion deltaRot = DecompressQuaternion(data.CompressedRotations[i], rotationBits);
            ragdollBones[i].localRotation = deltaRot * referenceRotation;
        }
    }

    #region Compression Helpers

    private Vector3 CompressVector(Vector3 v, int bits)
    {
        float max = 10f; // max expected delta in units; adjust to your scale
        float scale = (1 << bits) - 1;
        return new Vector3(
            Mathf.RoundToInt(Mathf.Clamp((v.x + max) / (2 * max), 0f, 1f) * scale),
            Mathf.RoundToInt(Mathf.Clamp((v.y + max) / (2 * max), 0f, 1f) * scale),
            Mathf.RoundToInt(Mathf.Clamp((v.z + max) / (2 * max), 0f, 1f) * scale)
        );
    }

    private Vector3 DecompressVector(Vector3 compressed, int bits)
    {
        float max = 10f;
        float scale = (1 << bits) - 1;
        return new Vector3(
            (compressed.x / scale) * (2 * max) - max,
            (compressed.y / scale) * (2 * max) - max,
            (compressed.z / scale) * (2 * max) - max
        );
    }

    private Vector4 CompressQuaternion(Quaternion q, int bits)
    {
        // Compress quaternion to 4 floats scaled to [0, 1]
        float scale = (1 << bits) - 1;
        return new Vector4(
            Mathf.RoundToInt((q.x + 1f) / 2f * scale),
            Mathf.RoundToInt((q.y + 1f) / 2f * scale),
            Mathf.RoundToInt((q.z + 1f) / 2f * scale),
            Mathf.RoundToInt((q.w + 1f) / 2f * scale)
        );
    }

    private Quaternion DecompressQuaternion(Vector4 c, int bits)
    {
        float scale = (1 << bits) - 1;
        return new Quaternion(
            (c.x / scale) * 2f - 1f,
            (c.y / scale) * 2f - 1f,
            (c.z / scale) * 2f - 1f,
            (c.w / scale) * 2f - 1f
        );
    }

    #endregion
}

/// <summary>
/// Stores compressed ragdoll bone data.
/// </summary>
public class RagdollCompressedData
{
    public Vector3[] CompressedPositions;
    public Vector4[] CompressedRotations;

    public RagdollCompressedData(int boneCount)
    {
        CompressedPositions = new Vector3[boneCount];
        CompressedRotations = new Vector4[boneCount];
    }
}
