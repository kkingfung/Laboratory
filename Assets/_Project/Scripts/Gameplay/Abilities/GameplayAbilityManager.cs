using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Abilities.Interfaces;
using Laboratory.Core.Abilities.Components;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

namespace Laboratory.Gameplay.Abilities
{
    /// <summary>
    /// Gameplay-specific ability manager that handles ability instantiation and management
    /// for gameplay scenarios. Integrates with the core ability system.
    /// </summary>
    public class GameplayAbilityManager : MonoBehaviour
    {
        [Header("Ability Configuration")]
        [SerializeField] private List<BaseAbilityData> availableAbilities = new List<BaseAbilityData>();
        [SerializeField] private int maxActiveAbilities = 6;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        
        private List<IAbility> activeAbilities = new List<IAbility>();
        private Dictionary<string, IAbility> abilityLookup = new Dictionary<string, IAbility>();
        private IEventBus eventBus;
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            InitializeServices();
            InitializeAbilities();
        }
        
        private void Start()
        {
            RegisterEventHandlers();
        }
        
        private void Update()
        {
            UpdateAbilities(Time.deltaTime);
        }
        
        private void OnDestroy()
        {
            UnregisterEventHandlers();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeServices()
        {
            if (GlobalServiceProvider.IsInitialized)
            {
                eventBus = GlobalServiceProvider.Resolve<IEventBus>();
                if (enableDebugLogs)
                    Debug.Log("[GameplayAbilityManager] Services initialized");
            }
        }
        
        private void InitializeAbilities()
        {
            foreach (var abilityData in availableAbilities)
            {
                if (abilityData != null && abilityData.AbilityPrefab != null)
                {
                    var abilityInstance = CreateAbilityInstance(abilityData);
                    if (abilityInstance != null)
                    {
                        RegisterAbility(abilityInstance);
                    }
                }
            }
            
            if (enableDebugLogs)
                Debug.Log($"[GameplayAbilityManager] Initialized {activeAbilities.Count} abilities");
        }
        
        #endregion
        
        #region Ability Management
        
        /// <summary>
        /// Creates an instance of an ability from ability data
        /// </summary>
        private IAbility CreateAbilityInstance(BaseAbilityData abilityData)
        {
            try
            {
                var abilityGO = Instantiate(abilityData.AbilityPrefab, transform);
                var ability = abilityGO.GetComponent<IAbility>();
                
                if (ability == null)
                {
                    Debug.LogError($"[GameplayAbilityManager] Ability prefab {abilityData.AbilityPrefab.name} doesn't implement IAbility");
                    Destroy(abilityGO);
                    return null;
                }
                
                return ability;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameplayAbilityManager] Error creating ability instance: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Registers an ability with the manager
        /// </summary>
        private void RegisterAbility(IAbility ability)
        {
            if (ability == null || activeAbilities.Count >= maxActiveAbilities)
                return;
                
            activeAbilities.Add(ability);
            abilityLookup[ability.AbilityId] = ability;
            
            if (enableDebugLogs)
                Debug.Log($"[GameplayAbilityManager] Registered ability: {ability.DisplayName}");
        }
        
        /// <summary>
        /// Updates all active abilities
        /// </summary>
        private void UpdateAbilities(float deltaTime)
        {
            for (int i = 0; i < activeAbilities.Count; i++)
            {
                activeAbilities[i]?.UpdateAbility(deltaTime);
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Attempts to activate an ability by ID
        /// </summary>
        public bool TryActivateAbility(string abilityId)
        {
            if (abilityLookup.TryGetValue(abilityId, out var ability))
            {
                return ability.TryActivate();
            }
            
            if (enableDebugLogs)
                Debug.LogWarning($"[GameplayAbilityManager] Ability not found: {abilityId}");
            return false;
        }
        
        /// <summary>
        /// Gets an ability by ID
        /// </summary>
        public IAbility GetAbility(string abilityId)
        {
            abilityLookup.TryGetValue(abilityId, out var ability);
            return ability;
        }
        
        /// <summary>
        /// Gets all active abilities
        /// </summary>
        public IReadOnlyList<IAbility> GetAllAbilities()
        {
            return activeAbilities.AsReadOnly();
        }
        
        /// <summary>
        /// Resets all ability cooldowns (debug/cheat function)
        /// </summary>
        [ContextMenu("Reset All Cooldowns")]
        public void ResetAllCooldowns()
        {
            foreach (var ability in activeAbilities)
            {
                ability?.ResetCooldown();
            }
            
            if (enableDebugLogs)
                Debug.Log("[GameplayAbilityManager] All cooldowns reset");
        }
        
        #endregion
        
        #region Event Handling
        
        private void RegisterEventHandlers()
        {
            // Subscribe to gameplay events if needed
        }
        
        private void UnregisterEventHandlers()
        {
            // Unsubscribe from events
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data class for ability configuration
    /// </summary>
    [Serializable]
    public class BaseAbilityData
    {
        [SerializeField] public string abilityId;
        [SerializeField] public GameObject AbilityPrefab;
        [SerializeField] public string displayName;
        [SerializeField] public string description;
        [SerializeField] public Sprite icon;
        [SerializeField] public bool enabledByDefault = true;
        
        public bool IsValid => !string.IsNullOrEmpty(abilityId) && AbilityPrefab != null;
    }
}
