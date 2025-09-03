using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Laboratory.Core.Input.Interfaces;

namespace Laboratory.Core.Input.Services
{
    /// <summary>
    /// Concrete implementation of IInputConfiguration.
    /// Stores all input-related configuration settings.
    /// </summary>
    [Serializable]
    public class InputConfiguration : IInputConfiguration
    {
        [SerializeField] private float _lookSensitivity = 1.0f;
        [SerializeField] private float _movementSensitivity = 1.0f;
        [SerializeField] private float _inputDeadzone = 0.1f;
        [SerializeField] private float _lookDeadzone = 0.05f;
        [SerializeField] private bool _inputBufferingEnabled = true;
        [SerializeField] private float _inputBufferTime = 0.5f;
        [SerializeField] private float _longPressThreshold = 0.5f;
        [SerializeField] private float _longPressRepeatRate = 0.1f;
        [SerializeField] private string _serializedBindings = "";

        public float LookSensitivity
        {
            get => _lookSensitivity;
            set => _lookSensitivity = Mathf.Clamp(value, 0.1f, 10.0f);
        }
        
        public float MovementSensitivity
        {
            get => _movementSensitivity;
            set => _movementSensitivity = Mathf.Clamp(value, 0.1f, 5.0f);
        }

        public float InputDeadzone
        {
            get => _inputDeadzone;
            set => _inputDeadzone = Mathf.Clamp01(value);
        }
        
        public float LookDeadzone
        {
            get => _lookDeadzone;
            set => _lookDeadzone = Mathf.Clamp01(value);
        }

        public bool InputBufferingEnabled
        {
            get => _inputBufferingEnabled;
            set => _inputBufferingEnabled = value;
        }

        public float InputBufferTime
        {
            get => _inputBufferTime;
            set => _inputBufferTime = Mathf.Clamp(value, 0.1f, 2.0f);
        }
        
        public float LongPressThreshold
        {
            get => _longPressThreshold;
            set => _longPressThreshold = Mathf.Clamp(value, 0.1f, 3.0f);
        }
        
        public float LongPressRepeatRate
        {
            get => _longPressRepeatRate;
            set => _longPressRepeatRate = Mathf.Clamp(value, 0.05f, 1.0f);
        }

        public string SerializedBindings
        {
            get => _serializedBindings;
            set => _serializedBindings = value ?? "";
        }
        
        /// <summary>
        /// Apply configuration settings to input systems.
        /// </summary>
        public void ApplySettings()
        {
            // Apply settings to Unity Input System
            if (UnityEngine.InputSystem.InputSystem.settings != null)
            {
                // Configure input system settings if needed
                // This is where you would apply global input settings
            }
        }
        
        #region Editor/Runtime Setters
        
        public void SetLookSensitivity(float value)
        {
            _lookSensitivity = Mathf.Clamp(value, 0.1f, 10.0f);
        }
        
        public void SetMovementSensitivity(float value)
        {
            _movementSensitivity = Mathf.Clamp(value, 0.1f, 5.0f);
        }

        public void SetInputDeadzone(float value)
        {
            _inputDeadzone = Mathf.Clamp01(value);
        }
        
        public void SetLookDeadzone(float value)
        {
            _lookDeadzone = Mathf.Clamp01(value);
        }

        public void SetInputBufferingEnabled(bool value)
        {
            _inputBufferingEnabled = value;
        }

        public void SetInputBufferTime(float value)
        {
            _inputBufferTime = Mathf.Clamp(value, 0.1f, 2.0f);
        }
        
        public void SetLongPressThreshold(float value)
        {
            _longPressThreshold = Mathf.Clamp(value, 0.1f, 3.0f);
        }
        
        public void SetLongPressRepeatRate(float value)
        {
            _longPressRepeatRate = Mathf.Clamp(value, 0.05f, 1.0f);
        }

        public void SetSerializedBindings(string value)
        {
            _serializedBindings = value ?? "";
        }
        
        #endregion
    }

    /// <summary>
    /// Concrete implementation of IInputBuffer.
    /// Manages buffered input events with time-based expiration.
    /// </summary>
    public class InputBuffer : IInputBuffer
    {
        private readonly List<BufferedInput> _bufferedInputs = new();
        private readonly IInputConfiguration _configuration;

        public float BufferTime
        {
            get => _configuration?.InputBufferTime ?? 0.5f;
            set
            {
                if (_configuration is InputConfiguration config)
                    config.SetInputBufferTime(value);
            }
        }

        public bool IsEnabled
        {
            get => _configuration?.InputBufferingEnabled ?? false;
            set
            {
                if (_configuration is InputConfiguration config)
                    config.SetInputBufferingEnabled(value);
            }
        }

        public int BufferedInputCount => _bufferedInputs.Count(x => !x.IsConsumed);

        public InputBuffer(IInputConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public void BufferInput(string actionName, object value, double timestamp)
        {
            if (!IsEnabled || string.IsNullOrEmpty(actionName)) return;

            var bufferedInput = new BufferedInput
            {
                ActionName = actionName,
                Value = value,
                Timestamp = timestamp,
                IsConsumed = false
            };

            _bufferedInputs.Add(bufferedInput);
        }
        
        /// <summary>
        /// Overload for compatibility with 4-parameter calls.
        /// </summary>
        public void BufferInput(string actionName, object value, double timestamp, string source)
        {
            // For now, ignore the source parameter and use the 3-parameter version
            BufferInput(actionName, value, timestamp);
        }

        public bool WasInputBuffered(string actionName, double withinTime)
        {
            if (!IsEnabled || string.IsNullOrEmpty(actionName)) return false;

            return _bufferedInputs.Any(input =>
                input.ActionName == actionName &&
                !input.IsConsumed &&
                withinTime - input.Timestamp <= BufferTime);
        }

        public bool ConsumeBufferedInput(string actionName)
        {
            if (!IsEnabled || string.IsNullOrEmpty(actionName)) return false;

            var input = _bufferedInputs.FirstOrDefault(x =>
                x.ActionName == actionName && !x.IsConsumed);

            if (input.ActionName != null)
            {
                var index = _bufferedInputs.FindIndex(x =>
                    x.ActionName == input.ActionName &&
                    x.Timestamp == input.Timestamp &&
                    !x.IsConsumed);

                if (index >= 0)
                {
                    var modifiedInput = _bufferedInputs[index];
                    modifiedInput.IsConsumed = true;
                    _bufferedInputs[index] = modifiedInput;
                    return true;
                }
            }

            return false;
        }

        public void ClearBuffer()
        {
            _bufferedInputs.Clear();
        }

        public void Update(double currentTime)
        {
            if (!IsEnabled) return;

            // Remove expired inputs
            _bufferedInputs.RemoveAll(input =>
                currentTime - input.Timestamp > BufferTime);
        }

        public T GetBufferedValue<T>(string actionName) where T : struct
        {
            if (!IsEnabled || string.IsNullOrEmpty(actionName)) return default(T);

            var input = _bufferedInputs
                .Where(x => x.ActionName == actionName && !x.IsConsumed)
                .OrderByDescending(x => x.Timestamp)
                .FirstOrDefault();

            if (input.ActionName != null && input.Value is T value)
            {
                return value;
            }

            return default(T);
        }

        public BufferedInput[] GetBufferedInputs(string actionName)
        {
            if (!IsEnabled || string.IsNullOrEmpty(actionName))
                return Array.Empty<BufferedInput>();

            return _bufferedInputs
                .Where(x => x.ActionName == actionName && !x.IsConsumed)
                .OrderByDescending(x => x.Timestamp)
                .ToArray();
        }
    }

    /// <summary>
    /// Concrete implementation of IInputValidator.
    /// Validates input values and context-based input permissions.
    /// </summary>
    public class InputValidator : IInputValidator
    {
        private InputValidationRules _rules = new()
        {
            MaxMovementMagnitude = 1.0f,
            MaxLookSensitivity = 10.0f,
            DisallowedActions = Array.Empty<string>(),
            RequireNormalizedMovement = false
        };

        public bool IsActionAllowed(string actionName)
        {
            if (string.IsNullOrEmpty(actionName)) return false;

            return !_rules.DisallowedActions.Contains(actionName);
        }

        public bool ValidateInputValue<T>(T value) where T : struct
        {
            // Basic type validation
            if (value is float floatValue)
            {
                return !float.IsNaN(floatValue) && !float.IsInfinity(floatValue);
            }

            if (value is Vector2 vector2Value)
            {
                return !float.IsNaN(vector2Value.x) && !float.IsNaN(vector2Value.y) &&
                       !float.IsInfinity(vector2Value.x) && !float.IsInfinity(vector2Value.y);
            }

            if (value is Vector3 vector3Value)
            {
                return !float.IsNaN(vector3Value.x) && !float.IsNaN(vector3Value.y) && !float.IsNaN(vector3Value.z) &&
                       !float.IsInfinity(vector3Value.x) && !float.IsInfinity(vector3Value.y) && !float.IsInfinity(vector3Value.z);
            }

            return true; // Default to valid for other types
        }

        public bool ValidateMovementInput(Unity.Mathematics.float2 movement)
        {
            if (!ValidateInputValue(new Vector2(movement.x, movement.y))) return false;

            var magnitude = Unity.Mathematics.math.length(movement);

            if (magnitude > _rules.MaxMovementMagnitude) return false;

            if (_rules.RequireNormalizedMovement && magnitude > 1.01f) return false;

            return true;
        }

        public bool ValidateLookInput(Unity.Mathematics.float3 look)
        {
            return ValidateInputValue(new Vector3(look.x, look.y, look.z));
        }

        public void SetValidationRules(InputValidationRules rules)
        {
            _rules = rules;
        }
    }

    /// <summary>
    /// Concrete implementation of IInputEventSystem.
    /// Manages event subscriptions and publishing for input actions.
    /// </summary>
    public class InputEventSystem : IInputEventSystem
    {
        private readonly Dictionary<string, List<Action<InputActionEventArgs>>> _subscriptions = new();

        public void Subscribe(string actionName, Action<InputActionEventArgs> callback)
        {
            if (string.IsNullOrEmpty(actionName) || callback == null) return;

            if (!_subscriptions.ContainsKey(actionName))
            {
                _subscriptions[actionName] = new List<Action<InputActionEventArgs>>();
            }

            if (!_subscriptions[actionName].Contains(callback))
            {
                _subscriptions[actionName].Add(callback);
            }
        }

        public void Unsubscribe(string actionName, Action<InputActionEventArgs> callback)
        {
            if (string.IsNullOrEmpty(actionName) || callback == null) return;

            if (_subscriptions.ContainsKey(actionName))
            {
                _subscriptions[actionName].Remove(callback);

                // Clean up empty lists
                if (_subscriptions[actionName].Count == 0)
                {
                    _subscriptions.Remove(actionName);
                }
            }
        }

        public void PublishEvent(InputActionEventArgs eventArgs)
        {
            if (string.IsNullOrEmpty(eventArgs.ActionName)) return;

            if (_subscriptions.TryGetValue(eventArgs.ActionName, out var callbacks))
            {
                // Create a copy to avoid issues with concurrent modification
                var callbacksCopy = callbacks.ToArray();

                foreach (var callback in callbacksCopy)
                {
                    try
                    {
                        callback.Invoke(eventArgs);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error invoking input event callback for '{eventArgs.ActionName}': {ex.Message}");
                    }
                }
            }
        }

        public void ClearSubscriptions()
        {
            _subscriptions.Clear();
        }
    }
}