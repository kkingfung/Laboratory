using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Laboratory.Core;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Models.ECS.Components;
using Laboratory.Infrastructure.Networking;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// MonoBehaviour system responsible for capturing player input using Unity's Input System
    /// and translating it to ECS components for processing by other systems. This system bridges
    /// the gap between Unity's input handling and the ECS architecture.
    /// </summary>
    public class PlayerInputSystem : MonoBehaviour
    {
        #region Fields
        
         /// <summary>
        /// Sensitivity multiplier for look input (mouse/controller)
        /// </summary>
        [SerializeField]
        private float _lookSensitivity = 1.0f;
        
        /// <summary>
        /// Deadzone threshold for controller input to prevent drift
        /// </summary>
        [SerializeField]
        private float _inputDeadzone = 0.1f;
        
        /// <summary>
        /// Entity manager for accessing and modifying ECS entities and components
        /// </summary>
        private EntityManager _entityManager;
        
        /// <summary>
        /// The player entity that this input system controls
        /// </summary>
        private Entity _playerEntity = Entity.Null;
        
        /// <summary>
        /// Input System controls for handling player input actions
        /// </summary>
        private PlayerControls _controls = null!;
        
        /// <summary>
        /// Flag indicating whether the system is properly initialized
        /// </summary>
        private bool _isInitialized = false;
        
        /// <summary>
        /// Cache for the last valid input to prevent unnecessary component updates
        /// </summary>
        private PlayerInputComponent _lastInput;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the MonoBehaviour is created. Initializes the ECS entity manager
        /// and sets up input controls.
        /// </summary>
        private void Awake()
        {
            InitializeEntityManager();
            InitializeInputControls();
        }

        /// <summary>
        /// Called when the GameObject starts. Finds and assigns the player entity
        /// for input processing.
        /// </summary>
        private void Start()
        {
            InitializePlayerEntity();
        }

        /// <summary>
        /// Called when the component is enabled. Enables input controls and
        /// starts processing input events.
        /// </summary>
        private void OnEnable()
        {
            EnableInputControls();
        }

        /// <summary>
        /// Called when the component is disabled. Disables input controls and
        /// stops processing input events.
        /// </summary>
        private void OnDisable()
        {
            DisableInputControls();
        }

        /// <summary>
        /// Called every frame. Processes input and updates the player entity's
        /// input component with current input state.
        /// </summary>
        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            ProcessPlayerInput();
        }

        /// <summary>
        /// Called when the MonoBehaviour is destroyed. Cleans up input controls
        /// and releases resources.
        /// </summary>
        private void OnDestroy()
        {
            CleanupResources();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the ECS entity manager from the default world
        /// </summary>
        private void InitializeEntityManager()
        {
            try
            {
                _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
                
                if (_entityManager == null)
                {
                    throw new InvalidOperationException("Default ECS World or EntityManager is null");
                }
                
                Debug.Log("PlayerInputSystem entity manager initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize EntityManager: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Initializes the Input System controls for player input
        /// </summary>
        private void InitializeInputControls()
        {
            try
            {
                _controls = new PlayerControls();
                Debug.Log("PlayerInputSystem input controls initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize input controls: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Finds and assigns the player entity for input processing
        /// </summary>
        private void InitializePlayerEntity()
        {
            try
            {
                // TODO: Implement proper player entity lookup based on your game's architecture
                // This could involve querying entities with specific components or tags
                _playerEntity = FindPlayerEntity();
                
                if (_playerEntity == Entity.Null)
                {
                    Debug.LogWarning("Player entity not found. Input will not be processed until entity is assigned.");
                    _isInitialized = false;
                    return;
                }
                
                ValidatePlayerEntityComponents();
                _isInitialized = true;
                Debug.Log("PlayerInputSystem fully initialized with player entity");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize player entity: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Finds the player entity in the ECS world (placeholder implementation)
        /// </summary>
        /// <returns>The player entity, or Entity.Null if not found</returns>
        private Entity FindPlayerEntity()
        {
            // TODO: Implement proper entity lookup logic
            // Example approaches:
            // 1. Query entities with PlayerTag component
            // 2. Use a singleton component to store player entity reference
            // 3. Search by specific component combination (PlayerInputComponent + PlayerStateComponent)
            
            // Placeholder implementation - replace with actual lookup logic
            using var query = _entityManager.CreateEntityQuery(typeof(PlayerInputComponent));
            var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
            
            if (entities.Length > 0)
            {
                return entities[0]; // Return first player entity found
            }
            
            return Entity.Null;
        }

        /// <summary>
        /// Validates that the player entity has required components for input processing
        /// </summary>
        private void ValidatePlayerEntityComponents()
        {
            if (!_entityManager.HasComponent<PlayerInputComponent>(_playerEntity))
            {
                throw new InvalidOperationException("Player entity missing PlayerInputComponent");
            }
            
            // Add additional component validation as needed
            Debug.Log("Player entity components validated successfully");
        }

        /// <summary>
        /// Enables input controls and starts listening for input events
        /// </summary>
        private void EnableInputControls()
        {
            try
            {
                if (_controls != null)
                {
                    _controls.InGame.Enable();
                    Debug.Log("Input controls enabled");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to enable input controls: {ex.Message}");
            }
        }

        /// <summary>
        /// Disables input controls and stops listening for input events
        /// </summary>
        private void DisableInputControls()
        {
            try
            {
                if (_controls != null)
                {
                    _controls.InGame.Disable();
                    Debug.Log("Input controls disabled");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to disable input controls: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes current input state and updates the player entity's input component
        /// </summary>
        private void ProcessPlayerInput()
        {
            try
            {
                if (_playerEntity == Entity.Null || !IsPlayerEntityValid())
                {
                    return;
                }

                var inputComponent = GatherInputData();
                
                // Only update component if input has changed to reduce ECS churn
                if (!InputComponentEquals(inputComponent, _lastInput))
                {
                    UpdatePlayerInputComponent(inputComponent);
                    _lastInput = inputComponent;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing player input: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates that the player entity still exists and has required components
        /// </summary>
        /// <returns>True if the player entity is valid for input processing</returns>
        private bool IsPlayerEntityValid()
        {
            return _entityManager.Exists(_playerEntity) && 
                   _entityManager.HasComponent<PlayerInputComponent>(_playerEntity);
        }

        /// <summary>
        /// Gathers all current input data and creates a PlayerInputComponent
        /// </summary>
        /// <returns>PlayerInputComponent with current input state</returns>
        private PlayerInputComponent GatherInputData()
        {
            var input = new PlayerInputComponent();

            // Read movement input from directional buttons
            float horizontal = 0f;
            float vertical = 0f;
            
            if (_controls.InGame.GoEast.IsPressed()) horizontal += 1f;
            if (_controls.InGame.GoWest.IsPressed()) horizontal -= 1f;
            if (_controls.InGame.GoNorth.IsPressed()) vertical += 1f;
            if (_controls.InGame.GoSouth.IsPressed()) vertical -= 1f;
            
            input.MoveDirection = ApplyDeadzone(new float2(horizontal, vertical));

            // No look input available in current input map - set to zero
            input.LookDirection = float3.zero;

            // Read discrete action inputs
            input.AttackPressed = _controls.InGame.AttackOrThrow.WasPressedThisFrame();
            input.ActionPressed = _controls.InGame.ActionOrCraft.WasPressedThisFrame();
            input.JumpPressed = _controls.InGame.Jump.IsPressed();
            input.RollPressed = _controls.InGame.Roll.IsPressed();

            // Add additional input actions as needed
            input.WeaponSkillPressed = _controls.InGame.WeaponSkill.IsPressed();
            input.CharSkillPressed = _controls.InGame.CharSkill.IsPressed();
            return input;
        }

        /// <summary>
        /// Applies deadzone filtering to input vector to prevent controller drift
        /// </summary>
        /// <param name="input">The raw input vector</param>
        /// <returns>Filtered input vector with deadzone applied</returns>
        private float2 ApplyDeadzone(float2 input)
        {
            float magnitude = math.length(input);
            
            if (magnitude < _inputDeadzone)
            {
                return float2.zero;
            }
            
            // Normalize and scale to remove deadzone
            float normalizedMagnitude = (magnitude - _inputDeadzone) / (1.0f - _inputDeadzone);
            return math.normalize(input) * normalizedMagnitude;
        }

        /// <summary>
        /// Compares two PlayerInputComponent instances for equality
        /// </summary>
        /// <param name="a">First input component</param>
        /// <param name="b">Second input component</param>
        /// <returns>True if components are equal</returns>
        private bool InputComponentEquals(PlayerInputComponent a, PlayerInputComponent b)
        {
            return math.all(a.MoveDirection == b.MoveDirection) &&
                   a.AttackPressed == b.AttackPressed &&
                   a.JumpPressed == b.JumpPressed &&
                   a.ActionPressed == b.ActionPressed &&
                   a.RollPressed == b.RollPressed &&
                   a.CharSkillPressed == b.CharSkillPressed &&
                   a.WeaponSkillPressed == b.WeaponSkillPressed;
        }

        /// <summary>
        /// Updates the player entity's input component with new input data
        /// </summary>
        /// <param name="inputComponent">The new input component data</param>
        private void UpdatePlayerInputComponent(PlayerInputComponent inputComponent)
        {
            try
            {
                _entityManager.SetComponentData(_playerEntity, inputComponent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to update player input component: {ex.Message}");
            }
        }

        /// <summary>
        /// Cleans up input controls and releases resources
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _controls?.InGame.Disable();
                _controls?.Dispose();
                _controls = null!;
                
                _playerEntity = Entity.Null;
                _isInitialized = false;
                
                Debug.Log("PlayerInputSystem resources cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during PlayerInputSystem cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually assigns the player entity for input processing
        /// </summary>
        /// <param name="playerEntity">The player entity to control</param>
        public void SetPlayerEntity(Entity playerEntity)
        {
            try
            {
                if (playerEntity == Entity.Null)
                {
                    Debug.LogWarning("Attempting to set null player entity");
                    return;
                }

                _playerEntity = playerEntity;
                ValidatePlayerEntityComponents();
                _isInitialized = true;
                
                Debug.Log($"Player entity set manually: {playerEntity}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set player entity: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Gets the current player entity being controlled
        /// </summary>
        /// <returns>The player entity, or Entity.Null if not set</returns>
        public Entity GetPlayerEntity()
        {
            return _playerEntity;
        }

        /// <summary>
        /// Checks if the input system is properly initialized and ready for use
        /// </summary>
        /// <returns>True if the system is initialized and functional</returns>
        public bool IsInitialized()
        {
            return _isInitialized && _playerEntity != Entity.Null && _controls != null;
        }

        /// <summary>
        /// Updates input sensitivity for look controls
        /// </summary>
        /// <param name="sensitivity">New sensitivity value</param>
        public void SetLookSensitivity(float sensitivity)
        {
            _lookSensitivity = math.max(0.1f, sensitivity); // Ensure minimum sensitivity
            Debug.Log($"Look sensitivity updated to: {_lookSensitivity}");
        }

        /// <summary>
        /// Updates input deadzone threshold
        /// </summary>
        /// <param name="deadzone">New deadzone value (0.0 to 0.9)</param>
        public void SetInputDeadzone(float deadzone)
        {
            _inputDeadzone = math.clamp(deadzone, 0f, 0.9f);
            Debug.Log($"Input deadzone updated to: {_inputDeadzone}");
        }

        #endregion
    }
}
