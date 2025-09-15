using System;
using UnityEngine.InputSystem;

namespace Laboratory.UI.Input
{
    /// <summary>
    /// Wrapper for the auto-generated PlayerControls to provide minimap-specific functionality for UI.
    /// This acts as a bridge between the MiniMapUI and the generated input system.
    /// </summary>
    public class UIPlayerControls : IDisposable
    {
        #region Fields
        
        private readonly global::PlayerControls _generatedControls;
        private bool _disposed = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Minimap input action map containing zoom, pan, and click controls.
        /// </summary>
        public MiniMapActions MiniMap { get; private set; }
        
        #endregion
        
        #region Constructor
        
        public UIPlayerControls()
        {
            _generatedControls = new global::PlayerControls();
            MiniMap = new MiniMapActions(_generatedControls);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enables all input actions.
        /// </summary>
        public void Enable()
        {
            _generatedControls.Enable();
        }
        
        /// <summary>
        /// Disables all input actions.
        /// </summary>
        public void Disable()
        {
            _generatedControls.Disable();
        }
        
        /// <summary>
        /// Disposes of all input actions and resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _generatedControls?.Dispose();
            _disposed = true;
        }
        
        #endregion
        
        #region Action Group Classes
        
        /// <summary>
        /// Minimap input actions for zoom, pan, and click functionality.
        /// </summary>
        public class MiniMapActions
        {
            private readonly global::PlayerControls _controls;
            
            public UnityEngine.InputSystem.InputAction Zoom { get; private set; }
            public UnityEngine.InputSystem.InputAction Pan { get; private set; }
            public UnityEngine.InputSystem.InputAction Click { get; private set; }
            
            public MiniMapActions(global::PlayerControls controls)
            {
                _controls = controls;
                
                // Try to find the MiniMap action map from the generated controls
                // If it doesn't exist yet (because we just added it), create fallback actions
                try
                {
                    var miniMapActionMap = _controls.asset.FindActionMap("MiniMap");
                    if (miniMapActionMap != null)
                    {
                        Zoom = miniMapActionMap.FindAction("Zoom");
                        Pan = miniMapActionMap.FindAction("Pan");
                        Click = miniMapActionMap.FindAction("Click");
                    }
                }
                catch
                {
                    // Fallback: create temporary actions if the action map doesn't exist yet
                    CreateFallbackActions();
                }
                
                // If we still don't have actions, create fallbacks
                if (Zoom == null || Pan == null || Click == null)
                {
                    CreateFallbackActions();
                }
            }
            
            private void CreateFallbackActions()
            {
                // Create a temporary action map for minimap controls
                var actionMap = new InputActionMap("MiniMap_Temp");
                
                // Create zoom action
                Zoom = actionMap.AddAction("Zoom", UnityEngine.InputSystem.InputActionType.Value);
                Zoom.AddBinding("<Mouse>/scroll/y");
                Zoom.AddBinding("<Gamepad>/rightStick/y");
                
                // Create pan action
                Pan = actionMap.AddAction("Pan", UnityEngine.InputSystem.InputActionType.Value);
                Pan.AddBinding("<Mouse>/position");
                Pan.AddBinding("<Gamepad>/rightStick");
                
                // Create click action
                Click = actionMap.AddAction("Click", UnityEngine.InputSystem.InputActionType.Button);
                Click.AddBinding("<Mouse>/leftButton");
                Click.AddBinding("<Gamepad>/buttonSouth");
                
                actionMap.Enable();
            }
        }
        
        #endregion
    }
}
