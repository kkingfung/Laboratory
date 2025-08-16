using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Records actor transform data during gameplay for replay functionality.
    /// Captures position and rotation data each fixed update frame.
    /// </summary>
    public class ActorRecorder : MonoBehaviour
    {
        #region Fields

        [Header("Recording Settings")]
        [Tooltip("Name identifier for this actor")]
        [SerializeField] private string actorName = "Player";
        
        [Tooltip("Whether recording is currently active")]
        [SerializeField] private bool record = true;

        private List<FrameData> frames = new List<FrameData>();

        #endregion

        #region Public Methods

        /// <summary>
        /// Clears all recorded frame data
        /// </summary>
        public void Clear()
        {
            frames.Clear();
        }

        /// <summary>
        /// Gets the replay data for this actor
        /// </summary>
        /// <returns>Actor replay data containing name and frames</returns>
        public ActorReplayData GetReplayData()
        {
            return new ActorReplayData
            {
                actorName = actorName,
                frames = frames.ToArray()
            };
        }

        /// <summary>
        /// Sets the recording state
        /// </summary>
        /// <param name="isRecording">True to start recording, false to stop</param>
        public void SetRecording(bool isRecording)
        {
            record = isRecording;
        }

        /// <summary>
        /// Gets the current number of recorded frames
        /// </summary>
        public int FrameCount => frames.Count;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Records frame data during fixed update if recording is active
        /// </summary>
        void FixedUpdate()
        {
            if (!record) return;

            frames.Add(new FrameData
            {
                position = transform.position,
                rotation = transform.rotation
            });
        }

        #endregion
    }
}
