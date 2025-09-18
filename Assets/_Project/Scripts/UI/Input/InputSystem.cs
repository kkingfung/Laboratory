using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Laboratory.UI.Input
{
    /// <summary>
    /// Input system compatibility layer for UI components.
    /// Provides a wrapper around Unity's Input system for UI-specific input handling.
    /// </summary>
    public static class InputSystem
    {
        /// <summary>
        /// Checks if the specified key was pressed down during the current frame.
        /// </summary>
        /// <param name="keyCode">Key to check</param>
        /// <returns>True if the key was pressed this frame</returns>
        public static bool GetKeyDown(KeyCode keyCode)
        {
            return UnityEngine.Input.GetKeyDown(keyCode);
        }

        /// <summary>
        /// Gets the raw input value for the specified axis.
        /// </summary>
        /// <param name="axisName">Name of the input axis</param>
        /// <returns>Raw axis value (-1 to 1)</returns>
        public static float GetAxisRaw(string axisName)
        {
            return UnityEngine.Input.GetAxisRaw(axisName);
        }
    }
}
