using System;
using Unity.Mathematics;

namespace Laboratory.Core.Input.Interfaces
{
    /// <summary>
    /// Interface for input buffering system that stores and manages timed input events.
    /// </summary>
    public interface IInputBuffer
    {
        #region Properties
        
        /// <summary>Maximum time inputs are buffered in seconds.</summary>
        float BufferTime { get; set; }
        
        /// <summary>Whether buffering is currently enabled.</summary>
        bool IsEnabled { get; set; }
        
        /// <summary>Current number of buffered inputs.</summary>
        int BufferedInputCount { get; }
        
        #endregion

        #region Buffer Management
        
        /// <summary>Adds an input to the buffer.</summary>
        void BufferInput(string actionName, object value, double timestamp);
        
        /// <summary>Checks if an input was performed within buffer time.</summary>
        bool WasInputBuffered(string actionName, double withinTime);
        
        /// <summary>Consumes and removes a buffered input.</summary>
        bool ConsumeBufferedInput(string actionName);
        
        /// <summary>Clears all buffered inputs.</summary>
        void ClearBuffer();
        
        /// <summary>Updates buffer, removing expired inputs.</summary>
        void Update(double currentTime);
        
        #endregion

        #region Query Methods
        
        /// <summary>Gets the most recent buffered value for an action.</summary>
        T GetBufferedValue<T>(string actionName) where T : struct;
        
        /// <summary>Gets all buffered inputs for a specific action.</summary>
        BufferedInput[] GetBufferedInputs(string actionName);
        
        #endregion
    }

    /// <summary>
    /// Interface for input validation system.
    /// </summary>
    public interface IInputValidator
    {
        /// <summary>Validates if an input action is allowed in current context.</summary>
        bool IsActionAllowed(string actionName);
        
        /// <summary>Validates input value range and format.</summary>
        bool ValidateInputValue<T>(T value) where T : struct;
        
        /// <summary>Validates movement input vector.</summary>
        bool ValidateMovementInput(float2 movement);
        
        /// <summary>Validates look input vector.</summary>
        bool ValidateLookInput(float3 look);
        
        /// <summary>Sets input validation rules.</summary>
        void SetValidationRules(InputValidationRules rules);
    }

    /// <summary>
    /// Interface for input event system.
    /// </summary>
    public interface IInputEventSystem
    {
        /// <summary>Subscribes to input action events.</summary>
        void Subscribe(string actionName, Action<InputActionEventArgs> callback);
        
        /// <summary>Unsubscribes from input action events.</summary>
        void Unsubscribe(string actionName, Action<InputActionEventArgs> callback);
        
        /// <summary>Publishes an input event.</summary>
        void PublishEvent(InputActionEventArgs eventArgs);
        
        /// <summary>Clears all event subscriptions.</summary>
        void ClearSubscriptions();
    }

    /// <summary>
    /// Represents a buffered input entry.
    /// </summary>
    public struct BufferedInput
    {
        public string ActionName;
        public object Value;
        public double Timestamp;
        public bool IsConsumed;
    }

    /// <summary>
    /// Input validation rules configuration.
    /// </summary>
    [Serializable]
    public struct InputValidationRules
    {
        public float MaxMovementMagnitude;
        public float MaxLookSensitivity;
        public string[] DisallowedActions;
        public bool RequireNormalizedMovement;
    }
}