using System;
using Unity.Netcode;
using UnityEngine;
using Laboratory.Core;
using Laboratory.Infrastructure.AsyncUtils;
using Laboratory.Gameplay.Combat;
using Laboratory.Models.ECS.Components;
using Laboratory.Models.ECS.Systems;

namespace Laboratory.Models.ECS.Systems
{
    /// <summary>
    /// NetworkBehaviour system responsible for handling networked damage events and broadcasting
    /// authoritative damage results across the network. This system manages damage requests from
    /// clients and ensures consistent damage application through server authority.
    /// </summary>
    public class NetworkDamageEventSystem : NetworkBehaviour
    {
        #region Fields
        
        /// <summary>
        /// Reference to the local damage event system for processing damage calculations
        /// </summary>
        private DamageEventSystem _damageEventSystem = null!;
        
        /// <summary>
        /// Flag indicating whether the system is properly initialized
        /// </summary>
        private bool _isInitialized = false;
        
        /// <summary>
        /// Cache for network manager singleton to avoid repeated lookups
        /// </summary>
        private NetworkManager _networkManager = null!;
        
        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Called when the NetworkBehaviour awakens. Initializes dependencies and
        /// sets up the damage event system reference.
        /// </summary>
        private void Awake()
        {
            InitializeDependencies();
        }

        /// <summary>
        /// Called when the NetworkBehaviour starts. Validates network connectivity
        /// and ensures proper initialization.
        /// </summary>
        private void Start()
        {
            ValidateNetworkState();
        }

        /// <summary>
        /// Called when the NetworkBehaviour is destroyed. Cleans up resources and references.
        /// </summary>
        public override void OnDestroy()
        {
            CleanupResources();

            base.OnDestroy();
        }

        #endregion

        #region Network RPCs

        /// <summary>
        /// Server RPC called by clients to request damage application to a target.
        /// The server validates the request and applies damage authoritatively.
        /// </summary>
        /// <param name="targetNetworkId">Network ID of the target entity to damage</param>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="hitDirection">Direction vector of the damage impact</param>
        /// <param name="attackerId">Network ID of the attacking entity</param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestDamageServerRpc(ulong targetNetworkId, float damage, Vector3 hitDirection, ulong attackerId)
        {
            if (!IsServer)
            {
                Debug.LogWarning("RequestDamageServerRpc called on non-server instance");
                return;
            }

            if (!_isInitialized)
            {
                Debug.LogError("NetworkDamageEventSystem not properly initialized, ignoring damage request");
                return;
            }

            ProcessDamageRequest(targetNetworkId, damage, hitDirection, attackerId);
        }

        /// <summary>
        /// Client RPC called by the server to broadcast damage events to all clients.
        /// This ensures all clients receive the authoritative damage result.
        /// </summary>
        /// <param name="targetNetworkId">Network ID of the damaged entity</param>
        /// <param name="damage">Amount of damage that was applied</param>
        /// <param name="hitDirection">Direction vector of the damage impact</param>
        /// <param name="attackerId">Network ID of the attacking entity</param>
        [ClientRpc]
        private void BroadcastDamageClientRpc(ulong targetNetworkId, float damage, Vector3 hitDirection, ulong attackerId)
        {
            try
            {
                ProcessDamageNotification(targetNetworkId, damage, hitDirection, attackerId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing damage notification: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes all required dependencies for the damage event system
        /// </summary>
        private void InitializeDependencies()
        {
            try
            {
                _damageEventSystem = FindObjectOfType<DamageEventSystem>();
                _networkManager = NetworkManager.Singleton;
                
                ValidateDependencies();
                _isInitialized = true;
                Debug.Log("NetworkDamageEventSystem initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize NetworkDamageEventSystem: {ex.Message}");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Validates that all required dependencies are properly resolved
        /// </summary>
        private void ValidateDependencies()
        {
            if (_damageEventSystem == null)
            {
                throw new InvalidOperationException("DamageEventSystem not found in scene");
            }
            
            if (_networkManager == null)
            {
                throw new InvalidOperationException("NetworkManager.Singleton is null");
            }
        }

        /// <summary>
        /// Validates the current network state and connectivity
        /// </summary>
        private void ValidateNetworkState()
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_networkManager == null || !_networkManager.IsListening)
            {
                Debug.LogWarning("NetworkManager is not listening, damage events may not function properly");
            }
        }

        /// <summary>
        /// Processes a damage request on the server side with validation and authority checks
        /// </summary>
        /// <param name="targetNetworkId">Network ID of the target entity</param>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="hitDirection">Direction vector of the damage impact</param>
        /// <param name="attackerId">Network ID of the attacking entity</param>
        private void ProcessDamageRequest(ulong targetNetworkId, float damage, Vector3 hitDirection, ulong attackerId)
        {
            try
            {
                // Validate damage parameters
                if (!ValidateDamageParameters(targetNetworkId, damage, attackerId))
                {
                    return;
                }

                // Find the target network object
                var targetObject = GetNetworkObjectById(targetNetworkId);
                if (targetObject == null)
                {
                    Debug.LogWarning($"Target network object with ID {targetNetworkId} not found");
                    return;
                }

                // Create damage event for server-side processing
                var damageEvent = CreateDamageEvent(targetNetworkId, damage, hitDirection, attackerId);
                
                // Apply damage through the local damage system
                _damageEventSystem.ApplyDamage(damageEvent);

                // Broadcast the authoritative result to all clients
                BroadcastDamageClientRpc(targetNetworkId, damage, hitDirection, attackerId);
                
                Debug.Log($"Processed damage: {damage} to target {targetNetworkId} from attacker {attackerId}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing damage request: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates damage parameters to ensure they are within acceptable ranges
        /// </summary>
        /// <param name="targetNetworkId">Network ID of the target entity</param>
        /// <param name="damage">Amount of damage to validate</param>
        /// <param name="attackerId">Network ID of the attacking entity</param>
        /// <returns>True if parameters are valid</returns>
        private bool ValidateDamageParameters(ulong targetNetworkId, float damage, ulong attackerId)
        {
            if (damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage))
            {
                Debug.LogWarning($"Invalid damage amount: {damage}");
                return false;
            }

            if (targetNetworkId == attackerId)
            {
                Debug.LogWarning("Self-damage detected, validation may be required");
                // Note: Self-damage might be valid in some game scenarios
            }

            // Add additional validation as needed (max damage limits, cooldowns, etc.)
            const float maxDamagePerRequest = 1000f;
            if (damage > maxDamagePerRequest)
            {
                Debug.LogWarning($"Damage amount {damage} exceeds maximum allowed {maxDamagePerRequest}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves a network object by its network ID
        /// </summary>
        /// <param name="networkId">The network ID to search for</param>
        /// <returns>The network object if found, null otherwise</returns>
        private NetworkObject GetNetworkObjectById(ulong networkId)
        {
            if (_networkManager?.SpawnManager == null)
            {
                Debug.LogError("NetworkManager or SpawnManager is null");
                return null;
            }

            return _networkManager.SpawnManager.SpawnedObjects.TryGetValue(networkId, out var networkObject) 
                ? networkObject 
                : null;
        }

        /// <summary>
        /// Creates a damage event structure from the provided parameters
        /// </summary>
        /// <param name="targetNetworkId">Network ID of the target entity</param>
        /// <param name="damage">Amount of damage to apply</param>
        /// <param name="hitDirection">Direction vector of the damage impact</param>
        /// <param name="attackerId">Network ID of the attacking entity</param>
        /// <returns>Configured damage event structure</returns>
        private DamageEvent CreateDamageEvent(ulong targetNetworkId, float damage, Vector3 hitDirection, ulong attackerId)
        {
            return new DamageEvent(
            
                targetClientId : targetNetworkId,
                attackerClientId : attackerId,
                damageAmount : damage,
                hitDirection : hitDirection
            );
        }

        /// <summary>
        /// Processes damage notification on client side and triggers appropriate UI effects
        /// </summary>
        /// <param name="targetNetworkId">Network ID of the damaged entity</param>
        /// <param name="damage">Amount of damage that was applied</param>
        /// <param name="hitDirection">Direction vector of the damage impact</param>
        /// <param name="attackerId">Network ID of the attacking entity</param>
        private void ProcessDamageNotification(ulong targetNetworkId, float damage, Vector3 hitDirection, ulong attackerId)
        {
            if (_networkManager?.LocalClientId == null)
            {
                Debug.LogWarning("Local client ID is null, cannot process damage notification");
                return;
            }

            // Only trigger local UI effects if we are the target
            if (_networkManager.LocalClientId == targetNetworkId)
            {
                var damageEvent = CreateDamageEvent(targetNetworkId, damage, hitDirection, attackerId);
                
                // Publish to message bus for UI and effect systems
                MessageBus.Publish(damageEvent);
                Debug.Log($"Processed local damage notification: {damage} damage received");
            }
            else
            {
                Debug.Log($"Received damage notification for other player {targetNetworkId}: {damage} damage");
            }
        }

        /// <summary>
        /// Cleans up resources and references when the system is destroyed
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                _damageEventSystem = null!;
                _networkManager = null!;
                _isInitialized = false;
                Debug.Log("NetworkDamageEventSystem resources cleaned up");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during NetworkDamageEventSystem cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if the network damage event system is properly initialized and ready for use
        /// </summary>
        /// <returns>True if the system is initialized and functional</returns>
        public bool IsInitialized()
        {
            return _isInitialized && _damageEventSystem != null && _networkManager != null;
        }

        /// <summary>
        /// Gets the current damage event system instance for external access
        /// </summary>
        /// <returns>The current damage event system instance, or null if not initialized</returns>
        public DamageEventSystem GetDamageEventSystem()
        {
            return _isInitialized ? _damageEventSystem : null;
        }

        #endregion
    }
}
