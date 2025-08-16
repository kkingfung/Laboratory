using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Selects targets for character look-at behavior.
    /// Supports both raycast-based and proximity-based target selection.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        #region Fields

        [Header("Raycast Targeting")]
        [Tooltip("Camera used for raycast targeting")]
        [SerializeField] private Camera playerCamera;
        
        [Tooltip("Maximum distance for raycast targeting")]
        [SerializeField] private float maxDistance = 10f;
        
        [Tooltip("Layer mask for targetable objects")]
        [SerializeField] private LayerMask targetLayers;

        [Header("Proximity Targeting")]
        [Tooltip("Use proximity-based targeting instead of raycast")]
        [SerializeField] private bool useProximity = false;
        
        [Tooltip("Radius for proximity-based targeting")]
        [SerializeField] private float proximityRadius = 3f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the currently selected target
        /// </summary>
        public Transform CurrentTarget { get; private set; }

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Updates target selection each frame
        /// </summary>
        void Update()
        {
            if (useProximity)
                FindProximityTarget();
            else
                FindRaycastTarget();
        }

        /// <summary>
        /// Draws debug gizmos in the scene view
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (useProximity)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, proximityRadius);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Finds target using raycast from camera forward
        /// </summary>
        void FindRaycastTarget()
        {
            CurrentTarget = null;
            
            if (playerCamera == null) return;

            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit hit, maxDistance, targetLayers))
            {
                CurrentTarget = hit.collider.transform;
            }
        }

        /// <summary>
        /// Finds the closest target within proximity radius
        /// </summary>
        void FindProximityTarget()
        {
            CurrentTarget = null;
            
            Collider[] hits = Physics.OverlapSphere(transform.position, proximityRadius, targetLayers);
            if (hits.Length > 0)
            {
                float closestDistance = float.MaxValue;
                foreach (var collider in hits)
                {
                    float distance = Vector3.Distance(transform.position, collider.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        CurrentTarget = collider.transform;
                    }
                }
            }
        }

        #endregion
    }
}
