using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Controls head aiming using Animation Rigging constraints.
    /// Provides smooth head rotation toward targets with configurable limits and weights.
    /// </summary>
    public class HeadAimController : MonoBehaviour
    {
        #region Fields

        [Header("Rigging Constraints")]
        [SerializeField] private MultiAimConstraint _headConstraint;
        [SerializeField] private MultiAimConstraint _neckConstraint;

        [Header("Aiming Settings")]
        [SerializeField] private float _aimWeight = 1f;
        [SerializeField] private float _headWeight = 0.8f;
        [SerializeField] private float _neckWeight = 0.4f;

        [Header("Rotation Limits")]
        [SerializeField] private float _maxHeadAngle = 80f;
        [SerializeField] private float _maxNeckAngle = 45f;
        [SerializeField] private bool _clampRotation = true;

        [Header("Target Settings")]
        [SerializeField] private Transform _aimTarget;
        [SerializeField] private float _maxAimDistance = 20f;
        [SerializeField] private LayerMask _targetLayers = -1;

        [Header("Animation Blending")]
        [SerializeField] private float _blendSpeed = 8f;
        [SerializeField] private bool _smoothBlending = true;
        [SerializeField] private AnimationCurve _weightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Runtime state
        private bool _isAiming = false;
        private float _currentAimWeight = 0f;
        private Vector3 _lastTargetPosition;
        private WeightedTransformArray _headSources;
        private WeightedTransformArray _neckSources;

        #endregion

        #region Properties

        /// <summary>
        /// Current aim target transform
        /// </summary>
        public Transform AimTarget => _aimTarget;

        /// <summary>
        /// Whether head aiming is currently active
        /// </summary>
        public bool IsAiming => _isAiming;

        /// <summary>
        /// Current aim weight being applied
        /// </summary>
        public float CurrentAimWeight => _currentAimWeight;

        /// <summary>
        /// Maximum head rotation angle
        /// </summary>
        public float MaxHeadAngle => _maxHeadAngle;

        /// <summary>
        /// Maximum neck rotation angle
        /// </summary>
        public float MaxNeckAngle => _maxNeckAngle;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            CacheConstraintSources();
            ValidateConstraints();
        }

        private void Start()
        {
            InitializeConstraints();
        }

        private void Update()
        {
            UpdateAiming();
            UpdateConstraintWeights();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the aim target transform
        /// </summary>
        /// <param name="target">Target transform to aim at</param>
        public void SetAimTarget(Transform target)
        {
            _aimTarget = target;
            if (target != null)
            {
                _lastTargetPosition = target.position;
                _isAiming = true;
            }
            else
            {
                _isAiming = false;
            }
        }

        /// <summary>
        /// Clears the current aim target
        /// </summary>
        public void ClearAimTarget()
        {
            _aimTarget = null;
            _isAiming = false;
        }

        /// <summary>
        /// Sets the overall aim weight
        /// </summary>
        /// <param name="weight">Aim weight (0-1)</param>
        public void SetAimWeight(float weight)
        {
            _aimWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the head constraint weight
        /// </summary>
        /// <param name="weight">Head weight (0-1)</param>
        public void SetHeadWeight(float weight)
        {
            _headWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the neck constraint weight
        /// </summary>
        /// <param name="weight">Neck weight (0-1)</param>
        public void SetNeckWeight(float weight)
        {
            _neckWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the maximum head rotation angle
        /// </summary>
        /// <param name="angle">Maximum angle in degrees</param>
        public void SetMaxHeadAngle(float angle)
        {
            _maxHeadAngle = Mathf.Clamp(angle, 0f, 180f);
        }

        /// <summary>
        /// Sets the maximum neck rotation angle
        /// </summary>
        /// <param name="angle">Maximum angle in degrees</param>
        public void SetMaxNeckAngle(float angle)
        {
            _maxNeckAngle = Mathf.Clamp(angle, 0f, 90f);
        }

        /// <summary>
        /// Enables or disables rotation clamping
        /// </summary>
        /// <param name="enabled">Whether rotation should be clamped</param>
        public void SetRotationClamping(bool enabled)
        {
            _clampRotation = enabled;
        }

        /// <summary>
        /// Sets the blend speed for weight transitions
        /// </summary>
        /// <param name="speed">Blend speed value</param>
        public void SetBlendSpeed(float speed)
        {
            _blendSpeed = Mathf.Max(0.1f, speed);
        }

        #endregion

        #region Private Methods

        private void CacheConstraintSources()
        {
            if (_headConstraint != null)
                _headSources = _headConstraint.data.sourceObjects;
            if (_neckConstraint != null)
                _neckSources = _neckConstraint.data.sourceObjects;
        }

        private void ValidateConstraints()
        {
            if (_headConstraint == null && _neckConstraint == null)
            {
                Debug.LogWarning("HeadAimController: No rigging constraints assigned");
            }
        }

        private void InitializeConstraints()
        {
            // Set initial weights to 0
            if (_headConstraint != null)
                _headConstraint.weight = 0f;
            if (_neckConstraint != null)
                _neckConstraint.weight = 0f;
        }

        private void UpdateAiming()
        {
            if (_aimTarget == null)
            {
                _isAiming = false;
                return;
            }

            // Check if target is still valid
            if (_aimTarget.position != _lastTargetPosition)
            {
                _lastTargetPosition = _aimTarget.position;
            }

            // Check distance to target
            float distance = Vector3.Distance(transform.position, _aimTarget.position);
            if (distance > _maxAimDistance)
            {
                ClearAimTarget();
                return;
            }

            // Check if target is within rotation limits
            if (_clampRotation && !IsTargetWithinRotationLimits())
            {
                ClearAimTarget();
                return;
            }

            _isAiming = true;
        }

        private bool IsTargetWithinRotationLimits()
        {
            if (_aimTarget == null) return false;

            Vector3 directionToTarget = (_aimTarget.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            return angle <= _maxHeadAngle;
        }

        private void UpdateConstraintWeights()
        {
            float targetWeight = _isAiming ? _aimWeight : 0f;

            if (_smoothBlending)
            {
                _currentAimWeight = Mathf.Lerp(_currentAimWeight, targetWeight, Time.deltaTime * _blendSpeed);
            }
            else
            {
                _currentAimWeight = targetWeight;
            }

            // Apply weights to constraints
            if (_headConstraint != null)
            {
                _headConstraint.weight = _currentAimWeight * _headWeight;
                UpdateConstraintTarget(_headConstraint, _headSources);
            }

            if (_neckConstraint != null)
            {
                _neckConstraint.weight = _currentAimWeight * _neckWeight;
                UpdateConstraintTarget(_neckConstraint, _neckSources);
            }
        }

        private void UpdateConstraintTarget(MultiAimConstraint constraint, WeightedTransformArray sources)
        {
            if (constraint == null || sources.Count == 0 || _aimTarget == null) return;

            // Update the first source object to point at the aim target
            sources.SetTransform(0, _aimTarget);
            constraint.data.sourceObjects = sources;
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_aimTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _aimTarget.position);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_aimTarget.position, 0.2f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _maxAimDistance);

            // Draw rotation limit cones
            if (_clampRotation)
            {
                Gizmos.color = Color.green;
                DrawRotationCone(transform.position, transform.forward, _maxHeadAngle, _maxAimDistance);
                
                Gizmos.color = Color.cyan;
                DrawRotationCone(transform.position, transform.forward, _maxNeckAngle, _maxAimDistance);
            }
        }

        private void DrawRotationCone(Vector3 position, Vector3 direction, float angle, float distance)
        {
            int segments = 16;
            Vector3[] points = new Vector3[segments + 1];
            
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float currentAngle = Mathf.Lerp(-angle, angle, t);
                Vector3 rotatedDirection = Quaternion.AngleAxis(currentAngle, Vector3.up) * direction;
                points[i] = position + rotatedDirection * distance;
            }
            
            for (int i = 0; i < segments; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }
        }

        #endregion
    }
}
