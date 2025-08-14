using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Laboratory.Tools
{
    /// <summary>
    /// Records player inputs over time and provides functionality to replay them.
    /// This class captures input frames with timestamps and can reproduce the exact sequence
    /// of inputs during playback for debugging, testing, or demonstration purposes.
    /// </summary>
    public class ReplayRecorder : IDisposable
    {
        #region Nested Types
        
        /// <summary>
        /// Represents a single frame of recorded input data with timestamp information.
        /// </summary>
        private struct InputFrame
        {
            /// <summary>
            /// The timestamp when this input frame was recorded, relative to recording start time.
            /// </summary>
            public float Timestamp;
            
            /// <summary>
            /// The movement input vector for this frame.
            /// </summary>
            public Vector2 Movement;
            
            /// <summary>
            /// Whether the jump input was pressed during this frame.
            /// </summary>
            public bool JumpPressed;
            
            /// <summary>
            /// Whether the attack input was pressed during this frame.
            /// </summary>
            public bool AttackPressed;
        }
        
        #endregion
        
        #region Fields
        
        /// <summary>
        /// Collection of all recorded input frames in chronological order.
        /// </summary>
        private readonly List<InputFrame> _recordedFrames = new();
        
        /// <summary>
        /// Reference to the player input component for capturing input actions.
        /// </summary>
        private readonly PlayerInput _playerInput;
        
        /// <summary>
        /// Indicates whether the recorder is currently capturing input data.
        /// </summary>
        private bool _isRecording = false;
        
        /// <summary>
        /// Indicates whether the recorder is currently replaying recorded input data.
        /// </summary>
        private bool _isReplaying = false;
        
        /// <summary>
        /// The time when recording or replaying started, used for calculating relative timestamps.
        /// </summary>
        private float _startTime = 0f;
        
        /// <summary>
        /// Current index in the recorded frames array during replay operations.
        /// </summary>
        private int _replayIndex = 0;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Gets a value indicating whether the recorder is currently recording input data.
        /// </summary>
        public bool IsRecording => _isRecording;
        
        /// <summary>
        /// Gets a value indicating whether the recorder is currently replaying recorded data.
        /// </summary>
        public bool IsReplaying => _isReplaying;
        
        /// <summary>
        /// Gets the number of recorded input frames.
        /// </summary>
        public int RecordedFrameCount => _recordedFrames.Count;
        
        /// <summary>
        /// Gets a value indicating whether replay data is available.
        /// </summary>
        public bool HasReplayData => _recordedFrames.Count > 0;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayRecorder"/> class.
        /// </summary>
        /// <param name="playerInput">The player input component to monitor for input actions.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="playerInput"/> is null.</exception>
        public ReplayRecorder(PlayerInput playerInput)
        {
            _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Begins recording player inputs. Clears any previously recorded data.
        /// </summary>
        /// <remarks>
        /// This method will stop any active replay and clear the recorded frames collection
        /// before starting a new recording session.
        /// </remarks>
        public void StartRecording()
        {
            _recordedFrames.Clear();
            _isRecording = true;
            _isReplaying = false;
            _startTime = Time.time;
        }
        
        /// <summary>
        /// Stops the current recording session.
        /// </summary>
        /// <remarks>
        /// Recorded data is preserved and can be replayed using <see cref="StartReplay"/>.
        /// </remarks>
        public void StopRecording()
        {
            _isRecording = false;
        }
        
        /// <summary>
        /// Begins replaying previously recorded input data.
        /// </summary>
        /// <remarks>
        /// If no recorded data is available, a warning will be logged and no replay will start.
        /// This method will stop any active recording before starting replay.
        /// </remarks>
        public void StartReplay()
        {
            if (_recordedFrames.Count == 0)
            {
                Debug.LogWarning("ReplayRecorder: No replay data available.");
                return;
            }

            _isReplaying = true;
            _isRecording = false;
            _replayIndex = 0;
            _startTime = Time.time;
        }
        
        /// <summary>
        /// Stops the current replay session.
        /// </summary>
        /// <remarks>
        /// Recorded data is preserved and replay can be restarted using <see cref="StartReplay"/>.
        /// </remarks>
        public void StopReplay()
        {
            _isReplaying = false;
        }
        
        /// <summary>
        /// Updates the recorder state. Must be called every frame to capture or replay input data.
        /// </summary>
        /// <remarks>
        /// This method should be called from Update() or FixedUpdate() depending on your timing requirements.
        /// During recording, it captures the current input state. During replay, it advances the playback position.
        /// </remarks>
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
        /// Gets the current input state during replay.
        /// </summary>
        /// <returns>
        /// A tuple containing the movement vector and button states for the current replay frame.
        /// Returns default values if not currently replaying or if replay has ended.
        /// </returns>
        /// <remarks>
        /// This method should be called by input consumers to get the simulated input during replay.
        /// </remarks>
        public (Vector2 movement, bool jump, bool attack) GetCurrentReplayInput()
        {
            if (!_isReplaying || _replayIndex >= _recordedFrames.Count)
                return (default, false, false);

            var frame = _recordedFrames[_replayIndex];
            return (frame.Movement, frame.JumpPressed, frame.AttackPressed);
        }
        
        /// <summary>
        /// Clears all recorded data and resets the recorder to its initial state.
        /// </summary>
        /// <remarks>
        /// This method stops any active recording or replay and removes all captured data.
        /// </remarks>
        public void ClearRecordedData()
        {
            _recordedFrames.Clear();
            _isRecording = false;
            _isReplaying = false;
            _replayIndex = 0;
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Captures the current input state and adds it to the recorded frames.
        /// </summary>
        /// <remarks>
        /// This method reads input values from the PlayerInput component and stores them
        /// with the current timestamp relative to the recording start time.
        /// </remarks>
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
        
        /// <summary>
        /// Advances the replay position based on elapsed time since replay started.
        /// </summary>
        /// <remarks>
        /// This method compares the elapsed replay time with recorded frame timestamps
        /// to determine which frame should be active for the current replay position.
        /// </remarks>
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
        
        #endregion
        
        #region IDisposable Implementation
        
        /// <summary>
        /// Releases all resources used by the ReplayRecorder.
        /// </summary>
        /// <remarks>
        /// This method clears all recorded data and should be called when the recorder
        /// is no longer needed to free memory resources.
        /// </remarks>
        public void Dispose()
        {
            ClearRecordedData();
        }
        
        #endregion
    }
}
