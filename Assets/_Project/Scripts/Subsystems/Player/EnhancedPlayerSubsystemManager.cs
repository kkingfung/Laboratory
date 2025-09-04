using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Character;
using Laboratory.Core.Character.Controllers;
using Laboratory.Core.Camera;
using Laboratory.Core.Input;
using Laboratory.Subsystems.Player;
using System;

namespace Laboratory.Player
{
    /// <summary>
    /// Enhanced Player Subsystem Manager with comprehensive initialization and management
    /// Version 3.1 - Improved event handling and component management
    /// </summary>
    public class EnhancedPlayerSubsystemManager : MonoBehaviour
    {
        [Header("Player Components")]
        [SerializeField] private CharacterLookController characterLook;
        [SerializeField] private CharacterCustomizationManager customization;
        [SerializeField] private UnifiedAimController aimController;
        [SerializeField] private PlayerCameraManager cameraManager;
        [SerializeField] private ClimbingController climbingController;

        [Header("Configuration")]
        [SerializeField] private PlayerSubsystemConfig config;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        private IEventBus eventBus;
        private IServiceContainer serviceContainer;
        private bool isInitialized = false;

        private void Awake()
        {
            InitializeSubsystem();
        }

        private void InitializeSubsystem()
        {
            try
            {
                // Get core services
                serviceContainer = GlobalServiceProvider.Instance;
                eventBus = serviceContainer.Resolve<IEventBus>();
                
                // Initialize all player components
                InitializeComponents();
                
                // Register services
                RegisterServices();
                
                // Subscribe to relevant events
                SubscribeToEvents();
                
                isInitialized = true;
                
                if (enableDebugLogs)
                    Debug.Log("[PlayerSubsystem] Successfully initialized with all components");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerSubsystem] Failed to initialize: {ex.Message}");
            }
        }

        private void InitializeComponents()
        {
            // Auto-discover components if not assigned
            if (characterLook == null)
                characterLook = GetComponent<CharacterLookController>();
                
            if (customization == null) 
                customization = GetComponent<CharacterCustomizationManager>();
                
            if (aimController == null)
                aimController = GetComponent<UnifiedAimController>();
                
            if (cameraManager == null)
                cameraManager = FindFirstObjectByType<PlayerCameraManager>();
                
            if (climbingController == null)
                climbingController = GetComponent<ClimbingController>();

            // Validate critical components
            if (characterLook == null)
                Debug.LogWarning("[PlayerSubsystem] CharacterLookController not found - some features may not work");
                
            if (aimController == null)
                Debug.LogWarning("[PlayerSubsystem] UnifiedAimController not found - aiming disabled");
        }

        private void RegisterServices()
        {
            // Register components as services for other systems to access
            if (characterLook != null)
                serviceContainer.RegisterInstance<CharacterLookController>(characterLook);
                
            if (aimController != null)
                serviceContainer.RegisterInstance<UnifiedAimController>(aimController);
                
            if (cameraManager != null)
                serviceContainer.RegisterInstance<PlayerCameraManager>(cameraManager);
        }

        private IDisposable[] eventSubscriptions;

        private void SubscribeToEvents()
        {
            // Subscribe to player-related events and store subscriptions for disposal
            eventSubscriptions = new IDisposable[]
            {
                eventBus.Subscribe<PlayerInputEvent>(HandlePlayerInput),
                eventBus.Subscribe<PlayerStateChangedEvent>(HandlePlayerStateChanged),
                eventBus.Subscribe<PlayerHealthChangedEvent>(HandlePlayerHealthChanged)
            };
        }

        private void HandlePlayerInput(PlayerInputEvent inputEvent)
        {
            if (!isInitialized) return;
            
            // Forward input to appropriate components based on current state
            try
            {
                // TODO: Implement HandleInput methods on controllers
                // aimController?.HandleInput(inputEvent.Movement, inputEvent.Look, inputEvent.IsAiming);
                // climbingController?.HandleInput(inputEvent.Movement, inputEvent.IsClimbing);
                // characterLook?.HandleLookInput(inputEvent.Look);
                
                if (enableDebugLogs)
                    Debug.Log($"[PlayerSubsystem] Processed input - Move: {inputEvent.Movement}, Look: {inputEvent.Look}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerSubsystem] Error handling input: {ex.Message}");
            }
        }

        private void HandlePlayerStateChanged(PlayerStateChangedEvent stateEvent)
        {
            if (!isInitialized) return;
            
            try
            {
                // TODO: Implement OnPlayerStateChanged methods on controllers
                // characterLook?.OnPlayerStateChanged(stateEvent.NewState);
                // aimController?.OnPlayerStateChanged(stateEvent.NewState);
                // customization?.OnPlayerStateChanged(stateEvent.NewState);
                
                if (enableDebugLogs)
                    Debug.Log($"[PlayerSubsystem] State changed: {stateEvent.PreviousState} → {stateEvent.NewState}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerSubsystem] Error handling state change: {ex.Message}");
            }
        }

        private void HandlePlayerHealthChanged(PlayerHealthChangedEvent healthEvent)
        {
            if (!isInitialized) return;
            
            // Update visual feedback based on health changes
            try
            {
                if (healthEvent.NewHealth <= 0)
                {
                    // Handle player death
                    eventBus.Publish(new PlayerDeathEvent());
                }
                else if (healthEvent.NewHealth < healthEvent.PreviousHealth)
                {
                    // Handle damage taken - could trigger screen effects, etc.
                    float damageTaken = healthEvent.PreviousHealth - healthEvent.NewHealth;
                    eventBus.Publish(new PlayerDamagedEvent(damageTaken));
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerSubsystem] Error handling health change: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            // Dispose of all event subscriptions
            if (eventSubscriptions != null)
            {
                foreach (var subscription in eventSubscriptions)
                {
                    subscription?.Dispose();
                }
                eventSubscriptions = null;
            }
        }

        #region Public API
        
        /// <summary>
        /// Get the current player state
        /// </summary>
        public string GetCurrentPlayerState()
        {
            // TODO: Implement GetCurrentState method on CharacterLookController
            // return characterLook?.GetCurrentState() ?? "Unknown";
            return isInitialized ? "Active" : "Inactive";
        }
        
        /// <summary>
        /// Force update all player components
        /// </summary>
        public void ForceUpdateComponents()
        {
            if (isInitialized)
            {
                // TODO: Implement ForceUpdate methods on controllers
                // characterLook?.ForceUpdate();
                // aimController?.ForceUpdate();
                // cameraManager?.ForceUpdate();
                
                if (enableDebugLogs)
                    Debug.Log("[PlayerSubsystem] Force update requested (methods not implemented)");
            }
        }
        
        /// <summary>
        /// Check if subsystem is fully initialized
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        #endregion
    }

    // Enhanced Player Events
    public class PlayerInputEvent
    {
        public Vector2 Movement { get; }
        public Vector2 Look { get; }  
        public bool IsAiming { get; }
        public bool IsClimbing { get; }
        public bool IsRunning { get; }

        public PlayerInputEvent(Vector2 movement, Vector2 look, bool isAiming, bool isClimbing, bool isRunning = false)
        {
            Movement = movement;
            Look = look;
            IsAiming = isAiming;
            IsClimbing = isClimbing;
            IsRunning = isRunning;
        }
    }

    public class PlayerStateChangedEvent
    {
        public string PreviousState { get; }
        public string NewState { get; }
        public float TransitionTime { get; }
        
        public PlayerStateChangedEvent(string previousState, string newState, float transitionTime = 0f)
        {
            PreviousState = previousState;
            NewState = newState;
            TransitionTime = transitionTime;
        }
    }

    public class PlayerHealthChangedEvent
    {
        public float PreviousHealth { get; }
        public float NewHealth { get; }
        public float MaxHealth { get; }
        
        public PlayerHealthChangedEvent(float previousHealth, float newHealth, float maxHealth)
        {
            PreviousHealth = previousHealth;
            NewHealth = newHealth;
            MaxHealth = maxHealth;
        }
    }

    public class PlayerDeathEvent
    {
        public System.DateTime TimeOfDeath { get; } = System.DateTime.Now;
    }

    public class PlayerDamagedEvent
    {
        public float DamageAmount { get; }
        public System.DateTime DamageTime { get; } = System.DateTime.Now;
        
        public PlayerDamagedEvent(float damageAmount)
        {
            DamageAmount = damageAmount;
        }
    }

    // Player Camera Manager Interface
    public interface IPlayerCameraManager
    {
        void SetTarget(Transform target);
        void SetCameraMode(string mode);
        void ForceUpdate();
    }
}