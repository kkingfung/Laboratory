using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Compresses and decompresses ragdoll bone transforms for efficient network transmission.
    /// Provides delta compression against reference positions and rotations to minimize data size.
    /// </summary>
    /// <remarks>
    /// This component is designed to work with NetworkRagdollSync to send transform data
    /// in a compact format over the network, reducing bandwidth requirements.
    /// </remarks>
    public class RagdollTransformCompressor : MonoBehaviour
    {
        #region Fields

        [Header("Ragdoll Configuration")]
        [Tooltip("All ragdoll bones that need compression - assign manually")]
        [SerializeField] private List<Transform> ragdollBones;

        [Header("Compression Settings")]
        [Tooltip("Number of bits used per position axis (higher = more precision)")]
        [SerializeField] [Range(8, 24)] private int positionBits = 16;

        [Tooltip("Number of bits used per rotation axis (higher = more precision)")]
        [SerializeField] [Range(8, 24)] private int rotationBits = 16;

        [Tooltip("Reference position used for delta compression")]
        [SerializeField] private Vector3 referencePosition = Vector3.zero;

        [Tooltip("Reference rotation used for delta compression")]
        [SerializeField] private Quaternion referenceRotation = Quaternion.identity;

        #endregion

        #region Public Methods

        /// <summary>
        /// Compresses all bone transforms into a compact data structure.
        /// Uses delta compression against reference transforms to minimize data size.
        /// </summary>
        /// <returns>Compressed ragdoll data ready for network transmission</returns>
        public RagdollCompressedData Compress()
        {
            RagdollCompressedData data = new RagdollCompressedData(ragdollBones.Count);

            for (int i = 0; i < ragdollBones.Count; i++)
            {
                Transform bone = ragdollBones[i];

                // Calculate delta from reference
                Vector3 deltaPos = bone.localPosition - referencePosition;
                data.CompressedPositions[i] = CompressVector(deltaPos, positionBits);

                // Calculate delta rotation from reference
                Quaternion deltaRot = bone.localRotation * Quaternion.Inverse(referenceRotation);
                data.CompressedRotations[i] = CompressQuaternion(deltaRot, rotationBits);
            }

            return data;
        }

        /// <summary>
        /// Decompresses bone transform data and applies it to the ragdoll bones.
        /// Reconstructs original transforms by adding deltas back to reference transforms.
        /// </summary>
        /// <param name="data">Compressed ragdoll data to decompress</param>
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

        #endregion

        #region Private Methods - Compression Helpers

        /// <summary>
        /// Compresses a Vector3 to fit within the specified bit range.
        /// </summary>
        /// <param name="v">Vector3 to compress</param>
        /// <param name="bits">Number of bits per axis</param>
        /// <returns>Compressed vector with quantized components</returns>
        private Vector3 CompressVector(Vector3 v, int bits)
        {
            float max = 10f; // Maximum expected delta in units; adjust based on your scale
            float scale = (1 << bits) - 1;
            
            return new Vector3(
                Mathf.RoundToInt(Mathf.Clamp((v.x + max) / (2 * max), 0f, 1f) * scale),
                Mathf.RoundToInt(Mathf.Clamp((v.y + max) / (2 * max), 0f, 1f) * scale),
                Mathf.RoundToInt(Mathf.Clamp((v.z + max) / (2 * max), 0f, 1f) * scale)
            );
        }

        /// <summary>
        /// Decompresses a Vector3 from quantized components.
        /// </summary>
        /// <param name="compressed">Compressed vector data</param>
        /// <param name="bits">Number of bits per axis used during compression</param>
        /// <returns>Decompressed Vector3</returns>
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

        /// <summary>
        /// Compresses a Quaternion to 4 quantized components.
        /// </summary>
        /// <param name="q">Quaternion to compress</param>
        /// <param name="bits">Number of bits per component</param>
        /// <returns>Compressed quaternion as Vector4</returns>
        private Vector4 CompressQuaternion(Quaternion q, int bits)
        {
            float scale = (1 << bits) - 1;
            
            return new Vector4(
                Mathf.RoundToInt((q.x + 1f) / 2f * scale),
                Mathf.RoundToInt((q.y + 1f) / 2f * scale),
                Mathf.RoundToInt((q.z + 1f) / 2f * scale),
                Mathf.RoundToInt((q.w + 1f) / 2f * scale)
            );
        }

        /// <summary>
        /// Decompresses a Quaternion from quantized components.
        /// </summary>
        /// <param name="c">Compressed quaternion data</param>
        /// <param name="bits">Number of bits per component used during compression</param>
        /// <returns>Decompressed Quaternion</returns>
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
    /// Data structure for storing compressed ragdoll bone transform information.
    /// Contains arrays of compressed positions and rotations for efficient network transmission.
    /// </summary>
    [System.Serializable]
    public class RagdollCompressedData
    {
        #region Fields

        /// <summary>
        /// Array of compressed bone positions
        /// </summary>
        public Vector3[] CompressedPositions;

        /// <summary>
        /// Array of compressed bone rotations
        /// </summary>
        public Vector4[] CompressedRotations;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of RagdollCompressedData with the specified bone count.
        /// </summary>
        /// <param name="boneCount">Number of bones in the ragdoll</param>
        public RagdollCompressedData(int boneCount)
        {
            CompressedPositions = new Vector3[boneCount];
            CompressedRotations = new Vector4[boneCount];
        }

        #endregion
    }
}
