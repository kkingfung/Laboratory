using UnityEngine;

namespace Game.Character
{
    /// <summary>
    /// Selects a target for the character to look at.
    /// Can be raycast-based (camera forward) or trigger-based.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        [Header("Raycast Targeting")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Proximity Targeting")]
        [SerializeField] private bool useProximity = false;
        [SerializeField] private float proximityRadius = 3f;

        public Transform CurrentTarget { get; private set; }

        void Update()
        {
            if (useProximity)
                FindProximityTarget();
            else
                FindRaycastTarget();
        }

        void FindRaycastTarget()
        {
            CurrentTarget = null;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit hit, maxDistance, targetLayers))
            {
                CurrentTarget = hit.collider.transform;
            }
        }

        void FindProximityTarget()
        {
            CurrentTarget = null;
            Collider[] hits = Physics.OverlapSphere(transform.position, proximityRadius, targetLayers);
            if (hits.Length > 0)
            {
                float closestDist = float.MaxValue;
                foreach (var col in hits)
                {
                    float dist = Vector3.Distance(transform.position, col.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        CurrentTarget = col.transform;
                    }
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            if (useProximity)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, proximityRadius);
            }
        }
    }
}
