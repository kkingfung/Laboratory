using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Timing;
using Laboratory.Core.Events;
using Laboratory.Core.Systems;
using Laboratory.Core.Abilities.Events;
using Laboratory.Core.Abilities;
using Laboratory.Core.Abilities.Systems;
using Laboratory.Infrastructure.Core;
using Laboratory.Core.Infrastructure;

namespace Laboratory.Subsystems.Combat.Abilities
{
    /// <summary>
    /// Enhanced ability manager with proper event integration and improved architecture.
    /// Manages player abilities, cooldowns, and activation logic with full event system support.
    /// </summary>
    public class AbilityManager : MonoBehaviour, IAbilityManager, IAbilityManagerCore
    {
        #region Constants

        private const float DefaultCooldown = 5f;

        #endregion

        #region Fields

        [Header("Configuration")]
        [SerializeField] private int abilityCount = 3;
        [SerializeField] private bool enableDebugLogs = false;
        
        [Header("Ability Data")]
        [SerializeField] private List<AbilityData> abilityDataList = new();

        private readonly List<CooldownTimer> _abilityCooldowns = new();
        private readonly List<AbilityData> _abilities = new();
        private readonly Dictionary<int, bool> _abilityStates = new();

        // Cache event bus reference
        private IEventBus _eventBus;

        #endregion

        #region Data Structures

        [System.Serializable]
        public class AbilityData
        {
            [Header("Basic Info")]
            public string name = "Unnamed Ability";
            public string description = "";
            
            [Header("Timing")]
            public float cooldownDuration = DefaultCooldown;
            public float castTime = 0f;
            
            [Header("Visual")]
            public Sprite icon;
            
            public int index { get; set; }

            public AbilityData() { }

            public AbilityData(int index, string name = null, float cooldown = DefaultCooldown)
            {
                this.index = index;
                this.name = string.IsNullOrEmpty(name) ? $"Ability {index + 1}" : name;
                this.cooldownDuration = cooldown;
            }

            /// <summary>
            /// Virtual method to check if ability can be activated.
            /// Override in derived classes for custom logic.
            /// </summary>
            public virtual bool CanActivate(AbilityManager manager)
            {
                return !manager.IsAbilityOnCooldown(index);
            }

            /// <summary>
            /// Virtual method called when ability is activated.
            /// Override in derived classes for custom effects.
            /// </summary>
            public virtual void OnActivate(AbilityManager manager)
            {
                Debug.Log($"[AbilityManager] Activated ability: {name}");
            }
        }

        #endregion

        #region IAbilityManager Events

        /// <summary>
        /// Event triggered when an ability is activated.
        /// </summary>
        public event Action<int> OnAbilityActivated;
        
        /// <summary>
        /// Event triggered when an ability's cooldown completes (IAbilityManagerCore).
        /// </summary>
        public event Action<int> OnAbilityCooldownComplete;
        
        /// <summary>
        /// Event triggered when an ability's state changes (IAbilityManagerCore - 3 parameters).
        /// </summary>
        event Action<int, bool, float> IAbilityManagerCore.OnAbilityStateChanged
        {
            add { _coreStateChanged += value; }
            remove { _coreStateChanged -= value; }
        }
        
        /// <summary>
        /// Event triggered when an ability's state changes (IAbilityManager - 2 parameters).
        /// </summary>
        event Action<int, bool> IAbilityManager.OnAbilityStateChanged
        {
            add { _managerStateChanged += value; }
            remove { _managerStateChanged -= value; }
        }
        
        // Backing events for explicit implementations
        private event Action<int, bool, float> _coreStateChanged;
        private event Action<int, bool> _managerStateChanged;

        #endregion

        #region Properties

        public int AbilityCount => abilityCount;
        public IReadOnlyList<AbilityData> Abilities => _abilities.AsReadOnly();
        public GameObject GameObject => gameObject;

        #endregion

        #region Unity Override Methods

        private void Awake()
        {
            InitializeAbilities();
            RegisterWithAbilitySystem();
        }

        private void Start()
        {
            // Initialize event bus reference
            InitializeEventBus();
            
            // Subscribe to events after all systems are initialized
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CleanupTimers();
            UnregisterFromAbilitySystem();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Attempts to activate an ability by index.
        /// </summary>
        /// <param name="index">Index of the ability to activate</param>
        /// <returns>True if ability was activated successfully</returns>
        public bool ActivateAbility(int index)
        {
            if (!ValidateAbilityIndex(index))
                return false;

            var ability = _abilities[index];
            var cooldownTimer = _abilityCooldowns[index];

            // Check if ability can be activated
            if (!ability.CanActivate(this))
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[AbilityManager] Cannot activate ability {index}: {ability.name}");
                return false;
            }

            // Start cooldown
            cooldownTimer.Start();
            _abilityStates[index] = true;

            // Execute ability effect
            ability.OnActivate(this);

            // Publish events
            PublishAbilityActivated(index);
            PublishAbilityStateChanged(index, true, cooldownTimer.Duration);
            
            // Trigger interface events
            OnAbilityActivated?.Invoke(index);
            _coreStateChanged?.Invoke(index, true, cooldownTimer.Duration);
            _managerStateChanged?.Invoke(index, true);

            if (enableDebugLogs)
                Debug.Log($"[AbilityManager] Successfully activated ability {index}: {ability.name}");

            return true;
        }

        /// <summary>
        /// Attempts to activate an ability only if it's not on cooldown.
        /// </summary>
        public bool TryActivateAbility(int index)
        {
            if (!ValidateAbilityIndex(index))
                return false;

            if (IsAbilityOnCooldown(index))
                return false;

            return ActivateAbility(index);
        }
        
        /// <summary>
        /// Gets the ability at the specified index (IAbilityManagerCore implementation).
        /// Note: This returns null as this manager uses AbilityData instead of AbilityBase.
        /// </summary>
        public AbilityBase GetAbility(int abilityIndex)
        {
            // This implementation uses AbilityData instead of AbilityBase
            // Return null to satisfy interface requirements
            return null;
        }
        
        /// <summary>
        /// Gets all abilities (IAbilityManagerCore implementation).
        /// Note: This returns empty list as this manager uses AbilityData instead of AbilityBase.
        /// </summary>
        public IReadOnlyList<AbilityBase> GetAllAbilities()
        {
            // This implementation uses AbilityData instead of AbilityBase
            // Return empty list to satisfy interface requirements
            return new List<AbilityBase>().AsReadOnly();
        }

        /// <summary>
        /// Gets the remaining cooldown time for an ability.
        /// </summary>
        /// <param name="index">Ability index</param>
        /// <returns>Remaining cooldown time in seconds</returns>
        public float GetAbilityCooldown(int index)
        {
            if (!ValidateAbilityIndex(index))
                return 0f;

            return _abilityCooldowns[index].Remaining;
        }

        /// <summary>
        /// Checks if an ability is currently on cooldown.
        /// </summary>
        /// <param name="index">Ability index</param>
        /// <returns>True if ability is on cooldown</returns>
        public bool IsAbilityOnCooldown(int index)
        {
            if (!ValidateAbilityIndex(index))
                return true; // Safer to return true for invalid indices

            return _abilityCooldowns[index].IsActive;
        }

        /// <summary>
        /// Gets the progress of an ability's cooldown (0 = ready, 1 = just activated).
        /// </summary>
        public float GetAbilityCooldownProgress(int index)
        {
            if (!ValidateAbilityIndex(index))
                return 0f;

            return _abilityCooldowns[index].Progress;
        }

        /// <summary>
        /// Gets ability data by index.
        /// </summary>
        object IAbilityManager.GetAbilityData(int index)
        {
            if (!ValidateAbilityIndex(index))
                return null;

            return _abilities[index];
        }
        
        /// <summary>
        /// Gets ability data by index (strongly typed version).
        /// </summary>
        public AbilityData GetAbilityData(int index)
        {
            if (!ValidateAbilityIndex(index))
                return null;

            return _abilities[index];
        }

        /// <summary>
        /// Resets all ability cooldowns. Useful for testing or special events.
        /// </summary>
        public void ResetAllCooldowns()
        {
            for (int i = 0; i < _abilityCooldowns.Count; i++)
            {
                _abilityCooldowns[i].Reset();
                _abilityStates[i] = false;
                PublishAbilityStateChanged(i, false, 0f);
            }

            if (enableDebugLogs)
                Debug.Log("[AbilityManager] Reset all ability cooldowns");
        }

        #endregion

        #region Private Methods

        private bool ValidateAbilityIndex(int index)
        {
            if (index < 0 || index >= abilityCount)
            {
                Debug.LogError($"[AbilityManager] Invalid ability index: {index}. Valid range: 0-{abilityCount - 1}");
                return false;
            }
            return true;
        }

        private void InitializeAbilities()
        {
            _abilities.Clear();
            _abilityCooldowns.Clear();
            _abilityStates.Clear();
            
            // Use provided ability data or create default
            for (int i = 0; i < abilityCount; i++)
            {
                AbilityData abilityData;
                
                if (i < abilityDataList.Count && abilityDataList[i] != null)
                {
                    abilityData = abilityDataList[i];
                    abilityData.index = i;
                }
                else
                {
                    abilityData = new AbilityData(i);
                }
                
                _abilities.Add(abilityData);
                _abilityStates[i] = false;
                
                // Create cooldown timer with event callbacks
                var cooldownTimer = new CooldownTimer(abilityData.cooldownDuration);
                cooldownTimer.OnCompleted += () => OnAbilityCooldownCompleted(i);
                _abilityCooldowns.Add(cooldownTimer);
            }

            if (enableDebugLogs)
                Debug.Log($"[AbilityManager] Initialized {abilityCount} abilities");
        }

        private void OnAbilityCooldownCompleted(int abilityIndex)
        {
            _abilityStates[abilityIndex] = false;
            
            // Fire ability ready events
            PublishAbilityStateChanged(abilityIndex, false, 0f);
            PublishAbilityCooldownComplete(abilityIndex);
            
            // Trigger interface events
            _coreStateChanged?.Invoke(abilityIndex, false, 0f);
            _managerStateChanged?.Invoke(abilityIndex, false);
            OnAbilityCooldownComplete?.Invoke(abilityIndex);

            if (enableDebugLogs)
                Debug.Log($"[AbilityManager] Ability {abilityIndex} cooldown completed: {_abilities[abilityIndex].name}");
        }

        #region Event Integration

        private void InitializeEventBus()
        {
            // Try to get the event bus from the service container
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
            }
            else
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[AbilityManager] Event bus not available - events will not be published");
            }
        }

        private void PublishAbilityActivated(int index)
        {
            if (_eventBus == null) return;

            var evt = new AbilityActivatedEvent(index, gameObject);
            
            try
            {
                _eventBus.Publish(evt);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AbilityManager] Failed to publish AbilityActivatedEvent: {ex.Message}");
            }
        }

        private void PublishAbilityStateChanged(int index, bool isOnCooldown, float cooldownRemaining)
        {
            if (_eventBus == null) return;

            var evt = new AbilityStateChangedEvent(index, isOnCooldown, cooldownRemaining, gameObject);
            
            try
            {
                _eventBus.Publish(evt);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AbilityManager] Failed to publish AbilityStateChangedEvent: {ex.Message}");
            }
        }

        private void PublishAbilityCooldownComplete(int index)
        {
            if (_eventBus == null) return;

            var evt = new AbilityCooldownCompleteEvent(index, gameObject);
            
            try
            {
                _eventBus.Publish(evt);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AbilityManager] Failed to publish AbilityCooldownCompleteEvent: {ex.Message}");
            }
        }

        #endregion

        #region System Integration

        private void RegisterWithAbilitySystem()
        {
            // Register with global ability system if available
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                var abilitySystem = serviceContainer.ResolveService<IAbilitySystem>();
                abilitySystem?.RegisterAbilityManager(this);
            }
        }

        private void UnregisterFromAbilitySystem()
        {
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                var abilitySystem = serviceContainer.ResolveService<IAbilitySystem>();
                abilitySystem?.UnregisterAbilityManager(this);
            }
        }

        private void SubscribeToEvents()
        {
            // Subscribe to any global events if needed
            // This is where you'd subscribe to external ability activation requests
        }

        private void UnsubscribeFromEvents()
        {
            // Clean up event subscriptions
        }

        private void CleanupTimers()
        {
            foreach (var cooldown in _abilityCooldowns)
            {
                cooldown?.Dispose();
            }
            _abilityCooldowns.Clear();
        }

        #endregion

        #endregion
    }
}
