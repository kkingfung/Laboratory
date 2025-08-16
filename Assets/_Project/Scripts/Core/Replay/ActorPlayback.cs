using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Handles playback of recorded actor frame data.
    /// Provides controls for play, pause, stop, and frame navigation.
    /// </summary>
    public class ActorPlayback : MonoBehaviour
    {
        #region Fields

        [Header("Playback Settings")]
        [Tooltip("Whether playback is currently active")]
        [SerializeField] private bool play = false;
        
        [Tooltip("Whether to loop playback when reaching the end")]
        [SerializeField] private bool loop = false;

        private FrameData[] frames;
        private int currentFrame = 0;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current frame index
        /// </summary>
        public int CurrentFrame => currentFrame;

        /// <summary>
        /// Gets the total number of frames
        /// </summary>
        public int FramesLength => frames?.Length ?? 0;

        /// <summary>
        /// Gets whether playback is currently active
        /// </summary>
        public bool IsPlaying => play;

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the frame data for playback
        /// </summary>
        /// <param name="newFrames">Array of frame data</param>
        public void SetFrames(FrameData[] newFrames)
        {
            frames = newFrames;
            currentFrame = 0;
        }

        /// <summary>
        /// Starts playback
        /// </summary>
        public void Play()
        {
            play = true;
        }

        /// <summary>
        /// Pauses playback
        /// </summary>
        public void Pause()
        {
            play = false;
        }

        /// <summary>
        /// Stops playback and resets to first frame
        /// </summary>
        public void Stop()
        {
            play = false;
            currentFrame = 0;
        }

        /// <summary>
        /// Jumps to a specific frame index
        /// </summary>
        /// <param name="frameIndex">Target frame index</param>
        public void GoToFrame(int frameIndex)
        {
            if (frames == null || frames.Length == 0) return;
            
            currentFrame = Mathf.Clamp(frameIndex, 0, frames.Length - 1);
            ApplyFrame();
        }

        /// <summary>
        /// Sets loop behavior
        /// </summary>
        /// <param name="shouldLoop">True to enable looping</param>
        public void SetLoop(bool shouldLoop)
        {
            loop = shouldLoop;
        }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Updates playback during fixed update
        /// </summary>
        void FixedUpdate()
        {
            if (!play || frames == null || frames.Length == 0) return;

            ApplyFrame();

            currentFrame++;
            if (currentFrame >= frames.Length)
            {
                if (loop) 
                    currentFrame = 0;
                else 
                    play = false;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Applies the current frame data to the transform
        /// </summary>
        private void ApplyFrame()
        {
            if (frames == null || frames.Length == 0 || currentFrame >= frames.Length) return;
            
            var frame = frames[currentFrame];
            transform.position = frame.position;
            transform.rotation = frame.rotation;
        }

        #endregion
    }
}
