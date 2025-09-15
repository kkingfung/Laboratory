using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    /// Player controls implementation using Unity's Input System.
    /// Integrates with the actual input action assets and provides proper event handling.
    /// </summary>
    public class PlayerControls : IDisposable
    {
        private Laboratory.Infrastructure.Input.PlayerControls _inputActions;
        private bool _disposed = false;
        
        public MiniMapControls MiniMap { get; private set; }
        
        public PlayerControls()
        {
            // Initialize the actual input system
            _inputActions = new Laboratory.Infrastructure.Input.PlayerControls();
            MiniMap = new MiniMapControls(_inputActions);
        }
        
        public void Enable() 
        {
            if (_disposed) return;
            
            try
            {
                _inputActions?.Enable();
                MiniMap?.Enable();
                Debug.Log("[UI.InputSystem] Player controls enabled successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UI.InputSystem] Failed to enable input actions: {ex.Message}");
            }
        }
        
        public void Disable() 
        {
            if (_disposed) return;
            
            try
            {
                _inputActions?.Disable();
                MiniMap?.Disable();
                Debug.Log("[UI.InputSystem] Player controls disabled successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UI.InputSystem] Failed to disable input actions: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                Disable();
                _inputActions?.Dispose();
                MiniMap?.Dispose();
                _inputActions = null;
                MiniMap = null;
                _disposed = true;
                Debug.Log("[UI.InputSystem] Player controls disposed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UI.InputSystem] Error during disposal: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Gets the underlying input actions for advanced usage.
        /// </summary>
        public Laboratory.Infrastructure.Input.PlayerControls GetInputActions()
        {
            return _inputActions;
        }
        
        /// <summary>
        /// Checks if the controls are currently enabled.
        /// </summary>
        public bool IsEnabled => _inputActions != null && !_disposed;
    }

    /// <summary>
    /// Minimap-specific input controls with proper Input System integration.
    /// Provides zoom, pan, and click functionality for minimap UI components.
    /// </summary>
    public class MiniMapControls : IDisposable
    {
        private Laboratory.Infrastructure.Input.PlayerControls _playerControls;
        private bool _disposed = false;
        private bool _enabled = false;
        
        public InputAction Zoom { get; private set; }
        public InputAction Pan { get; private set; }
        public InputAction Click { get; private set; }
        
        public MiniMapControls(Laboratory.Infrastructure.Input.PlayerControls playerControls)
        {
            _playerControls = playerControls;
            
            // Create input actions for minimap functionality
            Zoom = new InputAction("Zoom", _playerControls);
            Pan = new InputAction("Pan", _playerControls);
            Click = new InputAction("Click", _playerControls);
            
            InitializeActions();
        }
        
        private void InitializeActions()
        {
            if (_playerControls == null) return;
            
            try
            {
                // Set up zoom action (mouse scroll wheel)
                Zoom.SetupScrollWheelBinding();
                
                // Set up pan action (middle mouse button drag)
                Pan.SetupMouseDragBinding();
                
                // Set up click action (left mouse button)
                Click.SetupMouseClickBinding();
                
                Debug.Log("[MiniMapControls] Input actions initialized successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MiniMapControls] Failed to initialize actions: {ex.Message}");
            }
        }
        
        public void Enable()
        {
            if (_disposed || _enabled) return;
            
            try
            {
                Zoom?.Enable();
                Pan?.Enable();
                Click?.Enable();
                _enabled = true;
                Debug.Log("[MiniMapControls] Minimap controls enabled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MiniMapControls] Failed to enable: {ex.Message}");
            }
        }
        
        public void Disable()
        {
            if (_disposed || !_enabled) return;
            
            try
            {
                Zoom?.Disable();
                Pan?.Disable();
                Click?.Disable();
                _enabled = false;
                Debug.Log("[MiniMapControls] Minimap controls disabled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MiniMapControls] Failed to disable: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                Disable();
                Zoom?.Dispose();
                Pan?.Dispose();
                Click?.Dispose();
                
                Zoom = null;
                Pan = null;
                Click = null;
                _playerControls = null;
                _disposed = true;
                
                Debug.Log("[MiniMapControls] Minimap controls disposed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MiniMapControls] Error during disposal: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Checks if the minimap controls are currently enabled.
        /// </summary>
        public bool IsEnabled => _enabled && !_disposed;
    }

    /// <summary>
    /// Input action implementation that wraps Unity's Input System functionality.
    /// Provides a simplified interface for UI input handling with proper event management.
    /// </summary>
    public class InputAction : IDisposable
    {
        private readonly string _name;
        private Laboratory.Infrastructure.Input.PlayerControls _playerControls;
        private bool _disposed = false;
        private bool _enabled = false;
        
        // Input System action references
        private UnityEngine.InputSystem.InputAction _mouseScrollAction;
        private UnityEngine.InputSystem.InputAction _mouseDragAction;
        private UnityEngine.InputSystem.InputAction _mouseClickAction;
        
        public string Name => _name;
        
        // Event callbacks
        public System.Action<InputContext> performed;
        public System.Action<InputContext> started;
        public System.Action<InputContext> canceled;
        
        public InputAction(string name, Laboratory.Infrastructure.Input.PlayerControls playerControls = null)
        {
            _name = name;
            _playerControls = playerControls;
        }
        
        /// <summary>
        /// Sets up mouse scroll wheel binding for zoom functionality.
        /// </summary>
        public void SetupScrollWheelBinding()
        {
            try
            {
                // Create a custom input action for mouse scroll
                var actionMap = new UnityEngine.InputSystem.InputActionMap("MiniMapZoom");
                _mouseScrollAction = actionMap.AddAction("Zoom", UnityEngine.InputSystem.InputActionType.Value);
                _mouseScrollAction.AddBinding("<Mouse>/scroll/y");
                
                // Register callbacks
                _mouseScrollAction.performed += OnScrollPerformed;
                _mouseScrollAction.started += OnScrollStarted;
                _mouseScrollAction.canceled += OnScrollCanceled;
                
                Debug.Log($"[InputAction] Scroll wheel binding setup for {_name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputAction] Failed to setup scroll binding: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets up mouse drag binding for pan functionality.
        /// </summary>
        public void SetupMouseDragBinding()
        {
            try
            {
                // Create a custom input action for mouse drag
                var actionMap = new UnityEngine.InputSystem.InputActionMap("MiniMapPan");
                _mouseDragAction = actionMap.AddAction("Pan", UnityEngine.InputSystem.InputActionType.Value);
                _mouseDragAction.AddBinding("<Mouse>/delta");
                
                // Register callbacks
                _mouseDragAction.performed += OnDragPerformed;
                _mouseDragAction.started += OnDragStarted;
                _mouseDragAction.canceled += OnDragCanceled;
                
                Debug.Log($"[InputAction] Mouse drag binding setup for {_name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputAction] Failed to setup drag binding: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Sets up mouse click binding for click functionality.
        /// </summary>
        public void SetupMouseClickBinding()
        {
            try
            {
                // Create a custom input action for mouse click
                var actionMap = new UnityEngine.InputSystem.InputActionMap("MiniMapClick");
                _mouseClickAction = actionMap.AddAction("Click", UnityEngine.InputSystem.InputActionType.Button);
                _mouseClickAction.AddBinding("<Mouse>/leftButton");
                
                // Register callbacks
                _mouseClickAction.performed += OnClickPerformed;
                _mouseClickAction.started += OnClickStarted;
                _mouseClickAction.canceled += OnClickCanceled;
                
                Debug.Log($"[InputAction] Mouse click binding setup for {_name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputAction] Failed to setup click binding: {ex.Message}");
            }
        }
        
        public void Enable()
        {
            if (_disposed || _enabled) return;
            
            try
            {
                _mouseScrollAction?.Enable();
                _mouseDragAction?.Enable();
                _mouseClickAction?.Enable();
                _enabled = true;
                Debug.Log($"[InputAction] {_name} action enabled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputAction] Failed to enable {_name}: {ex.Message}");
            }
        }
        
        public void Disable()
        {
            if (_disposed || !_enabled) return;
            
            try
            {
                _mouseScrollAction?.Disable();
                _mouseDragAction?.Disable();
                _mouseClickAction?.Disable();
                _enabled = false;
                Debug.Log($"[InputAction] {_name} action disabled");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputAction] Failed to disable {_name}: {ex.Message}");
            }
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            try
            {
                Disable();
                
                // Unregister callbacks
                if (_mouseScrollAction != null)
                {
                    _mouseScrollAction.performed -= OnScrollPerformed;
                    _mouseScrollAction.started -= OnScrollStarted;
                    _mouseScrollAction.canceled -= OnScrollCanceled;
                    _mouseScrollAction.Dispose();
                }
                
                if (_mouseDragAction != null)
                {
                    _mouseDragAction.performed -= OnDragPerformed;
                    _mouseDragAction.started -= OnDragStarted;
                    _mouseDragAction.canceled -= OnDragCanceled;
                    _mouseDragAction.Dispose();
                }
                
                if (_mouseClickAction != null)
                {
                    _mouseClickAction.performed -= OnClickPerformed;
                    _mouseClickAction.started -= OnClickStarted;
                    _mouseClickAction.canceled -= OnClickCanceled;
                    _mouseClickAction.Dispose();
                }
                
                // Clear event callbacks
                performed = null;
                started = null;
                canceled = null;
                
                _playerControls = null;
                _disposed = true;
                
                Debug.Log($"[InputAction] {_name} action disposed");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InputAction] Error disposing {_name}: {ex.Message}");
            }
        }
        
        // Event handlers
        private void OnScrollPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            performed?.Invoke(inputContext);
        }
        
        private void OnScrollStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            started?.Invoke(inputContext);
        }
        
        private void OnScrollCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            canceled?.Invoke(inputContext);
        }
        
        private void OnDragPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            performed?.Invoke(inputContext);
        }
        
        private void OnDragStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            started?.Invoke(inputContext);
        }
        
        private void OnDragCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            canceled?.Invoke(inputContext);
        }
        
        private void OnClickPerformed(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            performed?.Invoke(inputContext);
        }
        
        private void OnClickStarted(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            started?.Invoke(inputContext);
        }
        
        private void OnClickCanceled(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            var inputContext = new InputContext(context);
            canceled?.Invoke(inputContext);
        }
        
        /// <summary>
        /// Checks if the action is currently enabled.
        /// </summary>
        public bool IsEnabled => _enabled && !_disposed;
    }

    /// <summary>
    /// Input context wrapper that provides access to Unity's Input System callback context.
    /// Simplifies value reading and provides type-safe access to input data.
    /// </summary>
    public class InputContext
    {
        private readonly UnityEngine.InputSystem.InputAction.CallbackContext _context;
        
        public InputContext(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Reads the current value from the input action.
        /// </summary>
        /// <typeparam name="T">The type of value to read</typeparam>
        /// <returns>The input value, or default(T) if reading fails</returns>
        public T ReadValue<T>() where T : struct
        {
            try
            {
                return _context.ReadValue<T>();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[InputContext] Failed to read value of type {typeof(T).Name}: {ex.Message}");
                return default(T);
            }
        }
        
        /// <summary>
        /// Reads the current value as a float.
        /// </summary>
        /// <returns>Float value or 0f if reading fails</returns>
        public float ReadFloatValue()
        {
            return ReadValue<float>();
        }
        
        /// <summary>
        /// Reads the current value as a Vector2.
        /// </summary>
        /// <returns>Vector2 value or Vector2.zero if reading fails</returns>
        public Vector2 ReadVector2Value()
        {
            return ReadValue<Vector2>();
        }
        
        /// <summary>
        /// Reads the current value as a bool.
        /// </summary>
        /// <returns>Bool value or false if reading fails</returns>
        public bool ReadBoolValue()
        {
            return ReadValue<float>() > 0.5f; // Convert float button value to bool
        }
        
        /// <summary>
        /// Gets whether the action was started this frame.
        /// </summary>
        public bool Started => _context.started;
        
        /// <summary>
        /// Gets whether the action was performed this frame.
        /// </summary>
        public bool Performed => _context.performed;
        
        /// <summary>
        /// Gets whether the action was canceled this frame.
        /// </summary>
        public bool Canceled => _context.canceled;
        
        /// <summary>
        /// Gets the time when the action was triggered.
        /// </summary>
        public double Time => _context.time;
        
        /// <summary>
        /// Gets the duration the action has been active.
        /// </summary>
        public double Duration => _context.duration;
        
        /// <summary>
        /// Gets the underlying Unity input action.
        /// </summary>
        public UnityEngine.InputSystem.InputAction Action => _context.action;
        
        /// <summary>
        /// Gets the control that triggered the action.
        /// </summary>
        public UnityEngine.InputSystem.InputControl Control => _context.control;
    }
}
