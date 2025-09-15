using UnityEngine;
using System.Collections;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Handles playback of recorded actor data during replay sessions.
    /// Provides smooth interpolation and synchronization for replay visualization.
    /// </summary>
    public class ActorPlayback : MonoBehaviour
    {
        #region Fields

        [Header("Playback Settings")]
        [SerializeField] private float _playbackSpeed = 1f;
        [SerializeField] private bool _loopPlayback = false;
        [SerializeField] private bool _autoPlay = false;
        [SerializeField] private float _interpolationSmoothing = 5f;

        [Header("Transform Interpolation")]
        [SerializeField] private bool _interpolatePosition = true;
        [SerializeField] private bool _interpolateRotation = true;
        [SerializeField] private bool _interpolateScale = true;
        [SerializeField] private float _positionThreshold = 0.001f;
        [SerializeField] private float _rotationThreshold = 0.1f;

        [Header("Animation Playback")]
        [SerializeField] private bool _playAnimations = true;
        [SerializeField] private bool _syncAnimationTime = true;
        [SerializeField] private Animator _targetAnimator;

        // Runtime state
        private ActorReplayData _replayData;
        private int _currentFrame = 0;
        private float _currentTime = 0f;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private Vector3 _targetScale;

        #endregion

        #region Properties

        /// <summary>
        /// Current replay data being played
        /// </summary>
        public ActorReplayData ReplayData => _replayData;

        /// <summary>
        /// Current frame index during playback
        /// </summary>
        public int CurrentFrame => _currentFrame;

        /// <summary>
        /// Current playback time in seconds
        /// </summary>
        public float CurrentTime => _currentTime;

        /// <summary>
        /// Whether playback is currently active
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// Whether playback is currently paused
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// Total duration of the replay data
        /// </summary>
        public float TotalDuration => _replayData?.TotalDuration ?? 0f;

        /// <summary>
        /// Total number of frames in the replay data
        /// </summary>
        public int TotalFrames => _replayData?.FrameCount ?? 0;

        /// <summary>
        /// Playback speed multiplier
        /// </summary>
        public float PlaybackSpeed => _playbackSpeed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializePlayback();
        }

        private void Start()
        {
            if (_autoPlay)
            {
                StartPlayback();
            }
        }

        private void Update()
        {
            if (_isPlaying && !_isPaused)
            {
                UpdatePlayback();
            }

            if (_interpolatePosition || _interpolateRotation || _interpolateScale)
            {
                UpdateInterpolation();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the replay data to play
        /// </summary>
        /// <param name="data">Actor replay data to play</param>
        public void SetReplayData(ActorReplayData data)
        {
            _replayData = data;
            ResetPlayback();
        }

        /// <summary>
        /// Starts playback from the current position
        /// </summary>
        public void StartPlayback()
        {
            if (_replayData == null)
            {
                Debug.LogWarning("ActorPlayback: No replay data set");
                return;
            }

            _isPlaying = true;
            _isPaused = false;
        }

        /// <summary>
        /// Pauses playback at the current position
        /// </summary>
        public void PausePlayback()
        {
            _isPaused = true;
        }

        /// <summary>
        /// Resumes playback from the paused position
        /// </summary>
        public void ResumePlayback()
        {
            _isPaused = false;
        }

        /// <summary>
        /// Stops playback and resets to the beginning
        /// </summary>
        public void StopPlayback()
        {
            _isPlaying = false;
            _isPaused = false;
            ResetPlayback();
        }

        /// <summary>
        /// Sets the playback speed
        /// </summary>
        /// <param name="speed">Speed multiplier (1.0 = normal speed)</param>
        public void SetPlaybackSpeed(float speed)
        {
            _playbackSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// Sets the current playback time
        /// </summary>
        /// <param name="time">Time in seconds</param>
        public void SetPlaybackTime(float time)
        {
            if (_replayData == null) return;

            _currentTime = Mathf.Clamp(time, 0f, _replayData.TotalDuration);
            _currentFrame = _replayData.GetFrameIndexAtTime(_currentTime);
            
            if (_currentFrame >= 0)
            {
                ApplyFrameData(_currentFrame);
            }
        }

        /// <summary>
        /// Sets the current frame index
        /// </summary>
        /// <param name="frame">Frame index</param>
        public void SetCurrentFrame(int frame)
        {
            if (_replayData == null) return;

            _currentFrame = Mathf.Clamp(frame, 0, _replayData.FrameCount - 1);
            _currentTime = _currentFrame * (_replayData.TotalDuration / _replayData.FrameCount);
            
            ApplyFrameData(_currentFrame);
        }

        /// <summary>
        /// Jumps to a specific time in the replay
        /// </summary>
        /// <param name="time">Time in seconds</param>
        public void JumpToTime(float time)
        {
            SetPlaybackTime(time);
        }

        /// <summary>
        /// Jumps to a specific frame in the replay
        /// </summary>
        /// <param name="frame">Frame index</param>
        public void JumpToFrame(int frame)
        {
            SetCurrentFrame(frame);
        }

        /// <summary>
        /// Enables or disables looping
        /// </summary>
        /// <param name="enabled">Whether looping should be enabled</param>
        public void SetLooping(bool enabled)
        {
            _loopPlayback = enabled;
        }

        /// <summary>
        /// Enables or disables position interpolation
        /// </summary>
        /// <param name="enabled">Whether position interpolation should be enabled</param>
        public void SetPositionInterpolation(bool enabled)
        {
            _interpolatePosition = enabled;
        }

        /// <summary>
        /// Enables or disables rotation interpolation
        /// </summary>
        /// <param name="enabled">Whether rotation interpolation should be enabled</param>
        public void SetRotationInterpolation(bool enabled)
        {
            _interpolateRotation = enabled;
        }

        /// <summary>
        /// Enables or disables scale interpolation
        /// </summary>
        /// <param name="enabled">Whether scale interpolation should be enabled</param>
        public void SetScaleInterpolation(bool enabled)
        {
            _interpolateScale = enabled;
        }

        /// <summary>
        /// Sets the interpolation smoothing factor
        /// </summary>
        /// <param name="smoothing">Smoothing factor (higher = smoother)</param>
        public void SetInterpolationSmoothing(float smoothing)
        {
            _interpolationSmoothing = Mathf.Max(0.1f, smoothing);
        }

        #endregion

        #region Private Methods

        private void InitializePlayback()
        {
            if (_targetAnimator == null)
                _targetAnimator = GetComponent<Animator>();

            ResetPlayback();
        }

        private void ResetPlayback()
        {
            _currentFrame = 0;
            _currentTime = 0f;
            _isPlaying = false;
            _isPaused = false;

            if (_replayData != null && _replayData.FrameCount > 0)
            {
                ApplyFrameData(0);
            }
        }

        private void UpdatePlayback()
        {
            if (_replayData == null) return;

            _currentTime += Time.deltaTime * _playbackSpeed;

            if (_currentTime >= _replayData.TotalDuration)
            {
                if (_loopPlayback)
                {
                    _currentTime = 0f;
                    _currentFrame = 0;
                }
                else
                {
                    StopPlayback();
                    return;
                }
            }

            // Update frame index
            int newFrame = _replayData.GetFrameIndexAtTime(_currentTime);
            if (newFrame != _currentFrame && newFrame >= 0)
            {
                _currentFrame = newFrame;
                ApplyFrameData(_currentFrame);
            }
        }

        private void ApplyFrameData(int frameIndex)
        {
            if (_replayData == null) return;

            // Get transform data
            if (_replayData.GetTransformData(frameIndex, out Vector3 position, out Quaternion rotation, out Vector3 scale))
            {
                if (_interpolatePosition)
                {
                    _targetPosition = position;
                }
                else
                {
                    transform.position = position;
                }

                if (_interpolateRotation)
                {
                    _targetRotation = rotation;
                }
                else
                {
                    transform.rotation = rotation;
                }

                if (_interpolateScale)
                {
                    _targetScale = scale;
                }
                else
                {
                    transform.localScale = scale;
                }
            }

            // Get animation data
            if (_playAnimations && _targetAnimator != null)
            {
                if (_replayData.GetAnimationData(frameIndex, out float animTime, out string animState, out float animWeight))
                {
                    if (_syncAnimationTime)
                    {
                        _targetAnimator.Play(animState, 0, animTime);
                    }
                    else
                    {
                        _targetAnimator.Play(animState);
                    }
                }
            }
        }

        private void UpdateInterpolation()
        {
            float smoothing = Time.deltaTime * _interpolationSmoothing;

            if (_interpolatePosition)
            {
                if (Vector3.Distance(transform.position, _targetPosition) > _positionThreshold)
                {
                    transform.position = Vector3.Lerp(transform.position, _targetPosition, smoothing);
                }
            }

            if (_interpolateRotation)
            {
                if (Quaternion.Angle(transform.rotation, _targetRotation) > _rotationThreshold)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, smoothing);
                }
            }

            if (_interpolateScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, smoothing);
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_replayData != null && _isPlaying)
            {
                // Draw current playback position
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(transform.position, 0.5f);

                // Draw target position if interpolating
                if (_interpolatePosition)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(_targetPosition, 0.3f);
                    Gizmos.DrawLine(transform.position, _targetPosition);
                }
            }
        }

        #endregion
    }
}
