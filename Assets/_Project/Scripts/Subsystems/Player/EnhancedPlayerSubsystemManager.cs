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
                // Handle aiming input
                if (aimController != null)
                {
                    aimController.SetAutoTargeting(inputEvent.IsAiming);
                    if (inputEvent.IsAiming)
                    {
                        // Auto-targeting is handled by the aim controller itself
                        // We just enable/disable it based on player input
                    }
                    else
                    {
                        aimController.ClearTarget();
                    }
                }
                
                // Handle climbing input
                if (climbingController != null && inputEvent.IsClimbing)
                {
                    // The climbing controller handles its own input detection
                    // We could add specific climbing input handling here if needed
                    var climbingComponent = climbingController as ICharacterController;
                    climbingComponent?.UpdateController();
                }
                
                // Handle look input - look controllers typically update automatically
                // but we could manually set targets if needed
                if (characterLook != null && inputEvent.Look != Vector2.zero)
                {
                    // The look controller handles automatic targeting
                    // Manual look input would be handled through camera control
                }
                
                if (enableDebugLogs)
                    Debug.Log($"[PlayerSubsystem] Processed input - Move: {inputEvent.Movement}, Look: {inputEvent.Look}, Aiming: {inputEvent.IsAiming}, Climbing: {inputEvent.IsClimbing}");
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
                // Handle state changes for different controllers
                HandleLookControllerStateChange(stateEvent);
                HandleAimControllerStateChange(stateEvent);
                HandleClimbingControllerStateChange(stateEvent);
                HandleCustomizationStateChange(stateEvent);
                
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
            if (!isInitialized) return "Inactive";
            
            // Gather state from various controllers
            var stateBuilder = new System.Text.StringBuilder();
            
            if (characterLook != null)
            {
                string lookState = characterLook.CurrentTarget != null ? "Looking" : "Neutral";
                stateBuilder.Append($"Look:{lookState}");
            }
            
            if (aimController != null)
            {
                string aimState = aimController.IsAiming ? "Aiming" : "NotAiming";
                if (stateBuilder.Length > 0) stateBuilder.Append(", ");
                stateBuilder.Append($"Aim:{aimState}");
            }
            
            if (climbingController != null)
            {
                var climbingComponent = climbingController as ICharacterController;
                string climbState = "Ground"; // Default assumption
                if (stateBuilder.Length > 0) stateBuilder.Append(", ");
                stateBuilder.Append($"Climb:{climbState}");
            }
            
            return stateBuilder.Length > 0 ? stateBuilder.ToString() : "Active";
        }
        
        /// <summary>
        /// Force update all player components
        /// </summary>
        public void ForceUpdateComponents()
        {
            if (!isInitialized) return;
            
            try
            {
                // Force update character look controller
                if (characterLook != null)
                {
                    // The CharacterLookController updates automatically in Update()
                    // We can trigger a manual target selection if needed
                    characterLook.enabled = false;
                    characterLook.enabled = true;
                }
                
                // Force update aim controller
                if (aimController != null)
                {
                    var controllerInterface = aimController as ICharacterController;
                    controllerInterface?.UpdateController();
                }
                
                // Force update climbing controller
                if (climbingController != null)
                {
                    var climbingInterface = climbingController as ICharacterController;
                    climbingInterface?.UpdateController();
                }
                
                // Force update camera manager
                if (cameraManager != null)
                {
                    var cameraInterface = cameraManager as IPlayerCameraManager;
                    cameraInterface?.ForceUpdate();
                }
                
                if (enableDebugLogs)
                    Debug.Log("[PlayerSubsystem] Force update completed for all components");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerSubsystem] Error during force update: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Check if subsystem is fully initialized
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        #endregion
        
        #region Private Helper Methods
        
        /// <summary>
        /// Handle state changes for the character look controller
        /// </summary>
        private void HandleLookControllerStateChange(PlayerStateChangedEvent stateEvent)
        {
            if (characterLook == null) return;
            
            // Enable/disable look behavior based on state
            switch (stateEvent.NewState.ToLower())
            {
                case "dead":
                case "stunned":
                case "unconscious":
                    characterLook.SetLookAtEnabled(false);
                    break;
                case "aiming":
                case "combat":
                case "alert":
                    characterLook.SetLookAtEnabled(true);
                    break;
                default:
                    characterLook.SetLookAtEnabled(true);
                    break;
            }
        }
        
        /// <summary>
        /// Handle state changes for the aim controller
        /// </summary>
        private void HandleAimControllerStateChange(PlayerStateChangedEvent stateEvent)
        {
            if (aimController == null) return;
            
            // Control aiming behavior based on state
            switch (stateEvent.NewState.ToLower())
            {
                case "dead":
                case "stunned":
                case "climbing":
                case "falling":
                    aimController.SetActive(false);
                    aimController.ClearTarget();
                    break;
                case "aiming":
                case "combat":
                    aimController.SetActive(true);
                    aimController.SetAutoTargeting(true);
                    break;
                case "idle":
                case "walking":
                case "running":
                    aimController.SetActive(true);
                    aimController.SetAutoTargeting(false);
                    break;
                default:
                    aimController.SetActive(true);
                    break;
            }
        }
        
        /// <summary>
        /// Handle state changes for the climbing controller
        /// </summary>
        private void HandleClimbingControllerStateChange(PlayerStateChangedEvent stateEvent)
        {
            if (climbingController == null) return;
            
            var climbingInterface = climbingController as ICharacterController;
            if (climbingInterface == null) return;
            
            // Control climbing behavior based on state
            switch (stateEvent.NewState.ToLower())
            {
                case "dead":
                case "stunned":
                case "unconscious":
                    climbingInterface.SetActive(false);
                    break;
                case "climbing":
                case "mantling":
                    climbingInterface.SetActive(true);
                    break;
                case "combat":
                case "aiming":
                    // Disable climbing during combat/aiming
                    climbingInterface.SetActive(false);
                    break;
                default:
                    climbingInterface.SetActive(true);
                    break;
            }
        }
        
        /// <summary>
        /// Handle state changes for the customization manager
        /// </summary>
        private void HandleCustomizationStateChange(PlayerStateChangedEvent stateEvent)
        {
            if (customization == null) return;
            
            // Handle visual customization based on state
            switch (stateEvent.NewState.ToLower())
            {
                case "dead":
                    // Could trigger death visual effects, ragdoll, etc.
                    PublishCustomizationEvent("Death", stateEvent.NewState);
                    break;
                case "damaged":
                    // Could trigger damage visual effects
                    PublishCustomizationEvent("Damage", stateEvent.NewState);
                    break;
                case "healed":
                    // Could trigger healing visual effects
                    PublishCustomizationEvent("Heal", stateEvent.NewState);
                    break;
                case "climbing":
                    // Could adjust character pose/appearance for climbing
                    PublishCustomizationEvent("Climbing", stateEvent.NewState);
                    break;
                default:
                    // Normal state - ensure default appearance
                    PublishCustomizationEvent("Normal", stateEvent.NewState);
                    break;
            }
        }
        
        /// <summary>
        /// Publishes customization events for visual state changes
        /// </summary>
        private void PublishCustomizationEvent(string eventType, string newState)
        {
            try
            {
                var customEvent = new PlayerCustomizationEvent(eventType, newState);
                eventBus?.Publish(customEvent);
                
                if (enableDebugLogs)
                    Debug.Log($"[PlayerSubsystem] Published customization event: {eventType} for state {newState}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerSubsystem] Error publishing customization event: {ex.Message}");
            }
        }
        
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
    
    public class PlayerCustomizationEvent
    {
        public string EventType { get; }
        public string PlayerState { get; }
        public System.DateTime EventTime { get; } = System.DateTime.Now;
        
        public PlayerCustomizationEvent(string eventType, string playerState)
        {
            EventType = eventType;
            PlayerState = playerState;
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