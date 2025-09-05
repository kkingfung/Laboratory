using System;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Health;
using Laboratory.Core.Input.Interfaces;
using Laboratory.Core.Character;
using Laboratory.Core.Character.Controllers;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Camera;
using HealthChangedEventArgs = Laboratory.Core.Health.HealthChangedEventArgs;
using DeathEventArgs = Laboratory.Core.Health.DeathEventArgs;

namespace Laboratory.Subsystems.Player
{
    /// <summary>
    /// Unified Player subsystem manager that coordinates all player-related functionality.
    /// Consolidates character control, camera management, health, and input handling.
    /// This replaces the scattered player components with a single, cohesive system.
    /// 
    /// Version 3.0 - Enhanced architecture with proper subsystem organization
    /// </summary>
    public class PlayerSubsystemManager : MonoBehaviour, IDisposable
    {
        #region Serialized Fields

        [Header("Player Configuration")]
        [SerializeField] private PlayerSubsystemConfig _config;
        
        [Header("Component References")]
        [SerializeField] private CharacterLookController _lookController;
        [SerializeField] private CharacterCustomizationManager _customizationManager;
        [SerializeField] private UnifiedAimController _aimController;
        [SerializeField] private ClimbingController _climbingController;
        [SerializeField] private PlayerCameraManager _cameraManager;
        
        [Header("Health System")]
        [SerializeField] private HealthComponentBase _healthComponent;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;

        #endregion

        #region Private Fields

        private IInputService _inputService;
        private IEventBus _eventBus;
        private PlayerSubsystemState _currentState;
        private bool _isInitialized = false;
        
        // Component states
        private bool _isMoving = false;
        private bool _isAiming = false;
        private bool _isClimbing = false;
        private Vector3 _lastPosition;

        #endregion

        #region Properties

        /// <summary>Current state of the player subsystem</summary>
        public PlayerSubsystemState CurrentState => _currentState;
        
        /// <summary>Whether the player subsystem is fully initialized</summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>Current player health percentage (0-1)</summary>
        public float HealthPercentage => _healthComponent?.HealthPercentage ?? 0f;
        
        /// <summary>Whether the player is currently alive</summary>
        public bool IsAlive => _healthComponent?.IsAlive ?? false;
        
        /// <summary>Player configuration settings</summary>
        public PlayerSubsystemConfig Configuration => _config;

        #endregion

        #region Events

        /// <summary>Fired when the player subsystem state changes</summary>
        public event Action<PlayerSubsystemStateChangedEventArgs> OnStateChanged;
        
        /// <summary>Fired when player movement starts or stops</summary>
        public event Action<PlayerMovementEventArgs> OnMovementChanged;
        
        /// <summary>Fired when player aiming starts or stops</summary>
        public event Action<PlayerAimingEventArgs> OnAimingChanged;
        
        /// <summary>Fired when player climbing starts or stops</summary>
        public event Action<PlayerClimbingEventArgs> OnClimbingChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (_isInitialized)
            {
                UpdateSubsystem();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            try
            {
                LogDebug("Initializing Player Subsystem...");
                
                // Inject dependencies
                InjectDependencies();
                
                // Initialize components
                InitializeHealthSystem();
                InitializeInputHandling();
                InitializeCameraSystem();
                InitializeCharacterSystems();
                
                // Set initial state
                ChangeState(PlayerSubsystemState.Idle);
                
                _isInitialized = true;
                LogDebug("Player Subsystem initialized successfully");
                
                // Notify other systems
                _eventBus?.Publish(new PlayerSubsystemInitializedEvent(gameObject));
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize Player Subsystem: {ex.Message}");
            }
        }

        private void InjectDependencies()
        {
            // Get services from DI container
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance?.TryResolve<IInputService>(out _inputService);
                GlobalServiceProvider.Instance?.TryResolve<IEventBus>(out _eventBus);
            }

            if (_inputService == null)
            {
                LogWarning("Input service not available - some functionality may be limited");
            }

            if (_eventBus == null)
            {
                LogWarning("Event bus not available - events will not be published");
            }
        }

        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (_lookController == null)
                _lookController = GetComponentInChildren<CharacterLookController>();
            
            if (_customizationManager == null)
                _customizationManager = GetComponentInChildren<CharacterCustomizationManager>();
            
            if (_aimController == null)
                _aimController = GetComponentInChildren<UnifiedAimController>();
            
            if (_climbingController == null)
                _climbingController = GetComponentInChildren<ClimbingController>();
            
            if (_cameraManager == null)
                _cameraManager = FindFirstObjectByType<PlayerCameraManager>();
            
            if (_healthComponent == null)
                _healthComponent = GetComponent<HealthComponentBase>();

            _lastPosition = transform.position;
        }

        private void InitializeHealthSystem()
        {
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged += OnPlayerHealthChanged;
                _healthComponent.OnDeath += OnPlayerDeath;
                LogDebug("Health system initialized");
            }
        }

        private void InitializeInputHandling()
        {
            if (_inputService != null)
            {
                // Subscribe to input events
                // Note: Specific input handling would be implemented based on the actual input system
                LogDebug("Input handling initialized");
            }
        }

        private void InitializeCameraSystem()
        {
            if (_cameraManager != null)
            {
                _cameraManager.SetTarget(transform);
                LogDebug("Camera system initialized");
            }
        }

        private void InitializeCharacterSystems()
        {
            // Initialize look controller
            if (_lookController != null)
            {
                LogDebug("Look controller initialized");
            }

            // Initialize aim controller
            if (_aimController != null)
            {
                LogDebug("Aim controller initialized");
            }

            // Initialize climbing controller
            if (_climbingController != null)
            {
                LogDebug("Climbing controller initialized");
            }

            LogDebug("Character systems initialized");
        }

        #endregion

        #region Update Methods

        private void UpdateSubsystem()
        {
            UpdateMovementState();
            UpdateAimingState();
            UpdateClimbingState();
            UpdateStateTransitions();
        }

        private void UpdateMovementState()
        {
            bool wasMoving = _isMoving;
            Vector3 currentPosition = transform.position;
            
            // Check if position changed significantly
            float movementThreshold = _config?.MovementThreshold ?? 0.01f;
            _isMoving = Vector3.Distance(currentPosition, _lastPosition) > movementThreshold;
            
            if (_isMoving != wasMoving)
            {
                var eventArgs = new PlayerMovementEventArgs(_isMoving, currentPosition, _lastPosition);
                OnMovementChanged?.Invoke(eventArgs);
                
                _eventBus?.Publish(new PlayerMovementChangedEvent(_isMoving, gameObject));
                LogDebug($"Movement changed: {_isMoving}");
            }
            
            _lastPosition = currentPosition;
        }

        private void UpdateAimingState()
        {
            bool wasAiming = _isAiming;
            
            // Check aiming state from aim controller
            if (_aimController != null)
            {
                _isAiming = _aimController.IsAiming;
            }
            
            if (_isAiming != wasAiming)
            {
                var eventArgs = new PlayerAimingEventArgs(_isAiming, _aimController?.CurrentTarget);
                OnAimingChanged?.Invoke(eventArgs);
                
                _eventBus?.Publish(new PlayerAimingChangedEvent(_isAiming, gameObject));
                LogDebug($"Aiming changed: {_isAiming}");
            }
        }

        private void UpdateClimbingState()
        {
            bool wasClimbing = _isClimbing;
            
            // Check climbing state from climbing controller
            if (_climbingController != null)
            {
                _isClimbing = _climbingController.IsClimbing;
            }
            
            if (_isClimbing != wasClimbing)
            {
                var eventArgs = new PlayerClimbingEventArgs(_isClimbing);
                OnClimbingChanged?.Invoke(eventArgs);
                
                _eventBus?.Publish(new PlayerClimbingChangedEvent(_isClimbing, gameObject));
                LogDebug($"Climbing changed: {_isClimbing}");
            }
        }

        private void UpdateStateTransitions()
        {
            var newState = DetermineCurrentState();
            
            if (newState != _currentState)
            {
                ChangeState(newState);
            }
        }

        private PlayerSubsystemState DetermineCurrentState()
        {
            if (!IsAlive)
                return PlayerSubsystemState.Dead;
            
            if (_isClimbing)
                return PlayerSubsystemState.Climbing;
            
            if (_isAiming)
                return PlayerSubsystemState.Aiming;
            
            if (_isMoving)
                return PlayerSubsystemState.Moving;
            
            return PlayerSubsystemState.Idle;
        }

        #endregion

        #region State Management

        private void ChangeState(PlayerSubsystemState newState)
        {
            var oldState = _currentState;
            _currentState = newState;
            
            var eventArgs = new PlayerSubsystemStateChangedEventArgs(oldState, newState, gameObject);
            OnStateChanged?.Invoke(eventArgs);
            
            _eventBus?.Publish(new PlayerSubsystemStateChangedEvent(oldState, newState, gameObject));
            
            LogDebug($"State changed: {oldState} → {newState}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets comprehensive player subsystem status information.
        /// </summary>
        public PlayerSubsystemStatus GetStatus()
        {
            return new PlayerSubsystemStatus
            {
                IsInitialized = _isInitialized,
                CurrentState = _currentState,
                IsAlive = IsAlive,
                HealthPercentage = HealthPercentage,
                IsMoving = _isMoving,
                IsAiming = _isAiming,
                IsClimbing = _isClimbing,
                Position = transform.position,
                HasInputService = _inputService != null,
                HasEventBus = _eventBus != null,
                ComponentsInitialized = GetComponentStatus()
            };
        }

        /// <summary>
        /// Enables or disables specific player subsystem components.
        /// </summary>
        public void SetComponentEnabled(PlayerComponent component, bool enabled)
        {
            switch (component)
            {
                case PlayerComponent.LookController:
                    if (_lookController != null) _lookController.enabled = enabled;
                    break;
                case PlayerComponent.AimController:
                    if (_aimController != null) _aimController.enabled = enabled;
                    break;
                case PlayerComponent.ClimbingController:
                    if (_climbingController != null) _climbingController.enabled = enabled;
                    break;
                case PlayerComponent.CameraManager:
                    if (_cameraManager != null) _cameraManager.enabled = enabled;
                    break;
            }
            
            LogDebug($"Component {component} enabled: {enabled}");
        }

        /// <summary>
        /// Forces a state transition (use with caution).
        /// </summary>
        public void ForceStateChange(PlayerSubsystemState newState)
        {
            ChangeState(newState);
            LogDebug($"Forced state change to: {newState}");
        }

        #endregion

        #region Event Handlers

        private void OnPlayerHealthChanged(HealthChangedEventArgs args)
        {
            LogDebug($"Player health changed: {args.OldHealth} → {args.NewHealth}");
            
            // Update state if health reaches critical levels
            if (args.NewHealth <= 0)
            {
                ChangeState(PlayerSubsystemState.Dead);
            }
        }

        private void OnPlayerDeath(DeathEventArgs args)
        {
            LogDebug("Player died");
            ChangeState(PlayerSubsystemState.Dead);
            
            // Disable relevant components on death
            SetComponentEnabled(PlayerComponent.AimController, false);
            SetComponentEnabled(PlayerComponent.ClimbingController, false);
        }

        #endregion

        #region Helper Methods

        private void ValidateConfiguration()
        {
            if (_config == null)
            {
                LogWarning("No configuration assigned, using default values");
                _config = ScriptableObject.CreateInstance<PlayerSubsystemConfig>();
            }
        }

        private PlayerComponentStatus GetComponentStatus()
        {
            return new PlayerComponentStatus
            {
                HasLookController = _lookController != null,
                HasAimController = _aimController != null,
                HasClimbingController = _climbingController != null,
                HasCameraManager = _cameraManager != null,
                HasHealthComponent = _healthComponent != null,
                HasCustomizationManager = _customizationManager != null
            };
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[PlayerSubsystem] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[PlayerSubsystem] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[PlayerSubsystem] {message}");
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // Unsubscribe from health events
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged -= OnPlayerHealthChanged;
                _healthComponent.OnDeath -= OnPlayerDeath;
            }
            
            // Clear event handlers
            OnStateChanged = null;
            OnMovementChanged = null;
            OnAimingChanged = null;
            OnClimbingChanged = null;
            
            _isInitialized = false;
            LogDebug("Player Subsystem disposed");
        }

        #endregion
    }

    #region Data Structures and Enums

    /// <summary>
    /// Player subsystem states
    /// </summary>
    public enum PlayerSubsystemState
    {
        Uninitialized,
        Idle,
        Moving,
        Aiming,
        Climbing,
        Dead
    }

    /// <summary>
    /// Player component identifiers
    /// </summary>
    public enum PlayerComponent
    {
        LookController,
        AimController,
        ClimbingController,
        CameraManager,
        HealthComponent,
        CustomizationManager
    }

    /// <summary>
    /// Comprehensive player subsystem status
    /// </summary>
    [System.Serializable]
    public struct PlayerSubsystemStatus
    {
        public bool IsInitialized;
        public PlayerSubsystemState CurrentState;
        public bool IsAlive;
        public float HealthPercentage;
        public bool IsMoving;
        public bool IsAiming;
        public bool IsClimbing;
        public Vector3 Position;
        public bool HasInputService;
        public bool HasEventBus;
        public PlayerComponentStatus ComponentsInitialized;
    }

    /// <summary>
    /// Player component availability status
    /// </summary>
    [System.Serializable]
    public struct PlayerComponentStatus
    {
        public bool HasLookController;
        public bool HasAimController;
        public bool HasClimbingController;
        public bool HasCameraManager;
        public bool HasHealthComponent;
        public bool HasCustomizationManager;
    }

    #endregion

    #region Event Data Classes

    /// <summary>
    /// Event arguments for player subsystem state changes
    /// </summary>
    public class PlayerSubsystemStateChangedEventArgs : EventArgs
    {
        public PlayerSubsystemState OldState { get; }
        public PlayerSubsystemState NewState { get; }
        public GameObject Player { get; }
        public float Timestamp { get; }

        public PlayerSubsystemStateChangedEventArgs(PlayerSubsystemState oldState, PlayerSubsystemState newState, GameObject player)
        {
            OldState = oldState;
            NewState = newState;
            Player = player;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Event arguments for player movement changes
    /// </summary>
    public class PlayerMovementEventArgs : EventArgs
    {
        public bool IsMoving { get; }
        public Vector3 CurrentPosition { get; }
        public Vector3 PreviousPosition { get; }
        public float MovementDistance => Vector3.Distance(CurrentPosition, PreviousPosition);

        public PlayerMovementEventArgs(bool isMoving, Vector3 currentPosition, Vector3 previousPosition)
        {
            IsMoving = isMoving;
            CurrentPosition = currentPosition;
            PreviousPosition = previousPosition;
        }
    }

    /// <summary>
    /// Event arguments for player aiming changes
    /// </summary>
    public class PlayerAimingEventArgs : EventArgs
    {
        public bool IsAiming { get; }
        public Transform Target { get; }

        public PlayerAimingEventArgs(bool isAiming, Transform target)
        {
            IsAiming = isAiming;
            Target = target;
        }
    }

    /// <summary>
    /// Event arguments for player climbing changes
    /// </summary>
    public class PlayerClimbingEventArgs : EventArgs
    {
        public bool IsClimbing { get; }

        public PlayerClimbingEventArgs(bool isClimbing)
        {
            IsClimbing = isClimbing;
        }
    }

    #region Global Event Classes

    /// <summary>
    /// Global event published when player subsystem is initialized
    /// </summary>
    public class PlayerSubsystemInitializedEvent
    {
        public GameObject Player { get; }
        public float Timestamp { get; }

        public PlayerSubsystemInitializedEvent(GameObject player)
        {
            Player = player;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Global event published when player subsystem state changes
    /// </summary>
    public class PlayerSubsystemStateChangedEvent
    {
        public PlayerSubsystemState OldState { get; }
        public PlayerSubsystemState NewState { get; }
        public GameObject Player { get; }
        public float Timestamp { get; }

        public PlayerSubsystemStateChangedEvent(PlayerSubsystemState oldState, PlayerSubsystemState newState, GameObject player)
        {
            OldState = oldState;
            NewState = newState;
            Player = player;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Global event published when player movement changes
    /// </summary>
    public class PlayerMovementChangedEvent
    {
        public bool IsMoving { get; }
        public GameObject Player { get; }
        public float Timestamp { get; }

        public PlayerMovementChangedEvent(bool isMoving, GameObject player)
        {
            IsMoving = isMoving;
            Player = player;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Global event published when player aiming changes
    /// </summary>
    public class PlayerAimingChangedEvent
    {
        public bool IsAiming { get; }
        public GameObject Player { get; }
        public float Timestamp { get; }

        public PlayerAimingChangedEvent(bool isAiming, GameObject player)
        {
            IsAiming = isAiming;
            Player = player;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>
    /// Global event published when player climbing changes
    /// </summary>
    public class PlayerClimbingChangedEvent
    {
        public bool IsClimbing { get; }
        public GameObject Player { get; }
        public float Timestamp { get; }

        public PlayerClimbingChangedEvent(bool isClimbing, GameObject player)
        {
            IsClimbing = isClimbing;
            Player = player;
            Timestamp = Time.unscaledTime;
        }
    }

    #endregion

    #endregion
}