using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Infrastructure
{
    /// <summary>
    /// Records player inputs over time and can replay them.
    /// </summary>
    public class ReplayRecorder : IDisposable
    {
        private struct InputFrame
        {
            public float Timestamp;
            public Vector2 Movement;
            public bool JumpPressed;
            public bool AttackPressed;
            // Add more input fields as needed
        }

        private readonly List<InputFrame> _recordedFrames = new();

        private bool _isRecording = false;
        private bool _isReplaying = false;

        private float _startTime = 0f;
        private int _replayIndex = 0;

        // Input actions or component you want to capture
        private readonly PlayerInput _playerInput;

        public ReplayRecorder(PlayerInput playerInput)
        {
            _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
        }

        /// <summary>
        /// Start recording inputs.
        /// </summary>
        public void StartRecording()
        {
            _recordedFrames.Clear();
            _isRecording = true;
            _isReplaying = false;
            _startTime = Time.time;
        }

        /// <summary>
        /// Stop recording inputs.
        /// </summary>
        public void StopRecording()
        {
            _isRecording = false;
        }

        /// <summary>
        /// Start replaying recorded inputs.
        /// </summary>
        public void StartReplay()
        {
            if (_recordedFrames.Count == 0)
            {
                Debug.LogWarning("No replay data available.");
                return;
            }

            _isReplaying = true;
            _isRecording = false;
            _replayIndex = 0;
            _startTime = Time.time;
        }

        /// <summary>
        /// Stop replay.
        /// </summary>
        public void StopReplay()
        {
            _isReplaying = false;
        }

        /// <summary>
        /// Call every frame to update recording or replaying.
        /// </summary>
        public void Update()
        {
            if (_isRecording)
            {
                RecordFrame();
            }
            else if (_isReplaying)
            {
                ReplayFrame();
            }
        }

        /// <summary>
        /// Get current replay input state. Returns default if not replaying.
        /// </summary>
        public (Vector2 movement, bool jump, bool attack) GetCurrentReplayInput()
        {
            if (!_isReplaying || _replayIndex >= _recordedFrames.Count)
                return (default, false, false);

            var frame = _recordedFrames[_replayIndex];
            return (frame.Movement, frame.JumpPressed, frame.AttackPressed);
        }

        private void RecordFrame()
        {
            float timestamp = Time.time - _startTime;

            Vector2 movement = _playerInput.actions["Move"].ReadValue<Vector2>();
            bool jumpPressed = _playerInput.actions["Jump"].WasPressedThisFrame();
            bool attackPressed = _playerInput.actions["Attack"].WasPressedThisFrame();

            _recordedFrames.Add(new InputFrame
            {
                Timestamp = timestamp,
                Movement = movement,
                JumpPressed = jumpPressed,
                AttackPressed = attackPressed
            });
        }

        private void ReplayFrame()
        {
            float elapsed = Time.time - _startTime;

            // Advance replay index while next frame's timestamp <= elapsed
            while (_replayIndex < _recordedFrames.Count &&
                   _recordedFrames[_replayIndex].Timestamp <= elapsed)
            {
                _replayIndex++;
            }
        }

        public void Dispose()
        {
            _recordedFrames.Clear();
        }
    }
}
