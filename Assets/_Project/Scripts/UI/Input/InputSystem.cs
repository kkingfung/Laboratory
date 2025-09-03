using UnityEngine;

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

    /// <summary>
    /// Player controls stub for compatibility with existing code.
    /// This should be replaced with proper Input System implementation when available.
    /// </summary>
    public class PlayerControls
    {
        public MiniMapControls MiniMap { get; } = new();
        
        public void Enable() 
        {
            // TODO: Enable input actions when proper Input System is implemented
        }
        
        public void Disable() 
        {
            // TODO: Disable input actions when proper Input System is implemented
        }
    }

    /// <summary>
    /// Minimap-specific input controls stub.
    /// </summary>
    public class MiniMapControls
    {
        public InputAction Zoom { get; } = new("Zoom");
        public InputAction Pan { get; } = new("Pan");
        public InputAction Click { get; } = new("Click");
    }

    /// <summary>
    /// Input action stub for compatibility.
    /// </summary>
    public class InputAction
    {
        public string Name { get; }
        public System.Action<InputContext> performed;
        public System.Action<InputContext> started;
        public System.Action<InputContext> canceled;

        public InputAction(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Input context stub for input action callbacks.
    /// </summary>
    public class InputContext
    {
        public T ReadValue<T>() where T : struct
        {
            // This is a stub - replace with actual implementation
            return default(T);
        }
    }
}
