using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Controls upper body aiming using Animation Rigging constraints.
    /// Provides smooth aiming behavior with configurable weights and limits.
    /// </summary>
    public class UpperBodyAimController : MonoBehaviour
    {
        #region Fields

        [Header("Rigging Constraints")]
        [SerializeField] private MultiAimConstraint _spineConstraint;
        [SerializeField] private MultiAimConstraint _chestConstraint;
        [SerializeField] private MultiAimConstraint _shoulderConstraint;

        [Header("Aiming Settings")]
        [SerializeField] private float _aimWeight = 1f;
        [SerializeField] private float _spineWeight = 0.5f;
        [SerializeField] private float _chestWeight = 0.7f;
        [SerializeField] private float _shoulderWeight = 0.3f;

        [Header("Target Settings")]
        [SerializeField] private Transform _aimTarget;
        [SerializeField] private float _maxAimDistance = 15f;
        [SerializeField] private LayerMask _targetLayers = -1;

        [Header("Animation Blending")]
        [SerializeField] private float _blendSpeed = 5f;
        [SerializeField] private bool _smoothBlending = true;
        [SerializeField] private AnimationCurve _weightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        // Runtime state
        private bool _isAiming = false;
        private float _currentAimWeight = 0f;
        private Vector3 _lastTargetPosition;
        private WeightedTransformArray _spineSources;
        private WeightedTransformArray _chestSources;
        private WeightedTransformArray _shoulderSources;

        #endregion

        #region Properties

        /// <summary>
        /// Current aim target transform
        /// </summary>
        public Transform AimTarget => _aimTarget;

        /// <summary>
        /// Whether aiming is currently active
        /// </summary>
        public bool IsAiming => _isAiming;

        /// <summary>
        /// Current aim weight being applied
        /// </summary>
        public float CurrentAimWeight => _currentAimWeight;

        /// <summary>
        /// Maximum aim distance
        /// </summary>
        public float MaxAimDistance => _maxAimDistance;

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
        /// Sets the spine constraint weight
        /// </summary>
        /// <param name="weight">Spine weight (0-1)</param>
        public void SetSpineWeight(float weight)
        {
            _spineWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the chest constraint weight
        /// </summary>
        /// <param name="weight">Chest weight (0-1)</param>
        public void SetChestWeight(float weight)
        {
            _chestWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Sets the shoulder constraint weight
        /// </summary>
        /// <param name="weight">Shoulder weight (0-1)</param>
        public void SetShoulderWeight(float weight)
        {
            _shoulderWeight = Mathf.Clamp01(weight);
        }

        /// <summary>
        /// Enables or disables aiming behavior
        /// </summary>
        /// <param name="enabled">Whether aiming should be enabled</param>
        public void SetAimingEnabled(bool enabled)
        {
            if (!enabled)
            {
                ClearAimTarget();
            }
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
            if (_spineConstraint != null)
                _spineSources = _spineConstraint.data.sourceObjects;
            if (_chestConstraint != null)
                _chestSources = _chestConstraint.data.sourceObjects;
            if (_shoulderConstraint != null)
                _shoulderSources = _shoulderConstraint.data.sourceObjects;
        }

        private void ValidateConstraints()
        {
            if (_spineConstraint == null && _chestConstraint == null && _shoulderConstraint == null)
            {
                Debug.LogWarning("UpperBodyAimController: No rigging constraints assigned");
            }
        }

        private void InitializeConstraints()
        {
            // Set initial weights to 0
            if (_spineConstraint != null)
                _spineConstraint.weight = 0f;
            if (_chestConstraint != null)
                _chestConstraint.weight = 0f;
            if (_shoulderConstraint != null)
                _shoulderConstraint.weight = 0f;
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

            _isAiming = true;
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
            if (_spineConstraint != null)
            {
                _spineConstraint.weight = _currentAimWeight * _spineWeight;
                UpdateConstraintTarget(_spineConstraint, _spineSources);
            }

            if (_chestConstraint != null)
            {
                _chestConstraint.weight = _currentAimWeight * _chestWeight;
                UpdateConstraintTarget(_chestConstraint, _chestSources);
            }

            if (_shoulderConstraint != null)
            {
                _shoulderConstraint.weight = _currentAimWeight * _shoulderWeight;
                UpdateConstraintTarget(_shoulderConstraint, _shoulderSources);
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
        }

        #endregion
    }
}
