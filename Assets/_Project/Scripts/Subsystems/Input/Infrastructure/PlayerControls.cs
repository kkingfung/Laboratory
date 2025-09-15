using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Laboratory.Infrastructure.Input
{
    /// <summary>
    /// Generated input actions wrapper for the Laboratory project.
    /// Provides strongly-typed access to input actions and controls.
    /// This is a temporary implementation until Unity Input System assets are properly generated.
    /// </summary>
    public class PlayerControls : IDisposable
    {
        #region Fields
        
        private InputActionMap _inGameActionMap;
        private bool _disposed = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// In-game input action map containing movement and action controls.
        /// </summary>
        public InGameActions InGame { get; private set; }
        
        #endregion
        
        #region Constructor
        
        public PlayerControls()
        {
            // Create the action map
            _inGameActionMap = new InputActionMap("InGame");
            
            // Initialize the action groups
            InGame = new InGameActions(_inGameActionMap);
            
            Debug.Log("[PlayerControls] Initialized with basic input actions");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enables all input actions.
        /// </summary>
        public void Enable()
        {
            _inGameActionMap.Enable();
        }
        
        /// <summary>
        /// Disables all input actions.
        /// </summary>
        public void Disable()
        {
            _inGameActionMap.Disable();
        }
        
        /// <summary>
        /// Disposes of all input actions and resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            
            _inGameActionMap?.Dispose();
            _disposed = true;
        }
        
        #endregion
        
        #region Action Group Classes
        
        /// <summary>
        /// In-game input actions for movement and gameplay.
        /// </summary>
        public class InGameActions
        {
            private readonly InputActionMap _actionMap;
            
            public InputAction GoEast { get; private set; }
            public InputAction GoWest { get; private set; }
            public InputAction GoNorth { get; private set; }
            public InputAction GoSouth { get; private set; }
            public InputAction AttackOrThrow { get; private set; }
            public InputAction Jump { get; private set; }
            public InputAction Roll { get; private set; }
            public InputAction ActionOrCraft { get; private set; }
            public InputAction CharSkill { get; private set; }
            public InputAction WeaponSkill { get; private set; }
            public InputAction Pause { get; private set; }
            
            public InGameActions(InputActionMap actionMap)
            {
                _actionMap = actionMap;
                CreateActions();
            }
            
            private void CreateActions()
            {
                // Movement actions
                GoEast = _actionMap.AddAction("GoEast", InputActionType.Button);
                GoEast.AddBinding("<Keyboard>/d");
                GoEast.AddBinding("<Keyboard>/rightArrow");
                GoEast.AddBinding("<Gamepad>/dpad/right");
                GoEast.AddBinding("<Gamepad>/leftStick/right");
                
                GoWest = _actionMap.AddAction("GoWest", InputActionType.Button);
                GoWest.AddBinding("<Keyboard>/a");
                GoWest.AddBinding("<Keyboard>/leftArrow");
                GoWest.AddBinding("<Gamepad>/dpad/left");
                GoWest.AddBinding("<Gamepad>/leftStick/left");
                
                GoNorth = _actionMap.AddAction("GoNorth", InputActionType.Button);
                GoNorth.AddBinding("<Keyboard>/w");
                GoNorth.AddBinding("<Keyboard>/upArrow");
                GoNorth.AddBinding("<Gamepad>/dpad/up");
                GoNorth.AddBinding("<Gamepad>/leftStick/up");
                
                GoSouth = _actionMap.AddAction("GoSouth", InputActionType.Button);
                GoSouth.AddBinding("<Keyboard>/s");
                GoSouth.AddBinding("<Keyboard>/downArrow");
                GoSouth.AddBinding("<Gamepad>/dpad/down");
                GoSouth.AddBinding("<Gamepad>/leftStick/down");
                
                // Action buttons
                AttackOrThrow = _actionMap.AddAction("AttackOrThrow", InputActionType.Button);
                AttackOrThrow.AddBinding("<Mouse>/leftButton");
                AttackOrThrow.AddBinding("<Keyboard>/space");
                AttackOrThrow.AddBinding("<Gamepad>/buttonSouth");
                
                Jump = _actionMap.AddAction("Jump", InputActionType.Button);
                Jump.AddBinding("<Keyboard>/space");
                Jump.AddBinding("<Gamepad>/buttonSouth");
                
                Roll = _actionMap.AddAction("Roll", InputActionType.Button);
                Roll.AddBinding("<Keyboard>/leftShift");
                Roll.AddBinding("<Gamepad>/buttonEast");
                
                ActionOrCraft = _actionMap.AddAction("ActionOrCraft", InputActionType.Button);
                ActionOrCraft.AddBinding("<Keyboard>/e");
                ActionOrCraft.AddBinding("<Gamepad>/buttonWest");
                
                CharSkill = _actionMap.AddAction("CharSkill", InputActionType.Button);
                CharSkill.AddBinding("<Keyboard>/q");
                CharSkill.AddBinding("<Gamepad>/leftShoulder");
                
                WeaponSkill = _actionMap.AddAction("WeaponSkill", InputActionType.Button);
                WeaponSkill.AddBinding("<Keyboard>/r");
                WeaponSkill.AddBinding("<Gamepad>/rightShoulder");
                
                // System actions
                Pause = _actionMap.AddAction("Pause", InputActionType.Button);
                Pause.AddBinding("<Keyboard>/escape");
                Pause.AddBinding("<Gamepad>/start");
            }
        }
        
        #endregion
    }
}
