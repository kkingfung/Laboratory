using UnityEngine;

namespace Laboratory.Core.Input.Events
{
    /// <summary>
    /// Event fired when movement input is detected.
    /// </summary>
    public class MovementInputEvent
    {
        public Vector2 Movement { get; set; }
        public bool IsMoving { get; set; }
        public float Timestamp { get; set; }
        public string Source { get; set; } = "";
        
        public MovementInputEvent(Vector2 movement, bool isMoving, float timestamp, string source = "")
        {
            Movement = movement;
            IsMoving = isMoving;
            Timestamp = timestamp;
            Source = source;
        }
        
        public MovementInputEvent() { } // Parameterless constructor
    }
    
    /// <summary>
    /// Event fired when look input is detected.
    /// </summary>
    public class LookInputEvent
    {
        public Vector2 LookDelta { get; set; }
        public float Timestamp { get; set; }
        public string Source { get; set; } = "";
        
        public LookInputEvent(Vector2 lookDelta, float timestamp, string source = "")
        {
            LookDelta = lookDelta;
            Timestamp = timestamp;
            Source = source;
        }
        
        public LookInputEvent() { } // Parameterless constructor
    }
    
    /// <summary>
    /// Event fired when action input is detected.
    /// </summary>
    public class ActionInputEvent
    {
        public string ActionName { get; set; } = "";
        public bool IsPressed { get; set; }
        public bool IsHeld { get; set; }
        public float HoldDuration { get; set; }
        public float Timestamp { get; set; }
        public string Source { get; set; } = "";
        
        public ActionInputEvent(string actionName, bool isPressed, bool isHeld, float holdDuration, float timestamp, string source = "")
        {
            ActionName = actionName;
            IsPressed = isPressed;
            IsHeld = isHeld;
            HoldDuration = holdDuration;
            Timestamp = timestamp;
            Source = source;
        }
        
        public ActionInputEvent() { } // Parameterless constructor
    }
    
    /// <summary>
    /// Event fired when scroll input is detected.
    /// </summary>
    public class ScrollInputEvent
    {
        public Vector2 ScrollDelta { get; set; }
        public float Timestamp { get; set; }
        public string Source { get; set; } = "";
        
        public ScrollInputEvent(Vector2 scrollDelta, float timestamp, string source = "")
        {
            ScrollDelta = scrollDelta;
            Timestamp = timestamp;
            Source = source;
        }
        
        public ScrollInputEvent() { } // Parameterless constructor
    }
    
    /// <summary>
    /// Event fired for raw input data.
    /// </summary>
    public class RawInputEvent
    {
        public string InputName { get; set; } = "";
        public object InputValue { get; set; }
        public float Timestamp { get; set; }
        public string Source { get; set; } = "";
        
        public RawInputEvent(string inputName, object inputValue, float timestamp, string source = "")
        {
            InputName = inputName;
            InputValue = inputValue;
            Timestamp = timestamp;
            Source = source;
        }
        
        public RawInputEvent() { } // Parameterless constructor
    }
    
    /// <summary>
    /// Event arguments for action input events.
    /// </summary>
    public struct ActionInputEventArgs
    {
        public string ActionName { get; set; }
        public bool IsPressed { get; set; }
        public float Timestamp { get; set; }
        public object Value { get; set; }
        public string DeviceName { get; set; }
        
        public ActionInputEventArgs(string actionName, bool isPressed, float timestamp, object value = null, string deviceName = "")
        {
            ActionName = actionName;
            IsPressed = isPressed;
            Timestamp = timestamp;
            Value = value;
            DeviceName = deviceName;
        }
    }
    
    /// <summary>
    /// Event arguments for long press events.
    /// </summary>
    public struct LongPressEventArgs
    {
        public string ActionName { get; set; }
        public float PressedTime { get; set; }
        public float PressTime { get; set; }
        public float ThresholdTime { get; set; }
        public float Timestamp { get; set; }
        public string DeviceName { get; set; }
        
        public LongPressEventArgs(string actionName, float pressedTime, float timestamp, float thresholdTime = 0.5f, string deviceName = "")
        {
            ActionName = actionName;
            PressedTime = pressedTime;
            PressTime = pressedTime; // Alias for compatibility
            ThresholdTime = thresholdTime;
            Timestamp = timestamp;
            DeviceName = deviceName;
        }
    }
    
    /// <summary>
    /// Event arguments for input configuration changes.
    /// </summary>
    public struct InputConfigurationChangedEventArgs
    {
        public string PropertyName { get; set; }
        public object OldValue { get; set; }
        public object NewValue { get; set; }
        public float Timestamp { get; set; }
        
        public InputConfigurationChangedEventArgs(string propertyName, object oldValue, object newValue, float timestamp)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
            Timestamp = timestamp;
        }
    }
    
    /// <summary>
    /// Event arguments for input device changes.
    /// </summary>
    public struct InputDeviceEventArgs
    {
        public string DeviceName { get; set; }
        public string DeviceId { get; set; }
        public bool IsConnected { get; set; }
        public float Timestamp { get; set; }
        
        public InputDeviceEventArgs(string deviceName, string deviceId, bool isConnected, float timestamp)
        {
            DeviceName = deviceName;
            DeviceId = deviceId;
            IsConnected = isConnected;
            Timestamp = timestamp;
        }
    }
    
    /// <summary>
    /// Event arguments for movement input events.
    /// </summary>
    public struct MovementInputEventArgs
    {
        public UnityEngine.Vector2 Movement { get; set; }
        public bool IsMoving { get; set; }
        public float Timestamp { get; set; }
        public string Source { get; set; }
        
        public MovementInputEventArgs(UnityEngine.Vector2 movement, bool isMoving, float timestamp, string source = "")
        {
            Movement = movement;
            IsMoving = isMoving;
            Timestamp = timestamp;
            Source = source;
        }
    }
    
    /// <summary>
    /// Static event bus for input system events.
    /// </summary>
    public static class InputEvents
    {
        /// <summary>Action was pressed event.</summary>
        public static event System.Action<ActionInputEventArgs> OnActionPressed;
        
        /// <summary>Action was released event.</summary>
        public static event System.Action<ActionInputEventArgs> OnActionReleased;
        
        /// <summary>Long press started event.</summary>
        public static event System.Action<LongPressEventArgs> OnLongPressStarted;
        
        /// <summary>Long press held event.</summary>
        public static event System.Action<LongPressEventArgs> OnLongPressHeld;
        
        /// <summary>Movement input event.</summary>
        public static event System.Action<MovementInputEvent> OnMovementInput;
        
        /// <summary>Look input event.</summary>
        public static event System.Action<LookInputEvent> OnLookInput;
        
        /// <summary>Scroll input event.</summary>
        public static event System.Action<ScrollInputEvent> OnScrollInput;
        
        /// <summary>Raw input event.</summary>
        public static event System.Action<RawInputEvent> OnRawInput;
        
        /// <summary>Input configuration changed event.</summary>
        public static event System.Action<InputConfigurationChangedEventArgs> OnConfigurationChanged;
        
        /// <summary>Input device connected/disconnected event.</summary>
        public static event System.Action<InputDeviceEventArgs> OnDeviceChanged;
        
        /// <summary>Input system initialized event.</summary>
        public static event System.Action OnInputSystemInitialized;
        
        /// <summary>Movement started event.</summary>
        public static event System.Action<MovementInputEventArgs> OnMovementStarted;
        
        /// <summary>Movement stopped event.</summary>
        public static event System.Action<MovementInputEventArgs> OnMovementStopped;
        
        /// <summary>Attack action event.</summary>
        public static event System.Action<ActionInputEventArgs> OnAttackAction;
        
        /// <summary>Jump action event.</summary>
        public static event System.Action<ActionInputEventArgs> OnJumpAction;
        
        /// <summary>Roll action event.</summary>
        public static event System.Action<ActionInputEventArgs> OnRollAction;
        
        /// <summary>Interact action event.</summary>
        public static event System.Action<ActionInputEventArgs> OnInteractAction;
        
        #region Public Event Triggers
        
        public static void TriggerInputSystemInitialized() => OnInputSystemInitialized?.Invoke();
        public static void ClearAllEvents()
        {
            OnActionPressed = null;
            OnActionReleased = null;
            OnLongPressStarted = null;
            OnLongPressHeld = null;
            OnMovementInput = null;
            OnLookInput = null;
            OnScrollInput = null;
            OnRawInput = null;
            OnConfigurationChanged = null;
            OnDeviceChanged = null;
            OnInputSystemInitialized = null;
            OnMovementStarted = null;
            OnMovementStopped = null;
            OnAttackAction = null;
            OnJumpAction = null;
            OnRollAction = null;
            OnInteractAction = null;
        }
        public static void TriggerConfigurationChanged(InputConfigurationChangedEventArgs args) => OnConfigurationChanged?.Invoke(args);
        public static void TriggerMovementInput(MovementInputEvent args) => OnMovementInput?.Invoke(args);
        public static void TriggerMovementStarted(MovementInputEventArgs args) => OnMovementStarted?.Invoke(args);
        public static void TriggerMovementStopped(MovementInputEventArgs args) => OnMovementStopped?.Invoke(args);
        public static void TriggerActionPressed(ActionInputEventArgs args) => OnActionPressed?.Invoke(args);
        public static void TriggerAttackAction(ActionInputEventArgs args) => OnAttackAction?.Invoke(args);
        public static void TriggerJumpAction(ActionInputEventArgs args) => OnJumpAction?.Invoke(args);
        public static void TriggerRollAction(ActionInputEventArgs args) => OnRollAction?.Invoke(args);
        public static void TriggerInteractAction(ActionInputEventArgs args) => OnInteractAction?.Invoke(args);
        public static void TriggerActionReleased(ActionInputEventArgs args) => OnActionReleased?.Invoke(args);
        public static void TriggerLongPressStarted(LongPressEventArgs args) => OnLongPressStarted?.Invoke(args);
        public static void TriggerLongPressHeld(LongPressEventArgs args) => OnLongPressHeld?.Invoke(args);
        public static void TriggerDeviceConnected(InputDeviceEventArgs args) => OnDeviceChanged?.Invoke(args);
        public static void TriggerDeviceDisconnected(InputDeviceEventArgs args) => OnDeviceChanged?.Invoke(args);
        
        #endregion
        
        #region Internal Event Triggers (Keep existing internal methods for backward compatibility)
        
        internal static void TriggerActionPressedInternal(ActionInputEventArgs args) => OnActionPressed?.Invoke(args);
        internal static void TriggerActionReleasedInternal(ActionInputEventArgs args) => OnActionReleased?.Invoke(args);
        internal static void TriggerLongPressStartedInternal(LongPressEventArgs args) => OnLongPressStarted?.Invoke(args);
        internal static void TriggerLongPressHeldInternal(LongPressEventArgs args) => OnLongPressHeld?.Invoke(args);
        internal static void TriggerMovementInputInternal(MovementInputEvent args) => OnMovementInput?.Invoke(args);
        internal static void TriggerLookInput(LookInputEvent args) => OnLookInput?.Invoke(args);
        internal static void TriggerScrollInput(ScrollInputEvent args) => OnScrollInput?.Invoke(args);
        internal static void TriggerRawInput(RawInputEvent args) => OnRawInput?.Invoke(args);
        internal static void TriggerConfigurationChangedInternal(InputConfigurationChangedEventArgs args) => OnConfigurationChanged?.Invoke(args);
        internal static void TriggerDeviceConnectedInternal(InputDeviceEventArgs args) => OnDeviceChanged?.Invoke(args);
        internal static void TriggerDeviceDisconnectedInternal(InputDeviceEventArgs args) => OnDeviceChanged?.Invoke(args);
        internal static void TriggerInputSystemInitializedInternal() => OnInputSystemInitialized?.Invoke();
        internal static void TriggerMovementStartedInternal(MovementInputEventArgs args) => OnMovementStarted?.Invoke(args);
        internal static void TriggerMovementStoppedInternal(MovementInputEventArgs args) => OnMovementStopped?.Invoke(args);
        internal static void TriggerAttackActionInternal(ActionInputEventArgs args) => OnAttackAction?.Invoke(args);
        internal static void TriggerJumpActionInternal(ActionInputEventArgs args) => OnJumpAction?.Invoke(args);
        internal static void TriggerRollActionInternal(ActionInputEventArgs args) => OnRollAction?.Invoke(args);
        internal static void TriggerInteractActionInternal(ActionInputEventArgs args) => OnInteractAction?.Invoke(args);
        
        #endregion
    }
}