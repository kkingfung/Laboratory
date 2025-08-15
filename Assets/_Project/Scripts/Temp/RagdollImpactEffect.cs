using UnityEngine;

/// <summary>
/// Spawns visual or audio effects when a ragdoll bone is impacted.
/// Can be triggered via PartialRagdollController, Hitbox, or NetworkRagdollSync.
/// </summary>
public class RagdollImpactEffect : MonoBehaviour
{
    [Header("Impact Effects")]
    [Tooltip("Prefab to spawn on impact (particle system, decal, etc.).")]
    [SerializeField] private GameObject impactPrefab;

    [Tooltip("Optional sound to play on impact.")]
    [SerializeField] private AudioClip impactSound;

    [Tooltip("Volume of the impact sound.")]
    [SerializeField] [Range(0f, 1f)] private float soundVolume = 1f;

    [Tooltip("Assign AudioSource for sound playback.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [Tooltip("Scale multiplier for spawned impact prefab.")]
    [SerializeField] private float impactScale = 1f;

    /// <summary>
    /// Call this method to trigger impact effect at the hit position.
    /// </summary>
    /// <param name="hitPosition">World position of the hit.</param>
    /// <param name="hitNormal">Surface normal at impact (optional, for rotation).</param>
    public void TriggerImpact(Vector3 hitPosition, Vector3 hitNormal = default)
    {
        // Spawn impact prefab
        if (impactPrefab != null)
        {
            Quaternion rotation = hitNormal != Vector3.zero
                ? Quaternion.LookRotation(hitNormal)
                : Quaternion.identity;

            GameObject impactInstance = Instantiate(impactPrefab, hitPosition, rotation);
            impactInstance.transform.localScale *= impactScale;

            // Optional: destroy after lifetime if prefab doesn't auto-destroy
            ParticleSystem ps = impactInstance.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(impactInstance, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(impactInstance, 2f); // fallback
            }
        }

        // Play sound
        if (impactSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(impactSound, soundVolume);
        }
    }
}
