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
        private Camera _playerCamera;
        
        [SerializeField] 
        [Tooltip("Use proximity-based targeting instead of camera raycast")]
        private bool _useProximity = false;
        
        [SerializeField] 
        [Tooltip("Maximum distance for raycast targeting")]
        private float _maxDistance = 10f;
        
        [SerializeField] 
        [Tooltip("Radius for proximity-based target detection")]
        private float _proximityRadius = 3f;
        
        [SerializeField] 
        [Tooltip("Layer mask for valid look-at targets")]
        private LayerMask _targetLayers;

        [Header("Head Rigging")]
        [SerializeField] 
        [Tooltip("Multi-aim constraint for head bone targeting")]
        private MultiAimConstraint _headConstraint;
        
        [SerializeField] 
        [Tooltip("Speed of head rotation transitions")]
        private float _headAimSpeed = 5f;
        
        [SerializeField] 
        [Tooltip("Maximum angle the head can turn from forward direction")]
        private float _headMaxAngle = 80f;

        [Header("Chest Rigging (Optional)")]
        [SerializeField] 
        [Tooltip("Multi-aim constraint for chest bone targeting")]
        private MultiAimConstraint _chestConstraint;
        
        [SerializeField] 
        [Tooltip("Enable chest rotation for more natural body movement")]
        private bool _useChestRotation = true;
        
        [SerializeField] 
        [Tooltip("Speed of chest rotation transitions")]
        private float _chestAimSpeed = 3f;
        
        [SerializeField] 
        [Tooltip("Maximum weight for chest constraint")]
        private float _chestWeightMax = 0.3f;

        [Header("Animator IK Fallback")]
        [SerializeField] 
        [Tooltip("Animator component for IK fallback when rigging is unavailable")]
        private Animator _animator;
        
        [SerializeField] 
        [Range(0, 1f)]
        [Tooltip("Weight of IK look-at when using fallback")]
        private float _ikLookWeight = 0.8f;
        
        [SerializeField] 
        [Tooltip("Use Animator IK as fallback when rigging constraints are disabled")]
        private bool _useIKFallback = true;

        // Runtime state
        private Transform _currentTarget;
        private WeightedTransformArray _headSources;
        private WeightedTransformArray _chestSources;
        private float _headWeight = 0f;
        private float _chestWeight = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// Currently targeted transform for look-at behavior
        /// </summary>
        public Transform CurrentTarget => _currentTarget;

        /// <summary>
        /// Current weight of head constraint (0-1)
        /// </summary>
        public float HeadWeight => _headWeight;

        /// <summary>
        /// Current weight of chest constraint (0-1)
        /// </summary>
        public float ChestWeight => _chestWeight;

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
            if (!_useIKFallback || _animator == null) 
                return;

            if (_currentTarget != null && ShouldUseIKFallback())
            {
                _animator.SetLookAtWeight(_ikLookWeight);
                _animator.SetLookAtPosition(_currentTarget.position);
            }
            else
            {
                _animator.SetLookAtWeight(0f);
            }
        }

        /// <summary>
        /// Draw debug gizmos for proximity radius
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (_useProximity)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, _proximityRadius);
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
            _currentTarget = target;
        }

        /// <summary>
        /// Clear the current target
        /// </summary>
        public void ClearTarget()
        {
            _currentTarget = null;
        }

        /// <summary>
        /// Enable or disable look-at behavior entirely
        /// </summary>
        /// <param name="enabled">Whether look-at should be active</param>
        public void SetLookAtEnabled(bool enabled)
        {
            if (!enabled)
            {
                _currentTarget = null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Cache constraint source arrays for runtime use
        /// </summary>
        private void CacheConstraintSources()
        {
            if (_headConstraint != null)
                _headSources = _headConstraint.data.sourceObjects;
            if (_chestConstraint != null)
                _chestSources = _chestConstraint.data.sourceObjects;
        }

        /// <summary>
        /// Determine if IK fallback should be used instead of rigging
        /// </summary>
        /// <returns>True if IK should be used as fallback</returns>
        private bool ShouldUseIKFallback()
        {
            return _headConstraint == null || _headConstraint.weight <= 0.01f;
        }

        /// <summary>
        /// Select appropriate target based on current targeting mode
        /// </summary>
        private void SelectTarget()
        {
            if (_useProximity)
                FindProximityTarget();
            else
                FindRaycastTarget();
        }

        /// <summary>
        /// Find target using camera raycast
        /// </summary>
        private void FindRaycastTarget()
        {
            _currentTarget = null;
            
            if (_playerCamera == null) 
                return;

            if (Physics.Raycast(_playerCamera.transform.position, _playerCamera.transform.forward,
                out RaycastHit hit, _maxDistance, _targetLayers))
            {
                _currentTarget = hit.collider.transform;
            }
        }

        /// <summary>
        /// Find closest target within proximity radius
        /// </summary>
        private void FindProximityTarget()
        {
            _currentTarget = null;
            Collider[] hits = Physics.OverlapSphere(transform.position, _proximityRadius, _targetLayers);
            
            if (hits.Length == 0) 
                return;

            float closestDist = float.MaxValue;
            foreach (var col in hits)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    _currentTarget = col.transform;
                }
            }
        }

        /// <summary>
        /// Update rigging constraints based on current target
        /// </summary>
        private void UpdateRigging()
        {
            if (_currentTarget != null)
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
            Vector3 dirToTarget = _currentTarget.position - transform.position;
            float angle = Vector3.Angle(transform.forward, dirToTarget);

            // Update head constraint
            if (_headConstraint != null)
            {
                if (angle < _headMaxAngle)
                {
                    SetHeadTarget(_currentTarget);
                    _headWeight = Mathf.Lerp(_headWeight, 1f, Time.deltaTime * _headAimSpeed);
                }
                else
                {
                    _headWeight = Mathf.Lerp(_headWeight, 0f, Time.deltaTime * _headAimSpeed);
                }
                _headConstraint.weight = _headWeight;
            }

            // Update chest constraint
            if (_useChestRotation && _chestConstraint != null)
            {
                SetChestTarget(_currentTarget);
                _chestWeight = Mathf.Lerp(_chestWeight, _chestWeightMax, Time.deltaTime * _chestAimSpeed);
                _chestConstraint.weight = _chestWeight;
            }
        }

        /// <summary>
        /// Update constraints when no target is available
        /// </summary>
        private void UpdateConstraintsWithoutTarget()
        {
            if (_headConstraint != null)
            {
                _headWeight = Mathf.Lerp(_headWeight, 0f, Time.deltaTime * _headAimSpeed);
                _headConstraint.weight = _headWeight;
            }

            if (_chestConstraint != null)
            {
                _chestWeight = Mathf.Lerp(_chestWeight, 0f, Time.deltaTime * _chestAimSpeed);
                _chestConstraint.weight = _chestWeight;
            }
        }

        /// <summary>
        /// Set target for head constraint
        /// </summary>
        /// <param name="target">Target transform</param>
        private void SetHeadTarget(Transform target)
        {
            if (_headSources.Count > 0)
            {
                _headSources.SetTransform(0, target);
                _headConstraint.data.sourceObjects = _headSources;
            }
        }

        /// <summary>
        /// Set target for chest constraint
        /// </summary>
        /// <param name="target">Target transform</param>
        private void SetChestTarget(Transform target)
        {
            if (_chestSources.Count > 0)
            {
                _chestSources.SetTransform(0, target);
                _chestConstraint.data.sourceObjects = _chestSources;
            }
        }

        #endregion
    }
}
