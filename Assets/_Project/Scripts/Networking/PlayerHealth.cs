using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Laboratory.Core.Health.Components;
using Laboratory.Core.Health;
using Laboratory.Core.Events;
using Laboratory.Core.DI;
using Laboratory.Core.Systems;
using Laboratory.Gameplay;
using Laboratory.Gameplay.Respawn;
using HealthChangedEventArgs = Laboratory.Core.Health.HealthChangedEventArgs;
using DeathEventArgs = Laboratory.Core.Health.DeathEventArgs;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Enhanced player health system that combines network health with respawn mechanics.
    /// This replaces the old fragmented PlayerHealth implementation with a composition-based approach.
    /// Uses the unified NetworkHealthComponent and adds player-specific respawn functionality.
    /// Integrates with the unified health system service for centralized management.
    /// </summary>
    [RequireComponent(typeof(NetworkHealthComponent))]
    [RequireComponent(typeof(PlayerRespawnComponent))]
    public class PlayerHealth : MonoBehaviour, IDisposable
    {
        #region Components
        
        private NetworkHealthComponent _healthComponent;
        private PlayerRespawnComponent _respawnComponent;
        private IEventBus _eventBus;
        private IHealthSystem _healthSystem;
        private bool _isDisposed = false;
        
        #endregion
        
        #region Properties
        
        /// <summary>Network health component handling the health logic.</summary>
        public NetworkHealthComponent HealthComponent => _healthComponent;
        
        /// <summary>Respawn component handling player respawn logic.</summary>
        public PlayerRespawnComponent RespawnComponent => _respawnComponent;
        
        /// <summary>Current health value.</summary>
        public int CurrentHealth => _healthComponent.CurrentHealth;
        
        /// <summary>Maximum health value.</summary>
        public int MaxHealth => _healthComponent.MaxHealth;
        
        /// <summary>Whether this player is currently alive.</summary>
        public bool IsAlive => _healthComponent.IsAlive;
        
        /// <summary>Health as a normalized percentage (0.0 to 1.0).</summary>
        public float HealthPercentage => _healthComponent.HealthPercentage;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Get required components
            _healthComponent = GetComponent<NetworkHealthComponent>();
            _respawnComponent = GetComponent<PlayerRespawnComponent>();
            
            if (_healthComponent == null)
            {
                Debug.LogError($"PlayerHealth on {gameObject.name} requires NetworkHealthComponent!");
                enabled = false;
                return;
            }
            
            if (_respawnComponent == null)
            {
                Debug.LogError($"PlayerHealth on {gameObject.name} requires PlayerRespawnComponent!");
                enabled = false;
                return;
            }
        }
        
        private void Start()
        {
            // Get services from global service provider
            if (GlobalServiceProvider.IsInitialized)
            {
                GlobalServiceProvider.Instance?.TryResolve<IEventBus>(out _eventBus);
                GlobalServiceProvider.Instance?.TryResolve<IHealthSystem>(out _healthSystem);
            }
            
            // Subscribe to health component events
            _healthComponent.OnHealthChanged += OnHealthChanged;
            _healthComponent.OnDeath += OnPlayerDeath;
            
            // Register with health system (manual registration for player-specific handling)
            _healthSystem?.RegisterHealthComponent(_healthComponent);
        }
        
        private void OnDestroy()
        {
            Dispose();
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Applies damage to this player. Server authority only.
        /// </summary>
        /// <param name="damageRequest">Damage request with all damage information.</param>
        /// <returns>True if damage was applied.</returns>
        public bool TakeDamage(DamageRequest damageRequest)
        {
            return _healthComponent.TakeDamage(damageRequest);
        }
        
        /// <summary>
        /// Applies damage from a specific client. Server authority only.
        /// </summary>
        /// <param name="damageRequest">Damage request with all damage information.</param>
        /// <param name="attackerClientId">Client ID of the attacker.</param>
        /// <returns>True if damage was applied.</returns>
        public bool TakeDamageFromClient(DamageRequest damageRequest, ulong attackerClientId)
        {
            var networkHealthComp = _healthComponent as NetworkHealthComponent;
            return networkHealthComp?.TakeDamageFromClient(damageRequest, attackerClientId) ?? false;
        }
        
        /// <summary>
        /// Heals the player by the specified amount. Server authority only.
        /// </summary>
        /// <param name="amount">Amount of health to restore.</param>
        /// <param name="source">Source of the healing.</param>
        /// <returns>True if healing was applied.</returns>
        public bool Heal(int amount, object source = null)
        {
            return _healthComponent.Heal(amount, source);
        }
        
        /// <summary>
        /// Resets player to full health. Server authority only.
        /// </summary>
        public void ResetHealth()
        {
            _healthComponent.ResetToMaxHealth();
        }
        
        /// <summary>
        /// Triggers player respawn if conditions are met.
        /// </summary>
        public void TriggerRespawn()
        {
            _respawnComponent.TriggerRespawn();
        }
        
        #endregion
        
        #region IDisposable
        
        public void Dispose()
        {
            if (_isDisposed) return;
            
            // Unregister from health system
            if (_healthSystem != null && _healthComponent != null)
            {
                _healthSystem.UnregisterHealthComponent(_healthComponent);
            }
            
            // Unsubscribe from events
            if (_healthComponent != null)
            {
                _healthComponent.OnHealthChanged -= OnHealthChanged;
                _healthComponent.OnDeath -= OnPlayerDeath;
            }
            
            _isDisposed = true;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnHealthChanged(HealthChangedEventArgs args)
        {
            // Publish player-specific health change event
            _eventBus?.Publish(new PlayerHealthChangedEvent
            {
                PlayerId = GetComponent<NetworkObject>().OwnerClientId,
                OldHealth = args.OldHealth,
                NewHealth = args.NewHealth,
                MaxHealth = MaxHealth,
                PlayerGameObject = gameObject
            });
        }
        
        private void OnPlayerDeath(DeathEventArgs args)
        {
            // Publish player death event
            _eventBus?.Publish(new PlayerDeathEvent
            {
                PlayerId = GetComponent<NetworkObject>().OwnerClientId,
                PlayerGameObject = gameObject,
                Source = args.Source,
                FinalDamage = args.FinalDamage
            });
            
            // Trigger respawn system
            _respawnComponent.StartRespawnSequence();
        }
        
        #endregion
    }
}

namespace Laboratory.Gameplay
{
    /// <summary>
    /// Handles player respawn mechanics separately from health logic.
    /// This follows single responsibility principle and makes the code more modular.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerRespawnComponent : NetworkBehaviour
    {
        [Header("Respawn Configuration")]
        [SerializeField] private float _respawnDelay = 5f;
        [SerializeField] private Transform _respawnPointFallback;
        
        [Header("Components to Disable on Death")]
        [SerializeField] private MonoBehaviour[] _componentsToDisable = new MonoBehaviour[0];
        [SerializeField] private GameObject[] _objectsToDisable = new GameObject[0];
        
        [Header("Animation")]
        [SerializeField] private Animator _animator;
        
        /// <summary>Time remaining until respawn becomes available.</summary>
        public NetworkVariable<float> RespawnTimeRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);
        
        /// <summary>Current respawn countdown coroutine.</summary>
        private Coroutine _respawnCoroutine;
        
        /// <summary>Whether the player can currently respawn.</summary>
        public bool CanRespawn => RespawnTimeRemaining.Value <= 0f;
        
        public override void OnNetworkSpawn()
        {
            RespawnTimeRemaining.OnValueChanged += OnRespawnTimeChanged;
        }
        
        public override void OnNetworkDespawn()
        {
            RespawnTimeRemaining.OnValueChanged -= OnRespawnTimeChanged;
        }
        
        /// <summary>
        /// Starts the respawn sequence. Server authority only.
        /// </summary>
        public void StartRespawnSequence()
        {
            if (!IsServer) return;
            
            DisablePlayerComponents();
            PlayDeathAnimationClientRpc();
            
            // Start respawn countdown
            if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = StartCoroutine(RespawnCountdown());
        }
        
        /// <summary>
        /// Triggers immediate respawn if allowed.
        /// </summary>
        public void TriggerRespawn()
        {
            if (!IsServer || !CanRespawn) return;
            
            PerformRespawn();
        }
        
        private void DisablePlayerComponents()
        {
            foreach (var component in _componentsToDisable)
            {
                if (component != null) component.enabled = false;
            }
            
            foreach (var gameObject in _objectsToDisable)
            {
                if (gameObject != null) gameObject.SetActive(false);
            }
        }
        
        private void EnablePlayerComponents()
        {
            foreach (var component in _componentsToDisable)
            {
                if (component != null) component.enabled = true;
            }
            
            foreach (var gameObject in _objectsToDisable)
            {
                if (gameObject != null) gameObject.SetActive(true);
            }
        }
        
        private IEnumerator RespawnCountdown()
        {
            RespawnTimeRemaining.Value = _respawnDelay;
            
            while (RespawnTimeRemaining.Value > 0f)
            {
                yield return new WaitForSeconds(1f);
                RespawnTimeRemaining.Value = Mathf.Max(0f, RespawnTimeRemaining.Value - 1f);
            }
            
            // Perform respawn when countdown reaches zero
            PerformRespawn();
        }
        
        private void PerformRespawn()
        {
            if (!IsServer) return;
            
            // Get health component and reset health
            var healthComponent = GetComponent<NetworkHealthComponent>();
            healthComponent?.ResetToMaxHealth();
            
            // Reset respawn timer
            RespawnTimeRemaining.Value = 0f;
            
            // Re-enable components
            EnablePlayerComponents();
            
            // Determine respawn position
            Vector3 spawnPos = GetRespawnPosition();
            transform.position = spawnPos;
            transform.rotation = Quaternion.identity;
            
            // Trigger respawn effects on all clients
            PlayRespawnClientRpc(spawnPos);
            
            // Publish respawn event
            var eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();
            eventBus?.Publish(new PlayerRespawnEvent
            {
                PlayerId = OwnerClientId,
                RespawnPosition = spawnPos,
                PlayerGameObject = gameObject
            });
        }
        
        private Vector3 GetRespawnPosition()
        {
            // Try to get position from respawn manager
            var respawnManager = FindFirstObjectByType<RespawnManager>();
            if (respawnManager != null)
            {
                Transform spawnPoint = respawnManager.GetRandomSpawnPoint();
                if (spawnPoint != null)
                {
                    return spawnPoint.position;
                }
            }
            
            // Fall back to designated respawn point or origin
            return _respawnPointFallback != null ? _respawnPointFallback.position : Vector3.zero;
        }
        
        private void OnRespawnTimeChanged(float oldValue, float newValue)
        {
            // Update respawn UI
            var eventBus = GlobalServiceProvider.Instance?.Resolve<IEventBus>();
            eventBus?.Publish(new PlayerRespawnTimerEvent
            {
                PlayerId = OwnerClientId,
                TimeRemaining = newValue,
                CanRespawn = CanRespawn
            });
        }
        
        #region Client RPCs
        
        [ClientRpc]
        private void PlayDeathAnimationClientRpc()
        {
            if (_animator != null)
            {
                _animator.SetTrigger("Die");
            }
            
            Debug.Log($"[PlayerRespawnComponent] {gameObject.name} death animation triggered on client");
        }
        
        [ClientRpc]
        private void PlayRespawnClientRpc(Vector3 spawnPosition)
        {
            // Ensure transform is updated on clients
            transform.position = spawnPosition;
            
            if (_animator != null)
            {
                _animator.SetTrigger("Respawn");
            }
            
            Debug.Log($"[PlayerRespawnComponent] {gameObject.name} respawn animation triggered on client");
        }
        
        #endregion
    }

    #region Player Event Data Classes

    /// <summary>
    /// Player-specific health change event.
    /// </summary>
    public class PlayerHealthChangedEvent
    {
        public ulong PlayerId { get; set; }
        public int OldHealth { get; set; }
        public int NewHealth { get; set; }
        public int MaxHealth { get; set; }
        public GameObject PlayerGameObject { get; set; }
    }

    /// <summary>
    /// Player death event with additional player context.
    /// </summary>
    public class PlayerDeathEvent
    {
        public ulong PlayerId { get; set; }
        public GameObject PlayerGameObject { get; set; }
        public object Source { get; set; }
        public DamageRequest FinalDamage { get; set; }
    }

    /// <summary>
    /// Player respawn event.
    /// </summary>
    public class PlayerRespawnEvent
    {
        public ulong PlayerId { get; set; }
        public Vector3 RespawnPosition { get; set; }
        public GameObject PlayerGameObject { get; set; }
    }

    /// <summary>
    /// Player respawn timer update event.
    /// </summary>
    public class PlayerRespawnTimerEvent
    {
        public ulong PlayerId { get; set; }
        public float TimeRemaining { get; set; }
        public bool CanRespawn { get; set; }
    }

    #endregion
}
