using System;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Container for replay session data.
    /// Stores all actor data and frame rate information for a complete replay session.
    /// </summary>
    [Serializable]
    public class ReplaySession
    {
        #region Fields

        /// <summary>
        /// Array of all actors in the replay session
        /// </summary>
        public ActorReplayData[] actors;

        /// <summary>
        /// Frame rate at which the replay was recorded
        /// </summary>
        public float frameRate;

        #endregion
    }
}
