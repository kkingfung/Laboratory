using System;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Represents a single frame of recorded data for replay functionality.
    /// Contains transform and animation information for a specific point in time.
    /// </summary>
    [System.Serializable]
    public class FrameData
    {
        #region Fields

        [Header("Transform Data")]
        [SerializeField] private Vector3 _position;
        [SerializeField] private Quaternion _rotation;
        [SerializeField] private Vector3 _scale;

        [Header("Animation Data")]
        [SerializeField] private float _animationTime;
        [SerializeField] private string _animationState;
        [SerializeField] private float _animationWeight;

        [Header("Physics Data")]
        [SerializeField] private Vector3 _velocity;
        [SerializeField] private Vector3 _angularVelocity;
        [SerializeField] private bool _hasPhysicsData;

        [Header("Metadata")]
        [SerializeField] private float _timestamp;
        [SerializeField] private int _frameIndex;
        [SerializeField] private bool _isKeyFrame;

        #endregion

        #region Properties

        /// <summary>
        /// Position of the actor at this frame
        /// </summary>
        public Vector3 Position => _position;

        /// <summary>
        /// Rotation of the actor at this frame
        /// </summary>
        public Quaternion Rotation => _rotation;

        /// <summary>
        /// Scale of the actor at this frame
        /// </summary>
        public Vector3 Scale => _scale;

        /// <summary>
        /// Animation time at this frame
        /// </summary>
        public float AnimationTime => _animationTime;

        /// <summary>
        /// Animation state name at this frame
        /// </summary>
        public string AnimationState => _animationState;

        /// <summary>
        /// Animation weight at this frame
        /// </summary>
        public float AnimationWeight => _animationWeight;

        /// <summary>
        /// Linear velocity at this frame
        /// </summary>
        public Vector3 Velocity => _velocity;

        /// <summary>
        /// Angular velocity at this frame
        /// </summary>
        public Vector3 AngularVelocity => _angularVelocity;

        /// <summary>
        /// Whether this frame contains physics data
        /// </summary>
        public bool HasPhysicsData => _hasPhysicsData;

        /// <summary>
        /// Timestamp of this frame in seconds
        /// </summary>
        public float Timestamp => _timestamp;

        /// <summary>
        /// Frame index in the sequence
        /// </summary>
        public int FrameIndex => _frameIndex;

        /// <summary>
        /// Whether this is a key frame for compression
        /// </summary>
        public bool IsKeyFrame => _isKeyFrame;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new FrameData instance with transform data
        /// </summary>
        /// <param name="position">Actor position</param>
        /// <param name="rotation">Actor rotation</param>
        /// <param name="scale">Actor scale</param>
        /// <param name="timestamp">Frame timestamp</param>
        /// <param name="frameIndex">Frame index</param>
        public FrameData(Vector3 position, Quaternion rotation, Vector3 scale, float timestamp, int frameIndex)
        {
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _timestamp = timestamp;
            _frameIndex = frameIndex;
            _isKeyFrame = false;
            _hasPhysicsData = false;
        }

        /// <summary>
        /// Creates a new FrameData instance with transform and animation data
        /// </summary>
        /// <param name="position">Actor position</param>
        /// <param name="rotation">Actor rotation</param>
        /// <param name="scale">Actor scale</param>
        /// <param name="animationTime">Animation time</param>
        /// <param name="animationState">Animation state</param>
        /// <param name="animationWeight">Animation weight</param>
        /// <param name="timestamp">Frame timestamp</param>
        /// <param name="frameIndex">Frame index</param>
        public FrameData(Vector3 position, Quaternion rotation, Vector3 scale, 
                        float animationTime, string animationState, float animationWeight,
                        float timestamp, int frameIndex)
        {
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _animationTime = animationTime;
            _animationState = animationState;
            _animationWeight = animationWeight;
            _timestamp = timestamp;
            _frameIndex = frameIndex;
            _isKeyFrame = false;
            _hasPhysicsData = false;
        }

        /// <summary>
        /// Creates a new FrameData instance with all data types
        /// </summary>
        /// <param name="position">Actor position</param>
        /// <param name="rotation">Actor rotation</param>
        /// <param name="scale">Actor scale</param>
        /// <param name="animationTime">Animation time</param>
        /// <param name="animationState">Animation state</param>
        /// <param name="animationWeight">Animation weight</param>
        /// <param name="velocity">Linear velocity</param>
        /// <param name="angularVelocity">Angular velocity</param>
        /// <param name="timestamp">Frame timestamp</param>
        /// <param name="frameIndex">Frame index</param>
        public FrameData(Vector3 position, Quaternion rotation, Vector3 scale,
                        float animationTime, string animationState, float animationWeight,
                        Vector3 velocity, Vector3 angularVelocity,
                        float timestamp, int frameIndex)
        {
            _position = position;
            _rotation = rotation;
            _scale = scale;
            _animationTime = animationTime;
            _animationState = animationState;
            _animationWeight = animationWeight;
            _velocity = velocity;
            _angularVelocity = angularVelocity;
            _timestamp = timestamp;
            _frameIndex = frameIndex;
            _isKeyFrame = false;
            _hasPhysicsData = true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the position data
        /// </summary>
        /// <param name="position">New position</param>
        public void SetPosition(Vector3 position)
        {
            _position = position;
        }

        /// <summary>
        /// Sets the rotation data
        /// </summary>
        /// <param name="rotation">New rotation</param>
        public void SetRotation(Quaternion rotation)
        {
            _rotation = rotation;
        }

        /// <summary>
        /// Sets the scale data
        /// </summary>
        /// <param name="scale">New scale</param>
        public void SetScale(Vector3 scale)
        {
            _scale = scale;
        }

        /// <summary>
        /// Sets the animation data
        /// </summary>
        /// <param name="animationTime">Animation time</param>
        /// <param name="animationState">Animation state</param>
        /// <param name="animationWeight">Animation weight</param>
        public void SetAnimationData(float animationTime, string animationState, float animationWeight)
        {
            _animationTime = animationTime;
            _animationState = animationState;
            _animationWeight = animationWeight;
        }

        /// <summary>
        /// Sets the physics data
        /// </summary>
        /// <param name="velocity">Linear velocity</param>
        /// <param name="angularVelocity">Angular velocity</param>
        public void SetPhysicsData(Vector3 velocity, Vector3 angularVelocity)
        {
            _velocity = velocity;
            _angularVelocity = angularVelocity;
            _hasPhysicsData = true;
        }

        /// <summary>
        /// Sets the timestamp
        /// </summary>
        /// <param name="timestamp">Frame timestamp</param>
        public void SetTimestamp(float timestamp)
        {
            _timestamp = timestamp;
        }

        /// <summary>
        /// Sets the frame index
        /// </summary>
        /// <param name="frameIndex">Frame index</param>
        public void SetFrameIndex(int frameIndex)
        {
            _frameIndex = frameIndex;
        }

        /// <summary>
        /// Marks this frame as a key frame
        /// </summary>
        /// <param name="isKeyFrame">Whether this is a key frame</param>
        public void SetKeyFrame(bool isKeyFrame)
        {
            _isKeyFrame = isKeyFrame;
        }

        /// <summary>
        /// Applies this frame's data to a transform
        /// </summary>
        /// <param name="targetTransform">Transform to apply data to</param>
        public void ApplyToTransform(Transform targetTransform)
        {
            if (targetTransform == null) return;

            targetTransform.position = _position;
            targetTransform.rotation = _rotation;
            targetTransform.localScale = _scale;
        }

        /// <summary>
        /// Applies this frame's data to an animator
        /// </summary>
        /// <param name="targetAnimator">Animator to apply data to</param>
        public void ApplyToAnimator(Animator targetAnimator)
        {
            if (targetAnimator == null || string.IsNullOrEmpty(_animationState)) return;

            targetAnimator.Play(_animationState, 0, _animationTime);
        }

        /// <summary>
        /// Applies this frame's data to a rigidbody
        /// </summary>
        /// <param name="targetRigidbody">Rigidbody to apply data to</param>
        public void ApplyToRigidbody(Rigidbody targetRigidbody)
        {
            if (targetRigidbody == null || !_hasPhysicsData) return;

            targetRigidbody.velocity = _velocity;
            targetRigidbody.angularVelocity = _angularVelocity;
        }

        /// <summary>
        /// Creates a copy of this frame data
        /// </summary>
        /// <returns>New FrameData instance with the same values</returns>
        public FrameData Clone()
        {
            var clone = new FrameData(_position, _rotation, _scale, _timestamp, _frameIndex);
            clone.SetAnimationData(_animationTime, _animationState, _animationWeight);
            
            if (_hasPhysicsData)
            {
                clone.SetPhysicsData(_velocity, _angularVelocity);
            }
            
            clone.SetKeyFrame(_isKeyFrame);
            return clone;
        }

        /// <summary>
        /// Interpolates between two frames
        /// </summary>
        /// <param name="other">Other frame to interpolate with</param>
        /// <param name="t">Interpolation factor (0-1)</param>
        /// <returns>Interpolated frame data</returns>
        public FrameData Interpolate(FrameData other, float t)
        {
            if (other == null) return Clone();

            t = Mathf.Clamp01(t);

            Vector3 interpolatedPosition = Vector3.Lerp(_position, other._position, t);
            Quaternion interpolatedRotation = Quaternion.Slerp(_rotation, other._rotation, t);
            Vector3 interpolatedScale = Vector3.Lerp(_scale, other._scale, t);

            float interpolatedAnimationTime = Mathf.Lerp(_animationTime, other._animationTime, t);
            float interpolatedAnimationWeight = Mathf.Lerp(_animationWeight, other._animationWeight, t);

            var interpolatedFrame = new FrameData(
                interpolatedPosition, interpolatedRotation, interpolatedScale,
                interpolatedAnimationTime, _animationState, interpolatedAnimationWeight,
                _timestamp + (other._timestamp - _timestamp) * t,
                Mathf.RoundToInt(Mathf.Lerp(_frameIndex, other._frameIndex, t))
            );

            // Interpolate physics data if both frames have it
            if (_hasPhysicsData && other._hasPhysicsData)
            {
                Vector3 interpolatedVelocity = Vector3.Lerp(_velocity, other._velocity, t);
                Vector3 interpolatedAngularVelocity = Vector3.Lerp(_angularVelocity, other._angularVelocity, t);
                interpolatedFrame.SetPhysicsData(interpolatedVelocity, interpolatedAngularVelocity);
            }

            return interpolatedFrame;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Calculates the distance between this frame and another
        /// </summary>
        /// <param name="other">Other frame to compare with</param>
        /// <returns>Distance between positions</returns>
        public float GetDistanceTo(FrameData other)
        {
            if (other == null) return float.MaxValue;
            return Vector3.Distance(_position, other._position);
        }

        /// <summary>
        /// Calculates the rotation difference between this frame and another
        /// </summary>
        /// <param name="other">Other frame to compare with</param>
        /// <returns>Angle difference in degrees</returns>
        public float GetRotationDifference(FrameData other)
        {
            if (other == null) return float.MaxValue;
            return Quaternion.Angle(_rotation, other._rotation);
        }

        /// <summary>
        /// Checks if this frame is significantly different from another
        /// </summary>
        /// <param name="other">Other frame to compare with</param>
        /// <param name="positionThreshold">Position difference threshold</param>
        /// <param name="rotationThreshold">Rotation difference threshold</param>
        /// <returns>True if frames are significantly different</returns>
        public bool IsSignificantlyDifferent(FrameData other, float positionThreshold, float rotationThreshold)
        {
            if (other == null) return true;

            float positionDiff = GetDistanceTo(other);
            float rotationDiff = GetRotationDifference(other);

            return positionDiff > positionThreshold || rotationDiff > rotationThreshold;
        }

        #endregion
    }
}
