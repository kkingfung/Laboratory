using UnityEngine;
using System.Collections.Generic;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Records actor transform and animation data for replay functionality.
    /// Provides efficient data collection and compression for performance-critical recording.
    /// </summary>
    public class ActorRecorder : MonoBehaviour
    {
        #region Fields

        [Header("Recording Settings")]
        [SerializeField] private float _recordingInterval = 0.016f; // 60 FPS default
        [SerializeField] private int _maxFrames = 3600; // 1 minute at 60 FPS
        [SerializeField] private bool _autoRecord = false;
        [SerializeField] private bool _useCompression = true;

        [Header("Data Collection")]
        [SerializeField] private bool _recordPosition = true;
        [SerializeField] private bool _recordRotation = true;
        [SerializeField] private bool _recordScale = true;
        [SerializeField] private bool _recordAnimations = true;
        [SerializeField] private bool _recordPhysics = false;

        [Header("Compression Settings")]
        [SerializeField] private float _positionPrecision = 0.001f;
        [SerializeField] private float _rotationPrecision = 0.1f;
        [SerializeField] private float _scalePrecision = 0.001f;
        [SerializeField] private bool _adaptivePrecision = true;

        [Header("Animation Recording")]
        [SerializeField] private Animator _targetAnimator;
        [SerializeField] private bool _recordAnimationStates = true;
        [SerializeField] private bool _recordAnimationTimes = true;
        [SerializeField] private bool _recordAnimationWeights = true;

        // Runtime state
        private bool _isRecording = false;
        private float _recordingTimer = 0f;
        private int _frameCount = 0;
        private ActorReplayData _replayData;
        private List<Vector3> _positions = new List<Vector3>();
        private List<Quaternion> _rotations = new List<Quaternion>();
        private List<Vector3> _scales = new List<Vector3>();
        private List<float> _animationTimes = new List<float>();
        private List<string> _animationStates = new List<string>();
        private List<float> _animationWeights = new List<float>();

        #endregion

        #region Properties

        /// <summary>
        /// Whether recording is currently active
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// Current number of recorded frames
        /// </summary>
        public int FrameCount => _frameCount;

        /// <summary>
        /// Total recording duration in seconds
        /// </summary>
        public float RecordingDuration => _frameCount * _recordingInterval;

        /// <summary>
        /// Maximum number of frames that can be recorded
        /// </summary>
        public int MaxFrames => _maxFrames;

        /// <summary>
        /// Recording interval between frames
        /// </summary>
        public float RecordingInterval => _recordingInterval;

        /// <summary>
        /// Whether compression is enabled
        /// </summary>
        public bool UseCompression => _useCompression;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeRecorder();
        }

        private void Start()
        {
            if (_autoRecord)
            {
                StartRecording();
            }
        }

        private void Update()
        {
            if (_isRecording)
            {
                UpdateRecording();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Starts recording actor data
        /// </summary>
        public void StartRecording()
        {
            if (_isRecording) return;

            _isRecording = true;
            _recordingTimer = 0f;
            _frameCount = 0;

            // Initialize recording data
            _positions.Clear();
            _rotations.Clear();
            _scales.Clear();
            _animationTimes.Clear();
            _animationStates.Clear();
            _animationWeights.Clear();

            // Record initial frame
            RecordFrame();

            Debug.Log($"ActorRecorder: Started recording for {gameObject.name}");
        }

        /// <summary>
        /// Stops recording and finalizes the replay data
        /// </summary>
        /// <returns>Completed ActorReplayData or null if recording failed</returns>
        public ActorReplayData StopRecording()
        {
            if (!_isRecording) return null;

            _isRecording = false;

            // Create replay data from recorded frames
            _replayData = CreateReplayData();

            if (_useCompression)
            {
                _replayData.Compress();
            }

            Debug.Log($"ActorRecorder: Stopped recording for {gameObject.name}. Recorded {_frameCount} frames.");

            return _replayData;
        }

        /// <summary>
        /// Pauses recording without losing data
        /// </summary>
        public void PauseRecording()
        {
            _isRecording = false;
            Debug.Log($"ActorRecorder: Paused recording for {gameObject.name}");
        }

        /// <summary>
        /// Resumes recording from where it was paused
        /// </summary>
        public void ResumeRecording()
        {
            if (_frameCount > 0)
            {
                _isRecording = true;
                Debug.Log($"ActorRecorder: Resumed recording for {gameObject.name}");
            }
        }

        /// <summary>
        /// Clears all recorded data
        /// </summary>
        public void ClearRecording()
        {
            _isRecording = false;
            _frameCount = 0;
            _recordingTimer = 0f;
            _replayData = null;

            _positions.Clear();
            _rotations.Clear();
            _scales.Clear();
            _animationTimes.Clear();
            _animationStates.Clear();
            _animationWeights.Clear();

            Debug.Log($"ActorRecorder: Cleared recording for {gameObject.name}");
        }

        /// <summary>
        /// Gets the current replay data
        /// </summary>
        /// <returns>Current ActorReplayData or null if not available</returns>
        public ActorReplayData GetReplayData()
        {
            return _replayData;
        }

        /// <summary>
        /// Sets the recording interval
        /// </summary>
        /// <param name="interval">Time between frames in seconds</param>
        public void SetRecordingInterval(float interval)
        {
            _recordingInterval = Mathf.Max(0.001f, interval);
        }

        /// <summary>
        /// Sets the maximum number of frames
        /// </summary>
        /// <param name="maxFrames">Maximum frames to record</param>
        public void SetMaxFrames(int maxFrames)
        {
            _maxFrames = Mathf.Max(1, maxFrames);
        }

        /// <summary>
        /// Sets the position precision for compression
        /// </summary>
        /// <param name="precision">Position precision value</param>
        public void SetPositionPrecision(float precision)
        {
            _positionPrecision = Mathf.Max(0.0001f, precision);
        }

        /// <summary>
        /// Sets the rotation precision for compression
        /// </summary>
        /// <param name="precision">Rotation precision value</param>
        public void SetRotationPrecision(float precision)
        {
            _rotationPrecision = Mathf.Max(0.01f, precision);
        }

        /// <summary>
        /// Sets the scale precision for compression
        /// </summary>
        /// <param name="precision">Scale precision value</param>
        public void SetScalePrecision(float precision)
        {
            _scalePrecision = Mathf.Max(0.0001f, precision);
        }

        /// <summary>
        /// Enables or disables compression
        /// </summary>
        /// <param name="enabled">Whether compression should be enabled</param>
        public void SetCompression(bool enabled)
        {
            _useCompression = enabled;
        }

        /// <summary>
        /// Enables or disables adaptive precision
        /// </summary>
        /// <param name="enabled">Whether adaptive precision should be enabled</param>
        public void SetAdaptivePrecision(bool enabled)
        {
            _adaptivePrecision = enabled;
        }

        #endregion

        #region Private Methods

        private void InitializeRecorder()
        {
            if (_targetAnimator == null)
                _targetAnimator = GetComponent<Animator>();

            // Pre-allocate lists for better performance
            _positions.Capacity = _maxFrames;
            _rotations.Capacity = _maxFrames;
            _scales.Capacity = _maxFrames;
            _animationTimes.Capacity = _maxFrames;
            _animationStates.Capacity = _maxFrames;
            _animationWeights.Capacity = _maxFrames;
        }

        private void UpdateRecording()
        {
            _recordingTimer += Time.deltaTime;

            if (_recordingTimer >= _recordingInterval)
            {
                RecordFrame();
                _recordingTimer = 0f;
            }

            // Check if we've reached the maximum frame count
            if (_frameCount >= _maxFrames)
            {
                Debug.LogWarning($"ActorRecorder: Maximum frame count reached for {gameObject.name}. Stopping recording.");
                StopRecording();
            }
        }

        private void RecordFrame()
        {
            if (_frameCount >= _maxFrames) return;

            // Record transform data
            if (_recordPosition)
            {
                _positions.Add(transform.position);
            }

            if (_recordRotation)
            {
                _rotations.Add(transform.rotation);
            }

            if (_recordScale)
            {
                _scales.Add(transform.localScale);
            }

            // Record animation data
            if (_recordAnimations && _targetAnimator != null)
            {
                if (_recordAnimationTimes)
                {
                    _animationTimes.Add(_targetAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime);
                }

                if (_recordAnimationStates)
                {
                    var currentState = _targetAnimator.GetCurrentAnimatorStateInfo(0);
                    _animationStates.Add(currentState.IsName("") ? "Idle" : currentState.ToString());
                }

                if (_recordAnimationWeights)
                {
                    _animationWeights.Add(1f); // Default weight, could be enhanced
                }
            }

            _frameCount++;
        }

        private ActorReplayData CreateReplayData()
        {
            var replayData = new ActorReplayData(gameObject.name, gameObject.tag, GetInstanceID(), _frameCount);

            // Transfer recorded data to replay data
            for (int i = 0; i < _frameCount; i++)
            {
                Vector3 position = _recordPosition && i < _positions.Count ? _positions[i] : transform.position;
                Quaternion rotation = _recordRotation && i < _rotations.Count ? _rotations[i] : transform.rotation;
                Vector3 scale = _recordScale && i < _scales.Count ? _scales[i] : transform.localScale;

                float animTime = _recordAnimations && i < _animationTimes.Count ? _animationTimes[i] : 0f;
                string animState = _recordAnimations && i < _animationStates.Count ? _animationStates[i] : "Idle";
                float animWeight = _recordAnimations && i < _animationWeights.Count ? _animationWeights[i] : 1f;

                replayData.AddFrame(position, rotation, scale, animTime, animState, animWeight);
            }

            return replayData;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_isRecording)
            {
                // Draw recording indicator
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(transform.position, 0.5f);

                // Draw recording path
                if (_positions.Count > 1)
                {
                    Gizmos.color = Color.yellow;
                    for (int i = 1; i < _positions.Count; i++)
                    {
                        Gizmos.DrawLine(_positions[i - 1], _positions[i]);
                    }
                }
            }
        }

        #endregion
    }
}
