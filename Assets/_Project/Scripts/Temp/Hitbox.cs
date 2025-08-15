using UnityEngine;

/// <summary>
/// Simple hitbox component for detecting hits.
/// Works with HitReactionManager or NetworkRagdollSync.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Header("Hitbox Settings")]
    [Tooltip("Name of the bone or hit region. Must match RagdollSetup bone names.")]
    [SerializeField] private string boneName;

    [Tooltip("Multiplier for impact force.")]
    [SerializeField] private float forceMultiplier = 1.0f;

    [Tooltip("Assign HitReactionManager manually.")]
    [SerializeField] private HitReactionManager hitReactionManager;

    [Tooltip("Assign NetworkRagdollSync manually if using multiplayer.")]
    [SerializeField] private NetworkRagdollSync networkRagdollSync;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true; // Hitboxes should generally be triggers
    }

    /// <summary>
    /// Call this from projectiles, melee attacks, or other hit detection.
    /// </summary>
    /// <param name="hitSource">Origin point of hit (for direction).</param>
    /// <param name="impactStrength">Base force of the hit.</param>
    public void ReceiveHit(Vector3 hitSource, float impactStrength)
    {
        Vector3 hitDirection = (transform.position - hitSource).normalized;
        Vector3 hitForce = hitDirection * impactStrength * forceMultiplier;

        // Local single-player / non-networked
        if (hitReactionManager != null)
        {
            hitReactionManager.OnHit(boneName, hitForce);
        }

        // Networked multiplayer
        if (networkRagdollSync != null)
        {
            networkRagdollSync.NetworkedHit(boneName, hitForce);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Example: automatic detection if a projectile hits
        if (other.CompareTag("Weapon") || other.CompareTag("Projectile"))
        {
            Vector3 impactPoint = other.ClosestPoint(transform.position);
            ReceiveHit(impactPoint, 5f); // Default impact strength
        }
    }
}
