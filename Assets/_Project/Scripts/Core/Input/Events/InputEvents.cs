using System;
using Unity.Mathematics;
using Laboratory.Core.Input.Interfaces;

namespace Laboratory.Core.Input.Events
{
    /// <summary>
    /// Collection of all input-related events for the input system.
    /// </summary>
    public static class InputEvents
    {
        #region Movement Events
        
        /// <summary>Fired when movement input is detected.</summary>
        public static event Action<MovementInputEventArgs> OnMovementInput;
        
        /// <summary>Fired when movement input starts.</summary>
        public static event Action<MovementInputEventArgs> OnMovementStarted;
        
        /// <summary>Fired when movement input stops.</summary>
        public static event Action<MovementInputEventArgs> OnMovementStopped;
        
        #endregion

        #region Action Events
        
        /// <summary>Fired when any action button is pressed.</summary>
        public static event Action<ActionInputEventArgs> OnActionPressed;
        
        /// <summary>Fired when any action button is released.</summary>
        public static event Action<ActionInputEventArgs> OnActionReleased;
        
        /// <summary>Fired when attack action is performed.</summary>
        public static event Action<ActionInputEventArgs> OnAttackAction;
        
        /// <summary>Fired when jump action is performed.</summary>
        public static event Action<ActionInputEventArgs> OnJumpAction;
        
        /// <summary>Fired when roll action is performed.</summary>
        public static event Action<ActionInputEventArgs> OnRollAction;
        
        /// <summary>Fired when interact action is performed.</summary>
        public static event Action<ActionInputEventArgs> OnInteractAction;
        
        #endregion

        #region Look Events
        
        /// <summary>Fired when look input is detected.</summary>
        public static event Action<LookInputEventArgs> OnLookInput;
        
        #endregion

        #region System Events
        
        /// <summary>Fired when input system is initialized.</summary>
        public static event Action OnInputSystemInitialized;
        
        /// <summary>Fired when input configuration changes.</summary>
        public static event Action<InputConfigurationChangedEventArgs> OnConfigurationChanged;
        
        /// <summary>Fired when input device is connected.</summary>
        public static event Action<InputDeviceEventArgs> OnDeviceConnected;
        
        /// <summary>Fired when input device is disconnected.</summary>
        public static event Action<InputDeviceEventArgs> OnDeviceDisconnected;
        
        #endregion

        #region Long Press Events
        
        /// <summary>Fired when long press is detected.</summary>
        public static event Action<LongPressEventArgs> OnLongPressStarted;
        
        /// <summary>Fired while long press is being held.</summary>
        public static event Action<LongPressEventArgs> OnLongPressHeld;
        
        /// <summary>Fired when long press is released.</summary>
        public static event Action<LongPressEventArgs> OnLongPressReleased;
        
        #endregion

        #region Event Trigger Methods
        
        public static void TriggerMovementInput(MovementInputEventArgs args) => OnMovementInput?.Invoke(args);
        public static void TriggerMovementStarted(MovementInputEventArgs args) => OnMovementStarted?.Invoke(args);
        public static void TriggerMovementStopped(MovementInputEventArgs args) => OnMovementStopped?.Invoke(args);
        
        public static void TriggerActionPressed(ActionInputEventArgs args) => OnActionPressed?.Invoke(args);
        public static void TriggerActionReleased(ActionInputEventArgs args) => OnActionReleased?.Invoke(args);
        public static void TriggerAttackAction(ActionInputEventArgs args) => OnAttackAction?.Invoke(args);
        public static void TriggerJumpAction(ActionInputEventArgs args) => OnJumpAction?.Invoke(args);
        public static void TriggerRollAction(ActionInputEventArgs args) => OnRollAction?.Invoke(args);
        public static void TriggerInteractAction(ActionInputEventArgs args) => OnInteractAction?.Invoke(args);
        
        public static void TriggerLookInput(LookInputEventArgs args) => OnLookInput?.Invoke(args);
        
        public static void TriggerInputSystemInitialized() => OnInputSystemInitialized?.Invoke();
        public static void TriggerConfigurationChanged(InputConfigurationChangedEventArgs args) => OnConfigurationChanged?.Invoke(args);
        public static void TriggerDeviceConnected(InputDeviceEventArgs args) => OnDeviceConnected?.Invoke(args);
        public static void TriggerDeviceDisconnected(InputDeviceEventArgs args) => OnDeviceDisconnected?.Invoke(args);
        
        public static void TriggerLongPressStarted(LongPressEventArgs args) => OnLongPressStarted?.Invoke(args);
        public static void TriggerLongPressHeld(LongPressEventArgs args) => OnLongPressHeld?.Invoke(args);
        public static void TriggerLongPressReleased(LongPressEventArgs args) => OnLongPressReleased?.Invoke(args);
        
        #endregion

        #region Cleanup
        
        /// <summary>Clears all event subscriptions. Call this when shutting down.</summary>
        public static void ClearAllEvents()
        {
            OnMovementInput = null;
            OnMovementStarted = null;
            OnMovementStopped = null;
            
            OnActionPressed = null;
            OnActionReleased = null;
            OnAttackAction = null;
            OnJumpAction = null;
            OnRollAction = null;
            OnInteractAction = null;
            
            OnLookInput = null;
            
            OnInputSystemInitialized = null;
            OnConfigurationChanged = null;
            OnDeviceConnected = null;
            OnDeviceDisconnected = null;
            
            OnLongPressStarted = null;
            OnLongPressHeld = null;
            OnLongPressReleased = null;
        }
        
        #endregion
    }

    #region Event Arguments Structures

    /// <summary>Movement input event arguments.</summary>
    public struct MovementInputEventArgs
    {
        public float2 Direction;
        public float Magnitude;
        public double Timestamp;
        public string DeviceName;
    }

    /// <summary>Action input event arguments.</summary>
    public struct ActionInputEventArgs
    {
        public string ActionName;
        public bool IsPressed;
        public double Timestamp;
        public string DeviceName;
        public float Value;
    }

    /// <summary>Look input event arguments.</summary>
    public struct LookInputEventArgs
    {
        public float3 Direction;
        public float2 Delta;
        public double Timestamp;
        public string DeviceName;
    }

    /// <summary>Input configuration changed event arguments.</summary>
    public struct InputConfigurationChangedEventArgs
    {
        public string ConfigurationName;
        public IInputConfiguration OldConfiguration;
        public IInputConfiguration NewConfiguration;
        public double Timestamp;
    }

    /// <summary>Input device event arguments.</summary>
    public struct InputDeviceEventArgs
    {
        public string DeviceName;
        public string DeviceType;
        public int DeviceId;
        public double Timestamp;
    }

    /// <summary>Long press event arguments.</summary>
    public struct LongPressEventArgs
    {
        public string ActionName;
        public float PressTime;
        public float ThresholdTime;
        public double Timestamp;
        public string DeviceName;
    }

    #endregion
}