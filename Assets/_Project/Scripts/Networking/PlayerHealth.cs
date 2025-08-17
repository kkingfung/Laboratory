using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Laboratory.Gameplay.Respawn;
using Laboratory.Models.ECS.Components;

namespace Laboratory.Infrastructure.Networking
{
    /// <summary>
    /// Server-authoritative player health system with network synchronization.
    /// Manages health, death, and respawn mechanics for multiplayer gameplay.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerHealth : NetworkBehaviour
    {
        #region Fields

        [Header("Health Configuration")]
        [SerializeField] private int maxHealth = 100;

        /// <summary>Current health value synchronized across all clients.</summary>
        public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
            100,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Player's current life state (alive/dead).</summary>
        public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
            true,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        /// <summary>Time remaining until respawn becomes available.</summary>
        public NetworkVariable<float> RespawnTimeRemaining = new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        [Header("Respawn Configuration")]
        [SerializeField] private float respawnDelay = 5f;
        [SerializeField] private Transform respawnPointFallback;

        [Header("Components to Disable on Death")]
        [SerializeField] private MonoBehaviour[] componentsToDisable = new MonoBehaviour[0];
        [SerializeField] private GameObject[] objectsToDisable = new GameObject[0];

        [Header("Animation")]
        [SerializeField] private Animator animator;

        /// <summary>Current respawn countdown coroutine.</summary>
        private Coroutine _respawnCoroutine;

        #endregion

        #region Properties

        /// <summary>Maximum health value for this player.</summary>
        public int MaxHealth => maxHealth;

        /// <summary>Health as a normalized percentage (0.0 to 1.0).</summary>
        public float HealthPercentage => maxHealth > 0 ? (float)CurrentHealth.Value / maxHealth : 0f;

        /// <summary>Whether the player can currently respawn.</summary>
        public bool CanRespawn => !IsAlive.Value && RespawnTimeRemaining.Value <= 0f;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize health values on component awake.
        /// </summary>
        private void Awake()
        {
            CurrentHealth.Value = maxHealth;
            IsAlive.Value = true;
        }

        /// <summary>
        /// Cleanup event subscriptions on destroy.
        /// </summary>
        private void OnDestroy()
        {
            CurrentHealth.OnValueChanged -= OnHealthChanged;
            IsAlive.OnValueChanged -= OnLifeStateChanged;
            RespawnTimeRemaining.OnValueChanged -= OnRespawnTimeChanged;
        }

        #endregion

        #region Network Override Methods

        /// <summary>
        /// Set up network variable change listeners when spawned.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            CurrentHealth.OnValueChanged += OnHealthChanged;
            IsAlive.OnValueChanged += OnLifeStateChanged;
            RespawnTimeRemaining.OnValueChanged += OnRespawnTimeChanged;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Applies damage to this player. Server authority only.
        /// Handles death logic when health reaches zero.
        /// </summary>
        /// <param name="amount">Amount of damage to apply.</param>
        /// <param name="attackerClientId">Client ID of the attacker.</param>
        /// <param name="hitDirection">Direction vector of the damage impact.</param>
        public void ApplyDamageServer(float amount, ulong attackerClientId, Vector3 hitDirection)
        {
            if (!IsServer) return;

            int oldHealth = CurrentHealth.Value;
            int newHealth = Mathf.Max(0, oldHealth - Mathf.RoundToInt(amount));
            CurrentHealth.Value = newHealth;

            // Publish damage event for UI and effects systems
            MessageBus.Publish(new DamageEvent(OwnerClientId, attackerClientId, amount, hitDirection));

            if (newHealth <= 0 && IsAlive.Value)
            {
                HandleDeathServer(attackerClientId);
            }
        }

        /// <summary>
        /// Heals the player by the specified amount. Server authority only.
        /// </summary>
        /// <param name="amount">Amount of health to restore.</param>
        public void HealServer(int amount)
        {
            if (!IsServer || amount <= 0) return;

            CurrentHealth.Value = Mathf.Min(maxHealth, CurrentHealth.Value + amount);
        }

        /// <summary>
        /// Resets player to full health. Server authority only.
        /// </summary>
        public void ResetHealthServer()
        {
            if (!IsServer) return;

            CurrentHealth.Value = maxHealth;
            IsAlive.Value = true;
            RespawnTimeRemaining.Value = 0f;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles player death logic, disables components, and starts respawn countdown.
        /// </summary>
        /// <param name="killerClientId">Client ID of the player who caused the death.</param>
        private void HandleDeathServer(ulong killerClientId)
        {
            if (!IsServer) return;

            IsAlive.Value = false;
            RespawnTimeRemaining.Value = respawnDelay;

            // Disable gameplay components
            foreach (var component in componentsToDisable)
            {
                if (component != null) component.enabled = false;
            }

            foreach (var gameObject in objectsToDisable)
            {
                if (gameObject != null) gameObject.SetActive(false);
            }

            // Trigger death animation on all clients
            PlayDeathAnimationClientRpc();

            // Publish death event for game systems
            MessageBus.Publish(new DeathEvent(OwnerClientId, killerClientId));

            // Start respawn countdown
            if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
            _respawnCoroutine = StartCoroutine(RespawnCountdown());
        }

        /// <summary>
        /// Coroutine that handles respawn countdown and automatic respawn.
        /// </summary>
        /// <returns>Coroutine enumerator.</returns>
        private IEnumerator RespawnCountdown()
        {
            float remaining = respawnDelay;
            RespawnTimeRemaining.Value = remaining;

            while (remaining > 0f)
            {
                yield return new WaitForSeconds(1f);
                remaining -= 1f;
                RespawnTimeRemaining.Value = Mathf.Max(0f, remaining);
            }

            // Perform respawn when countdown reaches zero
            RespawnServer();
        }

        /// <summary>
        /// Respawns the player at a designated spawn point.
        /// </summary>
        private void RespawnServer()
        {
            if (!IsServer) return;

            // Restore health and life state
            CurrentHealth.Value = maxHealth;
            IsAlive.Value = true;
            RespawnTimeRemaining.Value = 0f;

            // Re-enable gameplay components
            foreach (var component in componentsToDisable)
            {
                if (component != null) component.enabled = true;
            }

            foreach (var gameObject in objectsToDisable)
            {
                if (gameObject != null) gameObject.SetActive(true);
            }

            // Determine respawn position
            Vector3 spawnPos = GetRespawnPosition();
            transform.position = spawnPos;
            transform.rotation = Quaternion.identity;

            // Trigger respawn effects on all clients
            PlayRespawnClientRpc(spawnPos);
        }

        /// <summary>
        /// Gets an appropriate respawn position for the player.
        /// </summary>
        /// <returns>World position for respawn.</returns>
        private Vector3 GetRespawnPosition()
        {
            // Try to get position from respawn manager
            if (RespawnManager.Instance != null)
            {
                Transform spawnPoint = RespawnManager.Instance.GetRandomSpawnPoint();
                if (spawnPoint != null)
                {
                    return spawnPoint.position;
                }
            }

            // Fall back to designated respawn point or origin
            return respawnPointFallback != null ? respawnPointFallback.position : Vector3.zero;
        }

        /// <summary>
        /// Called when health value changes on any client.
        /// </summary>
        /// <param name="oldValue">Previous health value.</param>
        /// <param name="newValue">New health value.</param>
        private void OnHealthChanged(int oldValue, int newValue)
        {
            Debug.Log($"[{gameObject.name}] Health changed: {oldValue} -> {newValue}");
            // TODO: Integrate with message bus for UI updates
        }

        /// <summary>
        /// Called when life state changes on any client.
        /// </summary>
        /// <param name="oldValue">Previous life state.</param>
        /// <param name="newValue">New life state.</param>
        private void OnLifeStateChanged(bool oldValue, bool newValue)
        {
            Debug.Log($"[{gameObject.name}] Life state changed: {(oldValue ? "Alive" : "Dead")} -> {(newValue ? "Alive" : "Dead")}");
            // TODO: Update death/respawn UI elements
        }

        /// <summary>
        /// Called when respawn timer changes on any client.
        /// </summary>
        /// <param name="oldValue">Previous respawn time.</param>
        /// <param name="newValue">New respawn time.</param>
        private void OnRespawnTimeChanged(float oldValue, float newValue)
        {
            // TODO: Update respawn countdown UI
        }

        #endregion

        #region Client RPC Methods

        /// <summary>
        /// Triggers death animation on all clients.
        /// </summary>
        /// <param name="clientRpcParams">Client RPC parameters.</param>
        [ClientRpc]
        private void PlayDeathAnimationClientRpc(ClientRpcParams clientRpcParams = default)
        {
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
            // TODO: Add camera effects, screen overlay, etc.
        }

        /// <summary>
        /// Triggers respawn effects on all clients.
        /// </summary>
        /// <param name="spawnPosition">World position where respawn occurred.</param>
        /// <param name="clientRpcParams">Client RPC parameters.</param>
        [ClientRpc]
        private void PlayRespawnClientRpc(Vector3 spawnPosition, ClientRpcParams clientRpcParams = default)
        {
            // Ensure transform is updated on clients
            transform.position = spawnPosition;

            if (animator != null)
            {
                animator.SetTrigger("Respawn");
            }

            // TODO: Add respawn particle effects, sound effects, etc.
        }

        #endregion
    }
}
