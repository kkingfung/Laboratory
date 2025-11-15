using System;
using UnityEngine;
using Unity.Netcode;
using Laboratory.Core.Health;
using Laboratory.Core.Infrastructure;
using Laboratory.Core.Enums;

namespace Laboratory.Core.Health.Components
{
    /// <summary>
    /// Network-synchronized health component for multiplayer games
    /// Provides authoritative health management with client prediction support
    /// This replaces the empty NetworkHealthComponent file
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkHealthComponent : NetworkBehaviour, IHealthComponent
    {
        #region Network Variables

        [Header("Health Settings")]
        [SerializeField] private int _maxHealth = 100;
        [SerializeField] private bool _canRegenerate = false;
        [SerializeField] private float _regenerationRate = 1f;
        [SerializeField] private float _regenerationDelay = 3f;
        [SerializeField] private float _invulnerabilityDuration = 0.5f;

        // Network synchronized health values
        private NetworkVariable<int> _networkCurrentHealth = new NetworkVariable<int>(
            value: 100,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server);

        private NetworkVariable<int> _networkMaxHealth = new NetworkVariable<int>(
            value: 100,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> _networkIsAlive = new NetworkVariable<bool>(
            value: true,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server);

        private NetworkVariable<float> _networkLastDamageTime = new NetworkVariable<float>(
            value: 0f,
            readPerm: NetworkVariableReadPermission.Everyone,
            writePerm: NetworkVariableWritePermission.Server);

        #endregion

        #region Private Fields

        private IHealthEventPublisher _healthEventPublisher;
        private float _lastInvulnerabilityTime;
        
        // Client prediction variables
        private int _predictedHealth;
        private bool _predictionEnabled = true;
        private float _lastPredictionTime;

        #endregion

        #region Properties

        public int MaxHealth => _networkMaxHealth.Value;
        public int CurrentHealth => IsClient && _predictionEnabled ? _predictedHealth : _networkCurrentHealth.Value;
        public float HealthPercentage => MaxHealth > 0 ? (float)CurrentHealth / MaxHealth : 0f;
        public bool IsAlive => _networkIsAlive.Value && CurrentHealth > 0;
        public bool IsDead => !IsAlive;
        public bool CanRegenerate => _canRegenerate;
        public bool IsInvulnerable => Time.time - _lastInvulnerabilityTime < _invulnerabilityDuration;

        #endregion

        #region Events

        public event Action<HealthChangedEventArgs> OnHealthChanged;
        public event Action<DeathEventArgs> OnDeath;
        public event Action<DamageRequest> OnDamageTaken;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Initialize predicted health
            _predictedHealth = _maxHealth;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Initialize network variables on spawn
            if (IsServer)
            {
                _networkCurrentHealth.Value = _maxHealth;
                _networkMaxHealth.Value = _maxHealth;
                _networkIsAlive.Value = true;
                _networkLastDamageTime.Value = 0f;
            }

            // Subscribe to network variable changes
            _networkCurrentHealth.OnValueChanged += OnNetworkHealthChanged;
            _networkMaxHealth.OnValueChanged += OnNetworkMaxHealthChanged;
            _networkIsAlive.OnValueChanged += OnNetworkAliveStatusChanged;

            // Initialize predicted health from network value
            _predictedHealth = _networkCurrentHealth.Value;

            // Get health event publisher for publishing events
            var serviceContainer = ServiceContainer.Instance;
            if (serviceContainer != null)
            {
                _healthEventPublisher = serviceContainer.ResolveService<IHealthEventPublisher>();
            }
        }

        public override void OnNetworkDespawn()
        {
            // Unsubscribe from network variable changes
            _networkCurrentHealth.OnValueChanged -= OnNetworkHealthChanged;
            _networkMaxHealth.OnValueChanged -= OnNetworkMaxHealthChanged;
            _networkIsAlive.OnValueChanged -= OnNetworkAliveStatusChanged;

            base.OnNetworkDespawn();
        }

        private void Update()
        {
            if (IsServer)
            {
                HandleServerRegeneration();
            }
            else if (IsClient)
            {
                HandleClientPredictionValidation();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Apply damage to this health component
        /// Clients send RPC to server, server applies damage authoritatively
        /// </summary>
        public bool TakeDamage(DamageRequest damageRequest)
        {
            if (!IsAlive || damageRequest == null) return false;

            if (IsServer)
            {
                return ApplyDamageOnServer(damageRequest);
            }
            else if (IsClient && IsOwner)
            {
                // Client prediction
                ApplyDamageClientPrediction(damageRequest);
                
                // Send RPC to server for authoritative damage
                TakeDamageServerRpc(damageRequest.Amount, (int)damageRequest.Type, 
                    damageRequest.CanBeBlocked, damageRequest.CanBeDodged, 
                    damageRequest.IsCritical, damageRequest.HitPoint, damageRequest.Direction,
                    damageRequest.KnockbackForce, damageRequest.DamageId);
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Heal this health component
        /// </summary>
        public bool Heal(int amount, object source = null)
        {
            if (!IsAlive || amount <= 0) return false;

            if (IsServer)
            {
                return ApplyHealingOnServer(amount, source);
            }
            else if (IsClient && IsOwner)
            {
                // Client prediction
                ApplyHealingClientPrediction(amount);
                
                // Send RPC to server
                HealServerRpc(amount);
                
                return true;
            }

            return false;
        }

        /// <summary>
        /// Set maximum health (server only)
        /// </summary>
        public void SetMaxHealth(int newMaxHealth)
        {
            if (!IsServer || newMaxHealth <= 0) return;

            int oldMaxHealth = _networkMaxHealth.Value;
            float healthPercentage = (float)_networkCurrentHealth.Value / oldMaxHealth;
            
            _networkMaxHealth.Value = newMaxHealth;
            _networkCurrentHealth.Value = Mathf.RoundToInt(newMaxHealth * healthPercentage);
        }

        /// <summary>
        /// Restore to full health (server only)
        /// </summary>
        public void ResetToMaxHealth()
        {
            if (!IsServer) return;

            int oldHealth = _networkCurrentHealth.Value;
            _networkCurrentHealth.Value = _networkMaxHealth.Value;
            
            var eventArgs = new HealthChangedEventArgs(oldHealth, _networkCurrentHealth.Value, null);
            OnHealthChanged?.Invoke(eventArgs);
        }

        /// <summary>
        /// Kill this entity instantly (server only)
        /// </summary>
        public void Kill(GameObject killer = null, string cause = "Killed")
        {
            if (!IsServer || !_networkIsAlive.Value) return;

            var damageRequest = new DamageRequest(_networkCurrentHealth.Value, killer, DamageType.True)
            {
                Description = cause
            };

            ApplyDamageOnServer(damageRequest);
        }

        /// <summary>
        /// Revive this entity (server only)
        /// </summary>
        public void Revive(int healthAmount = -1)
        {
            if (!IsServer || _networkIsAlive.Value) return;

            _networkIsAlive.Value = true;
            int oldHealth = _networkCurrentHealth.Value;
            _networkCurrentHealth.Value = healthAmount < 0 ? _networkMaxHealth.Value : Mathf.Min(_networkMaxHealth.Value, healthAmount);

            var eventArgs = new HealthChangedEventArgs(oldHealth, _networkCurrentHealth.Value, null);
            OnHealthChanged?.Invoke(eventArgs);
        }

        #endregion

        #region Server Methods

        private bool ApplyDamageOnServer(DamageRequest damageRequest)
        {
            if (!_networkIsAlive.Value) return false;

            // Check invulnerability
            if (IsInvulnerable && damageRequest.TriggerInvulnerability) return false;

            int oldHealth = _networkCurrentHealth.Value;
            int damageAmount = Mathf.RoundToInt(damageRequest.Amount);
            
            _networkCurrentHealth.Value = Mathf.Max(0, _networkCurrentHealth.Value - damageAmount);
            _networkLastDamageTime.Value = Time.time;

            // Set invulnerability if needed
            if (damageRequest.TriggerInvulnerability)
            {
                _lastInvulnerabilityTime = Time.time;
            }

            // Broadcast damage taken to all clients
            OnDamageTakenClientRpc(damageRequest.Amount, (int)damageRequest.Type, 
                damageRequest.HitPoint, damageRequest.Direction, damageRequest.DamageId);

            var eventArgs = new HealthChangedEventArgs(oldHealth, _networkCurrentHealth.Value, damageRequest.Source);
            OnHealthChanged?.Invoke(eventArgs);

            // Publish damage event through interface to avoid circular dependencies
            _healthEventPublisher?.PublishDamageEvent(
                gameObject,
                damageRequest.Source,
                damageRequest.Amount,
                (int)damageRequest.Type,
                damageRequest.Direction
            );

            // Check for death
            if (_networkCurrentHealth.Value <= 0 && _networkIsAlive.Value)
            {
                HandleDeathOnServer(damageRequest);
            }

            return true;
        }

        private bool ApplyHealingOnServer(int amount, object source = null)
        {
            if (!_networkIsAlive.Value) return false;

            int oldHealth = _networkCurrentHealth.Value;
            _networkCurrentHealth.Value = Mathf.Min(_networkMaxHealth.Value, _networkCurrentHealth.Value + amount);

            // Broadcast healing to all clients
            OnHealedClientRpc(amount);

            var eventArgs = new HealthChangedEventArgs(oldHealth, _networkCurrentHealth.Value, source);
            OnHealthChanged?.Invoke(eventArgs);

            return true;
        }

        private void HandleDeathOnServer(DamageRequest finalDamage)
        {
            _networkIsAlive.Value = false;

            var deathArgs = new DeathEventArgs(finalDamage.Source, finalDamage);
            OnDeath?.Invoke(deathArgs);

            // Broadcast death to all clients
            OnDeathClientRpc(finalDamage.DamageId);

            // Publish death event through interface to avoid circular dependencies
            _healthEventPublisher?.PublishDeathEvent(
                gameObject,
                finalDamage.Source
            );
        }

        private void HandleServerRegeneration()
        {
            if (!_canRegenerate || !_networkIsAlive.Value || _networkCurrentHealth.Value >= _networkMaxHealth.Value)
                return;

            if (Time.time - _networkLastDamageTime.Value >= _regenerationDelay)
            {
                int regenAmount = Mathf.RoundToInt(_regenerationRate * Time.deltaTime);
                if (regenAmount > 0)
                {
                    ApplyHealingOnServer(regenAmount);
                }
            }
        }

        #endregion

        #region Client Prediction

        private void ApplyDamageClientPrediction(DamageRequest damageRequest)
        {
            if (!_predictionEnabled) return;

            _predictedHealth = Mathf.Max(0, _predictedHealth - Mathf.RoundToInt(damageRequest.Amount));
            _lastPredictionTime = Time.time;
        }

        private void ApplyHealingClientPrediction(int amount)
        {
            if (!_predictionEnabled) return;

            _predictedHealth = Mathf.Min(_networkMaxHealth.Value, _predictedHealth + amount);
            _lastPredictionTime = Time.time;
        }

        private void HandleClientPredictionValidation()
        {
            // Reset prediction if it's been too long since last update
            if (Time.time - _lastPredictionTime > 2f)
            {
                _predictedHealth = _networkCurrentHealth.Value;
                _predictionEnabled = true;
            }

            // Validate prediction against server state
            int healthDifference = Mathf.Abs(_predictedHealth - _networkCurrentHealth.Value);
            if (healthDifference > 10) // Significant difference, disable prediction temporarily
            {
                _predictionEnabled = false;
                _predictedHealth = _networkCurrentHealth.Value;
            }
        }

        #endregion

        #region Network RPCs

        [ServerRpc(RequireOwnership = true)]
        private void TakeDamageServerRpc(float amount, int damageType, bool canBeBlocked, bool canBeDodged, 
            bool isCritical, Vector3 hitPoint, Vector3 direction, float knockbackForce, int damageId)
        {
            var damageRequest = new DamageRequest(amount, null, (DamageType)damageType)
            {
                CanBeBlocked = canBeBlocked,
                CanBeDodged = canBeDodged,
                IsCritical = isCritical,
                HitPoint = hitPoint,
                Direction = direction,
                KnockbackForce = knockbackForce
            };
            // Note: DamageId is set automatically in constructor and cannot be modified

            ApplyDamageOnServer(damageRequest);
        }

        [ServerRpc(RequireOwnership = true)]
        private void HealServerRpc(int amount)
        {
            ApplyHealingOnServer(amount);
        }

        [ClientRpc]
        private void OnDamageTakenClientRpc(float amount, int damageType, Vector3 hitPoint, Vector3 direction, int damageId)
        {
            // Create damage request for client-side effects
            var damageRequest = new DamageRequest(amount, null, (DamageType)damageType)
            {
                HitPoint = hitPoint,
                Direction = direction
            };
            // Note: DamageId is auto-generated and cannot be set directly

            OnDamageTaken?.Invoke(damageRequest);
        }

        [ClientRpc]
        private void OnHealedClientRpc(int amount)
        {
            // Handle healing effects on clients
        }

        [ClientRpc]
        private void OnDeathClientRpc(int finalDamageId)
        {
            // Handle death effects on clients
        }

        #endregion

        #region Network Variable Callbacks

        private void OnNetworkHealthChanged(int previousValue, int newValue)
        {
            // Update predicted health when server value changes
            if (IsClient && !IsOwner)
            {
                _predictedHealth = newValue;
            }

            var eventArgs = new HealthChangedEventArgs(previousValue, newValue, null);
            OnHealthChanged?.Invoke(eventArgs);
        }

        private void OnNetworkMaxHealthChanged(int previousValue, int newValue)
        {
            // Handle max health changes
        }

        private void OnNetworkAliveStatusChanged(bool previousValue, bool newValue)
        {
            if (!newValue && previousValue) // Just died
            {
                var deathArgs = new DeathEventArgs(null, null);
                OnDeath?.Invoke(deathArgs);
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Debug Health Info")]
        private void DebugHealthInfo()
        {
            Debug.Log($"NetworkHealthComponent Debug Info:\n" +
                     $"Current Health: {CurrentHealth}/{MaxHealth} ({HealthPercentage:P1})\n" +
                     $"Is Alive: {IsAlive}\n" +
                     $"Network Health: {_networkCurrentHealth.Value}\n" +
                     $"Predicted Health: {_predictedHealth}\n" +
                     $"Prediction Enabled: {_predictionEnabled}\n" +
                     $"Is Invulnerable: {IsInvulnerable}\n" +
                     $"Can Regenerate: {CanRegenerate}");
        }

        #endregion
    }

}
