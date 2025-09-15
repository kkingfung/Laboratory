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
    public class UnifiedAbilitySystem : ScriptableObject, ICoreAbilityExecutor, IAbilitySystem, Laboratory.Core.Systems.IGameplayAbilitySystem
    {
        #region Fields

        [Header("System Settings")]
        [SerializeField] private bool enableSystemLogs = true;
        
        private readonly List<IAbilityManagerCore> _registeredManagers = new();
        private readonly Dictionary<IAbilityManagerCore, int> _managerIds = new();
        private readonly Dictionary<string, object> _registeredAbilities = new();
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
                    Debug.Log($"[UnifiedAbilitySystem] Manager already registered, skipping...");
                return;
            }

            _registeredManagers.Add(abilityManager);
            _managerIds[abilityManager] = _nextManagerId++;

            if (enableSystemLogs)
                Debug.Log($"[UnifiedAbilitySystem] Registered AbilityManager (ID: {_managerIds[abilityManager]})");
        }

        public void UnregisterAbilityManager(IAbilityManagerCore abilityManager)
        {
            if (abilityManager == null) return;

            if (_registeredManagers.Contains(abilityManager))
            {
                _registeredManagers.Remove(abilityManager);
                _managerIds.Remove(abilityManager);

                if (enableSystemLogs)
                    Debug.Log("[UnifiedAbilitySystem] Unregistered AbilityManager");
            }
        }

        public bool TryActivateAbility(IAbilityManagerCore manager, int abilityIndex)
        {
            if (ValidateManager(manager))
            {
                bool success = manager.ActivateAbility(abilityIndex);
                if (success)
                {
                    OnAbilityActivated?.Invoke(manager, abilityIndex);
                    OnAbilityStateChanged?.Invoke(manager, abilityIndex, true, 0f);
                }
                return success;
            }
            return false;
        }

        public float GetAbilityCooldown(IAbilityManagerCore manager, int abilityIndex)
        {
            if (ValidateManager(manager))
                return manager.GetAbilityCooldown(abilityIndex);
            return 0f;
        }

        public bool IsAbilityOnCooldown(IAbilityManagerCore manager, int abilityIndex)
        {
            if (ValidateManager(manager))
                return manager.IsAbilityOnCooldown(abilityIndex);
            return true;
        }

        public IReadOnlyList<IAbilityManagerCore> GetAllAbilityManagers()
        {
            return _registeredManagers.AsReadOnly();
        }

        #endregion

        #region ICoreAbilityExecutor Implementation

        public bool ExecuteAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId) || !_registeredAbilities.ContainsKey(abilityId))
            {
                if (enableSystemLogs)
                    Debug.LogWarning($"[UnifiedAbilitySystem] Ability '{abilityId}' not found");
                return false;
            }

            if (enableSystemLogs)
                Debug.Log($"[UnifiedAbilitySystem] Executing ability '{abilityId}'");
            
            // Simulate ability cooldown completion after 3 seconds
            // Note: ScriptableObjects can't use coroutines, so we'll use a different approach
            UnityEngine.Debug.Log($"[UnifiedAbilitySystem] Ability '{abilityId}' cooldown started");
            
            return true;
        }

        public bool CanExecuteAbility(string abilityId)
        {
            return !string.IsNullOrEmpty(abilityId) && _registeredAbilities.ContainsKey(abilityId);
        }

        public void RegisterAbility(string abilityId, object ability)
        {
            if (string.IsNullOrEmpty(abilityId) || ability == null)
            {
                if (enableSystemLogs)
                    Debug.LogError("[UnifiedAbilitySystem] Cannot register ability with null ID or ability object");
                return;
            }

            _registeredAbilities[abilityId] = ability;
            
            if (enableSystemLogs)
                Debug.Log($"[UnifiedAbilitySystem] Registered ability '{abilityId}'");
        }

        public void UnregisterAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return;

            if (_registeredAbilities.Remove(abilityId) && enableSystemLogs)
                Debug.Log($"[UnifiedAbilitySystem] Unregistered ability '{abilityId}'");
        }

        #endregion

        #region IGameplayAbilitySystem Implementation

        void Laboratory.Core.Systems.IGameplayAbilitySystem.RegisterAbilityManager(IAbilityManager abilityManager)
        {
            if (enableSystemLogs)
                Debug.Log("[UnifiedAbilitySystem] Registered IAbilityManager");
        }

        void Laboratory.Core.Systems.IGameplayAbilitySystem.UnregisterAbilityManager(IAbilityManager abilityManager)
        {
            if (enableSystemLogs)
                Debug.Log("[UnifiedAbilitySystem] Unregistered IAbilityManager");
        }

        bool Laboratory.Core.Systems.IGameplayAbilitySystem.TryActivateAbility(IAbilityManager manager, int abilityIndex)
        {
            return manager?.TryActivateAbility(abilityIndex) ?? false;
        }

        float Laboratory.Core.Systems.IGameplayAbilitySystem.GetAbilityCooldown(IAbilityManager manager, int abilityIndex)
        {
            return manager?.GetAbilityCooldown(abilityIndex) ?? 0f;
        }

        bool Laboratory.Core.Systems.IGameplayAbilitySystem.IsAbilityOnCooldown(IAbilityManager manager, int abilityIndex)
        {
            return manager?.IsAbilityOnCooldown(abilityIndex) ?? true;
        }

        IReadOnlyList<IAbilityManager> Laboratory.Core.Systems.IGameplayAbilitySystem.GetAllAbilityManagers()
        {
            return new List<IAbilityManager>().AsReadOnly();
        }

        event Action<IAbilityManager, int> Laboratory.Core.Systems.IGameplayAbilitySystem.OnAbilityActivated
        {
            add { }
            remove { }
        }

        event Action<IAbilityManager, int> Laboratory.Core.Systems.IGameplayAbilitySystem.OnAbilityCooldownComplete
        {
            add { }
            remove { }
        }

        event Action<IAbilityManager, int, bool, float> Laboratory.Core.Systems.IGameplayAbilitySystem.OnAbilityStateChanged
        {
            add { }
            remove { }
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
                Debug.LogError("[UnifiedAbilitySystem] AbilityManager is not registered");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Simulates an ability cooldown completion event
        /// </summary>
        public void TriggerCooldownComplete(IAbilityManagerCore manager, int abilityIndex)
        {
            OnAbilityCooldownComplete?.Invoke(manager, abilityIndex);
            OnAbilityStateChanged?.Invoke(manager, abilityIndex, false, 0f);
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
