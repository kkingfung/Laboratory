using System;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Stores replay data for a single actor including transform and animation information.
    /// Supports compression and serialization for efficient storage and transmission.
    /// </summary>
    [Serializable]
    public class ActorReplayData
    {
        #region Fields

        [Header("Actor Identification")]
        [SerializeField] private string _actorName;
        [SerializeField] private string _actorType;
        [SerializeField] private int _actorId;

        [Header("Transform Data")]
        [SerializeField] private Vector3[] _positions;
        [SerializeField] private Quaternion[] _rotations;
        [SerializeField] private Vector3[] _scales;

        [Header("Animation Data")]
        [SerializeField] private float[] _animationTimes;
        [SerializeField] private string[] _animationStates;
        [SerializeField] private float[] _animationWeights;

        [Header("Recording Settings")]
        [SerializeField] private float _recordingInterval = 0.016f; // 60 FPS default
        #pragma warning disable 0414 // Field assigned but never used - planned for future data compression
        [SerializeField] private bool _useCompression = true;
        #pragma warning restore 0414
        [SerializeField] private float _positionPrecision = 0.001f;
        [SerializeField] private float _rotationPrecision = 0.1f;

        // Runtime state
        private int _frameCount = 0;
        private float _totalDuration = 0f;
        private bool _isCompressed = false;

        #endregion

        #region Properties

        /// <summary>
        /// Name identifier for the actor
        /// </summary>
        public string ActorName => _actorName;

        /// <summary>
        /// Type/category of the actor
        /// </summary>
        public string ActorType => _actorType;

        /// <summary>
        /// Unique identifier for the actor
        /// </summary>
        public int ActorId => _actorId;

        /// <summary>
        /// Total number of recorded frames
        /// </summary>
        public int FrameCount => _frameCount;

        /// <summary>
        /// Total duration of the recording in seconds
        /// </summary>
        public float TotalDuration => _totalDuration;

        /// <summary>
        /// Whether the data has been compressed
        /// </summary>
        public bool IsCompressed => _isCompressed;

        /// <summary>
        /// Recording interval between frames
        /// </summary>
        public float RecordingInterval => _recordingInterval;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new ActorReplayData instance
        /// </summary>
        /// <param name="actorName">Name of the actor</param>
        /// <param name="actorType">Type of the actor</param>
        /// <param name="actorId">Unique identifier</param>
        /// <param name="maxFrames">Maximum number of frames to record</param>
        public ActorReplayData(string actorName, string actorType, int actorId, int maxFrames = 3600) // 1 minute at 60 FPS
        {
            _actorName = actorName;
            _actorType = actorType;
            _actorId = actorId;
            
            InitializeArrays(maxFrames);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds a new frame of data to the recording
        /// </summary>
        /// <param name="position">Actor position</param>
        /// <param name="rotation">Actor rotation</param>
        /// <param name="scale">Actor scale</param>
        /// <param name="animationTime">Animation time</param>
        /// <param name="animationState">Animation state name</param>
        /// <param name="animationWeight">Animation weight</param>
        public void AddFrame(Vector3 position, Quaternion rotation, Vector3 scale, 
                           float animationTime, string animationState, float animationWeight)
        {
            if (_frameCount >= _positions.Length)
            {
                Debug.LogWarning($"ActorReplayData: Maximum frame count reached for {_actorName}");
                return;
            }

            _positions[_frameCount] = position;
            _rotations[_frameCount] = rotation;
            _scales[_frameCount] = scale;
            _animationTimes[_frameCount] = animationTime;
            _animationStates[_frameCount] = animationState;
            _animationWeights[_frameCount] = animationWeight;

            _frameCount++;
            _totalDuration = _frameCount * _recordingInterval;
        }

        /// <summary>
        /// Gets transform data for a specific frame
        /// </summary>
        /// <param name="frameIndex">Frame index to retrieve</param>
        /// <param name="position">Output position</param>
        /// <param name="rotation">Output rotation</param>
        /// <param name="scale">Output scale</param>
        /// <returns>True if frame data was retrieved successfully</returns>
        public bool GetTransformData(int frameIndex, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            scale = Vector3.one;

            if (frameIndex < 0 || frameIndex >= _frameCount)
                return false;

            position = _positions[frameIndex];
            rotation = _rotations[frameIndex];
            scale = _scales[frameIndex];

            return true;
        }

        /// <summary>
        /// Gets animation data for a specific frame
        /// </summary>
        /// <param name="frameIndex">Frame index to retrieve</param>
        /// <param name="animationTime">Output animation time</param>
        /// <param name="animationState">Output animation state</param>
        /// <param name="animationWeight">Output animation weight</param>
        /// <returns>True if frame data was retrieved successfully</returns>
        public bool GetAnimationData(int frameIndex, out float animationTime, out string animationState, out float animationWeight)
        {
            animationTime = 0f;
            animationState = string.Empty;
            animationWeight = 0f;

            if (frameIndex < 0 || frameIndex >= _frameCount)
                return false;

            animationTime = _animationTimes[frameIndex];
            animationState = _animationStates[frameIndex];
            animationWeight = _animationWeights[frameIndex];

            return true;
        }

        /// <summary>
        /// Gets the frame index closest to a specific time
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <returns>Frame index, or -1 if time is out of range</returns>
        public int GetFrameIndexAtTime(float time)
        {
            if (time < 0f || time > _totalDuration)
                return -1;

            return Mathf.FloorToInt(time / _recordingInterval);
        }

        /// <summary>
        /// Clears all recorded data
        /// </summary>
        public void Clear()
        {
            _frameCount = 0;
            _totalDuration = 0f;
            _isCompressed = false;
        }

        /// <summary>
        /// Compresses the data to reduce memory usage
        /// </summary>
        public void Compress()
        {
            if (_isCompressed) return;

            // Simple compression: remove redundant frames
            CompressTransformData();
            CompressAnimationData();
            
            _isCompressed = true;
        }

        /// <summary>
        /// Decompresses the data for playback
        /// </summary>
        public void Decompress()
        {
            if (!_isCompressed) return;

            // Decompression logic would go here
            // For now, just mark as decompressed
            _isCompressed = false;
        }

        #endregion

        #region Private Methods

        private void InitializeArrays(int maxFrames)
        {
            _positions = new Vector3[maxFrames];
            _rotations = new Quaternion[maxFrames];
            _scales = new Vector3[maxFrames];
            _animationTimes = new float[maxFrames];
            _animationStates = new string[maxFrames];
            _animationWeights = new float[maxFrames];

            // Initialize with default values
            for (int i = 0; i < maxFrames; i++)
            {
                _scales[i] = Vector3.one;
                _rotations[i] = Quaternion.identity;
                _animationWeights[i] = 1f;
            }
        }

        private void CompressTransformData()
        {
            if (_frameCount <= 2) return;

            var compressedPositions = new Vector3[_frameCount];
            var compressedRotations = new Quaternion[_frameCount];
            var compressedScales = new Vector3[_frameCount];

            int compressedIndex = 0;
            compressedPositions[compressedIndex] = _positions[0];
            compressedRotations[compressedIndex] = _rotations[0];
            compressedScales[compressedIndex] = _scales[0];
            compressedIndex++;

            for (int i = 1; i < _frameCount - 1; i++)
            {
                // Check if this frame is significantly different from the previous
                if (ShouldKeepFrame(i))
                {
                    compressedPositions[compressedIndex] = _positions[i];
                    compressedRotations[compressedIndex] = _rotations[i];
                    compressedScales[compressedIndex] = _scales[i];
                    compressedIndex++;
                }
            }

            // Always keep the last frame
            compressedPositions[compressedIndex] = _positions[_frameCount - 1];
            compressedRotations[compressedIndex] = _rotations[_frameCount - 1];
            compressedScales[compressedIndex] = _scales[_frameCount - 1];
            compressedIndex++;

            // Update arrays with compressed data
            Array.Resize(ref _positions, compressedIndex);
            Array.Resize(ref _rotations, compressedIndex);
            Array.Resize(ref _scales, compressedIndex);
            _frameCount = compressedIndex;
        }

        private void CompressAnimationData()
        {
            if (_frameCount <= 2) return;

            var compressedTimes = new float[_frameCount];
            var compressedStates = new string[_frameCount];
            var compressedWeights = new float[_frameCount];

            int compressedIndex = 0;
            compressedTimes[compressedIndex] = _animationTimes[0];
            compressedStates[compressedIndex] = _animationStates[0];
            compressedWeights[compressedIndex] = _animationWeights[0];
            compressedIndex++;

            for (int i = 1; i < _frameCount - 1; i++)
            {
                // Check if animation state changed
                if (_animationStates[i] != _animationStates[i - 1] || 
                    Mathf.Abs(_animationWeights[i] - _animationWeights[i - 1]) > 0.01f)
                {
                    compressedTimes[compressedIndex] = _animationTimes[i];
                    compressedStates[compressedIndex] = _animationStates[i];
                    compressedWeights[compressedIndex] = _animationWeights[i];
                    compressedIndex++;
                }
            }

            // Always keep the last frame
            compressedTimes[compressedIndex] = _animationTimes[_frameCount - 1];
            compressedStates[compressedIndex] = _animationStates[_frameCount - 1];
            compressedWeights[compressedIndex] = _animationWeights[_frameCount - 1];
            compressedIndex++;

            // Update arrays with compressed data
            Array.Resize(ref _animationTimes, compressedIndex);
            Array.Resize(ref _animationStates, compressedIndex);
            Array.Resize(ref _animationWeights, compressedIndex);
        }

        private bool ShouldKeepFrame(int frameIndex)
        {
            if (frameIndex <= 0 || frameIndex >= _frameCount - 1) return true;

            // Check position change
            Vector3 prevPos = _positions[frameIndex - 1];
            Vector3 currentPos = _positions[frameIndex];
            Vector3 nextPos = _positions[frameIndex + 1];

            float posChange = Vector3.Distance(currentPos, prevPos) + Vector3.Distance(currentPos, nextPos);
            if (posChange > _positionPrecision) return true;

            // Check rotation change
            Quaternion prevRot = _rotations[frameIndex - 1];
            Quaternion currentRot = _rotations[frameIndex];
            Quaternion nextRot = _rotations[frameIndex + 1];

            float rotChange = Quaternion.Angle(currentRot, prevRot) + Quaternion.Angle(currentRot, nextRot);
            if (rotChange > _rotationPrecision) return true;

            return false;
        }

        #endregion
    }
}
