using System;
using System.Collections.Generic;
using UnityEngine;
using Laboratory.Core.DI;
using Laboratory.Core.Events;
using Laboratory.Core.Health;
using Laboratory.Core.Health.Components;
using Laboratory.Models.ECS.Components;
using Laboratory.Subsystems.Combat.Abilities;
using Laboratory.Core.Systems;
using Laboratory.Core.Abilities.Systems;
using HealthChangedEventArgs = Laboratory.Core.Health.HealthChangedEventArgs;
using DeathEventArgs = Laboratory.Core.Health.DeathEventArgs;
using DamageType = Laboratory.Core.Health.DamageType;
using CoreDamageRequest = Laboratory.Core.Health.DamageRequest;
using EcsDamageRequest = Laboratory.Models.ECS.Components.DamageRequest;

namespace Laboratory.Subsystems.Combat
{
    /// <summary>
    /// Unified Combat subsystem manager that coordinates all combat-related functionality.
    /// Consolidates health management, damage processing, ability systems, and combat events.
    /// Provides a single interface for all combat operations across the game.
    /// 
    /// Version 3.0 - Enhanced architecture with comprehensive combat management
    /// </summary>
    public class CombatSubsystemManager : MonoBehaviour, IDisposable
    {
        #region Serialized Fields

        [Header("Combat Configuration")]
        [SerializeField] private CombatSubsystemConfig _config;
        
        [Header("Core Components")]
        [SerializeField] private HealthComponentBase _healthComponent;
        [SerializeField] private AbilityManager _abilityManager;
        
        [Header("Combat Settings")]
        [SerializeField] private bool _enableFriendlyFire = false;
        [SerializeField] private float _combatRange = 10f;
        [SerializeField] private LayerMask _combatTargetLayers = -1;
        
        [Header("Debug")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _showCombatGizmos = false;

        #endregion

        #region Private Fields

        private IEventBus _eventBus;
        private IHealthSystem _healthSystem;
        private IAbilitySystem _abilitySystem;
        
        private CombatSubsystemState _currentState = CombatSubsystemState.Idle;
        private bool _isInitialized = false;
        
        // Combat tracking
        private readonly Dictionary<GameObject, CombatEntity> _trackedEntities = new();
        private readonly List<CombatAction> _recentActions = new();
        private CombatStatistics _statistics = new();
        
        // Damage over time effects
        private readonly List<DamageOverTimeEffect> _dotEffects = new();
        
        // Combat targets
        private GameObject _currentTarget;
        private float _lastCombatTime = -1f;

        #endregion

        #region Properties

        /// <summary>Current state of the combat subsystem</summary>
        public CombatSubsystemState CurrentState => _currentState;
        
        /// <summary>Whether the combat subsystem is fully initialized</summary>
        public bool IsInitialized => _isInitialized;
        
        /// <summary>Current health percentage (0-1)</summary>
        public float HealthPercentage => _healthComponent?.HealthPercentage ?? 0f;
        
        /// <summary>Whether this entity is currently alive</summary>
        public bool IsAlive => _healthComponent?.IsAlive ?? false;
        
        /// <summary>Whether this entity is currently in combat</summary>
        public bool IsInCombat => _currentState == CombatSubsystemState.InCombat;
        
        /// <summary>Current combat target</summary>
        public GameObject CurrentTarget => _currentTarget;
        
        /// <summary>Time since last combat action</summary>
        public float TimeSinceLastCombat => _lastCombatTime > 0 ? Time.time - _lastCombatTime : float.MaxValue;
        
        /// <summary>Combat configuration settings</summary>
        public CombatSubsystemConfig Configuration => _config;

        #endregion

        #region Events

        /// <summary>Fired when combat state changes</summary>
        public event Action<CombatStateChangedEventArgs> OnCombatStateChanged;
        
        /// <summary>Fired when entering combat</summary>
        public event Action<CombatEnteredEventArgs> OnCombatEntered;
        
        /// <summary>Fired when exiting combat</summary>
        public event Action<CombatExitedEventArgs> OnCombatExited;
        
        /// <summary>Fired when dealing damage to another entity</summary>
        public event Action<DamageDealtEventArgs> OnDamageDealt;
        
        /// <summary>Fired when taking damage from another entity</summary>
        public event Action<DamageTakenEventArgs> OnDamageTaken;
        
        /// <summary>Fired when a combat ability is used</summary>
        public event Action<CombatAbilityUsedEventArgs> OnAbilityUsed;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateConfiguration();
            InitializeComponents();
        }

        private void Start()
        {
            InitializeAsync();
        }

        private void Update()
        {
            if (_isInitialized)
            {
                UpdateCombatSubsystem();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region Initialization

        private void InitializeAsync()
        {
            try
            {
                LogDebug("Initializing Combat Subsystem...");
                
                // Inject dependencies
                InjectDependencies();
                
                // Initialize core systems
                InitializeHealthSystem();
                InitializeAbilitySystem();
                InitializeCombatTracking();
                
                // Set initial state
                ChangeState(CombatSubsystemState.Idle);
                
                _isInitialized = true;
                LogDebug("Combat Subsystem initialized successfully");
                
                // Notify other systems
                _eventBus?.Publish(new CombatSubsystemInitializedEvent(gameObject));
            }
            catch (Exception ex)
            {
                LogError($"Failed to initialize Combat Subsystem: {ex.Message}");
            }
        }

        private void InjectDependencies()
        {
            // Get services from DI container
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance?.TryResolve<IEventBus>(out _eventBus);
                GlobalServiceProvider.Instance?.TryResolve<IHealthSystem>(out _healthSystem);
                GlobalServiceProvider.Instance?.TryResolve<IAbilitySystem>(out _abilitySystem);
            }

            LogDebug($"Dependencies injected - EventBus: {_eventBus != null}, HealthSystem: {_healthSystem != null}, AbilitySystem: {_abilitySystem != null}");
        }

        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (_healthComponent == null)
                _healthComponent = GetComponent<HealthComponentBase>();
            
            if (_abilityManager == null)
                _abilityManager = GetComponent<AbilityManager>();
        }

        private void InitializeHealthSystem()
        {
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged += OnHealthChanged;
                _healthComponent.OnDeath += OnEntityDeath;
                LogDebug("Health system initialized");
            }

            if (_healthSystem != null)
            {
                _healthSystem.RegisterHealthComponent(_healthComponent);
            }
        }

        private void InitializeAbilitySystem()
        {
            if (_abilityManager != null)
            {
                _abilityManager.OnAbilityActivated += OnAbilityActivated;
                LogDebug("Ability system initialized");
            }

            if (_abilitySystem != null)
            {
                _abilitySystem.RegisterAbilityManager(_abilityManager);
            }
        }

        private void InitializeCombatTracking()
        {
            // Initialize combat statistics
            _statistics = new CombatStatistics
            {
                CombatStartTime = Time.time,
                TotalDamageDealt = 0,
                TotalDamageTaken = 0,
                AbilitiesUsed = 0,
                CombatDuration = 0f
            };

            LogDebug("Combat tracking initialized");
        }

        #endregion

        #region Update Methods

        private void UpdateCombatSubsystem()
        {
            UpdateCombatState();
            UpdateDamageOverTimeEffects();
            UpdateCombatTarget();
            UpdateCombatStatistics();
            CleanupOldCombatActions();
        }

        private void UpdateCombatState()
        {
            var newState = DetermineCombatState();
            if (newState != _currentState)
            {
                ChangeState(newState);
            }
        }

        private CombatSubsystemState DetermineCombatState()
        {
            if (!IsAlive)
                return CombatSubsystemState.Dead;

            // Check if we've taken damage or dealt damage recently
            float combatTimeout = _config?.CombatTimeout ?? 5f;
            bool recentCombat = TimeSinceLastCombat < combatTimeout;

            // Check if we have a valid combat target
            bool hasTarget = _currentTarget != null;

            // Check if we're currently using abilities
            bool usingAbilities = _abilityManager != null && HasActiveAbilities();

            if (recentCombat || hasTarget || usingAbilities)
            {
                return CombatSubsystemState.InCombat;
            }

            return CombatSubsystemState.Idle;
        }

        private bool HasActiveAbilities()
        {
            if (_abilityManager == null) return false;

            for (int i = 0; i < _abilityManager.AbilityCount; i++)
            {
                if (_abilityManager.IsAbilityOnCooldown(i))
                    return true;
            }
            return false;
        }

        private void UpdateDamageOverTimeEffects()
        {
            for (int i = _dotEffects.Count - 1; i >= 0; i--)
            {
                var dot = _dotEffects[i];
                if (dot.IsExpired)
                {
                    _dotEffects.RemoveAt(i);
                    continue;
                }

                if (dot.ShouldTick())
                {
                    ApplyDamageOverTime(dot);
                }
            }
        }

        private void UpdateCombatTarget()
        {
            if (_currentTarget == null) return;

            // Check if target is still valid
            if (_currentTarget.activeInHierarchy == false)
            {
                ClearCombatTarget();
                return;
            }

            // Check if target is still in range
            float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);
            if (distance > _combatRange)
            {
                if (_config?.AutoClearOutOfRangeTargets ?? true)
                {
                    ClearCombatTarget();
                }
            }
        }

        private void UpdateCombatStatistics()
        {
            if (_currentState == CombatSubsystemState.InCombat)
            {
                _statistics.CombatDuration = Time.time - _statistics.CombatStartTime;
            }
        }

        private void CleanupOldCombatActions()
        {
            float cleanupThreshold = 30f; // Keep actions for 30 seconds
            _recentActions.RemoveAll(action => Time.time - action.Timestamp > cleanupThreshold);
        }

        #endregion

        #region State Management

        private void ChangeState(CombatSubsystemState newState)
        {
            var oldState = _currentState;
            _currentState = newState;
            
            // Handle state transitions
            OnStateEnter(newState, oldState);
            
            // Fire events
            var eventArgs = new CombatStateChangedEventArgs(oldState, newState, gameObject);
            OnCombatStateChanged?.Invoke(eventArgs);
            
            _eventBus?.Publish(new CombatStateChangedEvent(oldState, newState, gameObject));
            
            LogDebug($"Combat state changed: {oldState} → {newState}");
        }

        private void OnStateEnter(CombatSubsystemState newState, CombatSubsystemState oldState)
        {
            switch (newState)
            {
                case CombatSubsystemState.InCombat:
                    if (oldState != CombatSubsystemState.InCombat)
                    {
                        EnterCombat();
                    }
                    break;
                
                case CombatSubsystemState.Idle:
                    if (oldState == CombatSubsystemState.InCombat)
                    {
                        ExitCombat();
                    }
                    break;
                
                case CombatSubsystemState.Dead:
                    OnCombatDeath();
                    break;
            }
        }

        private void EnterCombat()
        {
            _statistics.CombatStartTime = Time.time;
            _lastCombatTime = Time.time;
            
            var eventArgs = new CombatEnteredEventArgs(gameObject, _currentTarget);
            OnCombatEntered?.Invoke(eventArgs);
            
            _eventBus?.Publish(new CombatEnteredEvent(gameObject, _currentTarget));
            
            LogDebug("Entered combat");
        }

        private void ExitCombat()
        {
            var combatDuration = Time.time - _statistics.CombatStartTime;
            
            var eventArgs = new CombatExitedEventArgs(gameObject, combatDuration, _statistics);
            OnCombatExited?.Invoke(eventArgs);
            
            _eventBus?.Publish(new CombatExitedEvent(gameObject, combatDuration));
            
            ClearCombatTarget();
            LogDebug($"Exited combat (duration: {combatDuration:F1}s)");
        }

        private void OnCombatDeath()
        {
            // Clear all ongoing effects
            _dotEffects.Clear();
            ClearCombatTarget();
            
            LogDebug("Combat entity died");
        }

        #endregion

        #region Public API - Damage System

        /// <summary>
        /// Deals damage to a target entity.
        /// </summary>
        public bool DealDamage(GameObject target, float amount, DamageType damageType = DamageType.Physical, Vector3 direction = default)
        {
            if (target == null || amount <= 0) return false;

            var targetHealth = target.GetComponent<HealthComponentBase>();
            if (targetHealth == null) return false;

            // Check friendly fire
            if (!_enableFriendlyFire && IsFriendly(target))
            {
                LogDebug($"Friendly fire prevented on {target.name}");
                return false;
            }

            var damageRequest = new CoreDamageRequest
            {
                Amount = amount,
                Source = gameObject,
                Type = damageType,
                Direction = direction.normalized,
                TriggerInvulnerability = true
            };

            bool success = targetHealth.TakeDamage(damageRequest);
            
            if (success)
            {
                RecordDamageDealt(target, amount, damageType);
                SetCombatTarget(target);
                _lastCombatTime = Time.time;
            }

            return success;
        }

        /// <summary>
        /// Applies a damage over time effect.
        /// </summary>
        public void ApplyDamageOverTime(GameObject target, float damagePerSecond, float duration, DamageType damageType = DamageType.Poison)
        {
            var dot = new DamageOverTimeEffect
            {
                Target = target,
                DamagePerSecond = damagePerSecond,
                Duration = duration,
                DamageType = damageType,
                StartTime = Time.time,
                LastTickTime = Time.time,
                TickInterval = 1f // 1 second between ticks
            };

            _dotEffects.Add(dot);
            LogDebug($"Applied DOT effect to {target.name}: {damagePerSecond}/sec for {duration}s");
        }

        /// <summary>
        /// Heals this entity.
        /// </summary>
        public bool Heal(float amount, object source = null)
        {
            if (_healthComponent == null) return false;

            return _healthComponent.Heal(Mathf.RoundToInt(amount), source);
        }

        #endregion

        #region Public API - Ability System

        /// <summary>
        /// Activates a combat ability by index.
        /// </summary>
        public bool UseAbility(int abilityIndex)
        {
            if (_abilityManager == null) return false;

            bool success = _abilityManager.ActivateAbility(abilityIndex);
            
            if (success)
            {
                _statistics.AbilitiesUsed++;
                _lastCombatTime = Time.time;
                
                var abilityData = _abilityManager.GetAbilityData(abilityIndex);
                RecordAbilityUsed(abilityIndex, abilityData);
            }

            return success;
        }

        /// <summary>
        /// Gets the cooldown remaining for an ability.
        /// </summary>
        public float GetAbilityCooldown(int abilityIndex)
        {
            return _abilityManager?.GetAbilityCooldown(abilityIndex) ?? 0f;
        }

        /// <summary>
        /// Gets whether an ability is ready to use.
        /// </summary>
        public bool IsAbilityReady(int abilityIndex)
        {
            return _abilityManager?.IsAbilityOnCooldown(abilityIndex) == false;
        }

        #endregion

        #region Public API - Target Management

        /// <summary>
        /// Sets the current combat target.
        /// </summary>
        public void SetCombatTarget(GameObject target)
        {
            if (_currentTarget != target)
            {
                var oldTarget = _currentTarget;
                _currentTarget = target;
                
                LogDebug($"Combat target changed: {oldTarget?.name} → {target?.name}");
            }
        }

        /// <summary>
        /// Clears the current combat target.
        /// </summary>
        public void ClearCombatTarget()
        {
            if (_currentTarget != null)
            {
                LogDebug($"Cleared combat target: {_currentTarget.name}");
                _currentTarget = null;
            }
        }

        /// <summary>
        /// Finds nearby combat targets.
        /// </summary>
        public List<GameObject> FindNearbyTargets(float range = -1f)
        {
            if (range < 0) range = _combatRange;
            
            var targets = new List<GameObject>();
            var colliders = Physics.OverlapSphere(transform.position, range, _combatTargetLayers);
            
            foreach (var collider in colliders)
            {
                if (collider.gameObject != gameObject && IsValidCombatTarget(collider.gameObject))
                {
                    targets.Add(collider.gameObject);
                }
            }
            
            return targets;
        }

        #endregion

        #region Public API - Status

        /// <summary>
        /// Gets comprehensive combat subsystem status.
        /// </summary>
        public CombatSubsystemStatus GetStatus()
        {
            return new CombatSubsystemStatus
            {
                IsInitialized = _isInitialized,
                CurrentState = _currentState,
                IsAlive = IsAlive,
                IsInCombat = IsInCombat,
                HealthPercentage = HealthPercentage,
                CurrentTarget = _currentTarget,
                TimeSinceLastCombat = TimeSinceLastCombat,
                Statistics = _statistics,
                ActiveDOTEffects = _dotEffects.Count,
                RecentActionsCount = _recentActions.Count
            };
        }

        #endregion

        #region Event Handlers

        private void OnHealthChanged(HealthChangedEventArgs args)
        {
            LogDebug($"Health changed: {args.OldHealth} → {args.NewHealth}");
            
            // If we took damage, we're potentially in combat
            if (args.NewHealth < args.OldHealth)
            {
                _lastCombatTime = Time.time;
                _statistics.TotalDamageTaken += (args.OldHealth - args.NewHealth);
                
                var eventArgs = new DamageTakenEventArgs(args.OldHealth - args.NewHealth, args.Source as GameObject, gameObject);
                OnDamageTaken?.Invoke(eventArgs);
            }
        }

        private void OnEntityDeath(DeathEventArgs args)
        {
            LogDebug("Entity died in combat");
            ChangeState(CombatSubsystemState.Dead);
        }

        private void OnAbilityActivated(int abilityIndex)
        {
            LogDebug($"Ability {abilityIndex} activated");
            _lastCombatTime = Time.time;
        }

        #endregion

        #region Helper Methods

        private bool IsValidCombatTarget(GameObject target)
        {
            if (target == gameObject) return false;
            if (!target.activeInHierarchy) return false;
            
            var healthComponent = target.GetComponent<HealthComponentBase>();
            if (healthComponent == null || !healthComponent.IsAlive) return false;
            
            if (!_enableFriendlyFire && IsFriendly(target)) return false;
            
            return true;
        }

        private bool IsFriendly(GameObject target)
        {
            // This would need to be implemented based on your faction/team system
            // For now, assume all entities are hostile
            return false;
        }

        private void ApplyDamageOverTime(DamageOverTimeEffect dot)
        {
            if (dot.Target == null) return;

            var targetHealth = dot.Target.GetComponent<HealthComponentBase>();
            if (targetHealth == null || !targetHealth.IsAlive) return;

            var damageRequest = new CoreDamageRequest
            {
                Amount = dot.DamagePerSecond * dot.TickInterval,
                Source = gameObject,
                Type = dot.DamageType,
                TriggerInvulnerability = false // DOT doesn't trigger invulnerability
            };

            targetHealth.TakeDamage(damageRequest);
            dot.LastTickTime = Time.time;
            
            LogDebug($"Applied DOT damage: {damageRequest.Amount} to {dot.Target.name}");
        }

        private void RecordDamageDealt(GameObject target, float amount, DamageType damageType)
        {
            _statistics.TotalDamageDealt += Mathf.RoundToInt(amount);
            
            var action = new CombatAction
            {
                Type = CombatActionType.DamageDealt,
                Target = target,
                Amount = amount,
                DamageType = damageType,
                Timestamp = Time.time
            };
            _recentActions.Add(action);
            
            var eventArgs = new DamageDealtEventArgs(amount, target, gameObject, damageType);
            OnDamageDealt?.Invoke(eventArgs);
            
            LogDebug($"Dealt {amount} {damageType} damage to {target.name}");
        }

        private void RecordAbilityUsed(int abilityIndex, object abilityData)
        {
            var action = new CombatAction
            {
                Type = CombatActionType.AbilityUsed,
                AbilityIndex = abilityIndex,
                Timestamp = Time.time
            };
            _recentActions.Add(action);
            
            var eventArgs = new CombatAbilityUsedEventArgs(abilityIndex, abilityData, gameObject);
            OnAbilityUsed?.Invoke(eventArgs);
        }

        private void ValidateConfiguration()
        {
            if (_config == null)
            {
                LogWarning("No configuration assigned, using default values");
                _config = ScriptableObject.CreateInstance<CombatSubsystemConfig>();
            }
        }

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[CombatSubsystem] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[CombatSubsystem] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[CombatSubsystem] {message}");
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (!_showCombatGizmos) return;

            // Draw combat range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _combatRange);
            
            // Draw line to current target
            if (_currentTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _currentTarget.transform.position);
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            // Unsubscribe from health events
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged -= OnHealthChanged;
                _healthComponent.OnDeath -= OnEntityDeath;
            }
            
            // Unsubscribe from ability events
            if (_abilityManager != null)
            {
                _abilityManager.OnAbilityActivated -= OnAbilityActivated;
            }
            
            // Clear tracked entities
            _trackedEntities.Clear();
            _recentActions.Clear();
            _dotEffects.Clear();
            
            // Clear event handlers
            OnCombatStateChanged = null;
            OnCombatEntered = null;
            OnCombatExited = null;
            OnDamageDealt = null;
            OnDamageTaken = null;
            OnAbilityUsed = null;
            
            _isInitialized = false;
            LogDebug("Combat Subsystem disposed");
        }

        #endregion
    }

    #region Data Structures and Enums

    /// <summary>Combat subsystem states</summary>
    public enum CombatSubsystemState
    {
        Uninitialized,
        Idle,
        InCombat,
        Dead
    }

    /// <summary>Combat action types for tracking</summary>
    public enum CombatActionType
    {
        DamageDealt,
        DamageTaken,
        AbilityUsed,
        TargetChanged
    }

    /// <summary>Combat entity tracking data</summary>
    public class CombatEntity
    {
        public GameObject GameObject;
        public HealthComponentBase HealthComponent;
        public float LastInteractionTime;
        public bool IsFriendly;
        public float ThreatLevel;
    }

    /// <summary>Combat action record</summary>
    public class CombatAction
    {
        public CombatActionType Type;
        public GameObject Target;
        public float Amount;
        public DamageType DamageType;
        public int AbilityIndex;
        public float Timestamp;
    }

    /// <summary>Damage over time effect</summary>
    public class DamageOverTimeEffect
    {
        public GameObject Target;
        public float DamagePerSecond;
        public float Duration;
        public DamageType DamageType;
        public float StartTime;
        public float LastTickTime;
        public float TickInterval;
        
        public bool IsExpired => Time.time - StartTime >= Duration;
        public bool ShouldTick() => Time.time - LastTickTime >= TickInterval;
    }

    /// <summary>Combat statistics</summary>
    [System.Serializable]
    public struct CombatStatistics
    {
        public float CombatStartTime;
        public float CombatDuration;
        public int TotalDamageDealt;
        public int TotalDamageTaken;
        public int AbilitiesUsed;
    }

    /// <summary>Combat subsystem status</summary>
    [System.Serializable]
    public struct CombatSubsystemStatus
    {
        public bool IsInitialized;
        public CombatSubsystemState CurrentState;
        public bool IsAlive;
        public bool IsInCombat;
        public float HealthPercentage;
        public GameObject CurrentTarget;
        public float TimeSinceLastCombat;
        public CombatStatistics Statistics;
        public int ActiveDOTEffects;
        public int RecentActionsCount;
    }

    #endregion

    #region Event Classes

    /// <summary>Combat state changed event arguments</summary>
    public class CombatStateChangedEventArgs : EventArgs
    {
        public CombatSubsystemState OldState { get; }
        public CombatSubsystemState NewState { get; }
        public GameObject Entity { get; }
        public float Timestamp { get; }

        public CombatStateChangedEventArgs(CombatSubsystemState oldState, CombatSubsystemState newState, GameObject entity)
        {
            OldState = oldState;
            NewState = newState;
            Entity = entity;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Combat entered event arguments</summary>
    public class CombatEnteredEventArgs : EventArgs
    {
        public GameObject Entity { get; }
        public GameObject Target { get; }
        public float Timestamp { get; }

        public CombatEnteredEventArgs(GameObject entity, GameObject target)
        {
            Entity = entity;
            Target = target;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Combat exited event arguments</summary>
    public class CombatExitedEventArgs : EventArgs
    {
        public GameObject Entity { get; }
        public float Duration { get; }
        public CombatStatistics Statistics { get; }
        public float Timestamp { get; }

        public CombatExitedEventArgs(GameObject entity, float duration, CombatStatistics statistics)
        {
            Entity = entity;
            Duration = duration;
            Statistics = statistics;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Damage dealt event arguments</summary>
    public class DamageDealtEventArgs : EventArgs
    {
        public float Amount { get; }
        public GameObject Target { get; }
        public GameObject Source { get; }
        public DamageType DamageType { get; }
        public float Timestamp { get; }

        public DamageDealtEventArgs(float amount, GameObject target, GameObject source, DamageType damageType)
        {
            Amount = amount;
            Target = target;
            Source = source;
            DamageType = damageType;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Damage taken event arguments</summary>
    public class DamageTakenEventArgs : EventArgs
    {
        public float Amount { get; }
        public GameObject Source { get; }
        public GameObject Target { get; }
        public float Timestamp { get; }

        public DamageTakenEventArgs(float amount, GameObject source, GameObject target)
        {
            Amount = amount;
            Source = source;
            Target = target;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Combat ability used event arguments</summary>
    public class CombatAbilityUsedEventArgs : EventArgs
    {
        public int AbilityIndex { get; }
        public object AbilityData { get; }
        public GameObject Entity { get; }
        public float Timestamp { get; }

        public CombatAbilityUsedEventArgs(int abilityIndex, object abilityData, GameObject entity)
        {
            AbilityIndex = abilityIndex;
            AbilityData = abilityData;
            Entity = entity;
            Timestamp = Time.unscaledTime;
        }
    }

    #region Global Events

    /// <summary>Global event: Combat subsystem initialized</summary>
    public class CombatSubsystemInitializedEvent
    {
        public GameObject Entity { get; }
        public float Timestamp { get; }

        public CombatSubsystemInitializedEvent(GameObject entity)
        {
            Entity = entity;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Global event: Combat state changed</summary>
    public class CombatStateChangedEvent
    {
        public CombatSubsystemState OldState { get; }
        public CombatSubsystemState NewState { get; }
        public GameObject Entity { get; }
        public float Timestamp { get; }

        public CombatStateChangedEvent(CombatSubsystemState oldState, CombatSubsystemState newState, GameObject entity)
        {
            OldState = oldState;
            NewState = newState;
            Entity = entity;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Global event: Entity entered combat</summary>
    public class CombatEnteredEvent
    {
        public GameObject Entity { get; }
        public GameObject Target { get; }
        public float Timestamp { get; }

        public CombatEnteredEvent(GameObject entity, GameObject target)
        {
            Entity = entity;
            Target = target;
            Timestamp = Time.unscaledTime;
        }
    }

    /// <summary>Global event: Entity exited combat</summary>
    public class CombatExitedEvent
    {
        public GameObject Entity { get; }
        public float Duration { get; }
        public float Timestamp { get; }

        public CombatExitedEvent(GameObject entity, float duration)
        {
            Entity = entity;
            Duration = duration;
            Timestamp = Time.unscaledTime;
        }
    }

    #endregion

    #endregion
}