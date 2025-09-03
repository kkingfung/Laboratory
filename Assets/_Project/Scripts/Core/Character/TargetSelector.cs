using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.Core.Character
{
    /// <summary>
    /// Manages target selection for character aiming and interaction systems.
    /// Provides target detection, prioritization, and validation for various character behaviors.
    /// </summary>
    public class TargetSelector : MonoBehaviour
    {
        #region Fields

        [Header("Detection Settings")]
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private float _maxDetectionDistance = 15f;
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private bool _useProximityDetection = true;
        [SerializeField] private bool _useRaycastDetection = true;

        [Header("Target Prioritization")]
        [SerializeField] private float _distanceWeight = 1f;
        [SerializeField] private float _angleWeight = 0.5f;
        [SerializeField] private float _visibilityWeight = 0.8f;
        [SerializeField] private bool _prioritizeClosest = true;

        [Header("Camera Integration")]
        [SerializeField] private UnityEngine.Camera _playerCamera;
        #pragma warning disable 0414 // Field assigned but never used - intended for future camera integration
        [SerializeField] private float _screenCenterWeight = 0.3f;
        [SerializeField] private bool _useCameraCenterBias = true;
        #pragma warning restore 0414

        [Header("Target Validation")]
        [SerializeField] private bool _validateLineOfSight = true;
        [SerializeField] private LayerMask _obstacleLayers = -1;
        [SerializeField] private float _minTargetSize = 0.1f;

        // Runtime state
        private List<Transform> _detectedTargets = new List<Transform>();
        private Transform _currentTarget;
        private float _lastDetectionTime;
        private Vector3 _lastPlayerPosition;
        private Dictionary<Transform, float> _targetScores = new Dictionary<Transform, float>();

        #endregion

        #region Properties

        /// <summary>
        /// Currently selected target transform
        /// </summary>
        public Transform CurrentTarget => _currentTarget;

        /// <summary>
        /// List of all detected targets
        /// </summary>
        public IReadOnlyList<Transform> DetectedTargets => _detectedTargets.AsReadOnly();

        /// <summary>
        /// Number of currently detected targets
        /// </summary>
        public int TargetCount => _detectedTargets.Count;

        /// <summary>
        /// Whether any targets are currently detected
        /// </summary>
        public bool HasTargets => _detectedTargets.Count > 0;

        /// <summary>
        /// Detection radius for target finding
        /// </summary>
        public float DetectionRadius => _detectionRadius;

        /// <summary>
        /// Whether to prioritize closest target over scoring system
        /// </summary>
        public bool PrioritizeClosest => _prioritizeClosest;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeTargetSelector();
        }

        private void Start()
        {
            if (_playerCamera == null)
                _playerCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            UpdateTargetDetection();
            UpdateTargetSelection();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Sets the detection radius for target finding
        /// </summary>
        /// <param name="radius">Detection radius in units</param>
        public void SetDetectionRadius(float radius)
        {
            _detectionRadius = Mathf.Max(0.1f, radius);
        }

        /// <summary>
        /// Sets the maximum detection distance
        /// </summary>
        /// <param name="distance">Maximum distance in units</param>
        public void SetMaxDetectionDistance(float distance)
        {
            _maxDetectionDistance = Mathf.Max(0.1f, distance);
        }

        /// <summary>
        /// Sets the target layers for detection
        /// </summary>
        /// <param name="layers">Layer mask for valid targets</param>
        public void SetTargetLayers(LayerMask layers)
        {
            _targetLayers = layers;
        }

        /// <summary>
        /// Sets whether to prioritize closest target over scoring system
        /// </summary>
        /// <param name="prioritize">True to prioritize closest target</param>
        public void SetPrioritizeClosest(bool prioritize)
        {
            _prioritizeClosest = prioritize;
        }

        /// <summary>
        /// Manually sets the current target
        /// </summary>
        /// <param name="target">Target transform to set</param>
        public void SetCurrentTarget(Transform target)
        {
            if (target == null || !_detectedTargets.Contains(target))
            {
                _currentTarget = null;
                return;
            }

            _currentTarget = target;
        }

        /// <summary>
        /// Clears the current target selection
        /// </summary>
        public void ClearCurrentTarget()
        {
            _currentTarget = null;
        }

        /// <summary>
        /// Forces a target detection update
        /// </summary>
        public void ForceTargetUpdate()
        {
            _lastDetectionTime = 0f;
            UpdateTargetDetection();
        }

        /// <summary>
        /// Gets the target with the highest priority score
        /// </summary>
        /// <returns>Highest priority target or null if none found</returns>
        public Transform GetHighestPriorityTarget()
        {
            if (_detectedTargets.Count == 0) return null;

            // If prioritize closest is enabled, simply return the closest target
            if (_prioritizeClosest)
            {
                return GetClosestTarget();
            }

            // Otherwise, use the scoring system
            Transform bestTarget = null;
            float bestScore = float.MinValue;

            foreach (var target in _detectedTargets)
            {
                if (_targetScores.TryGetValue(target, out float score) && score > bestScore)
                {
                    bestScore = score;
                    bestTarget = target;
                }
            }

            return bestTarget;
        }

        /// <summary>
        /// Gets all targets within a specific distance
        /// </summary>
        /// <param name="distance">Maximum distance to check</param>
        /// <returns>List of targets within the specified distance</returns>
        public List<Transform> GetTargetsWithinDistance(float distance)
        {
            var targets = new List<Transform>();
            foreach (var target in _detectedTargets)
            {
                if (Vector3.Distance(transform.position, target.position) <= distance)
                {
                    targets.Add(target);
                }
            }
            return targets;
        }

        /// <summary>
        /// Gets the closest target from the detected targets list
        /// </summary>
        /// <returns>Closest target or null if none found</returns>
        public Transform GetClosestTarget()
        {
            if (_detectedTargets.Count == 0) return null;

            Transform closestTarget = null;
            float closestDistance = float.MaxValue;

            foreach (var target in _detectedTargets)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestTarget = target;
                }
            }

            return closestTarget;
        }

        #endregion

        #region Private Methods

        private void InitializeTargetSelector()
        {
            _lastPlayerPosition = transform.position;
            _detectedTargets.Clear();
            _targetScores.Clear();
        }

        private void UpdateTargetDetection()
        {
            // Check if we need to update detection
            if (Time.time - _lastDetectionTime < 0.1f) return;

            _lastDetectionTime = Time.time;
            _lastPlayerPosition = transform.position;

            // Clear previous detection
            _detectedTargets.Clear();
            _targetScores.Clear();

            // Perform target detection
            if (_useProximityDetection)
                DetectTargetsByProximity();

            if (_useRaycastDetection)
                DetectTargetsByRaycast();

            // Calculate target scores
            CalculateTargetScores();
        }

        private void DetectTargetsByProximity()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, _detectionRadius, _targetLayers);
            
            foreach (var hit in hits)
            {
                if (hit.transform != transform && !_detectedTargets.Contains(hit.transform))
                {
                    float distance = Vector3.Distance(transform.position, hit.transform.position);
                    if (distance <= _maxDetectionDistance)
                    {
                        if (ValidateTarget(hit.transform))
                        {
                            _detectedTargets.Add(hit.transform);
                        }
                    }
                }
            }
        }

        private void DetectTargetsByRaycast()
        {
            if (_playerCamera == null) return;

            // Cast rays from camera center to find targets
            Ray ray = _playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            RaycastHit[] hits = Physics.RaycastAll(ray, _maxDetectionDistance, _targetLayers);

            foreach (var hit in hits)
            {
                if (hit.transform != transform && !_detectedTargets.Contains(hit.transform))
                {
                    if (ValidateTarget(hit.transform))
                    {
                        _detectedTargets.Add(hit.transform);
                    }
                }
            }
        }

        private bool ValidateTarget(Transform target)
        {
            if (target == null) return false;

            // Check minimum size
            if (_minTargetSize > 0f)
            {
                var renderer = target.GetComponent<Renderer>();
                if (renderer != null)
                {
                    float size = renderer.bounds.size.magnitude;
                    if (size < _minTargetSize) return false;
                }
            }

            // Check line of sight if enabled
            if (_validateLineOfSight)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                float distance = Vector3.Distance(transform.position, target.position);
                
                if (Physics.Raycast(transform.position, direction, distance, _obstacleLayers))
                {
                    return false;
                }
            }

            return true;
        }

        private void CalculateTargetScores()
        {
            foreach (var target in _detectedTargets)
            {
                float score = CalculateTargetScore(target);
                _targetScores[target] = score;
            }
        }

        private float CalculateTargetScore(Transform target)
        {
            if (target == null) return 0f;

            float score = 0f;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            // Distance score (closer is better)
            float distance = Vector3.Distance(transform.position, target.position);
            float distanceScore = 1f - (distance / _maxDetectionDistance);
            score += distanceScore * _distanceWeight;

            // Angle score (more forward-facing is better)
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            float angleScore = 1f - (angle / 180f);
            score += angleScore * _angleWeight;

            // Visibility score (more visible is better)
            if (_playerCamera != null)
            {
                Vector3 screenPoint = _playerCamera.WorldToViewportPoint(target.position);
                float screenCenterDistance = Vector2.Distance(screenPoint, new Vector2(0.5f, 0.5f));
                float visibilityScore = 1f - screenCenterDistance;
                score += visibilityScore * _visibilityWeight;
            }

            return score;
        }

        private void UpdateTargetSelection()
        {
            if (_detectedTargets.Count == 0)
            {
                _currentTarget = null;
                return;
            }

            // Auto-select best target if none is currently selected
            if (_currentTarget == null || !_detectedTargets.Contains(_currentTarget))
            {
                _currentTarget = GetHighestPriorityTarget();
            }
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Draw max detection distance
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, _maxDetectionDistance);

            // Draw current target line
            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_currentTarget.position, 0.3f);
            }

            // Draw detected targets
            Gizmos.color = Color.cyan;
            foreach (var target in _detectedTargets)
            {
                if (target != _currentTarget)
                {
                    Gizmos.DrawWireSphere(target.position, 0.2f);
                }
            }
        }

        #endregion
    }
}
