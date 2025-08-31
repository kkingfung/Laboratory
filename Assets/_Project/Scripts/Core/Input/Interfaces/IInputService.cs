using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Laboratory.Core.Input.Interfaces
{
    /// <summary>
    /// Central interface for all input-related operations.
    /// Provides abstraction layer between Unity Input System and game logic.
    /// </summary>
    public interface IInputService
    {
        #region Events
        
        /// <summary>Fired when any input action is performed.</summary>
        event Action<InputActionEventArgs> OnInputActionPerformed;
        
        /// <summary>Fired when input configuration changes.</summary>
        event Action OnInputConfigurationChanged;
        
        /// <summary>Fired when input device is connected/disconnected.</summary>
        event Action<InputDevice> OnDeviceChanged;
        
        #endregion

        #region Properties
        
        /// <summary>Current input configuration.</summary>
        IInputConfiguration Configuration { get; }
        
        /// <summary>Whether the input service is initialized and ready.</summary>
        bool IsInitialized { get; }
        
        /// <summary>Current active input devices.</summary>
        InputDevice[] ActiveDevices { get; }
        
        #endregion

        #region Input State Query Methods
        
        /// <summary>Gets current movement input vector.</summary>
        float2 GetMovementInput();
        
        /// <summary>Gets current look input vector.</summary>
        float3 GetLookInput();
        
        /// <summary>Checks if specific action was performed this frame.</summary>
        bool WasActionPerformed(string actionName);
        
        /// <summary>Checks if specific action is currently pressed.</summary>
        bool IsActionPressed(string actionName);
        
        /// <summary>Gets the current value of a specific action.</summary>
        T GetActionValue<T>(string actionName) where T : struct;
        
        #endregion

        #region Input Management Methods
        
        /// <summary>Initializes the input service.</summary>
        void Initialize();
        
        /// <summary>Enables input processing.</summary>
        void Enable();
        
        /// <summary>Disables input processing.</summary>
        void Disable();
        
        /// <summary>Updates the input service (called per frame).</summary>
        void Update();
        
        /// <summary>Cleans up resources and shuts down the service.</summary>
        void Shutdown();
        
        #endregion

        #region Configuration Methods
        
        /// <summary>Saves current input configuration.</summary>
        void SaveConfiguration();
        
        /// <summary>Loads input configuration.</summary>
        void LoadConfiguration();
        
        /// <summary>Resets input configuration to defaults.</summary>
        void ResetConfiguration();
        
        /// <summary>Starts interactive rebinding for specific action.</summary>
        void StartRebind(string actionName, int bindingIndex, Action<bool> onComplete = null);
        
        #endregion

        #region Validation Methods
        
        /// <summary>Validates current input state.</summary>
        bool ValidateInputState();
        
        /// <summary>Checks if input action exists.</summary>
        bool HasAction(string actionName);
        
        #endregion
    }

    /// <summary>
    /// Input action event arguments.
    /// </summary>
    public struct InputActionEventArgs
    {
        public string ActionName;
        public InputActionPhase Phase;
        public object Value;
        public double Time;
        public InputDevice Device;
    }

    /// <summary>
    /// Input configuration interface.
    /// </summary>
    public interface IInputConfiguration
    {
        /// <summary>Look sensitivity setting.</summary>
        float LookSensitivity { get; set; }
        
        /// <summary>Input deadzone setting.</summary>
        float InputDeadzone { get; set; }
        
        /// <summary>Whether input buffering is enabled.</summary>
        bool InputBufferingEnabled { get; set; }
        
        /// <summary>Input buffer time in seconds.</summary>
        float InputBufferTime { get; set; }
        
        /// <summary>Custom key bindings.</summary>
        string SerializedBindings { get; set; }
    }
}