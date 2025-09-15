namespace Laboratory.Core.Input.Interfaces
{
    /// <summary>
    /// Interface for input configuration settings.
    /// Defines the contract for input system configuration.
    /// </summary>
    public interface IInputConfiguration
    {
        /// <summary>
        /// Mouse/camera look sensitivity multiplier.
        /// </summary>
        float LookSensitivity { get; }
        
        /// <summary>
        /// Movement input sensitivity multiplier.
        /// </summary>
        float MovementSensitivity { get; }
        
        /// <summary>
        /// Input deadzone threshold for analog inputs.
        /// </summary>
        float InputDeadzone { get; }
        
        /// <summary>
        /// Look input deadzone threshold.
        /// </summary>
        float LookDeadzone { get; }
        
        /// <summary>
        /// Whether input buffering is enabled.
        /// </summary>
        bool InputBufferingEnabled { get; }
        
        /// <summary>
        /// Time window for input buffering.
        /// </summary>
        float InputBufferTime { get; }
        
        /// <summary>
        /// Threshold for long press detection.
        /// </summary>
        float LongPressThreshold { get; }
        
        /// <summary>
        /// Repeat rate for long press inputs.
        /// </summary>
        float LongPressRepeatRate { get; }
        
        /// <summary>
        /// Serialized input bindings data.
        /// </summary>
        string SerializedBindings { get; }
        
        /// <summary>
        /// Apply configuration settings to input systems.
        /// </summary>
        void ApplySettings();
    }
}