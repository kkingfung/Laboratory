using System;
using UnityEngine;

namespace Laboratory.Core.Replay
{
    /// <summary>
    /// Represents frame data for actor replay system.
    /// Contains position, rotation, and optional animation state information.
    /// </summary>
    [Serializable]
    public struct FrameData
    {
        #region Fields

        /// <summary>
        /// World or local position of the actor
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// World or local rotation of the actor
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// Optional animation state for animation tracking
        /// </summary>
        public string animationState;

        #endregion
    }
}
