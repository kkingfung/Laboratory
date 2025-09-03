using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Laboratory.Core.Input.Interfaces;
using Laboratory.Core.Input.Events;
using Laboratory.Core.DI;
using Laboratory.Models.ECS.Input.Components;

namespace Laboratory.Models.ECS.Input.Systems
{
    /// <summary>
    /// Enhanced ECS system responsible for capturing player input and updating ECS components.
    /// Integrates with the unified input system and provides better error handling and validation.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class EnhancedPlayerInputSystem : SystemBase
    {
        #region Private Fields
        
        private IInputService _inputService;
        private EntityQuery _playerQuery;
        private bool _isInitialized = false;
        private uint _currentFrame = 0;
        
        #endregion

        #region SystemBase Overrides

        protected override void OnCreate()
        {
            base.OnCreate();
            InitializeSystem();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            
            if (!_isInitialized)
            {
                InitializeSystem();
            }
        }

        protected override void OnUpdate()
        {
            if (!_isInitialized)
            {
                return;
            }

            _currentFrame++;
            var currentTime = SystemAPI.Time.ElapsedTime;

            // Process input for all player entities
            Entities
                .WithAll<LocalPlayerTag, InputEnabledTag>()
                .WithoutBurst()
                .ForEach((ref EnhancedPlayerInputComponent inputComponent,
                         in InputConfigurationComponent config) =>
                {
                    ProcessPlayerInput(ref inputComponent, config, currentTime);
                })
                .Run();

            // Update input buffers
            Entities
                .WithAll<LocalPlayerTag>()
                .WithoutBurst()
                .ForEach((ref InputBufferComponent buffer,
                         in EnhancedPlayerInputComponent inputComponent) =>
                {
                    if (HasInputChanged(inputComponent))
                    {
                        buffer.AddInput(inputComponent);
                    }
                })
                .Run();
        }

        protected override void OnDestroy()
        {
            Cleanup();
            base.OnDestroy();
        }

        #endregion

        #region Private Methods

        private void InitializeSystem()
        {
            try
            {
                // Get the input service
                var serviceContainer = GlobalServiceProvider.Instance;
                serviceContainer?.TryResolve<IInputService>(out _inputService);

                if (_inputService == null)
                {
                    Debug.LogError("[EnhancedPlayerInputSystem] Failed to get IInputService from service container");
                    return;
                }

                // Create entity query for players
                _playerQuery = GetEntityQuery(
                    ComponentType.ReadWrite<EnhancedPlayerInputComponent>(),
                    ComponentType.ReadOnly<LocalPlayerTag>(),
                    ComponentType.ReadOnly<InputEnabledTag>()
                );

                _isInitialized = true;
                Debug.Log("[EnhancedPlayerInputSystem] System initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnhancedPlayerInputSystem] Failed to initialize: {ex.Message}");
                _isInitialized = false;
            }
        }

        private void ProcessPlayerInput(ref EnhancedPlayerInputComponent inputComponent,
                                      InputConfigurationComponent config,
                                      double currentTime)
        {
            try
            {
                // Clear previous frame's input
                inputComponent.Clear();

                // Gather current input data
                GatherInputData(ref inputComponent, config);

                // Update timestamp
                inputComponent.UpdateTimestamp(currentTime, _currentFrame);

                // Validate input
                inputComponent.ValidateInput();

                // Debug log if input validation failed
                if (!inputComponent.IsValid)
                {
                    Debug.LogWarning($"[EnhancedPlayerInputSystem] Invalid input detected: {inputComponent.ValidationFlags}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnhancedPlayerInputSystem] Error processing input: {ex.Message}");
                
                // Set invalid flag on error
                inputComponent.ValidationFlags |= InputValidationFlags.Invalid;
            }
        }

        private void GatherInputData(ref EnhancedPlayerInputComponent inputComponent,
                                   InputConfigurationComponent config)
        {
            // Gather movement input
            var rawMovement = _inputService.GetMovementInput();
            inputComponent.RawMoveDirection = rawMovement;
            inputComponent.MoveDirection = ApplyDeadzone(rawMovement, config.Deadzone);

            // Gather look input (if available)
            var lookInput = _inputService.GetLookInput();
            inputComponent.LookDirection = lookInput;
            inputComponent.LookDelta = new float2(lookInput.x, lookInput.y) * config.Sensitivity;

            // Gather action inputs
            inputComponent.AttackPressed = _inputService.WasActionPerformed("AttackOrThrow");
            inputComponent.ActionPressed = _inputService.WasActionPerformed("ActionOrCraft");
            inputComponent.JumpPressed = _inputService.IsActionPressed("Jump");
            inputComponent.RollPressed = _inputService.IsActionPressed("Roll");
            inputComponent.CharSkillPressed = _inputService.IsActionPressed("CharSkill");
            inputComponent.WeaponSkillPressed = _inputService.IsActionPressed("WeaponSkill");
            inputComponent.PausePressed = _inputService.WasActionPerformed("Pause");
        }

        private bool HasInputChanged(EnhancedPlayerInputComponent current)
        {
            // For simplicity, assume input has changed if any action is pressed or movement is detected
            return current.HasAnyAction || current.IsMoving;
        }

        private float2 ApplyDeadzone(float2 input, float deadzone)
        {
            var magnitude = math.length(input);
            
            if (magnitude < deadzone)
            {
                return float2.zero;
            }
            
            var normalizedMagnitude = (magnitude - deadzone) / (1.0f - deadzone);
            return math.normalize(input) * normalizedMagnitude;
        }

        private void Cleanup()
        {
            try
            {
                _inputService = null;
                _isInitialized = false;
                
                Debug.Log("[EnhancedPlayerInputSystem] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnhancedPlayerInputSystem] Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a player entity with all necessary input components.
        /// </summary>
        /// <param name="entityManager">Entity manager to use</param>
        /// <param name="config">Input configuration (optional)</param>
        /// <returns>Created player entity</returns>
        public static Entity CreatePlayerEntity(EntityManager entityManager, 
                                              InputConfigurationComponent? config = null)
        {
            var entity = entityManager.CreateEntity();
            
            // Add required components
            entityManager.AddComponent<EnhancedPlayerInputComponent>(entity);
            entityManager.AddComponent<InputBufferComponent>(entity);
            entityManager.AddComponent<LocalPlayerTag>(entity);
            entityManager.AddComponent<InputEnabledTag>(entity);
            
            // Add configuration
            var inputConfig = config ?? InputConfigurationComponent.Default;
            entityManager.AddComponentData(entity, inputConfig);
            
            Debug.Log($"[EnhancedPlayerInputSystem] Created player entity: {entity}");
            return entity;
        }

        /// <summary>
        /// Enables input processing for an entity.
        /// </summary>
        /// <param name="entityManager">Entity manager to use</param>
        /// <param name="entity">Entity to enable input for</param>
        public static void EnableInput(EntityManager entityManager, Entity entity)
        {
            if (!entityManager.HasComponent<InputEnabledTag>(entity))
            {
                entityManager.AddComponent<InputEnabledTag>(entity);
                Debug.Log($"[EnhancedPlayerInputSystem] Enabled input for entity: {entity}");
            }
        }

        /// <summary>
        /// Disables input processing for an entity.
        /// </summary>
        /// <param name="entityManager">Entity manager to use</param>
        /// <param name="entity">Entity to disable input for</param>
        public static void DisableInput(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<InputEnabledTag>(entity))
            {
                entityManager.RemoveComponent<InputEnabledTag>(entity);
                Debug.Log($"[EnhancedPlayerInputSystem] Disabled input for entity: {entity}");
            }
        }

        /// <summary>
        /// Updates input configuration for an entity.
        /// </summary>
        /// <param name="entityManager">Entity manager to use</param>
        /// <param name="entity">Entity to update</param>
        /// <param name="config">New configuration</param>
        public static void UpdateInputConfiguration(EntityManager entityManager, 
                                                   Entity entity,
                                                   InputConfigurationComponent config)
        {
            if (entityManager.HasComponent<InputConfigurationComponent>(entity))
            {
                entityManager.SetComponentData(entity, config);
                Debug.Log($"[EnhancedPlayerInputSystem] Updated input configuration for entity: {entity}");
            }
        }

        /// <summary>
        /// Gets the current input data for an entity.
        /// </summary>
        /// <param name="entityManager">Entity manager to use</param>
        /// <param name="entity">Entity to get input for</param>
        /// <returns>Current input component data</returns>
        public static EnhancedPlayerInputComponent? GetInputData(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<EnhancedPlayerInputComponent>(entity))
            {
                return entityManager.GetComponentData<EnhancedPlayerInputComponent>(entity);
            }
            
            return null;
        }

        /// <summary>
        /// Gets the input buffer for an entity.
        /// </summary>
        /// <param name="entityManager">Entity manager to use</param>
        /// <param name="entity">Entity to get buffer for</param>
        /// <returns>Input buffer component data</returns>
        public static InputBufferComponent? GetInputBuffer(EntityManager entityManager, Entity entity)
        {
            if (entityManager.HasComponent<InputBufferComponent>(entity))
            {
                return entityManager.GetComponentData<InputBufferComponent>(entity);
            }
            
            return null;
        }

        #endregion
    }
}
