using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Laboratory.Core.Events;
using Laboratory.Infrastructure.Core;
using Laboratory.Core.Health;
using Laboratory.Core.Infrastructure;
using ECS_DamageType = Laboratory.Models.ECS.Components.DamageType;
using ECS_DamageRequest = Laboratory.Models.ECS.Components.DamageRequest;
using Health_DamageType = Laboratory.Core.Enums.DamageType;
using Health_DamageRequest = Laboratory.Core.Health.DamageRequest;

#nullable enable

namespace Laboratory.Infrastructure.Health
{
    /// <summary>
    /// Centralized damage manager that processes all damage requests in the game.
    /// Provides consistent damage handling, modifiers, and event broadcasting.
    /// Replaces scattered damage logic with a unified, extensible system.
    /// </summary>
    public class DamageManager : MonoBehaviour
    {
        #region Singleton

        private static DamageManager? _instance;
        public static DamageManager? Instance => _instance;

        #endregion

        #region Fields

        [Header("Damage Settings")]
        [SerializeField] private bool _enableDebugLogging = false;
        [SerializeField] private bool _enableDamageModifiers = true;

        private readonly List<IDamageProcessor> _damageProcessors = new();
        private IEventBus? _eventBus;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            
            // Clean up resources
            _damageProcessors.Clear();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Get event bus from service container
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _eventBus = serviceContainer.ResolveService<IEventBus>();
            }

            // Register default damage processors
            RegisterProcessor(new StandardDamageProcessor());
            RegisterProcessor(new CriticalDamageProcessor());
            RegisterProcessor(new ElementalDamageProcessor());

            if (_enableDebugLogging)
            {
                Debug.Log($"DamageManager initialized with {_damageProcessors.Count} processors");
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Applies damage to a target using the unified damage system.
        /// </summary>
        public bool ApplyDamage(GameObject target, DamageRequest damageRequest)
        {
            if (target == null || damageRequest == null)
            {
                Debug.LogError("DamageManager.ApplyDamage: target or damageRequest is null");
                return false;
            }

            var healthComponent = target.GetComponent<IHealthComponent>();
            if (healthComponent == null)
            {
                if (_enableDebugLogging)
                {
                    Debug.LogWarning($"Target {target.name} has no IHealthComponent");
                }
                return false;
            }

            // Process damage through processors
            var processedRequest = ProcessDamageRequest(damageRequest, target);

            // Apply damage to health component
            bool damageApplied = healthComponent.TakeDamage(processedRequest);

            if (damageApplied)
            {
                // Broadcast damage event
                BroadcastDamageEvent(target, processedRequest);
            }

            return damageApplied;
        }

        /// <summary>
        /// Applies damage to multiple targets.
        /// </summary>
        public int ApplyAreaDamage(Vector3 center, float radius, DamageRequest baseDamageRequest, LayerMask targetLayers = default)
        {
            var colliders = Physics.OverlapSphere(center, radius, targetLayers);
            int targetsHit = 0;

            foreach (var collider in colliders)
            {
                // Create a copy of the damage request for each target
                var damageRequest = CreateDamageRequestCopy(baseDamageRequest);
                
                // Calculate distance-based damage falloff
                float distance = Vector3.Distance(center, collider.transform.position);
                float falloffMultiplier = Mathf.Clamp01(1f - (distance / radius));
                damageRequest.Amount *= falloffMultiplier;

                // Set direction from center to target
                damageRequest.Direction = (collider.transform.position - center).normalized;
                damageRequest.SourcePosition = center;

                if (ApplyDamage(collider.gameObject, damageRequest))
                {
                    targetsHit++;
                }
            }

            return targetsHit;
        }

        /// <summary>
        /// Registers a custom damage processor.
        /// </summary>
        public void RegisterProcessor(IDamageProcessor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            if (!_damageProcessors.Contains(processor))
            {
                _damageProcessors.Add(processor);
                _damageProcessors.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            }
        }

        /// <summary>
        /// Unregisters a damage processor.
        /// </summary>
        public void UnregisterProcessor(IDamageProcessor processor)
        {
            _damageProcessors.Remove(processor);
        }

        /// <summary>
        /// Clears all damage processors.
        /// </summary>
        public void ClearProcessors()
        {
            _damageProcessors.Clear();
        }
        
        #region Network Damage Processing
        
        /// <summary>
        /// Requests damage to be applied to a target player over the network. Server validates and applies damage.
        /// </summary>
        /// <param name="targetPlayerClientId">Client ID of the target player to damage.</param>
        /// <param name="damage">Amount of damage to apply.</param>
        /// <param name="damageType">Type of damage being applied.</param>
        /// <param name="hitDirection">Direction vector of the hit for physics effects.</param>
        /// <param name="rpcParams">Server RPC parameters containing sender information.</param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestPlayerDamageServerRpc(ulong targetPlayerClientId, float damage, DamageType damageType, Vector3 hitDirection, ServerRpcParams rpcParams = default)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            ulong attackerClientId = rpcParams.Receive.SenderClientId;

            var targetNetObj = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(targetPlayerClientId);
            if (targetNetObj == null)
            {
                Debug.LogWarning($"RequestPlayerDamageServerRpc: target player {targetPlayerClientId} not found.");
                return;
            }

            var healthComponent = targetNetObj.GetComponent<IHealthComponent>();
            if (healthComponent == null)
            {
                Debug.LogWarning("RequestPlayerDamageServerRpc: target missing health component.");
                return;
            }

            // Create damage request with network context
            var damageRequest = new DamageRequest(damage, null, damageType)
            {
                Direction = hitDirection,
                Source = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(attackerClientId)?.gameObject
            };
            
            damageRequest.Metadata["AttackerClientId"] = attackerClientId;
            damageRequest.Metadata["VictimClientId"] = targetPlayerClientId;
            damageRequest.Metadata["NetworkDamage"] = true;

            // Apply damage through unified system
            bool damageApplied = ApplyDamage(targetNetObj.gameObject, damageRequest);
            
            if (damageApplied)
            {
                // Notify all clients for visual/audio feedback
                BroadcastPlayerDamageClientRpc(targetPlayerClientId, attackerClientId, damage, damageType, hitDirection);
            }
        }
        
        /// <summary>
        /// Notifies all clients about player damage events for local effects and UI updates.
        /// </summary>
        [ClientRpc]
        private void BroadcastPlayerDamageClientRpc(ulong targetClientId, ulong attackerClientId, float damage, DamageType damageType, Vector3 hitDirection)
        {
            // Publish network damage event to unified event system
            if (_eventBus != null)
            {
                var networkDamageEvent = new NetworkPlayerDamageEvent
                {
                    TargetClientId = targetClientId,
                    AttackerClientId = attackerClientId,
                    Damage = damage,
                    DamageType = damageType,
                    HitDirection = hitDirection,
                    Timestamp = Time.time
                };
                
                _eventBus.Publish(networkDamageEvent);
            }
        }
        
        #endregion

        #endregion

        #region Private Methods

        private DamageRequest ProcessDamageRequest(DamageRequest originalRequest, GameObject target)
        {
            if (!_enableDamageModifiers)
                return originalRequest;

            var processedRequest = CreateDamageRequestCopy(originalRequest);

            foreach (var processor in _damageProcessors)
            {
                try
                {
                    processor.ProcessDamage(processedRequest, target);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error in damage processor {processor.GetType().Name}: {ex.Message}");
                }
            }

            return processedRequest;
        }

        private DamageRequest CreateDamageRequestCopy(DamageRequest original)
        {
            var copy = new DamageRequest(original.Amount, original.Source, original.Type)
            {
                Direction = original.Direction,
                SourcePosition = original.SourcePosition,
                CanBeBlocked = original.CanBeBlocked,
                TriggerInvulnerability = original.TriggerInvulnerability,
                Metadata = new Dictionary<string, object>(original.Metadata ?? new Dictionary<string, object>())
            };

            return copy;
        }

        private void BroadcastDamageEvent(GameObject target, DamageRequest damageRequest)
        {
            if (_eventBus == null) return;

            var damageEvent = new GlobalDamageEvent
            {
                Target = target,
                Source = damageRequest.Source as GameObject,
                Amount = damageRequest.Amount,
                Type = damageRequest.Type,
                Direction = damageRequest.Direction,
                SourcePosition = damageRequest.SourcePosition,
                Timestamp = Time.time
            };

            _eventBus.Publish(damageEvent);

            if (_enableDebugLogging)
            {
                Debug.Log($"Damage applied: {damageRequest.Amount} {damageRequest.Type} to {target.name}");
            }
        }

        #endregion
    }

    #region Damage Processor Interface and Implementations

    /// <summary>
    /// Interface for damage processors that can modify damage requests.
    /// </summary>
    public interface IDamageProcessor
    {
        /// <summary>Processing priority (lower numbers process first).</summary>
        int Priority { get; }

        /// <summary>Processes and potentially modifies a damage request.</summary>
        void ProcessDamage(DamageRequest damageRequest, GameObject target);
    }

    /// <summary>
    /// Standard damage processor that applies basic damage rules.
    /// </summary>
    public class StandardDamageProcessor : IDamageProcessor
    {
        public int Priority => 100;

        public void ProcessDamage(DamageRequest damageRequest, GameObject target)
        {
            // Apply standard damage rules
            // This is where you'd implement armor reduction, resistances, etc.
            
            // Example: Basic armor system
            var armorComponent = target.GetComponent<ArmorComponent>();
            if (armorComponent != null)
            {
                float damageReduction = armorComponent.GetDamageReduction(damageRequest.Type);
                damageRequest.Amount *= (1f - damageReduction);
            }
        }
    }

    /// <summary>
    /// Critical damage processor that handles critical hit calculations.
    /// </summary>
    public class CriticalDamageProcessor : IDamageProcessor
    {
        public int Priority => 200;

        public void ProcessDamage(DamageRequest damageRequest, GameObject target)
        {
            if (damageRequest.Type == DamageType.Critical)
            {
                // Apply critical damage multiplier
                damageRequest.Amount *= 2f;
                
                // Add critical hit metadata
                damageRequest.Metadata ??= new Dictionary<string, object>();
                damageRequest.Metadata["IsCritical"] = true;
            }
        }
    }

    /// <summary>
    /// Elemental damage processor that handles elemental effects.
    /// </summary>
    public class ElementalDamageProcessor : IDamageProcessor
    {
        public int Priority => 300;

        public void ProcessDamage(DamageRequest damageRequest, GameObject target)
        {
            switch (damageRequest.Type)
            {
                case DamageType.Fire:
                    // Apply fire effects
                    ApplyBurnEffect(target, damageRequest);
                    break;
                    
                case DamageType.Ice:
                    // Apply ice effects
                    ApplySlowEffect(target, damageRequest);
                    break;
                    
                case DamageType.Lightning:
                    // Apply lightning effects
                    ApplyStunEffect(target, damageRequest);
                    break;
            }
        }

        private void ApplyBurnEffect(GameObject target, DamageRequest damageRequest)
        {
            // Apply burning status effect
            damageRequest.Metadata ??= new Dictionary<string, object>();
            damageRequest.Metadata["ApplyBurn"] = true;
        }

        private void ApplySlowEffect(GameObject target, DamageRequest damageRequest)
        {
            // Apply slowing status effect
            damageRequest.Metadata ??= new Dictionary<string, object>();
            damageRequest.Metadata["ApplySlow"] = true;
        }

        private void ApplyStunEffect(GameObject target, DamageRequest damageRequest)
        {
            // Apply stun status effect
            damageRequest.Metadata ??= new Dictionary<string, object>();
            damageRequest.Metadata["ApplyStun"] = true;
        }
    }

    #endregion

    #region Supporting Classes

    /// <summary>
    /// Simple armor component for demonstration of damage reduction.
    /// </summary>
    public class ArmorComponent : MonoBehaviour
    {
        [SerializeField] private float _physicalReduction = 0.1f;
        [SerializeField] private float _fireResistance = 0.05f;
        [SerializeField] private float _iceResistance = 0.05f;
        [SerializeField] private float _lightningResistance = 0.05f;

        public float GetDamageReduction(DamageType damageType)
        {
            return damageType switch
            {
                DamageType.Normal => _physicalReduction,
                DamageType.Critical => _physicalReduction * 0.5f, // Less effective against crits
                DamageType.Fire => _fireResistance,
                DamageType.Ice => _iceResistance,
                DamageType.Lightning => _lightningResistance,
                _ => 0f
            };
        }
    }

    /// <summary>
    /// Global damage event that provides comprehensive damage information.
    /// </summary>
    public class GlobalDamageEvent
    {
        public GameObject Target { get; set; } = null!;
        public GameObject? Source { get; set; }
        public float Amount { get; set; }
        public DamageType Type { get; set; }
        public Vector3 Direction { get; set; }
        public Vector3 SourcePosition { get; set; }
        public float Timestamp { get; set; }
    }
    
    /// <summary>
    /// Network-aware player damage event for multiplayer scenarios.
    /// </summary>
    public class NetworkPlayerDamageEvent
    {
        public ulong TargetClientId { get; set; }
        public ulong AttackerClientId { get; set; }
        public float Damage { get; set; }
        public DamageType DamageType { get; set; }
        public Vector3 HitDirection { get; set; }
        public float Timestamp { get; set; }
    }

    #endregion
}
