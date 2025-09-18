using Unity.Mathematics;
using UnityEngine.InputSystem;

namespace Laboratory.Models.Input
{
    /// <summary>
    /// Input data structures for player controls
    /// </summary>
    public struct PlayerControlsData
    {
        public float2 MovementInput;
        public float2 LookInput;
        public bool JumpPressed;
        public bool FirePressed;
        public bool ReloadPressed;
        public bool InteractPressed;
        public bool SprintPressed;
        public bool CrouchPressed;
        public bool AimPressed;
        public float Timestamp;
    }

    /// <summary>
    /// Input device information
    /// </summary>
    public struct InputDeviceInfo
    {
        public string DeviceName;
        public InputDeviceType DeviceType;
        public bool IsConnected;
        public float LastActivityTime;
    }

    /// <summary>
    /// Input device types
    /// </summary>
    public enum InputDeviceType
    {
        KeyboardMouse,
        Gamepad,
        Touch,
        VR
    }
}