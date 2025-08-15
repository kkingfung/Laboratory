using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Game.Character
{
    /// <summary>
    /// Unified controller for character look-at behavior.
    /// Handles target selection, head/chest rigging, and Animator IK fallback.
    /// </summary>
    public class CharacterLookController : MonoBehaviour
    {
        [Header("Targeting Settings")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private bool useProximity = false;
        [SerializeField] private float maxDistance = 10f;
        [SerializeField] private float proximityRadius = 3f;
        [SerializeField] private LayerMask targetLayers;

        [Header("Head Rigging")]
        [SerializeField] private MultiAimConstraint headConstraint;
        [SerializeField] private float headAimSpeed = 5f;
        [SerializeField] private float headMaxAngle = 80f;

        [Header("Chest Rigging (Optional)")]
        [SerializeField] private MultiAimConstraint chestConstraint;
        [SerializeField] private bool useChestRotation = true;
        [SerializeField] private float chestAimSpeed = 3f;
        [SerializeField] private float chestWeightMax = 0.3f;

        [Header("Animator IK Fallback")]
        [SerializeField] private Animator animator;
        [SerializeField, Range(0, 1f)] private float ikLookWeight = 0.8f;
        [SerializeField] private bool useIKFallback = true;

        // runtime
        private Transform currentTarget;
        private WeightedTransformArray headSources;
        private WeightedTransformArray chestSources;
        private float headWeight = 0f;
        private float chestWeight = 0f;

        void Awake()
        {
            if (headConstraint != null)
                headSources = headConstraint.data.sourceObjects;
            if (chestConstraint != null)
                chestSources = chestConstraint.data.sourceObjects;
        }

        void Update()
        {
            SelectTarget();
            UpdateRigging();
        }

        void OnAnimatorIK(int layerIndex)
        {
            if (!useIKFallback || animator == null) return;

            if (currentTarget != null && (headConstraint == null || headConstraint.weight <= 0.01f))
            {
                animator.SetLookAtWeight(ikLookWeight);
                animator.SetLookAtPosition(currentTarget.position);
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }

        // -------------------------------
        // Target Selection
        // -------------------------------
        private void SelectTarget()
        {
            if (useProximity)
                FindProximityTarget();
            else
                FindRaycastTarget();
        }

        private void FindRaycastTarget()
        {
            currentTarget = null;
            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit hit, maxDistance, targetLayers))
            {
                currentTarget = hit.collider.transform;
            }
        }

        private void FindProximityTarget()
        {
            currentTarget = null;
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
                        currentTarget = col.transform;
                    }
                }
            }
        }

        // -------------------------------
        // Rigging Update
        // -------------------------------
        private void UpdateRigging()
        {
            if (currentTarget != null)
            {
                Vector3 dirToTarget = currentTarget.position - transform.position;
                float angle = Vector3.Angle(transform.forward, dirToTarget);

                if (headConstraint != null)
                {
                    if (angle < headMaxAngle)
                    {
                        SetHeadTarget(currentTarget);
                        headWeight = Mathf.Lerp(headWeight, 1f, Time.deltaTime * headAimSpeed);
                    }
                    else
                    {
                        headWeight = Mathf.Lerp(headWeight, 0f, Time.deltaTime * headAimSpeed);
                    }
                    headConstraint.weight = headWeight;
                }

                if (useChestRotation && chestConstraint != null)
                {
                    SetChestTarget(currentTarget);
                    chestWeight = Mathf.Lerp(chestWeight, chestWeightMax, Time.deltaTime * chestAimSpeed);
                    chestConstraint.weight = chestWeight;
                }
            }
            else
            {
                if (headConstraint != null)
                    headWeight = Mathf.Lerp(headWeight, 0f, Time.deltaTime * headAimSpeed);
                if (chestConstraint != null)
                    chestWeight = Mathf.Lerp(chestWeight, 0f, Time.deltaTime * chestAimSpeed);

                if (headConstraint != null) headConstraint.weight = headWeight;
                if (chestConstraint != null) chestConstraint.weight = chestWeight;
            }
        }

        private void SetHeadTarget(Transform target)
        {
            headSources.SetTransform(0, target);
            headConstraint.data.sourceObjects = headSources;
        }

        private void SetChestTarget(Transform target)
        {
            chestSources.SetTransform(0, target);
            chestConstraint.data.sourceObjects = chestSources;
        }

        // Debug helper
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
