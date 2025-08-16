using System;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Data container for actor replay information.
    /// Contains actor name and frame data for replay functionality.
    /// </summary>
    [Serializable]
    public class ActorReplayData
    {
        #region Fields

        /// <summary>
        /// Name identifier for the actor
        /// </summary>
        public string actorName;

        /// <summary>
        /// Array of frame data for replay
        /// </summary>
        public FrameData[] frames;

        #endregion
    }
}
