using System;
using UnityEngine;
using Laboratory.Core.Abilities.Interfaces;
using Laboratory.Core.Abilities.Events;

namespace Laboratory.Core.Abilities.Components
{
    /// <summary>
    /// Base implementation of the IAbility interface.
    /// Provides common functionality that most abilities will need.
    /// </summary>
    [System.Serializable]
    public abstract class BaseAbility : IAbility
    {
        #region Serialized Fields
        
        [Header("Basic Ability Settings")]
        [SerializeField] protected string abilityId;
        [SerializeField] protected string displayName;
        [SerializeField, TextArea(3, 5)] protected string description;
        [SerializeField] protected float cooldownTime = 1f;
        [SerializeField] protected float castTime = 0f;
        [SerializeField] protected int resourceCost = 0;
        [SerializeField] protected float range = 0f;
        
        [Header("Debug Settings")]
        [SerializeField] protected bool enableDebugLogs = false;
        
        #endregion
        
        #region Private Fields
        
        private float _cooldownTimer;
        private bool _isOnCooldown;
        
        #endregion
        
        #region IAbility Implementation
        
        public virtual string AbilityId => abilityId;
        public virtual string DisplayName => displayName;
        public virtual string Description => description;
        public virtual float CooldownTime => cooldownTime;
        public virtual float CastTime => castTime;
        public virtual int ResourceCost => resourceCost;
        public virtual float Range => range;
        
        public virtual bool IsUsable => !IsOnCooldown && CanActivate();
        public virtual bool IsOnCooldown => _isOnCooldown;
        public virtual float CooldownRemaining => _isOnCooldown ? _cooldownTimer : 0f;
        
        public virtual bool TryActivate()
        {
            if (!IsUsable)
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[{DisplayName}] Cannot activate: IsUsable = {IsUsable}");
                return false;
            }
            
            if (!CanPayCost())
            {
                if (enableDebugLogs)
                    Debug.LogWarning($"[{DisplayName}] Cannot activate: Insufficient resources");
                return false;
            }
            
            return ExecuteAbility();
        }
        
        public virtual void ForceActivate()
        {
            if (enableDebugLogs)
                Debug.Log($"[{DisplayName}] Force activating ability");
                
            ExecuteAbility();
        }
        
        public virtual void ResetCooldown()
        {
            _cooldownTimer = 0f;
            _isOnCooldown = false;
            
            if (enableDebugLogs)
                Debug.Log($"[{DisplayName}] Cooldown reset");
                
            OnCooldownComplete();
        }
        
        public virtual void UpdateAbility(float deltaTime)
        {
            if (_isOnCooldown)
            {
                _cooldownTimer -= deltaTime;
                if (_cooldownTimer <= 0f)
                {
                    _cooldownTimer = 0f;
                    _isOnCooldown = false;
                    OnCooldownComplete();
                }
            }
        }
        
        #endregion
        
        #region Protected Methods
        
        /// <summary>
        /// Override this method to implement the actual ability effect.
        /// This is called when the ability is activated.
        /// </summary>
        protected abstract void OnAbilityExecuted();
        
        /// <summary>
        /// Override this method to add additional conditions for activation.
        /// </summary>
        protected virtual bool CanActivate()
        {
            return true;
        }
        
        /// <summary>
        /// Override this method to implement resource cost checking.
        /// </summary>
        protected virtual bool CanPayCost()
        {
            return ResourceCost == 0; // No cost by default
        }
        
        /// <summary>
        /// Override this method to implement resource cost payment.
        /// </summary>
        protected virtual void PayCost()
        {
            // Override in derived classes to implement resource deduction
        }
        
        /// <summary>
        /// Called when the ability's cooldown completes.
        /// </summary>
        protected virtual void OnCooldownComplete()
        {
            if (enableDebugLogs)
                Debug.Log($"[{DisplayName}] Cooldown completed");
                
            // Publish cooldown complete event
            AbilityEventBus.PublishAbilityCooldownComplete(GetOwner(), GetAbilityIndex());
        }
        
        /// <summary>
        /// Gets the owner GameObject. Override in derived classes.
        /// </summary>
        protected virtual GameObject GetOwner()
        {
            return null; // Override in MonoBehaviour-based implementations
        }
        
        /// <summary>
        /// Gets the ability index. Override in derived classes.
        /// </summary>
        protected virtual int GetAbilityIndex()
        {
            return -1; // Override in derived classes
        }
        
        #endregion
        
        #region Private Methods
        
        private bool ExecuteAbility()
        {
            try
            {
                if (enableDebugLogs)
                    Debug.Log($"[{DisplayName}] Executing ability");
                
                // Pay the cost
                PayCost();
                
                // Start cooldown
                StartCooldown();
                
                // Execute the ability effect
                OnAbilityExecuted();
                
                // Publish activation event
                AbilityEventBus.PublishAbilityActivated(GetOwner(), GetAbilityIndex());
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{DisplayName}] Error executing ability: {ex.Message}");
                return false;
            }
        }
        
        private void StartCooldown()
        {
            if (cooldownTime > 0f)
            {
                _isOnCooldown = true;
                _cooldownTimer = cooldownTime;
                
                if (enableDebugLogs)
                    Debug.Log($"[{DisplayName}] Cooldown started: {cooldownTime}s");
                    
                // Publish state change event
                AbilityEventBus.PublishAbilityStateChanged(GetOwner(), GetAbilityIndex(), true, cooldownTime);
            }
        }
        
        #endregion
        
        #region Validation
        
#if UNITY_EDITOR
        /// <summary>
        /// Validates the ability configuration in the editor.
        /// </summary>
        public virtual void ValidateConfiguration()
        {
            if (string.IsNullOrEmpty(abilityId))
                Debug.LogWarning($"Ability ID is empty for {displayName}");
                
            if (string.IsNullOrEmpty(displayName))
                Debug.LogWarning($"Display name is empty for ability {abilityId}");
                
            if (cooldownTime < 0f)
                Debug.LogWarning($"Negative cooldown time for {displayName}: {cooldownTime}");
                
            if (castTime < 0f)
                Debug.LogWarning($"Negative cast time for {displayName}: {castTime}");
                
            if (resourceCost < 0)
                Debug.LogWarning($"Negative resource cost for {displayName}: {resourceCost}");
                
            if (range < 0f)
                Debug.LogWarning($"Negative range for {displayName}: {range}");
        }
#endif
        
        #endregion
    }
}
