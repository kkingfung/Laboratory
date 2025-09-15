using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Provides Animator IK fallback for look-at behavior when rigging constraints are unavailable.
    /// Automatically switches between rigging and IK modes based on availability.
    /// </summary>
    public class LookAtIKFallback : MonoBehaviour
    {
        #region Fields

        [Header("IK Settings")]
        [SerializeField] private float _ikWeight = 1f;
        [SerializeField] private float _bodyWeight = 0.3f;
        [SerializeField] private float _headWeight = 0.8f;
        [SerializeField] private float _eyesWeight = 1f;
        [SerializeField] private float _clampWeight = 0.5f;

        [Header("Target Settings")]
        [SerializeField] private Transform _lookAtTarget;
        [SerializeField] private float _maxLookDistance = 10f;
        [SerializeField] private LayerMask _targetLayers = -1;

        [Header("Fallback Behavior")]
        [SerializeField] private bool _useRiggingWhenAvailable = true;
        [SerializeField] private bool _smoothTransition = true;
        [SerializeField] private float _transitionSpeed = 5f;

        // Runtime state
        private Animator _animator;
        private bool _isRiggingAvailable = false;
        private float _currentIKWeight = 0f;
        private Vector3 _lastTargetPosition;

        #endregion

        #region Properties

        /// <summary>
        /// Current look-at target transform
        /// </summary>
        public Transform LookAtTarget => _lookAtTarget;

        /// <summary>
        /// Whether rigging constraints are available
        /// </summary>
        public bool IsRiggingAvailable => _isRiggingAvailable;

        /// <summary>
        /// Current IK weight being applied
        /// </summary>
        public float CurrentIKWeight => _currentIKWeight;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null)
            {
                Debug.LogError("LookAtIKFallback requires an Animator component");
                enabled = false;
                return;
            }

            CheckRiggingAvailability();
        }

        private void Update()
        {
            UpdateTarget();
            UpdateIKWeight();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_lookAtTarget == null || _currentIKWeight <= 0f) return;

            ApplyLookAtIK();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the look-at target transform
        /// </summary>
        /// <param name="target">Target transform to look at</param>
        public void SetLookAtTarget(Transform target)
        {
            _lookAtTarget = target;
            if (target != null)
            {
                _lastTargetPosition = target.position;
            }
        }

        /// <summary>
        /// Clears the current look-at target
        /// </summary>
        public void ClearLookAtTarget()
        {
            _lookAtTarget = null;
            _currentIKWeight = 0f;
        }

        /// <summary>
        /// Sets the IK weight for look-at behavior
        /// </summary>
        /// <param name="weight">IK weight (0-1)</param>
        public void SetIKWeight(float weight)
        {
            _ikWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the body weight for look-at behavior
        /// </summary>
        /// <param name="weight">Body weight (0-1)</param>
        public void SetBodyWeight(float weight)
        {
            _bodyWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the head weight for look-at behavior
        /// </summary>
        /// <param name="weight">Head weight (0-1)</param>
        public void SetHeadWeight(float weight)
        {
            _headWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the eyes weight for look-at behavior
        /// </summary>
        /// <param name="weight">Eyes weight (0-1)</param>
        public void SetEyesWeight(float weight)
        {
            _eyesWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Forces the use of IK mode regardless of rigging availability
        /// </summary>
        public void ForceIKMode()
        {
            _useRiggingWhenAvailable = false;
            _currentIKWeight = _ikWeight;
        }

        /// <summary>
        /// Enables automatic mode switching between rigging and IK
        /// </summary>
        public void EnableAutoMode()
        {
            _useRiggingWhenAvailable = true;
            CheckRiggingAvailability();
        }

        #endregion

        #region Private Methods

        private void CheckRiggingAvailability()
        {
            // Check if rigging constraints are available
            var riggingConstraints = GetComponentsInChildren<UnityEngine.Animations.Rigging.MultiAimConstraint>();
            _isRiggingAvailable = riggingConstraints.Length > 0;
        }

        private void UpdateTarget()
        {
            if (_lookAtTarget == null) return;

            // Check if target is still valid
            if (_lookAtTarget.position != _lastTargetPosition)
            {
                _lastTargetPosition = _lookAtTarget.position;
            }

            // Check distance to target
            float distance = Vector3.Distance(transform.position, _lookAtTarget.position);
            if (distance > _maxLookDistance)
            {
                ClearLookAtTarget();
            }
        }

        private void UpdateIKWeight()
        {
            if (!_useRiggingWhenAvailable)
            {
                // Force IK mode
                _currentIKWeight = _ikWeight;
                return;
            }

            if (_isRiggingAvailable)
            {
                // Use rigging when available, reduce IK weight
                if (_smoothTransition)
                {
                    _currentIKWeight = Mathf.Lerp(_currentIKWeight, 0f, Time.deltaTime * _transitionSpeed);
                }
                else
                {
                    _currentIKWeight = 0f;
                }
            }
            else
            {
                // Use IK when rigging is unavailable
                if (_smoothTransition)
                {
                    _currentIKWeight = Mathf.Lerp(_currentIKWeight, _ikWeight, Time.deltaTime * _transitionSpeed);
                }
                else
                {
                    _currentIKWeight = _ikWeight;
                }
            }
        }

        private void ApplyLookAtIK()
        {
            if (_animator == null || _lookAtTarget == null) return;

            // Apply look-at IK with current weights
            _animator.SetLookAtWeight(_currentIKWeight, _bodyWeight, _headWeight, _eyesWeight, _clampWeight);
            _animator.SetLookAtPosition(_lookAtTarget.position);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_lookAtTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _lookAtTarget.position);
                
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_lookAtTarget.position, 0.1f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _maxLookDistance);
        }

        #endregion
    }
}
