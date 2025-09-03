using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.Abilities.Events;
using Laboratory.Core.Systems;

namespace Laboratory.Core.Abilities.Systems
{
    /// <summary>
    /// Unified Ability System that manages all ability managers and provides centralized control.
    /// Implements the IAbilitySystem interface for consistent access across the game.
    /// </summary>
    [CreateAssetMenu(fileName = "AbilitySystem", menuName = "Laboratory/Systems/Ability System")]
    public class UnifiedAbilitySystem : ScriptableObject, IAbilitySystem
    {
        #region Fields

        [Header("System Settings")]
        [SerializeField] private bool enableSystemLogs = true;
        
        private readonly List<IAbilityManagerCore> _registeredManagers = new();
        private readonly Dictionary<IAbilityManagerCore, int> _managerIds = new();
        private int _nextManagerId = 0;

        #endregion

        #region Events

        public event Action<IAbilityManagerCore, int> OnAbilityActivated;
        public event Action<IAbilityManagerCore, int> OnAbilityCooldownComplete;
        public event Action<IAbilityManagerCore, int, bool, float> OnAbilityStateChanged;

        #endregion

        #region IAbilitySystem Implementation

        public void RegisterAbilityManager(IAbilityManagerCore abilityManager)
        {
            if (abilityManager == null)
            {
                Debug.LogError("[UnifiedAbilitySystem] Cannot register null ability manager");
                return;
            }

            if (_registeredManagers.Contains(abilityManager))
            {
                if (enableSystemLogs)
                    Debug.Log($"[UnifiedAbilitySystem] Manager (ID: {_managerIds[abilityManager]}) is already registered, skipping...");
                return;
            }

            _registeredManagers.Add(abilityManager);
            _managerIds[abilityManager] = _nextManagerId++;

            if (enableSystemLogs)
                Debug.Log($"[UnifiedAbilitySystem] Registered AbilityManager (ID: {_managerIds[abilityManager]})");

            // Subscribe to manager events
            SubscribeToManagerEvents(abilityManager);
        }

        public void UnregisterAbilityManager(IAbilityManagerCore abilityManager)
        {
            if (abilityManager == null) return;

            if (!_registeredManagers.Contains(abilityManager))
            {
                if (enableSystemLogs)
                    Debug.Log($"[UnifiedAbilitySystem] Manager was not registered, skipping unregistration...");
                return;
            }

            // Get the ID before removing for logging
            int managerId = _managerIds.TryGetValue(abilityManager, out int id) ? id : -1;

            // Unsubscribe from manager events
            UnsubscribeFromManagerEvents(abilityManager);

            _registeredManagers.Remove(abilityManager);
            _managerIds.Remove(abilityManager);

            if (enableSystemLogs)
                Debug.Log($"[UnifiedAbilitySystem] Unregistered AbilityManager (ID: {managerId})");
        }

        public bool TryActivateAbility(IAbilityManagerCore manager, int abilityIndex)
        {
            if (!ValidateManager(manager))
                return false;

            return manager.ActivateAbility(abilityIndex);
        }

        public float GetAbilityCooldown(IAbilityManagerCore manager, int abilityIndex)
        {
            if (!ValidateManager(manager))
                return 0f;

            return manager.GetAbilityCooldown(abilityIndex);
        }

        public bool IsAbilityOnCooldown(IAbilityManagerCore manager, int abilityIndex)
        {
            if (!ValidateManager(manager))
                return true; // Safe default

            return manager.IsAbilityOnCooldown(abilityIndex);
        }

        public IReadOnlyList<IAbilityManagerCore> GetAllAbilityManagers()
        {
            return _registeredManagers.AsReadOnly();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the system ID for a registered manager.
        /// </summary>
        public int GetManagerId(IAbilityManagerCore manager)
        {
            return _managerIds.TryGetValue(manager, out int id) ? id : -1;
        }

        /// <summary>
        /// Resets all cooldowns for all registered managers.
        /// Useful for testing or special game events.
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (var manager in _registeredManagers)
            {
                manager.ResetAllCooldowns();
            }

            if (enableSystemLogs)
                Debug.Log($"[UnifiedAbilitySystem] Reset cooldowns for {_registeredManagers.Count} managers");
        }

        /// <summary>
        /// Gets statistics about the ability system.
        /// </summary>
        public AbilitySystemStats GetSystemStats()
        {
            int totalAbilities = 0;
            int activeAbilities = 0;

            foreach (var manager in _registeredManagers)
            {
                totalAbilities += manager.AbilityCount;
                for (int i = 0; i < manager.AbilityCount; i++)
                {
                    if (manager.IsAbilityOnCooldown(i))
                        activeAbilities++;
                }
            }

            return new AbilitySystemStats
            {
                RegisteredManagers = _registeredManagers.Count,
                TotalAbilities = totalAbilities,
                ActiveAbilities = activeAbilities
            };
        }

        #endregion

        #region Private Methods

        private bool ValidateManager(IAbilityManagerCore manager)
        {
            if (manager == null)
            {
                Debug.LogError("[UnifiedAbilitySystem] AbilityManager is null");
                return false;
            }

            if (!_registeredManagers.Contains(manager))
            {
                Debug.LogError($"[UnifiedAbilitySystem] AbilityManager is not registered");
                return false;
            }

            return true;
        }

        private void SubscribeToManagerEvents(IAbilityManagerCore manager)
        {
            // Note: In a real implementation, you'd subscribe to the manager's events here
            // For now, we rely on the manager publishing to the global event bus
        }

        private void UnsubscribeFromManagerEvents(IAbilityManagerCore manager)
        {
            // Cleanup event subscriptions
        }

        #endregion

        #region Unity Events

        private void OnEnable()
        {
            // Subscribe to global ability events
            AbilityEventBus.OnAbilityActivated.AddListener(OnGlobalAbilityActivated);
            AbilityEventBus.OnAbilityStateChanged.AddListener(OnGlobalAbilityStateChanged);
            AbilityEventBus.OnAbilityCooldownComplete.AddListener(OnGlobalAbilityCooldownComplete);
        }

        private void OnDisable()
        {
            // Unsubscribe from global ability events
            AbilityEventBus.OnAbilityActivated.RemoveListener(OnGlobalAbilityActivated);
            AbilityEventBus.OnAbilityStateChanged.RemoveListener(OnGlobalAbilityStateChanged);
            AbilityEventBus.OnAbilityCooldownComplete.RemoveListener(OnGlobalAbilityCooldownComplete);
        }

        #endregion

        #region Event Handlers

        private void OnGlobalAbilityActivated(AbilityActivatedEvent evt)
        {
            // Find the manager that corresponds to this event source
            foreach (var manager in _registeredManagers)
            {
                if (manager.GameObject == evt.Source)
                {
                    OnAbilityActivated?.Invoke(manager, evt.AbilityIndex);
                    break;
                }
            }
        }

        private void OnGlobalAbilityStateChanged(AbilityStateChangedEvent evt)
        {
            foreach (var manager in _registeredManagers)
            {
                if (manager.GameObject == evt.Source)
                {
                    OnAbilityStateChanged?.Invoke(manager, evt.AbilityIndex, evt.IsOnCooldown, evt.CooldownRemaining);
                    break;
                }
            }
        }

        private void OnGlobalAbilityCooldownComplete(AbilityCooldownCompleteEvent evt)
        {
            foreach (var manager in _registeredManagers)
            {
                if (manager.GameObject == evt.Source)
                {
                    OnAbilityCooldownComplete?.Invoke(manager, evt.AbilityIndex);
                    break;
                }
            }
        }

        #endregion

        #region Data Structures

        [System.Serializable]
        public struct AbilitySystemStats
        {
            public int RegisteredManagers;
            public int TotalAbilities;
            public int ActiveAbilities;

            public override string ToString()
            {
                return $"Managers: {RegisteredManagers}, Total Abilities: {TotalAbilities}, Active: {ActiveAbilities}";
            }
        }

        #endregion
    }
}
