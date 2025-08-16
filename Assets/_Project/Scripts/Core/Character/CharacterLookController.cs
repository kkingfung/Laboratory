using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Unified controller for character look-at behavior.
    /// Handles target selection, head/chest rigging, and Animator IK fallback.
    /// Provides smooth transitions between targets and supports both proximity and raycast targeting.
    /// </summary>
    public class CharacterLookController : MonoBehaviour
    {
        #region Fields

        [Header("Targeting Settings")]
        [SerializeField] 
        [Tooltip("Camera used for raycast targeting when not using proximity mode")]
        private Camera playerCamera;
        
        [SerializeField] 
        [Tooltip("Use proximity-based targeting instead of camera raycast")]
        private bool useProximity = false;
        
        [SerializeField] 
        [Tooltip("Maximum distance for raycast targeting")]
        private float maxDistance = 10f;
        
        [SerializeField] 
        [Tooltip("Radius for proximity-based target detection")]
        private float proximityRadius = 3f;
        
        [SerializeField] 
        [Tooltip("Layer mask for valid look-at targets")]
        private LayerMask targetLayers;

        [Header("Head Rigging")]
        [SerializeField] 
        [Tooltip("Multi-aim constraint for head bone targeting")]
        private MultiAimConstraint headConstraint;
        
        [SerializeField] 
        [Tooltip("Speed of head rotation transitions")]
        private float headAimSpeed = 5f;
        
        [SerializeField] 
        [Tooltip("Maximum angle the head can turn from forward direction")]
        private float headMaxAngle = 80f;

        [Header("Chest Rigging (Optional)")]
        [SerializeField] 
        [Tooltip("Multi-aim constraint for chest bone targeting")]
        private MultiAimConstraint chestConstraint;
        
        [SerializeField] 
        [Tooltip("Enable chest rotation for more natural body movement")]
        private bool useChestRotation = true;
        
        [SerializeField] 
        [Tooltip("Speed of chest rotation transitions")]
        private float chestAimSpeed = 3f;
        
        [SerializeField] 
        [Tooltip("Maximum weight for chest constraint")]
        private float chestWeightMax = 0.3f;

        [Header("Animator IK Fallback")]
        [SerializeField] 
        [Tooltip("Animator component for IK fallback when rigging is unavailable")]
        private Animator animator;
        
        [SerializeField] 
        [Range(0, 1f)]
        [Tooltip("Weight of IK look-at when using fallback")]
        private float ikLookWeight = 0.8f;
        
        [SerializeField] 
        [Tooltip("Use Animator IK as fallback when rigging constraints are disabled")]
        private bool useIKFallback = true;

        // Runtime state
        private Transform currentTarget;
        private WeightedTransformArray headSources;
        private WeightedTransformArray chestSources;
        private float headWeight = 0f;
        private float chestWeight = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// Currently targeted transform for look-at behavior
        /// </summary>
        public Transform CurrentTarget => currentTarget;

        /// <summary>
        /// Current weight of head constraint (0-1)
        /// </summary>
        public float HeadWeight => headWeight;

        /// <summary>
        /// Current weight of chest constraint (0-1)
        /// </summary>
        public float ChestWeight => chestWeight;

        #endregion

        #region Unity Override Methods

        /// <summary>
        /// Initialize constraint source arrays during Awake
        /// </summary>
        private void Awake()
        {
            CacheConstraintSources();
        }

        /// <summary>
        /// Update targeting and rigging each frame
        /// </summary>
        private void Update()
        {
            SelectTarget();
            UpdateRigging();
        }

        /// <summary>
        /// Handle Animator IK as fallback when rigging constraints are unavailable
        /// </summary>
        /// <param name="layerIndex">Animation layer index</param>
        private void OnAnimatorIK(int layerIndex)
        {
            if (!useIKFallback || animator == null) 
                return;

            if (currentTarget != null && ShouldUseIKFallback())
            {
                animator.SetLookAtWeight(ikLookWeight);
                animator.SetLookAtPosition(currentTarget.position);
            }
            else
            {
                animator.SetLookAtWeight(0f);
            }
        }

        /// <summary>
        /// Draw debug gizmos for proximity radius
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (useProximity)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, proximityRadius);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Manually set the look-at target
        /// </summary>
        /// <param name="target">Transform to look at</param>
        public void SetTarget(Transform target)
        {
            currentTarget = target;
        }

        /// <summary>
        /// Clear the current target
        /// </summary>
        public void ClearTarget()
        {
            currentTarget = null;
        }

        /// <summary>
        /// Enable or disable look-at behavior entirely
        /// </summary>
        /// <param name="enabled">Whether look-at should be active</param>
        public void SetLookAtEnabled(bool enabled)
        {
            if (!enabled)
            {
                currentTarget = null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Cache constraint source arrays for runtime use
        /// </summary>
        private void CacheConstraintSources()
        {
            if (headConstraint != null)
                headSources = headConstraint.data.sourceObjects;
            if (chestConstraint != null)
                chestSources = chestConstraint.data.sourceObjects;
        }

        /// <summary>
        /// Determine if IK fallback should be used instead of rigging
        /// </summary>
        /// <returns>True if IK should be used as fallback</returns>
        private bool ShouldUseIKFallback()
        {
            return headConstraint == null || headConstraint.weight <= 0.01f;
        }

        /// <summary>
        /// Select appropriate target based on current targeting mode
        /// </summary>
        private void SelectTarget()
        {
            if (useProximity)
                FindProximityTarget();
            else
                FindRaycastTarget();
        }

        /// <summary>
        /// Find target using camera raycast
        /// </summary>
        private void FindRaycastTarget()
        {
            currentTarget = null;
            
            if (playerCamera == null) 
                return;

            if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward,
                out RaycastHit hit, maxDistance, targetLayers))
            {
                currentTarget = hit.collider.transform;
            }
        }

        /// <summary>
        /// Find closest target within proximity radius
        /// </summary>
        private void FindProximityTarget()
        {
            currentTarget = null;
            Collider[] hits = Physics.OverlapSphere(transform.position, proximityRadius, targetLayers);
            
            if (hits.Length == 0) 
                return;

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

        /// <summary>
        /// Update rigging constraints based on current target
        /// </summary>
        private void UpdateRigging()
        {
            if (currentTarget != null)
            {
                UpdateConstraintsWithTarget();
            }
            else
            {
                UpdateConstraintsWithoutTarget();
            }
        }

        /// <summary>
        /// Update constraints when a target is available
        /// </summary>
        private void UpdateConstraintsWithTarget()
        {
            Vector3 dirToTarget = currentTarget.position - transform.position;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            // Update head constraint
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

            // Update chest constraint
            if (useChestRotation && chestConstraint != null)
            {
                SetChestTarget(currentTarget);
                chestWeight = Mathf.Lerp(chestWeight, chestWeightMax, Time.deltaTime * chestAimSpeed);
                chestConstraint.weight = chestWeight;
            }
        }

        /// <summary>
        /// Update constraints when no target is available
        /// </summary>
        private void UpdateConstraintsWithoutTarget()
        {
            if (headConstraint != null)
            {
                headWeight = Mathf.Lerp(headWeight, 0f, Time.deltaTime * headAimSpeed);
                headConstraint.weight = headWeight;
            }

            if (chestConstraint != null)
            {
                chestWeight = Mathf.Lerp(chestWeight, 0f, Time.deltaTime * chestAimSpeed);
                chestConstraint.weight = chestWeight;
            }
        }

        /// <summary>
        /// Set target for head constraint
        /// </summary>
        /// <param name="target">Target transform</param>
        private void SetHeadTarget(Transform target)
        {
            if (headSources.Count > 0)
            {
                headSources.SetTransform(0, target);
                headConstraint.data.sourceObjects = headSources;
            }
        }

        /// <summary>
        /// Set target for chest constraint
        /// </summary>
        /// <param name="target">Target transform</param>
        private void SetChestTarget(Transform target)
        {
            if (chestSources.Count > 0)
            {
                chestSources.SetTransform(0, target);
                chestConstraint.data.sourceObjects = chestSources;
            }
        }

        #endregion
    }
}
