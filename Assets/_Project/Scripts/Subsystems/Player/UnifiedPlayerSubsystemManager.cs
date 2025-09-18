using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Health;
using Laboratory.Core.Character;
using Laboratory.Core.Camera;
using Laboratory.Core.Player;
using Laboratory.Core.Customization;

namespace Laboratory.Subsystems.Player
{
    /// <summary>
    /// Unified Player Subsystem Manager - Simplified Version
    /// Consolidates all player-related subsystem management
    /// </summary>
    public class UnifiedPlayerSubsystemManager : MonoBehaviour
    {
        #region Fields

        [Header("Subsystem Configuration")]
        [SerializeField] private PlayerSubsystemConfig subsystemConfig;
        [SerializeField] private bool enableDebugLogging = false;

        [Header("Component References")]
        [SerializeField] private GameObject playerObject;
        [SerializeField] private CharacterController characterController;

        // Service dependencies
        private IServiceContainer _container;
        private IEventBus _eventManager;
        
        // Core subsystem components
        private IHealthComponent _healthComponent;
        private MonoBehaviour _movementController;
        private MonoBehaviour _cameraController;
        private ICustomizationSystem _customizationSystem;

        // Internal state tracking
        private bool _isInitialized = false;
        private bool _isActive = true;
        private string _currentState = "idle";
        private Dictionary<string, object> _subsystemStates;
        private float _lastUpdateTime;
        private float _updateInterval = 0.016f; // ~60fps

        #endregion

        #region Properties

        public bool IsInitialized => _isInitialized;
        public bool IsActive => _isActive;
        public string CurrentState => _currentState;
        public PlayerSubsystemConfig Config => subsystemConfig;
        public GameObject PlayerObject => playerObject;

        #endregion

        #region Events

        public event Action<bool> OnInitializationStatusChanged;
        public event Action<string, string> OnStateChanged;
        public event Action<string> OnSubsystemError;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeSubsystemStates();
            Initialize();
        }

        private void Start()
        {
            RegisterEventHandlers();
            ValidateConfiguration();
        }

        private void Update()
        {
            if (!_isInitialized || !_isActive) return;

            if (Time.time - _lastUpdateTime < _updateInterval) return;
            
            UpdateSubsystems();
            _lastUpdateTime = Time.time;
        }

        private void OnEnable()
        {
            SetActive(true);
        }

        private void OnDisable()
        {
            SetActive(false);
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void InitializeSubsystemStates()
        {
            _subsystemStates = new Dictionary<string, object>
            {
                ["health"] = new HealthSubsystemState(),
                ["movement"] = new MovementSubsystemState(),
                ["camera"] = new CameraSubsystemState(),
                ["customization"] = new CustomizationSubsystemState()
            };

            if (enableDebugLogging)
                Debug.Log("[UnifiedPlayerSubsystemManager] Subsystem states initialized");
        }

        private void Initialize()
        {
            try
            {
                if (subsystemConfig == null)
                {
                    subsystemConfig = PlayerSubsystemConfig.CreateDefault();
                    Debug.LogWarning("[UnifiedPlayerSubsystemManager] Using default configuration");
                }

                _container = ServiceContainer.Instance;
                _eventManager = _container?.Resolve<IEventBus>();

                InitializeHealthSystem();
                InitializeMovementSystem();
                InitializeCameraSystem();
                InitializeCustomizationSystem();

                _isInitialized = true;
                OnInitializationStatusChanged?.Invoke(true);

                if (enableDebugLogging)
                    Debug.Log("[UnifiedPlayerSubsystemManager] Initialization completed successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedPlayerSubsystemManager] Initialization failed: {ex.Message}");
                OnSubsystemError?.Invoke($"Initialization failed: {ex.Message}");
            }
        }

        private void InitializeHealthSystem()
        {
            try
            {
                // Find health component
                var healthComponents = GetComponentsInChildren<MonoBehaviour>();
                foreach (var comp in healthComponents)
                {
                    if (comp is IHealthComponent healthComp)
                    {
                        _healthComponent = healthComp;
                        break;
                    }
                }

                if (_healthComponent != null && enableDebugLogging)
                    Debug.Log("[UnifiedPlayerSubsystemManager] Health system initialized");
                else if (enableDebugLogging)
                    Debug.LogWarning("[UnifiedPlayerSubsystemManager] No IHealthComponent found");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedPlayerSubsystemManager] Health system initialization failed: {ex.Message}");
            }
        }

        private void InitializeMovementSystem()
        {
            try
            {
                if (characterController == null)
                    characterController = GetComponent<CharacterController>();

                _movementController = _container?.Resolve<MonoBehaviour>();

                if (enableDebugLogging)
                    Debug.Log("[UnifiedPlayerSubsystemManager] Movement system initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedPlayerSubsystemManager] Movement system initialization failed: {ex.Message}");
            }
        }

        private void InitializeCameraSystem()
        {
            try
            {
                _cameraController = _container?.Resolve<MonoBehaviour>();

                if (enableDebugLogging)
                    Debug.Log("[UnifiedPlayerSubsystemManager] Camera system initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedPlayerSubsystemManager] Camera system initialization failed: {ex.Message}");
            }
        }

        private void InitializeCustomizationSystem()
        {
            try
            {
                _customizationSystem = _container?.Resolve<ICustomizationSystem>();

                if (enableDebugLogging)
                    Debug.Log("[UnifiedPlayerSubsystemManager] Customization system initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedPlayerSubsystemManager] Customization system initialization failed: {ex.Message}");
            }
        }

        #endregion

        #region Event Handling

        private void RegisterEventHandlers()
        {
            if (_eventManager != null && enableDebugLogging)
                Debug.Log("[UnifiedPlayerSubsystemManager] Event handlers registered");
        }

        #endregion

        #region State Management

        public void ChangeState(string newState)
        {
            if (_currentState == newState) return;

            string oldState = _currentState;
            _currentState = newState;

            OnStateChanged?.Invoke(oldState, newState);

            var stateEvent = new PlayerStateChangedEvent(playerObject, oldState, newState);
            _eventManager?.Publish(stateEvent);
        }

        public void SetActive(bool active)
        {
            _isActive = active;

            if (enableDebugLogging)
                Debug.Log($"[UnifiedPlayerSubsystemManager] Active state set to: {active}");
        }

        #endregion

        #region Update Methods

        private void UpdateSubsystems()
        {
            try
            {
                UpdatePerformanceMetrics();
                UpdateSubsystemStates();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedPlayerSubsystemManager] Error during subsystem update: {ex.Message}");
            }
        }

        private void UpdatePerformanceMetrics()
        {
            if (subsystemConfig?.performanceSettings.enableAdaptiveUpdates == true)
            {
                float currentFPS = 1.0f / Time.deltaTime;
                float targetFPS = subsystemConfig.performanceSettings.targetFPS;

                if (currentFPS < targetFPS * 0.8f)
                {
                    _updateInterval = Mathf.Min(_updateInterval * 1.1f, 0.1f);
                }
                else if (currentFPS > targetFPS * 1.2f)
                {
                    _updateInterval = Mathf.Max(_updateInterval * 0.9f, 0.008f);
                }
            }
        }

        private void UpdateSubsystemStates()
        {
            foreach (var kvp in _subsystemStates)
            {
                try
                {
                    switch (kvp.Key)
                    {
                        case "health":
                            UpdateHealthState(kvp.Value as HealthSubsystemState);
                            break;
                        case "movement":
                            UpdateMovementState(kvp.Value as MovementSubsystemState);
                            break;
                        case "camera":
                            UpdateCameraState(kvp.Value as CameraSubsystemState);
                            break;
                        case "customization":
                            UpdateCustomizationState(kvp.Value as CustomizationSubsystemState);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UnifiedPlayerSubsystemManager] Error updating {kvp.Key} state: {ex.Message}");
                }
            }
        }

        private void UpdateHealthState(HealthSubsystemState state)
        {
            if (state == null || _healthComponent == null) return;

            state.CurrentHealth = _healthComponent.CurrentHealth;
            state.MaxHealth = _healthComponent.MaxHealth;
            state.IsAlive = _healthComponent.CurrentHealth > 0;
        }

        private void UpdateMovementState(MovementSubsystemState state)
        {
            if (state == null) return;
            // Update movement state based on controller
        }

        private void UpdateCameraState(CameraSubsystemState state)
        {
            if (state == null) return;
            // Update camera state
        }

        private void UpdateCustomizationState(CustomizationSubsystemState state)
        {
            if (state == null) return;
            // Update customization state
        }

        #endregion

        #region Validation and Configuration

        private void ValidateConfiguration()
        {
            if (subsystemConfig == null)
            {
                Debug.LogError("[UnifiedPlayerSubsystemManager] Configuration is required but not assigned");
                return;
            }

            subsystemConfig.ValidateSettings();
            _updateInterval = 1f / subsystemConfig.updateFrequency;

            if (enableDebugLogging)
                Debug.Log("[UnifiedPlayerSubsystemManager] Configuration validated and applied");
        }

        public PlayerSubsystemConfig GetOrCreateConfig()
        {
            if (subsystemConfig == null)
            {
                subsystemConfig = PlayerSubsystemConfig.CreateDefault();
                if (enableDebugLogging)
                    Debug.Log("[UnifiedPlayerSubsystemManager] Created default configuration");
            }
            return subsystemConfig;
        }

        #endregion

        #region Public API

        public T GetSubsystem<T>() where T : class
        {
            if (typeof(T) == typeof(IHealthComponent))
                return _healthComponent as T;
            if (typeof(T) == typeof(MonoBehaviour) && _movementController != null)
                return _movementController as T;
            if (typeof(T) == typeof(ICustomizationSystem))
                return _customizationSystem as T;

            return null;
        }

        public bool HasSubsystem<T>() where T : class
        {
            return GetSubsystem<T>() != null;
        }

        public Dictionary<string, object> GetSubsystemStates()
        {
            return new Dictionary<string, object>(_subsystemStates);
        }

        public void ForceSubsystemUpdate()
        {
            UpdateSubsystems();
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            try
            {
                _healthComponent = null;
                _movementController = null;
                _cameraController = null;
                _customizationSystem = null;

                _subsystemStates?.Clear();

                _isInitialized = false;
                OnInitializationStatusChanged?.Invoke(false);

                if (enableDebugLogging)
                    Debug.Log("[UnifiedPlayerSubsystemManager] Cleanup completed");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UnifiedPlayerSubsystemManager] Error during cleanup: {ex.Message}");
            }
        }

        #endregion
    }

    #region State Classes

    [Serializable]
    public class HealthSubsystemState
    {
        public int CurrentHealth;
        public int MaxHealth;
        public bool IsAlive;
        public float LastChangedTime;
    }

    [Serializable]
    public class MovementSubsystemState
    {
        public Vector3 Velocity;
        public bool IsGrounded;
        public bool IsMoving;
        public float MovementSpeed;
        public string MovementMode;
        public float LastMovementTime;
    }

    [Serializable]
    public class CameraSubsystemState
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public string CameraMode;
        public bool IsActive;
        public float FieldOfView;
    }

    [Serializable]
    public class CustomizationSubsystemState
    {
        public bool IsCustomizing;
        public string ActiveCustomizationMode;
        public Dictionary<string, object> CustomizationData;
        public bool HasUnsavedChanges;
    }

    #endregion

    #region Event Classes

    [Serializable]
    public class PlayerStateChangedEvent
    {
        public GameObject Player { get; }
        public string OldState { get; }
        public string NewState { get; }
        public float Timestamp { get; }

        public PlayerStateChangedEvent(GameObject player, string oldState, string newState)
        {
            Player = player;
            OldState = oldState;
            NewState = newState;
            Timestamp = Time.unscaledTime;
        }
    }

    #endregion
}
