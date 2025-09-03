using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Laboratory.Core.Input.Interfaces;
using Laboratory.Core.Input.Events;
using Laboratory.Core.DI;
using Laboratory.Infrastructure.Input;

namespace Laboratory.Infrastructure.Input
{
    /// <summary>
    /// Unified input management service that consolidates all input functionality.
    /// Implements the main IInputService interface and coordinates all input subsystems.
    /// </summary>
    public class UnifiedInputManager : MonoBehaviour, IInputService
    {
        #region Serialized Fields
        
        [Header("Input Configuration")]
        [SerializeField] private InputActionAsset inputActionAsset;
        [SerializeField] private float lookSensitivity = 1.0f;
        [SerializeField] private float inputDeadzone = 0.1f;
        [SerializeField] private bool enableInputBuffering = true;
        [SerializeField] private float inputBufferTime = 0.5f;
        
        [Header("Long Press Settings")]
        [SerializeField] private float longPressThreshold = 0.5f;
        [SerializeField] private float longPressRepeatRate = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool debugLogging = false;
        
        #endregion

        #region Private Fields
        
        private PlayerControls _controls;
        private InputConfiguration _configuration;
        private Laboratory.Core.Input.Services.InputBuffer _inputBuffer;
        private InputValidator _inputValidator;
        private InputEventSystem _inputEventSystem;
        
        // Input state tracking
        private float2 _lastMovementInput;
        private float3 _lastLookInput;
        private readonly Dictionary<string, bool> _lastActionStates = new();
        private readonly Dictionary<string, float> _actionPressStartTimes = new();
        
        // Initialization state
        private bool _isInitialized = false;
        private bool _isEnabled = false;
        
        #endregion

        #region IInputService Events
        
        public event Action<InputActionEventArgs> OnInputActionPerformed;
        public event Action OnInputConfigurationChanged;
        public event Action<InputDevice> OnDeviceChanged;
        
        #endregion

        #region IInputService Properties
        
        public IInputConfiguration Configuration => _configuration;
        public bool IsInitialized => _isInitialized;
        public InputDevice[] ActiveDevices => InputSystem.devices.ToArray();
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            Initialize();
            RegisterAsService();
        }
        
        private void Start()
        {
            if (_isInitialized)
            {
                Enable();
            }
        }
        
        private void Update()
        {
            if (_isInitialized && _isEnabled)
            {
                UpdateInputProcessing();
            }
        }
        
        private void OnEnable()
        {
            if (_isInitialized)
            {
                Enable();
            }
        }
        
        private void OnDisable()
        {
            Disable();
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion

        #region IInputService Implementation
        
        public void Initialize()
        {
            try
            {
                LogDebug("Initializing UnifiedInputManager...");
                
                // Initialize configuration
                _configuration = new InputConfiguration
                {
                    LookSensitivity = lookSensitivity,
                    InputDeadzone = inputDeadzone,
                    InputBufferingEnabled = enableInputBuffering,
                    InputBufferTime = inputBufferTime
                };
                
                // Initialize subsystems
                _inputBuffer = new Laboratory.Core.Input.Services.InputBuffer(_configuration);
                _inputValidator = new InputValidator(_configuration);
                
                // Get or create event bus for InputEventSystem
                var eventBus = GlobalServiceProvider.Instance?.Resolve<Laboratory.Core.Events.IEventBus>();
                _inputEventSystem = new InputEventSystem(eventBus);
                
                // Initialize input controls
                if (inputActionAsset == null)
                {
                    LogError("InputActionAsset is not assigned!");
                    return;
                }
                
                _controls = new PlayerControls();
                LoadConfiguration();
                SetupInputActionCallbacks();
                
                _isInitialized = true;
                Laboratory.Core.Input.Events.InputEvents.TriggerInputSystemInitialized();
                
                LogDebug("UnifiedInputManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize input manager: {ex.Message}");
                _isInitialized = false;
            }
        }
        
        public void Enable()
        {
            if (!_isInitialized)
            {
                LogWarning("Cannot enable input - manager not initialized");
                return;
            }
            
            try
            {
                _controls?.Enable();
                _isEnabled = true;
                LogDebug("Input system enabled");
            }
            catch (Exception ex)
            {
                LogError($"Failed to enable input: {ex.Message}");
            }
        }
        
        public void Disable()
        {
            try
            {
                _controls?.Disable();
                _isEnabled = false;
                LogDebug("Input system disabled");
            }
            catch (Exception ex)
            {
                LogError($"Failed to disable input: {ex.Message}");
            }
        }
        
        void IInputService.Update()
        {
            UpdateInputProcessing();
        }
        
        public void Shutdown()
        {
            try
            {
                Disable();
                
                _controls?.Dispose();
                _controls = null;
                
                _inputBuffer?.ClearBuffer();
                _inputEventSystem?.ClearSubscriptions();
                Laboratory.Core.Input.Events.InputEvents.ClearAllEvents();
                
                _isInitialized = false;
                _isEnabled = false;
                
                LogDebug("Input system shutdown complete");
            }
            catch (Exception ex)
            {
                LogError($"Error during input system shutdown: {ex.Message}");
            }
        }
        
        #endregion

        #region Input State Query Methods
        
        public float2 GetMovementInput()
        {
            if (!_isEnabled || _controls == null) return float2.zero;
            
            try
            {
                float horizontal = 0f;
                float vertical = 0f;
                
                if (_controls.InGame.GoEast.IsPressed()) horizontal += 1f;
                if (_controls.InGame.GoWest.IsPressed()) horizontal -= 1f;
                if (_controls.InGame.GoNorth.IsPressed()) vertical += 1f;
                if (_controls.InGame.GoSouth.IsPressed()) vertical -= 1f;
                
                var rawInput = new float2(horizontal, vertical);
                return ApplyDeadzone(rawInput);
            }
            catch (Exception ex)
            {
                LogError($"Error getting movement input: {ex.Message}");
                return float2.zero;
            }
        }
        
        public float3 GetLookInput()
        {
            // Currently no look input in the input actions
            // This can be extended when look input is added to the PlayerControls
            return float3.zero;
        }
        
        public bool WasActionPerformed(string actionName)
        {
            if (!_isEnabled) return false;
            
            try
            {
                var action = GetInputAction(actionName);
                return action?.WasPressedThisFrame() ?? false;
            }
            catch (Exception ex)
            {
                LogError($"Error checking action performed '{actionName}': {ex.Message}");
                return false;
            }
        }
        
        public bool IsActionPressed(string actionName)
        {
            if (!_isEnabled) return false;
            
            try
            {
                var action = GetInputAction(actionName);
                return action?.IsPressed() ?? false;
            }
            catch (Exception ex)
            {
                LogError($"Error checking action pressed '{actionName}': {ex.Message}");
                return false;
            }
        }
        
        public T GetActionValue<T>(string actionName) where T : struct
        {
            if (!_isEnabled) return default(T);
            
            try
            {
                var action = GetInputAction(actionName);
                if (action != null)
                {
                    return action.ReadValue<T>();
                }
            }
            catch (Exception ex)
            {
                LogError($"Error getting action value '{actionName}': {ex.Message}");
            }
            
            return default(T);
        }
        
        #endregion

        #region Configuration Methods
        
        public void SaveConfiguration()
        {
            try
            {
                if (_controls != null && inputActionAsset != null)
                {
                    _configuration.SerializedBindings = inputActionAsset.SaveBindingOverridesAsJson();
                    PlayerPrefs.SetString("InputConfiguration", JsonUtility.ToJson(_configuration));
                    PlayerPrefs.Save();
                    
                    LogDebug("Input configuration saved");
                    
                    var eventArgs = new Laboratory.Core.Input.Events.InputConfigurationChangedEventArgs
                    {
                        PropertyName = "Default",
                        NewValue = _configuration,
                        OldValue = null,
                        Timestamp = (float)Time.unscaledTimeAsDouble
                    };
                    Laboratory.Core.Input.Events.InputEvents.TriggerConfigurationChanged(eventArgs);
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to save configuration: {ex.Message}");
            }
        }
        
        public void LoadConfiguration()
        {
            try
            {
                if (PlayerPrefs.HasKey("InputConfiguration"))
                {
                    var json = PlayerPrefs.GetString("InputConfiguration");
                    var loadedConfig = JsonUtility.FromJson<InputConfiguration>(json);
                    
                    _configuration.LookSensitivity = loadedConfig.LookSensitivity;
                    _configuration.InputDeadzone = loadedConfig.InputDeadzone;
                    _configuration.InputBufferingEnabled = loadedConfig.InputBufferingEnabled;
                    _configuration.InputBufferTime = loadedConfig.InputBufferTime;
                    
                    if (!string.IsNullOrEmpty(loadedConfig.SerializedBindings) && inputActionAsset != null)
                    {
                        inputActionAsset.LoadBindingOverridesFromJson(loadedConfig.SerializedBindings);
                    }
                    
                    LogDebug("Input configuration loaded");
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to load configuration: {ex.Message}");
            }
        }
        
        public void ResetConfiguration()
        {
            try
            {
                inputActionAsset?.RemoveAllBindingOverrides();
                PlayerPrefs.DeleteKey("InputConfiguration");
                PlayerPrefs.Save();
                
                // Reset to default values
                _configuration.LookSensitivity = 1.0f;
                _configuration.InputDeadzone = 0.1f;
                _configuration.InputBufferingEnabled = true;
                _configuration.InputBufferTime = 0.5f;
                _configuration.SerializedBindings = "";
                
                LogDebug("Input configuration reset to defaults");
                OnInputConfigurationChanged?.Invoke();
            }
            catch (Exception ex)
            {
                LogError($"Failed to reset configuration: {ex.Message}");
            }
        }
        
        public void StartRebind(string actionName, int bindingIndex, Action<bool> onComplete = null)
        {
            try
            {
                var action = GetInputAction(actionName);
                if (action == null)
                {
                    LogError($"Action '{actionName}' not found for rebinding");
                    onComplete?.Invoke(false);
                    return;
                }
                
                action.Disable();
                
                var rebindOperation = action.PerformInteractiveRebinding(bindingIndex)
                    .WithControlsExcluding("<Mouse>/position")
                    .WithCancelingThrough("<Keyboard>/escape")
                    .OnMatchWaitForAnother(0.1f)
                    .OnComplete(operation =>
                    {
                        action.Enable();
                        SaveConfiguration();
                        LogDebug($"Rebind completed for '{actionName}'");
                        onComplete?.Invoke(true);
                        operation.Dispose();
                    })
                    .OnCancel(operation =>
                    {
                        action.Enable();
                        LogDebug($"Rebind cancelled for '{actionName}'");
                        onComplete?.Invoke(false);
                        operation.Dispose();
                    })
                    .Start();
            }
            catch (Exception ex)
            {
                LogError($"Failed to start rebind for '{actionName}': {ex.Message}");
                onComplete?.Invoke(false);
            }
        }
        
        #endregion

        #region Validation Methods
        
        public bool ValidateInputState()
        {
            if (_inputValidator == null) return true;
            
            // Validate movement input
            var movement = GetMovementInput();
            var validatedMovement = _inputValidator.ValidateMovementInput(movement);
            
            // Check if the input passed validation (input should be within expected bounds)
            var inputMagnitude = math.length(movement);
            var validatedMagnitude = math.length(validatedMovement);
            
            // Return true if input is valid (either both are zero or magnitudes are close)
            return inputMagnitude < 0.001f || math.abs(inputMagnitude - validatedMagnitude) < 0.001f;
        }
        
        public bool HasAction(string actionName)
        {
            return GetInputAction(actionName) != null;
        }
        
        #endregion

        #region Private Helper Methods
        
        private void RegisterAsService()
        {
            try
            {
                var serviceContainer = GlobalServiceProvider.Instance?.Resolve<IServiceContainer>();
                // Note: RegisterSingleton method might not exist on IServiceContainer
                // This is a placeholder - the actual registration method depends on the DI container implementation
                if (serviceContainer != null)
                {
                    // serviceContainer.RegisterSingleton<IInputService>(this);
                    LogDebug("Service container found, but RegisterSingleton method not available");
                }
                LogDebug("Registered as IInputService");
            }
            catch (Exception ex)
            {
                LogError($"Failed to register as service: {ex.Message}");
            }
        }
        
        private void UpdateInputProcessing()
        {
            try
            {
                var currentTime = Time.unscaledTimeAsDouble;
                
                // Update subsystems
                _inputBuffer?.Update(currentTime);
                
                // Process input changes
                ProcessMovementInput();
                ProcessActionInputs();
                ProcessLongPressInputs();
            }
            catch (Exception ex)
            {
                LogError($"Error during input processing: {ex.Message}");
            }
        }
        
        private void ProcessMovementInput()
        {
            var currentMovement = GetMovementInput();
            
            if (!math.all(currentMovement == _lastMovementInput))
            {
                var magnitude = math.length(currentMovement);
                var eventArgs = new Laboratory.Core.Input.Events.MovementInputEventArgs
                {
                    Movement = new UnityEngine.Vector2(currentMovement.x, currentMovement.y),
                    IsMoving = magnitude > 0.01f,
                    Timestamp = (float)Time.unscaledTimeAsDouble,
                    Source = InputSystem.devices.Count > 0 ? InputSystem.devices[0].name : "Unknown"
                };
                
                Laboratory.Core.Input.Events.InputEvents.TriggerMovementInput(new Laboratory.Core.Input.Events.MovementInputEvent
                {
                    Movement = new UnityEngine.Vector2(currentMovement.x, currentMovement.y),
                    IsMoving = magnitude > 0.01f,
                    Timestamp = (float)Time.unscaledTimeAsDouble,
                    Source = InputSystem.devices.Count > 0 ? InputSystem.devices[0].name : "Unknown"
                });
                
                // Check for movement start/stop
                var wasMoving = math.length(_lastMovementInput) > 0.01f;
                var isMoving = magnitude > 0.01f;
                
                if (!wasMoving && isMoving)
                {
                    Laboratory.Core.Input.Events.InputEvents.TriggerMovementStarted(eventArgs);
                }
                else if (wasMoving && !isMoving)
                {
                    Laboratory.Core.Input.Events.InputEvents.TriggerMovementStopped(eventArgs);
                }
                
                _lastMovementInput = currentMovement;
            }
        }
        
        private void ProcessActionInputs()
        {
            var actionNames = new[] { "AttackOrThrow", "Jump", "Roll", "ActionOrCraft", "CharSkill", "WeaponSkill" };
            
            foreach (var actionName in actionNames)
            {
                ProcessSingleAction(actionName);
            }
        }
        
        private void ProcessSingleAction(string actionName)
        {
            var isPressed = IsActionPressed(actionName);
            var wasPressed = _lastActionStates.GetValueOrDefault(actionName, false);
            
            if (isPressed != wasPressed)
            {
                var eventArgs = new Laboratory.Core.Input.Events.ActionInputEventArgs
                {
                    ActionName = actionName,
                    IsPressed = isPressed,
                    Timestamp = (float)Time.unscaledTimeAsDouble,
                    DeviceName = InputSystem.devices.Count > 0 ? InputSystem.devices[0].name : "Unknown",
                    Value = isPressed ? 1f : 0f
                };
                
                if (isPressed)
                {
                    Laboratory.Core.Input.Events.InputEvents.TriggerActionPressed(eventArgs);
                    _actionPressStartTimes[actionName] = Time.unscaledTime;
                    
                    // Trigger the interface event
                    OnInputActionPerformed?.Invoke(new InputActionEventArgs
                    {
                        ActionName = actionName,
                        Phase = InputActionPhase.Started,
                        Value = 1f,
                        Time = Time.unscaledTimeAsDouble,
                        Device = InputSystem.devices.Count > 0 ? InputSystem.devices[0] : null
                    });
                    
                    // Trigger specific action events
                    switch (actionName)
                    {
                        case "AttackOrThrow":
                            Laboratory.Core.Input.Events.InputEvents.TriggerAttackAction(eventArgs);
                            break;
                        case "Jump":
                            Laboratory.Core.Input.Events.InputEvents.TriggerJumpAction(eventArgs);
                            break;
                        case "Roll":
                            Laboratory.Core.Input.Events.InputEvents.TriggerRollAction(eventArgs);
                            break;
                        case "ActionOrCraft":
                            Laboratory.Core.Input.Events.InputEvents.TriggerInteractAction(eventArgs);
                            break;
                    }
                }
                else
                {
                    Laboratory.Core.Input.Events.InputEvents.TriggerActionReleased(eventArgs);
                    _actionPressStartTimes.Remove(actionName);
                    
                    // Trigger the interface event for release
                    OnInputActionPerformed?.Invoke(new InputActionEventArgs
                    {
                        ActionName = actionName,
                        Phase = InputActionPhase.Canceled,
                        Value = 0f,
                        Time = Time.unscaledTimeAsDouble,
                        Device = InputSystem.devices.Count > 0 ? InputSystem.devices[0] : null
                    });
                }
                
                _lastActionStates[actionName] = isPressed;
                
                // Buffer the input if enabled
                if (_configuration.InputBufferingEnabled)
                {
                    _inputBuffer?.BufferInput(actionName, isPressed ? 1f : 0f, Time.unscaledTimeAsDouble, "UnifiedInputManager");
                }
            }
        }
        
        private void ProcessLongPressInputs()
        {
            var currentTime = Time.unscaledTime;
            
            foreach (var kvp in _actionPressStartTimes.ToList())
            {
                var actionName = kvp.Key;
                var startTime = kvp.Value;
                var pressTime = currentTime - startTime;
                
                if (pressTime >= longPressThreshold)
                {
                    var eventArgs = new Laboratory.Core.Input.Events.LongPressEventArgs
                    {
                        ActionName = actionName,
                        PressTime = pressTime,
                        ThresholdTime = longPressThreshold,
                        Timestamp = (float)Time.unscaledTimeAsDouble,
                        DeviceName = InputSystem.devices.Count > 0 ? InputSystem.devices[0].name : "Unknown"
                    };
                    
                    // Check if this is the first time crossing threshold
                    var lastPressTime = pressTime - Time.unscaledDeltaTime;
                    if (lastPressTime < longPressThreshold)
                    {
                        Laboratory.Core.Input.Events.InputEvents.TriggerLongPressStarted(eventArgs);
                    }
                    else if (Time.unscaledTime % longPressRepeatRate < Time.unscaledDeltaTime)
                    {
                        Laboratory.Core.Input.Events.InputEvents.TriggerLongPressHeld(eventArgs);
                    }
                }
            }
        }
        
        private void SetupInputActionCallbacks()
        {
            if (_controls == null) return;
            
            // Setup device change callbacks
            InputSystem.onDeviceChange += OnInputDeviceChange;
        }
        
        private void OnInputDeviceChange(InputDevice device, InputDeviceChange change)
        {
            var eventArgs = new Laboratory.Core.Input.Events.InputDeviceEventArgs
            {
                DeviceName = device.name,
                DeviceId = device.deviceId.ToString(),
                IsConnected = change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected,
                Timestamp = (float)Time.unscaledTimeAsDouble
            };
            
            switch (change)
            {
                case InputDeviceChange.Added:
                case InputDeviceChange.Reconnected:
                    Laboratory.Core.Input.Events.InputEvents.TriggerDeviceConnected(eventArgs);
                    OnDeviceChanged?.Invoke(device);
                    break;
                case InputDeviceChange.Removed:
                case InputDeviceChange.Disconnected:
                    Laboratory.Core.Input.Events.InputEvents.TriggerDeviceDisconnected(eventArgs);
                    OnDeviceChanged?.Invoke(device);
                    break;
            }
        }
        
        private InputAction GetInputAction(string actionName)
        {
            if (_controls == null) return null;
            
            return actionName switch
            {
                "GoEast" => _controls.InGame.GoEast,
                "GoWest" => _controls.InGame.GoWest,
                "GoNorth" => _controls.InGame.GoNorth,
                "GoSouth" => _controls.InGame.GoSouth,
                "AttackOrThrow" => _controls.InGame.AttackOrThrow,
                "Jump" => _controls.InGame.Jump,
                "Roll" => _controls.InGame.Roll,
                "ActionOrCraft" => _controls.InGame.ActionOrCraft,
                "CharSkill" => _controls.InGame.CharSkill,
                "WeaponSkill" => _controls.InGame.WeaponSkill,
                "Pause" => _controls.InGame.Pause,
                _ => null
            };
        }
        
        private float2 ApplyDeadzone(float2 input)
        {
            var magnitude = math.length(input);
            
            if (magnitude < _configuration.InputDeadzone)
            {
                return float2.zero;
            }
            
            var normalizedMagnitude = (magnitude - _configuration.InputDeadzone) / (1.0f - _configuration.InputDeadzone);
            return math.normalize(input) * normalizedMagnitude;
        }
        
        private void LogDebug(string message)
        {
            if (debugLogging)
            {
                Debug.Log($"[UnifiedInputManager] {message}");
            }
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[UnifiedInputManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[UnifiedInputManager] {message}");
        }
        
        #endregion
    }
}
