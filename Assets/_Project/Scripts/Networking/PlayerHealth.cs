// PlayerHealth.cs
using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 100;

    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // life state: server writes, everyone reads
    public NetworkVariable<bool> IsAlive = new NetworkVariable<bool>(
        true,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // Remaining time until respawn (server authoritative, clients read for UI)
    public NetworkVariable<float> RespawnTimeRemaining = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;
    [SerializeField] private Transform respawnPointFallback;

    // Optional: references to components to disable on death (movement, input, collider, renderer, etc.)
    [Header("Disable On Death")]
    [SerializeField] private MonoBehaviour[] componentsToDisable = new MonoBehaviour[0];
    [SerializeField] private GameObject[] objectsToDisable = new GameObject[0];

    [Header("Animation")]
    [SerializeField] private Animator animator; // assign if you have
    private Coroutine _respawnCoroutine;

    private void Awake()
    {
        CurrentHealth.Value = maxHealth;
        IsAlive.Value = true;
    }

    public override void OnNetworkSpawn()
    {
        // Clients can react to variable changes if they want to update UI:
        CurrentHealth.OnValueChanged += OnHealthChanged;
        IsAlive.OnValueChanged += OnLifeStateChanged;
        RespawnTimeRemaining.OnValueChanged += OnRespawnTimeChanged;
    }

    private void OnDestroy()
    {
        CurrentHealth.OnValueChanged -= OnHealthChanged;
        IsAlive.OnValueChanged -= OnLifeStateChanged;
        RespawnTimeRemaining.OnValueChanged -= OnRespawnTimeChanged;
    }

    private void OnHealthChanged(int oldVal, int newVal)
    {
        // client-side hooks (HUD) — UI should subscribe to MessageBus or this callback via a bridge
        // Not required here: MessageBus events are published server-side in ApplyDamageServer.
    }

    private void OnLifeStateChanged(bool oldVal, bool newVal)
    {
        // Show/hide death UI, etc., client-side
    }

    private void OnRespawnTimeChanged(float oldVal, float newVal)
    {
        // Update client respawn timer UI if needed
    }

    // Server-only: apply damage and handle death if needed
    public void ApplyDamageServer(float amount, ulong attackerClientId, Vector3 hitDirection)
    {
        if (!IsServer) return;

        int oldHealth = CurrentHealth.Value;
        int newHealth = Mathf.Max(0, oldHealth - Mathf.RoundToInt(amount));
        CurrentHealth.Value = newHealth;

        // Publish damage event for UIs & local effects
        MessageBus.Publish(new DamageEvent(OwnerClientId, attackerClientId, amount, hitDirection));

        if (newHealth <= 0 && IsAlive.Value)
        {
            HandleDeathServer(attackerClientId);
        }
    }

    private void HandleDeathServer(ulong killerClientId)
    {
        if (!IsServer) return;

        IsAlive.Value = false;
        RespawnTimeRemaining.Value = respawnDelay;

        // disable gameplay components
        foreach (var c in componentsToDisable)
            if (c != null) c.enabled = false;
        foreach (var go in objectsToDisable)
            if (go != null) go.SetActive(false);

        // Trigger death animation on all clients
        PlayDeathAnimationClientRpc();

        // Publish death event
        MessageBus.Publish(new DeathEvent(OwnerClientId, killerClientId));

        // Start respawn countdown on server
        if (_respawnCoroutine != null) StopCoroutine(_respawnCoroutine);
        _respawnCoroutine = StartCoroutine(RespawnCountdown());
    }

    private IEnumerator RespawnCountdown()
    {
        float remaining = respawnDelay;
        RespawnTimeRemaining.Value = remaining;

        while (remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
            RespawnTimeRemaining.Value = remaining;
        }

        // Perform respawn
        RespawnServer();
    }

    private void RespawnServer()
    {
        if (!IsServer) return;

        // Restore health and life state
        CurrentHealth.Value = maxHealth;
        IsAlive.Value = true;
        RespawnTimeRemaining.Value = 0f;

        // Re-enable gameplay components
        foreach (var c in componentsToDisable)
            if (c != null) c.enabled = true;
        foreach (var go in objectsToDisable)
            if (go != null) go.SetActive(true);

        // Move to respawn point
        Vector3 spawnPos = RespawnManager.Instance?.GetRespawnPosition(OwnerClientId) ?? (respawnPointFallback ? respawnPointFallback.position : Vector3.zero);
        transform.position = spawnPos;
        transform.rotation = Quaternion.identity;

        // Trigger respawn effect on clients
        PlayRespawnClientRpc(spawnPos);
    }

    [ClientRpc]
    private void PlayDeathAnimationClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        // optionally play local camera effects on client
    }

    [ClientRpc]
    private void PlayRespawnClientRpc(Vector3 spawnPosition, ClientRpcParams clientRpcParams = default)
    {
        // client-side respawn visual: teleport or play animation
        transform.position = spawnPosition;
        if (animator != null)
            animator.SetTrigger("Respawn");
    }
}
