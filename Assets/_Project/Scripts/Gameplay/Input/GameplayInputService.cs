using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Events.Messages;

namespace Laboratory.Gameplay.Input
{
    /// <summary>
    /// Service for handling gameplay-specific input operations.
    /// This bridges the gap between input systems and gameplay mechanics,
    /// providing high-level gameplay actions and context-aware input handling.
    /// </summary>
    public class GameplayInputService : MonoBehaviour
    {
        #region Configuration

        [Header("Input Configuration")]
        [SerializeField] private bool enableInputLogging = false;
        [SerializeField] private bool enableInputValidation = true;
        [SerializeField] private float inputCooldownTime = 0.1f;

        [Header("Gameplay Context")]
        [SerializeField] private bool isPaused = false;
        [SerializeField] private bool acceptInput = true;

        #endregion

        #region Private Fields

        private IEventBus _eventBus;
        private PlayerInput _playerInput;
        private bool _isInitialized = false;
        private float _lastInputTime = 0f;

        // Input action references
        private InputAction _attackAction;
        private InputAction _interactAction;
        private InputAction _pauseAction;
        private InputAction _inventoryAction;
        private InputAction _ability1Action;
        private InputAction _ability2Action;
        private InputAction _ability3Action;

        #endregion

        #region Events

        /// <summary>
        /// Event triggered when a gameplay action is performed.
        /// </summary>
        public event Action<GameplayInputType, Vector2> OnGameplayAction;

        /// <summary>
        /// Event triggered when input context changes (e.g., paused/unpaused).
        /// </summary>
        public event Action<bool> OnInputContextChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Whether the service is properly initialized and ready to process input.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Whether input is currently paused (e.g., during menus).
        /// </summary>
        public bool IsPaused
        {
            get => isPaused;
            set
            {
                if (isPaused != value)
                {
                    isPaused = value;
                    OnInputContextChanged?.Invoke(isPaused);
                    LogInput($"Input context changed: Paused = {isPaused}");
                }
            }
        }

        /// <summary>
        /// Whether the service should accept and process input.
        /// </summary>
        public bool AcceptInput
        {
            get => acceptInput;
            set => acceptInput = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeInputActions();
        }

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            EnableInputActions();
        }

        private void OnDisable()
        {
            DisableInputActions();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the gameplay input service.
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Get event bus from service container
                if (GlobalServiceProvider.IsInitialized &&
                    GlobalServiceProvider.Instance.TryResolve<IEventBus>(out var eventBus))
                {
                    _eventBus = eventBus;
                }

                // Get PlayerInput component
                _playerInput = GetComponent<PlayerInput>();
                if (_playerInput == null)
                {
                    _playerInput = FindFirstObjectByType<PlayerInput>();
                }

                if (_playerInput == null)
                {
                    Debug.LogWarning("[GameplayInputService] No PlayerInput component found. Some functionality may be limited.");
                }

                _isInitialized = true;
                LogInput("GameplayInputService initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayInputService] Failed to initialize: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Processes gameplay input events. Called from Update or input callbacks.
        /// </summary>
        public void ProcessInput()
        {
            if (!ShouldProcessInput())
                return;

            // Input processing is handled by individual action callbacks
            // This method can be used for additional processing if needed
        }

        /// <summary>
        /// Sets the input pause state for gameplay actions.
        /// </summary>
        /// <param name="paused">Whether input should be paused</param>
        public void SetPaused(bool paused)
        {
            IsPaused = paused;
        }

        /// <summary>
        /// Temporarily disables all input for a specified duration.
        /// </summary>
        /// <param name="duration">Duration in seconds to disable input</param>
        public void DisableInputTemporarily(float duration)
        {
            if (duration <= 0) return;

            AcceptInput = false;
            Invoke(nameof(ReenableInput), duration);
        }

        /// <summary>
        /// Forces a specific gameplay action to be triggered programmatically.
        /// </summary>
        /// <param name="inputType">Type of gameplay input to trigger</param>
        /// <param name="context">Optional context data (e.g., mouse position)</param>
        public void TriggerGameplayAction(GameplayInputType inputType, Vector2 context = default)
        {
            if (!_isInitialized) return;

            LogInput($"Programmatically triggered: {inputType}");
            OnGameplayAction?.Invoke(inputType, context);
            PublishGameplayInputEvent(inputType, context);
        }

        /// <summary>
        /// Cleanup method for the input service.
        /// </summary>
        public void Cleanup()
        {
            try
            {
                DisableInputActions();
                _eventBus = null;
                _isInitialized = false;
                LogInput("GameplayInputService cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayInputService] Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private void InitializeInputActions()
        {
            try
            {
                // Get input actions from PlayerInput component
                if (_playerInput != null && _playerInput.actions != null)
                {
                    var actionMap = _playerInput.actions.FindActionMap("Gameplay") ?? 
                                   _playerInput.actions.FindActionMap("Player");

                    if (actionMap != null)
                    {
                        _attackAction = actionMap.FindAction("Attack");
                        _interactAction = actionMap.FindAction("Interact") ?? actionMap.FindAction("Action");
                        _pauseAction = actionMap.FindAction("Pause");
                        _inventoryAction = actionMap.FindAction("Inventory");
                        _ability1Action = actionMap.FindAction("Ability1") ?? actionMap.FindAction("WeaponSkill");
                        _ability2Action = actionMap.FindAction("Ability2") ?? actionMap.FindAction("CharSkill");
                        _ability3Action = actionMap.FindAction("Ability3");
                    }
                }

                LogInput("Input actions initialized");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayInputService] Failed to initialize input actions: {ex.Message}");
            }
        }

        private void EnableInputActions()
        {
            try
            {
                // Subscribe to input action events
                if (_attackAction != null)
                    _attackAction.started += OnAttackInput;
                if (_interactAction != null)
                    _interactAction.started += OnInteractInput;
                if (_pauseAction != null)
                    _pauseAction.started += OnPauseInput;
                if (_inventoryAction != null)
                    _inventoryAction.started += OnInventoryInput;
                if (_ability1Action != null)
                    _ability1Action.started += OnAbility1Input;
                if (_ability2Action != null)
                    _ability2Action.started += OnAbility2Input;
                if (_ability3Action != null)
                    _ability3Action.started += OnAbility3Input;

                LogInput("Input actions enabled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayInputService] Failed to enable input actions: {ex.Message}");
            }
        }

        private void DisableInputActions()
        {
            try
            {
                // Unsubscribe from input action events
                if (_attackAction != null)
                    _attackAction.started -= OnAttackInput;
                if (_interactAction != null)
                    _interactAction.started -= OnInteractInput;
                if (_pauseAction != null)
                    _pauseAction.started -= OnPauseInput;
                if (_inventoryAction != null)
                    _inventoryAction.started -= OnInventoryInput;
                if (_ability1Action != null)
                    _ability1Action.started -= OnAbility1Input;
                if (_ability2Action != null)
                    _ability2Action.started -= OnAbility2Input;
                if (_ability3Action != null)
                    _ability3Action.started -= OnAbility3Input;

                LogInput("Input actions disabled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayInputService] Failed to disable input actions: {ex.Message}");
            }
        }

        private bool ShouldProcessInput()
        {
            if (!_isInitialized || !acceptInput || isPaused)
                return false;

            // Check cooldown to prevent input spam
            if (enableInputValidation && Time.time - _lastInputTime < inputCooldownTime)
                return false;

            return true;
        }

        private void ReenableInput()
        {
            AcceptInput = true;
            LogInput("Input re-enabled after temporary disable");
        }

        private void LogInput(string message)
        {
            if (enableInputLogging)
            {
                Debug.Log($"[GameplayInputService] {message}");
            }
        }

        private void PublishGameplayInputEvent(GameplayInputType inputType, Vector2 context)
        {
            if (_eventBus == null) return;

            try
            {
                var inputEvent = new GameplayInputEvent(inputType, context, gameObject);
                _eventBus.Publish(inputEvent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayInputService] Failed to publish input event: {ex.Message}");
            }
        }

        #endregion

        #region Input Action Callbacks

        private void OnAttackInput(InputAction.CallbackContext context)
        {
            if (!ShouldProcessInput()) return;

            _lastInputTime = Time.time;
            var mousePosition = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            
            LogInput("Attack input received");
            OnGameplayAction?.Invoke(GameplayInputType.Attack, mousePosition);
            PublishGameplayInputEvent(GameplayInputType.Attack, mousePosition);
        }

        private void OnInteractInput(InputAction.CallbackContext context)
        {
            if (!ShouldProcessInput()) return;

            _lastInputTime = Time.time;
            
            LogInput("Interact input received");
            OnGameplayAction?.Invoke(GameplayInputType.Interact, Vector2.zero);
            PublishGameplayInputEvent(GameplayInputType.Interact, Vector2.zero);
        }

        private void OnPauseInput(InputAction.CallbackContext context)
        {
            // Pause input should work even when input is "paused" for gameplay
            if (!_isInitialized || !acceptInput) return;

            _lastInputTime = Time.time;
            
            LogInput("Pause input received");
            OnGameplayAction?.Invoke(GameplayInputType.Pause, Vector2.zero);
            PublishGameplayInputEvent(GameplayInputType.Pause, Vector2.zero);
        }

        private void OnInventoryInput(InputAction.CallbackContext context)
        {
            if (!ShouldProcessInput()) return;

            _lastInputTime = Time.time;
            
            LogInput("Inventory input received");
            OnGameplayAction?.Invoke(GameplayInputType.Inventory, Vector2.zero);
            PublishGameplayInputEvent(GameplayInputType.Inventory, Vector2.zero);
        }

        private void OnAbility1Input(InputAction.CallbackContext context)
        {
            if (!ShouldProcessInput()) return;

            _lastInputTime = Time.time;
            
            LogInput("Ability 1 input received");
            OnGameplayAction?.Invoke(GameplayInputType.Ability1, Vector2.zero);
            PublishGameplayInputEvent(GameplayInputType.Ability1, Vector2.zero);
        }

        private void OnAbility2Input(InputAction.CallbackContext context)
        {
            if (!ShouldProcessInput()) return;

            _lastInputTime = Time.time;
            
            LogInput("Ability 2 input received");
            OnGameplayAction?.Invoke(GameplayInputType.Ability2, Vector2.zero);
            PublishGameplayInputEvent(GameplayInputType.Ability2, Vector2.zero);
        }

        private void OnAbility3Input(InputAction.CallbackContext context)
        {
            if (!ShouldProcessInput()) return;

            _lastInputTime = Time.time;
            
            LogInput("Ability 3 input received");
            OnGameplayAction?.Invoke(GameplayInputType.Ability3, Vector2.zero);
            PublishGameplayInputEvent(GameplayInputType.Ability3, Vector2.zero);
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Test Attack Input")]
        private void DebugTestAttackInput()
        {
            TriggerGameplayAction(GameplayInputType.Attack, Vector2.zero);
        }

        [ContextMenu("Test Interact Input")]
        private void DebugTestInteractInput()
        {
            TriggerGameplayAction(GameplayInputType.Interact, Vector2.zero);
        }

        [ContextMenu("Toggle Input Pause")]
        private void DebugTogglePause()
        {
            IsPaused = !IsPaused;
        }

        #endregion
    }

    #region Supporting Types

    /// <summary>
    /// Enumeration of gameplay input types that can be processed by the service.
    /// </summary>
    public enum GameplayInputType
    {
        Attack,
        Interact,
        Pause,
        Inventory,
        Ability1,
        Ability2,
        Ability3,
        Movement,
        Look,
        Jump,
        Run,
        Crouch
    }

    /// <summary>
    /// Event data for gameplay input events.
    /// </summary>
    public class GameplayInputEvent
    {
        public GameplayInputType InputType { get; }
        public Vector2 Context { get; }
        public GameObject Source { get; }
        public float Timestamp { get; }

        public GameplayInputEvent(GameplayInputType inputType, Vector2 context, GameObject source)
        {
            InputType = inputType;
            Context = context;
            Source = source;
            Timestamp = Time.time;
        }
    }

    #endregion
}