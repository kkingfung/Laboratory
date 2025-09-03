using UnityEngine;
using Laboratory.Core.Input.Interfaces;

namespace Laboratory.Infrastructure.Input
{
    /// <summary>
    /// Input configuration settings for the unified input system.
    /// </summary>
    [System.Serializable]
    public class InputConfiguration : IInputConfiguration
    {
        [Header("Sensitivity Settings")]
        [SerializeField] private float _lookSensitivity = 1.0f;
        [SerializeField] private float _movementSensitivity = 1.0f;
        
        [Header("Deadzone Settings")]
        [SerializeField] private float _inputDeadzone = 0.1f;
        [SerializeField] private float _lookDeadzone = 0.05f;
        
        [Header("Buffer Settings")]
        [SerializeField] private bool _enableInputBuffering = true;
        [SerializeField] private float _inputBufferTime = 0.5f;
        
        [Header("Long Press Settings")]
        [SerializeField] private float _longPressThreshold = 0.5f;
        [SerializeField] private float _longPressRepeatRate = 0.1f;
        
        [Header("Bindings")]
        [SerializeField] private string _serializedBindings = "";
        
        // Properties implementing IInputConfiguration
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
            get => _enableInputBuffering;
            set => _enableInputBuffering = value;
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
        
        // Legacy property names for backward compatibility with Infrastructure layer
        public float inputDeadzone => _inputDeadzone;
        public float lookDeadzone => _lookDeadzone;
        public float lookSensitivity => _lookSensitivity;
        
        public InputConfiguration()
        {
            // Default values already set above
        }
        
        public void ApplySettings()
        {
            // Apply configuration settings to input systems
        }
    }
}
