using System;
using Unity.Netcode;
using UnityEngine;
using Laboratory.Core.Events;
using Laboratory.Core.DI;

#nullable enable

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// Network-synchronized health component implementing the unified health interface.
    /// Replaces the fragmented NetworkHealth and PlayerHealth components with a single,
    /// consistent implementation that works across all networked entities.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkHealthComponent : HealthComponentBase
    {
        #region Network Variables

        /// <summary>Current health value synchronized across all clients.</summary>
        public NetworkVariable<int> NetworkCurrentHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Maximum health value synchronized across all clients.</summary>
        public NetworkVariable<int> NetworkMaxHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Life state synchronized across all clients.</summary>
        public NetworkVariable<bool> NetworkIsAlive = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        #endregion

        #region Properties

        /// <summary>Network object component cached for performance.</summary>
        private NetworkObject? _networkObject;
        public NetworkObject NetworkObject => _networkObject ??= GetComponent<NetworkObject>();

        /// <summary>Whether this instance has server authority.</summary>
        public bool IsServer => NetworkObject.IsServer;

        /// <summary>Whether this instance is owned by the local client.</summary>
        public bool IsOwner => NetworkObject.IsOwner;

        /// <summary>Client ID of the owner.</summary>
        public ulong OwnerClientId => NetworkObject.OwnerClientId;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            
            // Initialize network variables with configured values
            if (IsServer)
            {
                NetworkMaxHealth.Value = _maxHealth;
                NetworkCurrentHealth.Value = _currentHealth;
                NetworkIsAlive.Value = IsAlive;
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from network variable changes
            if (NetworkCurrentHealth != null)
                NetworkCurrentHealth.OnValueChanged -= OnNetworkHealthChanged;
            if (NetworkMaxHealth != null)
                NetworkMaxHealth.OnValueChanged -= OnNetworkMaxHealthChanged;
            if (NetworkIsAlive != null)
                NetworkIsAlive.OnValueChanged -= OnNetworkIsAliveChanged;
        }

        #endregion

        #region NetworkBehaviour Implementation

        public void OnNetworkSpawn()
        {
            // Subscribe to network variable changes
            NetworkCurrentHealth.OnValueChanged += OnNetworkHealthChanged;
            NetworkMaxHealth.OnValueChanged += OnNetworkMaxHealthChanged;
            NetworkIsAlive.OnValueChanged += OnNetworkIsAliveChanged;

            // Sync local values with network values
            SyncLocalValuesFromNetwork();
        }

        public void OnNetworkDespawn()
        {
            // Unsubscribe from network variable changes
            NetworkCurrentHealth.OnValueChanged -= OnNetworkHealthChanged;
            NetworkMaxHealth.OnValueChanged -= OnNetworkMaxHealthChanged;
            NetworkIsAlive.OnValueChanged -= OnNetworkIsAliveChanged;
        }

        #endregion

        #region Health Component Overrides

        public override bool TakeDamage(DamageRequest damageRequest)
        {
            // Only server can modify health
            if (!IsServer)
            {
                Debug.LogWarning($"TakeDamage called on client for {gameObject.name}. Only server can modify health.");
                return false;
            }

            if (!CanTakeDamage(damageRequest))
                return false;

            int oldHealth = NetworkCurrentHealth.Value;
            int damage = Mathf.RoundToInt(damageRequest.Amount);
            
            // Apply damage processing
            damage = ProcessDamage(damage, damageRequest);
            
            int newHealth = Mathf.Max(0, oldHealth - damage);
            NetworkCurrentHealth.Value = newHealth;
            
            _lastDamageTime = Time.time;

            // Publish network damage event
            PublishNetworkDamageEvent(damageRequest, oldHealth, newHealth);

            // Handle death on server
            if (newHealth <= 0 && oldHealth > 0)
            {
                NetworkIsAlive.Value = false;
                HandleNetworkDeath(damageRequest);
            }

            return true;
        }

        public override bool Heal(int amount, object? source = null)
        {
            // Only server can modify health
            if (!IsServer)
            {
                Debug.LogWarning($"Heal called on client for {gameObject.name}. Only server can modify health.");
                return false;
            }

            if (amount <= 0 || !NetworkIsAlive.Value)
                return false;

            int oldHealth = NetworkCurrentHealth.Value;
            int newHealth = Mathf.Min(NetworkMaxHealth.Value, oldHealth + amount);
            NetworkCurrentHealth.Value = newHealth;

            if (oldHealth != newHealth)
            {
                PublishNetworkHealEvent(amount, oldHealth, newHealth, source);
                return true;
            }

            return false;
        }

        public override void ResetToMaxHealth()
        {
            if (!IsServer)
            {
                Debug.LogWarning($"ResetToMaxHealth called on client for {gameObject.name}. Only server can reset health.");
                return;
            }

            int oldHealth = NetworkCurrentHealth.Value;
            NetworkCurrentHealth.Value = NetworkMaxHealth.Value;
            NetworkIsAlive.Value = true;

            if (oldHealth != NetworkMaxHealth.Value)
            {
                PublishNetworkHealEvent(NetworkMaxHealth.Value - oldHealth, oldHealth, NetworkMaxHealth.Value, this);
            }
        }

        #endregion

        #region Network Event Handlers

        private void OnNetworkHealthChanged(int oldValue, int newValue)
        {
            // Update local cached values
            _currentHealth = newValue;

            // Fire local events for UI and other systems
            var healthChangedArgs = new HealthChangedEventArgs(oldValue, newValue, this);
            OnHealthChanged?.Invoke(healthChangedArgs);
            
            // Publish to event bus for UI updates
            var eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();
            eventBus?.Publish(new HealthChangedEvent(newValue, NetworkMaxHealth.Value, gameObject));
        }

        private void OnNetworkMaxHealthChanged(int oldValue, int newValue)
        {
            _maxHealth = newValue;
            
            // If current health exceeds new max, clamp it
            if (IsServer && NetworkCurrentHealth.Value > newValue)
            {
                NetworkCurrentHealth.Value = newValue;
            }
        }

        private void OnNetworkIsAliveChanged(bool oldValue, bool newValue)
        {
            if (!newValue && oldValue) // Just died
            {
                var deathArgs = new DeathEventArgs(this, null);
                OnDeath?.Invoke(deathArgs);
                
                var eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();
                eventBus?.Publish(new DeathEvent(gameObject, null));
            }
        }

        #endregion

        #region Network-Specific Methods

        /// <summary>
        /// Server-only: Apply damage with network client information.
        /// </summary>
        public bool TakeDamageFromClient(DamageRequest damageRequest, ulong attackerClientId)
        {
            if (!IsServer)
                return false;

            // Add network metadata to damage request
            damageRequest.Metadata ??= new System.Collections.Generic.Dictionary<string, object>();
            damageRequest.Metadata["AttackerClientId"] = attackerClientId;
            damageRequest.Metadata["VictimClientId"] = OwnerClientId;

            return TakeDamage(damageRequest);
        }

        /// <summary>
        /// Sets maximum health. Server authority only.
        /// </summary>
        public override void SetMaxHealth(int newMaxHealth)
        {
            if (!IsServer || newMaxHealth <= 0) return;

            NetworkMaxHealth.Value = newMaxHealth;
            
            // If current health exceeds new max, clamp it
            if (NetworkCurrentHealth.Value > newMaxHealth)
            {
                NetworkCurrentHealth.Value = newMaxHealth;
            }
        }

        private void SyncLocalValuesFromNetwork()
        {
            _currentHealth = NetworkCurrentHealth.Value;
            _maxHealth = NetworkMaxHealth.Value;
        }

        private void PublishNetworkDamageEvent(DamageRequest damageRequest, int oldHealth, int newHealth)
        {
            var eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();
            if (eventBus == null) return;

            // Create network-aware damage event
            var networkDamageEvent = new NetworkDamageEvent
            {
                Target = gameObject,
                Source = damageRequest.Source as GameObject,
                Amount = damageRequest.Amount,
                Type = damageRequest.Type,
                Direction = damageRequest.Direction,
                TargetClientId = OwnerClientId,
                AttackerClientId = damageRequest.Metadata?.ContainsKey("AttackerClientId") == true 
                    ? (ulong)damageRequest.Metadata["AttackerClientId"] 
                    : 0,
                OldHealth = oldHealth,
                NewHealth = newHealth
            };

            eventBus.Publish(networkDamageEvent);
        }

        private void PublishNetworkHealEvent(int healAmount, int oldHealth, int newHealth, object? source)
        {
            var eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();
            if (eventBus == null) return;

            var healEvent = new NetworkHealEvent
            {
                Target = gameObject,
                Source = source as GameObject,
                Amount = healAmount,
                TargetClientId = OwnerClientId,
                OldHealth = oldHealth,
                NewHealth = newHealth
            };

            eventBus.Publish(healEvent);
        }

        private void HandleNetworkDeath(DamageRequest finalDamage)
        {
            // Server-side death handling
            OnDeathBehavior();

            // Trigger death effects on all clients
            TriggerDeathEffectsClientRpc();
        }

        #endregion

        #region Client RPCs

        [ClientRpc]
        private void TriggerDeathEffectsClientRpc()
        {
            // Client-side visual/audio effects for death
            // This is where you'd trigger particle effects, sounds, animations, etc.
            Debug.Log($"[NetworkHealthComponent] {gameObject.name} death effects triggered on client");
        }

        #endregion

        #region Protected Overrides

        protected override void OnDeathBehavior()
        {
            // Network-specific death behavior
            enabled = false;
            
            // Disable network components if needed
            var collider = GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
        }

        #endregion
    }

    #region Network Event Data Classes

    /// <summary>
    /// Network-aware damage event with client ID information.
    /// </summary>
    public class NetworkDamageEvent : HealthChangedEvent
    {
        public GameObject? Source { get; set; }
        public float Amount { get; set; }
        public DamageType Type { get; set; }
        public Vector3 Direction { get; set; }
        public ulong TargetClientId { get; set; }
        public ulong AttackerClientId { get; set; }
        public int OldHealth { get; set; }
        public int NewHealth { get; set; }

        public NetworkDamageEvent() : base(0, 0, null!) { }
    }

    /// <summary>
    /// Network-aware heal event with client ID information.
    /// </summary>
    public class NetworkHealEvent
    {
        public GameObject Target { get; set; } = null!;
        public GameObject? Source { get; set; }
        public int Amount { get; set; }
        public ulong TargetClientId { get; set; }
        public int OldHealth { get; set; }
        public int NewHealth { get; set; }
    }

    #endregion
}
