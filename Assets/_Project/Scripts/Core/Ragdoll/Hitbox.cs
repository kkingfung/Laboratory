using UnityEngine;

namespace Laboratory.Core.Ragdoll
{
    /// <summary>
    /// Component for detecting and processing hits on ragdoll bones.
    /// Integrates with HitReactionManager and NetworkRagdollSync for local and networked gameplay.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hitbox : MonoBehaviour
    {
        #region Fields
        
        [Header("Hitbox Configuration")]
        [Tooltip("Identifier for the bone or hit region. Must match RagdollSetup bone names.")]
        [SerializeField] private string boneName;
        
        [Tooltip("Multiplier applied to incoming impact forces.")]
        [SerializeField] private float forceMultiplier = 1.0f;
        
        [Header("Component References")]
        [Tooltip("Local hit reaction manager for single-player scenarios.")]
        [SerializeField] private HitReactionManager hitReactionManager;
        
        [Tooltip("Network ragdoll synchronization component for multiplayer scenarios.")]
        [SerializeField] private NetworkRagdollSync networkRagdollSync;
        
        #endregion
        
        #region Unity Override Methods
        
        /// <summary>
        /// Configures the collider component as a trigger for hit detection.
        /// </summary>
        private void Awake()
        {
            ConfigureCollider();
        }
        
        /// <summary>
        /// Handles automatic hit detection when objects enter the hitbox trigger.
        /// </summary>
        /// <param name="other">The collider that entered the trigger</param>
        private void OnTriggerEnter(Collider other)
        {
            ProcessAutomaticHit(other);
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Processes a hit from an external source with specified parameters.
        /// Calculates hit direction and applies force through available reaction systems.
        /// </summary>
        /// <param name="hitSource">World position of the hit origin</param>
        /// <param name="impactStrength">Base force magnitude of the impact</param>
        public void ReceiveHit(Vector3 hitSource, float impactStrength)
        {
            Vector3 hitDirection = CalculateHitDirection(hitSource);
            Vector3 hitForce = hitDirection * impactStrength * forceMultiplier;
            
            ProcessHitReaction(hitForce);
        }
        
        #endregion
        
        #region Private Methods
        
        /// <summary>
        /// Configures the attached collider as a trigger for hit detection.
        /// </summary>
        private void ConfigureCollider()
        {
            Collider collider = GetComponent<Collider>();
            collider.isTrigger = true;
        }
        
        /// <summary>
        /// Calculates the normalized hit direction from source to this hitbox.
        /// </summary>
        /// <param name="hitSource">World position of the hit origin</param>
        /// <returns>Normalized direction vector</returns>
        private Vector3 CalculateHitDirection(Vector3 hitSource)
        {
            return (transform.position - hitSource).normalized;
        }
        
        /// <summary>
        /// Routes hit force to appropriate reaction systems (local and/or networked).
        /// </summary>
        /// <param name="hitForce">Calculated hit force vector</param>
        private void ProcessHitReaction(Vector3 hitForce)
        {
            // Process local single-player hit reaction
            if (hitReactionManager != null)
            {
                hitReactionManager.OnHit(boneName, hitForce);
            }
            
            // Process networked multiplayer hit reaction
            if (networkRagdollSync != null)
            {
                networkRagdollSync.NetworkedHit(boneName, hitForce);
            }
        }
        
        /// <summary>
        /// Processes automatic hit detection for objects with specific tags.
        /// </summary>
        /// <param name="other">The collider that triggered the hit</param>
        private void ProcessAutomaticHit(Collider other)
        {
            if (IsWeaponOrProjectile(other))
            {
                Vector3 impactPoint = other.ClosestPoint(transform.position);
                ReceiveHit(impactPoint, 5f); // Default impact strength
            }
        }
        
        /// <summary>
        /// Determines if the collider represents a weapon or projectile.
        /// </summary>
        /// <param name="other">Collider to check</param>
        /// <returns>True if the collider should cause damage</returns>
        private bool IsWeaponOrProjectile(Collider other)
        {
            return other.CompareTag("Weapon") || other.CompareTag("Projectile");
        }
        
        #endregion
    }
}
